using System.IO;
using System.Text.RegularExpressions;
using Game.EditorTools;
using NUnit.Framework;

namespace Game.Tests
{
    public class BuildVersionTests
    {
        [TestCase("0.1.0", "0.1.0")]
        [TestCase("1.0.0.2026.09.27.54d78a87", "1.0.0")]
        [TestCase("1.2.3.2026.09.27.54d78a87-dirty", "1.2.3")]
        [TestCase("1.0", "0.1.0")]
        public void BaseVersionStripsStamp(string version, string expected)
        {
            Assert.AreEqual(expected, BuildVersion.BaseVersion(version));
        }

        [Test]
        public void ComputeMatchesVersionFormat()
        {
            var repoRoot = Path.GetDirectoryName(UnityEngine.Application.dataPath);
            var version = BuildVersion.Compute("1.0.0", repoRoot);
            StringAssert.IsMatch(@"^1\.0\.0\.\d{4}\.\d{2}\.\d{2}\.[0-9a-f]{8}(-dirty)?$", version);
        }
    }
}
