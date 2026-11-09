using UnityEngine;
using UnityEngine.EventSystems;

namespace VRRangeAcademy.UI
{
    /// <summary>
    /// Provee un rayo láser visual para interactuar con la interfaz espacial en VR y en el Editor.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class VRRaycastPointer : MonoBehaviour
    {
        [Header("Ray Settings")]
        [SerializeField] private float defaultLength = 3.0f;
        [SerializeField] private LayerMask uiLayerMask = ~0;
        [SerializeField] private Color rayColor = new Color(0.18f, 0.64f, 1.0f, 0.8f);

        private LineRenderer lineRenderer;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = 0.005f;
            lineRenderer.endWidth = 0.002f;
            lineRenderer.positionCount = 2;

            // Simple unlit material for the laser ray
            Material lineMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lineMat.color = rayColor;
            lineRenderer.material = lineMat;
        }

        private void Update()
        {
            Ray ray = new Ray(transform.position, transform.forward);
            Vector3 endPos = transform.position + transform.forward * defaultLength;

            if (Physics.Raycast(ray, out RaycastHit hit, defaultLength, uiLayerMask))
            {
                endPos = hit.point;
            }

            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, endPos);
        }
    }
}
