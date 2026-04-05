using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using F = HubBuildLogger.Family;

/// <summary>
/// STEP 1 — Fondations / emprise / circulation / pads reserves.
/// Pas de batiments, palissades, props ni nature.
/// Menu: DonGeonMaster > Creer Hub Prototype
/// </summary>
public class HubPrototypeSetup
{
    static readonly string P = "Assets/EmaceArt/Slavic World Free/Prefabs";
    static readonly string ScenePath = "Assets/_Project/Scenes/HubPrototype.unity";

    // ═══ LAYOUT CONSTANTS ═══
    // All distances in world units. Tile spacing S=4.
    const float S = 4f;

    // Village footprint: X [-22, 22] Z [-22, 32] => 44 wide, 54 deep
    const float VILLAGE_WEST = -22f, VILLAGE_EAST = 22f;
    const float VILLAGE_SOUTH = -22f, VILLAGE_NORTH = 32f;

    // Central plaza: 5 tiles wide (X -8..+8), 4 tiles deep (Z -4..+8)
    const float PLAZA_WEST = -8f, PLAZA_EAST = 8f;
    const float PLAZA_SOUTH = -4f, PLAZA_NORTH = 8f;

    // South arrival axis: Z -20 to plaza south (-4), width 3 tiles (X -4..+4)
    const float SOUTH_AXIS_START = -20f; // player spawn area
    const float SOUTH_AXIS_END = -4f;    // meets plaza

    // North dungeon axis: Z +8 (plaza north) to +30
    const float NORTH_AXIS_START = 8f;
    const float NORTH_AXIS_END = 30f;

    // Pads (reserved zones for future buildings, NOT placed yet)
    // Merchant pad: west of plaza
    static readonly Vector3 MERCHANT_PAD_CENTER = new(-14f, 0f, 2f);
    static readonly Vector3 MERCHANT_PAD_SIZE = new(10f, 0f, 10f); // 10x10 area

    // Forge pad: east of plaza
    static readonly Vector3 FORGE_PAD_CENTER = new(14f, 0f, 2f);
    static readonly Vector3 FORGE_PAD_SIZE = new(10f, 0f, 10f);

    // Main hall pad: north of plaza, before dungeon axis
    static readonly Vector3 MAINHALL_PAD_CENTER = new(0f, 0f, 12f);
    static readonly Vector3 MAINHALL_PAD_SIZE = new(12f, 0f, 8f);

    // Ambiance building pad: south-east
    static readonly Vector3 AMBIANCE_PAD_CENTER = new(12f, 0f, -12f);
    static readonly Vector3 AMBIANCE_PAD_SIZE = new(8f, 0f, 8f);

