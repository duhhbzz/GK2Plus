using System;
using System.Reflection;
using BepInEx.Configuration;
using GK2Plus.Core;
using GK2Plus.Framework.UI;
using HarmonyLib;
using UnityEngine;

namespace GK2Plus.Features.Farming
{
    internal sealed class ContinuousPlantingFeature : FeatureBase
    {
        private const float PreserveWindowSeconds = 3f;

        private static ContinuousPlantingFeature _activeInstance;

        private readonly ConfigFile _config;
        private readonly GK2UIService _uiService;

        private bool _patchesReady;
        private string _pendingSeedId;
        private float _lastSuccessfulPlantTime = -1f;

        public ContinuousPlantingFeature(
            ConfigFile config,
            GK2UIService uiService)
        {
            _config = config ??
                throw new ArgumentNullException(nameof(config));

            _uiService = uiService ??
                throw new ArgumentNullException(nameof(uiService));
        }

        public override string Id => "continuous-planting";

        public override string Name => "Continuous Planting";

        public override string Category => "Farming";

        public override string Description =>
            "Keep the selected seed active after a successful planting action while more of that seed remains in the player's inventory.";

        protected override bool DefaultEnabled => true;

        protected override void OnInitialize()
        {
            _activeInstance = this;

            MethodInfo tryApplySeed = AccessTools.Method(
                typeof(global::GardenInteractionHandler),
                nameof(global::GardenInteractionHandler.TryApplySeed));

            MethodInfo plantingOnExit = AccessTools.Method(
                typeof(global::PlantingPlayerState),
                nameof(global::PlantingPlayerState.OnExit));

            if (tryApplySeed == null ||
                plantingOnExit == null)
            {
                _patchesReady = false;

                Logger.LogError(
                    "Continuous Planting could not resolve the current GK2 planting methods. " +
                    "The feature will remain inactive for this game build.");
            }
            else
            {
                Harmony.Patch(
                    tryApplySeed,
                    postfix: new HarmonyMethod(
                        typeof(ContinuousPlantingFeature),
                        nameof(TryApplySeedPostfix)));

                Harmony.Patch(
                    plantingOnExit,
                    prefix: new HarmonyMethod(
                        typeof(ContinuousPlantingFeature),
                        nameof(PlantingStateOnExitPrefix)),
                    postfix: new HarmonyMethod(
                        typeof(ContinuousPlantingFeature),
                        nameof(PlantingStateOnExitPostfix)));

                _patchesReady = true;
            }

            Enabled.SettingChanged += (_, __) =>
            {
                ClearPendingSeed();
                _uiService.RefreshMenu();

                Logger.LogInfo(
                    $"Continuous Planting {(Enabled.Value ? "enabled" : "disabled")}.");
            };

            _uiService.RegisterFeatureToggleControl(
                new GK2FeatureToggleControl(
                    Id,
                    Category,
                    Name,
                    () => Enabled?.Value ?? DefaultEnabled,
                    BuildUiStatus,
                    SetEnabledFromMainMenu,
                    order: 100));
        }

        protected override void OnEnabled()
        {
            if (_patchesReady)
            {
                Logger.LogInfo("Continuous Planting enabled.");
            }
        }

        private string BuildUiStatus()
        {
            if (!_patchesReady)
            {
                return "UNAVAILABLE";
            }

            return Enabled?.Value == true
                ? "ON"
                : "OFF";
        }

        private void SetEnabledFromMainMenu(bool enabled)
        {
            if (!_patchesReady ||
                Enabled == null)
            {
                _uiService.RefreshMenu();
                return;
            }

            if (Enabled.Value == enabled)
            {
                _uiService.RefreshMenu();
                return;
            }

            Enabled.Value = enabled;
            _config.Save();
            _uiService.RefreshMenu();
        }

        private static void TryApplySeedPostfix(
            global::Item __0,
            bool __result)
        {
            ContinuousPlantingFeature feature =
                _activeInstance;

            if (feature == null ||
                !feature._patchesReady ||
                feature.Enabled?.Value != true ||
                !__result ||
                __0 == null ||
                __0.IsEmpty ||
                !__0.IsSeed)
            {
                return;
            }

            feature._pendingSeedId = __0.id;
            feature._lastSuccessfulPlantTime =
                Time.unscaledTime;

            feature.Logger.LogDebug(
                $"Continuous Planting armed for seed '{__0.id}'.");
        }

        private static void PlantingStateOnExitPrefix(
            ref string __state)
        {
            __state = null;

            ContinuousPlantingFeature feature =
                _activeInstance;

            if (feature == null ||
                !feature.ShouldPreserveCurrentSeed(
                    out string seedId))
            {
                return;
            }

            __state = seedId;
        }

        private static void PlantingStateOnExitPostfix(
            string __state)
        {
            ContinuousPlantingFeature feature =
                _activeInstance;

            if (feature == null)
            {
                return;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(__state))
                {
                    feature.RestoreSeedIfAvailable(
                        __state);
                }
            }
            finally
            {
                feature.ClearPendingSeed();
            }
        }

        private bool ShouldPreserveCurrentSeed(
            out string seedId)
        {
            seedId = null;

            if (!_patchesReady ||
                Enabled?.Value != true ||
                string.IsNullOrWhiteSpace(
                    _pendingSeedId))
            {
                ClearPendingSeed();
                return false;
            }

            if (_lastSuccessfulPlantTime < 0f ||
                Time.unscaledTime -
                    _lastSuccessfulPlantTime >
                    PreserveWindowSeconds)
            {
                ClearPendingSeed();
                return false;
            }

            global::PlayerData playerData =
                global::MainGame.PlayerData;

            global::Item current =
                playerData?.interactingItem;

            if (playerData == null ||
                current == null ||
                current.IsEmpty ||
                !current.IsSeed ||
                !string.Equals(
                    current.id,
                    _pendingSeedId,
                    StringComparison.Ordinal))
            {
                ClearPendingSeed();
                return false;
            }

            if (playerData.inventory == null ||
                playerData.inventory.Data
                    .GetTotalCountInInventory(
                        _pendingSeedId) <= 0)
            {
                ClearPendingSeed();
                return false;
            }

            seedId = _pendingSeedId;
            return true;
        }

        private void RestoreSeedIfAvailable(
            string seedId)
        {
            global::PlayerData playerData =
                global::MainGame.PlayerData;

            if (playerData?.inventory == null ||
                playerData.inventory.Data
                    .GetTotalCountInInventory(seedId) <= 0)
            {
                return;
            }

            global::Item seed =
                playerData.inventory.GetItemById(
                    seedId);

            if (seed == null ||
                seed.IsEmpty ||
                !seed.IsSeed)
            {
                return;
            }

            if (playerData.interactingItem != null &&
                !playerData.interactingItem.IsEmpty &&
                string.Equals(
                    playerData.interactingItem.id,
                    seedId,
                    StringComparison.Ordinal))
            {
                return;
            }

            playerData.SetInteractingItem(seed);

            Logger.LogDebug(
                $"Continuous Planting restored seed '{seedId}'.");
        }

        private void ClearPendingSeed()
        {
            _pendingSeedId = null;
            _lastSuccessfulPlantTime = -1f;
        }
    }
}
