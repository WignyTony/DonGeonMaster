using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DonGeonMaster.MapGeneration
{
    /// <summary>
    /// Fabrique d'éléments UI pour le module de debug MapGen.
    /// Tout le layout passe par LayoutElement + LayoutGroups. Aucun anchor manuel
    /// sauf pour le Root (seul élément qui doit stretch-fill le Canvas).
    /// </summary>
    public static class DebugUIBuilder
    {
        // ── Palette ──
        public static readonly Color BgDarkest  = new(0.07f, 0.07f, 0.09f, 1f);
        public static readonly Color BgDark     = new(0.10f, 0.10f, 0.13f, 1f);
        public static readonly Color BgMid      = new(0.14f, 0.14f, 0.18f, 1f);
        public static readonly Color BgLight    = new(0.18f, 0.18f, 0.23f, 1f);
        public static readonly Color HeaderBg   = new(0.13f, 0.20f, 0.33f, 1f);
        public static readonly Color BtnNormal  = new(0.22f, 0.24f, 0.34f, 1f);
        public static readonly Color BtnHover   = new(0.30f, 0.32f, 0.44f, 1f);
        public static readonly Color BtnPressed = new(0.16f, 0.17f, 0.26f, 1f);
        public static readonly Color BtnGreen   = new(0.18f, 0.50f, 0.28f, 1f);
        public static readonly Color BtnRed     = new(0.55f, 0.22f, 0.18f, 1f);
        public static readonly Color BtnOrange  = new(0.55f, 0.38f, 0.12f, 1f);
        public static readonly Color InputBg    = new(0.08f, 0.08f, 0.10f, 1f);
        public static readonly Color Success    = new(0.25f, 0.85f, 0.35f, 1f);
        public static readonly Color Warning    = new(1f, 0.80f, 0.20f, 1f);
        public static readonly Color Error      = new(1f, 0.30f, 0.30f, 1f);
        public static readonly Color TextWhite  = new(0.90f, 0.90f, 0.92f, 1f);
        public static readonly Color TextDim    = new(0.55f, 0.55f, 0.60f, 1f);

        // ────────────────────────────────────────────────────────
        //  CANVAS
        // ────────────────────────────────────────────────────────
        public static Canvas CreateCanvas(string name = "DebugCanvas")
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        // ────────────────────────────────────────────────────────
        //  CONTENEURS DE LAYOUT
        // ────────────────────────────────────────────────────────

        /// <summary>
        /// Crée un GO qui remplit entièrement son parent via anchors (0,0)→(1,1).
        /// Utilisé UNIQUEMENT pour le Root du Canvas et les viewports de ScrollView.
        /// </summary>
        public static RectTransform CreateStretchFill(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>
        /// Panel générique piloté par LayoutElement (jamais d'anchors manuels).
        /// Utiliser preferredWidth/Height pour taille fixe, flexibleWidth/Height pour remplir.
        /// </summary>
        public static RectTransform CreateLayoutPanel(Transform parent, string name, Color color,
            float preferredW = -1, float preferredH = -1, float flexW = -1, float flexH = -1)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();

            var le = go.AddComponent<LayoutElement>();
            if (preferredW >= 0) le.preferredWidth = preferredW;
            if (preferredH >= 0) le.preferredHeight = preferredH;
            if (flexW >= 0) le.flexibleWidth = flexW;
            if (flexH >= 0) le.flexibleHeight = flexH;

            var img = go.AddComponent<Image>();
            img.color = color;
            return rt;
        }

        /// <summary>HorizontalLayoutGroup row.</summary>
        public static RectTransform CreateHGroup(Transform parent, float height = -1,
            float spacing = 4, RectOffset padding = null)
        {
            var go = new GameObject("HGroup");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();

            if (height > 0)
            {
                var le = go.AddComponent<LayoutElement>();
                le.preferredHeight = height;
            }

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.padding = padding ?? new RectOffset(0, 0, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;   // enfants partagent l'espace
            hlg.childForceExpandHeight = true;
            return rt;
        }

        /// <summary>VerticalLayoutGroup container.</summary>
        public static RectTransform CreateVGroup(Transform parent, float spacing = 2,
            RectOffset padding = null, float flexH = -1, float prefH = -1)
        {
            var go = new GameObject("VGroup");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();

            if (flexH >= 0 || prefH >= 0)
            {
                var le = go.AddComponent<LayoutElement>();
                if (flexH >= 0) le.flexibleHeight = flexH;
                if (prefH >= 0) le.preferredHeight = prefH;
            }

            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = spacing;
            vlg.padding = padding ?? new RectOffset(0, 0, 0, 0);
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            return rt;
        }

        // ────────────────────────────────────────────────────────
        //  SCROLL VIEW
        // ────────────────────────────────────────────────────────
        public static ScrollRect CreateScrollView(Transform parent, float flexH = 1)
        {
            var go = new GameObject("ScrollView");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var le = go.AddComponent<LayoutElement>();
            le.flexibleHeight = flexH;
            le.flexibleWidth = 1;

            var img = go.AddComponent<Image>();
            img.color = Color.clear;

            var scroll = go.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            // Viewport (stretch-fill le ScrollView)
            var vpRT = CreateStretchFill(go.transform, "Viewport");
            vpRT.gameObject.AddComponent<Image>().color = Color.clear;
            vpRT.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = vpRT;

            // Content (ancré en haut, largeur 100%, hauteur auto)
            var content = new GameObject("Content");
            content.transform.SetParent(vpRT, false);
            var cRT = content.AddComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1);
            cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);
            cRT.offsetMin = Vector2.zero;
            cRT.offsetMax = Vector2.zero;

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 2;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = cRT;

            return scroll;
        }

        // ────────────────────────────────────────────────────────
        //  SECTION REPLIABLE
        // ────────────────────────────────────────────────────────
        public static (RectTransform header, RectTransform content) CreateCollapsibleSection(
            Transform parent, string title)
        {
            var section = new GameObject($"Sec_{title}");
            section.transform.SetParent(parent, false);
            section.AddComponent<RectTransform>();
            var sVLG = section.AddComponent<VerticalLayoutGroup>();
            sVLG.spacing = 0;
            sVLG.childControlWidth = true;
            sVLG.childControlHeight = false;
            sVLG.childForceExpandWidth = true;
            sVLG.childForceExpandHeight = false;
            section.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Header
            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(section.transform, false);
            headerGo.AddComponent<RectTransform>();
            headerGo.AddComponent<LayoutElement>().preferredHeight = 26;
            headerGo.AddComponent<Image>().color = HeaderBg;
            var headerBtn = headerGo.AddComponent<Button>();
            var bc = headerBtn.colors;
            bc.highlightedColor = new Color(0.18f, 0.26f, 0.42f);
            headerBtn.colors = bc;

            var headerText = CreateTextDirect(headerGo.transform, $"  \u25bc {title}", 12);
            headerText.fontStyle = FontStyles.Bold;
            headerText.color = TextWhite;

            // Content
            var contentGo = new GameObject("Body");
            contentGo.transform.SetParent(section.transform, false);
            var contentRT = contentGo.AddComponent<RectTransform>();
            var cVLG = contentGo.AddComponent<VerticalLayoutGroup>();
            cVLG.spacing = 3;
            cVLG.padding = new RectOffset(6, 6, 4, 6);
            cVLG.childControlWidth = true;
            cVLG.childControlHeight = false;
            cVLG.childForceExpandWidth = true;
            cVLG.childForceExpandHeight = false;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentGo.AddComponent<Image>().color = BgMid;

            headerBtn.onClick.AddListener(() =>
            {
                bool show = !contentGo.activeSelf;
                contentGo.SetActive(show);
                headerText.text = $"  {(show ? "\u25bc" : "\u25b6")} {title}";
            });

            return (headerGo.GetComponent<RectTransform>(), contentRT);
        }

        // ────────────────────────────────────────────────────────
        //  TEXTE
        // ────────────────────────────────────────────────────────
        public static TextMeshProUGUI CreateLabel(Transform parent, string text,
            int fontSize = 12, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft,
            float height = 20)
        {
            var go = new GameObject("Lbl");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<LayoutElement>().preferredHeight = height;
            return CreateTextDirect(go.transform, text, fontSize, align);
        }

        public static TextMeshProUGUI CreateTextDirect(Transform parent, string text,
            int fontSize = 12, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var go = new GameObject("Txt");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = TextWhite;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        // ────────────────────────────────────────────────────────
        //  INPUT FIELD
        // ────────────────────────────────────────────────────────
        public static (TMP_InputField field, TextMeshProUGUI label) CreateInputFieldWithLabel(
            Transform parent, string labelText, string defaultValue = "",
            string placeholder = "", float height = 24)
        {
            var row = CreateHGroup(parent, height, spacing: 4);

            // Label fixe
            var labelGo = new GameObject("Lbl");
            labelGo.transform.SetParent(row, false);
            var lLE = labelGo.AddComponent<LayoutElement>();
            lLE.preferredWidth = 100;
            lLE.flexibleWidth = 0;
            var label = CreateTextDirect(labelGo.transform, labelText, 11);
            label.color = TextDim;

            // InputField flex
            var field = CreateInputField(row, defaultValue, placeholder, height);
            return (field, label);
        }

        public static TMP_InputField CreateInputField(Transform parent,
            string defaultValue = "", string placeholder = "", float height = 24)
        {
            var go = new GameObject("Input");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth = 1;

            go.AddComponent<Image>().color = InputBg;
            var input = go.AddComponent<TMP_InputField>();

            // Text Area
            var ta = new GameObject("TextArea");
            ta.transform.SetParent(go.transform, false);
            var taRT = ta.AddComponent<RectTransform>();
            taRT.anchorMin = Vector2.zero;
            taRT.anchorMax = Vector2.one;
            taRT.offsetMin = new Vector2(6, 1);
            taRT.offsetMax = new Vector2(-6, -1);
            ta.AddComponent<RectMask2D>();

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(ta.transform, false);
            var tRT = textGo.AddComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
            tRT.offsetMin = Vector2.zero; tRT.offsetMax = Vector2.zero;
            var txt = textGo.AddComponent<TextMeshProUGUI>();
            txt.fontSize = 11; txt.color = TextWhite; txt.enableWordWrapping = false;

            var phGo = new GameObject("Placeholder");
            phGo.transform.SetParent(ta.transform, false);
            var pRT = phGo.AddComponent<RectTransform>();
            pRT.anchorMin = Vector2.zero; pRT.anchorMax = Vector2.one;
            pRT.offsetMin = Vector2.zero; pRT.offsetMax = Vector2.zero;
            var ph = phGo.AddComponent<TextMeshProUGUI>();
            ph.text = string.IsNullOrEmpty(placeholder) ? "..." : placeholder;
            ph.fontSize = 11; ph.color = TextDim; ph.fontStyle = FontStyles.Italic;
            ph.enableWordWrapping = false;

            input.textViewport = taRT;
            input.textComponent = txt;
            input.placeholder = ph;
            input.text = defaultValue;
            input.caretColor = Color.white;
            input.selectionColor = new Color(0.3f, 0.5f, 0.8f, 0.4f);
            return input;
        }

        // ────────────────────────────────────────────────────────
        //  BOUTON
        // ────────────────────────────────────────────────────────
        public static Button CreateButton(Transform parent, string text,
            UnityAction onClick = null, float height = 28, Color? color = null)
        {
            var go = new GameObject($"Btn_{text}");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth = 1;

            go.AddComponent<Image>().color = color ?? BtnNormal;

            var btn = go.AddComponent<Button>();
            var c = btn.colors;
            c.highlightedColor = BtnHover;
            c.pressedColor = BtnPressed;
            c.fadeDuration = 0.05f;
            btn.colors = c;

            if (onClick != null) btn.onClick.AddListener(onClick);

            var lbl = CreateTextDirect(go.transform, text, 11, TextAlignmentOptions.Center);
            lbl.fontStyle = FontStyles.Bold;
            return btn;
        }

        // ────────────────────────────────────────────────────────
        //  TOGGLE
        // ────────────────────────────────────────────────────────
        public static Toggle CreateToggle(Transform parent, string label,
            bool defaultValue = true, UnityAction<bool> onChanged = null, float height = 20)
        {
            var go = new GameObject($"Tgl_{label}");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<LayoutElement>().preferredHeight = height;
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.padding = new RectOffset(4, 0, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Box
            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(go.transform, false);
            bgGo.AddComponent<LayoutElement>().preferredWidth = 16;
            bgGo.AddComponent<LayoutElement>().preferredHeight = 16;
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.sizeDelta = new Vector2(16, 16);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = InputBg;

            var chkGo = new GameObject("Check");
            chkGo.transform.SetParent(bgGo.transform, false);
            var chkRT = chkGo.AddComponent<RectTransform>();
            chkRT.anchorMin = new Vector2(0.2f, 0.2f);
            chkRT.anchorMax = new Vector2(0.8f, 0.8f);
            chkRT.offsetMin = Vector2.zero;
            chkRT.offsetMax = Vector2.zero;
            var chkImg = chkGo.AddComponent<Image>();
            chkImg.color = Success;

            // Label
            var lblGo = new GameObject("Lbl");
            lblGo.transform.SetParent(go.transform, false);
            var lblRT = lblGo.AddComponent<RectTransform>();
            lblRT.sizeDelta = new Vector2(280, height);
            CreateTextDirect(lblGo.transform, label, 11).color = TextWhite;

            var toggle = go.AddComponent<Toggle>();
            toggle.isOn = defaultValue;
            toggle.graphic = chkImg;
            toggle.targetGraphic = bgImg;
            if (onChanged != null) toggle.onValueChanged.AddListener(onChanged);
            return toggle;
        }

        // ────────────────────────────────────────────────────────
        //  SLIDER
        // ────────────────────────────────────────────────────────
        public static (Slider slider, TextMeshProUGUI valueLabel) CreateSliderWithLabel(
            Transform parent, string label, float min, float max, float defaultValue,
            bool wholeNumbers = false, float height = 24)
        {
            var row = CreateHGroup(parent, height, spacing: 4);

            // Label fixe
            var lblGo = new GameObject("Lbl");
            lblGo.transform.SetParent(row, false);
            var lLE = lblGo.AddComponent<LayoutElement>();
            lLE.preferredWidth = 100; lLE.flexibleWidth = 0;
            CreateTextDirect(lblGo.transform, label, 11).color = TextDim;

            // Slider flex
            var sGo = new GameObject("Slider");
            sGo.transform.SetParent(row, false);
            sGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            sGo.AddComponent<Image>().color = InputBg;

            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sGo.transform, false);
            var faRT = fillArea.AddComponent<RectTransform>();
            faRT.anchorMin = new Vector2(0, 0.25f);
            faRT.anchorMax = new Vector2(1, 0.75f);
            faRT.offsetMin = new Vector2(2, 0);
            faRT.offsetMax = new Vector2(-2, 0);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fRT = fill.AddComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
            fRT.offsetMin = Vector2.zero; fRT.offsetMax = Vector2.zero;
            fill.AddComponent<Image>().color = new Color(0.3f, 0.5f, 0.75f);

            var slider = sGo.AddComponent<Slider>();
            slider.minValue = min; slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers; slider.value = defaultValue;
            slider.fillRect = fRT;

            // Valeur fixe
            var vGo = new GameObject("Val");
            vGo.transform.SetParent(row, false);
            var vLE = vGo.AddComponent<LayoutElement>();
            vLE.preferredWidth = 40; vLE.flexibleWidth = 0;
            var vTxt = CreateTextDirect(vGo.transform,
                wholeNumbers ? defaultValue.ToString("F0") : defaultValue.ToString("F2"),
                11, TextAlignmentOptions.Center);

            slider.onValueChanged.AddListener(v =>
                vTxt.text = wholeNumbers ? v.ToString("F0") : v.ToString("F2"));

            return (slider, vTxt);
        }

        // ────────────────────────────────────────────────────────
        //  UTILITAIRES COULEUR
        // ────────────────────────────────────────────────────────
        public static Color GetStatusColor(GenerationStatus s) => s switch
        {
            GenerationStatus.Succes => Success,
            GenerationStatus.SuccesAvecWarnings => Warning,
            GenerationStatus.Echec => Error,
            _ => TextWhite
        };

        public static Color GetSeverityColor(ValidationSeverity s) => s switch
        {
            ValidationSeverity.Erreur => Error,
            ValidationSeverity.Warning => Warning,
            _ => new Color(0.55f, 0.75f, 1f)
        };
    }
}