    [MenuItem("DonGeonMaster/Creer Hub Prototype", false, 300)]
    public static void CreateHubPrototype()
    {
        HubBuildLogger.Begin(ScenePath, $"Pack: {P} | STEP 1: Fondations only");

        // ═══ LOAD SOL PREFABS ONLY ═══
        var cobbleA = LD("Environment/Road/EA03_Environment_Road_Cobble_01a_PRE.prefab");
        var cobbleB = LD("Environment/Road/EA03_Environment_Road_Cobble_01b_PRE.prefab");
        var cobbleCorner = LD("Environment/Road/EA03_Environment_Road_Cobble_Corner_01a_PRE.prefab");
        var woodRoad = LD("Environment/Road/EA03_Env_Road_Wooden_01d_PRE.prefab");
        var mudFlat = LD("Environment/Mud/EA03_Env_Mud_Flat_01b_PRE.prefab");
        var mudFlatX = LD("Environment/Mud/EA03_Env_Mud_Flat_01b_x_PRE.prefab");
        var sandFlat = LD("Environment/Sand/EA03_Env_Sand_Flat_01a_PRE.prefab");

        if (cobbleA == null) {
            HubBuildLogger.Error("Cobble prefab not found — aborting");
            HubBuildLogger.End(); return;
        }

        // ═══ NEW SCENE ═══
        HubBuildLogger.SetPhase("ground_foundation");
        HubBuildLogger.BeginZone("scene_setup", "Scene Setup");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

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
        LogRT("scene_light", F.Helper, sunGO, "Warm directional light");

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 50f;
        camGO.transform.position = new Vector3(0f, 55f, -20f);
        camGO.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
        LogRT("scene_camera", F.Helper, camGO, "Top-down overview for step 1 validation");

        // ═══ HIERARCHY (step 1 only has Sol + Markers + Pads) ═══
        var hub = new GameObject("HubLayout");
        var solRoot = Ch("Sol", hub);
        var padRoot = Ch("Pads", hub);
        var mrkRoot = Ch("Markers", hub);

        // ═══ MARKERS ═══
        HubBuildLogger.SetPhase("gameplay_markers");
        HubBuildLogger.BeginZone("markers", "Gameplay Markers");
        MkMarker("PlayerSpawn",           new Vector3(0, 0, -18f), mrkRoot, Color.green,        "Player arrival south");
        MkMarker("DungeonEntranceMarker", new Vector3(0, 0,  28f), mrkRoot, Color.red,          "Dungeon focal north");
        MkMarker("MerchantAreaMarker",    MERCHANT_PAD_CENTER,     mrkRoot, Color.yellow,        "Merchant zone center");
        MkMarker("ForgeAreaMarker",       FORGE_PAD_CENTER,        mrkRoot, new Color(1,.5f,0),  "Forge zone center");

        // ═══ PADS (visual placeholders for future buildings) ═══
        HubBuildLogger.SetPhase("ground_foundation");
        HubBuildLogger.BeginZone("pads", "Reserved Building Pads");
        MkPad("MerchantPad", MERCHANT_PAD_CENTER, MERCHANT_PAD_SIZE, new Color(1f, 0.9f, 0.3f, 0.3f), padRoot, "Reserved for merchant building (STEP 2+)");
        MkPad("ForgePad",    FORGE_PAD_CENTER,    FORGE_PAD_SIZE,    new Color(1f, 0.5f, 0.1f, 0.3f), padRoot, "Reserved for forge building (STEP 2+)");
        MkPad("MainHallPad", MAINHALL_PAD_CENTER, MAINHALL_PAD_SIZE, new Color(0.5f, 0.5f, 1f, 0.3f), padRoot, "Reserved for main hall (STEP 2+)");
        MkPad("AmbiancePad", AMBIANCE_PAD_CENTER, AMBIANCE_PAD_SIZE, new Color(0.5f, 1f, 0.5f, 0.3f), padRoot, "Reserved for tavern/ambiance (STEP 2+)");

        // ═══ SOL ═══

        // ── CENTRAL PLAZA ──
        HubBuildLogger.BeginZone("central_plaza_foundation", "Central Plaza Foundation");
        var solPlaza = Ch("Sol_PlaceCentrale", solRoot);
        for (int x = -2; x <= 2; x++)
            for (int z = -1; z <= 2; z++) {
                var pf = (x == 0 && z == 0) ? (cobbleB ?? cobbleA) : cobbleA;
                Tile(pf, new Vector3(x*S, 0, z*S), 0, solPlaza,
                    $"Cobble_{x+2}_{z+1}", "cobble_plaza", F.Environment,
                    (x==0 && z==0) ? "Central plaza anchor" : "Plaza cobble fill");
            }
        if (cobbleCorner != null) {
            Tile(cobbleCorner, new Vector3(-2*S, 0, -1*S), 0,   solPlaza, "Corner_SW", "corner_plaza", F.Environment, "SW corner frames plaza");
            Tile(cobbleCorner, new Vector3( 2*S, 0, -1*S), 90,  solPlaza, "Corner_SE", "corner_plaza", F.Environment, "SE corner frames plaza");
            Tile(cobbleCorner, new Vector3(-2*S, 0,  2*S), -90, solPlaza, "Corner_NW", "corner_plaza", F.Environment, "NW corner frames plaza");
            Tile(cobbleCorner, new Vector3( 2*S, 0,  2*S), 180, solPlaza, "Corner_NE", "corner_plaza", F.Environment, "NE corner frames plaza");
        }

        // ── SOUTH ARRIVAL AXIS ──
        HubBuildLogger.BeginZone("south_arrival_axis", "South Arrival Axis");
        var solSud = Ch("Sol_AxeSud", solRoot);
        var roadS = woodRoad ?? cobbleA;
        for (int z = -5; z <= -2; z++)
            Tile(roadS, new Vector3(0, 0, z*S), 0, solSud,
                $"AxeSud_{z+6}", "road_south_main", F.Environment, "Main south approach");
        var sand = sandFlat ?? mudFlat;
        if (sand != null)
            for (int z = -5; z <= -2; z++) {
                Tile(sand, new Vector3(-S, 0, z*S), 0, solSud, $"TerreSud_L_{z+6}", "sand_south_flank", F.Environment, "Left dirt flank, south approach");
                Tile(sand, new Vector3( S, 0, z*S), 0, solSud, $"TerreSud_R_{z+6}", "sand_south_flank", F.Environment, "Right dirt flank, south approach");
            }

        // ── MERCHANT ALLEY (west from plaza) ──
        HubBuildLogger.BeginZone("merchant_pad", "Merchant Zone Ground");
        var solM = Ch("Sol_AlleeMarchand", solRoot);
        var roadW = woodRoad ?? cobbleA;
        for (int x = -3; x >= -4; x--)
            Tile(roadW, new Vector3(x*S, 0, S), 90, solM,
                $"AlleeMarchand_{x+5}", "road_merchant_path", F.Environment, "Wooden path plaza to merchant");
        if (sand != null) {
            Tile(sand, new Vector3(-3*S, 0, 0),   0, solM, "MerchantGround_0", "sand_merchant", F.Environment, "Merchant zone ground");
            Tile(sand, new Vector3(-4*S, 0, 0),   0, solM, "MerchantGround_1", "sand_merchant", F.Environment, "Merchant zone ground");
            Tile(sand, new Vector3(-3*S, 0, 2*S), 0, solM, "MerchantGround_2", "sand_merchant", F.Environment, "Merchant zone ground");
            Tile(sand, new Vector3(-4*S, 0, 2*S), 0, solM, "MerchantGround_3", "sand_merchant", F.Environment, "Merchant zone ground");
        }

        // ── FORGE ALLEY (east from plaza) ──
        HubBuildLogger.BeginZone("forge_pad", "Forge Zone Ground");
        var solF = Ch("Sol_AlleeForge", solRoot);
        for (int x = 3; x <= 4; x++)
            Tile(roadW, new Vector3(x*S, 0, S), 90, solF,
                $"AlleeForge_{x-3}", "road_forge_path", F.Environment, "Wooden path plaza to forge");
        var mudF = mudFlatX ?? mudFlat ?? sandFlat;
        if (mudF != null) {
            Tile(mudF, new Vector3(3*S, 0, 0),   0, solF, "ForgeGround_0", "mud_forge", F.Environment, "Forge zone ground");
            Tile(mudF, new Vector3(4*S, 0, 0),   0, solF, "ForgeGround_1", "mud_forge", F.Environment, "Forge zone ground");
            Tile(mudF, new Vector3(3*S, 0, 2*S), 0, solF, "ForgeGround_2", "mud_forge", F.Environment, "Forge zone ground");
            Tile(mudF, new Vector3(4*S, 0, 2*S), 0, solF, "ForgeGround_3", "mud_forge", F.Environment, "Forge zone ground");
        }

        // ── NORTH DUNGEON AXIS ──
        HubBuildLogger.BeginZone("north_dungeon_axis", "North Dungeon Axis");
        var solNord = Ch("Sol_AxeNord", solRoot);
        // Cobble transition from plaza
        for (int z = 3; z <= 4; z++)
            Tile(cobbleA, new Vector3(0, 0, z*S), 0, solNord,
                $"NordCobble_{z-3}", "cobble_transition", F.Environment, "Cobble transition toward dungeon");
        // Mud approach to dungeon
        var mudD = mudFlat ?? sandFlat;
        if (mudD != null)
            for (int z = 5; z <= 7; z++) {
                Tile(mudD, new Vector3( 0, 0, z*S), 0, solNord, $"DonjonGround_{z-5}",   "mud_dungeon_main", F.Environment, "Dark ground approaching dungeon");
                Tile(mudD, new Vector3(-S, 0, z*S), 0, solNord, $"DonjonGround_L_{z-5}", "mud_dungeon_flank", F.Environment, "Left dungeon flank");
                Tile(mudD, new Vector3( S, 0, z*S), 0, solNord, $"DonjonGround_R_{z-5}", "mud_dungeon_flank", F.Environment, "Right dungeon flank");
            }

        // ── MAINHALL PAD GROUND ──
        HubBuildLogger.BeginZone("mainhall_pad", "Main Hall Zone Ground");
        var solHall = Ch("Sol_MainHallZone", solRoot);
        // Cobble platform for future main hall
        for (int x = -1; x <= 1; x++)
            Tile(cobbleA, new Vector3(x*S, 0, 3*S), 0, solHall,
                $"HallGround_{x+1}", "cobble_mainhall", F.Environment, "Main hall zone foundation");

        // ── BASE GROUND PLANE ──
        HubBuildLogger.BeginZone("village_footprint", "Village Footprint");
        var gp = GameObject.CreatePrimitive(PrimitiveType.Plane);
        gp.name = "BaseGround";
        gp.transform.SetParent(solRoot.transform);
        // Plane covers village footprint: center at (0, -0.05, 5), scale to cover X[-22,22] Z[-22,32]
        gp.transform.position = new Vector3(0f, -0.05f, 5f);
        gp.transform.localScale = new Vector3(4.4f, 1f, 5.4f); // Plane is 10x10, so scale 4.4 = 44 units
        gp.isStatic = true;
        var gMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        gMat.SetColor("_BaseColor", new Color(0.35f, 0.28f, 0.2f));
        gMat.name = "HubGroundBase";
        gp.GetComponent<MeshRenderer>().sharedMaterial = gMat;
        LogRT("ground_base_plane", F.Helper, gp, $"Village footprint base: X[{VILLAGE_WEST},{VILLAGE_EAST}] Z[{VILLAGE_SOUTH},{VILLAGE_NORTH}]");

        // ── FOOTPRINT EDGE MARKERS (corner cubes to visualize village bounds) ──
        var fpRoot = Ch("FootprintEdges", solRoot);
        MkEdge("FP_SW", new Vector3(VILLAGE_WEST, 0, VILLAGE_SOUTH), fpRoot);
        MkEdge("FP_SE", new Vector3(VILLAGE_EAST, 0, VILLAGE_SOUTH), fpRoot);
        MkEdge("FP_NW", new Vector3(VILLAGE_WEST, 0, VILLAGE_NORTH), fpRoot);
        MkEdge("FP_NE", new Vector3(VILLAGE_EAST, 0, VILLAGE_NORTH), fpRoot);

        // ═══ SPATIAL METRICS LOG ═══
        HubBuildLogger.BeginZone("spatial_metrics", "Spatial Metrics (diagnostic)");
        LogMetric("village_footprint", $"X[{VILLAGE_WEST},{VILLAGE_EAST}] Z[{VILLAGE_SOUTH},{VILLAGE_NORTH}] = {VILLAGE_EAST-VILLAGE_WEST}x{VILLAGE_NORTH-VILLAGE_SOUTH} units");
        LogMetric("central_plaza", $"X[{PLAZA_WEST},{PLAZA_EAST}] Z[{PLAZA_SOUTH},{PLAZA_NORTH}] = {PLAZA_EAST-PLAZA_WEST}x{PLAZA_NORTH-PLAZA_SOUTH} units");
        LogMetric("south_axis_length", $"Z {SOUTH_AXIS_START} to {SOUTH_AXIS_END} = {SOUTH_AXIS_END-SOUTH_AXIS_START} units");
        LogMetric("north_axis_length", $"Z {NORTH_AXIS_START} to {NORTH_AXIS_END} = {NORTH_AXIS_END-NORTH_AXIS_START} units");
        LogMetric("merchant_pad", $"center=({MERCHANT_PAD_CENTER.x},{MERCHANT_PAD_CENTER.z}) size={MERCHANT_PAD_SIZE.x}x{MERCHANT_PAD_SIZE.z}");
        LogMetric("forge_pad", $"center=({FORGE_PAD_CENTER.x},{FORGE_PAD_CENTER.z}) size={FORGE_PAD_SIZE.x}x{FORGE_PAD_SIZE.z}");
        LogMetric("mainhall_pad", $"center=({MAINHALL_PAD_CENTER.x},{MAINHALL_PAD_CENTER.z}) size={MAINHALL_PAD_SIZE.x}x{MAINHALL_PAD_SIZE.z}");
        LogMetric("ambiance_pad", $"center=({AMBIANCE_PAD_CENTER.x},{AMBIANCE_PAD_CENTER.z}) size={AMBIANCE_PAD_SIZE.x}x{AMBIANCE_PAD_SIZE.z}");
        LogMetric("merchant_to_plaza", $"{Mathf.Abs(MERCHANT_PAD_CENTER.x) - PLAZA_WEST:F0} units gap");
        LogMetric("forge_to_plaza", $"{FORGE_PAD_CENTER.x - PLAZA_EAST:F0} units gap");
        LogMetric("mainhall_to_plaza_north", $"{MAINHALL_PAD_CENTER.z - PLAZA_NORTH:F0} units gap");

        // ═══ SAVE ═══
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        HubBuildLogger.End();
        Debug.Log("[HubSetup] STEP 1 COMPLETE — Foundations only. No buildings/palissades/props/nature.");
        Debug.Log("[HubSetup] Next: STEP 2 = perimeter/enceinte");
    }

