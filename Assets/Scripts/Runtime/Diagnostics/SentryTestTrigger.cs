using System;
using System.Linq;
using UnityEngine;

namespace Game.Diagnostics
{
    /// <summary>
    /// Run a player with <c>-sentry-test</c> to check that crash reporting reaches Sentry: it logs
    /// one error and one exception, each naming the build version.
    /// </summary>
    static class SentryTestTrigger
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void SendIfRequested()
        {
            if (!Environment.GetCommandLineArgs().Contains(SentrySetup.TestFlag))
                return;
            GameLog.Error("Diagnostics", $"Sentry test error from {Application.version}");
            GameLog.Exception(new InvalidOperationException($"Sentry test exception from {Application.version}"));
        }
    }
}
