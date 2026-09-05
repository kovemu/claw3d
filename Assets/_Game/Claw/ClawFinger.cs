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
        [SerializeField] private Rigidbody body;
        [Range(0f, 1f), SerializeField] private float openAmount = 1f;
        [SerializeField] private float clawVelocity = 10f;
        [SerializeField] private ClawGripMaterial gripMaterial = ClawGripMaterial.MaxFriction;

        private static PhysicsMaterial maxFrictionMaterial;
        private static PhysicsMaterial highFrictionMaterial;
        private static PhysicsMaterial iceyMaterial;

        public float CurrentAngle => hinge == null ? 0f : hinge.angle;

        private void Awake()
        {
            if (hinge == null) hinge = GetComponent<HingeJoint>();
            if (body == null) body = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            DriveTowardTarget();
        }

        public void Configure(ClawPhysicsConfig physicsConfig)
        {
            config = physicsConfig;
            hinge = GetComponent<HingeJoint>();
            body = GetComponent<Rigidbody>();

            body.mass = config.fingerMass;
            body.linearDamping = config.fingerIdleLinearDamping;
            body.angularDamping = config.fingerIdleAngularDamping;
            body.solverIterations = config.solverIterations;
            body.solverVelocityIterations = config.solverVelocityIterations;
            body.maxAngularVelocity = 30f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;

            JointLimits limits = hinge.limits;
            limits.min = Mathf.Min(config.fingerOpenAngleDegrees, config.fingerClosedAngleDegrees);
            limits.max = Mathf.Max(config.fingerOpenAngleDegrees, config.fingerClosedAngleDegrees);
            limits.bounciness = 0f;
            limits.contactDistance = config.fingerLimitContactDistance;
            hinge.limits = limits;
            hinge.useLimits = true;
            hinge.useSpring = false;
            hinge.useMotor = false;
            hinge.enableCollision = false;
            hinge.enablePreprocessing = true;

            // Do not apply the grab profile's very high drag while the claw is idle/open.
            // The source grab damping belongs to the active grab profile; applying it here
            // was effectively freezing the three-arm articulated body before the drop began.
            clawVelocity = config.realisticNormalVelocity;
            SetGripMaterial(ClawGripMaterial.MaxFriction);
            SetOpenAmount(1f);
        }

        public void SetOpenAmount(float amount)
        {
            openAmount = Mathf.Clamp01(amount);

            // Open/idle motion uses the lightweight source Rigidbody damping. Grab damping is
            // restored only when a grab profile is explicitly applied at the bottom of the drop.
            if (openAmount >= 0.999f)
                RestoreIdleDamping();
        }

        public void ApplyGrabSettings(float angularVelocity, float linearDamping, float angularDamping, ClawGripMaterial material)
        {
            clawVelocity = Mathf.Max(0f, angularVelocity);

            if (body != null)
            {
                body.linearDamping = Mathf.Max(0f, linearDamping);
                body.angularDamping = Mathf.Max(0f, angularDamping);
            }

            SetGripMaterial(material);
        }

        public void RestoreIdleDamping()
        {
            if (body == null || config == null) return;
            body.linearDamping = config.fingerIdleLinearDamping;
            body.angularDamping = config.fingerIdleAngularDamping;
        }

        private void DriveTowardTarget()
        {
            if (hinge == null || body == null || config == null) return;

            float target = Mathf.Lerp(config.fingerClosedAngleDegrees, config.fingerOpenAngleDegrees, openAmount);
            float error = Mathf.DeltaAngle(hinge.angle, target);
            Vector3 axisWorld = transform.TransformDirection(hinge.axis).normalized;

            float currentAxial = Vector3.Dot(body.angularVelocity, axisWorld);

            if (Mathf.Abs(error) <= config.fingerAngleDeadZone)
            {
                body.angularVelocity -= axisWorld * currentAxial;
                return;
            }

            float desiredAxial = Mathf.Sign(error) * clawVelocity;

            // Only replace the component around the hinge axis. Preserving the other angular
            // components lets the whole claw swing naturally with the rope/head Rigidbody.
            body.angularVelocity += axisWorld * (desiredAxial - currentAxial);
        }

        private void SetGripMaterial(ClawGripMaterial material)
        {
            gripMaterial = material;
            PhysicsMaterial physicsMaterial = ResolveMaterial(material);
            foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            {
                if (collider.enabled)
                    collider.material = physicsMaterial;
            }
        }

        private PhysicsMaterial ResolveMaterial(ClawGripMaterial material)
        {
            if (config == null) return null;

            switch (material)
            {
                case ClawGripMaterial.HighFriction:
                    return highFrictionMaterial ??= CreateMaterial(
                        "highFriction Claw",
                        config.highFriction,
                        PhysicsMaterialCombine.Maximum,
                        PhysicsMaterialCombine.Average);

                case ClawGripMaterial.Icey:
                    return iceyMaterial ??= CreateMaterial(
                        "icey",
                        config.iceyFriction,
                        PhysicsMaterialCombine.Minimum,
                        PhysicsMaterialCombine.Maximum);

                case ClawGripMaterial.MaxFriction:
                    return maxFrictionMaterial ??= CreateMaterial(
                        "maxFriction",
                        config.maxFriction,
                        PhysicsMaterialCombine.Maximum,
                        PhysicsMaterialCombine.Average);

                default:
                    return null;
            }
        }

        private static PhysicsMaterial CreateMaterial(
            string name,
            float friction,
            PhysicsMaterialCombine frictionCombine,
            PhysicsMaterialCombine bounceCombine)
        {
            return new PhysicsMaterial(name)
            {
                dynamicFriction = friction,
                staticFriction = friction,
                bounciness = 0f,
                frictionCombine = frictionCombine,
                bounceCombine = bounceCombine
            };
        }
    }
}
