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
        [SerializeField] private RawImage[] woodIcons;
        [SerializeField] private Text itemName;
        [SerializeField] private Text notice;
        public void ConfigureNotice(Text label) => notice=label;
        private int displayedRevision = -1;
        public void ConfigureWood(RawImage[] icons, Text label) { woodIcons = icons; itemName = label; }

        public int SlotCount => slotFrames?.Length ?? 0;

        public void Configure(LumberjackEquipment target, Image[] frames)
        {
            equipment = target;
            slotFrames = frames;
            Refresh(true);
        }

        private void Update()
        {
            Refresh(false);
            if(notice!=null && equipment!=null)notice.text=Time.unscaledTime<equipment.NoticeUntil?equipment.Notice:"";
        }

        private void Refresh(bool force)
        {
            if (equipment == null || slotFrames == null || (!force && displayedSlot == equipment.SelectedSlot && displayedRevision == equipment.InventoryRevision))
            {
                return;
            }

            displayedSlot = equipment.SelectedSlot;
            displayedRevision = equipment.InventoryRevision;
            if (itemName != null) itemName.text = equipment.ItemName(displayedSlot);
            if (woodIcons != null)
                for (int i = 0; i < woodIcons.Length; i++) if (woodIcons[i] != null) woodIcons[i].enabled = equipment.HasWood(i + 1);
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
