using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonGeonMaster.MapGeneration
{
    public class MapGenDebugUI : MonoBehaviour
    {
        // Références internes
        MapGenDebugController controller;
        ScrollRect mainScroll;
        RectTransform scrollContent;

        // === Champs de saisie ===
        TMP_InputField fieldMapWidth, fieldMapHeight, fieldCellSize, fieldBorderMargin;
        TMP_InputField fieldMinRooms, fieldMaxRooms, fieldMinRoomSize, fieldMaxRoomSize;
        TMP_InputField fieldCorridorWidth, fieldMinSpawnExitDist;
        Slider sliderVegDensity, sliderRockDensity, sliderDecorDensity;
        TMP_InputField fieldSeed;
        Toggle toggleRandomSeed, toggleLockSeed;
        TMP_Dropdown dropdownMode, dropdownLayout, dropdownBiome;
        Toggle toggleForceBiome, toggleEnsureAccess, toggleValidate;
        Toggle toggleForceBoss, toggleForceSpecial;
        TMP_InputField fieldBatchCount;
        TMP_InputField fieldPresetName;

        // === Catégories ===
        Dictionary<string, Toggle> categoryToggles = new();
        RectTransform categoriesContent;

        // === Affichage résultats ===
        TextMeshProUGUI lblStatus, lblSeed, lblTime, lblRooms, lblCorridors;
        TextMeshProUGUI lblObjects, lblErrors, lblWarnings, lblSpawn, lblExit;
        TextMeshProUGUI lblDistance, lblBatchStatus;
        RectTransform logContent;
        RectTransform seedHistoryContent;

        // === Historique seeds ===
        List<int> seedHistory = new();
        const int MaxSeedHistory = 15;

        public void Initialize(MapGenDebugController ctrl)
        {
            controller = ctrl;
            BuildUI();
        }

        void BuildUI()
        {
            // Panel gauche (configuration) : 25% de l'écran
            var leftPanel = DebugUIBuilder.CreatePanel(transform, "LeftPanel", 0, 0, 0.28f, 1);

            mainScroll = DebugUIBuilder.CreateScrollView(leftPanel, "ConfigScroll");
            scrollContent = mainScroll.content;

            BuildSizeSection();
            BuildSeedSection();
            BuildModeSection();
            BuildConstraintsSection();
            BuildCategoriesSection();
            BuildActionsSection();
            BuildPresetSection();
            BuildBatchSection();
            BuildStatsSection();
            BuildLogSection();
            BuildSeedHistorySection();
        }

        // ===================== SECTIONS =====================

        void BuildSizeSection()
        {
            var (_, content) = DebugUIBuilder.CreateCollapsibleSection(scrollContent, "TAILLE DE LA MAP");

            (fieldMapWidth, _) = DebugUIBuilder.CreateInputFieldWithLabel(content, "Largeur", "30");
            fieldMapWidth.contentType = TMP_InputField.ContentType.IntegerNumber;
            (fieldMapHeight, _) = DebugUIBuilder.CreateInputFieldWithLabel(content, "Hauteur", "30");
            fieldMapHeight.contentType = TMP_InputField.ContentType.IntegerNumber;
            (fieldCellSize, _) = DebugUIBuilder.CreateInputFieldWithLabel(content, "Taille cellule", "6");
            fieldCellSize.contentType = TMP_InputField.ContentType.DecimalNumber;
            (fieldBorderMargin, _) = DebugUIBuilder.CreateInputFieldWithLabel(content, "Marge bord", "2");
            fieldBorderMargin.contentType = TMP_InputField.ContentType.IntegerNumber;

            (fieldMinRooms, _) = DebugUIBuilder.CreateInputFieldWithLabel(content, "Salles min", "5");
            fieldMinRooms.contentType = TMP_InputField.ContentType.IntegerNumber;
            (fieldMaxRooms, _) = DebugUIBuilder.CreateInputFieldWithLabel(content, "Salles max", "12");
            fieldMaxRooms.contentType = TMP_InputField.ContentType.IntegerNumber;
            (fieldMinRoomSize, _) = DebugUIBuilder.CreateInputFieldWithLabel(content, "Taille salle min", "3");
            fieldMinRoomSize.contentType = TMP_InputField.ContentType.IntegerNumber;
            (fieldMaxRoomSize, _) = DebugUIBuilder.CreateInputFieldWithLabel(content, "Taille salle max", "8");
            fieldMaxRoomSize.contentType = TMP_InputField.ContentType.IntegerNumber;
            (fieldCorridorWidth, _) = DebugUIBuilder.CreateInputFieldWithLabel(content, "Largeur couloir", "2");
            fieldCorridorWidth.contentType = TMP_InputField.ContentType.IntegerNumber;

            (sliderVegDensity, _) = DebugUIBuilder.CreateSliderWithLabel(content, "Densité végét.", 0, 1, 0.6f);
            (sliderRockDensity, _) = DebugUIBuilder.CreateSliderWithLabel(content, "Densité roches", 0, 1, 0.2f);
            (sliderDecorDensity, _) = DebugUIBuilder.CreateSliderWithLabel(content, "Densité décor", 0, 1, 0.3f);
        }

        void BuildSeedSection()
        {
            var (_, content) = DebugUIBuilder.CreateCollapsibleSection(scrollContent, "SEED");

            (fieldSeed, _) = DebugUIBuilder.CreateInputFieldWithLabel(content, "Seed", "0");
            fieldSeed.contentType = TMP_InputField.ContentType.IntegerNumber;

            toggleRandomSeed = DebugUIBuilder.CreateToggle(content, "Seed aléatoire", true);
            toggleLockSeed = DebugUIBuilder.CreateToggle(content, "Verrouiller la seed", false);

            var btnRow = DebugUIBuilder.CreateHorizontalGroup(content);
            DebugUIBuilder.CreateButton(btnRow, "Seed Aléatoire", () =>
            {
                int newSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                fieldSeed.text = newSeed.ToString();
                toggleRandomSeed.isOn = false;
            });
            DebugUIBuilder.CreateButton(btnRow, "Copier Seed", () =>
            {
                GUIUtility.systemCopyBuffer = fieldSeed.text;
                Debug.Log($"Seed copiée: {fieldSeed.text}");
            });
        }

        void BuildModeSection()
        {
            var (_, content) = DebugUIBuilder.CreateCollapsibleSection(scrollContent, "MODE DE GÉNÉRATION");

            // Dropdown Mode
            dropdownMode = CreateDropdown(content, "Mode",
                Enum.GetNames(typeof(GenerationMode)));

            // Dropdown Layout
            dropdownLayout = CreateDropdown(content, "Layout",
                Enum.GetNames(typeof(LayoutType)));

            // Dropdown Biome
            dropdownBiome = CreateDropdown(content, "Biome forcé",
                Enum.GetNames(typeof(BiomeType)));
            toggleForceBiome = DebugUIBuilder.CreateToggle(content, "Forcer le biome", false);
        }

        void BuildConstraintsSection()
        {
            var (_, content) = DebugUIBuilder.CreateCollapsibleSection(scrollContent, "CONTRAINTES");

            toggleEnsureAccess = DebugUIBuilder.CreateToggle(content, "Accessibilité complète", true);
            toggleValidate = DebugUIBuilder.CreateToggle(content, "Valider après génération", true);
            toggleForceBoss = DebugUIBuilder.CreateToggle(content, "Forcer salle de boss", false);
            toggleForceSpecial = DebugUIBuilder.CreateToggle(content, "Forcer salle spéciale", false);

            (fieldMinSpawnExitDist, _) = DebugUIBuilder.CreateInputFieldWithLabel(
                content, "Dist min S→E", "15");
            fieldMinSpawnExitDist.contentType = TMP_InputField.ContentType.DecimalNumber;
        }

        void BuildCategoriesSection()
        {
            var (_, content) = DebugUIBuilder.CreateCollapsibleSection(scrollContent, "CATÉGORIES D'ASSETS");
            categoriesContent = content;

            var btnRow = DebugUIBuilder.CreateHorizontalGroup(content);
            DebugUIBuilder.CreateButton(btnRow, "Tout cocher", () => SetAllCategories(true));
            DebugUIBuilder.CreateButton(btnRow, "Tout décocher", () => SetAllCategories(false));
        }

        void BuildActionsSection()
        {
            var (_, content) = DebugUIBuilder.CreateCollapsibleSection(scrollContent, "ACTIONS");

            var row1 = DebugUIBuilder.CreateHorizontalGroup(content, 35);
            DebugUIBuilder.CreateButton(row1, "GÉNÉRER", () => controller.Generate(),
                color: new Color(0.2f, 0.6f, 0.3f));
            DebugUIBuilder.CreateButton(row1, "REGÉNÉRER", () => controller.Regenerate(),
                color: new Color(0.3f, 0.5f, 0.6f));

            var row2 = DebugUIBuilder.CreateHorizontalGroup(content, 35);
            DebugUIBuilder.CreateButton(row2, "Même Seed", () => controller.GenerateSameSeed());
            DebugUIBuilder.CreateButton(row2, "Nettoyer", () => controller.ClearMap(),
                color: new Color(0.6f, 0.3f, 0.2f));

            var row3 = DebugUIBuilder.CreateHorizontalGroup(content);
            DebugUIBuilder.CreateButton(row3, "Spawn Joueur", () => controller.SpawnPlayer());
            DebugUIBuilder.CreateButton(row3, "Respawn", () => controller.RespawnPlayer());

            var row4 = DebugUIBuilder.CreateHorizontalGroup(content);
            DebugUIBuilder.CreateButton(row4, "Validation", () => controller.RunValidation());
            DebugUIBuilder.CreateButton(row4, "Screenshot", () => controller.TakeScreenshot());

            var row5 = DebugUIBuilder.CreateHorizontalGroup(content);
            DebugUIBuilder.CreateButton(row5, "Exporter Log", () => controller.ExportLog());
            DebugUIBuilder.CreateButton(row5, "Ouvrir Logs", () => GenerationLogger.OpenLogFolder());

            var row6 = DebugUIBuilder.CreateHorizontalGroup(content);
            DebugUIBuilder.CreateButton(row6, "Copier Résumé", () => controller.CopySummary());
            DebugUIBuilder.CreateButton(row6, "Vue d'ensemble", () => controller.ToggleCameraMode());
        }

        void BuildPresetSection()
        {
            var (_, content) = DebugUIBuilder.CreateCollapsibleSection(scrollContent, "PRESETS");

            (fieldPresetName, _) = DebugUIBuilder.CreateInputFieldWithLabel(
                content, "Nom", "", "MonPreset");

            var row1 = DebugUIBuilder.CreateHorizontalGroup(content);
            DebugUIBuilder.CreateButton(row1, "Sauver", () =>
            {
                string name = fieldPresetName.text;
                if (string.IsNullOrWhiteSpace(name)) name = "Preset_" + DateTime.Now.ToString("HHmmss");
                controller.SavePreset(name);
            });
            DebugUIBuilder.CreateButton(row1, "Défaut", () => controller.ResetToDefaults());

            // Presets par défaut
            DebugUIBuilder.CreateLabel(content, "Presets rapides :", 11);
            var defaultPresets = PresetManager.GetDefaultPresets();
            foreach (var preset in defaultPresets)
            {
                DebugUIBuilder.CreateButton(content, preset.presetName, () =>
                {
                    ApplyConfig(preset.config);
                    Debug.Log($"Preset chargé: {preset.presetName}");
                }, 24);
            }

            // Presets sauvegardés
            DebugUIBuilder.CreateLabel(content, "Sauvegardés :", 11);
            RefreshSavedPresetButtons(content);
        }

        void RefreshSavedPresetButtons(RectTransform parent)
        {
            var saved = PresetManager.GetAvailablePresets();
            foreach (var name in saved)
            {
                var row = DebugUIBuilder.CreateHorizontalGroup(parent, 24);
                DebugUIBuilder.CreateButton(row, name, () =>
                {
                    var preset = PresetManager.LoadPreset(name);
                    if (preset != null) ApplyConfig(preset.config);
                }, 24);
            }
        }

        void BuildBatchSection()
        {
            var (_, content) = DebugUIBuilder.CreateCollapsibleSection(scrollContent, "BATCH TEST");

            (fieldBatchCount, _) = DebugUIBuilder.CreateInputFieldWithLabel(
                content, "Itérations", "100");
            fieldBatchCount.contentType = TMP_InputField.ContentType.IntegerNumber;

            var row = DebugUIBuilder.CreateHorizontalGroup(content);
            DebugUIBuilder.CreateButton(row, "Lancer Batch", () =>
            {
                int count = ParseInt(fieldBatchCount.text, 100);
                controller.RunBatchTest(count);
            }, color: new Color(0.6f, 0.4f, 0.1f));
            DebugUIBuilder.CreateButton(row, "Annuler", () => controller.CancelBatch(),
                color: new Color(0.6f, 0.2f, 0.2f));

            lblBatchStatus = DebugUIBuilder.CreateLabel(content, "Aucun batch en cours", 11);
        }

        void BuildStatsSection()
        {
            var (_, content) = DebugUIBuilder.CreateCollapsibleSection(scrollContent, "STATISTIQUES");

            lblStatus = DebugUIBuilder.CreateLabel(content, "Statut: -", 13);
            lblSeed = DebugUIBuilder.CreateLabel(content, "Seed: -", 11);
            lblTime = DebugUIBuilder.CreateLabel(content, "Temps: -", 11);
            lblRooms = DebugUIBuilder.CreateLabel(content, "Salles: -", 11);
            lblCorridors = DebugUIBuilder.CreateLabel(content, "Couloirs: -", 11);
            lblObjects = DebugUIBuilder.CreateLabel(content, "Objets: -", 11);
            lblErrors = DebugUIBuilder.CreateLabel(content, "Erreurs: -", 11);
            lblWarnings = DebugUIBuilder.CreateLabel(content, "Warnings: -", 11);
            lblSpawn = DebugUIBuilder.CreateLabel(content, "Spawn: -", 11);
            lblExit = DebugUIBuilder.CreateLabel(content, "Sortie: -", 11);
            lblDistance = DebugUIBuilder.CreateLabel(content, "Distance S→E: -", 11);
        }

        void BuildLogSection()
        {
            var (_, content) = DebugUIBuilder.CreateCollapsibleSection(scrollContent, "LOG RÉCENT");
            logContent = content;
            DebugUIBuilder.CreateLabel(content, "Aucune génération", 10);
        }

        void BuildSeedHistorySection()
        {
            var (_, content) = DebugUIBuilder.CreateCollapsibleSection(scrollContent, "HISTORIQUE SEEDS");
            seedHistoryContent = content;
            DebugUIBuilder.CreateLabel(content, "Aucun historique", 10);
        }

        // ===================== LECTURE CONFIG =====================

        public MapGenConfig ReadConfig()
        {
            var cfg = new MapGenConfig
            {
                mapWidth = ParseInt(fieldMapWidth.text, 30),
                mapHeight = ParseInt(fieldMapHeight.text, 30),
                cellSize = ParseFloat(fieldCellSize.text, 6f),
                borderMargin = ParseInt(fieldBorderMargin.text, 2),
                minRooms = ParseInt(fieldMinRooms.text, 5),
                maxRooms = ParseInt(fieldMaxRooms.text, 12),
                minRoomSize = ParseInt(fieldMinRoomSize.text, 3),
                maxRoomSize = ParseInt(fieldMaxRoomSize.text, 8),
                corridorWidth = ParseInt(fieldCorridorWidth.text, 2),
                vegetationDensity = sliderVegDensity.value,
                rockDensity = sliderRockDensity.value,
                decorDensity = sliderDecorDensity.value,
                seed = ParseInt(fieldSeed.text, 0),
                useRandomSeed = toggleRandomSeed.isOn,
                lockSeed = toggleLockSeed.isOn,
                mode = (GenerationMode)dropdownMode.value,
                layoutType = (LayoutType)dropdownLayout.value,
                forcedBiome = (BiomeType)dropdownBiome.value,
                useForcedBiome = toggleForceBiome.isOn,
                ensureAccessibility = toggleEnsureAccess.isOn,
                validateAfterGeneration = toggleValidate.isOn,
                forceBossRoom = toggleForceBoss.isOn,
                forceSpecialRoom = toggleForceSpecial.isOn,
                minSpawnToExitDistance = ParseFloat(fieldMinSpawnExitDist.text, 15f)
            };

            // Catégories activées
            cfg.enabledCategories.Clear();
            foreach (var kvp in categoryToggles)
            {
                if (kvp.Value.isOn)
                    cfg.enabledCategories.Add(kvp.Key);
            }

            return cfg;
        }

        public void ApplyConfig(MapGenConfig cfg)
        {
            fieldMapWidth.text = cfg.mapWidth.ToString();
            fieldMapHeight.text = cfg.mapHeight.ToString();
            fieldCellSize.text = cfg.cellSize.ToString("F1");
            fieldBorderMargin.text = cfg.borderMargin.ToString();
            fieldMinRooms.text = cfg.minRooms.ToString();
            fieldMaxRooms.text = cfg.maxRooms.ToString();
            fieldMinRoomSize.text = cfg.minRoomSize.ToString();
            fieldMaxRoomSize.text = cfg.maxRoomSize.ToString();
            fieldCorridorWidth.text = cfg.corridorWidth.ToString();
            sliderVegDensity.value = cfg.vegetationDensity;
            sliderRockDensity.value = cfg.rockDensity;
            sliderDecorDensity.value = cfg.decorDensity;
            fieldSeed.text = cfg.seed.ToString();
            toggleRandomSeed.isOn = cfg.useRandomSeed;
            toggleLockSeed.isOn = cfg.lockSeed;
            dropdownMode.value = (int)cfg.mode;
            dropdownLayout.value = (int)cfg.layoutType;
            dropdownBiome.value = (int)cfg.forcedBiome;
            toggleForceBiome.isOn = cfg.useForcedBiome;
            toggleEnsureAccess.isOn = cfg.ensureAccessibility;
            toggleValidate.isOn = cfg.validateAfterGeneration;
            toggleForceBoss.isOn = cfg.forceBossRoom;
            toggleForceSpecial.isOn = cfg.forceSpecialRoom;
            fieldMinSpawnExitDist.text = cfg.minSpawnToExitDistance.ToString("F0");

            // Catégories
            foreach (var kvp in categoryToggles)
                kvp.Value.isOn = cfg.enabledCategories.Count == 0 || cfg.enabledCategories.Contains(kvp.Key);
        }

        // ===================== MISE À JOUR RÉSULTATS =====================

        public void UpdateResults(GenerationResult result)
        {
            if (result == null) return;

            lblStatus.text = $"Statut: {result.status}";
            lblStatus.color = DebugUIBuilder.GetStatusColor(result.status);

            lblSeed.text = $"Seed: {result.seed}";
            lblTime.text = $"Temps: {result.generationTimeMs:F1} ms";
            lblRooms.text = $"Salles: {result.roomCount}";
            lblCorridors.text = $"Couloirs: {result.corridorCount}";
            lblObjects.text = $"Objets placés: {result.totalObjectsPlaced}";
            lblErrors.text = $"Erreurs: {result.errorCount}";
            lblErrors.color = result.errorCount > 0 ? Color.red : Color.white;
            lblWarnings.text = $"Warnings: {result.warningCount}";
            lblWarnings.color = result.warningCount > 0
                ? new Color(1f, 0.8f, 0.2f)
                : Color.white;
            lblSpawn.text = $"Spawn: ({result.spawnCell.x}, {result.spawnCell.y})";
            lblExit.text = $"Sortie: ({result.exitCell.x}, {result.exitCell.y})";
            lblDistance.text = $"Distance S→E: {result.spawnToExitDistance:F1}";

            // Mettre à jour le champ seed
            fieldSeed.text = result.seed.ToString();

            // Log récent
            UpdateLogDisplay(result);

            // Historique seeds
            AddToSeedHistory(result.seed);
        }

        public void UpdateBatchStatus(string status)
        {
            if (lblBatchStatus != null)
                lblBatchStatus.text = status;
        }

        void UpdateLogDisplay(GenerationResult result)
        {
            // Nettoyer le contenu existant
            for (int i = logContent.childCount - 1; i >= 0; i--)
                Destroy(logContent.GetChild(i).gameObject);

            int maxLines = 30;
            int shown = 0;

            foreach (var entry in result.validationEntries)
            {
                if (shown >= maxLines) break;
                var lbl = DebugUIBuilder.CreateLabel(logContent, entry.ToString(), 9,
                    height: 16);
                lbl.color = DebugUIBuilder.GetSeverityColor(entry.severity);
                shown++;
            }

            foreach (var step in result.pipelineSteps)
            {
                if (shown >= maxLines) break;
                DebugUIBuilder.CreateLabel(logContent, step, 9,
                    height: 16);
                shown++;
            }
        }

        void AddToSeedHistory(int seed)
        {
            seedHistory.Remove(seed);
            seedHistory.Insert(0, seed);
            if (seedHistory.Count > MaxSeedHistory)
                seedHistory.RemoveAt(seedHistory.Count - 1);

            // Rebuild UI
            for (int i = seedHistoryContent.childCount - 1; i >= 0; i--)
                Destroy(seedHistoryContent.GetChild(i).gameObject);

            foreach (int s in seedHistory)
            {
                int captured = s;
                DebugUIBuilder.CreateButton(seedHistoryContent, $"Seed: {s}", () =>
                {
                    fieldSeed.text = captured.ToString();
                    toggleRandomSeed.isOn = false;
                    controller.GenerateSameSeed();
                }, 22);
            }
        }

        // ===================== CATÉGORIES =====================

        public void RefreshCategories(AssetCategoryRegistry registry)
        {
            // Supprimer les anciens toggles (sauf les 2 boutons tout cocher/décocher)
            for (int i = categoriesContent.childCount - 1; i >= 1; i--)
                Destroy(categoriesContent.GetChild(i).gameObject);

            categoryToggles.Clear();

            if (registry == null) return;

            foreach (var cat in registry.categories)
            {
                if (cat == null) continue;
                var toggle = DebugUIBuilder.CreateToggle(categoriesContent, cat.displayName, true);
                categoryToggles[cat.categoryId] = toggle;
            }
        }

        void SetAllCategories(bool value)
        {
            foreach (var toggle in categoryToggles.Values)
                toggle.isOn = value;
        }

        // ===================== UTILITAIRES =====================

        TMP_Dropdown CreateDropdown(Transform parent, string label, string[] options)
        {
            var row = DebugUIBuilder.CreateHorizontalGroup(parent, 28);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row, false);
            labelGo.AddComponent<LayoutElement>().preferredWidth = 110;
            DebugUIBuilder.CreateTextDirect(labelGo.transform, label, 11);

            var ddGo = new GameObject($"Dropdown_{label}");
            ddGo.transform.SetParent(row, false);
            ddGo.AddComponent<LayoutElement>().flexibleWidth = 1;

            var img = ddGo.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.18f);

            var dd = ddGo.AddComponent<TMP_Dropdown>();

            // Caption
            var captionGo = new GameObject("Label");
            captionGo.transform.SetParent(ddGo.transform, false);
            var cRT = captionGo.AddComponent<RectTransform>();
            cRT.anchorMin = Vector2.zero;
            cRT.anchorMax = Vector2.one;
            cRT.offsetMin = new Vector2(8, 0);
            cRT.offsetMax = new Vector2(-25, 0);
            var captionText = captionGo.AddComponent<TextMeshProUGUI>();
            captionText.fontSize = 11;
            captionText.color = Color.white;

            dd.captionText = captionText;

            // Template (minimal)
            var templateGo = new GameObject("Template");
            templateGo.transform.SetParent(ddGo.transform, false);
            var tRT = templateGo.AddComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0, 0);
            tRT.anchorMax = new Vector2(1, 0);
            tRT.pivot = new Vector2(0.5f, 1);
            tRT.sizeDelta = new Vector2(0, 150);
            templateGo.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f);
            templateGo.AddComponent<ScrollRect>();

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(templateGo.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero;
            vpRT.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>();
            viewport.AddComponent<Mask>().showMaskGraphic = true;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewport.transform, false);
            var coRT = contentGo.AddComponent<RectTransform>();
            coRT.anchorMin = new Vector2(0, 1);
            coRT.anchorMax = new Vector2(1, 1);
            coRT.pivot = new Vector2(0.5f, 1);

            var itemGo = new GameObject("Item");
            itemGo.transform.SetParent(contentGo.transform, false);
            var iRT = itemGo.AddComponent<RectTransform>();
            iRT.sizeDelta = new Vector2(0, 24);
            var itemToggle = itemGo.AddComponent<Toggle>();

            var itemBg = new GameObject("Item Background");
            itemBg.transform.SetParent(itemGo.transform, false);
            var ibRT = itemBg.AddComponent<RectTransform>();
            ibRT.anchorMin = Vector2.zero;
            ibRT.anchorMax = Vector2.one;
            ibRT.offsetMin = Vector2.zero;
            ibRT.offsetMax = Vector2.zero;
            itemBg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);

            var itemCheck = new GameObject("Item Checkmark");
            itemCheck.transform.SetParent(itemBg.transform, false);
            var icRT = itemCheck.AddComponent<RectTransform>();
            icRT.anchorMin = Vector2.zero;
            icRT.anchorMax = Vector2.one;
            icRT.offsetMin = Vector2.zero;
            icRT.offsetMax = Vector2.zero;
            var checkImg = itemCheck.AddComponent<Image>();
            checkImg.color = new Color(0.3f, 0.5f, 0.8f, 0.5f);

            itemToggle.targetGraphic = itemBg.GetComponent<Image>();
            itemToggle.graphic = checkImg;

            var itemLabel = new GameObject("Item Label");
            itemLabel.transform.SetParent(itemGo.transform, false);
            var ilRT = itemLabel.AddComponent<RectTransform>();
            ilRT.anchorMin = Vector2.zero;
            ilRT.anchorMax = Vector2.one;
            ilRT.offsetMin = new Vector2(8, 0);
            ilRT.offsetMax = Vector2.zero;
            var itemText = itemLabel.AddComponent<TextMeshProUGUI>();
            itemText.fontSize = 11;
            itemText.color = Color.white;

            dd.itemText = itemText;
            dd.template = tRT;
            templateGo.SetActive(false);

            dd.ClearOptions();
            dd.AddOptions(new List<string>(options));

            return dd;
        }

        static int ParseInt(string s, int fallback)
        {
            return int.TryParse(s, out int v) ? v : fallback;
        }

        static float ParseFloat(string s, float fallback)
        {
            return float.TryParse(s, out float v) ? v : fallback;
        }

        // Raccourcis clavier
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5)) controller.Generate();
            if (Input.GetKeyDown(KeyCode.F6)) controller.Regenerate();
            if (Input.GetKeyDown(KeyCode.F7)) controller.ClearMap();
            if (Input.GetKeyDown(KeyCode.F8)) controller.SpawnPlayer();
            if (Input.GetKeyDown(KeyCode.F9)) controller.TakeScreenshot();
            if (Input.GetKeyDown(KeyCode.F10)) controller.ToggleCameraMode();
            if (Input.GetKeyDown(KeyCode.F12)) controller.ExportLog();

            // Tab pour toggle UI
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                var leftPanel = transform.GetChild(0);
                leftPanel.gameObject.SetActive(!leftPanel.gameObject.activeSelf);
            }
        }
    }
}