    // ═══ HELPERS ═══

    static GameObject LD(string rel) {
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>($"{P}/{rel}");
        if (pf == null) HubBuildLogger.Warning($"Prefab not found: {rel}");
        return pf;
    }

    static GameObject Ch(string name, GameObject parent) {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        return go;
    }

    static string HP(Transform t) {
        var parts = new System.Collections.Generic.List<string>();
        while (t != null) { parts.Insert(0, t.name); t = t.parent; }
        return string.Join("/", parts);
    }

    static void Tile(GameObject pf, Vector3 pos, float rotY, GameObject parent, string name,
        string role, F family, string note)
    {
        if (pf == null) { HubBuildLogger.Warning($"Skip {name}: prefab null"); return; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(pf);
        go.transform.SetParent(parent.transform);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0, rotY, 0);
        go.isStatic = true;
        go.name = name;
        HubBuildLogger.LogCreate(role, family, go.name, pf.name, AssetDatabase.GetAssetPath(pf),
            go.transform.position, go.transform.localPosition, go.transform.rotation.eulerAngles,
            go.transform.localScale, parent.name, HP(go.transform), note);
    }

    static void LogRT(string role, F family, GameObject go, string note) {
        string par = go.transform.parent != null ? go.transform.parent.name : "(root)";
        HubBuildLogger.LogCreate(role, family, go.name, "(runtime)", "",
            go.transform.position, go.transform.localPosition, go.transform.rotation.eulerAngles,
            go.transform.localScale, par, HP(go.transform), note);
    }

