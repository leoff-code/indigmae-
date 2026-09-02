using UnityEngine;
using UnityEngine.UI;

namespace CrystalSprint
{
    public sealed class InventoryHud : MonoBehaviour
    {
        [SerializeField] private LumberjackEquipment equipment;
        [SerializeField] private Image[] slotFrames;
        [SerializeField] private Color selectedColor = new(0.96f, 0.76f, 0.24f, 0.98f);
        [SerializeField] private Color normalColor = new(0.09f, 0.12f, 0.15f, 0.88f);

        private int displayedSlot = -1;

        public int SlotCount => slotFrames?.Length ?? 0;

        public void Configure(LumberjackEquipment target, Image[] frames)
        {
            equipment = target;
            slotFrames = frames;
            Refresh(true);
        }

        private void Update() => Refresh(false);

        private void Refresh(bool force)
        {
            if (equipment == null || slotFrames == null || (!force && displayedSlot == equipment.SelectedSlot))
            {
                return;
            }

            displayedSlot = equipment.SelectedSlot;
            for (int index = 0; index < slotFrames.Length; index++)
            {
                if (slotFrames[index] != null)
                {
                    slotFrames[index].color = index == displayedSlot ? selectedColor : normalColor;
                }
            }
        }
    }
}
