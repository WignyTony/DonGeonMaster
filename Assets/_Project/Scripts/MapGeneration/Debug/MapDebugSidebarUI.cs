using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonGeonMaster.MapGeneration.DebugTools
{
    public class MapDebugSidebarUI : MonoBehaviour
    {
        GameObject panel;
        TMP_InputField fSeed, fWidth, fHeight, fCellSize, fMinRooms, fMaxRooms;
        TMP_InputField fCorridorW, fMargin;
        TMP_InputField fVegDensity, fRockDensity, fDecorDensity;
        TMP_InputField fPresetName, fBatchCount;
        Toggle tPlaceAssets, tForceBiome;
        TMP_InputField fBiomeIndex;
        Transform presetListParent, seedHistoryParent, categoryParent;
        TextMeshProUGUI batchStatusText;
        Dictionary<string, Toggle> categoryToggles = new();

        public Action OnGenerate, OnRegenerate, OnClear, OnHero;
        public Action<string> OnSavePreset, OnLoadPreset;
        public Action OnExportLog, OnOpenLogs, OnCopySeed;
        public Action<int> OnBatchStart;
        public Action OnBatchCancel;
        public Action<int> OnReplaySeed;

        List<int> seedHistory = new();
        const int MaxHistory = 12;

        public void Build()
        {
            var canvasGO = new GameObject("SidebarCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            panel = new GameObject("Sidebar");
            panel.transform.SetParent(canvasGO.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0, 0);
            prt.anchorMax = new Vector2(0, 1);
            prt.pivot = new Vector2(0, 0.5f);
            prt.sizeDelta = new Vector2(320, 0);
            panel.AddComponent<Image>().color = new Color(0.07f, 0.07f, 0.10f, 0.95f);

            var scrollGO = new GameObject("Scroll");
            scrollGO.transform.SetParent(panel.transform, false);
            Stretch(scrollGO);
            scrollGO.AddComponent<Image>().color = Color.clear;
            var scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;

            var vpGO = new GameObject("Viewport");
            vpGO.transform.SetParent(scrollGO.transform, false);
            Stretch(vpGO);
            vpGO.AddComponent<Image>().color = Color.clear;
            vpGO.AddComponent<RectMask2D>();
            scroll.viewport = vpGO.GetComponent<RectTransform>();

            var content = new GameObject("Content");
            content.transform.SetParent(vpGO.transform, false);
            var crt = content.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 6, 6);
            vlg.spacing = 3;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = crt;

            var c = content.transform;

            Header(c, "MAP GEN DEBUG", 15, new Color(0.12f, 0.20f, 0.35f), 26);

            // ── Structure ──
            Header(c, "Structure", 10, new Color(0.13f, 0.13f, 0.18f), 20);
            fSeed      = Row(c, "Seed", "0", TMP_InputField.ContentType.IntegerNumber);
            fWidth     = Row(c, "Largeur", "30", TMP_InputField.ContentType.IntegerNumber);
            fHeight    = Row(c, "Hauteur", "30", TMP_InputField.ContentType.IntegerNumber);
            fCellSize  = Row(c, "Cellule", "6", TMP_InputField.ContentType.DecimalNumber);
            fMinRooms  = Row(c, "Salles min", "5", TMP_InputField.ContentType.IntegerNumber);
            fMaxRooms  = Row(c, "Salles max", "10", TMP_InputField.ContentType.IntegerNumber);
            fCorridorW = Row(c, "Couloir larg.", "2", TMP_InputField.ContentType.IntegerNumber);
            fMargin    = Row(c, "Marge bord", "2", TMP_InputField.ContentType.IntegerNumber);

            // ── Densites ──
            Header(c, "Densites", 10, new Color(0.13f, 0.13f, 0.18f), 20);
            fVegDensity   = Row(c, "Vegetation", "0.6", TMP_InputField.ContentType.DecimalNumber);
            fRockDensity  = Row(c, "Roches", "0.2", TMP_InputField.ContentType.DecimalNumber);
            fDecorDensity = Row(c, "Decor", "0.3", TMP_InputField.ContentType.DecimalNumber);

            // ── Biome ──
            Header(c, "Biome", 10, new Color(0.13f, 0.13f, 0.18f), 20);
            tForceBiome = Tgl(c, "Forcer biome", false);
            fBiomeIndex = Row(c, "Biome (0-7)", "0", TMP_InputField.ContentType.IntegerNumber);
            SmallText(c, "0=Foret 1=Automne 2=Hiver 3=Prairie 4=Desert 5=Marecage 6=Rocailleux 7=Fantaisie");

            // ── Assets ──
            Header(c, "Assets (par dessus blockout)", 10, new Color(0.13f, 0.13f, 0.18f), 20);
            tPlaceAssets = Tgl(c, "Placer les assets", false);
            categoryParent = c;
            // Les toggles de categories seront ajoutes par SetupCategories()

            // ── Actions ──
            Header(c, "Actions", 10, new Color(0.13f, 0.13f, 0.18f), 20);
            var r1 = BtnRow(c);
            Btn(r1, "GENERER (F5)", new Color(0.15f, 0.45f, 0.22f), () => OnGenerate?.Invoke());
            Btn(r1, "REGEN (F6)", new Color(0.22f, 0.30f, 0.45f), () => OnRegenerate?.Invoke());
            var r2 = BtnRow(c);
            Btn(r2, "HERO (F10)", new Color(0.35f, 0.25f, 0.45f), () => OnHero?.Invoke());
            Btn(r2, "CLEAR (F7)", new Color(0.50f, 0.20f, 0.18f), () => OnClear?.Invoke());

            // ── Outils ──
            Header(c, "Outils", 10, new Color(0.13f, 0.13f, 0.18f), 20);
            var r3 = BtnRow(c);
            Btn(r3, "Copier Seed", new Color(0.25f, 0.25f, 0.35f), () => OnCopySeed?.Invoke());
            Btn(r3, "Export Log", new Color(0.25f, 0.25f, 0.35f), () => OnExportLog?.Invoke());
            Btn(c, "Ouvrir Logs", new Color(0.20f, 0.20f, 0.30f), () => OnOpenLogs?.Invoke());

            // ── Batch ──
            Header(c, "Batch Test", 10, new Color(0.13f, 0.13f, 0.18f), 20);
            fBatchCount = Row(c, "Iterations", "100", TMP_InputField.ContentType.IntegerNumber);
            var rb = BtnRow(c);
            Btn(rb, "Lancer", new Color(0.50f, 0.35f, 0.12f), () => OnBatchStart?.Invoke(Int(fBatchCount, 100)));
            Btn(rb, "Annuler", new Color(0.50f, 0.20f, 0.18f), () => OnBatchCancel?.Invoke());
            batchStatusText = SmallTextReturn(c, "Aucun batch");

            // ── History ──
            Header(c, "Historique Seeds", 10, new Color(0.13f, 0.13f, 0.18f), 20);
            seedHistoryParent = c;

            // ── Presets ──
            Header(c, "Presets", 10, new Color(0.13f, 0.13f, 0.18f), 20);
            fPresetName = Row(c, "Nom", "", TMP_InputField.ContentType.Standard);
            var r4 = BtnRow(c);
            Btn(r4, "Sauver", new Color(0.20f, 0.35f, 0.25f), () =>
            {
                string n = fPresetName.text;
                if (string.IsNullOrWhiteSpace(n)) n = "Preset_" + DateTime.Now.ToString("HHmmss");
                OnSavePreset?.Invoke(n);
                RefreshPresetList();
            });
            Btn(r4, "Charger", new Color(0.25f, 0.25f, 0.40f), () =>
            {
                string n = fPresetName.text;
                if (!string.IsNullOrWhiteSpace(n)) OnLoadPreset?.Invoke(n);
            });
            presetListParent = c;
            RefreshPresetList();

            Spacer(c, 4);
            SmallText(c, "Tab=Sidebar  Molette=Zoom  Clic droit=Pan");
        }

        // ════════════════════════════════════════════
        //  CATEGORIES
        // ════════════════════════════════════════════

        public void SetupCategories(AssetCategoryRegistry registry)
        {
            if (categoryParent == null || registry == null) return;
            categoryToggles.Clear();
            foreach (var cat in registry.categories)
            {
                if (cat == null) continue;
                categoryToggles[cat.categoryId] = Tgl(categoryParent, cat.displayName, true);
            }
        }

        public bool PlaceAssetsEnabled => tPlaceAssets != null && tPlaceAssets.isOn;

        public List<string> GetEnabledCategories()
        {
            var list = new List<string>();
            foreach (var kv in categoryToggles)
                if (kv.Value.isOn) list.Add(kv.Key);
            return list;
        }

        // ════════════════════════════════════════════
        //  SEED HISTORY
        // ════════════════════════════════════════════

        public void RecordSeed(int seed)
        {
            seedHistory.Remove(seed);
            seedHistory.Insert(0, seed);
            if (seedHistory.Count > MaxHistory) seedHistory.RemoveAt(seedHistory.Count - 1);
            RefreshSeedHistory();
        }

        void RefreshSeedHistory()
        {
            if (seedHistoryParent == null) return;
            RemoveTagged(seedHistoryParent, "SH_");
            foreach (int seed in seedHistory)
            {
                int s = seed;
                TaggedBtn(seedHistoryParent, $"SH_{s}", $"Seed: {s}",
                    new Color(0.10f, 0.10f, 0.15f), new Color(0.65f, 0.75f, 0.90f),
                    () => OnReplaySeed?.Invoke(s));
            }
        }

        public void UpdateBatchStatus(string status)
        {
            if (batchStatusText != null) batchStatusText.text = status;
        }

        void RefreshPresetList()
        {
            if (presetListParent == null) return;
            RemoveTagged(presetListParent, "PB_");
            foreach (var name in PresetManager.GetAvailablePresets())
            {
                var n = name;
                TaggedBtn(presetListParent, $"PB_{n}", n,
                    new Color(0.12f, 0.12f, 0.18f), new Color(0.7f, 0.7f, 0.75f),
                    () => OnLoadPreset?.Invoke(n));
            }
        }

        // ════════════════════════════════════════════
        //  READ / WRITE CONFIG
        // ════════════════════════════════════════════

        public MapGenConfig ReadConfig()
        {
            var cfg = new MapGenConfig();
            cfg.seed = Int(fSeed, 0);
            cfg.useRandomSeed = cfg.seed == 0;
            cfg.mapWidth = Int(fWidth, 30);
            cfg.mapHeight = Int(fHeight, 30);
            cfg.cellSize = Flt(fCellSize, 6f);
            cfg.minRooms = Int(fMinRooms, 5);
            cfg.maxRooms = Int(fMaxRooms, 10);
            cfg.corridorWidth = Int(fCorridorW, 2);
            cfg.borderMargin = Int(fMargin, 2);
            cfg.vegetationDensity = Flt(fVegDensity, 0.6f);
            cfg.rockDensity = Flt(fRockDensity, 0.2f);
            cfg.decorDensity = Flt(fDecorDensity, 0.3f);
            cfg.useForcedBiome = tForceBiome != null && tForceBiome.isOn;
            int bi = Int(fBiomeIndex, 0);
            cfg.forcedBiome = (BiomeType)Mathf.Clamp(bi, 0, 7);
            cfg.enabledCategories = GetEnabledCategories();
            cfg.validateAfterGeneration = true;
            return cfg;
        }

        public void ApplyConfig(MapGenConfig cfg)
        {
            if (cfg == null) return;
            fSeed.text = cfg.seed.ToString();
            fWidth.text = cfg.mapWidth.ToString();
            fHeight.text = cfg.mapHeight.ToString();
            fCellSize.text = cfg.cellSize.ToString("F1");
            fMinRooms.text = cfg.minRooms.ToString();
            fMaxRooms.text = cfg.maxRooms.ToString();
            fCorridorW.text = cfg.corridorWidth.ToString();
            fMargin.text = cfg.borderMargin.ToString();
            fVegDensity.text = cfg.vegetationDensity.ToString("F2");
            fRockDensity.text = cfg.rockDensity.ToString("F2");
            fDecorDensity.text = cfg.decorDensity.ToString("F2");
            if (tForceBiome != null) tForceBiome.isOn = cfg.useForcedBiome;
            fBiomeIndex.text = ((int)cfg.forcedBiome).ToString();
        }

        public void WriteSeed(int seed) { if (fSeed != null) fSeed.text = seed.ToString(); }
        public void Show() { if (panel) panel.SetActive(true); }
        public void Hide() { if (panel) panel.SetActive(false); }
        public bool IsVisible => panel != null && panel.activeSelf;

        // ════════════════════════════════════════════
        //  UI BUILDERS
        // ════════════════════════════════════════════

        void Header(Transform p, string t, int fs, Color bg, float h)
        {
            var go = new GameObject("Hdr");
            go.transform.SetParent(p, false);
            go.AddComponent<LayoutElement>().preferredHeight = h;
            go.AddComponent<Image>().color = bg;
            var tgo = new GameObject("T");
            tgo.transform.SetParent(go.transform, false);
            Stretch(tgo, 8, 0);
            var tmp = tgo.AddComponent<TextMeshProUGUI>();
            tmp.text = t; tmp.fontSize = fs;
            tmp.fontStyle = FontStyles.Bold; tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
        }

        TMP_InputField Row(Transform p, string label, string def, TMP_InputField.ContentType ct)
        {
            var go = new GameObject($"R_{label}");
            go.transform.SetParent(p, false);
            go.AddComponent<LayoutElement>().preferredHeight = 28;
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6; hlg.padding = new RectOffset(4, 4, 0, 0);
            hlg.childControlWidth = true; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;

            var lgo = new GameObject("L");
            lgo.transform.SetParent(go.transform, false);
            var lle = lgo.AddComponent<LayoutElement>();
            lle.preferredWidth = 100; lle.flexibleWidth = 0;
            var lbl = lgo.AddComponent<TextMeshProUGUI>();
            lbl.text = label; lbl.fontSize = 11;
            lbl.color = new Color(0.6f, 0.6f, 0.65f);
            lbl.alignment = TextAlignmentOptions.MidlineLeft;

            var igo = new GameObject("I");
            igo.transform.SetParent(go.transform, false);
            var ile = igo.AddComponent<LayoutElement>();
            ile.preferredWidth = 100; ile.flexibleWidth = 1;
            igo.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.07f);
            var input = igo.AddComponent<TMP_InputField>();
            input.contentType = ct;

            var ta = new GameObject("TA");
            ta.transform.SetParent(igo.transform, false);
            Stretch(ta, 5, 1); ta.AddComponent<RectMask2D>();

            var txt = new GameObject("Txt");
            txt.transform.SetParent(ta.transform, false);
            Stretch(txt);
            var t1 = txt.AddComponent<TextMeshProUGUI>();
            t1.fontSize = 11; t1.color = Color.white;
            t1.textWrappingMode = TextWrappingModes.NoWrap;

            var ph = new GameObject("PH");
            ph.transform.SetParent(ta.transform, false);
            Stretch(ph);
            var t2 = ph.AddComponent<TextMeshProUGUI>();
            t2.text = "..."; t2.fontSize = 11;
            t2.color = new Color(0.35f, 0.35f, 0.40f);
            t2.fontStyle = FontStyles.Italic;
            t2.textWrappingMode = TextWrappingModes.NoWrap;

            input.textViewport = ta.GetComponent<RectTransform>();
            input.textComponent = t1; input.placeholder = t2;
            input.text = def; input.caretColor = Color.white;
            return input;
        }

        Toggle Tgl(Transform p, string label, bool def)
        {
            var go = new GameObject($"T_{label}");
            go.transform.SetParent(p, false);
            go.AddComponent<LayoutElement>().preferredHeight = 24;
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6; hlg.padding = new RectOffset(4, 0, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false; hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

            var bgGO = new GameObject("Bg");
            bgGO.transform.SetParent(go.transform, false);
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.04f, 0.07f);
            bgGO.GetComponent<RectTransform>().sizeDelta = new Vector2(16, 16);

            var chkGO = new GameObject("Chk");
            chkGO.transform.SetParent(bgGO.transform, false);
            var chkRT = chkGO.AddComponent<RectTransform>();
            chkRT.anchorMin = new Vector2(0.2f, 0.2f);
            chkRT.anchorMax = new Vector2(0.8f, 0.8f);
            chkRT.offsetMin = Vector2.zero;
            chkRT.offsetMax = Vector2.zero;
            var chkImg = chkGO.AddComponent<Image>();
            chkImg.color = new Color(0.25f, 0.85f, 0.35f);

            var lblGO = new GameObject("L");
            lblGO.transform.SetParent(go.transform, false);
            lblGO.AddComponent<RectTransform>().sizeDelta = new Vector2(200, 20);
            var lbl = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text = label; lbl.fontSize = 11;
            lbl.color = new Color(0.65f, 0.65f, 0.70f);
            lbl.alignment = TextAlignmentOptions.MidlineLeft;

            var toggle = go.AddComponent<Toggle>();
            toggle.isOn = def;
            toggle.graphic = chkImg;
            toggle.targetGraphic = bgImg;
            return toggle;
        }

        Transform BtnRow(Transform p)
        {
            var go = new GameObject("BR");
            go.transform.SetParent(p, false);
            go.AddComponent<LayoutElement>().preferredHeight = 30;
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4; hlg.childControlWidth = true;
            hlg.childControlHeight = true; hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            return go.transform;
        }

        void Btn(Transform p, string text, Color col, Action click)
        {
            var go = new GameObject("B");
            go.transform.SetParent(p, false);
            go.AddComponent<LayoutElement>().flexibleWidth = 1;
            go.AddComponent<Image>().color = col;
            var btn = go.AddComponent<Button>();
            var c = btn.colors;
            c.highlightedColor = col * 1.25f; c.pressedColor = col * 0.7f;
            c.fadeDuration = 0.05f; btn.colors = c;
            btn.onClick.AddListener(() => click());
            var tgo = new GameObject("T");
            tgo.transform.SetParent(go.transform, false);
            Stretch(tgo);
            var tmp = tgo.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = 10;
            tmp.fontStyle = FontStyles.Bold; tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        void TaggedBtn(Transform p, string tag, string text, Color bg, Color textCol, Action click)
        {
            var go = new GameObject(tag);
            go.transform.SetParent(p, false);
            go.AddComponent<LayoutElement>().preferredHeight = 22;
            go.AddComponent<Image>().color = bg;
            var btn = go.AddComponent<Button>();
            var c = btn.colors;
            c.highlightedColor = bg * 1.3f; btn.colors = c;
            btn.onClick.AddListener(() => click());
            var tgo = new GameObject("T");
            tgo.transform.SetParent(go.transform, false);
            Stretch(tgo, 6, 0);
            var tmp = tgo.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = 10;
            tmp.color = textCol; tmp.alignment = TextAlignmentOptions.MidlineLeft;
        }

        void RemoveTagged(Transform p, string prefix)
        {
            var toRemove = new List<GameObject>();
            for (int i = 0; i < p.childCount; i++)
                if (p.GetChild(i).name.StartsWith(prefix)) toRemove.Add(p.GetChild(i).gameObject);
            foreach (var go in toRemove) DestroyImmediate(go);
        }

        void SmallText(Transform p, string text)
        {
            var go = new GameObject("Help");
            go.transform.SetParent(p, false);
            go.AddComponent<LayoutElement>().preferredHeight = 18;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = 8;
            tmp.fontStyle = FontStyles.Italic;
            tmp.color = new Color(0.4f, 0.4f, 0.45f);
            tmp.alignment = TextAlignmentOptions.Center;
        }

        TextMeshProUGUI SmallTextReturn(Transform p, string text)
        {
            var go = new GameObject("Status");
            go.transform.SetParent(p, false);
            go.AddComponent<LayoutElement>().preferredHeight = 18;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = 9;
            tmp.color = new Color(0.5f, 0.5f, 0.55f);
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        void Spacer(Transform p, float h)
        {
            var go = new GameObject("Sp");
            go.transform.SetParent(p, false);
            go.AddComponent<LayoutElement>().preferredHeight = h;
        }

        void Stretch(GameObject go, float ih = 0, float iv = 0)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(ih, iv); rt.offsetMax = new Vector2(-ih, -iv);
        }

        static int Int(TMP_InputField f, int fb) => int.TryParse(f.text, out int v) ? v : fb;
        static float Flt(TMP_InputField f, float fb) => float.TryParse(f.text, out float v) ? v : fb;
    }
}
