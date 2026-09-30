using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx.Configuration;
using GK2Plus.Core;
using GK2Plus.Framework.Saves;
using GK2Plus.Framework.UI;
using HarmonyLib;
using LazyBearTechnology;
using UnityEngine;

namespace GK2Plus.Features.Tracking
{
    internal sealed class UnifiedTrackerFeature : FeatureBase
    {
        private enum TrackerPinType
        {
            Quest,
            Craft,
            Item,
            Plan
        }

        private sealed class TrackerPin
        {
            public TrackerPinType Type;
            public string Id;
            public int Target;
            public string Label;
            public string Requirements;
        }

        private static UnifiedTrackerFeature _activeInstance;

        private readonly ConfigFile _config;
        private readonly GK2UIService _uiService;
        private readonly GK2SaveService _saveService;

        private ConfigEntry<string> _pinsRaw;
        private ConfigEntry<bool> _hudVisible;
        private ConfigEntry<bool> _autoTrackNewQuests;
        private ConfigEntry<bool> _removeCompletedQuests;
        private ConfigEntry<string> _selectedQuestId;
        private ConfigEntry<string> _selectedCraftId;
        private ConfigEntry<string> _selectedItemId;
        private ConfigEntry<int> _selectedItemTarget;

        private readonly List<TrackerPin> _pins =
            new List<TrackerPin>();

        private const int MaxPins = 12;

        public UnifiedTrackerFeature(
            ConfigFile config,
            GK2UIService uiService,
            GK2SaveService saveService)
        {
            _config = config ??
                throw new ArgumentNullException(nameof(config));

            _uiService = uiService ??
                throw new ArgumentNullException(nameof(uiService));

            _saveService = saveService ??
                throw new ArgumentNullException(nameof(saveService));
        }

        public override string Id => "unified-tracker";

        public override string Name => "Unified Tracker";

        public override string Category => "Tracker";

        public override string Description =>
            "Pin active quests, known crafts, and custom item targets to a compact live HUD.";

        protected override bool DefaultEnabled => true;

