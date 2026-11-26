using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using VRRangeAcademy.UI;
using VRRangeAcademy.Anatomy;

namespace VRRangeAcademy.Editor
{
    public static class MainMenuSceneBuilder
    {
        [MenuItem("VR Academy/Construir Escena Menu Principal")]
        public static void BuildScene()
        {
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            foreach (var rootGo in activeScene.GetRootGameObjects())
            {
                if (rootGo.name.StartsWith("---") || rootGo.name == "EventSystem" || rootGo.name == "XR_Origin_Rig" || rootGo.name == "Anatomy_Inspection_Station")
                {
                    Object.DestroyImmediate(rootGo);
                }
            }

            GameObject existingCam = GameObject.Find("Main Camera");
            GameObject existingLight = GameObject.Find("Directional Light");

            // 1. Root Folders
            GameObject envRoot = new GameObject("--- ENVIRONMENT (TALLER ARMERO) ---");
            GameObject uiRoot = new GameObject("--- UI SPATIAL (WORLD SPACE) ---");
            GameObject playerRoot = new GameObject("--- PLAYER (XR RIG PLACEHOLDER) ---");

            // 2. Materials
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material matFloor = CreateMaterial("Mat_Workshop_Floor", new Color(0.25f, 0.27f, 0.28f), 0.3f, 0.1f, urpShader);
            Material matWalls = CreateMaterial("Mat_Workshop_Walls", new Color(0.42f, 0.44f, 0.45f), 0.1f, 0.0f, urpShader);
            Material matWood = CreateMaterial("Mat_Workbench_Wood", new Color(0.45f, 0.30f, 0.18f), 0.2f, 0.0f, urpShader);
            Material matMetal = CreateMaterial("Mat_Dark_Steel", new Color(0.18f, 0.20f, 0.22f), 0.7f, 0.8f, urpShader);
            Material matMat = CreateMaterial("Mat_GunMat_Blue", new Color(0.12f, 0.22f, 0.32f), 0.2f, 0.0f, urpShader);
            Material matLamp = CreateMaterial("Mat_Lamp_Yellow", new Color(1.0f, 0.95f, 0.7f), 0.1f, 0.0f, Shader.Find("Universal Render Pipeline/Unlit") ?? urpShader);

            // 3. Room Geometry (Taller / Workshop)
            // Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.parent = envRoot.transform;
            floor.transform.position = new Vector3(0, -0.05f, 2.0f);
            floor.transform.localScale = new Vector3(10f, 0.1f, 10f);
            floor.GetComponent<Renderer>().sharedMaterial = matFloor;

            // Back Wall
            GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "Back_Wall";
            backWall.transform.parent = envRoot.transform;
            backWall.transform.position = new Vector3(0, 2.0f, 5.0f);
            backWall.transform.localScale = new Vector3(10f, 4.0f, 0.2f);
            backWall.GetComponent<Renderer>().sharedMaterial = matWalls;

            // Left Wall
            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "Left_Wall";
            leftWall.transform.parent = envRoot.transform;
            leftWall.transform.position = new Vector3(-5.0f, 2.0f, 2.0f);
            leftWall.transform.localScale = new Vector3(0.2f, 4.0f, 10f);
            leftWall.GetComponent<Renderer>().sharedMaterial = matWalls;

            // Right Wall
            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "Right_Wall";
            rightWall.transform.parent = envRoot.transform;
            rightWall.transform.position = new Vector3(5.0f, 2.0f, 2.0f);
            rightWall.transform.localScale = new Vector3(0.2f, 4.0f, 10f);
            rightWall.GetComponent<Renderer>().sharedMaterial = matWalls;

            // Rear Wall (Pared trasera cerrada detrás del jugador)
            GameObject rearWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rearWall.name = "Rear_Wall";
            rearWall.transform.parent = envRoot.transform;
            rearWall.transform.position = new Vector3(0, 2.0f, -3.0f);
            rearWall.transform.localScale = new Vector3(10f, 4.0f, 0.2f);
            rearWall.GetComponent<Renderer>().sharedMaterial = matWalls;

            // Armored Security Door on Rear Wall
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Rear_Armory_Door";
            door.transform.parent = envRoot.transform;
            door.transform.position = new Vector3(0, 1.25f, -2.88f);
            door.transform.localScale = new Vector3(1.3f, 2.5f, 0.06f);
            door.GetComponent<Renderer>().sharedMaterial = matMetal;

            // Door Frame / Trim
            GameObject doorFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorFrame.name = "Door_Frame";
            doorFrame.transform.parent = door.transform;
            doorFrame.transform.localPosition = new Vector3(0, 0, 0);
            doorFrame.transform.localScale = new Vector3(1.15f, 1.05f, 1.2f);
            doorFrame.GetComponent<Renderer>().sharedMaterial = matWood;

            // Door Bulkhead Lamp
            GameObject doorLamp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            doorLamp.name = "Door_Bulkhead_Lamp";
            doorLamp.transform.parent = envRoot.transform;
            doorLamp.transform.position = new Vector3(0, 2.7f, -2.85f);
            doorLamp.transform.localScale = new Vector3(0.2f, 0.05f, 0.2f);
            doorLamp.transform.rotation = Quaternion.Euler(90f, 0, 0);
            GameObject doorLightObj = new GameObject("Door_Lamp_Light");
            doorLightObj.transform.parent = doorLamp.transform;
            doorLightObj.transform.localPosition = Vector3.zero;
            var doorLight = doorLightObj.AddComponent<Light>();
            doorLight.type = LightType.Point;
            doorLight.range = 5f;
            doorLight.intensity = 1.2f;
            doorLight.color = new Color(1.0f, 0.9f, 0.75f);

            // Ceiling
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.parent = envRoot.transform;
            ceiling.transform.position = new Vector3(0, 4.0f, 2.0f);
            ceiling.transform.localScale = new Vector3(10f, 0.1f, 10f);
            ceiling.GetComponent<Renderer>().sharedMaterial = matMetal;

            // 4. Central Workbench (Mesa armera frontal)
            GameObject workbench = new GameObject("Workbench_Frontal");
            workbench.transform.parent = envRoot.transform;
            workbench.transform.position = new Vector3(0, 0, 1.4f);

            // Bounding box collider sólido para que el jugador nunca traspase la mesa armera
            var benchCol = workbench.AddComponent<BoxCollider>();
            benchCol.center = new Vector3(0f, 0.45f, 0f);
            benchCol.size = new Vector3(2.45f, 0.90f, 0.95f);

            // Table top
            GameObject tableTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tableTop.name = "TableTop";
            tableTop.transform.parent = workbench.transform;
            tableTop.transform.localPosition = new Vector3(0, 0.85f, 0);
            tableTop.transform.localScale = new Vector3(2.4f, 0.08f, 0.9f);
            tableTop.GetComponent<Renderer>().sharedMaterial = matWood;

            // Table Gun Mat (Alfombrilla para desarme de armas)
            GameObject gunMat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gunMat.name = "GunMaintenanceMat";
            gunMat.transform.parent = workbench.transform;
            gunMat.transform.localPosition = new Vector3(0, 0.895f, 0);
            gunMat.transform.localScale = new Vector3(1.2f, 0.01f, 0.6f);
            gunMat.GetComponent<Renderer>().sharedMaterial = matMat;

            // Table Legs
            Vector3[] legOffsets = {
                new Vector3(-1.1f, 0.42f, -0.38f),
                new Vector3(1.1f, 0.42f, -0.38f),
                new Vector3(-1.1f, 0.42f, 0.38f),
                new Vector3(1.1f, 0.42f, 0.38f)
            };
            for (int i = 0; i < legOffsets.Length; i++)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = $"Leg_{i + 1}";
                leg.transform.parent = workbench.transform;
                leg.transform.localPosition = legOffsets[i];
                leg.transform.localScale = new Vector3(0.08f, 0.84f, 0.08f);
                leg.GetComponent<Renderer>().sharedMaterial = matMetal;
            }

