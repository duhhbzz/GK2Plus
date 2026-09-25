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
            _uiService.RegisterMenuAction(
                new GK2MenuAction(
                    "cheats.give-silver",
                    "Cheats",
                    "Give 1 Silver",
                    () => GiveMoney(100),
                    CanUseCheats));

            _uiService.RegisterMenuAction(
                new GK2MenuAction(
                    "cheats.give-gold",
                    "Cheats",
                    "Give 1 Gold",
                    () => GiveMoney(10000),
                    CanUseCheats));

            _uiService.RegisterMenuAction(
                new GK2MenuAction(
                    "cheats.heal-player",
                    "Cheats",
                    "Heal Player",
                    HealPlayer,
                    CanUseCheats));

            Logger.LogInfo(
                "Basic Cheats enabled: Give Money and Heal Player.");
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
    }
}
