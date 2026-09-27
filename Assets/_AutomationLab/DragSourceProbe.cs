using UnityEngine;
using UnityEngine.EventSystems;

public sealed class DragSourceProbe :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    public int BeginDragCount { get; private set; }
    public int DragCount { get; private set; }
    public int EndDragCount { get; private set; }

    public void ResetObservation()
    {
        BeginDragCount = 0;
        DragCount = 0;
        EndDragCount = 0;
    }

    public void OnBeginDrag(
        PointerEventData eventData)
    {
        BeginDragCount++;
    }

    public void OnDrag(
        PointerEventData eventData)
    {
        DragCount++;
    }

    public void OnEndDrag(
        PointerEventData eventData)
    {
        EndDragCount++;
    }
}
