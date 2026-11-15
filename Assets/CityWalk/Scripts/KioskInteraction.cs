using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CityWalk
{
    /// <summary>
    /// The contextual coffee action. A large "Tap to grab coffee" button appears near the kiosk;
    /// tapping it takes the coffee for this lap. Walking on past without tapping reports a missed
    /// coffee to the route. Works with touch, mouse, and the E key in the editor.
    /// </summary>
    public class KioskInteraction : MonoBehaviour
    {
        static readonly Color ButtonInk = new Color(0.047f, 0.231f, 0.188f);   // #0C3B30
        static readonly Color ButtonEdge = new Color(0.42f, 0.33f, 0.15f, 0.9f);
        const float ButtonSize = 230f;

        RouteDirector route;
        Transform player;
        KioskRules rules;
        RectTransform button;
        float shownAt;

        public static KioskInteraction Create(RouteDirector route, Transform player, Transform hudRoot)
        {
            var go = new GameObject("KioskInteraction");
            go.transform.SetParent(route.transform, false);
            var k = go.AddComponent<KioskInteraction>();
            k.Init(route, player, hudRoot);
            return k;
        }

        void Init(RouteDirector director, Transform playerTransform, Transform hudRoot)
        {
            route = director;
            player = playerTransform;
            rules = new KioskRules(new Vector2(route.kioskMarker.x, route.kioskMarker.z),
                                   new Vector2(route.parkCentre.x, route.parkCentre.z));

            route.CoffeeGrabbed += rules.OnCoffeeTaken;
            route.LapCompleted += (lap, time) => rules.OnLapCompleted();

            button = BuildButton(hudRoot);
            button.gameObject.SetActive(false);
        }

        RectTransform BuildButton(Transform hudRoot)
        {
            var art = UiArt.Instance;
            var anchor = new Vector2(1f, 0f);
            var centre = new Vector2(0.5f, 0.5f);

            // Dark gold rim behind the button face
            var rim = UiFactory.Panel("CoffeeButton", hudRoot, anchor, centre, new Vector2(-160f, 255f),
                new Vector2(ButtonSize + 14f, ButtonSize + 14f), ButtonEdge, UiFactory.Circle);
            rim.type = Image.Type.Simple;

            var face = UiFactory.Panel("Face", rim.transform, centre, centre, Vector2.zero,
                new Vector2(ButtonSize, ButtonSize), Color.white, art.buttonGold != null ? art.buttonGold : UiFactory.Circle);
            face.type = Image.Type.Simple;
            if (art.buttonGold == null) face.color = new Color(0.89f, 0.74f, 0.38f);

            if (art.iconCoffee != null)
            {
                var icon = UiFactory.Panel("Icon", face.transform, centre, centre, new Vector2(0f, 42f), new Vector2(72f, 72f), Color.white, art.iconCoffee);
                icon.type = Image.Type.Simple;
                icon.raycastTarget = false;
            }

            HudStyle.Label("Label", face.transform, "Tap to grab\ncoffee", 30, ButtonInk, TMPro.TextAlignmentOptions.Center, true)
                .Inset(10f, 20f, 10f, 110f).lineSpacing = -12f;

            var btn = rim.gameObject.AddComponent<Button>();
            btn.targetGraphic = face;
            btn.onClick.AddListener(Tap);
            return rim.rectTransform;
        }

        void Tap()
        {
            if (!rules.ButtonVisible) return;
            route.GrabCoffee();
        }

        void Update()
        {
            if (route == null || player == null) return;

            bool active = route.Phase == RoutePhase.Laps;
            var p = player.position;
            if (rules.Step(new Vector2(p.x, p.z), route.CoffeeThisLap, active))
                route.ReportCoffeeMissed();

            bool show = rules.ButtonVisible && Time.timeScale > 0f;
            if (show != button.gameObject.activeSelf)
            {
                button.gameObject.SetActive(show);
                if (show) shownAt = Time.unscaledTime;
            }

            if (show)
            {
                // Pop in, then a gentle pulse so it catches the eye.
                float t = Mathf.Clamp01((Time.unscaledTime - shownAt) / 0.25f);
                float scale = Mathf.Lerp(0.6f, 1f, Mathf.SmoothStep(0f, 1f, t)) * (1f + 0.03f * Mathf.Sin(Time.unscaledTime * 6f));
                button.localScale = Vector3.one * scale;

                if (KeyPressed()) Tap();
            }
        }

        static bool KeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.E);
#else
            return false;
#endif
        }
    }
}
