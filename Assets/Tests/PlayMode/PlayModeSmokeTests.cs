using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>Proves the PlayMode test pipeline runs frames.</summary>
    public class PlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator AdvancesAFrame()
        {
            var start = Time.frameCount;
            yield return null;
            Assert.Greater(Time.frameCount, start);
        }
    }
}
