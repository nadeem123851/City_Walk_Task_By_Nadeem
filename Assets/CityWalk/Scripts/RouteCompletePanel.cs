using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CityWalk
{
    /// <summary>
    /// End card: stars, a summary of the run, XP, coins and time, with Restart (play the route again)
    /// and Home (back to the home screen). Fades in shortly after arrival.
    /// </summary>
    public class RouteCompletePanel : MonoBehaviour
    {
        static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);
        static readonly Vector2 Top = new Vector2(0.5f, 1f);
        static readonly Color StatFill = HudStyle.Hex(0x154238, 0.9f);
        static readonly Color StatEdge = HudStyle.Hex(0xD6B25E, 0.3f);
        static readonly Color StarOff = new Color(1f, 1f, 1f, 0.18f);
        const float ShowDelay = 1.2f, FadeTime = 0.4f;

        CanvasGroup group;
        RectTransform card;
        TextMeshProUGUI summary, xpValue, coinValue, timeValue;
        readonly List<Image> stars = new List<Image>();
        float shownAt = -1f;

        public static RouteCompletePanel Create(Transform root)
        {
            var dim = HudStyle.DimOverlay("RouteCompleteOverlay", root);
            var panel = dim.gameObject.AddComponent<RouteCompletePanel>();
            panel.group = dim.gameObject.AddComponent<CanvasGroup>();
            panel.Build(dim.transform);
            dim.gameObject.SetActive(false);
            return panel;
        }

        void Build(Transform parent)
        {
            var art = HudStyle.Art;
            var c = HudStyle.Card("Card", parent, Centre, Centre, Vector2.zero, new Vector2(760f, 630f),
                HudStyle.PanelSolid, HudStyle.Gold, out _);
            card = c.rectTransform;

            var heading = HudStyle.Label("Heading", card, "ROUTE COMPLETE", 22, HudStyle.Gold, TextAlignmentOptions.Top).Inset(0f, 0f, 0f, 48f);
            heading.characterSpacing = 14f;
            HudStyle.Label("Title", card, "You made it to the office", 50, HudStyle.Text, TextAlignmentOptions.Top, true).Inset(0f, 0f, 0f, 88f);

            for (int i = 0; i < 3; i++)
                stars.Add(HudStyle.Icon("Star" + i, card, art.iconStar, Top, new Vector2((i - 1) * 60f, -185f), 48f, HudStyle.Gold));

            summary = HudStyle.Label("Summary", card, "", 24, HudStyle.TextMuted, TextAlignmentOptions.Top).Inset(20f, 0f, 20f, 228f);

            xpValue = Stat("Xp", -220f, "XP");
            coinValue = Stat("Coins", 0f, "Coins");
            timeValue = Stat("Time", 220f, "Time");

            HudStyle.OutlineButton("Restart", card, "Restart", Top, new Vector2(-137f, -512f), new Vector2(252f, 84f))
                .onClick.AddListener(GameScenes.Restart);
            HudStyle.GoldButton("Home", card, "Home", Top, new Vector2(137f, -512f), new Vector2(250f, 84f))
                .onClick.AddListener(GameScenes.LoadHome);
        }

        TextMeshProUGUI Stat(string name, float x, string label)
        {
            var box = HudStyle.Card(name, card, Top, Top, new Vector2(x, -295f), new Vector2(190f, 130f), StatFill, StatEdge, out _);
            HudStyle.Label("Label", box.transform, label, 22, HudStyle.Text, TextAlignmentOptions.Bottom).Inset(0f, 22f, 0f, 0f);
            return HudStyle.Label("Value", box.transform, "", 44, HudStyle.Gold, TextAlignmentOptions.Top, true).Inset(0f, 0f, 0f, 22f);
        }

        public void Show(RouteDirector route)
        {
            for (int i = 0; i < stars.Count; i++) stars[i].color = i < route.Stars ? HudStyle.Gold : StarOff;

            var bits = new List<string> { $"{route.TotalLaps} laps", Plural(route.Coffees, "coffee"), Plural(route.WrongTurns, "wrong turn") };
            if (route.MissedKiosks > 0) bits.Add(Plural(route.MissedKiosks, "missed kiosk"));
            summary.text = string.Join("  ·  ", bits);

            xpValue.text = $"+{route.Xp}";
            coinValue.text = $"+{route.Coins}";
            timeValue.text = RouteDirector.FormatTime(route.ElapsedTime);

            shownAt = Time.unscaledTime;
            group.alpha = 0f;
            group.interactable = group.blocksRaycasts = false;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        void Update()
        {
            if (shownAt < 0f) return;
            float t = Mathf.Clamp01((Time.unscaledTime - shownAt - ShowDelay) / FadeTime);
            group.alpha = t;
            group.interactable = group.blocksRaycasts = t > 0.5f;
            card.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, Mathf.SmoothStep(0f, 1f, t));
        }

        static string Plural(int n, string word) => $"{n} {word}{(n == 1 ? "" : "s")}";
    }
}
