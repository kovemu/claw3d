using UnityEngine;
using UnityEngine.InputSystem;

namespace Claw3D.Input
{
    public sealed class ClawInput : MonoBehaviour
    {
        public Vector2 Move { get; private set; }
        public bool DropPressed { get; private set; }

        private void Update()
        {
            Move = Vector2.zero;
            DropPressed = false;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            float x = 0f;
            float y = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;

            Move = Vector2.ClampMagnitude(new Vector2(x, y), 1f);
            DropPressed = keyboard.spaceKey.wasPressedThisFrame;
        }
    }
}
