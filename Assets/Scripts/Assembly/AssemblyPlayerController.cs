using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using VRRangeAcademy.Player;

namespace VRRangeAcademy.Assembly
{
    [RequireComponent(typeof(CharacterController))]
    public class AssemblyPlayerController : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform leftHandAnchor;
        [SerializeField] private Transform rightHandAnchor;

        [Header("Movimiento (WASD / Thumbstick VR)")]
        [SerializeField] private float moveSpeed = 2.4f;
        [SerializeField] private float sprintMultiplier = 1.5f;
        [SerializeField] private float gravity = 9.81f;
        [SerializeField] private float jumpHeight = 1.1f;

        [Header("Giro (Ratón / Snap Turn VR)")]
        [SerializeField] private float mouseSensitivity = 1.8f;
        [SerializeField] private float minPitch = -75f;
        [SerializeField] private float maxPitch = 80f;
        [SerializeField] private float snapTurnAngle = 45f;

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
        private float snapTurnCooldown = 0f;

        private AssemblyPiece heldPiece = null;
        private AssemblyPiece hoveredPiece = null;
        private Plane dragPlane;
        private bool wasVrRightGrip = false;
        private bool wasVrLeftGrip = false;
        private bool wasVrToggleModePressed = false;
        private bool wasVrRangeShortcutPressed = false;
        private Transform activeGrabberTransform = null;

        private VRControllerTracker leftTracker;
        private VRControllerTracker rightTracker;

        private void Awake()
        {
            spawnPosition = transform.position;
            characterController = GetComponent<CharacterController>();

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
                if (playerCamera == null) playerCamera = Camera.main;
            }

            // Buscar o resolver anclajes de mandos
            if (leftHandAnchor == null)
            {
                leftHandAnchor = transform.Find("LeftHand_OculusTouch") ?? (playerCamera != null ? playerCamera.transform.Find("LeftHand_OculusTouch") : null);
            }
            if (rightHandAnchor == null)
            {
                rightHandAnchor = transform.Find("RightHand_OculusTouch") ?? (playerCamera != null ? playerCamera.transform.Find("RightHand_OculusTouch") : null);
            }

            // RE-PARENT: Los anclajes de mandos VR en OpenXR deben ser hijos del Rig (no de la cámara)
            // para que su posición local coincida con las coordenadas del tracking space de la habitación.
            if (leftHandAnchor != null && leftHandAnchor.parent == playerCamera?.transform)
            {
                leftHandAnchor.SetParent(transform, true);
            }
            if (rightHandAnchor != null && rightHandAnchor.parent == playerCamera?.transform)
            {
                rightHandAnchor.SetParent(transform, true);
            }

            // Adjuntar trackers 6DoF a los mandos
            if (leftHandAnchor != null)
            {
                leftTracker = leftHandAnchor.GetComponent<VRControllerTracker>() ?? leftHandAnchor.gameObject.AddComponent<VRControllerTracker>();
                leftTracker.SetNode(XRNode.LeftHand, new Vector3(-0.24f, 1.25f, 0.42f), new Vector3(15f, -10f, 0f));
            }
            if (rightHandAnchor != null)
            {
                rightTracker = rightHandAnchor.GetComponent<VRControllerTracker>() ?? rightHandAnchor.gameObject.AddComponent<VRControllerTracker>();
                rightTracker.SetNode(XRNode.RightHand, new Vector3(0.24f, 1.25f, 0.42f), new Vector3(15f, 10f, 0f));
            }

            if (playerCamera != null)
            {
                pitch = playerCamera.transform.localEulerAngles.x;
            }
            yaw = transform.eulerAngles.y;

