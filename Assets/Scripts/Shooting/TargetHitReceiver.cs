using System.Collections;
using UnityEngine;

namespace VRRangeAcademy.Shooting
{
    public enum TargetKind
    {
        Silhouette,     // Silueta humana táctica (Dummy)
        ConcentricRing  // Diana circular reglamentaria
    }

    public class TargetHitReceiver : MonoBehaviour
    {
        [Header("Tipo de Diana")]
        [SerializeField] private TargetKind targetKind = TargetKind.Silhouette;
        [SerializeField] private string targetName = "Diana 01";
        [SerializeField] private float distanceMeters = 10f;

        [Header("Efecto de Impacto Mecánico")]
        [SerializeField] private float wobbleAngle = 18f;
        [SerializeField] private float wobbleSpeed = 22f;

        private Quaternion initialRotation;
        private Coroutine wobbleCoroutine;
        private AudioSource audioSource;
        private static AudioClip steelPingClip;

        private void Awake()
        {
            initialRotation = transform.localRotation;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1.0f; // 3D sound in range
                audioSource.maxDistance = 35f;
            }

            if (steelPingClip == null)
            {
                steelPingClip = CreateSteelPingSound();
            }
        }

        public void TakeHit(Vector3 hitPoint, Vector3 hitNormal, float damage = 25f)
        {
            // 1. Determinar puntuación y zona de impacto
            int points = 10;
            bool isCritical = false;
            string hitZone = "Cuerpo";

            Vector3 localHit = transform.InverseTransformPoint(hitPoint);

            if (targetKind == TargetKind.Silhouette)
            {
                // Silueta Dummy: la cabeza está en local Y > 0.65m aprox
                if (localHit.y > 0.60f)
                {
                    points = 100;
                    isCritical = true;
                    hitZone = "CABEZA (Crítico)";
                }
                else if (localHit.y > 0.15f)
                {
                    points = 50;
                    isCritical = false;
                    hitZone = "TORSO";
                }
                else
                {
                    points = 25;
                    hitZone = "EXTREMIDAD";
                }
            }
            else // ConcentricRing
            {
                // Diana circular: distancia radial al centro
                float distFromCenter = Mathf.Sqrt(localHit.x * localHit.x + localHit.y * localHit.y);
                if (distFromCenter < 0.12f)
                {
                    points = 100;
                    isCritical = true;
                    hitZone = "CENTRO / BULLSEYE";
                }
                else if (distFromCenter < 0.28f)
                {
                    points = 50;
                    hitZone = "ANILLO INTERIOR";
                }
                else
                {
                    points = 25;
                    hitZone = "ANILLO EXTERIOR";
                }
            }

            // 2. Efecto de oscilación / retroceso físico de la diana
            if (wobbleCoroutine != null) StopCoroutine(wobbleCoroutine);
            wobbleCoroutine = StartCoroutine(AnimateHitWobble());

            // 3. Sonido metálico de impacto
            if (audioSource != null && steelPingClip != null)
            {
                audioSource.pitch = isCritical ? 1.35f : Random.Range(0.95f, 1.15f);
                audioSource.PlayOneShot(steelPingClip, 1.0f);
            }

            // 4. Registrar en el manager del polígono
            if (ShootingRangeManager.Instance != null)
            {
                ShootingRangeManager.Instance.RegisterHit(points, isCritical, hitZone, distanceMeters, hitPoint);
            }
        }

        private IEnumerator AnimateHitWobble()
        {
            // Retroceso brusco hacia atrás por el impacto
            Quaternion kickRot = initialRotation * Quaternion.Euler(-wobbleAngle, 0f, 0f);
            float elapsed = 0f;

            // Fase 1: Retroceso rápido (0.06s)
            while (elapsed < 0.06f)
            {
                elapsed += Time.deltaTime;
                transform.localRotation = Quaternion.Slerp(initialRotation, kickRot, elapsed / 0.06f);
                yield return null;
            }

            // Fase 2: Rebote oscilante con amortiguación
            float t = 0f;
            while (t < 0.75f)
            {
                t += Time.deltaTime;
                float damp = Mathf.Exp(-t * 6f);
                float wave = Mathf.Sin(t * wobbleSpeed) * damp;
                transform.localRotation = initialRotation * Quaternion.Euler(-wobbleAngle * wave, 0f, 0f);
                yield return null;
            }

            transform.localRotation = initialRotation;
        }

        private static AudioClip CreateSteelPingSound()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.35f);
            float[] data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 14f);
                float tone1 = Mathf.Sin(2f * Mathf.PI * 2400f * t);
                float tone2 = Mathf.Sin(2f * Mathf.PI * 3650f * t) * 0.6f;
                float sub = Mathf.Sin(2f * Mathf.PI * 620f * t) * 0.4f;
                float noise = (Random.value * 2f - 1f) * Mathf.Exp(-t * 80f) * 0.5f;
                data[i] = (tone1 + tone2 + sub + noise) * env * 0.7f;
            }
            var clip = AudioClip.Create("SteelTargetPing", length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
