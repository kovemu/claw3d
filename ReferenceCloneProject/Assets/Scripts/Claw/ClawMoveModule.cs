using System.Collections;
using UnityEngine;

namespace Claw
{
    public sealed class ClawMoveModule : Module
    {
        [SerializeField] private MeshRenderer meshBounds;
        [SerializeField] private Transform clawMoverTrans;
        [SerializeField] private Rigidbody clawMoverRb;
        [SerializeField] private float speed = 0.007f;
        [SerializeField] private bool velocityBasedMovement;
        [SerializeField] private float velocitySpeed;
        [SerializeField] private bool returnAxisAtATime = true;
        [SerializeField] private float delayToOpen = 0.4f;

        private Bounds cachedBounds;
        private Vector3 startPosition;
        private Vector2 returnVector;
        private int returnDirection;
        private bool returningSecondAxis;
        private bool inverted;
        private float distanceMoved;

        public override void Initialize(ClawMachine owner)
        {
            startPosition = clawMoverTrans.position;
            cachedBounds = meshBounds.bounds;
            base.Initialize(owner);
        }

        public Transform GetMoverTrans()
        {
            return clawMoverTrans;
        }

        public void SetInverted(bool value)
        {
            inverted = value;
        }

        public float GetAndResetDistanceMoved()
        {
            float result = distanceMoved;
            distanceMoved = 0f;
            return result;
        }

        public void MoveClaw(Vector2 input)
        {
            if (inverted)
                input = new Vector2(input.y, input.x);

            input *= speed;

            Vector3 oldPosition = clawMoverTrans.position;
            Vector3 newPosition = oldPosition;

            Vector3 xCandidate = oldPosition + Vector3.right * input.x;
            if (cachedBounds.Contains(xCandidate))
                newPosition += Vector3.right * input.x;

            Vector3 zCandidate = oldPosition + Vector3.forward * input.y;
            if (cachedBounds.Contains(zCandidate))
                newPosition += Vector3.forward * input.y;

            // Source also starts/stops the x/y movement loop sounds here.
            distanceMoved += Vector3.Distance(newPosition, oldPosition);
            clawMoverRb.MovePosition(newPosition);
        }

        public void UpdateReturning()
        {
            if (returnAxisAtATime)
            {
                UpdateReturningAxisAtATime();
                return;
            }

            UpdateReturningTogether();
        }

        public void CancelReturning()
        {
            returnDirection = 0;
            returningSecondAxis = false;
            returnVector = Vector2.zero;
        }

        private void UpdateReturningAxisAtATime()
        {
            if (!returningSecondAxis)
            {
                ReturnZAxis();
                return;
            }

            ReturnXAxis();
        }

        private void ReturnZAxis()
        {
            if (returnDirection == 0)
                returnDirection = (int)Mathf.Sign(startPosition.z - clawMoverTrans.position.z);

            MoveClaw(new Vector2(0f, returnDirection));

            int newSign = (int)Mathf.Sign(startPosition.z - clawMoverTrans.position.z);
            if (newSign == returnDirection)
                return;

            returnDirection = 0;
            returningSecondAxis = true;
        }

        private void ReturnXAxis()
        {
            if (returnDirection == 0)
                returnDirection = (int)Mathf.Sign(startPosition.x - clawMoverTrans.position.x);

            MoveClaw(new Vector2(returnDirection, 0f));

            int newSign = (int)Mathf.Sign(startPosition.x - clawMoverTrans.position.x);
            if (newSign == returnDirection)
                return;

            returnDirection = 0;
            clawMachine.SetMachineState(ClawMachineState.waitToOpen);
            StartCoroutine(ChangeMachineStateAfterDelay(delayToOpen, ClawMachineState.openingClaw));
            returningSecondAxis = false;
            MoveClaw(Vector2.zero);
        }

        private void UpdateReturningTogether()
        {
            Vector3 current = clawMoverTrans.position;

            if (current.x <= startPosition.x && current.z <= startPosition.z)
            {
                returnVector = Vector2.zero;
                clawMachine.SetMachineState(ClawMachineState.waitToOpen);
                StartCoroutine(ChangeMachineStateAfterDelay(delayToOpen, ClawMachineState.openingClaw));
                return;
            }

            if (returnVector == Vector2.zero)
            {
                Vector3 direction = startPosition - current;
                direction.y = 0f;
                direction.Normalize();
                returnVector = new Vector2(direction.x, direction.z);
            }

            MoveClaw(returnVector);
        }

        private IEnumerator ChangeMachineStateAfterDelay(float delay, ClawMachineState newState)
        {
            yield return new WaitForSeconds(delay);
            clawMachine.SetMachineState(newState);
        }
    }
}
