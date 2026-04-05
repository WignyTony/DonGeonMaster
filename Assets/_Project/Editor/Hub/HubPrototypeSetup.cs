using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// STEP 1 — Sol + Markers uniquement.
/// Menu: DonGeonMaster > Creer Hub Prototype
/// </summary>
public class HubPrototypeSetup
{
    static readonly string Pack = "Assets/EmaceArt/Slavic World Free/Prefabs";
    static readonly string ScenePath = "Assets/_Project/Scenes/HubPrototype.unity";

    // Sol prefabs
    static readonly string CobbleA      = "Environment/Road/EA03_Environment_Road_Cobble_01a_PRE.prefab";
    static readonly string CobbleB      = "Environment/Road/EA03_Environment_Road_Cobble_01b_PRE.prefab";
    static readonly string CobbleCorner = "Environment/Road/EA03_Environment_Road_Cobble_Corner_01a_PRE.prefab";
    static readonly string WoodRoad     = "Environment/Road/EA03_Env_Road_Wooden_01d_PRE.prefab";
    static readonly string MudFlat      = "Environment/Mud/EA03_Env_Mud_Flat_01b_PRE.prefab";
    static readonly string MudFlatX     = "Environment/Mud/EA03_Env_Mud_Flat_01b_x_PRE.prefab";
    static readonly string SandFlat     = "Environment/Sand/EA03_Env_Sand_Flat_01a_PRE.prefab";
    static readonly string Platform     = "Environment/Road/EA03_Village_platform_01a_PRE.prefab";

    [MenuItem("DonGeonMaster/Creer Hub Prototype", false, 300)]
    public static void CreateHubPrototype()
    {
        // ═══ LOGGER START ═══
        HubBuildLogger.Begin(ScenePath, $"Pack: {Pack}");

        // ═══ LOAD PREFABS ═══
        var cobbleA = Load(CobbleA);
        var cobbleB = Load(CobbleB);
        var cobbleCorner = Load(CobbleCorner);
        var woodRoad = Load(WoodRoad);
        var mudFlat = Load(MudFlat);
        var mudFlatX = Load(MudFlatX);
        var sandFlat = Load(SandFlat);
        var platform = Load(Platform);

        if (cobbleA == null)
        {
            HubBuildLogger.Error("Cobble prefab not found — aborting");
            HubBuildLogger.End();
            return;
        }

        // ═══ NEW SCENE ═══
        HubBuildLogger.BeginZone("scene_setup", "Scene Setup");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.35f, 0.35f, 0.45f);
        RenderSettings.ambientEquatorColor = new Color(0.25f, 0.25f, 0.3f);
        RenderSettings.ambientGroundColor = new Color(0.15f, 0.12f, 0.1f);

