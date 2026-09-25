using System;
using System.Reflection;
using GK2Plus.Core;
using GK2Plus.Framework.Saves;
using GK2Plus.Framework.UI;
using HarmonyLib;
using LazyBearTechnology;

namespace GK2Plus.Features.Cheats
{
    /// <summary>
    /// First functional GK2+ cheat slice. Uses native player systems, routes
    /// persistent mutations through save safety, and permanently taints a save
    /// before the first cheat is executed so platform achievements are blocked.
    /// </summary>
    internal sealed class BasicCheatsFeature : FeatureBase
    {
        private const string MoneyResource = "money";
        private const string StaminaResource = "stamina";

        private static BasicCheatsFeature _activeInstance;
        private static bool _achievementBlockLogged;

        private readonly GK2SaveService _saveService;
        private readonly GK2UIService _uiService;
        private bool _achievementGuardReady;

        public BasicCheatsFeature(
            GK2SaveService saveService,
            GK2UIService uiService)
        {
            _saveService = saveService ??
                throw new ArgumentNullException(nameof(saveService));

            _uiService = uiService ??
                throw new ArgumentNullException(nameof(uiService));
        }

        public override string Id => "basic-cheats";

        public override string Name => "Basic Cheats";

        public override string Category => "Cheats";

        public override string Description =>
            "Optional player/economy cheat actions exposed through the GK2+ menu.";

        protected override bool DefaultEnabled => true;

        protected override void OnEnabled()
        {
            _activeInstance = this;
            _achievementBlockLogged = false;

            _achievementGuardReady =
                TryPatchAchievementPlatformBoundary();

            _uiService.RegisterTabNotice(
                "Cheats",
                BuildCheatNotice);

            RegisterMoneyAction(
                "cheats.give-1-silver",
                "+1 Silver",
                100);

            RegisterMoneyAction(
                "cheats.give-5-silver",
                "+5 Silver",
                500);

            RegisterMoneyAction(
                "cheats.give-10-silver",
                "+10 Silver",
                1000);

            RegisterMoneyAction(
                "cheats.give-100-silver",
                "+100 Silver",
                10000);

            RegisterMoneyAction(
                "cheats.give-1-gold",
                "+1 Gold",
                10000);

            RegisterMoneyAction(
                "cheats.give-5-gold",
                "+5 Gold",
                50000);

            RegisterMoneyAction(
                "cheats.give-10-gold",
                "+10 Gold",
                100000);

            RegisterMoneyAction(
                "cheats.give-100-gold",
                "+100 Gold",
                1000000);

            RegisterCheatAction(
                "cheats.heal-player",
                "Heal Player",
                HealPlayer);

            RegisterCheatAction(
                "cheats.refill-stamina",
                "Refill Stamina",
                RefillStamina);

            Logger.LogInfo(
                "Basic Cheats enabled: Money increments, Heal Player, " +
                "Refill Stamina, and per-save achievement protection.");
        }

        private bool TryPatchAchievementPlatformBoundary()
        {
            try
            {
                MethodInfo progressMethod = AccessTools.Method(
                typeof(AchievementsSystem),
                "TrySetAchievementProgressOnPlatform");

                MethodInfo unlockMethod = AccessTools.Method(
                    typeof(AchievementsSystem),
                    "TryUnlockAchievementOnPlatform");

                if (progressMethod == null ||
                    unlockMethod == null)
                {
                    Logger.LogError(
                        "Basic Cheats could not resolve GK2's platform achievement " +
                        "boundary. Cheat actions will remain unavailable.");

                    return false;
                }

                HarmonyMethod prefix = new HarmonyMethod(
                    typeof(BasicCheatsFeature),
                    nameof(AchievementPlatformPrefix));

                Harmony.Patch(
                    progressMethod,
                    prefix: prefix);

                Harmony.Patch(
                    unlockMethod,
                    prefix: prefix);

                Logger.LogInfo(
                    "GK2+ achievement guard patched GK2's platform progress/unlock boundary.");

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    $"GK2+ achievement guard failed to initialize: {ex}");

                return false;
            }
        }

        private static bool AchievementPlatformPrefix()
        {
            BasicCheatsFeature feature =
                _activeInstance;

            if (feature == null ||
                !feature._saveService.IsActiveSaveCheatTainted)
            {
                return true;
            }

            if (!_achievementBlockLogged)
            {
                _achievementBlockLogged = true;

                feature.Logger.LogWarning(
                    "GK2+ blocked a platform achievement call because the " +
                    "active save is cheat-tainted.");
            }

            return false;
        }

        private void RegisterMoneyAction(
            string id,
            string label,
            int bronzeAmount)
        {
            RegisterCheatAction(
                id,
                label,
                () => GiveMoney(bronzeAmount));
        }

        private void RegisterCheatAction(
            string id,
            string label,
            Action cheatAction)
        {
            _uiService.RegisterMenuAction(
                new GK2MenuAction(
                    id,
                    "Cheats",
                    label,
                    () => RequestCheatExecution(
                        id,
                        label,
                        cheatAction),
                    CanUseCheats));
        }

        private bool CanUseCheats()
        {
            return _achievementGuardReady &&
                   _saveService.HasLoadedSave &&
                   !_saveService.IsSaveOperationInProgress;
        }

