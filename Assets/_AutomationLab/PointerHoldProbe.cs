using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PointerHoldProbe :
    MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    public int DownCount { get; private set; }
    public int UpCount { get; private set; }
    public int HeldFrameCount { get; private set; }
    public bool IsPressed { get; private set; }

    public void ResetObservation()
    {
        DownCount = 0;
        UpCount = 0;
        HeldFrameCount = 0;
        IsPressed = false;
    }

    private void Update()
    {
        if (IsPressed)
            HeldFrameCount++;
    }

    public void OnPointerDown(
        PointerEventData eventData)
    {
        DownCount++;
        IsPressed = true;
    }

    public void OnPointerUp(
        PointerEventData eventData)
    {
        UpCount++;
        IsPressed = false;
    }
}
