using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace GK2Plus.Core
{
    internal interface IFeature
    {
        string Id { get; }
        string Name { get; }
        string Category { get; }
        string Description { get; }

        ConfigEntry<bool> Enabled { get; }

        void Initialize(
            ConfigFile config,
            ManualLogSource logger,
            Harmony harmony
        );
    }
}
