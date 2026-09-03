using UnityEngine;

namespace Claw3D.Physics
{
    [CreateAssetMenu(menuName = "Claw3D/Claw Physics Config", fileName = "ClawPhysicsConfig")]
    public sealed class ClawPhysicsConfig : ScriptableObject
    {
        [Header("Trolley")]
        [Min(0.1f)] public float trolleySpeed = 3.5f;
        public Vector2 xLimits = new(-3.2f, 3.2f);
        public Vector2 zLimits = new(-2.2f, 2.2f);

        [Header("Claw")]
        [Min(0.1f)] public float clawMass = 2.0f;
        [Min(0.1f)] public float swingDrag = 0.15f;
        [Min(0.1f)] public float angularDrag = 0.35f;

        [Header("Finger")]
        [Min(0.01f)] public float fingerMass = 0.35f;
        public float openAngle = 32f;
        public float closedAngle = -18f;
        [Min(1f)] public float fingerSpring = 140f;
        [Min(0f)] public float fingerDamper = 12f;
        [Min(1f)] public float fingerMaxForce = 900f;
    }
}
