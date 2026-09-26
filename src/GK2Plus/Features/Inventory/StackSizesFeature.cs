using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using GK2Plus.Core;
using HarmonyLib;

namespace GK2Plus.Features.Inventory
{
    /// <summary>
    /// Increases GK2's native per-item stack limits by scaling ItemDef.stackCount.
    ///
    /// The feature patches the native item-capacity paths and lazily scales each
    /// ItemDef the first time GK2 uses it. Non-stackable definitions (stackCount
    /// <= 1) are intentionally left unchanged.
    /// </summary>
    internal sealed class StackSizesFeature : FeatureBase
    {
        private const int DefaultMultiplier = 2;

        private static StackSizesFeature _activeInstance;

        private readonly ConfigFile _config;
        private readonly Dictionary<global::ItemDef, int> _originalStackCounts =
            new Dictionary<global::ItemDef, int>();

        private ConfigEntry<int> _multiplier;

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
                    "Multiplier applied to GK2's native stack limits. " +
                    "Items with a native stack limit of 1 are left unchanged.",
                    new AcceptableValueRange<int>(1, 20)
                )
            );
        }

        protected override void OnEnabled()
        {
            _activeInstance = this;

            PatchItemMethod(
                "CanAddItemCount",
                new[]
                {
                    typeof(global::Item),
                    typeof(int)
                }
            );

            PatchItemMethod(
                "CanAddItemCount",
                new[]
                {
                    typeof(global::Item)
                }
            );

            PatchItemMethod(
                "CanAddItemCountToInventory",
                new[]
                {
                    typeof(global::Item),
                    typeof(int),
                    typeof(bool),
                    typeof(global::Item),
                    typeof(bool)
                }
            );

            PatchItemMethod(
                "CanAddItemCountToInventory",
                new[]
                {
                    typeof(global::ItemDef),
                    typeof(int),
                    typeof(bool),
                    typeof(global::Item),
                    typeof(bool)
                }
            );

            PatchItemMethod(
                "CanAddItemCountToInventory",
                new[]
                {
                    typeof(global::Item),
                    typeof(bool),
                    typeof(global::Item),
                    typeof(bool)
                }
            );

            MethodInfo addItemMethod = AccessTools.Method(
                typeof(global::Inventory),
                "AddItemToInventory",
                new[]
                {
                    typeof(global::Item),
                    typeof(global::Item),
                    typeof(bool)
                }
            );

            if (addItemMethod != null)
            {
                Harmony.Patch(
                    addItemMethod,
                    prefix: new HarmonyMethod(
                        typeof(StackSizesFeature),
                        nameof(InventoryAddPrefix)
                    )
                );
            }
            else
            {
                Logger.LogWarning(
                    "Stack Sizes could not resolve " +
                    "Inventory.AddItemToInventory(Item, Item, bool).");
            }

            Logger.LogInfo(
                $"Configurable Stack Sizes enabled at {_multiplier.Value}x native limits. " +
                "Native stackCount=1 items remain unchanged.");
        }

        private void PatchItemMethod(
            string methodName,
            Type[] parameterTypes)
        {
            MethodInfo method = AccessTools.Method(
                typeof(global::Item),
                methodName,
                parameterTypes
            );

            if (method == null)
            {
                Logger.LogWarning(
                    $"Stack Sizes could not resolve Item.{methodName}(" +
                    $"{FormatParameterTypes(parameterTypes)}).");
                return;
            }

            Harmony.Patch(
                method,
                prefix: new HarmonyMethod(
                    typeof(StackSizesFeature),
                    nameof(ItemStackOperationPrefix)
                )
            );
        }

        private static void InventoryAddPrefix(object[] __args)
        {
            StackSizesFeature feature = _activeInstance;

            if (feature == null ||
                __args == null)
            {
                return;
            }

            feature.ScaleArguments(__args);
        }

        private static void ItemStackOperationPrefix(
            global::Item __instance,
            object[] __args)
        {
            StackSizesFeature feature = _activeInstance;

            if (feature == null)
            {
                return;
            }

            feature.EnsureScaled(__instance?.Definition);

            if (__args != null)
            {
                feature.ScaleArguments(__args);
            }
        }

        private void ScaleArguments(object[] args)
        {
            foreach (object arg in args)
            {
                if (arg is global::Item item)
                {
                    EnsureScaled(item.Definition);
                }
                else if (arg is global::ItemDef itemDef)
                {
                    EnsureScaled(itemDef);
                }
            }
        }

        private void EnsureScaled(global::ItemDef itemDef)
        {
            if (itemDef == null ||
                _originalStackCounts.ContainsKey(itemDef))
            {
                return;
            }

            int original = itemDef.stackCount;
            _originalStackCounts[itemDef] = original;

            // stackCount <= 1 represents items that should not gain normal
            // inventory stacking behavior (tools, equipment, overhead items, etc.).
            if (original <= 1 ||
                _multiplier.Value <= 1)
            {
                return;
            }

            long scaledLong =
                (long)original * _multiplier.Value;

            int scaled = scaledLong > int.MaxValue
                ? int.MaxValue
                : (int)scaledLong;

            itemDef.stackCount = scaled;

            Logger.LogInfo(
                $"Stack Sizes: '{itemDef.id}' native limit {original} -> {scaled} " +
                $"({_multiplier.Value}x).");
        }

        private static string FormatParameterTypes(Type[] parameterTypes)
        {
            if (parameterTypes == null ||
                parameterTypes.Length == 0)
            {
                return string.Empty;
            }

            string[] names =
                new string[parameterTypes.Length];

            for (int i = 0;
                 i < parameterTypes.Length;
                 i++)
            {
                names[i] =
                    parameterTypes[i].Name;
            }

            return string.Join(", ", names);
        }
    }
}
