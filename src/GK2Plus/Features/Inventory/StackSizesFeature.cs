using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using GK2Plus.Core;
using GK2Plus.Framework.UI;
using HarmonyLib;

namespace GK2Plus.Features.Inventory
{
    /// <summary>
    /// Increases GK2's native per-item stack limits by scaling the live
    /// ItemDef.stackCount values after game balance data has loaded.
    ///
    /// Inventory transfer logic remains entirely vanilla. GK2+ changes only the
    /// live definition value that vanilla already consults when merging stacks.
    /// </summary>
    internal sealed class StackSizesFeature : FeatureBase
    {
        private const int DefaultMultiplier = 2;
        private const int MaxLoggedSamples = 8;

        private static StackSizesFeature _activeInstance;

        private readonly ConfigFile _config;
        private readonly GK2UIService _uiService;

        private readonly Dictionary<global::ItemDef, int> _baseStackCounts =
            new Dictionary<global::ItemDef, int>();

        private readonly Dictionary<global::ItemDef, int> _lastAppliedValues =
            new Dictionary<global::ItemDef, int>();

        private ConfigEntry<int> _multiplier;
        private bool _loggedMissingItemDefs;

        internal StackSizesFeature(
            ConfigFile config,
            GK2UIService uiService)
        {
            _config = config ??
                throw new ArgumentNullException(nameof(config));

            _uiService = uiService ??
                throw new ArgumentNullException(nameof(uiService));
        }

        public override string Id => "stack-sizes";

        public override string Name => "Configurable Stack Sizes";

        public override string Category => "Inventory";

        public override string Description =>
            "Increase native stack limits for stackable inventory items.";

        protected override bool DefaultEnabled => true;

        protected override void OnInitialize()
        {
            _multiplier = _config.Bind(
                Category,
                $"{Id}.Multiplier",
                DefaultMultiplier,
                new ConfigDescription(
                    "Multiplier applied to GK2's live native stack limits. " +
                    "Items with a stack limit of 1 remain unchanged.",
                    new AcceptableValueRange<int>(1, 20)
                )
            );

            // Install the lifecycle hook even when the feature starts disabled.
            // That lets the main-menu toggle enable it before a save is loaded
            // without requiring a full game restart.
            _activeInstance = this;

            MethodInfo loadGameBalance = AccessTools.Method(
                typeof(global::GameBalance),
                "LoadGameBalance"
            );

            if (loadGameBalance == null)
            {
                Logger.LogError(
                    "Configurable Stack Sizes could not resolve " +
                    "GameBalance.LoadGameBalance. The feature will not modify stacks.");
            }
            else
            {
                Harmony.Patch(
                    loadGameBalance,
                    postfix: new HarmonyMethod(
                        typeof(StackSizesFeature),
                        nameof(GameBalanceLoadedPostfix)
                    )
                );
            }

            _uiService.RegisterFeatureToggleControl(
                new GK2FeatureToggleControl(
                    Id,
                    Category,
                    "Bigger Item Stacks",
                    () => Enabled?.Value ?? DefaultEnabled,
                    BuildUiStatus,
                    SetEnabledFromMainMenu));

            _uiService.RegisterFeatureOptionControl(
                new GK2FeatureOptionControl(
                    $"{Id}.multiplier",
                    Category,
                    "Stack Size Multiplier",
                    () => (_multiplier?.Value ?? DefaultMultiplier).ToString(),
                    BuildMultiplierOptions,
                    SetMultiplierFromMainMenu));
        }

        protected override void OnEnabled()
        {
            // Normally the game balance loads after BepInEx plugins initialize,
            // but applying here as well makes the feature resilient if that
            // lifecycle order changes in a future build.
            ApplyToCurrentGameBalance("feature initialization");

            Logger.LogInfo(
                $"Configurable Stack Sizes ready at {_multiplier.Value}x live native limits.");
        }

        private static void GameBalanceLoadedPostfix()
        {
            _activeInstance?.ApplyToCurrentGameBalance(
                "GameBalance.LoadGameBalance"
            );
        }

        private string BuildUiStatus()
        {
            return Enabled?.Value == true
                ? $"ON ({_multiplier?.Value ?? DefaultMultiplier}x)"
                : "OFF";
        }

        private IReadOnlyList<GK2FeatureOption> BuildMultiplierOptions()
        {
            List<GK2FeatureOption> options =
                new List<GK2FeatureOption>();

            for (int multiplier = 2; multiplier <= 20; multiplier++)
            {
                options.Add(
                    new GK2FeatureOption(
                        multiplier.ToString(),
                        $"{multiplier}x"));
            }

            return options;
        }

        private void SetMultiplierFromMainMenu(
            string value)
        {
            if (_multiplier == null ||
                !int.TryParse(
                    value,
                    out int multiplier))
            {
                return;
            }

            multiplier =
                Math.Max(
                    2,
                    Math.Min(
                        20,
                        multiplier));

            if (_multiplier.Value == multiplier)
            {
                _uiService.RefreshMenu();
                return;
            }

            bool enabled =
                Enabled?.Value == true;

            if (enabled)
            {
                RestoreCurrentGameBalance(
                    "main-menu multiplier change");
            }

            int previous =
                _multiplier.Value;

            _multiplier.Value =
                multiplier;

            _config.Save();

            if (enabled)
            {
                ApplyToCurrentGameBalance(
                    "main-menu multiplier change");
            }

            Logger.LogInfo(
                $"Stack Size Multiplier changed from {previous}x to {multiplier}x from the main menu.");

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

            Enabled.Value = enabled;
            _config.Save();

            if (enabled)
            {
                ApplyToCurrentGameBalance(
                    "main-menu enable");
            }
            else
            {
                RestoreCurrentGameBalance(
                    "main-menu disable");
            }

            Logger.LogInfo(
                $"Bigger Item Stacks {(enabled ? "enabled" : "disabled")} from the main menu.");

            _uiService.RefreshMenu();
        }

