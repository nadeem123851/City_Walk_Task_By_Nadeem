using TMPro;
using UnityEngine;

namespace CityWalk
{
    /// <summary>Pause card: stops game time while open. Resume carries on, Home goes back to the home screen.</summary>
    public class PausePanel : MonoBehaviour
    {
        static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);

        public bool IsPaused { get; private set; }

        public static PausePanel Create(Transform root)
        {
            var dim = HudStyle.DimOverlay("PauseOverlay", root);
            var panel = dim.gameObject.AddComponent<PausePanel>();

            var card = HudStyle.Card("Card", dim.transform, Centre, Centre, new Vector2(0f, 10f), new Vector2(600f, 300f),
                HudStyle.PanelSolid, HudStyle.Gold, out _);
            HudStyle.Label("Title", card.transform, "Paused", 52, HudStyle.Text, TextAlignmentOptions.Top, true).Inset(0f, 0f, 0f, 50f);
            HudStyle.OutlineButton("Home", card.transform, "Home", new Vector2(0.5f, 0f), new Vector2(-137f, 92f), new Vector2(250f, 86f))
                .onClick.AddListener(panel.GoHome);
            HudStyle.GoldButton("Resume", card.transform, "Resume", new Vector2(0.5f, 0f), new Vector2(137f, 92f), new Vector2(250f, 86f))
                .onClick.AddListener(panel.Resume);

            dim.gameObject.SetActive(false);
            return panel;
        }

        public void Show()
        {
            IsPaused = true;
            Time.timeScale = 0f;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            gameObject.SetActive(false);
        }

        /// <summary>Leaves the run for the home screen, with game time running again.</summary>
        public void GoHome()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            GameScenes.LoadHome();
        }

        void OnDestroy()
        {
            if (IsPaused) Time.timeScale = 1f;
        }
    }
}
