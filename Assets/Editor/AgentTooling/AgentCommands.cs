using System;
using System.IO;
using System.Linq;
using Unity.CodeEditor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Entry points for headless (-batchmode -executeMethod) runs; see tools/unity/.
    /// Each one exits the editor with 0 on success and 1 on failure.
    /// </summary>
    public static class AgentCommands
    {
        /// <summary>Regenerate the .sln/.csproj files (used by Rider, Cursor and csharp-ls).</summary>
        public static void SyncSolution()
        {
            Run(() =>
            {
                CodeEditor.CurrentEditor.SyncAll();
                Debug.Log($"[AgentCommands] Solution synced via {CodeEditor.CurrentEditor.GetType().Name}");
            });
        }

        /// <summary>
        /// Build the scenes enabled in Build Settings.
        /// Args: -buildTarget &lt;StandaloneOSX|StandaloneWindows64|WebGL&gt; -buildOutput &lt;path&gt; [-development]
        /// </summary>
        public static void Build()
        {
            Run(() =>
            {
                var target = EditorUserBuildSettings.activeBuildTarget;
                var output = Arg("-buildOutput") ?? DefaultOutput(target);
                var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                if (scenes.Length == 0)
                    throw new InvalidOperationException("No enabled scenes in Build Settings.");

                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    target = target,
                    locationPathName = output,
                    options = Environment.GetCommandLineArgs().Contains("-development")
                        ? BuildOptions.Development : BuildOptions.None,
                };
                BuildReport report;
                try
                {
                    report = BuildPipeline.BuildPlayer(options);
                }
                finally
                {
                    // post-build callbacks don't run when a build fails
                    BuildVersion.Restore();
                    SentryDsn.Restore();
                    SentryCliConfiguration.ClearToken();
                }
                var s = report.summary;
                Debug.Log($"[AgentCommands] Build {s.result}: {target} {BuildVersion.LastStamped} -> {output}, " +
                          $"{s.totalErrors} errors, {s.totalWarnings} warnings, {s.totalSize} bytes, {s.totalTime}");
                ReportForGameCI(s);
                if (s.result != BuildResult.Succeeded)
                    throw new Exception($"Build {s.result}");
            });
        }

        /// <summary>Create Assets/Scenes/Main.unity and put it first in Build Settings, if missing.</summary>
        public static void EnsureMainScene()
        {
            Run(() =>
            {
                const string path = "Assets/Scenes/Main.unity";
                if (!File.Exists(path))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                    EditorSceneManager.SaveScene(scene, path);
                }
                if (EditorBuildSettings.scenes.All(s => s.path != path))
                {
                    EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) }
                        .Concat(EditorBuildSettings.scenes).ToArray();
                }
                AssetDatabase.SaveAssets();
                Debug.Log($"[AgentCommands] Main scene ready: {path}");
            });
        }

        /// <summary>
        /// GameCI (CI's unity-builder) decides pass/fail by scanning the log for the lines its own
        /// default build method prints: "Build succeeded!", or a "# Build results #" block whose
        /// "Errors:" count must be 0. Print the same so it accepts this custom build method.
        /// </summary>
        static void ReportForGameCI(BuildSummary s)
        {
            var succeeded = s.result == BuildResult.Succeeded;
            var errors = succeeded ? s.totalErrors : Math.Max(1, s.totalErrors);
            var lines = string.Join("\n",
                "###########################",
                "#      Build results      #",
                "###########################",
                $"Duration: {s.totalTime}",
                $"Warnings: {s.totalWarnings}",
                $"Errors: {errors}",
                $"Size: {s.totalSize} bytes",
                "###########################",
                succeeded ? "Build succeeded!" : $"Build {s.result}!");
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", lines);
        }

        static string DefaultOutput(BuildTarget target) => target switch
        {
            BuildTarget.StandaloneOSX => "Builds/macOS/game1.app",
            BuildTarget.StandaloneWindows64 => "Builds/Windows/game1.exe",
            BuildTarget.WebGL => "Builds/WebGL",
            _ => $"Builds/{target}/game1",
        };

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            var i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        static void Run(Action action)
        {
            try
            {
                action();
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AgentCommands] {e}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }
    }
}
