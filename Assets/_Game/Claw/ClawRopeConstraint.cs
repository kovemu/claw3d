using Claw3D.Physics;
using UnityEngine;

namespace Claw3D.Claw
{
    /// <summary>
    /// Lightweight variable-length rope for the prototype. The reference game changes
    /// ObiRope rest length; this reproduces the same gameplay contract without depending
    /// on the proprietary Obi package: the rope may go slack, but it cannot stretch past
    /// its current rest length.
    /// </summary>
    public sealed class ClawRopeConstraint : MonoBehaviour
    {
        [SerializeField] private ClawPhysicsConfig config;
        [SerializeField] private Rigidbody carriage;
        [SerializeField] private Rigidbody clawHead;
        [SerializeField] private float initialLength;
        [SerializeField] private float currentLength;

        private float previousLength;

        public float InitialLength => initialLength;
        public float CurrentLength => currentLength;
        public float MaximumDropLength => initialLength + (config == null ? 0f : config.loweringDistance);

        public void Configure(ClawPhysicsConfig physicsConfig, Rigidbody carriageBody, Rigidbody headBody)
        {
            config = physicsConfig;
            carriage = carriageBody;
            clawHead = headBody;
            initialLength = Mathf.Max(0.01f, config.cableLength);
            currentLength = initialLength;
            previousLength = currentLength;
        }

        public void ResetLength()
        {
            currentLength = initialLength;
            previousLength = currentLength;
        }

        public bool ExtendOneStep()
        {
            if (config == null) return true;
            float target = initialLength + config.loweringDistance;
            previousLength = currentLength;
            currentLength = Mathf.MoveTowards(currentLength, target, config.loweringStepPerFixedUpdate);
            return Mathf.Abs(currentLength - target) < 0.00001f;
        }

        public bool RetractOneStep()
        {
            if (config == null) return true;
            previousLength = currentLength;
            currentLength = Mathf.MoveTowards(currentLength, initialLength, config.loweringStepPerFixedUpdate);
            return Mathf.Abs(currentLength - initialLength) < 0.00001f;
        }

        private void FixedUpdate()
        {
            EnforceConstraint();
        }

        private void EnforceConstraint()
        {
            if (carriage == null || clawHead == null || currentLength <= 0f) return;

            Vector3 anchor = carriage.position;
            Vector3 delta = clawHead.position - anchor;
            float distance = delta.magnitude;
            if (distance < 0.00001f) return;

            Vector3 direction = delta / distance;

            // Rope is a unilateral constraint: slack is allowed, stretching is not.
            if (distance > currentLength)
                clawHead.position = anchor + direction * currentLength;

            Vector3 velocity = clawHead.linearVelocity;
            float radialVelocity = Vector3.Dot(velocity, direction);
            float shrinkSpeed = Mathf.Max(0f, previousLength - currentLength) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);

            if (shrinkSpeed > 0f)
            {
                // A shortening rest length physically reels the claw upward while preserving
                // tangential velocity, which is what creates the characteristic return swing.
                Vector3 tangential = velocity - direction * radialVelocity;
                clawHead.linearVelocity = tangential - direction * shrinkSpeed;
            }
            else if (distance >= currentLength * 0.999f && radialVelocity > 0f)
            {
                clawHead.linearVelocity -= direction * radialVelocity * config.ropeRadialDamping;
            }

            previousLength = currentLength;
        }
    }
}
