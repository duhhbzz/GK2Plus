using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2Plus.Framework.UI
{
    internal sealed class GK2UiSectionPanelView
    {
        public GameObject Root;
        public RectTransform Rect;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Body;
    }

    /// <summary>
    /// Reusable titled panel for HUD sections and compact in-game widgets.
    /// </summary>
    internal static class GK2UiSectionPanelBuilder
    {
        public static GK2UiSectionPanelView Create(
            Transform parent,
            GK2UiTheme theme,
            string name,
            string titleText,
            float width,
            float titleHeight,
            float titleFontSize,
            float bodyFontSize,
            float horizontalPadding,
            float topPadding,
            float bottomPadding)
        {
            GameObject root =
                GK2UiFactory.CreateImage(
                    parent,
                    name,
                    null,
                    Image.Type.Simple,
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    Vector2.zero,
                    new Vector2(width, titleHeight + 40f),
                    theme?.HudBackground ??
                    new Color(0f, 0f, 0f, 0.68f));

            RectTransform rootRect =
                root.GetComponent<RectTransform>();

            GameObject titleBar =
                GK2UiFactory.CreateImage(
                    root.transform,
                    "TitleBar",
                    null,
                    Image.Type.Simple,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f),
                    Vector2.zero,
                    new Vector2(0f, titleHeight),
                    theme?.HudTitleBackground ??
                    new Color(0.10f, 0.08f, 0.06f, 0.94f));

            RectTransform titleBarRect =
                titleBar.GetComponent<RectTransform>();

            titleBarRect.offsetMin =
                new Vector2(
                    0f,
                    -titleHeight);
            titleBarRect.offsetMax =
                Vector2.zero;

            TextMeshProUGUI title =
                GK2UiFactory.CreateText(
                    titleBar.transform,
                    "Title",
                    theme?.TitleTextTemplate,
                    titleText,
                    titleFontSize,
                    TextAlignmentOptions.Center,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero);

            title.rectTransform.offsetMin =
                Vector2.zero;
            title.rectTransform.offsetMax =
                Vector2.zero;
            title.color =
                theme?.AccentText ??
                new Color(1f, 0.82f, 0.45f, 1f);

            TextMeshProUGUI body =
                GK2UiFactory.CreateText(
                    root.transform,
                    "Body",
                    theme?.BodyTextTemplate,
                    string.Empty,
                    bodyFontSize,
                    TextAlignmentOptions.TopLeft,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero);

            body.rectTransform.offsetMin =
                new Vector2(
                    horizontalPadding,
                    bottomPadding);
            body.rectTransform.offsetMax =
                new Vector2(
                    -horizontalPadding,
                    -(titleHeight +
                      topPadding));

            body.textWrappingMode =
                TextWrappingModes.Normal;
            body.richText =
                true;
            body.overflowMode =
                TextOverflowModes.Truncate;

            return new GK2UiSectionPanelView
            {
                Root = root,
                Rect = rootRect,
                Title = title,
                Body = body
            };
        }
    }
}
