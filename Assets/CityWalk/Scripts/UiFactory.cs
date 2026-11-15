using UnityEngine;
using UnityEngine.UI;

namespace CityWalk
{
    /// <summary>
    /// Builds basic uGUI elements in code (rounded panels, circles, text)
    /// so the starter project has no art or prefab dependencies.
    /// </summary>
    public static class UiFactory
    {
        static Sprite roundedSprite, circleSprite, gearSprite;
        static Font font;

        public static Font DefaultFont
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Sprite Rounded
        {
            get
            {
                if (roundedSprite == null) roundedSprite = MakeRounded(64, 18);
                return roundedSprite;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circleSprite == null) circleSprite = MakeRounded(128, 64);
                return circleSprite;
            }
        }

        /// <summary>A white settings gear (eight teeth and a hole), for tinting. Drawn once in code: the art pack has none.</summary>
        public static Sprite Gear
        {
            get
            {
                if (gearSprite == null) gearSprite = MakeGear(128);
                return gearSprite;
            }
        }

        static Sprite MakeGear(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            float c = size * 0.5f, body = size * 0.32f, tooth = size * 0.13f, hole = size * 0.13f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - c, dy = y + 0.5f - c;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float wave = Mathf.Cos(8f * Mathf.Atan2(dy, dx));                     // eight teeth round the rim
                    float edge = body + tooth * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.15f, 0.35f, wave));
                    float a = Mathf.Clamp01(edge - r + 0.5f) * Mathf.Clamp01(r - hole + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeRounded(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    byte a = (byte)(Mathf.Clamp01(radius - d + 0.5f) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            float border = Mathf.Min(radius, size / 2 - 1);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
        }

        public static RectTransform MakeRect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Panel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = MakeRect(name, parent, anchor, pivot, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? sprite : Rounded;
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        public static Text Label(string name, Transform parent, string text, int fontSize, Color color, TextAnchor align, FontStyle style = FontStyle.Normal)
        {
            var rt = MakeRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(16f, 6f);
            rt.offsetMax = new Vector2(-16f, -6f);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = DefaultFont;
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }
    }
}
