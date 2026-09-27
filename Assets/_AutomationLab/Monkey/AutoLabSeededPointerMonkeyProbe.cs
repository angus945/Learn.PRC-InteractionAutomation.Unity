using System;
using System.Collections;
using System.Threading.Tasks;
using Module.InteractionAutomation.Availability;
using Module.InteractionAutomation.Monkey;
using Module.InteractionAutomation.PhysicalInput;
using Module.InteractionAutomation.PhysicalInput.Unity3D;
using Module.InteractionAutomation.Runner;
using Module.InteractionAutomation.Runner.Unity3D;
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
                        holdDurationMilliseconds)));

        yield return null;

        Debug.Log(
            "P12.1 SEEDED POINTER MONKEY START\n" +
            $"Seed={seed}\n" +
            $"Iterations={iterationCount}\n" +
            "Actions=Click / DoubleClick / Scroll / Hold / Hover\n" +
            "Drag=Excluded");

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

            if (ReportFault(
                    "MONKEY SELECTION",
                    selection,
                    sequence,
                    default))
            {
                presentation.FailSelection(
                    sequence,
                    GetFailureDetail(selection));

                yield break;
            }

            PointerMonkeyStep step =
                selection
                    .GetAwaiter()
                    .GetResult();

            presentation.SelectStep(
                step);

            Debug.Log(
                "MONKEY STEP SELECTED\n" +
                $"Seed={seed}\n" +
                $"Step={step}");

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

            if (ReportFault(
                    "MONKEY EXECUTION",
                    execution,
                    sequence,
                    step))
            {
                presentation.FailStep(
                    step,
                    GetFailureDetail(execution));

                yield break;
            }

            presentation.CompleteStep(
                step);

            if (completedStepPauseSeconds > 0)
            {
                yield return
                    new WaitForSecondsRealtime(
                        completedStepPauseSeconds);
            }
        }

        presentation.CompleteRun();

        Debug.Log(
            "P12.1 SEEDED POINTER MONKEY PASS\n" +
            $"Seed={seed}\n" +
            $"Iterations={iterationCount}\n" +
            "All selected actions executed through InteractionRunner.");
    }

    private bool ValidateConfiguration()
    {
        if (interactionCamera == null ||
            inputProcessingBoundary == null ||
            presentation == null)
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

    private bool ReportFault(
        string label,
        Task task,
        int sequence,
        PointerMonkeyStep step)
    {
        if (!task.IsCanceled &&
            !task.IsFaulted)
        {
            return false;
        }

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

        return true;
    }

    private static string GetFailureDetail(
        Task task)
    {
        if (task.IsCanceled)
            return "Task was cancelled.";

        if (!task.IsFaulted)
            return string.Empty;

        Exception exception =
            task.Exception?
                .GetBaseException();

        return exception == null
            ? "Unknown execution failure."
            : $"{exception.GetType().Name}: {exception.Message}";
    }

    private void OnDestroy()
    {
        presentation?.Unbind();
        physicalInput?.Dispose();
    }
}
