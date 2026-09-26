using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using GK2Plus.Core;
using GK2Plus.Framework.UI;
using HarmonyLib;

namespace GK2Plus.Features.Inventory
{
    /// <summary>
    /// Shared Storage makes eligible storage inventories available through GK2+'s
    /// storage access surfaces while keeping GK2's native
    /// transfer, capacity, stack, filter, bag, and notification logic intact.
    ///
    /// No custom shared-storage state is created. The physically opened chest
    /// remains vanilla's right-side inventory and is still excluded from the
    /// current-zone MultiInventory by the game's own constructor path.
    /// </summary>
    internal sealed class SharedChestsFeature : FeatureBase
    {
        private enum SharedStorageScope
        {
            CurrentZone,
            Global
        }

        private static SharedChestsFeature _activeInstance;

        private readonly ConfigFile _config;
        private readonly GK2UIService _uiService;

        private ConfigEntry<SharedStorageScope> _storageScope;
        private ConfigEntry<bool> _characterInventoryAccess;
        private ConfigEntry<bool> _useItemsFromStorage;
        private ConfigEntry<bool> _craftFromStorage;

        private static global::Inventory _pendingUseSourceInventory;
        private static string _pendingUseItemId;

        private bool _loggedRuntimeSuccess;
        private bool _loggedCharacterInventorySuccess;
        private bool _loggedGlobalCraftingSuccess;
        private bool _loggedGlobalBuildingSuccess;
        private bool _loggedRemoteUseSuccess;
        private bool _loggedMissingShape;

        internal SharedChestsFeature(
            ConfigFile config,
            GK2UIService uiService)
        {
            _config = config ??
                throw new ArgumentNullException(nameof(config));

            _uiService = uiService ??
                throw new ArgumentNullException(nameof(uiService));
        }

        public override string Id => "shared-chests";

        public override string Name => "Shared Storage";

        public override string Category => "Inventory";

        public override string Description =>
            "Access eligible storage through GK2+ while preserving native inventory behavior.";

        protected override bool DefaultEnabled => true;

