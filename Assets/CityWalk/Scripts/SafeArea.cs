using UnityEngine;

namespace CityWalk
{
    /// <summary>Keeps this RectTransform inside the screen's safe area, clear of notches and rounded corners.</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        Rect applied;
        Vector2Int screen;

        void OnEnable() => Apply();

        void Update()
        {
            if (Screen.safeArea != applied || screen.x != Screen.width || screen.y != Screen.height) Apply();
        }

        void Apply()
        {
            var rt = (RectTransform)transform;
            applied = Screen.safeArea;
            screen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            rt.anchorMin = new Vector2(applied.xMin / Screen.width, applied.yMin / Screen.height);
            rt.anchorMax = new Vector2(applied.xMax / Screen.width, applied.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
