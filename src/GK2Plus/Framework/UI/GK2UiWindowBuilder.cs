using UnityEngine;
using UnityEngine.UI;

namespace GK2Plus.Framework.UI
{
    internal sealed class GK2UiWindowView
    {
        public GameObject Root;
        public GameObject Window;
        public RectTransform WindowRect;
        public GameObject SafeArea;
        public RectTransform SafeAreaRect;
    }

    /// <summary>
    /// Shared modal/window chrome used by GK2+ screens.
    /// </summary>
    internal static class GK2UiWindowBuilder
    {
        public static GK2UiWindowView CreateModal(
            Transform uiRoot,
            string rootName,
            GK2UiTheme theme,
            Vector2 windowSize,
            int sortingOrder = 30000,
            float dimmerAlpha = 0.26f)
        {
            Transform old =
                uiRoot?.Find(rootName);

            if (old != null)
            {
                Object.Destroy(
                    old.gameObject);
            }

            GameObject overlay =
                GK2UiFactory.CreateRect(
                    uiRoot,
                    rootName,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero);

            RectTransform overlayRect =
                overlay.GetComponent<RectTransform>();

            overlayRect.offsetMin =
                Vector2.zero;
            overlayRect.offsetMax =
                Vector2.zero;

            overlay.transform.SetAsLastSibling();

            Canvas canvas =
                overlay.AddComponent<Canvas>();

            canvas.overrideSorting =
                true;
            canvas.sortingOrder =
                sortingOrder;

            if (overlay.GetComponent<GraphicRaycaster>() == null)
            {
                overlay.AddComponent<GraphicRaycaster>();
            }

            GameObject dimmer =
                GK2UiFactory.CreateImage(
                    overlay.transform,
                    "Dimmer",
                    null,
                    Image.Type.Simple,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    new Color(0f, 0f, 0f, dimmerAlpha),
                    true);

            RectTransform dimmerRect =
                dimmer.GetComponent<RectTransform>();

            dimmerRect.offsetMin =
                Vector2.zero;
            dimmerRect.offsetMax =
                Vector2.zero;

            dimmer.transform.SetAsFirstSibling();

            GameObject window =
                GK2UiFactory.CreateRect(
                    overlay.transform,
                    "Window",
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    windowSize);

            GameObject backing =
                GK2UiFactory.CreateImage(
                    window.transform,
                    "SolidBacking",
                    null,
                    Image.Type.Simple,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    theme?.PanelBackground ??
                    new Color(0.055f, 0.060f, 0.073f, 0.97f));

            RectTransform backingRect =
                backing.GetComponent<RectTransform>();

            backingRect.offsetMin =
                Vector2.zero;
            backingRect.offsetMax =
                Vector2.zero;

            GameObject background =
                GK2UiFactory.CreateImage(
                    window.transform,
                    "Background",
                    theme?.WindowBackgroundSprite,
                    Image.Type.Sliced,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    new Color(1f, 1f, 1f, 0.98f));

            RectTransform backgroundRect =
                background.GetComponent<RectTransform>();

            backgroundRect.offsetMin =
                new Vector2(-3f, -3f);
            backgroundRect.offsetMax =
                new Vector2(3f, 3f);

            GameObject frame =
                GK2UiFactory.CreateImage(
                    window.transform,
                    "Frame",
                    theme?.WindowFrameSprite,
                    Image.Type.Sliced,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    Color.white);

            RectTransform frameRect =
                frame.GetComponent<RectTransform>();

            frameRect.offsetMin =
                Vector2.zero;
            frameRect.offsetMax =
                Vector2.zero;

            GameObject safeArea =
                GK2UiFactory.CreateRect(
                    window.transform,
                    "ContentSafeArea",
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero);

            RectTransform safeAreaRect =
                safeArea.GetComponent<RectTransform>();

            Vector4 insets =
                GK2UiMetrics.Menu.FrameInsets;

            safeAreaRect.offsetMin =
                new Vector2(
                    insets.x,
                    insets.y);
            safeAreaRect.offsetMax =
                new Vector2(
                    -insets.z,
                    -insets.w);

            return new GK2UiWindowView
            {
                Root = overlay,
                Window = window,
                WindowRect = window.GetComponent<RectTransform>(),
                SafeArea = safeArea,
                SafeAreaRect = safeAreaRect
            };
        }
    }
}
