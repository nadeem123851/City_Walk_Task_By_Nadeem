using System.Collections.Generic;
using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Small procedural meshes for the guidance visuals, shapes taken from the web preview.
    /// They are drawn with culling off, so winding order does not matter.
    /// </summary>
    public static class GuidanceMeshes
    {
        static Mesh chevron, octahedron, ring, beam;

        /// <summary>Flat V on the ground pointing along +z, 2.5 m wide.</summary>
        public static Mesh Chevron
        {
            get
            {
                if (chevron != null) return chevron;
                const float hw = 1.25f, th = 0.34f;
                Vector3 T = new Vector3(0f, 0f, 0.5f), Ti = new Vector3(0f, 0f, 0.5f - th);
                Vector3 Lo = new Vector3(-hw, 0f, -0.45f), Li = new Vector3(-hw, 0f, -0.45f - th);
                Vector3 Ro = new Vector3(hw, 0f, -0.45f), Ri = new Vector3(hw, 0f, -0.45f - th);
                chevron = Build("Chevron",
                    new[] { T, Lo, Li, Ti, Ri, Ro },
                    new[] { 0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 5 });
                return chevron;
            }
        }

        /// <summary>Diamond gem, 1 m tall and wide.</summary>
        public static Mesh Octahedron
        {
            get
            {
                if (octahedron != null) return octahedron;
                const float r = 0.5f;
                var v = new[]
                {
                    new Vector3(0, r, 0), new Vector3(0, -r, 0),
                    new Vector3(r, 0, 0), new Vector3(0, 0, r), new Vector3(-r, 0, 0), new Vector3(0, 0, -r),
                };
                var t = new List<int>();
                for (int i = 0; i < 4; i++)
                {
                    int a = 2 + i, b = 2 + (i + 1) % 4;
                    t.AddRange(new[] { 0, b, a, 1, a, b });
                }
                octahedron = Build("Gem", v, t.ToArray());
                return octahedron;
            }
        }

        /// <summary>Ground ring: a thin torus of radius 2.5 m lying flat.</summary>
        public static Mesh Ring
        {
            get
            {
                if (ring != null) return ring;
                const float major = 2.5f, minor = 0.11f;
                const int seg = 48, tube = 8;
                var v = new List<Vector3>();
                var t = new List<int>();
                for (int i = 0; i <= seg; i++)
                {
                    float a = i * Mathf.PI * 2f / seg;
                    var centre = new Vector3(Mathf.Cos(a) * major, 0f, Mathf.Sin(a) * major);
                    var outward = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    for (int j = 0; j <= tube; j++)
                    {
                        float b = j * Mathf.PI * 2f / tube;
                        v.Add(centre + outward * (Mathf.Cos(b) * minor) + Vector3.up * (Mathf.Sin(b) * minor));
                    }
                }
                for (int i = 0; i < seg; i++)
                    for (int j = 0; j < tube; j++)
                    {
                        int a = i * (tube + 1) + j, b = a + tube + 1;
                        t.AddRange(new[] { a, b, a + 1, b, b + 1, a + 1 });
                    }
                ring = Build("MarkerRing", v.ToArray(), t.ToArray());
                return ring;
            }
        }

        /// <summary>Open cylinder (no caps), radius 1.1 m, 34 m tall, base at y = 0.</summary>
        public static Mesh Beam
        {
            get
            {
                if (beam != null) return beam;
                const float r = 1.1f, h = 34f;
                const int seg = 24;
                var v = new List<Vector3>();
                var t = new List<int>();
                for (int i = 0; i <= seg; i++)
                {
                    float a = i * Mathf.PI * 2f / seg;
                    v.Add(new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
                    v.Add(new Vector3(Mathf.Cos(a) * r, h, Mathf.Sin(a) * r));
                }
                for (int i = 0; i < seg; i++)
                {
                    int a = i * 2;
                    t.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 });
                }
                beam = Build("MarkerBeam", v.ToArray(), t.ToArray());
                return beam;
            }
        }

        static Mesh Build(string name, Vector3[] vertices, int[] triangles)
        {
            var m = new Mesh { name = name, vertices = vertices, triangles = triangles };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Creates a child object that draws a mesh with a material, without shadows.</summary>
        public static MeshRenderer Spawn(string name, Transform parent, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return mr;
        }
    }
}
