using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace VRRangeAcademy.Shooting
{
    public class ShootingRangeManager : MonoBehaviour
    {
        public static ShootingRangeManager Instance { get; private set; }

        [Header("Estado del Polígono")]
        [SerializeField] private bool isRangeUnlocked = false;

        [Header("Puerta de Seguridad y Semáforo")]
        [SerializeField] private Transform securityDoor;
        [SerializeField] private Light doorStatusLight;
        [SerializeField] private Vector3 doorOpenOffset = new Vector3(0f, 0f, 2.5f);

        [Header("Marcador Digital de Puntuación")]
        [SerializeField] private TextMeshProUGUI txtScoreboard;
        [SerializeField] private TextMeshProUGUI txtLastHitToast;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        private AudioClip doorOpenClip;
        private AudioClip accessGrantedClip;

        // Estadísticas de tiro
        private int shotsFired = 0;
        private int targetsHit = 0;
        private int totalScore = 0;
        private int criticalHits = 0;
        private string activeWeaponName = "Pistola Colt M1911";

        public bool IsRangeUnlocked => isRangeUnlocked;
        public int TotalScore => totalScore;
        public int ShotsFired => shotsFired;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            doorOpenClip = CreateDoorHydraulicClip();
            accessGrantedClip = CreateAccessGrantedChime();

            UpdateScoreboardUI("ESPERANDO VALIDACIÓN DE APRENDIZAJE MECÁNICO");
            SetLightStatus(false);
        }

        public void UnlockRange(string weaponName)
        {
            if (isRangeUnlocked) return;
            isRangeUnlocked = true;
            activeWeaponName = weaponName;

            SetLightStatus(true);
            PlaySound(accessGrantedClip, 1.1f);

            // Animar apertura de puerta de seguridad hacia el polígono
            if (securityDoor != null)
            {
                StartCoroutine(AnimateDoorOpen());
            }

            UpdateScoreboardUI($"LÍNEA DE TIRO HABILITADA | ARMA: {activeWeaponName.ToUpper()}");
            ShowHitToast($"<color=#00FF99>[!] ACCESO AUTORIZADO AL POLÍGONO DE TIRO</color>\nDirígete a la línea de fuego para iniciar la prueba balística.");
        }

        private void SetLightStatus(bool unlocked)
        {
            if (doorStatusLight != null)
            {
                doorStatusLight.color = unlocked ? new Color(0.1f, 1f, 0.4f) : new Color(1f, 0.1f, 0.1f);
                doorStatusLight.intensity = unlocked ? 3.0f : 1.8f;
            }
        }

        private IEnumerator AnimateDoorOpen()
        {
            PlaySound(doorOpenClip, 0.9f);
            Vector3 startPos = securityDoor.localPosition;
            Vector3 targetPos = startPos + doorOpenOffset;
            float elapsed = 0f;
            float duration = 1.8f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                securityDoor.localPosition = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            securityDoor.localPosition = targetPos;
        }

        public void RegisterShot()
        {
            shotsFired++;
            RefreshScoreDisplay();
        }

        public void RegisterHit(int points, bool isCritical, string hitZone, float distance, Vector3 hitPos)
        {
            targetsHit++;
            totalScore += points;
            if (isCritical) criticalHits++;

            string critPrefix = isCritical ? "<color=#FFDD00>[CRÍTICO]</color> " : "";
            ShowHitToast($"{critPrefix}+<b>{points} PTS</b> ({hitZone} a {distance:F0}m)");

            RefreshScoreDisplay();
        }

        private void RefreshScoreDisplay()
        {
            float accuracy = shotsFired > 0 ? ((float)targetsHit / shotsFired) * 100f : 0f;
            string status = $"<b>REGISTRO BALÍSTICO EN VIVO:</b>\n" +
                            $"Arma: <color=#00FFFF>{activeWeaponName}</color>\n" +
                            $"Puntuación: <color=#00FF88>{totalScore} PTS</color>\n" +
                            $"Disparos: {shotsFired} | Impactos: {targetsHit} | Precisión: <color=#FFDD44>{accuracy:F1}%</color>\n" +
                            $"Impactos Críticos (Centro/Cabeza): {criticalHits}\n" +
                            $"Estado: <color=#00FF66>LÍNEA DE TIRO ACTIVA</color>";

            UpdateScoreboardUI(status);
        }

        private void UpdateScoreboardUI(string text)
        {
            if (txtScoreboard != null)
            {
                txtScoreboard.text = text;
            }
        }

        private void ShowHitToast(string msg)
        {
            if (txtLastHitToast != null)
            {
                txtLastHitToast.text = msg;
                StopCoroutine("FadeToast");
                StartCoroutine("FadeToast");
            }
        }

        private IEnumerator FadeToast()
        {
            if (txtLastHitToast == null) yield break;
            txtLastHitToast.gameObject.SetActive(true);
            yield return new WaitForSeconds(2.8f);
            txtLastHitToast.text = "";
        }

        public void ResetScore()
        {
            shotsFired = 0;
            targetsHit = 0;
            totalScore = 0;
            criticalHits = 0;
            RefreshScoreDisplay();
        }

        private void PlaySound(AudioClip clip, float pitch = 1f)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.pitch = pitch;
                audioSource.PlayOneShot(clip);
            }
        }

        private static AudioClip CreateDoorHydraulicClip()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 1.5f);
            float[] data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float whoosh = (Random.value * 2f - 1f) * Mathf.Sin(t * Mathf.PI / 1.5f) * 0.35f;
                float hum = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.2f;
                data[i] = (whoosh + hum) * 0.8f;
            }
            var clip = AudioClip.Create("DoorHydraulic", length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateAccessGrantedChime()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.7f);
            float[] data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 5f);
                float f1 = Mathf.Sin(2f * Mathf.PI * 587.33f * t); // D5
                float f2 = Mathf.Sin(2f * Mathf.PI * 880.00f * t); // A5
                data[i] = (f1 + f2) * 0.35f * env;
            }
            var clip = AudioClip.Create("AccessGrantedChime", length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
