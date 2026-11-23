using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VRRangeAcademy.Player
{
    /// <summary>
    /// Permite probar y controlar el simulador en PC utilizando teclado y ratón
    /// como alternativa o complemento a los visores VR (Meta Quest 3).
    /// </summary>
    public class DesktopPlayerController : MonoBehaviour
    {
        [Header("Movement & Physics Settings")]
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float sprintMultiplier = 1.6f;
        [SerializeField] private bool allowMovement = true;
        [SerializeField] private float gravity = 9.81f;
        [SerializeField] private float jumpHeight = 1.1f;

        [Header("Mouse Look Settings")]
        [SerializeField] private float mouseSensitivity = 2.0f;
        [SerializeField] private float pitchClamp = 80.0f;
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

        private void Awake()
        {
            spawnPosition = transform.position;

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
                if (playerCamera == null) playerCamera = Camera.main;
            }

            characterController = GetComponent<CharacterController>();

            // Iniciar con cursor libre para poder hacer clic en los menús cómodamente
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

            if (isCursorLocked)
            {
                HandleMouseLook();
                HandleMovement();
                HandleCenterRayInteraction();
            }
            else
            {
                // Si el cursor está libre, permitimos rotación manteniendo presionado el botón derecho
                if (IsRightMouseButtonHeld())
                {
                    HandleMouseLook();
                }
                HandleMovement();
            }
        }

        private void HandleCursorToggle()
        {
            // Tab o Escape para alternar entre mirar con el ratón o mover el cursor
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

        private void HandleMouseLook()
        {
            Vector2 mouseDelta = GetMouseDelta();

            rotationY += mouseDelta.x * mouseSensitivity * 0.1f;
            rotationX -= mouseDelta.y * mouseSensitivity * 0.1f;
            rotationX = Mathf.Clamp(rotationX, -pitchClamp, pitchClamp);

            transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
            if (playerCamera != null)
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
            Vector3 direction = (transform.forward * input.y + transform.right * input.x).normalized;

            Vector3 horizontalMove = direction * speed;

            bool isGrounded = characterController != null && characterController.isGrounded;

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

            // Soporte para mandos VR (Meta Quest 3 - Botón A en controlador derecho)
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

        private void HandleCenterRayInteraction()
        {
            // Cuando el ratón está bloqueado (mira al centro de la pantalla), hacer clic izquierdo o E activa botones
            bool interactPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) interactPressed = true;
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) interactPressed = true;
#else
            interactPressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E);
#endif

            if (interactPressed && playerCamera != null)
            {
                Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, maxInteractDistance, interactableLayer))
                {
                    // Buscar si tiene botón UI o interfaz
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

            return new Vector2(x, y).normalized;
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
            return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
#else
            return Input.GetKey(KeyCode.LeftShift);
#endif
        }
        #endregion

        private void OnGUI()
        {
            if (!showDesktopHints) return;

            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 13;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleLeft;

            string status = isCursorLocked ? "<color=#00FF88>MIRAR ACTIVO</color>" : "<color=#38B6FF>CURSO LIBRE (CLIC)</color>";
            string hint = $"<b>[Modo PC / Teclado]</b> Estado: {status}\n" +
                          "• <b>WASD:</b> Moverse  |  <b>Shift:</b> Correr  |  <b>Espacio:</b> Saltar\n" +
                          "• <b>Tab / Esc:</b> Alternar Bloqueo de Ratón / Puntero\n" +
                          "• <b>Clic Izquierdo:</b> Seleccionar botones del Menú\n" +
                          "• <b>Clic Derecho (mantener):</b> Mirar alrededor libremente";

            GUI.Box(new Rect(15, 15, 410, 95), hint, style);

            // Crosshair en el centro cuando el cursor está bloqueado
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
