using System;
using System.Collections.Generic;

namespace GK2Plus.Framework.UI
{
    internal sealed class GK2ItemOption
    {
        public GK2ItemOption(string id, string displayName)
        {
            Id = id ?? string.Empty;
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? Id
                : displayName;
        }

        public string Id { get; }

        public string DisplayName { get; }
    }

    internal sealed class GK2SpawnItemControl
    {
        public GK2SpawnItemControl(
            string tab,
            Func<IReadOnlyList<GK2ItemOption>> itemOptionsProvider,
            Func<string> selectedItemIdProvider,
            Func<int> quantityProvider,
            Action<string> itemSelected,
            Action<int> quantityChanged,
            Action spawn,
            Func<bool> canSpawn)
        {
            Tab = string.IsNullOrWhiteSpace(tab) ? "Cheats" : tab;
            ItemOptionsProvider = itemOptionsProvider ??
                throw new ArgumentNullException(nameof(itemOptionsProvider));
            SelectedItemIdProvider = selectedItemIdProvider ??
                throw new ArgumentNullException(nameof(selectedItemIdProvider));
            QuantityProvider = quantityProvider ??
                throw new ArgumentNullException(nameof(quantityProvider));
            ItemSelected = itemSelected ??
                throw new ArgumentNullException(nameof(itemSelected));
            QuantityChanged = quantityChanged ??
                throw new ArgumentNullException(nameof(quantityChanged));
            Spawn = spawn ??
                throw new ArgumentNullException(nameof(spawn));
            CanSpawn = canSpawn ?? (() => true);
        }

        public string Tab { get; }

        public Func<IReadOnlyList<GK2ItemOption>> ItemOptionsProvider { get; }

        public Func<string> SelectedItemIdProvider { get; }

        public Func<int> QuantityProvider { get; }

        public Action<string> ItemSelected { get; }

        public Action<int> QuantityChanged { get; }

        public Action Spawn { get; }

        public Func<bool> CanSpawn { get; }
    }
}
