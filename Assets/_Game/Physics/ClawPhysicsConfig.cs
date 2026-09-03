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

        [Header("Trolley")]
        public float trolleySpeed = 0.50f;
        public Vector2 xLimits = new(-0.26f, 0.26f);
        public Vector2 zLimits = new(-0.26f, 0.26f);
        public float topY = 1.00f;
        public float bottomY = 0.42f;
        public float railY = 0.96f;
        public float dropSpeed = 0.55f;
        public float liftSpeed = 0.50f;
        public float returnSpeed = 0.60f;
        public Vector2 homeXZ = new(-0.30f, 0.30f);

        [Header("Pendulum Rig")]
        public float cableLength = 0.24f;
        public float hubRadius = 0.05f;
        public float hubMass = 0.20f;
        public float hubLinearDamping = 0.80f;
        public float hubAngularDamping = 1.75f;

        [Header("Finger Geometry")]
        public int fingerCount = 3;
        public float fingerMountRadius = 0.032f;
        public float fingerMountY = -0.03f;
        public Vector3 fingerSegmentLengths = new(0.060f, 0.055f, 0.045f);
        public Vector3 fingerSegmentCurvesRadians = new(0.12f, 0.50f, 0.95f);
        public Vector3 fingerSegmentRadii = new(0.010f, 0.0075f, 0.0055f);
        public float fingerMass = 0.035f;
        public float fingerAngularDamping = 4.0f;

        [Header("Finger Motor")]
        [Tooltip("Open must rotate outward from the inward-curved rest shape. With this hinge axis that is the negative direction.")]
        public float openAngleDegrees = -48.7f;
        public float closedAngleDegrees = 0f;
        [Tooltip("PhysX spring units differ from Rapier; this is a Unity starting point.")]
        public float fingerSpring = 9.0f;
        public float fingerDamper = 0.45f;
        public float carryStrengthFactor = 0.75f;

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
        [Tooltip("Rapier reference uses 8 substeps. PhysX uses per-body solver iterations instead.")]
        public int solverIterations = 12;
        public int solverVelocityIterations = 8;
        public float fixedTimestep = 1f / 60f;
    }
}
