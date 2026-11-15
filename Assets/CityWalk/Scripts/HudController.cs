using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityWalk
{
    /// <summary>
    /// The in-game HUD in the target style: title card, lap counter, instruction banner with a gold
    /// edge, pace pill, XP, pause button and joystick. Reads RouteDirector state and listens to its
    /// events; it never changes game state itself.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        const float LapMessageSeconds = 2.6f;
        static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        static readonly Vector2 TopRight = new Vector2(1f, 1f);

        public VirtualJoystick Joystick { get; private set; }
        /// <summary>Safe-area root that other HUD pieces (like the coffee button) should parent to.</summary>
        public RectTransform Root { get; private set; }

        RouteDirector route;
        TextMeshProUGUI lapValue, bannerTitle, bannerBody, pillText, xpText;
        Image bannerFill, bannerBorder, bannerEdge, lapBorder;
        RectTransform lapCard;
        GameObject pill;
        PausePanel pausePanel;
        RouteCompletePanel completePanel;
        string lapMessageTitle, lapMessageBody;
        float lapMessageUntil = -1f, lapPulseAt = -10f;
        bool finished;

        public static HudController Create()
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var safe = UiFactory.MakeRect("SafeArea", canvasGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            HudStyle.Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();

            var hud = canvasGo.AddComponent<HudController>();
            hud.Root = safe;
            hud.Build(safe);
            return hud;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        void Build(Transform root)
        {
            var art = HudStyle.Art;

            // Title card
            var title = HudStyle.Card("TitleCard", root, TopLeft, TopLeft, new Vector2(40f, -36f), new Vector2(360f, 104f));
            HudStyle.Icon("Icon", title.transform, art.iconCity, new Vector2(0f, 0.5f), new Vector2(54f, 0f), 68f, Color.white);
            HudStyle.Label("Title", title.transform, "City Walk", 34, HudStyle.Text, TextAlignmentOptions.TopLeft, true).Inset(106f, 40f, 10f, 16f);
            HudStyle.Label("Subtitle", title.transform, "Morning routine", 22, HudStyle.TextMuted, TextAlignmentOptions.BottomLeft).Inset(106f, 18f, 10f, 56f);

            // Lap counter
            var lap = HudStyle.Card("LapCard", root, TopLeft, TopLeft, new Vector2(422f, -36f), new Vector2(126f, 104f),
                HudStyle.Panel, HudStyle.GoldEdge, out lapBorder);
            lapCard = lap.rectTransform;
            var lapLabel = HudStyle.Label("LapLabel", lap.transform, "LAP", 18, HudStyle.LabelMuted, TextAlignmentOptions.Top).Inset(0f, 0f, 0f, 14f);
            lapLabel.characterSpacing = 12f;
            lapValue = HudStyle.Label("LapValue", lap.transform, "1/3", 42, HudStyle.Text, TextAlignmentOptions.Bottom, true).Inset(0f, 12f, 0f, 0f);

            // Instruction banner with a gold edge on the left
            bannerFill = HudStyle.Card("Banner", root, TopLeft, TopLeft, new Vector2(592f, -36f), new Vector2(736f, 104f),
                HudStyle.Panel, HudStyle.GoldEdge, out bannerBorder);
            bannerEdge = UiFactory.Panel("GoldEdge", bannerFill.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(3f, 0f), new Vector2(6f, 84f), HudStyle.Gold);
            bannerEdge.sprite = null;   // plain bar
            bannerEdge.type = Image.Type.Simple;
            bannerEdge.raycastTarget = false;
            bannerTitle = HudStyle.Label("Title", bannerFill.transform, "", 32, HudStyle.Gold, TextAlignmentOptions.TopLeft, true).Inset(34f, 44f, 20f, 16f);
            bannerBody = HudStyle.Label("Body", bannerFill.transform, "", 22, HudStyle.Text, TextAlignmentOptions.BottomLeft).Inset(34f, 18f, 20f, 58f);

            // Pace pill
            var pillCard = HudStyle.Card("PacePill", root, TopLeft, TopLeft, new Vector2(592f, -160f), new Vector2(462f, 52f));
            HudStyle.Icon("Walker", pillCard.transform, art.iconWalker, new Vector2(0f, 0.5f), new Vector2(34f, 0f), 32f, HudStyle.Gold);
            pillText = HudStyle.Label("Text", pillCard.transform, "", 22, HudStyle.Text, TextAlignmentOptions.Left).Inset(60f, 0f, 16f, 0f);
            pill = pillCard.gameObject;

            // XP
            var xp = HudStyle.Card("XpCard", root, TopRight, TopRight, new Vector2(-152f, -42f), new Vector2(218f, 80f));
            HudStyle.Icon("Star", xp.transform, art.iconStar, new Vector2(0f, 0.5f), new Vector2(56f, 0f), 30f, HudStyle.Gold);
            xpText = HudStyle.Label("Value", xp.transform, "0 XP", 28, HudStyle.Text, TextAlignmentOptions.Left).Inset(82f, 0f, 12f, 0f);

            // Pause button
            var circle = art.circle != null ? art.circle : UiFactory.Circle;
            var pauseBg = UiFactory.Panel("PauseButton", root, TopRight, TopRight, new Vector2(-40f, -40f), new Vector2(88f, 88f),
                HudStyle.GoldEdge, circle);
            pauseBg.type = Image.Type.Simple;
            HudStyle.Icon("Disc", pauseBg.transform, circle, new Vector2(0.5f, 0.5f), Vector2.zero, 84f, HudStyle.Panel);
            HudStyle.Icon("Icon", pauseBg.transform, art.iconPause, new Vector2(0.5f, 0.5f), Vector2.zero, 40f, HudStyle.Text);
            pauseBg.gameObject.AddComponent<Button>().onClick.AddListener(() => pausePanel.Show());

            Joystick = VirtualJoystick.Create(root);

            pausePanel = PausePanel.Create(root);
            completePanel = RouteCompletePanel.Create(root, OnContinue);
        }

        /// <summary>Starts showing a route.</summary>
        public void Bind(RouteDirector director)
        {
            route = director;
            route.LapCompleted += (lap, seconds) =>
            {
                lapMessageTitle = $"Lap {lap} complete";
                lapMessageBody = $"Lap time {RouteDirector.FormatTime(seconds)}  ·  +{route.lapXp} XP";
                lapMessageUntil = Time.time + LapMessageSeconds;
                lapPulseAt = Time.time;
            };
            route.RouteCompleted += () =>
            {
                Joystick.gameObject.SetActive(false);
                completePanel.Show(route);
            };
            Refresh();
        }

        void OnContinue()
        {
            finished = true;
            Refresh();
        }

        void Update()
        {
            if (route != null) Refresh();
        }

        void Refresh()
        {
            lapValue.text = $"{route.CurrentLap}/{route.TotalLaps}";
            xpText.text = $"{route.Xp} XP";

            // Short pulse on the lap counter when a lap is done.
            float pulse = Mathf.Clamp01(1f - (Time.time - lapPulseAt) / 0.8f);
            lapCard.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(pulse * Mathf.PI));
            lapBorder.color = pulse > 0f ? HudStyle.Gold : HudStyle.GoldEdge;

            string title, body;
            bool warn = false;
            if (route.Phase == RoutePhase.Complete)
            {
                title = finished ? "Route finished" : "Arrived";
                body = finished ? "Your morning walk is done. See you tomorrow." : "You reached the office.";
            }
            else if (route.IsCoffeeMissed) { title = "Coffee missed"; body = "Go back to the kiosk and tap it. The lap will not count without it."; warn = true; }
            else if (route.IsWrongWay) { title = "Wrong way"; body = "Turn around and follow the green arrows."; warn = true; }
            else if (Time.time < lapMessageUntil) { title = lapMessageTitle; body = lapMessageBody; }
            else if (route.Phase == RoutePhase.ToOffice) { title = "Get to the office"; body = "Laps done. Follow the arrows to the yellow marker."; }
            else if (!route.CoffeeThisLap) { title = "Head for the yellow marker"; body = "The coffee kiosk is ahead. Tap it as you pass."; }
            else if (route.CurrentLap < route.TotalLaps) { title = "Coffee grabbed"; body = $"Keep going round the park to finish lap {route.CurrentLap}."; }
            else { title = "Last lap"; body = "Finish the lap, then head to the office."; }

            bannerTitle.text = title;
            bannerBody.text = body;
            bannerTitle.color = warn ? HudStyle.Warning : HudStyle.Gold;
            bannerFill.color = warn ? HudStyle.WarningPanel : HudStyle.Panel;
            bannerBorder.color = warn ? HudStyle.Warning : HudStyle.GoldEdge;
            bannerEdge.color = warn ? HudStyle.Warning : HudStyle.Gold;

            string pace = PaceHint.TextFor(route);
            if (pill.activeSelf != (pace != null)) pill.SetActive(pace != null);
            if (pace != null) pillText.text = pace;
        }
    }
}
