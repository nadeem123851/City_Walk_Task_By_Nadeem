using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// When to offer the coffee button, and when the player has walked past the kiosk without it.
    /// The button shows within <see cref="ShowRadius"/> of the kiosk marker. Once the player has been
    /// that close, walking on more than <see cref="MissRadius"/> away, further along the loop, counts
    /// as a missed coffee. Plain C# so it can be tested in Edit Mode.
    /// </summary>
    public sealed class KioskRules
    {
        public float ShowRadius = 11f;
        public float MissRadius = 11.5f;
        /// <summary>How far past the kiosk, in degrees round the park, still counts as walking on from it.</summary>
        public float MissAheadMaxDegrees = 100f;

        public Vector2 Kiosk { get; }
        public Vector2 ParkCentre { get; }

        public bool ButtonVisible { get; private set; }
        /// <summary>The player has been close enough to see the button this lap.</summary>
        public bool Seen { get; private set; }
        public bool Missed { get; private set; }

        public KioskRules(Vector2 kiosk, Vector2 parkCentre)
        {
            Kiosk = kiosk;
            ParkCentre = parkCentre;
        }

        /// <summary>
        /// Call every frame. <paramref name="active"/> is false outside the lap phase.
        /// Returns true on the frame the coffee becomes missed.
        /// </summary>
        public bool Step(Vector2 player, bool coffeeTaken, bool active)
        {
            float distance = Vector2.Distance(player, Kiosk);
            bool near = active && !coffeeTaken && distance < ShowRadius;
            ButtonVisible = near;
            if (near) Seen = true;

            if (!active || coffeeTaken || Missed || !Seen || distance <= MissRadius) return false;
            if (!IsAheadOfKiosk(player)) return false;

            Missed = true;
            return true;
        }

        /// <summary>The coffee was taken: no longer missed.</summary>
        public void OnCoffeeTaken() => Missed = false;

        /// <summary>A new lap starts: the kiosk has to be passed again.</summary>
        public void OnLapCompleted()
        {
            Seen = false;
            Missed = false;
        }

        /// <summary>True when the player is further round the loop (anticlockwise) than the kiosk.</summary>
        public bool IsAheadOfKiosk(Vector2 player)
        {
            Vector2 k = Kiosk - ParkCentre, p = player - ParkCentre;
            float delta = Mathf.DeltaAngle(Mathf.Atan2(k.y, k.x) * Mathf.Rad2Deg, Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg);
            return delta > 0f && delta < MissAheadMaxDegrees;
        }
    }
}
