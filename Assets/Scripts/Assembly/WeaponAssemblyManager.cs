using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

namespace VRRangeAcademy.Assembly
{
    [Serializable]
    public class WeaponPartData
    {
        public string partId;
        public string displayName;
        public string meshName;
        public string functionDescription;
        public Vector3 tableLocalPos;
        public Vector3 tableLocalRot;
        public Vector3 socketLocalPos;
        public Vector3 socketLocalRot;
        public Vector3 scale = Vector3.one;
    }

    [Serializable]
    public class WeaponData
    {
        public string weaponId;
        public string weaponName;
        public string caliber;
        public string fbxPath;
        public float modelScale = 1.8f;
        public Vector3 hologramOffset = new Vector3(0f, 1.48f, 1.42f);
        public Vector3 hologramEuler = new Vector3(0f, 85f, -8f);
        public List<WeaponPartData> parts = new List<WeaponPartData>();
    }

    public class WeaponAssemblyManager : MonoBehaviour
    {
        [Header("Definición de Armas")]
        [SerializeField] private List<WeaponData> availableWeapons = new List<WeaponData>();
        [SerializeField] private int currentWeaponIndex = 0;

        [Header("Contenedores de Escena")]
        [SerializeField] private Transform piecesContainer;
        [SerializeField] private Transform hologramContainer;
        [SerializeField] private GuidanceArrow guidanceArrow;

        [Header("Materiales")]
        [SerializeField] private Material gunMaterial;
        [SerializeField] private Material hologramMaterial;
        [SerializeField] private Material socketGlowMaterial;

        [Header("UI Holográfica / HUD")]
        [SerializeField] private TextMeshProUGUI txtWeaponTitle;
        [SerializeField] private TextMeshProUGUI txtStepCounter;
        [SerializeField] private TextMeshProUGUI txtStepTitle;
        [SerializeField] private TextMeshProUGUI txtStepDescription;
        [SerializeField] private TextMeshProUGUI txtControlsHint;
        [SerializeField] private Button btnPrevWeapon;
        [SerializeField] private Button btnNextWeapon;
        [SerializeField] private GameObject panelCompletion;
        [SerializeField] private TextMeshProUGUI txtCompletionTitle;
        [SerializeField] private Button btnProceedToShootingRange;
        [SerializeField] private Button btnResetAssembly;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip snapClip;
        [SerializeField] private AudioClip slideRackClip;
        [SerializeField] private AudioClip completionChimeClip;

        [Header("Escena Siguiente")]
        [SerializeField] private string shootingRangeSceneName = "ShootingRange";

        private List<AssemblyPiece> activePieces = new List<AssemblyPiece>();
        private int currentStepIndex = 0;
        private int totalSnappedCount = 0;
        private bool isAssemblyCompleted = false;

        public bool IsCompleted => isAssemblyCompleted;
        public int CurrentWeaponIndex => currentWeaponIndex;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (snapClip == null) snapClip = CreateMechanicalSnapClip();
            if (slideRackClip == null) slideRackClip = CreateSlideRackClip();
            if (completionChimeClip == null) completionChimeClip = CreateCompletionChimeClip();

            InitializeDefaultWeaponsIfEmpty();
        }

        private void Start()
        {
            HookUI();
            LoadWeapon(currentWeaponIndex);
        }

        private void Update()
        {
            HandleKeyboardWeaponSwitch();
        }

