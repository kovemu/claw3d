using Claw3D.Physics;
using UnityEngine;

namespace Claw3D.Claw
{
    [RequireComponent(typeof(HingeJoint))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ClawFinger : MonoBehaviour
    {
        [SerializeField] private ClawPhysicsConfig config;
        [SerializeField] private HingeJoint hinge;
        [Range(0f, 1f), SerializeField] private float openAmount = 1f;
        [Range(0.05f, 1f), SerializeField] private float strengthScale = 1f;

        private void Awake()
        {
            if (hinge == null) hinge = GetComponent<HingeJoint>();
        }

        public void Configure(ClawPhysicsConfig physicsConfig)
        {
            config = physicsConfig;
            hinge = GetComponent<HingeJoint>();

            Rigidbody body = GetComponent<Rigidbody>();
            body.mass = config.fingerMass;
            body.angularDamping = config.fingerAngularDamping;
            body.solverIterations = config.solverIterations;
            body.solverVelocityIterations = config.solverVelocityIterations;
            body.maxAngularVelocity = 20f;

            float low = Mathf.Min(config.closedAngleDegrees, config.openAngleDegrees) - 8f;
            float high = Mathf.Max(config.closedAngleDegrees, config.openAngleDegrees) + 8f;

            JointLimits limits = hinge.limits;
            limits.min = low;
            limits.max = high;
            limits.bounciness = 0f;
            limits.contactDistance = 1f;
            hinge.limits = limits;
            hinge.useLimits = true;

            ApplyMotor();
        }

        public void SetOpenAmount(float amount)
        {
            openAmount = Mathf.Clamp01(amount);
            ApplyMotor();
        }

        public void SetStrengthScale(float scale)
        {
            strengthScale = Mathf.Clamp(scale, 0.05f, 1f);
            ApplyMotor();
        }

        private void ApplyMotor()
        {
            if (hinge == null || config == null) return;

            float target = Mathf.Lerp(config.closedAngleDegrees, config.openAngleDegrees, openAmount);
            JointSpring spring = hinge.spring;
            spring.spring = config.fingerSpring * strengthScale;
            spring.damper = config.fingerDamper;
            spring.targetPosition = target;
            hinge.spring = spring;
            hinge.useSpring = true;
        }
    }
}
