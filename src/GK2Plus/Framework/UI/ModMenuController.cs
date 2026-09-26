using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using GK2Plus.Core;
using UnityEngine;
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
            "Zombies",
            "Cheats",
            "More"
        };

        private ManualLogSource _logger;
        private GameObject _menuRoot;
        private GameObject _pageTitle;
        private GameObject _pageText;
        private GameObject _featureSettingsNote;

        private GameObject _githubButton;
        private GameObject _nexusButton;
        private GameObject _bugButton;
        private GameObject _contentRoot;
        private GameObject _bodyTextTemplate;
        private GameObject _menuButtonLabelTemplate;
        private Sprite _menuButtonSprite;

        private readonly List<GK2FeatureToggleControl> _featureToggleControls =
            new List<GK2FeatureToggleControl>();

        private readonly Dictionary<GK2FeatureToggleControl, GameObject> _featureToggleRows =
            new Dictionary<GK2FeatureToggleControl, GameObject>();

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
                            "Press F2 to toggle.");
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

            if (Input.GetKeyDown(KeyCode.F2))
            {
                _logger?.LogInfo("GK2+ F2 detected; toggling mod menu.");
                ToggleMenu();
                return;
            }

            if (_menuRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                if (_itemPickerRoot != null)
                {
                    CloseItemPicker();
                }
                else
                {
                    HideMenu();
                }

                return;
            }

            if (_menuRoot.activeSelf)
            {
                UpdateSpawnQuantityFromInput();
                UpdateItemPickerSearch();
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
            Transform old = uiRoot.Find(RootObjectName);
            if (old != null)
            {
                Destroy(old.gameObject);
            }

            Sprite frameSprite = FindSprite("comm-frame_1-border");
            Sprite bgSprite = FindSprite("titlescreen-menu-bg");
            Sprite dividerSprite = FindSprite("widget_perks-text_decor-drk_1");
            Sprite redButtonSprite = FindSprite("comm-btn-simple_red-active");

            if (frameSprite == null || bgSprite == null || redButtonSprite == null)
            {
                throw new InvalidOperationException(
                    "Required native GK2 window/button sprites are not loaded.");
            }

            GameObject overlay = new GameObject(
                RootObjectName,
                typeof(RectTransform)
            );

            // Keep the visual tree active while cloning TMP/native UI templates.
            // Some GK2/TMP materials are initialized lazily and cloning them under
            // an inactive hierarchy can leave materialForRendering null.
            overlay.transform.SetParent(uiRoot, false);
            overlay.transform.SetAsLastSibling();

            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            _menuRoot = overlay;

            // Native GK2 windows use their own child Canvases/sorting orders.
            // A plain RectTransform under GUIElements.Root can therefore render
            // behind the main menu, HUD prompts, and other LazyWindows even when
            // it is the last sibling. Give GK2+ its own override canvas so an
            // open mod menu is consistently the top interactive window.
            Canvas overlayCanvas = overlay.AddComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 30000;

            if (overlay.GetComponent<GraphicRaycaster>() == null)
            {
                overlay.AddComponent<GraphicRaycaster>();
            }

            GameObject dimmer = CreateImage(
                overlay.transform,
                "Dimmer",
                null,
                Image.Type.Simple,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero
            );

            Image dimmerImage = dimmer.GetComponent<Image>();
            dimmerImage.color = new Color(0.03f, 0.01f, 0.03f, 0.18f);
            dimmerImage.raycastTarget = true;
            dimmer.transform.SetAsFirstSibling();

            GameObject window = new GameObject(
                "Window",
                typeof(RectTransform)
            );
            window.transform.SetParent(overlay.transform, false);
            window.transform.SetAsLastSibling();

            RectTransform windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.anchoredPosition = Vector2.zero;
            windowRect.sizeDelta = new Vector2(440f, 300f);

            GameObject solidBacking = CreateImage(
                window.transform,
                "SolidBacking",
                null,
                Image.Type.Simple,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero
            );
            solidBacking.GetComponent<Image>().color =
                new Color(0.24f, 0.05f, 0.13f, 0.99f);

            GameObject background = CreateStretchImage(
                window.transform,
                "Background",
                bgSprite,
                Image.Type.Sliced,
                new Vector2(-3f, -3f),
                new Vector2(3f, 3f)
            );
            background.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.98f);

            CreateStretchImage(
                window.transform,
                "Frame",
                frameSprite,
                Image.Type.Sliced,
                Vector2.zero,
                Vector2.zero
            );

            // Reusable safe area derived from the native frame's 9-slice border.
            // Controls placed in this RectTransform are positioned relative to
            // the visible inside edge of the frame instead of the raw window rect.
            // Calibrated VISUAL frame inset, in GK2 logical UI units.
            // The sprite's raw 9-slice border is much larger than the visible
            // decorative border and is not appropriate as a content safe area.
            //
            // At the current PixelSize 2 these correspond approximately to:
            // left/right 18 px and top/bottom 14 px before inner padding.
            Vector4 frameInsetsUi = new Vector4(
                9f,   // left
                7f,   // bottom
                9f,   // right
                7f    // top
            );

            GameObject safeArea = new GameObject(
                "ContentSafeArea",
                typeof(RectTransform)
            );
            safeArea.transform.SetParent(window.transform, false);

            RectTransform safeAreaRect = safeArea.GetComponent<RectTransform>();
            safeAreaRect.anchorMin = Vector2.zero;
            safeAreaRect.anchorMax = Vector2.one;
            safeAreaRect.pivot = new Vector2(0.5f, 0.5f);
            safeAreaRect.offsetMin = new Vector2(
                frameInsetsUi.x,
                frameInsetsUi.y
            );
            safeAreaRect.offsetMax = new Vector2(
                -frameInsetsUi.z,
                -frameInsetsUi.w
            );
            safeAreaRect.localScale = Vector3.one;

            _logger?.LogInfo(
                $"GK2+ frame safe area: " +
                $"spriteBorder={frameSprite.border}, " +
                $"visualInsetsUi(L,B,R,T)={frameInsetsUi}, " +
                $"safeSize={safeAreaRect.rect.size}"
            );

            CreateNativeTitleText(
                buttonLabelTemplate,
                window.transform,
                "GK2+ Mod Menu",
                new Vector2(0f, -16f),
                new Vector2(210f, 20f),
                0.72f
            );

            CreateBodyText(
                bodyTemplate,
                window.transform,
                "VersionText",
                $"v{ModInfo.Version}",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(18f, -17f),
                new Vector2(64f, 14f),
                8f,
                "Left"
            );

            CreateBodyText(
                bodyTemplate,
                window.transform,
                "HeaderF2Hint",
                "F2 Toggle",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-94f, -17f),
                new Vector2(58f, 14f),
                8f,
                "Center"
            );

            CreateBodyText(
                bodyTemplate,
                window.transform,
                "HeaderEscHint",
                "ESC Close",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-18f, -17f),
                new Vector2(62f, 14f),
                8f,
                "Center"
            );

            if (dividerSprite != null)
            {
                CreateFixedImage(
                    window.transform,
                    "HeaderDivider",
                    dividerSprite,
                    Image.Type.Sliced,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -35f),
                    new Vector2(390f, 5f)
                );
            }

            float tabWidth = 54f;
            float tabHeight = 18f;
            float gap = 2f;
            float rowWidth = (Tabs.Length * tabWidth) + ((Tabs.Length - 1) * gap);
            float firstX = -rowWidth / 2f + tabWidth / 2f;

            for (int i = 0; i < Tabs.Length; i++)
            {
                string tab = Tabs[i];
                float x = firstX + i * (tabWidth + gap);

                GameObject button = CreateActionButton(
                    buttonLabelTemplate,
                    window.transform,
                    redButtonSprite,
                    tab,
                    new Vector2(x, -52f),
                    new Vector2(tabWidth, tabHeight)
                );

                Button tabButton = button.GetComponent<Button>();
                tabButton.onClick.AddListener(() => SetActiveTab(tab));

                _tabButtons[tab] = button;
            }

            if (dividerSprite != null)
            {
                CreateFixedImage(
                    window.transform,
                    "TabDivider",
                    dividerSprite,
                    Image.Type.Sliced,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -69f),
                    new Vector2(390f, 5f)
                );
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
            contentRect.anchoredPosition = new Vector2(0f, -77f);
            contentRect.sizeDelta = new Vector2(392f, 176f);

            Image contentBg = content.AddComponent<Image>();
            contentBg.color = new Color(0.12f, 0.02f, 0.07f, 0.86f);
            contentBg.raycastTarget = false;

            _contentRoot = content;
            _bodyTextTemplate = bodyTemplate;
            _menuButtonLabelTemplate = buttonLabelTemplate;
            _menuButtonSprite = redButtonSprite;

            _pageTitle = CreateNativeTitleText(
                buttonLabelTemplate,
                content.transform,
                "General",
                new Vector2(0f, -13f),
                new Vector2(190f, 20f),
                0.62f
            );

            _pageText = CreateBodyText(
                bodyTemplate,
                content.transform,
                "PageText",
                "",
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -40f),
                new Vector2(350f, 102f),
                10f,
                "Center"
            );

            // Extra tracking only for tab dialogue/body copy.
            // Buttons keep their current spacing, which is already dialed in.
            Component pageTmp = FindTmp(_pageText);
            SetProperty(pageTmp, "characterSpacing", 2.45f);
            SetProperty(pageTmp, "wordSpacing", 1.25f);

            _featureSettingsNote = CreateBodyText(
                bodyTemplate,
                content.transform,
                "FeatureSettingsNote",
                "Return to the main menu to enable or disable features safely.",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -157f),
                new Vector2(340f, 12f),
                7f,
                "Center"
            );
            _featureSettingsNote.SetActive(false);

            _githubButton = CreateActionButton(
                buttonLabelTemplate,
                content.transform,
                redButtonSprite,
                "GitHub",
                new Vector2(-92f, -142f),
                new Vector2(78f, 20f)
            );
            _githubButton.GetComponent<Button>().onClick.AddListener(
                () => Application.OpenURL(ProjectLinks.GitHubUrl));

            _nexusButton = CreateActionButton(
                buttonLabelTemplate,
                content.transform,
                redButtonSprite,
                ProjectLinks.HasNexusUrl ? "Nexus Mods" : "Nexus Soon",
                new Vector2(0f, -142f),
                new Vector2(92f, 20f)
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
                content.transform,
                redButtonSprite,
                "Report Bug",
                new Vector2(100f, -142f),
                new Vector2(92f, 20f)
            );
            _bugButton.GetComponent<Button>().onClick.AddListener(
                () => Application.OpenURL(ProjectLinks.BugReportUrl));

            BuildRegisteredActionButtons();
            BuildFeatureToggleRows();
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
                    new Vector2(390f, 5f)
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

                const int maxPerRow = 4;
                const float maxRowWidth = 350f;
                const float gap = 6f;
                const float firstRowY = -82f;
                const float rowGap = 24f;

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
                        new Vector2(x, y),
                        new Vector2(buttonWidth, 20f));

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

        private void BuildFeatureToggleRows()
        {
            foreach (GameObject existing in _featureToggleRows.Values)
            {
                if (existing != null)
                {
                    Destroy(existing);
                }
            }

            _featureToggleRows.Clear();

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
                List<GK2FeatureToggleControl> controls =
                    group.ToList();

                for (int i = 0; i < controls.Count; i++)
                {
                    GK2FeatureToggleControl control =
                        controls[i];

                    GameObject row = new GameObject(
                        control.Id + "FeatureRow",
                        typeof(RectTransform));

                    row.transform.SetParent(
                        _contentRoot.transform,
                        false);

                    RectTransform rowRect =
                        row.GetComponent<RectTransform>();

                    rowRect.anchorMin =
                        new Vector2(0.5f, 1f);
                    rowRect.anchorMax =
                        new Vector2(0.5f, 1f);
                    rowRect.pivot =
                        new Vector2(0.5f, 1f);
                    rowRect.anchoredPosition =
                        new Vector2(0f, -88f - (i * 27f));
                    rowRect.sizeDelta =
                        new Vector2(350f, 20f);

                    CreateBodyText(
                        _bodyTextTemplate,
                        row.transform,
                        "FeatureLabel",
                        control.Label,
                        new Vector2(0.5f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(-60f, 0f),
                        new Vector2(210f, 20f),
                        9f,
                        "Left");

                    GameObject toggleButton =
                        CreateActionButton(
                            _menuButtonLabelTemplate,
                            row.transform,
                            _menuButtonSprite,
                            "OFF",
                            new Vector2(124f, 0f),
                            new Vector2(82f, 20f));

                    toggleButton.name =
                        "ToggleButton";

                    toggleButton
                        .GetComponent<Button>()
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

                string text = mainMenu
                    ? (control.EnabledProvider()
                        ? "ON"
                        : "OFF")
                    : control.StatusProvider();

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
                    !mainMenu);
            }
        }

        private bool HasFeatureControlsForTab(
            string tab)
        {
            return _featureToggleControls.Any(control =>
                string.Equals(
                    control.Tab,
                    tab,
                    StringComparison.OrdinalIgnoreCase));
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
                typeof(RectTransform));

            _spawnItemRow.transform.SetParent(
                _contentRoot.transform,
                false);

            RectTransform rowRect =
                _spawnItemRow.GetComponent<RectTransform>();

            rowRect.anchorMin = new Vector2(0.5f, 1f);
            rowRect.anchorMax = new Vector2(0.5f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, -154f);
            rowRect.sizeDelta = new Vector2(350f, 20f);

            GameObject spawnButton = CreateActionButton(
                _menuButtonLabelTemplate,
                _spawnItemRow.transform,
                _menuButtonSprite,
                "Spawn",
                new Vector2(-140f, 0f),
                new Vector2(64f, 20f));

            spawnButton.GetComponent<RectTransform>().anchorMin =
                new Vector2(0.5f, 1f);
            spawnButton.GetComponent<RectTransform>().anchorMax =
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
                new Vector2(-20f, 0f),
                new Vector2(164f, 20f));

            _spawnItemButton.GetComponent<Button>().onClick.AddListener(
                OpenItemPicker);

            _spawnQuantityInput = CreateTmpInputField(
                _menuButtonLabelTemplate,
                _spawnItemRow.transform,
                "SpawnQuantity",
                Math.Max(1, _spawnItemControl.QuantityProvider()).ToString(),
                new Vector2(110f, 0f),
                new Vector2(56f, 20f),
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

            Transform label =
                button.transform.Find("Label");

            if (label != null)
            {
                SetText(
                    label.gameObject,
                    text);
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
            GameObject obj = new GameObject(
                text + "Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button)
            );

            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = obj.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;

            Button button = obj.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Automatic;
            button.navigation = navigation;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 0.92f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.58f, 0.58f, 0.58f, 0.60f);
            colors.fadeDuration = 0.05f;
            button.colors = colors;

            GameObject label = CreateNativeButtonLabel(
                textTemplate,
                obj.transform,
                text,
                0.55f
            );

            return obj;
        }

        private GameObject CreateNativeButtonLabel(
            GameObject template,
            Transform parent,
            string text,
            float scale)
        {
            GameObject clone = Instantiate(template, parent, false);
            clone.name = "Label";
            clone.SetActive(true);

            StripLocalization(clone);

            RectTransform parentRect = parent as RectTransform;
            RectTransform rect = clone.GetComponent<RectTransform>();

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = new Vector3(scale, scale, 1f);

            if (parentRect != null)
            {
                float width = Mathf.Max(1f, parentRect.rect.width / scale);
                float height = Mathf.Max(1f, parentRect.rect.height / scale);
                rect.sizeDelta = new Vector2(width, height);
            }

            Component tmp = FindTmp(clone);

            if (tmp == null)
            {
                throw new InvalidOperationException(
                    "Native button label template has no TextMeshProUGUI.");
            }

            // Preserve the native main-menu font/material/outline.
            // Only the text and tracking are changed.
            SetProperty(tmp, "text", text);
            SetProperty(tmp, "enableAutoSizing", false);
            SetProperty(tmp, "characterSpacing", 0.75f);
            SetProperty(tmp, "wordSpacing", 0.30f);
            TrySetEnumProperty(tmp, "alignment", "Center");

            return clone;
        }
        private void SetActiveTab(string tabName)
        {
            _activeTab = tabName;

            foreach (var kvp in _tabButtons)
            {
                Image image = kvp.Value.GetComponent<Image>();
                if (image == null)
                {
                    continue;
                }

                image.color = kvp.Key == tabName
                    ? Color.white
                    : new Color(0.72f, 0.72f, 0.78f, 0.88f);
            }

            SetText(_pageTitle, tabName);
            SetText(_pageText, GetPlaceholderText(tabName));
            LayoutPageTextForTab(tabName);

            bool more = tabName == "More";

            if (_githubButton != null) _githubButton.SetActive(more);
            if (_nexusButton != null) _nexusButton.SetActive(more);
            if (_bugButton != null) _bugButton.SetActive(more);

            RefreshRegisteredActionButtons();
            RefreshFeatureToggleRows();
            RefreshSpawnItemRow();
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

            bool compact =
                string.Equals(
                    tab,
                    "Cheats",
                    StringComparison.OrdinalIgnoreCase) ||
                HasFeatureControlsForTab(tab);

            rect.anchoredPosition =
                new Vector2(0f, -40f);

            rect.sizeDelta = compact
                ? new Vector2(350f, 36f)
                : new Vector2(350f, 102f);
        }

        private string GetPlaceholderText(string tab)
        {
            string followText = ProjectLinks.HasNexusUrl
                ? "Follow development on GitHub or visit the GK2+ page on Nexus Mods."
                : "Follow development on GitHub. The Nexus Mods page is coming soon.";

            if (HasFeatureControlsForTab(tab))
            {
                return string.Equals(
                    DetectContext(),
                    "MainMenu",
                    StringComparison.Ordinal)
                    ? $"Configure {tab.ToLowerInvariant()} features before loading a save."
                    : $"Current {tab.ToLowerInvariant()} feature status for this save.";
            }

            switch (tab)
            {
                case "General":
                    return
                        "Coming Soon\n\n" +
                        "GK2+ is still under active development.\n" +
                        "This page will contain global mod settings, UI options, and hotkeys.\n\n" +
                        followText;
                case "Inventory":
                    return
                        "Coming Soon\n\n" +
                        "Planned: unified storage, shared chest resources, search/filtering,\n" +
                        "and stack-size quality-of-life options.\n\n" +
                        followText;
                case "Crafting":
                    return
                        "Coming Soon\n\n" +
                        "Planned: recipe pinning, resource-pull behavior, and crafting QoL.\n\n" +
                        followText;
                case "Farming":
                    return
                        "Coming Soon\n\n" +
                        "Planned: continuous planting, seed-selection behavior, and farming QoL.\n\n" +
                        followText;
                case "Zombies":
                    return
                        "Coming Soon\n\n" +
                        "Planned: a central zombie manager, stats overview, and equipment tools.\n\n" +
                        followText;
                case "Cheats":
                    if (_tabNotices.TryGetValue(
                        "Cheats",
                        out Func<string> cheatsNotice))
                    {
                        string dynamicNotice =
                            cheatsNotice();

                        if (!string.IsNullOrWhiteSpace(dynamicNotice))
                        {
                            return dynamicNotice;
                        }
                    }

                    return
                        "Money cheats use a save-safety checkpoint.\n" +
                        "Health and stamina refills use native player systems.";
                case "More":
                    return
                        "GK2+ Project Links\n\n" +
                        "GitHub: source, development progress, and releases.\n" +
                        (ProjectLinks.HasNexusUrl
                            ? "Nexus Mods: public mod page and downloads.\n"
                            : "Nexus Mods: public mod page coming soon.\n") +
                        "Report Bug: opens a new GitHub issue for GK2+.\n\n" +
                        "Quest/map tools, compatibility, diagnostics, and About will live here.";
                default:
                    return tab;
            }
        }

        public void RefreshActiveTab()
        {
            if (_built &&
                _pageTitle != null &&
                _pageText != null)
            {
                SetActiveTab(_activeTab);
            }
        }

        public void ToggleMenu()
        {
            if (!_built || _menuRoot == null)
            {
                _logger?.LogWarning("GK2+ F2 toggle ignored because the menu shell is not ready.");
                return;
            }

            bool show = !_menuRoot.activeSelf;
            _menuRoot.SetActive(show);

            if (show)
            {
                _menuRoot.transform.SetAsLastSibling();
                SetActiveTab(_activeTab);
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
            SetActiveTab(_activeTab);
        }

        public void HideMenu()
        {
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
            // The main-menu window is the strongest context signal. GK2 can
            // leave MainGame.PlayerData populated after returning to the title
            // screen, so player state alone can misclassify the main menu as
            // gameplay.
            foreach (var behaviour in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
            {
                if (behaviour != null &&
                    behaviour.GetType().Name == "UIMainMenuWindow" &&
                    behaviour.gameObject.activeInHierarchy)
                {
                    return "MainMenu";
                }
            }

            if (MainGame.PlayerData != null)
            {
                return "Gameplay";
            }

            return "MainMenu";
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
            _tabNotices.Clear();
            _pageTitle = null;
            _pageText = null;
            _featureSettingsNote = null;
            _githubButton = null;
            _nexusButton = null;
            _bugButton = null;
            _contentRoot = null;
            _bodyTextTemplate = null;
            _menuButtonLabelTemplate = null;
            _menuButtonSprite = null;
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














