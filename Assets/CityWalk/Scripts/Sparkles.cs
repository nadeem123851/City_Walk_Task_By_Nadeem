using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Short bursts of gold sparkles (coins now; lap and coffee moments later). One shared particle system per
    /// scene, emitting at any position, so a burst costs no new objects.
    /// </summary>
    public class Sparkles : MonoBehaviour
    {
        static Sparkles instance;
        ParticleSystem system;

        static readonly Color Gold = new Color(1f, 0.84f, 0.35f);

        /// <summary>Emits <paramref name="count"/> sparkles around <paramref name="position"/>, spread over about <paramref name="radius"/> metres.</summary>
        public static void Burst(Vector3 position, int count, float radius = 1f)
        {
            if (instance == null) Create();
            if (instance.system == null) return;

            var emit = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = Random.onUnitSphere;
                dir.y = Mathf.Abs(dir.y) * 0.8f + 0.2f;   // mostly upwards
                emit.position = position + dir * Random.Range(0f, radius * 0.25f);
                emit.velocity = dir * Random.Range(0.8f, 2.2f) * radius;
                emit.startSize = Random.Range(0.12f, 0.3f) * Mathf.Sqrt(radius);
                emit.startLifetime = Random.Range(0.45f, 0.8f);
                emit.rotation = Random.Range(0f, 360f);
                instance.system.Emit(emit, 1);
            }
        }

        static void Create()
        {
            var go = new GameObject("Sparkles");
            instance = go.AddComponent<Sparkles>();
            var material = CityArt.Instance.sparkle;
            if (material == null)
            {
                Debug.LogWarning("CityWalk: CityArt has no sparkle material; sparkles are off.");
                return;
            }

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;
            main.gravityModifier = 0.25f;
            main.startColor = Gold;

            var emission = ps.emission;
            emission.enabled = false;   // bursts only, through Emit
            var shape = ps.shape;
            shape.enabled = false;

            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

            var drag = ps.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.drag = 2.5f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            instance.system = ps;
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
