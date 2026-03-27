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
        TMP_InputField fPresetName;
        Transform presetListParent;

        public Action OnGenerate, OnRegenerate, OnClear, OnHero;
        public Action<string> OnSavePreset, OnLoadPreset;
        public Action OnExportLog, OnOpenLogs, OnCopySeed;

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

            // ScrollView
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

            // ── Header ──
            Header(c, "MAP GEN DEBUG", 15, new Color(0.12f, 0.20f, 0.35f), 26);

            // ── Config ──
            Header(c, "Configuration", 10, new Color(0.13f, 0.13f, 0.18f), 20);
            fSeed     = Row(c, "Seed", "0", TMP_InputField.ContentType.IntegerNumber);
            fWidth    = Row(c, "Largeur", "30", TMP_InputField.ContentType.IntegerNumber);
            fHeight   = Row(c, "Hauteur", "30", TMP_InputField.ContentType.IntegerNumber);
            fCellSize = Row(c, "Cellule", "6", TMP_InputField.ContentType.DecimalNumber);
            fMinRooms = Row(c, "Salles min", "5", TMP_InputField.ContentType.IntegerNumber);
            fMaxRooms = Row(c, "Salles max", "10", TMP_InputField.ContentType.IntegerNumber);

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
            Btn(c, "Ouvrir dossier Logs", new Color(0.20f, 0.20f, 0.30f), () => OnOpenLogs?.Invoke());

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

            // Liste des presets existants
            presetListParent = c;
            RefreshPresetList();

            // ── Aide ──
            Spacer(c, 4);
            SmallText(c, "Tab=Sidebar  Molette=Zoom  Clic droit=Pan");
        }

        void RefreshPresetList()
        {
            // Supprimer les anciens boutons de preset (tag "PresetBtn")
            if (presetListParent == null) return;
            var toRemove = new List<GameObject>();
            for (int i = 0; i < presetListParent.childCount; i++)
            {
                var child = presetListParent.GetChild(i);
                if (child.name.StartsWith("PB_")) toRemove.Add(child.gameObject);
            }
            foreach (var go in toRemove) DestroyImmediate(go);

            // Ajouter les presets disponibles
            var presets = PresetManager.GetAvailablePresets();
            foreach (var name in presets)
            {
                var n = name;
                var go = new GameObject($"PB_{n}");
                go.transform.SetParent(presetListParent, false);
                go.AddComponent<LayoutElement>().preferredHeight = 24;
                go.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f);
                var btn = go.AddComponent<Button>();
                var cols = btn.colors;
                cols.highlightedColor = new Color(0.20f, 0.20f, 0.30f);
                btn.colors = cols;
                btn.onClick.AddListener(() => OnLoadPreset?.Invoke(n));

                var tgo = new GameObject("T");
                tgo.transform.SetParent(go.transform, false);
                Stretch(tgo, 6, 0);
                var tmp = tgo.AddComponent<TextMeshProUGUI>();
                tmp.text = n;
                tmp.fontSize = 11;
                tmp.color = new Color(0.7f, 0.7f, 0.75f);
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
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
        }

        public void WriteSeed(int seed) { if (fSeed != null) fSeed.text = seed.ToString(); }
        public void Show() { if (panel) panel.SetActive(true); }
        public void Hide() { if (panel) panel.SetActive(false); }
        public bool IsVisible => panel != null && panel.activeSelf;

        // ════════════════════════════════════════════
        //  UI BUILDERS
        // ════════════════════════════════════════════

        void Header(Transform parent, string text, int fontSize, Color bg, float height)
        {
            var go = new GameObject("Hdr");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = height;
            go.AddComponent<Image>().color = bg;
            var tgo = new GameObject("T");
            tgo.transform.SetParent(go.transform, false);
            Stretch(tgo, 8, 0);
            var tmp = tgo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
        }

        TMP_InputField Row(Transform parent, string label, string defaultVal,
            TMP_InputField.ContentType contentType)
        {
            var go = new GameObject($"R_{label}");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 28;
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.padding = new RectOffset(4, 4, 0, 0);
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            var lblGO = new GameObject("L");
            lblGO.transform.SetParent(go.transform, false);
            var lle = lblGO.AddComponent<LayoutElement>();
            lle.preferredWidth = 100; lle.flexibleWidth = 0;
            var lbl = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text = label; lbl.fontSize = 11;
            lbl.color = new Color(0.6f, 0.6f, 0.65f);
            lbl.alignment = TextAlignmentOptions.MidlineLeft;

            var iGO = new GameObject("I");
            iGO.transform.SetParent(go.transform, false);
            var ile = iGO.AddComponent<LayoutElement>();
            ile.preferredWidth = 100; ile.flexibleWidth = 1;
            iGO.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.07f);
            var input = iGO.AddComponent<TMP_InputField>();
            input.contentType = contentType;

            var taGO = new GameObject("TA");
            taGO.transform.SetParent(iGO.transform, false);
            Stretch(taGO, 5, 1);
            taGO.AddComponent<RectMask2D>();

            var txtGO = new GameObject("Txt");
            txtGO.transform.SetParent(taGO.transform, false);
            Stretch(txtGO);
            var txt = txtGO.AddComponent<TextMeshProUGUI>();
            txt.fontSize = 11; txt.color = Color.white;
            txt.textWrappingMode = TextWrappingModes.NoWrap;

            var phGO = new GameObject("PH");
            phGO.transform.SetParent(taGO.transform, false);
            Stretch(phGO);
            var ph = phGO.AddComponent<TextMeshProUGUI>();
            ph.text = "..."; ph.fontSize = 11;
            ph.color = new Color(0.35f, 0.35f, 0.40f);
            ph.fontStyle = FontStyles.Italic;
            ph.textWrappingMode = TextWrappingModes.NoWrap;

            input.textViewport = taGO.GetComponent<RectTransform>();
            input.textComponent = txt;
            input.placeholder = ph;
            input.text = defaultVal;
            input.caretColor = Color.white;
            return input;
        }

        Transform BtnRow(Transform parent)
        {
            var go = new GameObject("BR");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 30;
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            return go.transform;
        }

        void Btn(Transform parent, string text, Color color, Action onClick)
        {
            var go = new GameObject("B");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().flexibleWidth = 1;
            go.AddComponent<Image>().color = color;
            var btn = go.AddComponent<Button>();
            var cols = btn.colors;
            cols.highlightedColor = color * 1.25f;
            cols.pressedColor = color * 0.7f;
            cols.fadeDuration = 0.05f;
            btn.colors = cols;
            btn.onClick.AddListener(() => onClick());
            var tgo = new GameObject("T");
            tgo.transform.SetParent(go.transform, false);
            Stretch(tgo);
            var tmp = tgo.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = 10;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        void SmallText(Transform parent, string text)
        {
            var go = new GameObject("Help");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 18;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = 9;
            tmp.fontStyle = FontStyles.Italic;
            tmp.color = new Color(0.4f, 0.4f, 0.45f);
            tmp.alignment = TextAlignmentOptions.Center;
        }

        void Spacer(Transform parent, float h)
        {
            var go = new GameObject("Sp");
            go.transform.SetParent(parent, false);
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
