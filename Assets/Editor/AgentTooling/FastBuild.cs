using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

namespace Game.EditorTools
{
    /// <summary>
    /// <c>-fastBuild</c> (CI passes it for pull requests): trade runtime performance for build
    /// time where it's large. Today that's WebGL, whose IL2CPP C++ compile dominates CI: generate
    /// less C++ ("Faster (smaller) builds") and compile it with the Debug configuration. Published
    /// builds (pushes to main) don't pass the flag.
    ///
    /// The settings live in ProjectSettings.asset, so they're applied after BuildVersion's
    /// uncommitted-changes check and restored after the build.
    /// </summary>
    public class FastBuild : IPreprocessBuildWithReport
    {
        public const string Flag = "-fastBuild";

        static Il2CppCodeGeneration? s_CodeGenerationBefore;
        static Il2CppCompilerConfiguration? s_CompilerConfigurationBefore;

        public int callbackOrder => int.MinValue + 1; // right after BuildVersion

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!Environment.GetCommandLineArgs().Contains(Flag) || report.summary.platform != BuildTarget.WebGL)
                return;
            s_CodeGenerationBefore = PlayerSettings.GetIl2CppCodeGeneration(NamedBuildTarget.WebGL);
            s_CompilerConfigurationBefore = PlayerSettings.GetIl2CppCompilerConfiguration(NamedBuildTarget.WebGL);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.WebGL, Il2CppCompilerConfiguration.Debug);
            Debug.Log("[FastBuild] WebGL: IL2CPP code generation OptimizeSize, compiler configuration Debug");
        }

        /// <summary>Put the settings back (after every other post-build step, and if a build throws).</summary>
        public static void Restore()
        {
            if (s_CodeGenerationBefore is { } codeGeneration)
                PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, codeGeneration);
            if (s_CompilerConfigurationBefore is { } compilerConfiguration)
                PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.WebGL, compilerConfiguration);
            s_CodeGenerationBefore = null;
            s_CompilerConfigurationBefore = null;
        }
    }

    /// <summary>Runs <see cref="FastBuild.Restore"/> late, but before BuildVersionRestore saves assets.</summary>
    public class FastBuildRestore : IPostprocessBuildWithReport
    {
        public int callbackOrder => int.MaxValue - 1;
        public void OnPostprocessBuild(BuildReport report) => FastBuild.Restore();
    }
}
