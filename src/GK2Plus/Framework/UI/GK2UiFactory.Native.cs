using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GK2Plus.Framework.UI
{
    internal enum GK2UiTooltipTextTone
    {
        Regular,
        Additional
    }

    internal sealed class GK2UiProgressBarView
    {
        public GameObject Root;
        public Slider Slider;
        public Image Fill;
        public TextMeshProUGUI Label;

        public void SetProgress(
            float current,
            float maximum,
            string label = null)
        {
            float normalized =
                maximum > 0f
                    ? Mathf.Clamp01(current / maximum)
                    : 0f;

            if (Slider != null)
            {
                Slider.value =
                    normalized;
            }

            if (Label != null)
            {
                Label.text =
                    label ??
                    $"{current:0}/{Mathf.Max(0f, maximum):0}";
            }
        }
    }

    internal sealed class GK2UiTooltipView
    {
        public GameObject Root;
        public RectTransform Rect;
        public VerticalLayoutGroup Layout;
    }

    /// <summary>
    /// Native controls captured from GK2 v1.008 with UnityExplorer.
    ///
    /// These builders intentionally use explicit native sprite/style names
    /// cached by GK2UiTheme instead of visual approximations or runtime
    /// hierarchy heuristics.
    /// </summary>
    internal static partial class GK2UiFactory
    {
        public static Button CreateNativeRedButton(
            Transform parent,
            string name,
            GK2UiTheme theme,
            string text,
            Vector2 anchoredPosition,
            Vector2 size,
            UnityAction onClick = null)
        {
            GameObject root =
                CreateRect(
                    parent,
                    name,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    anchoredPosition,
                    size);

            GameObject content =
                CreateRect(
                    root.transform,
                    "Content",
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero);

            RectTransform contentRect =
                content.GetComponent<RectTransform>();

            contentRect.offsetMin =
                Vector2.zero;
            contentRect.offsetMax =
                Vector2.zero;

            HorizontalLayoutGroup contentLayout =
                content.AddComponent<HorizontalLayoutGroup>();

            contentLayout.padding =
                new RectOffset();
            contentLayout.spacing =
                0f;
            contentLayout.childAlignment =
                TextAnchor.MiddleCenter;
            contentLayout.childControlWidth =
                true;
            contentLayout.childControlHeight =
                true;
            contentLayout.childForceExpandWidth =
                true;
            contentLayout.childForceExpandHeight =
                true;

            GameObject back =
                CreateImage(
                    content.transform,
                    "Back",
                    theme?.ButtonSprite,
                    Image.Type.Tiled,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    Color.white,
                    true);

            RectTransform backRect =
                back.GetComponent<RectTransform>();

            backRect.offsetMin =
                Vector2.zero;
            backRect.offsetMax =
                Vector2.zero;

            float nativeScale =
                Mathf.Max(
                    0.45f,
                    size.y /
                    GK2UiMetrics.Native.RedButtonSize.y);

            HorizontalLayoutGroup backLayout =
                back.AddComponent<HorizontalLayoutGroup>();

            backLayout.padding =
                new RectOffset(
                    Mathf.RoundToInt(
                        GK2UiMetrics.Native.RedButtonPaddingHorizontal *
                        nativeScale),
                    Mathf.RoundToInt(
                        GK2UiMetrics.Native.RedButtonPaddingHorizontal *
                        nativeScale),
                    Mathf.RoundToInt(
                        GK2UiMetrics.Native.RedButtonPaddingTop *
                        nativeScale),
                    Mathf.RoundToInt(
                        GK2UiMetrics.Native.RedButtonPaddingBottom *
                        nativeScale));
            backLayout.spacing =
                0f;
            backLayout.childAlignment =
                TextAnchor.MiddleCenter;
            backLayout.childControlWidth =
                true;
            backLayout.childControlHeight =
                true;
            backLayout.childForceExpandWidth =
                true;
            backLayout.childForceExpandHeight =
                true;

            TextMeshProUGUI label =
                CreateText(
                    back.transform,
                    "Label",
                    theme?.ButtonTextTemplate,
                    text,
                    GK2UiMetrics.Native.RedButtonFontSize *
                    nativeScale,
                    TextAlignmentOptions.Center,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero);

            // Captured from Settings on UIGamePauseWindow.
            label.color =
                new Color(
                    1f,
                    0.663f,
                    0.333f,
                    1f);

            Button button =
                root.AddComponent<Button>();

            button.targetGraphic =
                back.GetComponent<Image>();
            button.transition =
                Selectable.Transition.SpriteSwap;

            SpriteState sprites =
                button.spriteState;

            sprites.highlightedSprite =
                theme?.ButtonHighlightedSprite;
            sprites.pressedSprite =
                theme?.ButtonPressedSprite;
            sprites.selectedSprite =
                theme?.ButtonHighlightedSprite;
            sprites.disabledSprite =
                theme?.ButtonDisabledSprite;

            button.spriteState =
                sprites;

            ColorBlock colors =
                button.colors;

            colors.normalColor =
                Color.white;
            colors.highlightedColor =
                Color.white;
            colors.pressedColor =
                new Color(
                    0.784f,
                    0.784f,
                    0.784f,
                    1f);
            colors.selectedColor =
                new Color(
                    0.961f,
                    0.961f,
                    0.961f,
                    1f);
            colors.disabledColor =
                new Color(
                    0.784f,
                    0.784f,
                    0.784f,
                    0.502f);

            button.colors =
                colors;

            if (onClick != null)
            {
                button.onClick.AddListener(
                    onClick);
            }

            return button;
        }

        public static Button CreateNativeWindowTab(
            Transform parent,
            string name,
            GK2UiTheme theme,
            string text,
            Vector2 anchoredPosition,
            Vector2 size,
            bool selected,
            UnityAction onClick = null)
        {
            GameObject root =
                CreateRect(
                    parent,
                    name,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    anchoredPosition,
                    size);

            GameObject activeBack =
                CreateImage(
                    root.transform,
                    "ActiveBack",
                    theme?.MainWindowHeaderButtonSprite,
                    Image.Type.Sliced,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    Color.white,
                    false);

            RectTransform activeBackRect =
                activeBack.GetComponent<RectTransform>();
            activeBackRect.offsetMin =
                Vector2.zero;
            activeBackRect.offsetMax =
                Vector2.zero;
            activeBack.SetActive(
                selected);

            Image selection =
                CreateImage(
                    root.transform,
                    "Selection",
                    theme?.MainWindowHeaderButtonOverSprite,
                    Image.Type.Sliced,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    new Color(1f, 1f, 1f, 0f),
                    true)
                .GetComponent<Image>();

            selection.rectTransform.offsetMin =
                Vector2.zero;
            selection.rectTransform.offsetMax =
                Vector2.zero;

            TextMeshProUGUI label;

            if (theme?.MainWindowTabTextTemplate != null)
            {
                GameObject labelObject =
                    Object.Instantiate(
                        theme.MainWindowTabTextTemplate.gameObject,
                        root.transform,
                        false);

                labelObject.name =
                    "Label";
                labelObject.SetActive(
                    true);

                foreach (Component component in
                         labelObject.GetComponents<Component>())
                {
                    if (component == null)
                    {
                        continue;
                    }

                    string typeName =
                        component.GetType().Name;

                    if (typeName == "LocalizedLabel" ||
                        typeName == "LocalizedVerticalOffset" ||
                        typeName == "LanguageRtlLabelState")
                    {
                        Object.Destroy(
                            component);
                    }
                }

                label =
                    labelObject.GetComponent<TextMeshProUGUI>();

                RectTransform labelRect =
                    label.rectTransform;

                labelRect.anchorMin =
                    Vector2.zero;
                labelRect.anchorMax =
                    Vector2.one;
                labelRect.pivot =
                    new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition =
                    Vector2.zero;
                labelRect.offsetMin =
                    new Vector2(13f, 0f);
                labelRect.offsetMax =
                    new Vector2(-13f, 0f);
                labelRect.localScale =
                    Vector3.one;
                labelRect.localRotation =
                    Quaternion.identity;

                label.text =
                    text ?? string.Empty;
                label.fontSize =
                    16f;
                label.fontSizeMin =
                    16f;
                label.fontSizeMax =
                    16f;
                label.enableAutoSizing =
                    false;
                label.enableWordWrapping =
                    false;
                label.overflowMode =
                    TextOverflowModes.Overflow;
                label.alignment =
                    TextAlignmentOptions.Center;
                label.color =
                    Color.white;
                label.raycastTarget =
                    false;
            }
            else
            {
                label =
                    CreateText(
                        root.transform,
                        "Label",
                        theme?.ButtonTextTemplate ??
                        theme?.BodyTextTemplate,
                        text,
                        16f,
                        TextAlignmentOptions.Center,
                        Vector2.zero,
                        Vector2.one,
                        new Vector2(0.5f, 0.5f),
                        Vector2.zero,
                        Vector2.zero);

                label.rectTransform.offsetMin =
                    new Vector2(13f, 0f);
                label.rectTransform.offsetMax =
                    new Vector2(-13f, 0f);
                label.enableWordWrapping =
                    false;
                label.overflowMode =
                    TextOverflowModes.Overflow;
                label.color =
                    Color.white;
            }

            Button button =
                root.AddComponent<Button>();

            button.targetGraphic =
                label;
            button.transition =
                Selectable.Transition.ColorTint;

            ColorBlock colors =
                button.colors;

            colors.normalColor =
                Color.white;
            colors.highlightedColor =
                new Color(
                    1f,
                    0.678f,
                    0.227f,
                    1f);
            colors.pressedColor =
                new Color(
                    1f,
                    0.678f,
                    0.227f,
                    1f);
            colors.selectedColor =
                Color.white;
            colors.disabledColor =
                Color.white;
            colors.fadeDuration =
                0.05f;

            button.colors =
                colors;

            if (onClick != null)
            {
                button.onClick.AddListener(
                    onClick);
            }

            return button;
        }

        public static float GetNativeWindowTabPreferredWidth(
            GK2UiTheme theme,
            string text,
            float minimum = 42f)
        {
            TextMeshProUGUI template =
                theme?.MainWindowTabTextTemplate ??
                theme?.ButtonTextTemplate ??
                theme?.BodyTextTemplate;

            if (template == null)
            {
                return Mathf.Max(
                    minimum,
                    26f + ((text?.Length ?? 0) * 8f));
            }

            Vector2 preferred =
                template.GetPreferredValues(
                    text ?? string.Empty);

            // Native CharPageTabButton content has 13 units of left/right
            // padding around its 16pt small_font_bold label.
            return Mathf.Max(
                minimum,
                preferred.x + 26f);
        }

        public static void SetNativeWindowTabSelected(
            GameObject tab,
            bool selected)
        {
            if (tab == null)
            {
                return;
            }

            Transform activeBack =
                tab.transform.Find(
                    "ActiveBack");

            if (activeBack != null)
            {
                activeBack.gameObject.SetActive(
                    selected);
            }

            TextMeshProUGUI label =
                tab.GetComponentInChildren<TextMeshProUGUI>(
                    true);

            if (label != null)
            {
                label.color =
                    Color.white;
            }
        }

        public static GK2UiProgressBarView CreateNativeProgressBar(
            Transform parent,
            string name,
            GK2UiTheme theme,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            float current,
            float maximum,
            string label = null)
        {
            GameObject root =
                CreateImage(
                    parent,
                    name,
                    theme?.ProgressFrameSprite,
                    Image.Type.Sliced,
                    anchorMin,
                    anchorMax,
                    pivot,
                    anchoredPosition,
                    size,
                    Color.white);

            GameObject sliderObject =
                CreateRect(
                    root.transform,
                    "Slider",
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 0.5f),
                    Vector2.zero);

            RectTransform sliderRect =
                sliderObject.GetComponent<RectTransform>();

            sliderRect.offsetMin =
                new Vector2(0f, 0.5f);
            sliderRect.offsetMax =
                new Vector2(0f, 0.5f);

            sliderObject.AddComponent<CanvasRenderer>();

            Slider slider =
                sliderObject.AddComponent<Slider>();

            GameObject fillerContent =
                CreateRect(
                    sliderObject.transform,
                    "FillerContent",
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(-4f, -4f));

            RectTransform fillerContentRect =
                fillerContent.GetComponent<RectTransform>();

            fillerContentRect.offsetMin =
                new Vector2(
                    GK2UiMetrics.Native.ProgressHorizontalInset,
                    GK2UiMetrics.Native.ProgressBottomInset);
            fillerContentRect.offsetMax =
                new Vector2(
                    -GK2UiMetrics.Native.ProgressHorizontalInset,
                    -GK2UiMetrics.Native.ProgressTopInset);

            Image fill =
                CreateImage(
                    fillerContent.transform,
                    "BarFiller",
                    theme?.ProgressFillSprite,
                    Image.Type.Sliced,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    Color.white)
                .GetComponent<Image>();

            RectTransform fillRect =
                fill.rectTransform;

            fillRect.offsetMin =
                Vector2.zero;
            fillRect.offsetMax =
                Vector2.zero;

            slider.minValue =
                0f;
            slider.maxValue =
                1f;
            slider.wholeNumbers =
                false;
            slider.direction =
                Slider.Direction.LeftToRight;
            slider.fillRect =
                fillRect;
            slider.handleRect =
                null;
            slider.targetGraphic =
                null;
            slider.interactable =
                false;
            slider.transition =
                Selectable.Transition.None;

            TextMeshProUGUI progressLabel =
                CreateText(
                    root.transform,
                    "ProgressLabel",
                    theme?.ProgressTextTemplate ??
                    theme?.CountTextTemplate,
                    string.Empty,
                    GK2UiMetrics.Native.ProgressLabelFontSize,
                    TextAlignmentOptions.Center,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -1f),
                    new Vector2(-8f, -10f));

            progressLabel.rectTransform.offsetMin =
                new Vector2(4f, 4f);
            progressLabel.rectTransform.offsetMax =
                new Vector2(-4f, -6f);
            progressLabel.color =
                new Color(
                    0.914f,
                    1f,
                    0.643f,
                    1f);

            GK2UiProgressBarView view =
                new GK2UiProgressBarView
                {
                    Root = root,
                    Slider = slider,
                    Fill = fill,
                    Label = progressLabel
                };

            view.SetProgress(
                current,
                maximum,
                label);

            return view;
        }

        public static GK2UiTooltipView CreateNativeTooltip(
            Transform parent,
            string name,
            GK2UiTheme theme,
            Vector2 anchoredPosition)
        {
            GameObject root =
                CreateImage(
                    parent,
                    name,
                    theme?.TooltipFrameSprite,
                    Image.Type.Sliced,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    anchoredPosition,
                    new Vector2(
                        GK2UiMetrics.Native.TooltipPreferredWidth,
                        1f),
                    Color.white,
                    true);

            Canvas canvas =
                root.AddComponent<Canvas>();

            canvas.overrideSorting =
                true;
            canvas.sortingOrder =
                700;

            CanvasGroup canvasGroup =
                root.AddComponent<CanvasGroup>();

            canvasGroup.alpha =
                1f;
            canvasGroup.interactable =
                false;
            canvasGroup.blocksRaycasts =
                true;
            canvasGroup.ignoreParentGroups =
                false;

            VerticalLayoutGroup layout =
                root.AddComponent<VerticalLayoutGroup>();

            layout.padding =
                new RectOffset(
                    GK2UiMetrics.Native.TooltipPadding,
                    GK2UiMetrics.Native.TooltipPadding,
                    GK2UiMetrics.Native.TooltipPadding,
                    GK2UiMetrics.Native.TooltipPadding);
            layout.spacing =
                GK2UiMetrics.Native.TooltipSpacing;
            layout.childAlignment =
                TextAnchor.MiddleCenter;
            layout.childControlWidth =
                true;
            layout.childControlHeight =
                true;
            layout.childForceExpandWidth =
                false;
            layout.childForceExpandHeight =
                true;

            ContentSizeFitter fitter =
                root.AddComponent<ContentSizeFitter>();

            fitter.horizontalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement layoutElement =
                root.AddComponent<LayoutElement>();

            layoutElement.minWidth =
                GK2UiMetrics.Native.TooltipMinWidth;
            layoutElement.preferredWidth =
                GK2UiMetrics.Native.TooltipPreferredWidth;

            return new GK2UiTooltipView
            {
                Root = root,
                Rect = root.GetComponent<RectTransform>(),
                Layout = layout
            };
        }

        public static TextMeshProUGUI AddNativeTooltipText(
            GK2UiTooltipView tooltip,
            GK2UiTheme theme,
            string text,
            GK2UiTooltipTextTone tone =
                GK2UiTooltipTextTone.Regular)
        {
            if (tooltip?.Root == null)
            {
                return null;
            }

            bool additional =
                tone ==
                GK2UiTooltipTextTone.Additional;

            TextMeshProUGUI template =
                additional
                    ? theme?.TooltipAdditionalTextTemplate
                    : theme?.TooltipRegularTextTemplate;

            TextMeshProUGUI label =
                CreateText(
                    tooltip.Root.transform,
                    "TooltipText",
                    template ??
                    theme?.BodyTextTemplate,
                    text,
                    GK2UiMetrics.Native.TooltipTextSize,
                    TextAlignmentOptions.Center,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(
                        GK2UiMetrics.Native.TooltipPreferredWidth -
                        (GK2UiMetrics.Native.TooltipPadding * 2f),
                        1f));

            label.color =
                additional
                    ? new Color(
                        0.580f,
                        0.431f,
                        0.267f,
                        1f)
                    : new Color(
                        0.471f,
                        0.310f,
                        0.149f,
                        1f);

            return label;
        }

        public static GameObject AddNativeTooltipSeparator(
            GK2UiTooltipView tooltip,
            GK2UiTheme theme)
        {
            if (tooltip?.Root == null)
            {
                return null;
            }

            GameObject container =
                CreateRect(
                    tooltip.Root.transform,
                    "TooltipSeparator",
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(
                        GK2UiMetrics.Native.TooltipSeparatorWidth,
                        GK2UiMetrics.Native.TooltipSeparatorHeight));

            VerticalLayoutGroup layout =
                container.AddComponent<VerticalLayoutGroup>();

            layout.padding =
                new RectOffset();
            layout.spacing =
                0f;
            layout.childAlignment =
                TextAnchor.MiddleCenter;
            layout.childControlWidth =
                false;
            layout.childControlHeight =
                false;
            layout.childForceExpandWidth =
                true;
            layout.childForceExpandHeight =
                true;

            ContentSizeFitter fitter =
                container.AddComponent<ContentSizeFitter>();

            fitter.horizontalFit =
                ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            CreateImage(
                container.transform,
                "Icon",
                theme?.TooltipSeparatorSprite,
                Image.Type.Simple,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(
                    GK2UiMetrics.Native.TooltipSeparatorWidth,
                    GK2UiMetrics.Native.TooltipSeparatorHeight),
                Color.white);

            return container;
        }
    }
}
