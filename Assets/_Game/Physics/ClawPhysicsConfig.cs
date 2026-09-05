using UnityEngine;

namespace Claw3D.Physics
{
    public enum ClawDifficultyMode
    {
        Normal,
        Realistic
    }

    public enum ClawGrabType
    {
        None = 0,
        Dead = 1,
        Dying = 2,
        Normal = 3,
        Strong = 4
    }

    public enum ClawGripMaterial
    {
        Default,
        HighFriction,
        Icey,
        MaxFriction
    }

    [CreateAssetMenu(menuName = "Claw3D/Claw Physics Config", fileName = "ClawPhysicsConfig")]
    public sealed class ClawPhysicsConfig : ScriptableObject
    {
        [Header("Cabinet (prototype scale)")]
        public float cabinetHalfX = 0.42f;
        public float cabinetHalfZ = 0.42f;
        public float cabinetHeight = 1.05f;
        public float shellThickness = 0.04f;

        [Header("Carriage - Claw Machine Sim reference")]
        [Tooltip("Reference ClawMoveModule position step per FixedUpdate.")]
        public float trolleyStepPerFixedUpdate = 0.007f;
        public bool returnAxisAtATime = true;
        public Vector2 xLimits = new(-0.26f, 0.26f);
        public Vector2 zLimits = new(-0.26f, 0.26f);
        public float railY = 0.96f;
        public Vector2 homeXZ = new(-0.30f, 0.30f);

        [Header("Rope length - extracted active ClawRope")]
        [Tooltip("Our metre-scale scene mapping. The source hierarchy uses a separately scaled Obi solver space.")]
        public float cableLength = 0.24f;
        [Tooltip("Active ClawRope loweringSpeed serialized value.")]
        public float loweringStepPerFixedUpdate = 0.004f;
        [Tooltip("Active ClawRope loweringDistance serialized value.")]
        public float loweringDistance = 0.55f;

        [Header("Obi rope - verified active asset data")]
        [Tooltip("Active ObiFixedUpdater uses 4 substeps per Unity FixedUpdate.")]
        public int ropeSubsteps = 4;
        [Tooltip("Claw Rope Small blueprint initialActiveParticleCount / activeParticleCount is 3. The blueprint reserves a much larger particle pool for the cursor to extend the rope.")]
        public int ropeActiveParticles = 3;
        [Tooltip("Claw Rope Small serialized positions/restPositions arrays reserve 103 particles. This is capacity, not the initial active count.")]
        public int ropeParticlePoolCapacity = 103;
        [Tooltip("Prototype approximation only. Do not treat this value as source-verified until the blueprint inverse-mass array is fully mapped.")]
        public float ropeParticleMass = 0.10f;
        public float ropeStretchCompliance = 0f;
        public float ropeBendCompliance = 0.10f;
        public float ropeMaxBending = 0.013f;
        public bool ropeSelfCollisions = true;
        public float ropeCursorMu = 0.531f;
        public float ropeSourceMu = 0.741f;
        public float ropeAttachmentCompliance = 0f;
        [Range(0f, 1f)] public float ropeBodyVelocityCoupling = 0.55f;

        [Header("Claw head - verified active Rigidbody")]
        public float hubRadius = 0.05f;
        [Tooltip("Active ClawPhysics/Obi Solver/ClawMain.002 Rigidbody mass from level2.")]
        public float hubMass = 0.25f;
        public float hubLinearDamping = 0f;
        public float hubAngularDamping = 0.05f;

        [Header("Finger body / joint - verified active rig")]
        public int fingerCount = 3;
        public float fingerMountRadius = 0.045f;
        public float fingerMountY = -0.04f;
        [Tooltip("Each active single claw - pivot fixed Rigidbody mass from level2.")]
        public float fingerMass = 0.25f;
        public float fingerIdleLinearDamping = 0f;
        public float fingerIdleAngularDamping = 0.05f;
        [Tooltip("Active HingeJoint limits are 0..45 degrees. Spring and motor are disabled.")]
        public float fingerClosedAngleDegrees = 0f;
        public float fingerOpenAngleDegrees = 45f;
        [Tooltip("Active HingeJoint contactDistance is 0. The nearby serialized 0.2 value is bounceMinVelocity, not contactDistance.")]
        public float fingerLimitContactDistance = 0f;
        public float fingerAngleDeadZone = 0.6f;

        [Header("Claw settings - extracted values")]
        public ClawDifficultyMode difficultyMode = ClawDifficultyMode.Realistic;
        public float grabLinearDamping = 10f;
        public float grabAngularDamping = 30f;

        [Header("Normal difficulty")]
        public float normalClawVelocity = 11f;
        public float normalStrongClawVelocity = 12f;
        public int normalFailedTriesForStrong = 3;

        [Header("Realistic difficulty - weighted profiles")]
        public int realisticNormalWeight = 2;
        public int realisticStrongWeight = 1;
        public int realisticDeadWeight = 4;
        public int realisticDyingWeight = 5;
        public float realisticNormalVelocity = 10f;
        public float realisticStrongVelocity = 10f;
        public float realisticDeadVelocity = 5f;
        public float realisticDyingInitialVelocity = 15f;
        public float realisticDyingDelayedVelocity = 7f;
        public float realisticDyingDelaySeconds = 7f;

        [Header("Claw friction materials - verified assets")]
        public float maxFriction = 10f;
        public float highFriction = 0.75f;
        public float iceyFriction = 0.30f;

        [Header("Cycle - verified active values")]
        public float timeToClose = 0.50f;
        public float delayToOpen = 0.40f;
        public float timeToOpen = 1.50f;
        public float scoreSeconds = 0.90f;

        [Header("Toy Physics (prototype, not cloned yet)")]
        public float toyMinRadius = 0.055f;
        public float toyMaxRadius = 0.072f;
        public float toyMass = 0.055f;
        public float toyFriction = 2.0f;
        public float toyLinearDamping = 0.9f;
        public float toyAngularDamping = 1.6f;

        [Header("Legacy builder geometry - temporary")]
        public float topY = 1.00f;
        public float fingerFriction = 10f;
        public Vector3 fingerSegmentLengths = new(0.060f, 0.055f, 0.045f);
        public Vector3 fingerSegmentCurvesRadians = new(0.12f, 0.50f, 0.95f);
        public Vector3 fingerSegmentRadii = new(0.010f, 0.0075f, 0.0055f);
        public float fingerAngularDamping = 0.05f;

        [Header("Unity / PhysX")]
        public int solverIterations = 12;
        public int solverVelocityIterations = 8;
        [Tooltip("Target project's TimeManager fixedDeltaTime is 0.02 seconds (50 Hz).")]
        public float fixedTimestep = 0.02f;
    }
}