        protected override void OnInitialize()
        {
            _activeInstance = this;

            _storageScope = _config.Bind(
                Category,
                $"{Id}.StorageScope",
                SharedStorageScope.CurrentZone,
                new ConfigDescription(
                    "Choose which eligible storage inventories Shared Storage exposes. " +
                    "CurrentZone keeps access limited to the current world zone. " +
                    "Global includes eligible OpenInMultiInventory storage from persisted world data.")
            );

            _characterInventoryAccess = _config.Bind(
                Category,
                $"{Id}.CharacterInventoryAccess",
                true,
                new ConfigDescription(
                    "Allow Shared Storage access from the character inventory for the selected Storage Scope. " +
                    "Remote consumable Use is controlled separately; Equip, Plant, Fertilize, Destroy, and hotbar actions remain player-inventory only. " +
                    "Recommended: configure from the GK2+ main-menu Inventory tab; external config edits apply on next launch.")
            );

            _useItemsFromStorage = _config.Bind(
                Category,
                $"{Id}.UseItemsFromStorage",
                false,
                new ConfigDescription(
                    "Allow consumable Use actions directly from eligible Shared Storage shown in the character inventory. " +
                    "This does not enable Equip, Plant, Fertilize, Destroy, or hotbar actions for remote items.")
            );

            _craftFromStorage = _config.Bind(
                Category,
                $"{Id}.CraftFromStorage",
                false,
                new ConfigDescription(
                    "When Storage Scope is Global, extend native crafting and blueprint/building material checks and consumption to eligible global Shared Storage. " +
                    "Current-zone crafting remains controlled by GK2's native behavior.")
            );

            List<ConstructorInfo> constructors =
                typeof(global::UIBaseChestWindowData)
                    .GetConstructors(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic)
                    .Where(ctor =>
                        ctor.GetParameters()
                            .Any(parameter =>
                                parameter.ParameterType ==
                                typeof(global::MultiInventory)))
                    .ToList();

            if (constructors.Count == 0)
            {
                Logger.LogError(
                    "Shared Storage could not resolve a UIBaseChestWindowData constructor " +
                    "that receives MultiInventory. The feature will remain inactive.");
            }
            else
            {
                HarmonyMethod prefix =
                    new HarmonyMethod(
                        typeof(SharedChestsFeature),
                        nameof(UIBaseChestWindowDataPrefix));

                HarmonyMethod postfix =
                    new HarmonyMethod(
                        typeof(SharedChestsFeature),
                        nameof(UIBaseChestWindowDataPostfix));

                foreach (ConstructorInfo constructor in constructors)
                {
                    Harmony.Patch(
                        constructor,
                        prefix: prefix,
                        postfix: postfix);
                }

                Logger.LogInfo(
                    $"Shared Storage hooked {constructors.Count} native chest-window data constructor(s).");
            }

            List<ConstructorInfo> characterWindowConstructors =
                typeof(global::CharacterWindowData)
                    .GetConstructors(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic)
                    .ToList();

            if (characterWindowConstructors.Count > 0)
            {
                HarmonyMethod characterPostfix =
                    new HarmonyMethod(
                        typeof(SharedChestsFeature),
                        nameof(CharacterWindowDataPostfix));

                foreach (ConstructorInfo constructor in characterWindowConstructors)
                {
                    Harmony.Patch(
                        constructor,
                        postfix: characterPostfix);
                }

                Logger.LogInfo(
                    $"Shared Storage hooked {characterWindowConstructors.Count} character-window data constructor(s).");
            }
            else
            {
                Logger.LogWarning(
                    "Shared Storage could not resolve CharacterWindowData constructors. Character-inventory storage access will remain vanilla.");
            }

            MethodInfo useItemMethod =
                AccessTools.Method(
                    typeof(global::PlayerData),
                    nameof(global::PlayerData.UseItem),
                    new[] { typeof(global::Item) });

            if (useItemMethod != null)
            {
                Harmony.Patch(
                    useItemMethod,
                    prefix: new HarmonyMethod(
                        typeof(SharedChestsFeature),
                        nameof(PlayerDataUseItemPrefix)),
                    postfix: new HarmonyMethod(
                        typeof(SharedChestsFeature),
                        nameof(PlayerDataUseItemPostfix)),
                    finalizer: new HarmonyMethod(
                        typeof(SharedChestsFeature),
                        nameof(PlayerDataUseItemFinalizer)));
            }
            else
            {
                Logger.LogWarning(
                    "Shared Storage could not resolve PlayerData.UseItem. Remote item Use will remain vanilla.");
            }

            MethodInfo removeItemByIdMethod =
                AccessTools.Method(
                    typeof(global::Inventory),
                    nameof(global::Inventory.RemoveItemById),
                    new[]
                    {
                        typeof(string),
                        typeof(int),
                        typeof(global::Item),
                        typeof(global::Item),
                        typeof(bool)
                    });

            if (removeItemByIdMethod != null)
            {
                Harmony.Patch(
                    removeItemByIdMethod,
                    prefix: new HarmonyMethod(
                        typeof(SharedChestsFeature),
                        nameof(InventoryRemoveItemByIdPrefix)));
            }
            else
            {
                Logger.LogWarning(
                    "Shared Storage could not resolve Inventory.RemoveItemById. Remote item Use will remain vanilla.");
            }

            MethodInfo getCraftableMultiInventoryMethod =
                AccessTools.Method(
                    typeof(global::WgoData),
                    nameof(global::WgoData.GetCraftableMultiInventory),
                    new[] { typeof(bool) });

            if (getCraftableMultiInventoryMethod != null)
            {
                Harmony.Patch(
                    getCraftableMultiInventoryMethod,
                    postfix: new HarmonyMethod(
                        typeof(SharedChestsFeature),
                        nameof(WgoDataGetCraftableMultiInventoryPostfix)));
            }
            else
            {
                Logger.LogWarning(
                    "Shared Storage could not resolve WgoData.GetCraftableMultiInventory. Global crafting will remain zone-bound.");
            }

            MethodInfo buildManagerTryEnableMethod =
                AccessTools.Method(
                    typeof(global::BuildManager),
                    nameof(global::BuildManager.TryEnable),
                    new[]
                    {
                        typeof(global::Wgo),
                        typeof(Func<List<global::Inventory>>)
                    });

            if (buildManagerTryEnableMethod != null)
            {
                Harmony.Patch(
                    buildManagerTryEnableMethod,
                    prefix: new HarmonyMethod(
                        typeof(SharedChestsFeature),
                        nameof(BuildManagerTryEnablePrefix)));
            }
            else
            {
                Logger.LogWarning(
                    "Shared Storage could not resolve BuildManager.TryEnable. Global blueprint/building materials will remain zone-bound.");
            }

            _uiService.RegisterFeatureToggleControl(
                new GK2FeatureToggleControl(
                    Id,
                    Category,
                    "Shared Storage",
                    () => Enabled?.Value ?? DefaultEnabled,
                    BuildUiStatus,
                    SetEnabledFromMainMenu,
                    order: 200));

            _uiService.RegisterFeatureOptionControl(
                new GK2FeatureOptionControl(
                    $"{Id}.storage-scope",
                    Category,
                    "Storage Scope",
                    () => (_storageScope?.Value ?? SharedStorageScope.CurrentZone).ToString(),
                    BuildStorageScopeOptions,
                    SetStorageScopeFromMainMenu,
                    parentFeatureId: Id,
                    order: 5,
                    enabledProvider: () => Enabled?.Value ?? DefaultEnabled));

            _uiService.RegisterFeatureOptionControl(
                new GK2FeatureOptionControl(
                    $"{Id}.character-inventory-access",
                    Category,
                    "Character Inventory Access",
                    () => (_characterInventoryAccess?.Value ?? true)
                        ? "ON"
                        : "OFF",
                    BuildBooleanOptions,
                    SetCharacterInventoryAccessFromMainMenu,
                    parentFeatureId: Id,
                    order: 10,
                    enabledProvider: () => Enabled?.Value ?? DefaultEnabled));

            _uiService.RegisterFeatureOptionControl(
                new GK2FeatureOptionControl(
                    $"{Id}.use-items-from-storage",
                    Category,
                    "Use Items From Storage",
                    () => (_useItemsFromStorage?.Value ?? false)
                        ? "ON"
                        : "OFF",
                    BuildBooleanOptions,
                    SetUseItemsFromStorageFromMainMenu,
                    parentFeatureId: Id,
                    order: 20,
                    enabledProvider: () =>
                        (Enabled?.Value ?? DefaultEnabled) &&
                        (_characterInventoryAccess?.Value ?? true)));

            _uiService.RegisterFeatureOptionControl(
                new GK2FeatureOptionControl(
                    $"{Id}.craft-from-storage",
                    Category,
                    "Craft From Selected Scope",
                    () => (_craftFromStorage?.Value ?? false)
                        ? "ON"
                        : "OFF",
                    BuildBooleanOptions,
                    SetCraftFromStorageFromMainMenu,
                    parentFeatureId: Id,
                    order: 30,
                    enabledProvider: () =>
                        (Enabled?.Value ?? DefaultEnabled) &&
                        (_storageScope?.Value ?? SharedStorageScope.CurrentZone) ==
                            SharedStorageScope.Global));
        }

        protected override void OnEnabled()
        {
            Logger.LogInfo(
                $"Shared Storage enabled with scope {BuildStorageScopeLabel(_storageScope?.Value ?? SharedStorageScope.CurrentZone)}.");
        }

        private IReadOnlyList<GK2FeatureOption> BuildStorageScopeOptions()
        {
            return new[]
            {
                new GK2FeatureOption(
                    SharedStorageScope.CurrentZone.ToString(),
                    "Current Zone"),
                new GK2FeatureOption(
                    SharedStorageScope.Global.ToString(),
                    "Global")
            };
        }

