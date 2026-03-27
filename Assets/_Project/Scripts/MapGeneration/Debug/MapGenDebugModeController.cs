using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DonGeonMaster.MapGeneration.DebugTools
{
    public enum DebugMode { Config, TopDown }

    public class MapGenDebugModeController : MonoBehaviour
    {
        DebugMode currentMode = DebugMode.Config;
        MapData currentMap;
        GenerationResult currentResult;
        MapGenConfig currentConfig;
        MapGenConfig defaultConfig;

        MapGenerator generator;
        GenerationValidator validator;
        MapStructureDebugRenderer structureRenderer;
        Camera cam;
        GameObject sidebarPanel;

        void Start()
        {
            // Init systemes (avant toute UI)
            generator = new MapGenerator();
            validator = new GenerationValidator();
            structureRenderer = gameObject.GetComponent<MapStructureDebugRenderer>();
            if (structureRenderer == null)
                structureRenderer = gameObject.AddComponent<MapStructureDebugRenderer>();

            defaultConfig = new MapGenConfig
            {
                mapWidth = 30, mapHeight = 30, cellSize = 6,
                minRooms = 5, maxRooms = 10,
                minRoomSize = 3, maxRoomSize = 8,
                borderMargin = 2, corridorWidth = 2,
                useRandomSeed = true, validateAfterGeneration = true
            };

            SetupCamera();

            // UI en dernier (si ca plante, le reste fonctionne quand meme)
            try { CreateSidebarPlaceholder(); }
            catch (System.Exception e) { UnityEngine.Debug.LogError($"[ModeController] Sidebar UI error: {e.Message}"); }

            SetMode(DebugMode.Config);
            UnityEngine.Debug.Log("[ModeController] Pret. F5=generer Tab=toggle sidebar");
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

        void CreateSidebarPlaceholder()
        {
            // Canvas
            var canvasGO = new GameObject("DebugCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Sidebar : ancre a gauche, 350px, toute la hauteur
            sidebarPanel = new GameObject("SidebarPlaceholder");
            sidebarPanel.transform.SetParent(canvasGO.transform, false);
            var rt = sidebarPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 0.5f);
            rt.sizeDelta = new Vector2(350, 0);
            sidebarPanel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

            // Title : Image sur un GO, TextMeshProUGUI sur un enfant
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(sidebarPanel.transform, false);
            var titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 0.92f);
            titleRT.anchorMax = new Vector2(1, 1);
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;
            titleGO.AddComponent<Image>().color = new Color(0.12f, 0.20f, 0.35f);

            // TitleText : enfant de Title, TMP uniquement
            var titleTextGO = new GameObject("TitleText");
            titleTextGO.transform.SetParent(titleGO.transform, false);
            var ttRT = titleTextGO.AddComponent<RectTransform>();
            ttRT.anchorMin = Vector2.zero;
            ttRT.anchorMax = Vector2.one;
            ttRT.offsetMin = Vector2.zero;
            ttRT.offsetMax = Vector2.zero;
            var titleTxt = titleTextGO.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "MAP GEN DEBUG";
            titleTxt.fontSize = 18;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.color = Color.white;
            titleTxt.alignment = TextAlignmentOptions.Center;

            // Info : TMP seul (pas d'Image)
            var infoGO = new GameObject("Info");
            infoGO.transform.SetParent(sidebarPanel.transform, false);
            var infoRT = infoGO.AddComponent<RectTransform>();
            infoRT.anchorMin = new Vector2(0, 0.5f);
            infoRT.anchorMax = new Vector2(1, 0.85f);
            infoRT.offsetMin = new Vector2(16, 0);
            infoRT.offsetMax = new Vector2(-16, 0);
            var infoTxt = infoGO.AddComponent<TextMeshProUGUI>();
            infoTxt.text =
                "F5  =  Generer la map\n" +
                "Tab  =  Toggle ce panneau\n\n" +
                "Apres F5 :\n" +
                "  Molette = Zoom\n" +
                "  Clic droit = Pan\n\n" +
                "Config 30x30, 5-10 salles\n" +
                "(Sidebar complete en phase 4)";
            infoTxt.fontSize = 14;
            infoTxt.color = new Color(0.7f, 0.7f, 0.75f);
            infoTxt.alignment = TextAlignmentOptions.TopLeft;
        }

        // ════════════════════════════════════════════
        //  GENERATION (F5)
        // ════════════════════════════════════════════

        public void Generate()
        {
            if (defaultConfig == null)
            {
                UnityEngine.Debug.LogError("[ModeController] defaultConfig null, Start() a echoue");
                return;
            }

            currentConfig = defaultConfig.Clone();
            structureRenderer.Clear();

            var (map, result) = generator.Generate(currentConfig);
            currentMap = map;
            currentResult = result;

            if (currentConfig.validateAfterGeneration)
                validator.Validate(map, currentConfig, result);

            structureRenderer.Render(map, currentConfig);
            FitCamera();
            SetMode(DebugMode.TopDown);

            UnityEngine.Debug.Log(
                $"[ModeController] {result.status} | Seed:{result.seed} | " +
                $"Salles:{result.roomCount} Couloirs:{result.corridorCount} | " +
                $"Cells:{structureRenderer.RenderedCellCount} | " +
                $"E:{result.errorCount} W:{result.warningCount}");
        }

        // ════════════════════════════════════════════
        //  MODES
        // ════════════════════════════════════════════

        void SetMode(DebugMode mode)
        {
            currentMode = mode;
            if (sidebarPanel != null)
                sidebarPanel.SetActive(mode == DebugMode.Config);
        }

        void ToggleSidebar()
        {
            SetMode(currentMode == DebugMode.Config ? DebugMode.TopDown : DebugMode.Config);
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
        //  UPDATE
        // ════════════════════════════════════════════

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.f5Key.wasPressedThisFrame) Generate();
                if (kb.tabKey.wasPressedThisFrame) ToggleSidebar();
            }

            if (currentMode != DebugMode.TopDown || cam == null) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

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
