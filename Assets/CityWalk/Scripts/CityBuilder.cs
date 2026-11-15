using UnityEngine;
using UnityEngine.Rendering;

namespace CityWalk
{
    /// <summary>
    /// Builds the city: a grid of blocks with windowed buildings, asphalt roads with kerbs and road paint,
    /// paved pavements, a park in the middle and the landmarks on the route.
    /// </summary>
    public class CityBuilder : MonoBehaviour
    {
        public int blocksPerSide = 5;          // keep this odd so the park sits in the centre
        public float blockSize = 24f;
        public float roadWidth = 10f;
        public int randomSeed = 7;

        public float Pitch => blockSize + roadWidth;

        /// <summary>Layer for solid city volumes the camera must not pass through.</summary>
        public const string BuildingsLayerName = "Buildings";

        /// <summary>The Buildings layer index, or Default (0) if the layer is missing from the project.</summary>
        public static int BuildingsLayer
        {
            get
            {
                int layer = LayerMask.NameToLayer(BuildingsLayerName);
                return layer >= 0 ? layer : 0;
            }
        }

        static readonly Color Asphalt = Hex(0x5A5C61);
        static readonly Color Pavement = Hex(0xC2B9A7);
        static readonly Color Kerb = Hex(0xD6D0C2);
        static readonly Color Grass = Hex(0x5E9E45);
        static readonly Color RoadPaint = Hex(0xDEDACB);
        static readonly Color Trim = Hex(0xF2ECE0);
        static readonly Color LampMetal = Hex(0x2F3A3A);
        static readonly Color LampLens = Hex(0xF3EBD2);
        static readonly Color Trunk = Hex(0x6B4A32);
        static readonly Color Leaves = Hex(0x4E9A46);
        static readonly Color LeavesSunny = Hex(0x7DBB55);

        // Warm, varied wall tints (the facade atlas wall is #E0E0E0, so these read slightly darker on screen).
        static readonly Color[] BuildingColors =
        {
            Hex(0xE6CFA9), Hex(0xD49B7C), Hex(0xEFE7D6), Hex(0xAFC0D2), Hex(0xB8C4A6), Hex(0x8D9BAE),
        };

        static readonly Color PlinthStone = Hex(0xCFC6B4);
        static readonly Color PlinthCharcoal = Hex(0x5C6068);
        static readonly Color PlinthBrick = Hex(0xA8604A);

        static Color Hex(int rgb) => CityTextures.Hex(rgb);

        System.Random deco;   // decoration choices only, so the building layout from rng never changes

        public void Build()
        {
            var rng = new System.Random(randomSeed);
            deco = new System.Random(randomSeed + 1000);
            float half = blocksPerSide * Pitch * 0.5f;
            int centre = blocksPerSide / 2;

            // Ground (asphalt) - one big slab, top surface at y = 0, textured with 4 m of grain per repeat
            float slab = half * 2f + 40f;
            var ground = MaterialFactory.Primitive(PrimitiveType.Cube, "Ground", transform,
                new Vector3(0f, -0.5f, 0f), new Vector3(slab, 1f, slab),
                MaterialFactory.GetTextured(Asphalt, CityTextures.Asphalt(), Vector2.one * (slab / 4f)), true).NoShadow();
            ground.isStatic = true;

            BuildRoadPaint(half);

            var paving = MaterialFactory.GetTextured(Pavement, CityTextures.Paving(), new Vector2(10f, 10f));
            for (int x = 0; x < blocksPerSide; x++)
            {
                for (int z = 0; z < blocksPerSide; z++)
                {
                    Vector3 c = BlockCentre(x, z);
                    var block = new GameObject($"Block_{x}_{z}").transform;
                    block.SetParent(transform, false);
                    block.localPosition = c;

                    bool isPark = x == centre && z == centre;
                    MaterialFactory.Primitive(PrimitiveType.Cube, "Pavement", block,
                        new Vector3(0f, 0.075f, 0f), new Vector3(blockSize, 0.15f, blockSize), paving, true).NoShadow();
                    BuildKerbs(block);

                    if (isPark) BuildPark(block);
                    else BuildBuildings(block, rng, x, z);

                    BuildStreetLamps(block);
                }
            }

            CityLandmarks.Build(transform);

            // The city is built at runtime, so the Static flag alone does nothing: merge its meshes by material
            // here instead. Nothing under the city moves after this point.
            StaticBatchingUtility.Combine(gameObject);
        }