        private static string BuildStorageScopeLabel(
            SharedStorageScope scope)
        {
            return scope == SharedStorageScope.Global
                ? "Global"
                : "Current Zone";
        }

        private void SetStorageScopeFromMainMenu(
            string value)
        {
            if (_storageScope == null ||
                !Enum.TryParse(
                    value,
                    ignoreCase: true,
                    out SharedStorageScope scope))
            {
                return;
            }

            if (_storageScope.Value == scope)
            {
                _uiService.RefreshMenu();
                return;
            }

            _storageScope.Value = scope;
            _config.Save();

            Logger.LogInfo(
                $"Shared Storage scope changed to {BuildStorageScopeLabel(scope)} from the main menu.");

            _uiService.RefreshMenu();
        }

        private string BuildUiStatus()
        {
            return Enabled?.Value == true
                ? "ON"
                : "OFF";
        }

        private IReadOnlyList<GK2FeatureOption> BuildBooleanOptions()
        {
            return new[]
            {
                new GK2FeatureOption("true", "ON"),
                new GK2FeatureOption("false", "OFF")
            };
        }

        private void SetUseItemsFromStorageFromMainMenu(
            string value)
        {
            if (_useItemsFromStorage == null ||
                !bool.TryParse(
                    value,
                    out bool enabled))
            {
                return;
            }

            if (_useItemsFromStorage.Value != enabled)
            {
                _useItemsFromStorage.Value = enabled;
                _config.Save();

                Logger.LogInfo(
                    $"Shared Storage Use Items From Storage {(enabled ? "enabled" : "disabled")} from the main menu.");
            }

            _uiService.RefreshMenu();
        }

        private void SetCraftFromStorageFromMainMenu(
            string value)
        {
            if (_craftFromStorage == null ||
                !bool.TryParse(
                    value,
                    out bool enabled))
            {
                return;
            }

            if (_craftFromStorage.Value != enabled)
            {
                _craftFromStorage.Value = enabled;
                _config.Save();

                Logger.LogInfo(
                    $"Shared Storage Craft From Selected Scope {(enabled ? "enabled" : "disabled")} from the main menu.");
            }

            _uiService.RefreshMenu();
        }

        private void SetCharacterInventoryAccessFromMainMenu(
            string value)
        {
            if (_characterInventoryAccess == null ||
                !bool.TryParse(
                    value,
                    out bool enabled))
            {
                return;
            }

            if (_characterInventoryAccess.Value == enabled)
            {
                _uiService.RefreshMenu();
                return;
            }

            _characterInventoryAccess.Value = enabled;
            _config.Save();

            Logger.LogInfo(
                $"Shared Storage Character Inventory Access {(enabled ? "enabled" : "disabled")} from the main menu.");

            _uiService.RefreshMenu();
        }

        private void SetEnabledFromMainMenu(
            bool enabled)
        {
            if (Enabled == null)
            {
                return;
            }

            if (Enabled.Value == enabled)
            {
                _uiService.RefreshMenu();
                return;
            }

            Enabled.Value =
                enabled;

            _config.Save();

            Logger.LogInfo(
                $"Shared Storage {(enabled ? "enabled" : "disabled")} from the main menu.");

            _uiService.RefreshMenu();
        }

        private static void PlayerDataUseItemPrefix(
            global::PlayerData __instance,
            global::Item item)
        {
            ClearPendingUseRedirect();

            SharedChestsFeature feature =
                _activeInstance;

            if (feature == null ||
                feature.Enabled?.Value != true ||
                feature._characterInventoryAccess?.Value != true ||
                feature._useItemsFromStorage?.Value != true ||
                __instance == null ||
                item == null ||
                item.IsEmpty ||
                item.Definition == null ||
                !item.Definition.CanBeUsed ||
                item.Definition.stayOnUse ||
                feature.InventoryContainsItemByUniqueId(
                    __instance.Inventory,
                    item))
            {
                return;
            }

            global::Inventory sourceInventory =
                feature.FindEligibleStorageInventoryContainingItem(
                    item);

            if (sourceInventory == null)
            {
                return;
            }

            _pendingUseSourceInventory =
                sourceInventory;
            _pendingUseItemId =
                item.id;
        }

        private static void PlayerDataUseItemPostfix()
        {
            ClearPendingUseRedirect();
        }

        private static Exception PlayerDataUseItemFinalizer(
            Exception __exception)
        {
            ClearPendingUseRedirect();
            return __exception;
        }

        private static bool InventoryRemoveItemByIdPrefix(
            global::Inventory __instance,
            string itemId,
            int count,
            ref List<global::Item> __result)
        {
            if (_pendingUseSourceInventory == null ||
                string.IsNullOrWhiteSpace(
                    _pendingUseItemId) ||
                !string.Equals(
                    _pendingUseItemId,
                    itemId,
                    StringComparison.Ordinal) ||
                !ReferenceEquals(
                    __instance,
                    global::MainGame.PlayerData?.Inventory))
            {
                return true;
            }

            global::Inventory sourceInventory =
                _pendingUseSourceInventory;

            ClearPendingUseRedirect();

            __result =
                sourceInventory.RemoveItemById(
                    itemId,
                    count,
                    null,
                    null,
                    false);

            SharedChestsFeature feature =
                _activeInstance;

            if (feature != null &&
                __result != null &&
                __result.Count > 0 &&
                !feature._loggedRemoteUseSuccess)
            {
                feature._loggedRemoteUseSuccess = true;

                feature.Logger.LogInfo(
                    "Shared Storage successfully redirected a native consumable Use removal to the selected storage inventory.");
            }

            return false;
        }

        private static void ClearPendingUseRedirect()
        {
            _pendingUseSourceInventory = null;
            _pendingUseItemId = null;
        }

