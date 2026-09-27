using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.InteractionAutomationLab.Presentation
{
    public sealed class LabNonSelectableControl : MonoBehaviour, IPointerClickHandler
    {
        private LabProductController controller;

        private void Awake()
        {
            controller = GetComponentInParent<LabProductController>(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            controller.ActivateCanary();
        }
    }
}
