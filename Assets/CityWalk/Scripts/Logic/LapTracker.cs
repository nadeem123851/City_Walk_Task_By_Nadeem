using UnityEngine;

namespace CityWalk
{
    /// <summary>What happened during one <see cref="LapTracker.Step"/>.</summary>
    public enum LapStepResult
    {
        None,
        /// <summary>A full turn was finished with the coffee taken: the lap counts.</summary>
        LapCompleted,
        /// <summary>A full turn was finished without the coffee: the lap does not count.</summary>
        LapRejectedNoCoffee,
    }

    /// <summary>
    /// Counts laps round the park by adding up the angle the player sweeps round its centre.
    /// Walking the loop anticlockwise (seen from above: east along the south road, then north) adds angle,
    /// walking backwards takes it away, so cutting across the park or doubling back never earns a lap.
    /// A lap only counts if the coffee was taken during it. Plain C# so it can be tested in Edit Mode.
    /// </summary>
    public sealed class LapTracker
    {
        const float FullTurn = Mathf.PI * 2f;

        public int TotalLaps { get; }
        public Vector2 Centre { get; }

        /// <summary>Laps that counted so far.</summary>
        public int LapsCompleted { get; private set; }

        /// <summary>The lap being walked now, 1-based. Stays at TotalLaps once all laps are done.</summary>
        public int CurrentLap => Mathf.Min(LapsCompleted + 1, TotalLaps);

        public bool Finished => LapsCompleted >= TotalLaps;

        /// <summary>Set when the coffee is taken; cleared each time a lap counts.</summary>
        public bool CoffeeThisLap { get; set; }

        /// <summary>Total angle swept in the forward direction, in radians.</summary>
        public float Accumulated { get; private set; }

        float previousAngle;

        public LapTracker(Vector2 centre, Vector2 startPosition, int totalLaps = 3)
        {
            Centre = centre;
            TotalLaps = Mathf.Max(1, totalLaps);
            previousAngle = AngleOf(startPosition);
        }

        /// <summary>Advance with the player's new position (x, z).</summary>
        public LapStepResult Step(Vector2 position)
        {
            if (Finished) return LapStepResult.None;

            float angle = AngleOf(position);
            Accumulated += Mathf.DeltaAngle(previousAngle * Mathf.Rad2Deg, angle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            previousAngle = angle;

            if (Accumulated < FullTurn * (LapsCompleted + 1)) return LapStepResult.None;

            if (!CoffeeThisLap)
            {
                // Same rule as the web preview: the turn is taken back, so another full turn is needed.
                Accumulated -= FullTurn;
                return LapStepResult.LapRejectedNoCoffee;
            }

            LapsCompleted++;
            CoffeeThisLap = false;
            return LapStepResult.LapCompleted;
        }

        /// <summary>Speed along the forward direction of the loop. Negative means walking the wrong way.</summary>
        public float AlongSpeed(Vector2 position, Vector2 velocity)
        {
            Vector2 r = position - Centre;
            float len = r.magnitude;
            if (len < 0.001f) return 0f;
            Vector2 forward = new Vector2(-r.y, r.x) / len;   // anticlockwise tangent
            return Vector2.Dot(velocity, forward);
        }

        float AngleOf(Vector2 position)
        {
            Vector2 r = position - Centre;
            return Mathf.Atan2(r.y, r.x);
        }
    }
}