            // Lower shelf
            GameObject shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelf.name = "LowerShelf";
            shelf.transform.parent = workbench.transform;
            shelf.transform.localPosition = new Vector3(0, 0.2f, 0);
            shelf.transform.localScale = new Vector3(2.2f, 0.04f, 0.75f);
            shelf.GetComponent<Renderer>().sharedMaterial = matMetal;

            // Vise on table (Tornillo de banco a la izquierda)
            GameObject vise = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vise.name = "Workbench_Vise";
            vise.transform.parent = workbench.transform;
            vise.transform.localPosition = new Vector3(-0.95f, 0.97f, 0.15f);
            vise.transform.localScale = new Vector3(0.18f, 0.16f, 0.25f);
            vise.GetComponent<Renderer>().sharedMaterial = matMetal;

            // Pegboard on back wall (Tablero de herramientas)
            GameObject pegboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pegboard.name = "Wall_Pegboard";
            pegboard.transform.parent = envRoot.transform;
            pegboard.transform.position = new Vector3(0, 2.3f, 4.88f);
            pegboard.transform.localScale = new Vector3(3.5f, 1.5f, 0.04f);
            pegboard.GetComponent<Renderer>().sharedMaterial = matWood;

            // Back Workbenches / Storage Cabinets
            GameObject backCounter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backCounter.name = "Back_Storage_Counter";
            backCounter.transform.parent = envRoot.transform;
            backCounter.transform.position = new Vector3(0, 0.5f, 4.4f);
            backCounter.transform.localScale = new Vector3(4.0f, 1.0f, 0.8f);
            backCounter.GetComponent<Renderer>().sharedMaterial = matMetal;

            // 5. Lighting (Lámparas colgantes tipo taller industrial)
            GameObject lampObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lampObj.name = "Industrial_Lamp_Ceiling";
            lampObj.transform.parent = envRoot.transform;
            lampObj.transform.position = new Vector3(0, 2.8f, 1.5f);
            lampObj.transform.localScale = new Vector3(0.5f, 0.15f, 0.5f);
            lampObj.GetComponent<Renderer>().sharedMaterial = matLamp;

            GameObject lampLightObj = new GameObject("Lamp_SpotLight");
            lampLightObj.transform.parent = lampObj.transform;
            lampLightObj.transform.localPosition = Vector3.zero;
            Light lampLight = lampLightObj.AddComponent<Light>();
            lampLight.type = LightType.Spot;
            lampLight.spotAngle = 85f;
            lampLight.range = 7f;
            lampLight.intensity = 2.5f;
            lampLight.color = new Color(1.0f, 0.94f, 0.82f);
            lampLight.transform.localRotation = Quaternion.Euler(90f, 0, 0);

            // Configure Directional Light
            if (existingLight == null)
            {
                existingLight = new GameObject("Directional Light");
                existingLight.AddComponent<Light>();
            }
            existingLight.transform.parent = envRoot.transform;
            Light dirLight = existingLight.GetComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.intensity = 1.0f;
            dirLight.color = new Color(0.9f, 0.93f, 1.0f);
            existingLight.transform.rotation = Quaternion.Euler(45f, -30f, 0);

            // 7. Player / XR Setup
            Camera mainCam = SetupPlayerRig(playerRoot, existingCam);

            // 8. EventSystem (New Input System UI Module)
            EventSystem es = Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                es = esObj.AddComponent<EventSystem>();
            }
            var standalone = es.GetComponent<StandaloneInputModule>();
            if (standalone != null) Object.DestroyImmediate(standalone);
            if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            {
                es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // 9. Decorative Props & Ornamental Weapons
            GameObject workbenchM1911 = DecorateWorkshopEnvironment();

            // 6. UI CANVAS (World Space VR Diegetic Interface)
            BuildVRMenuCanvas(uiRoot, mainCam, workbenchM1911);

            // Save Scene
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log("<color=#2EA3FF><b>[VR Range Academy]</b> Escena MainMenu construida y guardada exitosamente!</color>");
        }

        private static void BuildVRMenuCanvas(GameObject parent, Camera cam, GameObject workbenchM1911)
        {
            GameObject canvasObj = new GameObject("VR_MainMenu_Canvas");
            canvasObj.transform.parent = parent.transform;
            // Positioned right behind the workbench, at natural standing eye level
            canvasObj.transform.position = new Vector3(0.2f, 1.45f, 2.25f);
            canvasObj.transform.rotation = Quaternion.Euler(0, -3f, 0);
            canvasObj.transform.localScale = Vector3.one * 0.0016f; // World space scaling

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1000f, 680f);

            MainMenuManager menuManager = canvasObj.AddComponent<MainMenuManager>();

