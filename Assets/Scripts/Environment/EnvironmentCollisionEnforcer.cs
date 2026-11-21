using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRRangeAcademy.Environment
{
    /// <summary>
    /// Garantiza en tiempo de ejecución que todos los objetos de utilería, decoración,
    /// mostradores, dianas y mobiliario tanto de la Escena 1 como de la Escena 2
    /// tengan colliders físicos sólidos y el jugador nunca los traspase.
    /// También ajusta la posición de pantallas diegéticas para evitar recortes en muros.
    /// </summary>
    public static class EnvironmentCollisionEnforcer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnforceSceneColliders()
        {
            EnforceCurrentScene();
            SceneManager.sceneLoaded += (scene, mode) => EnforceCurrentScene();
        }

        public static void EnforceCurrentScene()
        {
            // 1. Proteger mesas y bancos de trabajo
            EnforceWorkbenchColliders();

            // 2. Proteger separadores de puestos de tiro y accesos
            EnforceBoothDividers();

            // 3. Proteger todos los props de utilería y decoración (cajas, pallets, bidones, sacos, dianas)
            EnforcePropsColliders();

            // 4. Ajustar pantallas diegéticas para que no atraviesen tabiques
            EnforceScreensAlignment();
        }

        private static void EnforceWorkbenchColliders()
        {
            // Banco Central de la Escena 2 (Taller)
            var centralBench = GameObject.Find("Central_Assembly_Workbench");
            if (centralBench != null && centralBench.GetComponent<BoxCollider>() == null)
            {
                var bc = centralBench.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 0.53f, 0f);
                bc.size = new Vector3(2.55f, 1.06f, 1.20f);
            }

            // Banco Frontal de la Escena 1 (Menú)
            var frontalBench = GameObject.Find("Workbench_Frontal");
            if (frontalBench != null && frontalBench.GetComponent<BoxCollider>() == null)
            {
                var bc = frontalBench.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 0.45f, 0f);
                bc.size = new Vector3(2.45f, 0.90f, 0.95f);
            }
        }

        private static void EnforceBoothDividers()
        {
            // Separadores acústicos y mostradores de los puestos de tiro en Escena 2
            string[] lanes = { "A", "B", "C" };
            foreach (var lane in lanes)
            {
                var dL = GameObject.Find($"Booth_{lane}_Divider_L");
                if (dL != null && dL.GetComponent<Collider>() == null)
                {
                    dL.AddComponent<BoxCollider>();
                }

                var dR = GameObject.Find($"Booth_{lane}_Divider_R");
                if (dR != null && dR.GetComponent<Collider>() == null)
                {
                    dR.AddComponent<BoxCollider>();
                }

                var counter = GameObject.Find($"Booth_{lane}_Counter");
                if (counter != null && counter.GetComponent<Collider>() == null)
                {
                    counter.AddComponent<BoxCollider>();
                }
            }

            // Postes del marco de acceso al polígono
            var postL = GameObject.Find("Door_Post_Left");
            if (postL != null && postL.GetComponent<Collider>() == null)
            {
                postL.AddComponent<BoxCollider>();
            }

            var postR = GameObject.Find("Door_Post_Right");
            if (postR != null && postR.GetComponent<Collider>() == null)
            {
                postR.AddComponent<BoxCollider>();
            }
        }

        private static void EnforcePropsColliders()
        {
            var allRenderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
            foreach (var mr in allRenderers)
            {
                if (mr == null || !mr.enabled || !mr.gameObject.activeInHierarchy) continue;

                var go = mr.gameObject;
                string name = go.name;

                // Identificar si es un objeto decorativo o de utilería física
                bool isProp = name.StartsWith("Prop_") ||
                              name.StartsWith("Corner_") ||
                              name.StartsWith("Decor_") ||
                              name.StartsWith("Target_") ||
                              name.Contains("pallet") ||
                              name.Contains("canister") ||
                              name.Contains("sandbag") ||
                              name.Contains("mortar") ||
                              name.Contains("hedgehog") ||
                              name.Contains("howitzer") ||
                              name.Contains("dummy") ||
                              name.Contains("target") ||
                              name.Contains("box") ||
                              name.Contains("ammo_box") ||
                              name.Contains("workbench") ||
                              name.Contains("barrel") ||
                              name.Contains("M2_50cal");

                if (!isProp) continue;

                // Ignorar armas que están adosadas a la pared a más de 1.4m de altura
                if (name.StartsWith("Decor_") && go.transform.position.y > 1.4f && !name.Contains("M2_50cal"))
                {
                    continue;
                }

                // Si ya tiene un collider en este objeto o en su padre inmediato (que no sea un root general), omitir
                if (go.GetComponent<Collider>() != null)
                {
                    continue;
                }

                var parent = go.transform.parent;
                if (parent != null && parent.GetComponent<Collider>() != null && !parent.name.StartsWith("---"))
                {
                    continue;
                }

                // Añadir BoxCollider envolvente seguro para máxima compatibilidad con CharacterController
                var mf = go.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    var bc = go.AddComponent<BoxCollider>();
                    Bounds b = mf.sharedMesh.bounds;
                    bc.center = b.center;
                    bc.size = b.size;
                }
                else
                {
                    var bc = go.AddComponent<BoxCollider>();
                    Vector3 localCenter = go.transform.InverseTransformPoint(mr.bounds.center);
                    Vector3 localSize = go.transform.InverseTransformVector(mr.bounds.size);
                    bc.center = localCenter;
                    bc.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
                }
            }
        }

        private static void EnforceScreensAlignment()
        {
            // Asegurar que el Scoreboard_HUD no quede atravesado dentro del panel divisor de tiro
            var scoreboard = GameObject.Find("Scoreboard_HUD");
            if (scoreboard != null)
            {
                var rt = scoreboard.GetComponent<RectTransform>();
                if (rt != null)
                {
                    // Si está en posición conflictiva incrustada en el tabique (X entre 3.0 y 4.0 con rotación incorrecta)
                    if (scoreboard.transform.position.x < 4.0f && scoreboard.transform.position.z > 4.0f)
                    {
                        // Posicionar sólidamente adosado a la cara interior del puesto B, orientado hacia el tirador
                        scoreboard.transform.position = new Vector3(3.88f, 1.65f, 5.60f);
                        scoreboard.transform.rotation = Quaternion.Euler(0f, 70f, 0f);
                    }
                }
            }
        }
    }
}
