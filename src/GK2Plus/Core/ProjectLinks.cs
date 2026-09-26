namespace GK2Plus.Core
{
    internal static class ProjectLinks
    {
        internal const string GitHubUrl = "https://github.com/duhhbzz/GK2Plus";
        internal const string BugReportUrl = "https://github.com/duhhbzz/GK2Plus/issues/new";

        internal const string NexusUrl =
            "https://www.nexusmods.com/graveyardkeeper2/mods/69";

        internal static bool HasNexusUrl =>
            !string.IsNullOrWhiteSpace(NexusUrl);
    }
}
