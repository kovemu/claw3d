using UnityEngine;

namespace Claw3D.Physics
{
    [CreateAssetMenu(menuName = "Claw3D/Claw Physics Config", fileName = "ClawPhysicsConfig")]
    public sealed class ClawPhysicsConfig : ScriptableObject
    {
        [Header("Trolley")]
        [Min(0.1f)] public float trolleySpeed = 3.2f;
        public Vector2 xLimits = new(-2.7f, 2.7f);
        public Vector2 zLimits = new(-1.9f, 1.9f);
        [Min(0.1f)] public float returnSpeed = 4.0f;
        public Vector3 homePosition = new(-2.35f, 5.35f, -1.55f);

        [Header("Hoist")]
        [Min(0.1f)] public float topCableLength = 1.45f;
        [Min(0.1f)] public float bottomCableLength = 4.15f;
        [Min(0.1f)] public float dropSpeed = 2.2f;
        [Min(0.1f)] public float liftSpeed = 2.6f;
        [Min(0f)] public float gripHoldSeconds = 0.65f;
        [Min(0f)] public float releaseHoldSeconds = 0.6f;

        [Header("Claw")]
        [Min(0.1f)] public float clawMass = 2.0f;
        [Min(0f)] public float swingDrag = 0.2f;
        [Min(0f)] public float angularDrag = 0.5f;

        [Header("Finger")]
        [Min(0.01f)] public float fingerMass = 0.3f;
        public float openAngle = 34f;
        public float closedAngle = -20f;
        [Min(1f)] public float fingerSpring = 180f;
        [Min(0f)] public float fingerDamper = 16f;
        [Min(1f)] public float fingerMaxForce = 1200f;
    }
}
