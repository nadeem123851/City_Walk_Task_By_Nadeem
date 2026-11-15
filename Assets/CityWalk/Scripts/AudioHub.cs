using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Plays every sound in the game. Created on first use and kept across scene loads, so the city ambience runs
    /// on without a gap between the home screen and the game. Route sounds come from RouteDirector events;
    /// footsteps come from animation events on the walk and run clips.
    /// </summary>
    public class AudioHub : MonoBehaviour
    {
        static AudioHub instance;

        SoundSet sounds;
        AudioSource sfx, steps, ambience;

        /// <summary>The hub, created if needed. Null only when the SoundSet asset is missing.</summary>
        public static AudioHub Instance
        {
            get
            {
                if (instance != null) return instance;
                var set = SoundSet.Instance;
                if (set == null)
                {
                    Debug.LogWarning("CityWalk: Resources/SoundSet is missing; the game is silent.");
                    return null;
                }
                var go = new GameObject("AudioHub");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<AudioHub>();
                instance.Init(set);
                return instance;
            }
        }

        void Init(SoundSet set)
        {
            sounds = set;
            sfx = NewSource("Effects");
            steps = NewSource("Footsteps");
            ambience = NewSource("Ambience");
            ambience.loop = true;
            ambience.ignoreListenerPause = true;
            ApplySettings();
            GameSettings.Changed += ApplySettings;
            if (sounds.ambience.clip != null)
            {
                ambience.clip = sounds.ambience.clip;
                ambience.Play();
            }
        }

        /// <summary>Music and sound-effect volumes from the settings panel; effects, footsteps and clicks share the SFX level.</summary>
        void ApplySettings()
        {
            if (ambience == null) return;
            ambience.volume = sounds.ambience.volume * GameSettings.MusicLevel;
            sfx.volume = steps.volume = GameSettings.SfxLevel;
        }

        void OnDestroy()
        {
            GameSettings.Changed -= ApplySettings;
        }

        AudioSource NewSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;   // all 2D: the camera is always next to the player
            return source;
        }

        /// <summary>Plays the route's sounds when its events fire. Call once per game scene.</summary>
        public void Bind(RouteDirector route)
        {
            if (route == null) return;
            route.CoffeeGrabbed += () => Play(sounds.coffee);
            route.LapCompleted += (lap, time) => Play(sounds.lap);
            route.CoffeeMissed += () => Play(sounds.missed);
            route.WrongWayChanged += wrong => { if (wrong) Play(sounds.wrongWay); };
            route.RouteCompleted += () => Play(sounds.finish);
            route.MissionFailed += () => Play(sounds.fail);
            route.CoinCollected += total => Play(sounds.coin);
            route.PaceNudged += () => Play(sounds.nudge);
            route.BriskBonus += () => Play(sounds.bonus);
        }

        /// <summary>Button tap.</summary>
        public static void Click()
        {
            var hub = Instance;
            if (hub != null) hub.Play(hub.sounds.click);
        }

        /// <summary>One footstep; <paramref name="loudness"/> 0..1 (quieter when walking, full when brisk).</summary>
        public void Footstep(float loudness)
        {
            var s = sounds.step;
            if (s.clip == null) return;
            steps.pitch = 1f + Random.Range(-s.pitchJitter, s.pitchJitter);
            steps.PlayOneShot(s.clip, s.volume * Mathf.Clamp01(loudness));
        }

        void Play(Sound s)
        {
            if (s == null || s.clip == null) return;
            sfx.pitch = 1f + Random.Range(-s.pitchJitter, s.pitchJitter);
            sfx.PlayOneShot(s.clip, s.volume);
        }
    }
}
