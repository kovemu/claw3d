using Claw3D.Physics;
using UnityEngine;

namespace Claw3D.Claw
{
    public sealed class ClawController : MonoBehaviour
    {
        [SerializeField] private ClawPhysicsConfig config;
        [SerializeField] private Rigidbody trolleyBody;
        [SerializeField] private ClawFinger[] fingers;

        public void Configure(ClawPhysicsConfig physicsConfig, Rigidbody trolley, ClawFinger[] clawFingers)
        {
            config = physicsConfig;
            trolleyBody = trolley;
            fingers = clawFingers;
        }

        public void Move(Vector2 input)
        {
            if (config == null || trolleyBody == null) return;

            Vector3 delta = new(input.x, 0f, input.y);
            Vector3 target = trolleyBody.position + delta * (config.trolleySpeed * Time.fixedDeltaTime);
            target.x = Mathf.Clamp(target.x, config.xLimits.x, config.xLimits.y);
            target.z = Mathf.Clamp(target.z, config.zLimits.x, config.zLimits.y);
            trolleyBody.MovePosition(target);
        }

        public void SetGrip(bool closed)
        {
            if (fingers == null) return;
            foreach (ClawFinger finger in fingers)
            {
                if (finger != null) finger.SetClosed(closed);
            }
        }
    }
}
