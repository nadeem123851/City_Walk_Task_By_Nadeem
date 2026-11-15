using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Procedural textures for the city: asphalt, paving, lawn stripes, the facade atlas, a soft blob and the sky
    /// panorama. Each is made once with a fixed seed and kept for the whole session (it survives scene changes).
    /// The CPU copy is freed after upload.
    /// </summary>
    public static class CityTextures
    {
        static Texture2D asphalt, paving, lawn, facade, blob, sky;

        // ---------------------------------------------------------------- noise

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 982451653);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }

        static float Hash3(int x, int y, int z, int seed) => Hash(x * 7919 + z * 104729, y, seed);

        /// <summary>Value noise that repeats every <paramref name="period"/> cells.</summary>
        static float TileNoise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int ax = Mod(x0, period), bx = Mod(x0 + 1, period), ay = Mod(y0, period), by = Mod(y0 + 1, period);
            float a = Mathf.Lerp(Hash(ax, ay, seed), Hash(bx, ay, seed), fx);
            float b = Mathf.Lerp(Hash(ax, by, seed), Hash(bx, by, seed), fx);
            return Mathf.Lerp(a, b, fy);
        }

        static float Noise3(float x, float y, float z, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y), z0 = Mathf.FloorToInt(z);
            float fx = x - x0, fy = y - y0, fz = z - z0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            fz = fz * fz * (3f - 2f * fz);
            float c000 = Hash3(x0, y0, z0, seed), c100 = Hash3(x0 + 1, y0, z0, seed);
            float c010 = Hash3(x0, y0 + 1, z0, seed), c110 = Hash3(x0 + 1, y0 + 1, z0, seed);
            float c001 = Hash3(x0, y0, z0 + 1, seed), c101 = Hash3(x0 + 1, y0, z0 + 1, seed);
            float c011 = Hash3(x0, y0 + 1, z0 + 1, seed), c111 = Hash3(x0 + 1, y0 + 1, z0 + 1, seed);
            float a = Mathf.Lerp(Mathf.Lerp(c000, c100, fx), Mathf.Lerp(c010, c110, fx), fy);
            float b = Mathf.Lerp(Mathf.Lerp(c001, c101, fx), Mathf.Lerp(c011, c111, fx), fy);
            return Mathf.Lerp(a, b, fz);
        }

        static int Mod(int a, int m) => ((a % m) + m) % m;

        static Color32 Grey(float v)
        {
            byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);
            return new Color32(b, b, b, 255);
        }

        static Texture2D Finish(Texture2D tex, Color32[] px, string name, TextureWrapMode wrap, int aniso, bool mips)
        {
            tex.name = name;
            tex.SetPixels32(px);
            tex.wrapMode = wrap;
            tex.filterMode = mips ? FilterMode.Trilinear : FilterMode.Bilinear;
            tex.anisoLevel = aniso;
            tex.Apply(mips, true);
            return tex;
        }

        static float SmoothStep(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        // ---------------------------------------------------------------- ground

        /// <summary>Greyscale asphalt grain, 256 px for 4 m.</summary>
        public static Texture2D Asphalt()
        {
            if (asphalt != null) return asphalt;
            const int n = 256;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float v = 0.90f;
                v += (TileNoise(x / 32f, y / 32f, n / 32, 11) - 0.5f) * 0.10f;
                v += (TileNoise(x / 4f, y / 4f, n / 4, 12) - 0.5f) * 0.08f;
                float h = Hash(x, y, 13);
                if (h < 0.015f) v = 1f;
                else if (h > 0.985f) v = 0.72f;
                px[y * n + x] = Grey(v);
            }
            return asphalt = Finish(new Texture2D(n, n, TextureFormat.RGB24, true), px, "cw_asphalt", TextureWrapMode.Repeat, 4, true);
        }

        /// <summary>Paving slabs: 4 x 4 slabs of 64 px with pale joints.</summary>
        public static Texture2D Paving()
        {
            if (paving != null) return paving;
            const int n = 256, slab = 64;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int sx = x / slab, sy = y / slab, lx = x % slab, ly = y % slab;
                float v = 0.93f + Hash(sx, sy, 21) * 0.07f;
                v += (Hash(x, y, 22) - 0.5f) * 0.04f;
                if (lx < 2 || ly < 2) v = 0.72f;
                px[y * n + x] = Grey(v);
            }
            return paving = Finish(new Texture2D(n, n, TextureFormat.RGB24, true), px, "cw_paving", TextureWrapMode.Repeat, 4, true);
        }

        /// <summary>Lawn with two-tone mowing stripes.</summary>
        public static Texture2D Lawn()
        {
            if (lawn != null) return lawn;
            const int n = 128;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float v = x < n / 2 ? 0.95f : 0.87f;
                v += (Hash(x, y, 31) - 0.5f) * 0.08f;
                if (Hash(x, y, 32) < 0.02f) v = 1f;
                px[y * n + x] = Grey(v);
            }
            return lawn = Finish(new Texture2D(n, n, TextureFormat.RGB24, true), px, "cw_lawn", TextureWrapMode.Repeat, 2, true);
        }

        // ---------------------------------------------------------------- buildings

        /// <summary>
        /// Facade atlas: an 8 x 8 grid of 64 px window cells (1.7 m x 2.4 m each in the world), the same grid as the
        /// old windows texture so BuildingMesh is unchanged. Sky-blue glass, pale frames and sills, painted depth,
        /// blinds and curtains. Wall is #E0E0E0 so the building tint shows through.
        /// </summary>
        public static Texture2D Facade()
        {
            if (facade != null) return facade;
            const int n = 512, cell = 64;
            var px = new Color32[n * n];
            var wall = new Color32(0xE0, 0xE0, 0xE0, 255);
            var floorLine = new Color32(0xCF, 0xCF, 0xCF, 255);
            var white = new Color32(255, 255, 255, 255);
            var sillShade = new Color32(0xB4, 0xB4, 0xB4, 255);
            var bar = new Color32(0xF0, 0xF0, 0xF0, 255);
            Color glassTop = Hex(0x9CC3E6), glassBottom = Hex(0x4E6F92);
            Color[] curtains = { Hex(0xD9C7A8), Hex(0xC9D4DC), Hex(0xE2B9A0) };
            Color blindCol = Hex(0xE9E4D8), darkCol = Hex(0x2B3442);

            const int ox0 = 17, ox1 = 46, oy0 = 12, oy1 = 51;   // opening, inclusive, y from the bottom

            for (int cy = 0; cy < 8; cy++)
            for (int cx = 0; cx < 8; cx++)
            {
                float pick = Hash(cx, cy, 41);
                int variant = pick < 0.55f ? 0 : pick < 0.75f ? 1 : pick < 0.87f ? 2 : pick < 0.95f ? 3 : 4;
                if (cx == 0 && cy == 0) variant = 0;
                float blindTo = 0.4f + Hash(cx, cy, 42) * 0.3f;   // blind covers the top 40 to 70%
                Color curtain = curtains[(int)(Hash(cx, cy, 43) * 3f) % 3];

                for (int ly = 0; ly < cell; ly++)
                for (int lx = 0; lx < cell; lx++)
                {
                    Color32 c = ly < 2 ? floorLine : wall;
                    bool inOpening = lx >= ox0 && lx <= ox1 && ly >= oy0 && ly <= oy1;
                    bool inFrame = lx >= ox0 - 2 && lx <= ox1 + 2 && ly >= oy0 - 2 && ly <= oy1 + 2 && !inOpening;
                    // Sill: 3 px under the frame, 34 px wide, with a shade line under it
                    int sillX0 = (ox0 + ox1) / 2 - 17, sillX1 = sillX0 + 33;
                    if (lx >= sillX0 && lx <= sillX1 && ly >= oy0 - 5 && ly <= oy0 - 3) c = white;
                    else if (lx >= sillX0 && lx <= sillX1 && ly == oy0 - 6) c = sillShade;
                    else if (inFrame) c = white;

                    if (inOpening)
                    {
                        float t = (ly - oy0) / (float)(oy1 - oy0);   // 0 bottom, 1 top
                        Color g = Color.Lerp(glassBottom, glassTop, t);
                        if (variant == 1 && t > 1f - blindTo)
                            g = ((oy1 - ly) % 4 == 0) ? blindCol * 0.9f : blindCol;
                        else if (variant == 2 && (lx - ox0 < 7 || ox1 - lx < 7))
                            g = curtain;
                        else if (variant == 3)
                            g = darkCol;
                        else if (variant == 4)
                        {
                            float d = (lx - ox0) + (ly - oy0) * 0.6f;
                            if (d > 14f && d < 24f) g = Color.Lerp(g, Color.white, 0.18f);
                        }
                        // Glazing bars: one vertical, one horizontal at 40% height
                        int midX = (ox0 + ox1) / 2;
                        if ((lx == midX || lx == midX + 1) && variant != 3) g = (Color)bar;
                        if (ly == oy0 + Mathf.RoundToInt((oy1 - oy0) * 0.4f) && variant != 3) g = (Color)bar;
                        // Painted depth: shade inside the top and left edges
                        if (oy1 - ly < 3 || lx - ox0 < 2) g *= 0.55f;
                        g.a = 1f;
                        c = g;
                    }
                    px[(cy * cell + ly) * n + cx * cell + lx] = c;
                }
            }
            return facade = Finish(new Texture2D(n, n, TextureFormat.RGB24, true), px, "cw_facade", TextureWrapMode.Repeat, 2, true);
        }

        // ---------------------------------------------------------------- soft blob

        /// <summary>White disc whose alpha fades out to the edge, for blob shadows.</summary>
        public static Texture2D Blob()
        {
            if (blob != null) return blob;
            const int n = 64;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = SmoothStep(1f, 0.2f, d);
                px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
            return blob = Finish(new Texture2D(n, n, TextureFormat.RGBA32, true), px, "cw_blob", TextureWrapMode.Clamp, 0, true);
        }

        // ---------------------------------------------------------------- sky

        static readonly float[] SkyStops = { -90f, 0f, 6f, 18f, 40f, 90f };
        static readonly int[] SkyColours = { 0xE6DFD0, 0xE6DFD0, 0xD5E1EA, 0xA9CDF0, 0x6FA8E6, 0x3F7FD0 };

        static Color SkyGradient(float elevation)
        {
            for (int i = 1; i < SkyStops.Length; i++)
            {
                if (elevation <= SkyStops[i])
                {
                    float t = SmoothStep(SkyStops[i - 1], SkyStops[i], elevation);
                    return Color.Lerp(Hex(SkyColours[i - 1]), Hex(SkyColours[i]), t);
                }
            }
            return Hex(SkyColours[SkyColours.Length - 1]);
        }

        /// <summary>
        /// 1024 x 512 latitude-longitude panorama for Skybox/Panoramic: a deeper blue overhead, a warm cream
        /// horizon that matches the fog, warmth on the sun's side, and soft flat clouds.
        /// </summary>
        /// <param name="sunTowards">Direction from the scene towards the sun.</param>
        public static Texture2D SkyPanorama(Vector3 sunTowards)
        {
            if (sky != null) return sky;
            const int w = 1024, h = 512, cw = 256, ch = 128;

            // Cloud density on a coarse grid, then scaled up.
            var cloud = new float[cw * ch];
            for (int y = 0; y < ch; y++)
            for (int x = 0; x < cw; x++)
            {
                Vector3 d = Direction((x + 0.5f) / cw, (y + 0.5f) / ch);
                float nsum = 0f, amp = 0.5f, freq = 1f, norm = 0f;
                for (int o = 0; o < 4; o++)
                {
                    nsum += Noise3(d.x * 3f * freq, d.y * 9f * freq, d.z * 3f * freq, 51 + o) * amp;
                    norm += amp;
                    amp *= 0.5f;
                    freq *= 2f;
                }
                cloud[y * cw + x] = nsum / norm;
            }

            var sunXZ = new Vector2(sunTowards.x, sunTowards.z).normalized;
            Color warm = Hex(0xFFE9C4), cloudTop = Color.white, cloudBase = Hex(0xEEF1F5);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                Vector3 d = Direction(u, v);
                float el = Mathf.Asin(Mathf.Clamp(d.y, -1f, 1f)) * Mathf.Rad2Deg;
                Color c = SkyGradient(el);

                var viewXZ = new Vector2(d.x, d.z);
                if (viewXZ.sqrMagnitude > 1e-6f)
                {
                    float s = Mathf.Max(0f, Vector2.Dot(viewXZ.normalized, sunXZ));
                    float wgt = s * s * (1f - SmoothStep(0f, 40f, el));
                    c += warm * (0.35f * wgt);
                }

                // Bilinear sample of the cloud grid (wrapping round in u)
                float gx = u * cw - 0.5f, gy = v * ch - 0.5f;
                int x0 = Mathf.FloorToInt(gx), y0 = Mathf.Clamp(Mathf.FloorToInt(gy), 0, ch - 1);
                int y1 = Mathf.Min(y0 + 1, ch - 1);
                float fx = gx - x0, fy = Mathf.Clamp01(gy - y0);
                int xa = Mod(x0, cw), xb = Mod(x0 + 1, cw);
                float nval = Mathf.Lerp(Mathf.Lerp(cloud[y0 * cw + xa], cloud[y0 * cw + xb], fx),
                                        Mathf.Lerp(cloud[y1 * cw + xa], cloud[y1 * cw + xb], fx), fy);
                float density = SmoothStep(0.52f, 0.68f, nval);
                float mask = SmoothStep(5f, 11f, el) * (1f - SmoothStep(35f, 45f, el));
                density *= mask;
                if (density > 0f)
                {
                    Color cc = Color.Lerp(cloudBase, cloudTop, SmoothStep(0.52f, 0.75f, nval));
                    c = Color.Lerp(c, cc, density * 0.9f);
                }
                c.a = 1f;
                px[y * w + x] = c;
            }
            return sky = Finish(new Texture2D(w, h, TextureFormat.RGB24, false), px, "cw_sky", TextureWrapMode.Clamp, 0, false);
        }

        /// <summary>View direction for a texel of Unity's Skybox/Panoramic latitude-longitude layout.</summary>
        static Vector3 Direction(float u, float v)
        {
            float lon = (0.5f - u) * 2f * Mathf.PI;
            float lat = (1f - v) * Mathf.PI;
            return new Vector3(Mathf.Cos(lon) * Mathf.Sin(lat), Mathf.Cos(lat), Mathf.Sin(lon) * Mathf.Sin(lat));
        }

        public static Color Hex(int rgb, float alpha = 1f)
        {
            return new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f, alpha);
        }
    }
}
