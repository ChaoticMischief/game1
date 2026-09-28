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
        const string AutoRegisterEnabled = "MCPForUnity.AutoRegisterEnabled";

        static McpForUnityDefaults()
        {
            // HTTP on localhost:8080 lets Claude Code and Cursor share one editor
            // (Claude Code UnityMCP entry and .cursor/mcp.json point there).
            SetIfUnset(UseHttpTransport, true);
            if (!EditorPrefs.HasKey(HttpTransportScope))
                EditorPrefs.SetString(HttpTransportScope, "local");
            SetIfUnset(AutoStartOnLoad, true);
            EditorPrefs.SetBool(TelemetryDisabled, true);
            // The package's startup sweep would rewrite client configs on every editor start. Claude
            // Code's UnityMCP entry is machine-local (tools/bootstrap-macos.sh adds it, in the form the
            // package itself writes) and Cursor's is committed in .cursor/mcp.json, so skip the sweep.
            EditorPrefs.SetBool(AutoRegisterEnabled, false);
        }

        static void SetIfUnset(string key, bool value)
        {
            if (!EditorPrefs.HasKey(key))
                EditorPrefs.SetBool(key, value);
        }
    }
}
