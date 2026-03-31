using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
using DonGeonMaster.MapGeneration;
using DonGeonMaster.MapGeneration.DebugTools;

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
        cam.backgroundColor = new Color(0.06f, 0.06f, 0.10f);
        cam.orthographic = true;
        cam.orthographicSize = 60;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 500f;
        camGo.transform.position = new Vector3(90, 120, 90);
        camGo.transform.rotation = Quaternion.Euler(90, 0, 0);
        camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;

        // === Lumiere ===
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.color = new Color(1f, 0.95f, 0.9f);
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
        lightGo.AddComponent<UniversalAdditionalLightData>();

        // === Controller principal ===
        var ctrlGo = new GameObject("MapGenDebugSystem");
        var ctrl = ctrlGo.AddComponent<MapGenDebugModeController>();
        var heroBridge = ctrlGo.AddComponent<HeroDebugBridge>();

        // Connecter AssetCategoryRegistry
        var ctrlSO = new SerializedObject(ctrl);
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

        // Connecter le prefab GanzSe au HeroDebugBridge
        var ganzsePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GanzsePrefabPath);
        if (ganzsePrefab != null)
        {
            var heroSO = new SerializedObject(heroBridge);
            heroSO.FindProperty("ganzsePrefab").objectReferenceValue = ganzsePrefab;
            heroSO.ApplyModifiedProperties();
            Debug.Log("[MapGenSceneSetup] Prefab GanzSe connecte au HeroDebugBridge.");
        }
        else
        {
            Debug.LogWarning($"[MapGenSceneSetup] Prefab GanzSe non trouve: {GanzsePrefabPath}");
        }

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
        Debug.Log("[MapGenSceneSetup] F5=Generer, Tab=Sidebar, F10=Heros");
    }
}
