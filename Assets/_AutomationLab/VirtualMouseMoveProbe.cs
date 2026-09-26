using System.Threading.Tasks;
using Module.InteractionAutomation.Coordinates;
using Module.InteractionAutomation.PhysicalInput.Unity3D;
using UnityEngine;

public sealed class VirtualMouseMoveProbe : MonoBehaviour
{
    private UnityPhysicalInputDriver driver;

    private async void Start()
    {
        driver = new UnityPhysicalInputDriver();

        InteractionPoint target = InteractionPoint.FromScreenTopLeft(500, 300);

        Debug.Log($"Canonical requested: ({target.X}, {target.Y})");

        await driver.MovePointerAsync(target);

        Debug.Log("MovePointerAsync completed.");

        Vector2 unityPosition = driver.Mouse.position.ReadValue();

        Debug.Log($"Unity mouse state: ({unityPosition.x}, {unityPosition.y})");

        InteractionPoint canonicalObserved =
            Module.InteractionAutomation.Coordinates.Unity3D
                .UnityScreenCoordinates.ToCanonical(
                    unityPosition,
                    Screen.height);

        Debug.Log($"Canonical observed: ({canonicalObserved.X}, {canonicalObserved.Y})");
    }

    private void OnDestroy()
    {
        driver?.Dispose();
    }
}