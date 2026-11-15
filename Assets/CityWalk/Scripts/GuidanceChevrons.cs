using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Green chevrons on the ground that flow towards the next target. Laps: along the park loop to the
    /// kiosk, or round the loop once the coffee is taken. Missed coffee: straight back to the kiosk.
    /// After lap 3: along the road to the office. They fade in near the player, fade out at the far end,
    /// and stop just short of the target. A fixed pool of nine; nothing is created while playing.
    /// </summary>
    public class GuidanceChevrons : MonoBehaviour
    {
        static readonly Color ChevronColor = new Color(0.525f, 0.902f, 0.682f);   // #86E6AE

        public int count = 9;
        public float spacing = 2.3f;
        public float flowSpeed = 2.6f;
        public float startAhead = 2.2f;
        public float stopShort = 2.4f;
        public float cornerRadius = 3.5f;
        const float MaxOpacity = 0.9f;

        RouteDirector route;
        Transform player;
        RoutePath loopPath, officePath, backToKioskPath;
        float kioskDistanceOnLoop;
        Transform[] chevrons;
        MeshRenderer[] renderers;
        MaterialPropertyBlock block;

        public static GuidanceChevrons Create(RouteDirector route, Transform player)
        {
            var go = new GameObject("GuidanceChevrons");
            go.transform.SetParent(route.transform, false);
            var g = go.AddComponent<GuidanceChevrons>();
            g.Init(route, player);
            return g;
        }

        void Init(RouteDirector director, Transform playerTransform)
        {
            route = director;
            player = playerTransform;

            Vector2 start = Flat(route.StartPosition), kiosk = Flat(route.kioskMarker), office = Flat(route.officeMarker);
            float r = Mathf.Abs(kiosk.x - route.parkCentre.x);   // the loop runs on the road centre lines round the park
            Vector2 c = Flat(route.parkCentre);
            var corners = new[] { c + new Vector2(r, -r), c + new Vector2(r, r), c + new Vector2(-r, r), c + new Vector2(-r, -r) };

            var loop = new System.Collections.Generic.List<Vector2> { start };
            loop.AddRange(RoutePath.RoundCorners(corners, cornerRadius));
            loop.Add(start);
            loopPath = new RoutePath(loop, true);
            kioskDistanceOnLoop = loopPath.ClosestDistance(kiosk);
            officePath = new RoutePath(new[] { start, office }, false);
            backToKioskPath = new RoutePath(new[] { start, kiosk }, false);

            var material = MaterialFactory.GetUnlitColor(ChevronColor, true);
            block = new MaterialPropertyBlock();
            chevrons = new Transform[count];
            renderers = new MeshRenderer[count];
            for (int i = 0; i < count; i++)
            {
                renderers[i] = GuidanceMeshes.Spawn("Chevron_" + i, transform, GuidanceMeshes.Chevron, material);
                chevrons[i] = renderers[i].transform;
            }
        }

        void Update()
        {
            if (route == null || player == null) return;

            bool show = route.Phase == RoutePhase.Laps || route.Phase == RoutePhase.ToOffice;
            Vector2 p = Flat(player.position);

            RoutePath path;
            float from = 0f, ahead = float.MaxValue;
            if (route.Phase == RoutePhase.Laps && route.IsCoffeeMissed)
            {
                backToKioskPath.SetPoints(new[] { p, Flat(route.kioskMarker) });
                path = backToKioskPath;
                ahead = path.Length;
            }
            else if (route.Phase == RoutePhase.ToOffice)
            {
                path = officePath;
                from = path.ClosestDistance(p);
                ahead = path.Length - from;
            }
            else
            {
                path = loopPath;
                from = path.ClosestDistance(p);
                if (!route.CoffeeThisLap) ahead = path.DistanceAhead(from, kioskDistanceOnLoop);
            }

            float cycle = Mathf.Repeat(Time.time * flowSpeed, spacing);
            for (int i = 0; i < count; i++)
            {
                float offset = startAhead + i * spacing + cycle;
                bool visible = show && offset < ahead - stopShort;
                if (renderers[i].enabled != visible) renderers[i].enabled = visible;
                if (!visible) continue;

                Vector2 q = path.PointAt(from + offset);
                Vector2 q2 = path.PointAt(from + offset + 0.6f);
                Vector2 dir = q2 - q;
                chevrons[i].position = new Vector3(q.x, GroundHeight(q) + 0.03f, q.y);
                if (dir.sqrMagnitude > 1e-6f)
                    chevrons[i].rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.y));

                // Fade in at the near end, out at the far end.
                float u = (i * spacing + cycle) / (count * spacing);
                float alpha = Mathf.SmoothStep(0f, 1f, u / 0.14f) * (1f - Mathf.SmoothStep(0f, 1f, (u - 0.72f) / 0.28f)) * MaxOpacity;
                var c = ChevronColor;
                c.a = alpha;
                block.SetColor("_BaseColor", c);
                renderers[i].SetPropertyBlock(block);
            }
        }

        /// <summary>Road surface is at 0; block pavements are raised 0.15 m.</summary>
        static float GroundHeight(Vector2 p)
        {
            const float pitch = 34f, halfRoad = 5f;
            float fx = Mathf.Repeat(p.x + pitch * 0.5f, pitch), fz = Mathf.Repeat(p.y + pitch * 0.5f, pitch);
            bool onBlock = fx > halfRoad && fx < pitch - halfRoad && fz > halfRoad && fz < pitch - halfRoad;
            return onBlock ? 0.15f : 0f;
        }

        static Vector2 Flat(Vector3 v) => new Vector2(v.x, v.z);
    }
}
