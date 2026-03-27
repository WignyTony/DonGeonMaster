using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
using DonGeonMaster.MapGeneration;

public class MapGenSceneSetup
{
    static readonly string GanzsePrefabPath =
        "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Prefabs/Modular Character/" +
        "Modular Character Update 1.1/GanzSe Free Modular Character Update 1_1.prefab";

    [MenuItem("DonGeonMaster/Créer Scène MapGenDebug", false, 200)]
    public static void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // === Camera ===
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
        camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;

        // === Lumieres ===
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.color = new Color(1f, 0.95f, 0.85f);
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
        lightGo.AddComponent<UniversalAdditionalLightData>();

        var fillGo = new GameObject("Fill Light");
        var fill = fillGo.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.4f;
        fill.color = new Color(0.6f, 0.7f, 1f);
        fill.shadows = LightShadows.None;
        fillGo.transform.rotation = Quaternion.Euler(150, 60, 0);
        fillGo.AddComponent<UniversalAdditionalLightData>();

        // === Map root ===
        var mapRoot = new GameObject("GeneratedMap");

        // === Controller ===
        var ctrlGo = new GameObject("MapGenDebugController");
        var ctrl = ctrlGo.AddComponent<MapGenDebugController>();
        ctrlGo.AddComponent<MapCleanupService>();
        var spawnService = ctrlGo.AddComponent<PlayerSpawnService>();
        ctrlGo.AddComponent<BatchTestRunner>();
        ctrlGo.AddComponent<DebugVisualization>();

        // Connecter references serialisees
        var ctrlSO = new SerializedObject(ctrl);
        ctrlSO.FindProperty("mainCamera").objectReferenceValue = cam;

        // Connecter le registre d'assets s'il existe
        var registryGuids = AssetDatabase.FindAssets("t:AssetCategoryRegistry");
        if (registryGuids.Length > 0)
        {
            var registry = AssetDatabase.LoadAssetAtPath<AssetCategoryRegistry>(
                AssetDatabase.GUIDToAssetPath(registryGuids[0]));
            ctrlSO.FindProperty("assetRegistry").objectReferenceValue = registry;
        }
        else
        {
            Debug.LogWarning("[MapGenSceneSetup] AssetCategoryRegistry non trouve. " +
                "Lancez DonGeonMaster > Creer Categories d'Assets MapGen d'abord.");
        }
        ctrlSO.ApplyModifiedProperties();

        // Connecter MapRoot au cleanup service
        var cleanupSO = new SerializedObject(ctrlGo.GetComponent<MapCleanupService>());
        cleanupSO.FindProperty("mapRoot").objectReferenceValue = mapRoot.transform;
        cleanupSO.ApplyModifiedProperties();

        // Connecter le prefab GanzSe au PlayerSpawnService
        var ganzsePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GanzsePrefabPath);
        if (ganzsePrefab != null)
        {
            var spawnSO = new SerializedObject(spawnService);
            spawnSO.FindProperty("ganzsePrefab").objectReferenceValue = ganzsePrefab;
            spawnSO.ApplyModifiedProperties();
            Debug.Log("[MapGenSceneSetup] Prefab GanzSe connecte au PlayerSpawnService.");
        }
        else
        {
            Debug.LogWarning($"[MapGenSceneSetup] Prefab GanzSe non trouve: {GanzsePrefabPath}");
        }

        // === Sol de reference (default material, pas de Shader.Find) ===
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "ReferencePlane";
        ground.transform.position = new Vector3(90, -0.1f, 90);
        ground.transform.localScale = new Vector3(50, 1, 50);
        ground.isStatic = true;
        // Garder le Default-Material gris de Unity — jamais rose

        // === Sauvegarder ===
        string scenePath = "Assets/_Project/Scenes/MapGenDebug.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorSceneManager.OpenScene(scenePath);

        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
            EditorBuildSettings.scenes);
        if (!scenes.Exists(s => s.path == scenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        Debug.Log($"[MapGenSceneSetup] Scene MapGenDebug creee: {scenePath}");
        Debug.Log("[MapGenSceneSetup] F5=Generer, F10=Vue joueur, Tab=Sidebar");
    }
}
