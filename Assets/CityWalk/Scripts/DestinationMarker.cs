using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Where to go next: a hovering, spinning yellow gem with a soft halo, a ring on the ground and a
    /// translucent light beam that can be seen over the rooftops. Unlit and shadowless, so it's cheap.
    /// </summary>
    public class DestinationMarker : MonoBehaviour
    {
        static readonly Color GemColor = new Color(1f, 0.890f, 0.478f);            // #FFE37A
        static readonly Color HaloColor = new Color(1f, 0.965f, 0.784f, 0.45f);     // #FFF6C8
        static readonly Color RingColor = new Color(1f, 0.941f, 0.659f);           // #FFF0A8
        static readonly Color BeamColor = new Color(1f, 0.914f, 0.541f, 0.2f);      // #FFE98A

        Transform gem, ring;
        MaterialPropertyBlock beamBlock;
        MeshRenderer beamRenderer;
        float phase;

        public static DestinationMarker Create(Transform parent, string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var m = go.AddComponent<DestinationMarker>();
            m.Build();
            return m;
        }

        void Build()
        {
            ring = GuidanceMeshes.Spawn("Ring", transform, GuidanceMeshes.Ring, MaterialFactory.GetUnlitColor(RingColor, false)).transform;
            ring.localPosition = new Vector3(0f, 0.12f, 0f);

            beamRenderer = GuidanceMeshes.Spawn("Beam", transform, GuidanceMeshes.Beam, MaterialFactory.GetUnlitColor(BeamColor, true));
            beamBlock = new MaterialPropertyBlock();

            gem = GuidanceMeshes.Spawn("Gem", transform, GuidanceMeshes.Octahedron, MaterialFactory.GetUnlitColor(GemColor, false)).transform;
            gem.localScale = new Vector3(1f, 1.5f, 1f);
            var halo = GuidanceMeshes.Spawn("Halo", gem, GuidanceMeshes.Octahedron, MaterialFactory.GetUnlitColor(HaloColor, true)).transform;
            halo.localScale = Vector3.one * 1.3f;

            phase = Random.value * 10f;
        }

        void Update()
        {
            float t = Time.time + phase;
            gem.localPosition = new Vector3(0f, 3f + Mathf.Sin(t * 2.4f) * 0.22f, 0f);
            gem.localRotation = Quaternion.Euler(0f, t * 1.6f * Mathf.Rad2Deg, 0f);
            ring.localScale = Vector3.one * (1f + Mathf.Sin(t * 3f) * 0.035f);

            var c = BeamColor;
            c.a = 0.19f + Mathf.Sin(t * 3f) * 0.035f;
            beamBlock.SetColor("_BaseColor", c);
            beamRenderer.SetPropertyBlock(beamBlock);
        }
    }
}
