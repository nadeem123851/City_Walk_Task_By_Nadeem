using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Builds the city: a grid of blocks with windowed buildings, asphalt roads,
    /// raised pavements, a park in the middle and the landmarks on the route.
    /// </summary>
    public class CityBuilder : MonoBehaviour
    {
        public int blocksPerSide = 5;          // keep this odd so the park sits in the centre
        public float blockSize = 24f;
        public float roadWidth = 10f;
        public int randomSeed = 7;

        public float Pitch => blockSize + roadWidth;

        // Colours from the web preview (WebSource/main.js)
        static readonly Color Asphalt = Hex(0x3a3d43);
        static readonly Color Pavement = Hex(0xa9a59a);
        static readonly Color Grass = Hex(0x4f8a3f);
        static readonly Color LaneLine = Hex(0xe6e1cf);

        static readonly Color[] BuildingColors =
        {
            Hex(0xc9b79c), Hex(0x9aa3ad), Hex(0xd8c7a8), Hex(0x7f8892), Hex(0xb89678), Hex(0xddd6c8),
        };

        static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f);
        }

        public void Build()
        {
            var rng = new System.Random(randomSeed);
            float half = blocksPerSide * Pitch * 0.5f;
            int centre = blocksPerSide / 2;

            // Ground (asphalt) - one big slab, top surface at y = 0
            var ground = MaterialFactory.Primitive(PrimitiveType.Cube, "Ground", transform,
                new Vector3(0f, -0.5f, 0f), new Vector3(half * 2f + 40f, 1f, half * 2f + 40f), Asphalt, true);
            ground.isStatic = true;

            BuildLaneMarkings(half);

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
                        new Vector3(0f, 0.075f, 0f), new Vector3(blockSize, 0.15f, blockSize), Pavement, true);

                    if (isPark) BuildPark(block);
                    else BuildBuildings(block, rng, x, z);

                    BuildStreetLamps(block);
                }
            }

            CityLandmarks.Build(transform);
        }

        public Vector3 BlockCentre(int x, int z)
        {
            float offset = (blocksPerSide - 1) * 0.5f;
            return new Vector3((x - offset) * Pitch, 0f, (z - offset) * Pitch);
        }

        void BuildBuildings(Transform block, System.Random rng, int bx, int bz)
        {
            float inset = 2.5f;
            float cell = (blockSize - inset * 2f) / 2f;
            var windows = CityArt.Instance.windows;
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

                    // Box mesh whose window texture tiles by the building's size; one material per wall colour.
                    var b = new GameObject("Building");
                    b.transform.SetParent(block, false);
                    b.transform.localPosition = pos;
                    b.AddComponent<MeshFilter>().sharedMesh = BuildingMesh.Create(w, h, d, rng);
                    b.AddComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetTextured(color, windows);
                    b.AddComponent<BoxCollider>().size = new Vector3(w, h, d);
                    b.isStatic = true;

                    // Simple roof cap for a bit of silhouette
                    var roofColor = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f);
                    MaterialFactory.Primitive(PrimitiveType.Cube, "Roof", block,
                        new Vector3(pos.x, 0.15f + h + 0.4f, pos.z), new Vector3(w * 0.9f, 0.8f, d * 0.9f), roofColor);
                }
            }
        }

        void BuildPark(Transform block)
        {
            MaterialFactory.Primitive(PrimitiveType.Cube, "Lawn", block,
                new Vector3(0f, 0.17f, 0f), new Vector3(blockSize - 3f, 0.05f, blockSize - 3f), Grass);

            // Fountain
            MaterialFactory.Primitive(PrimitiveType.Cylinder, "FountainBase", block,
                new Vector3(0f, 0.45f, 0f), new Vector3(6f, 0.3f, 6f), new Color(0.75f, 0.74f, 0.70f), true);
            MaterialFactory.Primitive(PrimitiveType.Cylinder, "FountainWater", block,
                new Vector3(0f, 0.62f, 0f), new Vector3(5.2f, 0.05f, 5.2f), new Color(0.35f, 0.55f, 0.75f));
            MaterialFactory.Primitive(PrimitiveType.Cylinder, "FountainColumn", block,
                new Vector3(0f, 1.6f, 0f), new Vector3(0.6f, 1.2f, 0.6f), new Color(0.75f, 0.74f, 0.70f), true);

            // Trees around the edge of the park
            var trunk = new Color(0.36f, 0.26f, 0.18f);
            var leaves = new Color(0.25f, 0.45f, 0.24f);
            float r = blockSize * 0.36f;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f + 0.4f;
                var p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                MaterialFactory.Primitive(PrimitiveType.Cylinder, "Trunk", block,
                    p + new Vector3(0f, 1.3f, 0f), new Vector3(0.35f, 1.2f, 0.35f), trunk, true);
                MaterialFactory.Primitive(PrimitiveType.Sphere, "Leaves", block,
                    p + new Vector3(0f, 3.3f, 0f), new Vector3(2.6f, 2.4f, 2.6f), leaves);
            }
        }

        void BuildStreetLamps(Transform block)
        {
            float e = blockSize * 0.5f - 0.6f;
            var pole = new Color(0.18f, 0.18f, 0.2f);
            var bulb = new Color(1f, 0.92f, 0.7f);
            Vector3[] corners = { new Vector3(e, 0, e), new Vector3(-e, 0, e), new Vector3(e, 0, -e), new Vector3(-e, 0, -e) };
            foreach (var c in corners)
            {
                MaterialFactory.Primitive(PrimitiveType.Cylinder, "LampPole", block,
                    c + new Vector3(0f, 2.15f, 0f), new Vector3(0.12f, 2f, 0.12f), pole, true);
                MaterialFactory.Primitive(PrimitiveType.Sphere, "LampBulb", block,
                    c + new Vector3(0f, 4.25f, 0f), Vector3.one * 0.35f, bulb, false, 1.5f);
            }
        }

        void BuildLaneMarkings(float half)
        {
            var parent = new GameObject("LaneMarkings").transform;
            parent.SetParent(transform, false);
            for (int i = 0; i <= blocksPerSide; i++)
            {
                float p = -half + i * Pitch;
                if (i == 0 || i == blocksPerSide) continue; // outer edges have no centre line
                for (float t = -half; t < half; t += 4f)
                {
                    MaterialFactory.Primitive(PrimitiveType.Cube, "Dash", parent,
                        new Vector3(p, 0.01f, t + 1f), new Vector3(0.18f, 0.02f, 1.8f), LaneLine);
                    MaterialFactory.Primitive(PrimitiveType.Cube, "Dash", parent,
                        new Vector3(t + 1f, 0.01f, p), new Vector3(1.8f, 0.02f, 0.18f), LaneLine);
                }
            }
        }
    }
}
