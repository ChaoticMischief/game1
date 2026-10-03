using Game.Diagnostics;
using NUnit.Framework;

namespace Game.Tests
{
    public class BuildInfoTests
    {
        [TestCase("0.1.0.2026.09.28.1a2b3c4d", false, false, ExpectedResult = true)]  // pre-1.0
        [TestCase("1.0.0.2026.09.28.1a2b3c4d", false, false, ExpectedResult = false)] // GA release build
        [TestCase("1.0.0.2026.09.28.1a2b3c4d", true, false, ExpectedResult = true)]   // development build
        [TestCase("1.0.0.2026.09.28.1a2b3c4d-dirty", false, false, ExpectedResult = true)]
        [TestCase("1.0.0", false, true, ExpectedResult = true)]                        // editor
        public bool OverlayVisibility(string version, bool isDebugBuild, bool isEditor) =>
            BuildInfo.ShouldShowOverlay(version, isDebugBuild, isEditor);

        [Test]
        public void EditorTextIsMarked()
        {
            Assert.AreEqual("0.1.0 (editor)", BuildInfo.DisplayText("0.1.0", isEditor: true));
            Assert.AreEqual("0.1.0.2026.09.28.1a2b3c4d", BuildInfo.DisplayText("0.1.0.2026.09.28.1a2b3c4d", isEditor: false));
        }
    }
}
