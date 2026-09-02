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
        private readonly bool[] woodSlots = new bool[4];
        public int InventoryRevision { get; private set; }
        public string Notice { get; private set; }
        public float NoticeUntil { get; private set; }
        public void ShowNotice(string text){Notice=text;NoticeUntil=Time.unscaledTime+3f;}
        public bool HasWood(int slot) => slot > 0 && slot < 4 && woodSlots[slot];
        public string ItemName(int slot) => slot == 0 ? "Axt" : HasWood(slot) ? "Holzbündel" : "Leer";
        public bool TryAddWoodBundle()
        {
            for (int slot = 1; slot < 4; slot++)
                if (!woodSlots[slot]) { woodSlots[slot] = true; InventoryRevision++; ShowNotice("Holzbündel erhalten"); return true; }
            ShowNotice("Inventar voll – Holzbündel bleibt liegen");
            return false;
        }

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
            if (MusicMenu.IsOpen || Time.timeScale <= 0 || SelectedSlot != 0 || characterVisual == null || !characterVisual.PlayAttack())
            {
                return false;
            }

            AttackCount++;
            return true;
        }
    }
}
