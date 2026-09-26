using System.Collections;
using System.Threading.Tasks;
using Module.InteractionAutomation.Coordinates;
using Module.InteractionAutomation.Coordinates.Unity3D;
using Module.InteractionAutomation.PhysicalInput.Unity3D;
using UnityEngine;

public sealed class VirtualMouseMoveProbe : MonoBehaviour
{
    private UnityPhysicalInputDriver driver;

    private IEnumerator Start()
    {
        driver = new UnityPhysicalInputDriver();

        InteractionPoint target =
            InteractionPoint.FromScreenTopLeft(500, 300);

        Debug.Log(
            $"Requested canonical position: ({target.X}, {target.Y})");

        ValueTask operation =
            driver.MovePointerAsync(target);

        Debug.Log(
            $"Submission completed: {operation.IsCompleted}");

        Vector2 immediate =
            driver.Mouse.position.ReadValue();

        Debug.Log(
            $"Immediately observed Unity state: ({immediate.x}, {immediate.y})");

        yield return null;

        Vector2 processed =
            driver.Mouse.position.ReadValue();

        Debug.Log(
            $"Observed after Unity lifecycle progressed: ({processed.x}, {processed.y})");

        InteractionPoint canonicalObserved =
            UnityScreenCoordinates.ToCanonical(
                processed,
                Screen.height);

        Debug.Log(
            $"Canonical observed after processing: " +
            $"({canonicalObserved.X}, {canonicalObserved.Y})");
    }

    private void OnDestroy()
    {
        driver?.Dispose();
    }
}
