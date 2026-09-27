using System.Collections;
using System.Threading.Tasks;
using Module.InteractionAutomation.Observation.Unity3D;
using Framework.InteractionAutomation.Runner;
using Module.InteractionAutomation.Targets;
using Module.InteractionAutomation.Verification.Unity3D;
using Module.Verification.Oracle;
using UnityEngine;

public sealed class AutoLabDragScenario
{
    private static readonly InteractionTargetId SourceId =
        new InteractionTargetId(
            "button.confirm");

    private static readonly InteractionTargetId DestinationId =
        new InteractionTargetId(
            "toggle.music");

    private readonly InteractionRunner runner;
    private readonly UnityPointerDragObserver dragObserver;
    private readonly UnityPointerDropObserver dropObserver;

    public AutoLabDragScenario(
        InteractionRunner runner,
        UnityPointerDragObserver dragObserver,
        UnityPointerDropObserver dropObserver)
    {
        this.runner = runner;
        this.dragObserver = dragObserver;
        this.dropObserver = dropObserver;
    }

    public bool Passed { get; private set; }

    public IEnumerator Run()
    {
        Passed = false;

        dragObserver.ResetObservation();
        dropObserver.ResetObservation();

        Debug.Log(
            "RUNNER DRAG START");

        Task execution =
            runner.ExecuteAsync(
                    new PointerDragRequest(
                        SourceId,
                        DestinationId))
                .AsTask();

        yield return
            AutoLabScenarioSupport.WaitFor(
                execution);

        if (AutoLabScenarioSupport.ReportFault(
                "POINTER DRAG",
                execution))
        {
            yield break;
        }

        yield return null;

        EvaluationReport lifecycleVerification =
            UnityPointerVerificationProfiles
                .DragLifecycle()
                .Evaluate(
                    $"{SourceId.Value}->{DestinationId.Value}",
                    dragObserver.Capture());

        if (AutoLabScenarioSupport.ReportVerificationFailure(
                "POINTER DRAG LIFECYCLE",
                lifecycleVerification))
        {
            yield break;
        }

        EvaluationReport dropVerification =
            AutoLabDropVerificationProfile
                .Create()
                .Evaluate(
                    $"{SourceId.Value}->{DestinationId.Value}",
                    dropObserver.Capture());

        if (AutoLabScenarioSupport.ReportVerificationFailure(
                "POINTER DRAG DROP",
                dropVerification))
        {
            yield break;
        }

        Debug.Log(
            "POINTER DRAG PASS\n" +
            "Unity host contract and AutoLab drop expectation passed.");

        Passed = true;
    }
}
