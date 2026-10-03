using UnityEngine;
using UnityEngine.UI;

namespace GK2Plus.Framework.UI
{
    internal sealed class GK2UiPagePaneView
    {
        public GameObject Root;
        public RectTransform Rect;
        public Graphic Surface;
        public Image Frame;
    }

    /// <summary>
    /// Reusable native-style page pane. The theme resolves the visual
    /// treatment once from GK2's own Inspirations hierarchy, including
    /// sprite- or texture-backed surfaces, and every GK2+ page consumes
    /// the same component.
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
            GameObject root =
                GK2UiFactory.CreateRect(
                    parent,
                    name,
                    anchorMin,
                    anchorMax,
                    pivot,
                    Vector2.zero,
                    Vector2.zero);

            Graphic surface =
                CreateSurface(
                    root.transform,
                    theme);

            Image frame =
                CreateFrame(
                    root.transform,
                    theme);

            return new GK2UiPagePaneView
            {
                Root = root,
                Rect = root.GetComponent<RectTransform>(),
                Surface = surface,
                Frame = frame
            };
        }

        private static Graphic CreateSurface(
            Transform parent,
            GK2UiTheme theme)
        {
            GameObject surfaceObject;

            if (theme?.NativePaneSurfaceTexture != null)
            {
                surfaceObject =
                    GK2UiFactory.CreateRawImage(
                        parent,
                        "NativeSurface",
                        theme.NativePaneSurfaceTexture,
                        theme.NativePaneSurfaceUvRect,
                        theme.NativePaneSurfaceMaterial,
                        Vector2.zero,
                        Vector2.one,
                        new Vector2(0.5f, 0.5f),
                        Vector2.zero,
                        Vector2.zero,
                        theme.NativePaneSurfaceColor);
            }
            else
            {
                Sprite sprite =
                    theme?.NativePaneSurfaceSprite ??
                    theme?.ContentCellSprite;

                Image.Type type =
                    theme?.NativePaneSurfaceSprite != null
                        ? theme.NativePaneSurfaceType
                        : Image.Type.Sliced;

                surfaceObject =
                    GK2UiFactory.CreateImage(
                        parent,
                        "NativeSurface",
                        sprite,
                        sprite != null
                            ? type
                            : Image.Type.Simple,
                        Vector2.zero,
                        Vector2.one,
                        new Vector2(0.5f, 0.5f),
                        Vector2.zero,
                        Vector2.zero,
                        sprite != null
                            ? (theme?.NativePaneSurfaceSprite != null
                                ? theme.NativePaneSurfaceColor
                                : Color.white)
                            : theme?.ContentBackground ??
                              new Color(0.075f, 0.082f, 0.098f, 0.96f));

                Image image =
                    surfaceObject.GetComponent<Image>();

                if (image != null &&
                    theme?.NativePaneSurfaceSprite != null)
                {
                    image.material =
                        theme.NativePaneSurfaceMaterial;
                }
            }

            RectTransform surfaceRect =
                surfaceObject.GetComponent<RectTransform>();

            surfaceRect.offsetMin =
                Vector2.zero;
            surfaceRect.offsetMax =
                Vector2.zero;

            return
                surfaceObject.GetComponent<Graphic>();
        }

        private static Image CreateFrame(
            Transform parent,
            GK2UiTheme theme)
        {
            if (theme?.NativePaneFrameSprite == null)
            {
                return null;
            }

            GameObject frameObject =
                GK2UiFactory.CreateImage(
                    parent,
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

            Image frame =
                frameObject.GetComponent<Image>();

            frame.material =
                theme.NativePaneFrameMaterial;

            return frame;
        }
    }
}
