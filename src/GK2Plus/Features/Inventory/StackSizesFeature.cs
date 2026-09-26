using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using GK2Plus.Core;
using HarmonyLib;

namespace GK2Plus.Features.Inventory
{
    /// <summary>
    /// Recon-first implementation for configurable item stack sizes.
    ///
    /// The initial slice intentionally does not mutate inventory state. It probes
    /// GK2's native Item/ItemDef/Inventory stack-related surface and samples real
    /// item values as they enter an inventory so the final feature can patch the
    /// game's own stack boundary instead of reimplementing inventory merging.
    /// </summary>
    internal sealed class StackSizesFeature : FeatureBase
    {
        private const int DefaultMultiplier = 2;
        private const int MaxInspectedItems = 8;

        private static readonly string[] InterestingTerms =
        {
            "stack",
            "count",
            "max",
            "limit",
            "capacity"
        };

        private static StackSizesFeature _activeInstance;

        private readonly ConfigFile _config;
        private readonly HashSet<string> _inspectedItemIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private ConfigEntry<int> _multiplier;
        private int _inspectedItemCount;

        internal StackSizesFeature(ConfigFile config)
        {
            _config = config ??
                throw new ArgumentNullException(nameof(config));
        }

        public override string Id => "stack-sizes";

        public override string Name => "Configurable Stack Sizes";

        public override string Category => "Inventory";

        public override string Description =>
            "Increase native item stack limits while preserving GK2 item and inventory semantics.";

        protected override bool DefaultEnabled => true;

        protected override void OnInitialize()
        {
            _multiplier = _config.Bind(
                Category,
                $"{Id}.Multiplier",
                DefaultMultiplier,
                new ConfigDescription(
                    "Multiplier to apply to native stack limits once the native stack boundary is validated.",
                    new AcceptableValueRange<int>(1, 20)
                )
            );
        }

        protected override void OnEnabled()
        {
            _activeInstance = this;

            LogTypeSurface(typeof(global::Item));
            LogTypeSurface(typeof(global::ItemDef));
            LogTypeSurface(typeof(global::Inventory));
            LogTypeSurface(typeof(global::MultiInventory));

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

            if (addItemMethod == null)
            {
                Logger.LogWarning(
                    "Stack Sizes recon could not resolve Inventory.AddItemToInventory(Item, Item, bool).");
                return;
            }

            Harmony.Patch(
                addItemMethod,
                prefix: new HarmonyMethod(
                    typeof(StackSizesFeature),
                    nameof(InventoryAddPrefix)
                )
            );

            Logger.LogInfo(
                $"Stack Sizes recon enabled. Configured multiplier: {_multiplier.Value}x. " +
                "No stack limits are being changed yet.");
        }

        private static void InventoryAddPrefix(object[] __args)
        {
            StackSizesFeature feature = _activeInstance;

            if (feature == null ||
                __args == null ||
                __args.Length == 0 ||
                __args[0] == null)
            {
                return;
            }

            feature.InspectRuntimeItem(__args[0]);
        }

        private void InspectRuntimeItem(object item)
        {
            if (_inspectedItemCount >= MaxInspectedItems)
            {
                return;
            }

            string itemId =
                TryReadNamedValue(item, "id")?.ToString() ??
                TryReadNamedValue(item, "Id")?.ToString() ??
                "<unknown>";

            if (!_inspectedItemIds.Add(itemId))
            {
                return;
            }

            _inspectedItemCount++;

            Logger.LogInfo(
                $"[StackSizes Recon] Sampling item '{itemId}' ({item.GetType().FullName}).");

            LogInterestingInstanceValues(item);

            object definition =
                TryReadNamedValue(item, "Definition") ??
                TryReadNamedValue(item, "Def");

            if (definition != null)
            {
                Logger.LogInfo(
                    $"[StackSizes Recon] Sampling definition for '{itemId}' " +
                    $"({definition.GetType().FullName}).");

                LogInterestingInstanceValues(definition);
            }
            else
            {
                Logger.LogDebug(
                    $"[StackSizes Recon] No definition object resolved for '{itemId}'.");
            }
        }

