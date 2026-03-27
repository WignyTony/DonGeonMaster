using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DonGeonMaster.MapGeneration
{
    public static class DebugUIBuilder
    {
        static readonly Color PanelDark = new(0.12f, 0.12f, 0.14f, 0.95f);
        static readonly Color PanelMid = new(0.18f, 0.18f, 0.22f, 0.95f);
        static readonly Color HeaderColor = new(0.25f, 0.25f, 0.35f);
        static readonly Color ButtonColor = new(0.3f, 0.3f, 0.45f);
        static readonly Color ButtonHover = new(0.4f, 0.4f, 0.55f);
        static readonly Color SuccessColor = new(0.2f, 0.8f, 0.3f);
        static readonly Color WarningColor = new(1f, 0.8f, 0.2f);
        static readonly Color ErrorColor = new(1f, 0.3f, 0.3f);
        static readonly Color InputBg = new(0.1f, 0.1f, 0.12f);

        public static Canvas CreateCanvas(string name = "DebugCanvas")
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform CreatePanel(Transform parent, string name,
            float anchorMinX, float anchorMinY, float anchorMaxX, float anchorMaxY,
            Color? color = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorMinX, anchorMinY);
            rt.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = color ?? PanelDark;
            return rt;
        }

        public static ScrollRect CreateScrollView(Transform parent, string name = "ScrollView")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var scroll = go.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            // Viewport
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(go.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero;
            vpRT.offsetMax = Vector2.zero;
            var vpImg = viewport.AddComponent<Image>();
            vpImg.color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = vpRT;

            // Content
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var cRT = content.AddComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1);
            cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);
            cRT.offsetMin = Vector2.zero;
            cRT.offsetMax = Vector2.zero;

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 4;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = cRT;

            // Scrollbar
            var scrollbar = CreateScrollbar(go.transform);
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            return scroll;
        }

        static Scrollbar CreateScrollbar(Transform parent)
        {
            var go = new GameObject("Scrollbar");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-8, 0);
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

            var sb = go.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;

            var handle = new GameObject("Handle");
            handle.transform.SetParent(go.transform, false);
            var hRT = handle.AddComponent<RectTransform>();
            hRT.anchorMin = Vector2.zero;
            hRT.anchorMax = Vector2.one;
            var hImg = handle.AddComponent<Image>();
            hImg.color = new Color(0.4f, 0.4f, 0.5f, 0.8f);
            sb.handleRect = hRT;
            sb.targetGraphic = hImg;

            return sb;
        }

        public static (RectTransform header, RectTransform content) CreateCollapsibleSection(
            Transform parent, string title)
        {
            var section = new GameObject($"Section_{title}");
            section.transform.SetParent(parent, false);
            var sRT = section.AddComponent<RectTransform>();
            var sVLG = section.AddComponent<VerticalLayoutGroup>();
            sVLG.spacing = 2;
            sVLG.childControlWidth = true;
            sVLG.childControlHeight = false;
            sVLG.childForceExpandWidth = true;
            sVLG.childForceExpandHeight = false;
            section.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Header button
            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(section.transform, false);
            var headerRT = headerGo.AddComponent<RectTransform>();
            headerRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 28);
            var headerImg = headerGo.AddComponent<Image>();
            headerImg.color = HeaderColor;
            var headerBtn = headerGo.AddComponent<Button>();
            var headerLE = headerGo.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 28;

            var headerText = CreateTextDirect(headerGo.transform, $"▼ {title}", 13, TextAlignmentOptions.MidlineLeft);
            headerText.fontStyle = FontStyles.Bold;
            headerText.margin = new Vector4(8, 0, 0, 0);

            // Content container
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(section.transform, false);
            var contentRT = contentGo.AddComponent<RectTransform>();
            var contentVLG = contentGo.AddComponent<VerticalLayoutGroup>();
            contentVLG.padding = new RectOffset(4, 4, 2, 4);
            contentVLG.spacing = 3;
            contentVLG.childControlWidth = true;
            contentVLG.childControlHeight = false;
            contentVLG.childForceExpandWidth = true;
            contentVLG.childForceExpandHeight = false;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var contentBg = contentGo.AddComponent<Image>();
            contentBg.color = PanelMid;

            // Toggle collapse
            headerBtn.onClick.AddListener(() =>
            {
                bool active = !contentGo.activeSelf;
                contentGo.SetActive(active);
                headerText.text = (active ? "▼ " : "► ") + title;
            });

            return (headerRT, contentRT);
        }

        public static TextMeshProUGUI CreateLabel(Transform parent, string text,
            int fontSize = 12, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft,
            float height = 20)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<LayoutElement>().preferredHeight = height;
            return CreateTextDirect(go.transform, text, fontSize, align);
        }

        public static TextMeshProUGUI CreateTextDirect(Transform parent, string text,
            int fontSize = 12, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var go = new GameObject("Text");
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
            tmp.color = Color.white;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        public static (TMP_InputField field, TextMeshProUGUI label) CreateInputFieldWithLabel(
            Transform parent, string labelText, string defaultValue = "",
            string placeholder = "", float height = 26)
        {
            var row = CreateHorizontalGroup(parent, height);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row, false);
            var labelLE = labelGo.AddComponent<LayoutElement>();
            labelLE.preferredWidth = 110;
            labelLE.flexibleWidth = 0;
            var label = CreateTextDirect(labelGo.transform, labelText, 11);

            var field = CreateInputField(row, defaultValue, placeholder, height);
            return (field, label);
        }

        public static TMP_InputField CreateInputField(Transform parent,
            string defaultValue = "", string placeholder = "", float height = 26)
        {
            var go = new GameObject("InputField");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth = 1;

            var img = go.AddComponent<Image>();
            img.color = InputBg;

            var input = go.AddComponent<TMP_InputField>();

            var textArea = new GameObject("TextArea");
            textArea.transform.SetParent(go.transform, false);
            var taRT = textArea.AddComponent<RectTransform>();
            taRT.anchorMin = Vector2.zero;
            taRT.anchorMax = Vector2.one;
            taRT.offsetMin = new Vector2(6, 2);
            taRT.offsetMax = new Vector2(-6, -2);
            textArea.AddComponent<RectMask2D>();

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(textArea.transform, false);
            var textRT = textGo.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.fontSize = 12;
            text.color = Color.white;
            text.enableWordWrapping = false;

            var phGo = new GameObject("Placeholder");
            phGo.transform.SetParent(textArea.transform, false);
            var phRT = phGo.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.offsetMin = Vector2.zero;
            phRT.offsetMax = Vector2.zero;
            var ph = phGo.AddComponent<TextMeshProUGUI>();
            ph.text = placeholder;
            ph.fontSize = 12;
            ph.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            ph.fontStyle = FontStyles.Italic;
            ph.enableWordWrapping = false;

            input.textViewport = taRT;
            input.textComponent = text;
            input.placeholder = ph;
            input.text = defaultValue;
            input.caretColor = Color.white;
            input.selectionColor = new Color(0.3f, 0.5f, 0.8f, 0.5f);

            return input;
        }

        public static Button CreateButton(Transform parent, string text,
            UnityAction onClick = null, float height = 30, Color? color = null)
        {
            var go = new GameObject($"Btn_{text}");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<LayoutElement>().preferredHeight = height;

            var img = go.AddComponent<Image>();
            img.color = color ?? ButtonColor;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = ButtonHover;
            colors.pressedColor = new Color(0.2f, 0.2f, 0.35f);
            btn.colors = colors;

            if (onClick != null) btn.onClick.AddListener(onClick);

            var label = CreateTextDirect(go.transform, text, 12, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;

            return btn;
        }

        public static Toggle CreateToggle(Transform parent, string label,
            bool defaultValue = true, UnityAction<bool> onChanged = null, float height = 22)
        {
            var go = new GameObject($"Toggle_{label}");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<LayoutElement>().preferredHeight = height;
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.padding = new RectOffset(4, 0, 0, 0);

            // Checkbox background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(go.transform, false);
            var bgLE = bgGo.AddComponent<LayoutElement>();
            bgLE.preferredWidth = 18;
            bgLE.preferredHeight = 18;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = InputBg;

            // Checkmark
            var checkGo = new GameObject("Checkmark");
            checkGo.transform.SetParent(bgGo.transform, false);
            var checkRT = checkGo.AddComponent<RectTransform>();
            checkRT.anchorMin = new Vector2(0.15f, 0.15f);
            checkRT.anchorMax = new Vector2(0.85f, 0.85f);
            checkRT.offsetMin = Vector2.zero;
            checkRT.offsetMax = Vector2.zero;
            var checkImg = checkGo.AddComponent<Image>();
            checkImg.color = SuccessColor;

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            CreateTextDirect(labelGo.transform, label, 11);

            var toggle = go.AddComponent<Toggle>();
            toggle.isOn = defaultValue;
            toggle.graphic = checkImg;
            toggle.targetGraphic = bgImg;
            if (onChanged != null) toggle.onValueChanged.AddListener(onChanged);

            return toggle;
        }

        public static (Slider slider, TextMeshProUGUI valueLabel) CreateSliderWithLabel(
            Transform parent, string label, float min, float max, float defaultValue,
            bool wholeNumbers = false, float height = 26)
        {
            var row = new GameObject($"Slider_{label}");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            row.AddComponent<LayoutElement>().preferredHeight = height;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row.transform, false);
            var labelLE = labelGo.AddComponent<LayoutElement>();
            labelLE.preferredWidth = 110;
            CreateTextDirect(labelGo.transform, label, 11);

            // Slider
            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(row.transform, false);
            sliderGo.AddComponent<LayoutElement>().flexibleWidth = 1;

            var bgImg = sliderGo.AddComponent<Image>();
            bgImg.color = InputBg;

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGo.transform, false);
            var faRT = fillArea.AddComponent<RectTransform>();
            faRT.anchorMin = new Vector2(0, 0.3f);
            faRT.anchorMax = new Vector2(1, 0.7f);
            faRT.offsetMin = new Vector2(4, 0);
            faRT.offsetMax = new Vector2(-4, 0);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fRT = fill.AddComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero;
            fRT.anchorMax = Vector2.one;
            fRT.offsetMin = Vector2.zero;
            fRT.offsetMax = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.3f, 0.5f, 0.8f);

            var slider = sliderGo.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;
            slider.value = defaultValue;
            slider.fillRect = fRT;

            // Value display
            var valGo = new GameObject("Value");
            valGo.transform.SetParent(row.transform, false);
            var valLE = valGo.AddComponent<LayoutElement>();
            valLE.preferredWidth = 45;
            var valText = CreateTextDirect(valGo.transform,
                wholeNumbers ? defaultValue.ToString("F0") : defaultValue.ToString("F2"), 11,
                TextAlignmentOptions.Center);

            slider.onValueChanged.AddListener(v =>
            {
                valText.text = wholeNumbers ? v.ToString("F0") : v.ToString("F2");
            });

            return (slider, valText);
        }

        public static RectTransform CreateHorizontalGroup(Transform parent, float height = 30)
        {
            var go = new GameObject("HGroup");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            go.AddComponent<LayoutElement>().preferredHeight = height;
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            return rt;
        }

        public static TextMeshProUGUI CreateStatusLabel(Transform parent, string text,
            GenerationStatus status)
        {
            var label = CreateLabel(parent, text, 13);
            label.color = status switch
            {
                GenerationStatus.Succes => SuccessColor,
                GenerationStatus.SuccesAvecWarnings => WarningColor,
                GenerationStatus.Echec => ErrorColor,
                _ => Color.white
            };
            return label;
        }

        public static Color GetStatusColor(GenerationStatus status)
        {
            return status switch
            {
                GenerationStatus.Succes => SuccessColor,
                GenerationStatus.SuccesAvecWarnings => WarningColor,
                GenerationStatus.Echec => ErrorColor,
                _ => Color.white
            };
        }

        public static Color GetSeverityColor(ValidationSeverity severity)
        {
            return severity switch
            {
                ValidationSeverity.Erreur => ErrorColor,
                ValidationSeverity.Warning => WarningColor,
                _ => new Color(0.6f, 0.8f, 1f)
            };
        }
    }
}
