using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using GK2Plus.Features.Tracking;
using HarmonyLib;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2Plus.Framework.UI
{
    internal sealed class GK2QuestJournalController
    {
        private enum JournalFilter
        {
            Active,
            Completed
        }

        private sealed class QuestRowView
        {
            public GameObject Root;
            public RectTransform Rect;
            public Image Background;
            public Image Icon;
            public TextMeshProUGUI Title;
            public TextMeshProUGUI Description;
            public TextMeshProUGUI Status;
            public GK2UiProgressBarView Progress;
            public Button SelectButton;
            public Button PinButton;
            public Image PinIcon;
            public QuestData Quest;
        }

        private sealed class QuestGroupView
        {
            public GameObject Root;
            public RectTransform Rect;
            public Image Background;
            public Image HeaderBackground;
            public Image Portrait;
            public TextMeshProUGUI Title;
            public TextMeshProUGUI Count;
            public TextMeshProUGUI Chevron;
            public Button ToggleButton;
            public string GroupKey;
        }

        private readonly ManualLogSource _logger;

        private QuestTreePageWidget _host;
        private LazyScrollRect _nativeScrollRect;
        private GK2UiTheme _theme;

        private GameObject _root;
        private RectTransform _rootRect;

        private Button _activeFilterButton;
        private Button _completedFilterButton;
        private TextMeshProUGUI _activeFilterLabel;
        private TextMeshProUGUI _completedFilterLabel;

        private ScrollRect _questScroll;
        private RectTransform _questListContent;
        private GK2UiPool<QuestGroupView> _questGroupPool;
        private GK2UiPool<QuestRowView> _questRowPool;
        private readonly HashSet<string> _expandedQuestGroups =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private ScrollRect _detailScroll;
        private RectTransform _detailContent;
        private TextMeshProUGUI _detailTitle;
        private TextMeshProUGUI _detailStatus;
        private TextMeshProUGUI _detailDescription;
        private TextMeshProUGUI _detailPaneHeaderTitle;
        private GameObject _detailSummaryPanel;
        private RectTransform _detailSummaryRect;
        private RectTransform _detailPortraitRect;
        private RectTransform _descriptionHeaderRect;
        private RectTransform _objectivesHeaderRect;
        private Image _detailIcon;
        private Button _trackButton;
        private TextMeshProUGUI _trackButtonLabel;
        private GK2UiPool<GK2UiItemRequirementView> _objectivePool;

        private JournalFilter _filter =
            JournalFilter.Active;

        private string _selectedQuestId =
            string.Empty;

        internal GK2QuestJournalController(
            ManualLogSource logger)
        {
            _logger =
                logger;
        }

        internal bool TryShow(
            QuestTreePageWidget host,
            string focusOnQuest)
        {
            if (host == null ||
                MainGame.Instance?.GameSave?.questSystemData?.questCollection == null)
            {
                return false;
            }

            try
            {
                _host =
                    host;

                _theme =
                    GK2UiTheme.Resolve(
                        _logger);

                if (_theme == null ||
                    _theme.BodyTextTemplate == null ||
                    _theme.TitleTextTemplate == null)
                {
                    return false;
                }

                ResolveNativeScrollRect(
                    host);

                if (_nativeScrollRect != null)
                {
                    _nativeScrollRect.gameObject.SetActive(
                        false);
                }

                EnsureUi(
                    host);

                if (_root == null)
                {
                    return false;
                }

                _root.SetActive(
                    true);
                _root.transform.SetAsLastSibling();

                if (!string.IsNullOrWhiteSpace(
                        focusOnQuest))
                {
                    _selectedQuestId =
                        focusOnQuest;
                }

                Refresh();

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    $"Quest Journal failed to open: {ex}");

                RestoreNativeTree();
                return false;
            }
        }

        internal void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(
                    false);
            }
        }

        internal void RestoreNativeTree()
        {
            Hide();

            if (_nativeScrollRect != null)
            {
                _nativeScrollRect.gameObject.SetActive(
                    true);
            }
        }

        internal void RefreshIfVisible()
        {
            if (_root != null &&
                _root.activeInHierarchy)
            {
                Refresh();
            }
        }

        private void ResolveNativeScrollRect(
            QuestTreePageWidget host)
        {
            if (_nativeScrollRect != null)
            {
                return;
            }

            _nativeScrollRect =
                Traverse.Create(host)
                    .Field("scrollRect")
                    .GetValue<LazyScrollRect>();
        }

        private void EnsureUi(
            QuestTreePageWidget host)
        {
            if (_root != null &&
                _root.transform.parent ==
                    host.transform)
            {
                return;
            }

            if (_root != null)
            {
                UnityEngine.Object.Destroy(
                    _root);
            }

            _root =
                GK2UiFactory.CreateImage(
                    host.transform,
                    "GK2PlusQuestJournal",
                    _theme.ContentStoneSprite,
                    _theme.ContentStoneSprite != null
                        ? Image.Type.Tiled
                        : Image.Type.Simple,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    _theme.ContentStoneSprite != null
                        ? Color.white
                        : _theme.ContentBackground,
                    true);

            _rootRect =
                _root.GetComponent<RectTransform>();

            _rootRect.offsetMin =
                Vector2.zero;
            _rootRect.offsetMax =
                Vector2.zero;

            BuildHeader();
            BuildQuestList();
            BuildDetails();

            _logger?.LogInfo(
                "GK2+ Quest Journal mounted into native Quests tab.");
        }

        private void BuildHeader()
        {
            // Intentionally empty. Native-style section headers are built
            // into the left and right panes, matching Character/Inspirations.
        }

        private void BuildQuestList()
        {
            float left =
                GK2UiMetrics.QuestJournal.OuterPadding;

            float width =
                GK2UiMetrics.QuestJournal.LeftPaneWidth;

            GameObject pane =
                GK2UiFactory.CreateImage(
                    _root.transform,
                    "QuestListPane",
                    _theme.ContentCellSprite,
                    _theme.ContentCellSprite != null
                        ? Image.Type.Sliced
                        : Image.Type.Simple,
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    Vector2.zero,
                    Vector2.zero,
                    _theme.ContentCellSprite != null
                        ? Color.white
                        : _theme.RowBackground);

            RectTransform paneRect =
                pane.GetComponent<RectTransform>();

            paneRect.offsetMin =
                new Vector2(
                    left,
                    GK2UiMetrics.QuestJournal.OuterPadding);
            paneRect.offsetMax =
                new Vector2(
                    left + width,
                    -GK2UiMetrics.QuestJournal.OuterPadding);

            GK2UiSectionHeaderView header =
                GK2UiSectionHeaderBuilder.Create(
                    pane.transform,
                    _theme,
                    "QuestJournalHeader",
                    "Quest Journal",
                    GK2UiMetrics.QuestJournal.SectionHeaderHeight);

            header.Rect.offsetMin =
                new Vector2(
                    0f,
                    -GK2UiMetrics.QuestJournal.SectionHeaderHeight);
            header.Rect.offsetMax =
                Vector2.zero;

            float filterTop =
                GK2UiMetrics.QuestJournal.SectionHeaderHeight +
                3f;

            float filterWidth =
                width - 10f;

            GameObject filterStrip =
                GK2UiFactory.CreateImage(
                    pane.transform,
                    "QuestFilterStrip",
                    _theme.MainWindowHeaderSprite,
                    _theme.MainWindowHeaderSprite != null
                        ? Image.Type.Sliced
                        : Image.Type.Simple,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(
                        5f,
                        -filterTop),
                    new Vector2(
                        filterWidth,
                        GK2UiMetrics.QuestJournal.FilterHeight),
                    _theme.MainWindowHeaderSprite != null
                        ? Color.white
                        : _theme.HeaderBackground,
                    false);

            float gap =
                GK2UiMetrics.QuestJournal.FilterGap;

            float filterButtonWidth =
                (filterWidth - gap) /
                2f;

            _activeFilterButton =
                GK2UiFactory.CreateNativeWindowTab(
                    filterStrip.transform,
                    "ActiveFilter",
                    _theme,
                    "Active",
                    new Vector2(
                        -(filterButtonWidth + gap) / 2f,
                        0f),
                    new Vector2(
                        filterButtonWidth,
                        GK2UiMetrics.QuestJournal.FilterHeight),
                    true,
                    () =>
                    {
                        _filter =
                            JournalFilter.Active;
                        _selectedQuestId =
                            string.Empty;
                        Refresh();
                    });

            _activeFilterLabel =
                _activeFilterButton
                    .GetComponentInChildren<TextMeshProUGUI>(
                        true);

            _completedFilterButton =
                GK2UiFactory.CreateNativeWindowTab(
                    filterStrip.transform,
                    "CompletedFilter",
                    _theme,
                    "Completed",
                    new Vector2(
                        (filterButtonWidth + gap) / 2f,
                        0f),
                    new Vector2(
                        filterButtonWidth,
                        GK2UiMetrics.QuestJournal.FilterHeight),
                    false,
                    () =>
                    {
                        _filter =
                            JournalFilter.Completed;
                        _selectedQuestId =
                            string.Empty;
                        Refresh();
                    });

            _completedFilterLabel =
                _completedFilterButton
                    .GetComponentInChildren<TextMeshProUGUI>(
                        true);

            GameObject viewport =
                GK2UiFactory.CreateRect(
                    pane.transform,
                    "QuestViewport",
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero);

            RectTransform viewportRect =
                viewport.GetComponent<RectTransform>();

            float listTop =
                filterTop +
                GK2UiMetrics.QuestJournal.FilterHeight +
                4f;

            viewportRect.offsetMin =
                new Vector2(
                    5f,
                    5f);
            viewportRect.offsetMax =
                new Vector2(
                    -5f,
                    -listTop);

            viewport.AddComponent<RectMask2D>();

            GameObject content =
                GK2UiFactory.CreateRect(
                    viewport.transform,
                    "QuestListContent",
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f),
                    Vector2.zero,
                    Vector2.zero);

            _questListContent =
                content.GetComponent<RectTransform>();

            _questListContent.offsetMin =
                Vector2.zero;
            _questListContent.offsetMax =
                Vector2.zero;

            _questScroll =
                pane.AddComponent<ScrollRect>();

            _questScroll.viewport =
                viewportRect;
            _questScroll.content =
                _questListContent;
            _questScroll.horizontal =
                false;
            _questScroll.vertical =
                true;
            _questScroll.movementType =
                ScrollRect.MovementType.Clamped;
            _questScroll.inertia =
                false;
            _questScroll.scrollSensitivity =
                28f;

            _questGroupPool =
                new GK2UiPool<QuestGroupView>(
                    CreateQuestGroup,
                    (view, active) =>
                    {
                        if (view?.Root != null)
                        {
                            view.Root.SetActive(
                                active);
                        }
                    });

            _questRowPool =
                new GK2UiPool<QuestRowView>(
                    CreateQuestRow,
                    (view, active) =>
                    {
                        if (view?.Root != null)
                        {
                            view.Root.SetActive(
                                active);
                        }
                    });
        }

        private QuestGroupView CreateQuestGroup()
        {
            GameObject root =
                GK2UiFactory.CreateImage(
                    _questListContent,
                    "QuestGroup",
                    _theme.ContentCellSprite,
                    _theme.ContentCellSprite != null
                        ? Image.Type.Sliced
                        : Image.Type.Simple,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f),
                    Vector2.zero,
                    new Vector2(
                        0f,
                        GK2UiMetrics.QuestJournal.QuestGroupHeight),
                    _theme.ContentCellSprite != null
                        ? Color.white
                        : _theme.RowBackground,
                    false);

            RectTransform rect =
                root.GetComponent<RectTransform>();

            Image background =
                root.GetComponent<Image>();

            background.raycastTarget =
                false;

            float headerHeight =
                GK2UiMetrics.QuestJournal.QuestGroupHeight;

            Image headerBackground =
                GK2UiFactory.CreateImage(
                    root.transform,
                    "GroupHeader",
                    _theme.SectionHeaderInactiveSprite ??
                    _theme.SectionHeaderSprite,
                    (_theme.SectionHeaderInactiveSprite ??
                     _theme.SectionHeaderSprite) != null
                        ? Image.Type.Sliced
                        : Image.Type.Simple,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f),
                    Vector2.zero,
                    new Vector2(
                        0f,
                        headerHeight),
                    (_theme.SectionHeaderInactiveSprite ??
                     _theme.SectionHeaderSprite) != null
                        ? Color.white
                        : _theme.HeaderBackground,
                    false)
                .GetComponent<Image>();

            GameObject hitArea =
                GK2UiFactory.CreateImage(
                    root.transform,
                    "GroupToggle",
                    null,
                    Image.Type.Simple,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f),
                    Vector2.zero,
                    new Vector2(
                        0f,
                        headerHeight),
                    new Color(
                        1f,
                        1f,
                        1f,
                        0.001f),
                    true);

            Button toggle =
                hitArea.AddComponent<Button>();

            toggle.targetGraphic =
                hitArea.GetComponent<Image>();
            toggle.transition =
                Selectable.Transition.ColorTint;

            ColorBlock toggleColors =
                toggle.colors;

            toggleColors.normalColor =
                Color.white;
            toggleColors.highlightedColor =
                new Color(
                    1f,
                    0.90f,
                    0.72f,
                    1f);
            toggleColors.pressedColor =
                new Color(
                    1f,
                    0.78f,
                    0.45f,
                    1f);
            toggleColors.selectedColor =
                Color.white;
            toggleColors.fadeDuration =
                0.05f;

            toggle.colors =
                toggleColors;

            float pad =
                4f;

            float portraitSize =
                GK2UiMetrics.QuestJournal.QuestGroupIconSize;

            GK2UiPortraitView portraitView =
                GK2UiPortraitBuilder.CreateStonePortrait(
                    root.transform,
                    _theme,
                    "NpcPortrait",
                    new Vector2(
                        pad,
                        -5f),
                    portraitSize,
                    1.45f);

            Image portrait =
                portraitView.Portrait;

            float textX =
                pad + portraitSize + 8f;

            TextMeshProUGUI title =
                GK2UiFactory.CreateNativeTemplateText(
                    root.transform,
                    "GroupTitle",
                    _theme.MainWindowTabTextTemplate ??
                    _theme.TitleTextTemplate ??
                    _theme.BodyTextTemplate,
                    string.Empty,
                    12.5f,
                    TextAlignmentOptions.MidlineLeft,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(
                        textX,
                        -7f),
                    new Vector2(
                        -86f,
                        22f));

            title.color =
                Color.white;
            title.textWrappingMode =
                TextWrappingModes.NoWrap;
            title.overflowMode =
                TextOverflowModes.Ellipsis;

            TextMeshProUGUI count =
                GK2UiFactory.CreateText(
                    root.transform,
                    "GroupCount",
                    _theme.BodyTextTemplate,
                    string.Empty,
                    8.5f,
                    TextAlignmentOptions.MidlineLeft,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(
                        textX,
                        -29f),
                    new Vector2(
                        -86f,
                        15f));

            count.color =
                new Color(
                    0.72f,
                    0.68f,
                    0.61f,
                    1f);

            TextMeshProUGUI chevron =
                GK2UiFactory.CreateNativeTemplateText(
                    root.transform,
                    "Chevron",
                    _theme.MainWindowTabTextTemplate ??
                    _theme.TitleTextTemplate ??
                    _theme.BodyTextTemplate,
                    "-",
                    14f,
                    TextAlignmentOptions.Center,
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(
                        -10f,
                        -15f),
                    new Vector2(
                        20f,
                        20f));

            chevron.color =
                Color.white;

            return new QuestGroupView
            {
                Root = root,
                Rect = rect,
                Background = background,
                HeaderBackground = headerBackground,
                Portrait = portrait,
                Title = title,
                Count = count,
                Chevron = chevron,
                ToggleButton = toggle
            };
        }

        private QuestRowView CreateQuestRow()
        {
            GameObject root =
                GK2UiFactory.CreateImage(
                    _questListContent,
                    "QuestRow",
                    null,
                    Image.Type.Simple,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f),
                    Vector2.zero,
                    new Vector2(
                        0f,
                        GK2UiMetrics.QuestJournal.QuestRowHeight),
                    Color.clear,
                    true);

            RectTransform rect =
                root.GetComponent<RectTransform>();

            Image background =
                root.GetComponent<Image>();

            Button select =
                root.AddComponent<Button>();

            select.targetGraphic =
                background;
            select.transition =
                Selectable.Transition.ColorTint;

            ColorBlock selectColors =
                select.colors;

            selectColors.normalColor =
                Color.white;
            selectColors.highlightedColor =
                new Color(
                    1f,
                    0.92f,
                    0.76f,
                    1f);
            selectColors.pressedColor =
                new Color(
                    1f,
                    0.82f,
                    0.55f,
                    1f);
            selectColors.selectedColor =
                Color.white;
            selectColors.fadeDuration =
                0.05f;

            select.colors =
                selectColors;

            float pad =
                GK2UiMetrics.QuestJournal.QuestCardPadding;

            float iconSize =
                GK2UiMetrics.QuestJournal.QuestIconSize;

            GK2UiPortraitView iconView =
                GK2UiPortraitBuilder.CreateStonePortrait(
                    root.transform,
                    _theme,
                    "QuestPortrait",
                    new Vector2(
                        pad,
                        -6f),
                    iconSize,
                    1.15f);

            Image icon =
                iconView.Portrait;

            float textX =
                pad + iconSize + 7f;

            TextMeshProUGUI title =
                GK2UiFactory.CreateNativeTemplateText(
                    root.transform,
                    "QuestTitle",
                    _theme.MainWindowTabTextTemplate ??
                    _theme.TitleTextTemplate ??
                    _theme.BodyTextTemplate,
                    string.Empty,
                    10.75f,
                    TextAlignmentOptions.MidlineLeft,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(
                        textX,
                        -4f),
                    new Vector2(
                        -112f,
                        18f));

            title.textWrappingMode =
                TextWrappingModes.NoWrap;
            title.overflowMode =
                TextOverflowModes.Ellipsis;
            title.color =
                Color.white;

            TextMeshProUGUI status =
                GK2UiFactory.CreateText(
                    root.transform,
                    "QuestStatus",
                    _theme.BodyTextTemplate,
                    string.Empty,
                    7.75f,
                    TextAlignmentOptions.Right,
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(
                        -29f,
                        -4f),
                    new Vector2(
                        76f,
                        16f));

            status.color =
                new Color(
                    0.72f,
                    0.68f,
                    0.62f,
                    1f);

            GameObject pinObject =
                GK2UiFactory.CreateImage(
                    root.transform,
                    "PinButton",
                    UnifiedTrackerFeature.GetTrackerPinSpriteForExternalUi(),
                    Image.Type.Simple,
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(
                        -6f,
                        -4f),
                    new Vector2(
                        18f,
                        18f),
                    Color.white,
                    true);

            Image pinIcon =
                pinObject.GetComponent<Image>();

            pinIcon.preserveAspect =
                true;

            Button pinButton =
                pinObject.AddComponent<Button>();

            pinButton.targetGraphic =
                pinIcon;
            pinButton.transition =
                Selectable.Transition.ColorTint;

            TextMeshProUGUI description =
                GK2UiFactory.CreateText(
                    root.transform,
                    "QuestDescription",
                    _theme.BodyTextTemplate,
                    string.Empty,
                    8.25f,
                    TextAlignmentOptions.TopLeft,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(
                        textX,
                        -24f),
                    new Vector2(
                        -(textX + 7f),
                        25f));

            description.textWrappingMode =
                TextWrappingModes.Normal;
            description.overflowMode =
                TextOverflowModes.Truncate;
            description.maxVisibleLines =
                2;
            description.color =
                new Color(
                    0.76f,
                    0.72f,
                    0.66f,
                    1f);

            GK2UiProgressBarView progress =
                GK2UiFactory.CreateNativeProgressBar(
                    root.transform,
                    "QuestProgress",
                    _theme,
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(
                        (textX - 7f) / 2f,
                        4f),
                    new Vector2(
                        -(textX + 7f),
                        GK2UiMetrics.QuestJournal.QuestProgressHeight),
                    0f,
                    1f,
                    string.Empty);

            progress.Root.SetActive(
                false);

            if (_theme.DividerSprite != null)
            {
                GK2UiFactory.CreateImage(
                    root.transform,
                    "RowDivider",
                    _theme.DividerSprite,
                    Image.Type.Sliced,
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0.5f, 0f),
                    Vector2.zero,
                    new Vector2(
                        -12f,
                        4f),
                    Color.white,
                    false);
            }

            return new QuestRowView
            {
                Root = root,
                Rect = rect,
                Background = background,
                Icon = icon,
                Title = title,
                Description = description,
                Status = status,
                Progress = progress,
                SelectButton = select,
                PinButton = pinButton,
                PinIcon = pinIcon
            };
        }

        private void BuildDetails()
        {
            float left =
                GK2UiMetrics.QuestJournal.OuterPadding +
                GK2UiMetrics.QuestJournal.LeftPaneWidth +
                GK2UiMetrics.QuestJournal.PaneGap;

            GameObject pane =
                GK2UiFactory.CreateImage(
                    _root.transform,
                    "QuestDetailsPane",
                    _theme.ContentCellSprite,
                    _theme.ContentCellSprite != null
                        ? Image.Type.Sliced
                        : Image.Type.Simple,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    _theme.ContentCellSprite != null
                        ? Color.white
                        : _theme.RowBackground);

            RectTransform paneRect =
                pane.GetComponent<RectTransform>();

            paneRect.offsetMin =
                new Vector2(
                    left,
                    GK2UiMetrics.QuestJournal.OuterPadding);
            paneRect.offsetMax =
                new Vector2(
                    -GK2UiMetrics.QuestJournal.OuterPadding,
                    -GK2UiMetrics.QuestJournal.OuterPadding);

            GK2UiSectionHeaderView paneHeader =
                GK2UiSectionHeaderBuilder.Create(
                    pane.transform,
                    _theme,
                    "QuestDetailsHeader",
                    "Quest Details",
                    GK2UiMetrics.QuestJournal.SectionHeaderHeight);

            paneHeader.Rect.offsetMin =
                new Vector2(
                    0f,
                    -GK2UiMetrics.QuestJournal.SectionHeaderHeight);
            paneHeader.Rect.offsetMax =
                Vector2.zero;

            _detailPaneHeaderTitle =
                paneHeader.Title;

            GameObject viewport =
                GK2UiFactory.CreateRect(
                    pane.transform,
                    "DetailsViewport",
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero);

            RectTransform viewportRect =
                viewport.GetComponent<RectTransform>();

            viewportRect.offsetMin =
                new Vector2(
                    GK2UiMetrics.QuestJournal.DetailPadding,
                    GK2UiMetrics.QuestJournal.DetailPadding);
            viewportRect.offsetMax =
                new Vector2(
                    -GK2UiMetrics.QuestJournal.DetailPadding,
                    -(GK2UiMetrics.QuestJournal.SectionHeaderHeight +
                      GK2UiMetrics.QuestJournal.DetailPadding));

            viewport.AddComponent<RectMask2D>();

            GameObject content =
                GK2UiFactory.CreateRect(
                    viewport.transform,
                    "DetailsContent",
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f),
                    Vector2.zero,
                    Vector2.zero);

            _detailContent =
                content.GetComponent<RectTransform>();

            _detailScroll =
                pane.AddComponent<ScrollRect>();

            _detailScroll.viewport =
                viewportRect;
            _detailScroll.content =
                _detailContent;
            _detailScroll.horizontal =
                false;
            _detailScroll.vertical =
                true;
            _detailScroll.movementType =
                ScrollRect.MovementType.Clamped;
            _detailScroll.inertia =
                false;
            _detailScroll.scrollSensitivity =
                28f;

            _detailSummaryPanel =
                GK2UiFactory.CreateImage(
                    _detailContent,
                    "QuestSummary",
                    _theme.ContentCellSprite,
                    _theme.ContentCellSprite != null
                        ? Image.Type.Sliced
                        : Image.Type.Simple,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f),
                    Vector2.zero,
                    new Vector2(
                        0f,
                        GK2UiMetrics.QuestJournal.DetailSummaryHeight),
                    _theme.ContentCellSprite != null
                        ? Color.white
                        : _theme.RowBackground,
                    false);

            _detailSummaryRect =
                _detailSummaryPanel.GetComponent<RectTransform>();

            GK2UiPortraitView detailPortrait =
                GK2UiPortraitBuilder.CreateStonePortrait(
                    _detailSummaryPanel.transform,
                    _theme,
                    "DetailQuestPortrait",
                    new Vector2(
                        8f,
                        -8f),
                    GK2UiMetrics.QuestJournal.DetailPortraitSize,
                    1.25f);

            _detailIcon =
                detailPortrait.Portrait;
            _detailPortraitRect =
                detailPortrait.Rect;

            float summaryTextOffset =
                8f +
                GK2UiMetrics.QuestJournal.DetailPortraitSize +
                GK2UiMetrics.QuestJournal.DetailPortraitGap;

            _detailTitle =
                GK2UiFactory.CreateNativeTemplateText(
                    _detailSummaryPanel.transform,
                    "DetailTitle",
                    _theme.MainWindowTabTextTemplate ??
                    _theme.TitleTextTemplate,
                    string.Empty,
                    GK2UiMetrics.QuestJournal.DetailTitleSize,
                    TextAlignmentOptions.TopLeft,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(
                        summaryTextOffset,
                        -8f),
                    new Vector2(
                        -(summaryTextOffset + 112f),
                        36f));

            _detailTitle.color =
                Color.white;
            _detailTitle.textWrappingMode =
                TextWrappingModes.Normal;

            _detailStatus =
                GK2UiFactory.CreateText(
                    _detailSummaryPanel.transform,
                    "DetailStatus",
                    _theme.BodyTextTemplate,
                    string.Empty,
                    GK2UiMetrics.QuestJournal.DetailStatusSize,
                    TextAlignmentOptions.TopLeft,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(
                        summaryTextOffset,
                        -46f),
                    new Vector2(
                        -(summaryTextOffset + 112f),
                        18f));

            _detailStatus.color =
                new Color(
                    0.72f,
                    0.68f,
                    0.61f,
                    1f);

            _trackButton =
                GK2UiFactory.CreateNativeRedButton(
                    _detailSummaryPanel.transform,
                    "TrackQuestButton",
                    _theme,
                    "Track Quest",
                    Vector2.zero,
                    new Vector2(
                        104f,
                        GK2UiMetrics.Native.RedButtonSize.y),
                    () =>
                    {
                        QuestData quest =
                            ResolveSelectedQuest();

                        if (quest == null)
                        {
                            return;
                        }

                        UnifiedTrackerFeature.ToggleQuestFromExternalUi(
                            quest);

                        Refresh();
                    });

            RectTransform trackRect =
                _trackButton.GetComponent<RectTransform>();

            trackRect.anchorMin =
                new Vector2(1f, 0f);
            trackRect.anchorMax =
                new Vector2(1f, 0f);
            trackRect.pivot =
                new Vector2(1f, 0f);
            trackRect.anchoredPosition =
                new Vector2(
                    -8f,
                    8f);

            _trackButtonLabel =
                _trackButton
                    .GetComponentInChildren<TextMeshProUGUI>(
                        true);

            GK2UiSectionHeaderView descriptionHeader =
                GK2UiSectionHeaderBuilder.Create(
                    _detailContent,
                    _theme,
                    "DescriptionHeader",
                    "Description",
                    20f);

            _descriptionHeaderRect =
                descriptionHeader.Rect;

            _detailDescription =
                GK2UiFactory.CreateText(
                    _detailContent,
                    "DetailDescription",
                    _theme.BodyTextTemplate,
                    string.Empty,
                    GK2UiMetrics.QuestJournal.DetailBodySize,
                    TextAlignmentOptions.TopLeft,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f),
                    Vector2.zero,
                    new Vector2(
                        0f,
                        80f));

            _detailDescription.textWrappingMode =
                TextWrappingModes.Normal;
            _detailDescription.color =
                new Color(
                    0.82f,
                    0.78f,
                    0.71f,
                    1f);

            GK2UiSectionHeaderView objectivesHeader =
                GK2UiSectionHeaderBuilder.Create(
                    _detailContent,
                    _theme,
                    "ObjectivesHeader",
                    "Objectives",
                    20f);

            _objectivesHeaderRect =
                objectivesHeader.Rect;

            _objectivePool =
                new GK2UiPool<GK2UiItemRequirementView>(
                    () =>
                        GK2UiItemRequirementBuilder.Create(
                            _detailContent,
                            _theme,
                            GK2UiMetrics.QuestJournal.ObjectiveCellSize),
                    (view, active) =>
                    {
                        if (view?.Root != null)
                        {
                            view.Root.SetActive(
                                active);
                        }
                    });
        }

        private void Refresh()
        {
            List<QuestData> allVisible =
                GetVisibleQuests();

            int activeCount =
                allVisible.Count(quest =>
                    quest.IsActiveQuest);

            int completedCount =
                allVisible.Count(quest =>
                    quest.status ==
                    QuestStatus.Completed);

            if (_activeFilterLabel != null)
            {
                _activeFilterLabel.text =
                    $"Active ({activeCount})";
            }

            if (_completedFilterLabel != null)
            {
                _completedFilterLabel.text =
                    $"Completed ({completedCount})";
            }

            ApplyFilterStyle(
                _activeFilterButton,
                _filter == JournalFilter.Active);

            ApplyFilterStyle(
                _completedFilterButton,
                _filter == JournalFilter.Completed);

            List<QuestData> filtered =
                allVisible
                    .Where(quest =>
                        _filter ==
                        JournalFilter.Active
                            ? quest.IsActiveQuest
                            : quest.status ==
                              QuestStatus.Completed)
                    .OrderBy(GetQuestGroupDisplayName)
                    .ThenBy(quest =>
                        quest.Definition?.phase ??
                        0)
                    .ThenBy(GetQuestTitle)
                    .ToList();

            if (filtered.Count == 0)
            {
                _selectedQuestId =
                    string.Empty;
            }
            else if (!filtered.Any(quest =>
                         string.Equals(
                             quest.id,
                             _selectedQuestId,
                             StringComparison.OrdinalIgnoreCase)))
            {
                _selectedQuestId =
                    filtered[0].id;
            }

            List<IGrouping<string, QuestData>> groups =
                filtered
                    .GroupBy(
                        GetQuestGroupKey,
                        StringComparer.OrdinalIgnoreCase)
                    .OrderBy(group =>
                        GetQuestGroupDisplayName(
                            group.FirstOrDefault()))
                    .ToList();

            if (groups.Count > 0 &&
                _expandedQuestGroups.Count == 0)
            {
                string selectedGroup =
                    GetQuestGroupKey(
                        filtered.FirstOrDefault(quest =>
                            string.Equals(
                                quest.id,
                                _selectedQuestId,
                                StringComparison.OrdinalIgnoreCase)) ??
                        groups[0].First());

                _expandedQuestGroups.Add(
                    selectedGroup);
            }

            _questGroupPool.Begin();
            _questRowPool.Begin();

            float y =
                0f;

            foreach (IGrouping<string, QuestData> group in groups)
            {
                List<QuestData> quests =
                    group.ToList();

                bool expanded =
                    _expandedQuestGroups.Contains(
                        group.Key);

                float groupHeight =
                    GK2UiMetrics.QuestJournal.QuestGroupHeight;

                if (expanded)
                {
                    groupHeight +=
                        quests.Count *
                        (GK2UiMetrics.QuestJournal.QuestRowHeight +
                         GK2UiMetrics.QuestJournal.QuestRowGap);
                }

                QuestGroupView groupView =
                    _questGroupPool.Rent();

                ConfigureQuestGroup(
                    groupView,
                    group.Key,
                    quests,
                    expanded,
                    y,
                    groupHeight);

                if (expanded)
                {
                    float rowY =
                        GK2UiMetrics.QuestJournal.QuestGroupHeight;

                    foreach (QuestData quest in quests)
                    {
                        QuestRowView row =
                            _questRowPool.Rent();

                        ConfigureQuestRow(
                            row,
                            groupView.Root.transform,
                            quest,
                            rowY);

                        rowY +=
                            GK2UiMetrics.QuestJournal.QuestRowHeight +
                            GK2UiMetrics.QuestJournal.QuestRowGap;
                    }
                }

                y +=
                    groupHeight +
                    GK2UiMetrics.QuestJournal.QuestRowGap;
            }

            _questGroupPool.End();
            _questRowPool.End();

            _questListContent.sizeDelta =
                new Vector2(
                    0f,
                    Mathf.Max(
                        0f,
                        y -
                        GK2UiMetrics.QuestJournal.QuestRowGap));

            RenderSelectedQuest();

            Canvas.ForceUpdateCanvases();
        }

        private List<QuestData> GetVisibleQuests()
        {
            List<QuestData> quests =
                MainGame.Instance?
                    .GameSave?
                    .questSystemData?
                    .questCollection?
                    .quests;

            if (quests == null)
            {
                return new List<QuestData>();
            }

            return quests
                .Where(quest =>
                    quest != null &&
                    !quest.isHidden &&
                    !quest.isUnknown &&
                    quest.status !=
                        QuestStatus.Canceled)
                .ToList();
        }

        private void ApplyFilterStyle(
            Button button,
            bool selected)
        {
            if (button == null)
            {
                return;
            }

            GK2UiFactory.SetNativeWindowTabSelected(
                button.gameObject,
                selected);
        }

        private void ConfigureQuestGroup(
            QuestGroupView view,
            string groupKey,
            List<QuestData> quests,
            bool expanded,
            float y,
            float groupHeight)
        {
            if (view == null ||
                quests == null ||
                quests.Count == 0)
            {
                return;
            }

            QuestData representative =
                quests[0];

            view.Root.SetActive(
                true);

            view.Root.transform.SetParent(
                _questListContent,
                false);

            view.GroupKey =
                groupKey;

            view.Rect.anchorMin =
                new Vector2(0f, 1f);
            view.Rect.anchorMax =
                new Vector2(1f, 1f);
            view.Rect.pivot =
                new Vector2(0.5f, 1f);
            view.Rect.anchoredPosition =
                new Vector2(
                    0f,
                    -y);
            view.Rect.sizeDelta =
                new Vector2(
                    0f,
                    groupHeight);

            bool groupCompleted =
                quests.All(quest =>
                    quest.status ==
                    QuestStatus.Completed);

            view.Background.sprite =
                _theme.ContentCellSprite;
            view.Background.type =
                _theme.ContentCellSprite != null
                    ? Image.Type.Sliced
                    : Image.Type.Simple;
            view.Background.color =
                _theme.ContentCellSprite != null
                    ? (groupCompleted
                        ? new Color(
                            0.92f,
                            0.95f,
                            0.90f,
                            1f)
                        : Color.white)
                    : (groupCompleted
                        ? new Color(
                            0.15f,
                            0.17f,
                            0.15f,
                            0.96f)
                        : _theme.RowBackground);

            Sprite headerSprite =
                expanded
                    ? (_theme.SectionHeaderSprite ??
                       _theme.SectionHeaderInactiveSprite)
                    : (_theme.SectionHeaderInactiveSprite ??
                       _theme.SectionHeaderSprite);

            view.HeaderBackground.sprite =
                headerSprite;
            view.HeaderBackground.type =
                headerSprite != null
                    ? Image.Type.Sliced
                    : Image.Type.Simple;
            view.HeaderBackground.color =
                headerSprite != null
                    ? Color.white
                    : _theme.HeaderBackground;

            view.Title.text =
                GetQuestGroupDisplayName(
                    representative);

            view.Count.text =
                quests.Count == 1
                    ? "1 quest"
                    : $"{quests.Count} quests";

            view.Chevron.text =
                expanded
                    ? "-"
                    : "+";

            try
            {
                Sprite portrait =
                    representative.Definition?.Portrait ??
                    representative.Definition?.Icon;

                view.Portrait.sprite =
                    portrait;
                view.Portrait.gameObject.SetActive(
                    portrait != null);
                view.Portrait.color =
                    Color.white;

                if (portrait != null)
                {
                    view.Portrait.BlueColorReplace(
                        _theme?.ItemIconTint ??
                        Color.white);
                }
            }
            catch
            {
                view.Portrait.sprite =
                    null;
                view.Portrait.gameObject.SetActive(
                    false);
            }

            view.ToggleButton.onClick.RemoveAllListeners();
            view.ToggleButton.onClick.AddListener(
                () =>
                {
                    if (_expandedQuestGroups.Contains(
                            groupKey))
                    {
                        _expandedQuestGroups.Remove(
                            groupKey);
                    }
                    else
                    {
                        _expandedQuestGroups.Add(
                            groupKey);
                    }

                    Refresh();
                });
        }

        private void ConfigureQuestRow(
            QuestRowView row,
            Transform parent,
            QuestData quest,
            float y)
        {
            if (row == null ||
                parent == null ||
                quest == null)
            {
                return;
            }

            row.Root.SetActive(
                true);

            row.Root.transform.SetParent(
                parent,
                false);

            row.Quest =
                quest;

            float indent =
                GK2UiMetrics.QuestJournal.QuestSubRowIndent;

            row.Rect.anchorMin =
                new Vector2(0f, 1f);
            row.Rect.anchorMax =
                new Vector2(1f, 1f);
            row.Rect.pivot =
                new Vector2(0.5f, 1f);
            row.Rect.anchoredPosition =
                new Vector2(
                    indent / 2f,
                    -y);
            row.Rect.sizeDelta =
                new Vector2(
                    -indent,
                    GK2UiMetrics.QuestJournal.QuestRowHeight);

            bool selected =
                string.Equals(
                    quest.id,
                    _selectedQuestId,
                    StringComparison.OrdinalIgnoreCase);

            row.Background.sprite =
                null;
            row.Background.type =
                Image.Type.Simple;
            row.Background.color =
                selected
                    ? new Color(
                        0.78f,
                        0.58f,
                        0.26f,
                        0.16f)
                    : Color.clear;

            row.Title.text =
                GetQuestTitle(
                    quest);

            row.Description.text =
                quest.Description ??
                string.Empty;

            row.Status.text =
                GetQuestStatusText(
                    quest);

            if (quest.status ==
                QuestStatus.Completed)
            {
                row.Title.color =
                    new Color(
                        0.78f,
                        0.82f,
                        0.72f,
                        1f);
                row.Description.color =
                    new Color(
                        0.62f,
                        0.66f,
                        0.58f,
                        1f);
            }
            else
            {
                row.Title.color =
                    Color.white;
                row.Description.color =
                    new Color(
                        0.76f,
                        0.72f,
                        0.66f,
                        1f);
            }

            List<QuestPhraseRequirement> requirements =
                GetItemRequirements(
                    quest);

            GetQuestProgress(
                requirements,
                out int current,
                out int target,
                out float progress01);

            bool showProgress =
                target > 0;

            row.Progress.Root.SetActive(
                showProgress);

            if (showProgress)
            {
                row.Progress.SetProgress(
                    progress01,
                    1f,
                    $"{current}/{target}");
            }
            else
            {
                row.Progress.SetProgress(
                    0f,
                    1f,
                    string.Empty);
            }

            try
            {
                row.Icon.sprite =
                    quest.Definition?.Portrait ??
                    quest.Definition?.Icon;

                row.Icon.gameObject.SetActive(
                    row.Icon.sprite != null);
                row.Icon.color =
                    Color.white;

                if (row.Icon.sprite != null)
                {
                    row.Icon.BlueColorReplace(
                        _theme?.ItemIconTint ??
                        Color.white);
                }
            }
            catch
            {
                row.Icon.sprite =
                    null;
                row.Icon.gameObject.SetActive(
                    false);
            }

            row.SelectButton.onClick.RemoveAllListeners();
            row.SelectButton.onClick.AddListener(
                () =>
                {
                    _selectedQuestId =
                        quest.id;
                    Refresh();
                });

            bool canTrack =
                quest.IsActiveQuest &&
                UnifiedTrackerFeature.CanTrackQuestFromExternalUi();

            row.PinButton.gameObject.SetActive(
                canTrack);

            if (canTrack)
            {
                bool tracked =
                    UnifiedTrackerFeature.IsQuestTrackedFromExternalUi(
                        quest.id);

                row.PinIcon.sprite =
                    UnifiedTrackerFeature.GetTrackerPinSpriteForExternalUi();

                row.PinIcon.color =
                    tracked
                        ? Color.white
                        : new Color(
                            1f,
                            1f,
                            1f,
                            0.42f);

                row.PinButton.onClick.RemoveAllListeners();
                row.PinButton.onClick.AddListener(
                    () =>
                    {
                        UnifiedTrackerFeature.ToggleQuestFromExternalUi(
                            quest);
                        Refresh();
                    });
            }
        }

        private void RenderSelectedQuest()
        {
            QuestData quest =
                ResolveSelectedQuest();

            _detailPaneHeaderTitle.text =
                "Quest Details";

            _detailSummaryPanel.SetActive(
                true);

            if (quest == null)
            {
                _detailTitle.text =
                    _filter == JournalFilter.Active
                        ? "No Active Quests"
                        : "No Completed Quests";

                _detailStatus.text =
                    _filter == JournalFilter.Active
                        ? "New active quests will appear here."
                        : "Completed quests will appear here.";

                _detailPortraitRect.gameObject.SetActive(
                    false);

                _trackButton.gameObject.SetActive(
                    false);

                _descriptionHeaderRect.gameObject.SetActive(
                    false);
                _objectivesHeaderRect.gameObject.SetActive(
                    false);
                _detailDescription.gameObject.SetActive(
                    false);

                _objectivePool.Begin();
                _objectivePool.End();

                _detailSummaryRect.anchoredPosition =
                    Vector2.zero;
                _detailSummaryRect.sizeDelta =
                    new Vector2(
                        0f,
                        GK2UiMetrics.QuestJournal.DetailSummaryHeight);

                _detailContent.sizeDelta =
                    new Vector2(
                        0f,
                        GK2UiMetrics.QuestJournal.DetailSummaryHeight +
                        12f);

                return;
            }

            _detailPortraitRect.gameObject.SetActive(
                true);
            _detailDescription.gameObject.SetActive(
                true);

            float contentWidth =
                Mathf.Max(
                    240f,
                    _detailScroll.viewport.rect.width);

            _detailSummaryRect.anchoredPosition =
                Vector2.zero;
            _detailSummaryRect.sizeDelta =
                new Vector2(
                    0f,
                    GK2UiMetrics.QuestJournal.DetailSummaryHeight);

            _detailTitle.text =
                GetQuestTitle(
                    quest);

            _detailStatus.text =
                GetQuestStatusText(
                    quest);

            try
            {
                Sprite sprite =
                    quest.Definition?.Portrait ??
                    quest.Definition?.Icon;

                _detailIcon.sprite =
                    sprite;
                _detailIcon.gameObject.SetActive(
                    sprite != null);

                _detailIcon.color =
                    Color.white;

                if (sprite != null)
                {
                    _detailIcon.BlueColorReplace(
                        _theme?.ItemIconTint ??
                        Color.white);
                }
            }
            catch
            {
                _detailIcon.sprite =
                    null;
                _detailIcon.gameObject.SetActive(
                    false);
            }

            bool canTrack =
                quest.IsActiveQuest &&
                UnifiedTrackerFeature.CanTrackQuestFromExternalUi();

            _trackButton.gameObject.SetActive(
                canTrack);

            if (canTrack)
            {
                bool tracked =
                    UnifiedTrackerFeature.IsQuestTrackedFromExternalUi(
                        quest.id);

                _trackButtonLabel.text =
                    tracked
                        ? "Tracked"
                        : "Track Quest";
            }

            float y =
                GK2UiMetrics.QuestJournal.DetailSummaryHeight +
                8f;

            _descriptionHeaderRect.gameObject.SetActive(
                true);
            _descriptionHeaderRect.anchoredPosition =
                new Vector2(
                    0f,
                    -y);
            _descriptionHeaderRect.sizeDelta =
                new Vector2(
                    0f,
                    20f);

            y +=
                25f;

            _detailDescription.text =
                quest.Description ??
                string.Empty;

            Vector2 descriptionPreferred =
                _detailDescription.GetPreferredValues(
                    _detailDescription.text,
                    contentWidth,
                    0f);

            float descriptionHeight =
                Mathf.Max(
                    36f,
                    descriptionPreferred.y);

            _detailDescription.rectTransform.anchoredPosition =
                new Vector2(
                    0f,
                    -y);
            _detailDescription.rectTransform.sizeDelta =
                new Vector2(
                    0f,
                    descriptionHeight);

            y +=
                descriptionHeight +
                10f;

            List<QuestPhraseRequirement> itemRequirements =
                GetItemRequirements(
                    quest);

            bool showObjectives =
                quest.status !=
                    QuestStatus.Completed &&
                itemRequirements.Count > 0;

            _objectivesHeaderRect.gameObject.SetActive(
                showObjectives);

            _objectivePool.Begin();

            if (showObjectives)
            {
                _objectivesHeaderRect.anchoredPosition =
                    new Vector2(
                        0f,
                        -y);
                _objectivesHeaderRect.sizeDelta =
                    new Vector2(
                        0f,
                        20f);

                y +=
                    25f;

                int columns =
                    Mathf.Max(
                        1,
                        GK2UiMetrics.QuestJournal.ObjectiveColumns);

                for (int i = 0; i < itemRequirements.Count; i++)
                {
                    QuestPhraseRequirement requirement =
                        itemRequirements[i];

                    GK2UiItemRequirementView view =
                        _objectivePool.Rent();

                    int objectiveRow =
                        i / columns;

                    int column =
                        i % columns;

                    view.Rect.anchorMin =
                        new Vector2(0f, 1f);
                    view.Rect.anchorMax =
                        new Vector2(0f, 1f);
                    view.Rect.pivot =
                        new Vector2(0f, 1f);
                    view.Rect.anchoredPosition =
                        new Vector2(
                            4f +
                            column *
                            (GK2UiMetrics.QuestJournal.ObjectiveCellSize +
                             GK2UiMetrics.QuestJournal.ObjectiveCellGap),
                            -(y +
                              objectiveRow *
                              (GK2UiMetrics.QuestJournal.ObjectiveCellSize +
                               GK2UiMetrics.QuestJournal.ObjectiveCellGap)));

                    int current =
                        GetInventoryCount(
                            requirement.itemCount.itemId);

                    GK2UiItemRequirementBuilder.Bind(
                        view,
                        _theme,
                        requirement.itemCount.itemId,
                        current,
                        requirement.itemCount.count);
                }

                int rows =
                    Mathf.CeilToInt(
                        itemRequirements.Count /
                        (float)columns);

                y +=
                    rows *
                    GK2UiMetrics.QuestJournal.ObjectiveCellSize +
                    Mathf.Max(
                        0,
                        rows - 1) *
                    GK2UiMetrics.QuestJournal.ObjectiveCellGap;
            }

            _objectivePool.End();

            _detailContent.sizeDelta =
                new Vector2(
                    0f,
                    y + 16f);
        }

        private static List<QuestPhraseRequirement> GetItemRequirements(
            QuestData quest)
        {
            return quest?
                       .Definition?
                       .finishCheck?
                       .phraseReqs?
                       .Where(requirement =>
                           requirement != null &&
                           requirement.entity ==
                               QuestPhraseRequirement.Entity.Item &&
                           requirement.itemCount != null &&
                           !string.IsNullOrWhiteSpace(
                               requirement.itemCount.itemId))
                       .ToList() ??
                   new List<QuestPhraseRequirement>();
        }

        private static int GetInventoryCount(
            string itemId)
        {
            if (string.IsNullOrWhiteSpace(
                    itemId))
            {
                return 0;
            }

            return
                MainGame.PlayerData?
                    .Inventory?
                    .Data?
                    .GetTotalCountInInventory(
                        itemId,
                        null,
                        false) ??
                0;
        }

        private static void GetQuestProgress(
            List<QuestPhraseRequirement> requirements,
            out int current,
            out int target,
            out float progress01)
        {
            current = 0;
            target = 0;

            if (requirements != null)
            {
                foreach (QuestPhraseRequirement requirement in
                         requirements)
                {
                    if (requirement?.itemCount == null)
                    {
                        continue;
                    }

                    int need =
                        Mathf.Max(
                            1,
                            requirement.itemCount.count);

                    int have =
                        GetInventoryCount(
                            requirement.itemCount.itemId);

                    target +=
                        need;
                    current +=
                        Mathf.Min(
                            have,
                            need);
                }
            }

            progress01 =
                target <= 0
                    ? 0f
                    : Mathf.Clamp01(
                        current /
                        (float)target);
        }

        private QuestData ResolveSelectedQuest()
        {
            if (string.IsNullOrWhiteSpace(
                    _selectedQuestId))
            {
                return null;
            }

            QuestCollectionData collection =
                MainGame.Instance?
                    .GameSave?
                    .questSystemData?
                    .questCollection;

            if (collection?.questsCache == null ||
                !collection.questsCache.TryGetValue(
                    _selectedQuestId,
                    out QuestData quest))
            {
                return null;
            }

            return quest;
        }

        private static string GetQuestGroupKey(
            QuestData quest)
        {
            object identity =
                TryGetQuestCharacterIdentity(
                    quest);

            string identityId =
                NormalizeIdentityId(
                    GetIdentityString(
                        identity));

            if (!string.IsNullOrWhiteSpace(
                    identityId))
            {
                return
                    "npc:" +
                    identityId;
            }

            try
            {
                Sprite portrait =
                    quest?.Definition?.Portrait;

                if (portrait != null &&
                    !string.IsNullOrWhiteSpace(
                        portrait.name))
                {
                    return
                        "portrait:" +
                        portrait.name;
                }
            }
            catch
            {
                // Fall through to a stable catch-all group.
            }

            return "other";
        }

        private static string GetQuestGroupDisplayName(
            QuestData quest)
        {
            object identity =
                TryGetQuestCharacterIdentity(
                    quest);

            string identityId =
                NormalizeIdentityId(
                    GetIdentityString(
                        identity));

            if (!string.IsNullOrWhiteSpace(
                    identityId))
            {
                string localized =
                    LLBase.L(
                        identityId);

                if (!string.IsNullOrWhiteSpace(
                        localized) &&
                    !string.Equals(
                        localized,
                        identityId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return localized;
                }

                return HumanizeIdentifier(
                    identityId);
            }

            try
            {
                string portraitName =
                    quest?.Definition?.Portrait?.name;

                if (!string.IsNullOrWhiteSpace(
                    portraitName))
                {
                    return HumanizeIdentifier(
                        portraitName);
                }
            }
            catch
            {
                // Use the generic fallback below.
            }

            return "Other";
        }

        private static object TryGetQuestCharacterIdentity(
            QuestData quest)
        {
            object definition =
                quest?.Definition;

            if (definition == null)
            {
                return null;
            }

            string[] names =
            {
                "character",
                "Character",
                "npc",
                "Npc",
                "NPC",
                "giver",
                "Giver",
                "owner",
                "Owner",
                "characterId",
                "CharacterId",
                "npcId",
                "NpcId",
                "giverId",
                "GiverId"
            };

            Type type =
                definition.GetType();

            foreach (string name in names)
            {
                try
                {
                    PropertyInfo property =
                        type.GetProperty(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic);

                    if (property != null)
                    {
                        object value =
                            property.GetValue(
                                definition,
                                null);

                        if (value != null)
                        {
                            return value;
                        }
                    }

                    FieldInfo field =
                        type.GetField(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic);

                    if (field != null)
                    {
                        object value =
                            field.GetValue(
                                definition);

                        if (value != null)
                        {
                            return value;
                        }
                    }
                }
                catch
                {
                    // Some generated/runtime members may throw when inspected.
                }
            }

            return null;
        }

        private static string GetIdentityString(
            object identity)
        {
            if (identity == null)
            {
                return string.Empty;
            }

            if (identity is string text)
            {
                return text;
            }

            Type type =
                identity.GetType();

            string[] names =
            {
                "id",
                "Id",
                "ID",
                "name",
                "Name"
            };

            foreach (string name in names)
            {
                try
                {
                    PropertyInfo property =
                        type.GetProperty(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic);

                    if (property != null)
                    {
                        object value =
                            property.GetValue(
                                identity,
                                null);

                        if (value != null)
                        {
                            return value.ToString();
                        }
                    }

                    FieldInfo field =
                        type.GetField(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic);

                    if (field != null)
                    {
                        object value =
                            field.GetValue(
                                identity);

                        if (value != null)
                        {
                            return value.ToString();
                        }
                    }
                }
                catch
                {
                    // Ignore members that cannot be reflected safely.
                }
            }

            return identity.ToString();
        }

        private static string NormalizeIdentityId(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return string.Empty;
            }

            string normalized =
                value.Trim();

            int cloneIndex =
                normalized.IndexOf(
                    "(clone)",
                    StringComparison.OrdinalIgnoreCase);

            if (cloneIndex >= 0)
            {
                normalized =
                    normalized
                        .Remove(
                            cloneIndex,
                            "(clone)".Length)
                        .Trim();
            }

            return normalized;
        }

        private static string HumanizeIdentifier(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return "Other";
            }

            string normalized =
                NormalizeIdentityId(
                    value)
                    .Replace("_portrait", string.Empty)
                    .Replace("_icon", string.Empty)
                    .Replace("portrait_", string.Empty)
                    .Replace("icon_", string.Empty)
                    .Replace("_", " ")
                    .Replace("-", " ")
                    .Trim();

            if (normalized.Length == 0)
            {
                return "Other";
            }

            string[] words =
                normalized.Split(
                    new[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < words.Length; i++)
            {
                string word =
                    words[i];

                if (word.Length == 0)
                {
                    continue;
                }

                words[i] =
                    char.ToUpperInvariant(
                        word[0]) +
                    (word.Length > 1
                        ? word.Substring(1).ToLowerInvariant()
                        : string.Empty);
            }

            return string.Join(
                " ",
                words);
        }

        private static string GetQuestTitle(
            QuestData quest)
        {
            if (quest == null)
            {
                return string.Empty;
            }

            string localized =
                LLBase.L(
                    quest.id);

            return string.IsNullOrWhiteSpace(
                       localized)
                ? quest.id
                : localized;
        }

        private static string GetQuestStatusText(
            QuestData quest)
        {
            if (quest == null)
            {
                return string.Empty;
            }

            switch (quest.status)
            {
                case QuestStatus.Available:
                    return "Available";

                case QuestStatus.Awaiting:
                    return "Awaiting";

                case QuestStatus.InProgress:
                    return "In Progress";

                case QuestStatus.Completed:
                    return "Completed";

                case QuestStatus.Canceled:
                    return "Canceled";

                default:
                    return quest.status.ToString();
            }
        }
    }
}
