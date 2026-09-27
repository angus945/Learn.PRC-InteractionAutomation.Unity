using System;
using System.Threading;
using System.Threading.Tasks;
using Framework.InteractionAutomation.Monkey;
using Framework.InteractionAutomation.Monkey.Unity3D;
using Framework.InteractionAutomation.Runner.Unity3D;
using Module.InteractionAutomation.Observation.Unity3D;
using Module.InteractionAutomation.PhysicalInput;
using Module.InteractionAutomation.PhysicalInput.Unity3D;
using Module.InteractionAutomation.Timing.Unity3D;
using UnityEngine;

public sealed class AutoLabSeededPointerMonkeyProbe : MonoBehaviour
{
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private UnityFrameInputProcessingBoundary inputProcessingBoundary;
    [SerializeField] private AutoLabMonkeyPresentation presentation;
    [SerializeField] private UnityPointerDropObserver dragDropObserver;
    [SerializeField] private int seed = 12345;
    [SerializeField] private int iterationCount = 25;
    [SerializeField] private float holdDurationMilliseconds = 200f;
    [SerializeField] private float scrollHorizontal;
    [SerializeField] private float scrollVertical = -1f;
    [SerializeField] private float selectionPreviewSeconds = 0.2f;
    [SerializeField] private float completedStepPauseSeconds = 0.15f;

    private UnityPhysicalInputDriver physicalInput;
    private CancellationTokenSource runCancellation;

    // Unity lifecycle is an inbound adapter, not a second Monkey run loop.
    private async void Start()
    {
        if (!ValidateConfiguration())
            return;

        runCancellation = new CancellationTokenSource();
        CancellationToken cancellationToken = runCancellation.Token;
        try
        {
            physicalInput = new UnityPhysicalInputDriver();
            presentation.Bind(physicalInput.Mouse);
            presentation.BeginRun(seed, iterationCount);

            UnityInteractionAutomationRuntime runtime =
                AutomationLabInteractionComposition.CreateRuntime(
                    interactionCamera, physicalInput, inputProcessingBoundary);

            SeededPointerMonkey monkey = UnityPointerMonkeyFactory.Create(
                runtime,
                new PointerMonkeyConfiguration(
                    seed,
                    new ScrollDelta(scrollHorizontal, scrollVertical),
                    TimeSpan.FromMilliseconds(holdDurationMilliseconds)),
                new AutoLabMonkeyDragDestinationPolicy());

            UnityPointerMonkeyVerifier verifier =
                UnityPointerMonkeyVerifier.FromLoadedScenes(physicalInput.Mouse.deviceId);

            var extensions = new AutoLabMonkeyRunAdapter(
                presentation,
                dragDropObserver,
                selectionPreviewSeconds,
                completedStepPauseSeconds);

            var qaRunner = new UnityPointerMonkeyQaRunner(
                monkey, verifier, extensions, extensions);

            // Give the fixture its normal initial frame without forcing InputSystem.Update.
            await inputProcessingBoundary.WaitAsync(cancellationToken);

            Debug.Log(
                "P12 SEEDED POINTER MONKEY QA START\n" +
                $"Seed={seed}\nIterations={iterationCount}\n" +
                "Workflow=Framework.InteractionAutomation.Monkey.Unity3D\n" +
                "Policy=coverage-first deterministic selection");

            UnityPointerMonkeyRunReport report = await qaRunner.RunAsync(
                iterationCount,
                traceCapacity: Math.Max(iterationCount, 32),
                cancellationToken: cancellationToken);

            if (presentation != null)
            {
                presentation.UpdateCoverage(report.Coverage);
                presentation.CompleteRun(report);
            }

            string summary =
                $"Seed={seed}\n" +
                $"Completed={report.CompletedIterations}/{report.RequestedIterations}\n" +
                $"Verdict={report.Verdict}\n" +
                $"Coverage={report.Coverage.CoveredCount}/{report.Coverage.EligibleCount}\n" +
                $"EvidenceEntries={report.Evidence.Entries.Count}\n" +
                $"OverwrittenSteps={report.OverwrittenStepCount}";

            if (report.Passed)
                Debug.Log("P12 DETERMINISTIC MONKEY COMPLETE\n" + summary);
            else
                Debug.LogError("P12 SEEDED POINTER MONKEY QA FAILED\n" + summary);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is not a passed QA result.
            Debug.Log("P12 MONKEY CANCELLED — no PASS result was produced.");
        }
        catch (Exception exception)
        {
            Debug.LogError("P12 MONKEY INFRASTRUCTURE FAILURE");
            Debug.LogException(exception);
        }
        finally
        {
            // RunAsync has left gesture cleanup before the borrowed device is removed.
            if (presentation != null)
                presentation.Unbind();
            physicalInput?.Dispose();
            physicalInput = null;
            CancellationTokenSource completedCancellation = runCancellation;
            runCancellation = null;
            completedCancellation?.Dispose();
        }
    }

    private bool ValidateConfiguration()
    {
        if (interactionCamera == null || inputProcessingBoundary == null ||
            presentation == null || dragDropObserver == null)
        {
            Debug.LogError("AutoLabSeededPointerMonkeyProbe composition is incomplete.");
            return false;
        }

        if (iterationCount < 1 || !IsFinite(holdDurationMilliseconds) ||
            holdDurationMilliseconds <= 0 || !IsFinite(scrollHorizontal) ||
            !IsFinite(scrollVertical) || (scrollHorizontal == 0 && scrollVertical == 0) ||
            !IsFinite(selectionPreviewSeconds) || selectionPreviewSeconds < 0 ||
            !IsFinite(completedStepPauseSeconds) || completedStepPauseSeconds < 0)
        {
            Debug.LogError("Monkey iteration, duration, scroll or pacing configuration is invalid.");
            return false;
        }
        return true;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private void OnDisable()
    {
        runCancellation?.Cancel();
    }

    private void OnDestroy()
    {
        // Do not dispose physicalInput here while an asynchronous gesture may be unwinding.
        runCancellation?.Cancel();
    }
}
