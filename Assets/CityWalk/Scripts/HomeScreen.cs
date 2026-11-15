using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CityWalk
{
    /// <summary>
    /// The home screen: the 3D city with the player's character (<see cref="HomeStage"/>) behind a title card with
    /// Play, Character, Settings and Quit. Quit asks for confirmation first. If the 3D stage cannot be built, the
    /// game screenshot is shown instead. Put this on one GameObject in the Home scene.
    /// </summary>
    public class HomeScreen : MonoBehaviour
    {
        static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);
        static readonly Vector2 Bottom = new Vector2(0.5f, 0f);
        static readonly Vector2 ButtonSize = new Vector2(380f, 84f);

        public int citySeed = 7;

        GameObject quitDialog, titleCard;
        HomeStage stage;
        CharacterSelectPanel characterPanel;
        SettingsPanel settingsPanel;

        void Awake()
        {
            Time.timeScale = 1f;
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.AutoRotation;

            try
            {
                stage = HomeStage.Create(citySeed);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                if (stage != null) Destroy(stage.gameObject);
                stage = null;
            }

            _ = AudioHub.Instance;   // starts the city ambience, which carries on into the game

            var canvas = HudStyle.CreateCanvas("HomeCanvas", 0, out var safe);
            if (stage == null) BuildPictureBackground(canvas.transform);
            BuildMenu(safe);
        }

        /// <summary>Fallback when the 3D stage is missing: the game screenshot, with a camera so the screen is cleared.</summary>
        void BuildPictureBackground(Transform canvasRoot)
        {
            var art = HudStyle.Art;
            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = HudStyle.Hex(0x092E26);
            cam.cullingMask = 0;

            // Background screenshot, filling the screen without stretching.
            var bg = UiFactory.MakeRect("Background", canvasRoot, Centre, Centre, Vector2.zero, new Vector2(1920f, 1080f));
            bg.SetAsFirstSibling();
            var bgImage = bg.gameObject.AddComponent<Image>();
            bgImage.sprite = art.homeBackground;
            bgImage.color = art.homeBackground != null ? Color.white : HudStyle.Hex(0x0B3A30);
            bgImage.raycastTarget = false;
            var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;

            // Soft dark wash so the card reads clearly over the picture.
            var wash = HudStyle.DimOverlay("Wash", canvasRoot);
            wash.color = new Color(0f, 0f, 0f, 0.25f);
            wash.raycastTarget = false;
            wash.transform.SetSiblingIndex(1);
        }

        void BuildMenu(RectTransform safe)
        {
            var art = HudStyle.Art;

            // Title card with Play, Character, Settings and Quit.
            var card = HudStyle.Card("TitleCard", safe, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, 0f),
                new Vector2(560f, 780f), HudStyle.PanelSolid, HudStyle.Gold, out _);
            titleCard = card.gameObject;
            HudStyle.Icon("Icon", card.transform, art.iconCity, new Vector2(0.5f, 1f), new Vector2(0f, -100f), 104f, Color.white);
            HudStyle.Label("Title", card.transform, "City Walk", 72, HudStyle.Text, TextAlignmentOptions.Top, true).Inset(0f, 0f, 0f, 170f);
            HudStyle.Label("Subtitle", card.transform, "Morning routine", 30, HudStyle.TextMuted, TextAlignmentOptions.Top).Inset(0f, 0f, 0f, 258f);

            HudStyle.GoldButton("PlayButton", card.transform, "Play", Bottom, new Vector2(0f, 386f), new Vector2(380f, 96f))
                .onClick.AddListener(GameScenes.LoadGame);
            HudStyle.OutlineButton("CharacterButton", card.transform, "Character", Bottom, new Vector2(0f, 276f), ButtonSize)
                .onClick.AddListener(OpenCharacterSelect);
            HudStyle.OutlineButton("SettingsButton", card.transform, "Settings", Bottom, new Vector2(0f, 176f), ButtonSize)
                .onClick.AddListener(() => settingsPanel.Open());
            HudStyle.OutlineButton("QuitButton", card.transform, "Quit", Bottom, new Vector2(0f, 76f), ButtonSize)
                .onClick.AddListener(ShowQuitDialog);

            // Character select takes the card's place; the 3D character on the right previews each one.
            characterPanel = CharacterSelectPanel.Create(safe);
            characterPanel.Browsed += option => { if (stage != null) stage.ShowCharacter(option); };
            characterPanel.Closed += changed => titleCard.SetActive(true);

            settingsPanel = SettingsPanel.Create(safe, false);

            quitDialog = HudStyle.ConfirmDialog("QuitDialog", safe, "Quit City Walk?", "Are you sure you really want to exit?",
                "Yes", GameScenes.Quit, "No", HideQuitDialog);
        }

        void OpenCharacterSelect()
        {
            titleCard.SetActive(false);
            characterPanel.Open();
        }

        void ShowQuitDialog()
        {
            quitDialog.SetActive(true);
            quitDialog.transform.SetAsLastSibling();
        }

        void HideQuitDialog() => quitDialog.SetActive(false);

        void Update()
        {
            // Escape on a keyboard, or the Android back button, opens or closes the quit dialog.
            if (!BackPressed()) return;
            if (settingsPanel.IsOpen) settingsPanel.Close();
            else if (characterPanel.IsOpen) characterPanel.Close();
            else if (quitDialog.activeSelf) HideQuitDialog();
            else ShowQuitDialog();
        }

        static bool BackPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }
    }
}
