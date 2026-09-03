using Claw3D.Claw;
using Claw3D.Input;
using UnityEngine;

namespace Claw3D.Machine
{
    public sealed class MachineController : MonoBehaviour
    {
        [SerializeField] private ClawInput input;
        [SerializeField] private ClawController claw;
        [SerializeField] private MachineState state = MachineState.Aim;

        public void Configure(ClawInput clawInput, ClawController clawController)
        {
            input = clawInput;
            claw = clawController;
        }

        private void Update()
        {
            if (input == null || claw == null) return;

            if (input.DropPressed)
            {
                state = state == MachineState.Aim ? MachineState.Grip : MachineState.Aim;
                claw.SetGrip(state == MachineState.Grip);
            }
        }

        private void FixedUpdate()
        {
            if (state == MachineState.Aim && input != null && claw != null)
                claw.Move(input.Move);
        }
    }
}