        protected override void OnInitialize()
        {
            _activeInstance = this;

            _pinsRaw = _config.Bind(
                Category,
                $"{Id}.Pins",
                string.Empty,
                "Serialized GK2+ tracker pins. Manage this from the Tracker tab.");

            _hudVisible = _config.Bind(
                Category,
                $"{Id}.HudVisible",
                true,
                "Show the compact tracker HUD while a save is loaded.");

            _autoTrackNewQuests = _config.Bind(
                Category,
                $"{Id}.AutoTrackNewQuests",
                false,
                "Automatically add a quest to the tracker when GK2 starts it.");

            _removeCompletedQuests = _config.Bind(
                Category,
                $"{Id}.RemoveCompletedQuests",
                true,
                "Automatically remove quest pins when GK2 completes the quest.");

            _selectedQuestId = _config.Bind(
                Category,
                $"{Id}.SelectedQuest",
                string.Empty,
                "Current quest selection used by the Tracker tab.");

            _selectedCraftId = _config.Bind(
                Category,
                $"{Id}.SelectedCraft",
                string.Empty,
                "Current craft selection used by the Tracker tab.");

            _selectedItemId = _config.Bind(
                Category,
                $"{Id}.SelectedItem",
                string.Empty,
                "Current item selection used by the Tracker tab.");

            _selectedItemTarget = _config.Bind(
                Category,
                $"{Id}.SelectedItemTarget",
                100,
                new ConfigDescription(
                    "Target quantity for a custom item tracker pin.",
                    new AcceptableValueRange<int>(1, 10000)));

            LoadPins();

            _uiService.RegisterFeatureToggleControl(
                new GK2FeatureToggleControl(
                    Id,
                    Category,
                    Name,
                    () => Enabled?.Value ?? DefaultEnabled,
                    BuildFeatureStatus,
                    SetEnabledFromMainMenu,
                    order: 10));

            _uiService.RegisterFeatureOptionControl(
                new GK2FeatureOptionControl(
                    $"{Id}.auto-track-quests",
                    Category,
                    "Auto-track New Quests",
                    () => _autoTrackNewQuests.Value ? "On" : "Off",
                    GetBinaryOptions,
                    value => SetBooleanOption(
                        _autoTrackNewQuests,
                        value),
                    parentFeatureId: Id,
                    order: 12,
                    enabledProvider: () => Enabled?.Value == true,
                    allowInGameEditing: true));

            _uiService.RegisterFeatureOptionControl(
                new GK2FeatureOptionControl(
                    $"{Id}.remove-completed-quests",
                    Category,
                    "Remove Completed Quests",
                    () => _removeCompletedQuests.Value ? "On" : "Off",
                    GetBinaryOptions,
                    value => SetBooleanOption(
                        _removeCompletedQuests,
                        value),
                    parentFeatureId: Id,
                    order: 14,
                    enabledProvider: () => Enabled?.Value == true,
                    allowInGameEditing: true));

            _uiService.RegisterFeatureOptionControl(
                new GK2FeatureOptionControl(
                    $"{Id}.quest",
                    Category,
                    "Quest",
                    () => _selectedQuestId.Value,
                    GetQuestOptions,
                    value => SetSelection(
                        _selectedQuestId,
                        value),
                    parentFeatureId: Id,
                    order: 20,
                    enabledProvider: CanManageTracker,
                    allowInGameEditing: true,
                    searchable: true));

            _uiService.RegisterFeatureOptionControl(
                new GK2FeatureOptionControl(
                    $"{Id}.craft",
                    Category,
                    "Craft / Recipe",
                    () => _selectedCraftId.Value,
                    GetCraftOptions,
                    value => SetSelection(
                        _selectedCraftId,
                        value),
                    parentFeatureId: Id,
                    order: 30,
                    enabledProvider: CanManageTracker,
                    allowInGameEditing: true,
                    searchable: true));

            _uiService.RegisterFeatureOptionControl(
                new GK2FeatureOptionControl(
                    $"{Id}.item",
                    Category,
                    "Custom Item",
                    () => _selectedItemId.Value,
                    GetItemOptions,
                    value => SetSelection(
                        _selectedItemId,
                        value),
                    parentFeatureId: Id,
                    order: 40,
                    enabledProvider: CanManageTracker,
                    allowInGameEditing: true,
                    searchable: true));

            _uiService.RegisterFeatureOptionControl(
                new GK2FeatureOptionControl(
                    $"{Id}.item-target",
                    Category,
                    "Item Target",
                    () => _selectedItemTarget.Value.ToString(),
                    GetTargetOptions,
                    SetItemTarget,
                    parentFeatureId: Id,
                    order: 50,
                    enabledProvider: CanManageTracker,
                    allowInGameEditing: true));

            RegisterAction(
                "tracker.pin-quest",
                "Pin Quest",
                PinSelectedQuest,
                () => CanManageTracker() &&
                      !string.IsNullOrWhiteSpace(
                          _selectedQuestId.Value));

            RegisterAction(
                "tracker.pin-craft",
                "Pin Craft",
                PinSelectedCraft,
                () => CanManageTracker() &&
                      !string.IsNullOrWhiteSpace(
                          _selectedCraftId.Value));

            RegisterAction(
                "tracker.pin-item",
                "Pin Item",
                PinSelectedItem,
                () => CanManageTracker() &&
                      !string.IsNullOrWhiteSpace(
                          _selectedItemId.Value));

            RegisterAction(
                "tracker.unpin-selected",
                "Unpin Selected",
                UnpinSelected,
                CanManageTracker);

            RegisterAction(
                "tracker.toggle-hud",
                "Toggle HUD",
                ToggleHud,
                () => Enabled?.Value == true);

            RegisterAction(
                "tracker.clear",
                "Clear Pins",
                ClearPins,
                () => CanManageTracker() &&
                      _pins.Count > 0);

            _uiService.RegisterTabNotice(
                Category,
                BuildTrackerNotice);

            _uiService.RegisterTrackerHud(
                ShouldShowHud,
                BuildHudText);

            PatchNativeTrackerHooks();
        }

        private static IReadOnlyList<GK2FeatureOption>
            GetBinaryOptions()
        {
            return new[]
            {
                new GK2FeatureOption("Off", "Off"),
                new GK2FeatureOption("On", "On")
            };
        }

        private void SetBooleanOption(
            ConfigEntry<bool> entry,
            string value)
        {
            if (entry == null)
            {
                return;
            }

            entry.Value =
                string.Equals(
                    value,
                    "On",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    value,
                    "True",
                    StringComparison.OrdinalIgnoreCase);

            _config.Save();
            _uiService.RefreshMenu();
        }

