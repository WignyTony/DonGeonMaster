using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using DonGeonMaster.Hub;

/// <summary>
/// Cree la scene HubPrototype et le SlavicHubCatalog.asset.
/// Menu: DonGeonMaster > Creer Hub Prototype
/// </summary>
public class HubPrototypeSetup
{
    static readonly string PackPrefabs = "Assets/EmaceArt/Slavic World Free/Prefabs";
    static readonly string CatalogPath = "Assets/_Project/Configs/Hub/SlavicHubCatalog.asset";
    static readonly string ScenePath = "Assets/_Project/Scenes/HubPrototype.unity";

    // ── Prefab selection (exactly what the plan specifies) ──

    static readonly string[] BuildingPaths = {
        "Town/Building/EA03_Town_House_Comp_01a_PRE.prefab",
        "Town/Building/EA03_Town_House_Comp_02a_PRE.prefab",
        "Town/Administrative/EA03_Town_Building_Administrative _01a_PRE.prefab",
        "Village/OutBuilding/EA03_Village_OutBuilding_Shed_01a_PRE.prefab",
    };

    static readonly string[] PropPaths = {
        "Prop/Container/EA03_Prop_Container_Barrel_01d_PRE.prefab",
        "Prop/Container/EA03_Prop_Container_Crate_01a_PRE.prefab",
        "Prop/Furniture/EA03_Prop_Town_Bench_01a_PRE.prefab",
        "Prop/Sign/EA03_Prop_Sign_Chapel_01_PRE.prefab",
        "Prop/Village/EA03_Prop_Stand_Sheet_01a_PRE.prefab",
        "Environment/Stairs/EA03_Village_step_platform_Stair_R_03a_PRE.prefab",
    };

    static readonly string[] EnvironmentPaths = {
        "Environment/Road/EA03_Environment_Road_Cobble_01a_PRE.prefab",
        "Environment/Road/EA03_Environment_Road_Cobble_01b_PRE.prefab",
        "Environment/Road/EA03_Environment_Road_Cobble_Corner_01a_PRE.prefab",
        "Fence/Plank2/EA03_Fence_Plank_02a_PRE.prefab",
        "Fence/Plank2/EA03_Fence_Plank_02b_PRE.prefab",
        "Fence/Plank2/EA03_Fence_Plank_02c_PRE.prefab",
        "Fence/Plank2/EA03_Fence_Plank_02d_PRE.prefab",
        "Fence/Wall/EA03_Fence_WallGate_01a_PRE.prefab",
        "Fence/RockGate/EA03_Fence_RockGate_01a_PRE.prefab",
        "Environment/Rock/EA03_Environment_Rock_Big_Head_01a_PRE.prefab",
        "Environment/Rock/EA03_Environment_Rock_Flat_04c_PRE.prefab",
    };

    static readonly string[] NaturePaths = {
        "Nature/Tree/EA03_Nature_Tree_01b_PRE.prefab",
        "Nature/Tree/EA03_Nature_Tree_02b_PRE.prefab",
        "Nature/Tree/EA03_Nature_Tree_03b_PRE.prefab",
        "Nature/Bushes/EA03_Nature_Bush_01a_PRE.prefab",
        "Nature/Bushes/EA03_Nature_Bush_02a_PRE.prefab",
        "Nature/Bushes/EA03_Nature_Bush_03a_PRE.prefab",
    };

