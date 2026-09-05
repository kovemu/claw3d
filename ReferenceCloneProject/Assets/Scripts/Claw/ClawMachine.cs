using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Claw
{
    public enum ClawMachineState
    {
        off = 0,
        idle = 1,
        running = 2,
        grabbing = 3,
        returning = 4,
        waitToOpen = 5,
        openingClaw = 6,
        overwriteReturning = 7
    }

    public sealed class ClawMachine : MonoBehaviour
    {
        [SerializeField] private bool startActive = true;
        [SerializeField] private ClawMachineState curState = ClawMachineState.off;
        [SerializeField] private ClawMachineSettings settings = new ClawMachineSettings();
        [SerializeField] private ClawMoveModule clawMove;
        [SerializeField] private ClawModule claw;

        [SerializeField] private UnityEvent OnStartRound = new UnityEvent();
        [SerializeField] private UnityEvent OnEndRound = new UnityEvent();
        [SerializeField] private UnityEvent<float> OnClawReturnedToDefaultPosition = new UnityEvent<float>();

        private Coroutine openingCoroutine;

        public ClawMachineState GetCurState()
        {
            return curState;
        }

        public ClawMoveModule GetClawMoveModule()
        {
            return clawMove;
        }

        private void Awake()
        {
            if (clawMove != null)
                clawMove.Initialize(this);
            if (claw != null)
                claw.Initialize(this);

            SetMachineState(startActive ? ClawMachineState.idle : ClawMachineState.off);
        }

        public void CalledFixedUpdate()
        {
            // Source calls the input module's fixed update here first. The input module is intentionally
            // not guessed in Gate 1; the temporary Debug driver supplies MoveClaw calls separately.
            if (claw != null)
                claw.PhysicsUpdate();

            if (curState == ClawMachineState.returning && clawMove != null)
                clawMove.UpdateReturning();
        }

        private void FixedUpdate()
        {
            CalledFixedUpdate();
        }

        public void BeginRound()
        {
            if (curState == ClawMachineState.idle)
                SetMachineState(ClawMachineState.running);
        }

        public void BeginGrab()
        {
            if (curState == ClawMachineState.running)
                SetMachineState(ClawMachineState.grabbing);
        }

        public void SetMachineState(ClawMachineState next)
        {
            curState = next;

            switch (next)
            {
                case ClawMachineState.off:
                    return;

                case ClawMachineState.idle:
                    claw.OpenClaw();
                    return;

                case ClawMachineState.running:
                    if (OnStartRound != null)
                        OnStartRound.Invoke();
                    return;

                case ClawMachineState.grabbing:
                    claw.FullGrab();
                    // Source also enables the action camera here.
                    return;

                case ClawMachineState.returning:
                    if (settings != null && settings.dontReturn)
                        SetMachineState(ClawMachineState.openingClaw);

                    // Source additionally checks dontReturnIfEmpty through ClawHasContent().
                    // Prize-content filtering is outside Gate 1 and remains unmapped here.
                    return;

                case ClawMachineState.waitToOpen:
                    float distance = clawMove.GetAndResetDistanceMoved();
                    if (OnClawReturnedToDefaultPosition != null)
                        OnClawReturnedToDefaultPosition.Invoke(distance);
                    return;

                case ClawMachineState.openingClaw:
                    claw.OpenClaw();
                    if (openingCoroutine != null)
                        StopCoroutine(openingCoroutine);
                    openingCoroutine = StartCoroutine(FinishOpening(0.6f));
                    // Source also disables the action camera here.
                    return;

                case ClawMachineState.overwriteReturning:
                    curState = ClawMachineState.returning;
                    return;
            }
        }

        private IEnumerator FinishOpening(float delay)
        {
            yield return new WaitForSeconds(delay);
            openingCoroutine = null;

            // Source calls SetMachineState(idle), then PrizeSpawner.OnEndGrab(), then OnEndRound.
            SetMachineState(ClawMachineState.idle);
            if (OnEndRound != null)
                OnEndRound.Invoke();
        }
    }
}
