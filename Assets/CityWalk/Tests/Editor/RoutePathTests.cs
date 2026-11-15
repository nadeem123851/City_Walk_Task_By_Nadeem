using NUnit.Framework;
using UnityEngine;

namespace CityWalk.Tests
{
    public class RoutePathTests
    {
        static RoutePath Square(bool closed)
        {
            // 10 x 10 square, walked anticlockwise from the origin.
            return new RoutePath(new[]
            {
                new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 10), new Vector2(0, 10), new Vector2(0, 0),
            }, closed);
        }

        [Test]
        public void Length_IsSumOfSegments()
        {
            Assert.AreEqual(40f, Square(true).Length, 1e-4f);
        }

        [Test]
        public void PointAt_ReturnsPositionAndDirection()
        {
            var path = Square(true);
            var p = path.PointAt(15f, out var dir);
            Assert.AreEqual(10f, p.x, 1e-4f);
            Assert.AreEqual(5f, p.y, 1e-4f);
            Assert.AreEqual(Vector2.up, dir);
        }

        [Test]
        public void ClosedPath_WrapsDistances_OpenPathClamps()
        {
            Assert.AreEqual(new Vector2(5f, 0f), Square(true).PointAt(45f));
            Assert.AreEqual(new Vector2(0f, 0f), Square(false).PointAt(45f));
            Assert.AreEqual(new Vector2(0f, 0f), Square(false).PointAt(-5f));
        }

        [Test]
        public void ClosestDistance_FindsArcLengthOfNearestPoint()
        {
            var path = Square(true);
            Assert.AreEqual(5f, path.ClosestDistance(new Vector2(5f, -3f)), 1e-4f);
            Assert.AreEqual(25f, path.ClosestDistance(new Vector2(5f, 12f)), 1e-4f);
        }

        [Test]
        public void DistanceAhead_WrapsOnClosedPath()
        {
            var path = Square(true);
            Assert.AreEqual(10f, path.DistanceAhead(35f, 5f), 1e-4f);
            Assert.AreEqual(-30f, Square(false).DistanceAhead(35f, 5f), 1e-4f);
        }

        [Test]
        public void RoundCorners_StartsAndEndsRadiusFromEachCorner()
        {
            var corners = new[] { new Vector2(10, -10), new Vector2(10, 10), new Vector2(-10, 10), new Vector2(-10, -10) };
            var pts = RoutePath.RoundCorners(corners, 2f, 6);
            Assert.AreEqual(4 * 7, pts.Count);
            Assert.AreEqual(new Vector2(8f, -10f), pts[0], "curve into the first corner starts 2 m before it");
            Assert.AreEqual(new Vector2(10f, -8f), pts[6], "and ends 2 m after it");
        }
    }
}
