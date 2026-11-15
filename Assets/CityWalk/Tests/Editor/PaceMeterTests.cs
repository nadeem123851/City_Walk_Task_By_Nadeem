using NUnit.Framework;

namespace CityWalk.Tests
{
    public class PaceMeterTests
    {
        const float Dt = 0.1f;

        /// <summary>Steps the meter for <paramref name="seconds"/>; returns how many times each result came up.</summary>
        static (int nudges, int bonuses) Run(PaceMeter meter, float speed, float seconds, bool active = true)
        {
            int nudges = 0, bonuses = 0;
            for (float t = 0f; t < seconds - 1e-4f; t += Dt)
            {
                var r = meter.Step(speed, Dt, active);
                if (r == PaceStepResult.Nudged) nudges++;
                if (r == PaceStepResult.BriskBonus) bonuses++;
            }
            return (nudges, bonuses);
        }

        [Test]
        public void StandingAtTheStart_IsNotNudged()
        {
            var meter = new PaceMeter();
            Run(meter, 0f, 5f);
            Assert.IsFalse(meter.IsNudging);
        }

        [Test]
        public void SlowForMoreThanTwoSeconds_NudgesOnce()
        {
            var meter = new PaceMeter();
            Run(meter, 3.2f, 1.9f);
            Assert.IsFalse(meter.IsNudging, "not yet at 1.9 s");
            var (nudges, _) = Run(meter, 3.2f, 3f);
            Assert.IsTrue(meter.IsNudging);
            Assert.AreEqual(1, nudges, "the nudge sound plays once, not every frame");
        }

        [Test]
        public void GoingBrisk_ClearsTheNudge()
        {
            var meter = new PaceMeter();
            Run(meter, 3.2f, 3f);
            meter.Step(5.5f, Dt, true);
            Assert.IsFalse(meter.IsNudging);
        }

        [Test]
        public void BriskWalking_EarnsABonusEverySixSeconds()
        {
            var meter = new PaceMeter();
            var (_, bonuses) = Run(meter, 5.5f, 13f);
            Assert.AreEqual(2, bonuses);
            Assert.AreEqual(2, meter.Bonuses);
        }

        [Test]
        public void SlowingDown_RestartsTheBonusTimer()
        {
            var meter = new PaceMeter();
            Run(meter, 5.5f, 5f);
            Run(meter, 3.2f, 1f);
            var (_, bonuses) = Run(meter, 5.5f, 5f);
            Assert.AreEqual(0, bonuses);
        }

        [Test]
        public void ShortDipAtACorner_KeepsTheBonusStreak()
        {
            var meter = new PaceMeter();
            Run(meter, 5.5f, 4f);
            Run(meter, 3.9f, 0.3f);
            var (_, bonuses) = Run(meter, 5.5f, 2.2f);
            Assert.AreEqual(1, bonuses);
        }

        [Test]
        public void Inactive_ClearsTheNudgeAndCountsNothing()
        {
            var meter = new PaceMeter();
            Run(meter, 3.2f, 3f);
            var (nudges, bonuses) = Run(meter, 5.5f, 10f, false);
            Assert.IsFalse(meter.IsNudging);
            Assert.AreEqual(0, nudges + bonuses);
        }
    }
}
