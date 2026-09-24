namespace GK2Plus.Core
{
    internal static class ProjectLinks
    {
        internal const string GitHubUrl = "https://github.com/duhhbzz/GK2Plus";
        internal const string BugReportUrl = "https://github.com/duhhbzz/GK2Plus/issues/new";

        // Filled after the public Nexus Mods page exists.
        // Keeping this empty makes the 0.0.1 Nexus button safely non-interactive.
        internal const string NexusUrl = "";

        internal static bool HasNexusUrl =>
            !string.IsNullOrWhiteSpace(NexusUrl);
    }
}
