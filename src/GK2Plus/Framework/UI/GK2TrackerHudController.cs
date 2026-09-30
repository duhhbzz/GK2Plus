using System;
using System.Linq;
using BepInEx.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2Plus.Framework.UI
{
    internal sealed class GK2TrackerHudController : MonoBehaviour
    {
        private const string HudRootName = "GK2PlusNativeTrackerHud";

        private ManualLogSource _logger;
        private Func<bool> _visibleProvider;
        private Func<string> _textProvider;

        private GameObject _hudRoot;
        private RectTransform _panelRect;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _bodyText;
        private string _cachedText = string.Empty;
        private float _nextRefreshAt;

        public static GK2TrackerHudController Create(
            ManualLogSource logger,
            Func<bool> visibleProvider,
            Func<string> textProvider)
        {
            GameObject host =
                GameObject.Find("GK2PlusTrackerHudController");

            if (host == null)
            {
                host = new GameObject(
                    "GK2PlusTrackerHudController");
                DontDestroyOnLoad(host);
            }

            GK2TrackerHudController controller =
                host.GetComponent<GK2TrackerHudController>();

            if (controller == null)
            {
                controller =
                    host.AddComponent<GK2TrackerHudController>();
            }

            controller._logger = logger;
            controller._visibleProvider = visibleProvider;
            controller._textProvider = textProvider;
            controller._nextRefreshAt = 0f;

            return controller;
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefreshAt)
            {
                return;
            }

            _nextRefreshAt =
                Time.unscaledTime + 0.25f;

            bool visible =
                _visibleProvider?.Invoke() == true;

            if (!visible)
            {
                _cachedText = string.Empty;

                if (_hudRoot != null)
                {
                    _hudRoot.SetActive(false);
                }

                return;
            }

            try
            {
                _cachedText =
                    _textProvider?.Invoke() ??
                    string.Empty;
            }
            catch (Exception ex)
            {
                _cachedText = string.Empty;
                _logger?.LogError(
                    $"GK2+ tracker HUD refresh failed: {ex}");
            }

            if (string.IsNullOrWhiteSpace(_cachedText))
            {
                if (_hudRoot != null)
                {
                    _hudRoot.SetActive(false);
                }

                return;
            }

            if (!EnsureHud())
            {
                return;
            }

            _hudRoot.SetActive(true);
            _bodyText.text = _cachedText;

            int lineCount =
                Math.Max(
                    1,
                    _cachedText.Split('\n').Length);

            float height =
                Mathf.Clamp(
                    46f + (lineCount * 14f),
                    86f,
                    330f);

            _panelRect.sizeDelta =
                new Vector2(
                    300f,
                    height);

            RectTransform bodyRect =
                _bodyText.rectTransform;

            bodyRect.offsetMin =
                new Vector2(
                    14f,
                    10f);

            bodyRect.offsetMax =
                new Vector2(
                    -14f,
                    -34f);
        }

        private bool EnsureHud()
        {
            if (_hudRoot != null &&
                _bodyText != null)
            {
                return true;
            }

            GUIElements gui =
                GUIElements.Instance;

            if (gui == null ||
                gui.Root == null)
            {
                return false;
            }

            TextMeshProUGUI template =
                Resources
                    .FindObjectsOfTypeAll<TextMeshProUGUI>()
                    .FirstOrDefault(text =>
                        text != null &&
                        text.font != null &&
                        text.gameObject.activeInHierarchy);

            if (template == null)
            {
                template =
                    Resources
                        .FindObjectsOfTypeAll<TextMeshProUGUI>()
                        .FirstOrDefault(text =>
                            text != null &&
                            text.font != null);
            }

            if (template == null)
            {
                return false;
            }

            Transform old =
                gui.Root.Find(
                    HudRootName);

            if (old != null)
            {
                Destroy(
                    old.gameObject);
            }

            _hudRoot =
                new GameObject(
                    HudRootName,
                    typeof(RectTransform));

            _hudRoot.transform.SetParent(
                gui.Root,
                false);

            _hudRoot.transform.SetAsLastSibling();

            RectTransform rootRect =
                _hudRoot.GetComponent<RectTransform>();

            rootRect.anchorMin =
                Vector2.one;
            rootRect.anchorMax =
                Vector2.one;
            rootRect.pivot =
                Vector2.one;
            rootRect.anchoredPosition =
                new Vector2(
                    -18f,
                    -54f);
            rootRect.sizeDelta =
                new Vector2(
                    300f,
                    100f);

            _panelRect =
                rootRect;

            Sprite backgroundSprite =
                FindSprite(
                    "titlescreen-menu-bg");

            Sprite frameSprite =
                FindSprite(
                    "comm-frame_1-border");

            GameObject backing =
                CreateImage(
                    _hudRoot.transform,
                    "Backing",
                    backgroundSprite,
                    Image.Type.Sliced);

            Image backingImage =
                backing.GetComponent<Image>();

            backingImage.color =
                new Color(
                    0.16f,
                    0.17f,
                    0.21f,
                    0.94f);
            backingImage.raycastTarget =
                false;

            if (frameSprite != null)
            {
                GameObject frame =
                    CreateImage(
                        _hudRoot.transform,
                        "Frame",
                        frameSprite,
                        Image.Type.Sliced);

                Image frameImage =
                    frame.GetComponent<Image>();

                frameImage.color =
                    new Color(
                        0.78f,
                        0.78f,
                        0.82f,
                        0.95f);
                frameImage.raycastTarget =
                    false;
            }

            GameObject titleBacking =
                new GameObject(
                    "TitleBacking",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            titleBacking.transform.SetParent(
                _hudRoot.transform,
                false);

            RectTransform titleRect =
                titleBacking.GetComponent<RectTransform>();

            titleRect.anchorMin =
                new Vector2(
                    0f,
                    1f);
            titleRect.anchorMax =
                Vector2.one;
            titleRect.pivot =
                new Vector2(
                    0.5f,
                    1f);
            titleRect.offsetMin =
                new Vector2(
                    5f,
                    -30f);
            titleRect.offsetMax =
                new Vector2(
                    -5f,
                    -5f);

            Image titleImage =
                titleBacking.GetComponent<Image>();

            titleImage.color =
                new Color(
                    0.25f,
                    0.20f,
                    0.14f,
                    0.96f);
            titleImage.raycastTarget =
                false;

            _titleText =
                CreateText(
                    template,
                    titleBacking.transform,
                    "Title",
                    15f,
                    TextAlignmentOptions.Center);

            _titleText.text =
                "Tracked";

            _titleText.color =
                new Color(
                    1f,
                    0.84f,
                    0.48f,
                    1f);

            _bodyText =
                CreateText(
                    template,
                    _hudRoot.transform,
                    "Body",
                    12f,
                    TextAlignmentOptions.TopLeft);

            _bodyText.enableWordWrapping =
                true;
            _bodyText.richText =
                true;

            _logger?.LogInfo(
                "GK2+ native-style tracker HUD initialized.");

            return true;
        }

        private static GameObject CreateImage(
            Transform parent,
            string name,
            Sprite sprite,
            Image.Type type)
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

            RectTransform rect =
                obj.GetComponent<RectTransform>();

            rect.anchorMin =
                Vector2.zero;
            rect.anchorMax =
                Vector2.one;
            rect.offsetMin =
                Vector2.zero;
            rect.offsetMax =
                Vector2.zero;

            Image image =
                obj.GetComponent<Image>();

            image.sprite =
                sprite;
            image.type =
                sprite != null
                    ? type
                    : Image.Type.Simple;

            return obj;
        }

        private static TextMeshProUGUI CreateText(
            TextMeshProUGUI template,
            Transform parent,
            string name,
            float fontSize,
            TextAlignmentOptions alignment)
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

            TextMeshProUGUI text =
                obj.GetComponent<TextMeshProUGUI>();

            text.font =
                template.font;
            text.fontSharedMaterial =
                template.fontSharedMaterial;
            text.spriteAsset =
                template.spriteAsset;
            text.fontSize =
                fontSize;
            text.fontSizeMin =
                fontSize;
            text.fontSizeMax =
                fontSize;
            text.enableAutoSizing =
                false;
            text.alignment =
                alignment;
            text.color =
                template.color;
            text.raycastTarget =
                false;
            text.margin =
                Vector4.zero;
            text.characterSpacing =
                0.5f;
            text.lineSpacing =
                1f;

            RectTransform rect =
                text.rectTransform;

            rect.anchorMin =
                Vector2.zero;
            rect.anchorMax =
                Vector2.one;
            rect.offsetMin =
                Vector2.zero;
            rect.offsetMax =
                Vector2.zero;

            return text;
        }

        private static Sprite FindSprite(
            string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return Resources
                .FindObjectsOfTypeAll<Sprite>()
                .FirstOrDefault(sprite =>
                    sprite != null &&
                    string.Equals(
                        sprite.name,
                        name,
                        StringComparison.OrdinalIgnoreCase));
        }

        public void ShutdownController()
        {
            _visibleProvider = null;
            _textProvider = null;
            _cachedText =
                string.Empty;

            if (_hudRoot != null)
            {
                Destroy(
                    _hudRoot);
                _hudRoot = null;
            }

            if (gameObject != null)
            {
                Destroy(
                    gameObject);
            }
        }
    }
}
