using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using LazyBearTechnology;
using TMPro;
using HarmonyLib;
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
            public readonly List<GameObject> OverlayRows =
                new List<GameObject>();
        }

        private ManualLogSource _logger;
        private Func<bool> _visibleProvider;
        private Func<string> _textProvider;

        private GameObject _hudRoot;
        private RectTransform _rootRect;
        private TextMeshProUGUI _titleTemplate;
        private TextMeshProUGUI _bodyTemplate;
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
                _titleTemplate != null &&
                _bodyTemplate != null)
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

            _titleTemplate =
                ResolveInventoryHeaderTemplate();

            _bodyTemplate =
                ResolveListBodyTemplate();

            TextMeshProUGUI fallback =
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

            _titleTemplate ??=
                fallback;

            _bodyTemplate ??=
                fallback;

            if (_titleTemplate == null ||
                _bodyTemplate == null)
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

                int lineCount =
                    RenderPanelBody(
                        panel,
                        body.Trim());

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

        private int RenderPanelBody(
            HudPanel panel,
            string body)
        {
            foreach (GameObject row in
                     panel.OverlayRows)
            {
                if (row != null)
                {
                    Destroy(row);
                }
            }

            panel.OverlayRows.Clear();

            string[] lines =
                (body ?? string.Empty)
                    .Replace("\r", string.Empty)
                    .Split('\n');

            List<string> textLines =
                new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed =
                    lines[i].Trim();

                if (TryParseHudItemToken(
                        trimmed,
                        out string itemId,
                        out int current,
                        out int target,
                        out string label,
                        out bool customItem))
                {
                    textLines.Add(" ");

                    GameObject row =
                        CreateHudItemRow(
                            panel,
                            i,
                            itemId,
                            current,
                            target,
                            label,
                            customItem);

                    if (row != null)
                    {
                        panel.OverlayRows.Add(
                            row);
                    }

                    continue;
                }

                textLines.Add(
                    lines[i]);
            }

            panel.Body.text =
                string.Join(
                    "\n",
                    textLines);

            return Math.Max(
                1,
                lines.Length);
        }

        private static bool TryParseHudItemToken(
            string line,
            out string itemId,
            out int current,
            out int target,
            out string label,
            out bool customItem)
        {
            itemId = string.Empty;
            current = 0;
            target = 0;
            label = string.Empty;
            customItem = false;

            if (string.IsNullOrWhiteSpace(line) ||
                !line.StartsWith("[[", StringComparison.Ordinal) ||
                !line.EndsWith("]]", StringComparison.Ordinal))
            {
                return false;
            }

            string payload =
                line.Substring(
                    2,
                    line.Length - 4);

            string[] parts =
                payload.Split('|');

            if (parts.Length < 4 ||
                (!string.Equals(
                     parts[0],
                     "MAT",
                     StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(
                     parts[0],
                     "ITEM",
                     StringComparison.OrdinalIgnoreCase)) ||
                !int.TryParse(
                    parts[2],
                    out current) ||
                !int.TryParse(
                    parts[3],
                    out target))
            {
                return false;
            }

            try
            {
                itemId =
                    Uri.UnescapeDataString(
                        parts[1] ?? string.Empty);

                if (parts.Length > 4)
                {
                    label =
                        Uri.UnescapeDataString(
                            parts[4] ?? string.Empty);
                }
            }
            catch
            {
                return false;
            }

            customItem =
                string.Equals(
                    parts[0],
                    "ITEM",
                    StringComparison.OrdinalIgnoreCase);

            return !string.IsNullOrWhiteSpace(itemId);
        }

        private GameObject CreateHudItemRow(
            HudPanel panel,
            int lineIndex,
            string itemId,
            int current,
            int target,
            string label,
            bool customItem)
        {
            global::ItemDef def =
                global::GameBalance.Me?
                    .GetDataOrNull<global::ItemDef>(
                        itemId);

            if (def == null ||
                string.IsNullOrWhiteSpace(def.iconId))
            {
                return null;
            }

            Sprite sprite =
                LazySingletonSO<EasySpritesCollection>
                    .Instance?
                    .GetSprite(
                        def.iconId,
                        null);

            if (sprite == null)
            {
                return null;
            }

            GameObject row =
                new GameObject(
                    "HudItemRow_" + itemId,
                    typeof(RectTransform));

            row.transform.SetParent(
                panel.Root.transform,
                false);

            RectTransform rowRect =
                row.GetComponent<RectTransform>();

            rowRect.anchorMin =
                new Vector2(
                    0f,
                    1f);
            rowRect.anchorMax =
                new Vector2(
                    0f,
                    1f);
            rowRect.pivot =
                new Vector2(
                    0f,
                    1f);
            rowRect.anchoredPosition =
                new Vector2(
                    9f,
                    -31f - (lineIndex * 13.5f));
            rowRect.sizeDelta =
                new Vector2(
                    134f,
                    14f);

            GameObject iconObject =
                new GameObject(
                    "Icon",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            iconObject.transform.SetParent(
                row.transform,
                false);

            RectTransform iconRect =
                iconObject.GetComponent<RectTransform>();

            iconRect.anchorMin =
                new Vector2(
                    0f,
                    0.5f);
            iconRect.anchorMax =
                new Vector2(
                    0f,
                    0.5f);
            iconRect.pivot =
                new Vector2(
                    0f,
                    0.5f);
            iconRect.anchoredPosition =
                Vector2.zero;
            iconRect.sizeDelta =
                new Vector2(
                    12f,
                    12f);

            Image image =
                iconObject.GetComponent<Image>();

            image.sprite =
                sprite;
            image.preserveAspect =
                true;
            image.raycastTarget =
                false;

            TextMeshProUGUI countText =
                CreateText(
                    _bodyTemplate,
                    row.transform,
                    "Count",
                    9.5f,
                    TextAlignmentOptions.Left);

            RectTransform countRect =
                countText.rectTransform;

            countRect.anchorMin =
                new Vector2(
                    0f,
                    0f);
            countRect.anchorMax =
                new Vector2(
                    1f,
                    1f);
            countRect.offsetMin =
                new Vector2(
                    16f,
                    0f);
            countRect.offsetMax =
                Vector2.zero;

            string count =
                $"{current}/{Math.Max(1, target)}";

            if (customItem &&
                !string.IsNullOrWhiteSpace(label))
            {
                countText.text =
                    $"{label}  {count}";
            }
            else
            {
                countText.text =
                    count;
            }

            countText.color =
                current >= target
                    ? new Color(
                        0.80f,
                        0.92f,
                        0.58f,
                        1f)
                    : new Color(
                        0.96f,
                        0.58f,
                        0.46f,
                        1f);

            return row;
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
                    _titleTemplate,
                    titleBar.transform,
                    "Title",
                    12.5f,
                    TextAlignmentOptions.Center);

            title.color =
                new Color(
                    1f,
                    0.82f,
                    0.45f,
                    1f);

            TextMeshProUGUI body =
                CreateText(
                    _bodyTemplate,
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

        private static TextMeshProUGUI ResolveInventoryHeaderTemplate()
        {
            foreach (InventoryHeaderWidget widget in
                     Resources.FindObjectsOfTypeAll<InventoryHeaderWidget>())
            {
                if (widget == null)
                {
                    continue;
                }

                TextMeshProUGUI header =
                    Traverse.Create(widget)
                        .Field("header")
                        .GetValue<TextMeshProUGUI>();

                if (header != null &&
                    header.font != null)
                {
                    return header;
                }
            }

            return null;
        }

        private static TextMeshProUGUI ResolveListBodyTemplate()
        {
            foreach (UIBuildingWidget widget in
                     Resources.FindObjectsOfTypeAll<UIBuildingWidget>())
            {
                if (widget == null)
                {
                    continue;
                }

                TextMeshProUGUI label =
                    Traverse.Create(widget)
                        .Field("nameLabel")
                        .GetValue<TextMeshProUGUI>();

                if (label != null &&
                    label.font != null)
                {
                    return label;
                }
            }

            return null;
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

                if (string.Equals(
                        trimmed,
                        "[[QUESTS]]",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        trimmed,
                        "[[CRAFTS]]",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        trimmed,
                        "[[ITEMS]]",
                        StringComparison.OrdinalIgnoreCase))
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
