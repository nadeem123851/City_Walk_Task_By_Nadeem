using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CityWalk
{
    /// <summary>
    /// "Choose your walker": takes the menu card's place on the home screen while the 3D character on the right
    /// changes as the player browses. Arrows step through the characters, Select saves the choice, Back keeps the
    /// old one.
    /// </summary>
    public class CharacterSelectPanel : MonoBehaviour
    {
        static readonly Vector2 Left = new Vector2(0f, 0.5f);
        static readonly Vector2 Top = new Vector2(0.5f, 1f);
        static readonly Vector2 Bottom = new Vector2(0.5f, 0f);
        static readonly Color DotOff = new Color(1f, 1f, 1f, 0.22f);

        PlayerCharacter setup;
        TextMeshProUGUI nameText, taglineText, countText;
        readonly List<Image> dots = new List<Image>();
        int index;

        /// <summary>The character being looked at changed (to preview it).</summary>
        public event Action<CharacterOption> Browsed;
        /// <summary>The panel closed; true if a new choice was saved.</summary>
        public event Action<bool> Closed;

        public bool IsOpen => gameObject.activeSelf;

        public static CharacterSelectPanel Create(Transform safe)
        {
            var card = HudStyle.Card("CharacterCard", safe, Left, Left, new Vector2(110f, 0f), new Vector2(560f, 780f),
                HudStyle.PanelSolid, HudStyle.Gold, out _);
            var panel = card.gameObject.AddComponent<CharacterSelectPanel>();
            panel.setup = PlayerCharacter.Instance;
            panel.Build(card.transform);
            card.gameObject.SetActive(false);
            return panel;
        }

        void Build(Transform card)
        {
            var heading = HudStyle.Label("Heading", card, "CHOOSE YOUR WALKER", 22, HudStyle.Gold, TextAlignmentOptions.Top).Inset(0f, 0f, 0f, 64f);
            heading.characterSpacing = 12f;

            // Name between the two arrows
            nameText = HudStyle.Label("Name", card, "", 40, HudStyle.Text, TextAlignmentOptions.Center, true);
            nameText.enableAutoSizing = true;   // long names shrink to fit between the arrows
            nameText.fontSizeMin = 28f;
            nameText.fontSizeMax = 40f;
            Place(nameText.rectTransform, new Vector2(0f, -250f), new Vector2(300f, 80f));
            Arrow("Previous", card, "‹", -200f, () => Step(-1));
            Arrow("Next", card, "›", 200f, () => Step(1));

            taglineText = HudStyle.Label("Tagline", card, "", 26, HudStyle.TextMuted, TextAlignmentOptions.Top);
            taglineText.textWrappingMode = TextWrappingModes.Normal;
            Place(taglineText.rectTransform, new Vector2(0f, -330f), new Vector2(440f, 80f));

            // One dot per character, the current one gold, with "2 of 3" under them.
            int count = setup != null ? setup.options.Count : 0;
            var disc = HudStyle.Art.circle != null ? HudStyle.Art.circle : UiFactory.Circle;
            for (int i = 0; i < count; i++)
                dots.Add(HudStyle.Icon("Dot" + i, card, disc, Top, new Vector2((i - (count - 1) * 0.5f) * 34f, -440f), 16f, DotOff));
            countText = HudStyle.Label("Count", card, "", 20, HudStyle.LabelMuted, TextAlignmentOptions.Top);
            Place(countText.rectTransform, new Vector2(0f, -470f), new Vector2(200f, 30f));

            HudStyle.GoldButton("SelectButton", card, "Select", Bottom, new Vector2(0f, 186f), new Vector2(380f, 96f))
                .onClick.AddListener(Select);
            HudStyle.OutlineButton("BackButton", card, "Back", Bottom, new Vector2(0f, 76f), new Vector2(380f, 84f))
                .onClick.AddListener(Close);
        }

        static void Place(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = Top;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static void Arrow(string name, Transform card, string glyph, float x, Action onClick)
        {
            var b = HudStyle.OutlineButton(name, card, "", Top, new Vector2(x, -250f), new Vector2(72f, 72f));
            var label = b.GetComponentInChildren<TextMeshProUGUI>();
            label.text = glyph;
            label.fontSize = 56;
            label.rectTransform.anchoredPosition = new Vector2(0f, 4f);   // the angle glyphs sit low in the font
            b.onClick.AddListener(() => onClick());
        }

        /// <summary>Opens on the saved character.</summary>
        public void Open()
        {
            index = setup != null ? setup.SelectedIndex : 0;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Refresh();
        }

        void Step(int delta)
        {
            int count = setup != null ? setup.options.Count : 0;
            if (count == 0) return;
            index = (index + delta + count) % count;
            Refresh();
            Browsed?.Invoke(setup.options[index]);
        }

        void Refresh()
        {
            if (setup == null || setup.options.Count == 0)
            {
                nameText.text = "No characters";
                return;
            }
            var option = setup.options[index];
            nameText.text = option.name;
            taglineText.text = option.tagline;
            countText.text = $"{index + 1} of {setup.options.Count}";
            for (int i = 0; i < dots.Count; i++) dots[i].color = i == index ? HudStyle.Gold : DotOff;
        }

        void Select()
        {
            bool changed = setup != null && setup.SelectedIndex != index;
            if (setup != null) setup.SelectedIndex = index;
            gameObject.SetActive(false);
            Closed?.Invoke(changed);
        }

        /// <summary>Closes without saving; the preview goes back to the saved character.</summary>
        public void Close()
        {
            bool browsed = setup != null && setup.SelectedIndex != index;
            gameObject.SetActive(false);
            if (browsed) Browsed?.Invoke(setup.Selected);
            Closed?.Invoke(false);
        }
    }
}
