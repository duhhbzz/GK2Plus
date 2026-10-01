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
            public Image IconFrame;
            public Image Icon;
            public TextMeshProUGUI Title;
            public TextMeshProUGUI Description;
            public TextMeshProUGUI Status;
            public GameObject ProgressRoot;
            public Image ProgressFill;
            public TextMeshProUGUI ProgressLabel;
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
        private TextMeshProUGUI _objectivesTitle;
        private TextMeshProUGUI _detailPaneHeaderTitle;
        private RectTransform _descriptionHeaderRect;
        private RectTransform _objectivesHeaderRect;
        private Image _detailIcon;
        private Button _trackButton;
        private Image _trackPinIcon;
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
                    null,
                    Image.Type.Simple,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    _theme.ContentBackground,
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

        private void AddPaneFrame(
            Transform parent)
        {
            if (_theme?.WindowFrameSprite == null)
            {
                return;
            }

            GameObject frame =
                GK2UiFactory.CreateImage(
                    parent,
                    "Frame",
                    _theme.WindowFrameSprite,
                    Image.Type.Sliced,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    Color.white);

            RectTransform rect =
                frame.GetComponent<RectTransform>();

            rect.offsetMin =
                Vector2.zero;
            rect.offsetMax =
                Vector2.zero;
            frame.transform.SetAsLastSibling();
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
                    null,
                    Image.Type.Simple,
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    Vector2.zero,
                    Vector2.zero,
                    _theme.ContentBackground);

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

            AddPaneFrame(
                pane.transform);

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

            float halfFilterWidth =
                (width -
                 (GK2UiMetrics.QuestJournal.OuterPadding * 2f) -
                 GK2UiMetrics.QuestJournal.FilterGap) /
                2f;

            float filterTop =
                GK2UiMetrics.QuestJournal.SectionHeaderHeight +
                7f;

            _activeFilterButton =
                GK2UiFactory.CreateFlatButton(
                    pane.transform,
                    "ActiveFilter",
                    _theme,
                    "Active",
                    new Vector2(
                        GK2UiMetrics.QuestJournal.OuterPadding +
                        (halfFilterWidth / 2f),
                        -filterTop),
                    new Vector2(
                        halfFilterWidth,
                        GK2UiMetrics.QuestJournal.FilterHeight),
                    () =>
                    {
                        _filter =
                            JournalFilter.Active;
                        _selectedQuestId =
                            string.Empty;
                        Refresh();
                    },
                    true);

            RectTransform activeRect =
                _activeFilterButton.GetComponent<RectTransform>();

            activeRect.anchorMin =
                new Vector2(0f, 1f);
            activeRect.anchorMax =
                new Vector2(0f, 1f);
            activeRect.pivot =
                new Vector2(0.5f, 1f);

            _activeFilterLabel =
                _activeFilterButton
                    .GetComponentInChildren<TextMeshProUGUI>(
                        true);

            _completedFilterButton =
                GK2UiFactory.CreateFlatButton(
                    pane.transform,
                    "CompletedFilter",
                    _theme,
                    "Completed",
                    new Vector2(
                        GK2UiMetrics.QuestJournal.OuterPadding +
                        halfFilterWidth +
                        GK2UiMetrics.QuestJournal.FilterGap +
                        (halfFilterWidth / 2f),
                        -filterTop),
                    new Vector2(
                        halfFilterWidth,
                        GK2UiMetrics.QuestJournal.FilterHeight),
                    () =>
                    {
                        _filter =
                            JournalFilter.Completed;
                        _selectedQuestId =
                            string.Empty;
                        Refresh();
                    },
                    false);

            RectTransform completedRect =
                _completedFilterButton.GetComponent<RectTransform>();

            completedRect.anchorMin =
                new Vector2(0f, 1f);
            completedRect.anchorMax =
                new Vector2(0f, 1f);
            completedRect.pivot =
                new Vector2(0.5f, 1f);

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
                7f;

            viewportRect.offsetMin =
                new Vector2(
                    GK2UiMetrics.QuestJournal.OuterPadding,
                    GK2UiMetrics.QuestJournal.OuterPadding);
            viewportRect.offsetMax =
                new Vector2(
                    -GK2UiMetrics.QuestJournal.OuterPadding,
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
            Sprite cardSprite =
                _theme.InspirationCardSprite;

            GameObject root =
                GK2UiFactory.CreateImage(
                    _questListContent,
                    "QuestGroup",
                    cardSprite,
                    cardSprite != null
                        ? Image.Type.Sliced
                        : Image.Type.Simple,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f),
                    Vector2.zero,
                    new Vector2(
                        0f,
                        GK2UiMetrics.QuestJournal.QuestGroupHeight),
                    cardSprite != null
                        ? Color.white
                        : _theme.RowBackground,
                    true);

            RectTransform rect =
                root.GetComponent<RectTransform>();

            Image background =
                root.GetComponent<Image>();

            Button toggle =
                root.AddComponent<Button>();

            toggle.targetGraphic =
                background;
            toggle.transition =
                Selectable.Transition.ColorTint;

            float pad =
                GK2UiMetrics.QuestJournal.QuestCardPadding;

            float portraitSize =
                GK2UiMetrics.QuestJournal.QuestGroupIconSize;

            GameObject portraitBacking =
                GK2UiFactory.CreateImage(
                    root.transform,
                    "PortraitBacking",
                    _theme.ItemSlotSprite,
                    _theme.ItemSlotSprite != null
                        ? Image.Type.Sliced
                        : Image.Type.Simple,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(
                        pad,
                        -6f),
                    new Vector2(
                        portraitSize,
                        portraitSize),
                    _theme.ItemSlotSprite != null
                        ? Color.white
                        : new Color(0.10f, 0.10f, 0.11f, 0.98f));

            GameObject portraitObject =
                GK2UiFactory.CreateImage(
                    portraitBacking.transform,
                    "Portrait",
                    null,
                    Image.Type.Simple,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(-6f, -6f),
                    Color.white);

            Image portrait =
                portraitObject.GetComponent<Image>();

            portrait.preserveAspect =
                true;

            TextMeshProUGUI title =
                GK2UiFactory.CreateText(
                    root.transform,
                    "GroupTitle",
                    _theme.TitleTextTemplate ??
                    _theme.BodyTextTemplate,
                    string.Empty,
                    13f,
                    TextAlignmentOptions.MidlineLeft,
                    new Vector2(0f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 0.5f),
                    new Vector2(
                        pad + portraitSize + 10f,
                        5f),
                    new Vector2(
                        -96f,
                        24f));

            title.color =
                _theme.AccentText;
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
                    9f,
                    TextAlignmentOptions.MidlineLeft,
                    new Vector2(0f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 0.5f),
                    new Vector2(
                        pad + portraitSize + 10f,
                        -14f),
                    new Vector2(
                        -96f,
                        18f));

            count.color =
                new Color(0.78f, 0.76f, 0.70f, 1f);

            TextMeshProUGUI chevron =
                GK2UiFactory.CreateText(
                    root.transform,
                    "Chevron",
                    _theme.TitleTextTemplate ??
                    _theme.BodyTextTemplate,
                    "▶",
                    16f,
                    TextAlignmentOptions.Center,
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f),
                    new Vector2(-10f, 0f),
                    new Vector2(24f, 24f));

            chevron.color =
                _theme.AccentText;

            return new QuestGroupView
            {
                Root = root,
                Rect = rect,
                Background = background,
                Portrait = portrait,
                Title = title,
                Count = count,
                Chevron = chevron,
                ToggleButton = toggle
            };
        }

        private QuestRowView CreateQuestRow()
        {
            Sprite cardSprite =
                _theme.InspirationCardSprite;

            GameObject root =
                GK2UiFactory.CreateImage(
                    _questListContent,
                    "QuestRow",
                    cardSprite,
                    cardSprite != null
                        ? Image.Type.Sliced
                        : Image.Type.Simple,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f),
                    Vector2.zero,
                    new Vector2(
                        0f,
                        GK2UiMetrics.QuestJournal.QuestRowHeight),
                    cardSprite != null
                        ? Color.white
                        : _theme.RowBackground,
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

            float pad =
                GK2UiMetrics.QuestJournal.QuestCardPadding;

            const float iconBoxSize = 40f;

            GameObject iconBacking =
                GK2UiFactory.CreateImage(
                    root.transform,
                    "QuestIconBacking",
                    _theme.ItemSlotSprite,
                    _theme.ItemSlotSprite != null
                        ? Image.Type.Sliced
                        : Image.Type.Simple,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(
                        pad,
                        -29f),
                    new Vector2(
                        iconBoxSize,
                        iconBoxSize),
                    _theme.ItemSlotSprite != null
                        ? Color.white
                        : new Color(0.10f, 0.10f, 0.11f, 0.98f));

            GameObject iconObject =
                GK2UiFactory.CreateImage(
                    iconBacking.transform,
                    "QuestIcon",
                    null,
                    Image.Type.Simple,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(-6f, -6f),
                    Color.white);

            Image icon =
                iconObject.GetComponent<Image>();

            icon.preserveAspect =
                true;

            TextMeshProUGUI title =
                GK2UiFactory.CreateText(
                    root.transform,
                    "QuestTitle",
                    _theme.TitleTextTemplate ??
                    _theme.BodyTextTemplate,
                    string.Empty,
                    11.5f,
                    TextAlignmentOptions.TopLeft,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(
                        pad + iconBoxSize + 9f,
                        -6f),
                    new Vector2(
                        -92f,
                        22f));

            title.textWrappingMode =
                TextWrappingModes.NoWrap;
            title.overflowMode =
                TextOverflowModes.Ellipsis;
            title.color =
                _theme.AccentText;

            TextMeshProUGUI description =
                GK2UiFactory.CreateText(
                    root.transform,
                    "QuestDescription",
                    _theme.BodyTextTemplate,
                    string.Empty,
                    8.75f,
                    TextAlignmentOptions.TopLeft,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero);

            RectTransform descriptionRect =
                description.rectTransform;

            descriptionRect.offsetMin =
                new Vector2(
                    pad + iconBoxSize + 9f,
                    27f);
            descriptionRect.offsetMax =
                new Vector2(
                    -(pad + 7f),
                    -28f);

            description.textWrappingMode =
                TextWrappingModes.Normal;
            description.overflowMode =
                TextOverflowModes.Truncate;
            description.maxVisibleLines =
                2;

            GameObject progressRoot =
                GK2UiFactory.CreateImage(
                    root.transform,
                    "Progress",
                    null,
                    Image.Type.Simple,
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0.5f, 0f),
                    Vector2.zero,
                    Vector2.zero,
                    new Color(
                        0.10f,
                        0.11f,
                        0.08f,
                        0.94f));

            RectTransform progressRect =
                progressRoot.GetComponent<RectTransform>();

            progressRect.offsetMin =
                new Vector2(
                    pad + iconBoxSize + 9f,
                    7f);
            progressRect.offsetMax =
                new Vector2(
                    -(pad + 40f),
                    7f +
                    GK2UiMetrics.QuestJournal.QuestProgressHeight);

            GameObject fillObject =
                GK2UiFactory.CreateImage(
                    progressRoot.transform,
                    "Fill",
                    null,
                    Image.Type.Filled,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    new Color(
                        0.30f,
                        0.54f,
                        0.04f,
                        1f));

            Image progressFill =
                fillObject.GetComponent<Image>();

            progressFill.type =
                Image.Type.Filled;
            progressFill.fillMethod =
                Image.FillMethod.Horizontal;
            progressFill.fillOrigin =
                (int)Image.OriginHorizontal.Left;
            progressFill.fillAmount =
                0f;

            TextMeshProUGUI progressLabel =
                GK2UiFactory.CreateText(
                    progressRoot.transform,
                    "ProgressLabel",
                    _theme.CountTextTemplate ??
                    _theme.BodyTextTemplate,
                    string.Empty,
                    8.5f,
                    TextAlignmentOptions.Center,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero);

            progressLabel.rectTransform.offsetMin =
                Vector2.zero;
            progressLabel.rectTransform.offsetMax =
                Vector2.zero;

            TextMeshProUGUI status =
                GK2UiFactory.CreateText(
                    root.transform,
                    "QuestStatus",
                    _theme.BodyTextTemplate,
                    string.Empty,
                    8f,
                    TextAlignmentOptions.Right,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(
                        -36f,
                        -8f),
                    new Vector2(
                        86f,
                        16f));

            status.color =
                new Color(0.74f, 0.74f, 0.72f, 1f);

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
                        -7f,
                        -32f),
                    new Vector2(26f, 26f),
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

            return new QuestRowView
            {
                Root = root,
                Rect = rect,
                Background = background,
                Icon = icon,
                Title = title,
                Description = description,
                Status = status,
                ProgressRoot = progressRoot,
                ProgressFill = progressFill,
                ProgressLabel = progressLabel,
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
                    null,
                    Image.Type.Simple,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    _theme.ContentBackground);

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

            AddPaneFrame(
                pane.transform);

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
            _detailScroll.scrollSensitivity =
                28f;

            _detailTitle =
                GK2UiFactory.CreateText(
                    _detailContent,
                    "DetailTitle",
                    _theme.TitleTextTemplate,
                    string.Empty,
                    GK2UiMetrics.QuestJournal.DetailTitleSize,
                    TextAlignmentOptions.TopLeft,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f),
                    Vector2.zero,
                    new Vector2(-78f, 34f));

            _detailTitle.color =
                _theme.AccentText;
            _detailTitle.textWrappingMode =
                TextWrappingModes.Normal;

            _detailIcon =
                GK2UiFactory.CreateImage(
                    _detailContent,
                    "DetailQuestIcon",
                    null,
                    Image.Type.Simple,
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    Vector2.zero,
                    new Vector2(62f, 62f),
                    Color.white)
                    .GetComponent<Image>();

            _detailIcon.preserveAspect =
                true;

            _detailStatus =
                GK2UiFactory.CreateText(
                    _detailContent,
                    "DetailStatus",
                    _theme.BodyTextTemplate,
                    string.Empty,
                    GK2UiMetrics.QuestJournal.DetailStatusSize,
                    TextAlignmentOptions.TopLeft,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f),
                    Vector2.zero,
                    new Vector2(-78f, 18f));

            GK2UiSectionHeaderView descriptionHeader =
                GK2UiSectionHeaderBuilder.Create(
                    _detailContent,
                    _theme,
                    "DescriptionHeader",
                    "Description",
                    30f);

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
                    new Vector2(0f, 80f));

            _detailDescription.textWrappingMode =
                TextWrappingModes.Normal;

            GK2UiSectionHeaderView objectivesHeader =
                GK2UiSectionHeaderBuilder.Create(
                    _detailContent,
                    _theme,
                    "ObjectivesHeader",
                    "Objectives",
                    30f);

            _objectivesHeaderRect =
                objectivesHeader.Rect;
            _objectivesTitle =
                objectivesHeader.Title;

            _trackButton =
                GK2UiFactory.CreateFlatButton(
                    _detailContent,
                    "TrackQuestButton",
                    _theme,
                    "Track Quest",
                    Vector2.zero,
                    new Vector2(132f, 29f),
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
                    },
                    false);

            _trackButtonLabel =
                _trackButton
                    .GetComponentInChildren<TextMeshProUGUI>(
                        true);

            if (_trackButtonLabel != null)
            {
                _trackButtonLabel.rectTransform.offsetMin =
                    new Vector2(30f, 0f);
            }

            GameObject pin =
                GK2UiFactory.CreateImage(
                    _trackButton.transform,
                    "TrackerPin",
                    UnifiedTrackerFeature.GetTrackerPinSpriteForExternalUi(),
                    Image.Type.Simple,
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(5f, 0f),
                    new Vector2(25f, 25f),
                    Color.white);

            _trackPinIcon =
                pin.GetComponent<Image>();

            _trackPinIcon.preserveAspect =
                true;

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
                _activeFilterLabel,
                _filter == JournalFilter.Active);

            ApplyFilterStyle(
                _completedFilterButton,
                _completedFilterLabel,
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

            float y = 0f;

            foreach (IGrouping<string, QuestData> group in groups)
            {
                List<QuestData> quests =
                    group.ToList();

                QuestGroupView groupView =
                    _questGroupPool.Rent();

                bool expanded =
                    _expandedQuestGroups.Contains(
                        group.Key);

                ConfigureQuestGroup(
                    groupView,
                    group.Key,
                    quests,
                    expanded,
                    y);

                y +=
                    GK2UiMetrics.QuestJournal.QuestGroupHeight +
                    GK2UiMetrics.QuestJournal.QuestRowGap;

                if (!expanded)
                {
                    continue;
                }

                foreach (QuestData quest in quests)
                {
                    QuestRowView row =
                        _questRowPool.Rent();

                    ConfigureQuestRow(
                        row,
                        quest,
                        y);

                    y +=
                        GK2UiMetrics.QuestJournal.QuestRowHeight +
                        GK2UiMetrics.QuestJournal.QuestRowGap;
                }
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
            TextMeshProUGUI label,
            bool selected)
        {
            Image image =
                button?.targetGraphic as Image;

            if (image != null)
            {
                Sprite sprite =
                    selected
                        ? (_theme.SectionHeaderSprite ??
                           _theme.SectionHeaderInactiveSprite)
                        : (_theme.SectionHeaderInactiveSprite ??
                           _theme.SectionHeaderSprite);

                image.sprite =
                    sprite;
                image.type =
                    sprite != null
                        ? Image.Type.Sliced
                        : Image.Type.Simple;
                image.color =
                    sprite != null
                        ? Color.white
                        : (selected
                            ? _theme.TabSelected
                            : _theme.TabNeutral);
            }

            if (label != null)
            {
                TextMeshProUGUI source =
                    _theme.TitleTextTemplate;

                if (source != null)
                {
                    label.font =
                        source.font;
                    label.fontSharedMaterial =
                        source.fontSharedMaterial;
                    label.spriteAsset =
                        source.spriteAsset;
                }

                label.fontSize =
                    10.5f;
                label.fontSizeMin =
                    10.5f;
                label.fontSizeMax =
                    10.5f;
                label.color =
                    selected
                        ? _theme.AccentText
                        : new Color(
                            0.82f,
                            0.76f,
                            0.66f,
                            1f);
            }
        }

        private void ConfigureQuestGroup(
            QuestGroupView view,
            string groupKey,
            List<QuestData> quests,
            bool expanded,
            float y)
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

            view.GroupKey =
                groupKey;

            view.Rect.anchoredPosition =
                new Vector2(
                    0f,
                    -y);

            view.Background.color =
                Color.white;

            view.Title.text =
                GetQuestGroupDisplayName(
                    representative);

            view.Count.text =
                quests.Count == 1
                    ? "1 quest"
                    : $"{quests.Count} quests";

            view.Chevron.text =
                expanded
                    ? "▼"
                    : "▶";

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
            QuestData quest,
            float y)
        {
            if (row == null ||
                quest == null)
            {
                return;
            }

            row.Root.SetActive(
                true);

            row.Quest =
                quest;

            float indent =
                GK2UiMetrics.QuestJournal.QuestSubRowIndent;

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

            Sprite cardSprite =
                quest.status ==
                    QuestStatus.Completed
                    ? (_theme.InspirationCardCompletedSprite ??
                       _theme.InspirationCardSprite)
                    : _theme.InspirationCardSprite;

            if (cardSprite != null)
            {
                row.Background.sprite =
                    cardSprite;
                row.Background.type =
                    Image.Type.Sliced;
            }

            row.Background.color =
                selected
                    ? new Color(
                        1f,
                        0.92f,
                        0.72f,
                        1f)
                    : Color.white;

            row.Title.text =
                GetQuestTitle(
                    quest);

            row.Description.text =
                quest.Description ??
                string.Empty;

            row.Status.text =
                GetQuestStatusText(
                    quest);

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

            row.ProgressRoot.SetActive(
                showProgress);

            if (showProgress)
            {
                row.ProgressFill.fillAmount =
                    Mathf.Clamp01(
                        progress01);

                row.ProgressLabel.text =
                    $"{current}/{target}";
            }
            else
            {
                row.ProgressFill.fillAmount =
                    0f;
                row.ProgressLabel.text =
                    string.Empty;
            }

            try
            {
                row.Icon.sprite =
                    quest.Definition?.Icon;

                row.Icon.gameObject.SetActive(
                    row.Icon.sprite != null);
                row.Icon.color =
                    Color.white;
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

            if (quest == null)
            {
                _detailPaneHeaderTitle.text =
                    "Quest Details";
                _detailTitle.text =
                    _filter == JournalFilter.Active
                        ? "No Active Quests"
                        : "No Completed Quests";
                _detailStatus.text =
                    string.Empty;
                _detailDescription.text =
                    string.Empty;
                _detailIcon.gameObject.SetActive(
                    false);
                _descriptionHeaderRect.gameObject.SetActive(
                    false);
                _objectivesHeaderRect.gameObject.SetActive(
                    false);
                _trackButton.gameObject.SetActive(
                    false);

                _objectivePool.Begin();
                _objectivePool.End();

                _detailContent.sizeDelta =
                    new Vector2(0f, 100f);

                return;
            }

            _detailPaneHeaderTitle.text =
                "Quest Details";

            float width =
                Mathf.Max(
                    240f,
                    _detailScroll.viewport.rect.width);

            float contentWidth =
                width;

            float y = 0f;

            _detailTitle.text =
                GetQuestTitle(
                    quest);

            Vector2 titlePreferred =
                _detailTitle.GetPreferredValues(
                    _detailTitle.text,
                    contentWidth - 82f,
                    0f);

            float titleHeight =
                Mathf.Max(
                    30f,
                    titlePreferred.y);

            _detailTitle.rectTransform.anchoredPosition =
                new Vector2(0f, -y);
            _detailTitle.rectTransform.sizeDelta =
                new Vector2(
                    -82f,
                    titleHeight);

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
            }
            catch
            {
                _detailIcon.gameObject.SetActive(
                    false);
            }

            _detailIcon.rectTransform.anchoredPosition =
                new Vector2(0f, -y);

            y +=
                titleHeight + 3f;

            _detailStatus.text =
                GetQuestStatusText(
                    quest);

            _detailStatus.rectTransform.anchoredPosition =
                new Vector2(0f, -y);
            _detailStatus.rectTransform.sizeDelta =
                new Vector2(-82f, 18f);

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

                _trackPinIcon.color =
                    tracked
                        ? Color.white
                        : new Color(
                            1f,
                            1f,
                            1f,
                            0.50f);

                RectTransform trackRect =
                    _trackButton.GetComponent<RectTransform>();

                trackRect.anchorMin =
                    new Vector2(1f, 1f);
                trackRect.anchorMax =
                    new Vector2(1f, 1f);
                trackRect.pivot =
                    new Vector2(1f, 1f);
                trackRect.anchoredPosition =
                    new Vector2(
                        -76f,
                        -44f);
            }

            y =
                Mathf.Max(
                    y + 25f,
                    74f);

            _descriptionHeaderRect.gameObject.SetActive(
                true);
            _descriptionHeaderRect.anchoredPosition =
                new Vector2(0f, -y);
            _descriptionHeaderRect.sizeDelta =
                new Vector2(0f, 30f);

            y += 36f;

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
                    44f,
                    descriptionPreferred.y);

            _detailDescription.rectTransform.anchoredPosition =
                new Vector2(0f, -y);
            _detailDescription.rectTransform.sizeDelta =
                new Vector2(0f, descriptionHeight);

            y +=
                descriptionHeight + 14f;

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
                    new Vector2(0f, -y);
                _objectivesHeaderRect.sizeDelta =
                    new Vector2(0f, 30f);

                y += 37f;

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
                            column *
                            (GK2UiMetrics.QuestJournal.ObjectiveCellSize +
                             GK2UiMetrics.QuestJournal.ObjectiveCellGap),
                            -(y +
                              (objectiveRow *
                               (GK2UiMetrics.QuestJournal.ObjectiveCellSize +
                                GK2UiMetrics.QuestJournal.ObjectiveCellGap))));

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
                    (rows *
                     GK2UiMetrics.QuestJournal.ObjectiveCellSize) +
                    ((rows - 1) *
                     GK2UiMetrics.QuestJournal.ObjectiveCellGap);
            }

            _objectivePool.End();

            _detailContent.sizeDelta =
                new Vector2(
                    0f,
                    y + 20f);
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
