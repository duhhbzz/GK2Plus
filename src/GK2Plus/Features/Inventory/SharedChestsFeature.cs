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
    /// Makes the current-zone storage inventories that vanilla already adds to
    /// the chest window's left side selectable, while keeping GK2's native
    /// transfer, capacity, stack, filter, bag, and notification logic intact.
    ///
    /// No custom shared-storage state is created. The physically opened chest
    /// remains vanilla's right-side inventory and is still excluded from the
    /// current-zone MultiInventory by the game's own constructor path.
    /// </summary>
    internal sealed class SharedChestsFeature : FeatureBase
    {
        private static SharedChestsFeature _activeInstance;

        private readonly ConfigFile _config;
        private readonly GK2UIService _uiService;

        private bool _loggedRuntimeSuccess;
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

        public override string Name => "Shared Chests";

        public override string Category => "Inventory";

        public override string Description =>
            "Access other eligible storage in the current world zone from a normal chest window.";

        protected override bool DefaultEnabled => true;

        protected override void OnInitialize()
        {
            _activeInstance = this;

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
                    "Shared Chests could not resolve a UIBaseChestWindowData constructor " +
                    "that receives MultiInventory. The feature will remain inactive.");
            }
            else
            {
                HarmonyMethod postfix =
                    new HarmonyMethod(
                        typeof(SharedChestsFeature),
                        nameof(UIBaseChestWindowDataPostfix));

                foreach (ConstructorInfo constructor in constructors)
                {
                    Harmony.Patch(
                        constructor,
                        postfix: postfix);
                }

                Logger.LogInfo(
                    $"Shared Chests hooked {constructors.Count} native chest-window data constructor(s).");
            }

            _uiService.RegisterFeatureToggleControl(
                new GK2FeatureToggleControl(
                    Id,
                    Category,
                    "Shared Chests",
                    () => Enabled?.Value ?? DefaultEnabled,
                    BuildUiStatus,
                    SetEnabledFromMainMenu));
        }

        protected override void OnEnabled()
        {
            Logger.LogInfo(
                "Shared Chests enabled for current-zone native storage inventories.");
        }

        private string BuildUiStatus()
        {
            return Enabled?.Value == true
                ? "ON"
                : "OFF";
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
                $"Shared Chests {(enabled ? "enabled" : "disabled")} from the main menu.");

            _uiService.RefreshMenu();
        }

        private static void UIBaseChestWindowDataPostfix(
            object __instance,
            object[] __args)
        {
            _activeInstance?.ApplyToChestWindowData(
                __instance,
                __args);
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
                    "Shared Chests could not find inventory widget data under FirstMultiInventoryData.");
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
                    "Shared Chests could not identify the player inventory widget used as the native callback template.");
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
                        playerInventory) ||
                    !ContainsInventoryByReference(
                        worldZoneMultiInventory.inventoryList,
                        inventory))
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
                        "CustomItemsAvailableCondition");

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
                    "Shared Chests found current-zone inventories but could not activate their native widget callbacks/state.");
                return;
            }

            if (!_loggedRuntimeSuccess)
            {
                _loggedRuntimeSuccess = true;

                Logger.LogInfo(
                    $"Shared Chests activated {enabledWidgets} current-zone storage widget(s) " +
                    $"using vanilla transfer callbacks " +
                    $"(delegates={copiedDelegates}, availability={copiedAvailability}, states={updatedStates}).");
            }
            else
            {
                Logger.LogDebug(
                    $"Shared Chests prepared chest UI: currentZoneStorage={enabledWidgets}.");
            }
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
