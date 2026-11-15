using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CityWalk.Tests
{
    public class LapTrackerTests
    {
        // The park loop in Unity coordinates (x, z): road centre lines at +/-17 m round a park at the origin.
        static readonly Vector2 Start = new Vector2(-10f, -17f);
        // Laps end 1 m past the start line so rounding can't leave the turn a hair short.
        static readonly Vector2 PastStart = new Vector2(-9f, -17f);
        static readonly Vector2[] LoopCorners =
        {
            new Vector2(17f, -17f),   // kiosk junction, south-east
            new Vector2(17f, 17f),
            new Vector2(-17f, 17f),
            new Vector2(-17f, -17f),
        };

        const float WalkSpeed = 3.2f;
        const float StepLength = 0.25f;

        /// <summary>Walks a polyline in small steps, like frames, and collects what the tracker reports.</summary>
        static List<LapStepResult> Walk(LapTracker tracker, Vector2 from, IEnumerable<Vector2> points)
        {
            var results = new List<LapStepResult>();
            Vector2 pos = from;
            foreach (var target in points)
            {
                while ((target - pos).magnitude > 1e-4f)
                {
                    pos = Vector2.MoveTowards(pos, target, StepLength);
                    var r = tracker.Step(pos);
                    if (r != LapStepResult.None) results.Add(r);
                }
            }
            return results;
        }

        static IEnumerable<Vector2> OneLapForward()
        {
            foreach (var c in LoopCorners) yield return c;
            yield return PastStart;
        }

        static IEnumerable<Vector2> OneLapBackward()
        {
            for (int i = LoopCorners.Length - 1; i >= 0; i--) yield return LoopCorners[i];
            yield return Start;
        }

        [Test]
        public void ThreeForwardLaps_WithCoffee_AllCount()
        {
            var tracker = new LapTracker(Vector2.zero, Start, 3);
            for (int lap = 1; lap <= 3; lap++)
            {
                Assert.AreEqual(lap, tracker.CurrentLap);
                tracker.CoffeeThisLap = true;
                var results = Walk(tracker, lap == 1 ? Start : PastStart, OneLapForward());
                CollectionAssert.AreEqual(new[] { LapStepResult.LapCompleted }, results, $"lap {lap}");
                Assert.IsFalse(tracker.CoffeeThisLap, "coffee resets after a counted lap");
            }
            Assert.AreEqual(3, tracker.LapsCompleted);
            Assert.IsTrue(tracker.Finished);
            Assert.AreEqual(3, tracker.CurrentLap, "lap counter stays at 3/3 once finished");
        }

        [Test]
        public void ForwardLap_WithoutCoffee_DoesNotCount()
        {
            var tracker = new LapTracker(Vector2.zero, Start, 3);
            var results = Walk(tracker, Start, OneLapForward());
            CollectionAssert.AreEqual(new[] { LapStepResult.LapRejectedNoCoffee }, results);
            Assert.AreEqual(0, tracker.LapsCompleted);

            // Taking the coffee on the next time round makes that lap count.
            tracker.CoffeeThisLap = true;
            results = Walk(tracker, PastStart, OneLapForward());
            CollectionAssert.AreEqual(new[] { LapStepResult.LapCompleted }, results);
            Assert.AreEqual(1, tracker.LapsCompleted);
        }

        [Test]
        public void WalkingTheLoopBackwards_NeverCountsALap()
        {
            var tracker = new LapTracker(Vector2.zero, Start, 3);
            tracker.CoffeeThisLap = true;
            var results = Walk(tracker, Start, OneLapBackward());
            results.AddRange(Walk(tracker, Start, OneLapBackward()));
            CollectionAssert.IsEmpty(results);
            Assert.AreEqual(0, tracker.LapsCompleted);
            Assert.Less(tracker.Accumulated, 0f);
        }

        [Test]
        public void GoingBackwardsThenForwards_NeedsTheLostGroundBack()
        {
            var tracker = new LapTracker(Vector2.zero, Start, 3);
            tracker.CoffeeThisLap = true;
            // Step back west 5 m along the south road, then walk a normal lap from there.
            var back = new Vector2(-15f, -17f);
            Walk(tracker, Start, new[] { back });
            var results = Walk(tracker, back, new[] { Start });
            CollectionAssert.IsEmpty(results, "getting back to the start line is not a lap");
            results = Walk(tracker, Start, OneLapForward());
            CollectionAssert.AreEqual(new[] { LapStepResult.LapCompleted }, results);
        }

        [Test]
        public void CuttingAcrossThePark_AndBack_DoesNotCountALap()
        {
            var tracker = new LapTracker(Vector2.zero, Start, 3);
            tracker.CoffeeThisLap = true;
            // South road to north road past the fountain (x = 4 keeps clear of the centre), then back again.
            var path = new[] { new Vector2(4f, -17f), new Vector2(4f, 17f), new Vector2(4f, -17f), Start };
            var results = Walk(tracker, Start, path);
            CollectionAssert.IsEmpty(results);
            Assert.AreEqual(0f, tracker.Accumulated, 0.01f, "out and back cancels out");
        }

        [Test]
        public void AlongSpeed_IsPositiveForwards_NegativeBackwards()
        {
            var tracker = new LapTracker(Vector2.zero, Start, 3);
            // On the south road the loop runs east.
            Assert.Greater(tracker.AlongSpeed(Start, new Vector2(WalkSpeed, 0f)), 0f);
            Assert.Less(tracker.AlongSpeed(Start, new Vector2(-WalkSpeed, 0f)), 0f);
            // On the east road it runs north.
            Assert.Greater(tracker.AlongSpeed(new Vector2(17f, 0f), new Vector2(0f, WalkSpeed)), 0f);
        }
    }

    public class WrongWayDetectorTests
    {
        const float Dt = 1f / 60f;

        static bool Run(WrongWayDetector d, float along, float seconds)
        {
            bool changed = false;
            for (float t = 0f; t < seconds; t += Dt) changed |= d.Step(along, Dt);
            return changed;
        }

        [Test]
        public void BriefStepBack_DoesNotWarn()
        {
            var d = new WrongWayDetector();
            Run(d, -3.2f, 0.3f);
            Assert.IsFalse(d.IsWrongWay);
            Assert.AreEqual(0, d.WrongTurns);
        }

        [Test]
        public void WalkingBackwards_WarnsAfterDelay_AndClearsWhenForwardAgain()
        {
            var d = new WrongWayDetector();
            Assert.IsTrue(Run(d, -3.2f, 0.5f));
            Assert.IsTrue(d.IsWrongWay);
            Assert.AreEqual(1, d.WrongTurns);

            Run(d, 3.2f, 0.2f);
            Assert.IsTrue(d.IsWrongWay, "does not clear on the first forward step");
            Run(d, 3.2f, 0.3f);
            Assert.IsFalse(d.IsWrongWay);
        }

        [Test]
        public void SlowShuffleBackwards_DoesNotWarn()
        {
            var d = new WrongWayDetector();
            Run(d, -0.5f, 2f);
            Assert.IsFalse(d.IsWrongWay);
        }

        [Test]
        public void EachSeparateWrongTurn_IsCounted()
        {
            var d = new WrongWayDetector();
            Run(d, -3.2f, 0.5f);
            Run(d, 3.2f, 0.5f);
            Run(d, -3.2f, 0.5f);
            Assert.AreEqual(2, d.WrongTurns);
        }
    }
}
