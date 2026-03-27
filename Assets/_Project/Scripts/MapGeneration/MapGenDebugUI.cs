using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonGeonMaster.MapGeneration
{
    public class MapGenDebugUI : MonoBehaviour
    {
        MapGenDebugController controller;
        ScrollRect mainScroll;
        RectTransform scrollContent;

        // Champs de saisie
        TMP_InputField fieldMapWidth, fieldMapHeight, fieldCellSize, fieldBorderMargin;
        TMP_InputField fieldMinRooms, fieldMaxRooms, fieldMinRoomSize, fieldMaxRoomSize;
        TMP_InputField fieldCorridorWidth, fieldMinSpawnExitDist;
        Slider sliderVegDensity, sliderRockDensity, sliderDecorDensity;
        TMP_InputField fieldSeed;
        Toggle toggleRandomSeed, toggleLockSeed;
        Toggle toggleForceBiome, toggleEnsureAccess, toggleValidate;
        Toggle toggleForceBoss, toggleForceSpecial;
        TMP_InputField fieldBatchCount, fieldPresetName;

        // Sélecteurs de mode (remplacent les dropdowns fragiles)
        int currentModeIndex, currentLayoutIndex, currentBiomeIndex;
        TextMeshProUGUI lblModeValue, lblLayoutValue, lblBiomeValue;
        static readonly string[] ModeNames = Enum.GetNames(typeof(GenerationMode));
        static readonly string[] LayoutNames = Enum.GetNames(typeof(LayoutType));
        static readonly string[] BiomeNames = Enum.GetNames(typeof(BiomeType));

        // Catégories
        Dictionary<string, Toggle> categoryToggles = new();
        RectTransform categoriesContent;

        // Affichage résultats
        TextMeshProUGUI lblStatus, lblSeed, lblTime, lblRooms, lblCorridors;
        TextMeshProUGUI lblObjects, lblErrors, lblWarnings, lblSpawn, lblExit;
        TextMeshProUGUI lblDistance, lblBatchStatus;
        RectTransform logContent, seedHistoryContent;

        List<int> seedHistory = new();
        const int MaxSeedHistory = 15;

        public void Initialize(MapGenDebugController ctrl)
        {
            controller = ctrl;
            try
            {
                BuildUI();
                Debug.Log("[MapGenDebugUI] UI construite avec succès");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapGenDebugUI] ERREUR lors de la construction de l'UI: {e}");
            }
        }

        void BuildUI()
        {
            var leftPanel = DebugUIBuilder.CreatePanel(transform, "LeftPanel", 0, 0, 0.30f, 1);
            Debug.Log("[MapGenDebugUI] LeftPanel créé");

            // Titre
            var titleBar = DebugUIBuilder.CreatePanel(leftPanel, "TitleBar", 0, 0.96f, 1, 1,
                new Color(0.15f, 0.3f, 0.5f));
            DebugUIBuilder.CreateTextDirect(titleBar, "MAP GEN DEBUG", 16,
                TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;

            // ScrollView dans la zone sous le titre
            var scrollPanel = DebugUIBuilder.CreatePanel(leftPanel, "ScrollPanel", 0, 0, 1, 0.96f,
                new Color(0f, 0f, 0f, 0f));
            mainScroll = DebugUIBuilder.CreateScrollView(scrollPanel, "ConfigScroll");
            scrollContent = mainScroll.content;

            BuildActionsSection();
            BuildSizeSection();
            BuildSeedSection();
            BuildModeSection();
            BuildConstraintsSection();
            BuildCategoriesSection();
            BuildStatsSection();
            BuildLogSection();
            BuildPresetSection();
            BuildBatchSection();
            BuildSeedHistorySection();

            Debug.Log("[MapGenDebugUI] Toutes les sections construites");
        }

        // ===================== SECTIONS =====================

        void BuildActionsSection()
        {
            var (_, content) = DebugUIBuilder.CreateCollapsibleSection(scrollContent, "ACTIONS");

            var row1 = DebugUIBuilder.CreateHorizontalGroup(content, 38);
            DebugUIBuilder.CreateButton(row1, "GÉNÉRER (F5)", () => controller.Generate(),
                color: new Color(0.2f, 0.6f, 0.3f));
            DebugUIBuilder.CreateButton(row1, "REGÉNÉRER (F6)", () => controller.Regenerate(),
                color: new Color(0.3f, 0.5f, 0.6f));

            var row2 = DebugUIBuilder.CreateHorizontalGroup(content, 32);
            DebugUIBuilder.CreateButton(row2, "Même Seed", () => controller.GenerateSameSeed());
            DebugUIBuilder.CreateButton(row2, "Nettoyer (F7)", () => controller.ClearMap(),
                color: new Color(0.6f, 0.3f, 0.2f));

            var row3 = DebugUIBuilder.CreateHorizontalGroup(content, 28);
            DebugUIBuilder.CreateButton(row3, "Spawn (F8)", () => controller.SpawnPlayer());
            DebugUIBuilder.CreateButton(row3, "Respawn", () => controller.RespawnPlayer());

            var row4 = DebugUIBuilder.CreateHorizontalGroup(content, 28);
            DebugUIBuilder.CreateButton(row4, "Validation", () => controller.RunValidation());
            DebugUIBuilder.CreateButton(row4, "Screenshot (F9)", () => controller.TakeScreenshot());

            var row5 = DebugUIBuilder.CreateHorizontalGroup(content, 28);
            DebugUIBuilder.CreateButton(row5, "Exporter Log", () => controller.ExportLog());
            DebugUIBuilder.CreateButton(row5, "Ouvrir Logs", () => GenerationLogger.OpenLogFolder());

            var row6 = DebugUIBuilder.CreateHorizontalGroup(content, 28);
            DebugUIBuilder.CreateButton(row6, "Copier Résumé", () => controller.CopySummary());
            DebugUIBuilder.CreateButton(row6, "Vue (F10)", () => controller.ToggleCameraMode());
        }

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
            (fieldCorridorWidth, _) = DebugUIBuilder.CreateInputFieldWithLabel(content, "Larg. couloir", "2");
            fieldCorridorWidth.contentType = TMP_InputField.ContentType.IntegerNumber;

            (sliderVegDensity, _) = DebugUIBuilder.CreateSliderWithLabel(content, "Végétation", 0, 1, 0.6f);
            (sliderRockDensity, _) = DebugUIBuilder.CreateSliderWithLabel(content, "Roches", 0, 1, 0.2f);
            (sliderDecorDensity, _) = DebugUIBuilder.CreateSliderWithLabel(content, "Décor", 0, 1, 0.3f);
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

            // Sélecteurs à cycle (clic = option suivante) - remplace les TMP_Dropdown fragiles
            lblModeValue = CreateCycleSelector(content, "Mode", ModeNames, 0,
                i => currentModeIndex = i);
            lblLayoutValue = CreateCycleSelector(content, "Layout", LayoutNames, 0,
                i => currentLayoutIndex = i);
            lblBiomeValue = CreateCycleSelector(content, "Biome forcé", BiomeNames, 0,
                i => currentBiomeIndex = i);
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

        void BuildStatsSection()
        {
            var (_, content) = DebugUIBuilder.CreateCollapsibleSection(scrollContent, "STATISTIQUES");

            lblStatus = DebugUIBuilder.CreateLabel(content, "Statut: en attente", 14);
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

            DebugUIBuilder.CreateLabel(content, "Presets rapides :", 11);
            foreach (var preset in PresetManager.GetDefaultPresets())
            {
                var p = preset;
                DebugUIBuilder.CreateButton(content, p.presetName, () =>
                {
                    ApplyConfig(p.config);
                    Debug.Log($"Preset chargé: {p.presetName}");
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

        void BuildSeedHistorySection()
        {
            var (_, content) = DebugUIBuilder.CreateCollapsibleSection(scrollContent, "HISTORIQUE SEEDS");
            seedHistoryContent = content;
            DebugUIBuilder.CreateLabel(content, "Aucun historique", 10);
        }

        // ===================== CYCLE SELECTOR (remplacement robuste des dropdowns) =====================

        TextMeshProUGUI CreateCycleSelector(Transform parent, string label, string[] options,
            int startIndex, Action<int> onChanged)
        {
            var row = DebugUIBuilder.CreateHorizontalGroup(parent, 28);

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row, false);
            var le = labelGo.AddComponent<LayoutElement>();
            le.preferredWidth = 90;
            le.flexibleWidth = 0;
            DebugUIBuilder.CreateTextDirect(labelGo.transform, label, 11);

            // Bouton précédent
            DebugUIBuilder.CreateButton(row, "<", null, 28, new Color(0.25f, 0.25f, 0.35f));

            // Valeur actuelle
            var valueGo = new GameObject("Value");
            valueGo.transform.SetParent(row, false);
            valueGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            var valueBg = valueGo.AddComponent<Image>();
            valueBg.color = new Color(0.12f, 0.12f, 0.16f);
            var valueText = DebugUIBuilder.CreateTextDirect(valueGo.transform,
                options[startIndex], 11, TextAlignmentOptions.Center);

            // Bouton suivant
            DebugUIBuilder.CreateButton(row, ">", null, 28, new Color(0.25f, 0.25f, 0.35f));

            // Connecter les boutons (les récupérer par index)
            int currentIndex = startIndex;
            var prevBtn = row.GetChild(1).GetComponent<Button>();
            var nextBtn = row.GetChild(3).GetComponent<Button>();

            prevBtn.onClick.AddListener(() =>
            {
                currentIndex = (currentIndex - 1 + options.Length) % options.Length;
                valueText.text = options[currentIndex];
                onChanged(currentIndex);
            });
            nextBtn.onClick.AddListener(() =>
            {
                currentIndex = (currentIndex + 1) % options.Length;
                valueText.text = options[currentIndex];
                onChanged(currentIndex);
            });

            // Fixe la largeur des boutons < et >
            row.GetChild(1).GetComponent<LayoutElement>().preferredWidth = 28;
            row.GetChild(1).GetComponent<LayoutElement>().flexibleWidth = 0;
            row.GetChild(3).GetComponent<LayoutElement>().preferredWidth = 28;
            row.GetChild(3).GetComponent<LayoutElement>().flexibleWidth = 0;

            return valueText;
        }

        // ===================== LECTURE / APPLICATION CONFIG =====================

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
                mode = (GenerationMode)currentModeIndex,
                layoutType = (LayoutType)currentLayoutIndex,
                forcedBiome = (BiomeType)currentBiomeIndex,
                useForcedBiome = toggleForceBiome.isOn,
                ensureAccessibility = toggleEnsureAccess.isOn,
                validateAfterGeneration = toggleValidate.isOn,
                forceBossRoom = toggleForceBoss.isOn,
                forceSpecialRoom = toggleForceSpecial.isOn,
                minSpawnToExitDistance = ParseFloat(fieldMinSpawnExitDist.text, 15f)
            };

            cfg.enabledCategories.Clear();
            foreach (var kvp in categoryToggles)
                if (kvp.Value.isOn) cfg.enabledCategories.Add(kvp.Key);

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

            currentModeIndex = (int)cfg.mode;
            if (lblModeValue != null) lblModeValue.text = ModeNames[currentModeIndex];
            currentLayoutIndex = (int)cfg.layoutType;
            if (lblLayoutValue != null) lblLayoutValue.text = LayoutNames[currentLayoutIndex];
            currentBiomeIndex = (int)cfg.forcedBiome;
            if (lblBiomeValue != null) lblBiomeValue.text = BiomeNames[currentBiomeIndex];

            toggleForceBiome.isOn = cfg.useForcedBiome;
            toggleEnsureAccess.isOn = cfg.ensureAccessibility;
            toggleValidate.isOn = cfg.validateAfterGeneration;
            toggleForceBoss.isOn = cfg.forceBossRoom;
            toggleForceSpecial.isOn = cfg.forceSpecialRoom;
            fieldMinSpawnExitDist.text = cfg.minSpawnToExitDistance.ToString("F0");

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
            lblWarnings.color = result.warningCount > 0 ? new Color(1f, 0.8f, 0.2f) : Color.white;
            lblSpawn.text = $"Spawn: ({result.spawnCell.x}, {result.spawnCell.y})";
            lblExit.text = $"Sortie: ({result.exitCell.x}, {result.exitCell.y})";
            lblDistance.text = $"Distance S→E: {result.spawnToExitDistance:F1}";

            fieldSeed.text = result.seed.ToString();
            UpdateLogDisplay(result);
            AddToSeedHistory(result.seed);
        }

        public void UpdateBatchStatus(string status)
        {
            if (lblBatchStatus != null) lblBatchStatus.text = status;
        }

        void UpdateLogDisplay(GenerationResult result)
        {
            for (int i = logContent.childCount - 1; i >= 0; i--)
                Destroy(logContent.GetChild(i).gameObject);

            int maxLines = 25;
            int shown = 0;
            foreach (var entry in result.validationEntries)
            {
                if (shown >= maxLines) break;
                var lbl = DebugUIBuilder.CreateLabel(logContent, entry.ToString(), 9, height: 16);
                lbl.color = DebugUIBuilder.GetSeverityColor(entry.severity);
                shown++;
            }
            foreach (var step in result.pipelineSteps)
            {
                if (shown >= maxLines) break;
                DebugUIBuilder.CreateLabel(logContent, step, 9, height: 16);
                shown++;
            }
        }

        void AddToSeedHistory(int seed)
        {
            seedHistory.Remove(seed);
            seedHistory.Insert(0, seed);
            if (seedHistory.Count > MaxSeedHistory)
                seedHistory.RemoveAt(seedHistory.Count - 1);

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
            if (categoriesContent == null) return;

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
            Debug.Log($"[MapGenDebugUI] {categoryToggles.Count} catégories chargées");
        }

        void SetAllCategories(bool value)
        {
            foreach (var toggle in categoryToggles.Values)
                toggle.isOn = value;
        }

        // ===================== UTILITAIRES =====================

        static int ParseInt(string s, int fallback) => int.TryParse(s, out int v) ? v : fallback;
        static float ParseFloat(string s, float fallback) => float.TryParse(s, out float v) ? v : fallback;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5)) controller.Generate();
            if (Input.GetKeyDown(KeyCode.F6)) controller.Regenerate();
            if (Input.GetKeyDown(KeyCode.F7)) controller.ClearMap();
            if (Input.GetKeyDown(KeyCode.F8)) controller.SpawnPlayer();
            if (Input.GetKeyDown(KeyCode.F9)) controller.TakeScreenshot();
            if (Input.GetKeyDown(KeyCode.F10)) controller.ToggleCameraMode();
            if (Input.GetKeyDown(KeyCode.F12)) controller.ExportLog();

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (transform.childCount > 0)
                {
                    var leftPanel = transform.GetChild(0);
                    leftPanel.gameObject.SetActive(!leftPanel.gameObject.activeSelf);
                }
            }
        }
    }
}
