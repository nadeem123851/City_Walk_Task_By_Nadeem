using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Shows the route on the starter HUD: "Lap X/3" and the instruction banner.
    /// Listens to RouteDirector events and reads its state; never changes game state.
    /// Plain for now; Phase 7 replaces it with the styled HudController.
    /// </summary>
    public class RouteHudView : MonoBehaviour
    {
        const float LapMessageSeconds = 2.6f;

        RouteDirector route;
        BasicHud hud;
        string lapMessageTitle, lapMessageBody;
        float lapMessageUntil = -1f;

        public void Init(RouteDirector director, BasicHud basicHud)
        {
            route = director;
            hud = basicHud;
            hud.SetCounterLabel("Lap");

            route.LapCompleted += (lap, seconds) =>
            {
                lapMessageTitle = $"Lap {lap} complete";
                lapMessageBody = $"Lap time {RouteDirector.FormatTime(seconds)}  ·  +{route.lapXp} XP";
                lapMessageUntil = Time.time + LapMessageSeconds;
            };
            route.RouteCompleted += () => hud.ShowComplete();
            Refresh();
        }

        void Update()
        {
            if (route != null) Refresh();
        }

        void Refresh()
        {
            hud.SetCounter(route.CurrentLap, route.TotalLaps);

            string title, body;
            if (route.Phase == RoutePhase.Complete) { title = "Arrived"; body = $"You reached the office in {RouteDirector.FormatTime(route.ElapsedTime)}.  {route.Xp} XP"; }
            else if (route.IsWrongWay) { title = "Wrong way"; body = "Turn around and follow the route round the park."; }
            else if (Time.time < lapMessageUntil) { title = lapMessageTitle; body = lapMessageBody; }
            else if (route.Phase == RoutePhase.ToOffice) { title = "Get to the office"; body = "Laps done. Head east to the yellow marker."; }
            else if (!route.CoffeeThisLap) { title = "Head for the yellow marker"; body = "The coffee kiosk is ahead at the next crossing."; }
            else if (route.CurrentLap < route.TotalLaps) { title = "Coffee grabbed"; body = $"Keep going round the park to finish lap {route.CurrentLap}."; }
            else { title = "Last lap"; body = "Finish the lap, then head to the office."; }

            hud.SetInstruction(title, body);
        }
    }
}
