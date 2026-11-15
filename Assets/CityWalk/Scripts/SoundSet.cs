using System;
using UnityEngine;

namespace CityWalk
{
    /// <summary>A sound and how loud it plays.</summary>
    [Serializable]
    public class Sound
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.8f;
        [Tooltip("Random pitch change either way, so repeated sounds do not feel mechanical.")]
        [Range(0f, 0.3f)] public float pitchJitter;
    }

    /// <summary>
    /// The game's sounds (from Assets/CityWalk/Audio) and their volumes. One asset lives in Resources so the
    /// runtime-built game can find it and it ships in builds.
    /// </summary>
    [CreateAssetMenu(fileName = "SoundSet", menuName = "City Walk/Sound Set")]
    public class SoundSet : ScriptableObject
    {
        [Header("Player")]
        public Sound step = new Sound { volume = 0.45f, pitchJitter = 0.08f };

        [Header("Route")]
        public Sound coffee = new Sound { volume = 0.8f };
        public Sound lap = new Sound { volume = 0.8f };
        public Sound missed = new Sound { volume = 0.75f };
        public Sound wrongWay = new Sound { volume = 0.7f };
        public Sound finish = new Sound { volume = 0.9f };
        public Sound coin = new Sound { volume = 0.6f, pitchJitter = 0.05f };
        [Tooltip("Rush hour pace nudge: \"Pick up the pace\".")]
        public Sound nudge = new Sound { volume = 0.6f };
        [Tooltip("Bonus XP for keeping a brisk pace.")]
        public Sound bonus = new Sound { volume = 0.55f };

        [Header("Interface")]
        public Sound click = new Sound { volume = 0.6f };

        [Header("Background")]
        public Sound ambience = new Sound { volume = 0.35f };

        static SoundSet instance;

        /// <summary>The SoundSet asset in Resources, or null if it is missing.</summary>
        public static SoundSet Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<SoundSet>("SoundSet");
                return instance;
            }
        }
    }
}
