using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using VRRangeAcademy.UI;

namespace VRRangeAcademy.Editor
{
    public static class MainMenuSceneBuilder
    {
        [MenuItem("VR Academy/Construir Escena Menu Principal")]
        public static void BuildScene()
        {
            // Clear existing objects except camera & light if already present, or recreate cleanly
            GameObject existingCam = GameObject.Find("Main Camera");
            GameObject existingLight = GameObject.Find("Directional Light");

            // 1. Root Folders
            GameObject envRoot = GetOrCreateRoot("--- ENVIRONMENT (TALLER ARMERO) ---");
            GameObject uiRoot = GetOrCreateRoot("--- UI SPATIAL (WORLD SPACE) ---");
            GameObject playerRoot = GetOrCreateRoot("--- PLAYER (XR RIG PLACEHOLDER) ---");

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

            // 6. UI CANVAS (World Space VR Diegetic Interface)
            BuildVRMenuCanvas(uiRoot);

            // 7. Player / XR Setup
            SetupPlayerRig(playerRoot, existingCam);

            // 8. EventSystem
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            // Save Scene
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log("<color=#2EA3FF><b>[VR Range Academy]</b> Escena MainMenu construida y guardada exitosamente!</color>");
        }

        private static void BuildVRMenuCanvas(GameObject parent)
        {
            GameObject canvasObj = new GameObject("VR_MainMenu_Canvas");
            canvasObj.transform.parent = parent.transform;
            // Positioned right behind the workbench, at natural standing eye level
            canvasObj.transform.position = new Vector3(0.2f, 1.45f, 2.25f);
            canvasObj.transform.rotation = Quaternion.Euler(0, -3f, 0);
            canvasObj.transform.localScale = Vector3.one * 0.0016f; // World space scaling

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
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
                "👤 PERFIL DE USUARIO:\n<size=18><b>Cadete 01</b></size>", 14, FontStyles.Normal,
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
                "🛡️   COMENZAR CAPACITACIÓN", 24,
                new Color(0.14f, 0.36f, 0.60f, 1.0f), Color.white);

            // 2. Campo de Tiro Libre
            GameObject btnFreeRangeObj = CreateUIButton("Btn_CampoTiroLibre", mainPanel.transform,
                new Vector2(0.5f, 1.0f), new Vector2(0, -340f), new Vector2(920f, 72f),
                "🎯   CAMPO DE TIRO LIBRE                                    🔒", 22,
                new Color(0.10f, 0.22f, 0.35f, 0.85f), new Color(0.75f, 0.85f, 0.95f));

            // 3. Calibración / Tutorial de Mandos
            GameObject btnCalibObj = CreateUIButton("Btn_Calibracion", mainPanel.transform,
                new Vector2(0.5f, 1.0f), new Vector2(0, -430f), new Vector2(920f, 72f),
                "🥽   CALIBRACIÓN / TUTORIAL DE MANDOS", 22,
                new Color(0.10f, 0.22f, 0.35f, 0.85f), Color.white);

            // --- Utility Buttons ---
            // Ayuda
            GameObject btnHelpObj = CreateUIButton("Btn_Ayuda", mainPanel.transform,
                new Vector2(0.5f, 1.0f), new Vector2(-235f, -525f), new Vector2(440f, 62f),
                "❓   AYUDA Y NORMAS", 20,
                new Color(0.13f, 0.28f, 0.44f, 0.95f), Color.white);

            // Configuración
            GameObject btnSettingsObj = CreateUIButton("Btn_Configuracion", mainPanel.transform,
                new Vector2(0.5f, 1.0f), new Vector2(235f, -525f), new Vector2(440f, 62f),
                "⚙️   CONFIGURACIÓN", 20,
                new Color(0.13f, 0.28f, 0.44f, 0.95f), Color.white);

            // --- Footer Status Bar ---
            GameObject footerBar = CreateUIPanel("FooterStatusBar", mainPanel.transform,
                new Vector2(0.5f, 0f), new Vector2(0, 35f), new Vector2(980f, 48f),
                new Color(0.06f, 0.12f, 0.20f, 0.95f));

            CreateUIText("FooterText", footerBar.transform,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(950f, 40f),
                "🎮 <b>Dispositivo:</b> Meta Quest 3   |   <b>Mandos:</b> Touch Plus (use Gatillo para seleccionar)",
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
            so.ApplyModifiedProperties();

            // Build Left-Hand Controller Guide Diagram (As seen on left side of the reference image!)
            BuildControllerGuideDiagram(parent);
        }

        private static void BuildControllerGuideDiagram(GameObject parent)
        {
            GameObject guideObj = new GameObject("VR_ControllerGuide_HUD");
            guideObj.transform.parent = parent.transform;
            guideObj.transform.position = new Vector3(-1.15f, 1.25f, 1.95f);
            guideObj.transform.rotation = Quaternion.Euler(0, 22f, 0); // Angled directly towards player
            guideObj.transform.localScale = Vector3.one * 0.0016f;

            Canvas guideCanvas = guideObj.AddComponent<Canvas>();
            guideCanvas.renderMode = RenderMode.WorldSpace;
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
                "🎮 CONTROLES META QUEST 3\n<size=14><color=#7FC3FF>Touch Plus Controllers</color></size>", 19, FontStyles.Bold,
                TextAlignmentOptions.Center, new Color(0.3f, 0.85f, 1.0f));

            // Controls Breakdown List
            string controlsGuideText =
                "<b>Gatillo Index (Trigger):</b>\n  ↳ Seleccionar menús / Disparar arma\n\n" +
                "<b>Grip (Agarre medio):</b>\n  ↳ Sujetar armas, empuñadura y cargadores\n\n" +
                "<b>Botón 'A' (Mando Der) / 'X' (Mando Izq):</b>\n  ↳ Menú rápido / Liberar cerrojo (Slide lock)\n\n" +
                "<b>Botón 'B' / 'Y':</b>\n  ↳ Expulsar cargador / Seguro del arma\n\n" +
                "<b>Thumbstick (Joystick):</b>\n  ↳ Desplazamiento y giro suave / por pasos";

            CreateUIText("GuideBody", panel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0, -20f), new Vector2(430f, 380f),
                controlsGuideText, 16, FontStyles.Normal, TextAlignmentOptions.TopLeft, Color.white);
        }

        private static void SetupPlayerRig(GameObject parent, GameObject existingCamera)
        {
            GameObject rig = new GameObject("XR_Origin_Rig");
            rig.transform.parent = parent.transform;
            rig.transform.position = Vector3.zero;

            // Camera Setup
            GameObject camObj = existingCamera != null ? existingCamera : new GameObject("Main Camera");
            camObj.transform.parent = rig.transform;
            camObj.transform.position = new Vector3(0, 1.65f, 0); // Natural human standing height
            camObj.transform.rotation = Quaternion.identity;

            Camera cam = camObj.GetComponent<Camera>() ?? camObj.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;

            // Right Controller Placeholder with Laser Pointer
            GameObject rightHand = new GameObject("RightHand_Controller_Sim");
            rightHand.transform.parent = rig.transform;
            rightHand.transform.position = new Vector3(0.25f, 1.25f, 0.5f);
            rightHand.transform.rotation = Quaternion.Euler(8f, 0, 0); // Pointing slightly down towards menu

            rightHand.AddComponent<VRRaycastPointer>();
        }

        #region Helper Creators
        private static GameObject GetOrCreateRoot(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                obj = new GameObject(name);
            }
            return obj;
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
