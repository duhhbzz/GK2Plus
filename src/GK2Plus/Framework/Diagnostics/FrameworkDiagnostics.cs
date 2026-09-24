using BepInEx.Logging;

namespace GK2Plus.Framework.Diagnostics
{
    internal static class FrameworkDiagnostics
    {
        public static void LogReady(ManualLogSource logger)
        {
            logger.LogInfo("GK2+ framework scaffold ready.");
        }
    }
}
