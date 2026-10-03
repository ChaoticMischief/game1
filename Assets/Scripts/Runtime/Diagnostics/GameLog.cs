using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Diagnostics
{
    /// <summary>
    /// The project's logging entry point: every line is tagged with a category ("Player", "Save",
    /// "Net", ...) so logs can be filtered. Writes through Unity's logger, so it shows in the
    /// console and Player.log, and Sentry (when enabled) records Info/Warning lines as breadcrumbs
    /// and Error/Exception as events.
    /// </summary>
    public static class GameLog
    {
        public static void Info(string category, string message, Object context = null) =>
            Debug.unityLogger.Log(LogType.Log, category, message, context);

        public static void Warning(string category, string message, Object context = null) =>
            Debug.unityLogger.Log(LogType.Warning, category, message, context);

        public static void Error(string category, string message, Object context = null) =>
            Debug.unityLogger.Log(LogType.Error, category, message, context);

        public static void Exception(Exception exception, Object context = null) =>
            Debug.unityLogger.LogException(exception, context);
    }
}
