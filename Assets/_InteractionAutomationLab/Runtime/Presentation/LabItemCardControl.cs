using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.InteractionAutomationLab.Presentation
{
    public sealed class LabItemCardControl : Selectable, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private string itemId;
        private LabProductController controller;
        private GameObject dragGhost;

        public string ItemId => itemId;

        public void Configure(string configuredItemId)
        {
            itemId = configuredItemId;
        }

        protected override void Awake()
        {
            base.Awake();
            controller = GetComponentInParent<LabProductController>(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsInteractable()) return;
            controller.SelectItem(itemId);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsInteractable()) return;
            dragGhost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dragGhost.transform.SetParent(controller.DragPresentationRoot, false);
            Image image = dragGhost.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.45f);
            image.raycastTarget = false;
            RectTransform rect = dragGhost.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80f, 40f);
            rect.position = eventData.position;
            eventData.pointerDrag = gameObject;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragGhost != null) dragGhost.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragGhost != null) Destroy(dragGhost);
            dragGhost = null;
        }
    }
}
