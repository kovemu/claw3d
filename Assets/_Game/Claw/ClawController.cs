using Claw3D.Physics;
using UnityEngine;

namespace Claw3D.Claw
{
    public sealed class ClawController : MonoBehaviour
    {
        [SerializeField] private ClawPhysicsConfig config;
        [SerializeField] private Rigidbody trolleyBody;
        [SerializeField] private Rigidbody hubBody;
        [SerializeField] private ClawFinger[] fingers;

        public Vector3 TrolleyPosition => trolleyBody == null ? transform.position : trolleyBody.position;
        public Vector3 HubPosition => hubBody == null ? transform.position : hubBody.position;

        public void Configure(
            ClawPhysicsConfig physicsConfig,
            Rigidbody trolley,
            Rigidbody hub,
            ClawFinger[] clawFingers)
        {
            config = physicsConfig;
            trolleyBody = trolley;
            hubBody = hub;
            fingers = clawFingers;
            SetOpenAmount(1f);
            SetStrengthScale(1f);
        }

        public void MoveAim(Vector2 input)
        {
            if (config == null || trolleyBody == null) return;

            // Camera is fixed at +Z looking toward the cabinet center.
            // Map input to screen-space movement: left/right and up/down should
            // follow what the player sees, not raw world-axis directions.
            Vector3 p = trolleyBody.position;
            p.x = Mathf.Clamp(p.x - input.x * config.trolleySpeed * Time.fixedDeltaTime, config.xLimits.x, config.xLimits.y);
            p.z = Mathf.Clamp(p.z - input.y * config.trolleySpeed * Time.fixedDeltaTime, config.zLimits.x, config.zLimits.y);
            trolleyBody.MovePosition(p);
        }

        public bool MoveVerticalToward(float targetY, float speed)
        {
            if (config == null || trolleyBody == null) return true;
            Vector3 p = trolleyBody.position;
            p.y = Mathf.MoveTowards(p.y, targetY, speed * Time.fixedDeltaTime);
            trolleyBody.MovePosition(p);
            return Mathf.Abs(p.y - targetY) < 0.001f;
        }

        public bool ReturnHome()
        {
            if (config == null || trolleyBody == null) return true;

            Vector3 p = trolleyBody.position;
            Vector2 current = new(p.x, p.z);
            Vector2 next = Vector2.MoveTowards(current, config.homeXZ, config.returnSpeed * Time.fixedDeltaTime);
            p.x = next.x;
            p.z = next.y;
            trolleyBody.MovePosition(p);
            return Vector2.SqrMagnitude(next - config.homeXZ) < 0.000001f;
        }

        public void ApplyTopStopJolt(float phaseTime)
        {
            if (config == null || trolleyBody == null) return;
            float offset = config.joltAmplitude
                * Mathf.Exp(-phaseTime / Mathf.Max(0.001f, config.joltDecay))
                * Mathf.Sin(2f * Mathf.PI * config.joltFrequency * phaseTime);
            Vector3 p = trolleyBody.position;
            p.y = config.topY + offset;
            trolleyBody.MovePosition(p);
        }

        public void SetOpenAmount(float open)
        {
            if (fingers == null) return;
            foreach (ClawFinger finger in fingers)
                if (finger != null) finger.SetOpenAmount(open);
        }

        public void SetStrengthScale(float scale)
        {
            if (fingers == null) return;
            foreach (ClawFinger finger in fingers)
                if (finger != null) finger.SetStrengthScale(scale);
        }
    }
}