            // Iniciar bloqueado en PC para jugar directamente
            SetCursorLock(true);
        }

        private void Update()
        {
            HandleCursorToggle();
            HandleZoneShortcuts();
            HandleLook();
            HandleMovement();
            UpdateVrControllers();
            HandleInteraction();
            UpdateHeldPiecePosition();
            CheckShootingRangeZone();
        }

        private void HandleZoneShortcuts()
        {
            // Atajos de teclado (PC)
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.xKey.wasPressedThisFrame)
                {
                    var mgr = Object.FindAnyObjectByType<WeaponAssemblyManager>();
                    if (mgr != null)
                    {
                        if (transform.position.x > 1.6f) mgr.OnNextWeaponClicked();
                        else mgr.ToggleWorkbenchMode();
                    }
                }

                if (Keyboard.current.tKey.wasPressedThisFrame)
                {
                    ToggleBetweenWorkbenchAndRange();
                }

                if (Keyboard.current.mKey.wasPressedThisFrame)
                {
                    var mgr = Object.FindAnyObjectByType<WeaponAssemblyManager>();
                    mgr?.ReturnToMainMenu();
                }
            }
#else
            if (Input.GetKeyDown(KeyCode.X))
            {
                var mgr = Object.FindAnyObjectByType<WeaponAssemblyManager>();
                if (mgr != null)
                {
                    if (transform.position.x > 1.6f) mgr.OnNextWeaponClicked();
                    else mgr.ToggleWorkbenchMode();
                }
            }
            if (Input.GetKeyDown(KeyCode.T)) ToggleBetweenWorkbenchAndRange();
            if (Input.GetKeyDown(KeyCode.M))
            {
                var mgr = Object.FindAnyObjectByType<WeaponAssemblyManager>();
                mgr?.ReturnToMainMenu();
            }
#endif

            // Atajos de mandos VR (Meta Quest 3)
            // Botón 'Y' (mano izquierda - secondaryButton): Alternar entre Mesa de Armado y Polígono de Tiro
            var leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            bool vrYPressed = false;
            if (leftDevice.isValid && leftDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool yVal) && yVal)
            {
                vrYPressed = true;
            }

            if (vrYPressed && !wasVrRangeShortcutPressed)
            {
                ToggleBetweenWorkbenchAndRange();
            }
            wasVrRangeShortcutPressed = vrYPressed;
        }

        private void ToggleBetweenWorkbenchAndRange()
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

        public void TeleportTo(Vector3 targetPosition, float targetYaw = 0f, float targetPitch = 0f)
        {
            if (characterController != null) characterController.enabled = false;

            transform.position = targetPosition;
            yaw = targetYaw;
            pitch = targetPitch;
            transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);

            if (playerCamera != null && !XRSettings.isDeviceActive)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(targetPitch, 0f, 0f);
            }

            if (characterController != null) characterController.enabled = true;
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
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    SetCursorLock(!isCursorLocked);
                }
            }
#else
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape))
            {
                SetCursorLock(!isCursorLocked);
            }
#endif
        }

        public void SetCursorLock(bool locked)
        {
            isCursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleLook()
        {
            // 1. Control con ratón en PC
#if ENABLE_INPUT_SYSTEM
            bool shouldLook = isCursorLocked || (Mouse.current != null && Mouse.current.rightButton.isPressed);
            if (shouldLook && Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                yaw += delta.x * mouseSensitivity * 0.1f;
                pitch -= delta.y * mouseSensitivity * 0.1f;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }
#else
            if (isCursorLocked || Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
                pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }
#endif

            // 2. Giro por pasos (Snap Turn) con Thumbstick Derecho en Meta Quest 3
            var rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (rightDevice.isValid && rightDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rStick))
            {
                if (snapTurnCooldown > 0f)
                {
                    snapTurnCooldown -= Time.deltaTime;
                }

                if (Mathf.Abs(rStick.x) > 0.60f && snapTurnCooldown <= 0f)
                {
                    float angle = Mathf.Sign(rStick.x) * snapTurnAngle;
                    yaw += angle;
                    snapTurnCooldown = 0.28f;
                }
                else if (Mathf.Abs(rStick.x) < 0.2f)
                {
                    snapTurnCooldown = 0f;
                }
            }

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // En PC se rota la cámara en pitch. En VR, OpenXR aplica el seguimiento de la cabeza automáticamente.
            if (playerCamera != null && !XRSettings.isDeviceActive)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            Vector2 inputDir = Vector2.zero;
            bool isSprinting = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) inputDir.y += 1f;
                if (Keyboard.current.sKey.isPressed) inputDir.y -= 1f;
                if (Keyboard.current.dKey.isPressed) inputDir.x += 1f;
                if (Keyboard.current.aKey.isPressed) inputDir.x -= 1f;
                isSprinting = Keyboard.current.leftShiftKey.isPressed;
            }
