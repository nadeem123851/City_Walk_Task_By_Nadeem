using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CityWalk
{
    /// <summary>
    /// The in-game HUD in the target style: title card, lap counter, instruction banner with a gold
    /// edge, pace pill, XP, coins, pause button and joystick. Reads RouteDirector state and listens to its
    /// events; it never changes game state itself.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        static readonly Vector2 TopRight = new Vector2(1f, 1f);
        static readonly Vector2 Left = new Vector2(0f, 0.5f);

        // Pace pill, as in the web preview: green bar when brisk, orange bar and an orange pill when nudged.
        static readonly Color PaceBrisk = HudStyle.Hex(0x7FE0A0);
        static readonly Color PaceSlow = HudStyle.Hex(0xFFB347);
        static readonly Color PaceSlowPanel = new Color(92f / 255f, 54f / 255f, 8f / 255f, 0.94f);
        static readonly Color PaceTrack = new Color(1f, 1f, 1f, 0.16f);
        const float PaceTextLeft = 60f, PaceBarGap = 18f, PaceBarWidth = 120f, PaceRight = 28f;

        // XP float under the coin counter, for every XP gain ("Lap bonus +20 XP", "Brisk bonus +5 XP").
        const float XpFloatSeconds = 1.3f;
        static readonly Vector2 XpFloatPos = new Vector2(-152f, -206f);
        static readonly Color XpFloatColor = HudStyle.Hex(0xF3D27A);

        public VirtualJoystick Joystick { get; private set; }
        /// <summary>Safe-area root that other HUD pieces (like the coffee button) should parent to.</summary>
        public RectTransform Root { get; private set; }

        RouteDirector route;
        TextMeshProUGUI lapValue, bannerTitle, bannerBody, pillText, xpText, coinText, xpFloat;
        Image bannerFill, bannerBorder, bannerEdge, lapBorder, pillFill, pillBorder, paceFill;
        RectTransform lapCard, coinCard, pillRect, paceTrack;
        float xpFloatAt = -10f, paceTextWidth;
        GameObject pill;
        PausePanel pausePanel;
        SettingsPanel settingsPanel;
        RouteCompletePanel completePanel;
        float lapPulseAt = -10f, coinPulseAt = -10f;

        public static HudController Create()
        {
            var canvas = HudStyle.CreateCanvas("HUD", 10, out var safe);
            var hud = canvas.gameObject.AddComponent<HudController>();
            hud.Root = safe;
            hud.Build(safe);
            return hud;
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
            pillFill = HudStyle.Card("PacePill", root, TopLeft, TopLeft, new Vector2(592f, -160f), new Vector2(462f, 52f),
                HudStyle.Panel, HudStyle.GoldEdge, out pillBorder);
            pillRect = pillFill.rectTransform;
            HudStyle.Icon("Walker", pillFill.transform, art.iconWalker, Left, new Vector2(34f, 0f), 32f, HudStyle.Gold);
            pillText = HudStyle.Label("Text", pillFill.transform, "", 22, HudStyle.Text, TextAlignmentOptions.Left).Inset(PaceTextLeft, 0f, 16f, 0f);
            pill = pillFill.gameObject;
            // Pace bar: how fast the player is going, full at a brisk walk.
            var track = UiFactory.Panel("PaceBar", pillFill.transform, Left, Left, Vector2.zero, new Vector2(PaceBarWidth, 12f), PaceTrack);
            track.pixelsPerUnitMultiplier = 3f;   // 6 px corners on a 12 px bar
            track.raycastTarget = false;
            paceTrack = track.rectTransform;
            paceFill = UiFactory.Panel("Fill", paceTrack, Left, Left, Vector2.zero, new Vector2(0f, 12f), PaceBrisk);
            paceFill.pixelsPerUnitMultiplier = 3f;
            paceFill.raycastTarget = false;

            // XP
            var xp = HudStyle.Card("XpCard", root, TopRight, TopRight, new Vector2(-152f, -42f), new Vector2(218f, 80f));
            HudStyle.Icon("Star", xp.transform, art.iconStar, new Vector2(0f, 0.5f), new Vector2(56f, 0f), 30f, HudStyle.Gold);
            xpText = HudStyle.Label("Value", xp.transform, "0 XP", 28, HudStyle.Text, TextAlignmentOptions.Left).Inset(82f, 0f, 12f, 0f);

            // Coins, under the XP card: a gold coin drawn from two discs
            var coins = HudStyle.Card("CoinCard", root, TopRight, TopRight, new Vector2(-152f, -132f), new Vector2(218f, 64f));
            coinCard = coins.rectTransform;
            var disc = art.circle != null ? art.circle : UiFactory.Circle;
            HudStyle.Icon("Coin", coins.transform, disc, new Vector2(0f, 0.5f), new Vector2(56f, 0f), 34f, HudStyle.Gold);
            HudStyle.Icon("CoinInner", coins.transform, disc, new Vector2(0f, 0.5f), new Vector2(56f, 0f), 22f, HudStyle.Hex(0xC9962E));
            coinText = HudStyle.Label("Value", coins.transform, "0", 28, HudStyle.Text, TextAlignmentOptions.Left).Inset(82f, 0f, 12f, 0f);

            // XP float, right-aligned under the coin card
            var floatRect = UiFactory.MakeRect("XpFloat", root, TopRight, TopRight, XpFloatPos, new Vector2(422f, 44f));
            xpFloat = floatRect.gameObject.AddComponent<TextMeshProUGUI>();
            xpFloat.fontSize = 30;
            xpFloat.fontStyle = FontStyles.Bold;
            xpFloat.characterSpacing = -3f;
            xpFloat.color = XpFloatColor;
            xpFloat.alignment = TextAlignmentOptions.Right;
            xpFloat.textWrappingMode = TextWrappingModes.NoWrap;
            xpFloat.raycastTarget = false;
            floatRect.gameObject.SetActive(false);

            // Pause button
            var circle = art.circle != null ? art.circle : UiFactory.Circle;
            var pauseBg = UiFactory.Panel("PauseButton", root, TopRight, TopRight, new Vector2(-40f, -40f), new Vector2(88f, 88f),
                HudStyle.GoldEdge, circle);
            pauseBg.type = Image.Type.Simple;
            HudStyle.Icon("Disc", pauseBg.transform, circle, new Vector2(0.5f, 0.5f), Vector2.zero, 84f, HudStyle.Panel);
            HudStyle.Icon("Icon", pauseBg.transform, art.iconPause, new Vector2(0.5f, 0.5f), Vector2.zero, 40f, HudStyle.Text);
            var pauseButton = pauseBg.gameObject.AddComponent<Button>();
            pauseButton.onClick.AddListener(AudioHub.Click);
            pauseButton.onClick.AddListener(() => pausePanel.Show());

            // Settings gear, right under the pause button, in the same round style.
            var gearBg = UiFactory.Panel("SettingsButton", root, TopRight, TopRight, new Vector2(-40f, -144f), new Vector2(88f, 88f),
                HudStyle.GoldEdge, circle);
            gearBg.type = Image.Type.Simple;
            HudStyle.Icon("Disc", gearBg.transform, circle, new Vector2(0.5f, 0.5f), Vector2.zero, 84f, HudStyle.Panel);
            HudStyle.Icon("Icon", gearBg.transform, UiFactory.Gear, new Vector2(0.5f, 0.5f), Vector2.zero, 46f, HudStyle.Text);
            var gearButton = gearBg.gameObject.AddComponent<Button>();
            gearButton.onClick.AddListener(AudioHub.Click);
            gearButton.onClick.AddListener(() => settingsPanel.Open());

            Joystick = VirtualJoystick.Create(root);

            pausePanel = PausePanel.Create(root);
            completePanel = RouteCompletePanel.Create(root);
            settingsPanel = SettingsPanel.Create(root, true);
        }

        /// <summary>Starts showing a route.</summary>
        public void Bind(RouteDirector director)
        {
            route = director;
            route.LapCompleted += (lap, seconds) => lapPulseAt = Time.time;   // the lap card itself is LapCelebration
            route.CoinCollected += total => coinPulseAt = Time.time;
            route.XpGained += (amount, reason) =>
            {
                xpFloat.text = string.IsNullOrEmpty(reason) ? $"+{amount} XP" : $"{reason} +{amount} XP";
                xpFloatAt = Time.time;
                xpFloat.gameObject.SetActive(true);
            };
            route.RouteCompleted += () =>
            {
                Joystick.gameObject.SetActive(false);
                completePanel.Show(route);
            };
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
            coinText.text = route.Coins.ToString();
            float coinPulse = Mathf.Clamp01(1f - (Time.time - coinPulseAt) / 0.35f);
            coinCard.localScale = Vector3.one * (1f + 0.14f * Mathf.Sin(coinPulse * Mathf.PI));

            // Short pulse on the lap counter when a lap is done.
            float pulse = Mathf.Clamp01(1f - (Time.time - lapPulseAt) / 0.8f);
            lapCard.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(pulse * Mathf.PI));
            lapBorder.color = pulse > 0f ? HudStyle.Gold : HudStyle.GoldEdge;

            string title, body;
            bool warn = false;
            if (route.Phase == RoutePhase.Complete) { title = "Arrived"; body = "You reached the office."; }
            else if (route.IsCoffeeMissed) { title = "Coffee missed"; body = "Go back to the kiosk and tap it. The lap will not count without it."; warn = true; }
            else if (route.IsWrongWay) { title = "Wrong way"; body = "Turn around and follow the green arrows."; warn = true; }
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

            RefreshPace();
            RefreshXpFloat();
        }

        void RefreshPace()
        {
            string pace = PaceHint.TextFor(route);
            if (pill.activeSelf != (pace != null)) pill.SetActive(pace != null);
            if (pace == null) return;

            bool rush = route.IsRushHour, slow = route.IsPaceNudge;
            if (pillText.text != pace)
            {
                pillText.text = pace;
                paceTextWidth = pillText.GetPreferredValues(pace).x;
            }
            pillFill.color = slow ? PaceSlowPanel : HudStyle.Panel;
            pillBorder.color = slow ? PaceSlow : HudStyle.GoldEdge;

            // The pill fits its text, plus the bar in rush hour.
            float textWidth = paceTextWidth;
            float width = PaceTextLeft + textWidth + (rush ? PaceBarGap + PaceBarWidth : 0f) + PaceRight;
            pillRect.sizeDelta = new Vector2(width, pillRect.sizeDelta.y);

            if (paceTrack.gameObject.activeSelf != rush) paceTrack.gameObject.SetActive(rush);
            if (!rush) return;
            paceTrack.anchoredPosition = new Vector2(PaceTextLeft + textWidth + PaceBarGap, 0f);
            float speed = route.PlayerSpeed;
            float fill = Mathf.Clamp01(speed / route.FullPace) * PaceBarWidth;
            paceFill.rectTransform.sizeDelta = new Vector2(fill, 12f);
            paceFill.enabled = fill >= 1f;
            paceFill.color = speed >= route.BriskPace ? PaceBrisk : PaceSlow;
        }

        void RefreshXpFloat()
        {
            if (!xpFloat.gameObject.activeSelf) return;
            float a = (Time.time - xpFloatAt) / XpFloatSeconds;
            if (a >= 1f)
            {
                xpFloat.gameObject.SetActive(false);
                return;
            }
            xpFloat.rectTransform.anchoredPosition = XpFloatPos + new Vector2(0f, a * 24f);
            var c = XpFloatColor;
            c.a = 1f - Mathf.SmoothStep(0f, 1f, (a - 0.65f) / 0.35f);
            xpFloat.color = c;
        }
    }
}
