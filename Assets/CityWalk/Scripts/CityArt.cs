using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Holds the textures the city is built from. One asset lives in Resources so runtime code
    /// can find it and the textures are included in builds, while the files stay in Art/Textures.
    /// </summary>
    [CreateAssetMenu(fileName = "CityArt", menuName = "City Walk/City Art")]
    public class CityArt : ScriptableObject
    {
        public Texture2D windows;
        public Texture2D signCoffee;
        public Texture2D signBank;
        public Texture2D signCafe;
        public Texture2D signOffice;
        public Texture2D signLapLine;
        public Material sky;
        [Tooltip("Additive particle material with the sparkle texture, for coin, lap and coffee sparkles.")]
        public Material sparkle;

        static CityArt instance;

        /// <summary>The CityArt asset in Resources, or an empty one if it is missing.</summary>
        public static CityArt Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<CityArt>("CityArt");
                    if (instance == null)
                    {
                        Debug.LogWarning("CityWalk: Resources/CityArt asset not found; the city will have no textures.");
                        instance = CreateInstance<CityArt>();
                    }
                }
                return instance;
            }
        }
    }
}
