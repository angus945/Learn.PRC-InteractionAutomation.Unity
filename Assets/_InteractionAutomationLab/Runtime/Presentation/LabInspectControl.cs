using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.InteractionAutomationLab.Presentation
{
    public sealed class LabInspectControl : Selectable, IPointerClickHandler
    {
        private LabProductController controller;

        protected override void Awake()
        {
            base.Awake();
            controller = GetComponentInParent<LabProductController>(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsInteractable() || eventData.clickCount < 2) return;
            controller.InspectSelected();
        }
    }
}
