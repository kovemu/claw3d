using UnityEngine;

namespace Claw3D.Physics
{
    [CreateAssetMenu(menuName = "Claw3D/Claw Physics Config", fileName = "ClawPhysicsConfig")]
    public sealed class ClawPhysicsConfig : ScriptableObject
    {
        [Header("Cabinet (meters)")]
        public float cabinetHalfX = 0.42f;
        public float cabinetHalfZ = 0.42f;
        public float cabinetHeight = 1.05f;
        public float shellThickness = 0.04f;

        [Header("Trolley - horizontal")]
        [Tooltip("Maximum manual carriage speed.")]
        public float trolleySpeed = 0.50f;
        [Tooltip("Acceleration while pushing the stick in the current direction. Finite acceleration is important because it excites the hanging claw instead of teleporting it.")]
        public float trolleyAcceleration = 2.8f;
        [Tooltip("How quickly the carriage settles after the player releases the stick.")]
        public float trolleyDeceleration = 4.2f;
        [Tooltip("Extra acceleration available when the player rapidly reverses direction. This lets deliberate stick flicks build realistic claw swing.")]
        public float trolleyReverseAcceleration = 6.0f;
        public Vector2 xLimits = new(-0.26f, 0.26f);
        public Vector2 zLimits = new(-0.26f, 0.26f);

        [Header("Trolley - vertical")]
        public float topY = 1.00f;
        [Tooltip("Lowest trolley height during Drop. Current prototype tuning puts the open fingers into the prize pile.")]
        public float bottomY = 0.20f;
        public float railY = 0.96f;
        public float dropSpeed = 0.55f;
        public float liftSpeed = 0.50f;
        public float verticalAcceleration = 2.6f;
        public float verticalDeceleration = 3.8f;
        public float returnSpeed = 0.60f;
        public float returnAcceleration = 2.4f;
        public Vector2 homeXZ = new(-0.30f, 0.30f);

        [Header("Pendulum Rig")]
        public float cableLength = 0.24f;
        public float hubRadius = 0.05f;
        public float hubMass = 0.20f;
        [Tooltip("Lower than the old prototype so start/stop and stick flicks remain visibly physical instead of being damped away immediately.")]
        public float hubLinearDamping = 0.35f;
        public float hubAngularDamping = 0.85f;

        [Header("Finger Geometry")]
        public int fingerCount = 3;
        public float fingerMountRadius = 0.032f;
        public float fingerMountY = -0.03f;
        public Vector3 fingerSegmentLengths = new(0.060f, 0.055f, 0.045f);
        public Vector3 fingerSegmentCurvesRadians = new(0.12f, 0.50f, 0.95f);
        public Vector3 fingerSegmentRadii = new(0.010f, 0.0075f, 0.0055f);
        public float fingerMass = 0.035f;
        public float fingerAngularDamping = 4.0f;

        [Header("Finger Motor - limited torque")]
        [Tooltip("Open must rotate outward from the inward-curved rest shape. With this hinge axis that is the negative direction.")]
        public float openAngleDegrees = -48.7f;
        public float closedAngleDegrees = 0f;
        [Tooltip("Maximum hinge motor force/torque at full strength. Unlike a spring-only claw, an obstructed finger can now stall and be pried open by a prize.")]
        public float fingerMotorMaxForce = 0.38f;
        [Tooltip("Maximum commanded finger angular speed in degrees per second.")]
        public float fingerMotorMaxSpeed = 220f;
        [Tooltip("Position error to target-velocity gain for the limited-force hinge motor.")]
        public float fingerMotorVelocityGain = 7.0f;
        [Tooltip("Motor dead-zone in degrees. Prevents tiny jitter around the target angle.")]
        public float fingerMotorDeadZone = 0.45f;
        [Tooltip("Strength retained while lifting/returning. Realistic mode intentionally relaxes after the initial close so marginal catches can slip.")]
        public float carryStrengthFactor = 0.58f;

        [Header("Cycle")]
        public float gripSeconds = 0.55f;
        public float releaseSeconds = 0.45f;
        public float scoreSeconds = 0.90f;

        [Header("Top-stop Jolt")]
        public float joltAmplitude = 0.012f;
        public float joltFrequency = 18f;
        public float joltDecay = 0.12f;
        public float joltDuration = 0.35f;

        [Header("Toy Physics")]
        public float toyMinRadius = 0.055f;
        public float toyMaxRadius = 0.072f;
        public float toyMass = 0.055f;
        public float toyFriction = 2.0f;
        public float fingerFriction = 2.0f;
        public float toyLinearDamping = 0.9f;
        public float toyAngularDamping = 1.6f;

        [Header("Solver")]
        [Tooltip("Rapier reference uses multiple substeps. PhysX uses a 60 Hz fixed step plus elevated per-body solver iterations here.")]
        public int solverIterations = 12;
        public int solverVelocityIterations = 8;
        public float fixedTimestep = 1f / 60f;
    }
}
