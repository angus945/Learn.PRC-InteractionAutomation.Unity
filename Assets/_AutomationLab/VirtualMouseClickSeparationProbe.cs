using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Module.InteractionAutomation.Coordinates;
using Module.InteractionAutomation.Coordinates.Unity3D;
using Module.InteractionAutomation.PhysicalInput;
using Module.InteractionAutomation.PhysicalInput.Unity3D;
using Module.InteractionAutomation.Targets;
using Module.InteractionAutomation.Targets.Unity3D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public sealed class VirtualMouseClickSeparationProbe :
    MonoBehaviour
{
    private const string TargetId = "button.confirm";

    private const float TimeoutSeconds = 3f;
    private const float PositionTolerance = 0.01f;

    [SerializeField]
    private ButtonClickCounter clickCounter;

    [SerializeField]
    private PointerHoverProbe hoverProbe;

    private UnityPhysicalInputDriver input;

    private IEnumerator Start()
    {
        input =
            new UnityPhysicalInputDriver();

        //
        // Allow normal runtime composition to settle.
        //
        yield return null;

        if (!ValidateEnvironment())
            yield break;

        InteractionPoint targetCenter = default;

        //
        // --------------------------------------------
        // Discover target
        // --------------------------------------------
        //

        var source =
            AutomationLabInteractionComposition.CreateTargetSource();

        Task<IReadOnlyList<InteractionTargetSnapshot>> capture =
            source
                .GetTargetsAsync()
                .AsTask();

        while (!capture.IsCompleted)
            yield return null;

        if (capture.IsFaulted)
        {
            Debug.LogException(
                capture.Exception?.GetBaseException());

            yield break;
        }

        InteractionTargetSnapshot? selected =
            capture
                .GetAwaiter()
                .GetResult()
                .Where(target =>
                    target.Id.Value == TargetId)
                .Cast<InteractionTargetSnapshot?>()
                .FirstOrDefault();

        if (!selected.HasValue)
        {
            Debug.LogError(
                $"Target '{TargetId}' was not discovered.");

            yield break;
        }

        InteractionTargetSnapshot target =
            selected.Value;

        if (target.Bounds.Space !=
            InteractionCoordinateSpace.ApplicationSpace)
        {
            Debug.LogError(
                $"Target '{TargetId}' must expose ApplicationSpace bounds. " +
                $"Actual={target.Bounds.Space}");

            yield break;
        }

        targetCenter =
            target.Bounds.Center;

        Debug.Log(
            "CLICK EXPERIMENT TARGET\n" +
            $"Id={target.Id}\n" +
            $"Center=({targetCenter.X}, {targetCenter.Y})\n" +
            $"VirtualMouseDeviceId={input.Mouse.deviceId}");

        //
        // ============================================
        // EXPERIMENT A
        // NO PROCESSING SEPARATION
        // ============================================
        //

        Debug.Log(
            "EXPERIMENT A START\n" +
            "Move -> Down -> Up\n" +
            "No processing separation.");

        clickCounter.ResetCount();
        hoverProbe.ResetObservation();

        input.MovePointerAsync(
            targetCenter);

        input.PointerDownAsync(
            PointerButton.Left);

        input.PointerUpAsync(
            PointerButton.Left);

        //
        // Let Unity process whatever was queued.
        //
        yield return null;
        yield return null;

        int experimentAClickCount =
            clickCounter.Count;

        Vector2 experimentAPosition =
            input.Mouse.position.ReadValue();

        Debug.Log(
            "EXPERIMENT A RESULT\n" +
            $"ClickCount={experimentAClickCount}\n" +
            $"MousePosition={experimentAPosition}\n" +
            $"LeftPressed={input.Mouse.leftButton.isPressed}\n" +
            $"VirtualHover=" +
            $"{hoverProbe.IsHoveredByDevice(input.Mouse.deviceId)}");

        //
        // --------------------------------------------
        // Reset between experiments
        // --------------------------------------------
        //

        InteractionPoint resetPoint =
            InteractionPoint.FromApplicationTopLeft(
                20,
                20);

        input.PointerUpAsync(
            PointerButton.Left);

        yield return WaitForButtonState(
            false);

        input.MovePointerAsync(
            resetPoint);

        yield return WaitForPosition(
            resetPoint);

        //
        // Give EventSystem another normal cycle so
        // experiment B starts from a clean state.
        //
        yield return null;

        clickCounter.ResetCount();
        hoverProbe.ResetObservation();

        //
        // ============================================
        // EXPERIMENT B
        // EXPLICIT SEPARATION
        // ============================================
        //

        Debug.Log(
            "EXPERIMENT B START\n" +
            "Move -> settle -> Down -> settle -> Up -> settle.");

        //
        // Move
        //

        input.MovePointerAsync(
            targetCenter);

        yield return WaitForPosition(
            targetCenter);

        //
        // Let EventSystem observe hover.
        //
        yield return null;

        if (!hoverProbe.IsHoveredByDevice(
                input.Mouse.deviceId))
        {
            Debug.LogError(
                "EXPERIMENT B FAILED BEFORE DOWN\n" +
                "Virtual mouse reached the target position, " +
                "but EventSystem did not observe its hover.");

            yield break;
        }

        //
        // Down
        //

        input.PointerDownAsync(
            PointerButton.Left);

        yield return WaitForButtonState(
            true);

        //
        // Let EventSystem observe pressed state.
        //
        yield return null;

        //
        // Up
        //

        input.PointerUpAsync(
            PointerButton.Left);

        yield return WaitForButtonState(
            false);

        //
        // Let EventSystem observe release / click.
        //
        yield return null;

        int experimentBClickCount =
            clickCounter.Count;

        Vector2 experimentBPosition =
            input.Mouse.position.ReadValue();

        Debug.Log(
            "EXPERIMENT B RESULT\n" +
            $"ClickCount={experimentBClickCount}\n" +
            $"MousePosition={experimentBPosition}\n" +
            $"LeftPressed={input.Mouse.leftButton.isPressed}\n" +
            $"VirtualHover=" +
            $"{hoverProbe.IsHoveredByDevice(input.Mouse.deviceId)}");

        //
        // ============================================
        // EXPERIMENT C
        // INPUT SYSTEM SEPARATION ONLY
        // ============================================
        //

        InteractionPoint resetPointC =
            InteractionPoint.FromApplicationTopLeft(
                20,
                20);

        input.PointerUpAsync(
            PointerButton.Left);

        yield return WaitForButtonState(false);

        input.MovePointerAsync(
            resetPointC);

        yield return WaitForPosition(
            resetPointC);

        // 只在實驗開始前整理狀態。
        yield return null;

        clickCounter.ResetCount();
        hoverProbe.ResetObservation();

        Debug.Log(
            "EXPERIMENT C START\n" +
            "Move -> InputSystem processed -> " +
            "Down -> InputSystem processed -> " +
            "Up -> InputSystem processed.\n" +
            "No EventSystem frame separation between operations.");

        //
        // Move
        //

        input.MovePointerAsync(
            targetCenter);

        yield return WaitForPosition(
            targetCenter);

        //
        // 注意：這裡沒有 yield return null
        //

        //
        // Down
        //

        input.PointerDownAsync(
            PointerButton.Left);

        yield return WaitForButtonState(
            true);

        //
        // 注意：這裡也沒有 yield return null
        //

        //
        // Up
        //

        input.PointerUpAsync(
            PointerButton.Left);

        yield return WaitForButtonState(
            false);

        //
        // 現在才讓 EventSystem 有機會跑。
        // 
        yield return null;

        int experimentCClickCount =
            clickCounter.Count;

        Debug.Log(
            "EXPERIMENT C RESULT\n" +
            $"ClickCount={experimentCClickCount}\n" +
            $"MousePosition={input.Mouse.position.ReadValue()}\n" +
            $"LeftPressed={input.Mouse.leftButton.isPressed}\n" +
            $"VirtualHover=" +
            $"{hoverProbe.IsHoveredByDevice(input.Mouse.deviceId)}");

        //
        // ============================================
        // Interpretation
        // ============================================
        //

        Debug.Log(
            "CLICK SEPARATION EXPERIMENT COMPLETE\n" +
            $"A_NoSeparation={experimentAClickCount}\n" +
            $"B_InputAndEventSystemSeparation={experimentBClickCount}\n" +
            $"C_InputSystemSeparationOnly={experimentCClickCount}");

        if (experimentAClickCount > 0 &&
            experimentBClickCount > 0 &&
            experimentCClickCount > 0)
        {
            Debug.Log(
                "RESULT: A / B / C PASSED\n" +
                "The submitted-state driver preserves sequential Move / Down / Up " +
                "without requiring an explicit host-processing barrier.");
        }
        else
        {
            Debug.LogError(
                "RESULT: CLICK SEPARATION REGRESSION\n" +
                $"A_NoSeparation={experimentAClickCount}\n" +
                $"B_InputAndEventSystemSeparation={experimentBClickCount}\n" +
                $"C_InputSystemSeparationOnly={experimentCClickCount}\n" +
                "At least one previously verified interaction route no longer passes.");
        }
    }

    private IEnumerator WaitForPosition(
        InteractionPoint expected)
    {
        float startedAt =
            Time.realtimeSinceStartup;

        while (true)
        {
            Vector2 unityPosition =
                input.Mouse.position.ReadValue();

            InteractionPoint actual =
                UnityApplicationCoordinates.ToApplication(
                    unityPosition,
                    Screen.height);

            double dx =
                actual.X - expected.X;

            double dy =
                actual.Y - expected.Y;

            double distanceSquared =
                dx * dx +
                dy * dy;

            if (distanceSquared <=
                PositionTolerance * PositionTolerance)
            {
                yield break;
            }

            if (Time.realtimeSinceStartup -
                startedAt >= TimeoutSeconds)
            {
                Debug.LogError(
                    "Timed out waiting for virtual mouse position.\n" +
                    $"Expected=({expected.X}, {expected.Y})\n" +
                    $"Actual=({actual.X}, {actual.Y})");

                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator WaitForButtonState(
        bool expectedPressed)
    {
        float startedAt =
            Time.realtimeSinceStartup;

        while (input.Mouse.leftButton.isPressed !=
               expectedPressed)
        {
            if (Time.realtimeSinceStartup -
                startedAt >= TimeoutSeconds)
            {
                Debug.LogError(
                    "Timed out waiting for left button state.\n" +
                    $"ExpectedPressed={expectedPressed}\n" +
                    $"ActualPressed=" +
                    $"{input.Mouse.leftButton.isPressed}");

                yield break;
            }

            yield return null;
        }
    }

    private bool ValidateEnvironment()
    {
        if (clickCounter == null)
        {
            Debug.LogError(
                "ButtonClickCounter is not assigned.");

            return false;
        }

        if (hoverProbe == null)
        {
            Debug.LogError(
                "PointerHoverProbe is not assigned.");

            return false;
        }

        if (!input.Mouse.added)
        {
            Debug.LogError(
                "Virtual mouse is not registered.");

            return false;
        }

        if (!input.Mouse.enabled)
        {
            Debug.LogError(
                "Virtual mouse is disabled.");

            return false;
        }

        EventSystem eventSystem =
            EventSystem.current;

        if (eventSystem == null)
        {
            Debug.LogError(
                "No EventSystem exists.");

            return false;
        }

        InputSystemUIInputModule module =
            eventSystem.GetComponent<
                InputSystemUIInputModule>();

        if (module == null)
        {
            Debug.LogError(
                "EventSystem must use InputSystemUIInputModule.");

            return false;
        }

        if (module.pointerBehavior !=
            UIPointerBehavior.AllPointersAsIs)
        {
            Debug.LogError(
                "Pointer Behavior must be AllPointersAsIs.\n" +
                $"Current={module.pointerBehavior}");

            return false;
        }

        return true;
    }

    private void OnDestroy()
    {
        input?.Dispose();
    }
}