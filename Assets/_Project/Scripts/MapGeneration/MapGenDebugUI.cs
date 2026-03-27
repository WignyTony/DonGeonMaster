using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonGeonMaster.MapGeneration
{
    public enum DebugViewMode { Config, TopDown, FPS }

    public class MapGenDebugUI : MonoBehaviour
    {
        MapGenDebugController controller;
        DebugViewMode currentMode = DebugViewMode.Config;

        // ── Panels racine (pour show/hide par mode) ──
        GameObject sidebarGO;
        GameObject rightAreaGO;
        GameObject infoPanelsGO;
        GameObject logPanelGO;
        GameObject metricsBarGO;

        // ── Champs sidebar ──
        TMP_InputField fSeed, fMapW, fMapH, fCellSize, fMargin;
        TMP_InputField fMinRooms, fMaxRooms, fMinRoom, fMaxRoom, fCorridorW, fMinDist;
        Slider sVeg, sRock, sDecor;
        Toggle tRandomSeed, tLockSeed, tForceBiome, tAccess, tValidate, tBoss, tSpecial;
        TMP_InputField fBatchCount, fPresetName;
        int iMode, iLayout, iBiome;
        TextMeshProUGUI txtMode, txtLayout, txtBiome;
        static readonly string[] ModeNames = Enum.GetNames(typeof(GenerationMode));
        static readonly string[] LayoutNames = Enum.GetNames(typeof(LayoutType));
        static readonly string[] BiomeNames = Enum.GetNames(typeof(BiomeType));
        Dictionary<string, Toggle> catToggles = new();
        RectTransform catContainer;

        // ── Metrics bar ──
        TextMeshProUGUI mStatus, mSeed, mTime, mRooms, mObjects, mErrors, mWarnings;

        // ── Panels centre ──
        RectTransform summaryContent, validationContent;
        TextMeshProUGUI lblBatch;

        // ── Log panel ──
        RectTransform logContent;

        List<int> seedHistory = new();

        public void Initialize(MapGenDebugController ctrl)
        {
            controller = ctrl;
            try { BuildUI(); }
            catch (Exception e) { Debug.LogError($"[MapGenDebugUI] {e}"); }
        }

        // ════════════════════════════════════════════════════════
        //  GESTION DES MODES D'AFFICHAGE
        // ════════════════════════════════════════════════════════

        public void SetMode(DebugViewMode mode)
        {
            currentMode = mode;
            switch (mode)
            {
                case DebugViewMode.Config:
                    // Sidebar visible, dashboard complet visible
                    SetActive(sidebarGO, true);
                    SetActive(rightAreaGO, true);
                    SetActive(metricsBarGO, true);
                    SetActive(infoPanelsGO, true);
                    SetActive(logPanelGO, true);
                    break;

                case DebugViewMode.TopDown:
                    // Sidebar cachée, seule la metrics bar reste en overlay
                    SetActive(sidebarGO, false);
                    SetActive(rightAreaGO, true);
                    SetActive(metricsBarGO, true);
                    SetActive(infoPanelsGO, false);
                    SetActive(logPanelGO, false);
                    break;

                case DebugViewMode.FPS:
                    // Tout caché sauf metrics bar minimale
                    SetActive(sidebarGO, false);
                    SetActive(rightAreaGO, true);
                    SetActive(metricsBarGO, true);
                    SetActive(infoPanelsGO, false);
                    SetActive(logPanelGO, false);
                    break;
            }
        }

        static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        public void ToggleSidebar()
        {
            if (sidebarGO == null) return;
            bool show = !sidebarGO.activeSelf;
            sidebarGO.SetActive(show);
            // Quand on ré-ouvre la sidebar, montrer aussi les panels info
            SetActive(infoPanelsGO, show);
            SetActive(logPanelGO, show);
            currentMode = show ? DebugViewMode.Config : DebugViewMode.TopDown;
        }

        // ════════════════════════════════════════════════════════
        //  CONSTRUCTION UI
        // ════════════════════════════════════════════════════════

        void BuildUI()
        {
            var root = DebugUIBuilder.CreateStretchFill(transform, "Root");
            var rootHLG = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            rootHLG.spacing = 2;
            rootHLG.childControlWidth = true;
            rootHLG.childControlHeight = true;
            rootHLG.childForceExpandWidth = false;
            rootHLG.childForceExpandHeight = true;

            BuildLeftSidebar(root);
            BuildRightArea(root);
        }

        void BuildLeftSidebar(Transform root)
        {
            var sidebar = DebugUIBuilder.CreateLayoutPanel(root, "Sidebar",
                new Color(0.10f, 0.10f, 0.13f, 0.95f), preferredW: 390, flexW: 0, flexH: 1);
            sidebarGO = sidebar.gameObject;

            var vlg = sidebar.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 0;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var header = DebugUIBuilder.CreateLayoutPanel(sidebar, "Header",
                DebugUIBuilder.HeaderBg, preferredH: 36);
            var hTxt = DebugUIBuilder.CreateTextDirect(header, "MAP GEN DEBUG", 15,
                TextAlignmentOptions.Center);
            hTxt.fontStyle = FontStyles.Bold;

            var scroll = DebugUIBuilder.CreateScrollView(sidebar);
            var content = scroll.content;

            BuildActionsSection(content);
            BuildSeedSection(content);
            BuildDimensionsSection(content);
            BuildModeSection(content);
            BuildConstraintsSection(content);
            BuildCategoriesSection(content);
            BuildPresetsSection(content);
            BuildBatchSection(content);
        }

        void BuildRightArea(Transform root)
        {
            // RightArea: fond totalement transparent, ne bloque pas les clics
            var right = DebugUIBuilder.CreateLayoutPanel(root, "RightArea",
                Color.clear, flexW: 1, flexH: 1);
            rightAreaGO = right.gameObject;
            // Désactiver le raycast sur le fond transparent
            right.GetComponent<Image>().raycastTarget = false;

            var vlg = right.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 2;
            vlg.padding = new RectOffset(2, 2, 2, 2);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            BuildMetricsBar(right);
            BuildInfoPanels(right);
            BuildLogPanel(right);
        }

        void BuildMetricsBar(Transform parent)
        {
            var bar = DebugUIBuilder.CreateLayoutPanel(parent, "MetricsBar",
                new Color(0.08f, 0.08f, 0.12f, 0.85f), preferredH: 34);
            metricsBarGO = bar.gameObject;

            var hlg = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16;
            hlg.padding = new RectOffset(12, 12, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            mStatus   = MetricLabel(bar, "Statut: en attente", 13, true);
            mSeed     = MetricLabel(bar, "Seed: -");
            mTime     = MetricLabel(bar, "Temps: -");
            mRooms    = MetricLabel(bar, "Salles: -");
            mObjects  = MetricLabel(bar, "Objets: -");
            mErrors   = MetricLabel(bar, "Erreurs: 0");
            mWarnings = MetricLabel(bar, "Warn: 0");
        }

        TextMeshProUGUI MetricLabel(Transform parent, string text, int size = 11, bool bold = false)
        {
            var go = new GameObject("M");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(140, 30);
            var tmp = DebugUIBuilder.CreateTextDirect(go.transform, text, size);
            if (bold) tmp.fontStyle = FontStyles.Bold;
            return tmp;
        }

        void BuildInfoPanels(Transform parent)
        {
            var row = DebugUIBuilder.CreateLayoutPanel(parent, "InfoPanels",
                Color.clear, flexH: 1);
            infoPanelsGO = row.gameObject;
            row.GetComponent<Image>().raycastTarget = false;

            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 2;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            summaryContent = BuildInfoCard(row, "Resume Generation");
            validationContent = BuildInfoCard(row, "Resultats Validation");
        }

        RectTransform BuildInfoCard(Transform parent, string title)
        {
            var card = DebugUIBuilder.CreateLayoutPanel(parent, title.Replace(" ", ""),
                new Color(0.10f, 0.10f, 0.13f, 0.85f), flexW: 1, flexH: 1);
            var vlg = card.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 0;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var hdr = DebugUIBuilder.CreateLayoutPanel(card, "Hdr",
                DebugUIBuilder.HeaderBg, preferredH: 26);
            var hTxt = DebugUIBuilder.CreateTextDirect(hdr, $"  {title}", 12);
            hTxt.fontStyle = FontStyles.Bold;

            var scroll = DebugUIBuilder.CreateScrollView(card);
            return scroll.content;
        }

        void BuildLogPanel(Transform parent)
        {
            var panel = DebugUIBuilder.CreateLayoutPanel(parent, "LogPanel",
                new Color(0.10f, 0.10f, 0.13f, 0.85f), preferredH: 200);
            logPanelGO = panel.gameObject;

            var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 0;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var hdr = DebugUIBuilder.CreateLayoutPanel(panel, "LogHdr",
                DebugUIBuilder.HeaderBg, preferredH: 26);
            var hHLG = hdr.gameObject.AddComponent<HorizontalLayoutGroup>();
            hHLG.padding = new RectOffset(8, 4, 0, 0);
            hHLG.childAlignment = TextAnchor.MiddleLeft;
            hHLG.childControlWidth = true;
            hHLG.childControlHeight = true;
            hHLG.childForceExpandWidth = false;
            hHLG.childForceExpandHeight = true;

            var lblGo = new GameObject("LblLog");
            lblGo.transform.SetParent(hdr, false);
            lblGo.AddComponent<RectTransform>();
            lblGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            var lt = lblGo.AddComponent<TextMeshProUGUI>();
            lt.text = "  LOGS"; lt.fontSize = 12; lt.fontStyle = FontStyles.Bold;
            lt.color = DebugUIBuilder.TextWhite; lt.alignment = TextAlignmentOptions.MidlineLeft;

            DebugUIBuilder.CreateButton(hdr, "Clear", () => ClearLogs(), 24, DebugUIBuilder.BtnRed);

            var scroll = DebugUIBuilder.CreateScrollView(panel, flexH: 1);
            logContent = scroll.content;
            DebugUIBuilder.CreateLabel(logContent, "En attente...", 10, height: 16).color = DebugUIBuilder.TextDim;
        }

        // ════════════════════════════════════════════════════════
        //  SECTIONS SIDEBAR (inchangées)
        // ════════════════════════════════════════════════════════

        void BuildActionsSection(Transform parent)
        {
            var (_, c) = DebugUIBuilder.CreateCollapsibleSection(parent, "ACTIONS");
            Row(c, ("GENERER (F5)", () => controller.Generate(), DebugUIBuilder.BtnGreen),
                    ("REGENERER (F6)", () => controller.Regenerate(), (Color?)null));
            Row(c, ("Meme Seed", () => controller.GenerateSameSeed(), (Color?)null),
                    ("Nettoyer (F7)", () => controller.ClearMap(), DebugUIBuilder.BtnRed));
            Row(c, ("Spawn (F8)", () => controller.SpawnPlayer(), (Color?)null),
                    ("Respawn", () => controller.RespawnPlayer(), (Color?)null));
            Row(c, ("Validation", () => controller.RunValidation(), (Color?)null),
                    ("Screenshot", () => controller.TakeScreenshot(), (Color?)null));
            Row(c, ("Export Log", () => controller.ExportLog(), (Color?)null),
                    ("Ouvrir Logs", () => GenerationLogger.OpenLogFolder(), (Color?)null));
            Row(c, ("Copier Resume", () => controller.CopySummary(), (Color?)null),
                    ("Vue FPS (F10)", () => controller.EnterFPSMode(), (Color?)null));
        }

        void BuildSeedSection(Transform parent)
        {
            var (_, c) = DebugUIBuilder.CreateCollapsibleSection(parent, "SEED");
            (fSeed, _) = DebugUIBuilder.CreateInputFieldWithLabel(c, "Seed", "0");
            fSeed.contentType = TMP_InputField.ContentType.IntegerNumber;
            tRandomSeed = DebugUIBuilder.CreateToggle(c, "Seed aleatoire", true);
            tLockSeed = DebugUIBuilder.CreateToggle(c, "Verrouiller seed", false);
            Row(c, ("Random", () => {
                    fSeed.text = UnityEngine.Random.Range(int.MinValue, int.MaxValue).ToString();
                    tRandomSeed.isOn = false;
                }, (Color?)null),
                ("Copier", () => GUIUtility.systemCopyBuffer = fSeed.text, (Color?)null));
        }

        void BuildDimensionsSection(Transform parent)
        {
            var (_, c) = DebugUIBuilder.CreateCollapsibleSection(parent, "DIMENSIONS");
            fMapW = IntField(c, "Largeur map", "30");
            fMapH = IntField(c, "Hauteur map", "30");
            fCellSize = FloatField(c, "Taille cellule", "6");
            fMargin = IntField(c, "Marge bord", "2");
            fMinRooms = IntField(c, "Salles min", "5");
            fMaxRooms = IntField(c, "Salles max", "12");
            fMinRoom = IntField(c, "Taille salle min", "3");
            fMaxRoom = IntField(c, "Taille salle max", "8");
            fCorridorW = IntField(c, "Largeur couloir", "2");
            (sVeg, _) = DebugUIBuilder.CreateSliderWithLabel(c, "Vegetation", 0, 1, 0.6f);
            (sRock, _) = DebugUIBuilder.CreateSliderWithLabel(c, "Roches", 0, 1, 0.2f);
            (sDecor, _) = DebugUIBuilder.CreateSliderWithLabel(c, "Decor", 0, 1, 0.3f);
        }

        void BuildModeSection(Transform parent)
        {
            var (_, c) = DebugUIBuilder.CreateCollapsibleSection(parent, "MODE");
            txtMode = CycleSelector(c, "Mode", ModeNames, i => iMode = i);
            txtLayout = CycleSelector(c, "Layout", LayoutNames, i => iLayout = i);
            txtBiome = CycleSelector(c, "Biome", BiomeNames, i => iBiome = i);
            tForceBiome = DebugUIBuilder.CreateToggle(c, "Forcer le biome", false);
        }

        void BuildConstraintsSection(Transform parent)
        {
            var (_, c) = DebugUIBuilder.CreateCollapsibleSection(parent, "CONTRAINTES");
            tAccess = DebugUIBuilder.CreateToggle(c, "Accessibilite complete", true);
            tValidate = DebugUIBuilder.CreateToggle(c, "Valider apres generation", true);
            tBoss = DebugUIBuilder.CreateToggle(c, "Forcer salle de boss", false);
            tSpecial = DebugUIBuilder.CreateToggle(c, "Forcer salle speciale", false);
            fMinDist = FloatField(c, "Dist min S>E", "15");
        }

        void BuildCategoriesSection(Transform parent)
        {
            var (_, c) = DebugUIBuilder.CreateCollapsibleSection(parent, "CATEGORIES D'ASSETS");
            catContainer = c;
            Row(c, ("Tout cocher", () => SetAllCat(true), (Color?)null),
                   ("Tout decocher", () => SetAllCat(false), (Color?)null));
        }

        void BuildPresetsSection(Transform parent)
        {
            var (_, c) = DebugUIBuilder.CreateCollapsibleSection(parent, "PRESETS");
            (fPresetName, _) = DebugUIBuilder.CreateInputFieldWithLabel(c, "Nom preset", "", "MonPreset");
            Row(c, ("Sauver", () => {
                    string n = fPresetName.text;
                    if (string.IsNullOrWhiteSpace(n)) n = "P_" + DateTime.Now.ToString("HHmmss");
                    controller.SavePreset(n);
                }, (Color?)null),
                ("Defaut", () => controller.ResetToDefaults(), (Color?)null));

            DebugUIBuilder.CreateLabel(c, "-- Presets rapides --", 10, height: 18).color = DebugUIBuilder.TextDim;
            foreach (var p in PresetManager.GetDefaultPresets())
            {
                var preset = p;
                DebugUIBuilder.CreateButton(c, preset.presetName, () => ApplyConfig(preset.config), 22);
            }
        }

        void BuildBatchSection(Transform parent)
        {
            var (_, c) = DebugUIBuilder.CreateCollapsibleSection(parent, "BATCH TEST");
            fBatchCount = IntField(c, "Iterations", "100");
            Row(c, ("Lancer Batch", () => controller.RunBatchTest(ParseInt(fBatchCount.text, 100)),
                    DebugUIBuilder.BtnOrange),
                ("Annuler", () => controller.CancelBatch(), DebugUIBuilder.BtnRed));
            lblBatch = DebugUIBuilder.CreateLabel(c, "Aucun batch en cours", 10, height: 18);
            lblBatch.color = DebugUIBuilder.TextDim;
        }

        // ════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════

        void Row(Transform parent, (string text, Action act, Color? col) a,
                                   (string text, Action act, Color? col) b)
        {
            var r = DebugUIBuilder.CreateHGroup(parent, 26, spacing: 3);
            DebugUIBuilder.CreateButton(r, a.text, () => a.act(), 26, a.col);
            DebugUIBuilder.CreateButton(r, b.text, () => b.act(), 26, b.col);
        }

        TMP_InputField IntField(Transform parent, string label, string def)
        {
            var (f, _) = DebugUIBuilder.CreateInputFieldWithLabel(parent, label, def);
            f.contentType = TMP_InputField.ContentType.IntegerNumber;
            return f;
        }

        TMP_InputField FloatField(Transform parent, string label, string def)
        {
            var (f, _) = DebugUIBuilder.CreateInputFieldWithLabel(parent, label, def);
            f.contentType = TMP_InputField.ContentType.DecimalNumber;
            return f;
        }

        TextMeshProUGUI CycleSelector(Transform parent, string label, string[] opts, Action<int> onChanged)
        {
            var row = DebugUIBuilder.CreateHGroup(parent, 24, spacing: 2);

            var lGo = new GameObject("Lbl");
            lGo.transform.SetParent(row, false);
            lGo.AddComponent<RectTransform>();
            var lLE = lGo.AddComponent<LayoutElement>();
            lLE.preferredWidth = 100; lLE.flexibleWidth = 0;
            DebugUIBuilder.CreateTextDirect(lGo.transform, label, 11).color = DebugUIBuilder.TextDim;

            var btnPrev = DebugUIBuilder.CreateButton(row, "<", null, 24);
            btnPrev.GetComponent<LayoutElement>().flexibleWidth = 0;
            btnPrev.GetComponent<LayoutElement>().preferredWidth = 26;

            var vGo = new GameObject("Val");
            vGo.transform.SetParent(row, false);
            vGo.AddComponent<RectTransform>();
            vGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            vGo.AddComponent<Image>().color = DebugUIBuilder.InputBg;
            var vTxt = DebugUIBuilder.CreateTextDirect(vGo.transform, opts[0], 11,
                TextAlignmentOptions.Center);

            var btnNext = DebugUIBuilder.CreateButton(row, ">", null, 24);
            btnNext.GetComponent<LayoutElement>().flexibleWidth = 0;
            btnNext.GetComponent<LayoutElement>().preferredWidth = 26;

            int idx = 0;
            btnPrev.onClick.AddListener(() => {
                idx = (idx - 1 + opts.Length) % opts.Length;
                vTxt.text = opts[idx]; onChanged(idx);
            });
            btnNext.onClick.AddListener(() => {
                idx = (idx + 1) % opts.Length;
                vTxt.text = opts[idx]; onChanged(idx);
            });
            return vTxt;
        }

        // ════════════════════════════════════════════════════════
        //  CONFIG READ / APPLY
        // ════════════════════════════════════════════════════════

        public MapGenConfig ReadConfig()
        {
            var c = new MapGenConfig
            {
                mapWidth = P(fMapW, 30), mapHeight = P(fMapH, 30),
                cellSize = PF(fCellSize, 6f), borderMargin = P(fMargin, 2),
                minRooms = P(fMinRooms, 5), maxRooms = P(fMaxRooms, 12),
                minRoomSize = P(fMinRoom, 3), maxRoomSize = P(fMaxRoom, 8),
                corridorWidth = P(fCorridorW, 2),
                vegetationDensity = sVeg.value, rockDensity = sRock.value, decorDensity = sDecor.value,
                seed = P(fSeed, 0), useRandomSeed = tRandomSeed.isOn, lockSeed = tLockSeed.isOn,
                mode = (GenerationMode)iMode, layoutType = (LayoutType)iLayout,
                forcedBiome = (BiomeType)iBiome, useForcedBiome = tForceBiome.isOn,
                ensureAccessibility = tAccess.isOn, validateAfterGeneration = tValidate.isOn,
                forceBossRoom = tBoss.isOn, forceSpecialRoom = tSpecial.isOn,
                minSpawnToExitDistance = PF(fMinDist, 15f)
            };
            c.enabledCategories.Clear();
            foreach (var kv in catToggles) if (kv.Value.isOn) c.enabledCategories.Add(kv.Key);
            return c;
        }

        public void ApplyConfig(MapGenConfig c)
        {
            fMapW.text = c.mapWidth.ToString(); fMapH.text = c.mapHeight.ToString();
            fCellSize.text = c.cellSize.ToString("F1"); fMargin.text = c.borderMargin.ToString();
            fMinRooms.text = c.minRooms.ToString(); fMaxRooms.text = c.maxRooms.ToString();
            fMinRoom.text = c.minRoomSize.ToString(); fMaxRoom.text = c.maxRoomSize.ToString();
            fCorridorW.text = c.corridorWidth.ToString();
            sVeg.value = c.vegetationDensity; sRock.value = c.rockDensity; sDecor.value = c.decorDensity;
            fSeed.text = c.seed.ToString();
            tRandomSeed.isOn = c.useRandomSeed; tLockSeed.isOn = c.lockSeed;
            iMode = (int)c.mode; if (txtMode) txtMode.text = ModeNames[iMode];
            iLayout = (int)c.layoutType; if (txtLayout) txtLayout.text = LayoutNames[iLayout];
            iBiome = (int)c.forcedBiome; if (txtBiome) txtBiome.text = BiomeNames[iBiome];
            tForceBiome.isOn = c.useForcedBiome;
            tAccess.isOn = c.ensureAccessibility; tValidate.isOn = c.validateAfterGeneration;
            tBoss.isOn = c.forceBossRoom; tSpecial.isOn = c.forceSpecialRoom;
            fMinDist.text = c.minSpawnToExitDistance.ToString("F0");
            foreach (var kv in catToggles)
                kv.Value.isOn = c.enabledCategories.Count == 0 || c.enabledCategories.Contains(kv.Key);
        }

        // ════════════════════════════════════════════════════════
        //  MISE À JOUR RÉSULTATS
        // ════════════════════════════════════════════════════════

        public void UpdateResults(GenerationResult r)
        {
            if (r == null) return;

            mStatus.text = $"Statut: {r.status}";
            mStatus.color = DebugUIBuilder.GetStatusColor(r.status);
            mSeed.text = $"Seed: {r.seed}";
            mTime.text = $"Temps: {r.generationTimeMs:F1}ms";
            mRooms.text = $"Salles: {r.roomCount}";
            mObjects.text = $"Objets: {r.totalObjectsPlaced}";
            mErrors.text = $"Erreurs: {r.errorCount}";
            mErrors.color = r.errorCount > 0 ? DebugUIBuilder.Error : DebugUIBuilder.TextWhite;
            mWarnings.text = $"Warn: {r.warningCount}";
            mWarnings.color = r.warningCount > 0 ? DebugUIBuilder.Warning : DebugUIBuilder.TextWhite;
            fSeed.text = r.seed.ToString();

            RefreshContent(summaryContent, c =>
            {
                AddLine(c, $"Seed: {r.seed}", 11);
                AddLine(c, $"Temps: {r.generationTimeMs:F1} ms", 11);
                AddLine(c, $"Salles: {r.roomCount}  |  Couloirs: {r.corridorCount}", 11);
                AddLine(c, $"Marchables: {r.walkableCellCount}  |  Murs: {r.wallCellCount}", 11);
                AddLine(c, $"Objets: {r.totalObjectsPlaced}", 11);
                AddLine(c, $"Spawn: ({r.spawnCell.x},{r.spawnCell.y}) > Sortie: ({r.exitCell.x},{r.exitCell.y})", 11);
                AddLine(c, $"Distance: {r.spawnToExitDistance:F1}", 11);
                if (r.objectsPerCategory.Count > 0)
                {
                    AddLine(c, "-- Par categorie --", 10).color = DebugUIBuilder.TextDim;
                    foreach (var kv in r.objectsPerCategory)
                        AddLine(c, $"  {kv.Key}: {kv.Value}", 10);
                }
            });

            RefreshContent(validationContent, c =>
            {
                if (r.validationEntries.Count == 0)
                { AddLine(c, "Aucune entree", 10).color = DebugUIBuilder.TextDim; return; }
                foreach (var e in r.validationEntries)
                    AddLine(c, e.ToString(), 10).color = DebugUIBuilder.GetSeverityColor(e.severity);
            });

            RefreshContent(logContent, c =>
            {
                foreach (var e in r.validationEntries)
                    AddLine(c, e.ToString(), 9).color = DebugUIBuilder.GetSeverityColor(e.severity);
                foreach (var s in r.pipelineSteps)
                    AddLine(c, s, 9).color = DebugUIBuilder.TextDim;
            });

            seedHistory.Remove(r.seed);
            seedHistory.Insert(0, r.seed);
            if (seedHistory.Count > 15) seedHistory.RemoveAt(seedHistory.Count - 1);
        }

        public void UpdateBatchStatus(string s) { if (lblBatch) lblBatch.text = s; }

        public void RefreshCategories(AssetCategoryRegistry reg)
        {
            if (catContainer == null || reg == null) return;
            for (int i = catContainer.childCount - 1; i >= 1; i--)
                Destroy(catContainer.GetChild(i).gameObject);
            catToggles.Clear();
            foreach (var cat in reg.categories)
            {
                if (cat == null) continue;
                catToggles[cat.categoryId] = DebugUIBuilder.CreateToggle(catContainer, cat.displayName, true);
            }
        }

        void SetAllCat(bool v) { foreach (var t in catToggles.Values) t.isOn = v; }

        void ClearLogs()
        {
            RefreshContent(logContent, c =>
                AddLine(c, "Logs effaces", 10).color = DebugUIBuilder.TextDim);
        }

        void RefreshContent(RectTransform content, Action<RectTransform> build)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);
            build(content);
        }

        TextMeshProUGUI AddLine(Transform parent, string text, int size)
        {
            return DebugUIBuilder.CreateLabel(parent, text, size, height: 16);
        }

        static int P(TMP_InputField f, int fb) => int.TryParse(f.text, out int v) ? v : fb;
        static float PF(TMP_InputField f, float fb) => float.TryParse(f.text, out float v) ? v : fb;
        static int ParseInt(string s, int fb) => int.TryParse(s, out int v) ? v : fb;

        // ════════════════════════════════════════════════════════
        //  RACCOURCIS CLAVIER
        // ════════════════════════════════════════════════════════

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5)) controller.Generate();
            if (Input.GetKeyDown(KeyCode.F6)) controller.Regenerate();
            if (Input.GetKeyDown(KeyCode.F7)) controller.ClearMap();
            if (Input.GetKeyDown(KeyCode.F8)) controller.SpawnPlayer();
            if (Input.GetKeyDown(KeyCode.F9)) controller.TakeScreenshot();
            if (Input.GetKeyDown(KeyCode.F10)) controller.EnterFPSMode();
            if (Input.GetKeyDown(KeyCode.F12)) controller.ExportLog();
            if (Input.GetKeyDown(KeyCode.Tab)) ToggleSidebar();
        }
    }
}
