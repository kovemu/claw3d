using Claw3D.Physics;
using UnityEngine;

namespace Claw3D.Claw
{
    public sealed class ClawController : MonoBehaviour
    {
        [SerializeField] private ClawPhysicsConfig config;
        [SerializeField] private Rigidbody trolleyBody;
        [SerializeField] private ConfigurableJoint hoistJoint;
        [SerializeField] private Transform cableVisual;
        [SerializeField] private ClawFinger[] fingers;

        public float CableLength => hoistJoint == null ? 0f : -hoistJoint.connectedAnchor.y;
        public Vector3 TrolleyPosition => trolleyBody == null ? transform.position : trolleyBody.position;

        public void Configure(
            ClawPhysicsConfig physicsConfig,
            Rigidbody trolley,
            ConfigurableJoint joint,
            Transform cable,
            ClawFinger[] clawFingers)
        {
            config = physicsConfig;
            trolleyBody = trolley;
            hoistJoint = joint;
            cableVisual = cable;
            fingers = clawFingers;
            SetCableLength(config.topCableLength);
            SetGrip(false);
        }

        public void MoveAim(Vector2 input)
        {
            if (config == null || trolleyBody == null) return;

            Vector3 delta = new(input.x, 0f, input.y);
            Vector3 target = trolleyBody.position + delta * (config.trolleySpeed * Time.fixedDeltaTime);
            target.x = Mathf.Clamp(target.x, config.xLimits.x, config.xLimits.y);
            target.z = Mathf.Clamp(target.z, config.zLimits.x, config.zLimits.y);
            trolleyBody.MovePosition(target);
        }

        public bool MoveCableToward(float targetLength, float speed)
        {
            float next = Mathf.MoveTowards(CableLength, targetLength, speed * Time.fixedDeltaTime);
            SetCableLength(next);
            return Mathf.Abs(next - targetLength) < 0.01f;
        }

        public bool ReturnHome()
        {
            if (config == null || trolleyBody == null) return true;

            Vector3 target = config.homePosition;
            Vector3 next = Vector3.MoveTowards(trolleyBody.position, target, config.returnSpeed * Time.fixedDeltaTime);
            trolleyBody.MovePosition(next);
            return Vector3.SqrMagnitude(next - target) < 0.0025f;
        }

        public void SetGrip(bool closed)
        {
            if (fingers == null) return;
            foreach (ClawFinger finger in fingers)
            {
                if (finger != null) finger.SetClosed(closed);
            }
        }

        private void SetCableLength(float length)
        {
            if (hoistJoint == null || config == null) return;

            float clamped = Mathf.Clamp(length, config.topCableLength, config.bottomCableLength);
            Vector3 anchor = hoistJoint.connectedAnchor;
            anchor.y = -clamped;
            hoistJoint.connectedAnchor = anchor;
            UpdateCableVisual(clamped);
        }

        private void UpdateCableVisual(float length)
        {
            if (cableVisual == null) return;
            cableVisual.localPosition = new Vector3(0f, -length * 0.5f, 0f);
            cableVisual.localScale = new Vector3(0.035f, length * 0.5f, 0.035f);
        }
    }
}
