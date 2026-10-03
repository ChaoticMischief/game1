namespace Game.Diagnostics
{
    /// <summary>Sentry decisions kept free of Unity and Sentry types so EditMode tests cover them.</summary>
    public static class SentrySetup
    {
        /// <summary>Command-line flag that makes a player send a test error and exception to Sentry.</summary>
        public const string TestFlag = "-sentry-test";

        /// <summary>
        /// Sentry environment for a build, so dashboards can filter: "editor", "development"
        /// (Development Build), "local" (built from uncommitted changes), "prerelease" (0.x) or
        /// "production".
        /// </summary>
        public static string Environment(string version, bool isDebugBuild, bool isEditor)
        {
            if (isEditor) return "editor";
            if (isDebugBuild) return "development";
            if (BuildInfo.IsDirty(version)) return "local";
            if (BuildInfo.IsPreRelease(version)) return "prerelease";
            return "production";
        }
    }
}
