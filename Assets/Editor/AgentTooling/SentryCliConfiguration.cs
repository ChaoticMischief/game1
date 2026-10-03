using System;
using System.Text;
using System.Text.RegularExpressions;
using Sentry.Unity;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Supplies the Sentry auth token for debug-symbol upload at build time, so it's never
    /// committed: Sentry calls <see cref="Configure"/> on the in-memory
    /// Assets/Plugins/Sentry/SentryCliOptions.asset just before uploading.
    ///
    /// The token comes from <c>-sentryAuthToken &lt;token&gt;</c> (CI, from the SENTRY_AUTH_TOKEN
    /// secret) or the SENTRY_AUTH_TOKEN environment variable. Without one (local builds), upload is
    /// skipped. Sentry writes the token to a sentry.properties file in the build folder while
    /// sentry-cli runs and deletes it afterwards; CI checks the build output doesn't contain it.
    /// </summary>
    public sealed class SentryCliConfiguration : SentryCliOptionsConfiguration
    {
        static SentryCliOptions s_Configured;
        static bool s_UploadSymbolsBefore;
        static string s_OrganizationBefore;

        public override void Configure(SentryCliOptions options)
        {
            ClearToken();
            s_Configured = options;
            s_UploadSymbolsBefore = options.UploadSymbols;
            s_OrganizationBefore = options.Organization;
            var token = BuildSecrets.Find("-sentryAuthToken", "SENTRY_AUTH_TOKEN");
            if (token == null)
            {
                // Without a token Sentry would warn on every build; local builds don't need symbols.
                options.UploadSymbols = false;
                return;
            }
            options.Auth = token;
            if (string.IsNullOrEmpty(options.Organization))
                options.Organization = SentryAuthToken.Organization(token);
            Debug.Log($"[SentryCliConfiguration] Uploading debug symbols to {options.Organization}/{options.Project}");
        }

        /// <summary>
        /// Put the in-memory options back as committed, so nothing can save the token (or the
        /// disabled upload) into the asset. Called after every other post-build step, and if a
        /// build throws.
        /// </summary>
        public static void ClearToken()
        {
            if (s_Configured == null) return;
            s_Configured.Auth = "";
            s_Configured.UploadSymbols = s_UploadSymbolsBefore;
            s_Configured.Organization = s_OrganizationBefore;
            s_Configured = null;
        }
    }

    /// <summary>Sentry auth token helpers (no Sentry types, so EditMode tests can call them).</summary>
    public static class SentryAuthToken
    {
        /// <summary>
        /// The organization slug inside an organization auth token
        /// (<c>sntrys_&lt;base64 JSON with "org"&gt;_&lt;secret&gt;</c>); null for other tokens.
        /// </summary>
        public static string Organization(string token)
        {
            var parts = token.Split('_');
            if (parts.Length < 3 || parts[0] != "sntrys") return null;
            try
            {
                var payload = parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=');
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                var match = Regex.Match(json, "\"org\"\\s*:\\s*\"([^\"]+)\"");
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
