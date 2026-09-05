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

        // Verified source field HIBDPCIHGNI: Open=-1, Close=+1.
        protected int directionMultiplier;

        public float LoweringDistance
        {
            get { return loweringDistance; }
            set { loweringDistance = value; }
        }

        private void Awake()
        {
            InitializeSourceReferences();
            ApplyCurrentMaterial();
        }

        protected virtual void InitializeSourceReferences()
        {
            ClawReferences references = GetComponent<ClawReferences>();
            if (references != null)
                claws = references.claws;
        }

        public override void Initialize(ClawMachine owner)
        {
            // Source Module.Initialize only stores the owner. Claw references/material are prepared in Awake.
            base.Initialize(owner);
        }

        public virtual void FullGrab()
        {
            internalState = ClawInternalState.lowering;
            if (OnStartGrab != null)
                OnStartGrab.Invoke();
        }

        public virtual void PhysicsUpdate()
        {
            float command = clawSettings.clawVelocity * directionMultiplier;

            for (int i = 0; i < claws.Count; ++i)
                claws[i].SetAngularVelocity(command);

            if (internalState == ClawInternalState.none)
                return;

            switch (internalState)
            {
                case ClawInternalState.lowering:
                    LoweringPhysicsUpdate();
                    break;

                case ClawInternalState.goingUp:
                    GoingUpPhysicsUpdate();
                    break;

                case ClawInternalState.closing:
                    break;
            }
        }

        // The source base class has virtual distance-check implementations in these slots.
        // Gate 1 uses ClawRope, which overrides both slots with Obi rest-length control.
        protected virtual void LoweringPhysicsUpdate() { }
        protected virtual void GoingUpPhysicsUpdate() { }

        public void OpenClaw()
        {
            directionMultiplier = -1;
            if (OnOpenClaw != null)
                OnOpenClaw.Invoke();
        }

        public void CloseClaw()
        {
            directionMultiplier = 1;
            if (OnCloseClaw != null)
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
            for (int i = 0; i < claws.Count; ++i)
                claws[i].ApplyPhysicMaterial(clawSettings.clawPhysicMat);
        }
    }
}
