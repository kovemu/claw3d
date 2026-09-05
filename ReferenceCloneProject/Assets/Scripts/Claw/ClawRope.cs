#if CLAW_REFERENCE_OBI
using System.Collections;
using Obi;
using UnityEngine;

namespace Claw
{
    public sealed class ClawRope : ClawModule
    {
        [SerializeField] private ObiRopeCursor cursor;
        [SerializeField] private ObiRope rope;

        private float initialRestLength;
        private Coroutine closeDelayCoroutine;

        protected override void InitializeSourceReferences()
        {
            base.InitializeSourceReferences();
            initialRestLength = rope.restLength;
        }

        public override void FullGrab()
        {
            base.FullGrab();
            // Source also starts loop sound "claw.rope.lower" here.
        }

        protected override void LoweringPhysicsUpdate()
        {
            float requestedLength = rope.restLength + loweringSpeed;
            cursor.ChangeLength(requestedLength);

            if (rope.restLength < initialRestLength + loweringDistance)
                return;

            CloseClaw();
            internalState = ClawInternalState.closing;
            closeDelayCoroutine = StartCoroutine(ChangeStateAfterDelay(timeToClose, ClawInternalState.goingUp));
            // Source cancels loop sound "claw.rope.lower" here.
        }

        protected override void GoingUpPhysicsUpdate()
        {
            float requestedLength = rope.restLength - loweringSpeed;
            cursor.ChangeLength(requestedLength);

            if (rope.restLength > initialRestLength)
                return;

            cursor.ChangeLength(initialRestLength);
            internalState = ClawInternalState.none;

            if (clawMachine != null)
                clawMachine.SetMachineState(ClawMachineState.returning);

            // Source cancels loop sound "claw.rope.raise" here.
        }

        private IEnumerator ChangeStateAfterDelay(float delay, ClawInternalState newState)
        {
            yield return new WaitForSeconds(delay);
            internalState = newState;
            closeDelayCoroutine = null;
            // Source starts loop sound "claw.rope.raise" after this delay.
        }
    }
}
#endif
