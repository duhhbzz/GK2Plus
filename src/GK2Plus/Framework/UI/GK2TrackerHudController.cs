using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2Plus.Framework.UI
{
    internal sealed class GK2TrackerHudController : MonoBehaviour
    {
        private const string HudRootName = "GK2PlusNativeTrackerHud";

        private sealed class HudIngredientView
        {
            public GameObject Root;
            public RectTransform Rect;
            public Image Slot;
            public Image Icon;
            public TextMeshProUGUI Count;
            public TextMeshProUGUI Label;
            public string ItemId;
            public bool? EnoughState;
        }

        private sealed class HudPanel
        {
            public GameObject Root;
            public RectTransform Rect;
            public TextMeshProUGUI Title;
            public TextMeshProUGUI Body;
            public GK2UiPool<HudIngredientView> IngredientPool;
        }

        private ManualLogSource _logger;
        private Func<bool> _visibleProvider;
        private Func<string> _textProvider;

        private GameObject _hudRoot;
        private RectTransform _rootRect;
        private GK2UiTheme _theme;
        private TextMeshProUGUI _titleTemplate;
        private TextMeshProUGUI _bodyTemplate;
        private readonly Dictionary<string, HudPanel> _panels =
            new Dictionary<string, HudPanel>(StringComparer.OrdinalIgnoreCase);

        private string _cachedText = string.Empty;
        private string _renderedText = string.Empty;
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

            if (string.Equals(
                    _cachedText,
                    _renderedText,
                    StringComparison.Ordinal))
            {
                return;
            }

            Dictionary<string, string> groups =
                ParseGroups(
                    _cachedText);

            LayoutGroups(
                groups);

            _renderedText =
                _cachedText;
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

            _theme =
                GK2UiTheme.Resolve(
                    _logger);

            _titleTemplate =
                _theme?.TitleTextTemplate;

            _bodyTemplate =
                _theme?.BodyTextTemplate;

            if (_theme == null ||
                _titleTemplate == null ||
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
                    -GK2UiMetrics.Tracker.RootTopOffset);
            _rootRect.sizeDelta =
                new Vector2(
                    GK2UiMetrics.Tracker.PanelWidth,
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

                float bodyHeight =
                    Mathf.Max(
                        34f,
                        RenderPanelBody(
                            panel,
                            body.Trim()));

                float panelHeight =
                    GK2UiMetrics.Tracker.TitleHeight +
                    bodyHeight;

                panel.Rect.anchoredPosition =
                    new Vector2(
                        0f,
                        -y);
                panel.Rect.sizeDelta =
                    new Vector2(
                        GK2UiMetrics.Tracker.PanelWidth,
                        panelHeight);

                y +=
                    panelHeight +
                    GK2UiMetrics.Tracker.SectionGap;
            }
        }

        private float RenderPanelBody(
            HudPanel panel,
            string body)
        {
            panel.IngredientPool?.Begin();

            string[] lines =
                (body ?? string.Empty)
                    .Replace("\r", string.Empty)
                    .Split('\n');

            List<string> textLines =
                new List<string>();

            int materialColumn = 0;
            int materialVisualLine = -1;
            int ingredientViewIndex = 0;

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
                    if (customItem)
                    {
                        materialColumn = 0;
                        materialVisualLine = -1;

                        int visualLineIndex =
                            textLines.Count;

                        textLines.Add(" ");
                        textLines.Add(string.Empty);
                        textLines.Add(string.Empty);

                        HudIngredientView itemView =
                            panel.IngredientPool?.Rent();

                        ingredientViewIndex++;

                        ConfigureIngredientView(
                            itemView,
                            visualLineIndex,
                            0,
                            itemId,
                            current,
                            target,
                            label,
                            true);

                        continue;
                    }

                    if (materialColumn == 0)
                    {
                        materialVisualLine =
                            textLines.Count;

                        // 35px slot + padding fits in three compact text lines.
                        textLines.Add(" ");
                        textLines.Add(string.Empty);
                        textLines.Add(string.Empty);
                    }

                    HudIngredientView materialView =
                        panel.IngredientPool?.Rent();

                    ingredientViewIndex++;

                    ConfigureIngredientView(
                        materialView,
                        materialVisualLine,
                        materialColumn,
                        itemId,
                        current,
                        target,
                        string.Empty,
                        false);

                    materialColumn++;

                    if (materialColumn >=
                        GK2UiMetrics.Tracker.IngredientColumns)
                    {
                        materialColumn = 0;
                        materialVisualLine = -1;
                    }

                    continue;
                }

                materialColumn = 0;
                materialVisualLine = -1;

                textLines.Add(
                    lines[i]);
            }

            panel.Body.text =
                string.Join(
                    "\n",
                    textLines);

            panel.IngredientPool?.End();

            return
                GK2UiMetrics.Tracker.BodyTopPadding +
                GK2UiMetrics.Tracker.BodyBottomPadding +
                (Math.Max(
                    1,
                    textLines.Count) *
                 GK2UiMetrics.Tracker.LineHeight);
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

        private HudIngredientView CreateIngredientView(
            HudPanel panel)
        {
            const float cellSize =
                GK2UiMetrics.Tracker.IngredientCellSize;

            GameObject row =
                new GameObject(
                    "HudIngredient",
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
            rowRect.sizeDelta =
                new Vector2(
                    cellSize,
                    cellSize);

            GameObject slotObject =
                new GameObject(
                    "Slot",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            slotObject.transform.SetParent(
                row.transform,
                false);

            RectTransform slotRect =
                slotObject.GetComponent<RectTransform>();

            slotRect.anchorMin =
                new Vector2(
                    0f,
                    0.5f);
            slotRect.anchorMax =
                new Vector2(
                    0f,
                    0.5f);
            slotRect.pivot =
                new Vector2(
                    0f,
                    0.5f);
            slotRect.anchoredPosition =
                Vector2.zero;
            slotRect.sizeDelta =
                new Vector2(
                    cellSize,
                    cellSize);

            Image slotImage =
                slotObject.GetComponent<Image>();

            slotImage.sprite =
                _theme?.ItemSlotSprite;
            slotImage.type =
                _theme?.ItemSlotSprite != null
                    ? Image.Type.Sliced
                    : Image.Type.Simple;
            slotImage.color =
                _theme?.ItemSlotSprite != null
                    ? Color.white
                    : new Color(
                        0.08f,
                        0.09f,
                        0.11f,
                        0.92f);
            slotImage.raycastTarget =
                false;

            GameObject iconObject =
                new GameObject(
                    "Icon",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            iconObject.transform.SetParent(
                slotObject.transform,
                false);

            RectTransform iconRect =
                iconObject.GetComponent<RectTransform>();

            iconRect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f);
            iconRect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f);
            iconRect.pivot =
                new Vector2(
                    0.5f,
                    0.5f);
            iconRect.anchoredPosition =
                new Vector2(
                    0f,
                    1f);
            iconRect.sizeDelta =
                new Vector2(
                    GK2UiMetrics.Tracker.IngredientIconSize,
                    GK2UiMetrics.Tracker.IngredientIconSize);

            Image iconImage =
                iconObject.GetComponent<Image>();

            iconImage.preserveAspect =
                true;
            iconImage.raycastTarget =
                false;

            TextMeshProUGUI countText =
                CreateText(
                    _theme?.CountTextTemplate ??
                    _bodyTemplate,
                    slotObject.transform,
                    "Count",
                    GK2UiMetrics.Tracker.IngredientCountFontSize,
                    TextAlignmentOptions.BottomRight);

            RectTransform countRect =
                countText.rectTransform;

            countRect.anchorMin =
                Vector2.zero;
            countRect.anchorMax =
                Vector2.one;
            countRect.offsetMin =
                new Vector2(
                    0f,
                    0f);
            countRect.offsetMax =
                new Vector2(
                    -1f,
                    -1f);

            TextMeshProUGUI labelText =
                CreateText(
                    _bodyTemplate,
                    row.transform,
                    "ItemLabel",
                    9.5f,
                    TextAlignmentOptions.Left);

            RectTransform labelRect =
                labelText.rectTransform;

            labelRect.anchorMin =
                new Vector2(
                    0f,
                    0f);
            labelRect.anchorMax =
                new Vector2(
                    1f,
                    1f);
            labelRect.offsetMin =
                new Vector2(
                    40f,
                    0f);
            labelRect.offsetMax =
                Vector2.zero;

            labelText.gameObject.SetActive(
                false);

            return new HudIngredientView
            {
                Root = row,
                Rect = rowRect,
                Slot = slotImage,
                Icon = iconImage,
                Count = countText,
                Label = labelText
            };
        }

        private void ConfigureIngredientView(
            HudIngredientView view,
            int lineIndex,
            int columnIndex,
            string itemId,
            int current,
            int target,
            string label,
            bool customItem)
        {
            if (view == null ||
                global::GameBalance.Me == null ||
                string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            global::ItemDef def =
                global::GameBalance.Me
                    .GetDataOrNull<global::ItemDef>(
                        itemId);

            if (def == null ||
                string.IsNullOrWhiteSpace(def.iconId))
            {
                view.Root.SetActive(false);
                return;
            }

            Sprite iconSprite =
                LazySingletonSO<EasySpritesCollection>
                    .Instance?
                    .GetSprite(
                        def.iconId,
                        null);

            if (iconSprite == null)
            {
                view.Root.SetActive(false);
                return;
            }

            const float cellSize =
                GK2UiMetrics.Tracker.IngredientCellSize;
            const float cellGap =
                GK2UiMetrics.Tracker.IngredientGap;

            view.Root.SetActive(true);

            view.Rect.anchoredPosition =
                new Vector2(
                    8f + (columnIndex * (cellSize + cellGap)),
                    -(GK2UiMetrics.Tracker.TitleHeight +
                      GK2UiMetrics.Tracker.BodyTopPadding) -
                    (lineIndex *
                     GK2UiMetrics.Tracker.LineHeight));

            view.Rect.sizeDelta =
                new Vector2(
                    customItem
                        ? 136f
                        : cellSize,
                    cellSize);

            if (!string.Equals(
                    view.ItemId,
                    itemId,
                    StringComparison.OrdinalIgnoreCase))
            {
                view.ItemId =
                    itemId;

                view.Icon.sprite =
                    iconSprite;

                // GK2 item sprites contain a blue replacement channel. The
                // native UI applies this tint before drawing; doing the same
                // removes the raw blue halo seen in the tracker prototype.
                view.Icon.BlueColorReplace(
                    _theme?.ItemIconTint ??
                    Color.white);
            }

            int safeTarget =
                Math.Max(
                    1,
                    target);

            string count =
                $"{current}/{safeTarget}";

            if (!string.Equals(
                    view.Count.text,
                    count,
                    StringComparison.Ordinal))
            {
                view.Count.text =
                    count;
            }

            bool enough =
                current >= safeTarget;

            if (view.EnoughState != enough)
            {
                view.EnoughState =
                    enough;

                TextStyle nativeStyle =
                    enough
                        ? _theme?.CountNormalStyle
                        : _theme?.CountRedStyle;

                if (nativeStyle != null)
                {
                    nativeStyle.ApplyStyle(
                        view.Count);
                }
                else
                {
                    view.Count.color =
                        enough
                            ? new Color(
                                1f,
                                0.84f,
                                0.18f,
                                1f)
                            : new Color(
                                1f,
                                0.25f,
                                0.24f,
                                1f);
                }
            }

            view.Count.fontSize =
                GK2UiMetrics.Tracker.IngredientCountFontSize;
            view.Count.alignment =
                TextAlignmentOptions.BottomRight;

            view.Label.gameObject.SetActive(
                customItem &&
                !string.IsNullOrWhiteSpace(label));

            if (view.Label.gameObject.activeSelf)
            {
                view.Label.text =
                    label;
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
                _theme?.HudBackground ??
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
                    -GK2UiMetrics.Tracker.TitleHeight);
            titleBarRect.offsetMax =
                Vector2.zero;

            Image titleBg =
                titleBar.GetComponent<Image>();

            titleBg.color =
                _theme?.HudTitleBackground ??
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
                    GK2UiMetrics.Tracker.TitleFontSize,
                    TextAlignmentOptions.Center);

            title.color =
                _theme?.AccentText ??
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
                    GK2UiMetrics.Tracker.BodyFontSize,
                    TextAlignmentOptions.TopLeft);

            RectTransform bodyRect =
                body.rectTransform;

            bodyRect.offsetMin =
                new Vector2(
                    GK2UiMetrics.Tracker.BodyHorizontalPadding,
                    GK2UiMetrics.Tracker.BodyBottomPadding);
            bodyRect.offsetMax =
                new Vector2(
                    -GK2UiMetrics.Tracker.BodyHorizontalPadding,
                    -(GK2UiMetrics.Tracker.TitleHeight +
                      GK2UiMetrics.Tracker.BodyTopPadding));

            body.textWrappingMode =
                TextWrappingModes.Normal;
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

            panel.IngredientPool =
                new GK2UiPool<HudIngredientView>(
                    () => CreateIngredientView(panel),
                    (view, active) =>
                    {
                        if (view?.Root != null)
                        {
                            view.Root.SetActive(active);
                        }
                    });

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

            _renderedText =
                string.Empty;
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
            _theme = null;

            if (gameObject != null)
            {
                Destroy(
                    gameObject);
            }
        }
    }
}
