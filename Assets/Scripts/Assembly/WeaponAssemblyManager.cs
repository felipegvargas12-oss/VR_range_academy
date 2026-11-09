using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace VRRangeAcademy.Assembly
{
    public class WeaponAssemblyManager : MonoBehaviour
    {
        [Header("Configuración de Piezas")]
        [SerializeField] private List<AssemblyPiece> assemblyPieces = new List<AssemblyPiece>();
        [SerializeField] private List<Transform> holographicSockets = new List<Transform>();

        [Header("Guía Visual")]
        [SerializeField] private GuidanceArrow guidanceArrow;
        [SerializeField] private GameObject holographicGhostGun;
        [SerializeField] private GameObject fullyAssembledGun;

        [Header("UI Holográfica / HUD de Banco")]
        [SerializeField] private TextMeshProUGUI txtStepCounter;
        [SerializeField] private TextMeshProUGUI txtStepTitle;
        [SerializeField] private TextMeshProUGUI txtStepDescription;
        [SerializeField] private TextMeshProUGUI txtControlsHint;
        [SerializeField] private GameObject panelCompletion;
        [SerializeField] private TextMeshProUGUI txtCompletionTitle;
        [SerializeField] private Button btnProceedToShootingRange;
        [SerializeField] private Button btnResetAssembly;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip snapClip;
        [SerializeField] private AudioClip slideRackClip;
        [SerializeField] private AudioClip completionChimeClip;

        [Header("Transición de Escena")]
        [SerializeField] private string shootingRangeSceneName = "ShootingRange";
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private int currentStepIndex = 0;
        private int totalSnappedCount = 0;
        private bool isAssemblyCompleted = false;

        public bool IsCompleted => isAssemblyCompleted;

        private void Start()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Generar clips de sonido mecánicos procedurales si no están asignados
            if (snapClip == null) snapClip = CreateMechanicalSnapClip();
            if (slideRackClip == null) slideRackClip = CreateSlideRackClip();
            if (completionChimeClip == null) completionChimeClip = CreateCompletionChimeClip();

            // Ocultar arma ensamblada final al inicio
            if (fullyAssembledGun != null)
            {
                fullyAssembledGun.SetActive(false);
            }

            if (panelCompletion != null)
            {
                panelCompletion.SetActive(false);
            }

            if (btnProceedToShootingRange != null)
            {
                btnProceedToShootingRange.onClick.AddListener(OnProceedToShootingRange);
            }

            if (btnResetAssembly != null)
            {
                btnResetAssembly.onClick.AddListener(ResetAssembly);
            }

            // Suscribirse a eventos de cada pieza
            for (int i = 0; i < assemblyPieces.Count; i++)
            {
                var piece = assemblyPieces[i];
                if (piece != null)
                {
                    piece.OnSnapped += HandlePieceSnapped;
                    piece.OnGrabbed += HandlePieceGrabbed;
                }
            }

            UpdateStepUI();
            UpdateGuidanceArrow();
        }

        private void HandlePieceGrabbed(AssemblyPiece piece)
        {
            if (piece == null || isAssemblyCompleted) return;

            // Si el usuario agarra una pieza, orientar la flecha hacia su socket
            if (guidanceArrow != null && piece.targetSocket != null)
            {
                guidanceArrow.SetTargets(piece.transform, piece.targetSocket);
                guidanceArrow.SetVisible(true);
            }
        }

        private void HandlePieceSnapped(AssemblyPiece piece)
        {
            totalSnappedCount++;

            // Audio de confirmación
            PlaySound(snapClip, 1.0f);

            // Determinar siguiente paso pendiente
            FindNextPendingStep();

            UpdateStepUI();
            UpdateGuidanceArrow();

            if (totalSnappedCount >= assemblyPieces.Count)
            {
                StartCoroutine(CompleteAssemblySequence());
            }
        }

        private void FindNextPendingStep()
        {
            for (int i = 0; i < assemblyPieces.Count; i++)
            {
                if (assemblyPieces[i] != null && !assemblyPieces[i].IsSnapped)
                {
                    currentStepIndex = i;
                    return;
                }
            }
            currentStepIndex = assemblyPieces.Count;
        }

        private void UpdateStepUI()
        {
            if (isAssemblyCompleted) return;

            if (currentStepIndex < assemblyPieces.Count)
            {
                var cur = assemblyPieces[currentStepIndex];
                if (txtStepCounter != null)
                {
                    txtStepCounter.text = $"PASO {totalSnappedCount + 1} DE {assemblyPieces.Count}";
                }
                if (txtStepTitle != null)
                {
                    txtStepTitle.text = $"Acoplar: {cur.displayName}";
                }
                if (txtStepDescription != null)
                {
                    txtStepDescription.text = cur.functionDescription;
                }
                if (txtControlsHint != null)
                {
                    txtControlsHint.text = "VR: Mantener Grip/Trigger | PC: Clic izquierdo y arrastrar hacia el holograma";
                }
            }
        }

        private void UpdateGuidanceArrow()
        {
            if (guidanceArrow == null) return;

            if (isAssemblyCompleted || currentStepIndex >= assemblyPieces.Count)
            {
                guidanceArrow.SetVisible(false);
                return;
            }

            var nextPiece = assemblyPieces[currentStepIndex];
            if (nextPiece != null && nextPiece.targetSocket != null)
            {
                guidanceArrow.SetTargets(nextPiece.transform, nextPiece.targetSocket);
                guidanceArrow.SetVisible(true);
            }
            else
            {
                guidanceArrow.SetVisible(false);
            }
        }

        private IEnumerator CompleteAssemblySequence()
        {
            isAssemblyCompleted = true;

            if (guidanceArrow != null)
            {
                guidanceArrow.SetVisible(false);
            }

            yield return new WaitForSeconds(0.4f);

            // Sonido de rastrillar la corredera (slide rack)
            PlaySound(slideRackClip, 1.0f);

            yield return new WaitForSeconds(0.6f);

            // Campanada / Fanfarria de éxito
            PlaySound(completionChimeClip, 1.2f);

            // Activar modelo final ensamblado si existe
            if (fullyAssembledGun != null)
            {
                fullyAssembledGun.SetActive(true);
            }

            if (panelCompletion != null)
            {
                panelCompletion.SetActive(true);
            }

            if (txtCompletionTitle != null)
            {
                txtCompletionTitle.text = "[!] APRENDIZAJE MECANICO VALIDADO\nAcceso al Poligono de Tiro Desbloqueado";
            }
        }

        public void OnProceedToShootingRange()
        {
            PlaySound(snapClip, 1.0f);
            SceneManager.LoadScene(shootingRangeSceneName);
        }

        public void ResetAssembly()
        {
            StopAllCoroutines();
            isAssemblyCompleted = false;
            totalSnappedCount = 0;
            currentStepIndex = 0;

            if (panelCompletion != null) panelCompletion.SetActive(false);
            if (fullyAssembledGun != null) fullyAssembledGun.SetActive(false);

            foreach (var piece in assemblyPieces)
            {
                if (piece != null) piece.ResetToTable();
            }

            UpdateStepUI();
            UpdateGuidanceArrow();
        }

        private void PlaySound(AudioClip clip, float pitch = 1.0f)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.pitch = pitch;
                audioSource.PlayOneShot(clip);
            }
        }

        #region Procedural Audio Generators
        private AudioClip CreateMechanicalSnapClip()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.12f); // 120ms snap
            float[] data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 65f); // Fast decay
                float click = Mathf.Sin(2f * Mathf.PI * 1850f * t);
                float sub = Mathf.Sin(2f * Mathf.PI * 420f * t) * 0.4f;
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.25f;
                data[i] = (click + sub + noise) * envelope * 0.85f;
            }
            var clip = AudioClip.Create("SnapClick", length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateSlideRackClip()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.45f); // 450ms metallic double slide
            float[] data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float env1 = Mathf.Exp(-Mathf.Abs(t - 0.08f) * 45f);
                float env2 = Mathf.Exp(-Mathf.Abs(t - 0.26f) * 55f);
                float metal1 = Mathf.Sin(2f * Mathf.PI * 920f * t) * env1;
                float metal2 = Mathf.Sin(2f * Mathf.PI * 1450f * t) * env2;
                float scrape = (UnityEngine.Random.value * 2f - 1f) * (env1 * 0.3f + env2 * 0.4f);
                data[i] = (metal1 + metal2 + scrape) * 0.9f;
            }
            var clip = AudioClip.Create("SlideRack", length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateCompletionChimeClip()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.85f); // 850ms victory chime
            float[] data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 5f);
                float c1 = Mathf.Sin(2f * Mathf.PI * 523.25f * t); // C5
                float c2 = Mathf.Sin(2f * Mathf.PI * 659.25f * t) * 0.8f; // E5
                float c3 = Mathf.Sin(2f * Mathf.PI * 783.99f * t) * 0.9f; // G5
                float c4 = Mathf.Sin(2f * Mathf.PI * 1046.50f * t) * 0.7f; // C6
                data[i] = (c1 + c2 + c3 + c4) * 0.25f * envelope;
            }
            var clip = AudioClip.Create("CompletionChime", length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
        #endregion
    }
}
