using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrystalSprint
{
    public sealed class GameInput : IDisposable
    {
        private readonly InputAction move;
        private readonly InputAction look;
        private readonly InputAction jump;
        private readonly InputAction sprint;
        private readonly InputAction restart;
        private readonly InputAction attack;
        private readonly InputAction selectSlot;
        private readonly InputAction interact;

        private static bool Gameplay => !MusicMenu.IsOpen && !MusicMenu.JustClosed && Time.timeScale > 0f;
        public Vector2 Move => Gameplay ? move.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => Gameplay ? look.ReadValue<Vector2>() : Vector2.zero;
        public bool LookUsesGamepad => look.activeControl?.device is Gamepad;
        public bool JumpPressed => Gameplay && jump.WasPressedThisFrame();
        public bool SprintHeld => Gameplay && sprint.IsPressed();
        public bool RestartPressed => Gameplay && restart.WasPressedThisFrame();
        public bool AttackPressed => Gameplay && attack.WasPressedThisFrame();
        public bool InteractPressed => Gameplay && interact.WasPressedThisFrame();
        public int SelectedSlotPressed
        {
            get
            {
                float value = Gameplay ? selectSlot.ReadValue<float>() : 0f;
                return value > 0.5f ? Mathf.RoundToInt(value) - 1 : -1;
            }
        }

        public GameInput()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");

            look = new InputAction("Look", InputActionType.Value);
            look.AddBinding("<Mouse>/delta");
            look.AddBinding("<Gamepad>/rightStick").WithProcessor("scaleVector2(x=16,y=16)");

            jump = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            sprint = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            sprint.AddBinding("<Keyboard>/rightShift");
            sprint.AddBinding("<Gamepad>/leftStickPress");

            restart = new InputAction("Restart", InputActionType.Button, "<Keyboard>/r");
            restart.AddBinding("<Gamepad>/start");

            attack = new InputAction("Attack", InputActionType.Button, "<Mouse>/leftButton");
            attack.AddBinding("<Gamepad>/rightTrigger");
            interact = new InputAction("Use", InputActionType.Button, "<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonWest");

            selectSlot = new InputAction("Select inventory slot", InputActionType.Button);
            selectSlot.AddBinding("<Keyboard>/1").WithProcessor("scale(factor=1)");
            selectSlot.AddBinding("<Keyboard>/2").WithProcessor("scale(factor=2)");
            selectSlot.AddBinding("<Keyboard>/3").WithProcessor("scale(factor=3)");
            selectSlot.AddBinding("<Keyboard>/4").WithProcessor("scale(factor=4)");
        }

        public void Enable()
        {
            move.Enable();
            look.Enable();
            jump.Enable();
            sprint.Enable();
            restart.Enable();
            attack.Enable();
            selectSlot.Enable();
            interact.Enable();
        }

        public void Disable()
        {
            move.Disable();
            look.Disable();
            jump.Disable();
            sprint.Disable();
            restart.Disable();
            attack.Disable();
            selectSlot.Disable();
            interact.Disable();
        }

        public void Dispose()
        {
            move.Dispose();
            look.Dispose();
            jump.Dispose();
            sprint.Dispose();
            restart.Dispose();
            attack.Dispose();
            selectSlot.Dispose();
            interact.Dispose();
        }
    }
}
