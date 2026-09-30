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

        private sealed class HudMaterialData
        {
            public string ItemId;
            public int Current;
            public int Target;
        }

        private sealed class HudCraftEntryData
        {
            public string Title;
            public readonly List<HudMaterialData> Materials =
                new List<HudMaterialData>();
        }

        private sealed class HudCraftEntryView
        {
            public GameObject Root;
            public RectTransform Rect;
            public TextMeshProUGUI Title;
            public GK2UiPool<HudIngredientView> IngredientPool;
        }

        private sealed class HudQuestEntryData
        {
            public string Title;
            public string Description;
            public readonly List<HudMaterialData> Materials =
                new List<HudMaterialData>();
        }

        private sealed class HudQuestEntryView
        {
            public GameObject Root;
            public RectTransform Rect;
            public TextMeshProUGUI Title;
            public TextMeshProUGUI Description;
            public GK2UiPool<HudIngredientView> IngredientPool;
        }

        private sealed class HudPanel
        {
            public GameObject Root;
            public RectTransform Rect;
            public TextMeshProUGUI Title;
            public TextMeshProUGUI Body;
            public GK2UiPool<HudIngredientView> IngredientPool;
            public GK2UiPool<HudCraftEntryView> CraftEntryPool;
            public GK2UiPool<HudQuestEntryView> QuestEntryPool;
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
                            groupName,
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
            string groupName,
            string body)
        {
            if (string.Equals(
                    groupName,
                    "CRAFTS",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RenderCraftPanel(
                    panel,
                    body);
            }

            if (string.Equals(
                    groupName,
                    "QUESTS",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RenderQuestPanel(
                    panel,
                    body);
            }

            panel.Body.gameObject.SetActive(true);
            panel.CraftEntryPool?.Begin();
            panel.CraftEntryPool?.End();
            panel.QuestEntryPool?.Begin();
            panel.QuestEntryPool?.End();
            panel.IngredientPool?.Begin();

            string[] lines =
                (body ?? string.Empty)
                    .Replace("\r", string.Empty)
                    .Split('\n');

            List<string> textLines =
                new List<string>();

            int materialColumn = 0;
            int materialVisualLine = -1;

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

                        textLines.Add(" ");
                        textLines.Add(string.Empty);
                        textLines.Add(string.Empty);
                    }

                    HudIngredientView materialView =
                        panel.IngredientPool?.Rent();

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

        private float RenderCraftPanel(
            HudPanel panel,
            string body)
        {
            panel.Body.text =
                string.Empty;
            panel.Body.gameObject.SetActive(false);

            panel.IngredientPool?.Begin();
            panel.IngredientPool?.End();
            panel.QuestEntryPool?.Begin();
            panel.QuestEntryPool?.End();

            List<HudCraftEntryData> entries =
                ParseCraftEntries(
                    body);

            panel.CraftEntryPool?.Begin();

            float cursorY =
                GK2UiMetrics.Tracker.BodyTopPadding;

            for (int i = 0; i < entries.Count; i++)
            {
                HudCraftEntryView view =
                    panel.CraftEntryPool?.Rent();

                if (view == null)
                {
                    continue;
                }

                float entryHeight =
                    ConfigureCraftEntry(
                        view,
                        entries[i],
                        cursorY);

                cursorY +=
                    entryHeight + 5f;
            }

            panel.CraftEntryPool?.End();

            if (entries.Count > 0)
            {
                cursorY -= 5f;
            }

            return
                cursorY +
                GK2UiMetrics.Tracker.BodyBottomPadding;
        }

        private float RenderQuestPanel(
            HudPanel panel,
            string body)
        {
            panel.Body.text =
                string.Empty;
            panel.Body.gameObject.SetActive(false);

            panel.IngredientPool?.Begin();
            panel.IngredientPool?.End();
            panel.CraftEntryPool?.Begin();
            panel.CraftEntryPool?.End();

            List<HudQuestEntryData> entries =
                ParseQuestEntries(
                    body);

            panel.QuestEntryPool?.Begin();

            float cursorY =
                GK2UiMetrics.Tracker.BodyTopPadding;

            for (int i = 0; i < entries.Count; i++)
            {
                HudQuestEntryView view =
                    panel.QuestEntryPool?.Rent();

                if (view == null)
                {
                    continue;
                }

                float entryHeight =
                    ConfigureQuestEntry(
                        view,
                        entries[i],
                        cursorY);

                cursorY +=
                    entryHeight + 6f;
            }

            panel.QuestEntryPool?.End();

            if (entries.Count > 0)
            {
                cursorY -= 6f;
            }

            return
                cursorY +
                GK2UiMetrics.Tracker.BodyBottomPadding;
        }

        private static List<HudQuestEntryData> ParseQuestEntries(
            string body)
        {
            List<HudQuestEntryData> entries =
                new List<HudQuestEntryData>();

            HudQuestEntryData current =
                null;

            foreach (string rawLine in
                     (body ?? string.Empty)
                     .Replace("\r", string.Empty)
                     .Split('\n'))
            {
                string line =
                    rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (TryParseQuestToken(
                        line,
                        out string title,
                        out string description))
                {
                    current =
                        new HudQuestEntryData
                        {
                            Title = title,
                            Description = description
                        };

                    entries.Add(
                        current);

                    continue;
                }

                if (TryParseHudItemToken(
                        line,
                        out string itemId,
                        out int currentCount,
                        out int target,
                        out _,
                        out bool customItem) &&
                    !customItem &&
                    current != null)
                {
                    current.Materials.Add(
                        new HudMaterialData
                        {
                            ItemId = itemId,
                            Current = currentCount,
                            Target = target
                        });
                }
            }

            return entries;
        }

        private static bool TryParseQuestToken(
            string line,
            out string title,
            out string description)
        {
            title = string.Empty;
            description = string.Empty;

            if (string.IsNullOrWhiteSpace(line) ||
                !line.StartsWith(
                    "[[QUEST|",
                    StringComparison.OrdinalIgnoreCase) ||
                !line.EndsWith(
                    "]]",
                    StringComparison.Ordinal))
            {
                return false;
            }

            string payload =
                line.Substring(
                    2,
                    line.Length - 4);

            string[] parts =
                payload.Split('|');

            if (parts.Length < 3 ||
                !string.Equals(
                    parts[0],
                    "QUEST",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                title =
                    Uri.UnescapeDataString(
                        parts[1] ?? string.Empty);

                description =
                    Uri.UnescapeDataString(
                        parts[2] ?? string.Empty);
            }
            catch
            {
                title = string.Empty;
                description = string.Empty;
                return false;
            }

            return !string.IsNullOrWhiteSpace(
                title);
        }

        private float ConfigureQuestEntry(
            HudQuestEntryView view,
            HudQuestEntryData data,
            float cursorY)
        {
            if (view == null ||
                data == null)
            {
                return 0f;
            }

            const float titleDescriptionGap = 1f;
            const float descriptionItemsGap = 3f;
            const float entryBottomPadding = 2f;

            float entryWidth =
                GK2UiMetrics.Tracker.PanelWidth -
                (GK2UiMetrics.Tracker.BodyHorizontalPadding * 2f);

            view.Root.SetActive(true);

            view.Rect.anchoredPosition =
                new Vector2(
                    GK2UiMetrics.Tracker.BodyHorizontalPadding,
                    -(GK2UiMetrics.Tracker.TitleHeight +
                      cursorY));

            view.Title.text =
                data.Title ?? string.Empty;

            Vector2 titlePreferred =
                view.Title.GetPreferredValues(
                    view.Title.text,
                    entryWidth,
                    0f);

            float titleHeight =
                Mathf.Max(
                    GK2UiMetrics.Tracker.LineHeight,
                    titlePreferred.y);

            view.Title.rectTransform.anchoredPosition =
                Vector2.zero;
            view.Title.rectTransform.sizeDelta =
                new Vector2(
                    0f,
                    titleHeight);

            bool hasDescription =
                !string.IsNullOrWhiteSpace(
                    data.Description);

            float descriptionHeight =
                0f;

            view.Description.gameObject.SetActive(
                hasDescription);

            if (hasDescription)
            {
                view.Description.text =
                    data.Description;

                Vector2 descriptionPreferred =
                    view.Description.GetPreferredValues(
                        view.Description.text,
                        entryWidth,
                        0f);

                descriptionHeight =
                    Mathf.Max(
                        GK2UiMetrics.Tracker.LineHeight,
                        descriptionPreferred.y);

                view.Description.rectTransform.anchoredPosition =
                    new Vector2(
                        0f,
                        -(titleHeight +
                          titleDescriptionGap));

                view.Description.rectTransform.sizeDelta =
                    new Vector2(
                        0f,
                        descriptionHeight);
            }
            else
            {
                view.Description.text =
                    string.Empty;
            }

            float materialTop =
                titleHeight +
                (hasDescription
                    ? titleDescriptionGap +
                      descriptionHeight +
                      descriptionItemsGap
                    : descriptionItemsGap);

            int materialRows =
                data.Materials.Count == 0
                    ? 0
                    : Mathf.CeilToInt(
                        data.Materials.Count /
                        (float)GK2UiMetrics.Tracker.IngredientColumns);

            float materialHeight =
                materialRows == 0
                    ? 0f
                    : (materialRows *
                       GK2UiMetrics.Tracker.IngredientCellSize) +
                      ((materialRows - 1) *
                       GK2UiMetrics.Tracker.IngredientGap);

            float entryHeight =
                materialTop +
                materialHeight +
                entryBottomPadding;

            view.Rect.sizeDelta =
                new Vector2(
                    entryWidth,
                    entryHeight);

            view.IngredientPool?.Begin();

            for (int i = 0; i < data.Materials.Count; i++)
            {
                HudMaterialData material =
                    data.Materials[i];

                int row =
                    i /
                    GK2UiMetrics.Tracker.IngredientColumns;

                int column =
                    i %
                    GK2UiMetrics.Tracker.IngredientColumns;

                HudIngredientView ingredient =
                    view.IngredientPool?.Rent();

                if (ingredient == null)
                {
                    continue;
                }

                Vector2 position =
                    new Vector2(
                        column *
                        (GK2UiMetrics.Tracker.IngredientCellSize +
                         GK2UiMetrics.Tracker.IngredientGap),
                        -(materialTop +
                          (row *
                           (GK2UiMetrics.Tracker.IngredientCellSize +
                            GK2UiMetrics.Tracker.IngredientGap))));

                ConfigureIngredientView(
                    ingredient,
                    0,
                    0,
                    material.ItemId,
                    material.Current,
                    material.Target,
                    string.Empty,
                    false,
                    position);
            }

            view.IngredientPool?.End();

            return entryHeight;
        }

        private static List<HudCraftEntryData> ParseCraftEntries(
            string body)
        {
            List<HudCraftEntryData> entries =
                new List<HudCraftEntryData>();

            HudCraftEntryData current =
                null;

            foreach (string rawLine in
                     (body ?? string.Empty)
                     .Replace("\r", string.Empty)
                     .Split('\n'))
            {
                string line =
                    rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (TryParseHudItemToken(
                        line,
                        out string itemId,
                        out int currentCount,
                        out int target,
                        out _,
                        out bool customItem) &&
                    !customItem)
                {
                    if (current != null)
                    {
                        current.Materials.Add(
                            new HudMaterialData
                            {
                                ItemId = itemId,
                                Current = currentCount,
                                Target = target
                            });
                    }

                    continue;
                }

                current =
                    new HudCraftEntryData
                    {
                        Title =
                            StripRichTextBold(
                                line)
                    };

                entries.Add(
                    current);
            }

            return entries;
        }

        private static string StripRichTextBold(
            string value)
        {
            string result =
                value ?? string.Empty;

            if (result.StartsWith(
                    "<b>",
                    StringComparison.OrdinalIgnoreCase))
            {
                result =
                    result.Substring(3);
            }

            if (result.EndsWith(
                    "</b>",
                    StringComparison.OrdinalIgnoreCase))
            {
                result =
                    result.Substring(
                        0,
                        result.Length - 4);
            }

            return result;
        }

        private float ConfigureCraftEntry(
            HudCraftEntryView view,
            HudCraftEntryData data,
            float cursorY)
        {
            if (view == null ||
                data == null)
            {
                return 0f;
            }

            const float titleHeight = 14f;
            const float titleToItemsGap = 2f;
            const float entryBottomPadding = 2f;

            int materialRows =
                data.Materials.Count == 0
                    ? 0
                    : Mathf.CeilToInt(
                        data.Materials.Count /
                        (float)GK2UiMetrics.Tracker.IngredientColumns);

            float materialHeight =
                materialRows == 0
                    ? 0f
                    : (materialRows *
                       GK2UiMetrics.Tracker.IngredientCellSize) +
                      ((materialRows - 1) *
                       GK2UiMetrics.Tracker.IngredientGap);

            float entryHeight =
                titleHeight +
                (materialRows > 0
                    ? titleToItemsGap + materialHeight
                    : 0f) +
                entryBottomPadding;

            view.Root.SetActive(true);

            view.Rect.anchoredPosition =
                new Vector2(
                    GK2UiMetrics.Tracker.BodyHorizontalPadding,
                    -(GK2UiMetrics.Tracker.TitleHeight +
                      cursorY));

            view.Rect.sizeDelta =
                new Vector2(
                    GK2UiMetrics.Tracker.PanelWidth -
                    (GK2UiMetrics.Tracker.BodyHorizontalPadding * 2f),
                    entryHeight);

            view.Title.text =
                data.Title ?? string.Empty;

            view.IngredientPool?.Begin();

            for (int i = 0; i < data.Materials.Count; i++)
            {
                HudMaterialData material =
                    data.Materials[i];

                int row =
                    i /
                    GK2UiMetrics.Tracker.IngredientColumns;

                int column =
                    i %
                    GK2UiMetrics.Tracker.IngredientColumns;

                HudIngredientView ingredient =
                    view.IngredientPool?.Rent();

                if (ingredient == null)
                {
                    continue;
                }

                Vector2 position =
                    new Vector2(
                        column *
                        (GK2UiMetrics.Tracker.IngredientCellSize +
                         GK2UiMetrics.Tracker.IngredientGap),
                        -(titleHeight +
                          titleToItemsGap +
                          (row *
                           (GK2UiMetrics.Tracker.IngredientCellSize +
                            GK2UiMetrics.Tracker.IngredientGap))));

                ConfigureIngredientView(
                    ingredient,
                    0,
                    0,
                    material.ItemId,
                    material.Current,
                    material.Target,
                    string.Empty,
                    false,
                    position);
            }

            view.IngredientPool?.End();

            return entryHeight;
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
            Transform parent)
        {
            const float cellSize =
                GK2UiMetrics.Tracker.IngredientCellSize;

            GameObject row =
                new GameObject(
                    "HudIngredient",
                    typeof(RectTransform));

            row.transform.SetParent(
                parent,
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

            if (_theme?.ItemIconMaterial != null)
            {
                iconImage.material =
                    _theme.ItemIconMaterial;
            }

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
            bool customItem,
            Vector2? explicitPosition = null)
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
                explicitPosition ??
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

        private HudQuestEntryView CreateQuestEntryView(
            HudPanel panel)
        {
            GameObject root =
                new GameObject(
                    "HudQuestEntry",
                    typeof(RectTransform));

            root.transform.SetParent(
                panel.Root.transform,
                false);

            RectTransform rect =
                root.GetComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(
                    0f,
                    1f);
            rect.anchorMax =
                new Vector2(
                    0f,
                    1f);
            rect.pivot =
                new Vector2(
                    0f,
                    1f);

            TextMeshProUGUI title =
                CreateText(
                    _bodyTemplate,
                    root.transform,
                    "QuestTitle",
                    GK2UiMetrics.Tracker.BodyFontSize,
                    TextAlignmentOptions.TopLeft);

            title.fontStyle =
                FontStyles.Bold;
            title.textWrappingMode =
                TextWrappingModes.Normal;

            RectTransform titleRect =
                title.rectTransform;

            titleRect.anchorMin =
                new Vector2(
                    0f,
                    1f);
            titleRect.anchorMax =
                new Vector2(
                    1f,
                    1f);
            titleRect.pivot =
                new Vector2(
                    0f,
                    1f);

            TextMeshProUGUI description =
                CreateText(
                    _bodyTemplate,
                    root.transform,
                    "QuestDescription",
                    9f,
                    TextAlignmentOptions.TopLeft);

            description.textWrappingMode =
                TextWrappingModes.Normal;

            RectTransform descriptionRect =
                description.rectTransform;

            descriptionRect.anchorMin =
                new Vector2(
                    0f,
                    1f);
            descriptionRect.anchorMax =
                new Vector2(
                    1f,
                    1f);
            descriptionRect.pivot =
                new Vector2(
                    0f,
                    1f);

            HudQuestEntryView view =
                new HudQuestEntryView
                {
                    Root = root,
                    Rect = rect,
                    Title = title,
                    Description = description
                };

            view.IngredientPool =
                new GK2UiPool<HudIngredientView>(
                    () => CreateIngredientView(root.transform),
                    (ingredient, active) =>
                    {
                        if (ingredient?.Root != null)
                        {
                            ingredient.Root.SetActive(active);
                        }
                    });

            return view;
        }

        private HudCraftEntryView CreateCraftEntryView(
            HudPanel panel)
        {
            GameObject root =
                new GameObject(
                    "HudCraftEntry",
                    typeof(RectTransform));

            root.transform.SetParent(
                panel.Root.transform,
                false);

            RectTransform rect =
                root.GetComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(
                    0f,
                    1f);
            rect.anchorMax =
                new Vector2(
                    0f,
                    1f);
            rect.pivot =
                new Vector2(
                    0f,
                    1f);

            TextMeshProUGUI title =
                CreateText(
                    _bodyTemplate,
                    root.transform,
                    "EntryTitle",
                    GK2UiMetrics.Tracker.BodyFontSize,
                    TextAlignmentOptions.TopLeft);

            RectTransform titleRect =
                title.rectTransform;

            titleRect.anchorMin =
                new Vector2(
                    0f,
                    1f);
            titleRect.anchorMax =
                new Vector2(
                    1f,
                    1f);
            titleRect.pivot =
                new Vector2(
                    0f,
                    1f);
            titleRect.anchoredPosition =
                Vector2.zero;
            titleRect.sizeDelta =
                new Vector2(
                    0f,
                    14f);

            HudCraftEntryView view =
                new HudCraftEntryView
                {
                    Root = root,
                    Rect = rect,
                    Title = title
                };

            view.IngredientPool =
                new GK2UiPool<HudIngredientView>(
                    () => CreateIngredientView(root.transform),
                    (ingredient, active) =>
                    {
                        if (ingredient?.Root != null)
                        {
                            ingredient.Root.SetActive(active);
                        }
                    });

            return view;
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

            GK2UiSectionPanelView section =
                GK2UiSectionPanelBuilder.Create(
                    _hudRoot.transform,
                    _theme,
                    groupName + "Panel",
                    FormatTitle(groupName),
                    GK2UiMetrics.Tracker.PanelWidth,
                    GK2UiMetrics.Tracker.TitleHeight,
                    GK2UiMetrics.Tracker.TitleFontSize,
                    GK2UiMetrics.Tracker.BodyFontSize,
                    GK2UiMetrics.Tracker.BodyHorizontalPadding,
                    GK2UiMetrics.Tracker.BodyTopPadding,
                    GK2UiMetrics.Tracker.BodyBottomPadding);

            HudPanel panel =
                new HudPanel
                {
                    Root = section.Root,
                    Rect = section.Rect,
                    Title = section.Title,
                    Body = section.Body
                };

            panel.IngredientPool =
                new GK2UiPool<HudIngredientView>(
                    () => CreateIngredientView(panel.Root.transform),
                    (view, active) =>
                    {
                        if (view?.Root != null)
                        {
                            view.Root.SetActive(active);
                        }
                    });

            panel.CraftEntryPool =
                new GK2UiPool<HudCraftEntryView>(
                    () => CreateCraftEntryView(panel),
                    (view, active) =>
                    {
                        if (view?.Root != null)
                        {
                            view.Root.SetActive(active);
                        }
                    });

            panel.QuestEntryPool =
                new GK2UiPool<HudQuestEntryView>(
                    () => CreateQuestEntryView(panel),
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
            return GK2UiFactory.CreateText(
                parent,
                name,
                template,
                string.Empty,
                fontSize,
                alignment,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
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
