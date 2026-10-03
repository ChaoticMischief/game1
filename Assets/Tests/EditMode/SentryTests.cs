using System;
using System.IO;
using System.Text;
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

        [Test]
        public void OrganizationComesFromAnOrgToken()
        {
            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                "{\"iat\":1,\"url\":\"https://sentry.io\",\"org\":\"my-org\"}")).TrimEnd('=');
            Assert.AreEqual("my-org", SentryAuthToken.Organization($"sntrys_{payload}_secret"));
            Assert.IsNull(SentryAuthToken.Organization("sntryu_personaltoken"));
            Assert.IsNull(SentryAuthToken.Organization("sntrys_not!base64_secret"));
        }

        [TestCase("unity -sentryAuthToken abc", ExpectedResult = "abc")]
        [TestCase("unity -sentryAuthToken -buildOutput x", ExpectedResult = null)] // empty secret
        [TestCase("unity -sentryAuthToken", ExpectedResult = null)]
        [TestCase("unity", ExpectedResult = null)]
        public string SecretFromCommandLine(string commandLine) =>
            BuildSecrets.Find(commandLine.Split(' '), "-sentryAuthToken", "GAME1_TEST_UNSET_VARIABLE");

        /// <summary>The DSN is injected at build time only; the repo is public.</summary>
        [Test]
        public void CommittedOptionsHaveNoDsn()
        {
            var yaml = File.ReadAllText(SentryDsn.OptionsPath);
            StringAssert.IsMatch(@"<Dsn>k__BackingField: *\r?\n", yaml);
            Assert.IsFalse(Regex.IsMatch(yaml, @"<Dsn>k__BackingField: *\S"), "SentryOptions.asset contains a DSN");
        }

        /// <summary>The auth token is supplied at build time only (SentryCliConfiguration).</summary>
        [Test]
        public void CommittedCliOptionsHaveNoToken()
        {
            var yaml = File.ReadAllText("Assets/Plugins/Sentry/SentryCliOptions.asset");
            StringAssert.IsMatch(@"<Auth>k__BackingField: *\r?\n", yaml);
            Assert.IsFalse(Regex.IsMatch(yaml, @"<Auth>k__BackingField: *\S"), "SentryCliOptions.asset contains an auth token");
        }
    }
}
