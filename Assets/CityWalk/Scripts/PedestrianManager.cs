using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityWalk
{
    /// <summary>
    /// Spawns the walkers listed in the <see cref="PedestrianSet"/> and moves them round their pavement loops.
    /// Kept light for phones: one Update for all walkers, animators stop when off screen, shadows only near
    /// the camera, and every walker shares the same cached materials (no per-walker material copies).
    /// </summary>
    public class PedestrianManager : MonoBehaviour
    {
        const float PavementHeight = 0.15f;
        const float TurnRate = 5f;            // radians per second, as in the web preview
        const float ShadowCheckInterval = 0.25f;

        // Avoidance: walkers look ahead and give way instead of walking through each other.
        const float LookAhead = 3f;          // metres
        const float PassingWidth = 0.7f;     // closer than this sideways counts as in the way (parallel lanes are 0.8 m apart)
        const float SideStep = 0.5f;         // how far to step right to let someone pass
        const float SideStepRate = 1.2f;     // m/s sideways when stepping out
        const float ReturnRate = 0.5f;       // m/s sideways when drifting back to the line
        const float FollowGap = 1.1f;        // keep this far behind a slower walker
        const float PaceChangeRate = 0.35f;  // how fast the pace eases when rush hour ends

        class Walker
        {
            public Transform root;
            public Animator animator;
            public SkinnedMeshRenderer[] renderers;
            public RoutePath path;
            public float distance, speed, heading;
            public float currentSpeed, sideOffset;
            public Vector2 position, linePosition, forward;   // linePosition: on the route, before stepping aside
            public bool reverse, rushHourOnly, castsShadow;
        }

        readonly List<Walker> walkers = new List<Walker>();
        RouteDirector route;
        Transform cameraTransform;
        PedestrianSet set;
        float shadowTimer;
        float pace = 1f;   // 1 in rush hour, PedestrianSet.normalPace after it

        /// <summary>The current walking pace: 1 during rush hour, slower afterwards.</summary>
        public float Pace => pace;

        public int ActiveCount
        {
            get
            {
                int n = 0;
                foreach (var w in walkers) if (w.root.gameObject.activeSelf) n++;
                return n;
            }
        }

        public static PedestrianManager Create(RouteDirector route, Transform cameraTransform)
        {
            var set = PedestrianSet.Instance;
            if (set == null || set.characters.Count == 0 || set.routes.Count == 0)
            {
                Debug.LogWarning("CityWalk: Resources/PedestrianSet is missing or empty; no pedestrians.");
                return null;
            }
            var manager = new GameObject("Pedestrians").AddComponent<PedestrianManager>();
            manager.route = route;
            manager.cameraTransform = cameraTransform;
            manager.set = set;
            manager.Spawn();
            return manager;
        }

        /// <summary>
        /// The walker's model. A walker that would look like the player's chosen character takes the next model in
        /// the list instead (skipping the first, the suit, which is kept for the player), so the player never meets a twin.
        /// </summary>
        GameObject ModelFor(int character)
        {
            int n = set.characters.Count;
            int index = Mathf.Abs(character) % n;
            var player = PlayerCharacter.Instance != null ? PlayerCharacter.Instance.Selected : null;
            if (player == null || set.characters[index] != player.model) return set.characters[index];
            for (int k = 1; k < n; k++)
            {
                int other = (index + k) % n;
                if (other != 0 && set.characters[other] != player.model) return set.characters[other];
            }
            return null;
        }

        void Spawn()
        {
            var paths = new List<RoutePath>();
            foreach (var r in set.routes) paths.Add(r.corners.Count >= 2 ? r.ToPath() : null);

            for (int i = 0; i < set.walkers.Count; i++)
            {
                var entry = set.walkers[i];
                if (entry.route < 0 || entry.route >= paths.Count || paths[entry.route] == null) continue;
                var prefab = ModelFor(entry.character);
                if (prefab == null) continue;

                var go = Instantiate(prefab, transform);
                go.name = $"Pedestrian_{i:00}_{prefab.name}";
                go.transform.localScale = Vector3.one * Mathf.Max(0.1f, entry.scale);

                var w = new Walker
                {
                    root = go.transform,
                    path = paths[entry.route],
                    speed = Mathf.Max(0.1f, entry.speed),
                    reverse = entry.reverse,
                    rushHourOnly = entry.rushHourOnly,
                    renderers = go.GetComponentsInChildren<SkinnedMeshRenderer>(true),
                };
                w.distance = entry.startOffset * w.path.Length;
                w.currentSpeed = w.speed;

                SetUpRenderers(w, entry.jacket);
                SetUpAnimator(w, go, i);
                Place(w, 0f, snap: true);
                walkers.Add(w);
            }
        }

        void SetUpRenderers(Walker w, Color jacket)
        {
            // The outfit's main colour is the first material on the body mesh; tinting it varies the clothes.
            string jacketName = null;
            foreach (var r in w.renderers)
                if (r.name.EndsWith("_Body") && r.sharedMaterials.Length > 0 && r.sharedMaterials[0] != null)
                    jacketName = r.sharedMaterials[0].name;

            foreach (var r in w.renderers)
            {
                var mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] == null) continue;
                    Color colour = mats[m].HasProperty("_BaseColor") ? mats[m].GetColor("_BaseColor") : mats[m].color;
                    if (jacket.a > 0f && mats[m].name == jacketName) colour = jacket;
                    colour.a = 1f;
                    // Cached by colour, so walkers wearing the same colour share one material.
                    mats[m] = MaterialFactory.Get(colour);
                }
                r.sharedMaterials = mats;
                r.updateWhenOffscreen = false;
                r.skinnedMotionVectors = false;
                r.quality = SkinQuality.Bone2;
                r.lightProbeUsage = LightProbeUsage.Off;
                r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                r.shadowCastingMode = ShadowCastingMode.On;
            }
            w.castsShadow = true;
        }

        void SetUpAnimator(Walker w, GameObject go, int index)
        {
            var animator = go.GetComponent<Animator>();
            if (animator == null) animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = set.walkController;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullCompletely;
            go.AddComponent<FootstepEvents>();   // the walk clip has footstep events; pedestrians stay silent
            animator.speed = w.speed / Mathf.Max(0.1f, set.clipWalkSpeed);
            if (set.walkController != null)
                animator.Play(0, 0, Mathf.Repeat(index * 0.37f, 1f));   // out of step with each other
            w.animator = animator;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            bool rush = route == null || route.IsRushHour;
            // Brisk in rush hour; once it is over everyone eases down to a relaxed walk.
            pace = Mathf.MoveTowards(pace, rush ? 1f : Mathf.Clamp(set.normalPace, 0.3f, 1f), PaceChangeRate * dt);

            shadowTimer -= dt;
            bool checkShadows = shadowTimer <= 0f;
            if (checkShadows) shadowTimer = ShadowCheckInterval;

            foreach (var w in walkers)
            {
                if (!w.root.gameObject.activeSelf) continue;

                // Once rush hour is over the extra walkers leave, but only where nobody sees them go.
                if (w.rushHourOnly && !rush && !IsVisible(w))
                {
                    w.root.gameObject.SetActive(false);
                    continue;
                }

                Avoid(w, dt);
                Place(w, dt, snap: false);
                if (checkShadows) UpdateShadow(w);
            }
        }

        /// <summary>
        /// Gives way to the walkers ahead: step right for someone coming the other way (they do the same, so
        /// the two pass), and slow to the pace of a slower walker in front instead of walking through them.
        /// </summary>
        void Avoid(Walker w, float dt)
        {
            float wantedSpeed = w.speed * pace;
            float stepSide = 0f;   // +1 step right, -1 step left, 0 stay on the line
            Vector2 right = new Vector2(w.forward.y, -w.forward.x);

            foreach (var o in walkers)
            {
                if (o == w || !o.root.gameObject.activeSelf) continue;

                if (o.path == w.path)
                {
                    // Same loop: measure the gap along the loop, which stays right through the U-turns.
                    float gap = Mathf.Repeat(w.reverse ? w.distance - o.distance : o.distance - w.distance, w.path.Length);
                    if (gap > LookAhead) continue;
                    // Walking the loop the other way: both keep right, which is always opposite sides of the line.
                    if (o.reverse != w.reverse) stepSide = 1f;
                    else if (gap < FollowGap + 0.8f) wantedSpeed = Mathf.Min(wantedSpeed, FollowSpeed(w, o, gap));
                    continue;
                }

                // Measured between route lines, so stepping aside does not end the pass half way.
                Vector2 d = o.linePosition - w.linePosition;
                if (d.sqrMagnitude > LookAhead * LookAhead) continue;
                float ahead = Vector2.Dot(d, w.forward);
                if (ahead <= 0f || Mathf.Abs(Vector2.Dot(d, right)) > PassingWidth) continue;

                if (Vector2.Dot(w.forward, o.forward) < 0.3f)
                {
                    stepSide = SideAwayFrom(o, w, right);   // coming towards us or crossing
                }
                else if (ahead < FollowGap + 0.8f)
                {
                    // Same way and close: drop to their speed, slower still if too close.
                    wantedSpeed = Mathf.Min(wantedSpeed, FollowSpeed(w, o, ahead));
                }
            }

            w.currentSpeed = Mathf.MoveTowards(w.currentSpeed, wantedSpeed, 2f * dt);
            float step = stepSide != 0f ? FreeStep(w, right * stepSide) * stepSide : 0f;
            bool steppingOut = Mathf.Abs(step) > Mathf.Abs(w.sideOffset) || step * w.sideOffset < 0f;
            w.sideOffset = Mathf.MoveTowards(w.sideOffset, step, (steppingOut ? SideStepRate : ReturnRate) * dt);
            if (w.animator != null) w.animator.speed = w.currentSpeed / Mathf.Max(0.1f, set.clipWalkSpeed);
        }

        /// <summary>Speed behind a slower walker: their pace at the follow gap, slower still if closer.</summary>
        float FollowSpeed(Walker w, Walker leader, float gap)
        {
            float closeness = Mathf.Clamp01((gap - FollowGap) / 0.8f);
            return Mathf.Lerp(leader.currentSpeed * 0.8f, w.speed * pace, closeness);
        }

        /// <summary>
        /// Which way to step to pass <paramref name="o"/> on another loop: away from their line, or to the right when
        /// the lines meet. Both walkers work this out the same way, so they step to opposite sides.
        /// </summary>
        static float SideAwayFrom(Walker o, Walker w, Vector2 right)
        {
            float lateral = Vector2.Dot(o.linePosition - w.linePosition, right);
            return Mathf.Abs(lateral) < 0.05f ? 1f : -Mathf.Sign(lateral);
        }

        /// <summary>How far the walker can step to the given side without touching a wall, column, table or lamp post.</summary>
        static float FreeStep(Walker w, Vector2 side)
        {
            for (float s = SideStep; s > 0.05f; s -= 0.15f)
            {
                Vector2 p = w.linePosition + side * s;
                var foot = new Vector3(p.x, PavementHeight, p.y);
                if (!Physics.CheckCapsule(foot + Vector3.up * 0.55f, foot + Vector3.up * 1.6f, 0.25f, ~0, QueryTriggerInteraction.Ignore))
                    return s;
            }
            return 0f;
        }

        void Place(Walker w, float dt, bool snap)
        {
            w.distance += w.currentSpeed * dt * (w.reverse ? -1f : 1f);
            w.distance = Mathf.Repeat(w.distance, w.path.Length);
            Vector2 p = w.path.PointAt(w.distance, out Vector2 dir);
            if (w.reverse) dir = -dir;
            w.forward = dir.sqrMagnitude > 0f ? dir.normalized : Vector2.up;
            w.linePosition = p;
            w.position = p + new Vector2(w.forward.y, -w.forward.x) * w.sideOffset;

            float target = Mathf.Atan2(dir.x, dir.y);
            w.heading = snap ? target : w.heading + Mathf.Clamp(Mathf.DeltaAngle(w.heading * Mathf.Rad2Deg, target * Mathf.Rad2Deg) * Mathf.Deg2Rad, -TurnRate * dt, TurnRate * dt);
            w.root.SetPositionAndRotation(new Vector3(w.position.x, PavementHeight, w.position.y), Quaternion.Euler(0f, w.heading * Mathf.Rad2Deg, 0f));
        }

        void UpdateShadow(Walker w)
        {
            if (cameraTransform == null) return;
            bool cast = (w.root.position - cameraTransform.position).sqrMagnitude < set.shadowDistance * set.shadowDistance;
            if (cast == w.castsShadow) return;
            w.castsShadow = cast;
            foreach (var r in w.renderers) r.shadowCastingMode = cast ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }

        static bool IsVisible(Walker w)
        {
            foreach (var r in w.renderers) if (r.isVisible) return true;
            return false;
        }
    }
}
