using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace DonGeonMaster.MapGeneration.DebugTools
{
    public enum DebugMode { Config, TopDown, Hero }

    public class MapGenDebugModeController : MonoBehaviour
    {
        DebugMode currentMode = DebugMode.Config;
        MapData currentMap;
        GenerationResult currentResult;
        MapGenConfig currentConfig;

        MapGenerator generator;
        GenerationValidator validator;
        MapStructureDebugRenderer structureRenderer;
        HeroDebugBridge heroBridge;
        BatchTestRunner batchRunner;
        AssetPlacer assetPlacer;
        [SerializeField] AssetCategoryRegistry assetRegistry;
        MapDebugSidebarUI sidebar;
        MapDebugOverlayUI overlay;
        Camera cam;
        Transform assetRoot;

        void Start()
        {
            generator = new MapGenerator();
            validator = new GenerationValidator();
            assetPlacer = new AssetPlacer();

            structureRenderer = gameObject.GetComponent<MapStructureDebugRenderer>();
            if (structureRenderer == null)
                structureRenderer = gameObject.AddComponent<MapStructureDebugRenderer>();

            heroBridge = gameObject.GetComponent<HeroDebugBridge>();
            if (heroBridge == null)
                heroBridge = gameObject.AddComponent<HeroDebugBridge>();

            SetupCamera();
            EnsureEventSystem();
            BuildUI();
            SetMode(DebugMode.Config);

            UnityEngine.Debug.Log("[ModeController] Pret. F5=generer Tab=sidebar F10=heros");
        }

        // ════════════════════════════════════════════
        //  SETUP
        // ════════════════════════════════════════════

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

        void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();
            UnityEngine.Debug.Log("[ModeController] EventSystem + InputSystemUIInputModule cree");
        }

        void BuildUI()
        {
            sidebar = gameObject.AddComponent<MapDebugSidebarUI>();
            sidebar.Build();
            sidebar.OnGenerate = Generate;
            sidebar.OnRegenerate = Regenerate;
            sidebar.OnClear = Clear;
            sidebar.OnHero = EnterHeroMode;
            sidebar.OnExportLog = ExportLog;
            sidebar.OnOpenLogs = () => GenerationLogger.OpenLogFolder();
            sidebar.OnCopySeed = CopySeed;
            sidebar.OnSavePreset = SavePreset;
            sidebar.OnLoadPreset = LoadPreset;
            sidebar.OnBatchStart = StartBatch;
            sidebar.OnBatchCancel = CancelBatch;
            sidebar.OnReplaySeed = ReplaySeed;

            // Setup categories si le registry est assigne
            if (assetRegistry != null)
                sidebar.SetupCategories(assetRegistry);

            overlay = gameObject.AddComponent<MapDebugOverlayUI>();
            overlay.Build();

            // BatchTestRunner
            batchRunner = gameObject.GetComponent<BatchTestRunner>();
            if (batchRunner == null)
                batchRunner = gameObject.AddComponent<BatchTestRunner>();
            batchRunner.OnStatusUpdate += s => sidebar.UpdateBatchStatus(s);
            batchRunner.OnBatchComplete += m =>
            {
                sidebar.UpdateBatchStatus(
                    $"Termine: {m.successes}OK {m.warnings}W {m.failures}E\n" +
                    $"Seeds echec: [{string.Join(", ", m.failedSeeds)}]");
            };
        }

        // ════════════════════════════════════════════
        //  F5 : GENERATION
        // ════════════════════════════════════════════

        public void Generate()
        {
            heroBridge.Deactivate(cam);
            heroBridge.DestroyHero();

            // Lire config depuis la sidebar
            currentConfig = sidebar.ReadConfig();
            structureRenderer.Clear();

            var (map, result) = generator.Generate(currentConfig);
            currentMap = map;
            currentResult = result;

            if (currentConfig.validateAfterGeneration)
                validator.Validate(map, currentConfig, result);

            structureRenderer.useRealGround = sidebar.RealGroundEnabled;
            // Fournir les prefabs TileGround depuis le registry pour le mode sols reels
            if (assetRegistry != null)
            {
                var solsCat = assetRegistry.GetCategory("Sols");
                structureRenderer.floorPrefabs = solsCat != null ? solsCat.prefabs : null;
            }
            structureRenderer.Render(map, currentConfig);

            // Placer les assets par dessus le blockout si active
            ClearAssets();

            int registryCatCount = assetRegistry != null ? assetRegistry.categories.Count : 0;
            var sidebarCats = sidebar.GetEnabledCategories();
            int sidebarCatCount = sidebarCats != null ? sidebarCats.Count : 0;

            UnityEngine.Debug.Log($"[ModeController] === PRE-PLACEMENT === " +
                $"PlaceAssetsEnabled={sidebar.PlaceAssetsEnabled} | " +
                $"assetRegistry={(assetRegistry != null ? "OK" : "NULL")} ({registryCatCount} categories) | " +
                $"sidebar categories activees: {sidebarCatCount} | " +
                $"seed={currentConfig.seed}");

            if (!sidebar.PlaceAssetsEnabled)
            {
                UnityEngine.Debug.LogWarning("[ModeController] Placement NON execute: toggle 'Placer les assets' est OFF");
            }
            else if (assetRegistry == null)
            {
                UnityEngine.Debug.LogWarning("[ModeController] Placement NON execute: assetRegistry est NULL " +
                    "(assigner le champ dans l'Inspector de la scene MapGenDebug)");
            }
            else
            {
                var rootGO = new GameObject("AssetLayer");
                assetRoot = rootGO.transform;
                assetPlacer.Initialize(assetRoot, currentConfig, currentConfig.seed, result);
                assetPlacer.skipStructuralCategories = true;

                // Passer les infos de rendu sol au placer pour les dumps
                var lookup = new Dictionary<(int, int), MapStructureDebugRenderer.CellRenderInfo>();
                foreach (var ci in structureRenderer.cellRenderInfos)
                    lookup[(ci.x, ci.y)] = ci;
                assetPlacer.cellRenderLookup = lookup;

                int placed = assetPlacer.PlaceAssets(map, assetRegistry);

                // Infos sol pour le dump
                PlacementDebugDump.SetGroundRenderInfo(
                    structureRenderer.useRealGround,
                    structureRenderer.RealGroundFloorCount,
                    structureRenderer.RealGroundCorridorCount,
                    structureRenderer.BlockoutCellCount,
                    structureRenderer.cellRenderInfos);

                // Export debug dump (ecrase les fichiers precedents)
                PlacementDebugDump.Export();

                UnityEngine.Debug.Log($"[ModeController] === POST-PLACEMENT === " +
                    $"Total objets places: {placed}");

                if (placed == 0)
                    UnityEngine.Debug.LogWarning("[ModeController] PlaceAssets() a retourne 0 — voir synthese [AssetPlacer] ci-dessus");
            }

            FitCamera();
            SetMode(DebugMode.TopDown);

            // Mettre a jour l'UI
            overlay.UpdateStats(result, structureRenderer);
            sidebar.WriteSeed(result.seed);
            sidebar.RecordSeed(result.seed);

            // Log automatique sur disque
            GenerationLogger.WriteLog(map, currentConfig, result);

            UnityEngine.Debug.Log(
                $"[ModeController] {result.status} | Seed:{result.seed} | " +
                $"Salles:{result.roomCount} Couloirs:{result.corridorCount} | " +
                $"Cells:{structureRenderer.RenderedCellCount} | " +
                $"E:{result.errorCount} W:{result.warningCount}");
        }

        void Regenerate()
        {
            // Forcer une nouvelle seed
            var cfg = sidebar.ReadConfig();
            cfg.useRandomSeed = true;
            cfg.seed = 0;
            // Ecrire seed 0 pour forcer random, puis generer
            sidebar.WriteSeed(0);
            Generate();
        }

        void Clear()
        {
            heroBridge.Deactivate(cam);
            heroBridge.DestroyHero();
            structureRenderer.Clear();
            ClearAssets();
            currentMap = null;
            currentResult = null;
            FitCamera();
            SetMode(DebugMode.Config);
        }

        void ClearAssets()
        {
            if (assetRoot != null)
            {
                DestroyImmediate(assetRoot.gameObject);
                assetRoot = null;
            }
        }

        // ════════════════════════════════════════════
        //  OUTILS
        // ════════════════════════════════════════════

        void ExportLog()
        {
            if (currentMap == null || currentResult == null)
            { UnityEngine.Debug.LogWarning("[ModeController] Rien a exporter"); return; }
            var path = GenerationLogger.WriteLog(currentMap, currentConfig, currentResult);
            UnityEngine.Debug.Log($"[ModeController] Log exporte: {path}");
        }

        void CopySeed()
        {
            if (currentResult == null) return;
            GUIUtility.systemCopyBuffer = currentResult.seed.ToString();
            UnityEngine.Debug.Log($"[ModeController] Seed copiee: {currentResult.seed}");
        }

        void SavePreset(string name)
        {
            var cfg = sidebar.ReadConfig();
            PresetManager.SavePreset(new GenerationPreset(name, cfg));
            UnityEngine.Debug.Log($"[ModeController] Preset sauve: {name}");
        }

        void LoadPreset(string name)
        {
            var preset = PresetManager.LoadPreset(name);
            if (preset == null) return;
            sidebar.ApplyConfig(preset.config);
            UnityEngine.Debug.Log($"[ModeController] Preset charge: {name}");
        }

        void StartBatch(int iterations)
        {
            if (batchRunner.isRunning) return;
            var cfg = sidebar.ReadConfig();
            batchRunner.StartBatch(cfg, iterations);
            sidebar.UpdateBatchStatus($"Batch demarre: {iterations} iterations...");
        }

        void CancelBatch()
        {
            batchRunner.Cancel();
        }

        void ReplaySeed(int seed)
        {
            sidebar.WriteSeed(seed);
            Generate();
        }

        // ════════════════════════════════════════════
        //  F10 : HERO MODE
        // ════════════════════════════════════════════

        void EnterHeroMode()
        {
            if (currentMap == null)
            {
                UnityEngine.Debug.LogWarning("[ModeController] F5 d'abord");
                return;
            }
            heroBridge.Activate(currentMap, currentConfig, cam);
            if (!heroBridge.IsActive) return;
            SetMode(DebugMode.Hero);
        }

        void ExitHeroMode()
        {
            heroBridge.Deactivate(cam);
            FitCamera();
            SetMode(DebugMode.TopDown);
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
                    sidebar.Show();
                    overlay.Hide();
                    break;
                case DebugMode.TopDown:
                    sidebar.Hide();
                    overlay.Show();
                    break;
                case DebugMode.Hero:
                    sidebar.Hide();
                    overlay.Hide();
                    break;
            }
        }

        void ToggleSidebar()
        {
            if (currentMode == DebugMode.Hero) return;
            if (sidebar.IsVisible)
            {
                sidebar.Hide();
                if (currentResult != null) overlay.Show();
            }
            else
            {
                sidebar.Show();
                overlay.Hide();
            }
        }

        // ════════════════════════════════════════════
        //  CAMERA
        // ════════════════════════════════════════════

        void FitCamera()
        {
            if (cam == null) return;
            cam.enabled = true;
            cam.orthographic = true;
            cam.transform.rotation = Quaternion.Euler(90, 0, 0);

            if (structureRenderer.HasRendered)
            {
                var c = structureRenderer.StructureCenter;
                var s = structureRenderer.StructureSize;
                cam.transform.position = new Vector3(c.x, 120, c.z);
                cam.orthographicSize = Mathf.Max(s.x, s.z) * 0.6f;
            }
        }

        // ════════════════════════════════════════════
        //  UPDATE
        // ════════════════════════════════════════════

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.f5Key.wasPressedThisFrame) Generate();
                if (kb.f6Key.wasPressedThisFrame) Regenerate();
                if (kb.f7Key.wasPressedThisFrame) Clear();
                if (kb.tabKey.wasPressedThisFrame) ToggleSidebar();
                if (kb.f10Key.wasPressedThisFrame)
                {
                    if (currentMode == DebugMode.Hero) ExitHeroMode();
                    else EnterHeroMode();
                }
            }

            // Zoom + pan en TopDown ou Config (pas en Hero)
            if (currentMode == DebugMode.Hero || cam == null || !cam.orthographic) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            const float zoomStep = 0.85f;
            float scroll = mouse.scroll.y.ReadValue();
            if (scroll > 0f)
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize * zoomStep, 3f, 300f);
            else if (scroll < 0f)
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize / zoomStep, 3f, 300f);

            if (mouse.rightButton.isPressed)
            {
                var d = mouse.delta.ReadValue();
                float speed = cam.orthographicSize * 0.002f;
                cam.transform.Translate(-d.x * speed, 0, -d.y * speed, Space.World);
            }
        }

        // ════════════════════════════════════════════
        //  API publique
        // ════════════════════════════════════════════

        public MapData CurrentMap => currentMap;
        public GenerationResult CurrentResult => currentResult;
        public MapGenConfig CurrentConfig => currentConfig;
        public DebugMode CurrentMode => currentMode;
        public MapStructureDebugRenderer Renderer => structureRenderer;
    }
}
