using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using GK2Plus.Core;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GK2Plus.Framework.UI
{
    internal sealed class ModMenuController : MonoBehaviour
    {
        private const string RootObjectName = "GK2PlusModMenuRoot";
        private static readonly string[] Tabs =
        {
            "General",
            "Inventory",
            "Crafting",
            "Farming",
            "Movement",
            "Tracker",
            "Zombies",
            "Cheats",
            "More"
        };

        private ManualLogSource _logger;
        private GK2UiTheme _theme;
        private GameObject _menuRoot;
        private GameObject _pageTitle;
        private GameObject _pageText;
        private GameObject _featureSettingsNote;
        private GameObject _headerToggleHint;
        private GameObject _headerCloseHint;

        private float _controllerChordStartedAt =
            -1f;
        private bool _controllerChordLatched;

        private readonly Dictionary<string, List<GameObject>> _tabBodyDecor =
            new Dictionary<string, List<GameObject>>(
                StringComparer.OrdinalIgnoreCase);

        private GameObject _githubButton;
        private GameObject _nexusButton;
        private GameObject _bugButton;
        private GameObject _contentRoot;
        private RectTransform _bodyContentRect;
        private ScrollRect _bodyScrollRect;
        private Scrollbar _bodyScrollbar;
        private GameObject _bodyTextTemplate;
        private GameObject _menuButtonLabelTemplate;
        private Sprite _menuButtonSprite;

        private readonly List<GK2FeatureToggleControl> _featureToggleControls =
            new List<GK2FeatureToggleControl>();

        private readonly Dictionary<GK2FeatureToggleControl, GameObject> _featureToggleRows =
            new Dictionary<GK2FeatureToggleControl, GameObject>();

        private readonly Dictionary<string, GameObject> _featureGroupBackgrounds =
            new Dictionary<string, GameObject>(
                StringComparer.OrdinalIgnoreCase);

        private readonly List<GK2FeatureOptionControl> _featureOptionControls =
            new List<GK2FeatureOptionControl>();

        private readonly Dictionary<GK2FeatureOptionControl, GameObject> _featureOptionRows =
            new Dictionary<GK2FeatureOptionControl, GameObject>();

        private readonly HashSet<string> _collapsedFeatureGroups =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        private GameObject _featureOptionPickerRoot;
        private GK2FeatureOptionControl _activeFeatureOptionPickerControl;
        private GameObject _featureOptionPickerPageText;
        private Component _featureOptionPickerSearchInput;
        private string _lastFeatureOptionPickerSearch = string.Empty;
        private readonly List<GameObject> _featureOptionPickerButtons =
            new List<GameObject>();
        private int _featureOptionPickerPage;

        private const int FeatureOptionPickerPageSize = 6;

        private GK2SpawnItemControl _spawnItemControl;
        private GameObject _spawnItemRow;
        private GameObject _spawnItemButton;
        private Component _spawnQuantityInput;
        private string _lastSpawnQuantityText = string.Empty;

        private GameObject _itemPickerRoot;
        private Component _itemPickerSearchInput;
        private GameObject _itemPickerPageText;
        private readonly List<GameObject> _itemPickerResultButtons =
            new List<GameObject>();
        private string _lastItemPickerSearch = string.Empty;
        private int _itemPickerPage;

        private const int ItemPickerPageSize = 6;

        private static float BodyViewportTopOffset =>
            GK2UiMetrics.Menu.BodyViewportTopOffset;

        private static float BodyViewportHeight =>
            GK2UiMetrics.Menu.BodyViewportHeight;

        private readonly Dictionary<string, GameObject> _tabButtons =
            new Dictionary<string, GameObject>();

        private readonly List<GK2MenuAction> _registeredMenuActions =
            new List<GK2MenuAction>();

        private readonly Dictionary<GK2MenuAction, GameObject> _registeredActionButtons =
            new Dictionary<GK2MenuAction, GameObject>();

        private readonly Dictionary<string, Func<string>> _tabNotices =
            new Dictionary<string, Func<string>>(StringComparer.OrdinalIgnoreCase);

        private string _activeTab = "General";
        private bool _built;

        public static ModMenuController Create(ManualLogSource logger)
        {
            GameObject host = GameObject.Find("GK2PlusModMenuController");

            if (host != null)
            {
                ModMenuController existing = host.GetComponent<ModMenuController>();
                if (existing != null)
                {
                    return existing;
                }
            }

            host = new GameObject("GK2PlusModMenuController");
            DontDestroyOnLoad(host);

            ModMenuController controller = host.AddComponent<ModMenuController>();
            controller._logger = logger;
            return controller;
        }

        public void RegisterMenuAction(GK2MenuAction action)
        {
            if (action == null)
            {
                return;
            }

            if (_registeredMenuActions.Any(existing =>
                string.Equals(existing.Id, action.Id, StringComparison.OrdinalIgnoreCase)))
            {
                _logger?.LogWarning(
                    $"GK2+ ignored duplicate menu action id '{action.Id}'.");
                return;
            }

            _registeredMenuActions.Add(action);

            if (_built &&
                _contentRoot != null &&
                _menuButtonLabelTemplate != null &&
                _menuButtonSprite != null)
            {
                BuildRegisteredActionButtons();
                SetActiveTab(_activeTab);
            }
        }

        public void RegisterFeatureToggleControl(
            GK2FeatureToggleControl control)
        {
            if (control == null)
            {
                return;
            }

            GK2FeatureToggleControl existing =
                _featureToggleControls.FirstOrDefault(candidate =>
                    string.Equals(
                        candidate.Id,
                        control.Id,
                        StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                _featureToggleControls.Remove(existing);
            }

            _featureToggleControls.Add(control);

            if (_built &&
                _contentRoot != null &&
                _bodyTextTemplate != null &&
                _menuButtonLabelTemplate != null &&
                _menuButtonSprite != null)
            {
                BuildFeatureToggleRows();
                BuildFeatureOptionRows();
                SetActiveTab(_activeTab);
            }
        }

        public void RegisterFeatureOptionControl(
            GK2FeatureOptionControl control)
        {
            if (control == null)
            {
                return;
            }

            GK2FeatureOptionControl existing =
                _featureOptionControls.FirstOrDefault(candidate =>
                    string.Equals(
                        candidate.Id,
                        control.Id,
                        StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                _featureOptionControls.Remove(existing);
            }

            _featureOptionControls.Add(control);

            if (_built &&
                _contentRoot != null &&
                _bodyTextTemplate != null &&
                _menuButtonLabelTemplate != null &&
                _menuButtonSprite != null)
            {
                BuildFeatureOptionRows();
                SetActiveTab(_activeTab);
            }
        }

        public void RegisterSpawnItemControl(
            GK2SpawnItemControl control)
        {
            _spawnItemControl = control;

            if (_built &&
                _contentRoot != null &&
                _menuButtonLabelTemplate != null &&
                _menuButtonSprite != null)
            {
                BuildSpawnItemRow();
                SetActiveTab(_activeTab);
            }
        }

        public void RegisterTabNotice(
            string tab,
            Func<string> noticeProvider)
        {
            if (string.IsNullOrWhiteSpace(tab) ||
                noticeProvider == null)
            {
                return;
            }

            _tabNotices[tab] = noticeProvider;

            if (_built &&
                string.Equals(
                    _activeTab,
                    tab,
                    StringComparison.OrdinalIgnoreCase))
            {
                SetText(
                    _pageText,
                    GetPlaceholderText(_activeTab));
            }
        }

        private void Start()
        {
            StartCoroutine(BootstrapWhenReady());
        }

        private IEnumerator BootstrapWhenReady()
        {
            for (int frame = 0; frame < 7200; frame++)
            {
                Component mainMenu;
                RectTransform uiRoot;
                GameObject bodyTemplate;
                GameObject buttonLabelTemplate;

                if (TryGetReadyContext(
                    out mainMenu,
                    out uiRoot,
                    out bodyTemplate,
                    out buttonLabelTemplate))
                {
                    try
                    {
                        BuildMenu(
                            uiRoot,
                            bodyTemplate,
                            buttonLabelTemplate);

                        _built = true;
                        _logger?.LogInfo(
                            "GK2+ mod menu shell ready under persistent GUIElements.Root. " +
                            $"Press {GK2MenuInputSettings.CurrentHotkey} to toggle" +
                            (GK2MenuInputSettings.IsControllerShortcutEnabled
                                ? " or hold L3 + R3."
                                : "."));
                    }
                    catch (Exception ex)
                    {
                        CleanupPartialMenu();
                        _logger?.LogError($"GK2+ mod menu shell build failed once: {ex}");
                    }

                    yield break;
                }

                yield return null;
            }

            _logger?.LogWarning("GK2+ mod menu shell timed out waiting for native UI.");
        }

        private void Update()
        {
            if (!_built || _menuRoot == null)
            {
                return;
            }

            KeyCode hotkey =
                GK2MenuInputSettings.CurrentHotkey;

            if (Input.GetKeyDown(hotkey))
            {
                _logger?.LogInfo(
                    $"GK2+ {hotkey} detected; toggling mod menu.");
                ToggleMenu();
                return;
            }

            if (UpdateControllerMenuShortcut())
            {
                return;
            }

            bool controllerClose =
                GK2MenuInputSettings.IsControllerShortcutEnabled &&
                Input.GetKeyDown(
                    KeyCode.JoystickButton1);

            if (_menuRoot.activeSelf &&
                (Input.GetKeyDown(KeyCode.Escape) ||
                 controllerClose))
            {
                HandleCloseRequest();
                return;
            }

            if (_menuRoot.activeSelf)
            {
                UpdateSpawnQuantityFromInput();
                UpdateItemPickerSearch();
                UpdateFeatureOptionPickerSearch();
            }
        }

        private bool UpdateControllerMenuShortcut()
        {
            if (!GK2MenuInputSettings.IsControllerShortcutEnabled)
            {
                _controllerChordStartedAt =
                    -1f;
                _controllerChordLatched =
                    false;
                return false;
            }

            bool chordHeld =
                Input.GetKey(
                    KeyCode.JoystickButton8) &&
                Input.GetKey(
                    KeyCode.JoystickButton9);

            if (!chordHeld)
            {
                _controllerChordStartedAt =
                    -1f;
                _controllerChordLatched =
                    false;
                return false;
            }

            if (_controllerChordLatched)
            {
                return false;
            }

            if (_controllerChordStartedAt < 0f)
            {
                _controllerChordStartedAt =
                    Time.unscaledTime;
                return false;
            }

            if (Time.unscaledTime -
                _controllerChordStartedAt <
                GK2MenuInputSettings.ControllerHoldSeconds)
            {
                return false;
            }

            _controllerChordLatched =
                true;

            _logger?.LogInfo(
                "GK2+ controller L3 + R3 hold detected; toggling mod menu.");

            ToggleMenu();
            return true;
        }

        private void HandleCloseRequest()
        {
            if (_featureOptionPickerRoot != null)
            {
                CloseFeatureOptionPicker();
            }
            else if (_itemPickerRoot != null)
            {
                CloseItemPicker();
            }
            else
            {
                HideMenu();
            }
        }

        private bool TryGetReadyContext(
            out Component mainMenu,
            out RectTransform uiRoot,
            out GameObject bodyTemplate,
            out GameObject buttonLabelTemplate)
        {
            mainMenu = null;
            uiRoot = null;
            bodyTemplate = null;
            buttonLabelTemplate = null;

            foreach (var behaviour in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
            {
                if (behaviour == null ||
                    behaviour.GetType().Name != "UIMainMenuWindow" ||
                    !behaviour.gameObject.activeInHierarchy)
                {
                    continue;
                }

                mainMenu = behaviour;
                break;
            }

            if (mainMenu == null)
            {
                return false;
            }

            GUIElements guiElements = GUIElements.Instance;
            if (guiElements == null ||
                guiElements.Root == null ||
                !guiElements.gameObject.activeInHierarchy)
            {
                return false;
            }

            uiRoot = guiElements.Root;

            Transform root = mainMenu.transform;
            Transform hint = root.Find("Bg/Vertical Group/ButtonTipsStr");
            Transform buttonLabel = root.Find("Bg/Vertical Group/NewGame/Content/Back/Label");

            if (hint == null || buttonLabel == null)
            {
                return false;
            }

            if (FindTmp(hint.gameObject) == null ||
                FindTmp(buttonLabel.gameObject) == null)
            {
                return false;
            }

            bodyTemplate = hint.gameObject;
            buttonLabelTemplate = buttonLabel.gameObject;
            return true;
        }

        private void BuildMenu(
            Transform uiRoot,
            GameObject bodyTemplate,
            GameObject buttonLabelTemplate)
        {
            _theme =
                GK2UiTheme.Resolve(
                    _logger,
                    bodyTemplate,
                    buttonLabelTemplate);

            Sprite dividerSprite =
                _theme?.DividerSprite;
            Sprite redButtonSprite =
                _theme?.ButtonSprite;

            GameObject titleTemplate =
                _theme?.TitleTextTemplate?.gameObject ??
                buttonLabelTemplate;

            GameObject listTextTemplate =
                _theme?.BodyTextTemplate?.gameObject ??
                bodyTemplate;

            if (_theme == null ||
                _theme.WindowFrameSprite == null ||
                _theme.WindowBackgroundSprite == null ||
                redButtonSprite == null)
            {
                throw new InvalidOperationException(
                    "Required native GK2 UI theme assets are not loaded.");
            }

            GK2UiWindowView shell =
                GK2UiWindowBuilder.CreateModal(
                    uiRoot,
                    RootObjectName,
                    _theme,
                    GK2UiMetrics.Menu.WindowSize);

            GameObject overlay =
                shell.Root;

            GameObject window =
                shell.Window;

            RectTransform windowRect =
                shell.WindowRect;

            GameObject safeArea =
                shell.SafeArea;

            RectTransform safeAreaRect =
                shell.SafeAreaRect;

            Canvas overlayCanvas =
                overlay.GetComponent<Canvas>();

            _menuRoot =
                overlay;

            _logger?.LogInfo(
                $"GK2+ UI framework built mod-menu shell; " +
                $"safeSize={safeAreaRect.rect.size}.");

            GameObject headerGroup =
                GK2UiFactory.CreateRect(
                    window.transform,
                    "HeaderGroup",
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -11f),
                    new Vector2(-22f, 26f));

            GameObject headerBack =
                GK2UiFactory.CreateImage(
                    headerGroup.transform,
                    "Background",
                    _theme.MainWindowHeaderSprite,
                    Image.Type.Sliced,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    Color.white,
                    false);

            RectTransform headerBackRect =
                headerBack.GetComponent<RectTransform>();
            headerBackRect.offsetMin =
                Vector2.zero;
            headerBackRect.offsetMax =
                Vector2.zero;

            if (_theme.MainWindowHeaderSideSprite != null)
            {
                GK2UiFactory.CreateImage(
                    headerGroup.transform,
                    "DecorLeft",
                    _theme.MainWindowHeaderSideSprite,
                    Image.Type.Simple,
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(14f, 0f),
                    new Vector2(28f, 26f),
                    Color.white,
                    false);

                GameObject rightDecor =
                    GK2UiFactory.CreateImage(
                        headerGroup.transform,
                        "DecorRight",
                        _theme.MainWindowHeaderSideSprite,
                        Image.Type.Simple,
                        new Vector2(1f, 0.5f),
                        new Vector2(1f, 0.5f),
                        new Vector2(0.5f, 0.5f),
                        new Vector2(-14f, 0f),
                        new Vector2(28f, 26f),
                        Color.white,
                        false);

                rightDecor.transform.localScale =
                    new Vector3(-1f, 1f, 1f);
            }

            CreateNativeTitleText(
                _theme?.MainWindowTabTextTemplate?.gameObject ??
                titleTemplate,
                window.transform,
                "GK2+ Mod Menu",
                new Vector2(
                    0f,
                    GK2UiMetrics.Menu.HeaderTitleY),
                new Vector2(250f, 22f),
                1f
            );

            CreateBodyText(
                bodyTemplate,
                window.transform,
                "VersionText",
                GameCompatibility.DisplayText,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(18f, -17f),
                new Vector2(190f, 14f),
                8f,
                "Left"
            );

            _headerToggleHint =
                CreateBodyText(
                    bodyTemplate,
                    window.transform,
                    "HeaderToggleHint",
                    GK2MenuInputSettings.GetToggleHint(),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(-112f, -17f),
                    new Vector2(94f, 14f),
                    7.5f,
                    "Center"
                );

            _headerCloseHint =
                CreateBodyText(
                    bodyTemplate,
                    window.transform,
                    "HeaderCloseHint",
                    GK2MenuInputSettings.GetCloseHint(),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(-18f, -17f),
                    new Vector2(82f, 14f),
                    7.5f,
                    "Center"
                );

            float gap =
                GK2UiMetrics.Menu.TabGap;

            float availableTabWidth =
                GK2UiMetrics.Menu.WindowSize.x -
                (GK2UiMetrics.Native.WindowBackInset * 2f);

            float tabHeight =
                GK2UiMetrics.Menu.TabHeight;

            float[] tabWidths =
                new float[Tabs.Length];

            float preferredWidthSum =
                0f;

            for (int i = 0; i < Tabs.Length; i++)
            {
                tabWidths[i] =
                    GK2UiFactory.GetNativeWindowTabPreferredWidth(
                        _theme,
                        Tabs[i],
                        40f);

                preferredWidthSum +=
                    tabWidths[i];
            }

            float spacingWidth =
                (Tabs.Length - 1) * gap;

            float usableTabWidth =
                availableTabWidth - spacingWidth;

            if (preferredWidthSum > usableTabWidth &&
                preferredWidthSum > 0f)
            {
                float scale =
                    usableTabWidth /
                    preferredWidthSum;

                for (int i = 0; i < tabWidths.Length; i++)
                {
                    tabWidths[i] *=
                        scale;
                }
            }

            float rowWidth =
                spacingWidth;

            for (int i = 0; i < tabWidths.Length; i++)
            {
                rowWidth +=
                    tabWidths[i];
            }

            float currentLeft =
                -rowWidth / 2f;

            // Native CharacterWindow tabs sit on a continuous
            // main_window-header_1 strip. Without this backing the inactive
            // tabs look like floating labels and the active tab looks like an
            // isolated ornate button.
            GameObject tabStrip =
                GK2UiFactory.CreateImage(
                    window.transform,
                    "TabsHeaderGroup",
                    _theme.MainWindowHeaderSprite,
                    Image.Type.Sliced,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(
                        0f,
                        GK2UiMetrics.Menu.TabY),
                    new Vector2(
                        availableTabWidth,
                        GK2UiMetrics.Menu.TabHeight),
                    Color.white,
                    false);

            // The vanilla CharacterWindow has extra end-cap ornament outside
            // its five-tab group. With nine tabs packed into the mod menu those
            // decorations overlap the first/last labels, so the continuous
            // native header strip is the correct boundary here.

            if (_theme.MainWindowHeaderSeparatorSprite != null)
            {
                float separatorCursor =
                    currentLeft;

                for (int i = 0; i < Tabs.Length - 1; i++)
                {
                    separatorCursor +=
                        tabWidths[i];

                    float separatorX =
                        separatorCursor +
                        (gap / 2f);

                    GK2UiFactory.CreateImage(
                        window.transform,
                        "TabSeparator" + i,
                        _theme.MainWindowHeaderSeparatorSprite,
                        Image.Type.Simple,
                        new Vector2(0.5f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(
                            separatorX,
                            GK2UiMetrics.Menu.TabY),
                        new Vector2(40f, 26f),
                        Color.white,
                        false);

                    separatorCursor +=
                        gap;
                }
            }

            float tabCursor =
                currentLeft;

            for (int i = 0; i < Tabs.Length; i++)
            {
                string tab =
                    Tabs[i];

                float tabWidth =
                    tabWidths[i];

                float x =
                    tabCursor +
                    (tabWidth / 2f);

                Button tabButton =
                    GK2UiFactory.CreateNativeWindowTab(
                        window.transform,
                        tab + "TabButton",
                        _theme,
                        tab,
                        new Vector2(
                            x,
                            GK2UiMetrics.Menu.TabY),
                        new Vector2(
                            tabWidth,
                            tabHeight),
                        string.Equals(
                            tab,
                            _activeTab,
                            StringComparison.OrdinalIgnoreCase),
                        () => SetActiveTab(tab));

                _tabButtons[tab] =
                    tabButton.gameObject;

                tabCursor +=
                    tabWidth + gap;
            }

            GameObject content = new GameObject(
                "Content",
                typeof(RectTransform)
            );
            content.transform.SetParent(window.transform, false);

            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 1f);
            contentRect.anchorMax = new Vector2(0.5f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition =
                new Vector2(
                    0f,
                    GK2UiMetrics.Menu.ContentTopY);
            contentRect.sizeDelta =
                GK2UiMetrics.Menu.ContentSize;

            Image contentBg = content.AddComponent<Image>();
            contentBg.sprite =
                _theme.ContentStoneSprite;
            contentBg.type =
                _theme.ContentStoneSprite != null
                    ? Image.Type.Tiled
                    : Image.Type.Simple;
            contentBg.color =
                _theme.ContentStoneSprite != null
                    ? Color.white
                    : _theme.ContentBackground;
            contentBg.raycastTarget = false;

            GameObject bodyViewport = new GameObject(
                "BodyViewport",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(RectMask2D)
            );
            bodyViewport.transform.SetParent(content.transform, false);

            RectTransform bodyViewportRect =
                bodyViewport.GetComponent<RectTransform>();

            bodyViewportRect.anchorMin = new Vector2(0.5f, 1f);
            bodyViewportRect.anchorMax = new Vector2(0.5f, 1f);
            bodyViewportRect.pivot = new Vector2(0.5f, 1f);
            bodyViewportRect.anchoredPosition =
                new Vector2(0f, -BodyViewportTopOffset);
            bodyViewportRect.sizeDelta =
                new Vector2(
                    GK2UiMetrics.Menu.ContentSize.x - 4f,
                    BodyViewportHeight);

            Image bodyViewportImage =
                bodyViewport.GetComponent<Image>();
            bodyViewportImage.color =
                new Color(0f, 0f, 0f, 0.001f);
            bodyViewportImage.raycastTarget = true;

            GameObject bodyContent = new GameObject(
                "BodyScrollContent",
                typeof(RectTransform)
            );
            bodyContent.transform.SetParent(
                bodyViewport.transform,
                false);

            RectTransform bodyContentRect =
                bodyContent.GetComponent<RectTransform>();

            bodyContentRect.anchorMin =
                new Vector2(0f, 1f);
            bodyContentRect.anchorMax =
                new Vector2(1f, 1f);
            bodyContentRect.pivot =
                new Vector2(0.5f, 1f);
            bodyContentRect.anchoredPosition =
                Vector2.zero;
            bodyContentRect.sizeDelta =
                new Vector2(0f, BodyViewportHeight);

            ScrollRect bodyScroll =
                content.AddComponent<ScrollRect>();
            bodyScroll.viewport =
                bodyViewportRect;
            bodyScroll.content =
                bodyContentRect;
            bodyScroll.horizontal = false;
            bodyScroll.vertical = true;
            bodyScroll.movementType =
                ScrollRect.MovementType.Clamped;
            bodyScroll.inertia = false;
            bodyScroll.scrollSensitivity = 18f;
            bodyScroll.verticalNormalizedPosition = 1f;

            GameObject scrollbarObject = new GameObject(
                "BodyScrollbar",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Scrollbar)
            );
            scrollbarObject.transform.SetParent(
                content.transform,
                false);

            RectTransform scrollbarRect =
                scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin =
                new Vector2(1f, 1f);
            scrollbarRect.anchorMax =
                new Vector2(1f, 1f);
            scrollbarRect.pivot =
                new Vector2(1f, 1f);
            scrollbarRect.anchoredPosition =
                new Vector2(-4f, -BodyViewportTopOffset);
            scrollbarRect.sizeDelta =
                new Vector2(5f, BodyViewportHeight);

            Image scrollbarTrack =
                scrollbarObject.GetComponent<Image>();
            scrollbarTrack.color =
                new Color(0.05f, 0.05f, 0.06f, 0.82f);
            scrollbarTrack.raycastTarget = true;

            GameObject handle = new GameObject(
                "Handle",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            handle.transform.SetParent(
                scrollbarObject.transform,
                false);

            RectTransform handleRect =
                handle.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            Image handleImage =
                handle.GetComponent<Image>();
            handleImage.sprite =
                redButtonSprite;
            handleImage.type =
                Image.Type.Sliced;
            handleImage.color =
                new Color(0.88f, 0.88f, 0.92f, 0.92f);
            handleImage.raycastTarget = true;

            Scrollbar bodyScrollbar =
                scrollbarObject.GetComponent<Scrollbar>();
            bodyScrollbar.handleRect =
                handleRect;
            bodyScrollbar.targetGraphic =
                handleImage;
            bodyScrollbar.direction =
                Scrollbar.Direction.BottomToTop;
            bodyScrollbar.transition =
                Selectable.Transition.ColorTint;

            Navigation scrollbarNavigation =
                bodyScrollbar.navigation;
            scrollbarNavigation.mode =
                Navigation.Mode.None;
            bodyScrollbar.navigation =
                scrollbarNavigation;

            bodyScroll.verticalScrollbar =
                bodyScrollbar;
            bodyScroll.verticalScrollbarVisibility =
                ScrollRect.ScrollbarVisibility.Permanent;

            scrollbarObject.SetActive(false);

            _contentRoot = bodyContent;
            _bodyContentRect = bodyContentRect;
            _bodyScrollRect = bodyScroll;
            _bodyScrollbar = bodyScrollbar;
            _bodyTextTemplate = listTextTemplate;
            _menuButtonLabelTemplate = buttonLabelTemplate;
            _menuButtonSprite = redButtonSprite;

            GK2UiSectionHeaderView pageHeader =
                GK2UiSectionHeaderBuilder.Create(
                    content.transform,
                    _theme,
                    "PageHeader",
                    "General",
                    22f);

            pageHeader.Rect.anchoredPosition =
                new Vector2(0f, -5f);
            pageHeader.Rect.sizeDelta =
                new Vector2(-24f, 22f);

            _pageTitle =
                pageHeader.Title.gameObject;

            _pageText = CreateBodyText(
                listTextTemplate,
                _contentRoot.transform,
                "PageText",
                "",
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, BodyY(-40f)),
                new Vector2(
                    GK2UiMetrics.Menu.BodyContentWidth,
                    GK2UiMetrics.Menu.PageTextWideHeight),
                GK2UiMetrics.Menu.BodyFontSize,
                "Center"
            );

            // Extra tracking only for tab dialogue/body copy.
            // Buttons keep their current spacing, which is already dialed in.
            Component pageTmp = FindTmp(_pageText);
            SetProperty(pageTmp, "characterSpacing", 2.45f);
            SetProperty(pageTmp, "wordSpacing", 1.25f);

            _featureSettingsNote = CreateBodyText(
                listTextTemplate,
                _contentRoot.transform,
                "FeatureSettingsNote",
                "Return to the main menu to change feature settings safely.",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, BodyY(-157f)),
                new Vector2(340f, 12f),
                7f,
                "Center"
            );
            _featureSettingsNote.SetActive(false);

            BuildTabBodySections();

            _githubButton = CreateActionButton(
                buttonLabelTemplate,
                _contentRoot.transform,
                redButtonSprite,
                "GitHub",
                new Vector2(-132f, BodyY(-96f)),
                new Vector2(108f, 26f)
            );
            _githubButton.GetComponent<Button>().onClick.AddListener(
                () => Application.OpenURL(ProjectLinks.GitHubUrl));

            _nexusButton = CreateActionButton(
                buttonLabelTemplate,
                _contentRoot.transform,
                redButtonSprite,
                ProjectLinks.HasNexusUrl ? "Nexus Mods" : "Nexus Soon",
                new Vector2(0f, BodyY(-96f)),
                new Vector2(120f, 26f)
            );
            Button nexus = _nexusButton.GetComponent<Button>();
            nexus.interactable = ProjectLinks.HasNexusUrl;

            if (ProjectLinks.HasNexusUrl)
            {
                nexus.onClick.AddListener(
                    () => Application.OpenURL(ProjectLinks.NexusUrl));
            }

            _bugButton = CreateActionButton(
                buttonLabelTemplate,
                _contentRoot.transform,
                redButtonSprite,
                "Report Bug",
                new Vector2(132f, BodyY(-96f)),
                new Vector2(108f, 26f)
            );
            _bugButton.GetComponent<Button>().onClick.AddListener(
                () => Application.OpenURL(ProjectLinks.BugReportUrl));

            BuildRegisteredActionButtons();
            BuildFeatureToggleRows();
            BuildFeatureOptionRows();
            BuildSpawnItemRow();

            if (dividerSprite != null)
            {
                CreateFixedImage(
                    window.transform,
                    "FooterDivider",
                    dividerSprite,
                    Image.Type.Sliced,
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0f, 36f),
                    new Vector2(
                        GK2UiMetrics.Menu.WindowSize.x - 110f,
                        5f)
                );
            }


GameObject closeButton = CreateActionButton(
                buttonLabelTemplate,
                safeArea.transform,
                redButtonSprite,
                "Close",
                Vector2.zero,
                new Vector2(72f, 20f)
            );
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();

            // The safe area already accounts for the visible frame thickness.
            // Add 4 logical UI units of breathing room inside that safe edge.
            // At GK2 PixelSize 2 this renders as ~8 physical screen pixels.
            closeRect.SetInsetAndSizeFromParentEdge(
                RectTransform.Edge.Right,
                4f,
                72f
            );

            closeRect.SetInsetAndSizeFromParentEdge(
                RectTransform.Edge.Bottom,
                4f,
                20f
            );

            closeRect.localScale = Vector3.one;
            closeRect.localRotation = Quaternion.identity;

            Vector3[] windowCorners = new Vector3[4];
            Vector3[] safeCorners = new Vector3[4];
            Vector3[] closeCorners = new Vector3[4];

            windowRect.GetWorldCorners(windowCorners);
            safeAreaRect.GetWorldCorners(safeCorners);
            closeRect.GetWorldCorners(closeCorners);

            float safeRightGapPx = safeCorners[2].x - closeCorners[2].x;
            float safeBottomGapPx = closeCorners[0].y - safeCorners[0].y;
            float totalRightGapPx = windowCorners[2].x - closeCorners[2].x;
            float totalBottomGapPx = closeCorners[0].y - windowCorners[0].y;

            _logger?.LogInfo(
                $"GK2+ Close safe dock: " +
                $"safeRightGapPx={safeRightGapPx:0.##}, " +
                $"safeBottomGapPx={safeBottomGapPx:0.##}, " +
                $"totalRightGapPx={totalRightGapPx:0.##}, " +
                $"totalBottomGapPx={totalBottomGapPx:0.##}, " +
                $"buttonBL={closeCorners[0]}, buttonTR={closeCorners[2]}"
            );

Button close = closeButton.GetComponent<Button>();
            close.onClick.AddListener(HideMenu);

            SetActiveTab("General");

            // Phase 1: keep the proven manual shell lifecycle, but host it on
            // the persistent GUIElements.Root instead of the main-menu window.
            // Native LazyWindow stack integration will follow once we clone a
            // real GK2 window prefab rather than synthesizing the component.
            _menuRoot.SetActive(false);

            _logger?.LogInfo(
                $"GK2+ mod menu host attached to '{uiRoot.name}' " +
                $"(scene='{uiRoot.gameObject.scene.name}', sortingOrder={overlayCanvas.sortingOrder}).");
        }

        private void BuildRegisteredActionButtons()
        {
            foreach (GameObject existing in _registeredActionButtons.Values)
            {
                if (existing != null)
                {
                    Destroy(existing);
                }
            }

            _registeredActionButtons.Clear();

            if (_contentRoot == null ||
                _menuButtonLabelTemplate == null ||
                _menuButtonSprite == null)
            {
                return;
            }

            foreach (IGrouping<string, GK2MenuAction> group in
                _registeredMenuActions.GroupBy(action => action.Tab))
            {
                List<GK2MenuAction> actions = group.ToList();
                int count = actions.Count;

                if (count == 0)
                {
                    continue;
                }

                if (string.Equals(
                        group.Key,
                        "Cheats",
                        StringComparison.OrdinalIgnoreCase))
                {
                    BuildCheatActionButtons(
                        actions);
                    continue;
                }

                const int maxPerRow = 4;
                const float maxRowWidth = 480f;
                const float gap = 8f;
                const float rowGap = 32f;

                float firstRowY = -98f;
                Dictionary<string, float> featurePositions =
                    BuildFeatureControlLayout(group.Key);

                if (featurePositions.Count > 0)
                {
                    firstRowY =
                        Math.Min(
                            firstRowY,
                            featurePositions.Values.Min() - 30f);
                }

                for (int i = 0; i < count; i++)
                {
                    GK2MenuAction action = actions[i];

                    int row = i / maxPerRow;
                    int indexInRow = i % maxPerRow;
                    int rowStart = row * maxPerRow;
                    int rowCount = Mathf.Min(
                        maxPerRow,
                        count - rowStart);

                    float buttonWidth = Mathf.Min(
                        108f,
                        (maxRowWidth - ((rowCount - 1) * gap)) / rowCount);

                    float rowWidth =
                        (rowCount * buttonWidth) +
                        ((rowCount - 1) * gap);

                    float firstX =
                        (-rowWidth / 2f) +
                        (buttonWidth / 2f);

                    float x =
                        firstX +
                        indexInRow * (buttonWidth + gap);

                    float y =
                        firstRowY -
                        row * rowGap;

                    GameObject buttonObject = CreateActionButton(
                        _menuButtonLabelTemplate,
                        _contentRoot.transform,
                        _menuButtonSprite,
                        action.Label,
                        new Vector2(x, BodyY(y)),
                        new Vector2(buttonWidth, 26f));

                    Button button = buttonObject.GetComponent<Button>();
                    button.onClick.AddListener(() =>
                    {
                        if (!action.CanExecute())
                        {
                            _logger?.LogWarning(
                                $"GK2+ menu action '{action.Id}' is currently unavailable.");
                            RefreshRegisteredActionButtons();
                            return;
                        }

                        try
                        {
                            action.Execute();
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(
                                $"GK2+ menu action '{action.Id}' failed: {ex}");
                        }

                        RefreshRegisteredActionButtons();
                    });

                    _registeredActionButtons[action] = buttonObject;
                }
            }

            RefreshRegisteredActionButtons();
        }

        private void RefreshRegisteredActionButtons()
        {
            foreach (var pair in _registeredActionButtons)
            {
                GK2MenuAction action = pair.Key;
                GameObject buttonObject = pair.Value;

                if (buttonObject == null)
                {
                    continue;
                }

                bool visible = string.Equals(
                    action.Tab,
                    _activeTab,
                    StringComparison.OrdinalIgnoreCase);

                buttonObject.SetActive(visible);

                if (visible)
                {
                    Button button = buttonObject.GetComponent<Button>();
                    if (button != null)
                    {
                        button.interactable = action.CanExecute();
                    }
                }
            }
        }

        private Dictionary<string, float> BuildFeatureControlLayout(
            string tab)
        {
            float firstRowY =
                GK2UiMetrics.Menu.ControlFirstRowY;

            float childStep =
                GK2UiMetrics.Menu.ControlChildStep;

            float featureGapStep =
                GK2UiMetrics.Menu.ControlFeatureGap;

            Dictionary<string, float> positions =
                new Dictionary<string, float>(
                    StringComparer.OrdinalIgnoreCase);

            List<GK2FeatureToggleControl> toggles =
                _featureToggleControls
                    .Where(control =>
                        string.Equals(
                            control.Tab,
                            tab,
                            StringComparison.OrdinalIgnoreCase))
                    .OrderBy(control => control.Order)
                    .ThenBy(
                        control => control.Label,
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

            HashSet<GK2FeatureOptionControl> attachedOptions =
                new HashSet<GK2FeatureOptionControl>();

            float y = firstRowY;

            foreach (GK2FeatureToggleControl toggle in toggles)
            {
                positions[toggle.Id] = y;

                List<GK2FeatureOptionControl> children =
                    _featureOptionControls
                        .Where(option =>
                            string.Equals(
                                option.Tab,
                                tab,
                                StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(
                                option.ParentFeatureId,
                                toggle.Id,
                                StringComparison.OrdinalIgnoreCase))
                        .OrderBy(option => option.Order)
                        .ThenBy(
                            option => option.Label,
                            StringComparer.OrdinalIgnoreCase)
                        .ToList();

                bool expanded =
                    !_collapsedFeatureGroups.Contains(
                        toggle.Id);

                foreach (GK2FeatureOptionControl child in children)
                {
                    attachedOptions.Add(
                        child);

                    if (!expanded)
                    {
                        continue;
                    }

                    y -= childStep;
                    positions[child.Id] = y;
                }

                y -= featureGapStep;
            }

            foreach (GK2FeatureOptionControl option in
                _featureOptionControls
                    .Where(option =>
                        string.Equals(
                            option.Tab,
                            tab,
                            StringComparison.OrdinalIgnoreCase) &&
                        !attachedOptions.Contains(option))
                    .OrderBy(option => option.Order)
                    .ThenBy(
                        option => option.Label,
                        StringComparer.OrdinalIgnoreCase))
            {
                positions[option.Id] = y;
                y -= featureGapStep;
            }

            return positions;
        }

        private void BuildFeatureToggleRows()
        {
            foreach (GameObject existing in _featureToggleRows.Values)
            {
                if (existing != null)
                {
                    Destroy(existing);
                }
            }

            foreach (GameObject existing in _featureGroupBackgrounds.Values)
            {
                if (existing != null)
                {
                    Destroy(existing);
                }
            }

            _featureToggleRows.Clear();
            _featureGroupBackgrounds.Clear();

            if (_contentRoot == null ||
                _bodyTextTemplate == null ||
                _menuButtonLabelTemplate == null ||
                _menuButtonSprite == null)
            {
                return;
            }

            foreach (IGrouping<string, GK2FeatureToggleControl> group in
                _featureToggleControls.GroupBy(control => control.Tab))
            {
                Dictionary<string, float> positions =
                    BuildFeatureControlLayout(group.Key);

                List<GK2FeatureToggleControl> controls =
                    group
                        .OrderBy(control => control.Order)
                        .ThenBy(
                            control => control.Label,
                            StringComparer.OrdinalIgnoreCase)
                        .ToList();

                for (int i = 0; i < controls.Count; i++)
                {
                    GK2FeatureToggleControl control =
                        controls[i];

                    float rowY =
                        positions.TryGetValue(
                            control.Id,
                            out float resolvedY)
                            ? resolvedY
                            : GK2UiMetrics.Menu.ControlFirstRowY;

                    List<GK2FeatureOptionControl> childOptions =
                        _featureOptionControls
                            .Where(option =>
                                string.Equals(
                                    option.Tab,
                                    control.Tab,
                                    StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(
                                    option.ParentFeatureId,
                                    control.Id,
                                    StringComparison.OrdinalIgnoreCase))
                            .OrderBy(option => option.Order)
                            .ThenBy(
                                option => option.Label,
                                StringComparer.OrdinalIgnoreCase)
                            .ToList();

                    bool hasChildren =
                        childOptions.Count > 0;

                    bool expanded =
                        !_collapsedFeatureGroups.Contains(
                            control.Id);

                    if (hasChildren)
                    {
                        float groupHeight =
                            GK2UiMetrics.Menu.ControlRowHeight +
                            (expanded
                                ? childOptions.Count *
                                  GK2UiMetrics.Menu.ControlChildStep
                                : 0f);

                        GameObject groupBackground =
                            GK2UiFactory.CreateImage(
                                _contentRoot.transform,
                                control.Id + "FeatureGroupBackground",
                                _theme?.ContentCellSprite,
                                _theme?.ContentCellSprite != null
                                    ? Image.Type.Sliced
                                    : Image.Type.Simple,
                                new Vector2(0.5f, 1f),
                                new Vector2(0.5f, 1f),
                                new Vector2(0.5f, 1f),
                                new Vector2(
                                    0f,
                                    BodyY(rowY)),
                                new Vector2(
                                    GK2UiMetrics.Menu.BodyContentWidth,
                                    groupHeight),
                                _theme?.ContentCellSprite != null
                                    ? Color.white
                                    : (_theme?.RowBackground ??
                                       new Color(
                                           0.12f,
                                           0.13f,
                                           0.16f,
                                           0.88f)),
                                false);

                        _featureGroupBackgrounds[control.Id] =
                            groupBackground;
                    }

                    GK2UiListRowView rowView =
                        GK2UiListRowBuilder.Create(
                            _contentRoot.transform,
                            _theme,
                            control.Id + "FeatureRow",
                            control.Label,
                            BodyY(rowY),
                            "FeatureLabel",
                            "ToggleButton",
                            "OFF",
                            child: false,
                            subtitle: GetFeatureDescription(
                                control.Id),
                            expandable: hasChildren,
                            expanded: expanded,
                            drawBackground: !hasChildren);

                    GameObject row =
                        rowView.Root;

                    if (rowView.ExpandButton != null)
                    {
                        rowView.ExpandButton
                            .onClick
                            .AddListener(() =>
                            {
                                ToggleFeatureGroup(
                                    control.Id);
                            });
                    }

                    GameObject toggleButton =
                        rowView.ActionButton.gameObject;

                    rowView.ActionButton
                        .onClick
                        .AddListener(() =>
                        {
                            if (!string.Equals(
                                DetectContext(),
                                "MainMenu",
                                StringComparison.Ordinal))
                            {
                                _logger?.LogWarning(
                                    $"GK2+ feature '{control.Id}' can only be changed from the main menu.");
                                RefreshFeatureToggleRows();
                                return;
                            }

                            bool nextValue =
                                !control.EnabledProvider();

                            try
                            {
                                control.EnabledChanged(
                                    nextValue);
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(
                                    $"GK2+ feature toggle '{control.Id}' failed: {ex}");
                            }

                            RefreshFeatureToggleRows();
                            RefreshFeatureOptionRows();
                        });

                    _featureToggleRows[control] =
                        row;
                }
            }

            RefreshFeatureToggleRows();
        }

        private void RefreshFeatureToggleRows()
        {
            bool mainMenu =
                string.Equals(
                    DetectContext(),
                    "MainMenu",
                    StringComparison.Ordinal);

            foreach (var pair in _featureToggleRows)
            {
                GK2FeatureToggleControl control =
                    pair.Key;

                GameObject row =
                    pair.Value;

                if (row == null)
                {
                    continue;
                }

                bool visible =
                    string.Equals(
                        control.Tab,
                        _activeTab,
                        StringComparison.OrdinalIgnoreCase);

                row.SetActive(visible);

                if (_featureGroupBackgrounds.TryGetValue(
                        control.Id,
                        out GameObject groupBackground) &&
                    groupBackground != null)
                {
                    groupBackground.SetActive(
                        visible);
                }

                if (!visible)
                {
                    continue;
                }

                Button toggle =
                    row.transform
                        .Find("ToggleButton")
                        ?.GetComponent<Button>();

                if (toggle == null)
                {
                    continue;
                }

                // Keep the compact native button label bounded to the
                // button even while the control is read-only in gameplay.
                // Longer feature status text belongs in the page/status copy,
                // not inside a 92-unit action button.
                string text =
                    control.EnabledProvider()
                        ? "ON"
                        : "OFF";

                SetButtonText(
                    toggle.gameObject,
                    text);

                toggle.interactable =
                    mainMenu;
            }

            if (_featureSettingsNote != null)
            {
                bool hasFeatureControls =
                    HasFeatureControlsForTab(
                        _activeTab);

                _featureSettingsNote.SetActive(
                    hasFeatureControls &&
                    !mainMenu &&
                    !string.Equals(
                        _activeTab,
                        "Tracker",
                        StringComparison.OrdinalIgnoreCase));

                UpdateFeatureSettingsNotePosition();
            }
        }

        private void UpdateFeatureSettingsNotePosition()
        {
            if (_featureSettingsNote == null ||
                !HasFeatureControlsForTab(_activeTab))
            {
                return;
            }

            Dictionary<string, float> positions =
                BuildFeatureControlLayout(_activeTab);

            if (positions.Count == 0)
            {
                return;
            }

            float lowestRowY =
                positions.Values.Min();

            RectTransform noteRect =
                _featureSettingsNote.GetComponent<RectTransform>();

            if (noteRect != null)
            {
                noteRect.anchoredPosition =
                    new Vector2(
                        0f,
                        BodyY(lowestRowY - 44f));
            }
        }

        private bool HasFeatureControlsForTab(
            string tab)
        {
            return
                _featureToggleControls.Any(control =>
                    string.Equals(
                        control.Tab,
                        tab,
                        StringComparison.OrdinalIgnoreCase)) ||
                _featureOptionControls.Any(control =>
                    string.Equals(
                        control.Tab,
                        tab,
                        StringComparison.OrdinalIgnoreCase));
        }

        private void ToggleFeatureGroup(
            string featureId)
        {
            if (string.IsNullOrWhiteSpace(
                    featureId))
            {
                return;
            }

            if (!_collapsedFeatureGroups.Add(
                    featureId))
            {
                _collapsedFeatureGroups.Remove(
                    featureId);
            }

            BuildFeatureToggleRows();
            BuildFeatureOptionRows();
            SetActiveTab(
                _activeTab);
        }

        private void BuildFeatureOptionRows()
        {
            foreach (GameObject existing in _featureOptionRows.Values)
            {
                if (existing != null)
                {
                    Destroy(existing);
                }
            }

            _featureOptionRows.Clear();

            if (_contentRoot == null ||
                _bodyTextTemplate == null ||
                _menuButtonLabelTemplate == null ||
                _menuButtonSprite == null)
            {
                return;
            }

            foreach (IGrouping<string, GK2FeatureOptionControl> group in
                _featureOptionControls.GroupBy(control => control.Tab))
            {
                Dictionary<string, float> positions =
                    BuildFeatureControlLayout(group.Key);

                List<GK2FeatureOptionControl> controls =
                    group
                        .OrderByDescending(control =>
                            positions.TryGetValue(
                                control.Id,
                                out float rowY)
                                ? rowY
                                : float.MinValue)
                        .ThenBy(
                            control => control.Label,
                            StringComparer.OrdinalIgnoreCase)
                        .ToList();

                for (int i = 0; i < controls.Count; i++)
                {
                    GK2FeatureOptionControl control =
                        controls[i];

                    float rowY =
                        positions.TryGetValue(
                            control.Id,
                            out float resolvedY)
                            ? resolvedY
                            : GK2UiMetrics.Menu.ControlFirstRowY;

                    bool child =
                        !string.IsNullOrWhiteSpace(
                            control.ParentFeatureId);

                    GK2UiListRowView rowView =
                        GK2UiListRowBuilder.Create(
                            _contentRoot.transform,
                            _theme,
                            control.Id + "FeatureOptionRow",
                            control.Label,
                            BodyY(rowY),
                            "FeatureOptionLabel",
                            "OptionButton",
                            "Select",
                            child,
                            subtitle: GetFeatureOptionDescription(
                                control.Id),
                            drawBackground: !child);

                    GameObject row =
                        rowView.Root;

                    GameObject optionButton =
                        rowView.ActionButton.gameObject;

                    rowView.ActionButton
                        .onClick
                        .AddListener(() =>
                        {
                            bool mainMenuContext = string.Equals(
                                DetectContext(),
                                "MainMenu",
                                StringComparison.Ordinal);

                            if (!mainMenuContext &&
                                !control.AllowInGameEditing)
                            {
                                return;
                            }

                            HandleFeatureOptionClick(
                                control);
                        });

                    _featureOptionRows[control] =
                        row;
                }
            }

            RefreshFeatureOptionRows();
        }

        private void RefreshFeatureOptionRows()
        {
            bool mainMenu =
                string.Equals(
                    DetectContext(),
                    "MainMenu",
                    StringComparison.Ordinal);

            foreach (var pair in _featureOptionRows)
            {
                GK2FeatureOptionControl control =
                    pair.Key;

                GameObject row =
                    pair.Value;

                if (row == null)
                {
                    continue;
                }

                bool parentCollapsed =
                    !string.IsNullOrWhiteSpace(
                        control.ParentFeatureId) &&
                    _collapsedFeatureGroups.Contains(
                        control.ParentFeatureId);

                bool visible =
                    !parentCollapsed &&
                    string.Equals(
                        control.Tab,
                        _activeTab,
                        StringComparison.OrdinalIgnoreCase);

                row.SetActive(visible);

                if (!visible)
                {
                    continue;
                }

                Button button =
                    row.transform
                        .Find("OptionButton")
                        ?.GetComponent<Button>();

                if (button == null)
                {
                    continue;
                }

                string currentValue =
                    control.ValueProvider() ?? string.Empty;

                IReadOnlyList<GK2FeatureOption> options =
                    control.OptionsProvider() ??
                    Array.Empty<GK2FeatureOption>();

                GK2FeatureOption selected =
                    options.FirstOrDefault(option =>
                        string.Equals(
                            option.Value,
                            currentValue,
                            StringComparison.OrdinalIgnoreCase));

                string label =
                    selected != null
                        ? selected.Label
                        : currentValue;

                string buttonText =
                    string.IsNullOrWhiteSpace(label)
                        ? "Select"
                        : label;

                if ((mainMenu || control.AllowInGameEditing) &&
                    !IsBinaryFeatureOptionControl(
                        control,
                        options))
                {
                    buttonText += "  ▼";
                }

                SetButtonText(
                    button.gameObject,
                    buttonText);

                bool enabled =
                    control.EnabledProvider();

                button.interactable =
                    (mainMenu || control.AllowInGameEditing) &&
                    enabled;

                Transform labelTransform =
                    row.transform.Find(
                        "FeatureOptionLabel");

                Component labelText =
                    labelTransform == null
                        ? null
                        : FindTmp(labelTransform.gameObject);

                SetProperty(
                    labelText,
                    "color",
                    enabled
                        ? new Color(1f, 0.84f, 0.48f, 1f)
                        : new Color(0.56f, 0.56f, 0.60f, 0.68f));
            }
        }

        private void HandleFeatureOptionClick(
            GK2FeatureOptionControl control)
        {
            if (control == null ||
                !control.EnabledProvider())
            {
                return;
            }

            bool mainMenuContext =
                string.Equals(
                    DetectContext(),
                    "MainMenu",
                    StringComparison.Ordinal);

            if (!mainMenuContext &&
                !control.AllowInGameEditing)
            {
                return;
            }

            IReadOnlyList<GK2FeatureOption> options =
                control.OptionsProvider() ??
                Array.Empty<GK2FeatureOption>();

            if (!IsBinaryFeatureOptionControl(
                    control,
                    options))
            {
                OpenFeatureOptionPicker(
                    control);
                return;
            }

            string currentValue =
                control.ValueProvider() ??
                string.Empty;

            GK2FeatureOption current =
                options.FirstOrDefault(option =>
                    string.Equals(
                        option.Value,
                        currentValue,
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        option.Label,
                        currentValue,
                        StringComparison.OrdinalIgnoreCase));

            bool currentState;

            if (current != null)
            {
                if (!TryGetBinaryOptionState(
                        current,
                        out currentState))
                {
                    OpenFeatureOptionPicker(
                        control);
                    return;
                }
            }
            else if (!TryParseBinaryToken(
                         currentValue,
                         out currentState))
            {
                OpenFeatureOptionPicker(
                    control);
                return;
            }

            GK2FeatureOption next =
                options.FirstOrDefault(option =>
                {
                    bool optionState;

                    return TryGetBinaryOptionState(
                            option,
                            out optionState) &&
                        optionState != currentState;
                });

            if (next == null)
            {
                OpenFeatureOptionPicker(
                    control);
                return;
            }

            try
            {
                control.ValueChanged(
                    next.Value);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    $"GK2+ binary feature option '{control.Id}' failed: {ex}");
            }

            RefreshFeatureToggleRows();
            RefreshFeatureOptionRows();
        }

        private static bool IsBinaryFeatureOptionControl(
            GK2FeatureOptionControl control,
            IReadOnlyList<GK2FeatureOption> options)
        {
            if (control == null ||
                options == null ||
                options.Count != 2)
            {
                return false;
            }

            bool firstState;
            bool secondState;

            return
                TryGetBinaryOptionState(
                    options[0],
                    out firstState) &&
                TryGetBinaryOptionState(
                    options[1],
                    out secondState) &&
                firstState != secondState;
        }

        private static bool TryGetBinaryOptionState(
            GK2FeatureOption option,
            out bool state)
        {
            state = false;

            if (option == null)
            {
                return false;
            }

            if (TryParseBinaryToken(
                    option.Value,
                    out state))
            {
                return true;
            }

            return TryParseBinaryToken(
                option.Label,
                out state);
        }

        private static bool TryParseBinaryToken(
            string token,
            out bool state)
        {
            state = false;

            if (string.IsNullOrWhiteSpace(
                    token))
            {
                return false;
            }

            string normalized =
                token.Trim()
                    .Replace(" ", string.Empty)
                    .Replace("-", string.Empty)
                    .Replace("_", string.Empty)
                    .ToLowerInvariant();

            switch (normalized)
            {
                case "true":
                case "on":
                case "yes":
                case "enabled":
                    state = true;
                    return true;

                case "false":
                case "off":
                case "no":
                case "disabled":
                    state = false;
                    return true;

                default:
                    return false;
            }
        }

        private void OpenFeatureOptionPicker(
            GK2FeatureOptionControl control)
        {
            if (control == null ||
                !control.EnabledProvider() ||
                _menuRoot == null ||
                _menuButtonLabelTemplate == null ||
                _menuButtonSprite == null)
            {
                return;
            }

            bool mainMenuContext =
                string.Equals(
                    DetectContext(),
                    "MainMenu",
                    StringComparison.Ordinal);

            if (!mainMenuContext &&
                !control.AllowInGameEditing)
            {
                return;
            }

            CloseFeatureOptionPicker();

            _activeFeatureOptionPickerControl =
                control;
            _featureOptionPickerPage = 0;
            _lastFeatureOptionPickerSearch = string.Empty;
            _featureOptionPickerSearchInput = null;

            _featureOptionPickerRoot = new GameObject(
                "GK2PlusFeatureOptionPicker",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            _featureOptionPickerRoot.transform.SetParent(
                _menuRoot.transform,
                false);
            _featureOptionPickerRoot.transform.SetAsLastSibling();

            RectTransform overlayRect =
                _featureOptionPickerRoot.GetComponent<RectTransform>();

            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image dimmer =
                _featureOptionPickerRoot.GetComponent<Image>();

            dimmer.color =
                new Color(0.02f, 0.01f, 0.02f, 0.82f);
            dimmer.raycastTarget = true;

            GameObject panel = new GameObject(
                "Panel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            panel.transform.SetParent(
                _featureOptionPickerRoot.transform,
                false);

            RectTransform panelRect =
                panel.GetComponent<RectTransform>();

            panelRect.anchorMin =
                new Vector2(0.5f, 0.5f);
            panelRect.anchorMax =
                new Vector2(0.5f, 0.5f);
            panelRect.pivot =
                new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition =
                Vector2.zero;
            panelRect.sizeDelta =
                new Vector2(
                    300f,
                    control.Searchable
                        ? 244f
                        : 220f);

            Image panelImage =
                panel.GetComponent<Image>();

            panelImage.color =
                new Color(0.20f, 0.035f, 0.09f, 0.99f);
            panelImage.raycastTarget = true;

            CreateNativeTitleText(
                _menuButtonLabelTemplate,
                panel.transform,
                control.Label,
                new Vector2(0f, -16f),
                new Vector2(220f, 20f),
                0.60f);

            if (control.Searchable)
            {
                _featureOptionPickerSearchInput =
                    CreateTmpInputField(
                        _menuButtonLabelTemplate,
                        panel.transform,
                        "FeatureOptionSearch",
                        string.Empty,
                        new Vector2(0f, -45f),
                        new Vector2(230f, 20f),
                        numericOnly: false,
                        placeholder: "Search...");
            }

            _featureOptionPickerPageText = CreateBodyText(
                _bodyTextTemplate,
                panel.transform,
                "FeatureOptionPickerPage",
                "",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 13f),
                new Vector2(90f, 16f),
                8f,
                "Center");

            GameObject prev = CreateActionButton(
                _menuButtonLabelTemplate,
                panel.transform,
                _menuButtonSprite,
                "Prev",
                new Vector2(
                    -92f,
                    control.Searchable
                        ? -214f
                        : -190f),
                new Vector2(58f, 18f));

            prev.GetComponent<Button>().onClick.AddListener(() =>
            {
                _featureOptionPickerPage =
                    Math.Max(
                        0,
                        _featureOptionPickerPage - 1);

                RebuildFeatureOptionPicker();
            });

            GameObject next = CreateActionButton(
                _menuButtonLabelTemplate,
                panel.transform,
                _menuButtonSprite,
                "Next",
                new Vector2(
                    92f,
                    control.Searchable
                        ? -214f
                        : -190f),
                new Vector2(58f, 18f));

            next.GetComponent<Button>().onClick.AddListener(() =>
            {
                _featureOptionPickerPage++;
                RebuildFeatureOptionPicker();
            });

            GameObject cancel = CreateActionButton(
                _menuButtonLabelTemplate,
                panel.transform,
                _menuButtonSprite,
                "Cancel",
                new Vector2(
                    0f,
                    control.Searchable
                        ? -214f
                        : -190f),
                new Vector2(68f, 18f));

            cancel.GetComponent<Button>().onClick.AddListener(
                CloseFeatureOptionPicker);

            RebuildFeatureOptionPicker();
        }

        private void RebuildFeatureOptionPicker()
        {
            if (_featureOptionPickerRoot == null ||
                _activeFeatureOptionPickerControl == null)
            {
                return;
            }

            foreach (GameObject button in _featureOptionPickerButtons)
            {
                if (button != null)
                {
                    Destroy(button);
                }
            }

            _featureOptionPickerButtons.Clear();

            Transform panel =
                _featureOptionPickerRoot.transform.Find("Panel");

            if (panel == null)
            {
                return;
            }

            IReadOnlyList<GK2FeatureOption> options =
                _activeFeatureOptionPickerControl.OptionsProvider() ??
                Array.Empty<GK2FeatureOption>();

            string search =
                _lastFeatureOptionPickerSearch ??
                string.Empty;

            if (_activeFeatureOptionPickerControl.Searchable &&
                !string.IsNullOrWhiteSpace(search))
            {
                options =
                    options
                        .Where(option =>
                            (!string.IsNullOrWhiteSpace(option.Label) &&
                             option.Label.IndexOf(
                                 search,
                                 StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (!string.IsNullOrWhiteSpace(option.Value) &&
                             option.Value.IndexOf(
                                 search,
                                 StringComparison.OrdinalIgnoreCase) >= 0))
                        .ToList();
            }

            int pageCount =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        options.Count /
                        (double)FeatureOptionPickerPageSize));

            _featureOptionPickerPage =
                Mathf.Clamp(
                    _featureOptionPickerPage,
                    0,
                    pageCount - 1);

            int start =
                _featureOptionPickerPage *
                FeatureOptionPickerPageSize;

            List<GK2FeatureOption> page =
                options
                    .Skip(start)
                    .Take(FeatureOptionPickerPageSize)
                    .ToList();

            string currentValue =
                _activeFeatureOptionPickerControl.ValueProvider() ??
                string.Empty;

            for (int i = 0; i < page.Count; i++)
            {
                GK2FeatureOption option =
                    page[i];

                bool selected =
                    string.Equals(
                        option.Value,
                        currentValue,
                        StringComparison.OrdinalIgnoreCase);

                GameObject button = CreateActionButton(
                    _menuButtonLabelTemplate,
                    panel,
                    _menuButtonSprite,
                    selected
                        ? option.Label + "  ✓"
                        : option.Label,
                    new Vector2(
                        0f,
                        (_activeFeatureOptionPickerControl.Searchable
                            ? -76f
                            : -55f) -
                        (i * 21f)),
                    new Vector2(220f, 18f));

                button.GetComponent<Button>().onClick.AddListener(() =>
                {
                    try
                    {
                        _activeFeatureOptionPickerControl.ValueChanged(
                            option.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(
                            $"GK2+ feature option '{_activeFeatureOptionPickerControl.Id}' failed: {ex}");
                    }

                    CloseFeatureOptionPicker();
                    RefreshFeatureOptionRows();
                    RefreshFeatureToggleRows();
                });

                _featureOptionPickerButtons.Add(
                    button);
            }

            if (_featureOptionPickerPageText != null)
            {
                SetText(
                    _featureOptionPickerPageText,
                    $"{_featureOptionPickerPage + 1}/{pageCount}");
            }
        }

        private void CloseFeatureOptionPicker()
        {
            if (_featureOptionPickerRoot != null)
            {
                Destroy(_featureOptionPickerRoot);
                _featureOptionPickerRoot = null;
            }

            _activeFeatureOptionPickerControl = null;
            _featureOptionPickerPageText = null;
            _featureOptionPickerSearchInput = null;
            _lastFeatureOptionPickerSearch = string.Empty;
            _featureOptionPickerButtons.Clear();
            _featureOptionPickerPage = 0;
        }

        private void UpdateFeatureOptionPickerSearch()
        {
            if (_featureOptionPickerRoot == null ||
                _activeFeatureOptionPickerControl == null ||
                !_activeFeatureOptionPickerControl.Searchable ||
                _featureOptionPickerSearchInput == null)
            {
                return;
            }

            string current =
                GetStringProperty(
                    _featureOptionPickerSearchInput,
                    "text") ??
                string.Empty;

            if (string.Equals(
                    current,
                    _lastFeatureOptionPickerSearch,
                    StringComparison.Ordinal))
            {
                return;
            }

            _lastFeatureOptionPickerSearch =
                current;

            _featureOptionPickerPage = 0;
            RebuildFeatureOptionPicker();
        }

        private void BuildSpawnItemRow()
        {
            if (_spawnItemRow != null)
            {
                Destroy(_spawnItemRow);
                _spawnItemRow = null;
                _spawnItemButton = null;
                _spawnQuantityInput = null;
            }

            if (_spawnItemControl == null ||
                _contentRoot == null ||
                _menuButtonLabelTemplate == null ||
                _menuButtonSprite == null)
            {
                return;
            }

            _spawnItemRow = new GameObject(
                "SpawnItemRow",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            _spawnItemRow.transform.SetParent(
                _contentRoot.transform,
                false);

            RectTransform rowRect =
                _spawnItemRow.GetComponent<RectTransform>();

            rowRect.anchorMin = new Vector2(0.5f, 1f);
            rowRect.anchorMax = new Vector2(0.5f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, BodyY(-262f));
            rowRect.sizeDelta = new Vector2(
                GK2UiMetrics.Menu.BodyContentWidth,
                36f);

            Image rowBackground =
                _spawnItemRow.GetComponent<Image>();

            rowBackground.sprite =
                _theme?.ContentCellSprite;
            rowBackground.type =
                _theme?.ContentCellSprite != null
                    ? Image.Type.Sliced
                    : Image.Type.Simple;
            rowBackground.color =
                _theme?.ContentCellSprite != null
                    ? Color.white
                    : (_theme?.RowBackground ??
                       new Color(0.12f, 0.13f, 0.16f, 0.88f));
            rowBackground.raycastTarget =
                false;

            GameObject spawnButton = CreateActionButton(
                _menuButtonLabelTemplate,
                _spawnItemRow.transform,
                _menuButtonSprite,
                "Spawn",
                new Vector2(220f, -5f),
                new Vector2(92f, 26f));

            RectTransform spawnRect =
                spawnButton.GetComponent<RectTransform>();
            spawnRect.anchorMin =
                new Vector2(0.5f, 1f);
            spawnRect.anchorMax =
                new Vector2(0.5f, 1f);

            spawnButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (!_spawnItemControl.CanSpawn())
                {
                    _logger?.LogWarning(
                        "GK2+ Spawn Item is currently unavailable.");
                    return;
                }

                try
                {
                    _spawnItemControl.Spawn();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(
                        $"GK2+ Spawn Item action failed: {ex}");
                }

                RefreshSpawnItemRow();
            });

            _spawnItemButton = CreateActionButton(
                _menuButtonLabelTemplate,
                _spawnItemRow.transform,
                _menuButtonSprite,
                "Item",
                new Vector2(-72f, -5f),
                new Vector2(300f, 26f));

            _spawnItemButton.GetComponent<Button>().onClick.AddListener(
                OpenItemPicker);

            _spawnQuantityInput = CreateTmpInputField(
                _menuButtonLabelTemplate,
                _spawnItemRow.transform,
                "SpawnQuantity",
                Math.Max(1, _spawnItemControl.QuantityProvider()).ToString(),
                new Vector2(126f, -5f),
                new Vector2(66f, 26f),
                numericOnly: true,
                placeholder: "Qty");

            RefreshSpawnItemRow();
        }

        private void RefreshSpawnItemRow()
        {
            if (_spawnItemRow == null ||
                _spawnItemControl == null)
            {
                return;
            }

            bool visible = string.Equals(
                _activeTab,
                _spawnItemControl.Tab,
                StringComparison.OrdinalIgnoreCase);

            _spawnItemRow.SetActive(visible);

            if (!visible)
            {
                return;
            }

            string selectedId =
                _spawnItemControl.SelectedItemIdProvider() ?? string.Empty;

            IReadOnlyList<GK2ItemOption> options =
                _spawnItemControl.ItemOptionsProvider() ??
                Array.Empty<GK2ItemOption>();

            GK2ItemOption selected = options.FirstOrDefault(option =>
                string.Equals(
                    option.Id,
                    selectedId,
                    StringComparison.OrdinalIgnoreCase));

            string displayName =
                selected != null
                    ? selected.DisplayName
                    : (string.IsNullOrWhiteSpace(selectedId)
                        ? "Select Item"
                        : selectedId);

            if (_spawnItemButton != null)
            {
                SetButtonText(
                    _spawnItemButton,
                    displayName + "  ▼");
            }

            int quantity = Math.Max(
                1,
                _spawnItemControl.QuantityProvider());

            string quantityText = quantity.ToString();

            if (_spawnQuantityInput != null &&
                !string.Equals(
                    GetStringProperty(
                        _spawnQuantityInput,
                        "text"),
                    quantityText,
                    StringComparison.Ordinal))
            {
                SetProperty(
                    _spawnQuantityInput,
                    "text",
                    quantityText);
            }

            _lastSpawnQuantityText = quantityText;

            Button spawnButton =
                _spawnItemRow.transform
                    .Find("SpawnButton")
                    ?.GetComponent<Button>();

            if (spawnButton != null)
            {
                spawnButton.interactable =
                    _spawnItemControl.CanSpawn();
            }
        }

        private void UpdateSpawnQuantityFromInput()
        {
            if (_spawnQuantityInput == null ||
                _spawnItemControl == null ||
                _spawnItemRow == null ||
                !_spawnItemRow.activeInHierarchy)
            {
                return;
            }

            string text =
                GetStringProperty(
                    _spawnQuantityInput,
                    "text") ?? string.Empty;

            if (string.Equals(
                text,
                _lastSpawnQuantityText,
                StringComparison.Ordinal))
            {
                return;
            }

            _lastSpawnQuantityText = text;

            if (!int.TryParse(text, out int quantity))
            {
                return;
            }

            quantity = Mathf.Clamp(
                quantity,
                1,
                10000);

            _spawnItemControl.QuantityChanged(
                quantity);
        }

        private void OpenItemPicker()
        {
            if (_spawnItemControl == null ||
                _menuRoot == null ||
                _menuButtonLabelTemplate == null ||
                _menuButtonSprite == null)
            {
                return;
            }

            CloseItemPicker();

            _itemPickerRoot = new GameObject(
                "GK2PlusItemPicker",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            _itemPickerRoot.transform.SetParent(
                _menuRoot.transform,
                false);
            _itemPickerRoot.transform.SetAsLastSibling();

            RectTransform overlayRect =
                _itemPickerRoot.GetComponent<RectTransform>();

            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image dimmer =
                _itemPickerRoot.GetComponent<Image>();

            dimmer.color =
                new Color(0.02f, 0.01f, 0.02f, 0.82f);
            dimmer.raycastTarget = true;

            GameObject panel = new GameObject(
                "Panel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            panel.transform.SetParent(
                _itemPickerRoot.transform,
                false);

            RectTransform panelRect =
                panel.GetComponent<RectTransform>();

            panelRect.anchorMin =
                new Vector2(0.5f, 0.5f);
            panelRect.anchorMax =
                new Vector2(0.5f, 0.5f);
            panelRect.pivot =
                new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition =
                Vector2.zero;
            panelRect.sizeDelta =
                new Vector2(360f, 232f);

            Image panelImage =
                panel.GetComponent<Image>();

            panelImage.color =
                new Color(0.20f, 0.035f, 0.09f, 0.99f);
            panelImage.raycastTarget = true;

            CreateNativeTitleText(
                _menuButtonLabelTemplate,
                panel.transform,
                "Select Item",
                new Vector2(0f, -14f),
                new Vector2(180f, 20f),
                0.64f);

            _itemPickerSearchInput = CreateTmpInputField(
                _menuButtonLabelTemplate,
                panel.transform,
                "ItemSearch",
                string.Empty,
                new Vector2(0f, -42f),
                new Vector2(310f, 20f),
                numericOnly: false,
                placeholder: "Search item name or id");

            _lastItemPickerSearch = string.Empty;
            _itemPickerPage = 0;

            _itemPickerPageText = CreateBodyText(
                _menuButtonLabelTemplate,
                panel.transform,
                "PickerPage",
                "",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 13f),
                new Vector2(90f, 16f),
                8f,
                "Center");

            GameObject prev = CreateActionButton(
                _menuButtonLabelTemplate,
                panel.transform,
                _menuButtonSprite,
                "Prev",
                new Vector2(-118f, -202f),
                new Vector2(62f, 18f));

            prev.GetComponent<Button>().onClick.AddListener(() =>
            {
                _itemPickerPage =
                    Math.Max(0, _itemPickerPage - 1);
                RebuildItemPickerResults();
            });

            GameObject next = CreateActionButton(
                _menuButtonLabelTemplate,
                panel.transform,
                _menuButtonSprite,
                "Next",
                new Vector2(118f, -202f),
                new Vector2(62f, 18f));

            next.GetComponent<Button>().onClick.AddListener(() =>
            {
                _itemPickerPage++;
                RebuildItemPickerResults();
            });

            GameObject cancel = CreateActionButton(
                _menuButtonLabelTemplate,
                panel.transform,
                _menuButtonSprite,
                "Cancel",
                new Vector2(0f, -202f),
                new Vector2(72f, 18f));

            cancel.GetComponent<Button>().onClick.AddListener(
                CloseItemPicker);

            RebuildItemPickerResults();
        }

        private void UpdateItemPickerSearch()
        {
            if (_itemPickerRoot == null ||
                _itemPickerSearchInput == null)
            {
                return;
            }

            string search =
                GetStringProperty(
                    _itemPickerSearchInput,
                    "text") ?? string.Empty;

            if (string.Equals(
                search,
                _lastItemPickerSearch,
                StringComparison.Ordinal))
            {
                return;
            }

            _lastItemPickerSearch = search;
            _itemPickerPage = 0;
            RebuildItemPickerResults();
        }

        private void RebuildItemPickerResults()
        {
            if (_itemPickerRoot == null ||
                _spawnItemControl == null)
            {
                return;
            }

            foreach (GameObject result in
                _itemPickerResultButtons)
            {
                if (result != null)
                {
                    Destroy(result);
                }
            }

            _itemPickerResultButtons.Clear();

            Transform panel =
                _itemPickerRoot.transform.Find("Panel");

            if (panel == null)
            {
                return;
            }

            string search =
                (_lastItemPickerSearch ?? string.Empty)
                    .Trim();

            IEnumerable<GK2ItemOption> query =
                _spawnItemControl.ItemOptionsProvider() ??
                Array.Empty<GK2ItemOption>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(option =>
                    (option.DisplayName ?? string.Empty)
                        .IndexOf(
                            search,
                            StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (option.Id ?? string.Empty)
                        .IndexOf(
                            search,
                            StringComparison.OrdinalIgnoreCase) >= 0);
            }

            List<GK2ItemOption> filtered =
                query
                    .OrderBy(option =>
                        option.DisplayName,
                        StringComparer.OrdinalIgnoreCase)
                    .ThenBy(option =>
                        option.Id,
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

            int pageCount =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        filtered.Count /
                        (double)ItemPickerPageSize));

            _itemPickerPage =
                Mathf.Clamp(
                    _itemPickerPage,
                    0,
                    pageCount - 1);

            int start =
                _itemPickerPage *
                ItemPickerPageSize;

            List<GK2ItemOption> page =
                filtered
                    .Skip(start)
                    .Take(ItemPickerPageSize)
                    .ToList();

            for (int i = 0; i < page.Count; i++)
            {
                GK2ItemOption option = page[i];

                string label =
                    string.Equals(
                        option.DisplayName,
                        option.Id,
                        StringComparison.OrdinalIgnoreCase)
                        ? option.DisplayName
                        : $"{option.DisplayName}  ({option.Id})";

                GameObject button = CreateActionButton(
                    _menuButtonLabelTemplate,
                    panel,
                    _menuButtonSprite,
                    label,
                    new Vector2(
                        0f,
                        -72f - (i * 21f)),
                    new Vector2(310f, 18f));

                button.GetComponent<Button>().onClick.AddListener(() =>
                {
                    _spawnItemControl.ItemSelected(
                        option.Id);
                    CloseItemPicker();
                    RefreshSpawnItemRow();
                });

                _itemPickerResultButtons.Add(
                    button);
            }

            if (_itemPickerPageText != null)
            {
                SetText(
                    _itemPickerPageText,
                    $"{_itemPickerPage + 1}/{pageCount}  •  {filtered.Count} items");
            }
        }

        private void CloseItemPicker()
        {
            if (_itemPickerRoot != null)
            {
                Destroy(_itemPickerRoot);
                _itemPickerRoot = null;
            }

            _itemPickerSearchInput = null;
            _itemPickerPageText = null;
            _itemPickerResultButtons.Clear();
            _lastItemPickerSearch = string.Empty;
            _itemPickerPage = 0;
        }

        private Component CreateTmpInputField(
            GameObject textTemplate,
            Transform parent,
            string name,
            string text,
            Vector2 anchoredPosition,
            Vector2 size,
            bool numericOnly,
            string placeholder)
        {
            Type inputType = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly =>
                    assembly.GetType(
                        "TMPro.TMP_InputField",
                        false))
                .FirstOrDefault(type =>
                    type != null);

            if (inputType == null)
            {
                throw new InvalidOperationException(
                    "TMPro.TMP_InputField is unavailable.");
            }

            GameObject root = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            root.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                root.GetComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(0.5f, 1f);
            rect.anchorMax =
                new Vector2(0.5f, 1f);
            rect.pivot =
                new Vector2(0.5f, 1f);
            rect.anchoredPosition =
                anchoredPosition;
            rect.sizeDelta =
                size;

            Image background =
                root.GetComponent<Image>();

            background.sprite =
                _menuButtonSprite;
            background.type =
                Image.Type.Sliced;
            background.color =
                new Color(
                    0.62f,
                    0.62f,
                    0.66f,
                    0.92f);
            background.raycastTarget = true;

            GameObject viewport = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(RectMask2D));

            viewport.transform.SetParent(
                root.transform,
                false);

            RectTransform viewportRect =
                viewport.GetComponent<RectTransform>();

            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin =
                new Vector2(5f, 2f);
            viewportRect.offsetMax =
                new Vector2(-5f, -2f);

            GameObject textObject =
                Instantiate(
                    textTemplate,
                    viewport.transform,
                    false);

            textObject.name = "Text";
            textObject.SetActive(true);
            StripLocalization(textObject);

            RectTransform textRect =
                textObject.GetComponent<RectTransform>();

            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.localScale =
                new Vector3(0.55f, 0.55f, 1f);

            Component textTmp =
                FindTmp(textObject);

            SetProperty(
                textTmp,
                "text",
                text ?? string.Empty);
            SetProperty(
                textTmp,
                "enableAutoSizing",
                false);
            TrySetEnumProperty(
                textTmp,
                "alignment",
                "Center");

            GameObject placeholderObject =
                Instantiate(
                    textTemplate,
                    viewport.transform,
                    false);

            placeholderObject.name =
                "Placeholder";
            placeholderObject.SetActive(true);
            StripLocalization(
                placeholderObject);

            RectTransform placeholderRect =
                placeholderObject.GetComponent<RectTransform>();

            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
            placeholderRect.localScale =
                new Vector3(0.48f, 0.48f, 1f);

            Component placeholderTmp =
                FindTmp(
                    placeholderObject);

            SetProperty(
                placeholderTmp,
                "text",
                placeholder ?? string.Empty);
            SetProperty(
                placeholderTmp,
                "color",
                new Color(
                    1f,
                    0.84f,
                    0.48f,
                    0.55f));
            TrySetEnumProperty(
                placeholderTmp,
                "alignment",
                "Center");

            Component input =
                root.AddComponent(inputType);

            SetProperty(
                input,
                "textViewport",
                viewportRect);
            SetProperty(
                input,
                "textComponent",
                textTmp);
            SetProperty(
                input,
                "placeholder",
                placeholderTmp);
            SetProperty(
                input,
                "targetGraphic",
                background);
            SetProperty(
                input,
                "text",
                text ?? string.Empty);
            SetProperty(
                input,
                "characterLimit",
                numericOnly ? 5 : 64);
            SetProperty(
                input,
                "customCaretColor",
                true);
            SetProperty(
                input,
                "caretColor",
                Color.white);
            SetProperty(
                input,
                "caretWidth",
                2);

            TrySetEnumProperty(
                input,
                "lineType",
                "SingleLine");

            if (numericOnly)
            {
                TrySetEnumProperty(
                    input,
                    "contentType",
                    "IntegerNumber");
            }

            return input;
        }

        private static string GetStringProperty(
            Component component,
            string propertyName)
        {
            if (component == null)
            {
                return null;
            }

            PropertyInfo property =
                component.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            return property?.CanRead == true
                ? property.GetValue(
                    component,
                    null) as string
                : null;
        }

        private void SetButtonText(
            GameObject button,
            string text)
        {
            if (button == null)
            {
                return;
            }

            TextMeshProUGUI label =
                button.GetComponentInChildren<TextMeshProUGUI>(
                    true);

            if (label != null)
            {
                label.text =
                    text ?? string.Empty;
            }
        }

        private GameObject CreateActionButton(
            GameObject textTemplate,
            Transform parent,
            Sprite sprite,
            string text,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            Button button =
                GK2UiFactory.CreateNativeRedButton(
                    parent,
                    text + "Button",
                    _theme,
                    text,
                    anchoredPosition,
                    size);

            Navigation navigation =
                button.navigation;
            navigation.mode =
                Navigation.Mode.Automatic;
            button.navigation =
                navigation;

            return button.gameObject;
        }

        private void SetActiveTab(string tabName)
        {
            bool tabChanged =
                !string.Equals(
                    _activeTab,
                    tabName,
                    StringComparison.OrdinalIgnoreCase);

            _activeTab = tabName;

            foreach (var kvp in _tabButtons)
            {
                GK2UiFactory.SetNativeWindowTabSelected(
                    kvp.Value,
                    string.Equals(
                        kvp.Key,
                        tabName,
                        StringComparison.OrdinalIgnoreCase));
            }

            SetText(_pageTitle, tabName);
            SetText(_pageText, GetPlaceholderText(tabName));
            LayoutPageTextForTab(tabName);
            RefreshTabBodyDecor();

            bool more = tabName == "More";

            if (_githubButton != null) _githubButton.SetActive(more);
            if (_nexusButton != null) _nexusButton.SetActive(more);
            if (_bugButton != null) _bugButton.SetActive(more);

            RefreshRegisteredActionButtons();
            RefreshFeatureToggleRows();
            RefreshFeatureOptionRows();
            RefreshSpawnItemRow();
            RefreshBodyScrollBounds(tabChanged);
        }

        private static float BodyY(float legacyY)
        {
            return legacyY + BodyViewportTopOffset;
        }

        private void RefreshBodyScrollBounds(
            bool resetToTop)
        {
            if (_contentRoot == null ||
                _bodyContentRect == null ||
                _bodyScrollRect == null)
            {
                return;
            }

            float requiredHeight =
                BodyViewportHeight;

            foreach (Transform child in
                _contentRoot.transform)
            {
                if (child == null ||
                    !child.gameObject.activeSelf)
                {
                    continue;
                }

                RectTransform childRect =
                    child as RectTransform;

                if (childRect == null)
                {
                    continue;
                }

                float bottomDepth =
                    -childRect.anchoredPosition.y +
                    (childRect.rect.height *
                     childRect.pivot.y);

                requiredHeight =
                    Mathf.Max(
                        requiredHeight,
                        bottomDepth);
            }

            _bodyContentRect.sizeDelta =
                new Vector2(
                    _bodyContentRect.sizeDelta.x,
                    requiredHeight);

            bool overflow =
                requiredHeight >
                BodyViewportHeight + 0.5f;

            if (_bodyScrollbar != null)
            {
                _bodyScrollbar.gameObject.SetActive(
                    overflow);

                if (overflow)
                {
                    _bodyScrollbar.size =
                        Mathf.Clamp01(
                            BodyViewportHeight /
                            requiredHeight);
                }
            }

            if (resetToTop ||
                !overflow)
            {
                _bodyScrollRect.verticalNormalizedPosition =
                    1f;
            }
        }

        private void LayoutPageTextForTab(string tab)
        {
            if (_pageText == null)
            {
                return;
            }

            RectTransform rect =
                _pageText.GetComponent<RectTransform>();

            if (rect == null)
            {
                return;
            }

            rect.anchoredPosition =
                new Vector2(0f, BodyY(-30f));

            rect.sizeDelta =
                new Vector2(
                    GK2UiMetrics.Menu.BodyContentWidth,
                    string.Equals(
                        tab,
                        "Cheats",
                        StringComparison.OrdinalIgnoreCase)
                        ? 34f
                        : GK2UiMetrics.Menu.PageTextCompactHeight);
        }

        private string GetPlaceholderText(string tab)
        {
            if (_tabNotices.TryGetValue(
                    tab,
                    out Func<string> dynamicNoticeProvider))
            {
                string dynamicNotice =
                    dynamicNoticeProvider();

                if (!string.IsNullOrWhiteSpace(dynamicNotice))
                {
                    return dynamicNotice;
                }
            }

            switch (tab)
            {
                case "General":
                    return
                        "Core interface and quality-of-life features.";
                case "Inventory":
                    return
                        "Inventory capacity, access, and shared-storage behavior.";
                case "Crafting":
                    return
                        "Workbench compatibility and crafting quality-of-life.";
                case "Farming":
                    return
                        "Planting behavior and farming quality-of-life.";
                case "Movement":
                    return
                        "Player movement controls and speed options.";
                case "Tracker":
                    return
                        "Quest, craft, and item tracking behavior.";
                case "Zombies":
                    return
                        "Zombie management features will appear here as they are added.";
                case "Cheats":
                    return
                        "Cheat actions are protected by GK2+ save-safety checks.";
                case "More":
                    return
                        "Project links, support, and build information.";
                default:
                    return tab;
            }
        }

        private string GetFeatureDescription(
            string id)
        {
            switch (id ?? string.Empty)
            {
                case "quest-journal":
                    return
                        "Replaces the native quest tree with an RPG-style journal.";
                case "stack-sizes":
                    return
                        "Raises native stack limits for stackable inventory items.";
                case "shared-chests":
                    return
                        "Access eligible storage while preserving native inventory behavior.";
                case "backwards-compatible-extensions":
                    return
                        "Lets supported upgraded extensions satisfy lower-tier recipe requirements.";
                case "continuous-planting":
                    return
                        "Keeps the selected seed active while more of that seed remains.";
                case "sprinting":
                    return
                        "Hold the configured sprint key to temporarily move faster.";
                case "unified-tracker":
                    return
                        "Pin quests, crafts, and item targets to a compact live HUD.";
                default:
                    return
                        "GK2+ feature setting.";
            }
        }

        private string GetFeatureOptionDescription(
            string id)
        {
            switch (id ?? string.Empty)
            {
                case "menu.hotkey":
                    return
                        "Keyboard key used to open or close GK2+.";
                case "menu.controller-shortcut":
                    return
                        "Hold L3 + R3 to toggle GK2+; B closes it.";
                case "stack-sizes.multiplier":
                    return
                        "Multiplier applied to GK2's native stack limits.";
                case "sprinting.key":
                    return
                        "Keyboard key held during normal movement.";
                case "sprinting.multiplier":
                    return
                        "Temporary movement-speed multiplier while sprinting.";
                default:
                    return null;
            }
        }

        private void BuildTabBodySections()
        {
            foreach (List<GameObject> objects in
                     _tabBodyDecor.Values)
            {
                foreach (GameObject obj in objects)
                {
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
            }

            _tabBodyDecor.Clear();

            AddTabSection(
                "General",
                "General Settings",
                -64f);
            AddTabSection(
                "Inventory",
                "Inventory Features",
                -64f);
            AddTabSection(
                "Crafting",
                "Crafting Features",
                -64f);
            AddTabSection(
                "Farming",
                "Farming Features",
                -64f);
            AddTabSection(
                "Movement",
                "Movement",
                -64f);
            AddTabSection(
                "Tracker",
                "Tracker Settings",
                -64f);
            AddTabSection(
                "Zombies",
                "Zombie Systems",
                -64f);

            AddTabSection(
                "Cheats",
                "Currency",
                -64f);
            AddTabSection(
                "Cheats",
                "Player",
                -168f);
            AddTabSection(
                "Cheats",
                "Item Spawning",
                -232f);

            AddTabSection(
                "More",
                "Project Links",
                -64f);
            AddTabSection(
                "More",
                "About",
                -134f);

            GameObject moreAboutText =
                CreateBodyText(
                    _bodyTextTemplate,
                    _contentRoot.transform,
                    "MoreAboutText",
                    "Graveyard Keeper Plus\n" +
                    GameCompatibility.DisplayText +
                    "\nNative-style quality-of-life tools for GK2.",
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(
                        0f,
                        BodyY(-166f)),
                    new Vector2(
                        GK2UiMetrics.Menu.BodyContentWidth,
                        46f),
                    9f,
                    "Center");

            RegisterTabBodyDecor(
                "More",
                moreAboutText);

            RefreshTabBodyDecor();
        }

        private void AddTabSection(
            string tab,
            string title,
            float y)
        {
            GK2UiSectionHeaderView section =
                GK2UiSectionHeaderBuilder.Create(
                    _contentRoot.transform,
                    _theme,
                    tab + title.Replace(
                        " ",
                        string.Empty) + "Section",
                    title,
                    20f);

            section.Rect.anchoredPosition =
                new Vector2(
                    0f,
                    BodyY(y));

            section.Rect.sizeDelta =
                new Vector2(
                    GK2UiMetrics.Menu.BodyContentWidth,
                    20f);

            RegisterTabBodyDecor(
                tab,
                section.Root);
        }

        private void RegisterTabBodyDecor(
            string tab,
            GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            if (!_tabBodyDecor.TryGetValue(
                    tab,
                    out List<GameObject> objects))
            {
                objects =
                    new List<GameObject>();

                _tabBodyDecor[tab] =
                    objects;
            }

            objects.Add(
                obj);
        }

        private void RefreshTabBodyDecor()
        {
            foreach (var pair in _tabBodyDecor)
            {
                bool visible =
                    string.Equals(
                        pair.Key,
                        _activeTab,
                        StringComparison.OrdinalIgnoreCase);

                foreach (GameObject obj in pair.Value)
                {
                    if (obj != null)
                    {
                        obj.SetActive(
                            visible);
                    }
                }
            }
        }

        private void BuildCheatActionButtons(
            List<GK2MenuAction> actions)
        {
            List<GK2MenuAction> silver =
                actions
                    .Where(action =>
                        action.Label.IndexOf(
                            "Silver",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

            List<GK2MenuAction> gold =
                actions
                    .Where(action =>
                        action.Label.IndexOf(
                            "Gold",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

            List<GK2MenuAction> player =
                actions
                    .Where(action =>
                        action.Label.IndexOf(
                            "Silver",
                            StringComparison.OrdinalIgnoreCase) < 0 &&
                        action.Label.IndexOf(
                            "Gold",
                            StringComparison.OrdinalIgnoreCase) < 0)
                    .ToList();

            BuildCheatButtonRow(
                silver,
                -94f,
                108f);
            BuildCheatButtonRow(
                gold,
                -126f,
                108f);
            BuildCheatButtonRow(
                player,
                -198f,
                150f);
        }

        private void BuildCheatButtonRow(
            List<GK2MenuAction> actions,
            float y,
            float preferredWidth)
        {
            if (actions == null ||
                actions.Count == 0)
            {
                return;
            }

            const float gap = 10f;
            float maxWidth =
                GK2UiMetrics.Menu.BodyContentWidth - 16f;

            float width =
                Mathf.Min(
                    preferredWidth,
                    (maxWidth -
                     ((actions.Count - 1) * gap)) /
                    actions.Count);

            float rowWidth =
                (actions.Count * width) +
                ((actions.Count - 1) * gap);

            float firstX =
                (-rowWidth / 2f) +
                (width / 2f);

            for (int i = 0; i < actions.Count; i++)
            {
                GK2MenuAction action =
                    actions[i];

                GameObject buttonObject =
                    CreateActionButton(
                        _menuButtonLabelTemplate,
                        _contentRoot.transform,
                        _menuButtonSprite,
                        action.Label,
                        new Vector2(
                            firstX +
                            i * (width + gap),
                            BodyY(y)),
                        new Vector2(
                            width,
                            26f));

                Button button =
                    buttonObject.GetComponent<Button>();

                button.onClick.AddListener(() =>
                {
                    if (!action.CanExecute())
                    {
                        _logger?.LogWarning(
                            $"GK2+ menu action '{action.Id}' is currently unavailable.");
                        RefreshRegisteredActionButtons();
                        return;
                    }

                    try
                    {
                        action.Execute();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(
                            $"GK2+ menu action '{action.Id}' failed: {ex}");
                    }

                    RefreshRegisteredActionButtons();
                });

                _registeredActionButtons[action] =
                    buttonObject;
            }
        }

        public void RefreshActiveTab()
        {
            if (_built &&
                _pageTitle != null &&
                _pageText != null)
            {
                RefreshInputHints();
                SetActiveTab(_activeTab);
            }
        }

        private void RefreshInputHints()
        {
            SetText(
                _headerToggleHint,
                GK2MenuInputSettings.GetToggleHint());

            SetText(
                _headerCloseHint,
                GK2MenuInputSettings.GetCloseHint());
        }

        private void FocusActiveTabForController()
        {
            if (!GK2MenuInputSettings.IsControllerShortcutEnabled ||
                EventSystem.current == null ||
                !_tabButtons.TryGetValue(
                    _activeTab,
                    out GameObject tabButton) ||
                tabButton == null)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(
                tabButton);
        }

        public void ToggleMenu()
        {
            if (!_built || _menuRoot == null)
            {
                _logger?.LogWarning(
                    $"GK2+ {GK2MenuInputSettings.CurrentHotkey} toggle ignored because the menu shell is not ready.");
                return;
            }

            bool show = !_menuRoot.activeSelf;
            _menuRoot.SetActive(show);

            if (show)
            {
                _menuRoot.transform.SetAsLastSibling();
                RefreshInputHints();
                SetActiveTab(_activeTab);
                FocusActiveTabForController();
            }

            _logger?.LogInfo(
                $"GK2+ mod menu {(show ? "shown" : "hidden")} in {DetectContext()} context; " +
                $"parent='{_menuRoot.transform.parent?.name ?? "<none>"}'.");
        }

        public void ShowMenu()
        {
            if (!_built || _menuRoot == null)
            {
                return;
            }

            _menuRoot.SetActive(true);
            _menuRoot.transform.SetAsLastSibling();
            RefreshInputHints();
            SetActiveTab(_activeTab);
            FocusActiveTabForController();
        }

        public void HideMenu()
        {
            CloseFeatureOptionPicker();
            CloseItemPicker();

            if (_menuRoot != null)
            {
                _menuRoot.SetActive(false);
            }
        }

        public void ShutdownController()
        {
            HideMenu();
            CleanupPartialMenu();

            if (this != null)
            {
                Destroy(this.gameObject);
            }
        }

        private static string DetectContext()
        {
            foreach (var behaviour in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
            {
                if (behaviour != null &&
                    behaviour.GetType().Name == "UIMainMenuWindow" &&
                    behaviour.gameObject.activeInHierarchy)
                {
                    return "MainMenu";
                }
            }

            return "Gameplay";
        }

        private void CleanupPartialMenu()
        {
            if (_menuRoot != null)
            {
                Destroy(_menuRoot);
                _menuRoot = null;
            }

            _tabButtons.Clear();
            _registeredActionButtons.Clear();
            _featureToggleRows.Clear();
            _featureGroupBackgrounds.Clear();
            _featureOptionRows.Clear();
            _collapsedFeatureGroups.Clear();
            _tabNotices.Clear();
            _pageTitle = null;
            _pageText = null;
            _featureSettingsNote = null;
            _headerToggleHint = null;
            _headerCloseHint = null;
            _tabBodyDecor.Clear();
            _featureOptionPickerRoot = null;
            _activeFeatureOptionPickerControl = null;
            _featureOptionPickerPageText = null;
            _featureOptionPickerSearchInput = null;
            _lastFeatureOptionPickerSearch = string.Empty;
            _featureOptionPickerButtons.Clear();
            _githubButton = null;
            _nexusButton = null;
            _bugButton = null;
            _contentRoot = null;
            _bodyContentRect = null;
            _bodyScrollRect = null;
            _bodyScrollbar = null;
            _bodyTextTemplate = null;
            _menuButtonLabelTemplate = null;
            _menuButtonSprite = null;
            _theme = null;
            _spawnItemRow = null;
            _spawnItemButton = null;
            _spawnQuantityInput = null;
            _itemPickerRoot = null;
            _itemPickerSearchInput = null;
            _itemPickerPageText = null;
            _itemPickerResultButtons.Clear();
            _built = false;
        }

        private GameObject CreateNativeTitleText(
            GameObject template,
            Transform parent,
            string text,
            Vector2 anchoredPosition,
            Vector2 size,
            float scale)
        {
            GameObject clone = Instantiate(template, parent, false);
            clone.name = "NativeTitle";
            clone.SetActive(true);

            StripLocalization(clone);

            RectTransform rect = clone.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            rect.localScale = new Vector3(scale, scale, 1f);

            Component tmp = FindTmp(clone);
            SetProperty(tmp, "text", text);
            TrySetEnumProperty(tmp, "alignment", "Center");

            return clone;
        }

        private GameObject CreateBodyText(
            GameObject template,
            Transform parent,
            string name,
            string text,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            float fontSize,
            string alignment)
        {
            GameObject clone = Instantiate(template, parent, false);
            clone.name = name;
            clone.SetActive(true);

            StripLocalization(clone);

            foreach (Component component in clone.GetComponents<Component>())
            {
                if (component != null &&
                    component.GetType().Name == "LazyButtonTipsStr")
                {
                    Destroy(component);
                }
            }

            RectTransform rect = clone.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            if (anchorMin == Vector2.zero &&
                anchorMax == Vector2.one &&
                size == Vector2.zero)
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = size;
            }

            Component tmp = FindTmp(clone);
            SetProperty(tmp, "text", text);
            SetProperty(tmp, "enableAutoSizing", false);
            SetProperty(tmp, "fontSize", fontSize);
            SetProperty(tmp, "fontSizeMin", fontSize);
            SetProperty(tmp, "fontSizeMax", fontSize);
            SetProperty(tmp, "characterSpacing", 1.45f);
            SetProperty(tmp, "wordSpacing", 0.75f);
            SetProperty(tmp, "lineSpacing", 2.00f);
            SetProperty(tmp, "paragraphSpacing", 2.00f);
            SetProperty(tmp, "margin", Vector4.zero);

            // Preserve the native TMP material/outline. Calling outlineWidth here
            // forces TMP to instantiate a material and can throw when GK2 has not
            // populated materialForRendering yet.
            SetProperty(tmp, "color", new Color(1f, 0.84f, 0.48f, 1f));

            TrySetEnumProperty(tmp, "fontStyle", "Normal");
            TrySetEnumProperty(tmp, "fontWeight", "Regular");
            TrySetEnumProperty(tmp, "alignment", alignment);

            return clone;
        }

        private GameObject CreateImage(
            Transform parent,
            string name,
            Sprite sprite,
            Image.Type type,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject obj = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;

            if (anchorMin == Vector2.zero &&
                anchorMax == Vector2.one &&
                size == Vector2.zero)
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = size;
            }

            Image image = obj.GetComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            image.raycastTarget = false;

            return obj;
        }

        private GameObject CreateStretchImage(
            Transform parent,
            string name,
            Sprite sprite,
            Image.Type type,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject obj = CreateImage(
                parent,
                name,
                sprite,
                type,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero
            );

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            return obj;
        }

        private GameObject CreateFixedImage(
            Transform parent,
            string name,
            Sprite sprite,
            Image.Type type,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            return CreateImage(
                parent,
                name,
                sprite,
                type,
                anchorMin,
                anchorMax,
                pivot,
                anchoredPosition,
                size
            );
        }

        private void StripLocalization(GameObject obj)
        {
            foreach (Component component in obj.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                string typeName = component.GetType().Name;

                if (typeName == "LocalizedLabel" ||
                    typeName == "LocalizedVerticalOffset")
                {
                    Destroy(component);
                }
            }
        }

        private void SetText(GameObject obj, string text)
        {
            Component tmp = FindTmp(obj);
            if (tmp != null)
            {
                SetProperty(tmp, "text", text);
            }
        }

        private static Component FindTmp(GameObject obj)
        {
            if (obj == null)
            {
                return null;
            }

            return obj.GetComponents<Component>()
                .FirstOrDefault(component =>
                    component != null &&
                    component.GetType().Name == "TextMeshProUGUI");
        }

        private static Sprite FindSprite(string name)
        {
            return Resources.FindObjectsOfTypeAll<Sprite>()
                .FirstOrDefault(sprite =>
                    sprite != null &&
                    string.Equals(sprite.name, name, StringComparison.Ordinal));
        }

        private static void TrySetEnumProperty(
            Component component,
            string propertyName,
            string enumValue)
        {
            if (component == null)
            {
                return;
            }

            PropertyInfo property = component.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            if (property == null ||
                !property.CanWrite ||
                !property.PropertyType.IsEnum)
            {
                return;
            }

            try
            {
                object value = Enum.Parse(property.PropertyType, enumValue);
                property.SetValue(component, value, null);
            }
            catch
            {
            }
        }

        private static void SetProperty(
            Component component,
            string propertyName,
            object value)
        {
            if (component == null)
            {
                return;
            }

            PropertyInfo property = component.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            if (property != null && property.CanWrite)
            {
                property.SetValue(component, value, null);
            }
        }
    }
}














