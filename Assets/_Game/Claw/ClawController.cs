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

        private Vector2 horizontalVelocity;
        private float verticalVelocity;

        public Vector3 TrolleyPosition => trolleyBody == null ? transform.position : trolleyBody.position;
        public Vector3 HubPosition => hubBody == null ? transform.position : hubBody.position;
        public Vector2 HorizontalVelocity => horizontalVelocity;
        public float HubSwingSpeed => hubBody == null ? 0f : new Vector2(hubBody.linearVelocity.x, hubBody.linearVelocity.z).magnitude;

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
            horizontalVelocity = Vector2.zero;
            verticalVelocity = 0f;
            SetOpenAmount(1f);
            SetStrengthScale(1f);
        }

        public void MoveAim(Vector2 input)
        {
            if (config == null || trolleyBody == null) return;

            // Camera is at +Z looking inward. Keep screen-space controls intuitive.
            Vector2 desired = new Vector2(-input.x, -input.y) * config.trolleySpeed;

            float acceleration = input.sqrMagnitude < 0.0001f
                ? config.trolleyDeceleration
                : IsReversing(horizontalVelocity, desired)
                    ? config.trolleyReverseAcceleration
                    : config.trolleyAcceleration;

            horizontalVelocity = Vector2.MoveTowards(
                horizontalVelocity,
                desired,
                acceleration * Time.fixedDeltaTime);

            MoveHorizontalByVelocity();
        }

        public void BrakeHorizontal()
        {
            if (config == null || trolleyBody == null) return;
            horizontalVelocity = Vector2.MoveTowards(
                horizontalVelocity,
                Vector2.zero,
                config.trolleyDeceleration * Time.fixedDeltaTime);
            MoveHorizontalByVelocity();
        }

        public bool MoveVerticalToward(float targetY, float maxSpeed)
        {
            if (config == null || trolleyBody == null) return true;

            Vector3 p = trolleyBody.position;
            float delta = targetY - p.y;
            float distance = Mathf.Abs(delta);

            if (distance < 0.0005f)
            {
                p.y = targetY;
                verticalVelocity = 0f;
                trolleyBody.MovePosition(p);
                return true;
            }

            float direction = Mathf.Sign(delta);
            float stoppingSpeed = Mathf.Sqrt(2f * Mathf.Max(0.01f, config.verticalDeceleration) * distance);
            float desiredSpeed = direction * Mathf.Min(maxSpeed, stoppingSpeed);

            verticalVelocity = Mathf.MoveTowards(
                verticalVelocity,
                desiredSpeed,
                config.verticalAcceleration * Time.fixedDeltaTime);

            float nextY = p.y + verticalVelocity * Time.fixedDeltaTime;
            if ((direction > 0f && nextY >= targetY) || (direction < 0f && nextY <= targetY))
            {
                nextY = targetY;
                verticalVelocity = 0f;
            }

            p.y = nextY;
            trolleyBody.MovePosition(p);
            return Mathf.Abs(nextY - targetY) < 0.0005f;
        }

        public bool ReturnHome()
        {
            if (config == null || trolleyBody == null) return true;

            Vector3 p = trolleyBody.position;
            Vector2 current = new(p.x, p.z);
            Vector2 toHome = config.homeXZ - current;
            float distance = toHome.magnitude;

            if (distance < 0.001f)
            {
                p.x = config.homeXZ.x;
                p.z = config.homeXZ.y;
                horizontalVelocity = Vector2.zero;
                trolleyBody.MovePosition(p);
                return true;
            }

            Vector2 direction = toHome / distance;
            float stoppingSpeed = Mathf.Sqrt(2f * Mathf.Max(0.01f, config.trolleyDeceleration) * distance);
            float desiredSpeed = Mathf.Min(config.returnSpeed, stoppingSpeed);
            Vector2 desiredVelocity = direction * desiredSpeed;

            horizontalVelocity = Vector2.MoveTowards(
                horizontalVelocity,
                desiredVelocity,
                config.returnAcceleration * Time.fixedDeltaTime);

            MoveHorizontalByVelocity();
            return Vector2.Distance(new Vector2(trolleyBody.position.x, trolleyBody.position.z), config.homeXZ) < 0.001f;
        }

        public void ApplyTopStopJolt(float phaseTime)
        {
            if (config == null || trolleyBody == null) return;

            verticalVelocity = 0f;
            float offset = config.joltAmplitude
                * Mathf.Exp(-phaseTime / Mathf.Max(0.001f, config.joltDecay))
                * Mathf.Sin(2f * Mathf.PI * config.joltFrequency * phaseTime);

            Vector3 p = trolleyBody.position;
            p.y = config.topY + offset;
            trolleyBody.MovePosition(p);
        }

        public void StopAllMotion()
        {
            horizontalVelocity = Vector2.zero;
            verticalVelocity = 0f;
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

        private void MoveHorizontalByVelocity()
        {
            Vector3 p = trolleyBody.position;
            p.x += horizontalVelocity.x * Time.fixedDeltaTime;
            p.z += horizontalVelocity.y * Time.fixedDeltaTime;

            float clampedX = Mathf.Clamp(p.x, config.xLimits.x, config.xLimits.y);
            float clampedZ = Mathf.Clamp(p.z, config.zLimits.x, config.zLimits.y);

            if (!Mathf.Approximately(clampedX, p.x)) horizontalVelocity.x = 0f;
            if (!Mathf.Approximately(clampedZ, p.z)) horizontalVelocity.y = 0f;

            p.x = clampedX;
            p.z = clampedZ;
            trolleyBody.MovePosition(p);
        }

        private static bool IsReversing(Vector2 current, Vector2 desired)
        {
            if (current.sqrMagnitude < 0.0025f || desired.sqrMagnitude < 0.0025f) return false;
            return Vector2.Dot(current.normalized, desired.normalized) < -0.15f;
        }
    }
}
