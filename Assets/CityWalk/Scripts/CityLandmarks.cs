using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Builds the named places on the route: coffee kiosk, office sign, bank portico and cafe front.
    /// Positions match the web preview (WebSource/main.js) with z mirrored for Unity.
    /// Also decides which building lots are reshaped or left empty to make room for them.
    /// </summary>
    public static class CityLandmarks
    {
        public static readonly Color Green = Hex(0x165a47);
        public static readonly Color Gold = Hex(0xd9ae4f);
        static readonly Color KioskWindow = Hex(0x0c2f26);
        static readonly Color Stone = Hex(0xefeadc);
        static readonly Color BankBlue = Hex(0x2b3a55);
        static readonly Color CafeRed = Hex(0xb5443a);
        static readonly Color Metal = Hex(0x2a2b30);
        static readonly Color OfficeWall = Hex(0x3f7a66);
        static readonly Color BankWall = Hex(0xe9e3d3);

        public static readonly Vector3 KioskPosition = new Vector3(27.6f, 0f, -27.2f);
        public static readonly Vector3 OfficeSignPosition = new Vector3(58.4f, 0f, -10.4f);

        /// <summary>
        /// Adjusts one building lot. bx, bz pick the block (0..4) and i, j the lot inside it (0..1).
        /// Returns false when the lot must stay empty.
        /// </summary>
        public static bool ShapeLot(int bx, int bz, int i, int j, ref float w, ref float d, ref float h, ref Color wall)
        {
            if (bx == 3 && bz == 1 && i == 0 && j == 1) return false;              // kiosk plaza

            if (bx == 3 && bz == 2 && i == 0)                                         // 3 m alley between these two
            {
                w = 8.3f; d = 6.5f; h = j == 0 ? 26f : 32f;
            }
            else if (bx == 4 && bz == 2 && i == 0 && j == 0)                          // office
            {
                w = 8.3f; d = 8.3f; h = 40f; wall = OfficeWall;
            }
            else if (bx == 2 && bz == 3 && i == 0 && j == 0)                          // bank
            {
                w = 8.3f; d = 8.3f; h = 13f; wall = BankWall;
            }
            else if (bx == 1 && bz == 2 && i == 1 && j == 1)                          // cafe frontage
            {
                w = 8.3f; h = Mathf.Max(h, 16f);
            }
            return true;
        }

        public static void Build(Transform city)
        {
            var root = new GameObject("Landmarks").transform;
            root.SetParent(city, false);
            var art = CityArt.Instance;

            BuildKiosk(root, art);
            BuildOfficeSign(root, art);
            BuildBank(root, art);
            BuildCafe(root, art);
        }

        static void BuildKiosk(Transform root, CityArt art)
        {
            var k = new GameObject("CoffeeKiosk").transform;
            k.SetParent(root, false);
            k.localPosition = KioskPosition;

            Box(k, "Body", new Vector3(4.2f, 2.6f, 3f), new Vector3(0f, 1.45f, 0f), Green, true);
            Box(k, "Awning", new Vector3(4.8f, 0.25f, 3.8f), new Vector3(0f, 2.95f, 0f), Gold);
            Box(k, "ServingWindow", new Vector3(3.2f, 1.0f, 0.1f), new Vector3(0f, 1.75f, 1.52f), KioskWindow);
            Sign(k, "Sign_Front", art.signCoffee, new Vector3(0f, 3.55f, 1.9f), new Vector2(3.6f, 0.9f), Vector3.forward);
            Sign(k, "Sign_Side", art.signCoffee, new Vector3(-2.4f, 3.55f, 0f), new Vector2(3.6f, 0.9f), Vector3.left);
        }

        static void BuildOfficeSign(Transform root, CityArt art)
        {
            var o = new GameObject("OfficeSign").transform;
            o.SetParent(root, false);
            o.localPosition = OfficeSignPosition;

            Box(o, "Pylon", new Vector3(0.35f, 4.4f, 2.6f), new Vector3(0f, 2.35f, 0f), Green, true);
            Box(o, "Cap", new Vector3(0.45f, 0.2f, 2.8f), new Vector3(0f, 4.65f, 0f), Gold);
            Sign(o, "Sign", art.signOffice, new Vector3(-0.19f, 3.6f, 0f), new Vector2(2.4f, 0.6f), Vector3.left);
        }

        static void BuildBank(Transform root, CityArt art)
        {
            var b = new GameObject("BankPortico").transform;
            b.SetParent(root, false);
            b.localPosition = new Vector3(-4.75f, 0f, 24.5f);

            foreach (float dx in new[] { -3.3f, -1.1f, 1.1f, 3.3f })
                Cylinder(b, "Column", 0.28f, 4.2f, new Vector3(dx, 2.25f, 0f), Stone, true);
            Box(b, "Entablature", new Vector3(8.2f, 0.5f, 1.4f), new Vector3(0f, 4.6f, -0.05f), Stone);
            Box(b, "SignBoard", new Vector3(4.0f, 1.0f, 0.16f), new Vector3(0f, 5.35f, -0.5f), BankBlue);
            Sign(b, "Sign", art.signBank, new Vector3(0f, 5.35f, -0.59f), new Vector2(3.6f, 0.9f), Vector3.back);
        }

        static void BuildCafe(Transform root, CityArt art)
        {
            var c = new GameObject("CafeFront").transform;
            c.SetParent(root, false);
            c.localPosition = new Vector3(-24.6f, 0f, 4.75f);

            Box(c, "Awning", new Vector3(1.0f, 0.12f, 6.6f), new Vector3(0f, 3.0f, 0f), CafeRed);
            Sign(c, "Sign", art.signCafe, new Vector3(-0.44f, 3.9f, 0f), new Vector2(3.2f, 0.8f), Vector3.right);
            foreach (float dz in new[] { 1.85f, -1.85f })
            {
                Cylinder(c, "TableTop", 0.45f, 0.05f, new Vector3(0.05f, 0.92f, dz), Stone);
                Box(c, "TableLeg", new Vector3(0.08f, 0.75f, 0.08f), new Vector3(0.05f, 0.52f, dz), Metal);
            }
        }

        // ---------- helpers ----------

        static GameObject Box(Transform parent, string name, Vector3 size, Vector3 pos, Color color, bool collider = false)
        {
            return MaterialFactory.Primitive(PrimitiveType.Cube, name, parent, pos, size, color, collider);
        }

        /// <summary>Unity's cylinder is 2 m tall and 1 m wide at scale 1.</summary>
        static GameObject Cylinder(Transform parent, string name, float radius, float height, Vector3 pos, Color color, bool collider = false)
        {
            return MaterialFactory.Primitive(PrimitiveType.Cylinder, name, parent, pos,
                new Vector3(radius * 2f, height * 0.5f, radius * 2f), color, collider);
        }

        /// <summary>A one-sided sign readable from the side its face direction points to.</summary>
        static void Sign(Transform parent, string name, Texture2D texture, Vector3 pos, Vector2 size, Vector3 faces)
        {
            var go = MaterialFactory.Primitive(PrimitiveType.Quad, name, parent, pos,
                new Vector3(size.x, size.y, 1f), MaterialFactory.GetUnlit(texture));
            // A quad's visible side looks down its -z axis.
            go.transform.localRotation = Quaternion.LookRotation(-faces, Vector3.up);
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f);
        }
    }
}
