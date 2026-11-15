using UnityEngine;
using UnityEngine.SceneManagement;

namespace CityWalk
{
    /// <summary>Scene names and switching between the home screen and the game.</summary>
    public static class GameScenes
    {
        public const string Home = "Home";
        public const string Game = "CityWalk";

        public static void LoadHome() => Load(Home);
        public static void LoadGame() => Load(Game);

        /// <summary>Starts the current scene again from the beginning.</summary>
        public static void Restart() => Load(SceneManager.GetActiveScene().name);

        static void Load(string scene)
        {
            Time.timeScale = 1f;   // a paused game must not stay paused in the next scene
            SceneManager.LoadScene(scene);
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
