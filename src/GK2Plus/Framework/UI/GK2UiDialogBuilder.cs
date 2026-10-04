using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GK2Plus.Framework.UI
{
    internal sealed class GK2UiDialogView
    {
        public GameObject Root;
        public GameObject Window;
        public RectTransform WindowRect;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Body;
        public GameObject ButtonsContent;
        public Button ConfirmButton;
        public Button CancelButton;
    }

    /// <summary>
    /// Reusable confirmation/information dialog shell based on
    /// UIDialogWindow / GenericWIndowLayout captured from GK2 v1.008.
    /// </summary>
    internal static class GK2UiDialogBuilder
    {
        public static GK2UiDialogView Create(
            Transform uiRoot,
            string rootName,
            GK2UiTheme theme,
            string title,
            string body,
            string confirmText,
            UnityAction onConfirm,
            string cancelText = null,
            UnityAction onCancel = null,
            float width = 296f,
            float minimumHeight = 174f)
        {
            Transform old =
                uiRoot?.Find(rootName);

            if (old != null)
            {
                Object.Destroy(
                    old.gameObject);
            }

            GameObject root =
                GK2UiFactory.CreateRect(
                    uiRoot,
                    rootName,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero);

            RectTransform rootRect =
                root.GetComponent<RectTransform>();

            rootRect.offsetMin =
                Vector2.zero;
            rootRect.offsetMax =
                Vector2.zero;

            Canvas canvas =
                root.AddComponent<Canvas>();

            canvas.overrideSorting =
                true;
            canvas.sortingOrder =
                GK2UiMetrics.Native.DialogSortingOrder;

            root.AddComponent<GraphicRaycaster>();

            GameObject shadow =
                GK2UiFactory.CreateImage(
                    root.transform,
                    "BgShadow",
                    null,
                    Image.Type.Simple,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    new Color(
                        0f,
                        0f,
                        0f,
                        GK2UiMetrics.Native.DialogShadowAlpha),
                    true);

            RectTransform shadowRect =
                shadow.GetComponent<RectTransform>();

            shadowRect.offsetMin =
                Vector2.zero;
            shadowRect.offsetMax =
                Vector2.zero;

            GameObject window =
                GK2UiFactory.CreateRect(
                    root.transform,
                    "GenericWIndowLayout",
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 24f),
                    new Vector2(width, minimumHeight));

            VerticalLayoutGroup windowLayout =
                window.AddComponent<VerticalLayoutGroup>();

            windowLayout.padding =
                new RectOffset(
                    GK2UiMetrics.Native.DialogPaddingHorizontal,
                    GK2UiMetrics.Native.DialogPaddingHorizontal,
                    GK2UiMetrics.Native.DialogPaddingTop,
                    GK2UiMetrics.Native.DialogPaddingBottom);
            windowLayout.spacing =
                0f;
            windowLayout.childAlignment =
                TextAnchor.MiddleCenter;
            windowLayout.childControlWidth =
                false;
            windowLayout.childControlHeight =
                false;
            windowLayout.childForceExpandWidth =
                true;
            windowLayout.childForceExpandHeight =
                true;

            ContentSizeFitter windowFitter =
                window.AddComponent<ContentSizeFitter>();

            windowFitter.horizontalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            windowFitter.verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement windowElement =
                window.AddComponent<LayoutElement>();

            windowElement.minWidth =
                GK2UiMetrics.Native.DialogMinimumWidth;
            windowElement.minHeight =
                GK2UiMetrics.Native.DialogMinimumHeight;
            windowElement.preferredWidth =
                width;

            GameObject back =
                GK2UiFactory.CreateImage(
                    window.transform,
                    "Back",
                    theme?.WindowBackgroundSprite,
                    Image.Type.Simple,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    Color.white);

            RectTransform backRect =
                back.GetComponent<RectTransform>();

            float backInset =
                GK2UiMetrics.Native.WindowBackInset;

            backRect.offsetMin =
                new Vector2(backInset, backInset);
            backRect.offsetMax =
                new Vector2(-backInset, -backInset);

            IgnoreLayout(back);

            GameObject frame =
                GK2UiFactory.CreateImage(
                    window.transform,
                    "Frame",
                    theme?.WindowFrameSprite,
                    Image.Type.Sliced,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    Color.white,
                    true);

            RectTransform frameRect =
                frame.GetComponent<RectTransform>();

            frameRect.offsetMin =
                Vector2.zero;
            frameRect.offsetMax =
                Vector2.zero;

            IgnoreLayout(frame);

            GameObject header =
                GK2UiFactory.CreateRect(
                    window.transform,
                    "HeaderGroup",
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -11f),
                    new Vector2(-22f, 26f));

            IgnoreLayout(header);

            GameObject headerBack =
                GK2UiFactory.CreateImage(
                    header.transform,
                    "Background",
                    theme?.MainWindowHeaderSprite,
                    Image.Type.Sliced,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    Color.white);

            RectTransform headerBackRect =
                headerBack.GetComponent<RectTransform>();

            headerBackRect.offsetMin =
                Vector2.zero;
            headerBackRect.offsetMax =
                Vector2.zero;

            TextMeshProUGUI titleLabel =
                GK2UiFactory.CreateText(
                    header.transform,
                    "Title",
                    theme?.TitleTextTemplate,
                    title,
                    16f,
                    TextAlignmentOptions.Center,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero);

            titleLabel.rectTransform.offsetMin =
                Vector2.zero;
            titleLabel.rectTransform.offsetMax =
                Vector2.zero;

            GameObject content =
                GK2UiFactory.CreateRect(
                    window.transform,
                    "Content",
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(
                        Mathf.Max(
                            GK2UiMetrics.Native.DialogMinimumWidth -
                            (GK2UiMetrics.Native.DialogPaddingHorizontal * 2f),
                            width -
                            (GK2UiMetrics.Native.DialogPaddingHorizontal * 2f)),
                        1f));

            VerticalLayoutGroup contentLayout =
                content.AddComponent<VerticalLayoutGroup>();

            contentLayout.padding =
                new RectOffset();
            contentLayout.spacing =
                GK2UiMetrics.Native.DialogContentSpacing;
            contentLayout.childAlignment =
                TextAnchor.MiddleCenter;
            contentLayout.childControlWidth =
                false;
            contentLayout.childControlHeight =
                false;
            contentLayout.childForceExpandWidth =
                true;
            contentLayout.childForceExpandHeight =
                true;

            ContentSizeFitter contentFitter =
                content.AddComponent<ContentSizeFitter>();

            contentFitter.horizontalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI bodyLabel =
                GK2UiFactory.CreateText(
                    content.transform,
                    "Info",
                    theme?.DialogBodyTextTemplate ??
                    theme?.BodyTextTemplate,
                    body,
                    16f,
                    TextAlignmentOptions.Center,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0.5f, 1f),
                    Vector2.zero,
                    new Vector2(
                        width -
                        (GK2UiMetrics.Native.DialogPaddingHorizontal * 2f),
                        1f));

            bodyLabel.color =
                new Color(
                    0.588f,
                    0.553f,
                    0.533f,
                    1f);

            ContentSizeFitter bodyFitter =
                bodyLabel.gameObject.AddComponent<ContentSizeFitter>();

            bodyFitter.horizontalFit =
                ContentSizeFitter.FitMode.Unconstrained;
            bodyFitter.verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            GameObject buttonsContent =
                GK2UiFactory.CreateRect(
                    content.transform,
                    "ButtonsContent",
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(1f, 26f));

            HorizontalLayoutGroup buttonsLayout =
                buttonsContent.AddComponent<HorizontalLayoutGroup>();

            buttonsLayout.padding =
                new RectOffset();
            buttonsLayout.spacing =
                GK2UiMetrics.Native.DialogContentSpacing;
            buttonsLayout.childAlignment =
                TextAnchor.MiddleCenter;
            buttonsLayout.childControlWidth =
                false;
            buttonsLayout.childControlHeight =
                false;
            buttonsLayout.childForceExpandWidth =
                true;
            buttonsLayout.childForceExpandHeight =
                true;

            ContentSizeFitter buttonsFitter =
                buttonsContent.AddComponent<ContentSizeFitter>();

            buttonsFitter.horizontalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            buttonsFitter.verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            Button confirmButton =
                CreateDialogButton(
                    buttonsContent.transform,
                    "Confirm",
                    theme,
                    confirmText,
                    onConfirm);

            Button cancelButton =
                null;

            if (!string.IsNullOrWhiteSpace(cancelText))
            {
                cancelButton =
                    CreateDialogButton(
                        buttonsContent.transform,
                        "Cancel",
                        theme,
                        cancelText,
                        onCancel);
            }

            return new GK2UiDialogView
            {
                Root = root,
                Window = window,
                WindowRect = window.GetComponent<RectTransform>(),
                Title = titleLabel,
                Body = bodyLabel,
                ButtonsContent = buttonsContent,
                ConfirmButton = confirmButton,
                CancelButton = cancelButton
            };
        }

        private static Button CreateDialogButton(
            Transform parent,
            string name,
            GK2UiTheme theme,
            string text,
            UnityAction onClick)
        {
            Button button =
                GK2UiFactory.CreateNativeRedButton(
                    parent,
                    name,
                    theme,
                    text,
                    Vector2.zero,
                    new Vector2(84f, 26f),
                    onClick);

            LayoutElement element =
                button.gameObject.AddComponent<LayoutElement>();

            element.preferredWidth =
                84f;
            element.preferredHeight =
                26f;
            element.minWidth =
                84f;
            element.minHeight =
                26f;

            return button;
        }

        private static void IgnoreLayout(
            GameObject target)
        {
            LayoutElement element =
                target.GetComponent<LayoutElement>() ??
                target.AddComponent<LayoutElement>();

            element.ignoreLayout =
                true;
        }
    }
}
