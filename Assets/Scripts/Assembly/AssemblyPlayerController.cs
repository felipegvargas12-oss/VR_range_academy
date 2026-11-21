using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRRangeAcademy.Assembly
{
    [RequireComponent(typeof(CharacterController))]
    public class AssemblyPlayerController : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform leftHandAnchor;
        [SerializeField] private Transform rightHandAnchor;

        [Header("Movimiento con Teclado (WASD) y Físicas")]
        [SerializeField] private float moveSpeed = 2.4f;
        [SerializeField] private float sprintMultiplier = 1.5f;
        [SerializeField] private float gravity = 9.81f;
        [SerializeField] private float jumpHeight = 1.1f;

        [Header("Control de Vista con Ratón")]
        [SerializeField] private float mouseSensitivity = 1.8f;
        [SerializeField] private float minPitch = -75f;
        [SerializeField] private float maxPitch = 80f;

        [Header("Mecánica de Agarre (Pickup & Hold)")]
        [SerializeField] private float maxInteractDistance = 3.5f;
        [SerializeField] private float holdDistance = 0.65f;
        [SerializeField] private LayerMask interactableLayer = ~0;

        [Header("Desktop HUD / Retícula")]
        [SerializeField] private bool showDesktopHUD = true;

        private CharacterController characterController;
        private float pitch = 15f;
        private float yaw = 0f;
        private bool isCursorLocked = true;
        private float verticalVelocity = 0f;
        private bool wasVrJumpPressed = false;
        private Vector3 spawnPosition;

        private AssemblyPiece heldPiece = null;
        private AssemblyPiece hoveredPiece = null;
        private Plane dragPlane;
        private bool wasVrRightGrip = false;
        private bool wasVrLeftGrip = false;
        private bool wasVrToggleModePressed = false;
        private Transform activeGrabberTransform = null;

        private void Awake()
        {
            spawnPosition = transform.position;
            characterController = GetComponent<CharacterController>();
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
                if (playerCamera == null) playerCamera = Camera.main;
            }

            if (leftHandAnchor == null)
            {
                leftHandAnchor = playerCamera != null ? playerCamera.transform.Find("LeftHand_OculusTouch") : transform.Find("Main Camera/LeftHand_OculusTouch");
            }
            if (rightHandAnchor == null)
            {
                rightHandAnchor = playerCamera != null ? playerCamera.transform.Find("RightHand_OculusTouch") : transform.Find("Main Camera/RightHand_OculusTouch");
            }

            if (playerCamera != null)
            {
                pitch = playerCamera.transform.localEulerAngles.x;
            }
            yaw = transform.eulerAngles.y;

            // Iniciar bloqueado para jugar directamente como en un FPS / VR simulator
            SetCursorLock(true);
        }

        private void Update()
        {
            HandleCursorToggle();
            HandleZoneShortcuts();
            HandleMouseLook();
            HandleMovement();
            UpdateVrControllers();
            HandleInteraction();
            UpdateHeldPiecePosition();
            CheckShootingRangeZone();
        }

        private void HandleZoneShortcuts()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.xKey.wasPressedThisFrame)
            {
                if (WeaponAssemblyManager.Instance != null)
                {
                    WeaponAssemblyManager.Instance.ToggleWorkbenchMode();
                }
            }

            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                var mgr = Object.FindAnyObjectByType<WeaponAssemblyManager>();
                if (mgr != null)
                {
                    if (transform.position.x < 1.6f)
                    {
                        mgr.OnProceedToShootingRange();
                    }
                    else
                    {
                        mgr.OnReturnToWorkbench();
                    }
                }
            }
        }

        private void CheckShootingRangeZone()
        {
            // Si el jugador entra al polígono (X > 1.6), asegurarse de equipar el arma funcional
            if (transform.position.x > 1.6f)
            {
                var mgr = Object.FindAnyObjectByType<WeaponAssemblyManager>();
                if (mgr != null)
                {
                    mgr.EquipActiveWeapon();
                }
            }
        }

        private void HandleCursorToggle()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    SetCursorLock(!isCursorLocked);
                }
            }
        }

        public void SetCursorLock(bool locked)
        {
            isCursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleMouseLook()
        {
            bool shouldLook = isCursorLocked || (Mouse.current != null && Mouse.current.rightButton.isPressed);

            if (shouldLook && Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                yaw += delta.x * mouseSensitivity * 0.1f;
                pitch -= delta.y * mouseSensitivity * 0.1f;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                if (playerCamera != null)
                {
                    playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                }
            }
        }

        private void HandleMovement()
        {
            Vector2 inputDir = Vector2.zero;
            bool isSprinting = false;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) inputDir.y += 1f;
                if (Keyboard.current.sKey.isPressed) inputDir.y -= 1f;
                if (Keyboard.current.dKey.isPressed) inputDir.x += 1f;
                if (Keyboard.current.aKey.isPressed) inputDir.x -= 1f;
                isSprinting = Keyboard.current.leftShiftKey.isPressed;
            }

            inputDir.Normalize();

            // Dirección relativa a la rotación del jugador
            Vector3 move = transform.right * inputDir.x + transform.forward * inputDir.y;
            float currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1.0f);

            bool isGrounded = characterController.isGrounded;

            if (isGrounded)
            {
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = -2.0f; // Adherencia firme al suelo
                }

                if (IsJumpPressed())
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * gravity);
                }
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
            }

            Vector3 finalMove = move * currentSpeed + Vector3.up * verticalVelocity;
            characterController.Move(finalMove * Time.deltaTime);

            // Resguardo por caída al vacío
            if (transform.position.y < -10f)
            {
                characterController.enabled = false;
                transform.position = spawnPosition;
                characterController.enabled = true;
                verticalVelocity = 0f;
            }
        }

        private bool IsJumpPressed()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) return true;

            // Soporte para mandos VR (Meta Quest 3 - Botón A en mano derecha)
            var rightDevices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                UnityEngine.XR.InputDeviceCharacteristics.Right | UnityEngine.XR.InputDeviceCharacteristics.Controller,
                rightDevices
            );
            bool vrPressed = false;
            for (int i = 0; i < rightDevices.Count; i++)
            {
                if (rightDevices[i].TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool primary) && primary)
                {
                    vrPressed = true;
                    break;
                }
            }
            bool vrJump = vrPressed && !wasVrJumpPressed;
            wasVrJumpPressed = vrPressed;
            if (vrJump) return true;

            return false;
        }

        private void UpdateVrControllers()
        {
            // 1. Seguimiento 6DoF de mandos Meta Quest 3 Touch Plus
            var rightDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            if (rightDevice.isValid && rightHandAnchor != null)
            {
                if (rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 rPos))
                {
                    rightHandAnchor.localPosition = rPos;
                }
                if (rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion rRot))
                {
                    rightHandAnchor.localRotation = rRot;
                }
            }

            var leftDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
            if (leftDevice.isValid && leftHandAnchor != null)
            {
                if (leftDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 lPos))
                {
                    leftHandAnchor.localPosition = lPos;
                }
                if (leftDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion lRot))
                {
                    leftHandAnchor.localRotation = lRot;
                }
            }

            // 2. Detección de Agarre con Mandos Quest 3 (Grip o Trigger)
            bool rightGrip = false;
            if (rightDevice.isValid)
            {
                if (rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out float rG)) rightGrip = rG > 0.45f;
                if (!rightGrip && rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float rT)) rightGrip = rT > 0.45f;
            }

            bool leftGrip = false;
            if (leftDevice.isValid)
            {
                if (leftDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out float lG)) leftGrip = lG > 0.45f;
                if (!leftGrip && leftDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float lT)) leftGrip = lT > 0.45f;
            }

            // Mano Derecha: Agarre / Desarme
            if (rightGrip && !wasVrRightGrip && heldPiece == null)
            {
                var piece = FindNearbyOrRaycastPiece(rightHandAnchor);
                if (piece != null)
                {
                    heldPiece = piece;
                    activeGrabberTransform = rightHandAnchor;
                    heldPiece.Grab(rightHandAnchor);
                    TriggerHaptic(UnityEngine.XR.XRNode.RightHand, 0.85f, 0.15f);
                }
            }
            else if (!rightGrip && wasVrRightGrip && heldPiece != null && activeGrabberTransform == rightHandAnchor)
            {
                heldPiece.Release();
                heldPiece = null;
                activeGrabberTransform = null;
            }

            // Mano Izquierda: Agarre / Desarme
            if (leftGrip && !wasVrLeftGrip && heldPiece == null)
            {
                var piece = FindNearbyOrRaycastPiece(leftHandAnchor);
                if (piece != null)
                {
                    heldPiece = piece;
                    activeGrabberTransform = leftHandAnchor;
                    heldPiece.Grab(leftHandAnchor);
                    TriggerHaptic(UnityEngine.XR.XRNode.LeftHand, 0.85f, 0.15f);
                }
            }
            else if (!leftGrip && wasVrLeftGrip && heldPiece != null && activeGrabberTransform == leftHandAnchor)
            {
                heldPiece.Release();
                heldPiece = null;
                activeGrabberTransform = null;
            }

            wasVrRightGrip = rightGrip;
            wasVrLeftGrip = leftGrip;

            // 3. Botón para alternar modo Armar / Desarmar en mandos VR (Botón X en mano izquierda o A en derecha)
            bool vrToggleMode = false;
            if (leftDevice.isValid && leftDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool xBtn) && xBtn)
            {
                vrToggleMode = true;
            }
            if (rightDevice.isValid && rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool aBtn) && aBtn)
            {
                vrToggleMode = true;
            }
            if (vrToggleMode && !wasVrToggleModePressed)
            {
                if (WeaponAssemblyManager.Instance != null)
                {
                    WeaponAssemblyManager.Instance.ToggleWorkbenchMode();
                }
            }
            wasVrToggleModePressed = vrToggleMode;
        }

        private AssemblyPiece FindNearbyOrRaycastPiece(Transform hand)
        {
            if (hand == null) return hoveredPiece;

            // Proximidad física directa (24cm alrededor del mando Touch Plus)
            Collider[] hits = Physics.OverlapSphere(hand.position, 0.24f, interactableLayer);
            AssemblyPiece bestNear = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                var p = hits[i].GetComponentInParent<AssemblyPiece>();
                if (p != null)
                {
                    float d = Vector3.Distance(hand.position, p.transform.position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestNear = p;
                    }
                }
            }
            if (bestNear != null) return bestNear;

            // Raycast frontal desde el mando
            Ray ray = new Ray(hand.position, hand.forward);
            if (Physics.Raycast(ray, out RaycastHit rHit, maxInteractDistance, interactableLayer))
            {
                var p = rHit.collider.GetComponentInParent<AssemblyPiece>();
                if (p != null) return p;
            }

            return hoveredPiece;
        }

        public static void TriggerHaptic(UnityEngine.XR.XRNode node, float amplitude = 0.6f, float duration = 0.1f)
        {
            var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);
            if (device.isValid)
            {
                device.SendHapticImpulse(0, amplitude, duration);
            }
        }

        private void HandleInteraction()
        {
            if (playerCamera == null) return;

            Ray ray = isCursorLocked
                ? playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : (Mouse.current != null ? playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue()) : default);

            // Detectar si estamos apuntando a una pieza (suelta o ensamblada para desarmar)
            hoveredPiece = null;
            if (heldPiece == null && Physics.Raycast(ray, out RaycastHit hit, maxInteractDistance, interactableLayer))
            {
                var piece = hit.collider.GetComponentInParent<AssemblyPiece>();
                if (piece != null)
                {
                    hoveredPiece = piece;
                }
            }

            // Tecla de acción en Desktop: Clic Izquierdo o tecla E
            bool actionPressed = false;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) actionPressed = true;
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) actionPressed = true;

            if (actionPressed)
            {
                if (heldPiece == null)
                {
                    // 1. Agarrar la pieza a la que apuntamos (si está ensamblada, se desarma automáticamente)
                    if (hoveredPiece != null)
                    {
                        heldPiece = hoveredPiece;
                        activeGrabberTransform = rightHandAnchor != null ? rightHandAnchor : playerCamera.transform;
                        dragPlane = new Plane(-playerCamera.transform.forward, heldPiece.transform.position);
                        heldPiece.Grab(activeGrabberTransform);
                        TriggerHaptic(UnityEngine.XR.XRNode.RightHand, 0.8f, 0.15f);
                    }
                    // 2. Si no hay pieza, interactuar con botones de interfaz en espacio 3D
                    else if (Physics.Raycast(ray, out RaycastHit uiHit, maxInteractDistance, ~0))
                    {
                        var btn = uiHit.collider.GetComponentInParent<UnityEngine.UI.Button>();
                        if (btn != null && btn.interactable)
                        {
                            btn.onClick.Invoke();
                        }
                    }
                }
                else
                {
                    // Soltar pieza actual si se presiona de nuevo
                    heldPiece.Release();
                    heldPiece = null;
                    activeGrabberTransform = null;
                }
            }

            // Soltar si soltamos el clic mientras arrastramos en modo cursor libre
            if (!isCursorLocked && Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame && heldPiece != null && activeGrabberTransform == playerCamera.transform)
            {
                heldPiece.Release();
                heldPiece = null;
                activeGrabberTransform = null;
            }
        }

        public void DropHeldPiece()
        {
            if (heldPiece != null)
            {
                heldPiece.Release();
                heldPiece = null;
            }
            activeGrabberTransform = null;
            hoveredPiece = null;
        }

        private void UpdateHeldPiecePosition()
        {
            if (heldPiece == null || heldPiece.gameObject == null || !heldPiece.gameObject.activeInHierarchy)
            {
                heldPiece = null;
                activeGrabberTransform = null;
                return;
            }

            // Si la pieza se acopló en este frame, liberarla automáticamente
            if (heldPiece.IsSnapped)
            {
                heldPiece = null;
                activeGrabberTransform = null;
                return;
            }

            // Si está agarrada por una mano VR directa, su posición ya se actualiza en AssemblyPiece relativo a holdParent
            if (activeGrabberTransform != null && (activeGrabberTransform == rightHandAnchor || activeGrabberTransform == leftHandAnchor))
            {
                return;
            }

            // Modo Desktop (PC con cámara)
            if (playerCamera == null) return;
            Vector3 targetHoldPos;
            if (isCursorLocked)
            {
                targetHoldPos = playerCamera.transform.position + playerCamera.transform.forward * holdDistance - playerCamera.transform.up * 0.12f;
            }
            else
            {
                Ray ray = playerCamera.ScreenPointToRay(Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero);
                if (dragPlane.Raycast(ray, out float enter))
                {
                    targetHoldPos = ray.GetPoint(enter);
                }
                else
                {
                    targetHoldPos = playerCamera.transform.position + ray.direction * holdDistance;
                }
            }

            Quaternion targetHoldRot = playerCamera.transform.rotation * Quaternion.Euler(0f, 180f, 0f);
            heldPiece.SetTargetPositionDirect(targetHoldPos, targetHoldRot);
        }

        private void OnGUI()
        {
            if (!showDesktopHUD) return;

            // 1. Indicador interactivo solo para piezas sueltas en la mesa de ensamblaje (sin puntero de tiro en pantalla)
            var equippedGun = GetComponentInChildren<VRRangeAcademy.Shooting.WeaponShootingController>()
                           ?? (Camera.main != null ? Camera.main.GetComponentInChildren<VRRangeAcademy.Shooting.WeaponShootingController>() : null);

            // Si hay un arma equipada, se eliminan todas las retículas de pantalla para apuntar con las miras mecánicas del arma
            if (equippedGun == null && isCursorLocked && (hoveredPiece != null || heldPiece != null))
            {
                float centerX = Screen.width * 0.5f;
                float centerY = Screen.height * 0.5f;
                float dotSize = 6f;

                GUI.color = new Color(0.2f, 1f, 0.8f, 0.85f);
                GUI.DrawTexture(new Rect(centerX - dotSize * 0.5f, centerY - dotSize * 0.5f, dotSize, dotSize), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            // 2. Tooltip contextual de acción
            if (heldPiece != null)
            {
                GUI.skin.label.fontSize = 18;
                GUI.skin.label.fontStyle = FontStyle.Bold;
                GUI.color = new Color(0.2f, 1f, 0.8f);
                string holdHint = WeaponAssemblyManager.Instance != null && WeaponAssemblyManager.Instance.currentMode == WorkbenchMode.Disassembly
                    ? $"Desmontando: {heldPiece.displayName} -> ¡Colócala en la alfombrilla de armero!"
                    : $"Sosteniendo: {heldPiece.displayName} -> ¡Llévala al holograma para encastrarla!";
                GUI.Label(new Rect(Screen.width * 0.5f - 300f, Screen.height * 0.5f + 40f, 600f, 35f),
                    holdHint, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
                GUI.color = Color.white;
            }
            else if (hoveredPiece != null)
            {
                GUI.skin.label.fontSize = 18;
                GUI.skin.label.fontStyle = FontStyle.Bold;
                GUI.color = new Color(1f, 0.9f, 0.3f);
                string hoverHint = hoveredPiece.IsSnapped
                    ? $"[Quest 3 Grip / Clic] Desarmar {hoveredPiece.displayName}"
                    : $"[Quest 3 Grip / Clic] Agarrar {hoveredPiece.displayName}";
                GUI.Label(new Rect(Screen.width * 0.5f - 300f, Screen.height * 0.5f + 40f, 600f, 35f),
                    hoverHint, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
                GUI.color = Color.white;
            }

            // 3. Barra de ayuda inferior para VR Meta Quest 3 y Teclado/Ratón
            GUI.skin.box.fontSize = 13;
            string modeName = WeaponAssemblyManager.Instance != null && WeaponAssemblyManager.Instance.currentMode == WorkbenchMode.Disassembly ? "DESARME" : "ARMAR";
            string controlsText = $"[MODO {modeName}] | [Quest 3: Grip / Trigger] Desarmar / Agarrar | [X] Modo Armar/Desarmar | [Clic Der] Encarar Mira | [B] Modo Tiro | [T] Polígono | [Tab] Cursor";
            GUI.Box(new Rect(Screen.width * 0.5f - 620f, Screen.height - 40f, 1240f, 30f), controlsText);
        }
    }
}
