using UnityEngine;
using UnityEngine.InputSystem;

namespace DonGeonMaster.MapGeneration.DebugTools
{
    public enum DebugMode { Config, TopDown }

    /// <summary>
    /// Phase 2 : controleur central du debug de generation.
    /// Orchestre Config (lancement) et TopDown (apres F5).
    /// Pas de hero, pas de sidebar UI, pas d'assets decoratifs.
    /// </summary>
    public class MapGenDebugModeController : MonoBehaviour
    {
        // ── Etat ──
        DebugMode currentMode = DebugMode.Config;
        MapData currentMap;
        GenerationResult currentResult;
        MapGenConfig currentConfig;

        // ── Systemes ──
        MapGenerator generator;
        GenerationValidator validator;
        MapStructureDebugRenderer structureRenderer;
        Camera cam;

        // ── Config par defaut (remplacee par sidebar en phase 4) ──
        MapGenConfig defaultConfig;

        void Start()
        {
            generator = new MapGenerator();
            validator = new GenerationValidator();
            structureRenderer = gameObject.GetComponent<MapStructureDebugRenderer>();
            if (structureRenderer == null)
                structureRenderer = gameObject.AddComponent<MapStructureDebugRenderer>();

            SetupCamera();
            defaultConfig = new MapGenConfig
            {
                mapWidth = 30, mapHeight = 30, cellSize = 6,
                minRooms = 5, maxRooms = 10,
                minRoomSize = 3, maxRoomSize = 8,
                borderMargin = 2, corridorWidth = 2,
                useRandomSeed = true, validateAfterGeneration = true
            };

            currentMode = DebugMode.Config;
            UnityEngine.Debug.Log("[ModeController] Phase 2 pret. F5=generer Tab=sidebar(phase4)");
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

        // ════════════════════════════════════════════
        //  GENERATION (F5)
        // ════════════════════════════════════════════

        public void Generate()
        {
            // Config (sera lue depuis la sidebar en phase 4)
            currentConfig = defaultConfig.Clone();

            // Cleanup
            structureRenderer.Clear();

            // Generation
            var (map, result) = generator.Generate(currentConfig);
            currentMap = map;
            currentResult = result;

            // Validation
            if (currentConfig.validateAfterGeneration)
                validator.Validate(map, currentConfig, result);

            // Rendu structurel
            structureRenderer.Render(map, currentConfig);

            // Cadrage camera
            FitCamera();

            // Mode TopDown
            SetMode(DebugMode.TopDown);

            // Log
            string status = result.status.ToString();
            UnityEngine.Debug.Log(
                $"[ModeController] Generation {status} | Seed:{result.seed} | " +
                $"Salles:{result.roomCount} Couloirs:{result.corridorCount} | " +
                $"Cells:{structureRenderer.RenderedCellCount} " +
                $"Floor:{structureRenderer.RenderedFloorCount} Wall:{structureRenderer.RenderedWallCount} | " +
                $"Spawn:{structureRenderer.HasSpawnMarker} Exit:{structureRenderer.HasExitMarker} | " +
                $"Erreurs:{result.errorCount} Warnings:{result.warningCount}");
        }

        // ════════════════════════════════════════════
        //  MODES
        // ════════════════════════════════════════════

        void SetMode(DebugMode mode)
        {
            currentMode = mode;
            switch (mode)
            {
                case DebugMode.Config:
                    // Phase 4 : afficher sidebar ici
                    UnityEngine.Debug.Log("[ModeController] Mode: Config");
                    break;

                case DebugMode.TopDown:
                    // Phase 4 : masquer sidebar ici
                    UnityEngine.Debug.Log("[ModeController] Mode: TopDown");
                    break;
            }
        }

        void ToggleSidebar()
        {
            // Phase 4 : toggle sidebar UI
            // Pour l'instant : toggle le mode
            if (currentMode == DebugMode.Config)
                SetMode(DebugMode.TopDown);
            else
                SetMode(DebugMode.Config);
        }

        // ════════════════════════════════════════════
        //  CAMERA
        // ════════════════════════════════════════════

        void FitCamera()
        {
            if (cam == null || !structureRenderer.HasRendered) return;
            var c = structureRenderer.StructureCenter;
            var s = structureRenderer.StructureSize;
            cam.transform.position = new Vector3(c.x, 120, c.z);
            cam.orthographicSize = Mathf.Max(s.x, s.z) * 0.6f;
        }

        // ════════════════════════════════════════════
        //  UPDATE : inputs
        // ════════════════════════════════════════════

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.f5Key.wasPressedThisFrame) Generate();
                if (kb.tabKey.wasPressedThisFrame) ToggleSidebar();
            }

            // Zoom + pan uniquement en TopDown
            if (currentMode != DebugMode.TopDown || cam == null) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            // Zoom multiplicatif
            float scroll = mouse.scroll.y.ReadValue();
            if (scroll > 0f)
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize * 0.85f, 3f, 300f);
            else if (scroll < 0f)
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize * 1.15f, 3f, 300f);

            // Pan clic droit
            if (mouse.rightButton.isPressed)
            {
                var d = mouse.delta.ReadValue();
                float speed = cam.orthographicSize * 0.002f;
                cam.transform.Translate(-d.x * speed, 0, -d.y * speed, Space.World);
            }
        }

        // ════════════════════════════════════════════
        //  API publique (pour phases suivantes)
        // ════════════════════════════════════════════

        public MapData CurrentMap => currentMap;
        public GenerationResult CurrentResult => currentResult;
        public MapGenConfig CurrentConfig => currentConfig;
        public DebugMode CurrentMode => currentMode;
        public MapStructureDebugRenderer Renderer => structureRenderer;
    }
}
