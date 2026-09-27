using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using Module.InteractionAutomation.Availability;
using Module.InteractionAutomation.Monkey;
using Module.InteractionAutomation.Monkey.Unity3D;
using Module.InteractionAutomation.Observation.Unity3D;
using Module.InteractionAutomation.PhysicalInput;
using Module.InteractionAutomation.PhysicalInput.Unity3D;
using Module.InteractionAutomation.Runner;
using Module.InteractionAutomation.Runner.Unity3D;
using Module.Verification.Oracle;
using UnityEngine;

public sealed class AutoLabSeededPointerMonkeyProbe :
    MonoBehaviour
{
    [SerializeField]
    private Camera interactionCamera;

    [SerializeField]
    private UnityFrameInputProcessingBoundary inputProcessingBoundary;

    [SerializeField]
    private AutoLabMonkeyPresentation presentation;

    [SerializeField]
    private UnityPointerDropObserver dragDropObserver;

    [SerializeField]
    private int seed = 12345;

    [SerializeField]
    private int iterationCount = 25;

    [SerializeField]
    private float holdDurationMilliseconds = 200f;

    [SerializeField]
    private float scrollHorizontal;

    [SerializeField]
    private float scrollVertical = -1f;

    [SerializeField]
    private float selectionPreviewSeconds = 0.2f;

    [SerializeField]
    private float completedStepPauseSeconds = 0.15f;

    private UnityPhysicalInputDriver physicalInput;

    private IEnumerator Start()
    {
        if (!ValidateConfiguration())
            yield break;

        physicalInput =
            new UnityPhysicalInputDriver();

        presentation.Bind(
            physicalInput.Mouse);

        presentation.BeginRun(
            seed,
            iterationCount);

        InteractionRunner runner =
            AutomationLabInteractionComposition
                .CreateRunner(
                    interactionCamera,
                    physicalInput,
                    inputProcessingBoundary);

        var targetSource =
            AutomationLabInteractionComposition
                .CreateTargetSource(
                    interactionCamera);

        var monkey =
            new SeededPointerMonkey(
                targetSource,
                new SnapshotInteractionAvailabilityEvaluator(),
                runner,
                inputProcessingBoundary,
                new PointerMonkeyConfiguration(
                    seed,
                    new ScrollDelta(
                        scrollHorizontal,
                        scrollVertical),
                    TimeSpan.FromMilliseconds(
                        holdDurationMilliseconds)),
                new AutoLabMonkeyDragDestinationPolicy());

        UnityPointerMonkeyVerifier verifier =
            UnityPointerMonkeyVerifier
                .FromLoadedScene(
                    physicalInput.Mouse.deviceId);

        var recorder =
            new UnityPointerMonkeyEvidenceRecorder(
                seed,
                iterationCount,
                traceCapacity:
                    Math.Max(
                        iterationCount,
                        32));

        yield return null;

        Debug.Log(
            "P12 SEEDED POINTER MONKEY QA START\n" +
            $"Seed={seed}\n" +
            $"Iterations={iterationCount}\n" +
            "Actions=Click / DoubleClick / Drag / Scroll / Hold / Hover\n" +
            "Policy=coverage-first deterministic selection");

        bool stopped =
            false;

        for (int sequence = 1;
             sequence <= iterationCount;
             sequence++)
        {
            Task<PointerMonkeyStep> selection =
                monkey
                    .SelectNextAsync(sequence)
                    .AsTask();

            yield return WaitFor(
                selection);

            if (selection.IsCanceled ||
                selection.IsFaulted)
            {
                string detail =
                    GetFailureDetail(
                        selection);

                recorder.RecordRunInfrastructureFailure(
                    $"Selection failed at iteration {sequence}: {detail}");

                presentation.FailSelection(
                    sequence,
                    detail);

                ReportTaskFault(
                    "MONKEY SELECTION",
                    selection,
                    sequence,
                    default);

                stopped = true;
                break;
            }

            PointerMonkeyStep step =
                selection
                    .GetAwaiter()
                    .GetResult();

            presentation.SelectStep(
                step);

            presentation.UpdateCoverage(
                monkey.GetCoverageSnapshot());

            Debug.Log(
                "MONKEY STEP SELECTED\n" +
                $"Seed={seed}\n" +
                $"Step={step}\n" +
                $"CoverageKey={step.CoverageKey}");

            try
            {
                verifier.ResetObservation(
                    step);

                AutoLabMonkeyVerification
                    .ResetScenarioObservation(
                        step,
                        dragDropObserver);
            }
            catch (Exception exception)
            {
                PointerMonkeyCoverageSnapshot coverage =
                    monkey.GetCoverageSnapshot();

                UnityPointerMonkeyStepResult result =
                    recorder.RecordInfrastructureFailure(
                        step,
                        coverage,
                        exception);

                presentation.FailStep(
                    step,
                    FormatVerification(
                        result.Verification));

                stopped = true;
                break;
            }

            if (selectionPreviewSeconds > 0)
            {
                yield return
                    new WaitForSecondsRealtime(
                        selectionPreviewSeconds);
            }

            presentation.BeginStep(
                step);

            Task execution =
                monkey
                    .ExecuteAsync(step)
                    .AsTask();

            yield return WaitFor(
                execution);

            if (execution.IsCanceled ||
                execution.IsFaulted)
            {
                Exception exception =
                    GetFailureException(
                        execution);

                PointerMonkeyCoverageSnapshot coverage =
                    monkey.GetCoverageSnapshot();

                UnityPointerMonkeyStepResult result =
                    recorder.RecordInfrastructureFailure(
                        step,
                        coverage,
                        exception);

                presentation.FailStep(
                    step,
                    FormatVerification(
                        result.Verification));

                ReportTaskFault(
                    "MONKEY EXECUTION",
                    execution,
                    sequence,
                    step);

                stopped = true;
                break;
            }

            EvaluationReport verification =
                verifier.Evaluate(
                    step);

            verification =
                AutoLabMonkeyVerification
                    .AddScenarioVerification(
                        step,
                        verification,
                        dragDropObserver);

            PointerMonkeyCoverageSnapshot stepCoverage =
                monkey.GetCoverageSnapshot();

            UnityPointerMonkeyStepResult stepResult =
                recorder.RecordVerification(
                    step,
                    stepCoverage,
                    verification);

            presentation.UpdateCoverage(
                stepCoverage);

            if (stepResult.Verdict !=
                TestVerdict.Passed)
            {
                presentation.FailStep(
                    step,
                    FormatVerification(
                        verification));

                Debug.LogError(
                    "MONKEY VERIFICATION FAILED\n" +
                    $"Reproduction={stepResult.ReproductionKey}\n" +
                    FormatVerification(
                        verification));

                stopped = true;
                break;
            }

            presentation.CompleteStep(
                step,
                verification);

            if (completedStepPauseSeconds > 0)
            {
                yield return
                    new WaitForSecondsRealtime(
                        completedStepPauseSeconds);
            }
        }

        PointerMonkeyCoverageSnapshot finalCoverage =
            monkey.GetCoverageSnapshot();

        UnityPointerMonkeyRunReport runReport =
            recorder.Build(
                finalCoverage);

        presentation.UpdateCoverage(
            finalCoverage);

        presentation.CompleteRun(
            runReport);

        if (!runReport.Passed)
        {
            Debug.LogError(
                "P12 SEEDED POINTER MONKEY QA FAILED\n" +
                $"Seed={seed}\n" +
                $"Completed={runReport.CompletedIterations}/" +
                $"{runReport.RequestedIterations}\n" +
                $"Verdict={runReport.Verdict}\n" +
                $"Coverage={runReport.Coverage.CoveredCount}/" +
                $"{runReport.Coverage.EligibleCount}\n" +
                $"StoppedEarly={stopped}\n" +
                $"EvidenceEntries={runReport.Evidence.Entries.Count}\n" +
                $"OverwrittenSteps={runReport.OverwrittenStepCount}");

            yield break;
        }

        Debug.Log(
            "P12 DETERMINISTIC MONKEY COMPLETE\n" +
            $"Seed={seed}\n" +
            $"Iterations={runReport.CompletedIterations}\n" +
            $"Coverage={runReport.Coverage.CoveredCount}/" +
            $"{runReport.Coverage.EligibleCount} (100%)\n" +
            $"EvidenceEntries={runReport.Evidence.Entries.Count}\n" +
            "Selection / Drag / Verification / Evidence / Coverage PASS");
    }

    private bool ValidateConfiguration()
    {
        if (interactionCamera == null ||
            inputProcessingBoundary == null ||
            presentation == null ||
            dragDropObserver == null)
        {
            Debug.LogError(
                "AutoLabSeededPointerMonkeyProbe composition is incomplete.");
            return false;
        }

        if (iterationCount < 1)
        {
            Debug.LogError(
                "Monkey iteration count must be positive.");
            return false;
        }

        if (holdDurationMilliseconds <= 0)
        {
            Debug.LogError(
                "Monkey hold duration must be positive.");
            return false;
        }

        if (scrollHorizontal == 0 &&
            scrollVertical == 0)
        {
            Debug.LogError(
                "Monkey scroll delta cannot be zero.");
            return false;
        }

        if (selectionPreviewSeconds < 0 ||
            completedStepPauseSeconds < 0)
        {
            Debug.LogError(
                "Monkey presentation delays cannot be negative.");
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

    private void ReportTaskFault(
        string label,
        Task task,
        int sequence,
        PointerMonkeyStep step)
    {
        Debug.LogError(
            $"{label} FAILED\n" +
            $"Seed={seed}\n" +
            $"Iteration={sequence}\n" +
            $"Step={(step.IsValid ? step.ToString() : "<selection failed>")}");

        if (task.IsFaulted)
        {
            Debug.LogException(
                task.Exception?
                    .GetBaseException());
        }
    }

    private static Exception GetFailureException(
        Task task)
    {
        if (task.IsCanceled)
        {
            return new OperationCanceledException(
                "Monkey operation was cancelled.");
        }

        return task.Exception?
                   .GetBaseException() ??
               new InvalidOperationException(
                   "Unknown monkey operation failure.");
    }

    private static string GetFailureDetail(
        Task task)
    {
        Exception exception =
            GetFailureException(
                task);

        return
            $"{exception.GetType().Name}: " +
            $"{exception.Message}";
    }

    private static string FormatVerification(
        EvaluationReport report)
    {
        var message =
            new StringBuilder();

        message
            .Append("Verdict=")
            .Append(report.Verdict);

        foreach (OracleResult result in report.Results)
        {
            message
                .Append("\n")
                .Append(result.Code)
                .Append(": ")
                .Append(result.Verdict)
                .Append(" — ")
                .Append(result.Detail);
        }

        foreach (EvaluationError error in report.Errors)
        {
            message
                .Append("\n")
                .Append("OracleError ")
                .Append(error.OracleId)
                .Append(": ")
                .Append(error.ExceptionType)
                .Append(" — ")
                .Append(error.Message);
        }

        return message.ToString();
    }

    private void OnDestroy()
    {
        presentation?.Unbind();
        physicalInput?.Dispose();
    }
}
