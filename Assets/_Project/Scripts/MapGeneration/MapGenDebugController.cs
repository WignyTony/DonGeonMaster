using System;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace DonGeonMaster.MapGeneration
{
    public class MapGenDebugController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] AssetCategoryRegistry assetRegistry;
        [SerializeField] Camera mainCamera;

        // Composants
        MapGenDebugUI debugUI;
        MapCleanupService cleanupService;
        PlayerSpawnService spawnService;
        BatchTestRunner batchRunner;
        DebugVisualization debugVis;

        // Systemes
        MapGenerator generator;
        AssetPlacer assetPlacer;
        GenerationValidator validator;

        // Etat
        MapData currentMap;
        GenerationResult currentResult;
        MapGenConfig lastConfig;
        DebugViewMode viewMode = DebugViewMode.Config;

        void Awake()
        {
            SetupScene();
            generator = new MapGenerator();
            assetPlacer = new AssetPlacer();
            validator = new GenerationValidator();
        }

        void Start()
        {
            debugUI.Initialize(this);
            if (assetRegistry != null)
                debugUI.RefreshCategories(assetRegistry);

            // Etat initial : mode config, camera top-down sur zone vide
            SetupDefaultCamera();
            debugUI.SetMode(DebugViewMode.Config);
        }

        // ===================== SETUP =====================

        void SetupScene()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
                canvas = DebugUIBuilder.CreateCanvas("MapGenDebugCanvas");

            debugUI = canvas.gameObject.GetComponent<MapGenDebugUI>();
            if (debugUI == null)
                debugUI = canvas.gameObject.AddComponent<MapGenDebugUI>();

            cleanupService = GetComponent<MapCleanupService>() ?? gameObject.AddComponent<MapCleanupService>();
            spawnService = GetComponent<PlayerSpawnService>() ?? gameObject.AddComponent<PlayerSpawnService>();
            batchRunner = GetComponent<BatchTestRunner>() ?? gameObject.AddComponent<BatchTestRunner>();
            debugVis = GetComponent<DebugVisualization>() ?? gameObject.AddComponent<DebugVisualization>();

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    var camGo = new GameObject("MainCamera");
                    camGo.tag = "MainCamera";
                    mainCamera = camGo.AddComponent<Camera>();
                    mainCamera.clearFlags = CameraClearFlags.SolidColor;
                    mainCamera.backgroundColor = new Color(0.08f, 0.10f, 0.13f);
                }
            }

            if (FindAnyObjectByType<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.color = new Color(1f, 0.95f, 0.85f);
                lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
            }

            batchRunner.OnStatusUpdate += s => debugUI.UpdateBatchStatus(s);
            batchRunner.OnBatchComplete += m => debugUI.UpdateBatchStatus(m.BuildReport());
        }

        void SetupDefaultCamera()
        {
            if (mainCamera == null) return;
            mainCamera.transform.position = new Vector3(45, 80, 45);
            mainCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 50;
        }

        // ===================== ACTIONS PUBLIQUES =====================

        public void Generate()
        {
            var config = debugUI.ReadConfig();
            ExecuteGeneration(config);
        }

        public void Regenerate()
        {
            var config = debugUI.ReadConfig();
            config.useRandomSeed = true;
            config.lockSeed = false;
            ExecuteGeneration(config);
        }

        public void GenerateSameSeed()
        {
            if (lastConfig == null) { Generate(); return; }
            var config = debugUI.ReadConfig();
            config.seed = currentResult != null ? currentResult.seed : lastConfig.seed;
            config.useRandomSeed = false;
            ExecuteGeneration(config);
        }

        public void ClearMap()
        {
            cleanupService.ClearMap();
            spawnService.DespawnPlayer();
            currentMap = null;
            currentResult = null;
            debugVis.ClearData();
            // Revenir en mode config
            debugUI.SetMode(DebugViewMode.Config);
            viewMode = DebugViewMode.Config;
            SetupDefaultCamera();
        }

        public void SpawnPlayer()
        {
            if (currentMap == null)
            {
                Debug.LogWarning("[MapGenDebug] Generez une map d'abord");
                return;
            }
            spawnService.SpawnPlayer(currentMap, lastConfig);
        }

        public void RespawnPlayer() => spawnService.RespawnPlayer();

        /// <summary>F10 : passe en vue FPS. Spawn le joueur si necessaire.</summary>
        public void EnterFPSMode()
        {
            if (currentMap == null)
            {
                Debug.LogWarning("[MapGenDebug] Generez une map d'abord (F5)");
                return;
            }

            // Spawn joueur si pas deja present
            if (spawnService.CurrentPlayer == null)
                spawnService.SpawnPlayer(currentMap, lastConfig);

            if (spawnService.CurrentPlayer == null)
            {
                Debug.LogError("[MapGenDebug] Impossible de spawn le joueur");
                return;
            }

            // Activer mode FPS
            viewMode = DebugViewMode.FPS;
            debugUI.SetMode(DebugViewMode.FPS);
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = 60;

            // Desactiver le DebugPlayerMovement cursor lock
            var movement = spawnService.CurrentPlayer.GetComponent<DebugPlayerMovement>();
            if (movement) movement.enabled = true;
        }

        /// <summary>Revient en vue top-down depuis FPS.</summary>
        public void EnterTopDownMode()
        {
            viewMode = DebugViewMode.TopDown;
            debugUI.SetMode(DebugViewMode.TopDown);
            FitCameraToMap();

            // Desactiver controles joueur
            if (spawnService.CurrentPlayer != null)
            {
                var movement = spawnService.CurrentPlayer.GetComponent<DebugPlayerMovement>();
                if (movement) movement.enabled = false;
            }
            Cursor.lockState = CursorLockMode.None;
        }

        public void ToggleCameraMode()
        {
            if (viewMode == DebugViewMode.FPS)
                EnterTopDownMode();
            else
                EnterFPSMode();
        }

        public void RunValidation()
        {
            if (currentMap == null || currentResult == null) return;
            currentResult.validationEntries.Clear();
            validator.Validate(currentMap, lastConfig, currentResult);
            debugUI.UpdateResults(currentResult);
        }

        public void ExportLog()
        {
            if (currentMap == null || currentResult == null) return;
            GenerationLogger.WriteLog(currentMap, lastConfig, currentResult);
        }

        public void TakeScreenshot()
        {
            string folder = Path.Combine(Application.dataPath, "..", "MapGenScreenshots");
            Directory.CreateDirectory(folder);
            string name = $"MapGen_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
            if (currentResult != null) name += $"_seed{currentResult.seed}";
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png"), 2);
        }

        public void CopySummary()
        {
            if (currentResult != null)
                GUIUtility.systemCopyBuffer = currentResult.BuildSummary();
        }

        public void SavePreset(string name)
        {
            PresetManager.SavePreset(new GenerationPreset(name, debugUI.ReadConfig()));
        }

        public void ResetToDefaults() => debugUI.ApplyConfig(new MapGenConfig());

        public void RunBatchTest(int iterations) => batchRunner.StartBatch(debugUI.ReadConfig(), iterations);

        public void CancelBatch() => batchRunner.Cancel();

        // ===================== GENERATION =====================

        void ExecuteGeneration(MapGenConfig config)
        {
            cleanupService.ClearMap();
            spawnService.DespawnPlayer();
            lastConfig = config.Clone();

            // Generer
            var (map, result) = generator.Generate(config);
            currentMap = map;
            currentResult = result;

            // Placer assets
            result.AddPipelineStep("Placement des assets");
            if (config.mode != GenerationMode.StructureSeule && assetRegistry != null)
            {
                assetPlacer.Initialize(cleanupService.MapRoot, config, config.seed, result);
                assetPlacer.PlaceAssets(map, assetRegistry);
            }

            // Valider
            if (config.validateAfterGeneration)
                validator.Validate(map, config, result);

            // Log
            GenerationLogger.WriteLog(map, config, result);

            // Spawn joueur (pret pour F10)
            spawnService.SpawnPlayer(map, config);
            // Desactiver les controles joueur — on est en top-down
            if (spawnService.CurrentPlayer != null)
            {
                var movement = spawnService.CurrentPlayer.GetComponent<DebugPlayerMovement>();
                if (movement) movement.enabled = false;
            }

            // Gizmos
            debugVis.SetData(map, config, result);

            // UI
            debugUI.UpdateResults(result);

            // === PASSER EN MODE TOP-DOWN ===
            viewMode = DebugViewMode.TopDown;
            debugUI.SetMode(DebugViewMode.TopDown);
            FitCameraToMap();
            Cursor.lockState = CursorLockMode.None;

            // Screenshot auto si echec
            if (result.status == GenerationStatus.Echec)
                TakeScreenshot();

            Debug.Log($"[MapGenDebug] [{result.status}] Seed:{result.seed} " +
                      $"Temps:{result.generationTimeMs:F1}ms Salles:{result.roomCount} " +
                      $"Objets:{result.totalObjectsPlaced}");
        }

        void FitCameraToMap()
        {
            if (mainCamera == null || lastConfig == null) return;
            float mapW = lastConfig.mapWidth * lastConfig.cellSize;
            float mapH = lastConfig.mapHeight * lastConfig.cellSize;
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = Mathf.Max(mapW, mapH) * 0.55f;
            mainCamera.transform.position = new Vector3(mapW * 0.5f, 100, mapH * 0.5f);
            mainCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
        }

        // ===================== UPDATE =====================

        void LateUpdate()
        {
            // Mode FPS : camera suit le joueur
            if (viewMode == DebugViewMode.FPS && spawnService.CurrentPlayer != null)
            {
                var target = spawnService.CurrentPlayer.transform.position;
                mainCamera.transform.position = target + new Vector3(0, 15, -10);
                mainCamera.transform.LookAt(target);
            }

            // Mode TopDown : zoom + pan
            if (viewMode == DebugViewMode.TopDown && mainCamera != null && mainCamera.orthographic)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.001f)
                {
                    mainCamera.orthographicSize = Mathf.Clamp(
                        mainCamera.orthographicSize - scroll * 15f, 5f, 200f);
                }

                if (Input.GetMouseButton(1))
                {
                    float speed = mainCamera.orthographicSize * 0.004f;
                    float dx = -Input.GetAxis("Mouse X") * speed;
                    float dy = -Input.GetAxis("Mouse Y") * speed;
                    mainCamera.transform.Translate(dx, 0, dy, Space.World);
                }
            }
        }
    }
}
