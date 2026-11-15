using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Ground ring and bobbing beacon that marks where to go. Visuals taken from the starter's
    /// CheckpointRoute; Phase 5 replaces them with the gem and light beam from the video.
    /// </summary>
    public class RouteMarker : MonoBehaviour
    {
        static readonly Color MarkerColor = new Color(0.98f, 0.82f, 0.25f);

        Transform beacon;

        public static RouteMarker Create(Transform parent, string name, Vector3 position, float radius)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            MaterialFactory.Primitive(PrimitiveType.Cylinder, "Ring", go.transform, new Vector3(0f, 0.05f, 0f),
                new Vector3(radius * 2f, 0.02f, radius * 2f), MarkerColor, false, 0.6f);
            var beacon = MaterialFactory.Primitive(PrimitiveType.Cylinder, "Beacon", go.transform, new Vector3(0f, 3f, 0f),
                new Vector3(0.25f, 3f, 0.25f), MarkerColor, false, 1.2f);

            var marker = go.AddComponent<RouteMarker>();
            marker.beacon = beacon.transform;
            return marker;
        }

        void Update()
        {
            if (beacon != null)
                beacon.localScale = new Vector3(0.25f, 3f + Mathf.Sin(Time.time * 3f) * 0.2f, 0.25f);
        }
    }
}
