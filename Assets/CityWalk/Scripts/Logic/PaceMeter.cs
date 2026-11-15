namespace CityWalk
{
    public enum PaceStepResult { None, Nudged, BriskBonus }

    /// <summary>
    /// The rush hour pace meter, as in the web preview: walking slower than a brisk pace for 2 s raises the
    /// "Pick up the pace" nudge, and every 6 s of brisk walking earns a bonus. Plain C# so it can be tested in
    /// Edit Mode.
    /// </summary>
    public sealed class PaceMeter
    {
        /// <summary>At or above this speed (m/s) the player counts as brisk.</summary>
        public float BriskSpeed = 4.2f;
        /// <summary>Seconds slower than brisk before the nudge shows.</summary>
        public float NudgeDelay = 2f;
        /// <summary>Seconds of unbroken brisk walking per bonus.</summary>
        public float BonusInterval = 6f;
        /// <summary>Slow spells shorter than this (s), like the dip when turning a corner, do not break a brisk streak.</summary>
        public float DipGrace = 0.5f;
        /// <summary>The meter waits until the player first moves faster than this (m/s), so standing at the start is not nudged.</summary>
        public float StartSpeed = 1f;

        public bool IsNudging { get; private set; }
        public bool Started { get; private set; }
        public int Bonuses { get; private set; }

        float slowTime, briskTime;

        /// <summary>
        /// Advances the meter. <paramref name="active"/> is false outside rush hour and while another warning
        /// (wrong way, missed coffee) has the player's attention; the meter then resets.
        /// </summary>
        public PaceStepResult Step(float speed, float dt, bool active)
        {
            if (speed > StartSpeed) Started = true;
            if (!active || !Started)
            {
                Reset();
                return PaceStepResult.None;
            }

            if (speed < BriskSpeed)
            {
                slowTime += dt;
                if (slowTime > DipGrace) briskTime = 0f;
                if (slowTime > NudgeDelay && !IsNudging)
                {
                    IsNudging = true;
                    return PaceStepResult.Nudged;
                }
                return PaceStepResult.None;
            }

            slowTime = 0f;
            IsNudging = false;
            briskTime += dt;
            if (briskTime >= BonusInterval)
            {
                briskTime = 0f;
                Bonuses++;
                return PaceStepResult.BriskBonus;
            }
            return PaceStepResult.None;
        }

        /// <summary>Clears the nudge and the timers, keeping the bonus count.</summary>
        public void Reset()
        {
            IsNudging = false;
            slowTime = briskTime = 0f;
        }
    }
}
