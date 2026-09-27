using UnityEngine;
using UnityEngine.EventSystems;

public sealed class DropZoneProbe :
    MonoBehaviour,
    IDropHandler
{
    public int DropCount { get; private set; }

    public void ResetObservation()
    {
        DropCount = 0;
    }

    public void OnDrop(
        PointerEventData eventData)
    {
        DropCount++;
    }
}
