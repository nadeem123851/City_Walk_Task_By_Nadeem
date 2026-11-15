namespace CityWalk
{
    /// <summary>
    /// Turns "speed along the route" into a wrong-way warning with a little hysteresis, so a quick
    /// step back or a wobble doesn't flash the warning. Plain C# so it can be tested in Edit Mode.
    /// </summary>
    public sealed class WrongWayDetector
    {
        /// <summary>Going backwards faster than this (m/s) counts as wrong way.</summary>
        public float BackwardSpeed = 0.6f;
        /// <summary>How long (s) the player must go backwards before the warning shows.</summary>
        public float WarnDelay = 0.4f;
        /// <summary>Going forwards faster than this (m/s) counts as back on track.</summary>
        public float ForwardSpeed = 0.4f;
        /// <summary>How long (s) the player must go forwards before the warning clears.</summary>
        public float ClearDelay = 0.35f;

        public bool IsWrongWay { get; private set; }
        public int WrongTurns { get; private set; }

        float backwardTime, forwardTime;

        /// <summary>Returns true when <see cref="IsWrongWay"/> changed this step.</summary>
        public bool Step(float alongSpeed, float dt)
        {
            bool before = IsWrongWay;
            if (alongSpeed < -BackwardSpeed)
            {
                backwardTime += dt;
                forwardTime = 0f;
                if (backwardTime > WarnDelay && !IsWrongWay)
                {
                    IsWrongWay = true;
                    WrongTurns++;
                }
            }
            else if (alongSpeed > ForwardSpeed)
            {
                forwardTime += dt;
                backwardTime = 0f;
                if (forwardTime > ClearDelay) IsWrongWay = false;
            }
            return IsWrongWay != before;
        }

        /// <summary>Clears the warning without counting anything, for moments when direction doesn't matter.</summary>
        public bool Suppress()
        {
            bool changed = IsWrongWay;
            IsWrongWay = false;
            backwardTime = forwardTime = 0f;
            return changed;
        }
    }
}
