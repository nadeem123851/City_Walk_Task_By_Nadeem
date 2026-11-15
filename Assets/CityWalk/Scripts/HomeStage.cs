using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// The 3D scene behind the home screen menu: the game's city in morning light, the player's character standing
    /// idle on the park pavement on the right of the screen, and a camera that drifts gently. No pedestrians or
    /// traffic, so it is light on phones.
    /// </summary>
    public class HomeStage : MonoBehaviour
    {
        // On the pavement round the park, south side, facing the road; the lawn and trees are behind.
        static readonly Vector3 StandPosition = new Vector3(6f, 0.15f, -11.3f);
        const float StandYaw = 180f;

        // The camera stands on the road in front of the character, a little to its right.
        static readonly Vector3 CameraOffset = new Vector3(-1.2f, 1.5f, -4.1f);
        const float LookHeight = 1.0f;
        // Turns the view left of the character so it stands in the right part of the screen, clear of the menu.
        const float FrameYaw = -17f;

        const float DriftDegrees = 5f, DriftPeriod = 18f;   // slow sideways orbit round the character
        const float SwayDegrees = 18f, SwayPeriod = 11f;    // the character slowly turns one way and back

        Camera cam;
        Transform character;
        Transform stand;
        float startTime;

        /// <summary>The stage camera (also carries the audio listener).</summary>
        public Camera Camera => cam;

        /// <summary>Builds the stage. Throws if the city art is missing; the home screen then falls back to its picture.</summary>
        public static HomeStage Create(int citySeed)
        {
            var stage = new GameObject("HomeStage").AddComponent<HomeStage>();
            stage.Build(citySeed);
            return stage;
        }

        void Build(int citySeed)
        {
            CityWalkBootstrap.SetupLighting();

            var city = new GameObject("City").AddComponent<CityBuilder>();
            city.transform.SetParent(transform, false);
            city.randomSeed = citySeed;
            city.Build();

            stand = new GameObject("CharacterStand").transform;
            stand.SetParent(transform, false);
            stand.SetPositionAndRotation(StandPosition, Quaternion.Euler(0f, StandYaw, 0f));
            ShowCharacter();

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(transform, false);
            cam = camGo.GetComponent<Camera>();
            cam.backgroundColor = CityWalkBootstrap.FogColor;
            cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 400f;

            startTime = Time.time;
            PlaceCamera(0f);
        }

        /// <summary>(Re)builds the character on the stand, playing its idle animation: <paramref name="option"/>, or the saved choice.</summary>
        public void ShowCharacter(CharacterOption option = null)
        {
            if (character != null) Destroy(character.gameObject);
            var driver = CharacterAnimatorDriver.Create(stand, null, option);   // no mover: the blend stays on Idle, no footsteps
            character = driver != null ? driver.transform : null;
        }

        void LateUpdate()
        {
            float t = Time.time - startTime;
            if (character != null)
                character.localRotation = Quaternion.Euler(0f, SwayDegrees * Mathf.Sin(t * 2f * Mathf.PI / SwayPeriod), 0f);
            PlaceCamera(t);
        }

        void PlaceCamera(float t)
        {
            if (cam == null) return;
            Vector3 pivot = StandPosition + Vector3.up * LookHeight;
            float drift = DriftDegrees * Mathf.Sin(t * 2f * Mathf.PI / DriftPeriod);
            Vector3 offset = Quaternion.Euler(0f, drift, 0f) * CameraOffset;
            offset.y += 0.06f * Mathf.Sin(t * 2f * Mathf.PI / (DriftPeriod * 0.7f));
            Vector3 pos = StandPosition + offset;
            Vector3 toPivot = pivot - pos;
            cam.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, FrameYaw, 0f) * Quaternion.LookRotation(toPivot));
        }
    }
}
