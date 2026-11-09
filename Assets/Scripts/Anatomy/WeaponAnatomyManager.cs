using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRRangeAcademy.Anatomy
{
    /// <summary>
    /// Administra el modo interactivo de reconocimiento anatómico y nomenclatura de armas.
    /// Muestra la ficha técnica, función mecánica y protocolo de seguridad de cada pieza.
    /// </summary>
    public class WeaponAnatomyManager : MonoBehaviour
    {
        [Header("UI Display Card (World Space)")]
        [SerializeField] private GameObject anatomyDisplayCard;
        [SerializeField] private TextMeshProUGUI txtPartName;
        [SerializeField] private TextMeshProUGUI txtTechnicalName;
        [SerializeField] private TextMeshProUGUI txtFunction;
        [SerializeField] private TextMeshProUGUI txtSafetyRule;
        [SerializeField] private TextMeshProUGUI txtCounter;

        [Header("Navigation Buttons")]
        [SerializeField] private Button btnNextPart;
        [SerializeField] private Button btnPrevPart;
        [SerializeField] private Button btnToggleInspect;

        [Header("Weapon Parts")]
        [SerializeField] private List<WeaponPartIdentifier> weaponParts = new List<WeaponPartIdentifier>();
        [SerializeField] private int currentPartIndex = 0;

        [Header("Audio Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip switchPartClip;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }

            if (btnNextPart != null) btnNextPart.onClick.AddListener(NextPart);
            if (btnPrevPart != null) btnPrevPart.onClick.AddListener(PreviousPart);
            if (btnToggleInspect != null) btnToggleInspect.onClick.AddListener(ToggleAnatomyCard);

            if (weaponParts.Count > 0)
            {
                SelectPart(0);
            }
        }

        public void RegisterPart(WeaponPartIdentifier part)
        {
            if (!weaponParts.Contains(part))
            {
                weaponParts.Add(part);
            }
        }

        public void NextPart()
        {
            if (weaponParts.Count == 0) return;
            int next = (currentPartIndex + 1) % weaponParts.Count;
            SelectPart(next);
        }

        public void PreviousPart()
        {
            if (weaponParts.Count == 0) return;
            int prev = (currentPartIndex - 1 + weaponParts.Count) % weaponParts.Count;
            SelectPart(prev);
        }

        public void SelectPart(int index)
        {
            if (index < 0 || index >= weaponParts.Count) return;

            // Apagar highlight anterior
            if (currentPartIndex >= 0 && currentPartIndex < weaponParts.Count && weaponParts[currentPartIndex] != null)
            {
                weaponParts[currentPartIndex].SetHighlight(false);
            }

            currentPartIndex = index;
            var part = weaponParts[currentPartIndex];
            if (part != null)
            {
                part.SetHighlight(true);
                UpdateCardInfo(part);
                PlayBeep();
            }
        }

        public void SelectPart(WeaponPartIdentifier part)
        {
            int index = weaponParts.IndexOf(part);
            if (index >= 0)
            {
                SelectPart(index);
            }
        }

        private void UpdateCardInfo(WeaponPartIdentifier part)
        {
            if (txtPartName != null) txtPartName.text = part.PartName;
            if (txtTechnicalName != null) txtTechnicalName.text = "EN: " + part.TechnicalNameEn;
            if (txtFunction != null) txtFunction.text = "⚙️ <b>Función:</b>\n" + part.MechanicalFunction;
            if (txtSafetyRule != null) txtSafetyRule.text = "🛡️ <b>Norma de Seguridad:</b>\n" + part.SafetyProtocol;
            if (txtCounter != null) txtCounter.text = string.Format("Pieza {0} / {1}", currentPartIndex + 1, weaponParts.Count);
        }

        public void ToggleAnatomyCard()
        {
            if (anatomyDisplayCard != null)
            {
                anatomyDisplayCard.SetActive(!anatomyDisplayCard.activeSelf);
            }
        }

        private void PlayBeep()
        {
            if (audioSource == null) return;
            if (switchPartClip != null)
            {
                audioSource.PlayOneShot(switchPartClip);
            }
            else
            {
                // Procedural pitch chirp
                audioSource.pitch = 1.0f + (currentPartIndex * 0.08f);
                audioSource.PlayOneShot(CreateBeepClip());
            }
        }

        private AudioClip CreateBeepClip()
        {
            int sampleRate = 44100;
            float freq = 650;
            AudioClip clip = AudioClip.Create("Chirp", sampleRate / 14, 1, sampleRate, false);
            float[] data = new float[sampleRate / 14];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * i / sampleRate) * Mathf.Exp(-6f * i / data.Length);
            }
            clip.SetData(data, 0);
            return clip;
        }
    }
}