        private bool InventoryContainsItemByUniqueId(
            global::Inventory inventory,
            global::Item item)
        {
            if (inventory == null ||
                item == null ||
                item.UniqueId == null)
            {
                return false;
            }

            global::Item found =
                inventory.GetItemByUniqueId(
                    item.UniqueId.ToString());

            return found != null &&
                !found.IsEmpty;
        }

        private global::Inventory FindEligibleStorageInventoryContainingItem(
            global::Item item)
        {
            global::PlayerData playerData =
                global::MainGame.PlayerData;

            if (playerData == null ||
                item == null ||
                item.UniqueId == null)
            {
                return null;
            }

            global::MultiInventory currentZone =
                playerData.CurrentWorldZoneData == null
                    ? new global::MultiInventory()
                    : new global::MultiInventory(
                        playerData.CurrentWorldZoneData);

            global::MultiInventory eligible =
                (_storageScope?.Value ?? SharedStorageScope.CurrentZone) ==
                    SharedStorageScope.Global
                    ? BuildGlobalStorageMultiInventory(
                        currentZone,
                        playerData.Inventory)
                    : currentZone;

            foreach (global::Inventory inventory in
                eligible.inventoryList)
            {
                if (InventoryContainsItemByUniqueId(
                    inventory,
                    item))
                {
                    return inventory;
                }
            }

            return null;
        }

        private static void WgoDataGetCraftableMultiInventoryPostfix(
            global::WgoData __instance,
            ref global::MultiInventory __result)
        {
            SharedChestsFeature feature =
                _activeInstance;

            if (feature == null ||
                feature.Enabled?.Value != true ||
                feature._craftFromStorage?.Value != true ||
                feature._storageScope?.Value != SharedStorageScope.Global ||
                __instance == null ||
                __result == null)
            {
                return;
            }

            int added =
                feature.AppendMissingGlobalStorage(
                    __result,
                    __instance.Inventory,
                    __instance.CraftInventory,
                    global::MainGame.PlayerData?.Inventory);

            if (added > 0 &&
                !feature._loggedGlobalCraftingSuccess)
            {
                feature._loggedGlobalCraftingSuccess = true;

                feature.Logger.LogInfo(
                    $"Shared Storage extended native crafting with {added} global storage inventory source(s).");
            }
        }

        private static void BuildManagerTryEnablePrefix(
            ref Func<List<global::Inventory>> getAdditionalInventories)
        {
            SharedChestsFeature feature =
                _activeInstance;

            if (feature == null ||
                feature.Enabled?.Value != true ||
                feature._craftFromStorage?.Value != true ||
                feature._storageScope?.Value != SharedStorageScope.Global)
            {
                return;
            }

            Func<List<global::Inventory>> original =
                getAdditionalInventories;

            getAdditionalInventories = () =>
            {
                List<global::Inventory> inventories =
                    original?.Invoke() ??
                    new List<global::Inventory>();

                int added =
                    feature.AppendCrossZoneStorage(
                        inventories);

                if (added > 0 &&
                    !feature._loggedGlobalBuildingSuccess)
                {
                    feature._loggedGlobalBuildingSuccess = true;

                    feature.Logger.LogInfo(
                        $"Shared Storage extended native blueprint/building materials with {added} cross-zone storage inventory source(s).");
                }

                return inventories;
            };
        }

        private int AppendMissingGlobalStorage(
            global::MultiInventory target,
            params global::Inventory[] excludedInventories)
        {
            if (target == null)
            {
                return 0;
            }

            global::MultiInventory globalStorage =
                BuildGlobalStorageMultiInventory(
                    null,
                    excludedInventories);

            int added = 0;

            foreach (global::Inventory inventory in
                globalStorage.inventoryList)
            {
                if (inventory == null ||
                    ContainsInventoryByReference(
                        target.inventoryList,
                        inventory))
                {
                    continue;
                }

                target.Add(
                    inventory);
                added++;
            }

            return added;
        }

        private int AppendCrossZoneStorage(
            List<global::Inventory> target)
        {
            if (target == null)
            {
                return 0;
            }

            global::PlayerData playerData =
                global::MainGame.PlayerData;

            if (playerData == null)
            {
                return 0;
            }

            global::MultiInventory currentZone =
                playerData.CurrentWorldZoneData == null
                    ? new global::MultiInventory()
                    : new global::MultiInventory(
                        playerData.CurrentWorldZoneData);

            global::MultiInventory allStorage =
                BuildGlobalStorageMultiInventory(
                    null,
                    playerData.Inventory);

            int added = 0;

            foreach (global::Inventory inventory in
                allStorage.inventoryList)
            {
                if (inventory == null ||
                    ContainsInventoryByReference(
                        currentZone.inventoryList,
                        inventory) ||
                    ContainsInventoryByReference(
                        target,
                        inventory))
                {
                    continue;
                }

                target.Add(
                    inventory);
                added++;
            }

            return added;
        }

        private static void UIBaseChestWindowDataPrefix(
            global::Inventory __0,
            ref global::MultiInventory __1,
            global::WgoData __2)
        {
            SharedChestsFeature feature =
                _activeInstance;

            if (feature == null ||
                feature.Enabled?.Value != true ||
                feature._storageScope?.Value != SharedStorageScope.Global)
            {
                return;
            }

            __1 = feature.BuildGlobalStorageMultiInventory(
                __1,
                __2?.Inventory,
                __0);

            feature.Logger.LogDebug(
                $"Shared Storage Global scope supplied {__1?.inventoryList?.Count ?? 0} storage inventory source(s) to the chest window constructor.");
        }

