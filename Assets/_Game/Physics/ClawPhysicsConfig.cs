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

        [Header("Rope length - prototype mapped")]
        [Tooltip("Our metre-scale cabinet's starting rope length. The target asset uses its own scaled hierarchy, so this remains a geometry mapping value rather than a copied local-unit value.")]
        public float cableLength = 0.24f;
        [Tooltip("Current prototype reel step. Keep editable until the target ClawRope MonoBehaviour fields are fully mapped.")]
        public float loweringStepPerFixedUpdate = 0.004f;
        [Tooltip("Current prototype reel travel. Keep editable until the target ClawRope MonoBehaviour fields are fully mapped.")]
        public float loweringDistance = 0.55f;

        [Header("Obi-style rope solver - extracted target structure")]
        [Tooltip("Target ObiFixedUpdater performs four substeps per Unity FixedUpdate.")]
        public int ropeSubsteps = 4;
        [Tooltip("Target active rope contains five simulated particles (four structural elements).")]
        public int ropeActiveParticles = 5;
        [Tooltip("Target active blueprint particle inverse mass is 10, therefore each rope particle mass is 0.1 kg.")]
        public float ropeParticleMass = 0.10f;
        public float ropeStretchCompliance = 0f;
        public float ropeBendCompliance = 0.10f;
        public float ropeMaxBending = 0.013f;
        public bool ropeSelfCollisions = true;
        [Tooltip("Target ObiRopeCursor serialized cursor coordinate.")]
        public float ropeCursorMu = 0.531f;
        [Tooltip("Target ObiRopeCursor serialized source coordinate.")]
        public float ropeSourceMu = 0.741f;
        [Tooltip("Both target ObiParticleAttachment components are dynamic with zero compliance.")]
        public float ropeAttachmentCompliance = 0f;
        [Tooltip("Velocity correction applied after the custom PBD rope resolves its dynamic bottom attachment.")]
        [Range(0f, 1f)] public float ropeBodyVelocityCoupling = 0.55f;

        [Header("Claw head - extracted Rigidbody")]
        public float hubRadius = 0.05f;
        [Tooltip("Active target ClawMain.002 Rigidbody mass.")]
        public float hubMass = 1.0f;
        public float hubLinearDamping = 0f;
        public float hubAngularDamping = 0.05f;

        [Header("Finger body / joint - extracted")]
        public int fingerCount = 3;
        public float fingerMountRadius = 0.045f;
        public float fingerMountY = -0.04f;
        [Tooltip("Each active target claw-finger Rigidbody mass.")]
        public float fingerMass = 1.0f;
        public float fingerIdleLinearDamping = 0f;
        public float fingerIdleAngularDamping = 0.05f;
        [Tooltip("Reference HingeJoint limits are 0..45 degrees. The joint has no motor and no spring.")]
        public float fingerClosedAngleDegrees = 0f;
        public float fingerOpenAngleDegrees = 45f;
        [Tooltip("Target HingeJoint limit contact distance.")]
        public float fingerLimitContactDistance = 0.20f;
        public float fingerAngleDeadZone = 0.6f;

        [Header("Claw settings - extracted values")]
        public ClawDifficultyMode difficultyMode = ClawDifficultyMode.Realistic;
        [Tooltip("ClawSettings.drag used by the target grab profiles.")]
        public float grabLinearDamping = 10f;
        [Tooltip("ClawSettings.angularDrag used by the target grab profiles.")]
        public float grabAngularDamping = 30f;

        [Header("Normal difficulty")]
        public float normalClawVelocity = 11f;
        public float normalStrongClawVelocity = 12f;
        public int normalFailedTriesForStrong = 3;

        [Header("Realistic difficulty - weighted profiles")]
        [Tooltip("Weights extracted from RealisticGrabSetting: normal=2, strong=1, dead=4, dying=5.")]
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

        [Header("Claw friction materials - extracted values")]
        public float maxFriction = 10f;
        public float highFriction = 0.75f;
        public float iceyFriction = 0.30f;

        [Header("Cycle - extracted active values")]
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
        [Tooltip("Only retained so the old scene builder compiles while its generated claw is migrated to the reference rig.")]
        public float topY = 1.00f;
        public float fingerFriction = 10f;
        public Vector3 fingerSegmentLengths = new(0.060f, 0.055f, 0.045f);
        public Vector3 fingerSegmentCurvesRadians = new(0.12f, 0.50f, 0.95f);
        public Vector3 fingerSegmentRadii = new(0.010f, 0.0075f, 0.0055f);
        public float fingerAngularDamping = 0.05f;

        [Header("Unity / PhysX")]
        public int solverIterations = 12;
        public int solverVelocityIterations = 8;
        [Tooltip("Target project's TimeManager fixedDeltaTime is 0.02 seconds (50 Hz). Obi then substeps this four times.")]
        public float fixedTimestep = 0.02f;
    }
}
