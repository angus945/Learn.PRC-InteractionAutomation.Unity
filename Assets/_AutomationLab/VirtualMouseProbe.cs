using Module.InteractionAutomation.PhysicalInput.Unity3D;
using UnityEngine;

public sealed class VirtualMouseProbe : MonoBehaviour
{
    private UnityPhysicalInputDriver driver;

    private void Start()
    {
        driver = new UnityPhysicalInputDriver();

        Debug.Log(
            $"Virtual Mouse created: " +
            $"name={driver.Mouse.name}, " +
            $"deviceId={driver.Mouse.deviceId}, " +
            $"added={driver.Mouse.added}");
    }

    private void OnDestroy()
    {
        driver?.Dispose();
    }
}