        private global::MultiInventory BuildGlobalStorageMultiInventory(
            global::MultiInventory preferredFirst,
            params global::Inventory[] excludedInventories)
        {
            List<global::Inventory> inventories =
                new List<global::Inventory>();

            HashSet<global::Inventory> seen =
                new HashSet<global::Inventory>(
                    ReferenceEqualityComparer<global::Inventory>.Instance);

            HashSet<global::Inventory> excluded =
                new HashSet<global::Inventory>(
                    ReferenceEqualityComparer<global::Inventory>.Instance);

            if (excludedInventories != null)
            {
                foreach (global::Inventory excludedInventory in excludedInventories)
                {
                    if (excludedInventory != null)
                    {
                        excluded.Add(
                            excludedInventory);
                    }
                }
            }

            if (preferredFirst?.inventoryList != null)
            {
                foreach (global::Inventory inventory in
                    preferredFirst.inventoryList)
                {
                    if (inventory != null &&
                        !excluded.Contains(inventory) &&
                        seen.Add(inventory))
                    {
                        inventories.Add(
                            inventory);
                    }
                }
            }

            global::WorldData worldData =
                global::MainGame.Instance?.GameSave?.worldData;

            if (worldData?.gameSceneDataList == null)
            {
                return new global::MultiInventory(
                    inventories);
            }

            foreach (global::GameSceneData sceneData in
                worldData.gameSceneDataList)
            {
                if (sceneData?.wgoDataList == null)
                {
                    continue;
                }

                foreach (global::WgoData wgoData in
                    sceneData.wgoDataList)
                {
                    if (wgoData == null)
                    {
                        continue;
                    }

                    try
                    {
                        if (wgoData.Definition == null ||
                            wgoData.Definition.inventorySize == 0 ||
                            !wgoData.Definition.OpenInMultiInventory)
                        {
                            continue;
                        }

                        global::Inventory inventory =
                            wgoData.Inventory;

                        if (inventory == null ||
                            excluded.Contains(inventory) ||
                            !seen.Add(inventory))
                        {
                            continue;
                        }

                        inventories.Add(
                            inventory);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug(
                            $"Shared Storage skipped one persisted storage candidate during Global scope enumeration: {ex.Message}");
                    }
                }
            }

            return new global::MultiInventory(
                inventories);
        }

        private static void UIBaseChestWindowDataPostfix(
            object __instance,
            object[] __args)
        {
            _activeInstance?.ApplyToChestWindowData(
                __instance,
                __args);
        }

        private static void CharacterWindowDataPostfix(
            object __instance)
        {
            SharedChestsFeature feature =
                _activeInstance;

            if (feature == null ||
                feature._characterInventoryAccess?.Value == false)
            {
                return;
            }

            feature.ApplyToCharacterInventoryData(
                __instance);
        }

        private void ApplyToChestWindowData(
            object instance,
            object[] args)
        {
            if (Enabled == null ||
                !Enabled.Value ||
                instance == null ||
                args == null)
            {
                return;
            }

            global::Inventory playerInventory =
                args.OfType<global::Inventory>()
                    .FirstOrDefault();

            global::MultiInventory worldZoneMultiInventory =
                args.OfType<global::MultiInventory>()
                    .FirstOrDefault();

            if (playerInventory == null ||
                worldZoneMultiInventory == null ||
                worldZoneMultiInventory.inventoryList == null ||
                worldZoneMultiInventory.inventoryList.Count == 0)
            {
                return;
            }

            object firstMultiInventoryData =
                GetMemberValue(
                    instance,
                    "FirstMultiInventoryData");

            if (firstMultiInventoryData == null)
            {
                LogMissingShapeOnce(
                    "UIBaseChestWindowData.FirstMultiInventoryData was not found.");
                return;
            }

            List<object> widgetData =
                FindInventoryWidgetData(
                    firstMultiInventoryData);

            if (widgetData.Count == 0)
            {
                LogMissingShapeOnce(
                    "Shared Storage could not find inventory widget data under FirstMultiInventoryData.");
                return;
            }

            object playerWidget =
                widgetData.FirstOrDefault(widget =>
                    ReferenceEquals(
                        GetInventory(widget),
                        playerInventory));

            if (playerWidget == null)
            {
                LogMissingShapeOnce(
                    "Shared Storage could not identify the player inventory widget used as the native callback template.");
                return;
            }

            int enabledWidgets = 0;
            int copiedDelegates = 0;
            int copiedAvailability = 0;
            int updatedStates = 0;

            foreach (object widget in widgetData)
            {
                global::Inventory inventory =
                    GetInventory(widget);

                if (inventory == null ||
                    ReferenceEquals(
                        inventory,
                        playerInventory))
                {
                    continue;
                }

                bool eligibleStorage =
                    _storageScope?.Value == SharedStorageScope.Global ||
                    ContainsInventoryByReference(
                        worldZoneMultiInventory.inventoryList,
                        inventory);

                if (!eligibleStorage)
                {
                    continue;
                }

                int delegates =
                    CopyItemPressDelegates(
                        playerWidget,
                        widget);

                bool availability =
                    CopyNamedMember(
                        playerWidget,
                        widget,
                        "CustomItemsAvailableCondition") ||
                    GetMemberValue(
                        widget,
                        "CustomItemsAvailableCondition") != null;

                bool state =
                    SetItemRelatedWidgetState(
                        widget,
                        "Inactive");

                // Treat the widget as fully activated only when all three
                // pieces of vanilla left-side behavior were restored:
                // selectable state, item-press callbacks, and the normal item
                // availability predicate. Leaving the availability predicate at
                // vanilla's always-false value is what makes remote slots render
                // greyed out even though their contents are visible.
                if (state &&
                    delegates > 0 &&
                    availability)
                {
                    enabledWidgets++;
                    copiedDelegates += delegates;
                    copiedAvailability++;
                    updatedStates++;
                }
            }

            if (enabledWidgets <= 0)
            {
                LogMissingShapeOnce(
                    "Shared Storage found eligible storage inventories but could not activate their native widget callbacks/state.");
                return;
            }

