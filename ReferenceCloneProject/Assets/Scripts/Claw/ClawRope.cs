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

        public override void Initialize(ClawMachine owner)
        {
            base.Initialize(owner);
            if (rope != null)
                initialRestLength = rope.restLength;
        }

        public override void FullGrab()
        {
            base.FullGrab();
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (cursor == null || rope == null) return;

            switch (internalState)
            {
                case ClawInternalState.lowering:
                    cursor.ChangeLength(rope.restLength + loweringSpeed);

                    if (rope.restLength >= initialRestLength + loweringDistance)
                    {
                        CloseClaw();
                        internalState = ClawInternalState.closing;

                        if (closeDelayCoroutine != null)
                            StopCoroutine(closeDelayCoroutine);
                        closeDelayCoroutine = StartCoroutine(WaitBeforeGoingUp());
                    }
                    break;

                case ClawInternalState.goingUp:
                    cursor.ChangeLength(rope.restLength - loweringSpeed);

                    if (rope.restLength <= initialRestLength)
                    {
                        cursor.ChangeLength(initialRestLength);
                        internalState = ClawInternalState.none;

                        if (clawMachine != null)
                            clawMachine.SetMachineState(ClawMachineState.returning);
                    }
                    break;
            }
        }

        private IEnumerator WaitBeforeGoingUp()
        {
            yield return new WaitForSeconds(timeToClose);
            closeDelayCoroutine = null;

            if (internalState == ClawInternalState.closing)
                internalState = ClawInternalState.goingUp;
        }
    }
}
#endif
