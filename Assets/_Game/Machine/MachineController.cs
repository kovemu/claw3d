using Claw3D.Claw;
using Claw3D.Input;
using Claw3D.Physics;
using UnityEngine;

namespace Claw3D.Machine
{
    public sealed class MachineController : MonoBehaviour
    {
        [SerializeField] private ClawInput input;
        [SerializeField] private ClawController claw;
        [SerializeField] private ClawPhysicsConfig config;
        [SerializeField] private MachineState state = MachineState.Aim;

        private float stateTimer;

        public void Configure(ClawInput clawInput, ClawController clawController, ClawPhysicsConfig physicsConfig)
        {
            input = clawInput;
            claw = clawController;
            config = physicsConfig;
            EnterState(MachineState.Aim);
        }

        private void Update()
        {
            if (input == null || claw == null || config == null) return;

            if (state == MachineState.Aim && input.DropPressed)
                EnterState(MachineState.Drop);

            if (state == MachineState.Grip || state == MachineState.Release)
            {
                stateTimer += Time.deltaTime;

                if (state == MachineState.Grip && stateTimer >= config.gripHoldSeconds)
                    EnterState(MachineState.Lift);
                else if (state == MachineState.Release && stateTimer >= config.releaseHoldSeconds)
                    EnterState(MachineState.Aim);
            }
        }

        private void FixedUpdate()
        {
            if (input == null || claw == null || config == null) return;

            switch (state)
            {
                case MachineState.Aim:
                    claw.MoveAim(input.Move);
                    break;

                case MachineState.Drop:
                    if (claw.MoveCableToward(config.bottomCableLength, config.dropSpeed))
                        EnterState(MachineState.Grip);
                    break;

                case MachineState.Lift:
                    if (claw.MoveCableToward(config.topCableLength, config.liftSpeed))
                        EnterState(MachineState.Return);
                    break;

                case MachineState.Return:
                    if (claw.ReturnHome())
                        EnterState(MachineState.Release);
                    break;
            }
        }

        private void EnterState(MachineState next)
        {
            state = next;
            stateTimer = 0f;

            if (claw == null) return;

            switch (state)
            {
                case MachineState.Aim:
                case MachineState.Drop:
                    claw.SetGrip(false);
                    break;
                case MachineState.Grip:
                case MachineState.Lift:
                case MachineState.Return:
                    claw.SetGrip(true);
                    break;
                case MachineState.Release:
                    claw.SetGrip(false);
                    break;
            }
        }
    }
}
