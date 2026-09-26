using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
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
        private const string EnergyResource = "energy";

        private static BasicCheatsFeature _activeInstance;
        private static bool _achievementBlockLogged;

        private readonly GK2SaveService _saveService;
        private readonly GK2UIService _uiService;
        private readonly ConfigFile _config;

        private ConfigEntry<string> _spawnItemId;
        private ConfigEntry<int> _spawnItemCount;
        private bool _achievementGuardReady;

        public BasicCheatsFeature(
            GK2SaveService saveService,
            GK2UIService uiService,
            ConfigFile config)
        {
            _saveService = saveService ??
                throw new ArgumentNullException(nameof(saveService));

            _uiService = uiService ??
                throw new ArgumentNullException(nameof(uiService));

            _config = config ??
                throw new ArgumentNullException(nameof(config));
        }

        public override string Id => "basic-cheats";

        public override string Name => "Basic Cheats";

        public override string Category => "Cheats";

        public override string Description =>
            "Optional player/economy cheat actions exposed through the GK2+ menu.";

        protected override bool DefaultEnabled => true;

        protected override void OnInitialize()
        {
            _spawnItemId = _config.Bind(
                Category,
                $"{Id}.SpawnItemId",
                "stick",
                "Native GK2 item id used by the Spawn Item cheat."
            );

            _spawnItemCount = _config.Bind(
                Category,
                $"{Id}.SpawnItemCount",
                100,
                new ConfigDescription(
                    "Quantity used by the Spawn Item cheat.",
                    new AcceptableValueRange<int>(1, 10000)
                )
            );
        }

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
                "cheats.refill-energy",
                "Refill Energy",
                RefillEnergy);

            _uiService.RegisterSpawnItemControl(
                new GK2SpawnItemControl(
                    "Cheats",
                    GetSpawnItemOptions,
                    () => _spawnItemId?.Value ?? string.Empty,
                    () => _spawnItemCount?.Value ?? 1,
                    SelectSpawnItem,
                    SetSpawnQuantity,
                    () => RequestCheatExecution(
                        "cheats.spawn-item",
                        "Spawn Item",
                        SpawnConfiguredItem),
                    CanUseCheats));

            Logger.LogInfo(
                "Basic Cheats enabled: Money increments, Heal Player, " +
                "Refill Energy, Spawn Item, and per-save achievement protection.");
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

        private IReadOnlyList<GK2ItemOption> GetSpawnItemOptions()
        {
            List<GK2ItemOption> options =
                new List<GK2ItemOption>();

            GameBalance balance =
                GameBalance.Me;

            if (balance == null)
            {
                return options;
            }

            IEnumerable itemDefs =
                ResolveItemDefs(balance);

            if (itemDefs == null)
            {
                return options;
            }

            foreach (object value in itemDefs)
            {
                if (!(value is ItemDef itemDef) ||
                    string.IsNullOrWhiteSpace(itemDef.id))
                {
                    continue;
                }

                string displayName = itemDef.id;

                try
                {
                    string header =
                        itemDef.GetHeader();

                    if (!string.IsNullOrWhiteSpace(header))
                    {
                        displayName = header;
                    }
                }
                catch
                {
                    // Fall back to the technical item id if localization/header
                    // data is unavailable for an unusual definition.
                }

                options.Add(
                    new GK2ItemOption(
                        itemDef.id,
                        displayName));
            }

            options.Sort((left, right) =>
            {
                int byName =
                    string.Compare(
                        left.DisplayName,
                        right.DisplayName,
                        StringComparison.OrdinalIgnoreCase);

                return byName != 0
                    ? byName
                    : string.Compare(
                        left.Id,
                        right.Id,
                        StringComparison.OrdinalIgnoreCase);
            });

            return options;
        }

        private void SelectSpawnItem(
            string itemId)
        {
            if (_spawnItemId == null ||
                string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            _spawnItemId.Value =
                itemId.Trim();

            _config.Save();

            Logger.LogInfo(
                $"Spawn Item selection changed to '{_spawnItemId.Value}'.");
        }

        private void SetSpawnQuantity(
            int quantity)
        {
            if (_spawnItemCount == null)
            {
                return;
            }

            int clamped =
                Math.Max(
                    1,
                    Math.Min(
                        10000,
                        quantity));

            if (_spawnItemCount.Value == clamped)
            {
                return;
            }

            _spawnItemCount.Value =
                clamped;

            _config.Save();
        }

        private static IEnumerable ResolveItemDefs(
            GameBalance balance)
        {
            if (balance == null)
            {
                return null;
            }

            Type type =
                balance.GetType();

            FieldInfo field =
                AccessTools.Field(
                    type,
                    "itemDefs");

            if (field?.GetValue(balance) is IEnumerable fieldValues)
            {
                return fieldValues;
            }

            PropertyInfo property =
                AccessTools.Property(
                    type,
                    "itemDefs");

            if (property?.GetValue(balance, null) is IEnumerable propertyValues)
            {
                return propertyValues;
            }

            return null;
        }

        private void SpawnConfiguredItem()
        {
            string itemId =
                (_spawnItemId?.Value ?? string.Empty).Trim();

            int count =
                _spawnItemCount?.Value ?? 0;

            if (string.IsNullOrWhiteSpace(itemId))
            {
                Logger.LogWarning(
                    "Spawn Item was blocked because SpawnItemId is empty.");
                return;
            }

            if (count <= 0)
            {
                Logger.LogWarning(
                    "Spawn Item was blocked because SpawnItemCount must be greater than zero.");
                return;
            }

            PlayerData playerData = MainGame.PlayerData;
            GameBalance balance = GameBalance.Me;

            if (playerData == null ||
                balance == null)
            {
                Logger.LogWarning(
                    "Spawn Item was blocked because PlayerData or game balance is unavailable.");
                return;
            }

            ItemDef itemDef = balance.GetData<ItemDef>(itemId);

            if (itemDef == null)
            {
                Logger.LogWarning(
                    $"Spawn Item was blocked because '{itemId}' is not a valid ItemDef id.");
                return;
            }

            global::Inventory playerInventory =
                playerData.Inventory;

            if (playerInventory == null)
            {
                Logger.LogWarning(
                    "Spawn Item was blocked because PlayerData.Inventory is unavailable.");
                return;
            }

            List<Item> items =
                new ItemCount(itemId, count).CreateItems();

            if (items == null ||
                items.Count == 0)
            {
                Logger.LogWarning(
                    $"Spawn Item was blocked because GK2 could not materialize " +
                    $"{count}x '{itemId}' through ItemCount.CreateItems().");
                return;
            }

            int materializedCount = 0;

            foreach (Item createdItem in items)
            {
                if (createdItem != null &&
                    string.Equals(
                        createdItem.id,
                        itemId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    materializedCount += createdItem.Count;
                }
            }

            if (materializedCount != count)
            {
                Logger.LogWarning(
                    $"Spawn Item was blocked because GK2 materialized " +
                    $"{materializedCount}x '{itemId}' instead of {count}.");
                return;
            }

            int beforeTotal =
                playerInventory.Data.GetTotalCountInInventory(itemId);

            int beforeVisibleStack =
                playerInventory.GetItemById(itemId)?.Count ?? 0;

            bool sameInventoryReference =
                ReferenceEquals(
                    playerInventory,
                    playerData.inventory);

            int afterTotal = beforeTotal;
            int afterVisibleStack = beforeVisibleStack;
            bool nativeAddResult = false;

            Logger.LogInfo(
                $"Spawn Item diagnostic before add: item='{itemId}', " +
                $"requested={count}, materialized={materializedCount}, " +
                $"objects={items.Count}, total={beforeTotal}, " +
                $"visibleStack={beforeVisibleStack}, " +
                $"PlayerData.Inventory==playerData.inventory={sameInventoryReference}.");

            bool success =
                _saveService.TryRunProtectedMutation(
                    $"Spawn Item ({itemId} x{count})",
                    SaveMutationRisk.Moderate,
                    () =>
                    {
                        nativeAddResult =
                            playerInventory.AddItemsToInventory(items);

                        afterTotal =
                            playerInventory.Data.GetTotalCountInInventory(itemId);

                        afterVisibleStack =
                            playerInventory.GetItemById(itemId)?.Count ?? 0;

                        Logger.LogInfo(
                            $"Spawn Item diagnostic after add: item='{itemId}', " +
                            $"AddItemsToInventory={nativeAddResult}, " +
                            $"total={beforeTotal}->{afterTotal}, " +
                            $"visibleStack={beforeVisibleStack}->{afterVisibleStack}.");

                        if (!nativeAddResult)
                        {
                            throw new InvalidOperationException(
                                $"GK2 rejected the native item list for " +
                                $"{count}x '{itemId}'.");
                        }
                    },
                    () =>
                        nativeAddResult &&
                        afterTotal >= beforeTotal + count
                );

            if (!success)
            {
                Logger.LogWarning(
                    $"Spawn Item ({itemId} x{count}) did not complete. " +
                    $"Destination inventory changed {beforeTotal}->{afterTotal}; " +
                    $"visible stack {beforeVisibleStack}->{afterVisibleStack}.");
                return;
            }

            Logger.LogInfo(
                $"Spawn Item completed through GK2's native ItemCount pipeline: " +
                $"'{itemId}' materialized={materializedCount} across " +
                $"{items.Count} item object(s); destination total " +
                $"{beforeTotal}->{afterTotal}; visible stack " +
                $"{beforeVisibleStack}->{afterVisibleStack}. " +
                $"Native stack limit={itemDef.stackCount}.");
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

        private void RefillEnergy()
        {
            PlayerData playerData = MainGame.PlayerData;
            PlayerEnergyGameResSystem energySystem =
                PlayerEnergyGameResSystem.GetSystem();

            if (playerData == null ||
                energySystem == null)
            {
                Logger.LogWarning(
                    "Refill Energy was blocked because the native energy system is unavailable.");
                return;
            }

            float before =
                playerData.GetRes(EnergyResource);

            bool success = _saveService.TryRunProtectedMutation(
                "Refill Energy",
                SaveMutationRisk.Low,
                () => energySystem.Set(energySystem.Max),
                energySystem.HasMax);

            if (!success)
            {
                Logger.LogWarning(
                    "Refill Energy did not complete.");
                return;
            }

            float after =
                playerData.GetRes(EnergyResource);

            Logger.LogInfo(
                $"Refill Energy completed: {before:0.##} -> {after:0.##}/{energySystem.Max:0.##}.");
        }
    }
}
