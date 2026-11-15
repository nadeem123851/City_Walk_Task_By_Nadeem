using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Entry point. Put this on one empty GameObject in an empty scene and press Play.
    /// It builds the lighting, city, player, camera, HUD and route in code.
    /// </summary>
    public class CityWalkBootstrap : MonoBehaviour
    {
        public int citySeed = 7;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;

            SetupLighting();

            var city = new GameObject("City").AddComponent<CityBuilder>();
            city.randomSeed = citySeed;
            city.Build();
            float p = city.Pitch;

            // Player
            var playerGo = new GameObject("Player");
            var cc = playerGo.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.35f;
            var mover = playerGo.AddComponent<PlayerMover>();
            mover.avatar = SuitedManAvatar.Create(playerGo.transform);

            // Camera
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.backgroundColor = FogColor;
            cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 400f;
            var follow = camGo.AddComponent<FollowCamera>();
            follow.target = playerGo.transform;
            follow.player = mover;
            mover.cameraTransform = camGo.transform;

            // HUD
            var hud = BasicHud.Create();
            mover.joystick = hud.Joystick;

            // Spawn on the road just south of the park, facing east
            mover.Teleport(new Vector3(-p * 0.5f + 4f, 0.05f, -p * 0.5f), 90f);
            follow.Snap();

            // Route
            var route = new GameObject("Route").AddComponent<CheckpointRoute>();
            route.Init(playerGo.transform, hud, new[]
            {
                new CheckpointRoute.Stop { title = "Grab a coffee", hint = "The coffee kiosk is at the next crossing", position = new Vector3(p * 0.5f, 0f, -p * 0.5f) },
                new CheckpointRoute.Stop { title = "Stop by the bank", hint = "Head north up the avenue", position = new Vector3(p * 0.5f, 0f, p * 1.5f) },
                new CheckpointRoute.Stop { title = "Get to the office", hint = "Your office is to the west", position = new Vector3(-p * 1.5f, 0f, p * 1.5f) },
            });
        }

        // Haze colour from the web preview; also the camera background if the sky material is missing.
        static readonly Color FogColor = new Color(0.725f, 0.827f, 0.925f);

        static void SetupLighting()
        {
            // Warm morning sun. Direction is the web preview's SUN_DIR with z mirrored for Unity.
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.35f;
            sun.color = new Color(1f, 0.93f, 0.82f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sun.transform.rotation = Quaternion.LookRotation(new Vector3(0.42f, -0.80f, 0.43f));
            RenderSettings.sun = sun;

            // Cool sky light from above, warm bounce from the street below.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.72f, 0.79f, 0.88f);
            RenderSettings.ambientEquatorColor = new Color(0.58f, 0.60f, 0.62f);
            RenderSettings.ambientGroundColor = new Color(0.38f, 0.35f, 0.31f);

            // Gradient sky (Skybox/Panoramic with the pack's sky_gradient texture).
            var sky = CityArt.Instance.sky;
            if (sky != null) RenderSettings.skybox = sky;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogStartDistance = 70f;
            RenderSettings.fogEndDistance = 230f;
        }
    }
}
