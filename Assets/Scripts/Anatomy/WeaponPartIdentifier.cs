using UnityEngine;

namespace VRRangeAcademy.Anatomy
{
    /// <summary>
    /// Identifica una pieza individual del arma y contiene su información técnica y de seguridad.
    /// </summary>
    public class WeaponPartIdentifier : MonoBehaviour
    {
        [Header("Nomenclatura")]
        [SerializeField] private string partName = "Pieza del Arma";
        [SerializeField] private string technicalNameEn = "Weapon Part";

        [Header("Función y Seguridad")]
        [TextArea(2, 4)]
        [SerializeField] private string mechanicalFunction = "Función mecánica de la pieza en el ciclo del arma.";

        [TextArea(2, 4)]
        [SerializeField] private string safetyProtocol = "Protocolo de seguridad asociado a este componente.";

        [Header("Visual Feedback")]
        [SerializeField] private Color highlightColor = new Color(0.2f, 0.75f, 1.0f, 1.0f);

        private Renderer partRenderer;
        private Color originalColor;
        private Material originalMaterial;
        private bool isHighlighted = false;

        public string PartName => partName;
        public string TechnicalNameEn => technicalNameEn;
        public string MechanicalFunction => mechanicalFunction;
        public string SafetyProtocol => safetyProtocol;

        private void Awake()
        {
            partRenderer = GetComponent<Renderer>();
            if (partRenderer != null && partRenderer.sharedMaterial != null)
            {
                originalMaterial = partRenderer.material;
                if (originalMaterial.HasProperty("_BaseColor"))
                {
                    originalColor = originalMaterial.GetColor("_BaseColor");
                }
                else if (originalMaterial.HasProperty("_Color"))
                {
                    originalColor = originalMaterial.GetColor("_Color");
                }
            }
        }

        public void SetHighlight(bool highlight)
        {
            isHighlighted = highlight;
            if (partRenderer == null || originalMaterial == null) return;

            Color targetColor = highlight ? highlightColor : originalColor;

            if (partRenderer.material.HasProperty("_BaseColor"))
            {
                partRenderer.material.SetColor("_BaseColor", targetColor);
            }
            else if (partRenderer.material.HasProperty("_Color"))
            {
                partRenderer.material.SetColor("_Color", targetColor);
            }
        }
    }
}
