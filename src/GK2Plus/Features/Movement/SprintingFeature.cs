using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using GK2Plus.Core;
using GK2Plus.Framework.UI;
using HarmonyLib;
using UnityEngine;

namespace GK2Plus.Features.Movement
{
    internal sealed class SprintingFeature : FeatureBase
    {
        private static readonly IReadOnlyList<GK2FeatureOption> SprintKeyOptions =
            new List<GK2FeatureOption>
            {
                new GK2FeatureOption(KeyCode.LeftShift.ToString(), "Left Shift"),
                new GK2FeatureOption(KeyCode.RightShift.ToString(), "Right Shift"),
                new GK2FeatureOption(KeyCode.LeftControl.ToString(), "Left Ctrl"),
                new GK2FeatureOption(KeyCode.RightControl.ToString(), "Right Ctrl"),
                new GK2FeatureOption(KeyCode.CapsLock.ToString(), "Caps Lock")
            };

        private static readonly IReadOnlyList<GK2FeatureOption> MultiplierOptions =
            new List<GK2FeatureOption>
            {
                new GK2FeatureOption("1.25", "1.25x"),
                new GK2FeatureOption("1.5", "1.5x"),
                new GK2FeatureOption("1.75", "1.75x"),
                new GK2FeatureOption("2", "2x"),
                new GK2FeatureOption("2.5", "2.5x"),
                new GK2FeatureOption("3", "3x")
            };

        private static SprintingFeature _activeInstance;

        private readonly ConfigFile _config;
        private readonly GK2UIService _uiService;

        private ConfigEntry<KeyCode> _sprintKey;
        private ConfigEntry<float> _speedMultiplier;
        private bool _patchReady;

        public SprintingFeature(
            ConfigFile config,
            GK2UIService uiService)
        {
            _config = config ??
                throw new ArgumentNullException(nameof(config));

            _uiService = uiService ??
                throw new ArgumentNullException(nameof(uiService));
        }

        public override string Id => "sprinting";

        public override string Name => "Sprinting";

        public override string Category => "Movement";

        public override string Description =>
            "Hold the configured sprint key during normal free movement to temporarily increase walking speed.";

        protected override bool DefaultEnabled => false;

        protected override void OnInitialize()
        {
            _activeInstance = this;

            _sprintKey = _config.Bind(
                Category,
                $"{Id}.Key",
                KeyCode.LeftShift,
                "Keyboard key held to sprint during normal free movement.");

            _speedMultiplier = _config.Bind(
                Category,
                $"{Id}.SpeedMultiplier",
                1.5f,
                new ConfigDescription(
                    "Temporary movement-speed multiplier while sprinting.",
                    new AcceptableValueRange<float>(1.1f, 3f)));

            var fixedUpdate = AccessTools.Method(
                typeof(global::FreePlayerState),
                nameof(global::FreePlayerState.FixedUpdate));

            if (fixedUpdate == null)
            {
                _patchReady = false;
                Logger.LogError(
                    "Sprinting could not resolve FreePlayerState.FixedUpdate. " +
                    "The feature will remain inactive for this game build.");
            }
            else
            {
                Harmony.Patch(
                    fixedUpdate,
                    prefix: new HarmonyMethod(
                        typeof(SprintingFeature),
                        nameof(FreeMovementPrefix)),
                    postfix: new HarmonyMethod(
                        typeof(SprintingFeature),
                        nameof(FreeMovementPostfix)));

                _patchReady = true;
            }

            _uiService.RegisterFeatureToggleControl(
                new GK2FeatureToggleControl(
                    Id,
                    Category,
                    Name,
                    () => Enabled?.Value ?? DefaultEnabled,
                    BuildUiStatus,
                    SetEnabledFromMainMenu,
                    order: 100));

            _uiService.RegisterFeatureOptionControl(
                new GK2FeatureOptionControl(
                    $"{Id}.key",
                    Category,
                    "Sprint Key",
                    () => _sprintKey.Value.ToString(),
                    () => SprintKeyOptions,
                    SetSprintKey,
                    parentFeatureId: Id,
                    order: 110,
                    enabledProvider: () => _patchReady));

            _uiService.RegisterFeatureOptionControl(
                new GK2FeatureOptionControl(
                    $"{Id}.multiplier",
                    Category,
                    "Speed Multiplier",
                    () => _speedMultiplier.Value.ToString("0.##"),
                    () => MultiplierOptions,
                    SetMultiplier,
                    parentFeatureId: Id,
                    order: 120,
                    enabledProvider: () => _patchReady));
        }

        private string BuildUiStatus()
        {
            if (!_patchReady)
            {
                return "UNAVAILABLE";
            }

            return Enabled?.Value == true
                ? $"ON · {_sprintKey.Value} · {_speedMultiplier.Value:0.##}x"
                : "OFF";
        }

        private void SetEnabledFromMainMenu(bool enabled)
        {
            if (!_patchReady || Enabled == null)
            {
                _uiService.RefreshMenu();
                return;
            }

            Enabled.Value = enabled;
            _config.Save();
            _uiService.RefreshMenu();
        }

        private void SetSprintKey(string value)
        {
            if (!Enum.TryParse(value, true, out KeyCode key))
            {
                return;
            }

            _sprintKey.Value = key;
            _config.Save();
            _uiService.RefreshMenu();
        }

        private void SetMultiplier(string value)
        {
            if (!float.TryParse(
                    value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float multiplier))
            {
                return;
            }

            _speedMultiplier.Value =
                Mathf.Clamp(multiplier, 1.1f, 3f);

            _config.Save();
            _uiService.RefreshMenu();
        }

        private static void FreeMovementPrefix(
            global::PlayerController ___playerController,
            ref float __state)
        {
            __state = float.NaN;

            SprintingFeature feature = _activeInstance;

            if (feature == null ||
                !feature._patchReady ||
                feature.Enabled?.Value != true ||
                feature._sprintKey == null ||
                feature._speedMultiplier == null ||
                ___playerController == null ||
                !___playerController.IsControlsEnabled ||
                global::MainGame.IsGamePaused ||
                !Input.GetKey(feature._sprintKey.Value))
            {
                return;
            }

            global::PlayerPhysicalBody body =
                ___playerController.PhysicalBody;

            if (body == null)
            {
                return;
            }

            __state = body.SpeedMultiplier;
            body.SpeedMultiplier =
                __state * feature._speedMultiplier.Value;
        }

        private static void FreeMovementPostfix(
            global::PlayerController ___playerController,
            float __state)
        {
            if (float.IsNaN(__state) ||
                ___playerController?.PhysicalBody == null)
            {
                return;
            }

            ___playerController.PhysicalBody.SpeedMultiplier =
                __state;
        }
    }
}
