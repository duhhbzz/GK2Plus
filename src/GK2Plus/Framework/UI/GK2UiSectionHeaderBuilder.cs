using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2Plus.Framework.UI
{
    internal sealed class GK2UiSectionHeaderView
    {
        public GameObject Root;
        public RectTransform Rect;
        public Image Background;
        public TextMeshProUGUI Title;
    }

    internal static class GK2UiSectionHeaderBuilder
    {
        public static GK2UiSectionHeaderView Create(
            Transform parent,
            GK2UiTheme theme,
            string name,
            string title,
            float height)
        {
            GameObject root =
                GK2UiFactory.CreateImage(
                    parent,
                    name,
                    theme?.SectionHeaderSprite,
                    theme?.SectionHeaderSprite != null
                        ? Image.Type.Sliced
                        : Image.Type.Simple,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f),
                    Vector2.zero,
                    new Vector2(0f, height),
                    theme?.SectionHeaderSprite != null
                        ? Color.white
                        : theme?.HeaderBackground ??
                          new Color(0.43f, 0.33f, 0.20f, 0.97f));

            TextMeshProUGUI label =
                GK2UiFactory.CreateText(
                    root.transform,
                    "Title",
                    theme?.TitleTextTemplate,
                    title,
                    15f,
                    TextAlignmentOptions.Center,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero);

            label.rectTransform.offsetMin =
                Vector2.zero;
            label.rectTransform.offsetMax =
                Vector2.zero;

            if (theme?.SectionHeaderTextStyle != null)
            {
                theme.SectionHeaderTextStyle.ApplyStyle(
                    label);
            }
            else
            {
                label.color =
                    theme?.AccentText ??
                    new Color(1f, 0.82f, 0.45f, 1f);
            }

            return new GK2UiSectionHeaderView
            {
                Root = root,
                Rect = root.GetComponent<RectTransform>(),
                Background = root.GetComponent<Image>(),
                Title = label
            };
        }
    }
}