        private void PatchNativeTrackerHooks()
        {
            PatchPostfix(
                typeof(global::QuestTreeElementWidget),
                "Redraw",
                nameof(QuestTreeRedrawPostfix));

            PatchPostfix(
                typeof(global::UICraftWidget),
                "Redraw",
                nameof(CraftWidgetRedrawPostfix));

            PatchPostfix(
                typeof(global::UIBuildingWidget),
                "Redraw",
                nameof(BuildingWidgetRedrawPostfix));

            PatchPostfix(
                typeof(global::UITownBuildingWidget),
                "Redraw",
                nameof(TownBuildingWidgetRedrawPostfix));

            PatchPostfix(
                typeof(global::QuestSystemData),
                "StartQuest",
                nameof(QuestStartedPostfix));

            PatchPostfix(
                typeof(global::QuestSystemData),
                "CompleteQuest",
                nameof(QuestCompletedPostfix));
        }

        private void PatchPostfix(
            Type type,
            string methodName,
            string postfixName)
        {
            MethodInfo original =
                AccessTools.Method(
                    type,
                    methodName);

            MethodInfo postfix =
                AccessTools.Method(
                    typeof(UnifiedTrackerFeature),
                    postfixName);

            if (original == null ||
                postfix == null)
            {
                Logger.LogWarning(
                    $"Unified Tracker could not hook {type.Name}.{methodName}.");
                return;
            }

            Harmony.Patch(
                original,
                postfix:
                    new HarmonyMethod(
                        postfix));
        }

        private static void QuestTreeRedrawPostfix(
            global::QuestTreeElementWidget __instance)
        {
            global::QuestData quest =
                __instance?.Data?.questData;

            if (__instance == null ||
                quest == null)
            {
                return;
            }

            BindRightClick(
                __instance.gameObject,
                () => _activeInstance?
                    .ToggleQuestFromNative(
                        quest));
        }

        private static void CraftWidgetRedrawPostfix(
            global::UICraftWidget __instance)
        {
            global::CraftDef craft =
                __instance?.CraftDef;

            if (__instance == null ||
                craft == null)
            {
                return;
            }

            BindRightClick(
                __instance.gameObject,
                () => _activeInstance?
                    .ToggleCraftFromNative(
                        craft));
        }

        private static void BuildingWidgetRedrawPostfix(
            global::UIBuildingWidget __instance)
        {
            if (__instance == null)
            {
                return;
            }

            global::UIBuildingWidgetData data =
                Traverse.Create(
                    __instance)
                    .Field("data")
                    .GetValue<global::UIBuildingWidgetData>();

            global::BuildData build =
                data?.BuildData;

            if (build == null ||
                build.Definition == null)
            {
                return;
            }

            BindRightClick(
                __instance.gameObject,
                () => _activeInstance?
                    .TogglePlanFromNative(
                        "build:" +
                            build.Definition.id,
                        global::LLBase.L(
                            data.Name),
                        data.GetCurrentNeedItems()));
        }

        private static void TownBuildingWidgetRedrawPostfix(
            global::UITownBuildingWidget __instance)
        {
            if (__instance == null)
            {
                return;
            }

            global::UITownBuildingWidgetData data =
                Traverse.Create(
                    __instance)
                    .Field("data")
                    .GetValue<global::UITownBuildingWidgetData>();

            global::TownBuildingDef definition =
                data?.TownBuildingDef;

            if (definition == null)
            {
                return;
            }

            BindRightClick(
                __instance.gameObject,
                () => _activeInstance?
                    .TogglePlanFromNative(
                        "town:" +
                            definition.id,
                        data.Name,
                        data.GetCurrentNeedItems()));
        }

        private static void QuestStartedPostfix(
            global::QuestSystemData __instance,
            string id)
        {
            UnifiedTrackerFeature feature =
                _activeInstance;

            if (feature?.Enabled?.Value != true ||
                feature._autoTrackNewQuests?.Value != true ||
                __instance?
                    .questCollection?
                    .questsCache == null ||
                string.IsNullOrWhiteSpace(id) ||
                !__instance.questCollection.questsCache
                    .TryGetValue(
                        id,
                        out global::QuestData quest) ||
                quest == null ||
                !quest.IsActiveQuest)
            {
                return;
            }

            feature.AddOrUpdatePin(
                TrackerPinType.Quest,
                quest.id,
                0);

            feature.Logger.LogInfo(
                $"Unified Tracker auto-tracked quest '{quest.id}'.");
        }

