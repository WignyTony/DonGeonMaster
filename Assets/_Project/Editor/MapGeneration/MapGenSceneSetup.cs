using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
using DonGeonMaster.MapGeneration;

public class MapGenSceneSetup
{
    [MenuItem("DonGeonMaster/Créer Scène MapGenDebug", false, 200)]
    public static void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // === Caméra principale ===
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.1f, 0.12f);
        cam.orthographic = true;
        cam.orthographicSize = 50;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 500f;
        camGo.transform.position = new Vector3(45, 100, 45);
        camGo.transform.rotation = Quaternion.Euler(90, 0, 0);

        // URP Camera Data
        var urpCam = camGo.AddComponent<UniversalAdditionalCameraData>();
        urpCam.renderPostProcessing = true;

        // === Lumière directionnelle ===
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.color = new Color(1f, 0.95f, 0.85f);
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
        lightGo.AddComponent<UniversalAdditionalLightData>();

        // === Lumière ambiante ===
        var fillLightGo = new GameObject("Fill Light");
        var fillLight = fillLightGo.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.intensity = 0.4f;
        fillLight.color = new Color(0.6f, 0.7f, 1f);
        fillLight.shadows = LightShadows.None;
        fillLightGo.transform.rotation = Quaternion.Euler(150, 60, 0);
        fillLightGo.AddComponent<UniversalAdditionalLightData>();

        // === Root de la map générée ===
        var mapRoot = new GameObject("GeneratedMap");

        // === Controller principal ===
        var controllerGo = new GameObject("MapGenDebugController");
        var controller = controllerGo.AddComponent<MapGenDebugController>();
        controllerGo.AddComponent<MapCleanupService>();
        controllerGo.AddComponent<PlayerSpawnService>();
        controllerGo.AddComponent<BatchTestRunner>();
        controllerGo.AddComponent<DebugVisualization>();

        // Connecter le registre s'il existe
        var registryGuids = AssetDatabase.FindAssets("t:AssetCategoryRegistry");
        if (registryGuids.Length > 0)
        {
            var registry = AssetDatabase.LoadAssetAtPath<AssetCategoryRegistry>(
                AssetDatabase.GUIDToAssetPath(registryGuids[0]));
            var so = new SerializedObject(controller);
            so.FindProperty("assetRegistry").objectReferenceValue = registry;
            so.FindProperty("mainCamera").objectReferenceValue = cam;
            so.ApplyModifiedProperties();
        }
        else
        {
            var so = new SerializedObject(controller);
            so.FindProperty("mainCamera").objectReferenceValue = cam;
            so.ApplyModifiedProperties();
            Debug.LogWarning("[MapGenSceneSetup] Aucun AssetCategoryRegistry trouvé. " +
                "Utilisez DonGeonMaster > Créer Catégories d'Assets MapGen pour en créer un.");
        }

        // Connecter le MapRoot au cleanup service
        var cleanupSO = new SerializedObject(controllerGo.GetComponent<MapCleanupService>());
        cleanupSO.FindProperty("mapRoot").objectReferenceValue = mapRoot.transform;
        cleanupSO.ApplyModifiedProperties();

        // === Sol de référence (plan infini) ===
        var groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        groundPlane.name = "ReferencePlane";
        groundPlane.transform.position = new Vector3(90, -0.1f, 90);
        groundPlane.transform.localScale = new Vector3(50, 1, 50);
        var groundRenderer = groundPlane.GetComponent<Renderer>();
        var groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        groundMat.color = new Color(0.15f, 0.18f, 0.12f);
        groundRenderer.material = groundMat;
        groundPlane.isStatic = true;

        // === Sauvegarder la scène ===
        string scenePath = "Assets/_Project/Scenes/MapGenDebug.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorSceneManager.OpenScene(scenePath);

        // Ajouter au Build Settings si pas déjà présent
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
            EditorBuildSettings.scenes);
        if (!scenes.Exists(s => s.path == scenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        Debug.Log($"[MapGenSceneSetup] Scène MapGenDebug créée et sauvegardée: {scenePath}");
        Debug.Log("[MapGenSceneSetup] Raccourcis: F5=Générer, F6=Regénérer, F7=Nettoyer, " +
                  "F8=Spawn, F9=Screenshot, F10=Camera, F12=Log, Tab=Toggle UI");
    }
}
