using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonGeonMaster.MapGeneration.DebugTools
{
    /// <summary>
    /// Phase 4 : sidebar de config reelle. Champs editables, boutons d'action.
    /// Cree son propre Canvas. Show/Hide pilote par le ModeController.
    /// </summary>
    public class MapDebugSidebarUI : MonoBehaviour
    {
        GameObject panel;
        TMP_InputField fSeed, fWidth, fHeight, fCellSize, fMinRooms, fMaxRooms;

        // Callbacks assignes par le controller
        public Action OnGenerate, OnRegenerate, OnClear, OnHero;

        public void Build()
        {
            // Canvas
            var canvasGO = new GameObject("SidebarCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Panel gauche
            panel = new GameObject("SidebarPanel");
            panel.transform.SetParent(canvasGO.transform, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 0.5f);
            rt.sizeDelta = new Vector2(340, 0);
            panel.AddComponent<Image>().color = new Color(0.07f, 0.07f, 0.11f, 0.94f);

            // Contenu via VerticalLayoutGroup
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(panel.transform, false);
            var crt = contentGO.AddComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(12, 12);
            crt.offsetMax = new Vector2(-12, -12);
            var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Titre
            Label(contentGO.transform, "MAP GEN DEBUG", 18, FontStyles.Bold, 32,
                new Color(0.12f, 0.20f, 0.35f));

            // Separateur
            Label(contentGO.transform, "--- Configuration ---", 12, FontStyles.Normal, 20,
                Color.clear, new Color(0.5f, 0.5f, 0.55f));

            // Champs
            fSeed = Field(contentGO.transform, "Seed", "0", TMP_InputField.ContentType.IntegerNumber);
            fWidth = Field(contentGO.transform, "Largeur", "30", TMP_InputField.ContentType.IntegerNumber);
            fHeight = Field(contentGO.transform, "Hauteur", "30", TMP_InputField.ContentType.IntegerNumber);
            fCellSize = Field(contentGO.transform, "Cellule", "6", TMP_InputField.ContentType.DecimalNumber);
            fMinRooms = Field(contentGO.transform, "Salles min", "5", TMP_InputField.ContentType.IntegerNumber);
            fMaxRooms = Field(contentGO.transform, "Salles max", "10", TMP_InputField.ContentType.IntegerNumber);

            // Separateur
            Label(contentGO.transform, "--- Actions ---", 12, FontStyles.Normal, 24,
                Color.clear, new Color(0.5f, 0.5f, 0.55f));

            // Boutons
            Btn(contentGO.transform, "GENERER  (F5)", new Color(0.15f, 0.45f, 0.22f), () => OnGenerate?.Invoke());
            Btn(contentGO.transform, "REGENERER  (F6)", new Color(0.22f, 0.30f, 0.45f), () => OnRegenerate?.Invoke());
            Btn(contentGO.transform, "HERO  (F10)", new Color(0.35f, 0.25f, 0.45f), () => OnHero?.Invoke());
            Btn(contentGO.transform, "CLEAR  (F7)", new Color(0.50f, 0.20f, 0.18f), () => OnClear?.Invoke());

            // Aide
            Label(contentGO.transform, "\nTab = Toggle sidebar\nMolette = Zoom\nClic droit = Pan",
                11, FontStyles.Italic, 60, Color.clear, new Color(0.45f, 0.45f, 0.50f));
        }

        public MapGenConfig ReadConfig()
        {
            var c = new MapGenConfig();
            c.seed = Int(fSeed, 0);
            c.useRandomSeed = c.seed == 0;
            c.mapWidth = Int(fWidth, 30);
            c.mapHeight = Int(fHeight, 30);
            c.cellSize = Flt(fCellSize, 6f);
            c.minRooms = Int(fMinRooms, 5);
            c.maxRooms = Int(fMaxRooms, 10);
            c.validateAfterGeneration = true;
            return c;
        }

        public void WriteSeed(int seed) { if (fSeed != null) fSeed.text = seed.ToString(); }

        public void Show() { if (panel != null) panel.SetActive(true); }
        public void Hide() { if (panel != null) panel.SetActive(false); }
        public bool IsVisible => panel != null && panel.activeSelf;

        // ════════════════════════════════════════════
        //  HELPERS UI (Image et TMP toujours sur des GO separes)
        // ════════════════════════════════════════════

        void Label(Transform parent, string text, int size, FontStyles style, float height,
            Color bgColor, Color? textColor = null)
        {
            var go = new GameObject("Lbl");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = height;

            // BG (Image) sur ce GO
            if (bgColor.a > 0.01f)
                go.AddComponent<Image>().color = bgColor;

            // Texte sur un enfant
            var txtGO = new GameObject("T");
            txtGO.transform.SetParent(go.transform, false);
            var trt = txtGO.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(8, 0);
            trt.offsetMax = new Vector2(-8, 0);
            var tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = textColor ?? Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
        }

        TMP_InputField Field(Transform parent, string label, string defaultVal,
            TMP_InputField.ContentType contentType)
        {
            // Row
            var row = new GameObject($"Row_{label}");
            row.transform.SetParent(parent, false);
            row.AddComponent<LayoutElement>().preferredHeight = 28;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // Label (TMP seul, pas d'Image)
            var lblGO = new GameObject("Lbl");
            lblGO.transform.SetParent(row.transform, false);
            var lblLE = lblGO.AddComponent<LayoutElement>();
            lblLE.preferredWidth = 100;
            lblLE.flexibleWidth = 0;
            var lblTmp = lblGO.AddComponent<TextMeshProUGUI>();
            lblTmp.text = label;
            lblTmp.fontSize = 12;
            lblTmp.color = new Color(0.65f, 0.65f, 0.70f);
            lblTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // InputField
            var inputGO = new GameObject("Input");
            inputGO.transform.SetParent(row.transform, false);
            inputGO.AddComponent<LayoutElement>().flexibleWidth = 1;
            inputGO.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f);
            var input = inputGO.AddComponent<TMP_InputField>();
            input.contentType = contentType;

            // TextArea (enfant de Input)
            var taGO = new GameObject("TextArea");
            taGO.transform.SetParent(inputGO.transform, false);
            var taRT = taGO.AddComponent<RectTransform>();
            taRT.anchorMin = Vector2.zero;
            taRT.anchorMax = Vector2.one;
            taRT.offsetMin = new Vector2(6, 1);
            taRT.offsetMax = new Vector2(-6, -1);
            taGO.AddComponent<RectMask2D>();

            // Text (enfant de TextArea)
            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(taGO.transform, false);
            var txtRT = txtGO.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero;
            txtRT.offsetMax = Vector2.zero;
            var txt = txtGO.AddComponent<TextMeshProUGUI>();
            txt.fontSize = 12;
            txt.color = Color.white;
            txt.textWrappingMode = TextWrappingModes.NoWrap;

            // Placeholder (enfant de TextArea)
            var phGO = new GameObject("PH");
            phGO.transform.SetParent(taGO.transform, false);
            var phRT = phGO.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.offsetMin = Vector2.zero;
            phRT.offsetMax = Vector2.zero;
            var ph = phGO.AddComponent<TextMeshProUGUI>();
            ph.text = "...";
            ph.fontSize = 12;
            ph.color = new Color(0.4f, 0.4f, 0.45f);
            ph.fontStyle = FontStyles.Italic;
            ph.textWrappingMode = TextWrappingModes.NoWrap;

            input.textViewport = taRT;
            input.textComponent = txt;
            input.placeholder = ph;
            input.text = defaultVal;
            input.caretColor = Color.white;

            return input;
        }

        void Btn(Transform parent, string text, Color color, Action onClick)
        {
            // Bouton (Image sur ce GO)
            var go = new GameObject($"Btn_{text}");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 32;
            go.AddComponent<Image>().color = color;
            var btn = go.AddComponent<Button>();
            var cols = btn.colors;
            cols.highlightedColor = color * 1.2f;
            cols.pressedColor = color * 0.7f;
            cols.fadeDuration = 0.05f;
            btn.colors = cols;
            btn.onClick.AddListener(() => onClick());

            // Texte sur un enfant
            var txtGO = new GameObject("T");
            txtGO.transform.SetParent(go.transform, false);
            var trt = txtGO.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            var tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 13;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        static int Int(TMP_InputField f, int fb) => int.TryParse(f.text, out int v) ? v : fb;
        static float Flt(TMP_InputField f, float fb) => float.TryParse(f.text, out float v) ? v : fb;
    }
}
