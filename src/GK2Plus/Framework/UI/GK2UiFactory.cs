using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GK2Plus.Framework.UI
{
    internal enum GK2UiButtonTone
    {
        Neutral,
        Selected,
        Accent
    }

    /// <summary>
    /// Small construction primitives used by every GK2+ UI surface.
    /// </summary>
    internal static class GK2UiFactory
    {
        public static GameObject CreateRect(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject obj =
                new GameObject(
                    name,
                    typeof(RectTransform));

            obj.transform.SetParent(
                parent,
                false);

            ApplyRect(
                obj.GetComponent<RectTransform>(),
                anchorMin,
                anchorMax,
                pivot,
                anchoredPosition,
                size);

            return obj;
        }

        public static GameObject CreateImage(
            Transform parent,
            string name,
            Sprite sprite,
            Image.Type type,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color,
            bool raycastTarget = false)
        {
            GameObject obj =
                new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            obj.transform.SetParent(
                parent,
                false);

            ApplyRect(
                obj.GetComponent<RectTransform>(),
                anchorMin,
                anchorMax,
                pivot,
                anchoredPosition,
                size);

            Image image =
                obj.GetComponent<Image>();

            image.sprite =
                sprite;
            image.type =
                sprite != null
                    ? type
                    : Image.Type.Simple;
            image.color =
                color;
            image.raycastTarget =
                raycastTarget;

            return obj;
        }

        public static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            TextMeshProUGUI template,
            string text,
            float fontSize,
            TextAlignmentOptions alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject obj =
                new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));

            obj.transform.SetParent(
                parent,
                false);

            ApplyRect(
                obj.GetComponent<RectTransform>(),
                anchorMin,
                anchorMax,
                pivot,
                anchoredPosition,
                size);

            TextMeshProUGUI label =
                obj.GetComponent<TextMeshProUGUI>();

            if (template != null)
            {
                label.font =
                    template.font;
                label.fontSharedMaterial =
                    template.fontSharedMaterial;
                label.spriteAsset =
                    template.spriteAsset;
                label.color =
                    template.color;
                label.characterSpacing =
                    template.characterSpacing;
                label.wordSpacing =
                    template.wordSpacing;
                label.lineSpacing =
                    template.lineSpacing;
            }

            label.text =
                text ?? string.Empty;
            label.fontSize =
                fontSize;
            label.fontSizeMin =
                fontSize;
            label.fontSizeMax =
                fontSize;
            label.enableAutoSizing =
                false;
            label.alignment =
                alignment;
            label.richText =
                true;
            label.raycastTarget =
                false;

            return label;
        }

        public static Button CreateButton(
            Transform parent,
            string name,
            GK2UiTheme theme,
            string text,
            Vector2 anchoredPosition,
            Vector2 size,
            UnityAction onClick = null,
            GK2UiButtonTone tone = GK2UiButtonTone.Neutral)
        {
            GameObject obj =
                CreateImage(
                    parent,
                    name,
                    theme?.ButtonSprite,
                    Image.Type.Sliced,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    anchoredPosition,
                    size,
                    ResolveButtonColor(theme, tone),
                    true);

            Button button =
                obj.AddComponent<Button>();

            button.targetGraphic =
                obj.GetComponent<Image>();
            button.transition =
                Selectable.Transition.ColorTint;

            ColorBlock colors =
                button.colors;

            colors.normalColor =
                Color.white;
            colors.highlightedColor =
                new Color(1f, 1f, 1f, 0.95f);
            colors.pressedColor =
                new Color(0.80f, 0.80f, 0.80f, 0.95f);
            colors.selectedColor =
                Color.white;
            colors.disabledColor =
                new Color(0.55f, 0.55f, 0.55f, 0.60f);
            colors.fadeDuration =
                0.05f;

            button.colors =
                colors;

            TextMeshProUGUI label =
                CreateText(
                    obj.transform,
                    "Label",
                    theme?.ButtonTextTemplate,
                    text,
                    10f,
                    TextAlignmentOptions.Center,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero);

            RectTransform labelRect =
                label.rectTransform;
            labelRect.offsetMin =
                Vector2.zero;
            labelRect.offsetMax =
                Vector2.zero;

            if (onClick != null)
            {
                button.onClick.AddListener(
                    onClick);
            }

            return button;
        }

        public static Button CreateFlatButton(
            Transform parent,
            string name,
            GK2UiTheme theme,
            string text,
            Vector2 anchoredPosition,
            Vector2 size,
            UnityAction onClick = null,
            bool selected = false)
        {
            GameObject obj =
                CreateImage(
                    parent,
                    name,
                    null,
                    Image.Type.Simple,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    anchoredPosition,
                    size,
                    selected
                        ? (theme?.TabSelected ?? new Color(0.38f, 0.30f, 0.20f, 0.98f))
                        : (theme?.TabNeutral ?? new Color(0.15f, 0.17f, 0.21f, 0.96f)),
                    true);

            Image image =
                obj.GetComponent<Image>();

            Button button =
                obj.AddComponent<Button>();

            button.targetGraphic =
                image;
            button.transition =
                Selectable.Transition.ColorTint;

            ColorBlock colors =
                button.colors;

            colors.normalColor =
                Color.white;
            colors.highlightedColor =
                new Color(1.12f, 1.08f, 1f, 1f);
            colors.pressedColor =
                new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor =
                Color.white;
            colors.disabledColor =
                new Color(0.55f, 0.55f, 0.55f, 0.65f);
            colors.fadeDuration =
                0.05f;

            button.colors =
                colors;

            TextMeshProUGUI label =
                CreateText(
                    obj.transform,
                    "Label",
                    theme?.BodyTextTemplate ??
                    theme?.ButtonTextTemplate,
                    text,
                    9.5f,
                    TextAlignmentOptions.Center,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero);

            label.color =
                theme?.AccentText ??
                new Color(1f, 0.82f, 0.45f, 1f);

            RectTransform labelRect =
                label.rectTransform;
            labelRect.offsetMin =
                new Vector2(3f, 0f);
            labelRect.offsetMax =
                new Vector2(-3f, 0f);

            if (onClick != null)
            {
                button.onClick.AddListener(
                    onClick);
            }

            return button;
        }

        public static void SetButtonTone(
            Button button,
            GK2UiTheme theme,
            GK2UiButtonTone tone)
        {
            Image image =
                button?.targetGraphic as Image;

            if (image != null)
            {
                image.color =
                    ResolveButtonColor(
                        theme,
                        tone);
            }
        }

        public static void ApplyRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin =
                anchorMin;
            rect.anchorMax =
                anchorMax;
            rect.pivot =
                pivot;
            rect.anchoredPosition =
                anchoredPosition;
            rect.sizeDelta =
                size;
            rect.localScale =
                Vector3.one;
            rect.localRotation =
                Quaternion.identity;
        }

        private static Color ResolveButtonColor(
            GK2UiTheme theme,
            GK2UiButtonTone tone)
        {
            if (theme == null)
            {
                return Color.white;
            }

            switch (tone)
            {
                case GK2UiButtonTone.Selected:
                    return theme.ButtonSelected;

                case GK2UiButtonTone.Accent:
                    return new Color(
                        0.78f,
                        0.60f,
                        0.34f,
                        1f);

                default:
                    return theme.ButtonNeutral;
            }
        }
    }
}
