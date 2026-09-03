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
        [SerializeField] private MachineState state = MachineState.Idle;

        private float stateTimer;
        private float fingerOpen = 1f;
        private string prompt = "Space: start";

        public void Configure(ClawInput clawInput, ClawController clawController, ClawPhysicsConfig physicsConfig)
        {
            input = clawInput;
            claw = clawController;
            config = physicsConfig;
            EnterState(MachineState.Idle);
        }

        private void Update()
        {
            if (input == null || claw == null || config == null) return;
            stateTimer += Time.deltaTime;

            if (input.DropPressed)
            {
                if (state == MachineState.Idle) EnterState(MachineState.Aim);
                else if (state == MachineState.Aim) EnterState(MachineState.Drop);
            }

            switch (state)
            {
                case MachineState.Grip:
                    fingerOpen = 1f - Mathf.Clamp01(stateTimer / config.gripSeconds);
                    claw.SetOpenAmount(fingerOpen);
                    if (stateTimer >= config.gripSeconds) EnterState(MachineState.Lift);
                    break;

                case MachineState.Release:
                    fingerOpen = Mathf.Clamp01(stateTimer / config.releaseSeconds);
                    claw.SetOpenAmount(fingerOpen);
                    if (stateTimer >= config.releaseSeconds) EnterState(MachineState.Score);
                    break;

                case MachineState.Score:
                    if (stateTimer >= config.scoreSeconds) EnterState(MachineState.Idle);
                    break;
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
                    if (claw.MoveVerticalToward(config.bottomY, config.dropSpeed))
                        EnterState(MachineState.Grip);
                    break;

                case MachineState.Lift:
                    if (claw.MoveVerticalToward(config.topY, config.liftSpeed))
                        EnterState(MachineState.Return);
                    break;

                case MachineState.Return:
                    if (stateTimer < config.joltDuration && config.joltAmplitude > 0f)
                    {
                        claw.ApplyTopStopJolt(stateTimer);
                    }
                    else
                    {
                        claw.MoveVerticalToward(config.topY, config.liftSpeed);
                        if (claw.ReturnHome()) EnterState(MachineState.Release);
                    }
                    break;
            }
        }

        private void EnterState(MachineState next)
        {
            state = next;
            stateTimer = 0f;

            switch (state)
            {
                case MachineState.Idle:
                    fingerOpen = 1f;
                    claw.SetStrengthScale(1f);
                    claw.SetOpenAmount(1f);
                    prompt = "Space: start";
                    break;
                case MachineState.Aim:
                    fingerOpen = 1f;
                    claw.SetStrengthScale(1f);
                    claw.SetOpenAmount(1f);
                    prompt = "WASD / arrows: aim · Space: drop";
                    break;
                case MachineState.Drop:
                    claw.SetStrengthScale(1f);
                    claw.SetOpenAmount(1f);
                    prompt = "Dropping...";
                    break;
                case MachineState.Grip:
                    claw.SetStrengthScale(1f);
                    prompt = "Gripping...";
                    break;
                case MachineState.Lift:
                    fingerOpen = 0f;
                    claw.SetOpenAmount(0f);
                    claw.SetStrengthScale(config.carryStrengthFactor);
                    prompt = "Lifting...";
                    break;
                case MachineState.Return:
                    claw.SetOpenAmount(0f);
                    claw.SetStrengthScale(config.carryStrengthFactor);
                    prompt = "Returning...";
                    break;
                case MachineState.Release:
                    claw.SetStrengthScale(1f);
                    prompt = "Releasing...";
                    break;
                case MachineState.Score:
                    claw.SetOpenAmount(1f);
                    prompt = "Round complete";
                    break;
            }
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(12f, 12f, 350f, 58f), $"CLAW3D  |  {state}\n{prompt}");
        }
    }
}
