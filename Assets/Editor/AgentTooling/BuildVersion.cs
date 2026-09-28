using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

namespace Game.EditorTools
{
    /// <summary>
    /// Stamps every player build with <c>major.minor.patch.yyyy.MM.dd.hash8</c>, e.g.
    /// <c>1.0.0.2026.09.27.54d78a87</c>, with "-dirty" appended when the working tree has
    /// uncommitted changes. The date is the commit's (UTC), so rebuilding a commit
    /// reproduces its version. Players read it via <c>Application.version</c>.
    ///
    /// The base <c>major.minor.patch</c> is Player Settings > Version (committed). It's
    /// stamped just before the build and restored afterwards so builds don't dirty
    /// ProjectSettings.asset. Runs for every build path: tools/unity/unity.sh, MCP and the
    /// editor's Build Profiles window.
    /// </summary>
    public class BuildVersion : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        static readonly Regex BasePattern = new(@"^(\d+)\.(\d+)\.(\d+)");
        static string s_BaseBeforeBuild;

        /// <summary>The version stamped into the most recent build.</summary>
        public static string LastStamped { get; private set; }

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            s_BaseBeforeBuild = BaseVersion(PlayerSettings.bundleVersion);
            PlayerSettings.bundleVersion = LastStamped = Compute(s_BaseBeforeBuild, ProjectRoot());
            Debug.Log($"[BuildVersion] {PlayerSettings.bundleVersion}");
        }

        public void OnPostprocessBuild(BuildReport report) => Restore();

        /// <summary>Put Player Settings back to the base version (also called if a build throws).</summary>
        public static void Restore()
        {
            if (s_BaseBeforeBuild == null) return;
            PlayerSettings.bundleVersion = s_BaseBeforeBuild;
            s_BaseBeforeBuild = null;
            AssetDatabase.SaveAssets();
        }

        /// <summary>The <c>major.minor.patch</c> part of a (possibly already stamped) version.</summary>
        public static string BaseVersion(string version)
        {
            var m = BasePattern.Match(version ?? "");
            return m.Success ? m.Value : "0.1.0";
        }

        /// <summary>Full version for the git checkout at <paramref name="repoRoot"/>.</summary>
        public static string Compute(string baseVersion, string repoRoot)
        {
            // CI checkouts may lack git or trip its ownership check; GITHUB_SHA covers those.
            var hash = Git(repoRoot, "log -1 --format=%H");
            if (string.IsNullOrEmpty(hash)) hash = Environment.GetEnvironmentVariable("GITHUB_SHA");
            var date = Git(repoRoot, "log -1 --format=%cd --date=format-local:%Y.%m.%d");
            if (string.IsNullOrEmpty(date)) date = DateTime.UtcNow.ToString("yyyy.MM.dd");
            var changes = string.Join("\n", (Git(repoRoot, "status --porcelain --untracked-files=no") ?? "")
                .Split('\n').Where(line => line.Trim().Length > 0 && !IsUnityManagedCache(line)));
            var dirty = !string.IsNullOrEmpty(changes);
            if (dirty)
                Debug.Log($"[BuildVersion] uncommitted changes (build marked -dirty):\n{changes}");

            var shortHash = string.IsNullOrEmpty(hash) ? "nogit" : hash.Substring(0, Math.Min(8, hash.Length));
            return $"{BaseVersion(baseVersion)}.{date}.{shortHash}{(dirty ? "-dirty" : "")}";
        }

        /// <summary>
        /// Files Unity rewrites by itself, so their changes don't mean the source differs from the
        /// commit. URP clears the runtime-settings cache in *RenderPipelineGlobalSettings.asset on
        /// a fresh import (every CI run) and refills it during the build, after the stamp is taken.
        /// </summary>
        static bool IsUnityManagedCache(string porcelainLine) =>
            porcelainLine.EndsWith("RenderPipelineGlobalSettings.asset", StringComparison.Ordinal);

        static string ProjectRoot() => System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath);

        static string Git(string repoRoot, string args)
        {
            try
            {
                var psi = new ProcessStartInfo("git", $"-c safe.directory=* {args}")
                {
                    WorkingDirectory = repoRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                psi.EnvironmentVariables["TZ"] = "UTC";
                using var p = Process.Start(psi);
                var output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(10000);
                return p.ExitCode == 0 ? output : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
