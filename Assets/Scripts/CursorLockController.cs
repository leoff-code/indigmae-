using UnityEngine;
using UnityEngine.InputSystem;

namespace CrystalSprint
{
    [DefaultExecutionOrder(-1000)]
    public sealed class CursorLockController : MonoBehaviour
    {
        public bool IsLocked { get; private set; }
        public bool JustLockedThisFrame { get; private set; }

        private void Awake() => LockCursor();

        private void Update()
        {
            JustLockedThisFrame = false;
            bool escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            bool mouseClickPressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            ProcessInput(escapePressed, mouseClickPressed);
        }

        private void ProcessInput(bool escapePressed, bool mouseClickPressed)
        {
            if (escapePressed)
            {
                ReleaseCursor();
                return;
            }

            if (!IsLocked && mouseClickPressed)
            {
                LockCursor();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                ReleaseCursor();
            }
        }

        private void OnDisable() => ReleaseCursor();

        public void LockCursor()
        {
            IsLocked = true;
            JustLockedThisFrame = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void ReleaseCursor()
        {
            IsLocked = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
