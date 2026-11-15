using System.Collections.Generic;
using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Small faceted meshes in the low-poly style of the characters. Blob is a lumpy, flat-shaded icosphere
    /// (80 triangles, diameter 1 like a Unity sphere) for tree canopies and bushes.
    /// </summary>
    public static class LowPolyMeshes
    {
        static readonly Dictionary<int, Mesh> blobs = new Dictionary<int, Mesh>();

        public static Mesh Blob(int seed)
        {
            if (blobs.TryGetValue(seed, out var cached) && cached != null) return cached;

            // Icosahedron
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var p = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };
            int[] f =
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };
            for (int i = 0; i < p.Count; i++) p[i] = p[i].normalized;

            // One subdivision: split every edge at its midpoint
            var mid = new Dictionary<long, int>();
            int Mid(int a, int b)
            {
                long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                if (mid.TryGetValue(key, out int idx)) return idx;
                p.Add(((p[a] + p[b]) * 0.5f).normalized);
                mid[key] = p.Count - 1;
                return p.Count - 1;
            }
            var faces = new List<int>();
            for (int i = 0; i < f.Length; i += 3)
            {
                int a = f[i], b = f[i + 1], c = f[i + 2];
                int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                faces.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }

            // Lumps: push each shared vertex in or out by up to 12%
            var rng = new System.Random(seed);
            for (int i = 0; i < p.Count; i++)
                p[i] *= 0.5f * (1f + ((float)rng.NextDouble() * 2f - 1f) * 0.12f);

            // Flat shading: three vertices per face, each face wound so its normal points outwards.
            var verts = new Vector3[faces.Count];
            var tris = new int[faces.Count];
            for (int i = 0; i < faces.Count; i += 3)
            {
                Vector3 a = p[faces[i]], b = p[faces[i + 1]], c = p[faces[i + 2]];
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c) < 0f) { var s = b; b = c; c = s; }
                verts[i] = a; verts[i + 1] = b; verts[i + 2] = c;
                tris[i] = i; tris[i + 1] = i + 1; tris[i + 2] = i + 2;
            }
            var mesh = new Mesh { name = "Blob" + seed, vertices = verts, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            blobs[seed] = mesh;
            return mesh;
        }
    }
}
