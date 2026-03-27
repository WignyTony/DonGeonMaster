using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonGeonMaster.MapGeneration.DebugTools
{
    /// <summary>
    /// Phase 4 : sidebar compacte style inspector.
    /// Lignes de 30px, label 110px + input 100px, boutons 32px.
    /// </summary>
    public class MapDebugSidebarUI : MonoBehaviour
    {
        GameObject panel;
        TMP_InputField fSeed, fWidth, fHeight, fCellSize, fMinRooms, fMaxRooms;

        public Action OnGenerate, OnRegenerate, OnClear, OnHero;

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

            // Panel
            panel = new GameObject("Sidebar");
            panel.transform.SetParent(canvasGO.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0, 0);
            prt.anchorMax = new Vector2(0, 1);
            prt.pivot = new Vector2(0, 0.5f);
            prt.sizeDelta = new Vector2(320, 0);
            panel.AddComponent<Image>().color = new Color(0.07f, 0.07f, 0.10f, 0.95f);

            // Scroll view pour le contenu
            var scrollGO = new GameObject("Scroll");
            scrollGO.transform.SetParent(panel.transform, false);
            var srt = scrollGO.AddComponent<RectTransform>();
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;
            scrollGO.AddComponent<Image>().color = Color.clear;
            var scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;

            var vpGO = new GameObject("Viewport");
            vpGO.transform.SetParent(scrollGO.transform, false);
            var vprt = vpGO.AddComponent<RectTransform>();
            vprt.anchorMin = Vector2.zero;
            vprt.anchorMax = Vector2.one;
            vprt.offsetMin = Vector2.zero;
            vprt.offsetMax = Vector2.zero;
            vpGO.AddComponent<Image>().color = Color.clear;
            vpGO.AddComponent<RectMask2D>();
            scroll.viewport = vprt;

            var content = new GameObject("Content");
            content.transform.SetParent(vpGO.transform, false);
            var crt = content.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 8, 8);
            vlg.spacing = 4;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = crt;

            var c = content.transform;

            // ── Header ──
            SectionHeader(c, "MAP GEN DEBUG", 16, new Color(0.12f, 0.20f, 0.35f), 28);

            // ── Configuration ──
            SectionHeader(c, "Configuration", 11, new Color(0.15f, 0.15f, 0.20f), 22);
            fSeed     = Row(c, "Seed", "0", TMP_InputField.ContentType.IntegerNumber);
            fWidth    = Row(c, "Largeur", "30", TMP_InputField.ContentType.IntegerNumber);
            fHeight   = Row(c, "Hauteur", "30", TMP_InputField.ContentType.IntegerNumber);
            fCellSize = Row(c, "Cellule", "6", TMP_InputField.ContentType.DecimalNumber);
            fMinRooms = Row(c, "Salles min", "5", TMP_InputField.ContentType.IntegerNumber);
            fMaxRooms = Row(c, "Salles max", "10", TMP_InputField.ContentType.IntegerNumber);

            // ── Actions ──
            SectionHeader(c, "Actions", 11, new Color(0.15f, 0.15f, 0.20f), 22);
            var r1 = BtnRow(c);
            Btn(r1, "GENERER (F5)", new Color(0.15f, 0.45f, 0.22f), () => OnGenerate?.Invoke());
            Btn(r1, "REGENERER (F6)", new Color(0.22f, 0.30f, 0.45f), () => OnRegenerate?.Invoke());
            var r2 = BtnRow(c);
            Btn(r2, "HERO (F10)", new Color(0.35f, 0.25f, 0.45f), () => OnHero?.Invoke());
            Btn(r2, "CLEAR (F7)", new Color(0.50f, 0.20f, 0.18f), () => OnClear?.Invoke());

            // ── Aide ──
            Spacer(c, 6);
            SmallText(c, "Tab = Sidebar | Molette = Zoom | Clic droit = Pan");
        }

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

        public void WriteSeed(int seed) { if (fSeed != null) fSeed.text = seed.ToString(); }
        public void Show() { if (panel) panel.SetActive(true); }
        public void Hide() { if (panel) panel.SetActive(false); }
        public bool IsVisible => panel != null && panel.activeSelf;

        // ════════════════════════════════════════════
        //  UI BUILDERS
        // ════════════════════════════════════════════

        void SectionHeader(Transform parent, string text, int fontSize, Color bgColor, float height)
        {
            var go = new GameObject("Hdr");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = height;
            go.AddComponent<Image>().color = bgColor;

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
            go.AddComponent<LayoutElement>().preferredHeight = 30;
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.padding = new RectOffset(4, 4, 0, 0);
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // Label (TMP seul)
            var lblGO = new GameObject("L");
            lblGO.transform.SetParent(go.transform, false);
            var lle = lblGO.AddComponent<LayoutElement>();
            lle.preferredWidth = 110;
            lle.flexibleWidth = 0;
            var lbl = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text = label;
            lbl.fontSize = 12;
            lbl.color = new Color(0.6f, 0.6f, 0.65f);
            lbl.alignment = TextAlignmentOptions.MidlineLeft;

            // Input (Image + TMP_InputField, texte sur enfant)
            var iGO = new GameObject("I");
            iGO.transform.SetParent(go.transform, false);
            var ile = iGO.AddComponent<LayoutElement>();
            ile.preferredWidth = 100;
            ile.flexibleWidth = 1;
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
            txt.fontSize = 12;
            txt.color = Color.white;
            txt.textWrappingMode = TextWrappingModes.NoWrap;

            var phGO = new GameObject("PH");
            phGO.transform.SetParent(taGO.transform, false);
            Stretch(phGO);
            var ph = phGO.AddComponent<TextMeshProUGUI>();
            ph.text = "...";
            ph.fontSize = 12;
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
            go.AddComponent<LayoutElement>().preferredHeight = 32;
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
            tmp.text = text;
            tmp.fontSize = 11;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        void SmallText(Transform parent, string text)
        {
            var go = new GameObject("Help");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 20;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 10;
            tmp.fontStyle = FontStyles.Italic;
            tmp.color = new Color(0.4f, 0.4f, 0.45f);
            tmp.alignment = TextAlignmentOptions.MidlineCenter;
        }

        void Spacer(Transform parent, float height)
        {
            var go = new GameObject("Sp");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = height;
        }

        /// <summary>Stretch RectTransform to fill parent with optional horizontal inset.</summary>
        void Stretch(GameObject go, float insetH = 0, float insetV = 0)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(insetH, insetV);
            rt.offsetMax = new Vector2(-insetH, -insetV);
        }

        static int Int(TMP_InputField f, int fb) => int.TryParse(f.text, out int v) ? v : fb;
        static float Flt(TMP_InputField f, float fb) => float.TryParse(f.text, out float v) ? v : fb;
    }
}
