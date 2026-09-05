using Claw;
using UnityEngine;

/// <summary>
/// Temporary test-only input bridge. This is NOT part of the canonical source layer.
/// It exists only to exercise Gate A/B/C before the original input module is mapped.
/// </summary>
public sealed class ReferenceCloneKeyboardDriver : MonoBehaviour
{
    [SerializeField] private ClawMachine machine;

    private Vector2 move;

    private void Update()
    {
        if (machine == null) return;

        move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (!Input.GetKeyDown(KeyCode.Space)) return;

        if (machine.GetCurState() == ClawMachineState.idle)
            machine.BeginRound();
        else if (machine.GetCurState() == ClawMachineState.running)
            machine.BeginGrab();
    }

    private void FixedUpdate()
    {
        if (machine == null || machine.GetCurState() != ClawMachineState.running) return;

        ClawMoveModule mover = machine.GetClawMoveModule();
        if (mover != null)
            mover.MoveClaw(move);
    }
}
