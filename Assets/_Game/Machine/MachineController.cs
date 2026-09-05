using System.Collections.Generic;
using Claw3D.Claw;
using Claw3D.Input;
using Claw3D.Physics;
using Claw3D.Toys;
using UnityEngine;

namespace Claw3D.Machine
{
    public sealed class MachineController : MonoBehaviour
    {
        [SerializeField] private ClawInput input;
        [SerializeField] private ClawController claw;
        [SerializeField] private ClawPhysicsConfig config;
        [SerializeField] private MachineState state = MachineState.Idle;

        private readonly HashSet<ToyPhysics> scoredToys = new();
        private float stateTimer;
        private float grabProfileTimer;
        private bool dyingProfileChanged;
        private bool releaseStarted;
        private int prizes;
        private int prizesAtRoundStart;
        private int failedTries;
        private string prompt = "Space: start";

        public void Configure(ClawInput clawInput, ClawController clawController, ClawPhysicsConfig physicsConfig)
        {
            input = clawInput;
            claw = clawController;
            config = physicsConfig;
            EnterState(MachineState.Idle);
        }

        public void ReportPrize(ToyPhysics toy)
        {
            if (toy == null || !scoredToys.Add(toy)) return;
            prizes++;
            prompt = "PRIZE!";
        }

        private void Update()
        {
            if (input == null || claw == null || config == null) return;

            stateTimer += Time.deltaTime;

            if (state == MachineState.Grip || state == MachineState.Lift || state == MachineState.Return)
            {
                grabProfileTimer += Time.deltaTime;
                if (!dyingProfileChanged &&
                    claw.ActiveGrabType == ClawGrabType.Dying &&
                    grabProfileTimer >= config.realisticDyingDelaySeconds)
                {
                    claw.ApplyDelayedDyingProfile();
                    dyingProfileChanged = true;
                }
            }

            if (input.DropPressed)
            {
                if (state == MachineState.Idle)
                {
                    prizesAtRoundStart = prizes;
                    EnterState(MachineState.Aim);
                }
                else if (state == MachineState.Aim)
                {
                    EnterState(MachineState.Drop);
                }
            }

            switch (state)
            {
                case MachineState.Grip:
                    if (stateTimer >= config.timeToClose)
                        EnterState(MachineState.Lift);
                    break;

                case MachineState.Release:
                    if (!releaseStarted && stateTimer >= config.delayToOpen)
                    {
                        releaseStarted = true;
                        claw.SetOpenAmount(1f);
                        prompt = "Opening...";
                    }

                    if (releaseStarted && stateTimer >= config.delayToOpen + config.timeToOpen)
                        EnterState(MachineState.Score);
                    break;

                case MachineState.Score:
                    if (stateTimer >= config.scoreSeconds)
                    {
                        bool success = prizes > prizesAtRoundStart;
                        failedTries = success ? 0 : failedTries + 1;
                        EnterState(MachineState.Idle);
                    }
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
                    if (claw.LowerRopeOneStep())
                        EnterState(MachineState.Grip);
                    break;

                case MachineState.Lift:
                    if (claw.RaiseRopeOneStep())
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

            switch (state)
            {
                case MachineState.Idle:
                    claw.SetOpenAmount(1f);
                    prompt = "Space: start";
                    break;

                case MachineState.Aim:
                    claw.SetOpenAmount(1f);
                    prompt = "WASD / arrows: aim · Space: drop";
                    break;

                case MachineState.Drop:
                    // Choose the profile now, but do NOT apply the high grab damping while the
                    // claw is descending. That was freezing the articulated three-finger rig.
                    claw.SelectGrabProfile(failedTries);
                    claw.SetOpenAmount(1f);
                    grabProfileTimer = 0f;
                    dyingProfileChanged = false;
                    prompt = "Dropping...";
                    break;

                case MachineState.Grip:
                    claw.ApplySelectedGrabProfile();
                    claw.SetOpenAmount(0f);
                    grabProfileTimer = 0f;
                    prompt = "Gripping...";
                    break;

                case MachineState.Lift:
                    claw.SetOpenAmount(0f);
                    prompt = "Lifting...";
                    break;

                case MachineState.Return:
                    claw.SetOpenAmount(0f);
                    prompt = "Returning...";
                    break;

                case MachineState.Release:
                    releaseStarted = false;
                    claw.SetOpenAmount(0f);
                    prompt = "Waiting to open...";
                    break;

                case MachineState.Score:
                    claw.SetOpenAmount(1f);
                    prompt = prizes > prizesAtRoundStart ? "PRIZE! Nice grab." : "Miss. Try again.";
                    break;
            }
        }

        private void OnGUI()
        {
            string mode = config == null ? "?" : config.difficultyMode.ToString();
            GUI.Box(
                new Rect(12f, 12f, 500f, 152f),
                $"CLAW3D | {state} | {mode}\n{prompt}\n" +
                $"Grab: {claw.ActiveGrabType}  Failed: {failedTries}  Prizes: {prizes}\n" +
                $"Swing: {claw.HubSwingSpeed:0.00} m/s  Finger angle: {claw.AverageFingerAngle:0.0}°\n" +
                $"Rope: {claw.RopeRestLength:0.000} m  Particles: {claw.RopeActiveParticles}  Elements: {claw.RopeElements}");
        }
    }
}
