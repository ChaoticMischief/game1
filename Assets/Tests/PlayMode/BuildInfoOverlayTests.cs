using System.Collections;
using Game.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Game.Tests
{
    public class BuildInfoOverlayTests
    {
        [UnityTest]
        public IEnumerator MainSceneShowsTheBuildVersion()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;

            var overlay = Object.FindAnyObjectByType<BuildInfoOverlay>();
            Assert.IsNotNull(overlay, "Main scene should contain a BuildInfoOverlay");
            var label = overlay.GetComponent<UIDocument>().rootVisualElement.Q<Label>(BuildInfoOverlay.LabelName);
            Assert.IsNotNull(label, "overlay label missing");
            StringAssert.Contains(Application.version, label.text);
        }
    }
}
