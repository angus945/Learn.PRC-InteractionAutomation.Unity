using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PointerScrollProbe :
    MonoBehaviour,
    IScrollHandler
{
    public int ScrollCount { get; private set; }
    public Vector2 LastDelta { get; private set; }

    public void ResetObservation()
    {
        ScrollCount = 0;
        LastDelta = Vector2.zero;
    }

    public void OnScroll(
        PointerEventData eventData)
    {
        ScrollCount++;
        LastDelta = eventData.scrollDelta;
    }
}
