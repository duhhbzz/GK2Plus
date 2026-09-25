using System;
using GK2Plus.Core;
using GK2Plus.Framework.Saves;
using GK2Plus.Framework.UI;

namespace GK2Plus.Features.Cheats
{
    /// <summary>
    /// First functional GK2+ cheat slice. Uses native player systems and routes
    /// persistent mutations through save safety.
    /// </summary>
    internal sealed class BasicCheatsFeature : FeatureBase
    {
        private const string MoneyResource = "money";
        private const string StaminaResource = "stamina";

        private readonly GK2SaveService _saveService;
        private readonly GK2UIService _uiService;

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

            _uiService.RegisterMenuAction(
                new GK2MenuAction(
                    "cheats.heal-player",
                    "Cheats",
                    "Heal Player",
                    HealPlayer,
                    CanUseCheats));

            _uiService.RegisterMenuAction(
                new GK2MenuAction(
                    "cheats.refill-stamina",
                    "Cheats",
                    "Refill Stamina",
                    RefillStamina,
                    CanUseCheats));

            Logger.LogInfo(
                "Basic Cheats enabled: Money increments, Heal Player, and Refill Stamina.");
        }

        private void RegisterMoneyAction(
            string id,
            string label,
            int bronzeAmount)
        {
            _uiService.RegisterMenuAction(
                new GK2MenuAction(
                    id,
                    "Cheats",
                    label,
                    () => GiveMoney(bronzeAmount),
                    CanUseCheats));
        }

        private bool CanUseCheats()
        {
            return _saveService.HasLoadedSave &&
                   !_saveService.IsSaveOperationInProgress;
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
