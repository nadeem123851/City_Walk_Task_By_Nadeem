using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityWalk
{
    /// <summary>One character the player can walk as.</summary>
    [Serializable]
    public class CharacterOption
    {
        public string name = "Character";
        [Tooltip("One line under the name on the character select panel.")]
        public string tagline;
        [Tooltip("Quaternius model on the shared CharacterArmature rig.")]
        public GameObject model;
        public bool briefcase;
    }

    /// <summary>
    /// The player's look and animation: the characters to choose from, the animator and the clip speeds used to
    /// keep the feet from sliding. The choice is saved on the device. One asset lives in Resources so the
    /// runtime-built player can find it and it ships in builds.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerCharacter", menuName = "City Walk/Player Character")]
    public class PlayerCharacter : ScriptableObject
    {
        [Tooltip("The characters on the select panel, in order. The first is the default.")]
        public List<CharacterOption> options = new List<CharacterOption>();
        [Tooltip("Locomotion controller: 2D blend tree on MoveX / MoveZ (m/s, relative to where the player faces).")]
        public RuntimeAnimatorController controller;

        [Header("Clip ground speeds (m/s), measured from the planted foot")]
        public float walkClipSpeed = 1.34f;
        public float runClipSpeed = 3.08f;

        [Header("Playback")]
        [Tooltip("The animation speeds up to match the ground speed, but never beyond this.")]
        public float maxPlaybackRate = 1.8f;
        [Tooltip("Smoothing for the blend parameters, in seconds.")]
        public float dampTime = 0.1f;

        [Tooltip("Index into Options used until the player picks a character.")]
        public int defaultOption;

        const string ChoiceKey = "CityWalk.Character";

        static PlayerCharacter instance;

        /// <summary>The saved choice, as an index into <see cref="options"/>.</summary>
        public int SelectedIndex
        {
            get => options.Count == 0 ? 0 : Mathf.Clamp(PlayerPrefs.GetInt(ChoiceKey, defaultOption), 0, options.Count - 1);
            set
            {
                PlayerPrefs.SetInt(ChoiceKey, Mathf.Clamp(value, 0, Mathf.Max(0, options.Count - 1)));
                PlayerPrefs.Save();
            }
        }

        /// <summary>The character the player walks as, or null if there are none.</summary>
        public CharacterOption Selected => options.Count == 0 ? null : options[SelectedIndex];

        /// <summary>The PlayerCharacter asset in Resources, or null if it is missing.</summary>
        public static PlayerCharacter Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<PlayerCharacter>("PlayerCharacter");
                return instance;
            }
        }
    }
}