            if (!_loggedRuntimeSuccess)
            {
                _loggedRuntimeSuccess = true;

                Logger.LogInfo(
                    $"Shared Storage activated {enabledWidgets} storage widget(s) " +
                    $"using vanilla transfer callbacks " +
                    $"(delegates={copiedDelegates}, availability={copiedAvailability}, states={updatedStates}).");
            }
            else
            {
                Logger.LogDebug(
                    $"Shared Storage prepared chest UI: storage={enabledWidgets}.");
            }
        }

        private void ApplyToCharacterInventoryData(
            object characterWindowData)
        {
            if (Enabled == null ||
                !Enabled.Value ||
                characterWindowData == null)
            {
                return;
            }

            object mainPageData =
                GetMemberValue(
                    characterWindowData,
                    "CharMainPageWidgetData");

            if (mainPageData == null)
            {
                LogMissingShapeOnce(
                    "Shared Storage could not resolve CharacterWindowData.CharMainPageWidgetData.");
                return;
            }

            object multiInventoryData =
                GetMemberValue(
                    mainPageData,
                    "MultiInventoryWidgetData");

            if (multiInventoryData == null)
            {
                LogMissingShapeOnce(
                    "Shared Storage could not resolve the character inventory MultiInventoryWidgetData.");
                return;
            }

            AppendGlobalCharacterStorageWidgets(
                characterWindowData,
                multiInventoryData);

            List<object> widgetData =
                FindInventoryWidgetData(
                    multiInventoryData);

            if (widgetData.Count < 2)
            {
                return;
            }

            object playerWidget =
                widgetData.FirstOrDefault(widget =>
                    GetDelegateMember(
                        widget,
                        "OnItemCellPress") != null &&
                    !IsItemRelatedWidgetState(
                        widget,
                        "Disabled"));

            if (playerWidget == null)
            {
                LogMissingShapeOnce(
                    "Shared Storage could not identify the active player inventory widget in the character inventory.");
                return;
            }

            Delegate playerPress =
                GetDelegateMember(
                    playerWidget,
                    "OnItemCellPress");

            Delegate playerPress2 =
                GetDelegateMember(
                    playerWidget,
                    "OnItemCellPress2");

            Delegate playerDown =
                GetDelegateMember(
                    playerWidget,
                    "OnItemCellDown");

            Delegate playerHover =
                GetDelegateMember(
                    playerWidget,
                    "OnItemCellOver");

            int activated = 0;
            int callbacks = 0;
            int states = 0;

            foreach (object widget in widgetData)
            {
                if (ReferenceEquals(
                        widget,
                        playerWidget) ||
                    !IsItemRelatedWidgetState(
                        widget,
                        "Disabled"))
                {
                    continue;
                }

                // Runtime recon shows the character window already gives these
                // remote/current-zone inventory widgets the same native
                // PlayerItemsAvailabilityCondition as the player inventory.
                // Vanilla only leaves their input callbacks empty and their
                // widget state Disabled. Restore those missing pieces while
                // keeping PlayerInventoryUIItemOpHandler as the transfer owner.
                int copied = 0;

                copied += SetDelegateMember(
                    widget,
                    "OnItemCellPress",
                    playerPress)
                    ? 1
                    : 0;

                copied += SetDelegateMember(
                    widget,
                    "OnItemCellPress2",
                    playerPress2)
                    ? 1
                    : 0;

                copied += SetDelegateMember(
                    widget,
                    "OnItemCellDown",
                    playerDown)
                    ? 1
                    : 0;

                copied += SetDelegateMember(
                    widget,
                    "OnItemCellOver",
                    playerHover)
                    ? 1
                    : 0;

                bool availability =
                    CopyNamedMember(
                        playerWidget,
                        widget,
                        "CustomItemsAvailableCondition");

                bool state =
                    SetItemRelatedWidgetState(
                        widget,
                        "Default");

                if (state &&
                    copied >= 2 &&
                    availability)
                {
                    activated++;
                    callbacks += copied;
                    states++;
                }
            }

            if (activated <= 0)
            {
                return;
            }

            if (!_loggedCharacterInventorySuccess)
            {
                _loggedCharacterInventorySuccess = true;

                Logger.LogInfo(
                    $"Shared Storage activated {activated} character-inventory storage widget(s) " +
                    $"using native PlayerInventoryUIItemOpHandler callbacks " +
                    $"(callbacks={callbacks}, states={states}).");
            }
            else
            {
                Logger.LogDebug(
                    $"Shared Storage prepared character inventory UI: storage={activated}.");
            }
        }

        private void AppendGlobalCharacterStorageWidgets(
            object characterWindowData,
            object multiInventoryData)
        {
            if (_storageScope?.Value != SharedStorageScope.Global ||
                !(multiInventoryData is global::MultiInventoryWidgetData typedWidgetData))
            {
                return;
            }

            global::PlayerData playerData =
                GetMemberValue(
                    characterWindowData,
                    "PlayerData") as global::PlayerData ??
                global::MainGame.PlayerData;

            if (playerData == null)
            {
                return;
            }

            global::MultiInventory currentZone =
                playerData.CurrentWorldZoneData == null
                    ? new global::MultiInventory()
                    : new global::MultiInventory(
                        playerData.CurrentWorldZoneData);

            global::MultiInventory allStorage =
                BuildGlobalStorageMultiInventory(
                    currentZone,
                    playerData.Inventory);

            List<global::Inventory> extraInventories =
                allStorage.inventoryList
                    .Where(inventory =>
                        inventory != null &&
                        !ContainsInventoryByReference(
                            currentZone.inventoryList,
                            inventory))
                    .ToList();

            if (extraInventories.Count == 0)
            {
                return;
            }

            global::MultiInventory extras =
                new global::MultiInventory(
                    extraInventories);

            List<global::InventoryWidgetDataBase> extraWidgets =
                global::InventoryWidgetDataHelper
                    .GetWidgetsDataForMultiInventory(
                        extras,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        addBags: true,
                        disableHeaderForFirstWidget: false,
                        customState: global::ItemRelatedWidgetState.Disabled,
                        customStateBags: global::ItemRelatedWidgetState.Disabled);

