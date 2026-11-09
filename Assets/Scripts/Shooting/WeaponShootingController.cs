using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRRangeAcademy.Shooting
{
    public class WeaponShootingController : MonoBehaviour
    {
        [Header("Datos Balísticos")]
        public string weaponId = "M1911";
        public string weaponName = "Pistola Colt M1911";
        public string caliber = ".45 ACP";
        public int magazineCapacity = 7;
        public int currentAmmo = 7;
        public float fireRate = 0.20f;
        public float damage = 50f;
        public float bulletRange = 100f;

        [Header("Posicionamiento en Primera Persona")]
        [SerializeField] private Vector3 hipPosition = new Vector3(0.24f, -0.22f, 0.45f);
        [SerializeField] private Vector3 adsPosition = new Vector3(0.0f, -0.14f, 0.38f);
        [SerializeField] private float adsSpeed = 12f;

        [Header("Efectos Visuales (VFX)")]
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private Light muzzleLight;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        private AudioClip gunshotClip;
        private AudioClip reloadClip;
        private AudioClip dryFireClip;

        private float lastFireTime = 0f;
        private bool isReloading = false;
        private bool isAiming = false;
        private Camera playerCamera;
        private LineRenderer tracerLine;

        // Recoil
        private Vector3 currentRecoilEuler;
        private Vector3 targetRecoilEuler;
        [SerializeField] private float recoilKickAngle = 2.4f;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.3f;
            }

            gunshotClip = CreateGunshotSound(caliber);
            reloadClip = CreateReloadSound();
            dryFireClip = CreateDryFireSound();

            currentAmmo = magazineCapacity;

            // Muzzle light si no está asignada
            if (muzzleLight == null)
            {
                var lightGo = new GameObject("Muzzle_Light");
                lightGo.transform.SetParent(transform, false);
                muzzleLight = lightGo.AddComponent<Light>();
                muzzleLight.type = LightType.Point;
                muzzleLight.range = 5f;
                muzzleLight.color = new Color(1.0f, 0.85f, 0.4f);
                muzzleLight.intensity = 0f;
                muzzleLight.enabled = false;
            }

            // Tracer line renderer
            var tracerGo = new GameObject("TracerRenderer");
            tracerGo.transform.SetParent(transform, false);
            tracerLine = tracerGo.AddComponent<LineRenderer>();
            tracerLine.useWorldSpace = true;
            tracerLine.startWidth = 0.02f;
            tracerLine.endWidth = 0.005f;
            tracerLine.positionCount = 2;
            tracerLine.enabled = false;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            var traceMat = new Material(shader);
            traceMat.color = new Color(1f, 0.9f, 0.4f, 0.9f);
            if (traceMat.HasProperty("_BaseColor")) traceMat.SetColor("_BaseColor", new Color(1f, 0.9f, 0.4f, 0.9f));
            tracerLine.material = traceMat;
        }

        private void Start()
        {
            playerCamera = GetComponentInParent<Camera>() ?? Camera.main;
        }

        public void SetupWeapon(string id, string name, string cal, int magCap, float dmg, float rate)
        {
            weaponId = id;
            weaponName = name;
            caliber = cal;
            magazineCapacity = magCap;
            currentAmmo = magCap;
            damage = dmg;
            fireRate = rate;

            gunshotClip = CreateGunshotSound(caliber);
        }

        private void Update()
        {
            HandleInput();
            UpdateWeaponPosition();
            UpdateRecoil();
        }

        private void HandleInput()
        {
            // Solo actuar si el cursor está capturado o apuntando
            bool firePressed = false;
            bool reloadPressed = false;
            isAiming = false;

            if (Mouse.current != null)
            {
                firePressed = Mouse.current.leftButton.wasPressedThisFrame;
                isAiming = Mouse.current.rightButton.isPressed;
            }

            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame) firePressed = true;
                reloadPressed = Keyboard.current.rKey.wasPressedThisFrame;
            }

            if (firePressed && Time.time >= lastFireTime + fireRate && !isReloading)
            {
                TryShoot();
            }

            if (reloadPressed && !isReloading && currentAmmo < magazineCapacity)
            {
                StartCoroutine(ReloadSequence());
            }
        }

        private void TryShoot()
        {
            lastFireTime = Time.time;

            if (currentAmmo <= 0)
            {
                PlaySound(dryFireClip, 1.2f);
                return;
            }

            currentAmmo--;
            ExecuteShot();
        }

        private void ExecuteShot()
        {
            // 1. Sonido de detonación
            PlaySound(gunshotClip, Random.Range(0.96f, 1.04f));

            // 2. Destello de boca de cañón (Muzzle Flash)
            StartCoroutine(FlashMuzzleLight());

            // 3. Retroceso
            targetRecoilEuler += new Vector3(-recoilKickAngle, Random.Range(-0.4f, 0.4f), 0f);

            // 4. Registrar disparo en el polígono
            if (ShootingRangeManager.Instance != null)
            {
                ShootingRangeManager.Instance.RegisterShot();
            }

            // 5. Raycast balístico
            if (playerCamera == null) playerCamera = Camera.main;
            Ray ray = playerCamera != null
                ? playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : new Ray(transform.position, transform.forward);

            Vector3 hitPoint = ray.origin + ray.direction * bulletRange;
            Vector3 hitNormal = -ray.direction;

            if (Physics.Raycast(ray, out RaycastHit hit, bulletRange))
            {
                hitPoint = hit.point;
                hitNormal = hit.normal;

                // Verificar si impactó una diana o blanco del polígono
                var target = hit.collider.GetComponentInParent<TargetHitReceiver>();
                if (target != null)
                {
                    target.TakeHit(hitPoint, hitNormal, damage);
                }
            }

            // 6. Trazadora visual de bala
            Vector3 muzzlePos = muzzlePoint != null ? muzzlePoint.position : transform.position + transform.forward * 0.3f;
            StartCoroutine(RenderTracer(muzzlePos, hitPoint));
        }

        private IEnumerator FlashMuzzleLight()
        {
            if (muzzleLight != null)
            {
                muzzleLight.enabled = true;
                muzzleLight.intensity = Random.Range(4.5f, 6.0f);
                yield return new WaitForSeconds(0.045f);
                muzzleLight.enabled = false;
            }
        }

        private IEnumerator RenderTracer(Vector3 start, Vector3 end)
        {
            if (tracerLine != null)
            {
                tracerLine.enabled = true;
                tracerLine.SetPosition(0, start);
                tracerLine.SetPosition(1, end);
                yield return new WaitForSeconds(0.06f);
                tracerLine.enabled = false;
            }
        }

        private IEnumerator ReloadSequence()
        {
            isReloading = true;
            PlaySound(reloadClip, 1.0f);

            yield return new WaitForSeconds(1.2f);

            currentAmmo = magazineCapacity;
            isReloading = false;
        }

        private void UpdateWeaponPosition()
        {
            Vector3 targetPos = isAiming ? adsPosition : hipPosition;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * adsSpeed);
        }

        private void UpdateRecoil()
        {
            targetRecoilEuler = Vector3.Lerp(targetRecoilEuler, Vector3.zero, Time.deltaTime * 14f);
            currentRecoilEuler = Vector3.Lerp(currentRecoilEuler, targetRecoilEuler, Time.deltaTime * 24f);
            transform.localRotation = Quaternion.Euler(currentRecoilEuler);
        }

        private void OnGUI()
        {
            if (Camera.main == null) return;

            // Retícula dinámica de puntería
            if (!isAiming)
            {
                float cx = Screen.width * 0.5f;
                float cy = Screen.height * 0.5f;
                float gap = 8f + Mathf.Abs(currentRecoilEuler.x) * 4f;
                float len = 10f;
                float th = 2f;

                GUI.color = new Color(0f, 1f, 0.85f, 0.8f);
                // Superior
                GUI.DrawTexture(new Rect(cx - th * 0.5f, cy - gap - len, th, len), Texture2D.whiteTexture);
                // Inferior
                GUI.DrawTexture(new Rect(cx - th * 0.5f, cy + gap, th, len), Texture2D.whiteTexture);
                // Izquierda
                GUI.DrawTexture(new Rect(cx - gap - len, cy - th * 0.5f, len, th), Texture2D.whiteTexture);
                // Derecha
                GUI.DrawTexture(new Rect(cx + gap, cy - th * 0.5f, len, th), Texture2D.whiteTexture);
            }

            // HUD inferior de munición
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };

            float panelW = 320f;
            float panelH = 65f;
            float x = Screen.width - panelW - 30f;
            float y = Screen.height - panelH - 30f;

            GUI.color = new Color(0.04f, 0.12f, 0.18f, 0.85f);
            GUI.DrawTexture(new Rect(x, y, panelW, panelH), Texture2D.whiteTexture);

            GUI.color = isReloading ? new Color(1f, 0.8f, 0.2f) : (currentAmmo == 0 ? new Color(1f, 0.25f, 0.25f) : Color.white);
            string ammoText = isReloading ? "RECARGANDO..." : $"{currentAmmo} / {magazineCapacity}";
            GUI.Label(new Rect(x + 10f, y + 8f, panelW - 20f, 26f), ammoText, style);

            style.fontSize = 13;
            style.fontStyle = FontStyle.Normal;
            GUI.color = new Color(0.2f, 0.9f, 1.0f);
            GUI.Label(new Rect(x + 10f, y + 36f, panelW - 20f, 22f), $"{weaponName.ToUpper()}  [{caliber}]  |  [R] Recargar", style);
            GUI.color = Color.white;
        }

        private void PlaySound(AudioClip clip, float pitch = 1f)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.pitch = pitch;
                audioSource.PlayOneShot(clip);
            }
        }

        #region Generadores de Audio Balístico
        private static AudioClip CreateGunshotSound(string cal)
        {
            int sampleRate = 44100;
            float duration = 0.45f;
            int length = (int)(sampleRate * duration);
            float[] data = new float[length];

            float baseFreq = cal.Contains(".45") ? 95f : (cal.Contains("5.56") ? 140f : (cal.Contains("5.45") ? 130f : 160f));

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                // Impulso inicial explosivo (Crack ultrasónico de salida de cañón)
                float initialCrack = Mathf.Exp(-t * 110f) * (Random.value * 2f - 1f) * 1.0f;
                // Golpe subsónico concusivo profundo
                float boom = Mathf.Sin(2f * Mathf.PI * baseFreq * Mathf.Exp(-t * 22f) * t) * Mathf.Exp(-t * 12f) * 0.8f;
                // Ruido de expansión y reverberación de cañón
                float tail = (Random.value * 2f - 1f) * Mathf.Exp(-t * 8f) * 0.35f;

                data[i] = Mathf.Clamp(initialCrack + boom + tail, -1f, 1f) * 0.95f;
            }

            var clip = AudioClip.Create("Gunshot_" + cal.Replace(" ", "_"), length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateReloadSound()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.8f);
            float[] data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float eject = Mathf.Exp(-Mathf.Abs(t - 0.15f) * 60f) * Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.5f;
                float insert = Mathf.Exp(-Mathf.Abs(t - 0.45f) * 50f) * Mathf.Sin(2f * Mathf.PI * 950f * t) * 0.7f;
                float rack = Mathf.Exp(-Mathf.Abs(t - 0.68f) * 70f) * Mathf.Sin(2f * Mathf.PI * 1400f * t) * 0.8f;
                data[i] = eject + insert + rack;
            }
            var clip = AudioClip.Create("WeaponReload", length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateDryFireSound()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.10f);
            float[] data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                data[i] = Mathf.Sin(2f * Mathf.PI * 2200f * t) * Mathf.Exp(-t * 90f) * 0.6f;
            }
            var clip = AudioClip.Create("DryFireClick", length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
        #endregion
    }
}
