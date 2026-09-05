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

            // The target game uses the hinge only as a physical 0..45 degree stop.
            // There is no HingeJoint spring or motor; the Rigidbody itself is driven.
            JointLimits limits = hinge.limits;
            limits.min = config.fingerClosedAngleDegrees;
            limits.max = config.fingerOpenAngleDegrees;
            limits.bounciness = 0f;
            limits.contactDistance = 0f;
            hinge.limits = limits;
            hinge.useLimits = true;
            hinge.useSpring = false;
            hinge.useMotor = false;
            hinge.enableCollision = false;
            hinge.enablePreprocessing = true;

            ApplyGrabSettings(config.realisticNormalVelocity, config.grabLinearDamping, config.grabAngularDamping, ClawGripMaterial.MaxFriction);
            SetOpenAmount(1f);
        }

        public void SetOpenAmount(float amount)
        {
            openAmount = Mathf.Clamp01(amount);
        }

        public void ApplyGrabSettings(float angularVelocity, float linearDamping, float angularDamping, ClawGripMaterial material)
        {
            clawVelocity = Mathf.Max(0f, angularVelocity);
            gripMaterial = material;

            if (body != null)
            {
                body.linearDamping = Mathf.Max(0f, linearDamping);
                body.angularDamping = Mathf.Max(0f, angularDamping);
            }

            PhysicsMaterial physicsMaterial = ResolveMaterial(material);
            foreach (Collider collider in GetComponentsInChildren<Collider>(true))
                collider.material = physicsMaterial;
        }

        private void DriveTowardTarget()
        {
            if (hinge == null || body == null || config == null) return;

            float target = Mathf.Lerp(config.fingerClosedAngleDegrees, config.fingerOpenAngleDegrees, openAmount);
            float error = target - hinge.angle;
            Vector3 axisWorld = transform.TransformDirection(hinge.axis).normalized;

            if (Mathf.Abs(error) <= config.fingerAngleDeadZone)
            {
                // Remove only velocity around the hinge axis so contact impulses are free
                // to affect the rest of the physical assembly.
                float axial = Vector3.Dot(body.angularVelocity, axisWorld);
                body.angularVelocity -= axisWorld * axial;
                return;
            }

            // This mirrors the important behavior of the reference: the finger Rigidbody
            // receives an angular velocity command while the HingeJoint supplies only limits.
            body.angularVelocity = axisWorld * (Mathf.Sign(error) * clawVelocity);
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
