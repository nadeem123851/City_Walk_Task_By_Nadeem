using NUnit.Framework;
using UnityEngine;

namespace CityWalk.Tests
{
    public class KioskRulesTests
    {
        static readonly Vector2 Kiosk = new Vector2(17f, -17f);

        static KioskRules NewRules() => new KioskRules(Kiosk, Vector2.zero);

        /// <summary>Walks in 0.25 m steps; returns true if the coffee became missed on the way.</summary>
        static bool Walk(KioskRules rules, Vector2 from, Vector2 to, bool coffee = false, bool active = true)
        {
            bool missed = false;
            Vector2 p = from;
            while ((to - p).magnitude > 1e-4f)
            {
                p = Vector2.MoveTowards(p, to, 0.25f);
                missed |= rules.Step(p, coffee, active);
            }
            return missed;
        }

        [Test]
        public void Button_ShowsOnlyWithinElevenMetres()
        {
            var rules = NewRules();
            rules.Step(new Vector2(5f, -17f), false, true);     // 12 m away
            Assert.IsFalse(rules.ButtonVisible);
            rules.Step(new Vector2(7f, -17f), false, true);     // 10 m away
            Assert.IsTrue(rules.ButtonVisible);
        }

        [Test]
        public void Button_HiddenOnceCoffeeTaken_OrOutsideLaps()
        {
            var rules = NewRules();
            rules.Step(Kiosk, true, true);
            Assert.IsFalse(rules.ButtonVisible, "coffee already taken");
            rules.Step(Kiosk, false, false);
            Assert.IsFalse(rules.ButtonVisible, "not in the lap phase");
        }

        [Test]
        public void WalkingOnPastTheKiosk_WithoutTapping_IsMissed()
        {
            var rules = NewRules();
            Assert.IsFalse(Walk(rules, new Vector2(-10f, -17f), Kiosk));
            Assert.IsTrue(Walk(rules, Kiosk, new Vector2(17f, 0f)), "walked on north up the east road");
            Assert.IsTrue(rules.Missed);
            Assert.IsFalse(rules.ButtonVisible);
        }

        [Test]
        public void TappingCoffee_ClearsMissed_AndIsNeverMissedAfter()
        {
            var rules = NewRules();
            Walk(rules, new Vector2(-10f, -17f), Kiosk);
            Walk(rules, Kiosk, new Vector2(17f, 0f));
            Assert.IsTrue(rules.Missed);

            // Go back, tap, walk on.
            Walk(rules, new Vector2(17f, 0f), Kiosk);
            rules.OnCoffeeTaken();
            Assert.IsFalse(rules.Missed);
            Assert.IsFalse(Walk(rules, Kiosk, new Vector2(17f, 10f), coffee: true));
        }

        [Test]
        public void ApproachingThenTurningBack_IsNotMissed()
        {
            var rules = NewRules();
            Walk(rules, new Vector2(-10f, -17f), new Vector2(10f, -17f));
            Assert.IsTrue(rules.Seen);
            Assert.IsFalse(Walk(rules, new Vector2(10f, -17f), new Vector2(-10f, -17f)), "walking away backwards is wrong way, not a missed coffee");
        }

        [Test]
        public void NewLap_ResetsSeenAndMissed()
        {
            var rules = NewRules();
            Walk(rules, new Vector2(-10f, -17f), Kiosk);
            Walk(rules, Kiosk, new Vector2(17f, 0f));
            Assert.IsTrue(rules.Missed);
            rules.OnLapCompleted();
            Assert.IsFalse(rules.Seen);
            Assert.IsFalse(rules.Missed);
        }
    }
}
