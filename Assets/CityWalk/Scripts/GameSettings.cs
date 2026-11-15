using System;
using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Player settings kept on the device: music (the city ambience) and sound effects, each with a volume and a
    /// mute. Changes apply at once through <see cref="Changed"/>; <see cref="Save"/> writes them to disk.
    /// </summary>
    public static class GameSettings
    {
        const string MusicVolumeKey = "CityWalk.MusicVolume";
        const string MusicMutedKey = "CityWalk.MusicMuted";
        const string SfxVolumeKey = "CityWalk.SfxVolume";
        const string SfxMutedKey = "CityWalk.SfxMuted";

        /// <summary>Any setting changed.</summary>
        public static event Action Changed;

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
            set { PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value)); Changed?.Invoke(); }
        }

        public static bool MusicMuted
        {
            get => PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
            set { PlayerPrefs.SetInt(MusicMutedKey, value ? 1 : 0); Changed?.Invoke(); }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
            set { PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value)); Changed?.Invoke(); }
        }

        public static bool SfxMuted
        {
            get => PlayerPrefs.GetInt(SfxMutedKey, 0) == 1;
            set { PlayerPrefs.SetInt(SfxMutedKey, value ? 1 : 0); Changed?.Invoke(); }
        }

        /// <summary>What the music actually plays at: 0 when muted.</summary>
        public static float MusicLevel => MusicMuted ? 0f : MusicVolume;
        /// <summary>What effects, footsteps and clicks actually play at: 0 when muted.</summary>
        public static float SfxLevel => SfxMuted ? 0f : SfxVolume;

        /// <summary>Writes the settings to disk (done when the panel closes, not on every slider step).</summary>
        public static void Save() => PlayerPrefs.Save();
    }
}
