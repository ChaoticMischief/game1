using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

namespace Game.EditorTools
{
    /// <summary>
    /// Puts the Sentry DSN into player builds without committing it. The committed
    /// SentryOptions.asset has an empty DSN (so the SDK stays off in the editor, in forks and in
    /// anyone else's builds); this writes the DSN in just before a build and clears it afterwards.
    ///
    /// The DSN comes from, in order: the <c>-sentryDsn &lt;dsn&gt;</c> command-line argument (CI
    /// passes the repository secret this way, as GameCI doesn't forward environment variables into
    /// its container), the SENTRY_DSN environment variable, or UserSettings/SentryDsn.txt
    /// (gitignored, for local builds). With none of them, the build has Sentry off.
    /// </summary>
    public class SentryDsn : IPreprocessBuildWithReport
    {
        public const string OptionsPath = "Assets/Resources/Sentry/SentryOptions.asset";
        public const string LocalFile = "UserSettings/SentryDsn.txt";
        const string DsnProperty = "<Dsn>k__BackingField";

        static bool s_Injected;

        // Before Sentry's own build processors, which read the options. SentryBuildCleanup clears it
        // again after them.
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            var dsn = Find();
            if (string.IsNullOrEmpty(dsn))
            {
                Debug.LogWarning($"[SentryDsn] No -sentryDsn, SENTRY_DSN or {LocalFile}: this build won't report to Sentry.");
                return;
            }
            Set(dsn);
            s_Injected = true;
            Debug.Log($"[SentryDsn] Sentry enabled for this build ({Redact(dsn)})");
        }

        /// <summary>Clear the DSN again (also called if a build throws).</summary>
        public static void Restore()
        {
            if (!s_Injected) return;
            Set("");
            s_Injected = false;
        }

        /// <summary>The DSN host and project, without the public key.</summary>
        public static string Redact(string dsn) =>
            Uri.TryCreate(dsn, UriKind.Absolute, out var uri) ? $"{uri.Host}{uri.AbsolutePath}" : "unparseable DSN";

        static string Find() => BuildSecrets.Find("-sentryDsn", "SENTRY_DSN", LocalFile);

        static void Set(string dsn)
        {
            var options = AssetDatabase.LoadMainAssetAtPath(OptionsPath);
            if (options == null)
                throw new BuildFailedException($"[SentryDsn] {OptionsPath} is missing (Tools > Sentry creates it).");
            var so = new SerializedObject(options);
            var property = so.FindProperty(DsnProperty)
                ?? throw new BuildFailedException($"[SentryDsn] {OptionsPath} has no {DsnProperty}; did the Sentry SDK rename it?");
            property.stringValue = dsn;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(options);
        }
    }

    /// <summary>
    /// Clears the Sentry DSN and auth token after every other post-build step, including
    /// Sentry's symbol upload.
    /// </summary>
    public class SentryBuildCleanup : IPostprocessBuildWithReport
    {
        public int callbackOrder => int.MaxValue;

        public void OnPostprocessBuild(BuildReport report)
        {
            SentryDsn.Restore();
            SentryCliConfiguration.ClearToken();
        }
    }
}