        var sunGO = new GameObject("Directional Light");
        var sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.92f, 0.75f);
        sun.intensity = 1.2f;
        sun.shadows = LightShadows.Soft;
        sunGO.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        LogGO("scene_infrastructure", sunGO, null, "Directional light, warm tone", sunGO.transform);

        // Camera
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 50f;
        camGO.transform.position = new Vector3(0f, 40f, -35f);
        camGO.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
        LogGO("scene_infrastructure", camGO, null, "Overview camera", camGO.transform);

        // ═══ HIERARCHY ═══
        var hubRoot = new GameObject("HubLayout");
        var solRoot = new GameObject("Sol");
        solRoot.transform.SetParent(hubRoot.transform);
        var markersRoot = new GameObject("Markers");
        markersRoot.transform.SetParent(hubRoot.transform);

        // ═══ MARKERS ═══
        HubBuildLogger.BeginZone("markers", "Gameplay Markers");
        CreateMarker("PlayerSpawn",            new Vector3(0f,  0f, -18f), markersRoot.transform, Color.green,
            "Player arrival point, south gate");
        CreateMarker("DungeonEntranceMarker",  new Vector3(0f,  0f,  28f), markersRoot.transform, Color.red,
            "Dungeon entrance focal point, north");
        CreateMarker("MerchantAreaMarker",     new Vector3(-14f, 0f, 4f),  markersRoot.transform, Color.yellow,
            "Merchant zone anchor, west of plaza");
        CreateMarker("ForgeAreaMarker",        new Vector3(14f, 0f,  4f),  markersRoot.transform, new Color(1f, 0.5f, 0f),
            "Forge/upgrade zone anchor, east of plaza");

        // ═══ SOL — STEP 1 ═══
        float S = 4f;

        // ── 1. PLACE CENTRALE ──
        HubBuildLogger.BeginZone("central_plaza", "Central Plaza");
        var solCentre = new GameObject("Sol_PlaceCentrale");
        solCentre.transform.SetParent(solRoot.transform);

        for (int x = -2; x <= 2; x++)
        {
            for (int z = -1; z <= 2; z++)
            {
                var prefab = (x == 0 && z == 0) ? (cobbleB ?? cobbleA) : cobbleA;
                string note = (x == 0 && z == 0) ? "Central plaza anchor" : "Plaza cobble fill";
                PlaceTile(prefab, new Vector3(x * S, 0f, z * S), 0f, solCentre.transform,
                    $"Cobble_{x+2}_{z+1}", "cobble_plaza", note);
            }
        }
        if (cobbleCorner != null)
        {
            PlaceTile(cobbleCorner, new Vector3(-2*S, 0f, -1*S), 0f,   solCentre.transform, "Corner_SW", "corner_plaza", "SW corner frames plaza");
            PlaceTile(cobbleCorner, new Vector3( 2*S, 0f, -1*S), 90f,  solCentre.transform, "Corner_SE", "corner_plaza", "SE corner frames plaza");
            PlaceTile(cobbleCorner, new Vector3(-2*S, 0f,  2*S), -90f, solCentre.transform, "Corner_NW", "corner_plaza", "NW corner frames plaza");
            PlaceTile(cobbleCorner, new Vector3( 2*S, 0f,  2*S), 180f, solCentre.transform, "Corner_NE", "corner_plaza", "NE corner frames plaza");
        }

        // ── 2. AXE PRINCIPAL SUD ──
        HubBuildLogger.BeginZone("south_arrival", "South Arrival Path");
        var solSud = new GameObject("Sol_AxeSud");
        solSud.transform.SetParent(solRoot.transform);

        var axeSudPrefab = woodRoad ?? cobbleA;
        for (int z = -4; z <= -2; z++)
        {
            PlaceTile(axeSudPrefab, new Vector3(0f, 0f, z * S), 0f, solSud.transform,
                $"AxeSud_{z+5}", "road_arrival", "Main south approach path");
        }
        var terreSud = sandFlat ?? mudFlat;
        if (terreSud != null)
        {
            for (int z = -4; z <= -2; z++)
            {
                PlaceTile(terreSud, new Vector3(-S, 0f, z * S), 0f, solSud.transform,
                    $"TerreSud_L_{z+5}", "sand_flank", "Left flank, arrival zone");
                PlaceTile(terreSud, new Vector3( S, 0f, z * S), 0f, solSud.transform,
                    $"TerreSud_R_{z+5}", "sand_flank", "Right flank, arrival zone");
            }
        }

        // ── 3. AXE PRINCIPAL NORD ──
        HubBuildLogger.BeginZone("north_approach", "North Approach");
        var solNord = new GameObject("Sol_AxeNord");
        solNord.transform.SetParent(solRoot.transform);

        for (int z = 3; z <= 4; z++)
        {
            PlaceTile(cobbleA, new Vector3(0f, 0f, z * S), 0f, solNord.transform,
                $"NordCobble_{z-3}", "cobble_transition", "Cobble transition plaza to dungeon");
        }

        // ── 4. ZONE DONJON (terre) ──
        HubBuildLogger.BeginZone("dungeon_entrance", "Dungeon Entrance Ground");
        var terreDonjon = mudFlat ?? sandFlat;
        if (terreDonjon != null)
        {
            for (int z = 5; z <= 7; z++)
            {
                PlaceTile(terreDonjon, new Vector3(0f, 0f, z * S), 0f, solNord.transform,
                    $"DonjonTerre_{z-5}", "mud_dungeon", "Dark ground approaching dungeon");
                PlaceTile(terreDonjon, new Vector3(-S, 0f, z * S), 0f, solNord.transform,
                    $"DonjonTerre_L_{z-5}", "mud_dungeon", "Left dungeon approach flank");
                PlaceTile(terreDonjon, new Vector3( S, 0f, z * S), 0f, solNord.transform,
                    $"DonjonTerre_R_{z-5}", "mud_dungeon", "Right dungeon approach flank");
            }
        }

        // ── 5. ALLÉE MARCHAND OUEST ──
        HubBuildLogger.BeginZone("merchant_area", "Merchant Area");
        var solMarchand = new GameObject("Sol_AlleeMarchand");
        solMarchand.transform.SetParent(solRoot.transform);

        var alleePrefab = woodRoad ?? cobbleA;
        for (int x = -3; x >= -4; x--)
        {
            PlaceTile(alleePrefab, new Vector3(x * S, 0f, S), 90f, solMarchand.transform,
                $"AlleeMarchand_{x+5}", "road_merchant", "Wooden path to merchant zone");
        }
        if (terreSud != null)
        {
            PlaceTile(terreSud, new Vector3(-3*S, 0f, 0f), 0f, solMarchand.transform, "ZoneMarchand_0", "sand_merchant", "Merchant zone ground SW");
            PlaceTile(terreSud, new Vector3(-4*S, 0f, 0f), 0f, solMarchand.transform, "ZoneMarchand_1", "sand_merchant", "Merchant zone ground NW");
            PlaceTile(terreSud, new Vector3(-3*S, 0f, 2*S), 0f, solMarchand.transform, "ZoneMarchand_2", "sand_merchant", "Merchant zone ground SE");
            PlaceTile(terreSud, new Vector3(-4*S, 0f, 2*S), 0f, solMarchand.transform, "ZoneMarchand_3", "sand_merchant", "Merchant zone ground NE");
        }

        // ── 6. ALLÉE FORGE EST ──
        HubBuildLogger.BeginZone("forge_area", "Forge Area");
        var solForge = new GameObject("Sol_AlleeForge");
        solForge.transform.SetParent(solRoot.transform);

        for (int x = 3; x <= 4; x++)
        {
            PlaceTile(alleePrefab, new Vector3(x * S, 0f, S), 90f, solForge.transform,
                $"AlleeForge_{x-3}", "road_forge", "Wooden path to forge zone");
        }
        var mudForge = mudFlatX ?? mudFlat ?? sandFlat;
        if (mudForge != null)
        {
            PlaceTile(mudForge, new Vector3(3*S, 0f, 0f), 0f, solForge.transform, "ZoneForge_0", "mud_forge", "Forge zone ground SW");
            PlaceTile(mudForge, new Vector3(4*S, 0f, 0f), 0f, solForge.transform, "ZoneForge_1", "mud_forge", "Forge zone ground NW");
            PlaceTile(mudForge, new Vector3(3*S, 0f, 2*S), 0f, solForge.transform, "ZoneForge_2", "mud_forge", "Forge zone ground SE");
            PlaceTile(mudForge, new Vector3(4*S, 0f, 2*S), 0f, solForge.transform, "ZoneForge_3", "mud_forge", "Forge zone ground NE");
        }

        // ── 7. BASE GROUND ──
        HubBuildLogger.BeginZone("base_ground", "Base Ground Plane");
        var groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        groundPlane.name = "BaseGround";
        groundPlane.transform.SetParent(solRoot.transform);
        groundPlane.transform.position = new Vector3(0f, -0.05f, 5f);
        groundPlane.transform.localScale = new Vector3(5f, 1f, 6f);
        groundPlane.isStatic = true;
        var groundMr = groundPlane.GetComponent<MeshRenderer>();
        var groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        groundMat.SetColor("_BaseColor", new Color(0.35f, 0.28f, 0.2f));
        groundMat.name = "HubGroundBase";
        groundMr.sharedMaterial = groundMat;
        LogGO("ground_base", groundPlane, null, "Earth-colored base fills gaps", solRoot.transform);

        // ═══ SAVE ═══
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        // ═══ LOGGER END ═══
        HubBuildLogger.End();
    }

    // ═══ HELPERS ═══

    static GameObject Load(string rel)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Pack}/{rel}");
        if (prefab == null) HubBuildLogger.Warning($"Prefab not found: {rel}");
        return prefab;
    }

    static void PlaceTile(GameObject prefab, Vector3 pos, float rotY, Transform parent,
        string name, string role, string note)
    {
        if (prefab == null) return;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        go.isStatic = true;
        if (name != null) go.name = name;

        string assetPath = AssetDatabase.GetAssetPath(prefab);
        HubBuildLogger.LogCreate(role, go.name, prefab.name, assetPath,
            go.transform.position, go.transform.rotation.eulerAngles, go.transform.localScale,
            parent.name, note);
    }

    static void LogGO(string role, GameObject go, GameObject prefab, string note, Transform parent)
    {
        string assetPath = prefab != null ? AssetDatabase.GetAssetPath(prefab) : "";
        string prefabName = prefab != null ? prefab.name : "(runtime)";
        HubBuildLogger.LogCreate(role, go.name, prefabName, assetPath,
            go.transform.position, go.transform.rotation.eulerAngles, go.transform.localScale,
            parent != null ? parent.name : "(root)", note);
    }

    static void CreateMarker(string name, Vector3 pos, Transform parent, Color color, string note)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        var vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vis.name = "Visual";
        vis.transform.SetParent(go.transform);
        vis.transform.localPosition = Vector3.up * 2f;
        vis.transform.localScale = new Vector3(0.6f, 4f, 0.6f);

        var mr = vis.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", color);
        mat.name = $"Marker_{name}";
        mr.sharedMaterial = mat;

        var col = vis.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);

        bool valid = name == "PlayerSpawn" || name == "DungeonEntranceMarker" ||
                     name == "MerchantAreaMarker" || name == "ForgeAreaMarker";
        HubBuildLogger.LogMarker(name, pos, Vector3.zero, vis.name, valid);
    }
}
