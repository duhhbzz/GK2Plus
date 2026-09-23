using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace GK2Plus.Core
{
    internal abstract class FeatureBase : IFeature
    {
        protected ManualLogSource Logger { get; private set; }
        protected Harmony Harmony { get; private set; }

        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract string Category { get; }
        public abstract string Description { get; }

        protected virtual bool DefaultEnabled => true;

        public ConfigEntry<bool> Enabled { get; private set; }

        public void Initialize(
            ConfigFile config,
            ManualLogSource logger,
            Harmony harmony
        )
        {
            Logger = logger;
            Harmony = harmony;

            Enabled = config.Bind(
                Category,
                $"{Id}.Enabled",
                DefaultEnabled,
                Description
            );

            OnInitialize();

            if (Enabled.Value)
            {
                OnEnabled();
                Logger.LogDebug($"Feature enabled: {Name}");
            }
            else
            {
                Logger.LogDebug($"Feature disabled: {Name}");
            }
        }

        protected virtual void OnInitialize()
        {
        }

        protected virtual void OnEnabled()
        {
        }
    }
}
