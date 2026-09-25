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

        private GameObject _githubButton;
        private GameObject _nexusButton;
        private GameObject _bugButton;
        private GameObject _contentRoot;
        private GameObject _menuButtonLabelTemplate;
        private Sprite _menuButtonSprite;

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
                const float firstRowY = -92f;
                const float rowGap = 27f;

                for (int i = 0; i < count; i++)
                {
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

            bool cheats = string.Equals(
                tab,
                "Cheats",
                StringComparison.OrdinalIgnoreCase);

            rect.anchoredPosition = cheats
                ? new Vector2(0f, -40f)
                : new Vector2(0f, -40f);

            rect.sizeDelta = cheats
                ? new Vector2(350f, 42f)
                : new Vector2(350f, 102f);
        }

        private string GetPlaceholderText(string tab)
        {
            string followText = ProjectLinks.HasNexusUrl
                ? "Follow development on GitHub or visit the GK2+ page on Nexus Mods."
                : "Follow development on GitHub. The Nexus Mods page is coming soon.";

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

        public void HideMenu()
        {
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
            _tabNotices.Clear();
            _pageTitle = null;
            _pageText = null;
            _githubButton = null;
            _nexusButton = null;
            _bugButton = null;
            _contentRoot = null;
            _menuButtonLabelTemplate = null;
            _menuButtonSprite = null;
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














