using System;
using BepInEx.Logging;
using UnityEngine;

namespace GK2Plus.Core
{
    internal static class GameCompatibility
    {
        internal static string CurrentGameVersion
        {
            get
            {
                string version = Application.version;
                return string.IsNullOrWhiteSpace(version)
                    ? "unknown"
                    : version.Trim();
            }
        }

        internal static bool IsTestedVersion =>
            string.Equals(
                CurrentGameVersion,
                ModInfo.TestedGameVersion,
                StringComparison.OrdinalIgnoreCase
            );

        internal static string DisplayText =>
            IsTestedVersion
                ? $"v{ModInfo.Version} | GK2 {CurrentGameVersion} [tested]"
                : $"v{ModInfo.Version} | GK2 {CurrentGameVersion} [untested]";

        internal static string BadgeText =>
            IsTestedVersion
                ? $"GK2 {CurrentGameVersion} [Tested]"
                : $"GK2 {CurrentGameVersion} [Untested]";

        internal static void LogStatus(ManualLogSource logger)
        {
            if (IsTestedVersion)
            {
                logger.LogInfo(
                    $"Game compatibility: Graveyard Keeper 2 v{CurrentGameVersion} " +
                    $"matches tested version v{ModInfo.TestedGameVersion}."
                );
                return;
            }

            logger.LogWarning(
                $"Game compatibility: running Graveyard Keeper 2 v{CurrentGameVersion}; " +
                $"GK2+ v{ModInfo.Version} was tested against v{ModInfo.TestedGameVersion}. " +
                "GK2+ will continue loading, but this game version has not yet been validated."
            );
        }
    }
}