        private static void QuestCompletedPostfix(
            string id)
        {
            UnifiedTrackerFeature feature =
                _activeInstance;

            if (feature?.Enabled?.Value != true ||
                feature._removeCompletedQuests?.Value != true ||
                string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            int removed =
                feature._pins.RemoveAll(pin =>
                    pin.Type ==
                        TrackerPinType.Quest &&
                    string.Equals(
                        pin.Id,
                        id,
                        StringComparison.OrdinalIgnoreCase));

            if (removed > 0)
            {
                feature.SavePins();

                feature.Logger.LogInfo(
                    $"Unified Tracker removed completed quest '{id}'.");
            }
        }

        private static void BindRightClick(
            GameObject target,
            Action toggleAction)
        {
            if (target == null)
            {
                return;
            }

            TrackerRightClickTarget handler =
                target.GetComponent<TrackerRightClickTarget>();

            if (handler == null)
            {
                handler =
                    target.AddComponent<TrackerRightClickTarget>();
            }

            handler.Bind(
                toggleAction);
        }

        private void ToggleQuestFromNative(
            global::QuestData quest)
        {
            if (!CanManageTracker() ||
                quest == null ||
                string.IsNullOrWhiteSpace(quest.id))
            {
                return;
            }

            TrackerPin existing =
                _pins.FirstOrDefault(pin =>
                    pin.Type ==
                        TrackerPinType.Quest &&
                    string.Equals(
                        pin.Id,
                        quest.id,
                        StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                _pins.Remove(existing);
                SavePins();

                Logger.LogInfo(
                    $"Unified Tracker untracked quest '{quest.id}' from the native quest UI.");
                return;
            }

            if (!quest.IsActiveQuest)
            {
                return;
            }

            AddOrUpdatePin(
                TrackerPinType.Quest,
                quest.id,
                0);

            Logger.LogInfo(
                $"Unified Tracker tracked quest '{quest.id}' from the native quest UI.");
        }

        private void ToggleCraftFromNative(
            global::CraftDef craft)
        {
            if (!CanManageTracker() ||
                craft == null ||
                string.IsNullOrWhiteSpace(craft.id))
            {
                return;
            }

            TrackerPin existing =
                _pins.FirstOrDefault(pin =>
                    pin.Type ==
                        TrackerPinType.Craft &&
                    string.Equals(
                        pin.Id,
                        craft.id,
                        StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                _pins.Remove(existing);
                SavePins();

                Logger.LogInfo(
                    $"Unified Tracker untracked craft '{craft.id}' from the native crafting UI.");
                return;
            }

            AddOrUpdatePin(
                TrackerPinType.Craft,
                craft.id,
                0);

            Logger.LogInfo(
                $"Unified Tracker tracked craft '{craft.id}' from the native crafting UI.");
        }

        private void TogglePlanFromNative(
            string id,
            string label,
            IEnumerable<global::NeedItemData> needs)
        {
            if (!CanManageTracker() ||
                string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            TrackerPin existing =
                _pins.FirstOrDefault(pin =>
                    pin.Type ==
                        TrackerPinType.Plan &&
                    string.Equals(
                        pin.Id,
                        id,
                        StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                _pins.Remove(existing);
                SavePins();

                Logger.LogInfo(
                    $"Unified Tracker untracked plan '{id}' from the native build UI.");
                return;
            }

            AddOrUpdatePin(
                TrackerPinType.Plan,
                id,
                0,
                label,
                SerializeNeeds(needs));

            Logger.LogInfo(
                $"Unified Tracker tracked plan '{id}' from the native build UI.");
        }

        private void RegisterAction(
            string id,
            string label,
            Action action,
            Func<bool> canExecute)
        {
            _uiService.RegisterMenuAction(
                new GK2MenuAction(
                    id,
                    Category,
                    label,
                    action,
                    canExecute));
        }

        private bool CanManageTracker()
        {
            return Enabled?.Value == true &&
                   _saveService.HasLoadedSave;
        }

        private string BuildFeatureStatus()
        {
            if (Enabled?.Value != true)
            {
                return "OFF";
            }

            return _hudVisible?.Value == true
                ? $"ON · {_pins.Count}/{MaxPins} pins · HUD ON"
                : $"ON · {_pins.Count}/{MaxPins} pins · HUD OFF";
        }

        private void SetEnabledFromMainMenu(
            bool enabled)
        {
            if (Enabled == null)
            {
                return;
            }

            Enabled.Value = enabled;
            _config.Save();
            _uiService.RefreshMenu();
        }

        private void SetSelection(
            ConfigEntry<string> entry,
            string value)
        {
            if (entry == null)
            {
                return;
            }

            entry.Value =
                (value ?? string.Empty).Trim();

            _config.Save();
            _uiService.RefreshMenu();
        }

        private void SetItemTarget(
            string value)
        {
            if (!int.TryParse(
                    value,
                    out int target))
            {
                return;
            }

            _selectedItemTarget.Value =
                Math.Max(
                    1,
                    Math.Min(
                        10000,
                        target));

            _config.Save();
            _uiService.RefreshMenu();
        }

        private IReadOnlyList<GK2FeatureOption>
            GetQuestOptions()
        {
            List<GK2FeatureOption> options =
                new List<GK2FeatureOption>();

            if (!_saveService.HasLoadedSave)
            {
                return options;
            }

            global::QuestSystemData questSystem =
                global::MainGame.Instance?
                    .GameSave?
                    .questSystemData;

            if (questSystem?.questCollection?.quests == null)
            {
                return options;
            }

            foreach (global::QuestData quest in
                     questSystem.questCollection.quests)
            {
                if (quest == null ||
                    string.IsNullOrWhiteSpace(quest.id) ||
                    !quest.IsActiveQuest)
                {
                    continue;
                }

                options.Add(
                    new GK2FeatureOption(
                        quest.id,
                        LocalizeQuestName(quest)));
            }

            options.Sort(
                (left, right) =>
                    string.Compare(
                        left.Label,
                        right.Label,
                        StringComparison.OrdinalIgnoreCase));

            EnsureSelection(
                _selectedQuestId,
                options);

            return options;
        }

        private IReadOnlyList<GK2FeatureOption>
            GetCraftOptions()
        {
            List<GK2FeatureOption> options =
                new List<GK2FeatureOption>();

            if (!_saveService.HasLoadedSave ||
                global::GameBalance.Me == null)
            {
                return options;
            }

            HashSet<string> unlocked =
                new HashSet<string>(
                    global::MainGame.Instance?
                        .GameSave?
                        .knowledgeSystem?
                        .unlockedCrafts ??
                    new List<string>(),
                    StringComparer.OrdinalIgnoreCase);

            IEnumerable values =
                ResolveBalanceCollection(
                    global::GameBalance.Me,
                    "craftDefs");

            if (values == null)
            {
                return options;
            }

            foreach (object value in values)
            {
                if (!(value is global::CraftDefBase craft) ||
                    string.IsNullOrWhiteSpace(craft.id) ||
                    craft.isHidden)
                {
                    continue;
                }

                if (craft is global::CraftDef craftDef &&
                    craftDef.isNeedsUnlock &&
                    !unlocked.Contains(craft.id))
                {
                    continue;
                }

                options.Add(
                    new GK2FeatureOption(
                        craft.id,
                        GetCraftDisplayName(craft)));
            }

            options = options
                .GroupBy(
                    option => option.Value,
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(
                    option => option.Label,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    option => option.Value,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

            EnsureSelection(
                _selectedCraftId,
                options);

            return options;
        }

        private IReadOnlyList<GK2FeatureOption>
            GetItemOptions()
        {
            List<GK2FeatureOption> options =
                new List<GK2FeatureOption>();

            if (!_saveService.HasLoadedSave ||
                global::GameBalance.Me == null)
            {
                return options;
            }

            IEnumerable values =
                ResolveBalanceCollection(
                    global::GameBalance.Me,
                    "itemDefs");

            if (values == null)
            {
                return options;
            }

            foreach (object value in values)
            {
                if (!(value is global::ItemDef itemDef) ||
                    string.IsNullOrWhiteSpace(itemDef.id) ||
                    string.Equals(
                        itemDef.id,
                        "empty",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                options.Add(
                    new GK2FeatureOption(
                        itemDef.id,
                        GetItemDisplayName(itemDef)));
            }

            options.Sort(
                (left, right) =>
                {
                    int byName =
                        string.Compare(
                            left.Label,
                            right.Label,
                            StringComparison.OrdinalIgnoreCase);

                    return byName != 0
                        ? byName
                        : string.Compare(
                            left.Value,
                            right.Value,
                            StringComparison.OrdinalIgnoreCase);
                });

            EnsureSelection(
                _selectedItemId,
                options);

            return options;
        }

        private static IReadOnlyList<GK2FeatureOption>
            GetTargetOptions()
        {
            return new[]
            {
                new GK2FeatureOption("5", "5"),
                new GK2FeatureOption("10", "10"),
                new GK2FeatureOption("25", "25"),
                new GK2FeatureOption("50", "50"),
                new GK2FeatureOption("100", "100"),
                new GK2FeatureOption("250", "250"),
                new GK2FeatureOption("500", "500"),
                new GK2FeatureOption("1000", "1,000")
            };
        }

        private void EnsureSelection(
            ConfigEntry<string> entry,
            IReadOnlyList<GK2FeatureOption> options)
        {
            if (entry == null ||
                options == null ||
                options.Count == 0)
            {
                return;
            }

            if (options.Any(option =>
                string.Equals(
                    option.Value,
                    entry.Value,
                    StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            entry.Value =
                options[0].Value;

            _config.Save();
        }

        private void PinSelectedQuest()
        {
            AddOrUpdatePin(
                TrackerPinType.Quest,
                _selectedQuestId.Value,
                0);
        }

        private void PinSelectedCraft()
        {
            AddOrUpdatePin(
                TrackerPinType.Craft,
                _selectedCraftId.Value,
                0);
        }

        private void PinSelectedItem()
        {
            AddOrUpdatePin(
                TrackerPinType.Item,
                _selectedItemId.Value,
                _selectedItemTarget.Value);
        }

        private void AddOrUpdatePin(
            TrackerPinType type,
            string id,
            int target)
        {
            id = (id ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            TrackerPin existing =
                _pins.FirstOrDefault(pin =>
                    pin.Type == type &&
                    string.Equals(
                        pin.Id,
                        id,
                        StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.Target = target;
                SavePins();
                return;
            }

            if (_pins.Count >= MaxPins)
            {
                Logger.LogWarning(
                    $"Tracker pin limit reached ({MaxPins}).");
                return;
            }

            _pins.Add(
                new TrackerPin
                {
                    Type = type,
                    Id = id,
                    Target = target
                });

            SavePins();
        }

        private void UnpinSelected()
        {
            string questId =
                _selectedQuestId?.Value ??
                string.Empty;

            string craftId =
                _selectedCraftId?.Value ??
                string.Empty;

            string itemId =
                _selectedItemId?.Value ??
                string.Empty;

            _pins.RemoveAll(pin =>
                (pin.Type == TrackerPinType.Quest &&
                 string.Equals(
                     pin.Id,
                     questId,
                     StringComparison.OrdinalIgnoreCase)) ||
                (pin.Type == TrackerPinType.Craft &&
                 string.Equals(
                     pin.Id,
                     craftId,
                     StringComparison.OrdinalIgnoreCase)) ||
                (pin.Type == TrackerPinType.Item &&
                 string.Equals(
                     pin.Id,
                     itemId,
                     StringComparison.OrdinalIgnoreCase)));

            SavePins();
        }

        private void ClearPins()
        {
            _pins.Clear();
            SavePins();
        }

        private void ToggleHud()
        {
            _hudVisible.Value =
                !_hudVisible.Value;

            _config.Save();
            _uiService.RefreshMenu();
        }

        private void SavePins()
        {
            _pinsRaw.Value =
                string.Join(
                    ";",
                    _pins.Select(
                        pin =>
                            $"{pin.Type}|{Uri.EscapeDataString(pin.Id)}|{pin.Target}"));

            _config.Save();
            _uiService.RefreshMenu();
        }

        private void LoadPins()
        {
            _pins.Clear();

            string raw =
                _pinsRaw?.Value ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(raw))
            {
                return;
            }

            foreach (string record in
                     raw.Split(
                         new[] { ';' },
                         StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts =
                    record.Split('|');

                if (parts.Length < 3 ||
                    !Enum.TryParse(
                        parts[0],
                        true,
                        out TrackerPinType type) ||
                    !int.TryParse(
                        parts[2],
                        out int target))
                {
                    continue;
                }

                string id;

                try
                {
                    id =
                        Uri.UnescapeDataString(
                            parts[1]);
                }
                catch
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                _pins.Add(
                    new TrackerPin
                    {
                        Type = type,
                        Id = id,
                        Target = target
                    });

                if (_pins.Count >= MaxPins)
                {
                    break;
                }
            }
        }

        private string BuildTrackerNotice()
        {
            if (!_saveService.HasLoadedSave)
            {
                return
                    "Load a save to pin active quests, known crafts, or item targets.";
            }

            if (_pins.Count == 0)
            {
                return
                    "No tracker pins yet. Choose a quest, craft, or item below, then use the matching Pin action.";
            }

            return
                $"Pinned {_pins.Count}/{MaxPins}\n" +
                BuildSnapshotText(
                    compact: true);
        }

        private bool ShouldShowHud()
        {
            return Enabled?.Value == true &&
                   _hudVisible?.Value == true &&
                   _saveService.HasLoadedSave &&
                   _pins.Count > 0;
        }

        private string BuildHudText()
        {
            return BuildSnapshotText(
                compact: false);
        }

        private string BuildSnapshotText(
            bool compact)
        {
            if (_pins.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder =
                new StringBuilder();

            for (int i = 0; i < _pins.Count; i++)
            {
                TrackerPin pin =
                    _pins[i];

                if (i > 0)
                {
                    builder.AppendLine();
                }

                switch (pin.Type)
                {
                    case TrackerPinType.Quest:
                        AppendQuest(
                            builder,
                            pin,
                            compact);
                        break;

                    case TrackerPinType.Craft:
                        AppendCraft(
                            builder,
                            pin,
                            compact);
                        break;

                    case TrackerPinType.Item:
                        AppendItem(
                            builder,
                            pin);
                        break;
                }
            }

            return builder
                .ToString()
                .TrimEnd();
        }

        private void AppendQuest(
            StringBuilder builder,
            TrackerPin pin,
            bool compact)
        {
            global::QuestData quest =
                FindQuest(pin.Id);

            if (quest == null)
            {
                builder.Append(
                    $"Quest · {pin.Id} · unavailable");
                return;
            }

            builder.Append(
                $"Quest · {LocalizeQuestName(quest)}");

            if (quest.status == global::QuestStatus.Completed)
            {
                builder.Append(" · COMPLETE");
                return;
            }

            if (!compact &&
                !string.IsNullOrWhiteSpace(
                    quest.Description))
            {
                builder.AppendLine();
                builder.Append(
                    "  " +
                    SingleLine(
                        quest.Description,
                        90));
            }

            if (quest.Definition?
                    .finishCheck?
                    .phraseReqs == null)
            {
                return;
            }

            foreach (global::QuestPhraseRequirement requirement in
                     quest.Definition.finishCheck.phraseReqs)
            {
                if (requirement == null)
                {
                    continue;
                }

                if (requirement.entity ==
                    global::QuestPhraseRequirement.Entity.Item)
                {
                    string itemId =
                        requirement.itemCount.itemId;

                    int target =
                        requirement.itemCount.count;

                    int current =
                        CountPlayerItem(itemId);

                    builder.AppendLine();
                    builder.Append(
                        $"  {GetItemDisplayName(itemId)}  {current}/{target}");
                }
            }
        }

        private void AppendCraft(
            StringBuilder builder,
            TrackerPin pin,
            bool compact)
        {
            global::CraftDefBase craft =
                FindCraft(pin.Id);

            if (craft == null)
            {
                builder.Append(
                    $"Craft · {pin.Id} · unavailable");
                return;
            }

            builder.Append(
                $"Craft · {GetCraftDisplayName(craft)}");

            if (compact ||
                craft.needItems == null)
            {
                return;
            }

            foreach (global::NeedItemData need in
                     craft.needItems)
            {
                if (need == null ||
                    need.IsEmpty)
                {
                    continue;
                }

                int target =
                    Math.Max(
                        1,
                        need.GetCount(null));

                int current =
                    CountNeed(
                        need);

                builder.AppendLine();
                builder.Append(
                    $"  {GetNeedDisplayName(need)}  {current}/{target}");
            }
        }

        private void AppendItem(
            StringBuilder builder,
            TrackerPin pin)
        {
            int target =
                Math.Max(
                    1,
                    pin.Target);

            int current =
                CountPlayerItem(
                    pin.Id);

            builder.Append(
                $"Item · {GetItemDisplayName(pin.Id)}  {current}/{target}");

            if (current >= target)
            {
                builder.Append("  ✓");
            }
        }

        private static int CountNeed(
            global::NeedItemData need)
        {
            if (need == null)
            {
                return 0;
            }

            if (!need.IsGroup)
            {
                return CountPlayerItem(
                    need.Id);
            }

            if (!need.TryGetGroupItemDefs(
                    out List<global::ItemDef> defs) ||
                defs == null)
            {
                return 0;
            }

            int total = 0;

            foreach (global::ItemDef def in defs)
            {
                if (def != null &&
                    !string.IsNullOrWhiteSpace(def.id))
                {
                    total +=
                        CountPlayerItem(
                            def.id);
                }
            }

            return total;
        }

        private static int CountPlayerItem(
            string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return 0;
            }

            global::Inventory inventory =
                global::MainGame.PlayerData?
                    .Inventory;

            return inventory?.Data?
                       .GetTotalCountInInventory(
                           itemId) ??
                   0;
        }

        private static global::QuestData FindQuest(
            string id)
        {
            global::QuestSystemData questSystem =
                global::MainGame.Instance?
                    .GameSave?
                    .questSystemData;

            if (questSystem?
                    .questCollection?
                    .questsCache != null &&
                questSystem.questCollection
                    .questsCache
                    .TryGetValue(
                        id,
                        out global::QuestData quest))
            {
                return quest;
            }

            return null;
        }

        private static global::CraftDefBase FindCraft(
            string id)
        {
            if (string.IsNullOrWhiteSpace(id) ||
                global::GameBalance.Me == null)
            {
                return null;
            }

            IEnumerable values =
                ResolveBalanceCollection(
                    global::GameBalance.Me,
                    "craftDefs");

            if (values == null)
            {
                return null;
            }

            foreach (object value in values)
            {
                if (value is global::CraftDefBase craft &&
                    string.Equals(
                        craft.id,
                        id,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return craft;
                }
            }

            return null;
        }

        private static string LocalizeQuestName(
            global::QuestData quest)
        {
            if (quest == null ||
                string.IsNullOrWhiteSpace(quest.id))
            {
                return "Unknown Quest";
            }

            try
            {
                string localized =
                    LLBase.L(
                        quest.id);

                if (!string.IsNullOrWhiteSpace(localized))
                {
                    return localized;
                }
            }
            catch
            {
            }

            return quest.id;
        }

        private static string GetCraftDisplayName(
            global::CraftDefBase craft)
        {
            if (craft == null ||
                string.IsNullOrWhiteSpace(craft.id))
            {
                return "Unknown Craft";
            }

            try
            {
                string localized =
                    LLBase.L(
                        craft.id);

                if (!string.IsNullOrWhiteSpace(localized) &&
                    !string.Equals(
                        localized,
                        craft.id,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return localized;
                }
            }
            catch
            {
            }

            try
            {
                global::ItemDef result =
                    (craft as global::CraftDef)?
                        .TryGetResultingItemDef();

                if (result != null)
                {
                    string name =
                        result.GetHeader();

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        return name;
                    }
                }
            }
            catch
            {
            }

            return craft.id;
        }

        private static string GetNeedDisplayName(
            global::NeedItemData need)
        {
            if (need == null)
            {
                return "Unknown";
            }

            if (!need.IsGroup)
            {
                return GetItemDisplayName(
                    need.Id);
            }

            return $"Any {need.Id}";
        }

        private static string GetItemDisplayName(
            string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) ||
                global::GameBalance.Me == null)
            {
                return itemId ?? string.Empty;
            }

            global::ItemDef def =
                global::GameBalance.Me
                    .GetDataOrNull<global::ItemDef>(
                        itemId);

            return GetItemDisplayName(def) ??
                   itemId;
        }

        private static string GetItemDisplayName(
            global::ItemDef itemDef)
        {
            if (itemDef == null)
            {
                return string.Empty;
            }

            try
            {
                string header =
                    itemDef.GetHeader();

                if (!string.IsNullOrWhiteSpace(header))
                {
                    return header;
                }
            }
            catch
            {
            }

            return itemDef.id;
        }

        private static string SingleLine(
            string value,
            int maxLength)
        {
            string text =
                (value ?? string.Empty)
                    .Replace("\r", " ")
                    .Replace("\n", " ")
                    .Trim();

            while (text.Contains("  "))
            {
                text =
                    text.Replace(
                        "  ",
                        " ");
            }

            if (text.Length <= maxLength)
            {
                return text;
            }

            return text.Substring(
                       0,
                       Math.Max(
                           0,
                           maxLength - 1)) +
                   "…";
        }

        private static IEnumerable ResolveBalanceCollection(
            global::GameBalance balance,
            string memberName)
        {
            if (balance == null ||
                string.IsNullOrWhiteSpace(memberName))
            {
                return null;
            }

            Type type =
                balance.GetType();

            FieldInfo field =
                AccessTools.Field(
                    type,
                    memberName);

            if (field?.GetValue(balance) is IEnumerable fieldValues)
            {
                return fieldValues;
            }

            PropertyInfo property =
                AccessTools.Property(
                    type,
                    memberName);

            if (property?.GetValue(balance, null) is IEnumerable propertyValues)
            {
                return propertyValues;
            }

            return null;
        }
    }
}
