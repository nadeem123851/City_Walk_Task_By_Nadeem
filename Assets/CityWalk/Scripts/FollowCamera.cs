using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Behind-the-back camera. It swings round behind the player while they walk, and pulls in
    /// when a building gets between the player's head and the camera, so it never shows the
    /// inside of a building. Pulls in fast, eases back out slowly.
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public PlayerMover player;
        public float distance = 4.2f;
        public float height = 1.9f;
        public float lookHeight = 1.5f;
        public float pitch = 12f;
        public float yawFollowSpeed = 2.5f;
        public float positionSmoothing = 10f;

        [Header("Collision")]
        [Tooltip("Layers the camera must stay out of.")]
        public LayerMask collisionMask;
        [Tooltip("Radius of the sphere swept from the head to the camera; keeps the near plane off walls.")]
        public float collisionRadius = 0.3f;
        [Tooltip("The camera never gets closer to the head than this.")]
        public float minDistance = 1.25f;
        [Tooltip("How fast the camera pulls in when blocked (per second).")]
        public float pullInSpeed = 16f;
        [Tooltip("How fast the camera eases back out when clear (per second).")]
        public float easeOutSpeed = 3f;

        float yaw;
        Vector3 idealPosition;     // where the camera wants to be, ignoring buildings
        float distanceFraction = 1f; // share of the head-to-camera line actually used

        /// <summary>True while a building is pushing the camera in.</summary>
        public bool IsPulledIn => distanceFraction < 0.98f;

        void Awake()
        {
            if (collisionMask.value == 0)
                collisionMask = 1 << CityBuilder.BuildingsLayer;
        }

        public void Snap()
        {
            if (target == null) return;
            yaw = target.eulerAngles.y;
            idealPosition = DesiredPosition();
            distanceFraction = AllowedFraction(Pivot, idealPosition - Pivot);
            ApplyPosition();
        }

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;

            bool moving = player != null && player.CurrentSpeed > 0.2f;
            if (moving)
                yaw = Mathf.LerpAngle(yaw, target.eulerAngles.y, yawFollowSpeed * dt);

            idealPosition = Vector3.Lerp(idealPosition, DesiredPosition(), positionSmoothing * dt);

            float wanted = AllowedFraction(Pivot, idealPosition - Pivot);
            float speed = wanted < distanceFraction ? pullInSpeed : easeOutSpeed;
            distanceFraction = Mathf.Lerp(distanceFraction, wanted, 1f - Mathf.Exp(-speed * dt));

            ApplyPosition();
        }

        Vector3 Pivot => target.position + Vector3.up * lookHeight;

        void ApplyPosition()
        {
            Vector3 pivot = Pivot;
            transform.position = pivot + (idealPosition - pivot) * distanceFraction;
            transform.LookAt(pivot);
        }

        /// <summary>How much of the line from the head to the ideal position is clear of buildings.</summary>
        float AllowedFraction(Vector3 pivot, Vector3 toCamera)
        {
            float length = toCamera.magnitude;
            if (length < 0.001f) return 1f;

            float clear = length;
            if (Physics.SphereCast(pivot, collisionRadius, toCamera / length, out RaycastHit hit, length,
                    collisionMask, QueryTriggerInteraction.Ignore))
                clear = hit.distance;

            return Mathf.Clamp(Mathf.Max(clear, minDistance) / length, 0f, 1f);
        }

        Vector3 DesiredPosition()
        {
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            return target.position + Vector3.up * height + rot * new Vector3(0f, 0f, -distance);
        }
    }
}
