using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace GK2Plus.Framework.UI
{
    /// <summary>
    /// Shared, live-safe input settings for opening/closing the persistent
    /// GK2+ menu. These are framework settings rather than gameplay features.
    /// </summary>
    internal static class GK2MenuInputSettings
    {
        private const float ControllerChordHoldSeconds = 0.45f;

        private static ConfigFile _config;
        private static GK2UIService _uiService;

        internal static ConfigEntry<KeyCode> MenuHotkey { get; private set; }

        internal static ConfigEntry<bool> ControllerShortcutEnabled { get; private set; }

        internal static float ControllerHoldSeconds =>
            ControllerChordHoldSeconds;

        internal static KeyCode CurrentHotkey =>
            MenuHotkey?.Value ??
            KeyCode.F2;

        internal static bool IsControllerShortcutEnabled =>
            ControllerShortcutEnabled?.Value ??
            true;

        internal static void Bind(
            ConfigFile config)
        {
            _config =
                config ??
                throw new ArgumentNullException(
                    nameof(config));

            MenuHotkey =
                config.Bind(
                    "General",
                    "MenuHotkey",
                    KeyCode.F2,
                    "Keyboard key used to open/close the GK2+ menu. " +
                    "This setting is live-safe and can be changed from the GK2+ General tab.");

            ControllerShortcutEnabled =
                config.Bind(
                    "General",
                    "ControllerMenuShortcut",
                    true,
                    "Enable the controller / Steam Deck GK2+ menu shortcut. " +
                    "Hold L3 + R3 together briefly to open/close the menu; B closes an open GK2+ menu.");
        }

        internal static void RegisterControls(
            GK2UIService uiService)
        {
            if (uiService == null ||
                MenuHotkey == null ||
                ControllerShortcutEnabled == null)
            {
                return;
            }

            _uiService =
                uiService;

            uiService.RegisterFeatureOptionControl(
                new GK2FeatureOptionControl(
                    "menu.hotkey",
                    "General",
                    "Menu Hotkey",
                    () => CurrentHotkey.ToString(),
                    GetHotkeyOptions,
                    SetHotkey,
                    order: -1000,
                    allowInGameEditing: true));

            uiService.RegisterFeatureOptionControl(
                new GK2FeatureOptionControl(
                    "menu.controller-shortcut",
                    "General",
                    "Controller / Steam Deck Shortcut",
                    () =>
                        IsControllerShortcutEnabled
                            ? "On"
                            : "Off",
                    GetOnOffOptions,
                    SetControllerShortcut,
                    order: -990,
                    allowInGameEditing: true));
        }

        internal static string GetToggleHint()
        {
            string keyboard =
                CurrentHotkey.ToString();

            return IsControllerShortcutEnabled
                ? $"{keyboard} / L3+R3"
                : $"{keyboard} Toggle";
        }

        internal static string GetCloseHint()
        {
            return IsControllerShortcutEnabled
                ? "ESC / B Close"
                : "ESC Close";
        }

        private static IReadOnlyList<GK2FeatureOption>
            GetHotkeyOptions()
        {
            return new[]
            {
                new GK2FeatureOption("F1", "F1"),
                new GK2FeatureOption("F2", "F2"),
                new GK2FeatureOption("F3", "F3"),
                new GK2FeatureOption("F4", "F4"),
                new GK2FeatureOption("F5", "F5"),
                new GK2FeatureOption("F6", "F6"),
                new GK2FeatureOption("F7", "F7"),
                new GK2FeatureOption("F8", "F8"),
                new GK2FeatureOption("F9", "F9"),
                new GK2FeatureOption("F10", "F10"),
                new GK2FeatureOption("F11", "F11"),
                new GK2FeatureOption("F12", "F12"),
                new GK2FeatureOption("Home", "Home"),
                new GK2FeatureOption("Insert", "Insert"),
                new GK2FeatureOption("BackQuote", "Back Quote (`)")
            };
        }

        private static IReadOnlyList<GK2FeatureOption>
            GetOnOffOptions()
        {
            return new[]
            {
                new GK2FeatureOption("Off", "Off"),
                new GK2FeatureOption("On", "On")
            };
        }

        private static void SetHotkey(
            string value)
        {
            if (MenuHotkey == null ||
                !Enum.TryParse(
                    value,
                    true,
                    out KeyCode key))
            {
                return;
            }

            MenuHotkey.Value =
                key;

            SaveAndRefresh();
        }

        private static void SetControllerShortcut(
            string value)
        {
            if (ControllerShortcutEnabled == null)
            {
                return;
            }

            ControllerShortcutEnabled.Value =
                string.Equals(
                    value,
                    "On",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    value,
                    "True",
                    StringComparison.OrdinalIgnoreCase);

            SaveAndRefresh();
        }

        private static void SaveAndRefresh()
        {
            _config?.Save();
            _uiService?.RefreshMenu();
        }
    }
}
