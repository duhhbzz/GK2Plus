using System;
using System.Reflection;
using GK2Plus.Core;
using GK2Plus.Framework.Saves;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace GK2Plus.Features.General
{
    /// <summary>
    /// Adds a native-style Save Game button to GK2's pause menu and delegates
    /// persistence to the game's own SaveSystem through GK2SaveService.
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
                    "the pause-menu button will not be added.");
                return;
            }

            MethodInfo initMethod = AccessTools.Method(pauseWindowType, "Init");
            if (initMethod == null)
            {
                Logger.LogWarning(
                    "Manual Save could not find UIGamePauseWindow.Init; " +
                    "the pause-menu button will not be added.");
                return;
            }

            Harmony.Patch(
                initMethod,
                postfix: new HarmonyMethod(
                    typeof(ManualSaveFeature),
                    nameof(PauseWindowInitPostfix)));

            // Usually the patch lands before LazyUI initializes this window.
            // This one-time fallback also handles a window that was initialized
            // earlier than expected without introducing per-frame polling.
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
            if (_activeInstance == null)
            {
                return;
            }

            _activeInstance.TryInjectSaveButton(
                __instance as MonoBehaviour);
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

            Component settingsButton =
                settingsField?.GetValue(pauseWindow) as Component;

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

            // Clone the real Settings button while it is temporarily inactive.
            // That gives us GK2's native visuals/layout without allowing the
            // cloned LazyButton to register the Settings button's UI element ID
            // during Awake.
            bool settingsWasActive = settingsButton.gameObject.activeSelf;
            settingsButton.gameObject.SetActive(false);

            GameObject saveButtonObject = null;

            try
            {
                saveButtonObject = UnityEngine.Object.Instantiate(
                    settingsButton.gameObject,
                    parent,
                    false);

                saveButtonObject.name = InjectedButtonName;

                Component lazyButton = FindComponentByTypeName(
                    saveButtonObject,
                    "LazyButton");

                ClearLazyUiElementId(lazyButton);

                Button button = saveButtonObject.GetComponent<Button>();
                if (button == null)
                {
                    throw new InvalidOperationException(
                        "Cloned pause-menu button has no Unity Button component.");
                }

                // Do not inherit Settings' runtime/persistent click action.
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(OnSaveButtonClicked);

                SetNativeButtonLabel(saveButtonObject, "Save Game");

                // Place Save Game immediately after Settings. The native
                // VerticalLayoutGroup owns sizing/positioning from here.
                saveButtonObject.transform.SetSiblingIndex(
                    settingsButton.transform.GetSiblingIndex() + 1);

                settingsButton.gameObject.SetActive(settingsWasActive);
                saveButtonObject.SetActive(settingsWasActive);

                RebindGamepadCallbacks(lazyButton);

                Logger.LogInfo(
                    "Manual Save injected native Save Game button into " +
                    "UIGamePauseWindow.");
            }
            catch (Exception ex)
            {
                settingsButton.gameObject.SetActive(settingsWasActive);

                if (saveButtonObject != null)
                {
                    UnityEngine.Object.Destroy(saveButtonObject);
                }

                Logger.LogError(
                    $"Manual Save failed to inject pause-menu button: {ex}");
            }
        }

        private void OnSaveButtonClicked()
        {
            if (_saveService.TryManualSave(out string error))
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

        private static Component FindComponentByTypeName(
            GameObject obj,
            string typeName)
        {
            if (obj == null)
            {
                return null;
            }

            foreach (Component component in obj.GetComponents<Component>())
            {
                if (component != null &&
                    component.GetType().Name == typeName)
                {
                    return component;
                }
            }

            return null;
        }

        private static void ClearLazyUiElementId(Component lazyButton)
        {
            if (lazyButton == null)
            {
                return;
            }

            PropertyInfo property = lazyButton.GetType().GetProperty(
                "LazyUIElementId",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            if (property != null && property.CanWrite)
            {
                property.SetValue(lazyButton, string.Empty, null);
            }
        }

        private static void RebindGamepadCallbacks(Component lazyButton)
        {
            if (lazyButton == null)
            {
                return;
            }

            MethodInfo method = lazyButton.GetType().GetMethod(
                "SetCallbacksIntoGamepadNavigationItem",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            method?.Invoke(lazyButton, null);
        }

        private static void SetNativeButtonLabel(
            GameObject buttonObject,
            string text)
        {
            Transform label = buttonObject.transform.Find(
                "Content/Back/Label");

            if (label == null)
            {
                throw new InvalidOperationException(
                    "Cloned pause-menu button label was not found.");
            }

            foreach (Component component in label.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                string typeName = component.GetType().Name;
                if (typeName == "LocalizedLabel" ||
                    typeName == "LocalizedVerticalOffset")
                {
                    UnityEngine.Object.Destroy(component);
                }
            }

            Component tmp = FindComponentByTypeName(
                label.gameObject,
                "TextMeshProUGUI");

            if (tmp == null)
            {
                throw new InvalidOperationException(
                    "Cloned pause-menu button has no TextMeshProUGUI label.");
            }

            PropertyInfo textProperty = tmp.GetType().GetProperty(
                "text",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            if (textProperty == null || !textProperty.CanWrite)
            {
                throw new InvalidOperationException(
                    "Unable to set cloned pause-menu button label.");
            }

            textProperty.SetValue(tmp, text, null);
        }
    }
}
