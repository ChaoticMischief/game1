namespace Game.Diagnostics
{
    /// <summary>Decisions about showing the build version, kept free of Unity types so EditMode tests cover them.</summary>
    public static class BuildInfo
    {
        /// <summary>
        /// Show the version overlay in the editor, development builds, pre-1.0 versions and builds
        /// of uncommitted code; hide it in release builds from 1.0 on.
        /// </summary>
        public static bool ShouldShowOverlay(string version, bool isDebugBuild, bool isEditor) =>
            isEditor || isDebugBuild || IsPreRelease(version) || IsDirty(version);

        public static string DisplayText(string version, bool isEditor) =>
            isEditor ? $"{version} (editor)" : version;

        /// <summary>Major version 0, e.g. "0.1.0.2026.09.28.1a2b3c4d".</summary>
        public static bool IsPreRelease(string version) => version != null && version.StartsWith("0.");

        /// <summary>Built from uncommitted changes (the build stamp ends in "-dirty").</summary>
        public static bool IsDirty(string version) => version != null && version.EndsWith("-dirty");
    }
}
