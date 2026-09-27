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
using Module.InteractionAutomation.Verification.Unity3D;
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
    private UnityPointerDragObserver dragObserver;

    [SerializeField]
    private UnityPointerDropObserver dropObserver;

    [SerializeField]
    private UnityPointerHoverObserver pointerHoverObserver;

    [SerializeField]
    private UnityPointerClickObserver pointerClickObserver;

    [SerializeField]
    private UnityPointerPressObserver pointerPressObserver;

    [SerializeField]
    private UnityPointerScrollObserver pointerScrollObserver;

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

        EvaluationReport buttonVerification =
            AutoLabButtonClickVerificationProfile
                .Create()
                .Evaluate(
                    "button.confirm",
                    new AutoLabButtonClickObservation(
                        buttonClickCounter.Count));

        if (ReportVerificationFailure(
                "RUNNER BUTTON",
                buttonVerification))
        {
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

        EvaluationReport toggleVerification =
            AutoLabToggleVerificationProfile
                .Create()
                .Evaluate(
                    "toggle.music",
                    new AutoLabToggleObservation(
                        toggleStateProbe.ChangeCount,
                        toggleStateProbe.LastValue));

        if (ReportVerificationFailure(
                "RUNNER TOGGLE",
                toggleVerification))
        {
            yield break;
        }

        Debug.Log("PHASE 9 RUNNER INTEGRATION PASS");

        dragObserver.ResetObservation();
        dropObserver.ResetObservation();

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

        EvaluationReport dragLifecycleVerification =
            UnityPointerVerificationProfiles
                .DragLifecycle()
                .Evaluate(
                    "button.confirm->toggle.music",
                    dragObserver.Capture());

        if (ReportVerificationFailure(
                "POINTER DRAG LIFECYCLE",
                dragLifecycleVerification))
        {
            yield break;
        }

        EvaluationReport dropVerification =
            AutoLabDropVerificationProfile
                .Create()
                .Evaluate(
                    "button.confirm->toggle.music",
                    dropObserver.Capture());

        if (ReportVerificationFailure(
                "POINTER DRAG DROP",
                dropVerification))
        {
            yield break;
        }

        Debug.Log(
            "POINTER DRAG PASS\n" +
            "Unity host contract and AutoLab drop expectation passed.");

        pointerScrollObserver.ResetObservation();

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

        var scrollObservation =
            pointerScrollObserver.Capture();

        EvaluationReport scrollVerification =
            UnityPointerVerificationProfiles
                .ScrollObserved()
                .Evaluate(
                    "toggle.music",
                    scrollObservation);

        if (ReportVerificationFailure(
                "POINTER SCROLL",
                scrollVerification))
        {
            yield break;
        }

        Debug.Log(
            "POINTER SCROLL PASS\n" +
            $"Delta={scrollObservation.LastDelta}");

        pointerHoverObserver.ResetObservation();

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

        EvaluationReport hoverVerification =
            UnityPointerVerificationProfiles
                .HoverObserved()
                .Evaluate(
                    "button.confirm",
                    pointerHoverObserver.Capture(
                        mouseDeviceId));

        if (ReportVerificationFailure(
                "POINTER HOVER",
                hoverVerification))
        {
            yield break;
        }

        Debug.Log("POINTER HOVER PASS");

        pointerClickObserver.ResetObservation();

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

        EvaluationReport doubleClickVerification =
            UnityPointerVerificationProfiles
                .DoubleClick()
                .Evaluate(
                    "button.confirm",
                    pointerClickObserver.Capture());

        if (ReportVerificationFailure(
                "POINTER DOUBLE CLICK",
                doubleClickVerification))
        {
            yield break;
        }

        Debug.Log("POINTER DOUBLE CLICK PASS");

        pointerPressObserver.ResetObservation();

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

        var holdObservation =
            pointerPressObserver.Capture();

        EvaluationReport holdVerification =
            UnityPointerVerificationProfiles
                .HoldLifecycle()
                .Evaluate(
                    "button.confirm",
                    holdObservation);

        if (ReportVerificationFailure(
                "POINTER HOLD",
                holdVerification))
        {
            yield break;
        }

        Debug.Log(
            "POINTER HOLD PASS\n" +
            $"HeldFrames={holdObservation.PressedFrameCount}");

        Debug.Log(
            "PHASE 10 POINTER EXECUTION PASS\n" +
            "Click / DoubleClick / Drag / Scroll / Hold / Hover\n" +
            "Normal Unity Input System / EventSystem route observed.");
    }

    private bool ValidateComposition()
    {
        if (buttonClickCounter == null ||
            toggleStateProbe == null ||
            dragObserver == null ||
            dropObserver == null ||
            pointerHoverObserver == null ||
            pointerClickObserver == null ||
            pointerPressObserver == null ||
            pointerScrollObserver == null ||
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
