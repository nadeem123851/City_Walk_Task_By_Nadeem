using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CityWalk
{
    /// <summary>
    /// The home screen: a game screenshot behind a title card with Play and Quit.
    /// Quit asks for confirmation first. Put this on one GameObject in the Home scene.
    /// </summary>
    public class HomeScreen : MonoBehaviour
    {
        static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);

        GameObject quitDialog;

        void Awake()
        {
            Time.timeScale = 1f;
            Screen.orientation = ScreenOrientation.AutoRotation;

            // A camera so the screen is cleared even before the UI draws, with the audio listener for the ambience and clicks.
            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = HudStyle.Hex(0x092E26);
            cam.cullingMask = 0;

            _ = AudioHub.Instance;   // starts the city ambience, which carries on into the game

            var canvas = HudStyle.CreateCanvas("HomeCanvas", 0, out var safe);
            Build(canvas.transform, safe);
        }

        void Build(Transform canvasRoot, RectTransform safe)
        {
            var art = HudStyle.Art;

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

            // Title card with Play and Quit.
            var card = HudStyle.Card("TitleCard", safe, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, 0f),
                new Vector2(560f, 560f), HudStyle.PanelSolid, HudStyle.Gold, out _);
            HudStyle.Icon("Icon", card.transform, art.iconCity, new Vector2(0.5f, 1f), new Vector2(0f, -100f), 104f, Color.white);
            HudStyle.Label("Title", card.transform, "City Walk", 72, HudStyle.Text, TextAlignmentOptions.Top, true).Inset(0f, 0f, 0f, 170f);
            HudStyle.Label("Subtitle", card.transform, "Morning routine", 30, HudStyle.TextMuted, TextAlignmentOptions.Top).Inset(0f, 0f, 0f, 258f);

            HudStyle.GoldButton("PlayButton", card.transform, "Play", new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(380f, 96f))
                .onClick.AddListener(GameScenes.LoadGame);
            HudStyle.OutlineButton("QuitButton", card.transform, "Quit", new Vector2(0.5f, 0f), new Vector2(0f, 76f), new Vector2(380f, 90f))
                .onClick.AddListener(ShowQuitDialog);

            quitDialog = HudStyle.ConfirmDialog("QuitDialog", safe, "Quit City Walk?", "Are you sure you really want to exit?",
                "Yes", GameScenes.Quit, "No", HideQuitDialog);
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
            if (quitDialog.activeSelf) HideQuitDialog();
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
