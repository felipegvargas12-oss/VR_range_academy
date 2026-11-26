using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace VRRangeAcademy.Shooting
{
    public enum WeaponFireMode
    {
        SemiAuto,
        FullAuto
    }

    public class WeaponShootingController : MonoBehaviour
    {
        [Header("Datos Balísticos")]
        public string weaponId = "M1911";
        public string weaponName = "Pistola Colt M1911";
        public string caliber = ".45 ACP";
        public int magazineCapacity = 7;
        public int currentAmmo = 7;
        public float fireRate = 0.18f;
        public float damage = 50f;
        public float bulletRange = 100f;

        [Header("Modo de Disparo")]
        public WeaponFireMode fireMode = WeaponFireMode.SemiAuto;
        public bool canSwitchFireMode = false;
        [SerializeField] private float semiFireRate = 0.16f;
        [SerializeField] private float autoFireRate = 0.088f;

        [Header("Posicionamiento en Primera Persona")]
        [SerializeField] private Vector3 hipPosition = new Vector3(0.20f, -0.18f, 0.38f);
        [SerializeField] private Vector3 adsPosition = new Vector3(0.0f, -0.06f, 0.26f);
        [SerializeField] private Vector3 baseRotation = new Vector3(0f, 180f, 0f);
        [SerializeField] private float adsSpeed = 14f;

        [Header("Zoom de Apuntado (Iron Sights)")]
        [SerializeField] private float adsFov = 46f;
        [SerializeField] private float defaultFov = 60f;
        [SerializeField] private float fovTransitionSpeed = 12f;

        [Header("Efectos Visuales (VFX)")]
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private Light muzzleLight;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] gunshotClips;
        [SerializeField] private AudioClip reloadMagOutClip;
        [SerializeField] private AudioClip reloadMagInClip;
        [SerializeField] private AudioClip reloadChargeClip;
        [SerializeField] private AudioClip selectorSwitchClip;
        [SerializeField] private AudioClip dryFireClip;
        [SerializeField] private float gunshotPitch = 1.0f;

        private float lastFireTime = 0f;
        private bool isReloading = false;
        private bool isAiming = false;
        private Camera playerCamera;
        private LineRenderer tracerLine;

        // Recoil
        private Vector3 currentRecoilEuler;
        private Vector3 targetRecoilEuler;
        [SerializeField] private float recoilKickAngle = 2.4f;

        // VR Input Tracking
        private bool wasVrTriggerHeld = false;
        private bool wasVrSecondaryHeld = false;
        private bool wasVrPrimaryHeld = false;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.25f;
            }

            currentAmmo = magazineCapacity;

            // Muzzle light si no está asignada (en la boca del cañón)
            if (muzzleLight == null)
            {
                var lightGo = new GameObject("Muzzle_Light");
                lightGo.transform.SetParent(transform, false);
                lightGo.transform.localPosition = new Vector3(0f, 0.05f, -0.35f);
                muzzleLight = lightGo.AddComponent<Light>();
                muzzleLight.type = LightType.Point;
                muzzleLight.range = 5.5f;
                muzzleLight.color = new Color(1.0f, 0.85f, 0.4f);
                muzzleLight.intensity = 0f;
                muzzleLight.enabled = false;
            }

            // Tracer line renderer
            var tracerGo = new GameObject("TracerRenderer");
            tracerGo.transform.SetParent(transform, false);
            tracerLine = tracerGo.AddComponent<LineRenderer>();
            tracerLine.useWorldSpace = true;
            tracerLine.startWidth = 0.022f;
            tracerLine.endWidth = 0.006f;
            tracerLine.positionCount = 2;
            tracerLine.enabled = false;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            var traceMat = new Material(shader);
            traceMat.color = new Color(1f, 0.9f, 0.4f, 0.95f);
            if (traceMat.HasProperty("_BaseColor")) traceMat.SetColor("_BaseColor", new Color(1f, 0.9f, 0.4f, 0.95f));
            tracerLine.material = traceMat;
        }

        private void Start()
        {
            playerCamera = GetComponentInParent<Camera>() ?? Camera.main;
            if (playerCamera != null)
            {
                defaultFov = playerCamera.fieldOfView;
            }
            transform.localPosition = hipPosition;
            transform.localRotation = Quaternion.Euler(baseRotation);
        }

        private void OnDisable()
        {
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = defaultFov;
            }
        }

        public void SetupWeapon(string id, string name, string cal, int magCap, float dmg, float rate, bool allowFullAuto = false, WeaponFireMode initialMode = WeaponFireMode.SemiAuto)
        {
            weaponId = id;
            weaponName = name;
            caliber = cal;
            magazineCapacity = magCap;
            currentAmmo = magCap;
            damage = dmg;
            canSwitchFireMode = allowFullAuto;
            fireMode = initialMode;

            // Alineación de mira mecánica (Iron Sights) según geometría exacta 3D
            if (id == "M1911")
            {
                adsPosition = new Vector3(-0.0033f, -0.0592f, 0.26f);
                hipPosition = new Vector3(0.20f, -0.18f, 0.38f);
                adsFov = 48f;
                recoilKickAngle = 2.4f;
            }
            else if (id == "M4A1")
            {
                adsPosition = new Vector3(0.0010f, -0.1065f, 0.28f);
                hipPosition = new Vector3(0.18f, -0.18f, 0.42f);
                adsFov = 44f;
                recoilKickAngle = 1.8f;
            }
            else if (id == "AK74")
            {
                adsPosition = new Vector3(-0.0036f, -0.0435f, 0.28f);
                hipPosition = new Vector3(0.18f, -0.18f, 0.42f);
                adsFov = 45f;
                recoilKickAngle = 2.0f;
            }
            else if (id == "UZI")
            {
                adsPosition = new Vector3(-0.0015f, -0.0775f, 0.28f);
                hipPosition = new Vector3(0.18f, -0.18f, 0.38f);
                adsFov = 48f;
                recoilKickAngle = 1.6f;
            }

            transform.localPosition = hipPosition;

            if (allowFullAuto)
            {
                autoFireRate = rate;
                semiFireRate = Mathf.Max(rate * 1.4f, 0.13f);
            }
            else
            {
                autoFireRate = rate;
                semiFireRate = rate;
            }

            LoadRealisticAudioClips(id);
        }

        private void LoadRealisticAudioClips(string id)
        {
            List<AudioClip> shots = new List<AudioClip>();

            if (id == "M1911")
            {
                gunshotPitch = 1.0f;
                recoilKickAngle = 2.8f;
                TryAddClip(shots, "Assets/FreeWeaponSFX_v1.1/Handgun/Gunshots/sfx_wpn_hgn_gunshot_01.wav");
                TryAddClip(shots, "Assets/FreeWeaponSFX_v1.1/Handgun/Gunshots/sfx_wpn_hgn_gunshot_02.wav");
                TryAddClip(shots, "Assets/FreeWeaponSFX_v1.1/Handgun/Gunshots/sfx_wpn_hgn_gunshot_03.wav");

                reloadMagOutClip = LoadClip("Assets/FreeWeaponSFX_v1.1/Handgun/Reload/sfx_wpn_hgn_reload_02_magOut.wav");
                reloadMagInClip = LoadClip("Assets/FreeWeaponSFX_v1.1/Handgun/Reload/sfx_wpn_hgn_reload_04_magIn.wav");
                reloadChargeClip = LoadClip("Assets/FreeWeaponSFX_v1.1/Handgun/Reload/sfx_wpn_hgn_reload_05_charge.wav");
                selectorSwitchClip = LoadClip("Assets/FreeWeaponSFX_v1.1/Handgun/Foley/sfx_wpn_hgn_foley_aim_02.wav");
                dryFireClip = LoadClip("Assets/FreeWeaponSFX_v1.1/Handgun/Foley/sfx_wpn_hgn_foley_aim_01.wav");
            }
            else if (id == "M4A1")
            {
                gunshotPitch = 1.02f;
                recoilKickAngle = 1.9f;
                TryAddClip(shots, "Assets/FreeWeaponSFX_v1.1/AssaultRifle/Gunshots/sfx_wpn_ar_gunshot_01.wav");
                TryAddClip(shots, "Assets/FreeWeaponSFX_v1.1/AssaultRifle/Gunshots/sfx_wpn_ar_gunshot_02.wav");
                TryAddClip(shots, "Assets/FreeWeaponSFX_v1.1/AssaultRifle/Gunshots/sfx_wpn_ar_gunshot_03.wav");

                reloadMagOutClip = LoadClip("Assets/FreeWeaponSFX_v1.1/AssaultRifle/Reload/sfx_wpn_ar_reload_01_magOut.wav");
                reloadMagInClip = LoadClip("Assets/FreeWeaponSFX_v1.1/AssaultRifle/Reload/sfx_wpn_ar_reload_03_magIn.wav");
                reloadChargeClip = LoadClip("Assets/FreeWeaponSFX_v1.1/AssaultRifle/Reload/sfx_wpn_ar_reload_04_charge.wav");
                selectorSwitchClip = LoadClip("Assets/FreeWeaponSFX_v1.1/AssaultRifle/Foley/sfx_wpn_ar_foley_aim_02.wav");
                dryFireClip = LoadClip("Assets/FreeWeaponSFX_v1.1/AssaultRifle/Foley/sfx_wpn_ar_foley_aim_01.wav");
            }
            else if (id == "AK74")
            {
                gunshotPitch = 0.94f;
                recoilKickAngle = 2.2f;
                TryAddClip(shots, "Assets/FreeWeaponSFX_v1.1/AssaultRifle/Gunshots/sfx_wpn_ar_gunshot_02.wav");
                TryAddClip(shots, "Assets/FreeWeaponSFX_v1.1/AssaultRifle/Gunshots/sfx_wpn_ar_gunshot_01.wav");
                TryAddClip(shots, "Assets/FreeWeaponSFX_v1.1/AssaultRifle/Gunshots/sfx_wpn_ar_gunshot_03.wav");

                reloadMagOutClip = LoadClip("Assets/FreeWeaponSFX_v1.1/AssaultRifle/Reload/sfx_wpn_ar_reload_01_magOut.wav");
                reloadMagInClip = LoadClip("Assets/FreeWeaponSFX_v1.1/AssaultRifle/Reload/sfx_wpn_ar_reload_03_magIn.wav");
                reloadChargeClip = LoadClip("Assets/FreeWeaponSFX_v1.1/AssaultRifle/Reload/sfx_wpn_ar_reload_04_charge.wav");
                selectorSwitchClip = LoadClip("Assets/FreeWeaponSFX_v1.1/AssaultRifle/Foley/sfx_wpn_ar_foley_aim_02.wav");
                dryFireClip = LoadClip("Assets/FreeWeaponSFX_v1.1/AssaultRifle/Foley/sfx_wpn_ar_foley_aim_01.wav");
            }
            else if (id == "UZI")
            {
                gunshotPitch = 1.15f;
                recoilKickAngle = 1.6f;
                TryAddClip(shots, "Assets/FreeWeaponSFX_v1.1/AssaultRifle/Gunshots/sfx_wpn_ar_gunshot_03.wav");
                TryAddClip(shots, "Assets/FreeWeaponSFX_v1.1/Handgun/Gunshots/sfx_wpn_hgn_gunshot_02.wav");
                TryAddClip(shots, "Assets/FreeWeaponSFX_v1.1/Handgun/Gunshots/sfx_wpn_hgn_gunshot_01.wav");

                reloadMagOutClip = LoadClip("Assets/FreeWeaponSFX_v1.1/Handgun/Reload/sfx_wpn_hgn_reload_02_magOut.wav");
                reloadMagInClip = LoadClip("Assets/FreeWeaponSFX_v1.1/Handgun/Reload/sfx_wpn_hgn_reload_04_magIn.wav");
                reloadChargeClip = LoadClip("Assets/FreeWeaponSFX_v1.1/Handgun/Reload/sfx_wpn_hgn_reload_05_charge.wav");
                selectorSwitchClip = LoadClip("Assets/FreeWeaponSFX_v1.1/AssaultRifle/Foley/sfx_wpn_ar_foley_aim_02.wav");
                dryFireClip = LoadClip("Assets/FreeWeaponSFX_v1.1/Handgun/Foley/sfx_wpn_hgn_foley_aim_01.wav");
            }

            // Fallback sintético si no se encontró algún clip
            if (shots.Count > 0)
            {
                gunshotClips = shots.ToArray();
            }
            else
            {
                gunshotClips = new AudioClip[] { CreateSyntheticGunshot(caliber) };
            }

            if (reloadChargeClip == null) reloadChargeClip = CreateSyntheticReload();
            if (dryFireClip == null) dryFireClip = CreateSyntheticDryFire();
            if (selectorSwitchClip == null) selectorSwitchClip = CreateSyntheticSelectorClick();
        }

        private static AudioClip LoadClip(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
#else
            return null;
#endif
        }

        private static void TryAddClip(List<AudioClip> list, string path)
        {
            var clip = LoadClip(path);
            if (clip != null) list.Add(clip);
        }

        private void Update()
        {
            HandleInput();
            UpdateWeaponPosition();
            UpdateRecoil();
        }

        public void ToggleFireMode()
        {
            if (!canSwitchFireMode) return;

            fireMode = fireMode == WeaponFireMode.SemiAuto ? WeaponFireMode.FullAuto : WeaponFireMode.SemiAuto;
            PlaySound(selectorSwitchClip, 1.15f);

            string modeName = fireMode == WeaponFireMode.FullAuto ? "AUTOMÁTICO (RÁFAGA)" : "SEMIAUTOMÁTICO (TIRO A TIRO)";
            Debug.Log($"<color=#00FFCC>[Selector de Tiro] {weaponName}: {modeName}</color>");
        }

        private void HandleInput()
        {
            // 1. Selector de modo de disparo: Tecla [B] o [V], mando VR (botón B o X) o D-pad
            bool toggleModePressed = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.bKey.wasPressedThisFrame || Keyboard.current.vKey.wasPressedThisFrame)
                {
                    toggleModePressed = true;
                }
            }
            if (Gamepad.current != null)
            {
                if (Gamepad.current.dpad.left.wasPressedThisFrame || Gamepad.current.dpad.right.wasPressedThisFrame || Gamepad.current.buttonWest.wasPressedThisFrame)
                {
                    toggleModePressed = true;
                }
            }
            if (IsVrSecondaryButtonPressed())
            {
                toggleModePressed = true;
            }

            if (toggleModePressed && canSwitchFireMode)
            {
                ToggleFireMode();
            }

            // 2. Detección de Disparo: Continuo en FullAuto vs Golpe único en SemiAuto
            bool isFiring = false;
            if (fireMode == WeaponFireMode.FullAuto)
            {
                if (Mouse.current != null && Mouse.current.leftButton.isPressed) isFiring = true;
                if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) isFiring = true;
                if (Gamepad.current != null && Gamepad.current.rightTrigger.ReadValue() > 0.4f) isFiring = true;
                if (IsVrTriggerHeld()) isFiring = true;
            }
            else
            {
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) isFiring = true;
                if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) isFiring = true;
                if (Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame) isFiring = true;
                if (IsVrTriggerPressedThisFrame()) isFiring = true;
            }

            // 3. Apuntado (ADS)
            isAiming = false;
            if (Mouse.current != null && Mouse.current.rightButton.isPressed) isAiming = true;
            if (Gamepad.current != null && Gamepad.current.leftTrigger.ReadValue() > 0.4f) isAiming = true;
            if (IsVrGripHeld()) isAiming = true;

            // 4. Recarga con [R] en PC o Botón 'A' / 'X' en mandos VR
            bool reloadPressed = false;
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) reloadPressed = true;
            if (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame) reloadPressed = true;
            if (IsVrPrimaryButtonPressed()) reloadPressed = true;

            float currentRate = fireMode == WeaponFireMode.FullAuto ? autoFireRate : semiFireRate;
            if (isFiring && Time.time >= lastFireTime + currentRate && !isReloading)
            {
                TryShoot();
            }

            if (reloadPressed && !isReloading && currentAmmo < magazineCapacity)
            {
                StartCoroutine(ReloadSequence());
            }
        }

        #region VR Input Helpers (Meta Quest 3)
        private bool IsVrTriggerHeld()
        {
            var rightDevices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                UnityEngine.XR.InputDeviceCharacteristics.Right | UnityEngine.XR.InputDeviceCharacteristics.Controller,
                rightDevices
            );
            for (int i = 0; i < rightDevices.Count; i++)
            {
                if (rightDevices[i].TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float triggerVal) && triggerVal > 0.45f)
                    return true;
            }
            return false;
        }

        private bool IsVrTriggerPressedThisFrame()
        {
            bool isHeld = IsVrTriggerHeld();
            bool pressed = isHeld && !wasVrTriggerHeld;
            wasVrTriggerHeld = isHeld;
            return pressed;
        }

        private bool IsVrSecondaryButtonPressed()
        {
            var rightDevices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                UnityEngine.XR.InputDeviceCharacteristics.Right | UnityEngine.XR.InputDeviceCharacteristics.Controller,
                rightDevices
            );
            bool held = false;
            for (int i = 0; i < rightDevices.Count; i++)
            {
                if (rightDevices[i].TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool bVal) && bVal)
                {
                    held = true;
                    break;
                }
            }
            bool pressed = held && !wasVrSecondaryHeld;
            wasVrSecondaryHeld = held;
            return pressed;
        }

        private bool IsVrPrimaryButtonPressed()
        {
            var rightDevices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                UnityEngine.XR.InputDeviceCharacteristics.Right | UnityEngine.XR.InputDeviceCharacteristics.Controller,
                rightDevices
            );
            bool held = false;
            for (int i = 0; i < rightDevices.Count; i++)
            {
                if (rightDevices[i].TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool aVal) && aVal)
                {
                    held = true;
                    break;
                }
            }
            bool pressed = held && !wasVrPrimaryHeld;
            wasVrPrimaryHeld = held;
            return pressed;
        }

        private bool IsVrGripHeld()
        {
            var rightDevices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                UnityEngine.XR.InputDeviceCharacteristics.Right | UnityEngine.XR.InputDeviceCharacteristics.Controller,
                rightDevices
            );
            for (int i = 0; i < rightDevices.Count; i++)
            {
                if (rightDevices[i].TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out float gripVal) && gripVal > 0.45f)
                    return true;
            }
            return false;
        }
        private void TriggerShotHaptic(float amplitude, float duration)
        {
            var rightDevices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                UnityEngine.XR.InputDeviceCharacteristics.Right | UnityEngine.XR.InputDeviceCharacteristics.Controller,
                rightDevices
            );
            for (int i = 0; i < rightDevices.Count; i++)
            {
                rightDevices[i].SendHapticImpulse(0, amplitude, duration);
            }
        }
        #endregion

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
            // 1. Sonido de detonación realista (variación aleatoria de disparos reales)
            if (gunshotClips != null && gunshotClips.Length > 0)
            {
                int idx = Random.Range(0, gunshotClips.Length);
                PlaySound(gunshotClips[idx], gunshotPitch * Random.Range(0.97f, 1.03f));
            }

            // 2. Destello de boca de cañón (Muzzle Flash)
            StartCoroutine(FlashMuzzleLight());

            // 3. Retroceso balístico y vibración háptica en mandos Meta Quest 3
            targetRecoilEuler += new Vector3(-recoilKickAngle, Random.Range(-0.35f, 0.35f), 0f);
            TriggerShotHaptic(0.85f, 0.12f);

            // 4. Registrar disparo en el polígono
            if (ShootingRangeManager.Instance != null)
            {
                ShootingRangeManager.Instance.RegisterShot();
            }

            // 5. Raycast balístico
            if (playerCamera == null) playerCamera = Camera.main;
            Vector3 aimViewport = new Vector3(0.5f, 0.5f, 0f);
            if (!isAiming)
            {
                // Dispersión balística natural al disparar desde la cadera (exige encarar la mira del arma para precisión quirúrgica)
                Vector2 hipSpread = UnityEngine.Random.insideUnitCircle * 0.022f;
                aimViewport.x += hipSpread.x;
                aimViewport.y += hipSpread.y;
            }

            Ray ray = playerCamera != null
                ? playerCamera.ViewportPointToRay(aimViewport)
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

            // 6. Trazadora visual de bala orientada hacia adelante en el eje de la cámara
            Vector3 forwardDir = playerCamera != null ? playerCamera.transform.forward : transform.forward;
            Vector3 muzzlePos = muzzlePoint != null ? muzzlePoint.position : transform.position + forwardDir * 0.40f;
            StartCoroutine(RenderTracer(muzzlePos, hitPoint));
        }

        private IEnumerator FlashMuzzleLight()
        {
            if (muzzleLight != null)
            {
                muzzleLight.enabled = true;
                muzzleLight.intensity = Random.Range(5.0f, 6.5f);
                yield return new WaitForSeconds(0.04f);
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
                yield return new WaitForSeconds(0.05f);
                tracerLine.enabled = false;
            }
        }

        private IEnumerator ReloadSequence()
        {
            isReloading = true;

            // Fase 1: Extraer cargador vacío
            if (reloadMagOutClip != null)
            {
                PlaySound(reloadMagOutClip, 1.0f);
            }
            yield return new WaitForSeconds(0.38f);

            // Fase 2: Insertar cargador nuevo
            if (reloadMagInClip != null)
            {
                PlaySound(reloadMagInClip, 1.0f);
            }
            else if (reloadChargeClip != null)
            {
                PlaySound(reloadChargeClip, 1.0f);
            }
            yield return new WaitForSeconds(0.42f);

            // Fase 3: Accionar corredera / cerrojo
            if (reloadChargeClip != null)
            {
                PlaySound(reloadChargeClip, 1.05f);
            }
            yield return new WaitForSeconds(0.30f);

            currentAmmo = magazineCapacity;
            isReloading = false;
        }

        private void UpdateWeaponPosition()
        {
            Vector3 targetPos = isAiming ? adsPosition : hipPosition;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * adsSpeed);

            if (playerCamera != null)
            {
                float targetFov = isAiming ? adsFov : defaultFov;
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, Time.deltaTime * fovTransitionSpeed);
            }
        }

        private void UpdateRecoil()
        {
            targetRecoilEuler = Vector3.Lerp(targetRecoilEuler, Vector3.zero, Time.deltaTime * 14f);
            currentRecoilEuler = Vector3.Lerp(currentRecoilEuler, targetRecoilEuler, Time.deltaTime * 24f);
            transform.localRotation = Quaternion.Euler(baseRotation) * Quaternion.Euler(currentRecoilEuler);
        }

        private void OnGUI()
        {
            if (Camera.main == null) return;

            // NOTA: Se elimina todo puntero/retícula flotante de pantalla a petición del usuario.
            // El usuario apunta únicamente a través de las miras mecánicas (Iron Sights) 3D del arma.

            // HUD inferior derecho de munición y modo de fuego
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };

            float panelW = 390f;
            float panelH = 68f;
            float x = Screen.width - panelW - 30f;
            float y = Screen.height - panelH - 30f;

            GUI.color = new Color(0.04f, 0.12f, 0.18f, 0.90f);
            GUI.DrawTexture(new Rect(x, y, panelW, panelH), Texture2D.whiteTexture);

            // Borde superior cian
            GUI.color = new Color(0f, 0.85f, 1f, 0.75f);
            GUI.DrawTexture(new Rect(x, y, panelW, 2f), Texture2D.whiteTexture);

            // Contador de munición
            GUI.color = isReloading ? new Color(1f, 0.8f, 0.2f) : (currentAmmo == 0 ? new Color(1f, 0.25f, 0.25f) : Color.white);
            string ammoText = isReloading ? "RECARGANDO..." : $"{currentAmmo} / {magazineCapacity}";
            GUI.Label(new Rect(x + 10f, y + 8f, panelW - 20f, 26f), ammoText, style);

            // Indicador del Modo de Fuego activo y control de mira
            string modeStr = fireMode == WeaponFireMode.FullAuto ? "[AUTO]" : "[SEMI]";
            string switchHint = canSwitchFireMode ? " | [B] Modo" : "";
            string aimHint = isAiming ? " | [EN MIRA]" : " | [Clic Der] Mira";

            style.fontSize = 12;
            style.fontStyle = FontStyle.Normal;
            GUI.color = new Color(0.2f, 0.9f, 1.0f);
            GUI.Label(new Rect(x + 10f, y + 38f, panelW - 20f, 22f), $"{weaponName.ToUpper()}  {modeStr}  [{caliber}]{switchHint}{aimHint}  |  [R] Recargar", style);
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

        #region Generadores de Audio Balístico (Fallback Sintético)
        private static AudioClip CreateSyntheticGunshot(string cal)
        {
            int sampleRate = 44100;
            float duration = 0.45f;
            int length = (int)(sampleRate * duration);
            float[] data = new float[length];
            float baseFreq = cal.Contains(".45") ? 95f : (cal.Contains("5.56") ? 140f : (cal.Contains("5.45") ? 130f : 160f));

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float initialCrack = Mathf.Exp(-t * 110f) * (Random.value * 2f - 1f) * 1.0f;
                float boom = Mathf.Sin(2f * Mathf.PI * baseFreq * Mathf.Exp(-t * 22f) * t) * Mathf.Exp(-t * 12f) * 0.8f;
                float tail = (Random.value * 2f - 1f) * Mathf.Exp(-t * 8f) * 0.35f;
                data[i] = Mathf.Clamp(initialCrack + boom + tail, -1f, 1f) * 0.95f;
            }

            var clip = AudioClip.Create("Gunshot_" + cal.Replace(" ", "_"), length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateSyntheticReload()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.7f);
            float[] data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float insert = Mathf.Exp(-Mathf.Abs(t - 0.30f) * 50f) * Mathf.Sin(2f * Mathf.PI * 950f * t) * 0.7f;
                float rack = Mathf.Exp(-Mathf.Abs(t - 0.55f) * 70f) * Mathf.Sin(2f * Mathf.PI * 1400f * t) * 0.8f;
                data[i] = insert + rack;
            }
            var clip = AudioClip.Create("WeaponReload", length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateSyntheticDryFire()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.08f);
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

        private static AudioClip CreateSyntheticSelectorClick()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.04f);
            float[] data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                data[i] = Mathf.Sin(2f * Mathf.PI * 3200f * t) * Mathf.Exp(-t * 120f) * 0.5f;
            }
            var clip = AudioClip.Create("SelectorClick", length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
        #endregion
    }
}