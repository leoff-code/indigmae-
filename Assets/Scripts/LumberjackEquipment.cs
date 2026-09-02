using UnityEngine;

namespace CrystalSprint
{
    public sealed class LumberjackEquipment : MonoBehaviour
    {
        [SerializeField] private LumberjackVisual characterVisual;
        [SerializeField] private GameObject axeVisual;

        private GameInput input;
        private CursorLockController cursorLock;

        public int SelectedSlot { get; private set; }
        public bool AxeEquipped => SelectedSlot == 0 && axeVisual != null && axeVisual.activeSelf;
        public int AttackCount { get; private set; }

        public void Configure(LumberjackVisual visual, GameObject axe)
        {
            characterVisual = visual;
            axeVisual = axe;
            SelectSlot(0);
        }

        private void Awake()
        {
            input = new GameInput();
            cursorLock = FindAnyObjectByType<CursorLockController>();
            SelectSlot(SelectedSlot);
        }

        private void OnEnable() => input?.Enable();

        private void Update()
        {
            int selected = input.SelectedSlotPressed;
            if (selected >= 0)
            {
                SelectSlot(selected);
            }

            if (input.AttackPressed && SelectedSlot == 0 &&
                (cursorLock == null || (cursorLock.IsLocked && !cursorLock.JustLockedThisFrame)))
            {
                TriggerAttack();
            }
        }

        private void OnDisable() => input?.Disable();

        private void OnDestroy() => input?.Dispose();

        public void SelectSlot(int slot)
        {
            SelectedSlot = Mathf.Clamp(slot, 0, 3);
            if (axeVisual != null)
            {
                axeVisual.SetActive(SelectedSlot == 0);
            }
        }

        public bool TriggerAttack()
        {
            if (SelectedSlot != 0 || characterVisual == null || !characterVisual.PlayAttack())
            {
                return false;
            }

            AttackCount++;
            return true;
        }
    }
}
