using System;
using UnityEngine;

namespace CityWalk
{
    public enum RoutePhase { Laps, ToOffice, Complete, Failed }

    /// <summary>
    /// Runs the morning route: three laps round the park with a coffee each lap, then on to the office.
    /// Owns the score (XP), the clock and the wrong-way state, and tells the rest of the game what
    /// happened through events, so the HUD never reaches into game logic.
    /// </summary>
    public class RouteDirector : MonoBehaviour
    {
        [Header("Route")]
        public int totalLaps = 3;
        public Vector3 parkCentre = Vector3.zero;
        [Tooltip("Coffee marker on the junction by the kiosk.")]
        public Vector3 kioskMarker = new Vector3(17f, 0f, -17f);
        [Tooltip("Office marker one block east of the kiosk.")]
        public Vector3 officeMarker = new Vector3(51f, 0f, -17f);
        public float arriveRadius = 2.5f;
        [Tooltip("Until the tap-to-grab button exists (Phase 4), walking this close to the kiosk marker takes the coffee.")]
        public float autoCoffeeRadius = 4f;
        public bool autoCoffee = true;

        [Header("XP")]
        public int lapXp = 20;
        public int coffeeXp = 50;
        public int arrivalXp = 40;

        public RoutePhase Phase { get; private set; } = RoutePhase.Laps;
        public int CurrentLap => laps != null ? laps.CurrentLap : 1;
        public int LapsCompleted => laps != null ? laps.LapsCompleted : 0;
        public int TotalLaps => totalLaps;
        public bool CoffeeThisLap => laps != null && laps.CoffeeThisLap;
        public bool IsWrongWay => wrongWay.IsWrongWay;
        public int WrongTurns => wrongWay.WrongTurns;
        public int Coffees { get; private set; }
        public int Xp { get; private set; }
        public float ElapsedTime { get; private set; }

        /// <summary>Lap number just finished and its time in seconds.</summary>
        public event Action<int, float> LapCompleted;
        /// <summary>A full turn was walked without the coffee, so it did not count.</summary>
        public event Action LapRejected;
        public event Action CoffeeGrabbed;
        public event Action<bool> WrongWayChanged;
        public event Action<RoutePhase> PhaseChanged;
        /// <summary>Amount and reason, for example (20, "Lap bonus").</summary>
        public event Action<int, string> XpGained;
        public event Action RouteCompleted;

        PlayerMover player;
        LapTracker laps;
        readonly WrongWayDetector wrongWay = new WrongWayDetector();
        float lapStartTime;
        RouteMarker kioskMarkerView, officeMarkerView;

        public void Begin(PlayerMover playerMover)
        {
            player = playerMover;
            laps = new LapTracker(Flat(parkCentre), Flat(player.transform.position), totalLaps);

            kioskMarkerView = RouteMarker.Create(transform, "Marker_Kiosk", kioskMarker, arriveRadius);
            officeMarkerView = RouteMarker.Create(transform, "Marker_Office", officeMarker, arriveRadius);
            SetPhase(RoutePhase.Laps);
        }

        /// <summary>Takes the coffee for this lap. Called by the kiosk interaction.</summary>
        public void GrabCoffee()
        {
            if (Phase != RoutePhase.Laps || laps.CoffeeThisLap) return;
            laps.CoffeeThisLap = true;
            Coffees++;
            AddXp(coffeeXp, "Coffee");
            CoffeeGrabbed?.Invoke();
            UpdateMarkers();
        }

        void Update()
        {
            if (player == null || Phase == RoutePhase.Complete || Phase == RoutePhase.Failed) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            ElapsedTime += dt;

            Vector2 pos = Flat(player.transform.position);
            Vector2 vel = Flat(player.Velocity);
            float along;

            if (Phase == RoutePhase.Laps)
            {
                if (autoCoffee && !laps.CoffeeThisLap && Vector2.Distance(pos, Flat(kioskMarker)) < autoCoffeeRadius)
                    GrabCoffee();

                switch (laps.Step(pos))
                {
                    case LapStepResult.LapCompleted: OnLapCompleted(); break;
                    case LapStepResult.LapRejectedNoCoffee: LapRejected?.Invoke(); break;
                }
                along = laps.AlongSpeed(pos, vel);
            }
            else
            {
                Vector2 toOffice = Flat(officeMarker) - pos;
                float dist = toOffice.magnitude;
                if (dist < arriveRadius)
                {
                    Arrive();
                    return;
                }
                along = Vector2.Dot(vel, toOffice / Mathf.Max(dist, 0.001f));
            }

            if (wrongWay.Step(along, dt))
                WrongWayChanged?.Invoke(wrongWay.IsWrongWay);
        }

        void OnLapCompleted()
        {
            int lap = laps.LapsCompleted;
            float lapTime = ElapsedTime - lapStartTime;
            lapStartTime = ElapsedTime;
            AddXp(lapXp, "Lap bonus");
            LapCompleted?.Invoke(lap, lapTime);

            if (laps.Finished) SetPhase(RoutePhase.ToOffice);
            else UpdateMarkers();
        }

        void Arrive()
        {
            if (wrongWay.Suppress()) WrongWayChanged?.Invoke(false);
            AddXp(arrivalXp, "Arrived");
            player.MovementLocked = true;
            SetPhase(RoutePhase.Complete);
            RouteCompleted?.Invoke();
        }

        void AddXp(int amount, string reason)
        {
            Xp += amount;
            XpGained?.Invoke(amount, reason);
        }

        void SetPhase(RoutePhase phase)
        {
            Phase = phase;
            UpdateMarkers();
            PhaseChanged?.Invoke(phase);
        }

        void UpdateMarkers()
        {
            if (kioskMarkerView != null) kioskMarkerView.gameObject.SetActive(Phase == RoutePhase.Laps && !laps.CoffeeThisLap);
            if (officeMarkerView != null) officeMarkerView.gameObject.SetActive(Phase == RoutePhase.ToOffice);
        }

        static Vector2 Flat(Vector3 v) => new Vector2(v.x, v.z);

        /// <summary>Minutes and seconds, for example 1:07.</summary>
        public static string FormatTime(float seconds)
        {
            int s = Mathf.RoundToInt(seconds);
            return $"{s / 60}:{s % 60:00}";
        }
    }
}
