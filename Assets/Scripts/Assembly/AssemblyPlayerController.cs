using UnityEngine;
using UnityEngine.InputSystem;

namespace VRRangeAcademy.Assembly
{
    public class AssemblyPlayerController : MonoBehaviour
    {
        [Header("Referencias de Cámara y Manos")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform leftHandAnchor;
        [SerializeField] private Transform rightHandAnchor;

        [Header("Sensibilidad de Ratón (Desktop)")]
        [SerializeField] private float mouseSensitivity = 2.2f;
        [SerializeField] private float dragDistance = 0.85f;
        [SerializeField] private float minPitch = -70f;
        [SerializeField] private float maxPitch = 80f;

        [Header("Agarre y Raycast")]
        [SerializeField] private LayerMask interactableLayer = ~0;
        [SerializeField] private float maxRayDistance = 3.5f;

        private float pitch = 15f;
        private float yaw = 0f;
        private bool isCursorLocked = false;
        private AssemblyPiece currentlyHeldPiece = null;
        private Plane dragPlane;

        private void Start()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            // Inicializar ángulos con la rotación inicial
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            if (playerCamera != null)
            {
                pitch = playerCamera.transform.localEulerAngles.x;
            }

            // Iniciar cursor desbloqueado para interactuar cómodamente en editor
            SetCursorLock(false);
        }

        private void Update()
        {
            HandleCursorToggle();
            HandleMouseLook();
            HandleDesktopInteraction();
        }

        private void HandleCursorToggle()
        {
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                SetCursorLock(!isCursorLocked);
            }
        }

        private void SetCursorLock(bool locked)
        {
            isCursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleMouseLook()
        {
            // Rotar cámara si cursor está bloqueado O si se mantiene clic derecho
            bool shouldRotate = isCursorLocked || (Mouse.current != null && Mouse.current.rightButton.isPressed);

            if (shouldRotate && Mouse.current != null)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                yaw += mouseDelta.x * mouseSensitivity * 0.1f;
                pitch -= mouseDelta.y * mouseSensitivity * 0.1f;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                if (playerCamera != null)
                {
                    playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                }
            }
        }

        private void HandleDesktopInteraction()
        {
            if (Mouse.current == null || playerCamera == null) return;

            // Click izquierdo presionado: Intentar agarrar
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Ray ray = isCursorLocked
                    ? playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                    : playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

                if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, interactableLayer))
                {
                    var piece = hit.collider.GetComponentInParent<AssemblyPiece>();
                    if (piece != null && !piece.IsSnapped)
                    {
                        currentlyHeldPiece = piece;
                        // Crear plano perpendicular a la cámara a la distancia del objeto
                        dragPlane = new Plane(-playerCamera.transform.forward, hit.point);
                        piece.Grab(rightHandAnchor != null ? rightHandAnchor : transform);
                    }
                }
            }

            // Manteniendo click izquierdo: Arrastrar pieza en el espacio
            if (Mouse.current.leftButton.isPressed && currentlyHeldPiece != null)
            {
                Ray ray = isCursorLocked
                    ? playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                    : playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

                if (dragPlane.Raycast(ray, out float enter))
                {
                    Vector3 targetWorldPos = ray.GetPoint(enter);
                    // Suavizar posición hacia el punto del rayo
                    currentlyHeldPiece.SetTargetPositionDirect(targetWorldPos, currentlyHeldPiece.transform.rotation);
                }
                else
                {
                    Vector3 fallbackPos = playerCamera.transform.position + ray.direction * dragDistance;
                    currentlyHeldPiece.SetTargetPositionDirect(fallbackPos, currentlyHeldPiece.transform.rotation);
                }

                // Si la pieza se acopló durante el arrastre, liberarla
                if (currentlyHeldPiece.IsSnapped)
                {
                    currentlyHeldPiece = null;
                }
            }

            // Click izquierdo soltado: Liberar pieza
            if (Mouse.current.leftButton.wasReleasedThisFrame && currentlyHeldPiece != null)
            {
                currentlyHeldPiece.Release();
                currentlyHeldPiece = null;
            }
        }
    }
}
