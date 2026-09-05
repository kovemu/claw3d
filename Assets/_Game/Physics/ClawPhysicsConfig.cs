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

        [Header("Carriage - verified active ClawMoveModule")]
        [Tooltip("Active ClawMoveModule position step per FixedUpdate.")]
        public float trolleyStepPerFixedUpdate = 0.007f;
        public bool returnAxisAtATime = true;
        public Vector2 xLimits = new(-0.26f, 0.26f);
        public Vector2 zLimits = new(-0.26f, 0.26f);
        public float railY = 0.96f;
        public Vector2 homeXZ = new(-0.30f, 0.30f);

        [Header("Rope cycle - verified active ClawRope")]
        [Tooltip("Prototype-only geometric mapping from the old rig. Do not confuse this with the source Obi rope initial rest length below.")]
        public float cableLength = 0.24f;
        [Tooltip("Active ClawRope loweringSpeed. The game adds/subtracts this from Obi rope rest length during the cycle.")]
        public float loweringStepPerFixedUpdate = 0.004f;
        [Tooltip("Active ClawRope loweringDistance.")]
        public float loweringDistance = 0.55f;

        [Header("Obi rope - verified active Claw Rope Small blueprint")]
        [Tooltip("Active ObiFixedUpdater uses exactly four substeps per Unity FixedUpdate.")]
        public int ropeSubsteps = 4;
        [Tooltip("Initial active particles in Claw Rope Small.")]
        public int ropeActiveParticles = 3;
        [Tooltip("Total serialized pool: 3 initially active + 100 pooled particles.")]
        public int ropeParticlePoolCapacity = 103;
        [Tooltip("Blueprint inverse mass is 10, so each rope particle has mass 0.1 kg.")]
        public float ropeParticleMass = 0.10f;
        [Tooltip("Blueprint inter-particle distance used by ObiRopeCursor when adding pooled particles.")]
        public float ropeInterParticleDistance = 0.021475287f;
        [Tooltip("Initial active actor rest length: 0.012735528 + 0.014401228.")]
        public float ropeInitialRestLength = 0.027136756f;
        public Vector2 ropeInitialElementRestLengths = new(0.012735528f, 0.014401228f);
        [Tooltip("Distance constraints are enabled with zero stretch compliance.")]
        public float ropeStretchCompliance = 0f;
        [Tooltip("Bend constraints are DISABLED on the active ObiRope actor. Kept as zero so the prototype cannot accidentally add fake bend stiffness.")]
        public float ropeBendCompliance = 0f;
        [Tooltip("Serialized maxBending is 0.275, but bend constraints are disabled on the active actor.")]
        public float ropeMaxBending = 0.275f;
        public bool ropeSelfCollisions = false;
        [Tooltip("Active ObiRopeCursor m_CursorMu.")]
        public float ropeCursorMu = 0f;
        [Tooltip("Active ObiRopeCursor m_SourceMu.")]
        public float ropeSourceMu = 0f;
        public bool ropeCursorDirection = true;
        [Tooltip("Particle group 'start' attaches particle 2 to MOVER; group 'end' attaches particle 0 to the claw head.")]
        public int ropeStartParticleIndex = 2;
        public int ropeEndParticleIndex = 0;
        [Tooltip("Both active ObiParticleAttachment components are Dynamic with zero compliance and infinite break threshold.")]
        public float ropeAttachmentCompliance = 0f;
        [Tooltip("Active ObiSolver: distance constraint group is Sequential, 1 iteration, SOR 1.")]
        public int ropeDistanceIterations = 1;
        [Tooltip("Active ObiSolver: pin constraint group is Parallel, 1 iteration, SOR 1.")]
        public int ropePinIterations = 1;
        [Tooltip("Active rope ObiCollisionMaterial 'HighFriction' dynamic friction.")]
        public float ropeCollisionDynamicFriction = 1f;
        [Tooltip("Active rope ObiCollisionMaterial 'HighFriction' static friction.")]
        public float ropeCollisionStaticFriction = 0f;

        [Header("Reference rope attachment mapping")]
        [Tooltip("Verified source-space offset from the active MOVER origin to the rope start attachment. Applied without Transform scale because the prototype trolley primitive is scaled for rendering.")]
        public Vector3 ropeTopAttachmentOffset = new(-0.00910f, -0.10477f, -0.00210f);
        [Tooltip("The source end attachment is bound to the ClawMain transform. Exact source offset is not yet independently extracted, so the learning rig uses the transform origin instead of inventing a value.")]
        public Vector3 ropeHeadAttachmentOffset = Vector3.zero;

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
