using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using GK2Plus.Core;
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
        private readonly Dictionary<global::ItemDef, int> _lastAppliedValues =
            new Dictionary<global::ItemDef, int>();

        private ConfigEntry<int> _multiplier;
        private bool _loggedMissingItemDefs;

        internal StackSizesFeature(ConfigFile config)
        {
            _config = config ??
                throw new ArgumentNullException(nameof(config));
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
        }

        protected override void OnEnabled()
        {
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
                return;
            }

            Harmony.Patch(
                loadGameBalance,
                postfix: new HarmonyMethod(
                    typeof(StackSizesFeature),
                    nameof(GameBalanceLoadedPostfix)
                )
            );

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

        private void ApplyToCurrentGameBalance(string source)
        {
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

                if (_lastAppliedValues.TryGetValue(
                        itemDef,
                        out int lastApplied) &&
                    current == lastApplied)
                {
                    unchanged++;
                    continue;
                }

                // Use the value that is live right now rather than a GK2+
                // hard-coded baseline. This preserves changes made by the game
                // or another mod before this patch runs.
                int liveBase = current;
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
