using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VRRangeAcademy.Player
{
    /// <summary>
    /// Permite probar y controlar el simulador tanto en Meta Quest 3 (VR)
    /// como en PC mediante teclado y ratón.
    /// </summary>
    public class DesktopPlayerController : MonoBehaviour
    {
        [Header("Movement & Physics Settings")]
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float sprintMultiplier = 1.6f;
        [SerializeField] private bool allowMovement = true;
        [SerializeField] private float gravity = 9.81f;
        [SerializeField] private float jumpHeight = 1.1f;

        [Header("Look & Turn Settings")]
        [SerializeField] private float mouseSensitivity = 2.0f;
        [SerializeField] private float pitchClamp = 80.0f;
        [SerializeField] private float snapTurnAngle = 45.0f;
        [SerializeField] private Camera playerCamera;

        [Header("Interaction Settings")]
        [SerializeField] private float maxInteractDistance = 4.0f;
        [SerializeField] private LayerMask interactableLayer = ~0;

        [Header("Desktop HUD")]
        [SerializeField] private bool showDesktopHints = true;

        private float rotationX = 0f;
        private float rotationY = 0f;
        private bool isCursorLocked = false;
        private CharacterController characterController;
        private float verticalVelocity = 0f;
        private bool wasVrJumpPressed = false;
        private Vector3 spawnPosition;
        private float snapTurnCooldown = 0f;

        private void Awake()
        {
            spawnPosition = transform.position;

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
                if (playerCamera == null) playerCamera = Camera.main;
            }

            characterController = GetComponent<CharacterController>();

            // En PC iniciar con cursor libre para poder hacer clic en los menús cómodamente
            SetCursorLock(false);

            if (playerCamera != null)
            {
                rotationY = transform.eulerAngles.y;
                rotationX = playerCamera.transform.localEulerAngles.x;
            }
        }

        private void Update()
        {
            HandleCursorToggle();

            HandleLookAndTurn();
            HandleMovement();
            HandleCenterRayInteraction();
        }

        private void HandleCursorToggle()
        {
            bool togglePressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                togglePressed = Keyboard.current.tabKey.wasPressedThisFrame ||
                                Keyboard.current.escapeKey.wasPressedThisFrame;
            }
#else
            togglePressed = Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape);
#endif

            if (togglePressed)
            {
                SetCursorLock(!isCursorLocked);
            }
        }

        public void SetCursorLock(bool locked)
        {
            isCursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleLookAndTurn()
        {
            // 1. Ratón en PC (cuando el cursor está bloqueado o manteniendo botón derecho)
            if (isCursorLocked || IsRightMouseButtonHeld())
            {
                Vector2 mouseDelta = GetMouseDelta();
                rotationY += mouseDelta.x * mouseSensitivity * 0.1f;
                rotationX -= mouseDelta.y * mouseSensitivity * 0.1f;
                rotationX = Mathf.Clamp(rotationX, -pitchClamp, pitchClamp);
            }

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
                    rotationY += angle;
                    snapTurnCooldown = 0.28f; // Intervalo para evitar mareo en VR
                }
                else if (Mathf.Abs(rStick.x) < 0.2f)
                {
                    snapTurnCooldown = 0f;
                }
            }

            transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

            // En PC aplicamos pitch a la cámara. En VR, OpenXR aplica el tracking del HMD automáticamente.
            if (playerCamera != null && !XRSettings.isDeviceActive)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            if (!allowMovement) return;

            Vector2 input = GetMovementInput();
            bool isSprinting = IsSprintHeld();

            float speed = moveSpeed * (isSprinting ? sprintMultiplier : 1.0f);

            // Si hay visor VR activo, orientar movimiento según la dirección hacia donde mira la cámara
            Transform orienter = (XRSettings.isDeviceActive && playerCamera != null) ? playerCamera.transform : transform;
            Vector3 forward = Vector3.ProjectOnPlane(orienter.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(orienter.right, Vector3.up).normalized;

            Vector3 direction = (forward * input.y + right * input.x);
            if (direction.sqrMagnitude > 1f) direction.Normalize();

            Vector3 horizontalMove = direction * speed;

            bool isGrounded = characterController != null && characterController.isGrounded;

            if (isGrounded)
            {
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = -2.0f; // Adherencia al suelo
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

            Vector3 totalMove = horizontalMove + Vector3.up * verticalVelocity;

            if (characterController != null && characterController.enabled)
            {
                characterController.Move(totalMove * Time.deltaTime);
            }
            else
            {
                transform.position += totalMove * Time.deltaTime;
            }

            // Resguardo por caída al vacío
            if (transform.position.y < -10f)
            {
                if (characterController != null) characterController.enabled = false;
                transform.position = spawnPosition;
                if (characterController != null) characterController.enabled = true;
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

            // Mandos VR Meta Quest 3 (Botón 'A' en controlador derecho)
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

        private void HandleCenterRayInteraction()
        {
            bool interactPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) interactPressed = true;
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) interactPressed = true;
#else
            interactPressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E);
#endif

            // En VR, si el usuario no tiene mandos o usa mirada central
            var rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (rightDevice.isValid && rightDevice.TryGetFeatureValue(CommonUsages.trigger, out float trig) && trig > 0.6f)
            {
                interactPressed = true;
            }

            if (interactPressed && playerCamera != null)
            {
                Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, maxInteractDistance, interactableLayer))
                {
                    var button = hit.collider.GetComponentInParent<Button>();
                    if (button != null && button.interactable)
                    {
                        button.onClick.Invoke();
                    }
                }
            }
        }

        #region Input Helpers
        private Vector2 GetMovementInput()
        {
            float x = 0f;
            float y = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            }