        private void HandleKeyboardWeaponSwitch()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.digit1Key.wasPressedThisFrame) SwitchWeapon(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) SwitchWeapon(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) SwitchWeapon(2);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) SwitchWeapon(3);

            if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.qKey.wasPressedThisFrame)
            {
                OnPrevWeaponClicked();
            }
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.rKey.wasPressedThisFrame)
            {
                OnNextWeaponClicked();
            }
        }

        private void HookUI()
        {
            if (btnPrevWeapon != null) btnPrevWeapon.onClick.AddListener(OnPrevWeaponClicked);
            if (btnNextWeapon != null) btnNextWeapon.onClick.AddListener(OnNextWeaponClicked);
            if (btnProceedToShootingRange != null) btnProceedToShootingRange.onClick.AddListener(OnProceedToShootingRange);
            if (btnResetAssembly != null) btnResetAssembly.onClick.AddListener(ResetAssembly);
        }

        public void OnPrevWeaponClicked()
        {
            if (availableWeapons.Count == 0) return;
            int nextIdx = (currentWeaponIndex - 1 + availableWeapons.Count) % availableWeapons.Count;
            SwitchWeapon(nextIdx);
        }

        public void OnNextWeaponClicked()
        {
            if (availableWeapons.Count == 0) return;
            int nextIdx = (currentWeaponIndex + 1) % availableWeapons.Count;
            SwitchWeapon(nextIdx);
        }

        public void SwitchWeapon(int index)
        {
            if (index < 0 || index >= availableWeapons.Count) return;
            currentWeaponIndex = index;
            PlaySound(snapClip, 1.2f);
            LoadWeapon(currentWeaponIndex);
        }

        public void LoadWeapon(int weaponIndex)
        {
            if (weaponIndex < 0 || weaponIndex >= availableWeapons.Count) return;
            var w = availableWeapons[weaponIndex];

            StopAllCoroutines();
            isAssemblyCompleted = false;
            totalSnappedCount = 0;
            currentStepIndex = 0;

            if (panelCompletion != null) panelCompletion.SetActive(false);

            // 1. Limpiar piezas y holograma anteriores
            ClearActiveAssembly();

            // 2. Construir Holograma de Referencia y Sockets
            BuildWeaponHologram(w);

            // 3. Generar Piezas Físicas en la Mesa
            BuildWeaponPiecesOnTable(w);

            // 4. Actualizar UI y Flecha
            UpdateWeaponHeaderUI(w);
            UpdateStepUI();
            UpdateGuidanceArrow();
        }

        private void ClearActiveAssembly()
        {
            var player = UnityEngine.Object.FindAnyObjectByType<AssemblyPlayerController>();
            if (player != null) player.DropHeldPiece();

            foreach (var p in activePieces)
            {
                if (p != null)
                {
                    p.OnSnapped -= HandlePieceSnapped;
                    p.OnGrabbed -= HandlePieceGrabbed;
                    p.gameObject.SetActive(false);
                    if (Application.isPlaying)
                    {
                        p.transform.SetParent(null);
                        Destroy(p.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(p.gameObject);
                    }
                }
            }
            activePieces.Clear();

            if (piecesContainer != null)
            {
                for (int i = piecesContainer.childCount - 1; i >= 0; i--)
                {
                    var child = piecesContainer.GetChild(i).gameObject;
                    child.SetActive(false);
                    if (Application.isPlaying)
                    {
                        child.transform.SetParent(null);
                        Destroy(child);
                    }
                    else
                    {
                        DestroyImmediate(child);
                    }
                }
            }

            if (hologramContainer != null)
            {
                for (int i = hologramContainer.childCount - 1; i >= 0; i--)
                {
                    var child = hologramContainer.GetChild(i).gameObject;
                    child.SetActive(false);
                    if (Application.isPlaying)
                    {
                        child.transform.SetParent(null);
                        Destroy(child);
                    }
                    else
                    {
                        DestroyImmediate(child);
                    }
                }
            }

            // Si el jugador tenía un arma funcional equipada, removerla al reiniciar/cambiar arma
            var cam = Camera.main;
            if (cam != null)
            {
                var existing = cam.GetComponentInChildren<Shooting.WeaponShootingController>();
                if (existing != null)
                {
                    if (Application.isPlaying) Destroy(existing.gameObject);
                    else DestroyImmediate(existing.gameObject);
                }
            }

            totalSnappedCount = 0;
            currentStepIndex = 0;
            isAssemblyCompleted = false;
        }

        private void BuildWeaponHologram(WeaponData w)
        {
            if (hologramContainer == null) return;

            hologramContainer.position = w.hologramOffset;
            hologramContainer.rotation = Quaternion.Euler(w.hologramEuler);
            hologramContainer.localScale = Vector3.one * w.modelScale;

            // Cargar modelo FBX
            var prefab = Resources.Load<GameObject>(w.weaponId) ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(w.fbxPath);
            if (prefab != null)
            {
                var ghost = Instantiate(prefab, hologramContainer);
                ghost.name = "Ghost_" + w.weaponId;
                ghost.transform.localPosition = Vector3.zero;
                ghost.transform.localRotation = Quaternion.identity;
                ghost.transform.localScale = Vector3.one;

                foreach (var mr in ghost.GetComponentsInChildren<MeshRenderer>())
                {
                    mr.sharedMaterial = hologramMaterial;
                }
                foreach (var col in ghost.GetComponentsInChildren<Collider>())
                {
                    Destroy(col);
                }
            }

            // Crear Sockets para cada pieza
            foreach (var part in w.parts)
            {
                var socketGo = new GameObject("Socket_" + part.partId);
                socketGo.transform.SetParent(hologramContainer, false);
                socketGo.transform.localPosition = part.socketLocalPos;
                socketGo.transform.localRotation = Quaternion.Euler(part.socketLocalRot);

                // Anclaje luminoso
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "Anchor_Glow";
                sphere.transform.SetParent(socketGo.transform, false);
                sphere.transform.localPosition = Vector3.zero;
                sphere.transform.localScale = Vector3.one * 0.02f;
                Destroy(sphere.GetComponent<Collider>());
                if (socketGlowMaterial != null)
                {
                    sphere.GetComponent<MeshRenderer>().sharedMaterial = socketGlowMaterial;
                }
            }
        }

        private void BuildWeaponPiecesOnTable(WeaponData w)
        {
            if (piecesContainer == null) return;

            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(w.fbxPath);

            foreach (var part in w.parts)
            {
                var pieceGo = new GameObject("Piece_" + part.partId);
                pieceGo.transform.SetParent(piecesContainer, false);
                pieceGo.transform.localPosition = part.tableLocalPos;
                pieceGo.transform.localRotation = Quaternion.Euler(part.tableLocalRot);
                pieceGo.transform.localScale = Vector3.one * w.modelScale;

                var mf = pieceGo.AddComponent<MeshFilter>();
                var mr = pieceGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = gunMaterial;

                // Buscar la submalla correspondiente
                var mesh = FindSubmesh(w.fbxPath, part.meshName);
                if (mesh != null) mf.sharedMesh = mesh;

                // Collider
                var col = pieceGo.AddComponent<BoxCollider>();
                Vector3 sz = col.size;
                sz.x = Mathf.Max(sz.x, 0.08f);
                sz.y = Mathf.Max(sz.y, 0.08f);
                sz.z = Mathf.Max(sz.z, 0.08f);
                col.size = sz;

                // Rigidbody
                var rb = pieceGo.AddComponent<Rigidbody>();
                rb.mass = 0.5f;
                rb.isKinematic = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

                // AssemblyPiece component
                var pieceComp = pieceGo.AddComponent<AssemblyPiece>();
                pieceComp.pieceId = part.partId;
                pieceComp.displayName = part.displayName;
                pieceComp.functionDescription = part.functionDescription;
                pieceComp.snapThresholdDistance = 0.22f;

                // Vincular su socket de destino
                var socketTrans = hologramContainer.Find("Socket_" + part.partId);
                pieceComp.targetSocket = socketTrans;
                pieceComp.SetInitialTransform(pieceGo.transform.position, pieceGo.transform.rotation);

                pieceComp.OnSnapped += HandlePieceSnapped;
                pieceComp.OnGrabbed += HandlePieceGrabbed;

                activePieces.Add(pieceComp);
            }
        }

        private Mesh FindSubmesh(string fbxPath, string meshName)
        {
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in assets)
            {
                if (a is Mesh m && m.name == meshName) return m;
            }
            return null;
        }

        private void HandlePieceGrabbed(AssemblyPiece piece)
        {
            if (piece == null || isAssemblyCompleted) return;
            if (guidanceArrow != null && piece.targetSocket != null)
            {
                guidanceArrow.SetTargets(piece.transform, piece.targetSocket);
                guidanceArrow.SetVisible(true);
            }
        }

        private void HandlePieceSnapped(AssemblyPiece piece)
        {
            totalSnappedCount++;
            PlaySound(snapClip, 1.0f);

            FindNextPendingStep();
            UpdateStepUI();
            UpdateGuidanceArrow();

            if (totalSnappedCount >= activePieces.Count)
            {
                StartCoroutine(CompleteAssemblySequence());
            }
        }

        private void FindNextPendingStep()
        {
            for (int i = 0; i < activePieces.Count; i++)
            {
                if (activePieces[i] != null && !activePieces[i].IsSnapped)
                {
                    currentStepIndex = i;
                    return;
                }
            }
            currentStepIndex = activePieces.Count;
        }

        private void UpdateWeaponHeaderUI(WeaponData w)
        {
            if (txtWeaponTitle != null)
            {
                txtWeaponTitle.text = $"{w.weaponName.ToUpper()}  [{w.caliber}]";
            }
        }

        private void UpdateStepUI()
        {
            if (isAssemblyCompleted) return;

            if (currentStepIndex < activePieces.Count)
            {
                var cur = activePieces[currentStepIndex];
                if (txtStepCounter != null)
                {
                    txtStepCounter.text = $"PASO {totalSnappedCount + 1} DE {activePieces.Count}";
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
                    txtControlsHint.text = "[W,A,S,D] Moverse | [Clic/E] Agarrar | [1-4] Cambiar Arma | [Tab] Alternar Cursor";
                }
            }
        }

        private void UpdateGuidanceArrow()
        {
            if (guidanceArrow == null) return;

            if (isAssemblyCompleted || currentStepIndex >= activePieces.Count)
            {
                guidanceArrow.SetVisible(false);
                return;
            }

            var nextPiece = activePieces[currentStepIndex];
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
            if (guidanceArrow != null) guidanceArrow.SetVisible(false);

            yield return new WaitForSeconds(0.4f);
            PlaySound(slideRackClip, 1.0f);

            yield return new WaitForSeconds(0.6f);
            PlaySound(completionChimeClip, 1.2f);

            if (panelCompletion != null) panelCompletion.SetActive(true);
            var curW = availableWeapons[currentWeaponIndex];
            if (txtCompletionTitle != null)
            {
                txtCompletionTitle.text = $"[!] ENSAMBLAJE DE {curW.weaponName.ToUpper()} COMPLETADO\nAcceso al Polígono de Tiro Desbloqueado";
            }

            // Desbloquear el Polígono Integrado y equipar arma funcional
            if (Shooting.ShootingRangeManager.Instance != null)
            {
                Shooting.ShootingRangeManager.Instance.UnlockRange(curW.weaponName);
            }

            EquipFunctionalWeaponToPlayer(curW);
        }

        private void EquipFunctionalWeaponToPlayer(WeaponData w)
        {
            var cam = Camera.main;
            if (cam == null) return;

            var existing = cam.GetComponentInChildren<Shooting.WeaponShootingController>();
            if (existing != null) Destroy(existing.gameObject);

            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(w.fbxPath);
            if (prefab == null) return;

            var gunGo = Instantiate(prefab, cam.transform);
            gunGo.name = "Equipped_" + w.weaponId;
            gunGo.transform.localPosition = new Vector3(0.24f, -0.22f, 0.45f);
            gunGo.transform.localRotation = Quaternion.identity;
            gunGo.transform.localScale = Vector3.one * (w.modelScale * 0.70f);

            foreach (var col in gunGo.GetComponentsInChildren<Collider>()) Destroy(col);
            foreach (var rb in gunGo.GetComponentsInChildren<Rigidbody>()) Destroy(rb);
            foreach (var mr in gunGo.GetComponentsInChildren<MeshRenderer>())
            {
                if (gunMaterial != null) mr.sharedMaterial = gunMaterial;
            }

            var shootCtrl = gunGo.AddComponent<Shooting.WeaponShootingController>();
            int cap = w.weaponId == "M1911" ? 7 : (w.weaponId == "UZI" ? 25 : 30);
            float rate = w.weaponId == "UZI" ? 0.10f : (w.weaponId == "M4A1" ? 0.12f : (w.weaponId == "AK74" ? 0.14f : 0.20f));
            shootCtrl.SetupWeapon(w.weaponId, w.weaponName, w.caliber, cap, 50f, rate);
        }

        public void OnProceedToShootingRange()
        {
            PlaySound(snapClip, 1.0f);
            if (panelCompletion != null) panelCompletion.SetActive(false);

            // Teleportar al jugador al Puesto B (carril central X=4.6, mirar hacia Z+)
            // Puesto B apunta directo a Diana1 circular (5m); el jugador puede moverse a A o C para las otras dianas
            var player = FindAnyObjectByType<AssemblyPlayerController>();
            if (player != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.transform.position = new Vector3(4.6f, 0f, 0.5f);
                player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                if (cc != null) cc.enabled = true;
            }

            // Cambiar la cámara para mirar hacia el polígono (Z positivo = hacia las dianas)
            var cam = Camera.main;
            if (cam != null)
                cam.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }

        public void ResetAssembly()
        {
            LoadWeapon(currentWeaponIndex);
        }

        private void PlaySound(AudioClip clip, float pitch = 1.0f)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.pitch = pitch;
                audioSource.PlayOneShot(clip);
            }
        }

        #region Weapons Data Initialization
        public void ForceReinitializeDefaultWeapons()
        {
            availableWeapons.Clear();
            InitializeDefaultWeaponsIfEmpty();
        }

        public void InitializeDefaultWeaponsIfEmpty()
        {
            if (availableWeapons.Count > 0) return;

            // 1. M1911 Pistol
            var m1911 = new WeaponData {
                weaponId = "M1911",
                weaponName = "Pistola Colt M1911",
                caliber = ".45 ACP",
                fbxPath = "Assets/Low Poly Weapons VOL.1/Models/M1911.fbx",
                modelScale = 1.9f,
                hologramOffset = new Vector3(0f, 1.48f, 1.42f),
                hologramEuler = new Vector3(0f, 85f, -8f)
            };
            m1911.parts.Add(new WeaponPartData {
                partId = "Frame", displayName = "Armazón (Frame)", meshName = "M1911",
                functionDescription = "Estructura principal que aloja el cargador, gatillo y guías de corredera.",
                tableLocalPos = new Vector3(-0.05f, 1.075f, 0.06f), tableLocalRot = new Vector3(0f, 90f, 90f),
                socketLocalPos = Vector3.zero, socketLocalRot = Vector3.zero
            });
            m1911.parts.Add(new WeaponPartData {
                partId = "Slide", displayName = "Corredera (Slide)", meshName = "Slide",
                functionDescription = "Aloja la aguja percutora, extrae vainas y alimenta nuevos cartuchos.",
                tableLocalPos = new Vector3(-0.45f, 1.075f, 0.06f), tableLocalRot = new Vector3(0f, 90f, 90f),
                socketLocalPos = new Vector3(0f, 0.01f, -0.14f), socketLocalRot = Vector3.zero
            });
            m1911.parts.Add(new WeaponPartData {
                partId = "Trigger", displayName = "Gatillo (Trigger)", meshName = "Trigger",
                functionDescription = "Acciona el fiador interno para liberar el martillo percutor.",
                tableLocalPos = new Vector3(-0.15f, 1.075f, -0.14f), tableLocalRot = new Vector3(0f, 90f, 0f),
                socketLocalPos = new Vector3(0f, -0.02f, -0.01f), socketLocalRot = Vector3.zero
            });
            m1911.parts.Add(new WeaponPartData {
                partId = "Hammer", displayName = "Martillo (Hammer)", meshName = "Hammer",
                functionDescription = "Transmite la energía cinética del resorte principal al percutor.",
                tableLocalPos = new Vector3(0.12f, 1.13f, -0.14f), tableLocalRot = new Vector3(0f, 90f, 0f),
                socketLocalPos = new Vector3(0f, 0.04f, 0.05f), socketLocalRot = Vector3.zero
            });
            m1911.parts.Add(new WeaponPartData {
                partId = "Magazine", displayName = "Cargador (Magazine)", meshName = "Magazin",
                functionDescription = "Almacena los cartuchos .45 ACP y los eleva hacia la recámara.",
                tableLocalPos = new Vector3(0.42f, 1.075f, -0.02f), tableLocalRot = new Vector3(0f, 90f, 90f),
                socketLocalPos = new Vector3(0f, -0.05f, 0.05f), socketLocalRot = new Vector3(339.32f, 0f, 0f)
            });
            availableWeapons.Add(m1911);

            // 2. M4A1 Carbine
            var m4 = new WeaponData {
                weaponId = "M4A1",
                weaponName = "Carabina M4A1",
                caliber = "5.56x45mm NATO",
                fbxPath = "Assets/Low Poly Weapons VOL.1/Models/M4_8.fbx",
                modelScale = 1.35f,
                hologramOffset = new Vector3(0f, 1.50f, 1.42f),
                hologramEuler = new Vector3(0f, 85f, -8f)
            };
            m4.parts.Add(new WeaponPartData {
                partId = "Receiver", displayName = "Cajón y Cañón (Upper & Lower)", meshName = "M4_8",
                functionDescription = "Estructura central con cañón estriado, recámara y sistema de gases.",
                tableLocalPos = new Vector3(0.00f, 1.075f, 0.10f), tableLocalRot = new Vector3(0f, 90f, 90f),
                socketLocalPos = Vector3.zero, socketLocalRot = Vector3.zero
            });
            m4.parts.Add(new WeaponPartData {
                partId = "Bolt", displayName = "Cerrojo Rotativo (Bolt Carrier)", meshName = "Bolt",
                functionDescription = "Bloquea la recámara y extrae los casquillos disparados.",
                tableLocalPos = new Vector3(-0.45f, 1.075f, -0.12f), tableLocalRot = new Vector3(0f, 90f, 0f),
                socketLocalPos = new Vector3(0f, 0.08f, 0.16f), socketLocalRot = Vector3.zero
            });
            m4.parts.Add(new WeaponPartData {
                partId = "Mag", displayName = "Cargador STANAG (30 Cartuchos)", meshName = "Mag",
                functionDescription = "Cargador reglamentario de alta capacidad para munición 5.56 NATO.",
                tableLocalPos = new Vector3(0.45f, 1.075f, -0.07f), tableLocalRot = new Vector3(0f, 90f, 90f),
                socketLocalPos = Vector3.zero, socketLocalRot = Vector3.zero
            });
            m4.parts.Add(new WeaponPartData {
                partId = "Sight", displayName = "Mira Frontal Táctica (Front Sight)", meshName = "Sight",
                functionDescription = "Punto de mira frontal para adquisición rápida de objetivos.",
                tableLocalPos = new Vector3(-0.35f, 1.075f, 0.13f), tableLocalRot = new Vector3(0f, 90f, 0f),
                socketLocalPos = new Vector3(0f, 0.09f, -0.20f), socketLocalRot = new Vector3(270f, 0f, 0f)
            });
            m4.parts.Add(new WeaponPartData {
                partId = "Trigger", displayName = "Mecanismo de Gatillo (Trigger)", meshName = "Trigger",
                functionDescription = "Controla la cadencia de fuego y la liberación del martillo.",
                tableLocalPos = new Vector3(0.15f, 1.075f, -0.16f), tableLocalRot = new Vector3(0f, 90f, 0f),
                socketLocalPos = new Vector3(0f, 0.01f, 0.09f), socketLocalRot = Vector3.zero
            });
            availableWeapons.Add(m4);

            // 3. AK-74 Assault Rifle
            var ak74 = new WeaponData {
                weaponId = "AK74",
                weaponName = "Fusil de Asalto AK-74",
                caliber = "5.45x39mm",
                fbxPath = "Assets/Low Poly Weapons VOL.1/Models/AK74.fbx",
                modelScale = 1.35f,
                hologramOffset = new Vector3(0f, 1.50f, 1.42f),
                hologramEuler = new Vector3(0f, 85f, -8f)
            };
            ak74.parts.Add(new WeaponPartData {
                partId = "Receiver", displayName = "Cajón de Mecanismos y Cañón", meshName = "AK74",
                functionDescription = "Estructura de acero forjado con cilindro de gases y culata de polímero.",
                tableLocalPos = new Vector3(0.00f, 1.075f, 0.10f), tableLocalRot = new Vector3(0f, 90f, 90f),
                socketLocalPos = Vector3.zero, socketLocalRot = Vector3.zero
            });
            ak74.parts.Add(new WeaponPartData {
                partId = "Bolt", displayName = "Portacerrojo con Pistón de Gases", meshName = "Bolt",
                functionDescription = "Pistón de carrera larga acoplado directamente al cerrojo rotativo.",
                tableLocalPos = new Vector3(-0.45f, 1.075f, -0.12f), tableLocalRot = new Vector3(0f, 90f, 0f),
                socketLocalPos = new Vector3(0f, 0.01f, -0.05f), socketLocalRot = Vector3.zero
            });
            ak74.parts.Add(new WeaponPartData {
                partId = "Mag", displayName = "Cargador Curvo Banana (30 T.)", meshName = "Mag",
                functionDescription = "Cargador curvo característico de baquelita reforzada para calibre 5.45mm.",
                tableLocalPos = new Vector3(0.45f, 1.075f, -0.07f), tableLocalRot = new Vector3(0f, 90f, 90f),
                socketLocalPos = Vector3.zero, socketLocalRot = Vector3.zero
            });
            ak74.parts.Add(new WeaponPartData {
                partId = "Trigger", displayName = "Gatillo y Fiador (Trigger)", meshName = "Trigger",
                functionDescription = "Conjunto de disparo robusto diseñado para máxima confiabilidad.",
                tableLocalPos = new Vector3(0.12f, 1.075f, -0.16f), tableLocalRot = new Vector3(0f, 90f, 0f),
                socketLocalPos = Vector3.zero, socketLocalRot = Vector3.zero
            });
            availableWeapons.Add(ak74);

            // 4. UZI Submachine Gun
            var uzi = new WeaponData {
                weaponId = "UZI",
                weaponName = "Subfusil UZI",
                caliber = "9x19mm Parabellum",
                fbxPath = "Assets/Low Poly Weapons VOL.1/Models/Uzi.fbx",
                modelScale = 1.6f,
                hologramOffset = new Vector3(0f, 1.48f, 1.42f),
                hologramEuler = new Vector3(0f, 85f, -8f)
            };
            uzi.parts.Add(new WeaponPartData {
                partId = "Receiver", displayName = "Cajón y Empuñadura (Receiver)", meshName = "Uzi",
                functionDescription = "Cajón de chapa estampada con cargador insertado en la empuñadura central.",
                tableLocalPos = new Vector3(0.00f, 1.075f, 0.06f), tableLocalRot = new Vector3(0f, 90f, 90f),
                socketLocalPos = Vector3.zero, socketLocalRot = Vector3.zero
            });
            uzi.parts.Add(new WeaponPartData {
                partId = "Bolt", displayName = "Cerrojo Telescópico (Telescoping Bolt)", meshName = "Bolt",
                functionDescription = "Cerrojo que envuelve la recámara para reducir la longitud total del arma.",
                tableLocalPos = new Vector3(-0.40f, 1.075f, -0.12f), tableLocalRot = new Vector3(0f, 90f, 0f),
                socketLocalPos = new Vector3(0f, 0.03f, 0.02f), socketLocalRot = Vector3.zero
            });
            uzi.parts.Add(new WeaponPartData {
                partId = "Magazine", displayName = "Cargador Recto (25 Cartuchos 9mm)", meshName = "Magazin",
                functionDescription = "Se aloja en el interior de la empuñadura facilitando la recarga a ciegas.",
                tableLocalPos = new Vector3(0.40f, 1.075f, -0.07f), tableLocalRot = new Vector3(0f, 90f, 90f),
                socketLocalPos = new Vector3(0f, 0.01f, 0.02f), socketLocalRot = Vector3.zero
            });
            uzi.parts.Add(new WeaponPartData {
                partId = "Stock", displayName = "Culata Plegable de Acero (Folding Stock)", meshName = "Stock",
                functionDescription = "Culata extensible que proporciona apoyo al hombro para disparos estables.",
                tableLocalPos = new Vector3(-0.35f, 1.075f, 0.13f), tableLocalRot = new Vector3(0f, 90f, 0f),
                socketLocalPos = new Vector3(0f, 0.02f, 0.15f), socketLocalRot = Vector3.zero
            });
            uzi.parts.Add(new WeaponPartData {
                partId = "Trigger", displayName = "Gatillo y Selector de Tiro", meshName = "Trigger",
                functionDescription = "Selector de modo de tiro: Seguro, Semiautomático y Ráfaga automática.",
                tableLocalPos = new Vector3(0.10f, 1.075f, -0.16f), tableLocalRot = new Vector3(0f, 90f, 0f),
                socketLocalPos = new Vector3(0f, 0.01f, -0.02f), socketLocalRot = Vector3.zero
            });
            availableWeapons.Add(uzi);
        }
        #endregion

        #region Procedural Audio Generators
        private AudioClip CreateMechanicalSnapClip()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.12f);
            float[] data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 65f);
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
            int length = (int)(sampleRate * 0.45f);
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
            int length = (int)(sampleRate * 0.85f);
            float[] data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 5f);
                float c1 = Mathf.Sin(2f * Mathf.PI * 523.25f * t);
                float c2 = Mathf.Sin(2f * Mathf.PI * 659.25f * t) * 0.8f;
                float c3 = Mathf.Sin(2f * Mathf.PI * 783.99f * t) * 0.9f;
                float c4 = Mathf.Sin(2f * Mathf.PI * 1046.50f * t) * 0.7f;
                data[i] = (c1 + c2 + c3 + c4) * 0.25f * envelope;
            }
            var clip = AudioClip.Create("CompletionChime", length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
        #endregion
    }
}
