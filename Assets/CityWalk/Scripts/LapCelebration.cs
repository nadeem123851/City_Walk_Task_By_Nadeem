using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CityWalk
{
    /// <summary>
    /// The lap moment, as in the web preview: a "LAP n COMPLETE" card with the lap time pops in above the middle
    /// of the screen for 2.6 s and gold sparkles burst round the player. The "Lap bonus +20 XP" float is the HUD's
    /// XP float, and the lap sound is played by AudioHub.
    /// </summary>
    public class LapCelebration : MonoBehaviour
    {
        const float CardSeconds = 2.6f;
        static readonly Color Title = HudStyle.Hex(0xF3D27A);
        static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);

        RectTransform card;
        CanvasGroup cardGroup;
        TextMeshProUGUI cardTitle, cardBody;
        Transform player;
        float shownAt = -100f;

        public static LapCelebration Create(Transform hudRoot, RouteDirector route, Transform player)
        {
            var go = new GameObject("LapCelebration", typeof(RectTransform));
            go.transform.SetParent(hudRoot, false);
            HudStyle.Stretch((RectTransform)go.transform);
            // Under the pause and route-complete panels, so it never covers them.
            var pause = hudRoot.Find("PauseOverlay");
            if (pause != null) go.transform.SetSiblingIndex(pause.GetSiblingIndex());
            var celebration = go.AddComponent<LapCelebration>();
            celebration.player = player;
            celebration.Build(go.transform);
            route.LapCompleted += (lap, seconds) => celebration.Show(lap, seconds, route.lapXp);
            return celebration;
        }

        void Build(Transform root)
        {
            // Card: 700 x 150, its centre 135 px above the middle of the screen (web preview: top 330 of 1080).
            var img = HudStyle.Card("LapCompleteCard", root, Centre, Centre, new Vector2(0f, 135f), new Vector2(700f, 150f),
                HudStyle.Panel, HudStyle.Gold, out _);
            img.raycastTarget = false;
            card = img.rectTransform;
            cardGroup = img.gameObject.AddComponent<CanvasGroup>();
            cardGroup.blocksRaycasts = false;
            cardGroup.interactable = false;
            cardTitle = HudStyle.Label("Title", card, "", 50, Title, TextAlignmentOptions.Top, true).Inset(0f, 0f, 0f, 22f);
            cardTitle.characterSpacing = 1f;
            cardBody = HudStyle.Label("Body", card, "", 27, HudStyle.Text, TextAlignmentOptions.Bottom).Inset(0f, 26f, 0f, 0f);

            card.gameObject.SetActive(false);
        }

        void Show(int lap, float seconds, int xp)
        {
            cardTitle.text = $"LAP {lap} COMPLETE";
            cardBody.text = $"Lap time {RouteDirector.FormatTime(seconds)}  ·  +{xp} XP";
            shownAt = Time.time;
            card.gameObject.SetActive(true);
            card.SetAsLastSibling();
            if (player != null) Sparkles.Burst(player.position + Vector3.up * 1.4f, 30, 3.6f);
        }

        void Update()
        {
            if (!card.gameObject.activeSelf) return;
            float t = Time.time - shownAt;

            // Card: fade in over the first 10 percent, scale 0.7 to 1 over the first 12 percent, fade out over the last 15.
            float a = t / CardSeconds;
            if (a >= 1f)
            {
                card.gameObject.SetActive(false);
                return;
            }
            cardGroup.alpha = Mathf.SmoothStep(0f, 1f, a / 0.1f) * (1f - Mathf.SmoothStep(0f, 1f, (a - 0.85f) / 0.15f));
            card.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, Mathf.SmoothStep(0f, 1f, a / 0.12f));
        }
    }
}
