using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public sealed class PointerHoverProbe :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    private readonly HashSet<int> hoveredDeviceIds = new();
    private readonly Dictionary<int, int> enterCounts = new();

    public void ResetObservation()
    {
        hoveredDeviceIds.Clear();
        enterCounts.Clear();
    }

    public bool IsHoveredByDevice(int deviceId)
    {
        return hoveredDeviceIds.Contains(deviceId);
    }

    public int GetEnterCount(int deviceId)
    {
        return enterCounts.TryGetValue(
            deviceId,
            out int count)
            ? count
            : 0;
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        int deviceId =
            GetDeviceId(eventData);

        hoveredDeviceIds.Add(deviceId);

        enterCounts.TryGetValue(
            deviceId,
            out int count);

        enterCounts[deviceId] =
            count + 1;

        Debug.Log(
            "EVENTSYSTEM POINTER ENTER\n" +
            $"Target={name}\n" +
            $"PointerId={eventData.pointerId}\n" +
            $"DeviceId={deviceId}\n" +
            $"Position={eventData.position}\n" +
            $"RaycastTarget=" +
            $"{eventData.pointerCurrentRaycast.gameObject?.name ?? "<null>"}");
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        int deviceId =
            GetDeviceId(eventData);

        hoveredDeviceIds.Remove(deviceId);

        Debug.Log(
            "EVENTSYSTEM POINTER EXIT\n" +
            $"Target={name}\n" +
            $"PointerId={eventData.pointerId}\n" +
            $"DeviceId={deviceId}\n" +
            $"Position={eventData.position}");
    }

    private static int GetDeviceId(
        PointerEventData eventData)
    {
        if (eventData is ExtendedPointerEventData extended &&
            extended.device != null)
        {
            return extended.device.deviceId;
        }

        return -1;
    }
}