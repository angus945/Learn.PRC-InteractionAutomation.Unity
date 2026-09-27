using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using Module.InteractionAutomation.Availability;
using Module.InteractionAutomation.PhysicalInput;
using Module.InteractionAutomation.PhysicalInput.Unity3D;
using Module.InteractionAutomation.Observation.Unity3D;
using Module.InteractionAutomation.Runner;
using Module.InteractionAutomation.Runner.Unity3D;
using Module.InteractionAutomation.Targets;
using Module.Verification.Oracle;
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
    private UnityPointerDragObserver dragSourceProbe;

    [SerializeField]
    private UnityPointerDropObserver dropZoneProbe;

    [SerializeField]
    private UnityPointerHoverObserver pointerHoverProbe;

    [SerializeField]
    private UnityPointerClickObserver pointerClickCountProbe;

    [SerializeField]
    private UnityPointerPressObserver pointerHoldProbe;

    [SerializeField]
    private UnityPointerScrollObserver pointerScrollProbe;

    [SerializeField]
    private UnityFrameInputProcessingBoundary inputProcessingBoundary;

    private UnityPhysicalInputDriver physicalInput;
    private InteractionRunner runner;

    private IEnumerator Start()
    {
        if (!ValidateComposition())
            yield break;

        physicalInput =
            new UnityPhysicalInputDriver();

        runner =
            AutomationLabInteractionComposition
                .CreateRunner(
                    interactionCamera,
                    physicalInput,
                    inputProcessingBoundary);

        yield return null;

        buttonClickCounter.ResetCount();
        toggleStateProbe.ResetObservation(
            initialValue: false);

        Debug.Log("RUNNER BUTTON START");

        Task buttonClick =
            runner.ExecuteAsync(
                    new PointerClickRequest(
                        new InteractionTargetId(
                            "button.confirm")))
                .AsTask();

        yield return WaitFor(buttonClick);

        if (ReportFault(buttonClick))
            yield break;

        yield return null;

        if (buttonClickCounter.Count != 1)
        {
            Debug.LogError(
                "RUNNER BUTTON FAILED\n" +
                $"ClickCount={buttonClickCounter.Count}");
            yield break;
        }

        Debug.Log("RUNNER BUTTON PASS");

        Debug.Log("RUNNER TOGGLE START");

        Task toggleClick =
            runner.ExecuteAsync(
                    new PointerClickRequest(
                        new InteractionTargetId(
                            "toggle.music")))
                .AsTask();

        yield return WaitFor(toggleClick);

        if (ReportFault(toggleClick))
            yield break;

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

        Debug.Log("PHASE 9 RUNNER INTEGRATION PASS");

        dragSourceProbe.ResetObservation();
        dropZoneProbe.ResetObservation();

        Debug.Log("RUNNER DRAG START");

        Task drag =
            runner.ExecuteAsync(
                    new PointerDragRequest(
                        new InteractionTargetId(
                            "button.confirm"),
                        new InteractionTargetId(
                            "toggle.music")))
                .AsTask();

        yield return WaitFor(drag);

        if (ReportFault(drag))
            yield break;

        yield return null;

        var dragObservation =
            new DragObservation(
                dragSourceProbe.Capture(),
                dropZoneProbe.Capture());

        EvaluationReport dragVerification =
            DragVerificationProfile
                .Create()
                .Evaluate(
                    "button.confirm->toggle.music",
                    dragObservation);

        if (ReportVerificationFailure(
                "POINTER DRAG",
                dragVerification))
        {
            yield break;
        }

        Debug.Log(
            "POINTER DRAG PASS\n" +
            "Observation captured separately from Oracle judgment.");

        pointerScrollProbe.ResetObservation();

        Debug.Log("RUNNER SCROLL START");

        Task scroll =
            runner.ExecuteAsync(
                    new PointerScrollRequest(
                        new InteractionTargetId(
                            "toggle.music"),
                        new ScrollDelta(
                            horizontal: 0,
                            vertical: -1)))
                .AsTask();

        yield return WaitFor(scroll);

        if (ReportFault(scroll))
            yield break;

        yield return null;

        if (pointerScrollProbe.ScrollCount != 1)
        {
            Debug.LogError(
                "POINTER SCROLL FAILED\n" +
                $"ScrollCount={pointerScrollProbe.ScrollCount}\n" +
                $"LastDelta={pointerScrollProbe.LastDelta}");
            yield break;
        }

        Debug.Log(
            "POINTER SCROLL PASS\n" +
            $"Delta={pointerScrollProbe.LastDelta}");

        pointerHoverProbe.ResetObservation();

        Debug.Log("RUNNER HOVER START");

        Task hover =
            runner.ExecuteAsync(
                    new PointerHoverRequest(
                        new InteractionTargetId(
                            "button.confirm")))
                .AsTask();

        yield return WaitFor(hover);

        if (ReportFault(hover))
            yield break;

        yield return null;

        int mouseDeviceId =
            physicalInput.Mouse.deviceId;

        if (!pointerHoverProbe.IsHoveredByDevice(mouseDeviceId) ||
            pointerHoverProbe.GetEnterCount(mouseDeviceId) < 1)
        {
            Debug.LogError(
                "POINTER HOVER FAILED\n" +
                $"DeviceId={mouseDeviceId}\n" +
                $"EnterCount={pointerHoverProbe.GetEnterCount(mouseDeviceId)}");
            yield break;
        }

        Debug.Log("POINTER HOVER PASS");

        pointerClickCountProbe.ResetObservation();

        Debug.Log("RUNNER DOUBLE CLICK START");

        Task doubleClick =
            runner.ExecuteAsync(
                    new PointerDoubleClickRequest(
                        new InteractionTargetId(
                            "button.confirm")))
                .AsTask();

        yield return WaitFor(doubleClick);

        if (ReportFault(doubleClick))
            yield break;

        yield return null;

        if (pointerClickCountProbe.EventCount < 2 ||
            pointerClickCountProbe.MaxClickCount < 2)
        {
            Debug.LogError(
                "POINTER DOUBLE CLICK FAILED\n" +
                $"EventCount={pointerClickCountProbe.EventCount}\n" +
                $"LastClickCount={pointerClickCountProbe.LastClickCount}\n" +
                $"MaxClickCount={pointerClickCountProbe.MaxClickCount}");
            yield break;
        }

        Debug.Log("POINTER DOUBLE CLICK PASS");

        pointerHoldProbe.ResetObservation();

        Debug.Log("RUNNER HOLD START");

        Task hold =
            runner.ExecuteAsync(
                    new PointerHoldRequest(
                        new InteractionTargetId(
                            "button.confirm"),
                        TimeSpan.FromMilliseconds(200)))
                .AsTask();

        yield return WaitFor(hold);

        if (ReportFault(hold))
            yield break;

        yield return null;

        if (pointerHoldProbe.DownCount != 1 ||
            pointerHoldProbe.UpCount != 1 ||
            pointerHoldProbe.PressedFrameCount < 1 ||
            pointerHoldProbe.IsPressed)
        {
            Debug.LogError(
                "POINTER HOLD FAILED\n" +
                $"Down={pointerHoldProbe.DownCount}\n" +
                $"Up={pointerHoldProbe.UpCount}\n" +
                $"HeldFrames={pointerHoldProbe.PressedFrameCount}\n" +
                $"IsPressed={pointerHoldProbe.IsPressed}");
            yield break;
        }

        Debug.Log(
            "POINTER HOLD PASS\n" +
            $"HeldFrames={pointerHoldProbe.PressedFrameCount}");

        Debug.Log(
            "PHASE 10 POINTER EXECUTION PASS\n" +
            "Click / DoubleClick / Drag / Scroll / Hold / Hover\n" +
            "Normal Unity Input System / EventSystem route observed.");
    }

    private bool ValidateComposition()
    {
        if (buttonClickCounter == null ||
            toggleStateProbe == null ||
            dragSourceProbe == null ||
            dropZoneProbe == null ||
            pointerHoverProbe == null ||
            pointerClickCountProbe == null ||
            pointerHoldProbe == null ||
            pointerScrollProbe == null ||
            inputProcessingBoundary == null)
        {
            Debug.LogError(
                "InteractionRunnerProbe composition is incomplete.");
            return false;
        }

        return true;
    }

    private static IEnumerator WaitFor(
        Task task)
    {
        while (!task.IsCompleted)
            yield return null;
    }

    private static bool ReportFault(
        Task task)
    {
        if (!task.IsFaulted)
            return false;

        Debug.LogException(
            task.Exception?
                .GetBaseException());

        return true;
    }

    private static bool ReportVerificationFailure(
        string label,
        EvaluationReport report)
    {
        if (report.Verdict == TestVerdict.Passed)
            return false;

        var message =
            new StringBuilder();

        message
            .Append(label)
            .Append(" VERIFICATION FAILED\n")
            .Append("Verdict=")
            .Append(report.Verdict)
            .Append('\n');

        foreach (OracleResult result in report.Results)
        {
            message
                .Append(result.Code)
                .Append(": ")
                .Append(result.Verdict)
                .Append(" — ")
                .Append(result.Detail)
                .Append('\n');
        }

        foreach (EvaluationError error in report.Errors)
        {
            message
                .Append("OracleError ")
                .Append(error.OracleId)
                .Append(": ")
                .Append(error.ExceptionType)
                .Append(" — ")
                .Append(error.Message)
                .Append('\n');
        }

        Debug.LogError(
            message.ToString());

        return true;
    }

    private void OnDestroy()
    {
        physicalInput?.Dispose();
    }
}