        private void ApplyToCurrentGameBalance(string source)
        {
            if (Enabled == null ||
                !Enabled.Value)
            {
                Logger.LogDebug(
                    $"Stack Sizes skipped after {source} because the feature is disabled.");
                return;
            }

            global::GameBalance balance = global::GameBalance.Me;

            if (balance == null)
            {
                Logger.LogDebug(
                    $"Stack Sizes: game balance is not available during {source}; " +
                    "waiting for LoadGameBalance.");
                return;
            }

            IEnumerable itemDefs = ResolveItemDefs(balance);

            if (itemDefs == null)
            {
                if (!_loggedMissingItemDefs)
                {
                    _loggedMissingItemDefs = true;
                    Logger.LogError(
                        "Stack Sizes could not resolve GameBalance.itemDefs. " +
                        "No stack limits were changed.");
                }

                return;
            }

            int changed = 0;
            int unchanged = 0;
            int nonStackable = 0;
            List<string> samples = new List<string>();

            foreach (object value in itemDefs)
            {
                if (!(value is global::ItemDef itemDef))
                {
                    continue;
                }

                int current = itemDef.stackCount;

                if (current <= 1)
                {
                    nonStackable++;
                    continue;
                }

                if (_multiplier.Value <= 1)
                {
                    unchanged++;
                    continue;
                }

                bool hasLastApplied =
                    _lastAppliedValues.TryGetValue(
                        itemDef,
                        out int lastApplied);

                if (hasLastApplied &&
                    current == lastApplied)
                {
                    unchanged++;
                    continue;
                }

                // Capture the currently-live value as our reversible baseline.
                // If another mod changes the value after our prior application,
                // treat that new live value as the next baseline rather than
                // overwriting it with a hard-coded vanilla assumption.
                if (!_baseStackCounts.TryGetValue(
                        itemDef,
                        out int liveBase) ||
                    current != liveBase)
                {
                    liveBase = current;
                    _baseStackCounts[itemDef] = liveBase;
                }

                long scaledLong =
                    (long)liveBase * _multiplier.Value;

                int scaled = scaledLong > int.MaxValue
                    ? int.MaxValue
                    : (int)scaledLong;

                itemDef.stackCount = scaled;
                _lastAppliedValues[itemDef] = scaled;
                changed++;

                if (samples.Count < MaxLoggedSamples)
                {
                    samples.Add(
                        $"{itemDef.id}: {liveBase}->{scaled}"
                    );
                }
            }

            string sampleText = samples.Count > 0
                ? $" Samples: {string.Join(", ", samples)}."
                : string.Empty;

            Logger.LogInfo(
                $"Stack Sizes applied after {source}: " +
                $"changed={changed}, unchanged={unchanged}, " +
                $"nonStackable={nonStackable}, multiplier={_multiplier.Value}x." +
                sampleText
            );
        }

        private void RestoreCurrentGameBalance(
            string source)
        {
            global::GameBalance balance =
                global::GameBalance.Me;

            if (balance == null)
            {
                Logger.LogDebug(
                    $"Stack Sizes restore skipped during {source}; game balance is unavailable.");
                return;
            }

            IEnumerable itemDefs =
                ResolveItemDefs(balance);

            if (itemDefs == null)
            {
                Logger.LogWarning(
                    $"Stack Sizes restore skipped during {source}; item definitions are unavailable.");
                return;
            }

            int restored = 0;
            int unchanged = 0;
            int externalChanges = 0;

            foreach (object value in itemDefs)
            {
                if (!(value is global::ItemDef itemDef) ||
                    !_baseStackCounts.TryGetValue(
                        itemDef,
                        out int baseline))
                {
                    continue;
                }

                int current =
                    itemDef.stackCount;

                if (_lastAppliedValues.TryGetValue(
                        itemDef,
                        out int lastApplied) &&
                    current == lastApplied)
                {
                    itemDef.stackCount =
                        baseline;

                    restored++;
                    continue;
                }

                if (current == baseline)
                {
                    unchanged++;
                }
                else
                {
                    // Something else changed this definition after GK2+ did.
                    // Do not stomp that live value while turning our feature off.
                    externalChanges++;
                }
            }

            _lastAppliedValues.Clear();

            Logger.LogInfo(
                $"Stack Sizes restored after {source}: " +
                $"restored={restored}, unchanged={unchanged}, " +
                $"externalChangesPreserved={externalChanges}.");
        }

        private static IEnumerable ResolveItemDefs(
            global::GameBalance balance)
        {
            Type type = balance.GetType();

            FieldInfo field = AccessTools.Field(
                type,
                "itemDefs"
            );

            if (field?.GetValue(balance) is IEnumerable fieldValues)
            {
                return fieldValues;
            }

            PropertyInfo property = AccessTools.Property(
                type,
                "itemDefs"
            );

            if (property?.GetValue(balance, null) is IEnumerable propertyValues)
            {
                return propertyValues;
            }

            return null;
        }
    }
}
