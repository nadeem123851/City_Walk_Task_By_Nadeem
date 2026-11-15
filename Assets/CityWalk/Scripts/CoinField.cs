using System.Collections.Generic;
using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Gold coins to pick up along the route, placed as in the web preview: three on each side of the park loop
    /// and two in the alley come back every lap; three on the road to the office are there once.
    /// Walking within reach collects a coin (sparkle, sound, HUD counter, total on the end card).
    /// </summary>
    public class CoinField : MonoBehaviour
    {
        const float PickupRadius = 1.3f;      // metres, measured on the ground
        const float Height = 1.0f;            // coin centre above the road
        const float Diameter = 0.68f;
        const float Thickness = 0.07f;
        const float SpinSpeed = 172f;         // degrees per second (3 rad/s, as in the web preview)

        static readonly Color CoinGold = new Color(0.949f, 0.761f, 0.188f);   // #F2C230

        // Unity coordinates (web preview z flipped). The park loop runs on the road centre lines at +-17.
        static readonly Vector2[] LapCoins =
        {
            new Vector2(-4f, -17f), new Vector2(0f, -17f), new Vector2(4f, -17f),      // south road, from the lap line
            new Vector2(17f, 6f), new Vector2(17f, 9f), new Vector2(17f, 12f),          // east road
            new Vector2(26f, 0f), new Vector2(29f, 0f),                                 // in the alley
            new Vector2(-2f, 17f), new Vector2(-5f, 17f), new Vector2(-8f, 17f),        // north road
            new Vector2(-17f, 4f), new Vector2(-17f, 0f), new Vector2(-17f, -4f),       // west road
        };
        static readonly Vector2[] OfficeCoins =
        {
            new Vector2(26f, -17f), new Vector2(31f, -17f), new Vector2(36f, -17f),
        };

        class Coin
        {
            public Transform transform;
            public Vector2 ground;
            public bool respawns;
            public float phase;
        }

        readonly List<Coin> coins = new List<Coin>();
        RouteDirector route;
        Transform player;

        public int ActiveCount
        {
            get
            {
                int n = 0;
                foreach (var c in coins) if (c.transform.gameObject.activeSelf) n++;
                return n;
            }
        }

        public static CoinField Create(RouteDirector route, Transform player)
        {
            var field = new GameObject("Coins").AddComponent<CoinField>();
            field.route = route;
            field.player = player;
            field.Build();
            route.LapCompleted += (lap, time) => field.RespawnLapCoins();
            return field;
        }

        void Build()
        {
            var material = MaterialFactory.Get(CoinGold, 0.25f);   // a soft glow, like the web preview's emissive gold, keeping the shading
            int i = 0;
            foreach (var p in LapCoins) coins.Add(Make(p, true, material, i++));
            foreach (var p in OfficeCoins) coins.Add(Make(p, false, material, i++));
        }

        Coin Make(Vector2 ground, bool respawns, Material material, int index)
        {
            var go = MaterialFactory.Primitive(PrimitiveType.Cylinder, "Coin", transform,
                new Vector3(ground.x, Height, ground.y), new Vector3(Diameter, Thickness * 0.5f, Diameter), material);
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return new Coin { transform = go.transform, ground = ground, respawns = respawns, phase = index };
        }

        void Update()
        {
            if (player == null) return;
            float t = Time.time;
            Vector2 p = new Vector2(player.position.x, player.position.z);

            foreach (var c in coins)
            {
                if (!c.transform.gameObject.activeSelf) continue;

                // Spin round the vertical axis with the face upright, and bob gently.
                c.transform.SetPositionAndRotation(
                    new Vector3(c.ground.x, Height + Mathf.Sin(t * 3f + c.phase) * 0.08f, c.ground.y),
                    Quaternion.Euler(0f, t * SpinSpeed + c.phase * 57f, 0f) * Quaternion.Euler(90f, 0f, 0f));

                if ((c.ground - p).sqrMagnitude < PickupRadius * PickupRadius && CanCollect())
                {
                    c.transform.gameObject.SetActive(false);
                    Sparkles.Burst(c.transform.position, 8, 0.8f);
                    route.CollectCoin();
                }
            }
        }

        bool CanCollect() => route != null && route.Phase != RoutePhase.Complete && route.Phase != RoutePhase.Failed;

        /// <summary>A new lap: the loop and alley coins are back.</summary>
        void RespawnLapCoins()
        {
            foreach (var c in coins)
                if (c.respawns) c.transform.gameObject.SetActive(true);
        }
    }
}
