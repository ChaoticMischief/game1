using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>Proves the EditMode test pipeline (tools/unity/test.sh, MCP run_tests) works.</summary>
    public class ToolingSmokeTests
    {
        [Test]
        public void RunsOnUnity6()
        {
            StringAssert.StartsWith("6000.", Application.unityVersion);
        }
    }
}
