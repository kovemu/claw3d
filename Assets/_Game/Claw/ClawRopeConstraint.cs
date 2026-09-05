using Claw3D.Physics;
using UnityEngine;

namespace Claw3D.Claw
{
    /// <summary>
    /// Small PBD rope tailored to the claw prototype. It independently recreates the
    /// important runtime structure observed in the reference: a very low-resolution rope,
    /// multiple substeps, distance constraints, soft bending, a kinematic top attachment,
    /// and a dynamic bottom attachment that exchanges motion with the claw Rigidbody.
    /// </summary>
    public sealed class ClawRopeConstraint : MonoBehaviour
    {
        [SerializeField] private ClawPhysicsConfig config;
        [SerializeField] private Rigidbody carriage;
        [SerializeField] private Rigidbody clawHead;
        [SerializeField] private float initialLength;
        [SerializeField] private float currentLength;

        private Vector3[] positions;
        private Vector3[] velocities;
        private Vector3[] stepStartPositions;
        private bool initialized;

        public float InitialLength => initialLength;
        public float CurrentLength => currentLength;
        public float MaximumDropLength => initialLength + (config == null ? 0f : config.loweringDistance);
        public int ParticleCount => positions == null ? 0 : positions.Length;
        public bool HasSimulationPoints => initialized && positions != null && positions.Length >= 2;

        public void Configure(ClawPhysicsConfig physicsConfig, Rigidbody carriageBody, Rigidbody headBody)
        {
            config = physicsConfig;
            carriage = carriageBody;
            clawHead = headBody;
            initialLength = Mathf.Max(0.01f, config.cableLength);
            currentLength = initialLength;
            InitializeParticles();
        }

        public void ResetLength()
        {
            currentLength = initialLength;
            InitializeParticles();
        }

        public bool ExtendOneStep()
        {
            if (config == null) return true;
            float target = initialLength + config.loweringDistance;
            currentLength = Mathf.MoveTowards(currentLength, target, config.loweringStepPerFixedUpdate);
            return Mathf.Abs(currentLength - target) < 0.00001f;
        }

        public bool RetractOneStep()
        {
            if (config == null) return true;
            currentLength = Mathf.MoveTowards(currentLength, initialLength, config.loweringStepPerFixedUpdate);
            return Mathf.Abs(currentLength - initialLength) < 0.00001f;
        }

        public Vector3 GetParticlePosition(int index)
        {
            if (!HasSimulationPoints) return transform.position;
            index = Mathf.Clamp(index, 0, positions.Length - 1);
            return positions[index];
        }

        private void FixedUpdate()
        {
            if (!EnsureReady()) return;
            SimulateRope();
        }

        private bool EnsureReady()
        {
            if (config == null || carriage == null || clawHead == null) return false;
            int desiredCount = Mathf.Max(2, config.ropeActiveParticles);
            if (!initialized || positions == null || positions.Length != desiredCount)
                InitializeParticles();
            return initialized;
        }

        private void InitializeParticles()
        {
            if (config == null || carriage == null || clawHead == null)
            {
                initialized = false;
                return;
            }

            int count = Mathf.Max(2, config.ropeActiveParticles);
            positions = new Vector3[count];
            velocities = new Vector3[count];
            stepStartPositions = new Vector3[count];

            Vector3 a = carriage.worldCenterOfMass;
            Vector3 b = clawHead.worldCenterOfMass;
            Vector3 direction = b - a;
            if (direction.sqrMagnitude < 0.000001f) direction = Vector3.down;
            direction.Normalize();

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                positions[i] = Vector3.Lerp(a, a + direction * currentLength, t);
                velocities[i] = Vector3.zero;
            }

            positions[count - 1] = b;
            initialized = true;
        }

        private void SimulateRope()
        {
            int substeps = Mathf.Max(1, config.ropeSubsteps);
            float subDt = Time.fixedDeltaTime / substeps;
            float segmentLength = currentLength / (positions.Length - 1);
            float particleInvMass = 1f / Mathf.Max(0.0001f, config.ropeParticleMass);
            float bodyInvMass = clawHead.isKinematic ? 0f : 1f / Mathf.Max(0.0001f, clawHead.mass);

            Vector3 actualHeadStart = clawHead.worldCenterOfMass;
            Vector3 proxyHead = actualHeadStart;

            for (int step = 0; step < substeps; step++)
            {
                Vector3 top = carriage.worldCenterOfMass;
                positions[0] = top;
                velocities[0] = Vector3.zero;

                for (int i = 1; i < positions.Length; i++)
                {
                    stepStartPositions[i] = positions[i];
                    velocities[i] += UnityEngine.Physics.gravity * subDt;
                    positions[i] += velocities[i] * subDt;
                }

                SolveDistanceConstraints(segmentLength, particleInvMass);
                SolveSoftBending(subDt);
                SolveDynamicBottomAttachment(ref proxyHead, particleInvMass, bodyInvMass);
                SolveDistanceConstraints(segmentLength, particleInvMass);
                positions[0] = top;

                for (int i = 1; i < positions.Length; i++)
                    velocities[i] = (positions[i] - stepStartPositions[i]) / Mathf.Max(0.0001f, subDt);
            }

            ApplyBottomAttachmentToRigidbody(actualHeadStart, proxyHead);
        }

        private void SolveDistanceConstraints(float restLength, float invMass)
        {
            for (int i = 0; i < positions.Length - 1; i++)
            {
                Vector3 delta = positions[i + 1] - positions[i];
                float distance = delta.magnitude;
                if (distance < 0.000001f) continue;

                float wA = i == 0 ? 0f : invMass;
                float wB = invMass;
                float w = wA + wB;
                if (w <= 0f) continue;

                float error = distance - restLength;
                Vector3 correction = delta / distance * (error / w);

                if (wA > 0f) positions[i] += correction * wA;
                positions[i + 1] -= correction * wB;
            }
        }

        private void SolveSoftBending(float subDt)
        {
            if (positions.Length < 3 || config.ropeBendCompliance <= 0f) return;

            float dt2 = subDt * subDt;
            float stiffness = dt2 / (dt2 + Mathf.Max(0.000001f, config.ropeBendCompliance));

            for (int i = 1; i < positions.Length - 1; i++)
            {
                Vector3 midpoint = (positions[i - 1] + positions[i + 1]) * 0.5f;
                positions[i] = Vector3.LerpUnclamped(positions[i], midpoint, stiffness);
            }
        }

        private void SolveDynamicBottomAttachment(ref Vector3 proxyHead, float particleInvMass, float bodyInvMass)
        {
            int last = positions.Length - 1;
            Vector3 delta = proxyHead - positions[last];
            float totalInvMass = particleInvMass + bodyInvMass;
            if (totalInvMass <= 0f) return;

            positions[last] += delta * (particleInvMass / totalInvMass);
            proxyHead -= delta * (bodyInvMass / totalInvMass);
        }

        private void ApplyBottomAttachmentToRigidbody(Vector3 actualHeadStart, Vector3 proxyHead)
        {
            if (clawHead == null || clawHead.isKinematic) return;

            Vector3 correction = proxyHead - actualHeadStart;
            if (correction.sqrMagnitude < 0.00000001f) return;

            clawHead.position += correction;

            float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            Vector3 velocityCorrection = correction / dt * config.ropeBodyVelocityCoupling;
            const float maxCorrectionSpeed = 8f;
            if (velocityCorrection.magnitude > maxCorrectionSpeed)
                velocityCorrection = velocityCorrection.normalized * maxCorrectionSpeed;

            clawHead.linearVelocity += velocityCorrection;
        }
    }
}
