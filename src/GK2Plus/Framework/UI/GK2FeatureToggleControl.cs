using System;

namespace GK2Plus.Framework.UI
{
    /// <summary>
    /// Describes a feature setting that can be changed safely from the main
    /// menu and rendered as read-only status while a save is loaded.
    /// </summary>
    internal sealed class GK2FeatureToggleControl
    {
        public GK2FeatureToggleControl(
            string id,
            string tab,
            string label,
            Func<bool> enabledProvider,
            Func<string> statusProvider,
            Action<bool> enabledChanged)
        {
            Id = string.IsNullOrWhiteSpace(id)
                ? throw new ArgumentException("A feature toggle id is required.", nameof(id))
                : id;

            Tab = string.IsNullOrWhiteSpace(tab)
                ? "General"
                : tab;

            Label = string.IsNullOrWhiteSpace(label)
                ? id
                : label;

            EnabledProvider = enabledProvider ??
                throw new ArgumentNullException(nameof(enabledProvider));

            StatusProvider = statusProvider ??
                throw new ArgumentNullException(nameof(statusProvider));

            EnabledChanged = enabledChanged ??
                throw new ArgumentNullException(nameof(enabledChanged));
        }

        public string Id { get; }

        public string Tab { get; }

        public string Label { get; }

        public Func<bool> EnabledProvider { get; }

        public Func<string> StatusProvider { get; }

        public Action<bool> EnabledChanged { get; }
    }
}
