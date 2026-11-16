using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CityWalk.EditorTools
{
    /// <summary>
    /// City Walk > Record Playthrough: a captioned feature tour recorded from the Game view with sound to
    /// Docs/CityWalk_Features_Playthrough.mp4 (Unity Recorder, 1920x1080, 30 fps). A scripted player goes through the
    /// home screen (3D stage, character select, settings), a full route (movement, camera, guidance, coffee, coins,
    /// pace meter, lap timer and lap card, wrong way, pause and in-game settings, office, finish shot, results), then a
    /// short mission-failed run, with a caption bar explaining each feature as it appears.
    /// Editor only; needs the com.unity.recorder package. The player's saved character and sound settings are put
    /// back afterwards.
    /// </summary>
    [InitializeOnLoad]
    public static class PlaythroughRecorder
    {
        const string PendingKey = "CityWalk.PlaythroughRecorder.Pending";
        const string OutputName = "Docs/CityWalk_Features_Playthrough";
        const float Fps = 30f;
        const float CaptionHold = 4.2f;      // each caption stays at least this long (video seconds)
        const float UrgentHold = 3f;         // an event caption may cut in once the current one has had this long
        const float FailDemoLimit = 12f;     // lap limit used for the mission-failed demo
        static readonly Vector2 Kiosk = new Vector2(17f, -17f);

        static RecorderController recorder;
        static int stage, waypoint, lastTapLap, startFrame, stageFrame, lastLaps;
        static float gameStart, pauseUntil;
        static List<Vector3> waypoints;   // x, z = ground position; y = stick strength (0.6 walk, 1 brisk)
        static string log;
        static RouteDirector hooked;

        // Saved player settings, restored at the end
        static int savedCharacter;
        static float savedMusic, savedSfx;
        static bool savedMusicMuted, savedSfxMuted;

        // Captions
        static CanvasGroup captionGroup;
        static TextMeshProUGUI captionTag, captionBody;
        static readonly Queue<(string tag, string body)> captions = new Queue<(string, string)>();
        static readonly Queue<(string tag, string body)> urgentCaptions = new Queue<(string, string)>();
        static readonly HashSet<string> shown = new HashSet<string>();
        static readonly HashSet<string> fired = new HashSet<string>();
        static float captionSince = -100f;        static bool captionVisible;

        // Pause demo (lap 3) and mission-failed demo steps
        static int pauseStep;
        static float pauseStepAt;

        /// <summary>Video clock in seconds: rendered frames at the fixed 30 fps, so it runs on while the game is paused.</summary>
        static float Clock => (Time.frameCount - startFrame) / Fps;
        static float StageTime => (Time.frameCount - stageFrame) / Fps;

        static PlaythroughRecorder()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("City Walk/Record Playthrough")]
        static void Record()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("City Walk: stop Play mode first."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/CityWalk/Scenes/Home.unity");
            SessionState.SetBool(PendingKey, true);
            EditorApplication.isPlaying = true;
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
            {
                SessionState.SetBool(PendingKey, false);
                Begin();
            }
            else if (change == PlayModeStateChange.ExitingPlayMode && recorder != null)
            {
                if (recorder.IsRecording()) recorder.StopRecording();
                recorder = null;
                EditorApplication.update -= Tick;
                RestoreSettings();
            }
        }

        static void Begin()
        {
            var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = "Playthrough";
            movie.Enabled = true;
            movie.EncoderSettings = new CoreEncoderSettings
            {
                Codec = CoreEncoderSettings.OutputCodec.MP4,
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
            };
            movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = 1920, OutputHeight = 1080 };
            movie.CaptureAudio = true;   // game sounds (AudioHub) go into the video
            movie.OutputFile = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", OutputName));
            settings.AddRecorderSettings(movie);
            settings.SetRecordModeToManual();
            settings.FrameRate = Fps;
            settings.FrameRatePlayback = FrameRatePlayback.Constant;   // fixed time step, so the video is smooth even if the editor is slow
            settings.CapFrameRate = true;
            recorder = new RecorderController(settings);
            recorder.PrepareRecording();
            recorder.StartRecording();

            // Remember the player's own choices; the tour changes them on screen.
            var pc = PlayerCharacter.Instance;
            savedCharacter = pc != null ? pc.SelectedIndex : 0;
            savedMusic = GameSettings.MusicVolume; savedSfx = GameSettings.SfxVolume;
            savedMusicMuted = GameSettings.MusicMuted; savedSfxMuted = GameSettings.SfxMuted;
            GameSettings.MusicVolume = 1f; GameSettings.SfxVolume = 1f;
            GameSettings.MusicMuted = false; GameSettings.SfxMuted = false;

            waypoints = new List<Vector3>();
            // Lap 1: walk (slow enough for a pace nudge), then brisk; coffee at the kiosk corner; into the 3 m alley and back
            W(4f, -17f, 0.6f); W(17f, -17f, 1f); W(17f, 0f, 1f);
            W(29f, 0f, 0.6f); W(17.5f, 0f, 0.6f);
            W(17f, 17f, 1f); W(-17f, 17f, 1f); W(-17f, -17f, 1f);
            // Lap 2: brisk, with a short walk the wrong way
            W(17f, -17f, 1f); W(17f, -3f, 1f); W(17f, -11f, 0.6f);
            W(17f, 17f, 1f); W(-17f, 17f, 1f); W(-17f, -17f, 1f);
            // Lap 3: rush hour is over, normal pace
            W(17f, -17f, 0.6f); W(17f, 17f, 0.6f); W(-17f, 17f, 0.6f); W(-17f, -17f, 0.6f);
            // To the office
            W(51f, -17f, 0.6f);

            stage = 0; waypoint = 0; lastTapLap = 0; lastLaps = 0; pauseUntil = 0f; pauseStep = 0; log = "";
            hooked = null;
            captions.Clear(); urgentCaptions.Clear(); shown.Clear(); fired.Clear(); captionVisible = false; captionSince = -100f;
            startFrame = Time.frameCount;
            stageFrame = Time.frameCount;
            BuildCaptionBar();
            EditorApplication.update += Tick;
            Debug.Log("City Walk: recording playthrough to " + movie.OutputFile + ".mp4");
        }

        static void W(float x, float z, float stick) => waypoints.Add(new Vector3(x, stick, z));

        // ---------------------------------------------------------------- captions

        static void BuildCaptionBar()
        {
            var canvasGo = new GameObject("TourCaptions", typeof(Canvas), typeof(CanvasScaler));
            Object.DontDestroyOnLoad(canvasGo);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            var bottom = new Vector2(0.5f, 0f);
            var card = HudStyle.Card("Caption", canvasGo.transform, bottom, bottom, new Vector2(0f, 26f), new Vector2(1080f, 112f),
                HudStyle.Hex(0x092E26, 0.94f), HudStyle.Gold, out _);
            card.raycastTarget = false;
            captionGroup = card.gameObject.AddComponent<CanvasGroup>();
            captionGroup.blocksRaycasts = false;
            captionGroup.alpha = 0f;

            captionTag = HudStyle.Label("Tag", card.transform, "", 19, HudStyle.Gold, TextAlignmentOptions.Top, true).Inset(30f, 0f, 30f, 14f);
            captionTag.characterSpacing = 8f;
            captionBody = HudStyle.Label("Body", card.transform, "", 27, HudStyle.Text, TextAlignmentOptions.Center).Inset(36f, 10f, 36f, 40f);
            captionBody.textWrappingMode = TextWrappingModes.Normal;
            captionBody.enableAutoSizing = true;
            captionBody.fontSizeMin = 20f;
            captionBody.fontSizeMax = 27f;
        }

        /// <summary>
        /// Queues a caption once per key; it shows when the one before has had its time. Urgent captions belong to
        /// something happening on screen right now, so they go ahead of the general ones.
        /// </summary>
        static void Caption(string key, string tag, string body, bool urgent = false)
        {
            if (!shown.Add(key)) return;
            (urgent ? urgentCaptions : captions).Enqueue((tag, body));
            log += $"caption {key} t{Clock:F1}\n";
        }

        static void UpdateCaptions()
        {
            if (captionGroup == null) return;            float age = Clock - captionSince;
            Queue<(string tag, string body)> next = null;
            if (urgentCaptions.Count > 0 && (!captionVisible || age >= UrgentHold)) next = urgentCaptions;
            else if (urgentCaptions.Count == 0 && captions.Count > 0 && (!captionVisible || age >= CaptionHold)) next = captions;
            if (next != null)
            {
                var (tag, body) = next.Dequeue();
                captionTag.text = tag;
                captionBody.text = body;
                captionSince = Clock;
                captionVisible = true;
                log += $"  shown {tag} t{Clock:F1}\n";                age = 0f;
            }
            else if (captionVisible && captions.Count == 0 && urgentCaptions.Count == 0 && age >= CaptionHold + 2.5f)
            {
                captionVisible = false;
                captionSince = Clock;
                age = 0f;
            }
            // Quick fade in and out
            float target = captionVisible ? 1f : 0f;
            captionGroup.alpha = Mathf.MoveTowards(captionGroup.alpha, target, 1f / (0.25f * Fps));
        }

        // ---------------------------------------------------------------- the tour

        static void Tick()
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
            var mover = Object.FindFirstObjectByType<PlayerMover>();
            var route = Object.FindFirstObjectByType<RouteDirector>();
            float t = StageTime;

            switch (stage)
            {
                case 0:   // home screen: the 3D stage
                    Caption("home", "NEW  ·  3D HOME SCREEN",
                        "The home screen is a live 3D view of the city, with your walker standing on the street.");
                    if (t > 5f) { Click("CharacterButton"); Next(); }
                    break;
                case 1:   // character select
                    Caption("character", "NEW  ·  CHOOSE YOUR WALKER",
                        "Three characters: Business Man, Worker and Casual. The 3D model changes as you browse, and the choice is saved.");
                    if (Step(t, 1.6f)) Click("Next");          // Business Man
                    if (Step(t, 3.4f)) Click("Next");          // Worker
                    if (Step(t, 5.2f)) Click("Next");          // Casual
                    if (Step(t, 7.0f)) Click("Next");          // Business Man
                    if (t > 8.6f) { Click("SelectButton"); Next(); }
                    break;
                case 2:   // settings
                    if (Step(t, 1f)) Click("SettingsButton");
                    if (t > 1f)
                    {
                        Caption("settings", "NEW  ·  SETTINGS",
                            "Music and sound effects each have a volume slider and an On/Off switch, saved on the device.");
                        var slider = FindNamed<Slider>("MusicSlider");
                        if (slider != null && t < 5f) slider.value = 1f - 0.6f * Mathf.Sin(Mathf.Clamp01((t - 1.8f) / 2.6f) * Mathf.PI);
                    }
                    if (Step(t, 5.4f)) Click("Sound effectsSwitch");   // off
                    if (Step(t, 6.6f)) Click("Sound effectsSwitch");   // back on
                    if (t > 8f) { Click("CloseButton"); Next(); }
                    break;
                case 3:   // play
                    if (t > 1.2f) { Click("PlayButton"); Next(); }
                    break;
                case 4:   // waiting for the game scene
                    if (mover != null && route != null) { gameStart = Clock; Hook(route); Next(); }
                    break;
                case 5:   // the route
                    Drive(mover, route);
                    break;
                case 6:   // finish shot, then the results card
                    if (Step(t, 3.6f))
                        Caption("results", "BRIEF  ·  ROUTE COMPLETE",
                            "The results card: stars for a clean run, XP, coins and the total time, with Restart and Home.", true);
                    if (t > 10f) { Click("Restart"); Next(); }
                    break;
                case 7:   // mission-failed demo: wait for the new run, then shorten the lap limit
                    if (mover != null && route != null && t > 0.5f)
                    {
                        route.lapTimeLimit = FailDemoLimit;
                        Hook(route);
                        Caption("faildemo", "NEW  ·  LAP TIME LIMIT",
                            "Each lap must be done in 1:30 or the mission fails. For this demo the limit is cut to 0:12 so it runs out.", true);
                        Next();
                    }
                    break;
                case 8:   // stand still until time runs out
                    SetStick(mover, Vector2.zero);
                    if (route != null && route.Phase == RoutePhase.Failed)
                    {
                        Caption("failed", "NEW  ·  MISSION FAILED",
                            "Out of time: the run stops and the Mission Failed card shows laps, coins and time, with Home or Retry.", true);
                        Next();
                    }
                    else if (t > 40f) Finish("fail demo timeout");
                    break;
                case 9:   // mission failed card, then Home
                    if (t > 7.5f) { Click("Home"); Next(); }
                    break;
                case 10:  // back home
                    Caption("end", "CITY WALK", "Thanks for watching.", true);
                    if (t > 5f) Finish("done");
                    break;
            }
            UpdateCaptions();
        }

        /// <summary>True once per stage, on the first tick after the stage clock passes <paramref name="at"/>.</summary>
        static bool Step(float t, float at) => t >= at && fired.Add(stage + ":" + at);

        static void Hook(RouteDirector route)
        {
            if (hooked == route) return;
            hooked = route;
            route.BriskBonus += () => Caption("brisk", "NEW  ·  BRISK BONUS",
                "Keeping up a brisk pace through rush hour earns a +5 XP bonus every few seconds.", true);
            route.CoinCollected += _ => Caption("coins", "NEW  ·  COINS",
                "Coins line the pavements round the park. Walk through them to collect; the count is top right.");
            route.LapCompleted += (lap, time) => Caption("lapcard", "NEW  ·  LAP CARD",
                "Each finished lap pops a card with the lap time and the XP earned. The lap timer starts again at 1:30.", true);
            route.CoffeeGrabbed += () => Caption("coffee", "BRIEF  ·  COFFEE KIOSK",
                "Tap the coffee button as you pass the kiosk. Every lap needs a coffee to count (+50 XP).", true);
            route.WrongWayChanged += on => { if (on) Caption("wrongway", "BRIEF  ·  WRONG WAY",
                "Walk back round the loop and a warning tells you to turn around. Turn back and it clears.", true); };
            route.RouteCompleted += () => Caption("finish", "NEW  ·  FINISH SHOT",
                "Arriving at the office: the camera swings round the walker with a burst of sparkles.", true);
        }

        static void Drive(PlayerMover mover, RouteDirector route)
        {
            if (mover == null || route == null) return;
            float g = Clock - gameStart;

            if (g < 3f)   // idle intro
            {
                SetStick(mover, Vector2.zero);
                Caption("idle", "BRIEF  ·  CHARACTER AND CAMERA",
                    "Standing still plays the idle animation. A third-person camera follows behind the walker.");
                return;
            }
            Caption("move", "BRIEF  ·  MOVEMENT",
                "The on-screen joystick moves the walker: a light push walks, a full push breaks into a brisk walk.");
            Caption("hud", "NEW  ·  LAP TIMER",
                "Top left: the lap counter and the lap timer. Each lap must be done in 1:30; it turns orange near the end.");
            Caption("guide", "BRIEF  ·  GUIDANCE",
                "Green chevrons on the road and the yellow marker show the way round the park, then to the office.");
            if (route.IsPaceNudge)
                Caption("pace", "NEW  ·  PACE METER",
                    "It's rush hour, so walk briskly. The pace bar fills with speed; drop to a stroll and you're told to pick up the pace.", true);

            if (route.Phase == RoutePhase.Complete)
            {
                SetStick(mover, Vector2.zero);
                log += $"complete t{Clock:F1} xp {route.Xp} laps {route.LapsCompleted} stars {route.Stars}\n";
                Next();
                return;
            }
            if (route.Phase == RoutePhase.Failed) { Finish("failed during the route"); return; }
            if (g > 480f) { Finish("timeout"); return; }

            if (route.LapsCompleted != lastLaps)
            {
                lastLaps = route.LapsCompleted;
                if (lastLaps == 1)
                    Caption("peds", "BRIEF  ·  PEDESTRIANS",
                        "Animated pedestrians walk the pavements, with extra people out during rush hour.");
                if (lastLaps == 2)
                    Caption("rush", "BRIEF  ·  RUSH HOUR OVER",
                        "Rush hour ends after lap 2, so the last lap can be walked at an easy pace with no nudges.");
            }
            if (route.Phase == RoutePhase.ToOffice)
                Caption("office", "BRIEF  ·  TO THE OFFICE",
                    "Three laps done. The marker now leads one block east to the office.");

            // Lap 3: show the pause menu and the in-game settings, then carry on.
            if (route.CurrentLap == 3 && waypoint >= 15 && pauseStep < 6)
            {
                SetStick(mover, Vector2.zero);
                PauseDemo();
                return;
            }

            if (Clock < pauseUntil) { SetStick(mover, Vector2.zero); return; }

            Vector3 p = mover.transform.position;
            if (route.Phase == RoutePhase.Laps && !route.CoffeeThisLap && lastTapLap != route.CurrentLap &&
                Vector2.Distance(new Vector2(p.x, p.z), Kiosk) < 6.5f && TapCoffee())
            {
                lastTapLap = route.CurrentLap;
                log += $"coffee lap {route.CurrentLap} t{Clock:F1}\n";
            }

            if (waypoint >= waypoints.Count) { SetStick(mover, Vector2.zero); return; }
            var target = new Vector3(waypoints[waypoint].x, p.y, waypoints[waypoint].z);
            Vector3 to = target - p;
            if (to.magnitude < 1.4f)
            {
                if (waypoint == 3)
                {
                    pauseUntil = Clock + 3f;   // stand deep in the alley so the camera pull-in shows
                    Caption("alley", "BRIEF  ·  CAMERA COLLISION",
                        "In the narrow alley the camera pulls in close so it never goes through a wall.", true);
                }
                log += $"waypoint {waypoint} t{Clock:F1} lap {route.CurrentLap} left {route.LapTimeLeft:F1}\n";
                waypoint++;
                return;
            }

            // The stick is camera-relative, as for a real player.
            var cam = mover.cameraTransform;
            Vector3 f = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            Vector3 r = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            Vector3 d = to.normalized;
            SetStick(mover, new Vector2(Vector3.Dot(d, r), Vector3.Dot(d, f)).normalized * waypoints[waypoint].y);
        }

        static void PauseDemo()
        {
            float since = Clock - pauseStepAt;
            switch (pauseStep)
            {
                case 0:
                    pauseStepAt = Clock; pauseStep++;
                    break;
                case 1:
                    if (since > 0.6f)
                    {
                        Click("PauseButton");
                        Caption("pause", "NEW  ·  PAUSE MENU",
                            "Pause freezes the game and the lap timer. Resume carries on; Home goes back to the home screen.", true);
                        pauseStepAt = Clock; pauseStep++;
                    }
                    break;
                case 2:
                    if (since > 4.4f) { Click("Resume"); pauseStepAt = Clock; pauseStep++; }
                    break;
                case 3:
                    if (since > 1.0f)
                    {
                        Click("SettingsButton");
                        Caption("gear", "NEW  ·  IN-GAME SETTINGS",
                            "The gear under the pause button opens the same music and sound settings during the walk.", true);
                        pauseStepAt = Clock; pauseStep++;
                    }
                    break;
                case 4:
                    if (since > 4.4f) { Click("CloseButton"); pauseStepAt = Clock; pauseStep++; }
                    break;
                case 5:
                    if (since > 0.5f) pauseStep++;
                    break;
            }
        }

        // ---------------------------------------------------------------- helpers

        static bool TapCoffee()
        {
            foreach (var b in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (!b.gameObject.activeInHierarchy) continue;
                if (b.name.Contains("Coffee") || (b.transform.parent != null && b.transform.parent.name.Contains("Coffee")))
                {
                    b.onClick.Invoke();
                    return true;
                }
            }
            return false;
        }

        static void SetStick(PlayerMover mover, Vector2 value)
        {
            if (mover == null || mover.joystick == null) return;
            // Value has a private setter: the recorder stands in for a finger on the joystick.
            typeof(VirtualJoystick).GetProperty("Value").SetValue(mover.joystick, value);
        }

        static T FindNamed<T>(string name) where T : Component
        {
            foreach (var c in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
                if (c.name == name && c.gameObject.activeInHierarchy) return c;
            return null;
        }

        static void Click(string name)
        {
            var b = FindNamed<Button>(name);
            if (b != null)
            {
                b.onClick.Invoke();
                log += $"click {name} t{Clock:F1}\n";
            }
            else log += $"button not found: {name} t{Clock:F1}\n";
        }

        static void Next()
        {
            stage++;
            stageFrame = Time.frameCount;
        }

        static void RestoreSettings()
        {
            var pc = PlayerCharacter.Instance;
            if (pc != null) pc.SelectedIndex = savedCharacter;
            GameSettings.MusicVolume = savedMusic; GameSettings.SfxVolume = savedSfx;
            GameSettings.MusicMuted = savedMusicMuted; GameSettings.SfxMuted = savedSfxMuted;
            GameSettings.Save();
        }

        static void Finish(string why)
        {
            log += $"finish: {why} t{Clock:F1}\n";
            if (recorder != null && recorder.IsRecording()) recorder.StopRecording();
            recorder = null;
            EditorApplication.update -= Tick;
            RestoreSettings();
            Debug.Log("City Walk: playthrough recorded.\n" + log);
            System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "playthrough_log.txt"), log);
            EditorApplication.isPlaying = false;
        }
    }
}
