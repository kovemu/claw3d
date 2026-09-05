using Claw3D.Physics;
using UnityEngine;

namespace Claw3D.Claw
{
    public sealed class ClawController : MonoBehaviour
    {
        [SerializeField] private ClawPhysicsConfig config;
        [SerializeField] private Rigidbody trolleyBody;
        [SerializeField] private Rigidbody hubBody;
        [SerializeField] private ClawFinger[] fingers;
        [SerializeField] private ClawRopeConstraint rope;
        [SerializeField] private ClawGrabType activeGrabType = ClawGrabType.None;

        public Vector3 TrolleyPosition => trolleyBody == null ? transform.position : trolleyBody.position;
        public Vector3 HubPosition => hubBody == null ? transform.position : hubBody.position;
        public float HubSwingSpeed => hubBody == null ? 0f : new Vector2(hubBody.linearVelocity.x, hubBody.linearVelocity.z).magnitude;
        public ClawGrabType ActiveGrabType => activeGrabType;
        public int RopeActiveParticles => rope == null ? 0 : rope.ActiveParticleCount;
        public int RopeElements => rope == null ? 0 : rope.ElementCount;
        public float RopeRestLength => rope == null ? 0f : rope.CurrentLength;
        public float AverageFingerAngle
        {
            get
            {
                if (fingers == null || fingers.Length == 0) return 0f;
                float sum = 0f;
                int count = 0;
                foreach (ClawFinger finger in fingers)
                {
                    if (finger == null) continue;
                    sum += finger.CurrentAngle;
                    count++;
                }
                return count == 0 ? 0f : sum / count;
            }
        }

        public void Configure(
            ClawPhysicsConfig physicsConfig,
            Rigidbody trolley,
            Rigidbody hub,
            ClawFinger[] clawFingers,
            ClawRopeConstraint ropeConstraint = null)
        {
            config = physicsConfig;
            trolleyBody = trolley;
            hubBody = hub;
            fingers = clawFingers;
            rope = ropeConstraint != null ? ropeConstraint : GetComponent<ClawRopeConstraint>();
            activeGrabType = ClawGrabType.None;
            SetOpenAmount(1f);
        }

        public void MoveAim(Vector2 input)
        {
            if (config == null || trolleyBody == null) return;

            Vector3 p = trolleyBody.position;
            p.x = Mathf.Clamp(
                p.x - input.x * config.trolleyStepPerFixedUpdate,
                config.xLimits.x,
                config.xLimits.y);
            p.z = Mathf.Clamp(
                p.z - input.y * config.trolleyStepPerFixedUpdate,
                config.zLimits.x,
                config.zLimits.y);
            trolleyBody.MovePosition(p);
        }

        public bool LowerRopeOneStep()
        {
            return rope == null || rope.ExtendOneStep();
        }

        public bool RaiseRopeOneStep()
        {
            return rope == null || rope.RetractOneStep();
        }

        public bool ReturnHome()
        {
            if (config == null || trolleyBody == null) return true;

            Vector3 p = trolleyBody.position;
            float step = config.trolleyStepPerFixedUpdate;

            if (config.returnAxisAtATime)
            {
                if (Mathf.Abs(p.x - config.homeXZ.x) > 0.0001f)
                {
                    p.x = Mathf.MoveTowards(p.x, config.homeXZ.x, step);
                }
                else
                {
                    p.x = config.homeXZ.x;
                    p.z = Mathf.MoveTowards(p.z, config.homeXZ.y, step);
                }
            }
            else
            {
                Vector2 current = new Vector2(p.x, p.z);
                Vector2 next = Vector2.MoveTowards(current, config.homeXZ, step);
                p.x = next.x;
                p.z = next.y;
            }

            trolleyBody.MovePosition(p);
            return Mathf.Abs(p.x - config.homeXZ.x) < 0.0001f &&
                   Mathf.Abs(p.z - config.homeXZ.y) < 0.0001f;
        }

        public ClawGrabType SelectGrabProfile(int failedTries)
        {
            if (config == null)
            {
                activeGrabType = ClawGrabType.Normal;
                return activeGrabType;
            }

            if (config.difficultyMode == ClawDifficultyMode.Normal)
            {
                activeGrabType = failedTries >= config.normalFailedTriesForStrong
                    ? ClawGrabType.Strong
                    : ClawGrabType.Normal;
                return activeGrabType;
            }

            int normal = Mathf.Max(0, config.realisticNormalWeight);
            int strongWeight = Mathf.Max(0, config.realisticStrongWeight);
            int dead = Mathf.Max(0, config.realisticDeadWeight);
            int dying = Mathf.Max(0, config.realisticDyingWeight);
            int total = normal + strongWeight + dead + dying;
            int roll = total <= 0 ? 0 : Random.Range(0, total);

            if (roll < normal)
                activeGrabType = ClawGrabType.Normal;
            else if ((roll -= normal) < strongWeight)
                activeGrabType = ClawGrabType.Strong;
            else if ((roll -= strongWeight) < dead)
                activeGrabType = ClawGrabType.Dead;
            else
                activeGrabType = ClawGrabType.Dying;

            return activeGrabType;
        }

        public void ApplySelectedGrabProfile()
        {
            if (config == null) return;

            if (config.difficultyMode == ClawDifficultyMode.Normal)
            {
                bool strong = activeGrabType == ClawGrabType.Strong;
                ApplyToFingers(
                    strong ? config.normalStrongClawVelocity : config.normalClawVelocity,
                    strong ? ClawGripMaterial.HighFriction : ClawGripMaterial.Default);
                return;
            }

            ApplyActiveGrabProfile(false);
        }

        public void ApplyDelayedDyingProfile()
        {
            if (activeGrabType != ClawGrabType.Dying || config == null) return;
            ApplyToFingers(config.realisticDyingDelayedVelocity, ClawGripMaterial.Icey);
        }

        public void SetOpenAmount(float open)
        {
            if (fingers == null) return;
            foreach (ClawFinger finger in fingers)
                if (finger != null) finger.SetOpenAmount(open);
        }

        private void ApplyActiveGrabProfile(bool delayed)
        {
            if (config == null) return;

            switch (activeGrabType)
            {
                case ClawGrabType.Strong:
                    ApplyToFingers(config.realisticStrongVelocity, ClawGripMaterial.HighFriction);
                    break;
                case ClawGrabType.Dead:
                    ApplyToFingers(config.realisticDeadVelocity, ClawGripMaterial.Icey);
                    break;
                case ClawGrabType.Dying:
                    ApplyToFingers(
                        delayed ? config.realisticDyingDelayedVelocity : config.realisticDyingInitialVelocity,
                        delayed ? ClawGripMaterial.Icey : ClawGripMaterial.HighFriction);
                    break;
                default:
                    ApplyToFingers(config.realisticNormalVelocity, ClawGripMaterial.Default);
                    break;
            }
        }

        private void ApplyToFingers(float angularVelocity, ClawGripMaterial material)
        {
            if (fingers == null || config == null) return;
            foreach (ClawFinger finger in fingers)
            {
                if (finger == null) continue;
                finger.ApplyGrabSettings(
                    angularVelocity,
                    config.grabLinearDamping,
                    config.grabAngularDamping,
                    material);
            }
        }
    }
}