        public Vector3 BlockCentre(int x, int z)
        {
            float offset = (blocksPerSide - 1) * 0.5f;
            return new Vector3((x - offset) * Pitch, 0f, (z - offset) * Pitch);
        }

        /// <summary>A pale kerb strip round the block: an edge for the road that reads at any distance.</summary>
        void BuildKerbs(Transform block)
        {
            float e = blockSize * 0.5f - 0.13f;
            var size = new Vector3(blockSize + 0.04f, 0.155f, 0.30f);
            var sizeSide = new Vector3(0.30f, 0.155f, blockSize - 0.56f);
            MaterialFactory.Primitive(PrimitiveType.Cube, "Kerb", block, new Vector3(0f, 0.0775f, e), size, Kerb).NoShadow();
            MaterialFactory.Primitive(PrimitiveType.Cube, "Kerb", block, new Vector3(0f, 0.0775f, -e), size, Kerb).NoShadow();
            MaterialFactory.Primitive(PrimitiveType.Cube, "Kerb", block, new Vector3(e, 0.0775f, 0f), sizeSide, Kerb).NoShadow();
            MaterialFactory.Primitive(PrimitiveType.Cube, "Kerb", block, new Vector3(-e, 0.0775f, 0f), sizeSide, Kerb).NoShadow();
        }

