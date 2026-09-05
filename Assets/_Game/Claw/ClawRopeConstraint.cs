using System.Collections.Generic;
using Claw3D.Physics;
using UnityEngine;

namespace Claw3D.Claw
{
    /// <summary>
    /// Learning implementation of the rope structure extracted from Claw Machine Sim.
    /// Mirrors the observed runtime contract: three initially active particles, a pooled
    /// particle reserve, head-side cursor insertion/removal, four substeps, sequential
    /// distance constraints and dynamic pin feedback to the claw Rigidbody.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class ClawRopeConstraint : MonoBehaviour
    {
        private sealed class StructuralElement
        {
            public int particle1;
            public int particle2;
            public float restLength;

            public StructuralElement(int p1, int p2, float length)
            {
                particle1 = p1;
                particle2 = p2;
                restLength = length;
            }
        }

        [SerializeField] private ClawPhysicsConfig config;
        [SerializeField] private Rigidbody carriage;
        [SerializeField] private Rigidbody clawHead;
        [SerializeField] private float initialLength;
        [SerializeField] private float currentLength;

        private Vector3[] positions;
        private Vector3[] velocities;
        private Vector3[] previousPositions;
        private bool[] activeParticles;
        private readonly List<StructuralElement> elements = new();
        private readonly Stack<int> freeParticles = new();

        private int activeParticleCount;
        private Vector3 previousTopAttachment;
        private bool initialized;

        public float InitialLength => initialLength;
        public float CurrentLength => currentLength;
        public float MaximumDropLength => initialLength + (config == null ? 0f : config.loweringDistance);
        public int ParticleCount => !initialized || elements.Count == 0 ? 0 : elements.Count + 1;
        public int ActiveParticleCount => activeParticleCount;
        public int ElementCount => elements.Count;
        public bool HasSimulationPoints => initialized && elements.Count > 0;

        public void Configure(ClawPhysicsConfig physicsConfig, Rigidbody carriageBody, Rigidbody headBody)
        {
            config = physicsConfig;
            carriage = carriageBody;
            clawHead = headBody;
            InitializePool();
        }

        public void ResetLength()
        {
            InitializePool();
        }

        public bool ExtendOneStep()
        {
            if (config == null || !EnsureReady()) return true;

            float target = initialLength + config.loweringDistance;
            float next = Mathf.MoveTowards(currentLength, target, config.loweringStepPerFixedUpdate);
            ChangeLength(next);
            return Mathf.Abs(currentLength - target) < 0.00001f;
        }

        public bool RetractOneStep()
        {
            if (config == null || !EnsureReady()) return true;

            float next = Mathf.MoveTowards(currentLength, initialLength, config.loweringStepPerFixedUpdate);
            ChangeLength(next);
            return Mathf.Abs(currentLength - initialLength) < 0.00001f;
        }

        public Vector3 GetParticlePosition(int orderedIndex)
        {
            if (!HasSimulationPoints) return transform.position;

            orderedIndex = Mathf.Clamp(orderedIndex, 0, ParticleCount - 1);
            if (orderedIndex == 0)
                return positions[elements[0].particle1];

            return positions[elements[orderedIndex - 1].particle2];
        }

        private void FixedUpdate()
        {
            if (!EnsureReady()) return;
            SimulateRope();
        }

        private bool EnsureReady()
        {
            if (config == null || carriage == null || clawHead == null) return false;

            int capacity = Mathf.Max(3, config.ropeParticlePoolCapacity);
            if (!initialized || positions == null || positions.Length != capacity || elements.Count == 0)
                InitializePool();

            return initialized;
        }

        private void InitializePool()
        {
            if (config == null || carriage == null || clawHead == null)
            {
                initialized = false;
                return;
            }

            int capacity = Mathf.Max(3, config.ropeParticlePoolCapacity);
            positions = new Vector3[capacity];
            velocities = new Vector3[capacity];
            previousPositions = new Vector3[capacity];
            activeParticles = new bool[capacity];
            elements.Clear();
            freeParticles.Clear();

            for (int i = capacity - 1; i >= 3; --i)
                freeParticles.Push(i);

            activeParticles[0] = true;
            activeParticles[1] = true;
            activeParticles[2] = true;
            activeParticleCount = 3;

            float firstRest = Mathf.Max(0.000001f, config.ropeInitialElementRestLengths.x);
            float secondRest = Mathf.Max(0.000001f, config.ropeInitialElementRestLengths.y);
            initialLength = firstRest + secondRest;
            currentLength = initialLength;

            Vector3 headPoint = GetHeadAttachmentPoint();
            Vector3 topPoint = GetTopAttachmentPoint();
            Vector3 span = topPoint - headPoint;
            if (span.sqrMagnitude < 0.0000001f)
                span = Vector3.up * initialLength;

            float ratio = firstRest / initialLength;
            positions[0] = headPoint;
            positions[1] = Vector3.Lerp(headPoint, topPoint, ratio);
            positions[2] = topPoint;

            Vector3 headVelocity = clawHead.GetPointVelocity(headPoint);
            Vector3 topVelocity = carriage.linearVelocity;
            velocities[0] = headVelocity;
            velocities[1] = Vector3.Lerp(headVelocity, topVelocity, ratio);
            velocities[2] = topVelocity;

            elements.Add(new StructuralElement(0, 1, firstRest));
            elements.Add(new StructuralElement(1, 2, secondRest));

            previousTopAttachment = topPoint;
            initialized = true;
        }

        private void ChangeLength(float newLength)
        {
            if (!EnsureReady()) return;

            float maxLength = Mathf.Max(
                initialLength,
                (Mathf.Max(3, config.ropeParticlePoolCapacity) - 1) * config.ropeInterParticleDistance);
            newLength = Mathf.Clamp(newLength, initialLength, maxLength);

            float lengthChange = newLength - currentLength;
            if (Mathf.Abs(lengthChange) < 0.0000001f)
            {
                currentLength = RecalculateRestLength();
                return;
            }

            if (lengthChange > 0f)
                GrowFromCursor(lengthChange);
            else
                ShrinkFromCursor(-lengthChange);

            currentLength = RecalculateRestLength();

            if (Mathf.Abs(newLength - initialLength) < 0.00001f)
                CleanupCursorAtInitialLength();

            currentLength = RecalculateRestLength();
        }

        private void GrowFromCursor(float lengthChange)
        {
            if (elements.Count == 0) return;

            float interParticleDistance = Mathf.Max(0.000001f, config.ropeInterParticleDistance);
            StructuralElement cursor = elements[0];

            float lengthDelta = Mathf.Min(
                lengthChange,
                Mathf.Max(0f, interParticleDistance - cursor.restLength));

            if (lengthDelta > 0f)
            {
                cursor.restLength += lengthDelta;
                lengthChange -= lengthDelta;
            }

            while (lengthChange > 0.0000001f &&
                   freeParticles.Count > 0 &&
                   cursor.restLength + lengthChange > interParticleDistance)
            {
                lengthDelta = Mathf.Min(lengthChange, interParticleDistance);
                lengthChange -= lengthDelta;

                int newParticle = ActivateCursorParticle(cursor, lengthDelta);
                StructuralElement newElement = new StructuralElement(cursor.particle1, newParticle, lengthDelta);
                cursor.particle1 = newParticle;

                int cursorIndex = elements.IndexOf(cursor);
                elements.Insert(Mathf.Max(0, cursorIndex), newElement);
                cursor = newElement;
            }

            if (lengthChange > 0f)
                cursor.restLength += lengthChange;
        }

        private void ShrinkFromCursor(float lengthChange)
        {
            if (elements.Count == 0) return;

            StructuralElement cursor = elements[0];

            while (elements.Count > 2 && lengthChange > cursor.restLength)
            {
                lengthChange -= cursor.restLength;
                RemoveCursorElement();
                cursor = elements[0];
            }

            if (lengthChange > 0f)
            {
                float minimum = elements.Count <= 2
                    ? Mathf.Max(0.000001f, config.ropeInitialElementRestLengths.x)
                    : 0f;
                cursor.restLength = Mathf.Max(minimum, cursor.restLength - lengthChange);
            }
        }

        private int ActivateCursorParticle(StructuralElement cursor, float lengthDelta)
        {
            int particle = freeParticles.Pop();
            int source = Mathf.Clamp(config.ropeEndParticleIndex, 0, positions.Length - 1);

            activeParticles[particle] = true;
            activeParticleCount++;
            velocities[particle] = velocities[source];
            previousPositions[particle] = positions[source];

            Vector3 a = positions[cursor.particle1];
            Vector3 b = positions[cursor.particle2];
            positions[particle] = a + (b - a) * lengthDelta;
            return particle;
        }

        private void RemoveCursorElement()
        {
            if (elements.Count <= 2) return;

            StructuralElement cursor = elements[0];
            int removedParticle = cursor.particle2;
            StructuralElement next = elements[1];

            if (next.particle1 == removedParticle)
                next.particle1 = cursor.particle1;

            elements.RemoveAt(0);
            DeactivateParticle(removedParticle);
        }

        private void CleanupCursorAtInitialLength()
        {
            while (elements.Count > 2 && elements[0].restLength <= 0.000001f)
                RemoveCursorElement();

            while (elements.Count > 2)
                RemoveCursorElement();

            if (elements.Count >= 2)
            {
                elements[0].restLength = Mathf.Max(0.000001f, config.ropeInitialElementRestLengths.x);
                elements[1].restLength = Mathf.Max(0.000001f, config.ropeInitialElementRestLengths.y);
            }
        }

        private void DeactivateParticle(int particle)
        {
            if (particle < 0 || particle >= activeParticles.Length || !activeParticles[particle]) return;
            if (particle <= 2) return;

            activeParticles[particle] = false;
            positions[particle] = Vector3.zero;
            velocities[particle] = Vector3.zero;
            previousPositions[particle] = Vector3.zero;
            activeParticleCount--;
            freeParticles.Push(particle);
        }

        private float RecalculateRestLength()
        {
            float length = 0f;
            for (int i = 0; i < elements.Count; ++i)
                length += elements[i].restLength;
            return length;
        }

        private void SimulateRope()
        {
            int substeps = Mathf.Max(1, config.ropeSubsteps);
            float subDt = Time.fixedDeltaTime / substeps;
            Vector3 topTarget = GetTopAttachmentPoint();
            Vector3 topStart = previousTopAttachment;

            Vector3 headProxy = GetHeadAttachmentPoint();
            Vector3 headVelocity = clawHead.GetPointVelocity(headProxy);
            Vector3 accumulatedHeadPositionCorrection = Vector3.zero;

            for (int step = 0; step < substeps; ++step)
            {
                float alpha0 = step / (float)substeps;
                float alpha1 = (step + 1) / (float)substeps;
                Vector3 top0 = Vector3.Lerp(topStart, topTarget, alpha0);
                Vector3 top1 = Vector3.Lerp(topStart, topTarget, alpha1);
                Vector3 topVelocity = (top1 - top0) / Mathf.Max(0.000001f, subDt);

                // Predict the attached Rigidbody using the velocity PhysX currently owns. Do not
                // recursively feed the rope correction back into this velocity inside each substep:
                // Obi writes the accumulated dynamic-pin result back after the solver step.
                headProxy += headVelocity * subDt;

                IntegrateParticles(subDt);

                int distanceIterations = Mathf.Max(1, config.ropeDistanceIterations);
                for (int i = 0; i < distanceIterations; ++i)
                    SolveDistanceConstraintsSequential(subDt);

                SolveTopDynamicPin(top1);

                int pinIterations = Mathf.Max(1, config.ropePinIterations);
                for (int i = 0; i < pinIterations; ++i)
                    SolveHeadDynamicPin(
                        ref headProxy,
                        subDt,
                        ref accumulatedHeadPositionCorrection);

                UpdateParticleVelocities(subDt);

                int topParticle = GetTopParticleIndex();
                if (topParticle >= 0)
                    velocities[topParticle] = topVelocity;
            }

            previousTopAttachment = topTarget;

            // Previous implementation converted every substep correction using subDt and then
            // summed them. With four substeps this over-amplified the Rigidbody feedback by roughly
            // four times and produced the visible high-frequency trembling. Convert the accumulated
            // position correction once over the whole FixedUpdate instead.
            Vector3 velocityDelta = accumulatedHeadPositionCorrection /
                                    Mathf.Max(0.000001f, Time.fixedDeltaTime);
            ApplyHeadVelocityFeedback(velocityDelta);
        }

        private void IntegrateParticles(float dt)
        {
            for (int i = 0; i < activeParticles.Length; ++i)
            {
                if (!activeParticles[i]) continue;

                previousPositions[i] = positions[i];
                velocities[i] += UnityEngine.Physics.gravity * dt;
                positions[i] += velocities[i] * dt;
            }
        }

        private void SolveDistanceConstraintsSequential(float subDt)
        {
            float invMass = 1f / Mathf.Max(0.000001f, config.ropeParticleMass);
            float compliance = Mathf.Max(0f, config.ropeStretchCompliance);
            float alpha = compliance / Mathf.Max(0.0000001f, subDt * subDt);

            for (int i = 0; i < elements.Count; ++i)
            {
                StructuralElement element = elements[i];
                Vector3 delta = positions[element.particle2] - positions[element.particle1];
                float distance = delta.magnitude;
                if (distance < 0.000001f) continue;

                float totalInvMass = invMass + invMass + alpha;
                float error = distance - element.restLength;
                float lambda = error / totalInvMass;
                Vector3 correction = delta / distance * lambda;

                positions[element.particle1] += correction * invMass;
                positions[element.particle2] -= correction * invMass;
            }
        }

        private void SolveTopDynamicPin(Vector3 target)
        {
            int topParticle = GetTopParticleIndex();
            if (topParticle < 0) return;

            // Source attachment is Dynamic, but MOVER is kinematic. Zero compliance therefore
            // resolves to the target position while carriage motion is transmitted into the rope.
            positions[topParticle] = target;
        }

        private void SolveHeadDynamicPin(
            ref Vector3 headProxy,
            float subDt,
            ref Vector3 accumulatedPositionCorrection)
        {
            int headParticle = GetHeadParticleIndex();
            if (headParticle < 0 || clawHead == null || clawHead.isKinematic) return;

            float particleInvMass = 1f / Mathf.Max(0.000001f, config.ropeParticleMass);
            float bodyInvMass = 1f / Mathf.Max(0.000001f, clawHead.mass);
            float compliance = Mathf.Max(0f, config.ropeAttachmentCompliance);
            float alpha = compliance / Mathf.Max(0.0000001f, subDt * subDt);
            float totalInvMass = particleInvMass + bodyInvMass + alpha;
            if (totalInvMass <= 0f) return;

            Vector3 delta = headProxy - positions[headParticle];
            Vector3 particleCorrection = delta * (particleInvMass / totalInvMass);
            Vector3 bodyCorrection = -delta * (bodyInvMass / totalInvMass);

            positions[headParticle] += particleCorrection;
            headProxy += bodyCorrection;
            accumulatedPositionCorrection += bodyCorrection;
        }

        private void UpdateParticleVelocities(float subDt)
        {
            float invDt = 1f / Mathf.Max(0.000001f, subDt);
            for (int i = 0; i < activeParticles.Length; ++i)
            {
                if (!activeParticles[i]) continue;
                velocities[i] = (positions[i] - previousPositions[i]) * invDt;
            }
        }

        private void ApplyHeadVelocityFeedback(Vector3 velocityDelta)
        {
            if (clawHead == null || clawHead.isKinematic) return;
            if (!IsFinite(velocityDelta) || velocityDelta.sqrMagnitude < 0.00000001f) return;

            // Keep a generous safety ceiling for numerical accidents during editor hot-reload or
            // scene migration. Normal rope impulses should remain far below this value.
            const float maxVelocityCorrection = 12f;
            if (velocityDelta.magnitude > maxVelocityCorrection)
                velocityDelta = velocityDelta.normalized * maxVelocityCorrection;

            Vector3 impulse = velocityDelta * clawHead.mass;
            clawHead.AddForceAtPosition(impulse, GetHeadAttachmentPoint(), ForceMode.Impulse);
        }

        private int GetHeadParticleIndex()
        {
            return elements.Count == 0 ? -1 : elements[0].particle1;
        }

        private int GetTopParticleIndex()
        {
            return elements.Count == 0 ? -1 : elements[elements.Count - 1].particle2;
        }

        private Vector3 GetTopAttachmentPoint()
        {
            if (carriage == null || config == null) return transform.position;
            return carriage.position + carriage.rotation * config.ropeTopAttachmentOffset;
        }

        private Vector3 GetHeadAttachmentPoint()
        {
            if (clawHead == null || config == null) return transform.position;
            return clawHead.position + clawHead.rotation * config.ropeHeadAttachmentOffset;
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
                   !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
                   !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }
    }
}
