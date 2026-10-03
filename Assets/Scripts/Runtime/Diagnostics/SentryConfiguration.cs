using Sentry.Unity;
using UnityEngine;

namespace Game.Diagnostics
{
    /// <summary>
    /// Programmatic Sentry options, applied on top of Assets/Resources/Sentry/SentryOptions.asset
    /// (Tools > Sentry). The DSN isn't committed: the build injects it from SENTRY_DSN (see
    /// Game.EditorTools.SentryDsn), so builds without it, like forks', don't report.
    /// </summary>
    [CreateAssetMenu(fileName = "SentryConfiguration", menuName = "Game/Sentry Configuration")]
    public sealed class SentryConfiguration : SentryOptionsConfiguration
    {
        public override void Configure(SentryUnityOptions options)
        {
            options.Environment = SentrySetup.Environment(Application.version, Debug.isDebugBuild, Application.isEditor);
        }
    }
}
