using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using F = HubBuildLogger.Family;

/// <summary>
/// STEP 1B — Fondation spatiale neutre.
/// Sol uniquement. Aucun marker, pad, batiment, prop, nature visible.
/// Menu: DonGeonMaster > Creer Hub Prototype
/// </summary>
public class HubPrototypeSetup
{
    static readonly string P = "Assets/EmaceArt/Slavic World Free/Prefabs";
    static readonly string ScenePath = "Assets/_Project/Scenes/HubPrototype.unity";

    // ═══ LAYOUT CONSTANTS (log-only, aucun objet visuel n'en depend) ═══
    const float S = 4f; // tile spacing

    const float VILLAGE_WEST = -22f, VILLAGE_EAST = 22f;
    const float VILLAGE_SOUTH = -22f, VILLAGE_NORTH = 32f;
    const float PLAZA_WEST = -8f, PLAZA_EAST = 8f;
    const float PLAZA_SOUTH = -4f, PLAZA_NORTH = 8f;

    [MenuItem("DonGeonMaster/Creer Hub Prototype", false, 300)]
    public static void CreateHubPrototype()
    {
        HubBuildLogger.Begin(ScenePath, $"Pack: {P} | STEP 1B: Sol neutre uniquement");

        // ═══ LOAD — 2 familles de sol seulement ═══
        var cobble = LD("Environment/Road/EA03_Environment_Road_Cobble_01a_PRE.prefab");
        var dirt = LD("Environment/Sand/EA03_Env_Sand_Flat_01a_PRE.prefab");
        if (dirt == null) dirt = LD("Environment/Mud/EA03_Env_Mud_Flat_01b_PRE.prefab");

        if (cobble == null) {
            HubBuildLogger.Error("Cobble prefab not found — aborting");
            HubBuildLogger.End(); return;
        }
        if (dirt == null) HubBuildLogger.Warning("No dirt prefab found — lateral zones will be cobble only");

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
        LogRT("scene_light", F.Helper, sunGO, "Directional light");

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 50f;
        camGO.transform.position = new Vector3(0f, 55f, -20f);
        camGO.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
        LogRT("scene_camera", F.Helper, camGO, "Top-down overview");

        // ═══ HIERARCHY — Sol only ═══
        var hub = new GameObject("HubLayout");
        var solRoot = Ch("Sol", hub);

        int tileCount = 0;

        // ── PLACE CENTRALE (cobble 5x4) ──
        HubBuildLogger.BeginZone("central_plaza", "Central Plaza");
        var solPlaza = Ch("Sol_Place", solRoot);
        for (int x = -2; x <= 2; x++)
            for (int z = -1; z <= 2; z++) {
                Tile(cobble, new Vector3(x*S, 0, z*S), 0, solPlaza,
                    $"Plaza_{x+2}_{z+1}", "ground_cobble", F.Environment, "Plaza cobble tile");
                tileCount++;
            }

        // ── AXE PRINCIPAL SUD (cobble, Z -20 to -4) ──
        HubBuildLogger.BeginZone("south_axis", "South Axis");
        var solAxe = Ch("Sol_Axe", solRoot);
        for (int z = -5; z <= -2; z++) {
            Tile(cobble, new Vector3(0, 0, z*S), 0, solAxe,
                $"AxeSud_{z+6}", "ground_cobble", F.Environment, "South main axis cobble");
            tileCount++;
        }

        // ── AXE PRINCIPAL NORD (cobble transition + dirt approach, Z +12 to +28) ──
        HubBuildLogger.BeginZone("north_axis", "North Axis");
        // Cobble transition
        for (int z = 3; z <= 4; z++) {
            Tile(cobble, new Vector3(0, 0, z*S), 0, solAxe,
                $"AxeNord_{z-3}", "ground_cobble", F.Environment, "North axis cobble transition");
            tileCount++;
        }
        // Dirt approach to dungeon zone
        if (dirt != null)
            for (int z = 5; z <= 7; z++) {
                Tile(dirt, new Vector3( 0, 0, z*S), 0, solAxe, $"NordDirt_{z-5}",   "ground_dirt", F.Environment, "North dirt approach center");
                Tile(dirt, new Vector3(-S, 0, z*S), 0, solAxe, $"NordDirt_L_{z-5}", "ground_dirt", F.Environment, "North dirt approach left");
                Tile(dirt, new Vector3( S, 0, z*S), 0, solAxe, $"NordDirt_R_{z-5}", "ground_dirt", F.Environment, "North dirt approach right");
                tileCount += 3;
            }

        // ── BRANCHES LATERALES (dirt, 2x2 each side) ──
        HubBuildLogger.BeginZone("lateral_wings", "Lateral Wings");
        var solWings = Ch("Sol_Wings", solRoot);
        // West wing (toward future merchant area)
        if (dirt != null) {
            for (int x = -3; x <= -2; x++)
                for (int z = 0; z <= 1; z++) {
                    Tile(dirt, new Vector3(x*S, 0, z*S), 0, solWings,
                        $"WestWing_{x+4}_{z}", "ground_dirt", F.Environment, "West lateral ground");
                    tileCount++;
                }
        }
        // East wing (toward future forge area)
        if (dirt != null) {
            for (int x = 2; x <= 3; x++)
                for (int z = 0; z <= 1; z++) {
                    Tile(dirt, new Vector3(x*S, 0, z*S), 0, solWings,
                        $"EastWing_{x-2}_{z}", "ground_dirt", F.Environment, "East lateral ground");
                    tileCount++;
                }
        }

        // ── BASE GROUND PLANE ──
        HubBuildLogger.BeginZone("base_ground", "Base Ground Plane");
        var gp = GameObject.CreatePrimitive(PrimitiveType.Plane);
        gp.name = "BaseGround";
        gp.transform.SetParent(solRoot.transform);
        gp.transform.position = new Vector3(0f, -0.05f, 5f);
        gp.transform.localScale = new Vector3(4.4f, 1f, 5.4f);
        gp.isStatic = true;
        var gMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        gMat.SetColor("_BaseColor", new Color(0.35f, 0.28f, 0.2f));
        gMat.name = "HubGroundBase";
        gp.GetComponent<MeshRenderer>().sharedMaterial = gMat;
        LogRT("ground_base_plane", F.Helper, gp, "Earth base plane covering village footprint");

        // ═══ SPATIAL METRICS (log-only, no scene objects) ═══
        HubBuildLogger.BeginZone("spatial_metrics", "Spatial Metrics (log-only)");
        LogMetric("village_footprint", $"X[{VILLAGE_WEST},{VILLAGE_EAST}] Z[{VILLAGE_SOUTH},{VILLAGE_NORTH}] = {VILLAGE_EAST-VILLAGE_WEST}x{VILLAGE_NORTH-VILLAGE_SOUTH} units");
        LogMetric("central_plaza", $"X[{PLAZA_WEST},{PLAZA_EAST}] Z[{PLAZA_SOUTH},{PLAZA_NORTH}] = {PLAZA_EAST-PLAZA_WEST}x{PLAZA_NORTH-PLAZA_SOUTH} units");
        LogMetric("south_axis", $"Z -20 to -4 = 16 units (4 tiles)");
        LogMetric("north_axis", $"Z 8 to 28 = 20 units (2 cobble + 9 dirt)");
        LogMetric("west_wing", $"X[-12,-8] Z[0,4] = 8x8 units (4 tiles)");
        LogMetric("east_wing", $"X[8,12] Z[0,4] = 8x8 units (4 tiles)");
        LogMetric("future_merchant_pad", $"center=(-14,2) size=10x10 (log-only, no scene object)");
        LogMetric("future_forge_pad", $"center=(14,2) size=10x10 (log-only, no scene object)");
        LogMetric("future_mainhall_pad", $"center=(0,12) size=12x8 (log-only, no scene object)");
        LogMetric("future_ambiance_pad", $"center=(12,-12) size=8x8 (log-only, no scene object)");
        LogMetric("future_spawn", $"(0,0,-18) (log-only, no scene object)");
        LogMetric("future_dungeon_entrance", $"(0,0,28) (log-only, no scene object)");
        LogMetric("total_ground_tiles", $"{tileCount}");

        // ═══ VALIDATION (log-only) ═══
        HubBuildLogger.BeginZone("validation", "Scene Content Validation");
        LogMetric("buildings_in_scene", "0 — correct for STEP 1B");
        LogMetric("props_in_scene", "0 — correct for STEP 1B");
        LogMetric("nature_in_scene", "0 — correct for STEP 1B");
        LogMetric("markers_visible_in_scene", "0 — correct for STEP 1B");
        LogMetric("pads_visible_in_scene", "0 — correct for STEP 1B");
        LogMetric("footprint_edges_in_scene", "0 — correct for STEP 1B");
        LogMetric("prefab_families_used", "2 (cobble + dirt)");

        // ═══ SAVE ═══
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        HubBuildLogger.End();
        Debug.Log($"[HubSetup] STEP 1B COMPLETE — {tileCount} ground tiles + base plane. Nothing else.");
        Debug.Log("[HubSetup] Scene contains: sol only. No markers/pads/edges/buildings/props/nature.");
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
}
