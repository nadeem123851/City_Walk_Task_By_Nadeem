using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CityWalk
{
    /// <summary>
    /// The HUD look: dark green panels with a thin gold edge, gold accents and TextMeshPro text.
    /// Colours from the plan (section 7). Helpers build the common pieces in code.
    /// </summary>
    public static class HudStyle
    {
        // The plan says #092E26 at 90 percent. Unity blends UI in linear colour space, which lets a bright
        // background lift a dark panel far more than the browser did, so 97 percent gives the same look on screen.
        public static readonly Color Panel = Hex(0x092E26, 0.97f);
        // Cards and dialogs are fully opaque: in linear colour space even 3 percent see-through shows the city clearly.
        public static readonly Color PanelSolid = Hex(0x0B3A30, 1f);
        public static readonly Color Gold = Hex(0xE3BD62);
        public static readonly Color GoldEdge = Hex(0xD6B25E, 0.45f);
        public static readonly Color Text = Hex(0xF4EFE0);
        public static readonly Color TextMuted = Hex(0xF4EFE0, 0.72f);
        public static readonly Color LabelMuted = Hex(0xC9C3A8);
        public static readonly Color Warning = Hex(0xFF8A5C);
        public static readonly Color WarningPanel = Hex(0x4A1C14, 0.92f);
        public static readonly Color ButtonInk = Hex(0x0C3B30);
        public static readonly Color Dim = new Color(0f, 0f, 0f, 0.45f);

        static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);

        public static UiArt Art => UiArt.Instance;

        /// <summary>
        /// Screen-space canvas scaled from 1920x1080 (match 0.5), with an event system.
        /// Returns the canvas; <paramref name="safeArea"/> is a child rect kept clear of notches.
        /// </summary>
        public static Canvas CreateCanvas(string name, int sortingOrder, out RectTransform safeArea)
        {
            EnsureEventSystem();
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            safeArea = UiFactory.MakeRect("SafeArea", go.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeArea>();
            return canvas;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        /// <summary>A small confirm dialog: title, message, and two buttons. Hidden until shown.</summary>
        public static GameObject ConfirmDialog(string name, Transform parent, string title, string message,
            string confirmLabel, System.Action onConfirm, string cancelLabel, System.Action onCancel)
        {
            var dim = DimOverlay(name, parent);
            var card = Card("Card", dim.transform, Centre, Centre, Vector2.zero, new Vector2(700f, 330f), PanelSolid, Gold, out _);
            Label("Title", card.transform, title, 44, Text, TMPro.TextAlignmentOptions.Top, true).Inset(0f, 0f, 0f, 44f);
            Label("Message", card.transform, message, 26, TextMuted, TMPro.TextAlignmentOptions.Top).Inset(30f, 0f, 30f, 112f);
            OutlineButton("Confirm", card.transform, confirmLabel, new Vector2(0.5f, 0f), new Vector2(-135f, 82f), new Vector2(240f, 84f))
                .onClick.AddListener(() => onConfirm?.Invoke());
            GoldButton("Cancel", card.transform, cancelLabel, new Vector2(0.5f, 0f), new Vector2(135f, 82f), new Vector2(240f, 84f))
                .onClick.AddListener(() => onCancel?.Invoke());
            dim.gameObject.SetActive(false);
            return dim.gameObject;
        }

        /// <summary>A panel: tinted fill plus a thin gold border on top.</summary>
        public static Image Card(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
            Color fill, Color edge, out Image border)
        {
            var img = UiFactory.Panel(name, parent, anchor, pivot, pos, size, fill, Art.panel != null ? Art.panel : UiFactory.Rounded);
            border = UiFactory.Panel("Border", img.transform, Centre, Centre, Vector2.zero, Vector2.zero, edge,
                Art.panelBorder != null ? Art.panelBorder : UiFactory.Rounded);
            Stretch(border.rectTransform);
            border.raycastTarget = false;
            return img;
        }

        public static Image Card(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            return Card(name, parent, anchor, pivot, pos, size, Panel, GoldEdge, out _);
        }

        /// <summary>TextMeshPro label laid out inside a rect relative to its parent.</summary>
        public static TextMeshProUGUI Label(string name, Transform parent, string text, float size, Color color,
            TextAlignmentOptions align, bool bold = false)
        {
            var rt = UiFactory.MakeRect(name, parent, Centre, Centre, Vector2.zero, Vector2.zero);
            Stretch(rt);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            if (bold) t.characterSpacing = -3f;   // LiberationSans SDF adds extra space to bold glyphs
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        /// <summary>Places a label: left/bottom/right/top insets inside its parent.</summary>
        public static TextMeshProUGUI Inset(this TextMeshProUGUI t, float left, float bottom, float right, float top)
        {
            t.rectTransform.offsetMin = new Vector2(left, bottom);
            t.rectTransform.offsetMax = new Vector2(-right, -top);
            return t;
        }

        public static Image Icon(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 pos, float size, Color tint)
        {
            var img = UiFactory.Panel(name, parent, anchor, Centre, pos, new Vector2(size, size), tint, sprite);
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Gold pill button with dark text.</summary>
        public static Button GoldButton(string name, Transform parent, string text, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var img = UiFactory.Panel(name, parent, anchor, Centre, pos, size, Gold, Art.pill != null ? Art.pill : UiFactory.Rounded);
            Label("Label", img.transform, text, 30, ButtonInk, TextAlignmentOptions.Center, true);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(AudioHub.Click);
            return b;
        }

        /// <summary>Dark pill button with a gold outline and light text.</summary>
        public static Button OutlineButton(string name, Transform parent, string text, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var sprite = Art.pill != null ? Art.pill : UiFactory.Rounded;
            // Gold pill with a slightly smaller dark pill on top leaves a thin gold outline.
            var edge = UiFactory.Panel(name, parent, anchor, Centre, pos, size, Gold, sprite);
            var fill = UiFactory.Panel("Fill", edge.transform, Centre, Centre, Vector2.zero, size - new Vector2(5f, 5f), PanelSolid, sprite);
            fill.raycastTarget = false;
            Label("Label", edge.transform, text, 30, Text, TextAlignmentOptions.Center, true);
            var b = edge.gameObject.AddComponent<Button>();
            b.targetGraphic = fill;
            b.onClick.AddListener(AudioHub.Click);
            return b;
        }

        /// <summary>A full-screen dark overlay that blocks taps behind it.</summary>
        public static Image DimOverlay(string name, Transform parent)
        {
            var rt = UiFactory.MakeRect(name, parent, Centre, Centre, Vector2.zero, Vector2.zero);
            Stretch(rt);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Dim;
            return img;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static Color Hex(int rgb, float alpha = 1f)
        {
            return new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f, alpha);
        }
    }
}
