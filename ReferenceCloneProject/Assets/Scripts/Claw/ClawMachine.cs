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
        [SerializeField] private ClawMoveModule clawMove;
        [SerializeField] private ClawModule claw;

        [SerializeField] private UnityEvent OnStartRound = new UnityEvent();
        [SerializeField] private UnityEvent OnEndRound = new UnityEvent();

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

            curState = startActive ? ClawMachineState.idle : ClawMachineState.off;
        }

        public void CalledFixedUpdate()
        {
            if (curState == ClawMachineState.off) return;

            // Source ordering: input module fixed update occurs before this claw call.
            // Input reconstruction is intentionally not guessed in Gate 1.
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
            if (curState != ClawMachineState.idle) return;
            curState = ClawMachineState.running;
            OnStartRound.Invoke();
        }

        public void BeginGrab()
        {
            if (curState != ClawMachineState.running || claw == null) return;
            curState = ClawMachineState.grabbing;
            claw.FullGrab();
        }

        public void SetMachineState(ClawMachineState next)
        {
            curState = next;

            if (next == ClawMachineState.openingClaw)
            {
                if (claw != null)
                    claw.OpenClaw();

                if (openingCoroutine != null)
                    StopCoroutine(openingCoroutine);
                openingCoroutine = StartCoroutine(FinishOpening());
            }
        }

        private IEnumerator FinishOpening()
        {
            // Canonical machine path uses a hard-coded 0.6 second opening completion delay.
            yield return new WaitForSeconds(0.6f);
            openingCoroutine = null;
            curState = ClawMachineState.idle;
            OnEndRound.Invoke();
        }
    }
}
