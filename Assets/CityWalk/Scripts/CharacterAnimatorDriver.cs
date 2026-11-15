using UnityEngine;
using UnityEngine.Rendering;

namespace CityWalk
{
    /// <summary>
    /// Builds the player's character model and drives its animator from the player's movement.
    /// The controller blends Idle, Walk and Run on the forward speed, and Run_Back / Run_Left / Run_Right on the
    /// sideways and backward speed (seen while the player turns). Parameters are smoothed, and playback speeds up
    /// with the ground speed so the feet slide as little as possible.
    /// </summary>
    public class CharacterAnimatorDriver : MonoBehaviour
    {
        public static readonly int MoveX = Animator.StringToHash("MoveX");
        public static readonly int MoveZ = Animator.StringToHash("MoveZ");

        PlayerMover mover;
        PlayerCharacter setup;
        Animator animator;
        float playbackRate = 1f;

        /// <summary>The animator on the character model.</summary>
        public Animator Animator => animator;

        /// <summary>Adds the character model under <paramref name="player"/>. Returns null if the asset is missing.</summary>
        public static CharacterAnimatorDriver Create(Transform player, PlayerMover mover)
        {
            var setup = PlayerCharacter.Instance;
            if (setup == null || setup.model == null)
            {
                Debug.LogWarning("CityWalk: Resources/PlayerCharacter is missing or has no model; the player is invisible.");
                return null;
            }

            var model = Instantiate(setup.model, player, false);
            model.name = "Character";
            var driver = model.AddComponent<CharacterAnimatorDriver>();
            driver.mover = mover;
            driver.setup = setup;
            driver.SetUp(model);
            return driver;
        }

        void SetUp(GameObject model)
        {
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                // Same cached URP Lit materials as the rest of the city, so they always render in builds.
                var mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] == null) continue;
                    Color c = mats[m].HasProperty("_BaseColor") ? mats[m].GetColor("_BaseColor") : mats[m].color;
                    c.a = 1f;
                    mats[m] = MaterialFactory.Get(c);
                }
                r.sharedMaterials = mats;
                r.updateWhenOffscreen = true;   // the camera is always on the player; avoids pop-in at odd angles
                r.shadowCastingMode = ShadowCastingMode.On;
                r.lightProbeUsage = LightProbeUsage.Off;
            }

            animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = setup.controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (setup.briefcase)
            {
                var hand = FindChild(model.transform, "Wrist.L");
                if (hand != null) AddBriefcase(hand);
            }
        }

        void Update()
        {
            if (mover == null || animator == null) return;
            float dt = Time.deltaTime;

            // Velocity relative to where the player faces: +z forward, +x to the right.
            Vector3 local = transform.InverseTransformDirection(mover.Velocity);
            local.y = 0f;
            animator.SetFloat(MoveX, local.x, setup.dampTime, dt);
            animator.SetFloat(MoveZ, local.z, setup.dampTime, dt);

            float target = PlaybackRateFor(local);
            playbackRate = Mathf.Lerp(playbackRate, target, 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, setup.dampTime)));
            animator.speed = playbackRate;
        }

        /// <summary>
        /// How much faster than recorded the blend must play so its feet keep up with the ground.
        /// The tree places Walk at the walk speed and Run at the brisk speed (forward), and the three
        /// directional runs at the walk speed; this estimates the blend's own foot speed the same way.
        /// </summary>
        float PlaybackRateFor(Vector3 local)
        {
            float speed = local.magnitude;
            if (speed < 0.05f) return 1f;

            float walk = Mathf.Max(0.1f, mover.walkSpeed), run = Mathf.Max(walk + 0.1f, mover.runSpeed);
            float forwardClip = speed <= walk
                ? setup.walkClipSpeed * speed / walk
                : Mathf.Lerp(setup.walkClipSpeed, setup.runClipSpeed, Mathf.InverseLerp(walk, run, speed));
            float otherClip = setup.runClipSpeed * Mathf.Min(1f, speed / walk);
            float forwardness = Mathf.Clamp01(local.z / speed);
            float clipSpeed = Mathf.Lerp(otherClip, forwardClip, forwardness);

            return Mathf.Clamp(speed / Mathf.Max(0.05f, clipSpeed), 1f, Mathf.Max(1f, setup.maxPlaybackRate));
        }

        static Transform FindChild(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        /// <summary>
        /// Where the briefcase sits relative to the hand bone, in metres (tuned by hand in the Inspector:
        /// mostly 3.4 cm in towards the leg so the handle sits in the palm).
        /// </summary>
        static readonly Vector3 BriefcaseOffset = new Vector3(-0.001f, -0.005f, -0.034f);

        /// <summary>A small leather briefcase held in the left hand, hanging beside the leg.</summary>
        static void AddBriefcase(Transform hand)
        {
            var leather = new Color(0.35f, 0.23f, 0.13f);
            var dark = new Color(0.11f, 0.08f, 0.06f);

            var root = new GameObject("Briefcase").transform;
            root.SetParent(hand, false);
            Vector3 s = hand.lossyScale;
            root.localScale = new Vector3(1f / Mathf.Max(1e-4f, s.x), 1f / Mathf.Max(1e-4f, s.y), 1f / Mathf.Max(1e-4f, s.z));
            // The bone is scaled (x100 on this rig), so convert the offset from metres to its local units.
            root.localPosition = Vector3.Scale(BriefcaseOffset, root.localScale);
            root.localRotation = Quaternion.identity;

            // Measured at idle: the bone's +Y points down the fingers, -X points forward, +Z points away from the body.
            MaterialFactory.Primitive(PrimitiveType.Cube, "Handle", root,
                new Vector3(0f, 0.09f, 0.02f), new Vector3(0.13f, 0.03f, 0.03f), dark);
            MaterialFactory.Primitive(PrimitiveType.Cube, "Case", root,
                new Vector3(0f, 0.25f, 0.03f), new Vector3(0.40f, 0.28f, 0.09f), leather);
        }
    }
}