    static void LogMetric(string name, string value) {
        HubBuildLogger.LogCreate($"metric_{name}", F.Helper, name, "(metric)", "",
            Vector3.zero, Vector3.zero, Vector3.zero, Vector3.one, "Metrics", "", value);
    }

    static void MkMarker(string name, Vector3 pos, GameObject parent, Color color, string note) {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
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
        HubBuildLogger.LogMarker(name, pos, Vector3.zero, vis.name,
            name == "PlayerSpawn" || name == "DungeonEntranceMarker" ||
            name == "MerchantAreaMarker" || name == "ForgeAreaMarker");
    }

    static void MkPad(string name, Vector3 center, Vector3 size, Color color, GameObject parent, string note) {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.position = center;
        // Flat quad to visualize the reserved area
        var vis = GameObject.CreatePrimitive(PrimitiveType.Quad);
        vis.name = "PadVisual";
        vis.transform.SetParent(go.transform);
        vis.transform.localPosition = Vector3.up * 0.02f;
        vis.transform.localRotation = Quaternion.Euler(90, 0, 0);
        vis.transform.localScale = new Vector3(size.x, size.z, 1);
        var mr = vis.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Surface", 1); // transparent
        mat.SetFloat("_Blend", 0);
        mat.name = $"Pad_{name}";
        mr.sharedMaterial = mat;
        var col = vis.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);
        HubBuildLogger.LogCreate("reserved_pad", F.Helper, name, "(pad)", "",
            center, center, Vector3.zero, size, parent.name, HP(go.transform),
            $"{note} | center=({center.x},{center.z}) size={size.x}x{size.z}");
    }

    static void MkEdge(string name, Vector3 pos, GameObject parent) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.position = pos + Vector3.up * 0.5f;
        go.transform.localScale = new Vector3(1f, 1f, 1f);
        var mr = go.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", Color.magenta);
        mat.name = "FP_Edge";
        mr.sharedMaterial = mat;
        HubBuildLogger.LogCreate("footprint_edge", F.Helper, name, "(edge marker)", "",
            pos, pos, Vector3.zero, Vector3.one, parent.name, HP(go.transform),
            "Village boundary corner visualization");
    }
}
