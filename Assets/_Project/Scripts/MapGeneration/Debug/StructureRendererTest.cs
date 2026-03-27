using UnityEngine;
using UnityEngine.InputSystem;

namespace DonGeonMaster.MapGeneration.DebugTools
{
    /// <summary>
    /// Test Phase 1 : valide MapStructureDebugRenderer en isolation.
    /// F5 = generer, F7 = clear, molette = zoom, clic droit = pan.
    /// </summary>
    public class StructureRendererTest : MonoBehaviour
    {
        MapStructureDebugRenderer structureRenderer;
        MapGenerator generator;
        Camera cam;
        MapGenConfig config;
        int genCount;

        void Start()
        {
            structureRenderer = gameObject.AddComponent<MapStructureDebugRenderer>();
            generator = new MapGenerator();
            config = new MapGenConfig
            {
                mapWidth = 30, mapHeight = 30, cellSize = 6,
                minRooms = 5, maxRooms = 10,
                minRoomSize = 3, maxRoomSize = 8,
                borderMargin = 2, corridorWidth = 2,
                useRandomSeed = true
            };

            SetupCamera();
            UnityEngine.Debug.Log("[Phase1] Pret. F5=generer F7=clear F8=petite map F9=grande map");
        }

        void SetupCamera()
        {
            var camGO = GameObject.Find("Main Camera");
            if (camGO == null)
            {
                camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
            }
            cam = camGO.GetComponent<Camera>();
            if (cam == null) cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.06f, 0.10f);
            cam.orthographic = true;
            cam.orthographicSize = 60;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;
            cam.transform.position = new Vector3(90, 120, 90);
            cam.transform.rotation = Quaternion.Euler(90, 0, 0);

            if (FindAnyObjectByType<Light>() == null)
            {
                var lightGO = new GameObject("Light");
                var l = lightGO.AddComponent<Light>();
                l.type = LightType.Directional;
                l.intensity = 1.2f;
                l.color = new Color(1f, 0.95f, 0.9f);
                lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
            }
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.f5Key.wasPressedThisFrame) GenerateAndRender();
            if (kb.f7Key.wasPressedThisFrame) { structureRenderer.Clear(); UnityEngine.Debug.Log("[Phase1] Clear OK"); }
            // Presets taille pour tester le cadrage
            if (kb.f8Key.wasPressedThisFrame) { config.mapWidth = 15; config.mapHeight = 15; config.minRooms = 3; config.maxRooms = 5; GenerateAndRender(); }
            if (kb.f9Key.wasPressedThisFrame) { config.mapWidth = 50; config.mapHeight = 50; config.minRooms = 10; config.maxRooms = 20; GenerateAndRender(); }

            // Camera controls
            var mouse = Mouse.current;
            if (mouse == null || cam == null) return;

            float scroll = mouse.scroll.y.ReadValue();
            if (scroll > 0f)
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize * 0.85f, 3f, 300f);
            else if (scroll < 0f)
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize * 1.15f, 3f, 300f);

            if (mouse.rightButton.isPressed)
            {
                var d = mouse.delta.ReadValue();
                float speed = cam.orthographicSize * 0.002f;
                cam.transform.Translate(-d.x * speed, 0, -d.y * speed, Space.World);
            }
        }

        void GenerateAndRender()
        {
            genCount++;
            structureRenderer.Clear();

            var (map, result) = generator.Generate(config);
            structureRenderer.Render(map, config);

            // Cadrage : la structure entiere doit etre visible avec marge
            FitCamera();

            // Validation
            bool spawnOK = structureRenderer.HasSpawnMarker;
            bool exitOK = structureRenderer.HasExitMarker;
            bool floorOK = structureRenderer.RenderedFloorCount >= 10;
            bool wallOK = structureRenderer.RenderedWallCount >= 10;
            bool allOK = structureRenderer.HasRendered && spawnOK && exitOK && floorOK && wallOK;

            UnityEngine.Debug.Log(
                $"[Phase1] Gen #{genCount} | Seed:{result.seed} | " +
                $"{result.roomCount} salles, {result.corridorCount} couloirs | " +
                $"Cells:{structureRenderer.RenderedCellCount} " +
                $"Floor:{structureRenderer.RenderedFloorCount} Wall:{structureRenderer.RenderedWallCount} | " +
                $"Spawn:{(spawnOK ? "OK" : "MANQUANT")} Exit:{(exitOK ? "OK" : "MANQUANT")} | " +
                $"Map:{config.mapWidth}x{config.mapHeight} | " +
                $"{(allOK ? "PHASE1 OK" : "PHASE1 ECHEC")}");
        }

        void FitCamera()
        {
            var c = structureRenderer.StructureCenter;
            var s = structureRenderer.StructureSize;
            cam.transform.position = new Vector3(c.x, 120, c.z);
            // Marge de 10% pour que les bords ne soient pas colles
            cam.orthographicSize = Mathf.Max(s.x, s.z) * 0.6f;
        }
    }
}
