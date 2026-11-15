using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityWalk
{
    /// <summary>
    /// Fixed-position on-screen joystick for touch screens (also works with a mouse).
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public float radius = 74f;
        public Vector2 Value { get; private set; }

        RectTransform baseRect;
        RectTransform knob;

        public static VirtualJoystick Create(Transform canvas)
        {
            var art = UiArt.Instance;
            var centre = new Vector2(0.5f, 0.5f);

            // Dark green disc with a thin gold rim (a slightly larger gold disc behind it), and a pearl knob
            var circle = art.circle != null ? art.circle : UiFactory.Circle;
            var bg = UiFactory.Panel("Joystick", canvas, new Vector2(0f, 0f), centre,
                new Vector2(237f, 207f), new Vector2(252f, 252f), HudStyle.GoldEdge, circle);
            bg.type = Image.Type.Simple;
            var disc = UiFactory.Panel("Disc", bg.transform, centre, centre, Vector2.zero, new Vector2(248f, 248f), HudStyle.Panel, circle);
            disc.type = Image.Type.Simple;
            disc.raycastTarget = false;
            var knobImg = UiFactory.Panel("Knob", bg.transform, centre, centre,
                Vector2.zero, new Vector2(104f, 104f), Color.white, art.knob != null ? art.knob : UiFactory.Circle);
            knobImg.type = Image.Type.Simple;
            knobImg.raycastTarget = false;

            var js = bg.gameObject.AddComponent<VirtualJoystick>();
            js.baseRect = (RectTransform)bg.transform;
            js.knob = (RectTransform)knobImg.transform;
            return js;
        }

        public void OnPointerDown(PointerEventData e) => OnDrag(e);

        public void OnDrag(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, e.position, e.pressEventCamera, out var local))
                return;
            var clamped = Vector2.ClampMagnitude(local, radius);
            knob.anchoredPosition = clamped;
            Value = clamped / radius;
        }

        public void OnPointerUp(PointerEventData e)
        {
            knob.anchoredPosition = Vector2.zero;
            Value = Vector2.zero;
        }

        void OnDisable()
        {
            Value = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
        }
    }
}
