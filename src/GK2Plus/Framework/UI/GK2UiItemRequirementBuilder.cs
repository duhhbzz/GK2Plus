using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2Plus.Framework.UI
{
    internal sealed class GK2UiItemRequirementView
    {
        public GameObject Root;
        public RectTransform Rect;
        public Image Icon;
        public TextMeshProUGUI Count;
        public string ItemId;
        public bool? Enough;
    }

    internal static class GK2UiItemRequirementBuilder
    {
        public static GK2UiItemRequirementView Create(
            Transform parent,
            GK2UiTheme theme,
            float size)
        {
            float scale =
                Mathf.Max(0.1f, size) /
                GK2UiMetrics.Native.ItemCellSize;

            GameObject root =
                GK2UiFactory.CreateRect(
                    parent,
                    "ItemRequirement",
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    Vector2.zero,
                    new Vector2(size, size));

            // Native UIItemCell: 42x42 logical cell with a 44x44 Simple
            // background image centered over it.
            GK2UiFactory.CreateImage(
                root.transform,
                "Background",
                theme?.ItemSlotSprite,
                Image.Type.Simple,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(
                    GK2UiMetrics.Native.ItemCellBackgroundSize * scale,
                    GK2UiMetrics.Native.ItemCellBackgroundSize * scale),
                theme?.ItemSlotSprite != null
                    ? Color.white
                    : new Color(0.08f, 0.09f, 0.11f, 0.95f));

            Image icon =
                GK2UiFactory.CreateImage(
                    root.transform,
                    "ItemIcon",
                    null,
                    Image.Type.Simple,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(
                        GK2UiMetrics.Native.ItemIconSize * scale,
                        GK2UiMetrics.Native.ItemIconSize * scale),
                    Color.white)
                    .GetComponent<Image>();

            icon.preserveAspect =
                true;

            if (theme?.ItemIconMaterial != null)
            {
                icon.material =
                    theme.ItemIconMaterial;
            }

            TextMeshProUGUI count =
                GK2UiFactory.CreateText(
                    root.transform,
                    "CountLabel",
                    theme?.CountTextTemplate ??
                    theme?.BodyTextTemplate,
                    string.Empty,
                    16f * scale,
                    TextAlignmentOptions.TopRight,
                    Vector2.zero,
                    Vector2.zero,
                    new Vector2(1f, 0f),
                    new Vector2(
                        -2f * scale,
                        2f * scale),
                    new Vector2(
                        44f * scale,
                        10f * scale));

            // Native count label is golden and uses tiny_font's
            // item_quantity material. CountNormal/CountRed styles will
            // replace this during Bind when available.
            count.color =
                new Color(
                    1f,
                    0.765f,
                    0f,
                    1f);

            return new GK2UiItemRequirementView
            {
                Root = root,
                Rect = root.GetComponent<RectTransform>(),
                Icon = icon,
                Count = count
            };
        }

        public static bool Bind(
            GK2UiItemRequirementView view,
            GK2UiTheme theme,
            string itemId,
            int current,
            int target)
        {
            if (view == null ||
                string.IsNullOrWhiteSpace(itemId) ||
                GameBalance.Me == null)
            {
                return false;
            }

            ItemDef definition =
                GameBalance.Me.GetDataOrNull<ItemDef>(
                    itemId);

            if (definition == null ||
                string.IsNullOrWhiteSpace(definition.iconId))
            {
                view.Root.SetActive(false);
                return false;
            }

            Sprite sprite =
                LazySingletonSO<EasySpritesCollection>
                    .Instance?
                    .GetSprite(
                        definition.iconId,
                        null);

            if (sprite == null)
            {
                view.Root.SetActive(false);
                return false;
            }

            view.Root.SetActive(true);

            if (!string.Equals(
                    view.ItemId,
                    itemId,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                view.ItemId =
                    itemId;
                view.Icon.sprite =
                    sprite;
                view.Icon.BlueColorReplace(
                    theme?.ItemIconTint ??
                    Color.white);
            }

            int safeTarget =
                Mathf.Max(
                    1,
                    target);

            view.Count.text =
                $"{current}/{safeTarget}";

            bool enough =
                current >= safeTarget;

            if (view.Enough != enough)
            {
                view.Enough =
                    enough;

                TextStyle style =
                    enough
                        ? theme?.CountNormalStyle
                        : theme?.CountRedStyle;

                if (style != null)
                {
                    style.ApplyStyle(
                        view.Count);
                }
                else
                {
                    view.Count.color =
                        enough
                            ? new Color(1f, 0.84f, 0.18f, 1f)
                            : new Color(1f, 0.25f, 0.24f, 1f);
                }
            }

            return true;
        }
    }
}
