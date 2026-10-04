using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2Plus.Framework.UI
{
    internal sealed class GK2UiListRowView
    {
        public GameObject Root;
        public RectTransform Rect;
        public TextMeshProUGUI Label;
        public TextMeshProUGUI Subtitle;
        public Button ExpandButton;
        public Button ActionButton;
    }

    /// <summary>
    /// Standard native-style settings/list row used by the GK2+ menu.
    /// </summary>
    internal static class GK2UiListRowBuilder
    {
        public static GK2UiListRowView Create(
            Transform parent,
            GK2UiTheme theme,
            string name,
            string label,
            float y,
            string labelObjectName,
            string buttonObjectName,
            string buttonText,
            bool child = false,
            string subtitle = null,
            bool expandable = false,
            bool expanded = true,
            bool drawBackground = true)
        {
            GameObject row =
                GK2UiFactory.CreateRect(
                    parent,
                    name,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, y),
                    new Vector2(
                        GK2UiMetrics.Menu.BodyContentWidth,
                        GK2UiMetrics.Menu.ControlRowHeight));

            Image background =
                row.AddComponent<Image>();

            if (drawBackground)
            {
                background.sprite =
                    theme?.ContentCellSprite;
                background.type =
                    theme?.ContentCellSprite != null
                        ? Image.Type.Sliced
                        : Image.Type.Simple;
                background.color =
                    theme?.ContentCellSprite != null
                        ? (child
                            ? new Color(0.88f, 0.88f, 0.88f, 0.92f)
                            : Color.white)
                        : (child
                            ? (theme?.ChildRowBackground ??
                               new Color(0.095f, 0.105f, 0.13f, 0.82f))
                            : (theme?.RowBackground ??
                               new Color(0.12f, 0.13f, 0.16f, 0.88f)));
            }
            else
            {
                background.sprite =
                    null;
                background.type =
                    Image.Type.Simple;
                background.color =
                    Color.clear;
            }

            background.raycastTarget =
                false;

            float leftPadding =
                child
                    ? 24f
                    : 12f;

            float buttonWidth =
                GK2UiMetrics.Menu.ControlButtonWidth;

            float rightPadding =
                9f;

            bool hasSubtitle =
                !string.IsNullOrWhiteSpace(
                    subtitle);

            TextMeshProUGUI labelText =
                GK2UiFactory.CreateText(
                    row.transform,
                    labelObjectName,
                    theme?.BodyTextTemplate,
                    expandable
                        ? $"{(expanded ? "-" : "+")}  {label}"
                        : label,
                    child
                        ? 9f
                        : 10f,
                    TextAlignmentOptions.Left,
                    new Vector2(0f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 0.5f),
                    new Vector2(
                        leftPadding,
                        hasSubtitle
                            ? 5f
                            : 0f),
                    new Vector2(
                        -(buttonWidth +
                          rightPadding +
                          leftPadding),
                        hasSubtitle
                            ? -11f
                            : 0f));

            labelText.color =
                child
                    ? new Color(0.88f, 0.80f, 0.64f, 1f)
                    : (theme?.AccentText ??
                       new Color(1f, 0.82f, 0.45f, 1f));

            TextMeshProUGUI subtitleText =
                null;

            if (hasSubtitle)
            {
                subtitleText =
                    GK2UiFactory.CreateText(
                        row.transform,
                        labelObjectName + "Subtitle",
                        theme?.BodyTextTemplate,
                        subtitle,
                        7.5f,
                        TextAlignmentOptions.Left,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 1f),
                        new Vector2(0f, 0.5f),
                        new Vector2(
                            leftPadding,
                            -8f),
                        new Vector2(
                            -(buttonWidth +
                              rightPadding +
                              leftPadding),
                            -14f));

                subtitleText.color =
                    new Color(
                        0.66f,
                        0.62f,
                        0.57f,
                        0.95f);
            }

            Button expandButton =
                null;

            if (expandable)
            {
                expandButton =
                    labelText.gameObject.AddComponent<Button>();

                expandButton.targetGraphic =
                    labelText;
                expandButton.transition =
                    Selectable.Transition.ColorTint;

                ColorBlock expandColors =
                    expandButton.colors;

                Color normal =
                    labelText.color;

                // Let the Button state own the visible tint so the normal
                // accent is not multiplied by itself by Unity's ColorTint.
                labelText.color =
                    Color.white;

                expandColors.normalColor =
                    normal;
                expandColors.highlightedColor =
                    new Color(
                        1f,
                        0.678f,
                        0.227f,
                        1f);
                expandColors.pressedColor =
                    new Color(
                        1f,
                        0.678f,
                        0.227f,
                        1f);
                expandColors.selectedColor =
                    normal;
                expandColors.disabledColor =
                    new Color(
                        normal.r,
                        normal.g,
                        normal.b,
                        0.5f);
                expandColors.fadeDuration =
                    0.05f;

                expandButton.colors =
                    expandColors;

                labelText.raycastTarget =
                    true;

                Navigation expandNavigation =
                    expandButton.navigation;
                expandNavigation.mode =
                    Navigation.Mode.Automatic;
                expandButton.navigation =
                    expandNavigation;
            }

            Button action =
                GK2UiFactory.CreateNativeRedButton(
                    row.transform,
                    buttonObjectName,
                    theme,
                    buttonText,
                    Vector2.zero,
                    new Vector2(
                        buttonWidth,
                        GK2UiMetrics.Native.RedButtonSize.y));

            RectTransform actionRect =
                action.GetComponent<RectTransform>();

            actionRect.anchorMin =
                new Vector2(1f, 0.5f);
            actionRect.anchorMax =
                new Vector2(1f, 0.5f);
            actionRect.pivot =
                new Vector2(1f, 0.5f);
            actionRect.anchoredPosition =
                new Vector2(
                    -rightPadding,
                    0f);

            return new GK2UiListRowView
            {
                Root = row,
                Rect = row.GetComponent<RectTransform>(),
                Label = labelText,
                Subtitle = subtitleText,
                ExpandButton = expandButton,
                ActionButton = action
            };
        }
    }
}
