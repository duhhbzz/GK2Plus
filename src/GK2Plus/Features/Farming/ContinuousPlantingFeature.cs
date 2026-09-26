using System;
using System.Reflection;
using GK2Plus.Core;
using GK2Plus.Framework.UI;
using HarmonyLib;
using UnityEngine;

namespace GK2Plus.Features.Farming
{
    /// <summary>
    /// Keeps the currently selected seed active across the short player-state
    /// transition that can occur after a successful garden planting action.
    ///
    /// The patch is intentionally narrow:
    /// - it only arms after GardenInteractionHandler.TryApplySeed succeeds;
    /// - it only restores the same seed id;
    /// - it only restores when more of that seed remains in player inventory;
    /// - explicit player cancellation still wins because the vanilla planting
    ///   state removes the interacting item before OnExit.
    /// </summary>
    internal sealed class ContinuousPlantingFeature : FeatureBase
    {
        private const float PreserveWindowSeconds = 3f;

        private static ContinuousPlantingFeature _activeInstance;

        private readonly GK2UIService _uiService;

        private bool _patchesReady;
        private string _pendingSeedId;
        private float _lastSuccessfulPlantTime = -1f;

        public ContinuousPlantingFeature(
            GK2UIService uiService)
        {
            _uiService = uiService ??
                throw new ArgumentNullException(nameof(uiService));
        }

        public override string Id => "continuous-planting";

        public override string Name => "Continuous Planting";

        public override string Category => "Farming";

        public override string Description =>
            "Keeps the selected seed active after planting so the next compatible plot can be planted without reselecting it.";

        protected override bool DefaultEnabled => true;

        protected override void OnInitialize()
        {
            _activeInstance = this;

            MethodInfo tryApplySeed = AccessTools.Method(
                typeof(GardenInteractionHandler),
                nameof(GardenInteractionHandler.TryApplySeed));

            MethodInfo plantingOnExit = AccessTools.Method(
                typeof(PlantingPlayerState),
                nameof(PlantingPlayerState.OnExit));

            if (tryApplySeed == null ||
                plantingOnExit == null)
            {
                _patchesReady = false;

                Logger.LogError(
                    "Continuous Planting could not resolve its GK2 planting targets. " +
                    "The feature will remain unavailable.");

                RegisterMenu();
                return;
            }

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

            Enabled.SettingChanged += (_, __) =>
            {
                ClearPendingSeed();
                _uiService.RefreshMenu();

                Logger.LogInfo(
                    $"Continuous Planting {(Enabled.Value ? "enabled" : "disabled")}.");
            };

            RegisterMenu();

            Logger.LogInfo(
                "Continuous Planting hooks ready. " +
                "Successful seed planting can preserve the selected seed across planting-state exit.");
        }

        protected override void OnEnabled()
        {
            Logger.LogInfo(
                "Continuous Planting enabled.");
        }

        private void RegisterMenu()
        {
            _uiService.RegisterTabNotice(
                "Farming",
                BuildFarmingNotice);

            _uiService.RegisterMenuAction(
                new GK2MenuAction(
                    "farming.continuous-planting.toggle",
                    "Farming",
                    "Toggle Continuous",
                    ToggleEnabled,
                    () => _patchesReady));
        }

        private string BuildFarmingNotice()
        {
            if (!_patchesReady)
            {
                return
                    "CONTINUOUS PLANTING UNAVAILABLE\n" +
                    "GK2+ could not resolve the required planting methods for this game version.";
            }

            return
                $"Continuous Planting: {(Enabled.Value ? "Enabled" : "Disabled")}\n\n" +
                "After a successful plant, GK2+ keeps the same seed selected " +
                "when more of that seed remains in your inventory.";
        }

        private void ToggleEnabled()
        {
            if (!_patchesReady)
            {
                return;
            }

            Enabled.Value = !Enabled.Value;
        }

        private static void TryApplySeedPostfix(
            Item __0,
            bool __result)
        {
            ContinuousPlantingFeature feature =
                _activeInstance;

            if (feature == null ||
                !feature._patchesReady ||
                feature.Enabled == null ||
                !feature.Enabled.Value ||
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

            if (string.IsNullOrWhiteSpace(__state))
            {
                feature.ClearPendingSeed();
                return;
            }

            try
            {
                feature.RestoreSeedIfAvailable(
                    __state);
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
                Enabled == null ||
                !Enabled.Value ||
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

            PlayerData playerData =
                MainGame.PlayerData;

            Item current =
                playerData?.interactingItem;

            // Vanilla explicit cancellation (Action/Menu/Tab) removes the
            // interacting item before PlantingPlayerState.OnExit. In that case
            // current is null and we deliberately do not restore anything.
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
            PlayerData playerData =
                MainGame.PlayerData;

            if (playerData == null ||
                playerData.inventory == null)
            {
                return;
            }

            if (playerData.inventory.Data
                .GetTotalCountInInventory(seedId) <= 0)
            {
                return;
            }

            Item seed =
                playerData.inventory.GetItemById(
                    seedId);

            if (seed == null ||
                seed.IsEmpty ||
                !seed.IsSeed)
            {
                return;
            }

            // If another system already restored the same seed, leave it alone.
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
                $"Continuous Planting restored seed '{seedId}' after planting-state exit.");
        }

        private void ClearPendingSeed()
        {
            _pendingSeedId = null;
            _lastSuccessfulPlantTime = -1f;
        }
    }
}
