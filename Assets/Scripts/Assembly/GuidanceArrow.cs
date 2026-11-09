using UnityEngine;

namespace VRRangeAcademy.Assembly
{
    [RequireComponent(typeof(LineRenderer))]
    public class GuidanceArrow : MonoBehaviour
    {
        [Header("Transforms de Guía")]
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform endPoint;

        [Header("Configuración de Curva Bézier")]
        [SerializeField] private int resolution = 28;
        [SerializeField] private float curveArcHeight = 0.28f;
        [SerializeField] private float lateralArcOffset = 0.08f;

        [Header("Animación de Pulso")]
        [SerializeField] private float scrollSpeed = 2.0f;
        [SerializeField] private Color glowColor = new Color(0f, 0.85f, 1f, 0.9f);
        [SerializeField] private Color tipColor = new Color(0.2f, 1f, 1f, 1f);

        [Header("Punta de Flecha")]
        [SerializeField] private GameObject arrowHeadPrefab;
        private GameObject arrowHeadInstance;

        private LineRenderer lineRenderer;
        private Material lineMaterial;
        private bool isVisible = true;

        private void Awake()
        {
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
            ConfigureLineRenderer();
            SetVisible(false);
        }

        private void ConfigureLineRenderer()
        {
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = resolution;
            lineRenderer.startWidth = 0.015f;
            lineRenderer.endWidth = 0.035f;

            if (lineRenderer.sharedMaterial != null)
            {
                lineMaterial = lineRenderer.material;
            }
            else
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                lineMaterial = new Material(shader);
                if (lineMaterial.HasProperty("_BaseColor")) lineMaterial.SetColor("_BaseColor", glowColor);
                lineMaterial.color = glowColor;
                lineRenderer.material = lineMaterial;
            }

            // Gradiente
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0f, 0.5f, 1f), 0.0f),
                    new GradientColorKey(new Color(0f, 0.9f, 1f), 0.7f),
                    new GradientColorKey(tipColor, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.4f, 0.0f),
                    new GradientAlphaKey(0.85f, 0.7f),
                    new GradientAlphaKey(1.0f, 1.0f)
                }
            );
            lineRenderer.colorGradient = gradient;

            // Crear punta de flecha procedural (cono o prisma cian) si no hay prefab
            CreateArrowHead();
        }

        private void CreateArrowHead()
        {
            if (arrowHeadInstance != null) return;

            arrowHeadInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arrowHeadInstance.name = "ArrowHead_Cone";
            arrowHeadInstance.transform.SetParent(transform);
            
            // Destruir collider para no estorbar
            var col = arrowHeadInstance.GetComponent<Collider>();
            if (col != null) Destroy(col);

            arrowHeadInstance.transform.localScale = new Vector3(0.045f, 0.04f, 0.045f);

            var mr = arrowHeadInstance.GetComponent<MeshRenderer>();
            if (mr != null && lineMaterial != null)
            {
                mr.material = lineMaterial;
            }
        }

        private void Update()
        {
            if (!isVisible || startPoint == null || endPoint == null)
            {
                lineRenderer.enabled = false;
                if (arrowHeadInstance != null) arrowHeadInstance.SetActive(false);
                return;
            }

            lineRenderer.enabled = true;
            if (arrowHeadInstance != null) arrowHeadInstance.SetActive(true);

            Vector3 p0 = startPoint.position;
            Vector3 p2 = endPoint.position;

            // Punto de control de la curva Bézier cuadrática
            Vector3 mid = Vector3.Lerp(p0, p2, 0.5f);
            Vector3 dir = (p2 - p0).normalized;
            Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;
            Vector3 controlPoint = mid + Vector3.up * curveArcHeight - side * lateralArcOffset;

            // Generar puntos a lo largo de la curva
            for (int i = 0; i < resolution; i++)
            {
                float t = i / (float)(resolution - 1);
                // Fórmula Bézier cuadrática: B(t) = (1-t)^2*P0 + 2*(1-t)*t*P1 + t^2*P2
                Vector3 pt = Mathf.Pow(1f - t, 2) * p0 + 2f * (1f - t) * t * controlPoint + Mathf.Pow(t, 2) * p2;
                lineRenderer.SetPosition(i, pt);
            }

            // Orientar la punta de la flecha hacia el punto final
            if (arrowHeadInstance != null)
            {
                arrowHeadInstance.transform.position = p2;
                Vector3 tangent = (p2 - controlPoint).normalized;
                if (tangent != Vector3.zero)
                {
                    arrowHeadInstance.transform.rotation = Quaternion.LookRotation(tangent) * Quaternion.Euler(90f, 0f, 0f);
                }
            }

            // Animación de textura / pulso
            if (lineMaterial != null)
            {
                float offset = Time.time * scrollSpeed;
                lineMaterial.mainTextureOffset = new Vector2(-offset, 0f);

                // Pulso de brillo sutil
                float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * 4f);
                lineMaterial.color = glowColor * pulse;
            }
        }

        public void SetTargets(Transform start, Transform end)
        {
            startPoint = start;
            endPoint = end;
            isVisible = (start != null && end != null);
        }

        public void SetVisible(bool visible)
        {
            isVisible = visible;
            if (lineRenderer != null) lineRenderer.enabled = visible;
            if (arrowHeadInstance != null) arrowHeadInstance.SetActive(visible);
        }
    }
}