#else
            inputDir.x = Input.GetAxisRaw("Horizontal");
            inputDir.y = Input.GetAxisRaw("Vertical");
            isSprinting = Input.GetKey(KeyCode.LeftShift);
#endif

            // Lectura de Thumbstick Izquierdo en Meta Quest 3
            var leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (leftDevice.isValid)
            {
                if (leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 lStick))
                {
                    if (lStick.sqrMagnitude > 0.04f)
                    {
                        inputDir.x += lStick.x;
                        inputDir.y += lStick.y;
                    }
                }
                if (leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool lClick) && lClick)
                {
                    isSprinting = true;
                }
            }

            if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

            // Orientar movimiento según la dirección de mirada
            Transform orienter = (XRSettings.isDeviceActive && playerCamera != null) ? playerCamera.transform : transform;
            Vector3 forward = Vector3.ProjectOnPlane(orienter.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(orienter.right, Vector3.up).normalized;
            Vector3 move = forward * inputDir.y + right * inputDir.x;

            float currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1.0f);
            bool isGrounded = characterController.isGrounded;

            if (isGrounded)
            {
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = -2.0f; // Adherencia firme
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
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) return true;
#else
            if (Input.GetKeyDown(KeyCode.Space)) return true;
#endif

            // Mandos VR Meta Quest 3 (Botón 'A' en mano derecha)
            var rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            bool vrPressed = false;
            if (rightDevice.isValid && rightDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool primary) && primary)
            {
                vrPressed = true;
            }

            bool vrJump = vrPressed && !wasVrJumpPressed;
            wasVrJumpPressed = vrPressed;
            if (vrJump) return true;

            return false;
        }

        private void UpdateVrControllers()
        {
            var rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            var leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

            // 1. Seguimiento 6DoF de mandos Meta Quest 3 Touch Plus (si no tienen tracker independiente)
            if (rightHandAnchor != null && (rightTracker == null || !rightTracker.IsTracked))
            {
                if (rightDevice.isValid)
                {
                    if (rightDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rPos))
                        rightHandAnchor.localPosition = rPos;
                    if (rightDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rRot))
                        rightHandAnchor.localRotation = rRot;
                }
            }

            if (leftHandAnchor != null && (leftTracker == null || !leftTracker.IsTracked))
            {
                if (leftDevice.isValid)
                {
                    if (leftDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 lPos))
                        leftHandAnchor.localPosition = lPos;
                    if (leftDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion lRot))
                        leftHandAnchor.localRotation = lRot;
                }
            }

            // 2. Detección de Agarre con Mandos Quest 3 (Grip lateral o Gatillo frontal)
            bool rightGrip = false;
            if (rightDevice.isValid)
            {
                if (rightDevice.TryGetFeatureValue(CommonUsages.grip, out float rG)) rightGrip = rG > 0.45f;
                if (!rightGrip && rightDevice.TryGetFeatureValue(CommonUsages.trigger, out float rT)) rightGrip = rT > 0.45f;
            }

            bool leftGrip = false;
            if (leftDevice.isValid)
            {
                if (leftDevice.TryGetFeatureValue(CommonUsages.grip, out float lG)) leftGrip = lG > 0.45f;
                if (!leftGrip && leftDevice.TryGetFeatureValue(CommonUsages.trigger, out float lT)) leftGrip = lT > 0.45f;
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
                    TriggerHaptic(XRNode.RightHand, 0.85f, 0.15f);
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
                    TriggerHaptic(XRNode.LeftHand, 0.85f, 0.15f);
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

            // 3. Botón para alternar modo Armar / Desarmar en mandos VR (Botón 'X' en mano izquierda)
            bool vrToggleMode = false;
            if (leftDevice.isValid && leftDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool xBtn) && xBtn)
            {
                vrToggleMode = true;
            }
            if (vrToggleMode && !wasVrToggleModePressed)
            {
                var mgr = WeaponAssemblyManager.Instance;
                if (mgr != null)
                {
                    if (transform.position.x > 1.6f)
                    {
                        mgr.OnNextWeaponClicked();
                    }
                    else
                    {
                        mgr.ToggleWorkbenchMode();
                    }
                }
            }
            wasVrToggleModePressed = vrToggleMode;

            // 4. Botón 'B' en mano derecha para soltar pieza de forma forzada si estuviera atorada
            if (rightDevice.isValid && rightDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool bBtn) && bBtn && heldPiece != null)
            {
                DropHeldPiece();
            }
        }

        private AssemblyPiece FindNearbyOrRaycastPiece(Transform hand)
        {
            if (hand == null) return hoveredPiece;

            // Proximidad física directa (28cm alrededor del mando Touch Plus)
            Collider[] hits = Physics.OverlapSphere(hand.position, 0.28f, interactableLayer);
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

        public static void TriggerHaptic(XRNode node, float amplitude = 0.6f, float duration = 0.1f)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
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
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) actionPressed = true;
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) actionPressed = true;
#else
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E)) actionPressed = true;
#endif

            if (actionPressed)
            {
                if (heldPiece == null)
                {
                    // 1. Agarrar la pieza a la que apuntamos
                    if (hoveredPiece != null)
                    {
                        heldPiece = hoveredPiece;
                        activeGrabberTransform = rightHandAnchor != null ? rightHandAnchor : playerCamera.transform;
                        dragPlane = new Plane(-playerCamera.transform.forward, heldPiece.transform.position);
                        heldPiece.Grab(activeGrabberTransform);
                        TriggerHaptic(XRNode.RightHand, 0.8f, 0.15f);
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
                    // Soltar pieza actual
                    heldPiece.Release();
                    heldPiece = null;
                    activeGrabberTransform = null;
                }
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
#if ENABLE_INPUT_SYSTEM
                Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
                Vector2 mousePos = Input.mousePosition;
#endif
                Ray ray = playerCamera.ScreenPointToRay(mousePos);
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
            if (!showDesktopHUD || XRSettings.isDeviceActive) return;

            var equippedGun = GetComponentInChildren<VRRangeAcademy.Shooting.WeaponShootingController>()
                           ?? (Camera.main != null ? Camera.main.GetComponentInChildren<VRRangeAcademy.Shooting.WeaponShootingController>() : null);

            if (equippedGun == null && isCursorLocked && (hoveredPiece != null || heldPiece != null))
            {
                float centerX = Screen.width * 0.5f;
                float centerY = Screen.height * 0.5f;
                float dotSize = 6f;

                GUI.color = new Color(0.2f, 1f, 0.8f, 0.85f);
                GUI.DrawTexture(new Rect(centerX - dotSize * 0.5f, centerY - dotSize * 0.5f, dotSize, dotSize), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            if (heldPiece != null)
            {
                GUI.skin.label.fontSize = 18;
                GUI.skin.label.fontStyle = FontStyle.Bold;
                GUI.color = new Color(0.2f, 1f, 0.8f);
                string holdHint = WeaponAssemblyManager.Instance != null && WeaponAssemblyManager.Instance.currentMode == WorkbenchMode.Disassembly
                    ? $"Desmontando: {heldPiece.displayName} -> Colócala en la alfombrilla de armero"
                    : $"Sosteniendo: {heldPiece.displayName} -> Llévala al holograma para encastrarla";
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
                    ? $"[Grip / Clic] Desarmar {hoveredPiece.displayName}"
                    : $"[Grip / Clic] Agarrar {hoveredPiece.displayName}";
                GUI.Label(new Rect(Screen.width * 0.5f - 300f, Screen.height * 0.5f + 40f, 600f, 35f),
                    hoverHint, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
                GUI.color = Color.white;
            }

            GUI.skin.box.fontSize = 13;
            string modeName = WeaponAssemblyManager.Instance != null && WeaponAssemblyManager.Instance.currentMode == WorkbenchMode.Disassembly ? "DESARME" : "ARMAR";
            string controlsText = $"[MODO {modeName}] | [Quest 3: Grip / Trigger] Desarmar / Agarrar | [X] Modo Armar/Desarmar | [Y] Polígono | [Joystick Der] Giro";
            GUI.Box(new Rect(Screen.width * 0.5f - 620f, Screen.height - 40f, 1240f, 30f), controlsText);
        }
    }
}
