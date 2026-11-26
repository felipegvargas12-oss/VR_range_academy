using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace VRRangeAcademy.Editor
{
    public static class MetaQuest3BuildValidator
    {
        [MenuItem("VR Academy/Configurar Todo para Meta Quest 3")]
        public static void ConfigureProjectForMetaQuest3()
        {
            Debug.Log("<color=#00E5FF><b>[Meta Quest 3]</b> Iniciando adaptación y validación completa del proyecto...</color>");

            // 1. Configuración de PlayerSettings para Android / Meta Quest 3
            EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Android;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            // 2. OpenXR Settings para Android
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings != null)
            {
                foreach (var feat in settings.GetFeatures())
                {
                    if (feat is MetaQuestTouchPlusControllerProfile ||
                        feat is OculusTouchControllerProfile ||
                        feat is MetaQuestTouchProControllerProfile ||
                        feat is HandInteractionProfile ||
                        feat is MetaQuestFeature)
                    {
                        feat.enabled = true;
                        Debug.Log($"<color=#76FF03>• Perfil OpenXR activado: {feat.name}</color>");
                    }
                }
                EditorUtility.SetDirty(settings);
            }

            AssetDatabase.SaveAssets();

            // 3. Reconstrucción limpia de escenas
            Debug.Log("<color=#00E5FF><b>[Meta Quest 3]</b> Reconstruyendo Escena 1: MainMenu...</color>");
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            MainMenuSceneBuilder.BuildScene();
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("<color=#00E5FF><b>[Meta Quest 3]</b> Reconstruyendo Escena 2: WeaponAssembly...</color>");
            WeaponAssemblySceneBuilder.BuildAssemblySceneMenu();

            // Regresar a MainMenu como escena de inicio
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");

            Debug.Log("<color=#00FF88><b>[Meta Quest 3]</b> ¡Proyecto adaptado exitosamente! Mandos Touch Plus, rayo láser interactivo y OpenXR listos para compilar APK.</color>");
        }
    }
}