        void BuildBuildings(Transform block, System.Random rng, int bx, int bz)
        {
            float inset = 2.5f;
            float cell = (blockSize - inset * 2f) / 2f;
            var windows = CityTextures.Facade();
            if (windows == null) windows = CityArt.Instance.windows;
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    float w = cell - 1.2f - (float)rng.NextDouble() * 2f;
                    float d = cell - 1.2f - (float)rng.NextDouble() * 2f;
                    float h = 8f + (float)rng.NextDouble() * 34f;
                    var color = BuildingColors[rng.Next(BuildingColors.Length)];
                    if (!CityLandmarks.ShapeLot(bx, bz, i, j, ref w, ref d, ref h, ref color))
                        continue;

                    var pos = new Vector3(-blockSize * 0.5f + inset + cell * (i + 0.5f), 0.15f + h * 0.5f,
                                          -blockSize * 0.5f + inset + cell * (j + 0.5f));

                    // Box mesh whose facade texture tiles by the building's size; one material per wall colour.
                    var b = new GameObject("Building");
                    b.transform.SetParent(block, false);
                    b.transform.localPosition = pos;
                    b.AddComponent<MeshFilter>().sharedMesh = BuildingMesh.Create(w, h, d, rng);
                    b.AddComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetTextured(color, windows);
                    b.AddComponent<BoxCollider>().size = new Vector3(w, h, d);
                    b.layer = BuildingsLayer;
                    b.isStatic = true;

                    // Ground-floor plinth (hides the windows that would touch the pavement), a string course
                    // above it and a pale cornice in place of the old inset roof slab.
                    Color plinth = color == CityLandmarks.BankWall ? PlinthStone
                                 : color == CityLandmarks.OfficeWall ? PlinthCharcoal
                                 : PickPlinth();
                    MaterialFactory.Primitive(PrimitiveType.Cube, "Plinth", block,
                        new Vector3(pos.x, 0.15f + 1.2f, pos.z), new Vector3(w + 0.2f, 2.4f, d + 0.2f), plinth).NoShadow();
                    MaterialFactory.Primitive(PrimitiveType.Cube, "StringCourse", block,
                        new Vector3(pos.x, 0.15f + 2.4f + 0.125f, pos.z), new Vector3(w + 0.3f, 0.25f, d + 0.3f), Trim).NoShadow();
                    MaterialFactory.Primitive(PrimitiveType.Cube, "Cornice", block,
                        new Vector3(pos.x, 0.15f + h + 0.175f, pos.z), new Vector3(w + 0.4f, 0.35f, d + 0.4f), Trim);
                }
            }
        }

        Color PickPlinth()
        {
            double r = deco.NextDouble();
            return r < 0.45 ? PlinthStone : r < 0.75 ? PlinthCharcoal : PlinthBrick;
        }

        void BuildPark(Transform block)
        {
            MaterialFactory.Primitive(PrimitiveType.Cube, "Lawn", block,
                new Vector3(0f, 0.17f, 0f), new Vector3(blockSize - 3f, 0.05f, blockSize - 3f),
                MaterialFactory.GetTextured(Grass, CityTextures.Lawn(), new Vector2(7f, 7f))).NoShadow();

            // Fountain
            MaterialFactory.Primitive(PrimitiveType.Cylinder, "FountainBase", block,
                new Vector3(0f, 0.45f, 0f), new Vector3(6f, 0.3f, 6f), new Color(0.75f, 0.74f, 0.70f), true);
            MaterialFactory.Primitive(PrimitiveType.Cylinder, "FountainWater", block,
                new Vector3(0f, 0.62f, 0f), new Vector3(5.2f, 0.05f, 5.2f), Hex(0x7EC8E3)).NoShadow();
            MaterialFactory.Primitive(PrimitiveType.Cylinder, "FountainColumn", block,
                new Vector3(0f, 1.6f, 0f), new Vector3(0.6f, 1.2f, 0.6f), new Color(0.75f, 0.74f, 0.70f), true);

            // Faceted trees around the edge of the park
            float r = blockSize * 0.36f;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f + 0.4f;
                var p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                Tree(block, p, 0.85f + (float)deco.NextDouble() * 0.3f);
            }
        }

        /// <summary>A trunk with three lumpy canopy blobs in two greens, the lighter one on the sun's side.</summary>
        void Tree(Transform parent, Vector3 pos, float scale)
        {
            MaterialFactory.Primitive(PrimitiveType.Cylinder, "Trunk", parent,
                pos + new Vector3(0f, 1.3f, 0f), new Vector3(0.35f, 1.3f, 0.35f), Trunk, true);

            var yaw = Quaternion.Euler(0f, (float)deco.NextDouble() * 360f, 0f);
            Blob(parent, pos + new Vector3(0f, 3.5f * scale, 0f), new Vector3(2.8f, 2.4f, 2.8f) * scale, Leaves, yaw, deco.Next(3));
            Blob(parent, pos + new Vector3(-0.5f, 4.4f, -0.4f) * scale, new Vector3(1.9f, 1.6f, 1.9f) * scale, LeavesSunny, yaw, deco.Next(3));
            Blob(parent, pos + new Vector3(0.8f, 3.1f, 0.4f) * scale, new Vector3(1.8f, 1.4f, 1.8f) * scale, Leaves, yaw, deco.Next(3));
        }

        static void Blob(Transform parent, Vector3 pos, Vector3 size, Color color, Quaternion rot, int variant)
        {
            var go = new GameObject("Leaves");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = size;
            go.AddComponent<MeshFilter>().sharedMesh = LowPolyMeshes.Blob(100 + variant);
            go.AddComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Get(color);
        }

        /// <summary>Street lamps, off for the morning: pole, base collar, an arm over the road, a head and a lens.</summary>
        void BuildStreetLamps(Transform block)
        {
            float e = blockSize * 0.5f - 0.6f;
            Vector3[] corners = { new Vector3(e, 0, e), new Vector3(-e, 0, e), new Vector3(e, 0, -e), new Vector3(-e, 0, -e) };
            foreach (var c in corners)
            {
                float sx = Mathf.Sign(c.x);
                MaterialFactory.Primitive(PrimitiveType.Cylinder, "LampPole", block,
                    c + new Vector3(0f, 2.15f, 0f), new Vector3(0.12f, 2f, 0.12f), LampMetal, true);
                MaterialFactory.Primitive(PrimitiveType.Cylinder, "LampCollar", block,
                    c + new Vector3(0f, 0.25f, 0f), new Vector3(0.26f, 0.1f, 0.26f), LampMetal);
                MaterialFactory.Primitive(PrimitiveType.Cube, "LampArm", block,
                    c + new Vector3(sx * 0.35f, 4.12f, 0f), new Vector3(0.7f, 0.06f, 0.06f), LampMetal);
                MaterialFactory.Primitive(PrimitiveType.Cube, "LampHead", block,
                    c + new Vector3(sx * 0.75f, 4.08f, 0f), new Vector3(0.5f, 0.1f, 0.22f), LampMetal);
                MaterialFactory.Primitive(PrimitiveType.Cube, "LampLens", block,
                    c + new Vector3(sx * 0.75f, 4.015f, 0f), new Vector3(0.4f, 0.03f, 0.16f), LampLens).NoShadow();
            }
        }

        /// <summary>
        /// All road paint as one mesh: clear junction boxes, zebra crossings and stop lines on every arm, and
        /// centre dashes between them. The crossing over the lap line is left out.
        /// </summary>
        void BuildRoadPaint(float half)
        {
            var batch = new QuadBatch();
            const float y = 0.02f, box = 5f;   // junction box half size (the road is 10 m wide)

            // Inner road centre lines (the outer edges have none)
            var lines = new System.Collections.Generic.List<float>();
            for (int i = 1; i < blocksPerSide; i++) lines.Add(-half + i * Pitch);

            foreach (float line in lines)
            {
                for (int axis = 0; axis < 2; axis++)   // 0: road runs along x at z = line; 1: along z at x = line
                {
                    // Junctions along this road are where it meets the other lines.
                    var stops = new System.Collections.Generic.List<float> { -half };
                    stops.AddRange(lines);
                    stops.Add(half);
                    for (int s = 0; s + 1 < stops.Count; s++)
                    {
                        float a = stops[s], b = stops[s + 1];
                        bool aJunction = s > 0, bJunction = s + 1 < stops.Count - 1;
                        float start = aJunction ? a + box : a, end = bJunction ? b - box : b;
                        if (aJunction) Arm(batch, axis, line, start, +1f, y);
                        if (bJunction) Arm(batch, axis, line, end, -1f, y);

                        // Centre dashes every 4 m from 8 m off a junction box, clear of the crossings
                        float from = aJunction ? start + 8f : start + 2f;
                        float to = bJunction ? end - 7f : end - 1f;
                        for (float t = from; t <= to + 0.01f; t += 4f)
                            Quad(batch, axis, line, t, 0f, 1.8f, 0.18f, y);
                    }
                }
            }

            var go = new GameObject("RoadPaint");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = batch.Build("RoadPaint");
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = MaterialFactory.Get(RoadPaint);
            r.shadowCastingMode = ShadowCastingMode.Off;
            go.isStatic = true;
        }

        /// <summary>Crossing and stop line on one arm. <paramref name="edge"/> is the box edge; dir points away from it.</summary>
        void Arm(QuadBatch batch, int axis, float line, float edge, float dir, float y)
        {
            float crossCentre = edge + dir * 2f;   // zebra covers 0.5 to 3.5 m from the box
            bool lapArm = axis == 0 && Mathf.Abs(line - CityLandmarks.LapRoadZ) < 0.5f &&
                          Mathf.Abs(crossCentre - CityLandmarks.LapGateX) < 3f;
            if (lapArm) return;   // the gold lap line is the only marking there
            for (int k = 0; k < 10; k++)
                Quad(batch, axis, line, crossCentre, -4.5f + k, 3f, 0.5f, y);
            // Stop line across the left half of the road for traffic coming towards the junction (keep left)
            float across = dir * 2.5f * (axis == 0 ? -1f : 1f);
            Quad(batch, axis, line, edge + dir * 4.2f, across, 0.3f, 5f, y);
        }

        /// <summary>A quad at distance <paramref name="along"/> down the road and <paramref name="across"/> from its centre line.</summary>
        static void Quad(QuadBatch batch, int axis, float line, float along, float across, float lengthAlong, float widthAcross, float y)
        {
            if (axis == 0) batch.Add(new Vector3(along, y, line + across), lengthAlong, widthAcross);
            else batch.Add(new Vector3(line + across, y, along), widthAcross, lengthAlong);
        }
    }
}