            // --- Background Panel ---
            GameObject mainPanel = CreateUIPanel("MainPanel", canvasObj.transform,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 680f),
                new Color(0.08f, 0.16f, 0.25f, 0.92f));

            // Cyan header top bar
            GameObject topAccent = CreateUIPanel("TopAccentBar", mainPanel.transform,
                new Vector2(0.5f, 1.0f), new Vector2(0, -12f), new Vector2(980f, 4f),
                new Color(0.18f, 0.64f, 1.0f, 1.0f));

            // Profile Box (Left Header)
            GameObject profileBox = CreateUIPanel("ProfileBox", mainPanel.transform,
                new Vector2(0f, 1.0f), new Vector2(130f, -65f), new Vector2(230f, 85f),
                new Color(0.12f, 0.24f, 0.36f, 0.85f));

            GameObject profileTxtObj = CreateUIText("ProfileText", profileBox.transform,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(210f, 75f),
                "PERFIL DE USUARIO:\n<size=18><b>Cadete 01</b></size>", 14, FontStyles.Normal,
                TextAlignmentOptions.Center, new Color(0.85f, 0.92f, 1.0f));

            // Main Title (Right Header)
            CreateUIText("MainTitleText", mainPanel.transform,
                new Vector2(0f, 1.0f), new Vector2(610f, -65f), new Vector2(680f, 85f),
                "SIMULADOR DE MANIPULACIÓN\nSEGURA DE ARMAS DE FUEGO", 28, FontStyles.Bold,
                TextAlignmentOptions.Left, Color.white);

            // Sub-header Banner ("Menú Principal")
            GameObject subBanner = CreateUIPanel("SubHeaderBanner", mainPanel.transform,
                new Vector2(0.5f, 1.0f), new Vector2(0, -135f), new Vector2(980f, 42f),
                new Color(0.16f, 0.34f, 0.52f, 0.95f));

            CreateUIText("SubHeaderText", subBanner.transform,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 38f),
                "Menú Principal", 22, FontStyles.Bold,
                TextAlignmentOptions.Center, Color.white);

            // Instruction Text
            CreateUIText("InstructionText", mainPanel.transform,
                new Vector2(0.5f, 1.0f), new Vector2(0, -180f), new Vector2(940f, 40f),
                "Bienvenido al simulador. Apunte con el rayo de su mando hacia la opción deseada y presione el gatillo para ingresar.",
                15, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.85f, 0.92f, 1.0f));

            // --- Main Buttons ---
            // 1. Comenzar Capacitación
            GameObject btnStartObj = CreateUIButton("Btn_ComenzarCapacitacion", mainPanel.transform,
                new Vector2(0.5f, 1.0f), new Vector2(0, -250f), new Vector2(920f, 72f),
                "[+]   COMENZAR CAPACITACIÓN", 24,
                new Color(0.14f, 0.36f, 0.60f, 1.0f), Color.white);

            // 2. Campo de Tiro Libre
            GameObject btnFreeRangeObj = CreateUIButton("Btn_CampoTiroLibre", mainPanel.transform,
                new Vector2(0.5f, 1.0f), new Vector2(0, -340f), new Vector2(920f, 72f),
                "[TIRO]   CAMPO DE TIRO LIBRE (POLÍGONO)", 22,
                new Color(0.10f, 0.28f, 0.45f, 0.95f), Color.white);

            // 3. Calibración / Tutorial de Mandos
            GameObject btnCalibObj = CreateUIButton("Btn_Calibracion", mainPanel.transform,
                new Vector2(0.5f, 1.0f), new Vector2(0, -430f), new Vector2(920f, 72f),
                "[VR]   CALIBRACIÓN / TUTORIAL DE MANDOS", 22,
                new Color(0.10f, 0.22f, 0.35f, 0.85f), Color.white);

            // --- Utility Buttons ---
            // Ayuda
            GameObject btnHelpObj = CreateUIButton("Btn_Ayuda", mainPanel.transform,
                new Vector2(0.5f, 1.0f), new Vector2(-235f, -525f), new Vector2(440f, 62f),
                "[?]   AYUDA Y NORMAS", 20,
                new Color(0.13f, 0.28f, 0.44f, 0.95f), Color.white);

            // Configuración
            GameObject btnSettingsObj = CreateUIButton("Btn_Configuracion", mainPanel.transform,
                new Vector2(0.5f, 1.0f), new Vector2(235f, -525f), new Vector2(440f, 62f),
                "[*]   CONFIGURACIÓN", 20,
                new Color(0.13f, 0.28f, 0.44f, 0.95f), Color.white);

            // --- Footer Status Bar ---
            GameObject footerBar = CreateUIPanel("FooterStatusBar", mainPanel.transform,
                new Vector2(0.5f, 0f), new Vector2(0, 35f), new Vector2(980f, 48f),
                new Color(0.06f, 0.12f, 0.20f, 0.95f));

            CreateUIText("FooterText", footerBar.transform,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(950f, 40f),
                "<b>Dispositivo:</b> Meta Quest 3   |   <b>Mandos:</b> Touch Plus (use Gatillo Index para seleccionar)",
                15, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.6f, 0.82f, 1.0f));

            // --- Toast Notification Panel ---
            GameObject toastPanel = CreateUIPanel("ToastNotificationPanel", mainPanel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0, -60f), new Vector2(750f, 70f),
                new Color(0.05f, 0.15f, 0.25f, 0.98f));
            toastPanel.SetActive(false);

            GameObject toastTextObj = CreateUIText("ToastText", toastPanel.transform,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 60f),
                "Notificación", 18, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.3f, 0.9f, 1.0f));

            // Connect references to MainMenuManager via SerializedObject
            SerializedObject so = new SerializedObject(menuManager);
            so.FindProperty("mainPanel").objectReferenceValue = mainPanel;
            so.FindProperty("btnStartTraining").objectReferenceValue = btnStartObj.GetComponent<Button>();
            so.FindProperty("btnFreeRange").objectReferenceValue = btnFreeRangeObj.GetComponent<Button>();
            so.FindProperty("btnCalibration").objectReferenceValue = btnCalibObj.GetComponent<Button>();
            so.FindProperty("btnHelp").objectReferenceValue = btnHelpObj.GetComponent<Button>();
            so.FindProperty("btnSettings").objectReferenceValue = btnSettingsObj.GetComponent<Button>();
            so.FindProperty("messageToastPanel").objectReferenceValue = toastPanel;
            so.FindProperty("messageToastText").objectReferenceValue = toastTextObj.GetComponent<TextMeshProUGUI>();
            so.FindProperty("profileNameText").objectReferenceValue = profileTxtObj.GetComponent<TextMeshProUGUI>();
            so.FindProperty("trainingSceneName").stringValue = "WeaponAssembly";
            so.FindProperty("freeRangeSceneName").stringValue = "WeaponAssembly";
            so.ApplyModifiedProperties();

            // Build Left-Hand Controller Guide Diagram (As seen on left side of the reference image!)
            BuildControllerGuideDiagram(parent, cam);

            // Build Right-Hand Anatomy & Nomenclature Station (Symmetrical to Guide Diagram)
            BuildAnatomyInspectionStation(parent, cam, workbenchM1911);
        }

        private static void BuildControllerGuideDiagram(GameObject parent, Camera cam)
        {
            GameObject guideObj = new GameObject("VR_ControllerGuide_HUD");
            guideObj.transform.parent = parent.transform;
            guideObj.transform.position = new Vector3(-1.35f, 1.38f, 1.98f);
            guideObj.transform.rotation = Quaternion.Euler(6.43f, -34.29f, 0f); // Inclinado y orientado directamente hacia el jugador
            guideObj.transform.localScale = Vector3.one * 0.0016f;

            Canvas guideCanvas = guideObj.AddComponent<Canvas>();
            guideCanvas.renderMode = RenderMode.WorldSpace;
            guideCanvas.worldCamera = cam;
            guideObj.AddComponent<CanvasScaler>();
            guideObj.AddComponent<GraphicRaycaster>();

            RectTransform guideRect = guideObj.GetComponent<RectTransform>();
            guideRect.sizeDelta = new Vector2(480f, 520f);

            // Guide Background Panel
            GameObject panel = CreateUIPanel("GuidePanel", guideObj.transform,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 520f),
                new Color(0.08f, 0.16f, 0.25f, 0.90f));

            // Title
            CreateUIText("GuideTitle", panel.transform,
                new Vector2(0.5f, 1.0f), new Vector2(0, -35f), new Vector2(440f, 55f),
                "CONTROLES META QUEST 3\n<size=14><color=#7FC3FF>Touch Plus Controllers</color></size>", 19, FontStyles.Bold,
                TextAlignmentOptions.Center, new Color(0.3f, 0.85f, 1.0f));

            // Controls Breakdown List
            string controlsGuideText =
                "<b>Gatillo Index (Trigger):</b>\n  -> Seleccionar menús / Disparar arma\n\n" +
                "<b>Grip (Agarre medio):</b>\n  -> Sujetar armas, empuñadura y piezas\n\n" +
                "<b>Botón 'A' (Mando Der) / 'X' (Mando Izq):</b>\n  -> Menú rápido / Alternar Armar-Desarmar\n\n" +
                "<b>Botón 'B' / 'Y':</b>\n  -> Soltar pieza / Alternar Polígono de Tiro\n\n" +
                "<b>Thumbstick (Joystick):</b>\n  -> Desplazamiento (Izq) y Giro por pasos (Der)";

            CreateUIText("GuideBody", panel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0, -20f), new Vector2(430f, 380f),
                controlsGuideText, 16, FontStyles.Normal, TextAlignmentOptions.TopLeft, Color.white);
        }

        private static void BuildAnatomyInspectionStation(GameObject parent, Camera cam, GameObject m1911Workbench)
        {
            GameObject stationObj = new GameObject("Anatomy_Inspection_Station");
            stationObj.transform.parent = parent.transform;
            stationObj.transform.position = new Vector3(1.35f, 1.38f, 1.98f);
            stationObj.transform.rotation = Quaternion.Euler(6.43f, 34.29f, 0f);
            stationObj.transform.localScale = Vector3.one * 0.0016f;

            Canvas canvas = stationObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            stationObj.AddComponent<CanvasScaler>();
            stationObj.AddComponent<GraphicRaycaster>();

            RectTransform rect = stationObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(480f, 520f);

            WeaponAnatomyManager anatomyManager = stationObj.AddComponent<WeaponAnatomyManager>();

            // Panel Fondo
            GameObject cardBg = CreateUIPanel("CardBackground", stationObj.transform,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 520f),
                new Color(0.08f, 0.16f, 0.25f, 0.90f));

            // Barra de acento superior cian
            CreateUIPanel("TopAccentBar", cardBg.transform,
                new Vector2(0.5f, 1.0f), new Vector2(0, -8f), new Vector2(460f, 4f),
                new Color(0.18f, 0.64f, 1.0f, 1.0f));

            // Encabezado
            CreateUIText("CardHeader", cardBg.transform,
                new Vector2(0.5f, 1.0f), new Vector2(0, -34f), new Vector2(460f, 46f),
                "NOMENCLATURA Y PARTES DEL ARMA\n<size=13><color=#7FC3FF>Inspección Técnica y Seguridad</color></size>",
                18, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.3f, 0.85f, 1.0f));

            // Nombre de la Pieza
            GameObject txtPartNameObj = CreateUIText("TxtPartName", cardBg.transform,
                new Vector2(0.5f, 1.0f), new Vector2(0, -78f), new Vector2(450f, 36f),
                "Armazón y Empuñadura", 18, FontStyles.Bold,
                TextAlignmentOptions.Center, new Color(1.0f, 0.85f, 0.35f, 1.0f));

            // Nombre Técnico en Inglés
            GameObject txtTechObj = CreateUIText("TxtTechnicalName", cardBg.transform,
                new Vector2(0.5f, 1.0f), new Vector2(0, -106f), new Vector2(450f, 22f),
                "EN: Frame / Receiver", 13, FontStyles.Normal,
                TextAlignmentOptions.Center, new Color(0.65f, 0.85f, 1.0f));

            // Caja Función Mecánica
            GameObject funcBox = CreateUIPanel("FunctionBox", cardBg.transform,
                new Vector2(0.5f, 1.0f), new Vector2(0, -178f), new Vector2(450f, 110f),
                new Color(0.05f, 0.11f, 0.18f, 0.85f));

            GameObject txtFuncObj = CreateUIText("TxtFunction", funcBox.transform,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(430f, 100f),
                "<b>Función Mecánica:</b>\nEstructura principal de la pistola donde se alojan los rieles guía de la corredera, el brocal del cargador y los mecanismos de disparo.",
                14, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.9f, 0.95f, 1.0f));

            // Caja Norma de Seguridad
            GameObject safetyBox = CreateUIPanel("SafetyBox", cardBg.transform,
                new Vector2(0.5f, 1.0f), new Vector2(0, -305f), new Vector2(450f, 125f),
                new Color(0.18f, 0.10f, 0.08f, 0.85f));

            GameObject txtSafetyObj = CreateUIText("TxtSafetyRule", safetyBox.transform,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(430f, 115f),
                "<color=#FFA726><b>Norma de Seguridad:</b></color>\nAsegure un empuñe firme y alto con ambas manos (High-Grip) para un control estable del retroceso sin cruzar los pulgares detrás de la corredera.",
                13.5f, FontStyles.Normal, TextAlignmentOptions.TopLeft, Color.white);

            // Barra de Navegación Inferior
            GameObject navBar = CreateUIPanel("NavBar", cardBg.transform,
                new Vector2(0.5f, 0f), new Vector2(0, 40f), new Vector2(450f, 52f),
                new Color(0.04f, 0.08f, 0.14f, 0.90f));

            // Botón Anterior
            GameObject btnPrevObj = CreateUIButton("BtnPrev", navBar.transform,
                new Vector2(0f, 0.5f), new Vector2(75f, 0f), new Vector2(130f, 42f),
                "< ANTERIOR", 14,
                new Color(0.12f, 0.28f, 0.44f, 0.95f), Color.white);

            // Contador de Piezas
            GameObject txtCounterObj = CreateUIText("TxtCounter", navBar.transform,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 40f),
                "Pieza 1 / 5", 15, FontStyles.Bold,
                TextAlignmentOptions.Center, new Color(0.6f, 0.82f, 1.0f));

            // Botón Siguiente
            GameObject btnNextObj = CreateUIButton("BtnNext", navBar.transform,
                new Vector2(1f, 0.5f), new Vector2(-75f, 0f), new Vector2(130f, 42f),
                "SIGUIENTE >", 14,
                new Color(0.12f, 0.28f, 0.44f, 0.95f), Color.white);

            // Si m1911Workbench no vino como parámetro, buscarlo en la escena
            if (m1911Workbench == null)
            {
                m1911Workbench = GameObject.Find("Workbench_M1911") ?? GameObject.Find("M1911");
            }

            // Configurar piezas anatómicas en el M1911 de la mesa
            var partIdentifiers = SetupM1911AnatomyParts(m1911Workbench);

            // Serializar propiedades en WeaponAnatomyManager
            SerializedObject so = new SerializedObject(anatomyManager);
            so.FindProperty("anatomyDisplayCard").objectReferenceValue = cardBg;
            so.FindProperty("txtPartName").objectReferenceValue = txtPartNameObj.GetComponent<TextMeshProUGUI>();
            so.FindProperty("txtTechnicalName").objectReferenceValue = txtTechObj.GetComponent<TextMeshProUGUI>();
            so.FindProperty("txtFunction").objectReferenceValue = txtFuncObj.GetComponent<TextMeshProUGUI>();
            so.FindProperty("txtSafetyRule").objectReferenceValue = txtSafetyObj.GetComponent<TextMeshProUGUI>();
            so.FindProperty("txtCounter").objectReferenceValue = txtCounterObj.GetComponent<TextMeshProUGUI>();
            so.FindProperty("btnNextPart").objectReferenceValue = btnNextObj.GetComponent<Button>();
            so.FindProperty("btnPrevPart").objectReferenceValue = btnPrevObj.GetComponent<Button>();

            var partsProp = so.FindProperty("weaponParts");
            partsProp.ClearArray();
            for (int i = 0; i < partIdentifiers.Count; i++)
            {
                partsProp.InsertArrayElementAtIndex(i);
                partsProp.GetArrayElementAtIndex(i).objectReferenceValue = partIdentifiers[i];
            }
            so.ApplyModifiedProperties();
        }

        private static List<WeaponPartIdentifier> SetupM1911AnatomyParts(GameObject m1911)
        {
            var parts = new List<WeaponPartIdentifier>();
            if (m1911 == null) return parts;

            System.Action<string, string, string, string, string> addPart = (path, name, en, func, safety) => {
                Transform t = m1911.transform.Find(path);
                if (t != null)
                {
                    var id = t.GetComponent<WeaponPartIdentifier>();
                    if (id == null) id = t.gameObject.AddComponent<WeaponPartIdentifier>();
                    id.Configure(name, en, func, safety);
                    EditorUtility.SetDirty(id);
                    parts.Add(id);
                }
            };

            // 1. Armazón / Frame
            addPart("M1911",
                "Armazón y Empuñadura (Receiver / Frame)",
                "Frame / Receiver",
                "Estructura principal de la pistola donde se alojan los rieles guía de la corredera, el brocal del cargador y los mecanismos de disparo.",
                "Asegure un empuñe firme y alto con ambas manos (High-Grip) para un control estable del retroceso sin cruzar los pulgares detrás de la corredera.");

            // 2. Corredera / Slide
            addPart("M1911/Slide",
                "Corredera (Slide)",
                "Slide Assembly",
                "Aloja el cañón, la aguja percutora y el extractor. Retrocede por la fuerza de los gases expulsando la vaina servida y avanza cargando un nuevo cartucho en la recámara.",
                "Tirar hacia atrás y bloquear con la palanca de retención permite verificar visual y físicamente que la recámara esté 100% vacía antes de cualquier manipulación.");

            // 3. Cargador / Magazine
            addPart("M1911/Magazin",
                "Cargador (Magazine)",
                "Detachable Magazine",
                "Dispositivo desmontable que almacena los cartuchos y los eleva mediante un resorte interno para alimentar la recámara.",
                "En cualquier maniobra de seguridad o despeje, el PRIMER paso absoluto e inviolable es presionar el retén y extraer el cargador.");

            // 4. Cola del Disparador / Trigger
            addPart("M1911/Trigger",
                "Cola del Disparador (Trigger)",
                "Trigger",
                "Transmite la presión mecánica del dedo índice para liberar el fiador y percutir el cartucho.",
                "REGLA DE ORO: Mantenga siempre el dedo índice estirado fuera del guardamonte y a lo largo del armazón hasta que las miras apunten al blanco.");

            // 5. Martillo Percutor / Hammer
            addPart("M1911/Hammer",
                "Martillo Percutor (Hammer)",
                "Hammer",
                "Componente pivotante que, impulsado por el muelle real, golpea la aguja percutora contra el iniciador/fulminante del cartucho.",
                "Nunca intente desamartillar manualmente un arma cargada sin mantener la boca del cañón apuntando en una dirección 100% segura.");

            return parts;
        }

        private static Camera SetupPlayerRig(GameObject parent, GameObject existingCamera)
        {
            GameObject rig = new GameObject("XR_Origin_Rig");
            rig.transform.parent = parent.transform;
            rig.transform.position = Vector3.zero;

            CharacterController cc = rig.AddComponent<CharacterController>();
            cc.height = 1.75f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.875f, 0);

            rig.AddComponent<VRRangeAcademy.Player.DesktopPlayerController>();

            // Camera Setup
            GameObject camObj = existingCamera != null ? existingCamera : new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.parent = rig.transform;
            camObj.transform.position = new Vector3(0, 1.65f, 0); // Altura natural humana de pie
            camObj.transform.rotation = Quaternion.identity;

            Camera cam = camObj.GetComponent<Camera>();
            if (cam == null) cam = camObj.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;

            if (camObj.GetComponent<AudioListener>() == null)
            {
                camObj.AddComponent<AudioListener>();
            }

            Shader ctrlShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            Material matCtrl = new Material(ctrlShader);
            matCtrl.color = new Color(0.88f, 0.90f, 0.94f);
            if (matCtrl.HasProperty("_BaseColor")) matCtrl.SetColor("_BaseColor", new Color(0.88f, 0.90f, 0.94f));

            // Mando Izquierdo (Meta Quest 3 Touch Plus)
            GameObject leftHand = new GameObject("LeftHand_Controller");
            leftHand.transform.parent = rig.transform;
            leftHand.transform.localPosition = new Vector3(-0.25f, 1.25f, 0.45f);
            var leftTracker = leftHand.AddComponent<VRRangeAcademy.Player.VRControllerTracker>();
            leftTracker.SetNode(UnityEngine.XR.XRNode.LeftHand, new Vector3(-0.25f, 1.25f, 0.45f), Vector3.zero);
            CreateControllerVisualGeometry(leftHand.transform, "LeftHand_Visual", matCtrl, true);

            // Mando Derecho (Meta Quest 3 Touch Plus) con Rayo Láser Interactivo
            GameObject rightHand = new GameObject("RightHand_Controller");
            rightHand.transform.parent = rig.transform;
            rightHand.transform.localPosition = new Vector3(0.25f, 1.25f, 0.45f);
            var rightTracker = rightHand.AddComponent<VRRangeAcademy.Player.VRControllerTracker>();
            rightTracker.SetNode(UnityEngine.XR.XRNode.RightHand, new Vector3(0.25f, 1.25f, 0.45f), Vector3.zero);
            CreateControllerVisualGeometry(rightHand.transform, "RightHand_Visual", matCtrl, false);

            rightHand.AddComponent<VRRaycastPointer>();

            return cam;
        }

        private static void CreateControllerVisualGeometry(Transform parent, string name, Material mat, bool isLeft)
        {
            GameObject visual = new GameObject(name);
            visual.transform.SetParent(parent, false);

            // Empuñadura
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "Handle";
            handle.transform.SetParent(visual.transform, false);
            handle.transform.localPosition = new Vector3(0f, -0.04f, -0.02f);
            handle.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            handle.transform.localScale = new Vector3(0.032f, 0.055f, 0.032f);
            handle.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(handle.GetComponent<Collider>());

            // Frontal
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Faceplate";
            head.transform.SetParent(visual.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.01f, 0.01f);
            head.transform.localScale = new Vector3(0.045f, 0.025f, 0.055f);
            head.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(head.GetComponent<Collider>());

            // Aro de seguimiento Touch
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "TrackingRing";
            ring.transform.SetParent(visual.transform, false);
            ring.transform.localPosition = new Vector3(isLeft ? -0.015f : 0.015f, 0.025f, 0.025f);
            ring.transform.localRotation = Quaternion.Euler(45f, isLeft ? -15f : 15f, 0f);
            ring.transform.localScale = new Vector3(0.065f, 0.008f, 0.065f);
            ring.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(ring.GetComponent<Collider>());
        }

        private static GameObject DecorateWorkshopEnvironment()
        {
            var existingDecor = GameObject.Find("--- DECORATIVE PROPS & WEAPONS (NON-INTERACTIVE) ---");
            if (existingDecor != null) Object.DestroyImmediate(existingDecor);

            GameObject decorRoot = new GameObject("--- DECORATIVE PROPS & WEAPONS (NON-INTERACTIVE) ---");

            System.Func<GameObject, Vector3, Quaternion, Vector3, bool, GameObject> spawnProp = (prefab, pos, rot, scale, isWeapon) => {
                if (prefab == null) return null;
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.transform.parent = decorRoot.transform;
                inst.transform.position = pos;
                inst.transform.rotation = rot;
                inst.transform.localScale = scale;

                // Armas ornamentales vs objetos físicos
                if (isWeapon)
                {
                    var rbs = inst.GetComponentsInChildren<Rigidbody>();
                    foreach (var rb in rbs) Object.DestroyImmediate(rb);

                    if (pos.y < 0.5f)
                    {
                        // Armas pesadas apoyadas en suelo / trípode (e.g. M2 .50cal): collider físico para no atravesarlas
                        var bc = inst.AddComponent<BoxCollider>();
                        bc.size = new Vector3(1.2f, 1.1f, 1.2f);
                        bc.center = new Vector3(0f, 0.55f, 0f);
                    }
                    else
                    {
                        // Armas en mesas o paredes: sin collider bloqueante
                        var colliders = inst.GetComponentsInChildren<Collider>();
                        foreach (var c in colliders) c.enabled = false;
                    }
                }
                else
                {
                    // Props decorativos (cajas, pallets, sacos, bidones, dianas, dummies): garantizar collider
                    if (inst.GetComponentInChildren<Collider>() == null)
                    {
                        var renderers = inst.GetComponentsInChildren<Renderer>();
                        if (renderers.Length > 0)
                        {
                            var bc = inst.AddComponent<BoxCollider>();
                            Bounds b = renderers[0].bounds;
                            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                            bc.center = inst.transform.InverseTransformPoint(b.center);
                            Vector3 sz = inst.transform.InverseTransformVector(b.size);
                            bc.size = new Vector3(Mathf.Abs(sz.x), Mathf.Abs(sz.y), Mathf.Abs(sz.z));
                        }
                    }
                }
                return inst;
            };

            // Prefabs
            var m1911 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Weapons VOL.1/Prefabs/M1911.prefab");
            var ak74 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Weapons VOL.1/Prefabs/AK74.prefab");
            var m4_8 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Weapons VOL.1/Prefabs/M4_8.prefab");
            var bennelli = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Weapons VOL.1/Prefabs/Bennelli_M4.prefab");
            var uzi = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Weapons VOL.1/Prefabs/Uzi.prefab");
            var m107 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Weapons VOL.1/Prefabs/M107.prefab");
            var m249 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Weapons VOL.1/Prefabs/M249.prefab");
            var elcan = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Weapons VOL.1/Prefabs/ELCAN.prefab");
            var flash = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Weapons VOL.1/Prefabs/Flash.prefab");

            var ammoBox = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Defensive_props/Prefabs/defence/ammo_box.prefab");
            var shell = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Defensive_props/Prefabs/defence/Shell_casing.prefab");
            var electronics = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Defensive_props/Prefabs/electronics/electronics1.prefab");
            var pallet = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Defensive_props/Prefabs/boxes/pallet.prefab");
            var box1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Defensive_props/Prefabs/boxes/box1.prefab");
            var box2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Defensive_props/Prefabs/boxes/box2.prefab");
            var canister = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Defensive_props/Prefabs/defence/canister.prefab");
            var sandbag = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Defensive_props/Prefabs/fortifications/sandbag barricade.prefab");

            var target = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Glowing Rifts/Shooting Target/ShootingTarget.prefab");
            var dummy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blink/Art/NPCs/Stylized/DummyTarget/DummyTarget.prefab");
            var rpg = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Weapons VOL.1/Prefabs/RPG7.prefab");
            var rgd5 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Weapons VOL.1/Prefabs/RGD-5.prefab");
            var smoke = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Weapons VOL.1/Prefabs/Smoke.prefab");
            var m2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Weapons VOL.1/Prefabs/M2_50cal.prefab");

            // Mesa frontal (Workbench)
            GameObject workbenchM1911 = spawnProp(m1911, new Vector3(-0.18f, 0.91f, 1.35f), Quaternion.Euler(-90f, 35f, 0f), Vector3.one, true);
            if (workbenchM1911 != null) workbenchM1911.name = "Workbench_M1911";
            spawnProp(elcan, new Vector3(0.18f, 0.91f, 1.45f), Quaternion.Euler(0, 45f, 0), Vector3.one * 0.9f, true);
            spawnProp(flash, new Vector3(0.24f, 0.91f, 1.32f), Quaternion.Euler(0, -60f, 0), Vector3.one * 0.9f, true);
            spawnProp(shell, new Vector3(-0.02f, 0.905f, 1.48f), Quaternion.Euler(0, 15f, 90f), Vector3.one * 0.8f, false);
            spawnProp(shell, new Vector3(-0.06f, 0.905f, 1.45f), Quaternion.Euler(0, -40f, 90f), Vector3.one * 0.8f, false);
            spawnProp(ammoBox, new Vector3(0.85f, 0.89f, 1.35f), Quaternion.Euler(0, -15f, 0), Vector3.one * 0.55f, false);
            spawnProp(electronics, new Vector3(-0.82f, 0.89f, 1.35f), Quaternion.Euler(0, 15f, 0), Vector3.one * 0.65f, false);

            // Armero en la pared trasera (Wall Pegboard)
            spawnProp(ak74, new Vector3(0f, 2.75f, 4.84f), Quaternion.Euler(0, 90f, 0), Vector3.one, true);
            spawnProp(m4_8, new Vector3(0f, 2.35f, 4.84f), Quaternion.Euler(0, 90f, 0), Vector3.one, true);
            spawnProp(bennelli, new Vector3(0f, 1.95f, 4.84f), Quaternion.Euler(0, 90f, 0), Vector3.one, true);
            spawnProp(uzi, new Vector3(-1.15f, 2.35f, 4.84f), Quaternion.Euler(0, 90f, 0), Vector3.one, true);

            // Mostrador trasero
            spawnProp(m107, new Vector3(0.85f, 1.01f, 4.35f), Quaternion.Euler(0, -25f, 0), Vector3.one * 0.9f, true);
            spawnProp(m249, new Vector3(-0.85f, 1.01f, 4.35f), Quaternion.Euler(0, 25f, 0), Vector3.one * 0.9f, true);
            spawnProp(ammoBox, new Vector3(0.0f, 1.01f, 4.4f), Quaternion.identity, Vector3.one * 0.7f, false);

            // Rincón logístico izquierdo
            spawnProp(pallet, new Vector3(-3.4f, 0f, 3.2f), Quaternion.Euler(0, 12f, 0), Vector3.one, false);
            spawnProp(box1, new Vector3(-3.4f, 0.16f, 3.2f), Quaternion.Euler(0, 12f, 0), Vector3.one * 0.85f, false);
            spawnProp(box2, new Vector3(-3.4f, 0.98f, 3.2f), Quaternion.Euler(0, 28f, 0), Vector3.one * 0.85f, false);
            spawnProp(canister, new Vector3(-3.8f, 0f, 1.9f), Quaternion.identity, Vector3.one, false);

            // Rincón de tiro derecho
            spawnProp(sandbag, new Vector3(3.6f, 0f, 3.2f), Quaternion.Euler(0, -45f, 0), Vector3.one * 0.9f, false);
            spawnProp(target, new Vector3(2.8f, 0f, 2.2f), Quaternion.Euler(0, -55f, 0), Vector3.one * 1.5f, false);
            spawnProp(dummy, new Vector3(3.7f, 0f, 4.2f), Quaternion.Euler(0, -60f, 0), Vector3.one * 0.8f, false);

            Material matWood = GameObject.Find("Workbench_Frontal/TableTop")?.GetComponent<Renderer>()?.sharedMaterial;
            Material matMetal = GameObject.Find("Ceiling")?.GetComponent<Renderer>()?.sharedMaterial;

            // Armeros en pared trasera (a ambos lados de la puerta)
            GameObject rearBoardLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rearBoardLeft.name = "Rear_Wall_Pegboard_Left";
            rearBoardLeft.transform.parent = decorRoot.transform;
            rearBoardLeft.transform.position = new Vector3(-2.3f, 2.05f, -2.88f);
            rearBoardLeft.transform.localScale = new Vector3(2.4f, 1.7f, 0.04f);
            if (matWood != null) rearBoardLeft.GetComponent<Renderer>().sharedMaterial = matWood;

            spawnProp(rpg, new Vector3(-2.3f, 2.55f, -2.83f), Quaternion.Euler(0, -90f, 0), Vector3.one, true);
            spawnProp(ak74, new Vector3(-2.3f, 2.10f, -2.83f), Quaternion.Euler(0, -90f, 0), Vector3.one, true);
            spawnProp(uzi, new Vector3(-2.3f, 1.65f, -2.83f), Quaternion.Euler(0, -90f, 0), Vector3.one, true);

            GameObject rearBoardRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rearBoardRight.name = "Rear_Wall_Pegboard_Right";
            rearBoardRight.transform.parent = decorRoot.transform;
            rearBoardRight.transform.position = new Vector3(2.3f, 2.05f, -2.88f);
            rearBoardRight.transform.localScale = new Vector3(2.4f, 1.7f, 0.04f);
            if (matWood != null) rearBoardRight.GetComponent<Renderer>().sharedMaterial = matWood;

            spawnProp(m107, new Vector3(2.3f, 2.55f, -2.83f), Quaternion.Euler(0, -90f, 0), Vector3.one * 0.9f, true);
            spawnProp(m4_8, new Vector3(2.3f, 2.10f, -2.83f), Quaternion.Euler(0, -90f, 0), Vector3.one, true);
            spawnProp(bennelli, new Vector3(2.3f, 1.65f, -2.83f), Quaternion.Euler(0, -90f, 0), Vector3.one, true);

            // Armero en pared lateral izquierda
            GameObject leftBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftBoard.name = "Left_Wall_Pegboard";
            leftBoard.transform.parent = decorRoot.transform;
            leftBoard.transform.position = new Vector3(-4.88f, 2.10f, 0.4f);
            leftBoard.transform.localScale = new Vector3(0.04f, 1.7f, 3.0f);
            if (matWood != null) leftBoard.GetComponent<Renderer>().sharedMaterial = matWood;

            spawnProp(m249, new Vector3(-4.83f, 2.55f, 0.4f), Quaternion.Euler(0, 0, 0), Vector3.one * 0.9f, true);
            spawnProp(ak74, new Vector3(-4.83f, 2.10f, 0.4f), Quaternion.Euler(0, 0, 0), Vector3.one, true);
            spawnProp(m4_8, new Vector3(-4.83f, 1.65f, 0.4f), Quaternion.Euler(0, 0, 0), Vector3.one, true);

            // Pared lateral derecha con repisa de granadas y ametralladora pesada
            GameObject rightBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightBoard.name = "Right_Wall_Pegboard";
            rightBoard.transform.parent = decorRoot.transform;
            rightBoard.transform.position = new Vector3(4.88f, 2.10f, 0.4f);
            rightBoard.transform.localScale = new Vector3(0.04f, 1.7f, 3.0f);
            if (matWood != null) rightBoard.GetComponent<Renderer>().sharedMaterial = matWood;

            GameObject shelfRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelfRight.name = "Right_Wall_Shelf";
            shelfRight.transform.parent = decorRoot.transform;
            shelfRight.transform.position = new Vector3(4.75f, 1.50f, 0.4f);
            shelfRight.transform.localScale = new Vector3(0.28f, 0.04f, 2.6f);
            if (matMetal != null) shelfRight.GetComponent<Renderer>().sharedMaterial = matMetal;

            spawnProp(m2, new Vector3(4.68f, 2.30f, 0.4f), Quaternion.Euler(0, -90f, 0), Vector3.one * 0.75f, true);
            spawnProp(rgd5, new Vector3(4.75f, 1.60f, 0.9f), Quaternion.identity, Vector3.one * 1.2f, true);
            spawnProp(rgd5, new Vector3(4.75f, 1.60f, 0.6f), Quaternion.identity, Vector3.one * 1.2f, true);
            spawnProp(smoke, new Vector3(4.75f, 1.60f, 0.1f), Quaternion.identity, Vector3.one * 1.2f, true);
            spawnProp(smoke, new Vector3(4.75f, 1.60f, -0.2f), Quaternion.identity, Vector3.one * 1.2f, true);

            return workbenchM1911;
        }

        #region Helper Creators
        private static GameObject GetOrCreateRoot(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                Object.DestroyImmediate(obj);
            }
            return new GameObject(name);
        }

        private static Material CreateMaterial(string name, Color color, float metallic, float smoothness, Shader shader)
        {
            Material mat = new Material(shader);
            mat.name = name;
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            return mat;
        }

        private static GameObject CreateUIPanel(string name, Transform parent, Vector2 anchor, Vector2 anchoredPos, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Image img = panel.AddComponent<Image>();
            img.color = color;

            return panel;
        }

        private static GameObject CreateUIText(string name, Transform parent, Vector2 anchor, Vector2 anchoredPos, Vector2 size,
            string content, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            return textObj;
        }

        private static GameObject CreateUIButton(string name, Transform parent, Vector2 anchor, Vector2 anchoredPos, Vector2 size,
            string label, float fontSize, Color bgColor, Color textColor)
        {
            GameObject btnObj = CreateUIPanel(name, parent, anchor, anchoredPos, size, bgColor);
            Button btn = btnObj.AddComponent<Button>();

            // BoxCollider con profundidad para detección inmediata por raycasts en VR
            BoxCollider col = btnObj.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, size.y, 20f);

            ColorBlock cb = btn.colors;
            cb.normalColor = bgColor;
            cb.highlightedColor = new Color(bgColor.r * 1.3f, bgColor.g * 1.3f, bgColor.b * 1.3f, 1.0f);
            cb.pressedColor = new Color(0.2f, 0.7f, 1.0f, 1.0f);
            btn.colors = cb;

            CreateUIText(name + "_Label", btnObj.transform,
                new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(40f, 10f),
                label, fontSize, FontStyles.Bold, TextAlignmentOptions.Center, textColor);

            return btnObj;
        }
        #endregion
    }
}
