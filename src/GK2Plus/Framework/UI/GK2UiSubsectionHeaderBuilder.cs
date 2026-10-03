using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2Plus.Framework.UI
{
    internal sealed class GK2UiSubsectionHeaderView
    {
        public GameObject Root;
        public RectTransform Rect;
        public TextMeshProUGUI Title;
    }

    /// <summary>
    /// Compact in-page section divider inspired by GK2's Received/section
    /// separators. It deliberately avoids creating another framed panel.
    /// </summary>
    internal static class GK2UiSubsectionHeaderBuilder
    {
        public static GK2UiSubsectionHeaderView Create(
            Transform parent,
            GK2UiTheme theme,
            string name,
            string title,
            float height)
        {
            GameObject root =
                GK2UiFactory.CreateRect(
                    parent,
                    name,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f),
                    Vector2.zero,
                    new Vector2(0f, height));

            const float titleWidth = 92f;
            const float lineInset = 10f;
            const float centerGap = 8f;

            Color lineColor =
                theme?.TabBorder ??
                new Color(0.34f, 0.36f, 0.40f, 0.72f);

            GameObject leftLine =
                GK2UiFactory.CreateImage(
                    root.transform,
                    "LeftLine",
                    null,
                    Image.Type.Simple,
                    new Vector2(0f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    lineColor);

            RectTransform leftRect =
                leftLine.GetComponent<RectTransform>();

            leftRect.offsetMin =
                new Vector2(
                    lineInset,
                    -0.5f);
            leftRect.offsetMax =
                new Vector2(
                    -((titleWidth / 2f) + centerGap),
                    0.5f);

            GameObject rightLine =
                GK2UiFactory.CreateImage(
                    root.transform,
                    "RightLine",
                    null,
                    Image.Type.Simple,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    lineColor);

            RectTransform rightRect =
                rightLine.GetComponent<RectTransform>();

            rightRect.offsetMin =
                new Vector2(
                    (titleWidth / 2f) + centerGap,
                    -0.5f);
            rightRect.offsetMax =
                new Vector2(
                    -lineInset,
                    0.5f);

            TextMeshProUGUI label =
                GK2UiFactory.CreateText(
                    root.transform,
                    "Title",
                    theme?.BodyTextTemplate ??
                    theme?.TitleTextTemplate,
                    title,
                    10.5f,
                    TextAlignmentOptions.Center,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(
                        titleWidth,
                        height));

            label.color =
                new Color(
                    0.70f,
                    0.68f,
                    0.66f,
                    1f);

            return new GK2UiSubsectionHeaderView
            {
                Root = root,
                Rect = root.GetComponent<RectTransform>(),
                Title = label
            };
        }
    }
}
