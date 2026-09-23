using System.Collections.Generic;
using BepInEx.Bootstrap;
using BepInEx.Logging;

namespace GK2Plus.Core
{
    internal sealed class CompatibilityManager
    {
        private readonly ManualLogSource _logger;

        internal CompatibilityManager(ManualLogSource logger)
        {
            _logger = logger;
        }

        internal bool IsPluginLoaded(string pluginGuid)
        {
            return Chainloader.PluginInfos.ContainsKey(pluginGuid);
        }

        internal IEnumerable<string> GetLoadedPluginGuids()
        {
            return Chainloader.PluginInfos.Keys;
        }

        internal void Scan()
        {
            _logger.LogInfo(
                $"Compatibility scan complete. " +
                $"{Chainloader.PluginInfos.Count} BepInEx plugin(s) detected."
            );
        }
    }
}
