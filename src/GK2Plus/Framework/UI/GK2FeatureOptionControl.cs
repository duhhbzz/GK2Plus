using System;
using System.Collections.Generic;

namespace GK2Plus.Framework.UI
{
    internal sealed class GK2FeatureOption
    {
        public GK2FeatureOption(
            string value,
            string label)
        {
            Value = value ?? string.Empty;
            Label = string.IsNullOrWhiteSpace(label)
                ? Value
                : label;
        }

        public string Value { get; }

        public string Label { get; }
    }

    /// <summary>
    /// Describes a main-menu-only selectable feature setting. These controls
    /// are intentionally hidden while a save is loaded; the owning feature can
    /// fold the selected value into its read-only in-game status instead.
    /// </summary>
    internal sealed class GK2FeatureOptionControl
    {
        public GK2FeatureOptionControl(
            string id,
            string tab,
            string label,
            Func<string> valueProvider,
            Func<IReadOnlyList<GK2FeatureOption>> optionsProvider,
            Action<string> valueChanged,
            string parentFeatureId = null,
            int order = 0,
            Func<bool> enabledProvider = null)
        {
            Id = string.IsNullOrWhiteSpace(id)
                ? throw new ArgumentException(
                    "A feature option id is required.",
                    nameof(id))
                : id;

            Tab = string.IsNullOrWhiteSpace(tab)
                ? "General"
                : tab;

            Label = string.IsNullOrWhiteSpace(label)
                ? id
                : label;

            ValueProvider = valueProvider ??
                throw new ArgumentNullException(nameof(valueProvider));

            OptionsProvider = optionsProvider ??
                throw new ArgumentNullException(nameof(optionsProvider));

            ValueChanged = valueChanged ??
                throw new ArgumentNullException(nameof(valueChanged));

            ParentFeatureId = parentFeatureId ?? string.Empty;
            Order = order;
            EnabledProvider = enabledProvider ?? (() => true);
        }

        public string Id { get; }

        public string Tab { get; }

        public string Label { get; }

        public Func<string> ValueProvider { get; }

        public Func<IReadOnlyList<GK2FeatureOption>> OptionsProvider { get; }

        public Action<string> ValueChanged { get; }

        public string ParentFeatureId { get; }

        public int Order { get; }

        public Func<bool> EnabledProvider { get; }
    }
}
