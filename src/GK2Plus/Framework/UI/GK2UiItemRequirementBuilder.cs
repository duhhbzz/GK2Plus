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
            GameObject root =
                GK2UiFactory.CreateImage(
                    parent,
                    "ItemRequirement",
                    theme?.ItemSlotSprite,
                    theme?.ItemSlotSprite != null
                        ? Image.Type.Sliced
                        : Image.Type.Simple,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    Vector2.zero,
                    new Vector2(size, size),
                    theme?.ItemSlotSprite != null
                        ? Color.white
                        : new Color(0.08f, 0.09f, 0.11f, 0.95f));

            Image icon =
                GK2UiFactory.CreateImage(
                    root.transform,
                    "Icon",
                    null,
                    Image.Type.Simple,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 1f),
                    new Vector2(size - 8f, size - 8f),
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
                    "Count",
                    theme?.CountTextTemplate ??
                    theme?.BodyTextTemplate,
                    string.Empty,
                    Mathf.Max(10f, size * 0.24f),
                    TextAlignmentOptions.BottomRight,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero);

            count.rectTransform.offsetMin =
                Vector2.zero;
            count.rectTransform.offsetMax =
                new Vector2(-2f, -1f);

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
