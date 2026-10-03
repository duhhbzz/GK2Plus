using UnityEngine;
using UnityEngine.UI;

namespace GK2Plus.Framework.UI
{
    internal sealed class GK2UiPagePaneView
    {
        public GameObject Root;
        public RectTransform Rect;
        public Image Surface;
        public Image Frame;
    }

    /// <summary>
    /// Reusable native-style page pane. The theme resolves its visual
    /// treatment once from GK2's own Inspirations page, then every GK2+
    /// screen can consume the same surface and frame.
    /// </summary>
    internal static class GK2UiPagePaneBuilder
    {
        public static GK2UiPagePaneView Create(
            Transform parent,
            GK2UiTheme theme,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot)
        {
            Sprite surfaceSprite =
                theme?.NativePaneSurfaceSprite ??
                theme?.ContentCellSprite;

            Image.Type surfaceType =
                theme?.NativePaneSurfaceSprite != null
                    ? theme.NativePaneSurfaceType
                    : Image.Type.Sliced;

            Color surfaceColor =
                theme?.NativePaneSurfaceSprite != null
                    ? theme.NativePaneSurfaceColor
                    : Color.white;

            GameObject root =
                GK2UiFactory.CreateImage(
                    parent,
                    name,
                    surfaceSprite,
                    surfaceSprite != null
                        ? surfaceType
                        : Image.Type.Simple,
                    anchorMin,
                    anchorMax,
                    pivot,
                    Vector2.zero,
                    Vector2.zero,
                    surfaceSprite != null
                        ? surfaceColor
                        : theme?.ContentBackground ??
                          new Color(0.075f, 0.082f, 0.098f, 0.96f));

            Image frame =
                null;

            if (theme?.NativePaneFrameSprite != null)
            {
                GameObject frameObject =
                    GK2UiFactory.CreateImage(
                        root.transform,
                        "NativeFrame",
                        theme.NativePaneFrameSprite,
                        theme.NativePaneFrameType,
                        Vector2.zero,
                        Vector2.one,
                        new Vector2(0.5f, 0.5f),
                        Vector2.zero,
                        Vector2.zero,
                        theme.NativePaneFrameColor);

                RectTransform frameRect =
                    frameObject.GetComponent<RectTransform>();

                frameRect.offsetMin =
                    Vector2.zero;
                frameRect.offsetMax =
                    Vector2.zero;

                frame =
                    frameObject.GetComponent<Image>();
            }

            return new GK2UiPagePaneView
            {
                Root = root,
                Rect = root.GetComponent<RectTransform>(),
                Surface = root.GetComponent<Image>(),
                Frame = frame
            };
        }
    }
}
