using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;
using VRRangeAcademy.Player;

namespace VRRangeAcademy.UI
{
    /// <summary>
    /// Provee un rayo láser visual e interactivo para interactuar con la interfaz espacial en VR (Meta Quest 3)
    /// y en el Editor de Unity mediante teclado y ratón.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class VRRaycastPointer : MonoBehaviour
    {
        [Header("Ray Settings")]
        [SerializeField] private float defaultLength = 5.0f;
        [SerializeField] private LayerMask raycastLayerMask = ~0;
        [SerializeField] private Color normalColor = new Color(0.18f, 0.64f, 1.0f, 0.75f);
        [SerializeField] private Color hoverColor = new Color(0.2f, 1.0f, 0.6f, 0.95f);
        [SerializeField] private Color clickColor = new Color(1.0f, 0.85f, 0.2f, 1.0f);

        [Header("Reticle Visual")]
        [SerializeField] private bool showReticle = true;
        [SerializeField] private float reticleSize = 0.018f;

        [Header("Referencias")]
        [SerializeField] private VRControllerTracker controllerTracker;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clickAudioClip;

        private LineRenderer lineRenderer;
        private GameObject reticleObj;
        private Renderer reticleRenderer;
        private Material lineMat;
        private Material reticleMat;

        private Button hoveredButton = null;
        private bool wasTriggerPressed = false;
        private float lastClickTime = 0f;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = 0.005f;
            lineRenderer.endWidth = 0.002f;
            lineRenderer.positionCount = 2;

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            lineMat = new Material(unlitShader);
            lineMat.color = normalColor;
            if (lineMat.HasProperty("_BaseColor")) lineMat.SetColor("_BaseColor", normalColor);
            lineRenderer.material = lineMat;

            if (controllerTracker == null)
            {
                controllerTracker = GetComponent<VRControllerTracker>() ?? GetComponentInParent<VRControllerTracker>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0.3f;
                }
            }

            CreateReticle(unlitShader);
        }

        private void CreateReticle(Shader shader)
        {
            if (!showReticle) return;

            reticleObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reticleObj.name = "VR_Pointer_Reticle";
            reticleObj.transform.localScale = Vector3.one * reticleSize;

            // Eliminar collider de la retícula para que no interfiera con los raycasts
            var col = reticleObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            reticleMat = new Material(shader);
            reticleMat.color = normalColor;
            if (reticleMat.HasProperty("_BaseColor")) reticleMat.SetColor("_BaseColor", normalColor);
            reticleRenderer = reticleObj.GetComponent<Renderer>();
            reticleRenderer.material = reticleMat;
        }

        private void OnDestroy()
        {
            if (reticleObj != null)
            {
                Destroy(reticleObj);
            }
        }

        private void OnDisable()
        {
            if (reticleObj != null) reticleObj.SetActive(false);
            if (lineRenderer != null) lineRenderer.enabled = false;
        }

        private void OnEnable()
        {
            if (lineRenderer != null) lineRenderer.enabled = true;
            if (reticleObj != null) reticleObj.SetActive(true);
        }

        private void Update()
        {
            Ray ray = new Ray(transform.position, transform.forward);
            Vector3 endPos = transform.position + transform.forward * defaultLength;
            bool hitSomething = false;
            Button hitButton = null;

            if (Physics.Raycast(ray, out RaycastHit hit, defaultLength, raycastLayerMask))
            {
                endPos = hit.point;
                hitSomething = true;
                hitButton = hit.collider.GetComponentInParent<Button>();

                // Si no tiene BoxCollider directo pero es un Canvas con GraphicRaycaster, buscar en UI
                if (hitButton == null)
                {
                    var parentSelectable = hit.collider.GetComponentInParent<Selectable>();
                    if (parentSelectable is Button b) hitButton = b;
                }
            }

            // Actualizar estado de hover
            if (hitButton != hoveredButton)
            {
                hoveredButton = hitButton;
                if (hoveredButton != null && hoveredButton.interactable)
                {
                    TriggerHaptic(0.35f, 0.05f);
                }
            }

            // Detección de clic (Gatillo VR o Botón A o Clic Izquierdo Desktop)
            bool isTriggerDown = CheckClickInput();
            bool justClicked = isTriggerDown && !wasTriggerPressed;
            wasTriggerPressed = isTriggerDown;

            Color currentColor = normalColor;
            if (hoveredButton != null && hoveredButton.interactable)
            {
                currentColor = isTriggerDown ? clickColor : hoverColor;

                if (justClicked && Time.time - lastClickTime > 0.25f)
                {
                    lastClickTime = Time.time;
                    ExecuteButtonClick(hoveredButton);
                }
            }

            // Actualizar visuales del láser y retícula
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, endPos);
            SetLaserColor(currentColor);

            if (reticleObj != null)
            {
                reticleObj.SetActive(hitSomething);
                if (hitSomething)
                {
                    reticleObj.transform.position = endPos - transform.forward * 0.005f;
                    reticleObj.transform.rotation = Quaternion.LookRotation(hit.normal);
                    reticleMat.color = currentColor;
                    if (reticleMat.HasProperty("_BaseColor")) reticleMat.SetColor("_BaseColor", currentColor);
                }
            }
        }

        private bool CheckClickInput()
        {
            // 1. VR Controller Tracker
            if (controllerTracker != null && controllerTracker.IsTracked)
            {
                if (controllerTracker.IsTriggerPressed(0.45f) || controllerTracker.IsPrimaryButtonPressed())
                {
                    return true;
                }
            }

            // 2. OpenXR InputDevices directo (Fallback para Mano Derecha)
            var rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (rightDevice.isValid)
            {
                if (rightDevice.TryGetFeatureValue(CommonUsages.trigger, out float trig) && trig > 0.45f) return true;
                if (rightDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool aBtn) && aBtn) return true;
            }

            // 3. Fallback Teclado / Ratón (Desktop / Editor)
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed) return true;
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.isPressed) return true;
#else
            if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.E)) return true;
#endif

            return false;
        }

        private void ExecuteButtonClick(Button btn)
        {
            TriggerHaptic(0.7f, 0.1f);

            if (audioSource != null && clickAudioClip != null)
            {
                audioSource.PlayOneShot(clickAudioClip, 0.8f);
            }

            btn.onClick.Invoke();
        }

        private void TriggerHaptic(float amplitude, float duration)
        {
            if (controllerTracker != null && controllerTracker.IsTracked)
            {
                controllerTracker.SendHaptic(amplitude, duration);
                return;
            }

            var rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (rightDevice.isValid)
            {
                rightDevice.SendHapticImpulse(0, amplitude, duration);
            }
        }

        private void SetLaserColor(Color color)
        {
            lineMat.color = color;
            if (lineMat.HasProperty("_BaseColor")) lineMat.SetColor("_BaseColor", color);
            lineRenderer.startColor = color;
            lineRenderer.endColor = new Color(color.r, color.g, color.b, color.a * 0.4f);
        }
    }
}
