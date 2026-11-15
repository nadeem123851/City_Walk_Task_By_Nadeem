using TMPro;
using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Mission failed card: shown when a lap runs over its time limit. Laps done, coins and time, with Home (back to
    /// the home screen) and Retry (play the route again). Fades in a moment after the run stops.
    /// </summary>
    public class MissionFailedPanel : MonoBehaviour
    {
        static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);
        static readonly Vector2 Top = new Vector2(0.5f, 1f);
        static readonly Color StatFill = HudStyle.Hex(0x154238, 0.9f);
        static readonly Color StatEdge = HudStyle.Hex(0xD6B25E, 0.3f);
        const float ShowDelay = 1f, FadeTime = 0.4f;

        CanvasGroup group;
        RectTransform card;
        TextMeshProUGUI summary, lapsValue, coinValue, timeValue;
        float shownAt = -1f;

        public static MissionFailedPanel Create(Transform root)
        {
            var dim = HudStyle.DimOverlay("MissionFailedOverlay", root);
            var panel = dim.gameObject.AddComponent<MissionFailedPanel>();
            panel.group = dim.gameObject.AddComponent<CanvasGroup>();
            panel.Build(dim.transform);
            dim.gameObject.SetActive(false);
            return panel;
        }

        void Build(Transform parent)
        {
            var c = HudStyle.Card("Card", parent, Centre, Centre, Vector2.zero, new Vector2(760f, 580f),
                HudStyle.PanelSolid, HudStyle.Warning, out _);
            card = c.rectTransform;

            var heading = HudStyle.Label("Heading", card, "MISSION FAILED", 22, HudStyle.Warning, TextAlignmentOptions.Top).Inset(0f, 0f, 0f, 48f);
            heading.characterSpacing = 14f;
            HudStyle.Label("Title", card, "Out of time", 50, HudStyle.Text, TextAlignmentOptions.Top, true).Inset(0f, 0f, 0f, 88f);

            summary = HudStyle.Label("Summary", card, "", 24, HudStyle.TextMuted, TextAlignmentOptions.Top).Inset(60f, 0f, 60f, 168f);
            summary.textWrappingMode = TextWrappingModes.Normal;

            lapsValue = Stat("Laps", -220f, "Laps");
            coinValue = Stat("Coins", 0f, "Coins");
            timeValue = Stat("Time", 220f, "Time");

            HudStyle.OutlineButton("Home", card, "Home", Top, new Vector2(-137f, -492f), new Vector2(252f, 84f))
                .onClick.AddListener(GameScenes.LoadHome);
            HudStyle.GoldButton("Retry", card, "Retry", Top, new Vector2(137f, -492f), new Vector2(250f, 84f))
                .onClick.AddListener(GameScenes.Restart);
        }

        TextMeshProUGUI Stat(string name, float x, string label)
        {
            var box = HudStyle.Card(name, card, Top, Top, new Vector2(x, -275f), new Vector2(190f, 130f), StatFill, StatEdge, out _);
            HudStyle.Label("Label", box.transform, label, 22, HudStyle.Text, TextAlignmentOptions.Bottom).Inset(0f, 22f, 0f, 0f);
            return HudStyle.Label("Value", box.transform, "", 44, HudStyle.Gold, TextAlignmentOptions.Top, true).Inset(0f, 0f, 0f, 22f);
        }

        public void Show(RouteDirector route)
        {
            summary.text = $"Lap {route.CurrentLap} took longer than {RouteDirector.FormatTime(route.lapTimeLimit)}. " +
                           "Walk briskly and grab the coffee on the way round.";
            lapsValue.text = $"{route.LapsCompleted}/{route.TotalLaps}";
            coinValue.text = route.Coins.ToString();
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
    }
}