        private void LogTypeSurface(Type type)
        {
            Logger.LogInfo(
                $"[StackSizes Recon] Candidate members on {type.FullName}:");

            bool foundAny = false;

            foreach (Type current in EnumerateTypeHierarchy(type))
            {
                BindingFlags flags =
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.DeclaredOnly;

                foreach (FieldInfo field in current.GetFields(flags))
                {
                    if (!IsInteresting(field.Name))
                    {
                        continue;
                    }

                    foundAny = true;
                    Logger.LogInfo(
                        $"[StackSizes Recon] FIELD {current.Name}.{field.Name} : " +
                        $"{field.FieldType.FullName}");
                }

                foreach (PropertyInfo property in current.GetProperties(flags))
                {
                    if (!IsInteresting(property.Name))
                    {
                        continue;
                    }

                    foundAny = true;
                    Logger.LogInfo(
                        $"[StackSizes Recon] PROPERTY {current.Name}.{property.Name} : " +
                        $"{property.PropertyType.FullName} " +
                        $"(get={property.CanRead}, set={property.CanWrite})");
                }

                foreach (MethodInfo method in current.GetMethods(flags))
                {
                    if (!IsInteresting(method.Name))
                    {
                        continue;
                    }

                    foundAny = true;
                    Logger.LogInfo(
                        $"[StackSizes Recon] METHOD {current.Name}.{FormatMethod(method)}");
                }
            }

            if (!foundAny)
            {
                Logger.LogInfo(
                    $"[StackSizes Recon] No name-matched candidates found on {type.FullName}.");
            }
        }

        private void LogInterestingInstanceValues(object instance)
        {
            Type type = instance.GetType();

            foreach (Type current in EnumerateTypeHierarchy(type))
            {
                BindingFlags flags =
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly;

                foreach (FieldInfo field in current.GetFields(flags))
                {
                    if (!IsInteresting(field.Name))
                    {
                        continue;
                    }

                    try
                    {
                        object value = field.GetValue(instance);
                        Logger.LogInfo(
                            $"[StackSizes Recon] VALUE {current.Name}.{field.Name} = " +
                            $"{FormatValue(value)}");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug(
                            $"[StackSizes Recon] Could not read {current.Name}.{field.Name}: " +
                            $"{ex.GetType().Name}");
                    }
                }

                foreach (PropertyInfo property in current.GetProperties(flags))
                {
                    if (!IsInteresting(property.Name) ||
                        !property.CanRead ||
                        property.GetIndexParameters().Length != 0)
                    {
                        continue;
                    }

                    try
                    {
                        object value = property.GetValue(instance, null);
                        Logger.LogInfo(
                            $"[StackSizes Recon] VALUE {current.Name}.{property.Name} = " +
                            $"{FormatValue(value)}");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug(
                            $"[StackSizes Recon] Could not read {current.Name}.{property.Name}: " +
                            $"{ex.GetType().Name}");
                    }
                }
            }
        }

        private static object TryReadNamedValue(object instance, string memberName)
        {
            if (instance == null)
            {
                return null;
            }

            for (Type current = instance.GetType();
                 current != null;
                 current = current.BaseType)
            {
                BindingFlags flags =
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly;

                FieldInfo field = current.GetField(memberName, flags);

                if (field != null)
                {
                    try
                    {
                        return field.GetValue(instance);
                    }
                    catch
                    {
                    }
                }

                PropertyInfo property = current.GetProperty(memberName, flags);

                if (property != null &&
                    property.CanRead &&
                    property.GetIndexParameters().Length == 0)
                {
                    try
                    {
                        return property.GetValue(instance, null);
                    }
                    catch
                    {
                    }
                }
            }

            return null;
        }

        private static IEnumerable<Type> EnumerateTypeHierarchy(Type type)
        {
            for (Type current = type;
                 current != null && current != typeof(object);
                 current = current.BaseType)
            {
                yield return current;
            }
        }

        private static bool IsInteresting(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            foreach (string term in InterestingTerms)
            {
                if (name.IndexOf(
                    term,
                    StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static string FormatMethod(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            string[] parameterText = new string[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                parameterText[i] =
                    $"{parameters[i].ParameterType.Name} {parameters[i].Name}";
            }

            return
                $"{method.ReturnType.Name} {method.Name}(" +
                string.Join(", ", parameterText) +
                ")";
        }

        private static string FormatValue(object value)
        {
            return value == null
                ? "<null>"
                : value.ToString();
        }
    }
}
