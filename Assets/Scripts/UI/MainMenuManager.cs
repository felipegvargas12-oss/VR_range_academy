using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace VRRangeAcademy.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject helpPanel;
        [SerializeField] private GameObject messageToastPanel;
        [SerializeField] private TextMeshProUGUI messageToastText;

        [Header("Menu Buttons")]
        [SerializeField] private Button btnStartTraining;
        [SerializeField] private Button btnFreeRange;
        [SerializeField] private Button btnCalibration;
        [SerializeField] private Button btnHelp;
        [SerializeField] private Button btnSettings;
        [SerializeField] private GameObject freeRangeLockIcon;

        [Header("User Profile")]
        [SerializeField] private TextMeshProUGUI profileNameText;
        [SerializeField] private string defaultUserName = "Cadete 01";

        [Header("Scene Transition Settings")]
        [SerializeField] private string trainingSceneName = "WeaponAssembly";
        [SerializeField] private string freeRangeSceneName = "ShootingRange";
        [SerializeField] private bool isFreeRangeUnlocked = false;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private AudioClip lockedSound;

        private Coroutine toastCoroutine;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D UI sound
            }

            if (profileNameText != null)
            {
                profileNameText.text = "PERFIL DE USUARIO:\n" + defaultUserName;
            }

            UpdateLockStatus();
            HookButtons();
        }

        private void HookButtons()
        {
            if (btnStartTraining != null)
                btnStartTraining.onClick.AddListener(OnStartTrainingClicked);

            if (btnFreeRange != null)
                btnFreeRange.onClick.AddListener(OnFreeRangeClicked);

            if (btnCalibration != null)
                btnCalibration.onClick.AddListener(OnCalibrationClicked);

            if (btnHelp != null)
                btnHelp.onClick.AddListener(OnHelpClicked);

            if (btnSettings != null)
                btnSettings.onClick.AddListener(OnSettingsClicked);
        }

        public void OnStartTrainingClicked()
        {
            PlaySound(clickSound, 800f);
            ShowToast("Iniciando Capacitación de Manipulación Segura...", 2.5f);
            StartCoroutine(LoadSceneDelayed(trainingSceneName, 1.2f));
        }

        public void OnFreeRangeClicked()
        {
            if (!isFreeRangeUnlocked)
            {
                PlaySound(lockedSound, 250f);
                ShowToast("🔒 Modo bloqueado: Debes completar la capacitación primero.", 3.0f);
                return;
            }

            PlaySound(clickSound, 800f);
            ShowToast("Accediendo al Campo de Tiro Libre...", 2.5f);
            StartCoroutine(LoadSceneDelayed(freeRangeSceneName, 1.2f));
        }

        public void OnCalibrationClicked()
        {
            PlaySound(clickSound, 600f);
            ShowToast("🎯 Calibrando altura y seguimiento de mandos Meta Quest...", 3.0f);
        }

        public void OnHelpClicked()
        {
            PlaySound(clickSound, 700f);
            if (helpPanel != null)
            {
                bool isOpen = !helpPanel.activeSelf;
                helpPanel.SetActive(isOpen);
                if (isOpen && settingsPanel != null) settingsPanel.SetActive(false);
            }
            else
            {
                ShowToast("📖 Regla 1: Trate siempre toda arma como si estuviera cargada.", 4.0f);
            }
        }

        public void OnSettingsClicked()
        {
            PlaySound(clickSound, 700f);
            if (settingsPanel != null)
            {
                bool isOpen = !settingsPanel.activeSelf;
                settingsPanel.SetActive(isOpen);
                if (isOpen && helpPanel != null) helpPanel.SetActive(false);
            }
            else
            {
                ShowToast("⚙️ Configuración VR: Modos de confort, volumen y audio activos.", 3.0f);
            }
        }

        public void SetFreeRangeUnlocked(bool unlocked)
        {
            isFreeRangeUnlocked = unlocked;
            UpdateLockStatus();
        }

        private void UpdateLockStatus()
        {
            if (freeRangeLockIcon != null)
            {
                freeRangeLockIcon.SetActive(!isFreeRangeUnlocked);
            }

            if (btnFreeRange != null)
            {
                var colors = btnFreeRange.colors;
                colors.normalColor = isFreeRangeUnlocked ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.75f);
                btnFreeRange.colors = colors;
            }
        }

        public void ShowToast(string message, float duration = 2.5f)
        {
            if (messageToastPanel != null && messageToastText != null)
            {
                if (toastCoroutine != null) StopCoroutine(toastCoroutine);
                toastCoroutine = StartCoroutine(ToastRoutine(message, duration));
            }
            else
            {
                Debug.Log($"[MainMenu] {message}");
            }
        }

        private IEnumerator ToastRoutine(string message, float duration)
        {
            messageToastText.text = message;
            messageToastPanel.SetActive(true);
            yield return new WaitForSeconds(duration);
            messageToastPanel.SetActive(false);
        }

        private IEnumerator LoadSceneDelayed(string sceneName, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning($"[MainMenu] Escena '{sceneName}' no encontrada en Build Settings. Verifique la configuración.");
            }
        }

        private void PlaySound(AudioClip clip, float fallbackFreq)
        {
            if (audioSource == null) return;

            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
            else
            {
                // Procedural beep fallback
                audioSource.pitch = fallbackFreq / 600f;
                audioSource.PlayOneShot(CreateBeepClip());
            }
        }

        private AudioClip CreateBeepClip()
        {
            int sampleRate = 44100;
            float freq = 440;
            AudioClip clip = AudioClip.Create("Beep", sampleRate / 10, 1, sampleRate, false);
            float[] data = new float[sampleRate / 10];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * i / sampleRate) * Mathf.Exp(-5f * i / data.Length);
            }
            clip.SetData(data, 0);
            return clip;
        }
    }
}
