using System.IO;
using System.Text.RegularExpressions;
using Game.Diagnostics;
using Game.EditorTools;
using NUnit.Framework;

namespace Game.Tests
{
    public class SentryTests
    {
        [TestCase("1.0.0", false, true, ExpectedResult = "editor")]
        [TestCase("1.0.0.2026.10.03.1a2b3c4d", true, false, ExpectedResult = "development")]
        [TestCase("0.1.0.2026.10.03.1a2b3c4d-dirty", false, false, ExpectedResult = "local")]
        [TestCase("0.1.0.2026.10.03.1a2b3c4d", false, false, ExpectedResult = "prerelease")]
        [TestCase("1.0.0.2026.10.03.1a2b3c4d", false, false, ExpectedResult = "production")]
        public string Environment(string version, bool isDebugBuild, bool isEditor) =>
            SentrySetup.Environment(version, isDebugBuild, isEditor);

        [Test]
        public void RedactDropsThePublicKey() =>
            Assert.AreEqual("o1.ingest.us.sentry.io/42", SentryDsn.Redact("https://abc123@o1.ingest.us.sentry.io/42"));

        /// <summary>The DSN is injected at build time only; the repo is public.</summary>
        [Test]
        public void CommittedOptionsHaveNoDsn()
        {
            var yaml = File.ReadAllText(SentryDsn.OptionsPath);
            StringAssert.IsMatch(@"<Dsn>k__BackingField: *\r?\n", yaml);
            Assert.IsFalse(Regex.IsMatch(yaml, @"<Dsn>k__BackingField: *\S"), "SentryOptions.asset contains a DSN");
        }
    }
}
