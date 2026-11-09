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

        [Header("Movimiento con Teclado (WASD)")]
        [SerializeField] private float moveSpeed = 2.4f;
        [SerializeField] private float sprintMultiplier = 1.5f;
        [SerializeField] private float gravity = 9.81f;

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

        private AssemblyPiece heldPiece = null;
        private AssemblyPiece hoveredPiece = null;
        private Plane dragPlane;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
                if (playerCamera == null) playerCamera = Camera.main;
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
            HandleMouseLook();
            HandleMovement();
            HandleInteraction();
            UpdateHeldPiecePosition();
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

            // Gravedad básica
            if (characterController.isGrounded)
            {
                verticalVelocity = -0.5f;
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
            }

            Vector3 finalMove = move * currentSpeed + Vector3.up * verticalVelocity;
            characterController.Move(finalMove * Time.deltaTime);
        }

        private void HandleInteraction()
        {
            if (playerCamera == null) return;

            Ray ray = isCursorLocked
                ? playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : (Mouse.current != null ? playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue()) : default);

            // Detectar si estamos apuntando a una pieza
            hoveredPiece = null;
            if (heldPiece == null && Physics.Raycast(ray, out RaycastHit hit, maxInteractDistance, interactableLayer))
            {
                var piece = hit.collider.GetComponentInParent<AssemblyPiece>();
                if (piece != null && !piece.IsSnapped)
                {
                    hoveredPiece = piece;
                }
            }

            // Tecla de acción: Clic Izquierdo o tecla E
            bool actionPressed = false;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) actionPressed = true;
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) actionPressed = true;

            if (actionPressed)
            {
                if (heldPiece == null)
                {
                    // Agarrar la pieza a la que apuntamos
                    if (hoveredPiece != null)
                    {
                        heldPiece = hoveredPiece;
                        dragPlane = new Plane(-playerCamera.transform.forward, heldPiece.transform.position);
                        heldPiece.Grab(rightHandAnchor != null ? rightHandAnchor : playerCamera.transform);
                    }
                }
                else
                {
                    // Soltar pieza actual si se presiona de nuevo
                    heldPiece.Release();
                    heldPiece = null;
                }
            }

            // Soltar si soltamos el clic mientras arrastramos en modo cursor libre
            if (!isCursorLocked && Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame && heldPiece != null)
            {
                heldPiece.Release();
                heldPiece = null;
            }
        }

        private void UpdateHeldPiecePosition()
        {
            if (heldPiece == null || playerCamera == null) return;

            // Si la pieza se acopló en este frame, liberarla automáticamente
            if (heldPiece.IsSnapped)
            {
                heldPiece = null;
                return;
            }

            Vector3 targetHoldPos;
            if (isCursorLocked)
            {
                // Sostener justo enfrente del centro de la vista
                targetHoldPos = playerCamera.transform.position + playerCamera.transform.forward * holdDistance - playerCamera.transform.up * 0.12f;
            }
            else
            {
                // Seguir el rayo del cursor del ratón
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

            // Mover suavemente la pieza hacia la posición sostenida
            heldPiece.SetTargetPositionDirect(targetHoldPos, heldPiece.transform.rotation);
        }

        private void OnGUI()
        {
            if (!showDesktopHUD) return;

            // 1. Retícula central cuando el cursor está bloqueado
            if (isCursorLocked)
            {
                float centerX = Screen.width * 0.5f;
                float centerY = Screen.height * 0.5f;
                float crossSize = 10f;
                float thickness = 2f;

                Color crossColor = (hoveredPiece != null || heldPiece != null) ? new Color(0f, 1f, 0.7f, 0.9f) : new Color(1f, 1f, 1f, 0.6f);

                GUI.color = crossColor;
                // Barra horizontal
                GUI.DrawTexture(new Rect(centerX - crossSize, centerY - thickness * 0.5f, crossSize * 2f, thickness), Texture2D.whiteTexture);
                // Barra vertical
                GUI.DrawTexture(new Rect(centerX - thickness * 0.5f, centerY - crossSize, thickness, crossSize * 2f), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            // 2. Tooltip contextual de acción
            if (heldPiece != null)
            {
                GUI.skin.label.fontSize = 18;
                GUI.skin.label.fontStyle = FontStyle.Bold;
                GUI.color = new Color(0.2f, 1f, 0.8f);
                GUI.Label(new Rect(Screen.width * 0.5f - 250f, Screen.height * 0.5f + 40f, 500f, 35f),
                    $"Sosteniendo: {heldPiece.displayName} -> ¡Llévala al holograma para encastrarla!", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
                GUI.color = Color.white;
            }
            else if (hoveredPiece != null)
            {
                GUI.skin.label.fontSize = 18;
                GUI.skin.label.fontStyle = FontStyle.Bold;
                GUI.color = new Color(1f, 0.9f, 0.3f);
                GUI.Label(new Rect(Screen.width * 0.5f - 250f, Screen.height * 0.5f + 40f, 500f, 35f),
                    $"[Clic Izquierdo / E] Agarrar {hoveredPiece.displayName}", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
                GUI.color = Color.white;
            }

            // 3. Barra de ayuda inferior para teclado y ratón
            GUI.skin.box.fontSize = 13;
            string controlsText = "[W,A,S,D] Moverse | [Ratón] Mirar | [Clic Izquierdo / E] Agarrar / Soltar Pieza | [Shift] Correr | [Tab] Alternar Cursor";
            GUI.Box(new Rect(Screen.width * 0.5f - 390f, Screen.height - 40f, 780f, 30f), controlsText);
        }
    }
}
