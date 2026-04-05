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
    static readonly string CobbleA   = "Environment/Road/EA03_Environment_Road_Cobble_01a_PRE.prefab";
    static readonly string CobbleB   = "Environment/Road/EA03_Environment_Road_Cobble_01b_PRE.prefab";
    static readonly string CobbleCorner = "Environment/Road/EA03_Environment_Road_Cobble_Corner_01a_PRE.prefab";
    static readonly string WoodRoad  = "Environment/Road/EA03_Env_Road_Wooden_01d_PRE.prefab";
    static readonly string MudFlat   = "Environment/Mud/EA03_Env_Mud_Flat_01b_PRE.prefab";
    static readonly string MudFlatX  = "Environment/Mud/EA03_Env_Mud_Flat_01b_x_PRE.prefab";
    static readonly string SandFlat  = "Environment/Sand/EA03_Env_Sand_Flat_01a_PRE.prefab";
    static readonly string Platform  = "Environment/Road/EA03_Village_platform_01a_PRE.prefab";

    [MenuItem("DonGeonMaster/Creer Hub Prototype", false, 300)]
    public static void CreateHubPrototype()
    {
        // ═══ LOAD PREFABS ═══
        var cobbleA = Load(CobbleA);
        var cobbleB = Load(CobbleB);
        var cobbleCorner = Load(CobbleCorner);
        var woodRoad = Load(WoodRoad);
        var mudFlat = Load(MudFlat);
        var mudFlatX = Load(MudFlatX);
        var sandFlat = Load(SandFlat);
        var platform = Load(Platform);

        if (cobbleA == null) { Debug.LogError("[HubSetup] Cobble prefab not found — aborting"); return; }

        // ═══ NEW SCENE ═══
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

        // Camera
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 50f;
        camGO.transform.position = new Vector3(0f, 40f, -35f);
        camGO.transform.rotation = Quaternion.Euler(50f, 0f, 0f);

        // ═══ HIERARCHY ═══
        var hubRoot = new GameObject("HubLayout");
        var solRoot = new GameObject("Sol");
        solRoot.transform.SetParent(hubRoot.transform);
        var markersRoot = new GameObject("Markers");
        markersRoot.transform.SetParent(hubRoot.transform);

        // ═══ MARKERS ═══
        CreateMarker("PlayerSpawn",            new Vector3(0f,  0f, -18f), markersRoot.transform, Color.green);
        CreateMarker("DungeonEntranceMarker",  new Vector3(0f,  0f,  28f), markersRoot.transform, Color.red);
        CreateMarker("MerchantAreaMarker",     new Vector3(-14f, 0f, 4f),  markersRoot.transform, Color.yellow);
        CreateMarker("ForgeAreaMarker",        new Vector3(14f, 0f,  4f),  markersRoot.transform, new Color(1f, 0.5f, 0f));

        // ═══ SOL — STEP 1 ═══
        // Hub layout (sud -> nord) :
        //   Z = -20 to -14 : bande arrivée joueur (terre/sable)
        //   Z = -14 to -6  : transition sud-centre (chemin bois + terre)
        //   Z = -6  to +10 : place centrale (pavés cobble)
        //   Z = +10 to +20 : zone nord-centre (transition cobble -> terre)
        //   Z = +20 to +30 : entrée donjon (terre sombre/mud)
        //
        //   X = -18 to -8  : allée marchand (ouest)
        //   X = +8  to +18 : allée forge (est)

        float S = 4f; // tile spacing — les tiles cobble couvrent ~4 unites

        // ── 1. PLACE CENTRALE (cobble) : Z -6 to +10, X -8 to +8 ──
        var solCentre = new GameObject("Sol_PlaceCentrale");
        solCentre.transform.SetParent(solRoot.transform);

        for (int x = -2; x <= 2; x++)
        {
            for (int z = -1; z <= 2; z++)
            {
                // Centre exact = tile speciale
                var prefab = (x == 0 && z == 0) ? (cobbleB ?? cobbleA) : cobbleA;
                PlaceTile(prefab, new Vector3(x * S, 0f, z * S), 0f, solCentre.transform,
                    $"Cobble_{x+2}_{z+1}");
            }
        }
        // Coins de la place
        if (cobbleCorner != null)
        {
            PlaceTile(cobbleCorner, new Vector3(-2*S, 0f, -1*S), 0f,   solCentre.transform, "Corner_SW");
            PlaceTile(cobbleCorner, new Vector3( 2*S, 0f, -1*S), 90f,  solCentre.transform, "Corner_SE");
            PlaceTile(cobbleCorner, new Vector3(-2*S, 0f,  2*S), -90f, solCentre.transform, "Corner_NW");
            PlaceTile(cobbleCorner, new Vector3( 2*S, 0f,  2*S), 180f, solCentre.transform, "Corner_NE");
        }

        // ── 2. AXE PRINCIPAL SUD (bois + terre) : Z -18 to -6 ──
        var solSud = new GameObject("Sol_AxeSud");
        solSud.transform.SetParent(solRoot.transform);

        var axeSudPrefab = woodRoad ?? cobbleA;
        for (int z = -4; z <= -2; z++)
        {
            PlaceTile(axeSudPrefab, new Vector3(0f, 0f, z * S), 0f, solSud.transform,
                $"AxeSud_{z+5}");
        }
        // Terre flanquante sur l'arrivée
        var terreSud = sandFlat ?? mudFlat;
        if (terreSud != null)
        {
            for (int z = -4; z <= -2; z++)
            {
                PlaceTile(terreSud, new Vector3(-S, 0f, z * S), 0f, solSud.transform, $"TerreSud_L_{z+5}");
                PlaceTile(terreSud, new Vector3( S, 0f, z * S), 0f, solSud.transform, $"TerreSud_R_{z+5}");
            }
        }

        // ── 3. AXE PRINCIPAL NORD (cobble → terre) : Z +10 to +28 ──
        var solNord = new GameObject("Sol_AxeNord");
        solNord.transform.SetParent(solRoot.transform);

        // Transition cobble
        for (int z = 3; z <= 4; z++)
        {
            PlaceTile(cobbleA, new Vector3(0f, 0f, z * S), 0f, solNord.transform,
                $"NordCobble_{z-3}");
        }
        // Zone donjon (terre sombre)
        var terreDonjon = mudFlat ?? sandFlat;
        if (terreDonjon != null)
        {
            for (int z = 5; z <= 7; z++)
            {
                PlaceTile(terreDonjon, new Vector3(0f, 0f, z * S), 0f, solNord.transform,
                    $"DonjonTerre_{z-5}");
                PlaceTile(terreDonjon, new Vector3(-S, 0f, z * S), 0f, solNord.transform,
                    $"DonjonTerre_L_{z-5}");
                PlaceTile(terreDonjon, new Vector3( S, 0f, z * S), 0f, solNord.transform,
                    $"DonjonTerre_R_{z-5}");
            }
        }

        // ── 4. ALLÉE MARCHAND OUEST : X -8 to -18, Z ~0 to 8 ──
        var solMarchand = new GameObject("Sol_AlleeMarchand");
        solMarchand.transform.SetParent(solRoot.transform);

        var alleePrefab = woodRoad ?? cobbleA;
        for (int x = -3; x >= -4; x--)
        {
            PlaceTile(alleePrefab, new Vector3(x * S, 0f, S), 90f, solMarchand.transform,
                $"AlleeMarchand_{x+5}");
        }
        // Zone marchand (terre)
        if (terreSud != null)
        {
            PlaceTile(terreSud, new Vector3(-3*S, 0f, 0f), 0f, solMarchand.transform, "ZoneMarchand_0");
            PlaceTile(terreSud, new Vector3(-4*S, 0f, 0f), 0f, solMarchand.transform, "ZoneMarchand_1");
            PlaceTile(terreSud, new Vector3(-3*S, 0f, 2*S), 0f, solMarchand.transform, "ZoneMarchand_2");
            PlaceTile(terreSud, new Vector3(-4*S, 0f, 2*S), 0f, solMarchand.transform, "ZoneMarchand_3");
        }

        // ── 5. ALLÉE FORGE EST : X +8 to +18, Z ~0 to 8 ──
        var solForge = new GameObject("Sol_AlleeForge");
        solForge.transform.SetParent(solRoot.transform);

        for (int x = 3; x <= 4; x++)
        {
            PlaceTile(alleePrefab, new Vector3(x * S, 0f, S), 90f, solForge.transform,
                $"AlleeForge_{x-3}");
        }
        // Zone forge (terre)
        var mudForge = mudFlatX ?? mudFlat ?? sandFlat;
        if (mudForge != null)
        {
            PlaceTile(mudForge, new Vector3(3*S, 0f, 0f), 0f, solForge.transform, "ZoneForge_0");
            PlaceTile(mudForge, new Vector3(4*S, 0f, 0f), 0f, solForge.transform, "ZoneForge_1");
            PlaceTile(mudForge, new Vector3(3*S, 0f, 2*S), 0f, solForge.transform, "ZoneForge_2");
            PlaceTile(mudForge, new Vector3(4*S, 0f, 2*S), 0f, solForge.transform, "ZoneForge_3");
        }

        // ── 6. BASE GROUND (grand plan sous tout pour combler les trous) ──
        var groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        groundPlane.name = "BaseGround";
        groundPlane.transform.SetParent(solRoot.transform);
        groundPlane.transform.position = new Vector3(0f, -0.05f, 5f);
        groundPlane.transform.localScale = new Vector3(5f, 1f, 6f);
        groundPlane.isStatic = true;
        // Couleur terre neutre
        var groundMr = groundPlane.GetComponent<MeshRenderer>();
        var groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        groundMat.SetColor("_BaseColor", new Color(0.35f, 0.28f, 0.2f));
        groundMat.name = "HubGroundBase";
        groundMr.sharedMaterial = groundMat;

        // ═══ SAVE ═══
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        // Count placed tiles
        int tileCount = 0;
        foreach (Transform child in solRoot.transform)
            tileCount += child.childCount;
        tileCount++; // BaseGround

        Debug.Log($"[HubSetup] STEP 1 done — {tileCount} sol elements, 4 markers");
        Debug.Log($"[HubSetup] Markers: PlayerSpawn(0,0,-18) DungeonEntrance(0,0,28) Merchant(-14,0,4) Forge(14,0,4)");
        Debug.Log($"[HubSetup] Scene saved: {ScenePath}");
    }

    static GameObject Load(string rel)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Pack}/{rel}");
        if (prefab == null) Debug.LogWarning($"[HubSetup] Not found: {rel}");
        return prefab;
    }

    static void PlaceTile(GameObject prefab, Vector3 pos, float rotY, Transform parent, string name = null)
    {
        if (prefab == null) return;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        go.isStatic = true;
        if (name != null) go.name = name;
    }

    static void CreateMarker(string name, Vector3 pos, Transform parent, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        // Pilier visuel (visible en scene view)
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
    }
}
