using System;
using System.Collections.Generic;
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

        private sealed class HudPanel
        {
            public GameObject Root;
            public RectTransform Rect;
            public TextMeshProUGUI Title;
            public TextMeshProUGUI Body;
        }

        private ManualLogSource _logger;
        private Func<bool> _visibleProvider;
        private Func<string> _textProvider;

        private GameObject _hudRoot;
        private RectTransform _rootRect;
        private TextMeshProUGUI _textTemplate;
        private readonly Dictionary<string, HudPanel> _panels =
            new Dictionary<string, HudPanel>(StringComparer.OrdinalIgnoreCase);

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
                HideAll();
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
                _cachedText =
                    string.Empty;

                _logger?.LogError(
                    $"GK2+ tracker HUD refresh failed: {ex}");
            }

            if (string.IsNullOrWhiteSpace(_cachedText) ||
                !EnsureHud())
            {
                HideAll();
                return;
            }

            Dictionary<string, string> groups =
                ParseGroups(
                    _cachedText);

            LayoutGroups(
                groups);
        }

        private bool EnsureHud()
        {
            if (_hudRoot != null &&
                _rootRect != null &&
                _textTemplate != null)
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

            _textTemplate =
                Resources
                    .FindObjectsOfTypeAll<TextMeshProUGUI>()
                    .FirstOrDefault(text =>
                        text != null &&
                        text.font != null &&
                        text.gameObject.activeInHierarchy) ??
                Resources
                    .FindObjectsOfTypeAll<TextMeshProUGUI>()
                    .FirstOrDefault(text =>
                        text != null &&
                        text.font != null);

            if (_textTemplate == null)
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

            _rootRect =
                _hudRoot.GetComponent<RectTransform>();

            _rootRect.anchorMin =
                Vector2.one;
            _rootRect.anchorMax =
                Vector2.one;
            _rootRect.pivot =
                Vector2.one;
            _rootRect.anchoredPosition =
                new Vector2(
                    -20f,
                    -56f);
            _rootRect.sizeDelta =
                new Vector2(
                    152f,
                    600f);

            _logger?.LogInfo(
                "GK2+ compact grouped tracker HUD initialized.");

            return true;
        }

        private void LayoutGroups(
            IReadOnlyDictionary<string, string> groups)
        {
            string[] order =
            {
                "QUESTS",
                "CRAFTS",
                "ITEMS"
            };

            float y = 0f;

            foreach (string groupName in order)
            {
                if (!groups.TryGetValue(
                        groupName,
                        out string body) ||
                    string.IsNullOrWhiteSpace(body))
                {
                    if (_panels.TryGetValue(
                            groupName,
                            out HudPanel hidden))
                    {
                        hidden.Root.SetActive(false);
                    }

                    continue;
                }

                HudPanel panel =
                    GetOrCreatePanel(
                        groupName);

                panel.Root.SetActive(true);
                panel.Title.text =
                    FormatTitle(
                        groupName);
                panel.Body.text =
                    body.Trim();

                int lineCount =
                    Math.Max(
                        1,
                        panel.Body.text.Split('\n').Length);

                float bodyHeight =
                    Mathf.Clamp(
                        10f + (lineCount * 13.5f),
                        34f,
                        210f);

                float panelHeight =
                    27f + bodyHeight;

                panel.Rect.anchoredPosition =
                    new Vector2(
                        0f,
                        -y);
                panel.Rect.sizeDelta =
                    new Vector2(
                        152f,
                        panelHeight);

                y +=
                    panelHeight + 8f;
            }
        }

        private HudPanel GetOrCreatePanel(
            string groupName)
        {
            if (_panels.TryGetValue(
                    groupName,
                    out HudPanel existing))
            {
                return existing;
            }

            GameObject root =
                new GameObject(
                    groupName + "Panel",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            root.transform.SetParent(
                _hudRoot.transform,
                false);

            RectTransform rect =
                root.GetComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(
                    1f,
                    1f);
            rect.anchorMax =
                new Vector2(
                    1f,
                    1f);
            rect.pivot =
                new Vector2(
                    1f,
                    1f);

            Image background =
                root.GetComponent<Image>();

            background.color =
                new Color(
                    0f,
                    0f,
                    0f,
                    0.68f);
            background.raycastTarget =
                false;

            GameObject titleBar =
                new GameObject(
                    "TitleBar",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            titleBar.transform.SetParent(
                root.transform,
                false);

            RectTransform titleBarRect =
                titleBar.GetComponent<RectTransform>();

            titleBarRect.anchorMin =
                new Vector2(
                    0f,
                    1f);
            titleBarRect.anchorMax =
                new Vector2(
                    1f,
                    1f);
            titleBarRect.pivot =
                new Vector2(
                    0.5f,
                    1f);
            titleBarRect.offsetMin =
                new Vector2(
                    0f,
                    -25f);
            titleBarRect.offsetMax =
                Vector2.zero;

            Image titleBg =
                titleBar.GetComponent<Image>();

            titleBg.color =
                new Color(
                    0.10f,
                    0.08f,
                    0.06f,
                    0.94f);
            titleBg.raycastTarget =
                false;

            TextMeshProUGUI title =
                CreateText(
                    titleBar.transform,
                    "Title",
                    12f,
                    TextAlignmentOptions.Center);

            title.color =
                new Color(
                    1f,
                    0.82f,
                    0.45f,
                    1f);

            TextMeshProUGUI body =
                CreateText(
                    root.transform,
                    "Body",
                    10f,
                    TextAlignmentOptions.TopLeft);

            RectTransform bodyRect =
                body.rectTransform;

            bodyRect.offsetMin =
                new Vector2(
                    9f,
                    6f);
            bodyRect.offsetMax =
                new Vector2(
                    -9f,
                    -29f);

            body.enableWordWrapping =
                true;
            body.richText =
                true;
            body.overflowMode =
                TextOverflowModes.Truncate;

            HudPanel panel =
                new HudPanel
                {
                    Root = root,
                    Rect = rect,
                    Title = title,
                    Body = body
                };

            _panels[groupName] =
                panel;

            return panel;
        }

        private TextMeshProUGUI CreateText(
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
                _textTemplate.font;
            text.fontSharedMaterial =
                _textTemplate.fontSharedMaterial;
            text.spriteAsset =
                _textTemplate.spriteAsset;
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
                _textTemplate.color;
            text.raycastTarget =
                false;
            text.margin =
                Vector4.zero;
            text.characterSpacing =
                0f;
            text.lineSpacing =
                0f;

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

        private static Dictionary<string, string>
            ParseGroups(
                string raw)
        {
            Dictionary<string, string> result =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);

            string current =
                null;

            System.Text.StringBuilder buffer =
                new System.Text.StringBuilder();

            Action flush = () =>
            {
                if (!string.IsNullOrWhiteSpace(current))
                {
                    result[current] =
                        buffer
                            .ToString()
                            .Trim();
                }

                buffer.Clear();
            };

            foreach (string line in
                     (raw ?? string.Empty)
                     .Replace("\r", string.Empty)
                     .Split('\n'))
            {
                string trimmed =
                    line.Trim();

                if (trimmed.StartsWith(
                        "[[",
                        StringComparison.Ordinal) &&
                    trimmed.EndsWith(
                        "]]",
                        StringComparison.Ordinal))
                {
                    flush();

                    current =
                        trimmed.Substring(
                            2,
                            trimmed.Length - 4);

                    continue;
                }

                if (current == null)
                {
                    continue;
                }

                if (buffer.Length > 0)
                {
                    buffer.AppendLine();
                }

                buffer.Append(
                    line);
            }

            flush();

            return result;
        }

        private static string FormatTitle(
            string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                return string.Empty;
            }

            string lower =
                groupName.ToLowerInvariant();

            return char.ToUpperInvariant(lower[0]) +
                   lower.Substring(1);
        }

        private void HideAll()
        {
            foreach (HudPanel panel in
                     _panels.Values)
            {
                panel.Root?.SetActive(false);
            }
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

            _panels.Clear();

            if (gameObject != null)
            {
                Destroy(
                    gameObject);
            }
        }
    }
}
