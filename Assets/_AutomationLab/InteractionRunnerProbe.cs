using System.Collections;
using System.Threading.Tasks;
using Module.InteractionAutomation.Availability;
using Module.InteractionAutomation.PhysicalInput.Unity3D;
using Module.InteractionAutomation.Runner;
using Module.InteractionAutomation.Runner.Unity3D;
using Module.InteractionAutomation.Targets;
using UnityEngine;

public sealed class InteractionRunnerProbe :
    MonoBehaviour
{
    [SerializeField]
    private Camera interactionCamera;

    [SerializeField]
    private ButtonClickCounter buttonClickCounter;

    [SerializeField]
    private ToggleStateProbe toggleStateProbe;

    [SerializeField]
    private DragSourceProbe dragSourceProbe;

    [SerializeField]
    private DropZoneProbe dropZoneProbe;

    [SerializeField]
    private UnityFrameInputProcessingBoundary inputProcessingBoundary;

    private UnityPhysicalInputDriver physicalInput;
    private InteractionRunner runner;

    private IEnumerator Start()
    {
        if (buttonClickCounter == null)
        {
            Debug.LogError(
                "ButtonClickCounter is not assigned.");

            yield break;
        }

        if (toggleStateProbe == null)
        {
            Debug.LogError(
                "ToggleStateProbe is not assigned.");

            yield break;
        }

        if (dragSourceProbe == null)
        {
            Debug.LogError(
                "DragSourceProbe is not assigned.");

            yield break;
        }

        if (dropZoneProbe == null)
        {
            Debug.LogError(
                "DropZoneProbe is not assigned.");

            yield break;
        }

        if (inputProcessingBoundary == null)
        {
            Debug.LogError(
                "UnityFrameInputProcessingBoundary is not assigned.");

            yield break;
        }

        physicalInput =
            new UnityPhysicalInputDriver();

        var targetSource =
            AutomationLabInteractionComposition
                .CreateTargetSource(
                    interactionCamera);

        var availability =
            new SnapshotInteractionAvailabilityEvaluator();

        runner =
            new InteractionRunner(
                targetSource,
                availability,
                physicalInput,
                inputProcessingBoundary);

        // Allow normal Unity runtime composition to settle.
        yield return null;

        buttonClickCounter.ResetCount();

        toggleStateProbe.ResetObservation(
            initialValue: false);

        Debug.Log(
            "RUNNER BUTTON START");

        Task buttonClick =
            runner
                .PointerClickAsync(
                    new InteractionTargetId(
                        "button.confirm"))
                .AsTask();

        while (!buttonClick.IsCompleted)
            yield return null;

        if (buttonClick.IsFaulted)
        {
            Debug.LogException(
                buttonClick.Exception?
                    .GetBaseException());

            yield break;
        }

        // Runner completion only means input submission.
        // Give Unity product processing a normal frame.
        yield return null;

        if (buttonClickCounter.Count != 1)
        {
            Debug.LogError(
                "RUNNER BUTTON FAILED\n" +
                $"Expected ClickCount=1\n" +
                $"Actual={buttonClickCounter.Count}");

            yield break;
        }

        Debug.Log(
            "RUNNER BUTTON PASS");

        Debug.Log(
            "RUNNER TOGGLE START");

        Task toggleClick =
            runner
                .PointerClickAsync(
                    new InteractionTargetId(
                        "toggle.music"))
                .AsTask();

        while (!toggleClick.IsCompleted)
            yield return null;

        if (toggleClick.IsFaulted)
        {
            Debug.LogException(
                toggleClick.Exception?
                    .GetBaseException());

            yield break;
        }

        yield return null;

        if (toggleStateProbe.ChangeCount != 1 ||
            !toggleStateProbe.LastValue)
        {
            Debug.LogError(
                "RUNNER TOGGLE FAILED\n" +
                $"ChangeCount={toggleStateProbe.ChangeCount}\n" +
                $"LastValue={toggleStateProbe.LastValue}");

            yield break;
        }

        Debug.Log(
            "PHASE 9 RUNNER INTEGRATION PASS\n" +
            "button.confirm -> available PointerClick PASS\n" +
            "toggle.music -> available PointerClick PASS");

        dragSourceProbe.ResetObservation();
        dropZoneProbe.ResetObservation();

        Debug.Log(
            "RUNNER DRAG START");

        Task drag =
            runner
                .PointerDragAsync(
                    new InteractionTargetId(
                        "button.confirm"),
                    new InteractionTargetId(
                        "toggle.music"))
                .AsTask();

        while (!drag.IsCompleted)
            yield return null;

        if (drag.IsFaulted)
        {
            Debug.LogException(
                drag.Exception?
                    .GetBaseException());

            yield break;
        }

        // Runner completion still means submission only.
        // Observation belongs after host/UI processing.
        yield return null;

        if (dragSourceProbe.BeginDragCount != 1 ||
            dragSourceProbe.DragCount < 1 ||
            dragSourceProbe.EndDragCount != 1 ||
            dropZoneProbe.DropCount != 1)
        {
            Debug.LogError(
                "PHASE 10 DRAG FAILED\n" +
                $"BeginDrag={dragSourceProbe.BeginDragCount}\n" +
                $"Drag={dragSourceProbe.DragCount}\n" +
                $"EndDrag={dragSourceProbe.EndDragCount}\n" +
                $"Drop={dropZoneProbe.DropCount}");

            yield break;
        }

        Debug.Log(
            "PHASE 10 DRAG PASS\n" +
            "button.confirm -> PointerDrag -> toggle.music\n" +
            "Normal Unity Input System / EventSystem route observed.");
    }

    private void OnDestroy()
    {
        physicalInput?.Dispose();
    }
}
