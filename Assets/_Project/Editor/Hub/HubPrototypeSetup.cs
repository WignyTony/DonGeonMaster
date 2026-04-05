using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using F = HubBuildLogger.Family;

/// <summary>
/// Genere la scene HubPrototype complete.
/// Menu: DonGeonMaster > Creer Hub Prototype
/// </summary>
public class HubPrototypeSetup
{
    static readonly string P = "Assets/EmaceArt/Slavic World Free/Prefabs";
    static readonly string ScenePath = "Assets/_Project/Scenes/HubPrototype.unity";

    [MenuItem("DonGeonMaster/Creer Hub Prototype", false, 300)]
    public static void CreateHubPrototype()
    {
        HubBuildLogger.Begin(ScenePath, $"Pack: {P}");

        // ═══ LOAD ALL PREFABS ═══
        // Sol
        var cobbleA = L("Environment/Road/EA03_Environment_Road_Cobble_01a_PRE.prefab");
        var cobbleB = L("Environment/Road/EA03_Environment_Road_Cobble_01b_PRE.prefab");
        var cobbleCorner = L("Environment/Road/EA03_Environment_Road_Cobble_Corner_01a_PRE.prefab");
        var woodRoad = L("Environment/Road/EA03_Env_Road_Wooden_01d_PRE.prefab");
        var mudFlat = L("Environment/Mud/EA03_Env_Mud_Flat_01b_PRE.prefab");
        var mudFlatX = L("Environment/Mud/EA03_Env_Mud_Flat_01b_x_PRE.prefab");
        var sandFlat = L("Environment/Sand/EA03_Env_Sand_Flat_01a_PRE.prefab");
        // Buildings
        var house01 = L("Town/Building/EA03_Town_House_Comp_01a_PRE.prefab");
        var house02 = L("Town/Building/EA03_Town_House_Comp_02a_PRE.prefab");
        var admin01 = L("Town/Administrative/EA03_Town_Building_Administrative _01a_PRE.prefab");
        var shed01 = L("Village/OutBuilding/EA03_Village_OutBuilding_Shed_01a_PRE.prefab");
        // Fence
        var fenceA = L("Fence/Plank2/EA03_Fence_Plank_02a_PRE.prefab");
        var fenceB = L("Fence/Plank2/EA03_Fence_Plank_02b_PRE.prefab");
        var fenceC = L("Fence/Plank2/EA03_Fence_Plank_02c_PRE.prefab");
        var fenceD = L("Fence/Plank2/EA03_Fence_Plank_02d_PRE.prefab");
        var wallGate = L("Fence/Wall/EA03_Fence_WallGate_01a_PRE.prefab");
        // Dungeon
        var rockGate = L("Fence/RockGate/EA03_Fence_RockGate_01a_PRE.prefab");
        var rockBig = L("Environment/Rock/EA03_Environment_Rock_Big_Head_01a_PRE.prefab");
        var rockFlat = L("Environment/Rock/EA03_Environment_Rock_Flat_04c_PRE.prefab");
        var stairs = L("Environment/Stairs/EA03_Village_step_platform_Stair_R_03a_PRE.prefab");
        // Props
        var barrel = L("Prop/Container/EA03_Prop_Container_Barrel_01d_PRE.prefab");
        var crate01 = L("Prop/Container/EA03_Prop_Container_Crate_01a_PRE.prefab");
        var crate02 = L("Prop/Container/EA03_Prop_Container_Crate_02a_PRE.prefab");
        var chest = L("Prop/Container/EA03_Prop_Container_Chest_02a_PRE.prefab");
        var bag = L("Prop/Container/EA03_Prop_Container_Bag_02a_PRE.prefab");
        var basket = L("Prop/Container/EA03_Prop_Container_Basket_01a_PRE.prefab");
        var bench = L("Prop/Furniture/EA03_Prop_Town_Bench_01a_PRE.prefab");
        var stool = L("Prop/Furniture/EA03_Prop_Stool_01a_PRE.prefab");
        var table = L("Prop/Furniture/EA03_Prop_Tabble_01a_PRE.prefab");
        var sign = L("Prop/Sign/EA03_Prop_Sign_Chapel_01_PRE.prefab");
        var stand = L("Prop/Village/EA03_Prop_Stand_Sheet_01a_PRE.prefab");
        var stove = L("Prop/Village/EA03_Prop_House_Stove_01b_PRE.prefab");
        // Nature
        var tree01 = L("Nature/Tree/EA03_Nature_Tree_01b_PRE.prefab");
        var tree02 = L("Nature/Tree/EA03_Nature_Tree_02b_PRE.prefab");
        var tree03 = L("Nature/Tree/EA03_Nature_Tree_03b_PRE.prefab");
        var tree05 = L("Nature/Tree/EA03_Nature_Tree_05a_PRE.prefab");
        var bush01 = L("Nature/Bushes/EA03_Nature_Bush_01a_PRE.prefab");
        var bush02 = L("Nature/Bushes/EA03_Nature_Bush_02a_PRE.prefab");
        var bush03 = L("Nature/Bushes/EA03_Nature_Bush_03a_PRE.prefab");
        var bush04 = L("Nature/Bushes/EA03_Nature_Bush_04a_PRE.prefab");

        if (cobbleA == null || house01 == null) {
            HubBuildLogger.Error("Critical prefabs missing — aborting");
            HubBuildLogger.End();
            return;
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
        camGO.transform.position = new Vector3(0f, 40f, -35f);
        camGO.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
        LogRT("scene_camera", F.Helper, camGO, "Overview camera");

        // ═══ HIERARCHY ═══
        var hub = new GameObject("HubLayout");
        var solRoot = Child("Sol", hub);
        var palRoot = Child("Palissades", hub);
        var bldRoot = Child("Batiments", hub);
        var prpRoot = Child("Props", hub);
        var natRoot = Child("Nature", hub);
        var mrkRoot = Child("Markers", hub);

        // ═══ MARKERS ═══
        HubBuildLogger.SetPhase("gameplay_markers");
        HubBuildLogger.BeginZone("markers", "Gameplay Markers");
        MkMarker("PlayerSpawn",           new Vector3(0f,  0f, -18f), mrkRoot, Color.green,       "Player arrival, south gate");
        MkMarker("DungeonEntranceMarker", new Vector3(0f,  0f,  28f), mrkRoot, Color.red,         "Dungeon focal point, north");
        MkMarker("MerchantAreaMarker",    new Vector3(-14f,0f,  4f),  mrkRoot, Color.yellow,      "Merchant anchor, west");
        MkMarker("ForgeAreaMarker",       new Vector3(14f, 0f,  4f),  mrkRoot, new Color(1,.5f,0),"Forge anchor, east");

        float S = 4f; // tile spacing

        // ═══ SOL ═══
        HubBuildLogger.SetPhase("ground_foundation");

        // Central plaza
        HubBuildLogger.BeginZone("central_plaza", "Central Plaza Ground");
        var solCentre = Child("Sol_PlaceCentrale", solRoot);
        for (int x = -2; x <= 2; x++)
            for (int z = -1; z <= 2; z++) {
                var pf = (x == 0 && z == 0) ? (cobbleB ?? cobbleA) : cobbleA;
                Tile(pf, new Vector3(x*S,0,z*S), 0, solCentre, $"Cobble_{x+2}_{z+1}", "cobble_plaza", F.Environment,
                    (x==0&&z==0) ? "Central plaza anchor" : "Plaza paving");
            }
        if (cobbleCorner != null) {
            Tile(cobbleCorner, new Vector3(-2*S,0,-1*S), 0,   solCentre, "Corner_SW", "corner_plaza", F.Environment, "SW corner");
            Tile(cobbleCorner, new Vector3( 2*S,0,-1*S), 90,  solCentre, "Corner_SE", "corner_plaza", F.Environment, "SE corner");
            Tile(cobbleCorner, new Vector3(-2*S,0, 2*S), -90, solCentre, "Corner_NW", "corner_plaza", F.Environment, "NW corner");
            Tile(cobbleCorner, new Vector3( 2*S,0, 2*S), 180, solCentre, "Corner_NE", "corner_plaza", F.Environment, "NE corner");
        }

        // South path
        HubBuildLogger.BeginZone("south_arrival", "South Arrival");
        var solSud = Child("Sol_AxeSud", solRoot);
        var roadS = woodRoad ?? cobbleA;
        for (int z = -4; z <= -2; z++)
            Tile(roadS, new Vector3(0,0,z*S), 0, solSud, $"AxeSud_{z+5}", "road_arrival", F.Environment, "South approach path");
        var sand = sandFlat ?? mudFlat;
        if (sand != null)
            for (int z = -4; z <= -2; z++) {
                Tile(sand, new Vector3(-S,0,z*S), 0, solSud, $"TerreSud_L_{z+5}", "sand_flank", F.Environment, "Left flank");
                Tile(sand, new Vector3( S,0,z*S), 0, solSud, $"TerreSud_R_{z+5}", "sand_flank", F.Environment, "Right flank");
            }

        // North axis
        HubBuildLogger.BeginZone("north_approach", "North Approach");
        var solNord = Child("Sol_AxeNord", solRoot);
        for (int z = 3; z <= 4; z++)
            Tile(cobbleA, new Vector3(0,0,z*S), 0, solNord, $"NordCobble_{z-3}", "cobble_transition", F.Environment, "Transition to dungeon");

        // Dungeon ground
        HubBuildLogger.SetPhase("dungeon_approach");
        HubBuildLogger.BeginZone("dungeon_entrance", "Dungeon Entrance");
        var mud = mudFlat ?? sandFlat;
        if (mud != null)
            for (int z = 5; z <= 7; z++) {
                Tile(mud, new Vector3( 0,0,z*S), 0, solNord, $"DonjonTerre_{z-5}",   "mud_dungeon", F.Environment, "Dark dungeon ground");
                Tile(mud, new Vector3(-S,0,z*S), 0, solNord, $"DonjonTerre_L_{z-5}", "mud_dungeon", F.Environment, "Left dungeon flank");
                Tile(mud, new Vector3( S,0,z*S), 0, solNord, $"DonjonTerre_R_{z-5}", "mud_dungeon", F.Environment, "Right dungeon flank");
            }

        // Merchant ground
        HubBuildLogger.SetPhase("ground_foundation");
        HubBuildLogger.BeginZone("merchant_area", "Merchant Area");
        var solM = Child("Sol_AlleeMarchand", solRoot);
        var roadW = woodRoad ?? cobbleA;
        for (int x = -3; x >= -4; x--)
            Tile(roadW, new Vector3(x*S,0,S), 90, solM, $"AlleeMarchand_{x+5}", "road_merchant", F.Environment, "Path to merchant");
        if (sand != null) {
            Tile(sand, new Vector3(-3*S,0,0),   0, solM, "ZoneMarchand_0", "sand_merchant", F.Environment, "Merchant ground SW");
            Tile(sand, new Vector3(-4*S,0,0),   0, solM, "ZoneMarchand_1", "sand_merchant", F.Environment, "Merchant ground NW");
            Tile(sand, new Vector3(-3*S,0,2*S), 0, solM, "ZoneMarchand_2", "sand_merchant", F.Environment, "Merchant ground SE");
            Tile(sand, new Vector3(-4*S,0,2*S), 0, solM, "ZoneMarchand_3", "sand_merchant", F.Environment, "Merchant ground NE");
        }

        // Forge ground
        HubBuildLogger.BeginZone("forge_area", "Forge Area");
        var solF = Child("Sol_AlleeForge", solRoot);
        for (int x = 3; x <= 4; x++)
            Tile(roadW, new Vector3(x*S,0,S), 90, solF, $"AlleeForge_{x-3}", "road_forge", F.Environment, "Path to forge");
        var mudF = mudFlatX ?? mudFlat ?? sandFlat;
        if (mudF != null) {
            Tile(mudF, new Vector3(3*S,0,0),   0, solF, "ZoneForge_0", "mud_forge", F.Environment, "Forge ground SW");
            Tile(mudF, new Vector3(4*S,0,0),   0, solF, "ZoneForge_1", "mud_forge", F.Environment, "Forge ground NW");
            Tile(mudF, new Vector3(3*S,0,2*S), 0, solF, "ZoneForge_2", "mud_forge", F.Environment, "Forge ground SE");
            Tile(mudF, new Vector3(4*S,0,2*S), 0, solF, "ZoneForge_3", "mud_forge", F.Environment, "Forge ground NE");
        }

        // Base ground plane
        HubBuildLogger.BeginZone("base_ground", "Base Ground Plane");
        var gp = GameObject.CreatePrimitive(PrimitiveType.Plane);
        gp.name = "BaseGround";
        gp.transform.SetParent(solRoot.transform);
        gp.transform.position = new Vector3(0,-0.05f,5f);
        gp.transform.localScale = new Vector3(5f,1f,6f);
        gp.isStatic = true;
        var gMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        gMat.SetColor("_BaseColor", new Color(0.35f,0.28f,0.2f));
        gMat.name = "HubGroundBase";
        gp.GetComponent<MeshRenderer>().sharedMaterial = gMat;
        LogRT("ground_base", F.Helper, gp, "Earth base fills gaps");

        // ═══ PALISSADE ═══
        HubBuildLogger.SetPhase("village_perimeter");
        HubBuildLogger.BeginZone("perimeter", "Village Perimeter");
        float fS = 5f;
        // South wall
        if (fenceA != null) {
            Tile(fenceA, new Vector3(-15,0,-20), 0, palRoot, "Fence_S1", "fence_south", F.Environment, "South perimeter segment");
            Tile(fenceB??fenceA, new Vector3(-10,0,-20), 0, palRoot, "Fence_S2", "fence_south", F.Environment, "South perimeter segment");
            Tile(fenceC??fenceA, new Vector3(-5,0,-20), 0, palRoot, "Fence_S3", "fence_south", F.Environment, "South perimeter segment");
        }
        if (wallGate != null)
            Tile(wallGate, new Vector3(0,0,-20), 0, palRoot, "SouthGate", "gate_south", F.Environment, "South entrance gate");
        if (fenceA != null) {
            Tile(fenceA, new Vector3(5,0,-20), 0, palRoot, "Fence_S5", "fence_south", F.Environment, "South perimeter segment");
            Tile(fenceD??fenceA, new Vector3(10,0,-20), 0, palRoot, "Fence_S6", "fence_south", F.Environment, "South perimeter segment");
            Tile(fenceA, new Vector3(15,0,-20), 0, palRoot, "Fence_S7", "fence_south", F.Environment, "South perimeter segment");
        }
        // West wall
        if (fenceA != null)
            for (int i = -3; i <= 5; i++)
                Tile(fenceA, new Vector3(-20,0,i*fS), 90, palRoot, $"Fence_W{i+4}", "fence_west", F.Environment, "West perimeter");
        // East wall
        if (fenceA != null)
            for (int i = -3; i <= 5; i++)
                Tile(fenceA, new Vector3(20,0,i*fS), 90, palRoot, $"Fence_E{i+4}", "fence_east", F.Environment, "East perimeter");
        // North wall (gap at center for dungeon)
        if (fenceA != null) {
            Tile(fenceA, new Vector3(-15,0,28), 0, palRoot, "Fence_N1", "fence_north", F.Environment, "North perimeter left");
            Tile(fenceA, new Vector3(-10,0,28), 0, palRoot, "Fence_N2", "fence_north", F.Environment, "North perimeter left");
            Tile(fenceA, new Vector3(10,0,28), 0, palRoot, "Fence_N3", "fence_north", F.Environment, "North perimeter right");
            Tile(fenceA, new Vector3(15,0,28), 0, palRoot, "Fence_N4", "fence_north", F.Environment, "North perimeter right");
        }

        // ═══ DUNGEON ENTRANCE ═══
        if (rockBig != null) {
            Tile(rockBig, new Vector3(-4,0,27), 0,   palRoot, "DungeonRock_L", "dungeon_rock", F.Environment, "Left dungeon rock frame");
            Tile(rockBig, new Vector3( 4,0,27), 180, palRoot, "DungeonRock_R", "dungeon_rock", F.Environment, "Right dungeon rock frame");
        }
        if (rockFlat != null)
            Tile(rockFlat, new Vector3(0,0,30), 0, palRoot, "DungeonRock_Back", "dungeon_rock", F.Environment, "Backdrop rock behind entrance");
        if (rockGate != null)
            Tile(rockGate, new Vector3(0,0,26), 0, palRoot, "DungeonGate", "dungeon_gate", F.Environment, "Stone gate marking dungeon entry");
        if (stairs != null)
            Tile(stairs, new Vector3(0,0,23), 0, palRoot, "DungeonStairs", "dungeon_stairs", F.Environment, "Stairs descending into dungeon");

        // ═══ BATIMENTS ═══
        HubBuildLogger.SetPhase("main_buildings");
        HubBuildLogger.BeginZone("central_plaza", "Buildings on Plaza");
        // Main building (north of plaza)
        Tile(admin01??house01, new Vector3(0,0,12), 180, bldRoot, "CentralHall", "building_main", F.Building, "Main hall north of plaza");
        // Merchant (west)
        Tile(house01, new Vector3(-14,0,4), 90, bldRoot, "MerchantHouse", "building_merchant", F.Building, "Merchant building west of plaza");
        // Forge (east)
        Tile(shed01??house02, new Vector3(14,0,4), -90, bldRoot, "ForgeWorkshop", "building_forge", F.Building, "Forge workshop east of plaza");
        // Ambiance (south-east)
        Tile(house02??house01, new Vector3(10,0,-10), -45, bldRoot, "TavernHouse", "building_ambiance", F.Building, "Ambient building south-east corner");

        // ═══ PROPS ═══
        HubBuildLogger.SetPhase("props_dressing");
        HubBuildLogger.BeginZone("central_plaza", "Props — Central Plaza");
        // Central plaza dressing
        Tile(bench, new Vector3(4,0,-2), 90, prpRoot, "PlazaBench_1", "prop_bench", F.Prop, "Bench facing plaza center");
        Tile(bench, new Vector3(-4,0,6), -90, prpRoot, "PlazaBench_2", "prop_bench", F.Prop, "Bench near north side of plaza");
        Tile(barrel, new Vector3(-3,0,3), 0, prpRoot, "PlazaBarrel_1", "prop_barrel", F.Prop, "Barrel cluster on plaza");
        Tile(barrel, new Vector3(-3.5f,0,2.5f), 30, prpRoot, "PlazaBarrel_2", "prop_barrel", F.Prop, "Second barrel in cluster");
        Tile(crate01, new Vector3(-2.5f,0,3.5f), 15, prpRoot, "PlazaCrate", "prop_crate", F.Prop, "Crate near barrel cluster");
        Tile(sign, new Vector3(1,0,-5), 0, prpRoot, "PlazaSign", "prop_sign", F.Prop, "Village signpost at plaza south edge");

        HubBuildLogger.BeginZone("merchant_area", "Props — Merchant");
        Tile(stand, new Vector3(-12,0,2), 90, prpRoot, "MerchantStand", "prop_stand", F.Prop, "Merchant stall near building");
        Tile(basket, new Vector3(-13,0,1), 0, prpRoot, "MerchantBasket", "prop_basket", F.Prop, "Basket of goods at merchant");
        Tile(bag, new Vector3(-11,0,5.5f), 20, prpRoot, "MerchantBag", "prop_bag", F.Prop, "Merchant supply bag");
        Tile(crate02, new Vector3(-13.5f,0,5), 0, prpRoot, "MerchantCrate", "prop_crate", F.Prop, "Storage crate at merchant");

        HubBuildLogger.BeginZone("forge_area", "Props — Forge");
        Tile(stove, new Vector3(13,0,2), -90, prpRoot, "ForgeStove", "prop_stove", F.Prop, "Forge stove / anvil area");
        Tile(barrel, new Vector3(12.5f,0,5.5f), 0, prpRoot, "ForgeBarrel", "prop_barrel", F.Prop, "Water barrel near forge");
        Tile(crate01, new Vector3(15,0,3), 45, prpRoot, "ForgeCrate", "prop_crate", F.Prop, "Material crate at forge");
        Tile(chest, new Vector3(15,0,5), 0, prpRoot, "ForgeChest", "prop_chest", F.Prop, "Upgrade chest at forge");
        Tile(stool, new Vector3(12,0,6), 0, prpRoot, "ForgeStool", "prop_stool", F.Prop, "Stool near forge workspace");
        Tile(table, new Vector3(13.5f,0,6.5f), -90, prpRoot, "ForgeTable", "prop_table", F.Prop, "Work table at forge");

        // ═══ NATURE ═══
        HubBuildLogger.SetPhase("nature_dressing");
        HubBuildLogger.BeginZone("perimeter", "Nature — Perimeter");
        // Trees along edges
        Tile(tree01, new Vector3(-18,0,12), 0,   natRoot, "Tree_NW1", "tree_edge", F.Nature, "Tree north-west edge");
        Tile(tree02, new Vector3(-18,0,-5), 120, natRoot, "Tree_W1",  "tree_edge", F.Nature, "Tree west edge");
        Tile(tree03, new Vector3(18,0,14),  60,  natRoot, "Tree_NE1", "tree_edge", F.Nature, "Tree north-east edge");
        Tile(tree05, new Vector3(18,0,-8),  200, natRoot, "Tree_SE1", "tree_edge", F.Nature, "Tree south-east edge");
        // Bushes filling corners
        Tile(bush01, new Vector3(-17,0,-16), 0,   natRoot, "Bush_SW1", "bush_corner", F.Nature, "Bush south-west corner");
        Tile(bush02, new Vector3(-16,0,20),  30,  natRoot, "Bush_NW1", "bush_corner", F.Nature, "Bush north-west");
        Tile(bush03, new Vector3(17,0,-16),  90,  natRoot, "Bush_SE1", "bush_corner", F.Nature, "Bush south-east corner");
        Tile(bush04, new Vector3(16,0,20),   180, natRoot, "Bush_NE1", "bush_corner", F.Nature, "Bush north-east");
        // Additional bushes along paths
        Tile(bush01, new Vector3(-6,0,-14),  45,  natRoot, "Bush_Path1", "bush_path", F.Nature, "Bush along south path");
        Tile(bush02, new Vector3(6,0,-14),   -30, natRoot, "Bush_Path2", "bush_path", F.Nature, "Bush along south path");
        Tile(bush03, new Vector3(-8,0,16),   0,   natRoot, "Bush_North1", "bush_approach", F.Nature, "Bush near dungeon approach");
        Tile(bush04, new Vector3(8,0,16),    60,  natRoot, "Bush_North2", "bush_approach", F.Nature, "Bush near dungeon approach");

        // ═══ SAVE ═══
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        // ═══ VALIDATION ═══
        HubBuildLogger.End();
        int bld = HubBuildLogger.CountFamily(F.Building);
        int prp = HubBuildLogger.CountFamily(F.Prop);
        int nat = HubBuildLogger.CountFamily(F.Nature);
        if (bld < 4) Debug.LogError($"[HubSetup] INVALID BUILD: only {bld} buildings (need >= 4)");
        if (prp < 12) Debug.LogError($"[HubSetup] INVALID BUILD: only {prp} props (need >= 12)");
        if (nat < 8) Debug.LogError($"[HubSetup] INVALID BUILD: only {nat} nature (need >= 8)");
        if (bld >= 4 && prp >= 12 && nat >= 8)
            Debug.Log($"[HubSetup] BUILD VALID: Bld:{bld} Prop:{prp} Nat:{nat}");
    }

    // ═══ HELPERS ═══

    static GameObject L(string rel) {
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>($"{P}/{rel}");
        if (pf == null) HubBuildLogger.Warning($"Prefab not found: {rel}");
        return pf;
    }

    static GameObject Child(string name, GameObject parent) {
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
        go.transform.rotation = Quaternion.Euler(0,rotY,0);
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
        bool valid = name == "PlayerSpawn" || name == "DungeonEntranceMarker" ||
                     name == "MerchantAreaMarker" || name == "ForgeAreaMarker";
        HubBuildLogger.LogMarker(name, pos, Vector3.zero, vis.name, valid);
    }
}
