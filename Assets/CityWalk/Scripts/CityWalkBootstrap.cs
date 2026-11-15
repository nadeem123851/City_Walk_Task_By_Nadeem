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
            CharacterAnimatorDriver.Create(playerGo.transform, mover);

            // Camera
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.backgroundColor = FogColor;
            cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 400f;
            CityLook.ApplyPost(cam);
            var follow = camGo.AddComponent<FollowCamera>();
            follow.target = playerGo.transform;
            follow.player = mover;
            mover.cameraTransform = camGo.transform;

            // HUD
            var hud = HudController.Create();
            mover.joystick = hud.Joystick;

            // Spawn on the lap line on the road south of the park, facing east
            mover.Teleport(new Vector3(CityLandmarks.LapGateX, 0.05f, -p * 0.5f), 90f);
            follow.Snap();

            // Route: three laps round the park with a coffee each lap, then the office
            var route = new GameObject("Route").AddComponent<RouteDirector>();
            route.kioskMarker = new Vector3(p * 0.5f, 0f, -p * 0.5f);
            route.officeMarker = new Vector3(p * 1.5f, 0f, -p * 0.5f);
            route.Begin(mover);
            hud.Bind(route);
            LapCelebration.Create(hud.Root, route, playerGo.transform);
            KioskInteraction.Create(route, playerGo.transform, hud.Root);
            GuidanceChevrons.Create(route, playerGo.transform);
            PedestrianManager.Create(route, camGo.transform);
            CoinField.Create(route, playerGo.transform);
            AudioHub.Instance?.Bind(route);

            // Finish shot: the camera swings round the player at the office and sparkles burst; the end card follows.
            route.RouteCompleted += () =>
            {
                follow.PlayFinishShot();
                Sparkles.Burst(playerGo.transform.position + Vector3.up * 1.5f, 40, 4f);
            };
        }

        // Haze colour from the web preview; also the camera background if the sky material is missing.
        /// <summary>Fog and clear colour: the warm cream of the sky's horizon.</summary>
        public static readonly Color FogColor = CityTextures.Hex(0xE6DFD0);

        /// <summary>Morning sun, sky, ambient light and fog. Shared with the 3D home screen.</summary>
        public static void SetupLighting()
        {
            // Warm morning sun. Direction is the web preview's SUN_DIR with z mirrored for Unity.
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.4f;
            sun.color = CityTextures.Hex(0xFFE7C2);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            sun.transform.rotation = Quaternion.LookRotation(new Vector3(0.42f, -0.80f, 0.43f));
            RenderSettings.sun = sun;
            var lightData = UnityEngine.Rendering.Universal.LightExtensions.GetUniversalAdditionalLightData(sun);
            if (lightData != null) lightData.softShadowQuality = UnityEngine.Rendering.Universal.SoftShadowQuality.Low;

            // Cool sky light from above, warm bounce from the street below.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = CityTextures.Hex(0x9DBBEA);
            RenderSettings.ambientEquatorColor = CityTextures.Hex(0xAAB6C9);
            RenderSettings.ambientGroundColor = CityTextures.Hex(0x8A7A68);

            // Generated panorama sky with clouds, on a copy of the pack's Skybox/Panoramic material
            // (never edit the loaded asset: in the editor that change would be saved into the project).
            var skyAsset = CityArt.Instance.sky;
            if (skyAsset != null)
            {
                var sky = new Material(skyAsset) { name = "CityWalk_Sky" };
                sky.SetTexture("_MainTex", CityTextures.SkyPanorama(-sun.transform.forward));
                if (sky.HasProperty("_Rotation")) sky.SetFloat("_Rotation", 0f);
                RenderSettings.skybox = sky;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 250f;

            CityLook.BuildGradeVolume();
        }
    }
}
