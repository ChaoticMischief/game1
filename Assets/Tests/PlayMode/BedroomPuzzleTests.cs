using System.Collections;
using Game.Player;
using Game.Room;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>
    /// Plays the version 1 bedroom through by driving the plushies directly (bypassing input):
    /// the bear holds the plate, the rabbit hops over it, goes through the door and throws the
    /// lever, then the bear joins the rabbit at the exit.
    /// </summary>
    public class BedroomPuzzleTests
    {
        const float k_Timeout = 8f;
        static readonly WaitForSeconds s_Settle = new WaitForSeconds(0.3f);

        [UnityTest]
        public IEnumerator BothPlushiesCanEscapeTheBedroom()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;

            var room = Object.FindAnyObjectByType<RoomController>();
            Assert.IsNotNull(room, "Main scene has no bedroom; run Game > Build Bedroom Greybox.");
            Object.FindAnyObjectByType<PlushieRoster>().enabled = false; // we drive the plushies ourselves

            PlushieController bear = null, rabbit = null;
            foreach (var plushie in room.GetComponentsInChildren<PlushieController>())
            {
                if (plushie.DisplayName == "Bear")
                    bear = plushie;
                else if (plushie.DisplayName == "Rabbit")
                    rabbit = plushie;
            }
            Assert.IsNotNull(bear);
            Assert.IsNotNull(rabbit);
            var door = room.GetComponentInChildren<Door>();

            // 1. The bear walks onto the plate and holds the door open.
            yield return WalkTo(bear, -3f);
            yield return WaitUntil(() => door.IsOpen, "door never opened with the bear on the plate");

            // 2. The rabbit hops over the bear and walks to just before the shelf.
            yield return WalkTo(rabbit, -5.5f);
            rabbit.MoveInput = 1f;
            rabbit.RequestJump();
            yield return WaitUntil(() => rabbit.transform.position.x > -1f, "rabbit never got over the bear");
            yield return WalkTo(rabbit, 3f);

            // 3. The rabbit jumps onto the shelf and throws the lever.
            rabbit.MoveInput = 1f;
            rabbit.RequestJump();
            yield return WaitUntil(() => rabbit.transform.position.y > 2f, "rabbit never reached the shelf");
            yield return WalkTo(rabbit, 6f);

            // 4. The bear steps off the plate: the lever keeps the door open, and the bear walks through.
            yield return WalkTo(bear, 9f);
            Assert.IsTrue(door.IsOpen, "door closed after the bear left the plate; the lever should hold it");

            // 5. The rabbit joins the bear at the exit.
            yield return WalkTo(rabbit, 9f);
            yield return WaitUntil(() => room.IsComplete, "room never completed");
        }

        static IEnumerator WalkTo(PlushieController plushie, float x)
        {
            var deadline = Time.time + k_Timeout;
            while (Mathf.Abs(plushie.transform.position.x - x) > 0.15f)
            {
                Assert.Less(Time.time, deadline, plushie.DisplayName + " never reached x=" + x + " (at " + plushie.transform.position + ")");
                plushie.MoveInput = Mathf.Sign(x - plushie.transform.position.x);
                yield return null;
            }
            plushie.MoveInput = 0f;
            yield return s_Settle;
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, string failure)
        {
            var deadline = Time.time + k_Timeout;
            while (!condition())
            {
                Assert.Less(Time.time, deadline, failure);
                yield return null;
            }
        }
    }
}
