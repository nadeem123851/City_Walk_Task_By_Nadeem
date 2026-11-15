using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// The player's look and animation: the character model, its animator and the clip speeds used to keep the
    /// feet from sliding. One asset lives in Resources so the runtime-built player can find it and it ships in builds.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerCharacter", menuName = "City Walk/Player Character")]
    public class PlayerCharacter : ScriptableObject
    {
        [Tooltip("Character model (Quaternius Suit, CharacterArmature rig).")]
        public GameObject model;
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

        [Header("Props")]
        public bool briefcase = true;

        static PlayerCharacter instance;

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
