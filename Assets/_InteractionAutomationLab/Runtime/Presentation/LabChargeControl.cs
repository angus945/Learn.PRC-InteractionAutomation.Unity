using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.InteractionAutomationLab.Presentation
{
    public sealed class LabChargeControl : Selectable, IPointerDownHandler, IPointerUpHandler
    {
        private const float ChargeThresholdSeconds = 0.15f;
        private LabProductController controller;
        private float pressedAt;
        private bool tracking;

        protected override void Awake()
        {
            base.Awake();
            controller = GetComponentInParent<LabProductController>(true);
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (!IsInteractable()) return;
            pressedAt = Time.realtimeSinceStartup;
            tracking = true;
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            if (!tracking) return;
            float duration = Time.realtimeSinceStartup - pressedAt;
            tracking = false;
            if (duration >= ChargeThresholdSeconds) controller.CompleteCharge();
        }

        protected override void OnDisable()
        {
            tracking = false;
            base.OnDisable();
        }
    }
}
