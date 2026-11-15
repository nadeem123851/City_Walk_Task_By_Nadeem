using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Holds the HUD sprites from Assets/CityWalk/UI. One asset lives in Resources so runtime-built
    /// UI can use them and they are included in builds.
    /// </summary>
    [CreateAssetMenu(fileName = "UiArt", menuName = "City Walk/UI Art")]
    public class UiArt : ScriptableObject
    {
        public Sprite buttonGold;
        public Sprite iconCoffee;
        public Sprite iconWalker;

        static UiArt instance;

        /// <summary>The UiArt asset in Resources, or an empty one if it is missing.</summary>
        public static UiArt Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<UiArt>("UiArt");
                    if (instance == null)
                    {
                        Debug.LogWarning("CityWalk: Resources/UiArt asset not found; HUD sprites will be plain.");
                        instance = CreateInstance<UiArt>();
                    }
                }
                return instance;
            }
        }
    }
}