        private string BuildCheatNotice()
        {
            if (!_achievementGuardReady)
            {
                return
                    "CHEATS UNAVAILABLE - ACHIEVEMENT PROTECTION FAILED TO INITIALIZE\n" +
                    "GK2+ will not allow cheat actions without the achievement guard.";
            }

            if (!_saveService.HasLoadedSave)
            {
                return
                    "Load a save to use cheats.\n" +
                    "Cheat actions are disabled from the main menu.";
            }

            if (_saveService.IsActiveSaveCheatTainted)
            {
                return
                    "CHEATS USED - ACHIEVEMENTS DISABLED FOR THIS SAVE\n" +
                    "This also applies to this slot's GK2+ safety backups.";
            }

            return
                "Using any cheat permanently disables platform achievements for this save.\n" +
                "The first cheat will ask for confirmation.";
        }

        private void RequestCheatExecution(
            string cheatId,
            string label,
            Action cheatAction)
        {
            if (!CanUseCheats())
            {
                Logger.LogWarning(
                    $"Cheat '{cheatId}' is currently unavailable.");
                return;
            }

            if (_saveService.IsActiveSaveCheatTainted)
            {
                cheatAction();
                return;
            }

            UIDialogWindow dialog =
                LazyUI.GetWindow<UIDialogWindow>();

            if (dialog == null)
            {
                Logger.LogWarning(
                    "GK2+ could not open the native cheat confirmation dialog.");
                return;
            }

            _uiService.HideMenu();

            string information =
                "Using a cheat will permanently disable platform achievements " +
                "for this save and its GK2+ safety backups.\n\n" +
                "This cannot be undone for this save.\n\n" +
                $"Enable cheats and use {label}?";

            UIDialogWindowData data =
                new UIDialogWindowData(
                    "Enable Cheats?",
                    information,
                    Confirm,
                    Cancel)
                {
                    ShowCloseButton = true,
                    CloseButtonAction = Cancel
                };

            try
            {
                dialog.Open(data);
            }
            catch
            {
                _uiService.ShowMenu();
                throw;
            }

            void Cancel()
            {
                dialog.Close();
                _uiService.ShowMenu();
            }

            void Confirm()
            {
                dialog.Close();

                if (!_saveService.TryMarkActiveSaveCheatTainted(
                    cheatId,
                    out string error))
                {
                    Logger.LogError(
                        $"GK2+ did not execute cheat '{cheatId}' because the " +
                        $"save could not be safely tainted: {error}");

                    _uiService.ShowMenu();
                    return;
                }

                _uiService.RefreshMenu();

                try
                {
                    cheatAction();
                }
                finally
                {
                    _uiService.ShowMenu();
                    _uiService.RefreshMenu();
                }
            }
        }

        private void GiveMoney(int amount)
        {
            PlayerData playerData = MainGame.PlayerData;

            if (playerData == null)
            {
                Logger.LogWarning(
                    "Give Money was blocked because PlayerData is unavailable.");
                return;
            }

            int before = playerData.GetResInt(MoneyResource);
            PlayerMoneyGameResSystem moneySystem =
                PlayerMoneyGameResSystem.GetSystem();

            double max = moneySystem != null
                ? moneySystem.Max
                : int.MaxValue;

            int expected = (int)Math.Min(
                max,
                (double)before + amount);

            bool success = _saveService.TryRunProtectedMutation(
                $"Give Money (+{amount})",
                SaveMutationRisk.Moderate,
                () => playerData.AddRes(MoneyResource, amount),
                () => playerData.GetResInt(MoneyResource) == expected);

            if (!success)
            {
                Logger.LogWarning(
                    $"Give Money (+{amount}) did not complete.");
                return;
            }

            int after = playerData.GetResInt(MoneyResource);

            Logger.LogInfo(
                $"Give Money completed: {before} -> {after} bronze units.");
        }

        private void HealPlayer()
        {
            PlayerData playerData = MainGame.PlayerData;
            HPComponent hp = playerData?.hpComponent;

            if (hp == null)
            {
                Logger.LogWarning(
                    "Heal Player was blocked because the player HP component is unavailable.");
                return;
            }

            int before = hp.Hp;

            bool success = _saveService.TryRunProtectedMutation(
                "Heal Player",
                SaveMutationRisk.Low,
                hp.RestoreFullHp,
                () => hp.HasFullHp);

            if (!success)
            {
                Logger.LogWarning(
                    "Heal Player did not complete.");
                return;
            }

            Logger.LogInfo(
                $"Heal Player completed: {before} -> {hp.Hp}/{hp.MaxHpValue} HP.");
        }

        private void RefillStamina()
        {
            PlayerData playerData = MainGame.PlayerData;
            PlayerStaminaGameResSystem staminaSystem =
                PlayerStaminaGameResSystem.GetSystem();

            if (playerData == null ||
                playerData.staminaSystem == null ||
                staminaSystem == null)
            {
                Logger.LogWarning(
                    "Refill Stamina was blocked because the native stamina system is unavailable.");
                return;
            }

            float before =
                playerData.GetRes(StaminaResource);

            bool success = _saveService.TryRunProtectedMutation(
                "Refill Stamina",
                SaveMutationRisk.Low,
                playerData.staminaSystem.SetMax,
                staminaSystem.HasMax);

            if (!success)
            {
                Logger.LogWarning(
                    "Refill Stamina did not complete.");
                return;
            }

            float after =
                playerData.GetRes(StaminaResource);

            Logger.LogInfo(
                $"Refill Stamina completed: {before:0.##} -> {after:0.##}/{staminaSystem.Max:0.##}.");
        }
    }
}
