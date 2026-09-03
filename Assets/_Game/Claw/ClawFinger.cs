using Claw3D.Physics;
using UnityEngine;

namespace Claw3D.Claw
{
    [RequireComponent(typeof(HingeJoint))]
    public sealed class ClawFinger : MonoBehaviour
    {
        [SerializeField] private ClawPhysicsConfig config;
        [SerializeField] private bool invertAngle;
        private HingeJoint hinge;

        private void Awake()
        {
            hinge = GetComponent<HingeJoint>();
        }

        public void Configure(ClawPhysicsConfig physicsConfig, bool inverted)
        {
            config = physicsConfig;
            invertAngle = inverted;
            hinge = GetComponent<HingeJoint>();
            ApplyJointSettings();
        }

        public void SetClosed(bool closed)
        {
            if (hinge == null || config == null) return;
            float target = closed ? config.closedAngle : config.openAngle;
            if (invertAngle) target = -target;

            JointSpring spring = hinge.spring;
            spring.spring = config.fingerSpring;
            spring.damper = config.fingerDamper;
            spring.targetPosition = target;
            hinge.spring = spring;
            hinge.useSpring = true;
        }

        private void ApplyJointSettings()
        {
            if (hinge == null || config == null) return;
            hinge.useSpring = true;
            hinge.useLimits = false;
            SetClosed(false);
        }
    }
}
