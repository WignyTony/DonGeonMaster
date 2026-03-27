using UnityEngine;
using UnityEngine.InputSystem;

namespace DonGeonMaster.MapGeneration.DebugTools
{
    /// <summary>
    /// Script de test Phase 1 : valide MapStructureDebugRenderer en isolation.
    /// Attacher a un GameObject vide dans une scene vide.
    /// F5 = generer + afficher la structure. F7 = clear. Molette = zoom. Clic droit = pan.
    /// </summary>
    public class StructureRendererTest : MonoBehaviour
    {
        MapStructureDebugRenderer structureRenderer;
        MapGenerator generator;
        Camera cam;
        MapGenConfig config;
        MapData lastMap;

        void Start()
        {
            structureRenderer = gameObject.AddComponent<MapStructureDebugRenderer>();

            var camGO = GameObject.Find("Main Camera");
            if (camGO == null)
            {
                camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
            }
            cam = camGO.GetComponent<Camera>() ?? camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
            cam.orthographic = true;
            cam.orthographicSize = 60;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;
            cam.transform.position = new Vector3(90, 100, 90);
            cam.transform.rotation = Quaternion.Euler(90, 0, 0);

            if (FindAnyObjectByType<Light>() == null)
            {
                var lightGO = new GameObject("Light");
                var light = lightGO.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1f;
                lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
            }

            generator = new MapGenerator();
            config = new MapGenConfig
            {
                mapWidth = 30,
                mapHeight = 30,
                cellSize = 6,
                minRooms = 5,
                maxRooms = 10,
                minRoomSize = 3,
                maxRoomSize = 8,
                borderMargin = 2,
                corridorWidth = 2,
                useRandomSeed = true
            };

            UnityEngine.Debug.Log("[Phase1Test] Pret. F5 = generer, F7 = clear, molette = zoom, clic droit = pan");
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.f5Key.wasPressedThisFrame)
                GenerateAndRender();

            if (kb.f7Key.wasPressedThisFrame)
            {
                structureRenderer.Clear();
                UnityEngine.Debug.Log("[Phase1Test] Clear OK");
            }

            var mouse = Mouse.current;
            if (mouse != null && cam != null && cam.orthographic)
            {
                float scroll = mouse.scroll.y.ReadValue() * 0.01f;
                if (Mathf.Abs(scroll) > 0.001f)
                    cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll, 5, 200);

                if (mouse.rightButton.isPressed)
                {
                    var delta = mouse.delta.ReadValue();
                    float speed = cam.orthographicSize * 0.002f;
                    cam.transform.Translate(-delta.x * speed, 0, -delta.y * speed, Space.World);
                }
            }
        }

        void GenerateAndRender()
        {
            structureRenderer.Clear();

            var (map, result) = generator.Generate(config);
            lastMap = map;

            structureRenderer.Render(map, config);

            cam.transform.position = new Vector3(
                structureRenderer.StructureCenter.x, 100, structureRenderer.StructureCenter.z);
            cam.orthographicSize = Mathf.Max(
                structureRenderer.StructureSize.x, structureRenderer.StructureSize.z) * 0.55f;

            UnityEngine.Debug.Log($"[Phase1Test] === VALIDATION ===");
            UnityEngine.Debug.Log($"  Seed: {result.seed}");
            UnityEngine.Debug.Log($"  Salles: {result.roomCount}, Couloirs: {result.corridorCount}");
            UnityEngine.Debug.Log($"  HasRendered: {structureRenderer.HasRendered}");
            UnityEngine.Debug.Log($"  CellsRendered: {structureRenderer.RenderedCellCount}");
            UnityEngine.Debug.Log($"  Floors: {structureRenderer.RenderedFloorCount}");
            UnityEngine.Debug.Log($"  Walls: {structureRenderer.RenderedWallCount}");
            UnityEngine.Debug.Log($"  Center: {structureRenderer.StructureCenter}");
            UnityEngine.Debug.Log($"  Size: {structureRenderer.StructureSize}");
            UnityEngine.Debug.Log($"  Spawn: ({map.spawnCell.x},{map.spawnCell.y})");
            UnityEngine.Debug.Log($"  Exit: ({map.exitCell.x},{map.exitCell.y})");

            bool ok = structureRenderer.HasRendered
                      && structureRenderer.RenderedFloorCount > 0
                      && structureRenderer.RenderedWallCount > 0
                      && structureRenderer.StructureCenter.magnitude > 0
                      && structureRenderer.StructureSize.magnitude > 0
                      && map.spawnCell.x >= 0
                      && map.exitCell.x >= 0;

            UnityEngine.Debug.Log(ok
                ? "[Phase1Test] VALIDATION OK"
                : "[Phase1Test] VALIDATION ECHEC");
        }
    }
}
