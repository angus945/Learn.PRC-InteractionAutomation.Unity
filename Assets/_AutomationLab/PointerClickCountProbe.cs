using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PointerClickCountProbe :
    MonoBehaviour,
    IPointerClickHandler
{
    public int EventCount { get; private set; }
    public int LastClickCount { get; private set; }
    public int MaxClickCount { get; private set; }

    public void ResetObservation()
    {
        EventCount = 0;
        LastClickCount = 0;
        MaxClickCount = 0;
    }

    public void OnPointerClick(
        PointerEventData eventData)
    {
        EventCount++;
        LastClickCount = eventData.clickCount;

        if (eventData.clickCount > MaxClickCount)
            MaxClickCount = eventData.clickCount;
    }
}
