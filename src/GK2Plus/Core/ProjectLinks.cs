namespace GK2Plus.Core
{
    internal static class ProjectLinks
    {
        internal const string GitHubUrl = "https://github.com/duhhbzz/GK2Plus";
        internal const string BugReportUrl = "https://github.com/duhhbzz/GK2Plus/issues/new";

        // Set after the public Nexus Mods page is restored/published.
        // Keeping this empty leaves the in-game Nexus button safely non-interactive.
        internal const string NexusUrl = "";

        internal static bool HasNexusUrl =>
            !string.IsNullOrWhiteSpace(NexusUrl);
    }
}
