using UnityEditor;

namespace Game.EditorTools
{
    /// <summary>
    /// Applies this project's MCP for Unity defaults on editor load. They're EditorPrefs
    /// (per machine, not per project), so keeping them here makes a fresh machine match.
    /// A value set explicitly in Window > MCP for Unity is left alone, except telemetry,
    /// which is always kept off.
    /// Keys mirror MCPForUnity/Editor/Constants/EditorPrefKeys.cs (v10).
    /// </summary>
    [InitializeOnLoad]
    static class McpForUnityDefaults
    {
        const string UseHttpTransport = "MCPForUnity.UseHttpTransport";
        const string HttpTransportScope = "MCPForUnity.HttpTransportScope";
        const string AutoStartOnLoad = "MCPForUnity.AutoStartOnLoad";
        const string TelemetryDisabled = "MCPForUnity.TelemetryDisabled";

        static McpForUnityDefaults()
        {
            // HTTP on localhost:8080 lets Claude Code and Cursor share one editor
            // (.mcp.json and .cursor/mcp.json point there).
            SetIfUnset(UseHttpTransport, true);
            if (!EditorPrefs.HasKey(HttpTransportScope))
                EditorPrefs.SetString(HttpTransportScope, "local");
            SetIfUnset(AutoStartOnLoad, true);
            EditorPrefs.SetBool(TelemetryDisabled, true);
        }

        static void SetIfUnset(string key, bool value)
        {
            if (!EditorPrefs.HasKey(key))
                EditorPrefs.SetBool(key, value);
        }
    }
}
