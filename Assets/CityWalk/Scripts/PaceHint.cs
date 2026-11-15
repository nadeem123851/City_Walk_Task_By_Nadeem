using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// The pace pill: "Rush hour: walk briskly" on laps 1 and 2, "Rush hour is over: normal pace" after.
    /// Hidden once the route is over.
    /// </summary>
    public class PaceHint : MonoBehaviour
    {
        public const string RushHourText = "Rush hour: walk briskly";
        public const string NormalPaceText = "Rush hour is over: normal pace";

        RouteDirector route;
        BasicHud hud;

        public void Init(RouteDirector director, BasicHud basicHud)
        {
            route = director;
            hud = basicHud;
            Refresh();
        }

        void Update()
        {
            if (route != null) Refresh();
        }

        void Refresh() => hud.SetHint(TextFor(route));

        /// <summary>The pill text for the route's current state, or null to hide it.</summary>
        public static string TextFor(RouteDirector route)
        {
            if (route.Phase == RoutePhase.Complete || route.Phase == RoutePhase.Failed) return null;
            return route.IsRushHour ? RushHourText : NormalPaceText;
        }
    }
}
