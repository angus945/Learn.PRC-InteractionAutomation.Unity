using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.InteractionAutomationLab.Presentation
{
    public sealed class LabEquipmentSlotControl : Selectable, IDropHandler
    {
        [SerializeField] private string slotId;
        private LabProductController controller;

        public string SlotId => slotId;

        public void Configure(string configuredSlotId)
        {
            slotId = configuredSlotId;
        }

        protected override void Awake()
        {
            base.Awake();
            controller = GetComponentInParent<LabProductController>(true);
        }

        public void OnDrop(PointerEventData eventData)
        {
            LabItemCardControl item = eventData.pointerDrag == null ? null : eventData.pointerDrag.GetComponent<LabItemCardControl>();
            if (item == null) return;
            controller.TryEquip(item.ItemId, slotId);
        }
    }
}
