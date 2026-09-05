using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Claw
{
    public enum ClawInternalState
    {
        none = 0,
        lowering = 1,
        closing = 2,
        goingUp = 3
    }

    public class ClawModule : Module
    {
        [SerializeField] protected List<ClawArm> claws = new List<ClawArm>();
        [SerializeField] protected ClawSettings clawSettings;
        [SerializeField] protected float loweringSpeed;
        [SerializeField] protected float loweringDistance;
        [SerializeField] protected float timeToClose;
        [SerializeField] protected float timeToOpen;
        [SerializeField] protected ClawInternalState internalState = ClawInternalState.none;

        [SerializeField] protected UnityEvent OnCloseClaw = new UnityEvent();
        [SerializeField] protected UnityEvent OnOpenClaw = new UnityEvent();
        [SerializeField] protected UnityEvent OnStartGrab = new UnityEvent();

        // Source direction state: Open=-1, Close=+1.
        protected int directionMultiplier;

        public float LoweringDistance
        {
            get { return loweringDistance; }
            set { loweringDistance = value; }
        }

        public override void Initialize(ClawMachine owner)
        {
            base.Initialize(owner);
            ApplyCurrentMaterial();
        }

        public virtual void FullGrab()
        {
            internalState = ClawInternalState.lowering;
            OnStartGrab.Invoke();
        }

        public virtual void PhysicsUpdate()
        {
            if (clawSettings == null) return;

            float command = clawSettings.clawVelocity * directionMultiplier;
            for (int i = 0; i < claws.Count; ++i)
            {
                ClawArm arm = claws[i];
                if (arm != null)
                    arm.SetAngularVelocity(command);
            }
        }

        public void OpenClaw()
        {
            directionMultiplier = -1;
            OnOpenClaw.Invoke();
        }

        public void CloseClaw()
        {
            directionMultiplier = 1;
            OnCloseClaw.Invoke();
        }

        public ClawSettings GetCurrentClawSettings()
        {
            return clawSettings;
        }

        public void SetClawSettings(ClawSettings settings)
        {
            clawSettings = settings;
            ApplyCurrentMaterial();
        }

        protected void ApplyCurrentMaterial()
        {
            PhysicMaterial material = clawSettings == null ? null : clawSettings.clawPhysicMat;
            for (int i = 0; i < claws.Count; ++i)
            {
                ClawArm arm = claws[i];
                if (arm != null)
                    arm.ApplyPhysicMaterial(material);
            }
        }
    }
}
