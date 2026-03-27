using System;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DonGeonMaster.MapGeneration
{
    public class MapGenDebugController : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField] AssetCategoryRegistry assetRegistry;
        [SerializeField] Camera mainCamera;
        [SerializeField] Camera overviewCamera;

        // Composants
        MapGenDebugUI debugUI;
        MapCleanupService cleanupService;
        PlayerSpawnService spawnService;
        BatchTestRunner batchRunner;
        DebugVisualization debugVis;

        // Systèmes
        MapGenerator generator;
        AssetPlacer assetPlacer;
        GenerationValidator validator;

        // État
        MapData currentMap;
        GenerationResult currentResult;
        MapGenConfig lastConfig;
        CameraMode cameraMode = CameraMode.VueDEnsemble;

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
            SetupOverviewCamera();
        }

        void SetupScene()
        {
            // Canvas + EventSystem
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                canvas = DebugUIBuilder.CreateCanvas("MapGenDebugCanvas");
            }

            // Composants
            debugUI = canvas.gameObject.GetComponent<MapGenDebugUI>();
            if (debugUI == null)
                debugUI = canvas.gameObject.AddComponent<MapGenDebugUI>();

            cleanupService = GetComponent<MapCleanupService>();
            if (cleanupService == null)
                cleanupService = gameObject.AddComponent<MapCleanupService>();

            spawnService = GetComponent<PlayerSpawnService>();
            if (spawnService == null)
                spawnService = gameObject.AddComponent<PlayerSpawnService>();

            batchRunner = GetComponent<BatchTestRunner>();
            if (batchRunner == null)
                batchRunner = gameObject.AddComponent<BatchTestRunner>();

            debugVis = GetComponent<DebugVisualization>();
            if (debugVis == null)
                debugVis = gameObject.AddComponent<DebugVisualization>();

            // Caméra
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    var camGo = new GameObject("MainCamera");
                    camGo.tag = "MainCamera";
                    mainCamera = camGo.AddComponent<Camera>();
                    mainCamera.clearFlags = CameraClearFlags.SolidColor;
                    mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
                }
            }

            // Lumière
            if (FindAnyObjectByType<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.color = new Color(1f, 0.95f, 0.85f);
                lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
            }

            // Batch events
            batchRunner.OnStatusUpdate += status => debugUI.UpdateBatchStatus(status);
            batchRunner.OnBatchComplete += metrics =>
            {
                debugUI.UpdateBatchStatus(metrics.BuildReport());
            };
        }

        void SetupOverviewCamera()
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
            if (lastConfig == null)
            {
                Generate();
                return;
            }
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
            Debug.Log("[MapGenDebug] Map nettoyée");
        }

        public void SpawnPlayer()
        {
            if (currentMap == null)
            {
                Debug.LogWarning("[MapGenDebug] Générez une map d'abord");
                return;
            }
            spawnService.SpawnPlayer(currentMap, lastConfig);
        }

        public void RespawnPlayer()
        {
            spawnService.RespawnPlayer();
        }

        public void RunValidation()
        {
            if (currentMap == null || currentResult == null)
            {
                Debug.LogWarning("[MapGenDebug] Pas de map à valider");
                return;
            }
            currentResult.validationEntries.Clear();
            validator.Validate(currentMap, lastConfig, currentResult);
            debugUI.UpdateResults(currentResult);
            Debug.Log($"[MapGenDebug] Validation: {currentResult.errorCount}E / {currentResult.warningCount}W");
        }

        public void ExportLog()
        {
            if (currentMap == null || currentResult == null)
            {
                Debug.LogWarning("[MapGenDebug] Pas de données à exporter");
                return;
            }
            string path = GenerationLogger.WriteLog(currentMap, lastConfig, currentResult);
            Debug.Log($"[MapGenDebug] Log exporté: {path}");
        }

        public void TakeScreenshot()
        {
            string folder = Path.Combine(Application.dataPath, "..", "MapGenScreenshots");
            Directory.CreateDirectory(folder);
            string filename = $"MapGen_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
            if (currentResult != null)
                filename += $"_seed{currentResult.seed}";
            string path = Path.Combine(folder, filename + ".png");
            ScreenCapture.CaptureScreenshot(path, 2);
            Debug.Log($"[MapGenDebug] Screenshot: {path}");
        }

        public void CopySummary()
        {
            if (currentResult == null)
            {
                Debug.LogWarning("[MapGenDebug] Pas de résultat à copier");
                return;
            }
            GUIUtility.systemCopyBuffer = currentResult.BuildSummary();
            Debug.Log("[MapGenDebug] Résumé copié dans le presse-papiers");
        }

        public void ToggleCameraMode()
        {
            if (cameraMode == CameraMode.VueDEnsemble && spawnService.CurrentPlayer != null)
            {
                cameraMode = CameraMode.SuiviJoueur;
                mainCamera.orthographic = false;
                mainCamera.fieldOfView = 60;
            }
            else
            {
                cameraMode = CameraMode.VueDEnsemble;
                FitCameraToMap();
            }
        }

        public void SavePreset(string name)
        {
            var config = debugUI.ReadConfig();
            var preset = new GenerationPreset(name, config);
            PresetManager.SavePreset(preset);
            Debug.Log($"[MapGenDebug] Preset sauvegardé: {name}");
        }

        public void ResetToDefaults()
        {
            debugUI.ApplyConfig(new MapGenConfig());
        }

        public void RunBatchTest(int iterations)
        {
            var config = debugUI.ReadConfig();
            batchRunner.StartBatch(config, iterations);
        }

        public void CancelBatch()
        {
            batchRunner.Cancel();
        }

        // ===================== LOGIQUE INTERNE =====================

        void ExecuteGeneration(MapGenConfig config)
        {
            // Nettoyage
            cleanupService.ClearMap();
            spawnService.DespawnPlayer();

            lastConfig = config.Clone();

            // Génération
            var (map, result) = generator.Generate(config);
            currentMap = map;
            currentResult = result;

            result.AddPipelineStep("Placement des assets en scène");

            // Placement d'assets
            bool placeAssets = config.mode != GenerationMode.StructureSeule;
            if (placeAssets && assetRegistry != null)
            {
                var root = cleanupService.MapRoot;
                assetPlacer.Initialize(root, config, config.seed, result);
                assetPlacer.PlaceAssets(map, assetRegistry);
            }

            // Validation
            if (config.validateAfterGeneration)
            {
                validator.Validate(map, config, result);
            }

            // Log automatique
            GenerationLogger.WriteLog(map, config, result);

            // Auto-spawn joueur
            spawnService.SpawnPlayer(map, config);

            // Mise à jour de la visualisation debug
            debugVis.SetData(map, config, result);

            // Mise à jour UI
            debugUI.UpdateResults(result);

            // Ajuster la caméra
            FitCameraToMap();

            // Screenshot auto si échec
            if (result.status == GenerationStatus.Echec)
            {
                TakeScreenshot();
                Debug.LogWarning($"[MapGenDebug] Génération échouée (seed: {result.seed}) - Screenshot auto pris");
            }

            string statusEmoji = result.status switch
            {
                GenerationStatus.Succes => "OK",
                GenerationStatus.SuccesAvecWarnings => "WARN",
                _ => "FAIL"
            };
            Debug.Log($"[MapGenDebug] Génération [{statusEmoji}] Seed:{result.seed} " +
                      $"Temps:{result.generationTimeMs:F1}ms Salles:{result.roomCount} " +
                      $"Objets:{result.totalObjectsPlaced}");
        }

        void FitCameraToMap()
        {
            if (mainCamera == null || lastConfig == null) return;

            float mapW = lastConfig.mapWidth * lastConfig.cellSize;
            float mapH = lastConfig.mapHeight * lastConfig.cellSize;
            float centerX = mapW * 0.5f;
            float centerZ = mapH * 0.5f;

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = Mathf.Max(mapW, mapH) * 0.55f;
            mainCamera.transform.position = new Vector3(centerX, 100, centerZ);
            mainCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
            cameraMode = CameraMode.VueDEnsemble;
        }

        void LateUpdate()
        {
            if (cameraMode == CameraMode.SuiviJoueur && spawnService.CurrentPlayer != null)
            {
                var target = spawnService.CurrentPlayer.transform.position;
                mainCamera.transform.position = target + new Vector3(0, 15, -10);
                mainCamera.transform.LookAt(target);
            }

            // Scroll zoom en mode vue d'ensemble
            if (cameraMode == CameraMode.VueDEnsemble && mainCamera.orthographic)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.001f)
                {
                    mainCamera.orthographicSize = Mathf.Clamp(
                        mainCamera.orthographicSize - scroll * 15f, 5f, 200f);
                }

                // Pan camera avec clic droit
                if (Input.GetMouseButton(1))
                {
                    float panSpeed = mainCamera.orthographicSize * 0.003f;
                    float dx = -Input.GetAxis("Mouse X") * panSpeed;
                    float dy = -Input.GetAxis("Mouse Y") * panSpeed;
                    mainCamera.transform.Translate(dx, 0, dy, Space.World);
                }
            }
        }
    }
}
