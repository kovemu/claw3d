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
        private float distanceMoved;
        private bool inverted;
        private Coroutine returnDelayCoroutine;

        public override void Initialize(ClawMachine owner)
        {
            base.Initialize(owner);

            if (meshBounds != null)
                cachedBounds = meshBounds.bounds;
            if (clawMoverTrans != null)
                startPosition = clawMoverTrans.position;
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
            if (clawMoverTrans == null || clawMoverRb == null) return;

            if (inverted)
                input = new Vector2(input.y, input.x);

            Vector2 step = input * speed;
            Vector3 oldPosition = clawMoverTrans.position;
            Vector3 newPosition = oldPosition;

            float proposedX = oldPosition.x + step.x;
            if (IsInsideX(proposedX))
                newPosition.x = proposedX;

            float proposedZ = oldPosition.z + step.y;
            if (IsInsideZ(proposedZ))
                newPosition.z = proposedZ;

            distanceMoved += Vector3.Distance(newPosition, oldPosition);
            clawMoverRb.MovePosition(newPosition);
        }

        public void UpdateReturning()
        {
            if (clawMoverTrans == null || clawMoverRb == null) return;

            Vector3 current = clawMoverTrans.position;
            Vector3 next = current;

            if (returnAxisAtATime)
            {
                // Verified canonical return order: Z first, then X.
                if (!Mathf.Approximately(current.z, startPosition.z))
                    next.z = Mathf.MoveTowards(current.z, startPosition.z, speed);
                else
                    next.x = Mathf.MoveTowards(current.x, startPosition.x, speed);
            }
            else
            {
                Vector2 planar = Vector2.MoveTowards(
                    new Vector2(current.x, current.z),
                    new Vector2(startPosition.x, startPosition.z),
                    speed);
                next.x = planar.x;
                next.z = planar.y;
            }

            distanceMoved += Vector3.Distance(next, current);
            clawMoverRb.MovePosition(next);

            bool xDone = Mathf.Abs(next.x - startPosition.x) <= 0.00001f;
            bool zDone = Mathf.Abs(next.z - startPosition.z) <= 0.00001f;
            if (!xDone || !zDone || clawMachine == null) return;

            clawMachine.SetMachineState(ClawMachineState.waitToOpen);

            if (returnDelayCoroutine != null)
                StopCoroutine(returnDelayCoroutine);
            returnDelayCoroutine = StartCoroutine(OpenAfterDelay());
        }

        public void CancelReturning()
        {
            if (returnDelayCoroutine != null)
            {
                StopCoroutine(returnDelayCoroutine);
                returnDelayCoroutine = null;
            }
        }

        private IEnumerator OpenAfterDelay()
        {
            yield return new WaitForSeconds(delayToOpen);
            returnDelayCoroutine = null;

            if (clawMachine != null && clawMachine.GetCurState() == ClawMachineState.waitToOpen)
                clawMachine.SetMachineState(ClawMachineState.openingClaw);
        }

        private bool IsInsideX(float x)
        {
            if (meshBounds == null) return true;
            return x >= cachedBounds.min.x && x <= cachedBounds.max.x;
        }

        private bool IsInsideZ(float z)
        {
            if (meshBounds == null) return true;
            return z >= cachedBounds.min.z && z <= cachedBounds.max.z;
        }
    }
}
