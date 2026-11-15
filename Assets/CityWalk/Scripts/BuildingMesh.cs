using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Builds a box mesh for a building with UVs that tile the 8x8 window texture by the
    /// building's real size, so every building can share one window material per colour.
    /// The roof and floor point at a blank corner of the texture, so they show the wall colour.
    /// </summary>
    public static class BuildingMesh
    {
        const float WindowWidth = 1.7f;   // metres per window column
        const float FloorHeight = 2.4f;   // metres per window row
        const float GridCells = 8f;       // the texture is an 8x8 grid of windows
        static readonly Vector2 BlankUv = new Vector2(0.004f, 0.004f);

        /// <summary>Creates a w x h x d box centred on the origin.</summary>
        /// <param name="rng">Picks a random start cell so neighbouring buildings don't line up.</param>
        public static Mesh Create(float w, float h, float d, System.Random rng)
        {
            var offset = new Vector2(rng.Next(8) / GridCells, rng.Next(8) / GridCells);
            var verts = new Vector3[24];
            var normals = new Vector3[24];
            var uvs = new Vector2[24];
            var tris = new int[36];
            var half = new Vector3(w, h, d) * 0.5f;

            int face = 0;
            AddFace(Vector3.right, d, h, half, offset, true, ref face, verts, normals, uvs, tris);
            AddFace(Vector3.left, d, h, half, offset, true, ref face, verts, normals, uvs, tris);
            AddFace(Vector3.forward, w, h, half, offset, true, ref face, verts, normals, uvs, tris);
            AddFace(Vector3.back, w, h, half, offset, true, ref face, verts, normals, uvs, tris);
            AddFace(Vector3.up, w, d, half, offset, false, ref face, verts, normals, uvs, tris);
            AddFace(Vector3.down, w, d, half, offset, false, ref face, verts, normals, uvs, tris);

            var mesh = new Mesh { name = "Building" };
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        static void AddFace(Vector3 n, float faceWidth, float faceHeight, Vector3 half, Vector2 offset, bool windows,
            ref int face, Vector3[] verts, Vector3[] normals, Vector2[] uvs, int[] tris)
        {
            // Axes as seen by someone standing outside the face, looking at it.
            Vector3 up = Mathf.Abs(n.y) > 0.5f ? Vector3.forward : Vector3.up;
            Vector3 right = Vector3.Cross(up, -n);
            Vector3 centre = Vector3.Scale(n, half);
            Vector3 r = right * (Mathf.Abs(Vector3.Dot(right, half)));
            Vector3 u = up * (Mathf.Abs(Vector3.Dot(up, half)));

            int v = face * 4;
            verts[v + 0] = centre - r - u;   // bottom left
            verts[v + 1] = centre + r - u;   // bottom right
            verts[v + 2] = centre + r + u;   // top right
            verts[v + 3] = centre - r + u;   // top left
            for (int k = 0; k < 4; k++) normals[v + k] = n;

            if (windows)
            {
                float nu = Mathf.Max(1, Mathf.Round(faceWidth / WindowWidth)) / GridCells;
                float nv = Mathf.Max(1, Mathf.Round(faceHeight / FloorHeight)) / GridCells;
                uvs[v + 0] = offset;
                uvs[v + 1] = offset + new Vector2(nu, 0f);
                uvs[v + 2] = offset + new Vector2(nu, nv);
                uvs[v + 3] = offset + new Vector2(0f, nv);
            }
            else
            {
                for (int k = 0; k < 4; k++) uvs[v + k] = BlankUv;
            }

            int t = face * 6;
            tris[t + 0] = v + 0; tris[t + 1] = v + 3; tris[t + 2] = v + 2;
            tris[t + 3] = v + 0; tris[t + 4] = v + 2; tris[t + 5] = v + 1;
            face++;
        }
    }
}
