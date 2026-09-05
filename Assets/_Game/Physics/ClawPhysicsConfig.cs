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
        [Tooltip("Reference ClawMoveModule speed. This is a position step per FixedUpdate, not metres/second.")]
        public float trolleyStepPerFixedUpdate = 0.007f;
        public bool returnAxisAtATime = true;
        public Vector2 xLimits = new(-0.26f, 0.26f);
        public Vector2 zLimits = new(-0.26f, 0.26f);
        public float railY = 0.96f;
        public Vector2 homeXZ = new(-0.30f, 0.30f);

        [Header("Rope - Claw Machine Sim reference")]
        [Tooltip("Prototype starting rope length. The commercial scene's lowering distance is reproduced separately below.")]
        public float cableLength = 0.24f;
        [Tooltip("Reference active ClawRope loweringSpeed: rest length changes by 0.004 per physics update.")]
        public float loweringStepPerFixedUpdate = 0.004f;
        [Tooltip("Reference active ClawRope loweringDistance.")]
        public float loweringDistance = 0.55f;
        [Tooltip("Small radial velocity damping used by our massless-rope constraint when it becomes taut.")]
        [Range(0f, 1f)] public float ropeRadialDamping = 0.98f;

        [Header("Claw head")]
        public float hubRadius = 0.05f;
        [Tooltip("Reference central claw Rigidbody mass.")]
        public float hubMass = 0.25f;
        public float hubLinearDamping = 0f;
        [Tooltip("Reference central claw Rigidbody angular drag/damping.")]
        public float hubAngularDamping = 0.05f;

        [Header("Finger body / joint")]
        public int fingerCount = 3;
        public float fingerMountRadius = 0.045f;
        public float fingerMountY = -0.04f;
        [Tooltip("Reference finger Rigidbody mass.")]
        public float fingerMass = 0.25f;
        public float fingerIdleLinearDamping = 0f;
        public float fingerIdleAngularDamping = 0.05f;
        [Tooltip("Reference HingeJoint limits are 0..45 degrees. The joint has no motor and no spring.")]
        public float fingerClosedAngleDegrees = 0f;
        public float fingerOpenAngleDegrees = 45f;
        public float fingerAngleDeadZone = 0.6f;

        [Header("Claw settings - exact extracted values")]
        public ClawDifficultyMode difficultyMode = ClawDifficultyMode.Realistic;
        [Tooltip("ClawSettings.drag used by the target game grab profiles.")]
        public float grabLinearDamping = 10f;
        [Tooltip("ClawSettings.angularDrag used by the target game grab profiles.")]
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

        [Header("Claw friction materials - exact extracted values")]
        public float maxFriction = 10f;
        public float highFriction = 0.75f;
        public float iceyFriction = 0.30f;

        [Header("Cycle - active scene values")]
        [Tooltip("Reference ClawModule timeToClose.")]
        public float timeToClose = 0.50f;
        [Tooltip("Reference ClawMoveModule delayToOpen after returning home.")]
        public float delayToOpen = 0.40f;
        [Tooltip("Reference ClawModule timeToOpen.")]
        public float timeToOpen = 1.50f;
        public float scoreSeconds = 0.90f;

        [Header("Toy Physics (prototype, not cloned yet)")]
        public float toyMinRadius = 0.055f;
        public float toyMaxRadius = 0.072f;
        public float toyMass = 0.055f;
        public float toyFriction = 2.0f;
        public float toyLinearDamping = 0.9f;
        public float toyAngularDamping = 1.6f;

        [Header("Solver")]
        public int solverIterations = 12;
        public int solverVelocityIterations = 8;
        public float fixedTimestep = 1f / 60f;
    }
}
