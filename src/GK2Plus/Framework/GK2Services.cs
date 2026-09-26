using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using GK2Plus.Framework.Crafting;
using GK2Plus.Framework.Events;
using GK2Plus.Framework.Farming;
using GK2Plus.Framework.Inventory;
using GK2Plus.Framework.Localization;
using GK2Plus.Framework.Quests;
using GK2Plus.Framework.Saves;
using GK2Plus.Framework.UI;
using GK2Plus.Framework.World;
using GK2Plus.Framework.Zombies;
using UnityEngine;

namespace GK2Plus.Framework
{
    /// <summary>
    /// Owns GK2+ framework adapters and their lifecycle.
    ///
    /// This does not initialize itself automatically. Plugin.cs should own the
    /// container and call Initialize()/Shutdown() when we wire the framework in.
    /// </summary>
    internal sealed class GK2Services
    {
        private readonly ManualLogSource _logger;
        private readonly List<IGK2Service> _services;

        public GK2Services(ManualLogSource logger, ConfigEntry<KeyCode> menuHotkey)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Events = new GK2EventService(logger);
            Saves = new GK2SaveService(logger);
            World = new GK2WorldService(logger);
            Inventory = new GK2InventoryService(logger);
            Crafting = new GK2CraftingService(logger);
            Farming = new GK2FarmingService(logger);
            Zombies = new GK2ZombieService(logger);
            Quests = new GK2QuestService(logger);
            Localization = new GK2LocalizationService(logger);
            UI = new GK2UIService(logger, menuHotkey);

            _services = new List<IGK2Service>
            {
                Events,
                Saves,
                World,
                Inventory,
                Crafting,
                Farming,
                Zombies,
                Quests,
                Localization,
                UI
            };
        }

        public GK2EventService Events { get; }

        public GK2SaveService Saves { get; }

        public GK2WorldService World { get; }

        public GK2InventoryService Inventory { get; }

        public GK2CraftingService Crafting { get; }

        public GK2FarmingService Farming { get; }

        public GK2ZombieService Zombies { get; }

        public GK2QuestService Quests { get; }

        public GK2LocalizationService Localization { get; }

        public GK2UIService UI { get; }

        public void Initialize()
        {
            _logger.LogInfo($"Initializing {_services.Count} GK2+ framework service(s)...");

            foreach (var service in _services)
            {
                service.Initialize();
            }
        }

        public void Shutdown()
        {
            for (var i = _services.Count - 1; i >= 0; i--)
            {
                try
                {
                    _services[i].Shutdown();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to shut down framework service '{_services[i].Name}': {ex}");
                }
            }
        }
    }
}
