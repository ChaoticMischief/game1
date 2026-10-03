using System;
using System.IO;

namespace Game.EditorTools
{
    /// <summary>
    /// Values builds need but the public repo mustn't contain (Sentry DSN, Sentry auth token).
    /// CI passes them as command-line arguments, since GameCI doesn't forward environment
    /// variables into its container; GitHub masks them in the logs.
    /// </summary>
    public static class BuildSecrets
    {
        /// <summary>
        /// The value of <paramref name="argument"/> on the command line, else the environment
        /// variable <paramref name="environmentVariable"/>, else the contents of
        /// <paramref name="localFile"/> (gitignored, under UserSettings/); null if none is set.
        /// </summary>
        public static string Find(string argument, string environmentVariable, string localFile = null) =>
            Find(Environment.GetCommandLineArgs(), argument, environmentVariable, localFile);

        public static string Find(string[] args, string argument, string environmentVariable, string localFile = null)
        {
            // An empty secret leaves the flag without a value, followed by the next flag or nothing.
            var i = Array.IndexOf(args, argument);
            var value = i >= 0 && i + 1 < args.Length && !args[i + 1].StartsWith("-") ? args[i + 1] : null;
            if (string.IsNullOrWhiteSpace(value))
                value = Environment.GetEnvironmentVariable(environmentVariable);
            if (string.IsNullOrWhiteSpace(value) && localFile != null && File.Exists(localFile))
                value = File.ReadAllText(localFile);
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