            typedWidgetData.AddRange(
                extraWidgets);

            Logger.LogDebug(
                $"Shared Storage Global scope added {extraInventories.Count} cross-zone inventory widget source(s) to the character inventory.");
        }

        private static Delegate GetDelegateMember(
            object instance,
            string memberName)
        {
            object value =
                GetMemberValue(
                    instance,
                    memberName);

            return value as Delegate;
        }

        private static bool SetDelegateMember(
            object target,
            string memberName,
            Delegate value)
        {
            if (target == null ||
                value == null)
            {
                return false;
            }

            MemberInfo member =
                FindMember(
                    target.GetType(),
                    memberName,
                    writable: true);

            if (member != null &&
                GetMemberType(member) == value.GetType() &&
                TrySetMemberValue(
                    target,
                    member,
                    value))
            {
                return true;
            }

            FieldInfo backingField =
                FindField(
                    target.GetType(),
                    $"<{memberName}>k__BackingField");

            return backingField != null &&
                backingField.FieldType == value.GetType() &&
                TrySetFieldValue(
                    target,
                    backingField,
                    value);
        }

        private static bool IsItemRelatedWidgetState(
            object target,
            string enumValue)
        {
            foreach (MemberInfo member in
                GetAllReadableMembers(
                    target.GetType()))
            {
                Type memberType =
                    GetMemberType(member);

                if (memberType == null ||
                    !memberType.IsEnum ||
                    !string.Equals(
                        memberType.Name,
                        "ItemRelatedWidgetState",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                object current =
                    TryGetMemberValue(
                        target,
                        member);

                return current != null &&
                    string.Equals(
                        current.ToString(),
                        enumValue,
                        StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private void LogMissingShapeOnce(
            string message)
        {
            if (_loggedMissingShape)
            {
                Logger.LogDebug(message);
                return;
            }

            _loggedMissingShape = true;

            Logger.LogWarning(
                message + " No chest inventory behavior was changed.");
        }

        private static List<object> FindInventoryWidgetData(
            object multiInventoryData)
        {
            List<object> result =
                new List<object>();

            HashSet<object> seen =
                new HashSet<object>(
                    ReferenceEqualityComparer<object>.Instance);

            foreach (MemberInfo member in
                GetAllReadableMembers(
                    multiInventoryData.GetType()))
            {
                object value =
                    TryGetMemberValue(
                        multiInventoryData,
                        member);

                if (value == null ||
                    value is string ||
                    !(value is IEnumerable enumerable))
                {
                    continue;
                }

                foreach (object item in enumerable)
                {
                    if (item == null ||
                        !seen.Add(item))
                    {
                        continue;
                    }

                    if (GetInventory(item) != null)
                    {
                        result.Add(item);
                    }
                }
            }

            return result;
        }

        private static global::Inventory GetInventory(
            object widgetData)
        {
            if (widgetData == null)
            {
                return null;
            }

            object exact =
                GetMemberValue(
                    widgetData,
                    "Inventory");

            if (exact is global::Inventory inventory)
            {
                return inventory;
            }

            foreach (MemberInfo member in
                GetAllReadableMembers(
                    widgetData.GetType()))
            {
                Type memberType =
                    GetMemberType(member);

                if (memberType == null ||
                    !typeof(global::Inventory)
                        .IsAssignableFrom(memberType))
                {
                    continue;
                }

                object value =
                    TryGetMemberValue(
                        widgetData,
                        member);

                if (value is global::Inventory fallback)
                {
                    return fallback;
                }
            }

            return null;
        }

        private static int CopyItemPressDelegates(
            object source,
            object target)
        {
            int copied = 0;

            Dictionary<string, MemberInfo> targetMembers =
                GetAllWritableMembers(
                    target.GetType())
                    .ToDictionary(
                        member => member.Name,
                        member => member,
                        StringComparer.OrdinalIgnoreCase);

            foreach (MemberInfo sourceMember in
                GetAllReadableMembers(
                    source.GetType()))
            {
                Type memberType =
                    GetMemberType(sourceMember);

                if (memberType == null ||
                    !typeof(Delegate).IsAssignableFrom(
                        memberType) ||
                    sourceMember.Name.IndexOf(
                        "Item",
                        StringComparison.OrdinalIgnoreCase) < 0 ||
                    sourceMember.Name.IndexOf(
                        "Press",
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                object value =
                    TryGetMemberValue(
                        source,
                        sourceMember);

                if (!(value is Delegate))
                {
                    continue;
                }

                if (!targetMembers.TryGetValue(
                        sourceMember.Name,
                        out MemberInfo targetMember) ||
                    GetMemberType(targetMember) != memberType)
                {
                    continue;
                }

                if (TrySetMemberValue(
                    target,
                    targetMember,
                    value))
                {
                    copied++;
                }
            }

            return copied;
        }

        private static bool CopyNamedMember(
            object source,
            object target,
            string memberName)
        {
            MemberInfo sourceMember =
                FindMember(
                    source.GetType(),
                    memberName,
                    writable: false);

            if (sourceMember == null)
            {
                return false;
            }

            object value =
                TryGetMemberValue(
                    source,
                    sourceMember);

            Type sourceType =
                GetMemberType(sourceMember);

            if (sourceType == null)
            {
                return false;
            }

            MemberInfo targetMember =
                FindMember(
                    target.GetType(),
                    memberName,
                    writable: true);

            if (targetMember != null &&
                GetMemberType(targetMember) == sourceType &&
                TrySetMemberValue(
                    target,
                    targetMember,
                    value))
            {
                return true;
            }

            // GK2 exposes some widget-data values through getter-only
            // auto-properties. In that case the writable storage is the
            // compiler-generated backing field, not the property itself.
            FieldInfo backingField =
                FindField(
                    target.GetType(),
                    $"<{memberName}>k__BackingField");

            if (backingField != null &&
                backingField.FieldType == sourceType &&
                TrySetFieldValue(
                    target,
                    backingField,
                    value))
            {
                return true;
            }

            // Also handle a same-named private field when the public surface is
            // a getter-only property with a manually implemented backing field.
            FieldInfo exactField =
                FindField(
                    target.GetType(),
                    memberName);

            return exactField != null &&
                exactField.FieldType == sourceType &&
                TrySetFieldValue(
                    target,
                    exactField,
                    value);
        }

        private static bool SetItemRelatedWidgetState(
            object target,
            string enumValue)
        {
            foreach (MemberInfo member in
                GetAllReadableMembers(
                    target.GetType()))
            {
                Type memberType =
                    GetMemberType(member);

                if (memberType == null ||
                    !memberType.IsEnum ||
                    !string.Equals(
                        memberType.Name,
                        "ItemRelatedWidgetState",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                try
                {
                    object value =
                        Enum.Parse(
                            memberType,
                            enumValue,
                            ignoreCase: true);

                    if (TrySetMemberValue(
                        target,
                        member,
                        value))
                    {
                        return true;
                    }

                    FieldInfo backingField =
                        FindField(
                            target.GetType(),
                            $"<{member.Name}>k__BackingField");

                    if (backingField != null &&
                        backingField.FieldType == memberType &&
                        TrySetFieldValue(
                            target,
                            backingField,
                            value))
                    {
                        return true;
                    }

                    if (member is FieldInfo field &&
                        TrySetFieldValue(
                            target,
                            field,
                            value))
                    {
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private static bool ContainsInventoryByReference(
            IEnumerable<global::Inventory> inventories,
            global::Inventory candidate)
        {
            foreach (global::Inventory inventory in inventories)
            {
                if (ReferenceEquals(
                    inventory,
                    candidate))
                {
                    return true;
                }
            }

            return false;
        }

        private static object GetMemberValue(
            object instance,
            string memberName)
        {
            MemberInfo member =
                FindMember(
                    instance.GetType(),
                    memberName,
                    writable: false);

            return member == null
                ? null
                : TryGetMemberValue(
                    instance,
                    member);
        }

        private static MemberInfo FindMember(
            Type type,
            string memberName,
            bool writable)
        {
            IEnumerable<MemberInfo> members =
                writable
                    ? GetAllWritableMembers(type)
                    : GetAllReadableMembers(type);

            return members.FirstOrDefault(member =>
                string.Equals(
                    member.Name,
                    memberName,
                    StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<MemberInfo> GetAllReadableMembers(
            Type type)
        {
            foreach (FieldInfo field in
                GetAllFields(type))
            {
                yield return field;
            }

            foreach (PropertyInfo property in
                GetAllProperties(type))
            {
                if (property.GetGetMethod(true) != null &&
                    property.GetIndexParameters().Length == 0)
                {
                    yield return property;
                }
            }
        }

        private static IEnumerable<MemberInfo> GetAllWritableMembers(
            Type type)
        {
            foreach (FieldInfo field in
                GetAllFields(type))
            {
                if (!field.IsInitOnly)
                {
                    yield return field;
                }
            }

            foreach (PropertyInfo property in
                GetAllProperties(type))
            {
                if (property.GetSetMethod(true) != null &&
                    property.GetIndexParameters().Length == 0)
                {
                    yield return property;
                }
            }
        }

        private static IEnumerable<FieldInfo> GetAllFields(
            Type type)
        {
            for (Type current = type;
                current != null;
                current = current.BaseType)
            {
                foreach (FieldInfo field in
                    current.GetFields(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly))
                {
                    yield return field;
                }
            }
        }

        private static IEnumerable<PropertyInfo> GetAllProperties(
            Type type)
        {
            for (Type current = type;
                current != null;
                current = current.BaseType)
            {
                foreach (PropertyInfo property in
                    current.GetProperties(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly))
                {
                    yield return property;
                }
            }
        }

        private static FieldInfo FindField(
            Type type,
            string fieldName)
        {
            return GetAllFields(type)
                .FirstOrDefault(field =>
                    string.Equals(
                        field.Name,
                        fieldName,
                        StringComparison.OrdinalIgnoreCase));
        }

        private static bool TrySetFieldValue(
            object instance,
            FieldInfo field,
            object value)
        {
            try
            {
                field.SetValue(
                    instance,
                    value);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Type GetMemberType(
            MemberInfo member)
        {
            if (member is FieldInfo field)
            {
                return field.FieldType;
            }

            if (member is PropertyInfo property)
            {
                return property.PropertyType;
            }

            return null;
        }

        private static object TryGetMemberValue(
            object instance,
            MemberInfo member)
        {
            try
            {
                if (member is FieldInfo field)
                {
                    return field.GetValue(instance);
                }

                if (member is PropertyInfo property)
                {
                    MethodInfo getter =
                        property.GetGetMethod(true);

                    return getter?.Invoke(
                        instance,
                        null);
                }
            }
            catch
            {
            }

            return null;
        }

        private static bool TrySetMemberValue(
            object instance,
            MemberInfo member,
            object value)
        {
            try
            {
                if (member is FieldInfo field)
                {
                    field.SetValue(
                        instance,
                        value);

                    return true;
                }

                if (member is PropertyInfo property)
                {
                    MethodInfo setter =
                        property.GetSetMethod(true);

                    if (setter != null)
                    {
                        setter.Invoke(
                            instance,
                            new[] { value });

                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private sealed class ReferenceEqualityComparer<T> :
            IEqualityComparer<T>
            where T : class
        {
            internal static readonly ReferenceEqualityComparer<T> Instance =
                new ReferenceEqualityComparer<T>();

            public bool Equals(
                T x,
                T y)
            {
                return ReferenceEquals(
                    x,
                    y);
            }

            public int GetHashCode(
                T obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
