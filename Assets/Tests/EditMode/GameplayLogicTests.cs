using Game.Hints;
using Game.Player;
using Game.Room;
using NUnit.Framework;

namespace Game.Tests
{
    public class JumpMathTests
    {
        [Test]
        public void VelocityReachesRequestedHeight()
        {
            var v = JumpMath.VelocityForHeight(2f, 20f);
            Assert.AreEqual(2f, v * v / (2f * 20f), 1e-4f);
        }

        [TestCase(0f, 10f)]
        [TestCase(-1f, 10f)]
        [TestCase(2f, 0f)]
        public void InvalidInputGivesNoJump(float height, float gravity)
        {
            Assert.AreEqual(0f, JumpMath.VelocityForHeight(height, gravity));
        }
    }

    public class HudTextTests
    {
        [Test]
        public void SelectedLineNamesThePlushie()
        {
            Assert.AreEqual("Currently selected plushie: Rabbit", HudText.SelectedLine("Rabbit"));
        }

        [TestCase(null)]
        [TestCase("")]
        public void SelectedLineHandlesNoPlushie(string name)
        {
            Assert.AreEqual("Currently selected plushie: none", HudText.SelectedLine(name));
        }

        [Test]
        public void HelpMentionsEveryControl()
        {
            foreach (var word in new[] { "A / D", "Space", "R: select the rabbit", "B: select the bear", "1 / 2", "E:", "F1", "Gamepad" })
                StringAssert.Contains(word, HudText.HelpBody);
        }
    }

    public class CameraFitTests
    {
        [Test]
        public void WideWindowKeepsTheRoomHeight()
        {
            Assert.AreEqual(6.3f, CameraFit.OrthographicSize(11f, 6.3f, 16f / 9f), 1e-4f);
        }

        [Test]
        public void NarrowWindowWidensTheViewToFitTheRoomWidth()
        {
            // 4:3 is 1.333 wide per unit of height, so 22 units wide needs a half-height of 11 / 1.333.
            Assert.AreEqual(8.25f, CameraFit.OrthographicSize(11f, 6.3f, 4f / 3f), 1e-3f);
        }

        [Test]
        public void ZeroAspectFallsBackToTheRoomHeight()
        {
            Assert.AreEqual(6.3f, CameraFit.OrthographicSize(11f, 6.3f, 0f));
        }
    }

    public class PlushieSelectionTests
    {
        [Test]
        public void NextWrapsAround()
        {
            var selection = new PlushieSelection(2);
            Assert.IsTrue(selection.Next());
            Assert.AreEqual(1, selection.Index);
            Assert.IsTrue(selection.Next());
            Assert.AreEqual(0, selection.Index);
        }

        [Test]
        public void PreviousWrapsAround()
        {
            var selection = new PlushieSelection(3);
            Assert.IsTrue(selection.Previous());
            Assert.AreEqual(2, selection.Index);
        }

        [Test]
        public void SingleOrEmptyRosterNeverChanges()
        {
            Assert.IsFalse(new PlushieSelection(1).Next());
            var empty = new PlushieSelection(0);
            Assert.IsFalse(empty.Next());
            Assert.IsFalse(empty.Previous());
            Assert.AreEqual(0, empty.Index);
        }

        [Test]
        public void SelectIgnoresOutOfRangeAndResetReturnsToFirst()
        {
            var selection = new PlushieSelection(2);
            Assert.IsFalse(selection.Select(5));
            Assert.IsTrue(selection.Select(1));
            selection.Reset();
            Assert.AreEqual(0, selection.Index);
        }
    }

    public class PlateStateTests
    {
        [Test]
        public void HeavyBodyPressesLightBodyDoesNot()
        {
            var plate = new PlateState<int>(3f, false);
            plate.Add(1, 1f);
            Assert.IsFalse(plate.Pressed);
            plate.Remove(1);
            plate.Add(2, 5f);
            Assert.IsTrue(plate.Pressed);
        }

        [Test]
        public void MassesAddUp()
        {
            var plate = new PlateState<int>(3f, false);
            plate.Add(1, 2f);
            plate.Add(2, 1.5f);
            Assert.IsTrue(plate.Pressed);
            plate.Remove(2);
            Assert.IsFalse(plate.Pressed);
        }

        [Test]
        public void ReleasingAPlainPlateDeactivatesIt()
        {
            var plate = new PlateState<int>(3f, false);
            plate.Add(1, 5f);
            plate.Remove(1);
            Assert.IsFalse(plate.Active);
        }

        [Test]
        public void LatchingPlateStaysActiveUntilReset()
        {
            var lever = new PlateState<int>(0.5f, true);
            lever.Add(1, 1f);
            lever.Remove(1);
            Assert.IsFalse(lever.Pressed);
            Assert.IsTrue(lever.Latched);
            Assert.IsTrue(lever.Active);
            lever.Reset();
            Assert.IsFalse(lever.Active);
        }

        [Test]
        public void EmptyPlateWithZeroRequirementIsNotPressed()
        {
            Assert.IsFalse(new PlateState<int>(0f, false).Pressed);
        }
    }

    public class HintSequenceTests
    {
        static readonly string[] k_Tiers = { "nudge", "who", "solution" };

        [Test]
        public void RevealsOneTierPerRequestThenRepeatsTheLast()
        {
            var hints = new HintSequence(k_Tiers);
            Assert.AreEqual("nudge", hints.Next());
            Assert.AreEqual("who", hints.Next());
            Assert.AreEqual("solution", hints.Next());
            Assert.AreEqual("solution", hints.Next());
            Assert.AreEqual(3, hints.Shown);
            Assert.IsFalse(hints.HasMore);
        }

        [Test]
        public void ResetStartsOver()
        {
            var hints = new HintSequence(k_Tiers);
            hints.Next();
            hints.Reset();
            Assert.AreEqual("nudge", hints.Next());
        }

        [Test]
        public void NoTiersGivesEmptyText()
        {
            Assert.AreEqual(string.Empty, new HintSequence(null).Next());
        }
    }
}
