using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Simple behind-the-back camera. It swings round behind the player while they walk.
    /// No collision handling yet: the camera can clip into buildings (part of the task).
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

        float yaw;

        public void Snap()
        {
            if (target == null) return;
            yaw = target.eulerAngles.y;
            transform.position = DesiredPosition();
            transform.LookAt(target.position + Vector3.up * lookHeight);
        }

        void LateUpdate()
        {
            if (target == null) return;

            bool moving = player != null && player.CurrentSpeed > 0.2f;
            if (moving)
                yaw = Mathf.LerpAngle(yaw, target.eulerAngles.y, yawFollowSpeed * Time.deltaTime);

            transform.position = Vector3.Lerp(transform.position, DesiredPosition(), positionSmoothing * Time.deltaTime);
            transform.LookAt(target.position + Vector3.up * lookHeight);
        }

        Vector3 DesiredPosition()
        {
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            return target.position + Vector3.up * height + rot * new Vector3(0f, 0f, -distance);
        }
    }
}
