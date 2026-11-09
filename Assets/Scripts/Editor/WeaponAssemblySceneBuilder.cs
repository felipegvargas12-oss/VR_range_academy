using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace VRRangeAcademy.Editor
{
    public class WeaponAssemblySceneBuilder : EditorWindow
    {
        private const string SCENE_PATH = "Assets/Scenes/WeaponAssembly.unity";

        [MenuItem("VR Academy/Construir Escena 2 - Banco de Anatomía (Despiece)", false, 20)]
        public static void BuildAssemblySceneMenu()
        {
            BuildAssemblyScene();
        }

        public static void BuildAssemblyScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Error", "Por favor detén el Play Mode antes de construir la escena.", "OK");
                return;
            }

            // 1. Crear o abrir la escena
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "WeaponAssembly";

            // 2. Materiales de la escena
            var matFloor = CreateMaterial("Mat_Assembly_Floor", new Color(0.18f, 0.19f, 0.21f), 0.2f, 0.1f);
            var matWall = CreateMaterial("Mat_Assembly_Wall", new Color(0.24f, 0.25f, 0.26f), 0.1f, 0.05f);
            var matMat = CreateMaterial("Mat_ArmorerMat", new Color(0.14f, 0.16f, 0.15f), 0.1f, 0.0f);
            var matHolo = CreateHologramMaterial("Mat_CyanHologram", new Color(0f, 0.85f, 1f, 0.42f));
            var matHoloSolid = CreateHologramMaterial("Mat_CyanHoloSolid", new Color(0.1f, 0.95f, 1f, 0.85f));
            var matController = CreateMaterial("Mat_QuestController", new Color(0.92f, 0.92f, 0.94f), 0.4f, 0.1f);

            // 3. Iluminación
            BuildLighting();

            // 4. Entorno de Armería / Taller
            var envRoot = new GameObject("--- ENVIRONMENT (TALLER DESPIECE) ---");
            BuildWorkshopRoom(envRoot, matFloor, matWall);
            BuildBackgroundProps(envRoot);

            // 5. Banco de Trabajo Central y Tapete de Armero
            var workbenchObj = BuildCentralWorkbench(envRoot, matMat);

            // 6. Modelo Holográfico Flotante de Referencia
            var holoGunObj = BuildHolographicReferenceGun(workbenchObj, matHolo);

            // 7. Piezas Desarmadas Distribuidas en el Tapete
            var pieces = BuildDisassembledPiecesOnMat(workbenchObj, holoGunObj);

            // 8. Flecha Guía Dinámica
            var arrowObj = BuildGuidanceArrow(workbenchObj);

            // 9. HUD Holográfico en el Espacio
            var hudCanvas = BuildHolographicHUD(workbenchObj);

            // 10. Jugador Primera Persona con Mandos Oculus Touch
            var playerRig = BuildPlayerWithControllers(matController);

            // 11. Manager de Ensamble y Mecánicas
            var managerGo = new GameObject("WeaponAssemblyManager");
            var manager = managerGo.AddComponent<Assembly.WeaponAssemblyManager>();

            // Configurar SerializedObject del manager para vincular referencias
            ConfigureManager(manager, pieces, holoGunObj, arrowObj, hudCanvas);

            // 12. Guardar la escena y agregar a Build Settings
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AddSceneToBuildSettings(SCENE_PATH);

            Debug.Log($"<color=#00FF99>Escena 2 (Banco de Anatomía / Mesa de Despiece) construida con éxito en: {SCENE_PATH}</color>");
        }

        private static void BuildLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.20f, 0.22f, 0.26f);

            // Luz direccional ambiental tenue
            var dirLightGo = new GameObject("Directional Light");
            var dirLight = dirLightGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(0.95f, 0.95f, 1.0f);
            dirLight.intensity = 0.4f;
            dirLightGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            // Foco de iluminación directa sobre el banco de trabajo (Cono de luz cálida del mockup)
            var spotLightGo = new GameObject("Workbench_SpotLight");
            spotLightGo.transform.position = new Vector3(0f, 3.2f, 1.45f);
            spotLightGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var spotLight = spotLightGo.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.color = new Color(1.0f, 0.88f, 0.72f); // Luz incandescente cálida
            spotLight.intensity = 4.2f;
            spotLight.range = 5.5f;
            spotLight.spotAngle = 65f;
            spotLight.innerSpotAngle = 35f;

            // Luz suave cian emitida por el holograma
            var holoLightGo = new GameObject("Hologram_CyanLight");
            holoLightGo.transform.position = new Vector3(0f, 1.35f, 1.45f);
            var holoLight = holoLightGo.AddComponent<Light>();
            holoLight.type = LightType.Point;
            holoLight.color = new Color(0f, 0.82f, 1f);
            holoLight.intensity = 1.6f;
            holoLight.range = 2.4f;

            // Lámparas colgantes secundarias en las esquinas
            CreateCeilingLamp(new Vector3(-2.8f, 3.2f, 1.45f), new Color(1f, 0.9f, 0.75f), 2.2f);
            CreateCeilingLamp(new Vector3(2.8f, 3.2f, 1.45f), new Color(1f, 0.9f, 0.75f), 2.2f);
            CreateCeilingLamp(new Vector3(0f, 3.2f, -1.8f), new Color(0.85f, 0.9f, 1f), 1.8f);
        }

        private static void CreateCeilingLamp(Vector3 pos, Color col, float intensity)
        {
            var lampGo = new GameObject("Ceiling_Lamp");
            lampGo.transform.position = pos;

            var fixture = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fixture.transform.SetParent(lampGo.transform);
            fixture.transform.localPosition = Vector3.zero;
            fixture.transform.localScale = new Vector3(0.24f, 0.08f, 0.24f);
            Object.DestroyImmediate(fixture.GetComponent<Collider>());

            var l = lampGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = col;
            l.intensity = intensity;
            l.range = 4.5f;
        }

        private static void BuildWorkshopRoom(GameObject root, Material matFloor, Material matWall)
        {
            // Suelo
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform);
            floor.transform.position = new Vector3(0f, -0.05f, 1.5f);
            floor.transform.localScale = new Vector3(12f, 0.1f, 12f);
            floor.GetComponent<MeshRenderer>().material = matFloor;

            // Pared Frontal (Detrás de la mesa)
            var wallFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallFront.name = "Front_Wall";
            wallFront.transform.SetParent(root.transform);
            wallFront.transform.position = new Vector3(0f, 2.25f, 5.0f);
            wallFront.transform.localScale = new Vector3(12f, 4.5f, 0.2f);
            wallFront.GetComponent<MeshRenderer>().material = matWall;

            // Pared Izquierda
            var wallLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallLeft.name = "Left_Wall";
            wallLeft.transform.SetParent(root.transform);
            wallLeft.transform.position = new Vector3(-6.0f, 2.25f, 1.5f);
            wallLeft.transform.localScale = new Vector3(0.2f, 4.5f, 12f);
            wallLeft.GetComponent<MeshRenderer>().material = matWall;

            // Pared Derecha
            var wallRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallRight.name = "Right_Wall";
            wallRight.transform.SetParent(root.transform);
            wallRight.transform.position = new Vector3(6.0f, 2.25f, 1.5f);
            wallRight.transform.localScale = new Vector3(0.2f, 4.5f, 12f);
            wallRight.GetComponent<MeshRenderer>().material = matWall;

            // Pared Trasera
            var wallRear = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallRear.name = "Rear_Wall";
            wallRear.transform.SetParent(root.transform);
            wallRear.transform.position = new Vector3(0f, 2.25f, -4.5f);
            wallRear.transform.localScale = new Vector3(12f, 4.5f, 0.2f);
            wallRear.GetComponent<MeshRenderer>().material = matWall;

            // Techo con vigas
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(root.transform);
            ceiling.transform.position = new Vector3(0f, 4.5f, 1.5f);
            ceiling.transform.localScale = new Vector3(12f, 0.15f, 12f);
            ceiling.GetComponent<MeshRenderer>().material = matWall;
        }

        private static void BuildBackgroundProps(GameObject root)
        {
            var propsFbx = "Assets/Defensive_props/models/Defensive_props.fbx";
            var matProps = AssetDatabase.LoadAssetAtPath<Material>("Assets/Defensive_props/Materials/Mat_Defensive_props_URP.mat");
            if (matProps == null)
            {
                matProps = CreateMaterial("Mat_Defensive_props_URP", new Color(0.4f, 0.42f, 0.4f), 0.4f, 0.2f);
            }

            // Bancos de trabajo traseros
            InstantiateMeshProp(propsFbx, "workbench", new Vector3(-2.8f, 0f, 4.4f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1.1f, 1.1f, 1.1f), matProps, root.transform);
            InstantiateMeshProp(propsFbx, "workbench", new Vector3(2.8f, 0f, 4.4f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1.1f, 1.1f, 1.1f), matProps, root.transform);

            // Cajas y consumibles en pared izquierda
            InstantiateMeshProp(propsFbx, "wooden_table", new Vector3(-5.2f, 0f, 1.8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, matProps, root.transform);
            InstantiateMeshProp(propsFbx, "box1", new Vector3(-5.2f, 0.85f, 1.5f), Quaternion.identity, Vector3.one * 0.9f, matProps, root.transform);
            InstantiateMeshProp(propsFbx, "box2", new Vector3(-5.2f, 0.85f, 2.1f), Quaternion.identity, Vector3.one * 0.85f, matProps, root.transform);
            InstantiateMeshProp(propsFbx, "canister", new Vector3(-4.8f, 0f, 3.2f), Quaternion.identity, Vector3.one * 1.1f, matProps, root.transform);

            // Consolas y cajas en pared derecha
            InstantiateMeshProp(propsFbx, "electronics1", new Vector3(5.2f, 0f, 2.0f), Quaternion.Euler(0f, -90f, 0f), Vector3.one, matProps, root.transform);
            InstantiateMeshProp(propsFbx, "ammo_box", new Vector3(5.1f, 0f, 0.8f), Quaternion.identity, Vector3.one * 1.1f, matProps, root.transform);
            InstantiateMeshProp(propsFbx, "sandbag barricade", new Vector3(4.8f, 0f, 4.0f), Quaternion.Euler(0f, -45f, 0f), Vector3.one * 0.85f, matProps, root.transform);
        }

        private static GameObject BuildCentralWorkbench(GameObject root, Material matMat)
        {
            var benchRoot = new GameObject("Central_Assembly_Workbench");
            benchRoot.transform.SetParent(root.transform);
            benchRoot.transform.position = new Vector3(0f, 0f, 1.45f);

            var propsFbx = "Assets/Defensive_props/models/Defensive_props.fbx";
            var matProps = AssetDatabase.LoadAssetAtPath<Material>("Assets/Defensive_props/Materials/Mat_Defensive_props_URP.mat");

            // Modelo físico del banco
            var tableObj = InstantiateMeshProp(propsFbx, "workbench", Vector3.zero, Quaternion.identity, new Vector3(1.25f, 1.05f, 1.05f), matProps, benchRoot.transform);

            // Tapete de Armero (Mat de Despiece en neopreno verde/gris sobre la mesa)
            var matObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            matObj.name = "Armorer_Disassembly_Mat";
            matObj.transform.SetParent(benchRoot.transform);
            matObj.transform.localPosition = new Vector3(0f, 0.88f, 0.05f);
            matObj.transform.localScale = new Vector3(1.05f, 0.012f, 0.58f);
            matObj.GetComponent<MeshRenderer>().material = matMat;

            // Borde / marco estético del tapete
            var matBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            matBorder.name = "Mat_Cyan_Accent_Border";
            matBorder.transform.SetParent(matObj.transform);
            matBorder.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            matBorder.transform.localScale = new Vector3(0.98f, 0.2f, 0.96f);
            Object.DestroyImmediate(matBorder.GetComponent<Collider>());
            var borderMat = CreateMaterial("Mat_MatBorder", new Color(0.12f, 0.28f, 0.35f), 0.1f, 0.0f);
            matBorder.GetComponent<MeshRenderer>().material = borderMat;

            // Props en la mesa (Caja de herramientas, munición de prueba, destornillador)
            InstantiateMeshProp(propsFbx, "ammo_box", new Vector3(0.58f, 0.88f, 0.15f), Quaternion.Euler(0f, -15f, 0f), Vector3.one * 0.7f, matProps, benchRoot.transform);
            InstantiateMeshProp(propsFbx, "Shell_casing", new Vector3(0.48f, 0.88f, -0.15f), Quaternion.Euler(0f, 40f, 0f), Vector3.one * 1.5f, matProps, benchRoot.transform);
            InstantiateMeshProp(propsFbx, "Shell_casing", new Vector3(0.52f, 0.88f, -0.12f), Quaternion.Euler(0f, 10f, 0f), Vector3.one * 1.5f, matProps, benchRoot.transform);

            // Proyector holográfico (Placa emisora con emisor de luz en la mesa)
            var holoEmitter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            holoEmitter.name = "Hologram_Projector_Base";
            holoEmitter.transform.SetParent(benchRoot.transform);
            holoEmitter.transform.localPosition = new Vector3(0f, 0.885f, 0.08f);
            holoEmitter.transform.localScale = new Vector3(0.38f, 0.005f, 0.38f);
            Object.DestroyImmediate(holoEmitter.GetComponent<Collider>());
            var emitterMat = CreateMaterial("Mat_HoloProjector", new Color(0.08f, 0.22f, 0.32f), 0.8f, 0.6f);
            emitterMat.EnableKeyword("_EMISSION");
            emitterMat.SetColor("_EmissionColor", new Color(0f, 0.6f, 0.9f) * 0.6f);
            holoEmitter.GetComponent<MeshRenderer>().material = emitterMat;

            return benchRoot;
        }

        private static GameObject BuildHolographicReferenceGun(GameObject parent, Material matHolo)
        {
            var holoRoot = new GameObject("Holographic_Reference_Gun");
            holoRoot.transform.SetParent(parent.transform);
            // Flotando sobre el proyector a la altura visual de los ojos
            holoRoot.transform.localPosition = new Vector3(0f, 1.34f, 0.08f);
            // Rotación lateral tipo exhibición como en el mockup
            holoRoot.transform.localRotation = Quaternion.Euler(0f, 85f, -8f);
            holoRoot.transform.localScale = Vector3.one * 1.25f;

            var m1911Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Weapons VOL.1/Models/M1911.fbx");
            if (m1911Prefab != null)
            {
                var ghostInstance = Object.Instantiate(m1911Prefab, holoRoot.transform);
                ghostInstance.name = "M1911_Hologram_Ghost";
                ghostInstance.transform.localPosition = Vector3.zero;
                ghostInstance.transform.localRotation = Quaternion.identity;
                ghostInstance.transform.localScale = Vector3.one;

                // Aplicar material holográfico a todas las subpartes y eliminar colliders
                foreach (var mr in ghostInstance.GetComponentsInChildren<MeshRenderer>())
                {
                    mr.material = matHolo;
                }
                foreach (var col in ghostInstance.GetComponentsInChildren<Collider>())
                {
                    Object.DestroyImmediate(col);
                }
            }

            // Crear Sockets de Encastre con transforms exactos
            CreateSocket(holoRoot.transform, "Socket_Frame", Vector3.zero, Quaternion.identity);
            CreateSocket(holoRoot.transform, "Socket_Hammer", new Vector3(0f, 0.04f, 0.05f), Quaternion.identity);
            CreateSocket(holoRoot.transform, "Socket_Trigger", new Vector3(0f, -0.02f, -0.01f), Quaternion.identity);
            CreateSocket(holoRoot.transform, "Socket_Slide", new Vector3(0f, 0.01f, -0.14f), Quaternion.identity);
            CreateSocket(holoRoot.transform, "Socket_Magazine", new Vector3(0f, -0.05f, 0.05f), Quaternion.Euler(339.32f, 0f, 0f));

            return holoRoot;
        }

        private static Transform CreateSocket(Transform parent, string name, Vector3 localPos, Quaternion localRot)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;

            // Indicador sutil de punto de anclaje
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Socket_Anchor_Glow";
            sphere.transform.SetParent(go.transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * 0.022f;
            Object.DestroyImmediate(sphere.GetComponent<Collider>());
            var glowMat = CreateHologramMaterial("Mat_SocketGlow", new Color(0f, 1f, 0.8f, 0.35f));
            sphere.GetComponent<MeshRenderer>().material = glowMat;

            return go.transform;
        }

        private static List<Assembly.AssemblyPiece> BuildDisassembledPiecesOnMat(GameObject workbench, GameObject holoGun)
        {
            var piecesList = new List<Assembly.AssemblyPiece>();
            var m1911Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Weapons VOL.1/Models/M1911.fbx");
            var matGun = AssetDatabase.LoadAssetAtPath<Material>("Assets/Low Poly Weapons VOL.1/Materials/Mat_Guns_URP.mat");
            if (matGun == null)
            {
                matGun = CreateMaterial("Mat_Guns_URP", new Color(0.2f, 0.22f, 0.23f), 0.7f, 0.3f);
            }

            var piecesParent = new GameObject("--- DISASSEMBLED PIECES (ON TABLE) ---");
            piecesParent.transform.SetParent(workbench.transform);
            piecesParent.transform.localPosition = Vector3.zero;

            // Datos de cada pieza según el mockup de despiece
            var configs = new[] {
                new { Id = "Frame", Name = "Armazón (Frame)", Mesh = "M1911", Socket = "Socket_Frame",
                      Pos = new Vector3(0.08f, 0.91f, 0.02f), Rot = Quaternion.Euler(0f, 90f, 90f),
                      Desc = "Aloja el cargador, gatillo y sirve de guía para la corredera." },

                new { Id = "Slide", Name = "Corredera (Slide)", Mesh = "Slide", Socket = "Socket_Slide",
                      Pos = new Vector3(-0.25f, 0.91f, 0.02f), Rot = Quaternion.Euler(0f, 90f, 90f),
                      Desc = "Recorre hacia atrás al disparar, extrae vainas y alimenta nuevos cartuchos." },

                new { Id = "Trigger", Name = "Gatillo (Trigger)", Mesh = "Trigger", Socket = "Socket_Trigger",
                      Pos = new Vector3(-0.06f, 0.90f, 0.18f), Rot = Quaternion.Euler(0f, 90f, 0f),
                      Desc = "Acciona el fiador que libera el martillo para golpear la aguja percutora." },

                new { Id = "Hammer", Name = "Martillo (Hammer)", Mesh = "Hammer", Socket = "Socket_Hammer",
                      Pos = new Vector3(0.12f, 0.90f, 0.18f), Rot = Quaternion.Euler(0f, 90f, 0f),
                      Desc = "Transmite la energía cinética acumulada del resorte al percutor." },

                new { Id = "Magazine", Name = "Cargador (Magazine)", Mesh = "Magazin", Socket = "Socket_Magazine",
                      Pos = new Vector3(0.28f, 0.90f, -0.06f), Rot = Quaternion.Euler(0f, 90f, 90f),
                      Desc = "Almacena los cartuchos y los eleva hacia la recámara mediante un resorte interno." }
            };

            foreach (var cfg in configs)
            {
                var pieceGo = new GameObject("Piece_" + cfg.Id);
                pieceGo.transform.SetParent(piecesParent.transform);
                pieceGo.transform.localPosition = cfg.Pos;
                pieceGo.transform.localRotation = cfg.Rot;
                pieceGo.transform.localScale = Vector3.one * 1.25f;

                // Mesh Filter y Renderer
                var mf = pieceGo.AddComponent<MeshFilter>();
                var mr = pieceGo.AddComponent<MeshRenderer>();
                mr.material = matGun;

                // Extraer la malla específica del FBX
                var mesh = FindSubmesh(m1911Prefab, cfg.Mesh);
                if (mesh != null) mf.sharedMesh = mesh;

                // BoxCollider o MeshCollider convexo
                var col = pieceGo.AddComponent<BoxCollider>();
                Vector3 sz = col.size;
                sz.x = Mathf.Max(sz.x, 0.06f);
                sz.y = Mathf.Max(sz.y, 0.06f);
                sz.z = Mathf.Max(sz.z, 0.06f);
                col.size = sz;

                // Rigidbody
                var rb = pieceGo.AddComponent<Rigidbody>();
                rb.mass = 0.5f;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

                // Script AssemblyPiece
                var pieceComp = pieceGo.AddComponent<Assembly.AssemblyPiece>();
                pieceComp.pieceId = cfg.Id;
                pieceComp.displayName = cfg.Name;
                pieceComp.functionDescription = cfg.Desc;
                pieceComp.snapThresholdDistance = 0.22f;

                // Asignar el socket correspondiente en el holograma
                var socketTrans = holoGun.transform.Find(cfg.Socket);
                pieceComp.targetSocket = socketTrans;

                pieceComp.SetInitialTransform(pieceGo.transform.position, pieceGo.transform.rotation);
                piecesList.Add(pieceComp);
            }

            return piecesList;
        }

        private static Mesh FindSubmesh(GameObject prefab, string meshName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(prefab));
            foreach (var a in assets)
            {
                if (a is Mesh m && m.name == meshName) return m;
            }
            return null;
        }

        private static Assembly.GuidanceArrow BuildGuidanceArrow(GameObject parent)
        {
            var arrowGo = new GameObject("Guidance_Curved_Arrow");
            arrowGo.transform.SetParent(parent.transform);
            arrowGo.transform.localPosition = Vector3.zero;

            var lr = arrowGo.AddComponent<LineRenderer>();
            var arrowComp = arrowGo.AddComponent<Assembly.GuidanceArrow>();

            return arrowComp;
        }

        private static GameObject BuildHolographicHUD(GameObject workbench)
        {
            var hudGo = new GameObject("Holographic_Assembly_HUD");
            hudGo.transform.SetParent(workbench.transform);
            hudGo.transform.localPosition = new Vector3(0f, 1.86f, 0.12f);
            hudGo.transform.localRotation = Quaternion.Euler(15f, 0f, 0f); // Tilted towards player

            var canvas = hudGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = hudGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(900f, 420f);
            rt.localScale = Vector3.one * 0.0012f;

            hudGo.AddComponent<CanvasScaler>();
            hudGo.AddComponent<GraphicRaycaster>();

            // Panel de fondo translúcido cian/tecnológico
            var panelBg = new GameObject("Panel_Background");
            panelBg.transform.SetParent(hudGo.transform, false);
            var panelImage = panelBg.AddComponent<Image>();
            panelImage.color = new Color(0.04f, 0.09f, 0.14f, 0.88f);
            var panelRt = panelBg.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.sizeDelta = Vector2.zero;

            // Borde cian
            var outline = panelBg.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0.85f, 1f, 0.6f);
            outline.effectDistance = new Vector2(2f, 2f);

            // Título Principal
            CreateText(hudGo.transform, "Title", "BANCO DE ANATOMIA TECNICA", 26, new Vector2(0f, 155f), new Vector2(850f, 45f), FontStyles.Bold, new Color(0f, 0.9f, 1f));

            // Contador de Paso
            CreateText(hudGo.transform, "Txt_StepCounter", "PASO 1 DE 5", 22, new Vector2(0f, 110f), new Vector2(850f, 35f), FontStyles.Bold, new Color(1f, 0.85f, 0.3f));

            // Título de la Pieza
            CreateText(hudGo.transform, "Txt_StepTitle", "Acoplar: Armazón (Frame)", 24, new Vector2(0f, 65f), new Vector2(850f, 40f), FontStyles.Bold, Color.white);

            // Descripción Técnica / Función
            CreateText(hudGo.transform, "Txt_StepDescription", "Aloja el cargador, el mecanismo de disparo y sirve de guía longitudinal para la corredera.", 18, new Vector2(0f, 5f), new Vector2(820f, 65f), FontStyles.Normal, new Color(0.85f, 0.9f, 0.95f));

            // Pista de Controles
            CreateText(hudGo.transform, "Txt_ControlsHint", "VR: Agarra con Grip/Trigger y acopla al holograma | PC: Clic izquierdo arrastrar", 16, new Vector2(0f, -60f), new Vector2(850f, 35f), FontStyles.Italic, new Color(0.4f, 0.85f, 1f));

            // Panel de Finalización (Oculto al inicio)
            var completionPanel = new GameObject("Panel_Completion");
            completionPanel.transform.SetParent(hudGo.transform, false);
            var compImg = completionPanel.AddComponent<Image>();
            compImg.color = new Color(0.02f, 0.16f, 0.12f, 0.95f);
            var compRt = completionPanel.GetComponent<RectTransform>();
            compRt.anchorMin = Vector2.zero;
            compRt.anchorMax = Vector2.one;
            compRt.sizeDelta = Vector2.zero;

            var compOutline = completionPanel.AddComponent<Outline>();
            compOutline.effectColor = new Color(0.1f, 1f, 0.5f, 0.8f);
            compOutline.effectDistance = new Vector2(3f, 3f);

            CreateText(completionPanel.transform, "Txt_CompletionTitle", "[!] APRENDIZAJE MECANICO VALIDADO\nAcceso al Poligono de Tiro Desbloqueado", 26, new Vector2(0f, 75f), new Vector2(840f, 85f), FontStyles.Bold, new Color(0.2f, 1f, 0.5f));

            CreateButton(completionPanel.transform, "Btn_ProceedToShootingRange", "Acceder al Poligono de Tiro >", new Vector2(0f, -25f), new Vector2(480f, 55f), new Color(0.1f, 0.65f, 0.35f));
            CreateButton(completionPanel.transform, "Btn_ResetAssembly", "Reiniciar Practica de Ensamble", new Vector2(0f, -95f), new Vector2(380f, 45f), new Color(0.2f, 0.3f, 0.4f));

            completionPanel.SetActive(false);

            return hudGo;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, Vector2 pos, Vector2 size, FontStyles style, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return tmp;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color btnColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = btnColor;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = btnColor * 1.25f;
            colors.pressedColor = btnColor * 0.8f;
            btn.colors = colors;

            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            return btn;
        }

        private static GameObject BuildPlayerWithControllers(Material matController)
        {
            var rigGo = new GameObject("XR_Assembly_PlayerRig");
            rigGo.transform.position = new Vector3(0f, 0f, 0f);

            var camGo = new GameObject("Main Camera");
            camGo.transform.SetParent(rigGo.transform);
            camGo.transform.localPosition = new Vector3(0f, 1.60f, 0.35f);
            camGo.transform.localRotation = Quaternion.Euler(18f, 0f, 0f); // Looking towards table

            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 100f;
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();

            // Mandos Oculus Touch visuales frente a la cámara (como en el mockup)
            var leftHand = CreateControllerVisual(camGo.transform, "LeftHand_OculusTouch", new Vector3(-0.24f, -0.22f, 0.42f), Quaternion.Euler(22f, -15f, -12f), matController);
            var rightHand = CreateControllerVisual(camGo.transform, "RightHand_OculusTouch", new Vector3(0.24f, -0.22f, 0.42f), Quaternion.Euler(22f, 15f, 12f), matController);

            // Controlador híbrido (Desktop + VR)
            var controllerComp = rigGo.AddComponent<Assembly.AssemblyPlayerController>();

            // Event System si no existe
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            return rigGo;
        }

        private static GameObject CreateControllerVisual(Transform parent, string name, Vector3 localPos, Quaternion localRot, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;

            // Cuerpo / Mango del mando Oculus Touch
            var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "Touch_Handle";
            handle.transform.SetParent(go.transform, false);
            handle.transform.localPosition = new Vector3(0f, -0.06f, 0f);
            handle.transform.localScale = new Vector3(0.045f, 0.07f, 0.045f);
            handle.GetComponent<MeshRenderer>().material = mat;
            Object.DestroyImmediate(handle.GetComponent<Collider>());

            // Aro de seguimiento característico Oculus Quest / Touch
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Touch_TrackingRing";
            ring.transform.SetParent(go.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.02f, 0.03f);
            ring.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            ring.transform.localScale = new Vector3(0.11f, 0.012f, 0.11f);
            ring.GetComponent<MeshRenderer>().material = mat;
            Object.DestroyImmediate(ring.GetComponent<Collider>());

            // Botón Grip lateral
            var grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Touch_GripTrigger";
            grip.transform.SetParent(go.transform, false);
            grip.transform.localPosition = new Vector3(0f, -0.04f, -0.025f);
            grip.transform.localScale = new Vector3(0.02f, 0.035f, 0.02f);
            var matBlack = CreateMaterial("Mat_TouchButtons", new Color(0.1f, 0.1f, 0.12f), 0.5f, 0.1f);
            grip.GetComponent<MeshRenderer>().material = matBlack;
            Object.DestroyImmediate(grip.GetComponent<Collider>());

            return go;
        }

        private static void ConfigureManager(Assembly.WeaponAssemblyManager manager, List<Assembly.AssemblyPiece> pieces, GameObject holoGun, Assembly.GuidanceArrow arrow, GameObject hudCanvas)
        {
            var so = new SerializedObject(manager);

            // Lista de piezas
            var piecesProp = so.FindProperty("assemblyPieces");
            piecesProp.ClearArray();
            for (int i = 0; i < pieces.Count; i++)
            {
                piecesProp.InsertArrayElementAtIndex(i);
                piecesProp.GetArrayElementAtIndex(i).objectReferenceValue = pieces[i];
            }

            // Referencias
            so.FindProperty("guidanceArrow").objectReferenceValue = arrow;
            so.FindProperty("holographicGhostGun").objectReferenceValue = holoGun;

            // UI Elements
            so.FindProperty("txtStepCounter").objectReferenceValue = hudCanvas.transform.Find("Txt_StepCounter").GetComponent<TextMeshProUGUI>();
            so.FindProperty("txtStepTitle").objectReferenceValue = hudCanvas.transform.Find("Txt_StepTitle").GetComponent<TextMeshProUGUI>();
            so.FindProperty("txtStepDescription").objectReferenceValue = hudCanvas.transform.Find("Txt_StepDescription").GetComponent<TextMeshProUGUI>();
            so.FindProperty("txtControlsHint").objectReferenceValue = hudCanvas.transform.Find("Txt_ControlsHint").GetComponent<TextMeshProUGUI>();

            var compPanel = hudCanvas.transform.Find("Panel_Completion").gameObject;
            so.FindProperty("panelCompletion").objectReferenceValue = compPanel;
            so.FindProperty("txtCompletionTitle").objectReferenceValue = compPanel.transform.Find("Txt_CompletionTitle").GetComponent<TextMeshProUGUI>();
            so.FindProperty("btnProceedToShootingRange").objectReferenceValue = compPanel.transform.Find("Btn_ProceedToShootingRange").GetComponent<Button>();
            so.FindProperty("btnResetAssembly").objectReferenceValue = compPanel.transform.Find("Btn_ResetAssembly").GetComponent<Button>();

            so.ApplyModifiedProperties();
        }

        private static GameObject InstantiateMeshProp(string fbxPath, string meshName, Vector3 pos, Quaternion rot, Vector3 scale, Material mat, Transform parent)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            Mesh foundMesh = null;
            foreach (var a in assets)
            {
                if (a is Mesh m && m.name == meshName)
                {
                    foundMesh = m;
                    break;
                }
            }

            var go = new GameObject("Prop_" + meshName);
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;

            if (foundMesh != null)
            {
                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = foundMesh;
                var mr = go.AddComponent<MeshRenderer>();
                if (mat != null) mr.material = mat;
            }

            return go;
        }

        private static Material CreateMaterial(string name, Color color, float smoothness, float metallic)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            mat.name = name;
            mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            var path = $"Assets/Materials/{name}.mat";
            EnsureDirectory("Assets/Materials");
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static Material CreateHologramMaterial(string name, Color cyanGlow)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.name = name;
            mat.color = cyanGlow;

            // Configurar blend mode transparente
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0); // Alpha
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            var path = $"Assets/Materials/{name}.mat";
            EnsureDirectory("Assets/Materials");
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void EnsureDirectory(string dir)
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                string[] parts = dir.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current = next;
                }
            }
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes)
            {
                if (s.path == scenePath) return; // Ya existe
            }
            scenes.Insert(1, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
