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
            bool child = false)
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

            background.color =
                child
                    ? (theme?.ChildRowBackground ??
                       new Color(0.095f, 0.105f, 0.13f, 0.82f))
                    : (theme?.RowBackground ??
                       new Color(0.12f, 0.13f, 0.16f, 0.88f));

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

            TextMeshProUGUI labelText =
                GK2UiFactory.CreateText(
                    row.transform,
                    labelObjectName,
                    theme?.BodyTextTemplate,
                    label,
                    child
                        ? 8.5f
                        : 9f,
                    TextAlignmentOptions.Left,
                    new Vector2(0f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 0.5f),
                    new Vector2(
                        leftPadding,
                        0f),
                    new Vector2(
                        -(buttonWidth +
                          rightPadding +
                          leftPadding),
                        0f));

            labelText.color =
                theme?.AccentText ??
                new Color(1f, 0.82f, 0.45f, 1f);

            Button action =
                GK2UiFactory.CreateButton(
                    row.transform,
                    buttonObjectName,
                    theme,
                    buttonText,
                    Vector2.zero,
                    new Vector2(
                        buttonWidth,
                        GK2UiMetrics.Menu.ControlRowHeight - 2f),
                    null,
                    GK2UiButtonTone.Neutral);

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
                ActionButton = action
            };
        }
    }
}
