using System;
using System.Globalization;
using System.Reflection;
using GK2Plus.Core;
using GK2Plus.Framework.Saves;
using HarmonyLib;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2Plus.Features.General
{
    /// <summary>
    /// Adds a native-style Save Game button to GK2's pause menu and delegates
    /// persistence to the game's own SaveSystem through GK2SaveService.
    /// Also augments the native Exit to Main Menu confirmation with the actual
    /// native last-save age.
    /// </summary>
    internal sealed class ManualSaveFeature : FeatureBase
    {
        private const string PauseWindowTypeName = "UIGamePauseWindow";
        private const string InjectedButtonName = "GK2PlusSaveGame";

        private static ManualSaveFeature _activeInstance;

        private readonly GK2SaveService _saveService;

        public ManualSaveFeature(GK2SaveService saveService)
        {
            _saveService = saveService ??
                throw new ArgumentNullException(nameof(saveService));
        }

        public override string Id => "manual-save";

        public override string Name => "Manual Save";

        public override string Category => "General";

        public override string Description =>
            "Adds a Save Game button to the in-game pause menu.";

        protected override bool DefaultEnabled => true;

        protected override void OnEnabled()
        {
            _activeInstance = this;

            Type pauseWindowType = AccessTools.TypeByName(PauseWindowTypeName);
            if (pauseWindowType == null)
            {
                Logger.LogWarning(
                    "Manual Save could not find UIGamePauseWindow; " +
                    "the pause-menu integration will not be added.");
                return;
            }

            MethodInfo initMethod = AccessTools.Method(
                pauseWindowType,
                "Init");

            MethodInfo goToMenuMethod = AccessTools.Method(
                pauseWindowType,
                "OnPressedGoToMainMenu");

            if (initMethod == null || goToMenuMethod == null)
            {
                Logger.LogWarning(
                    "Manual Save could not resolve the required pause-menu methods.");
                return;
            }

            Harmony.Patch(
                initMethod,
                postfix: new HarmonyMethod(
                    typeof(ManualSaveFeature),
                    nameof(PauseWindowInitPostfix)));

            Harmony.Patch(
                goToMenuMethod,
                prefix: new HarmonyMethod(
                    typeof(ManualSaveFeature),
                    nameof(PauseWindowGoToMenuPrefix)));

            // Usually the patch lands before LazyUI initializes this window.
            // This one-time fallback also handles an already-created pause
            // window without introducing per-frame polling.
            foreach (MonoBehaviour behaviour in
                Resources.FindObjectsOfTypeAll<MonoBehaviour>())
            {
                if (behaviour != null &&
                    behaviour.GetType() == pauseWindowType)
                {
                    TryInjectSaveButton(behaviour);
                    break;
                }
            }

            Logger.LogInfo(
                "Manual Save enabled; using GK2's native SaveSystem.");
        }

        private static void PauseWindowInitPostfix(object __instance)
        {
            _activeInstance?.TryInjectSaveButton(
                __instance as MonoBehaviour);
        }

        private static bool PauseWindowGoToMenuPrefix(
            object __instance)
        {
            if (_activeInstance == null)
            {
                return true;
            }

            UIGamePauseWindow pauseWindow =
                __instance as UIGamePauseWindow;

            if (pauseWindow == null)
            {
                return true;
            }

            _activeInstance.OpenExitConfirmation(
                pauseWindow);

            // Suppress the original confirmation only after ours opens.
            return false;
        }

        private void TryInjectSaveButton(MonoBehaviour pauseWindow)
        {
            if (pauseWindow == null)
            {
                return;
            }

            Transform existing = pauseWindow.transform.Find(
                "GenericWIndowLayout/Content/" + InjectedButtonName);

            if (existing != null)
            {
                return;
            }

            FieldInfo settingsField = AccessTools.Field(
                pauseWindow.GetType(),
                "settingsBtn");

            LazyButton settingsButton =
                settingsField?.GetValue(pauseWindow) as LazyButton;

            if (settingsButton == null)
            {
                Logger.LogWarning(
                    "Manual Save could not resolve the pause-menu Settings button.");
                return;
            }

            Transform parent = settingsButton.transform.parent;
            if (parent == null)
            {
                Logger.LogWarning(
                    "Manual Save could not resolve the pause-menu button container.");
                return;
            }

            // Clone the native Settings button while inactive so its runtime
            // LazyUI element ID cannot register a duplicate during Awake.
            bool settingsWasActive =
                settingsButton.gameObject.activeSelf;

            settingsButton.gameObject.SetActive(false);

            GameObject saveButtonObject = null;

            try
            {
                saveButtonObject = UnityEngine.Object.Instantiate(
                    settingsButton.gameObject,
                    parent,
                    false);

                saveButtonObject.name = InjectedButtonName;

                LazyButton saveButton =
                    saveButtonObject.GetComponent<LazyButton>();

                if (saveButton == null)
                {
                    throw new InvalidOperationException(
                        "Cloned pause-menu button has no LazyButton component.");
                }

                saveButton.LazyUIElementId = string.Empty;

                // The clone may contain multiple TMP layers used by the native
                // button visuals. Stop every cloned localization component from
                // owning those labels, then update all text layers consistently.
                foreach (LocalizedLabel localized in
                    saveButtonObject.GetComponentsInChildren<LocalizedLabel>(true))
                {
                    localized.IgnoreLocalize = true;
                    localized.enabled = false;
                }

                foreach (TextMeshProUGUI label in
                    saveButtonObject.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    label.text = "Save Game";
                }

                // Do not inherit Settings' runtime click action.
                saveButton.onClick =
                    new Button.ButtonClickedEvent();

                saveButton.onClick.AddListener(
                    OnSaveButtonClicked);

                // Place Save Game immediately after Settings. The native
                // VerticalLayoutGroup owns positioning and spacing.
                saveButtonObject.transform.SetSiblingIndex(
                    settingsButton.transform.GetSiblingIndex() + 1);

                settingsButton.gameObject.SetActive(
                    settingsWasActive);

                saveButtonObject.SetActive(
                    settingsWasActive);

                saveButton.SetCallbacksIntoGamepadNavigationItem();

                Logger.LogInfo(
                    "Manual Save injected native Save Game button into " +
                    "UIGamePauseWindow.");
            }
            catch (Exception ex)
            {
                settingsButton.gameObject.SetActive(
                    settingsWasActive);

                if (saveButtonObject != null)
                {
                    UnityEngine.Object.Destroy(
                        saveButtonObject);
                }

                Logger.LogError(
                    $"Manual Save failed to inject pause-menu button: {ex}");
            }
        }

        private void OnSaveButtonClicked()
        {
            if (_saveService.TryManualSave(
                out string error))
            {
                Logger.LogInfo(
                    "Manual Save completed successfully.");
            }
            else
            {
                Logger.LogWarning(
                    "Manual Save was not completed: " +
                    (error ?? "unknown error"));
            }
        }

        private void OpenExitConfirmation(
            UIGamePauseWindow pauseWindow)
        {
            UIDialogWindow dialog =
                LazyUI.GetWindow<UIDialogWindow>();

            if (dialog == null)
            {
                Logger.LogWarning(
                    "Manual Save could not open the native exit confirmation dialog.");
                return;
            }

            string information =
                "Unsaved progress since your last save will be lost.\n\n" +
                BuildLastSavedStatus() +
                "\n\nExit to main menu?";

            UIDialogWindowData data =
                new UIDialogWindowData(
                    LLBase.L("exit_menu_confirm"),
                    information,
                    Yes,
                    () => dialog.Close())
                {
                    ShowCloseButton = true
                };

            dialog.Open(data);

            void Yes()
            {
                dialog.Close();
                pauseWindow.Close();
                MainGame.Instance.GoToMenu();
            }
        }

        private string BuildLastSavedStatus()
        {
            if (!_saveService.TryGetLastSaveDateTime(
                out DateTime savedAt))
            {
                return "Last saved: unknown.";
            }

            TimeSpan elapsed =
                DateTime.Now - savedAt;

            if (elapsed < TimeSpan.Zero)
            {
                elapsed = TimeSpan.Zero;
            }

            string relative;

            if (elapsed.TotalMinutes < 1d)
            {
                relative = "just now";
            }
            else if (elapsed.TotalMinutes < 2d)
            {
                relative = "1 minute ago";
            }
            else if (elapsed.TotalHours < 1d)
            {
                relative =
                    $"{(int)elapsed.TotalMinutes} minutes ago";
            }
            else if (elapsed.TotalHours < 2d)
            {
                relative = "1 hour ago";
            }
            else if (elapsed.TotalDays < 1d)
            {
                relative =
                    $"{(int)elapsed.TotalHours} hours ago";
            }
            else if (elapsed.TotalDays < 2d)
            {
                relative = "1 day ago";
            }
            else
            {
                relative =
                    $"{(int)elapsed.TotalDays} days ago";
            }

            string exact = elapsed.TotalDays >= 1d
                ? savedAt.ToString(
                    "g",
                    CultureInfo.CurrentCulture)
                : savedAt.ToString(
                    "t",
                    CultureInfo.CurrentCulture);

            return
                $"Last saved: {relative} ({exact}).";
        }
    }
}
