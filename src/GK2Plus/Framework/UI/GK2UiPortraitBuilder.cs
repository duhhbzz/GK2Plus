using UnityEngine;
using UnityEngine.UI;

namespace GK2Plus.Framework.UI
{
    internal sealed class GK2UiPortraitView
    {
        public GameObject Root;
        public RectTransform Rect;
        public Image Portrait;
    }

    /// <summary>
    /// Neutral character portrait treatment for GK2+ UI.
    /// Uses the generic textured content cell instead of inventory/item-slot
    /// art, so it never carries baked quantity tabs or item-specific details.
    /// </summary>
    internal static class GK2UiPortraitBuilder
    {
        public static GK2UiPortraitView CreateStonePortrait(
            Transform parent,
            GK2UiTheme theme,
            string name,
            Vector2 anchoredPosition,
            float size,
            float visualScale = 1f)
        {
            Sprite backingSprite =
                theme?.ContentCellSprite;

            GameObject root =
                GK2UiFactory.CreateImage(
                    parent,
                    name,
                    backingSprite,
                    backingSprite != null
                        ? Image.Type.Sliced
                        : Image.Type.Simple,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    anchoredPosition,
                    new Vector2(size, size),
                    backingSprite != null
                        ? Color.white
                        : new Color(
                            0.16f,
                            0.17f,
                            0.19f,
                            1f));

            GameObject inset =
                GK2UiFactory.CreateImage(
                    root.transform,
                    "Inset",
                    null,
                    Image.Type.Simple,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    new Color(
                        0.07f,
                        0.075f,
                        0.09f,
                        0.72f));

            RectTransform insetRect =
                inset.GetComponent<RectTransform>();

            insetRect.offsetMin =
                new Vector2(3f, 3f);
            insetRect.offsetMax =
                new Vector2(-3f, -3f);

            GameObject portraitObject =
                GK2UiFactory.CreateImage(
                    inset.transform,
                    "Portrait",
                    null,
                    Image.Type.Simple,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    Color.white);

            RectTransform portraitRect =
                portraitObject.GetComponent<RectTransform>();

            portraitRect.offsetMin =
                new Vector2(2f, 2f);
            portraitRect.offsetMax =
                new Vector2(-2f, -2f);
            portraitRect.localScale =
                new Vector3(
                    visualScale,
                    visualScale,
                    1f);

            Image portrait =
                portraitObject.GetComponent<Image>();

            portrait.preserveAspect =
                true;

            if (theme?.ItemIconMaterial != null)
            {
                portrait.material =
                    theme.ItemIconMaterial;
            }

            return new GK2UiPortraitView
            {
                Root = root,
                Rect = root.GetComponent<RectTransform>(),
                Portrait = portrait
            };
        }
    }
}