    [MenuItem("DonGeonMaster/Creer Hub Prototype", false, 300)]
    public static void CreateHubPrototype()
    {
        // ═══ CATALOG ═══
        Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath));

        var catalog = ScriptableObject.CreateInstance<HubAssetCatalog>();
        catalog.buildings = LoadPrefabs(BuildingPaths);
        catalog.props = LoadPrefabs(PropPaths);
        catalog.environment = LoadPrefabs(EnvironmentPaths);
        catalog.nature = LoadPrefabs(NaturePaths);

        AssetDatabase.CreateAsset(catalog, CatalogPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[HubPrototypeSetup] Catalog: {catalog.buildings.Count}B {catalog.props.Count}P {catalog.environment.Count}E {catalog.nature.Count}N");

        // ═══ SCENE ═══
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // -- Lighting --
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.35f, 0.35f, 0.45f);
        RenderSettings.ambientEquatorColor = new Color(0.25f, 0.25f, 0.3f);
        RenderSettings.ambientGroundColor = new Color(0.15f, 0.12f, 0.1f);

        var sunGO = new GameObject("Directional Light");
        var sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.92f, 0.75f);
        sun.intensity = 1.2f;
        sunGO.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

        // -- Ground plane --
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "GroundPlane";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(8f, 1f, 8f);
        ground.isStatic = true;

        // -- Hierarchy containers --
        var hubRoot = new GameObject("HubLayout");
        var buildingsRoot = new GameObject("Buildings");
        buildingsRoot.transform.SetParent(hubRoot.transform);
        var propsRoot = new GameObject("Props");
        propsRoot.transform.SetParent(hubRoot.transform);
        var envRoot = new GameObject("Environment");
        envRoot.transform.SetParent(hubRoot.transform);
        var natureRoot = new GameObject("Nature");
        natureRoot.transform.SetParent(hubRoot.transform);
        var markersRoot = new GameObject("Markers");
        markersRoot.transform.SetParent(hubRoot.transform);

        // ── GAMEPLAY MARKERS ──
        CreateMarker("PlayerSpawn", new Vector3(0f, 0f, -15f), markersRoot.transform);
        CreateMarker("DungeonEntranceMarker", new Vector3(0f, 0f, 25f), markersRoot.transform);
        CreateMarker("MerchantAreaMarker", new Vector3(-12f, 0f, 5f), markersRoot.transform);
        CreateMarker("ForgeAreaMarker", new Vector3(12f, 0f, 5f), markersRoot.transform);

        // ── BUILDINGS ──
        // Marchand (west side)
        Place(catalog.buildings, 0, new Vector3(-12f, 0f, 5f), 90f, buildingsRoot.transform, "Marchand_Building");
        // Forge (east side)
        Place(catalog.buildings, 3, new Vector3(12f, 0f, 5f), -90f, buildingsRoot.transform, "Forge_Building");
        // Central (administrative)
        Place(catalog.buildings, 2, new Vector3(0f, 0f, 12f), 180f, buildingsRoot.transform, "Central_Building");
        // Ambiance (second house, south-west)
        Place(catalog.buildings, 1, new Vector3(-10f, 0f, -8f), 45f, buildingsRoot.transform, "Ambiance_Building");

        // ── ROADS (place centrale) ──
        float roadSpacing = 4f;
        int roadIdx_a = FindIdx(catalog.environment, "Cobble_01a");
        int roadIdx_b = FindIdx(catalog.environment, "Cobble_01b");
        int roadIdx_corner = FindIdx(catalog.environment, "Corner");
        if (roadIdx_a >= 0)
        {
            // Central grid 3x3
            for (int rx = -1; rx <= 1; rx++)
                for (int rz = -1; rz <= 1; rz++)
                {
                    int idx = (rx == 0 && rz == 0) ? roadIdx_b : roadIdx_a;
                    if (idx < 0) idx = roadIdx_a;
                    Place(catalog.environment, idx, new Vector3(rx * roadSpacing, 0f, rz * roadSpacing), 0f, envRoot.transform);
                }
        }

        // ── FENCES (perimeter) ──
        int fenceA = FindIdx(catalog.environment, "Plank_02a");
        int fenceB = FindIdx(catalog.environment, "Plank_02b");
        int fenceC = FindIdx(catalog.environment, "Plank_02c");
        int gate = FindIdx(catalog.environment, "WallGate");
        float fenceSpacing = 5f;

        // South fence (with gate at center)
        if (fenceA >= 0)
        {
            Place(catalog.environment, fenceA, new Vector3(-15f, 0f, -18f), 0f, envRoot.transform);
            Place(catalog.environment, fenceB >= 0 ? fenceB : fenceA, new Vector3(-10f, 0f, -18f), 0f, envRoot.transform);
            Place(catalog.environment, fenceC >= 0 ? fenceC : fenceA, new Vector3(-5f, 0f, -18f), 0f, envRoot.transform);
            if (gate >= 0) Place(catalog.environment, gate, new Vector3(0f, 0f, -18f), 0f, envRoot.transform, "SouthGate");
            Place(catalog.environment, fenceA, new Vector3(5f, 0f, -18f), 0f, envRoot.transform);
            Place(catalog.environment, fenceB >= 0 ? fenceB : fenceA, new Vector3(10f, 0f, -18f), 0f, envRoot.transform);
            Place(catalog.environment, fenceA, new Vector3(15f, 0f, -18f), 0f, envRoot.transform);
        }

        // West fence
        if (fenceA >= 0)
            for (int i = -3; i <= 3; i++)
                Place(catalog.environment, fenceA, new Vector3(-18f, 0f, i * fenceSpacing), 90f, envRoot.transform);

        // East fence
        if (fenceA >= 0)
            for (int i = -3; i <= 3; i++)
                Place(catalog.environment, fenceA, new Vector3(18f, 0f, i * fenceSpacing), 90f, envRoot.transform);

        // ── DUNGEON ENTRANCE (north) ──
        int rockBig = FindIdx(catalog.environment, "Rock_Big_Head");
        int rockFlat = FindIdx(catalog.environment, "Rock_Flat");
        int rockGate = FindIdx(catalog.environment, "RockGate");
        int stairs = FindIdx(catalog.props, "Stair");

        if (rockBig >= 0) Place(catalog.environment, rockBig, new Vector3(-4f, 0f, 26f), 0f, envRoot.transform, "DungeonRock_L");
        if (rockBig >= 0) Place(catalog.environment, rockBig, new Vector3(4f, 0f, 26f), 180f, envRoot.transform, "DungeonRock_R");
        if (rockFlat >= 0) Place(catalog.environment, rockFlat, new Vector3(0f, 0f, 28f), 0f, envRoot.transform, "DungeonRock_Back");
        if (rockGate >= 0) Place(catalog.environment, rockGate, new Vector3(0f, 0f, 24f), 0f, envRoot.transform, "DungeonGate");
        if (stairs >= 0) Place(catalog.props, stairs, new Vector3(0f, 0f, 22f), 0f, propsRoot.transform, "DungeonStairs");

        // ── PROPS ──
        int barrel = FindIdx(catalog.props, "Barrel");
        int crate = FindIdx(catalog.props, "Crate_01a");
        int bench = FindIdx(catalog.props, "Bench");
        int sign = FindIdx(catalog.props, "Sign");
        int stand = FindIdx(catalog.props, "Stand");

        if (barrel >= 0) Place(catalog.props, barrel, new Vector3(-3f, 0f, 3f), 0f, propsRoot.transform);
        if (barrel >= 0) Place(catalog.props, barrel, new Vector3(-3.5f, 0f, 2.5f), 30f, propsRoot.transform);
        if (crate >= 0) Place(catalog.props, crate, new Vector3(-2.5f, 0f, 3.5f), 15f, propsRoot.transform);
        if (bench >= 0) Place(catalog.props, bench, new Vector3(4f, 0f, -2f), 90f, propsRoot.transform, "PlaceBench");
        if (sign >= 0) Place(catalog.props, sign, new Vector3(0f, 0f, -12f), 0f, propsRoot.transform, "VillageSign");
        if (stand >= 0) Place(catalog.props, stand, new Vector3(-10f, 0f, 2f), 90f, propsRoot.transform, "MerchantStand");
        if (crate >= 0) Place(catalog.props, crate, new Vector3(11f, 0f, 3f), 45f, propsRoot.transform, "ForgeCrate");

        // ── NATURE ──
        if (catalog.nature.Count >= 3)
        {
            Place(catalog.nature, 0, new Vector3(-16f, 0f, 10f), 0f, natureRoot.transform);
            Place(catalog.nature, 1, new Vector3(16f, 0f, 12f), 120f, natureRoot.transform);
            Place(catalog.nature, 2, new Vector3(-14f, 0f, -12f), 240f, natureRoot.transform);
        }
        if (catalog.nature.Count >= 6)
        {
            Place(catalog.nature, 3, new Vector3(-8f, 0f, 15f), 0f, natureRoot.transform);
            Place(catalog.nature, 4, new Vector3(8f, 0f, -10f), 60f, natureRoot.transform);
            Place(catalog.nature, 5, new Vector3(14f, 0f, -5f), 180f, natureRoot.transform);
        }

        // ── CAMERA ──
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 50f;
        camGO.transform.position = new Vector3(0f, 25f, -30f);
        camGO.transform.rotation = Quaternion.Euler(40f, 0f, 0f);

        // ── SAVE SCENE ──
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        Debug.Log($"[HubPrototypeSetup] HubPrototype scene created at {ScenePath}");
        Debug.Log($"[HubPrototypeSetup] Markers: PlayerSpawn, DungeonEntranceMarker, MerchantAreaMarker, ForgeAreaMarker");
    }

    static List<GameObject> LoadPrefabs(string[] relativePaths)
    {
        var list = new List<GameObject>();
        foreach (var rel in relativePaths)
        {
            string full = $"{PackPrefabs}/{rel}";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(full);
            if (prefab != null)
                list.Add(prefab);
            else
                Debug.LogWarning($"[HubPrototypeSetup] Prefab not found: {full}");
        }
        return list;
    }

    static void CreateMarker(string name, Vector3 pos, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        // Visual indicator (small colored cube for scene view)
        var vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vis.name = "Visual";
        vis.transform.SetParent(go.transform);
        vis.transform.localPosition = Vector3.up * 1.5f;
        vis.transform.localScale = new Vector3(0.5f, 3f, 0.5f);

        var mr = vis.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        Color c;
        switch (name)
        {
            case "PlayerSpawn": c = Color.green; break;
            case "DungeonEntranceMarker": c = Color.red; break;
            case "MerchantAreaMarker": c = Color.yellow; break;
            case "ForgeAreaMarker": c = new Color(1f, 0.5f, 0f); break;
            default: c = Color.white; break;
        }
        mat.SetColor("_BaseColor", c);
        mat.name = $"Marker_{name}";
        mr.sharedMaterial = mat;

        // Remove collider from marker visual
        var col = vis.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);
    }

    static GameObject Place(List<GameObject> list, int idx, Vector3 pos, float rotY, Transform parent, string rename = null)
    {
        if (idx < 0 || idx >= list.Count || list[idx] == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(list[idx]);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        if (rename != null) go.name = rename;
        return go;
    }

    static int FindIdx(List<GameObject> list, string partialName)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] != null && list[i].name.Contains(partialName))
                return i;
        return -1;
    }
}
