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

            // 2. Materiales de la escena (URP Lit asegurando _BaseColor)
            var matFloor = CreateMaterial("Mat_Assembly_Floor", new Color(0.18f, 0.19f, 0.21f), 0.2f, 0.1f);
            var matWall = CreateMaterial("Mat_Assembly_Wall", new Color(0.25f, 0.26f, 0.28f), 0.1f, 0.05f);
            var matMat = CreateMaterial("Mat_ArmorerMat", new Color(0.13f, 0.17f, 0.15f), 0.1f, 0.0f);
            var matHolo = CreateHologramMaterial("Mat_CyanHologram", new Color(0f, 0.85f, 1f, 0.45f));
            var matSocketGlow = CreateHologramMaterial("Mat_SocketGlow", new Color(0f, 1f, 0.8f, 0.4f));
            var matGuidance = CreateHologramMaterial("Mat_GuidanceLine", new Color(0f, 0.85f, 1f, 0.9f));
            var matController = CreateMaterial("Mat_QuestController", new Color(0.92f, 0.92f, 0.94f), 0.4f, 0.1f);
            // Materiales URP propios para props y armas (los externos usan Standard/HDRP → magenta en URP)
            var matProps = CreateMaterial("Mat_Defensive_props_URP", new Color(0.28f, 0.26f, 0.22f), 0.3f, 0.5f);
            var matGuns  = CreateMaterial("Mat_Guns_URP", new Color(0.14f, 0.15f, 0.17f), 0.6f, 0.85f);

            // 3. Iluminación
            BuildLighting();

            // 4. Entorno de Armería / Taller decorado
            var envRoot = new GameObject("--- ENVIRONMENT (TALLER DESPIECE) ---");
            BuildWorkshopRoom(envRoot, matFloor, matWall);
            BuildEnrichedDecorations(envRoot, matProps, matGuns);

            // 5. Banco de Trabajo Central y Tapete de Armero
            var benchRoot = BuildCentralWorkbench(envRoot, matMat, matProps);

            // 6. Contenedores de Piezas y Holograma
            var piecesContainer = new GameObject("--- DISASSEMBLED PIECES (ON TABLE) ---");
            piecesContainer.transform.SetParent(benchRoot.transform, false);

            var holoContainer = new GameObject("Holographic_Reference_Gun");
            holoContainer.transform.SetParent(benchRoot.transform, false);
            holoContainer.transform.localPosition = new Vector3(0f, 1.48f, 0.0f);
            holoContainer.transform.localRotation = Quaternion.Euler(0f, 85f, -8f);

            // 7. Flecha Guía Dinámica
            var arrowObj = BuildGuidanceArrow(benchRoot, matGuidance);

            // 7.5. Polígono de Tiro Integrado (Galería de Disparo y Dianas Reactivas)
            Transform rangeDoor;
            Light rangeBeaconLight;
            TextMeshProUGUI txtScoreboard;
            TextMeshProUGUI txtHitToast;
            BuildIntegratedShootingGallery(envRoot, matFloor, matWall, matProps, out rangeDoor, out rangeBeaconLight, out txtScoreboard, out txtHitToast);

            // Manager del Polígono de Tiro
            var rangeMgrGo = new GameObject("ShootingRangeManager");
            var rangeMgr = rangeMgrGo.AddComponent<VRRangeAcademy.Shooting.ShootingRangeManager>();
            ConfigureShootingRangeManager(rangeMgr, rangeDoor, rangeBeaconLight, txtScoreboard, txtHitToast);

            // 8. HUD Holográfico con Selector de Armas
            var hudCanvas = BuildHolographicHUD(benchRoot);

            // 9. Jugador Primera Persona con Mandos Oculus Touch y WASD
            var playerRig = BuildPlayerWithControllers(matController);

            // 10. Manager de Ensamble y Mecánicas Dinámicas
            var managerGo = new GameObject("WeaponAssemblyManager");
            var manager = managerGo.AddComponent<Assembly.WeaponAssemblyManager>();

            // Configurar campos serializados del manager
            ConfigureManager(manager, piecesContainer.transform, holoContainer.transform, arrowObj, hudCanvas, matGuns, matHolo, matSocketGlow);

            // 11. Guardar la escena y agregar a Build Settings
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AddSceneToBuildSettings(SCENE_PATH);

            Debug.Log($"<color=#00FF99>Escena 2 (Banco de Anatomía Técnica) construida con éxito en: {SCENE_PATH}</color>");
        }

        private static void BuildLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.20f, 0.22f, 0.26f);

            // Luz direccional ambiental tenue
            var dirLightGo = new GameObject("Directional Light");
            var dirLight = dirLightGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(0.92f, 0.94f, 1.0f);
            dirLight.intensity = 0.38f;
            dirLightGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            // Foco de iluminación directa sobre el banco de trabajo (Cono de luz cálida del mockup)
            var spotLightGo = new GameObject("Workbench_SpotLight");
            spotLightGo.transform.position = new Vector3(0f, 3.4f, 1.42f);
            spotLightGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var spotLight = spotLightGo.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.color = new Color(1.0f, 0.88f, 0.70f); // Luz incandescente cálida
            spotLight.intensity = 4.8f;
            spotLight.range = 6.0f;
            spotLight.spotAngle = 72f;
            spotLight.innerSpotAngle = 38f;

            // Luz suave cian emitida por el holograma
            var holoLightGo = new GameObject("Hologram_CyanLight");
            holoLightGo.transform.position = new Vector3(0f, 1.48f, 1.42f);
            var holoLight = holoLightGo.AddComponent<Light>();
            holoLight.type = LightType.Point;
            holoLight.color = new Color(0f, 0.85f, 1f);
            holoLight.intensity = 1.8f;
            holoLight.range = 2.6f;

            // Lámparas colgantes secundarias en las esquinas
            CreateCeilingLamp(new Vector3(-3.2f, 3.2f, 1.42f), new Color(1f, 0.9f, 0.75f), 2.4f);
            CreateCeilingLamp(new Vector3(3.2f, 3.2f, 1.42f), new Color(1f, 0.9f, 0.75f), 2.4f);
            CreateCeilingLamp(new Vector3(0f, 3.2f, -2.0f), new Color(0.85f, 0.9f, 1f), 1.8f);
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
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform);
            floor.transform.position = new Vector3(-2.7f, -0.05f, 0.0f);
            floor.transform.localScale = new Vector3(8.6f, 0.1f, 11.0f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = matFloor;

            var wallFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallFront.name = "Front_Wall";
            wallFront.transform.SetParent(root.transform);
            wallFront.transform.position = new Vector3(-2.7f, 2.25f, 5.5f);
            wallFront.transform.localScale = new Vector3(8.6f, 4.5f, 0.2f);
            wallFront.GetComponent<MeshRenderer>().sharedMaterial = matWall;

            var wallLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallLeft.name = "Left_Wall";
            wallLeft.transform.SetParent(root.transform);
            wallLeft.transform.position = new Vector3(-7.0f, 2.25f, 0.0f);
            wallLeft.transform.localScale = new Vector3(0.2f, 4.5f, 11.0f);
            wallLeft.GetComponent<MeshRenderer>().sharedMaterial = matWall;

            var wallRear = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallRear.name = "Rear_Wall";
            wallRear.transform.SetParent(root.transform);
            wallRear.transform.position = new Vector3(-2.7f, 2.25f, -5.5f);
            wallRear.transform.localScale = new Vector3(8.6f, 4.5f, 0.2f);
            wallRear.GetComponent<MeshRenderer>().sharedMaterial = matWall;

            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(root.transform);
            ceiling.transform.position = new Vector3(-2.7f, 4.5f, 0.0f);
            ceiling.transform.localScale = new Vector3(8.6f, 0.15f, 11.0f);
            ceiling.GetComponent<MeshRenderer>().sharedMaterial = matWall;
        }

        private static void BuildEnrichedDecorations(GameObject root, Material matProps, Material matGuns)
        {
            var propsFbx = "Assets/Defensive_props/models/Defensive_props.fbx";
            var weaponsRoot = "Assets/Low Poly Weapons VOL.1/Models/";

            // 1. Armeros en la pared frontal del taller
            InstantiateWeaponOrnament(weaponsRoot + "AK74.fbx", new Vector3(-4.5f, 2.2f, 5.38f), Quaternion.Euler(0f, 180f, 0f), 1.2f, matGuns, root.transform);
            InstantiateWeaponOrnament(weaponsRoot + "M4_8.fbx", new Vector3(-3.3f, 2.2f, 5.38f), Quaternion.Euler(0f, 180f, 0f), 1.2f, matGuns, root.transform);
            InstantiateWeaponOrnament(weaponsRoot + "Bennelli_M4.fbx", new Vector3(-2.1f, 2.2f, 5.38f), Quaternion.Euler(0f, 180f, 0f), 1.2f, matGuns, root.transform);
            InstantiateWeaponOrnament(weaponsRoot + "M107.fbx", new Vector3(-0.9f, 2.2f, 5.38f), Quaternion.Euler(0f, 180f, 0f), 1.1f, matGuns, root.transform);

            // Tablero perforado de armero
            var pegboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pegboard.name = "Wall_Gun_Pegboard";
            pegboard.transform.SetParent(root.transform);
            pegboard.transform.position = new Vector3(-2.7f, 2.2f, 5.42f);
            pegboard.transform.localScale = new Vector3(4.8f, 1.4f, 0.05f);
            var matWood = CreateMaterial("Mat_Pegboard_Wood", new Color(0.35f, 0.25f, 0.16f), 0.2f, 0f);
            pegboard.GetComponent<MeshRenderer>().sharedMaterial = matWood;

            // 2. Banco auxiliar frontal en el taller
            InstantiateMeshProp(propsFbx, "workbench", new Vector3(-5.2f, 0f, 4.8f), Quaternion.Euler(0f, 90f, 0f), new Vector3(1.2f, 1.05f, 1.05f), matProps, root.transform);

            // Props en banco izquierdo: Consola electrónica, caja de herramientas, canister y granadas
            InstantiateMeshProp(propsFbx, "electronics1", new Vector3(-5.2f, 1.05f, 4.8f), Quaternion.identity, Vector3.one * 0.9f, matProps, root.transform);
            InstantiateMeshProp(propsFbx, "ammo_box", new Vector3(-5.8f, 1.05f, 4.8f), Quaternion.Euler(0f, 25f, 0f), Vector3.one * 0.9f, matProps, root.transform);
            InstantiateWeaponOrnament(weaponsRoot + "RGD-5.fbx", new Vector3(-4.6f, 1.05f, 4.8f), Quaternion.identity, 1.3f, matGuns, root.transform);
            InstantiateWeaponOrnament(weaponsRoot + "Smoke.fbx", new Vector3(-4.4f, 1.05f, 4.7f), Quaternion.Euler(0f, 30f, 0f), 1.3f, matGuns, root.transform);

            // Props en banco auxiliar trasero: Osciloscopio, cajas y miras tácticas ópticas
            InstantiateMeshProp(propsFbx, "workbench", new Vector3(-3.5f, 0f, -3.8f), Quaternion.Euler(0f, 90f, 0f), new Vector3(1.2f, 1.05f, 1.05f), matProps, root.transform);
            InstantiateMeshProp(propsFbx, "electronics3", new Vector3(-3.5f, 1.05f, -3.8f), Quaternion.identity, Vector3.one * 0.9f, matProps, root.transform);
            InstantiateMeshProp(propsFbx, "box2", new Vector3(-4.2f, 1.05f, -3.8f), Quaternion.Euler(0f, -15f, 0f), Vector3.one * 0.85f, matProps, root.transform);
            InstantiateWeaponOrnament(weaponsRoot + "ELCAN.fbx", new Vector3(-2.8f, 1.05f, -3.8f), Quaternion.Euler(0f, 45f, 0f), 1.3f, matGuns, root.transform);
            InstantiateWeaponOrnament(weaponsRoot + "TAN_LR_Scope_01.fbx", new Vector3(-2.5f, 1.05f, -3.7f), Quaternion.Euler(0f, -20f, 0f), 1.2f, matGuns, root.transform);
            InstantiateWeaponOrnament(weaponsRoot + "ANPEQ15.fbx", new Vector3(-3.0f, 1.05f, -3.6f), Quaternion.identity, 1.3f, matGuns, root.transform);

            // 3. Rincón Izquierdo (Pallet con cajas, bidones de combustible, obús y erizo defensivo)
            InstantiateMeshProp(propsFbx, "pallet", new Vector3(-5.6f, 0f, 2.5f), Quaternion.Euler(0f, 20f, 0f), Vector3.one, matProps, root.transform);
            InstantiateMeshProp(propsFbx, "box1", new Vector3(-5.6f, 0.15f, 2.5f), Quaternion.identity, Vector3.one * 0.9f, matProps, root.transform);
            InstantiateMeshProp(propsFbx, "box3", new Vector3(-5.6f, 0.55f, 2.5f), Quaternion.Euler(0f, 15f, 0f), Vector3.one * 0.8f, matProps, root.transform);
            InstantiateMeshProp(propsFbx, "canister", new Vector3(-5.0f, 0f, 3.4f), Quaternion.identity, Vector3.one * 1.1f, matProps, root.transform);
            InstantiateMeshProp(propsFbx, "canister", new Vector3(-4.6f, 0f, 3.2f), Quaternion.Euler(0f, 45f, 0f), Vector3.one * 1.0f, matProps, root.transform);
            InstantiateMeshProp(propsFbx, "howitzer_shell", new Vector3(-4.2f, 0f, 3.8f), Quaternion.identity, Vector3.one * 1.2f, matProps, root.transform);
            InstantiateMeshProp(propsFbx, "Antitank_hedgehog", new Vector3(-5.5f, 0f, -3.8f), Quaternion.Euler(0f, 35f, 0f), Vector3.one * 1.0f, matProps, root.transform);

            // 4. Pared Izquierda: Tablero con Lanzacohetes y Ametralladora ligera
            InstantiateWeaponOrnament(weaponsRoot + "RPG7.fbx", new Vector3(-6.88f, 2.3f, 1.0f), Quaternion.Euler(0f, 90f, 0f), 1.2f, matGuns, root.transform);
            InstantiateWeaponOrnament(weaponsRoot + "M249.fbx", new Vector3(-6.88f, 1.7f, 1.0f), Quaternion.Euler(0f, 90f, 0f), 1.1f, matGuns, root.transform);

            // 5. Rincón Trasero Izquierdo (Ametralladora M2 .50cal montada en trípode, nido de sacos terreros y mortero)
            InstantiateWeaponOrnament(weaponsRoot + "M2_50cal.fbx", new Vector3(-5.6f, 0f, -1.8f), Quaternion.Euler(0f, 135f, 0f), 1.0f, matGuns, root.transform);
            InstantiateMeshProp(propsFbx, "sandbag barricade", new Vector3(-5.6f, 0f, -1.0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, matProps, root.transform);
            InstantiateMeshProp(propsFbx, "ammo_box", new Vector3(-4.8f, 0f, -2.4f), Quaternion.identity, Vector3.one * 1.2f, matProps, root.transform);
            InstantiateMeshProp(propsFbx, "mortar.001", new Vector3(-5.0f, 0f, -3.0f), Quaternion.Euler(0f, 30f, 0f), Vector3.one * 1.1f, matProps, root.transform);

            // 6. Pared Trasera (Armero auxiliar con puerta blindada)
            var doorFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorFrame.name = "Armory_Security_Door";
            doorFrame.transform.SetParent(root.transform);
            doorFrame.transform.position = new Vector3(-2.7f, 1.5f, -5.4f);
            doorFrame.transform.localScale = new Vector3(1.6f, 3.0f, 0.15f);
            var matSteel = CreateMaterial("Mat_Armory_Steel", new Color(0.18f, 0.2f, 0.22f), 0.7f, 0.8f);
            doorFrame.GetComponent<MeshRenderer>().sharedMaterial = matSteel;
        }

        private static GameObject BuildCentralWorkbench(GameObject root, Material matMat, Material matProps)
        {
            var benchRoot = new GameObject("Central_Assembly_Workbench");
            benchRoot.transform.SetParent(root.transform);
            benchRoot.transform.position = new Vector3(0f, 0f, 1.42f);

            var propsFbx = "Assets/Defensive_props/models/Defensive_props.fbx";

            // Modelo físico del banco: Rotado 90 grados para quedar ancho (2.52m de ancho)
            var tableObj = InstantiateMeshProp(propsFbx, "workbench", Vector3.zero, Quaternion.Euler(0f, 90f, 0f), new Vector3(1.25f, 1.05f, 1.05f), matProps, benchRoot.transform);

            // Tapete de Armero (Mat de Despiece de neopreno): Colocado exactamente sobre la mesa a Y=1.055m
            var matObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            matObj.name = "Armorer_Disassembly_Mat";
            matObj.transform.SetParent(benchRoot.transform);
            matObj.transform.localPosition = new Vector3(0f, 1.055f, 0f);
            matObj.transform.localScale = new Vector3(1.68f, 0.014f, 0.74f);
            matObj.GetComponent<MeshRenderer>().sharedMaterial = matMat;

            // Borde / marco estético del tapete
            var matBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            matBorder.name = "Mat_Cyan_Accent_Border";
            matBorder.transform.SetParent(matObj.transform);
            matBorder.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            matBorder.transform.localScale = new Vector3(0.98f, 0.2f, 0.96f);
            Object.DestroyImmediate(matBorder.GetComponent<Collider>());
            var borderMat = CreateMaterial("Mat_MatBorder", new Color(0.12f, 0.32f, 0.42f), 0.1f, 0.0f);
            matBorder.GetComponent<MeshRenderer>().sharedMaterial = borderMat;

            // Props en la mesa (Caja de munición y vainas de calibre de prueba en los extremos)
            InstantiateMeshProp(propsFbx, "ammo_box", new Vector3(0.95f, 1.055f, 0.12f), Quaternion.Euler(0f, -15f, 0f), Vector3.one * 0.7f, matProps, benchRoot.transform);
            InstantiateMeshProp(propsFbx, "Shell_casing", new Vector3(0.85f, 1.055f, -0.15f), Quaternion.Euler(0f, 40f, 0f), Vector3.one * 1.5f, matProps, benchRoot.transform);
            InstantiateMeshProp(propsFbx, "Shell_casing", new Vector3(0.89f, 1.055f, -0.12f), Quaternion.Euler(0f, 10f, 0f), Vector3.one * 1.5f, matProps, benchRoot.transform);

            // Proyector holográfico (Placa emisora con brillo en la mesa)
            var holoEmitter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            holoEmitter.name = "Hologram_Projector_Base";
            holoEmitter.transform.SetParent(benchRoot.transform);
            holoEmitter.transform.localPosition = new Vector3(0f, 1.058f, 0f);
            holoEmitter.transform.localScale = new Vector3(0.55f, 0.006f, 0.55f);
            Object.DestroyImmediate(holoEmitter.GetComponent<Collider>());
            var emitterMat = CreateMaterial("Mat_HoloProjector", new Color(0.08f, 0.22f, 0.35f), 0.8f, 0.6f);
            emitterMat.EnableKeyword("_EMISSION");
            emitterMat.SetColor("_EmissionColor", new Color(0f, 0.7f, 1.0f) * 0.6f);
            holoEmitter.GetComponent<MeshRenderer>().sharedMaterial = emitterMat;

            return benchRoot;
        }

        private static Assembly.GuidanceArrow BuildGuidanceArrow(GameObject parent, Material lineMat)
        {
            var arrowGo = new GameObject("Guidance_Curved_Arrow");
            arrowGo.transform.SetParent(parent.transform);
            arrowGo.transform.localPosition = Vector3.zero;

            var lr = arrowGo.AddComponent<LineRenderer>();
            lr.sharedMaterial = lineMat;
            lr.startWidth = 0.015f;
            lr.endWidth = 0.035f;
            lr.enabled = false;

            var arrowComp = arrowGo.AddComponent<Assembly.GuidanceArrow>();
            return arrowComp;
        }

        private static GameObject BuildHolographicHUD(GameObject workbench)
        {
            var hudGo = new GameObject("Holographic_Assembly_HUD");
            hudGo.transform.SetParent(workbench.transform);
            hudGo.transform.localPosition = new Vector3(0f, 2.05f, 0.05f);
            hudGo.transform.localRotation = Quaternion.Euler(15f, 0f, 0f); // Inclinado hacia el usuario

            var canvas = hudGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = hudGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1050f, 480f);
            rt.localScale = Vector3.one * 0.0012f;

            hudGo.AddComponent<CanvasScaler>();
            hudGo.AddComponent<GraphicRaycaster>();

            // Panel de fondo translúcido cian/tecnológico
            var panelBg = new GameObject("Panel_Background");
            panelBg.transform.SetParent(hudGo.transform, false);
            var panelImage = panelBg.AddComponent<Image>();
            panelImage.color = new Color(0.04f, 0.09f, 0.15f, 0.90f);
            var panelRt = panelBg.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.sizeDelta = Vector2.zero;

            var outline = panelBg.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0.85f, 1f, 0.65f);
            outline.effectDistance = new Vector2(2f, 2f);

            // Título Principal
            CreateText(hudGo.transform, "Title", "BANCO DE ANATOMIA TECNICA", 24, new Vector2(0f, 195f), new Vector2(950f, 35f), FontStyles.Bold, new Color(0f, 0.9f, 1f));

            // Barra de Selección de Armas
            CreateButton(hudGo.transform, "Btn_PrevWeapon", "< Anterior", new Vector2(-360f, 145f), new Vector2(160f, 40f), new Color(0.12f, 0.28f, 0.42f));
            CreateText(hudGo.transform, "Txt_WeaponTitle", "PISTOLA COLT M1911 [.45 ACP]", 22, new Vector2(0f, 145f), new Vector2(520f, 40f), FontStyles.Bold, new Color(0.2f, 1f, 0.85f));
            CreateButton(hudGo.transform, "Btn_NextWeapon", "Siguiente >", new Vector2(360f, 145f), new Vector2(160f, 40f), new Color(0.12f, 0.28f, 0.42f));

            // Contador de Paso
            CreateText(hudGo.transform, "Txt_StepCounter", "PASO 1 DE 5", 20, new Vector2(0f, 95f), new Vector2(850f, 32f), FontStyles.Bold, new Color(1f, 0.85f, 0.3f));

            // Título de la Pieza
            CreateText(hudGo.transform, "Txt_StepTitle", "Acoplar: Armazón (Frame)", 23, new Vector2(0f, 55f), new Vector2(850f, 38f), FontStyles.Bold, Color.white);

            // Descripción Técnica / Función
            CreateText(hudGo.transform, "Txt_StepDescription", "Aloja el cargador, el mecanismo de disparo y sirve de guía longitudinal para la corredera.", 17, new Vector2(0f, 0f), new Vector2(880f, 60f), FontStyles.Normal, new Color(0.85f, 0.92f, 0.96f));

            // Pista de Controles
            CreateText(hudGo.transform, "Txt_ControlsHint", "[W,A,S,D] Moverse | [Clic/E] Agarrar | [1-4] Cambiar Arma | [Tab] Alternar Cursor", 15, new Vector2(0f, -55f), new Vector2(950f, 32f), FontStyles.Italic, new Color(0.4f, 0.88f, 1f));

            // Panel de Finalización (Oculto al inicio)
            var completionPanel = new GameObject("Panel_Completion");
            completionPanel.transform.SetParent(hudGo.transform, false);
            var compImg = completionPanel.AddComponent<Image>();
            compImg.color = new Color(0.02f, 0.16f, 0.12f, 0.96f);
            var compRt = completionPanel.GetComponent<RectTransform>();
            compRt.anchorMin = Vector2.zero;
            compRt.anchorMax = Vector2.one;
            compRt.sizeDelta = Vector2.zero;

            var compOutline = completionPanel.AddComponent<Outline>();
            compOutline.effectColor = new Color(0.1f, 1f, 0.5f, 0.85f);
            compOutline.effectDistance = new Vector2(3f, 3f);

            CreateText(completionPanel.transform, "Txt_CompletionTitle", "[!] APRENDIZAJE MECANICO VALIDADO\nAcceso al Poligono de Tiro Desbloqueado", 26, new Vector2(0f, 75f), new Vector2(900f, 85f), FontStyles.Bold, new Color(0.2f, 1f, 0.5f));

            CreateButton(completionPanel.transform, "Btn_ProceedToShootingRange", "Acceder al Poligono de Tiro >", new Vector2(0f, -25f), new Vector2(480f, 55f), new Color(0.1f, 0.65f, 0.35f));
            CreateButton(completionPanel.transform, "Btn_ResetAssembly", "Reiniciar Ensamble de este Arma", new Vector2(0f, -95f), new Vector2(380f, 45f), new Color(0.2f, 0.3f, 0.4f));

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
            tmp.fontSize = 18;
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

            var cc = rigGo.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.radius = 0.35f;

            var camGo = new GameObject("Main Camera");
            camGo.transform.SetParent(rigGo.transform);
            camGo.transform.localPosition = new Vector3(0f, 1.62f, 0.35f);
            camGo.transform.localRotation = Quaternion.Euler(18f, 0f, 0f); // Looking towards table

            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 100f;
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();

            // Mandos Oculus Touch visuales frente a la cámara (como en el mockup)
            var leftHand = CreateControllerVisual(camGo.transform, "LeftHand_OculusTouch", new Vector3(-0.24f, -0.22f, 0.42f), Quaternion.Euler(22f, -15f, -12f), matController);
            var rightHand = CreateControllerVisual(camGo.transform, "RightHand_OculusTouch", new Vector3(0.24f, -0.22f, 0.42f), Quaternion.Euler(22f, 15f, 12f), matController);

            // Controlador híbrido (Desktop WASD + Ratón + VR)
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

            var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "Touch_Handle";
            handle.transform.SetParent(go.transform, false);
            handle.transform.localPosition = new Vector3(0f, -0.06f, 0f);
            handle.transform.localScale = new Vector3(0.045f, 0.07f, 0.045f);
            handle.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(handle.GetComponent<Collider>());

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Touch_TrackingRing";
            ring.transform.SetParent(go.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.02f, 0.03f);
            ring.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            ring.transform.localScale = new Vector3(0.11f, 0.012f, 0.11f);
            ring.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(ring.GetComponent<Collider>());

            var grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Touch_GripTrigger";
            grip.transform.SetParent(go.transform, false);
            grip.transform.localPosition = new Vector3(0f, -0.04f, -0.025f);
            grip.transform.localScale = new Vector3(0.02f, 0.035f, 0.02f);
            var matBlack = CreateMaterial("Mat_TouchButtons", new Color(0.1f, 0.1f, 0.12f), 0.5f, 0.1f);
            grip.GetComponent<MeshRenderer>().sharedMaterial = matBlack;
            Object.DestroyImmediate(grip.GetComponent<Collider>());

            return go;
        }

        private static void ConfigureManager(Assembly.WeaponAssemblyManager manager, Transform piecesParent, Transform holoParent, Assembly.GuidanceArrow arrow, GameObject hudCanvas, Material matGun, Material matHolo, Material matSocketGlow)
        {
            var so = new SerializedObject(manager);

            so.FindProperty("piecesContainer").objectReferenceValue = piecesParent;
            so.FindProperty("hologramContainer").objectReferenceValue = holoParent;
            so.FindProperty("guidanceArrow").objectReferenceValue = arrow;

            so.FindProperty("gunMaterial").objectReferenceValue = matGun;
            so.FindProperty("hologramMaterial").objectReferenceValue = matHolo;
            so.FindProperty("socketGlowMaterial").objectReferenceValue = matSocketGlow;

            // UI Elements
            so.FindProperty("txtWeaponTitle").objectReferenceValue = hudCanvas.transform.Find("Txt_WeaponTitle").GetComponent<TextMeshProUGUI>();
            so.FindProperty("txtStepCounter").objectReferenceValue = hudCanvas.transform.Find("Txt_StepCounter").GetComponent<TextMeshProUGUI>();
            so.FindProperty("txtStepTitle").objectReferenceValue = hudCanvas.transform.Find("Txt_StepTitle").GetComponent<TextMeshProUGUI>();
            so.FindProperty("txtStepDescription").objectReferenceValue = hudCanvas.transform.Find("Txt_StepDescription").GetComponent<TextMeshProUGUI>();
            so.FindProperty("txtControlsHint").objectReferenceValue = hudCanvas.transform.Find("Txt_ControlsHint").GetComponent<TextMeshProUGUI>();

            so.FindProperty("btnPrevWeapon").objectReferenceValue = hudCanvas.transform.Find("Btn_PrevWeapon").GetComponent<Button>();
            so.FindProperty("btnNextWeapon").objectReferenceValue = hudCanvas.transform.Find("Btn_NextWeapon").GetComponent<Button>();

            var compPanel = hudCanvas.transform.Find("Panel_Completion").gameObject;
            so.FindProperty("panelCompletion").objectReferenceValue = compPanel;
            so.FindProperty("txtCompletionTitle").objectReferenceValue = compPanel.transform.Find("Txt_CompletionTitle").GetComponent<TextMeshProUGUI>();
            so.FindProperty("btnProceedToShootingRange").objectReferenceValue = compPanel.transform.Find("Btn_ProceedToShootingRange").GetComponent<Button>();
            so.FindProperty("btnResetAssembly").objectReferenceValue = compPanel.transform.Find("Btn_ResetAssembly").GetComponent<Button>();

            so.ApplyModifiedProperties();

            // Cargar arma inicial M1911 en modo edición para que se vea en el Scene View
            manager.ForceReinitializeDefaultWeapons();
            manager.LoadWeapon(0);
        }

        private static void ConfigureShootingRangeManager(VRRangeAcademy.Shooting.ShootingRangeManager mgr, Transform door, Light beacon, TextMeshProUGUI scoreboard, TextMeshProUGUI hitToast)
        {
            var so = new SerializedObject(mgr);
            so.FindProperty("securityDoor").objectReferenceValue = door;
            so.FindProperty("doorStatusLight").objectReferenceValue = beacon;
            so.FindProperty("txtScoreboard").objectReferenceValue = scoreboard;
            so.FindProperty("txtLastHitToast").objectReferenceValue = hitToast;
            so.ApplyModifiedProperties();
        }

        private static void BuildIntegratedShootingGallery(GameObject root, Material matFloor, Material matWall, Material matProps, out Transform doorTrans, out Light beaconLight, out TextMeshProUGUI txtScoreboard, out TextMeshProUGUI txtHitToast)
        {
            var rangeRoot = new GameObject("--- SHOOTING GALLERY (POLIGONO INTEGRADO) ---");
            rangeRoot.transform.SetParent(root.transform, false);

            var matRangeFloor = CreateMaterial("Mat_RangeFloor", new Color(0.11f, 0.12f, 0.13f), 0.1f, 0.1f);
            var matBulletTrap = CreateMaterial("Mat_BulletTrap", new Color(0.08f, 0.09f, 0.10f), 0.5f, 0.8f);
            var matGlass = CreateHologramMaterial("Mat_BallisticGlass", new Color(0.10f, 0.45f, 0.65f, 0.30f));
            var matSteel = CreateMaterial("Mat_Armory_Steel", new Color(0.18f, 0.20f, 0.22f), 0.7f, 0.8f);
            var matRedLine = CreateMaterial("Mat_SafetyRedLine", new Color(0.95f, 0.15f, 0.12f), 0.2f, 0.0f);
            var matAcoustic = CreateMaterial("Mat_BoothAcoustic", new Color(0.18f, 0.22f, 0.26f), 0.05f, 0.0f);

            // 1. Suelo del polígono (extendiéndose desde Z=-5.5 hasta Z=22.5)
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Range_Floor";
            floor.transform.SetParent(rangeRoot.transform, false);
            floor.transform.position = new Vector3(4.6f, -0.05f, 8.5f);
            floor.transform.localScale = new Vector3(6.0f, 0.1f, 28.0f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = matRangeFloor;

            // 2. Techo del polígono
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Range_Ceiling";
            ceiling.transform.SetParent(rangeRoot.transform, false);
            ceiling.transform.position = new Vector3(4.6f, 4.5f, 8.5f);
            ceiling.transform.localScale = new Vector3(6.0f, 0.15f, 28.0f);
            ceiling.GetComponent<MeshRenderer>().sharedMaterial = matWall;

            // 3. Pared lateral derecha de la galería
            var wallRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallRight.name = "Range_Right_Wall";
            wallRight.transform.SetParent(rangeRoot.transform, false);
            wallRight.transform.position = new Vector3(7.6f, 2.25f, 8.5f);
            wallRight.transform.localScale = new Vector3(0.2f, 4.5f, 28.0f);
            wallRight.GetComponent<MeshRenderer>().sharedMaterial = matWall;

            // 4. Muro de contención balística al fondo (Bullet Trap a Z=22.5)
            var trap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trap.name = "Range_Bullet_Trap_Wall";
            trap.transform.SetParent(rangeRoot.transform, false);
            trap.transform.position = new Vector3(4.6f, 2.25f, 22.5f);
            trap.transform.localScale = new Vector3(6.0f, 4.5f, 0.4f);
            trap.GetComponent<MeshRenderer>().sharedMaterial = matBulletTrap;

            // Franja de advertencia en el muro de contención
            var warningStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            warningStripe.name = "Trap_Warning_Stripe";
            warningStripe.transform.SetParent(trap.transform, false);
            warningStripe.transform.localPosition = new Vector3(0f, 0.4f, -0.52f);
            warningStripe.transform.localScale = new Vector3(0.98f, 0.08f, 0.1f);
            Object.DestroyImmediate(warningStripe.GetComponent<Collider>());
            var matWarning = CreateMaterial("Mat_WarningYellow", new Color(0.95f, 0.75f, 0.05f), 0.3f, 0.0f);
            warningStripe.GetComponent<MeshRenderer>().sharedMaterial = matWarning;

            // 5. Pared Divisoria entre Taller y Galería (a X=1.6m)
            var divBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            divBack.name = "Divider_Rear_Wall";
            divBack.transform.SetParent(rangeRoot.transform, false);
            divBack.transform.position = new Vector3(1.6f, 2.25f, -3.0f);
            divBack.transform.localScale = new Vector3(0.18f, 4.5f, 5.0f);
            divBack.GetComponent<MeshRenderer>().sharedMaterial = matWall;

            var divLower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            divLower.name = "Divider_Window_Base";
            divLower.transform.SetParent(rangeRoot.transform, false);
            divLower.transform.position = new Vector3(1.6f, 0.5f, 3.5f);
            divLower.transform.localScale = new Vector3(0.18f, 1.0f, 4.0f);
            divLower.GetComponent<MeshRenderer>().sharedMaterial = matSteel;

            var glassWindow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glassWindow.name = "Divider_Ballistic_Glass";
            glassWindow.transform.SetParent(rangeRoot.transform, false);
            glassWindow.transform.position = new Vector3(1.6f, 2.4f, 3.5f);
            glassWindow.transform.localScale = new Vector3(0.06f, 2.8f, 4.0f);
            glassWindow.GetComponent<MeshRenderer>().sharedMaterial = matGlass;

            // Marco de compuerta de seguridad (en Z=0.5m)
            var doorFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorFrame.name = "Range_Door_Frame";
            doorFrame.transform.SetParent(rangeRoot.transform, false);
            doorFrame.transform.position = new Vector3(1.6f, 1.5f, 0.5f);
            doorFrame.transform.localScale = new Vector3(0.22f, 3.0f, 1.8f);
            doorFrame.GetComponent<MeshRenderer>().sharedMaterial = matSteel;

            // Compuerta corrediza blindada
            var doorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorObj.name = "Range_Security_Gate";
            doorObj.transform.SetParent(rangeRoot.transform, false);
            doorObj.transform.position = new Vector3(1.6f, 1.4f, 0.5f);
            doorObj.transform.localScale = new Vector3(0.14f, 2.7f, 1.6f);
            doorObj.GetComponent<MeshRenderer>().sharedMaterial = matSteel;
            doorTrans = doorObj.transform;

            // Baliza de acceso LED sobre la puerta
            var beaconGo = new GameObject("Range_Access_Beacon");
            beaconGo.transform.SetParent(rangeRoot.transform, false);
            beaconGo.transform.position = new Vector3(1.6f, 3.05f, 0.5f);
            beaconLight = beaconGo.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.color = Color.red;
            beaconLight.intensity = 2.0f;
            beaconLight.range = 4.0f;

            // 6. Tres Puestos de Tiro independientes (carriles A/B/C) — se crean via BuildShootingBooth más abajo

            // 7. Iluminación Downrange de los Blancos (3 carriles A/B/C)
            // Carril A (X=3.0) -> Diana3(18m)   Carril B (X=4.6) -> Diana1(5m)   Carril C (X=6.2) -> Diana2(11m)
            CreateDownrangeSpotLight(rangeRoot.transform, new Vector3(4.6f, 3.8f, 5.0f),  new Vector3(4.6f, 1.35f, 6.5f));
            CreateDownrangeSpotLight(rangeRoot.transform, new Vector3(6.2f, 3.8f, 10.5f), new Vector3(6.2f, 1.0f, 12.0f));
            CreateDownrangeSpotLight(rangeRoot.transform, new Vector3(3.0f, 3.8f, 17.5f), new Vector3(3.0f, 1.0f, 19.0f));

            // 7.5. Señalización de distancias sobre las dianas
            BuildDistanceLabel(rangeRoot.transform, new Vector3(4.6f, 3.0f, 6.5f),  "5 m");
            BuildDistanceLabel(rangeRoot.transform, new Vector3(6.2f, 3.0f, 12.0f), "11 m");
            BuildDistanceLabel(rangeRoot.transform, new Vector3(3.0f, 3.0f, 19.0f), "18 m");

            // 7.6. Tres puestos de tiro separados (repisa + divisor acústico por carril)
            BuildShootingBooth(rangeRoot.transform, 4.6f, matSteel, matAcoustic, matRedLine, "B");  // puesto central -> Diana1
            BuildShootingBooth(rangeRoot.transform, 6.2f, matSteel, matAcoustic, matRedLine, "C");  // puesto derecho -> Diana2
            BuildShootingBooth(rangeRoot.transform, 3.0f, matSteel, matAcoustic, matRedLine, "A");  // puesto izquierdo -> Diana3

            // 8. Dianas Reactivas — carriles A(X=3.0) B(X=4.6) C(X=6.2)
            // Diana 1 carril B: Circular a 5m
            var targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Glowing Rifts/Shooting Target/ShootingTarget.prefab");
            if (targetPrefab != null)
            {
                var t1 = Object.Instantiate(targetPrefab, rangeRoot.transform);
                t1.name = "Target_Lane1_5m";
                t1.transform.position = new Vector3(4.6f, 1.35f, 6.5f);  // Carril B
                t1.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                t1.transform.localScale = Vector3.one * 1.5f;

                var col = t1.AddComponent<BoxCollider>();
                col.size = new Vector3(0.9f, 0.9f, 0.15f);
                col.center = Vector3.zero;

                var hitRec = t1.AddComponent<VRRangeAcademy.Shooting.TargetHitReceiver>();
                SetField(hitRec, "targetKind", VRRangeAcademy.Shooting.TargetKind.ConcentricRing);
                SetField(hitRec, "targetName", "Diana Circular 5m");
                SetField(hitRec, "distanceMeters", 5f);
            }

            // Dianas 2 y 3: Siluetas Dummy
            var dummyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blink/Art/NPCs/Stylized/DummyTarget/DummyTarget.prefab");
            if (dummyPrefab != null)
            {
                // Diana 2 carril C: Silueta a 11m
                var t2 = Object.Instantiate(dummyPrefab, rangeRoot.transform);
                t2.name = "Target_Lane2_Dummy_11m";
                t2.transform.position = new Vector3(6.2f, 0.95f, 12.0f);  // Carril C
                t2.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                t2.transform.localScale = Vector3.one * 0.85f;

                var col2 = t2.AddComponent<BoxCollider>();
                col2.size = new Vector3(0.9f, 2.1f, 0.5f);
                col2.center = Vector3.zero;

                var hitRec2 = t2.AddComponent<VRRangeAcademy.Shooting.TargetHitReceiver>();
                SetField(hitRec2, "targetKind", VRRangeAcademy.Shooting.TargetKind.Silhouette);
                SetField(hitRec2, "targetName", "Silueta Táctica 11m");
                SetField(hitRec2, "distanceMeters", 11f);

                // Diana 3 carril A: Silueta a 18m
                var t3 = Object.Instantiate(dummyPrefab, rangeRoot.transform);
                t3.name = "Target_Lane3_Dummy_18m";
                t3.transform.position = new Vector3(3.0f, 0.95f, 19.0f);  // Carril A
                t3.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                t3.transform.localScale = Vector3.one * 0.85f;

                var col3 = t3.AddComponent<BoxCollider>();
                col3.size = new Vector3(0.9f, 2.1f, 0.5f);
                col3.center = Vector3.zero;

                var hitRec3 = t3.AddComponent<VRRangeAcademy.Shooting.TargetHitReceiver>();
                SetField(hitRec3, "targetKind", VRRangeAcademy.Shooting.TargetKind.Silhouette);
                SetField(hitRec3, "targetName", "Silueta Francotirador 18m");
                SetField(hitRec3, "distanceMeters", 18f);
            }

            // 9. Marcador Digital LED (Scoreboard HUD)
            BuildRangeScoreboard(rangeRoot.transform, new Vector3(2.65f, 1.85f, 0.9f), Quaternion.Euler(0f, -90f, 0f), out txtScoreboard, out txtHitToast);
        }

        private static void BuildRangeScoreboard(Transform parent, Vector3 pos, Quaternion rot, out TextMeshProUGUI txtBoard, out TextMeshProUGUI txtToast)
        {
            var boardGo = new GameObject("Scoreboard_HUD");
            boardGo.transform.SetParent(parent, false);
            boardGo.transform.position = pos;
            boardGo.transform.rotation = rot;

            var canvas = boardGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = boardGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(720f, 440f);
            rt.localScale = Vector3.one * 0.0016f;

            boardGo.AddComponent<CanvasScaler>();
            boardGo.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("Bg");
            bg.transform.SetParent(boardGo.transform, false);
            var img = bg.AddComponent<Image>();
            img.color = new Color(0.04f, 0.08f, 0.12f, 0.94f);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            var outline = bg.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0.85f, 1f, 0.7f);
            outline.effectDistance = new Vector2(2f, 2f);

            CreateText(boardGo.transform, "Title", "POLÍGONO DE TIRO - REGISTRO BALÍSTICO", 24, new Vector2(0f, 175f), new Vector2(680f, 35f), FontStyles.Bold, new Color(0f, 0.9f, 1f));

            txtBoard = CreateText(boardGo.transform, "Txt_Stats", "ESPERANDO VALIDACIÓN DE APRENDIZAJE MECÁNICO", 20, new Vector2(0f, 40f), new Vector2(660f, 180f), FontStyles.Normal, Color.white);
            txtBoard.alignment = TextAlignmentOptions.TopLeft;

            txtToast = CreateText(boardGo.transform, "Txt_HitToast", "", 24, new Vector2(0f, -145f), new Vector2(660f, 45f), FontStyles.Bold, new Color(0.2f, 1f, 0.6f));
        }

        private static void BuildShootingBooth(Transform parent, float centerX, Material matSteel, Material matAcoustic, Material matRedLine, string lane)
        {
            // Repisa de apoyo (mostrador del puesto)
            var counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = $"Booth_{lane}_Counter";
            counter.transform.SetParent(parent, false);
            counter.transform.position = new Vector3(centerX, 1.05f, 0.9f);
            counter.transform.localScale = new Vector3(1.5f, 0.08f, 0.6f);
            counter.GetComponent<MeshRenderer>().sharedMaterial = matSteel;

            // Divisor izquierdo del carril
            var dL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dL.name = $"Booth_{lane}_Divider_L";
            dL.transform.SetParent(parent, false);
            dL.transform.position = new Vector3(centerX - 0.78f, 1.6f, 0.65f);
            dL.transform.localScale = new Vector3(0.07f, 2.2f, 1.5f);
            Object.DestroyImmediate(dL.GetComponent<Collider>());
            dL.GetComponent<MeshRenderer>().sharedMaterial = matAcoustic;

            // Divisor derecho del carril
            var dR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dR.name = $"Booth_{lane}_Divider_R";
            dR.transform.SetParent(parent, false);
            dR.transform.position = new Vector3(centerX + 0.78f, 1.6f, 0.65f);
            dR.transform.localScale = new Vector3(0.07f, 2.2f, 1.5f);
            Object.DestroyImmediate(dR.GetComponent<Collider>());
            dR.GetComponent<MeshRenderer>().sharedMaterial = matAcoustic;

            // Línea roja de disparo en el suelo
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = $"Safety_Line_{lane}";
            line.transform.SetParent(parent, false);
            line.transform.position = new Vector3(centerX, 0.01f, 1.2f);
            line.transform.localScale = new Vector3(1.5f, 0.005f, 0.12f);
            Object.DestroyImmediate(line.GetComponent<Collider>());
            line.GetComponent<MeshRenderer>().sharedMaterial = matRedLine;

            // Foco cenital del puesto
            var spotGo = new GameObject($"Booth_{lane}_SpotLight");
            spotGo.transform.SetParent(parent, false);
            spotGo.transform.position = new Vector3(centerX, 3.6f, 0.5f);
            spotGo.transform.rotation = Quaternion.Euler(80f, 0f, 0f);
            var spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(0.95f, 0.96f, 1.0f);
            spot.intensity = 3.0f;
            spot.range = 5.5f;
            spot.spotAngle = 55f;
        }

        private static void BuildDistanceLabel(Transform parent, Vector3 worldPos, string text)
        {
            var go = new GameObject($"Label_Dist_{text.Replace(" ","")}");
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300f, 80f);
            rt.localScale = Vector3.one * 0.004f;
            go.AddComponent<CanvasScaler>();

            var txtGo = new GameObject("Txt");
            txtGo.transform.SetParent(go.transform, false);
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 52;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(1f, 0.85f, 0.1f);
            tmp.alignment = TextAlignmentOptions.Center;
            var trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
        }

        private static void CreateDownrangeSpotLight(Transform parent, Vector3 pos, Vector3 target)
        {
            var go = new GameObject("SpotLight_Target");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.LookAt(target);

            var l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = new Color(1.0f, 0.95f, 0.88f);
            l.intensity = 4.0f;
            l.range = 10f;
            l.spotAngle = 48f;
        }

        private static void SetField(object obj, string fieldName, object value)
        {
            var fi = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (fi != null) fi.SetValue(obj, value);
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
                if (mat == null)
                {
                    mat = CreateMaterial("Mat_Defensive_props_URP", new Color(0.28f, 0.26f, 0.22f), 0.3f, 0.5f);
                }
                mr.sharedMaterial = mat;
            }

            return go;
        }

        private static GameObject InstantiateWeaponOrnament(string fbxPath, Vector3 pos, Quaternion rot, float scale, Material mat, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (prefab == null) return null;

            var go = Object.Instantiate(prefab, parent);
            go.name = "Decor_" + prefab.name;
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = Vector3.one * scale;

            // Asegurar que las armas decorativas no tengan colliders ni rigidbodies
            foreach (var col in go.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(col);
            }
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>())
            {
                Object.DestroyImmediate(rb);
            }
            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
            {
                if (mat != null) mr.sharedMaterial = mat;
            }

            return go;
        }

        private static Material CreateMaterial(string name, Color color, float smoothness, float metallic)
        {
            var path = $"Assets/Materials/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                if (existing.shader == null || existing.shader.name == "Hidden/InternalErrorShader" || !existing.shader.name.Contains("Universal Render Pipeline"))
                {
                    existing.shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                }
                existing.color = color;
                if (existing.HasProperty("_BaseColor")) existing.SetColor("_BaseColor", color);
                if (existing.HasProperty("_Smoothness")) existing.SetFloat("_Smoothness", smoothness);
                if (existing.HasProperty("_Metallic")) existing.SetFloat("_Metallic", metallic);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            mat.name = name;
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            EnsureDirectory("Assets/Materials");
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static Material CreateHologramMaterial(string name, Color cyanGlow)
        {
            var path = $"Assets/Materials/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                if (existing.shader == null || existing.shader.name == "Hidden/InternalErrorShader" || !existing.shader.name.Contains("Universal Render Pipeline"))
                {
                    existing.shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                }
                existing.color = cyanGlow;
                if (existing.HasProperty("_BaseColor")) existing.SetColor("_BaseColor", cyanGlow);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.name = name;
            mat.color = cyanGlow;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", cyanGlow);

            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0); // Alpha
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

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
                if (s.path == scenePath) return;
            }
            scenes.Insert(1, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
