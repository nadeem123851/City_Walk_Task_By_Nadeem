using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CityWalk
{
    /// <summary>
    /// Settings card: Music (the city ambience) and Sound effects, each with a volume slider and an On/Off switch.
    /// Changes are heard at once and saved when the card closes. Opens from the home screen and from the gear
    /// under the in-game pause button; in the game it pauses play while open.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);
        static readonly Vector2 Top = new Vector2(0.5f, 1f);
        static readonly Vector2 Bottom = new Vector2(0.5f, 0f);
        static readonly Color Track = new Color(1f, 1f, 1f, 0.16f);

        class Row
        {
            public Slider slider;
            public TextMeshProUGUI percent;
            public Image switchFill;
            public TextMeshProUGUI switchLabel;
            public Func<float> volume;
            public Func<bool> muted;
        }

        Row music, sfx;
        bool pausesGame, pausedByMe;
        bool refreshing;

        public bool IsOpen => gameObject.activeSelf;

        /// <summary>Builds the card under <paramref name="root"/>. With <paramref name="pauseGame"/> it stops game time while open.</summary>
        public static SettingsPanel Create(Transform root, bool pauseGame)
        {
            var dim = HudStyle.DimOverlay("SettingsOverlay", root);
            var panel = dim.gameObject.AddComponent<SettingsPanel>();
            panel.pausesGame = pauseGame;
            panel.Build(dim.transform);
            dim.gameObject.SetActive(false);
            return panel;
        }

        void Build(Transform parent)
        {
            var card = HudStyle.Card("Card", parent, Centre, Centre, Vector2.zero, new Vector2(820f, 470f),
                HudStyle.PanelSolid, HudStyle.Gold, out _).transform;
            HudStyle.Label("Title", card, "Settings", 52, HudStyle.Text, TextAlignmentOptions.Top, true).Inset(0f, 0f, 0f, 46f);

            music = MakeRow(card, "Music", -170f, () => GameSettings.MusicVolume, () => GameSettings.MusicMuted,
                v => GameSettings.MusicVolume = v, m => GameSettings.MusicMuted = m);
            sfx = MakeRow(card, "Sound effects", -270f, () => GameSettings.SfxVolume, () => GameSettings.SfxMuted,
                v => GameSettings.SfxVolume = v, m => GameSettings.SfxMuted = m);

            HudStyle.GoldButton("CloseButton", card, "Done", Bottom, new Vector2(0f, 76f), new Vector2(300f, 86f))
                .onClick.AddListener(Close);
        }

        Row MakeRow(Transform card, string title, float y, Func<float> volume, Func<bool> muted, Action<float> setVolume, Action<bool> setMuted)
        {
            var row = new Row { volume = volume, muted = muted };

            var label = HudStyle.Label(title + "Label", card, title, 30, HudStyle.Text, TextAlignmentOptions.Left);
            Place(label.rectTransform, new Vector2(-250f, y), new Vector2(220f, 60f));

            row.slider = MakeSlider(title + "Slider", card, new Vector2(-5f, y), new Vector2(290f, 44f));
            row.slider.onValueChanged.AddListener(v =>
            {
                if (refreshing) return;
                setVolume(v);
                if (v > 0.001f && muted()) setMuted(false);   // moving the slider turns the sound back on
                Refresh();
            });

            row.percent = HudStyle.Label(title + "Percent", card, "", 26, HudStyle.TextMuted, TextAlignmentOptions.Right);
            Place(row.percent.rectTransform, new Vector2(198f, y), new Vector2(90f, 50f));

            // On/Off switch: gold with dark text when on, dark with a gold outline when off.
            var sprite = HudStyle.Art.pill != null ? HudStyle.Art.pill : UiFactory.Rounded;
            var edge = UiFactory.Panel(title + "Switch", card, Top, Centre, new Vector2(330f, y), new Vector2(110f, 58f), HudStyle.Gold, sprite);
            row.switchFill = UiFactory.Panel("Fill", edge.transform, Centre, Centre, Vector2.zero, new Vector2(105f, 53f), HudStyle.Gold, sprite);
            row.switchFill.raycastTarget = false;
            row.switchLabel = HudStyle.Label("Label", edge.transform, "", 26, HudStyle.ButtonInk, TextAlignmentOptions.Center, true);
            var button = edge.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(AudioHub.Click);
            button.onClick.AddListener(() => { setMuted(!muted()); Refresh(); });
            return row;
        }

        static Slider MakeSlider(string name, Transform card, Vector2 pos, Vector2 size)
        {
            var root = UiFactory.MakeRect(name, card, Top, Centre, pos, size);
            var slider = root.gameObject.AddComponent<Slider>();

            var track = UiFactory.Panel("Track", root, Centre, Centre, Vector2.zero, new Vector2(size.x, 12f), Track);
            track.pixelsPerUnitMultiplier = 3f;   // 6 px corners on a 12 px bar

            var fillArea = UiFactory.MakeRect("FillArea", root, Centre, Centre, Vector2.zero, new Vector2(size.x, 12f));
            var fill = UiFactory.Panel("Fill", fillArea, Centre, Centre, Vector2.zero, Vector2.zero, HudStyle.Gold);
            fill.pixelsPerUnitMultiplier = 3f;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);

            // The knob travels inside the track, so its centre stops half a knob from each end.
            var handleArea = UiFactory.MakeRect("HandleArea", root, Centre, Centre, Vector2.zero, new Vector2(size.x - 40f, size.y));
            var knobSprite = HudStyle.Art.circle != null ? HudStyle.Art.circle : UiFactory.Circle;
            var handle = UiFactory.Panel("Handle", handleArea, Centre, Centre, Vector2.zero, new Vector2(40f, 0f), HudStyle.Text, knobSprite);
            handle.type = Image.Type.Simple;
            handle.preserveAspect = true;
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            return slider;
        }

        static void Place(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = Top;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (pausesGame && Time.timeScale > 0f)
            {
                Time.timeScale = 0f;
                pausedByMe = true;
            }
            Refresh();
        }

        public void Close()
        {
            GameSettings.Save();
            gameObject.SetActive(false);
            if (pausedByMe) Time.timeScale = 1f;
            pausedByMe = false;
        }

        void Refresh()
        {
            refreshing = true;
            foreach (var row in new[] { music, sfx })
            {
                bool on = !row.muted();
                row.slider.value = row.volume();
                row.percent.text = $"{Mathf.RoundToInt(row.volume() * 100f)}%";
                row.switchFill.color = on ? HudStyle.Gold : HudStyle.PanelSolid;
                row.switchLabel.text = on ? "On" : "Off";
                row.switchLabel.color = on ? HudStyle.ButtonInk : HudStyle.Text;
                // A muted row's slider dims, so it is clear nothing is playing.
                row.slider.fillRect.GetComponent<Image>().color = on ? HudStyle.Gold : HudStyle.GoldEdge;
            }
            refreshing = false;
        }

        void OnDestroy()
        {
            if (pausedByMe) Time.timeScale = 1f;
        }
    }
}
