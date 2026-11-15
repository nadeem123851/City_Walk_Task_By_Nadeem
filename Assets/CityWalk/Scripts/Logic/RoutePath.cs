using System.Collections.Generic;
using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// A polyline on the ground (x, z) measured by distance along it. Can be a closed loop (distances
    /// wrap) or an open line (distances clamp). Used to place guidance chevrons along the route.
    /// Plain C# so it can be tested in Edit Mode.
    /// </summary>
    public sealed class RoutePath
    {
        readonly List<Vector2> points = new List<Vector2>();
        readonly List<float> cumulative = new List<float>();

        public bool Closed { get; }
        public float Length { get; private set; }
        public IReadOnlyList<Vector2> Points => points;

        public RoutePath(IEnumerable<Vector2> pts, bool closed)
        {
            Closed = closed;
            SetPoints(pts);
        }

        /// <summary>Replaces the points, for paths that change every frame (for example back to the kiosk).</summary>
        public void SetPoints(IEnumerable<Vector2> pts)
        {
            points.Clear();
            points.AddRange(pts);
            cumulative.Clear();
            cumulative.Add(0f);
            for (int i = 1; i < points.Count; i++)
                cumulative.Add(cumulative[i - 1] + Vector2.Distance(points[i - 1], points[i]));
            Length = cumulative.Count > 0 ? cumulative[cumulative.Count - 1] : 0f;
        }

        /// <summary>Point and travel direction at distance s along the path.</summary>
        public Vector2 PointAt(float s, out Vector2 direction)
        {
            direction = Vector2.up;
            if (points.Count == 0) return Vector2.zero;
            if (points.Count == 1 || Length <= 0f) return points[0];

            s = Closed ? Mathf.Repeat(s, Length) : Mathf.Clamp(s, 0f, Length);
            int i = 1;
            while (i < points.Count - 1 && cumulative[i] < s) i++;
            Vector2 a = points[i - 1], b = points[i];
            float segment = cumulative[i] - cumulative[i - 1];
            float t = segment > 0f ? (s - cumulative[i - 1]) / segment : 0f;
            if ((b - a).sqrMagnitude > 1e-8f) direction = (b - a).normalized;
            return Vector2.Lerp(a, b, t);
        }

        public Vector2 PointAt(float s) => PointAt(s, out _);

        /// <summary>Distance along the path of the point closest to p.</summary>
        public float ClosestDistance(Vector2 p)
        {
            float best = float.MaxValue, bestS = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 a = points[i - 1], ab = points[i] - a;
                float len2 = ab.sqrMagnitude;
                float t = len2 > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2) : 0f;
                float d = (a + ab * t - p).sqrMagnitude;
                if (d < best - 1e-6f)
                {
                    best = d;
                    bestS = cumulative[i - 1] + t * Mathf.Sqrt(len2);
                }
            }
            return bestS;
        }

        /// <summary>How far ahead of distance <paramref name="from"/> the distance <paramref name="to"/> is, following the path.</summary>
        public float DistanceAhead(float from, float to)
        {
            return Closed ? Mathf.Repeat(to - from, Length) : to - from;
        }

        /// <summary>
        /// Turns a closed polygon into a point list with rounded corners: each corner is replaced by a
        /// quadratic curve starting and ending <paramref name="radius"/> metres either side of it.
        /// </summary>
        public static List<Vector2> RoundCorners(IList<Vector2> corners, float radius, int stepsPerCorner = 6)
        {
            var result = new List<Vector2>();
            int n = corners.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = corners[(i + n - 1) % n], c = corners[i], b = corners[(i + 1) % n];
                Vector2 p0 = c - (c - a).normalized * radius;
                Vector2 p1 = c + (b - c).normalized * radius;
                for (int k = 0; k <= stepsPerCorner; k++)
                {
                    float t = k / (float)stepsPerCorner, u = 1f - t;
                    result.Add(u * u * p0 + 2f * u * t * c + t * t * p1);
                }
            }
            return result;
        }
    }
}
