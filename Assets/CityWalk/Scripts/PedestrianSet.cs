using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityWalk
{
    /// <summary>A closed walking loop on the pavement, as corner points on the ground (x, z in world space).</summary>
    [Serializable]
    public class PedestrianRoute
    {
        public string name;
        public List<Vector2> corners = new List<Vector2>();

        public PedestrianRoute() { }

        public PedestrianRoute(string name, params Vector2[] corners)
        {
            this.name = name;
            this.corners = new List<Vector2>(corners);
        }

        /// <summary>The loop as a path, with corners rounded so walkers turn smoothly.</summary>
        public RoutePath ToPath(float cornerRadius = 0.6f)
        {
            return new RoutePath(RoutePath.RoundCorners(corners, cornerRadius, 4), true);
        }

        /// <summary>A square loop round a block centre, <paramref name="halfSize"/> metres out from it.</summary>
        public static PedestrianRoute BlockLoop(string name, float cx, float cz, float halfSize)
        {
            return new PedestrianRoute(name,
                new Vector2(cx - halfSize, cz - halfSize), new Vector2(cx + halfSize, cz - halfSize),
                new Vector2(cx + halfSize, cz + halfSize), new Vector2(cx - halfSize, cz + halfSize));
        }
    }

    /// <summary>One walker. Add an entry to the list to add a pedestrian.</summary>
    [Serializable]
    public class PedestrianEntry
    {
        [Tooltip("Index into Routes.")] public int route;
        [Tooltip("Index into Characters.")] public int character;
        [Tooltip("Where on the loop the walker starts, 0 to 1.")] [Range(0f, 1f)] public float startOffset;
        [Tooltip("Walking speed in m/s.")] public float speed = 1.4f;
        [Tooltip("Walk the loop the other way round.")] public bool reverse;
        [Tooltip("Size, 1 = as modelled.")] public float scale = 1f;
        [Tooltip("Main clothing colour. Leave alpha at 0 to keep the outfit's own colour.")] public Color jacket = Color.clear;
        [Tooltip("Leaves once rush hour is over (only while off screen).")] public bool rushHourOnly;
    }

    /// <summary>
    /// The pedestrians: character models, the walk animation, pavement loops and the walker list.
    /// One asset lives in Resources so the runtime-built city can find it and it ships in builds.
    /// </summary>
    [CreateAssetMenu(fileName = "PedestrianSet", menuName = "City Walk/Pedestrian Set")]
    public class PedestrianSet : ScriptableObject
    {
        [Tooltip("Character models (Quaternius, all on the same CharacterArmature rig).")]
        public List<GameObject> characters = new List<GameObject>();
        [Tooltip("Controller that plays the walk loop.")]
        public RuntimeAnimatorController walkController;
        [Tooltip("Speed the walk clip looks right at, in m/s. The animation is sped up or slowed to match each walker.")]
        public float clipWalkSpeed = 1.38f;
        [Tooltip("Walking pace once rush hour is over, as a fraction of each walker's rush-hour speed. " +
                 "Movement and animation slow together so the feet do not slide.")]
        [Range(0.3f, 1f)] public float normalPace = 0.72f;
        [Tooltip("Shadows are only cast within this distance of the camera.")]
        public float shadowDistance = 26f;

        public List<PedestrianRoute> routes = new List<PedestrianRoute>();
        public List<PedestrianEntry> walkers = new List<PedestrianEntry>();

        static PedestrianSet instance;

        /// <summary>The PedestrianSet asset in Resources, or null if it is missing.</summary>
        public static PedestrianSet Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<PedestrianSet>("PedestrianSet");
                return instance;
            }
        }

        /// <summary>Default layout from the web preview: 15 walkers, 5 of them only during rush hour.</summary>
        void Reset()
        {
            routes = DefaultRoutes();
            walkers = DefaultWalkers();
        }

        // Unity z is minus the web preview's z. Blocks are 24 m with 10 m roads (pitch 34); the park is the
        // centre block. Pavements run 2.5 m from the kerb to the buildings, so loops 0.6 to 1.6 m in from the
        // kerb stay clear of walls, and lamp posts sit at the block corners, outside every loop.
        public static List<PedestrianRoute> DefaultRoutes()
        {
            const float strip0 = 22.6f, strip1 = 23.6f, along = 10.5f;
            return new List<PedestrianRoute>
            {
                PedestrianRoute.BlockLoop("Park pavement", 0f, 0f, 10.85f),
                new PedestrianRoute("South strip (kiosk side)",
                    new Vector2(-along, -strip0), new Vector2(along, -strip0), new Vector2(along, -strip1), new Vector2(-along, -strip1)),
                new PedestrianRoute("North strip (bank side)",
                    new Vector2(-along, strip0), new Vector2(along, strip0), new Vector2(along, strip1), new Vector2(-along, strip1)),
                new PedestrianRoute("West strip (cafe side)",
                    new Vector2(-strip0, -along), new Vector2(-strip0, along), new Vector2(-strip1, along), new Vector2(-strip1, -along)),
                PedestrianRoute.BlockLoop("Kiosk block", 34f, -34f, 10.7f),
                PedestrianRoute.BlockLoop("South-west block", -34f, -34f, 10.7f),
                PedestrianRoute.BlockLoop("North-east block", 34f, 34f, 10.7f),
            };
        }

        public static List<PedestrianEntry> DefaultWalkers()
        {
            // Jacket colours from the web preview; clear keeps the outfit as modelled.
            // Character 0 (Suit) is left out: that is the player, who should stand out.
            // The pavement strips are thin loops whose two lanes already carry people both ways, so nobody walks
            // them reversed (a reversed walker would have to step into the other lane to pass).
            Color c = Color.clear;
            Color[] j = { Hex(0x7a2f3a), Hex(0x2f4f7a), Hex(0x6b4a2e), Hex(0x2f6b4a), Hex(0x8a8a8a), Hex(0x3a3a44), Hex(0x9a6a3a), Hex(0x4a3a6b) };
            var w = new List<PedestrianEntry>
            {
                W(0, 6, 0.00f, 1.45f, false, j[5]),    W(0, 1, 0.27f, 1.30f, true, j[1]),  W(0, 2, 0.55f, 1.55f, false, c),
                W(0, 3, 0.80f, 1.25f, true, c, true),
                W(1, 4, 0.10f, 1.50f, false, c),    W(1, 1, 0.60f, 1.35f, false, j[0]),  W(1, 7, 0.35f, 1.40f, false, j[3], true),
                W(2, 4, 0.20f, 1.40f, false, j[2]),     W(2, 6, 0.70f, 1.30f, false, c, true),
                W(3, 2, 0.15f, 1.45f, false, j[6]), W(3, 5, 0.65f, 1.35f, false, c, true),
                W(4, 7, 0.40f, 1.40f, false, c),    W(4, 1, 0.90f, 1.60f, true, j[7], true),
                W(5, 3, 0.50f, 1.30f, false, c),    W(6, 5, 0.25f, 1.50f, true, c),
            };
            for (int i = 0; i < w.Count; i++) w[i].scale = 0.95f + (i * 37 % 10) * 0.01f;
            return w;
        }

        static PedestrianEntry W(int route, int character, float offset, float speed, bool reverse, Color jacket, bool rushOnly = false)
        {
            return new PedestrianEntry
            {
                route = route, character = character, startOffset = offset, speed = speed,
                reverse = reverse, jacket = jacket, rushHourOnly = rushOnly,
            };
        }

        static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f, 1f);
        }
    }
}