#else
            x = Input.GetAxisRaw("Horizontal");
            y = Input.GetAxisRaw("Vertical");
#endif

            // Thumbstick Izquierdo en Meta Quest 3
            var leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (leftDevice.isValid && leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 lStick))
            {
                if (lStick.sqrMagnitude > 0.04f)
                {
                    x += lStick.x;
                    y += lStick.y;
                }
            }

            Vector2 move = new Vector2(x, y);
            if (move.sqrMagnitude > 1f) move.Normalize();
            return move;
        }

        private Vector2 GetMouseDelta()
        {
            float dx = 0f;
            float dy = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                dx = delta.x;
                dy = delta.y;
            }
#else
            dx = Input.GetAxis("Mouse X") * 10f;
            dy = Input.GetAxis("Mouse Y") * 10f;
#endif

            return new Vector2(dx, dy);
        }

        private bool IsRightMouseButtonHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.isPressed;
#else
            return Input.GetMouseButton(1);
#endif
        }

        private bool IsSprintHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) return true;
#else
            if (Input.GetKey(KeyCode.LeftShift)) return true;
#endif

            // Mandos Quest 3: Presionar Thumbstick Izquierdo o Grip
            var leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (leftDevice.isValid)
            {
                if (leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool click) && click) return true;
                if (leftDevice.TryGetFeatureValue(CommonUsages.grip, out float grip) && grip > 0.6f) return true;
            }

            return false;
        }
        #endregion

        private void OnGUI()
        {
            if (!showDesktopHints || XRSettings.isDeviceActive) return;

            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 13;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleLeft;

            string status = isCursorLocked ? "<color=#00FF88>MIRAR ACTIVO</color>" : "<color=#38B6FF>CURSOR LIBRE (CLIC)</color>";
            string hint = $"<b>[Modo PC / Teclado]</b> Estado: {status}\n" +
                          "• <b>WASD / Joystick Izq:</b> Moverse  |  <b>Shift / Grip:</b> Correr  |  <b>Espacio / Botón A:</b> Saltar\n" +
                          "• <b>Tab / Esc:</b> Alternar Bloqueo de Ratón / Puntero\n" +
                          "• <b>Gatillo Index / Clic Izquierdo:</b> Seleccionar botones del Menú\n" +
                          "• <b>Joystick Der:</b> Giro por pasos (Snap Turn) 45°";

            GUI.Box(new Rect(15, 15, 430, 95), hint, style);

            if (isCursorLocked)
            {
                float size = 8f;
                float cx = Screen.width * 0.5f;
                float cy = Screen.height * 0.5f;
                GUI.color = new Color(0.2f, 0.7f, 1f, 0.9f);
                GUI.Box(new Rect(cx - size * 0.5f, cy - size * 0.5f, size, size), GUIContent.none);
            }
        }
    }
}
