using System.Collections.Generic;
using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Collects flat, upward-facing quads (road paint and the like) and builds them into one mesh, so a whole
    /// layer of ground detail is a single draw call.
    /// </summary>
    public class QuadBatch
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<int> tris = new List<int>();

        public int Count => verts.Count / 4;

        /// <summary>Adds an axis-aligned quad centred at <paramref name="centre"/>, sizeX by sizeZ metres.</summary>
        public void Add(Vector3 centre, float sizeX, float sizeZ)
        {
            int v = verts.Count;
            float hx = sizeX * 0.5f, hz = sizeZ * 0.5f;
            verts.Add(centre + new Vector3(-hx, 0f, -hz));
            verts.Add(centre + new Vector3(-hx, 0f, hz));
            verts.Add(centre + new Vector3(hx, 0f, hz));
            verts.Add(centre + new Vector3(hx, 0f, -hz));
            uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(0f, 1f)); uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(1f, 0f));
            tris.Add(v); tris.Add(v + 1); tris.Add(v + 2);
            tris.Add(v); tris.Add(v + 2); tris.Add(v + 3);
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name };
            if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            var normals = new Vector3[verts.Count];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
