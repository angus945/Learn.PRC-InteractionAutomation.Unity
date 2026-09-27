using System.Collections;
using System.Threading.Tasks;
using Module.InteractionAutomation.Observation.Unity3D;
using Module.InteractionAutomation.PhysicalInput;
using Module.InteractionAutomation.Runner;
using Module.InteractionAutomation.Targets;
using Module.InteractionAutomation.Verification.Unity3D;
using Module.Verification.Oracle;
using UnityEngine;

public sealed class AutoLabScrollScenario
{
    private static readonly InteractionTargetId TargetId =
        new InteractionTargetId(
            "toggle.music");

    private readonly InteractionRunner runner;
    private readonly UnityPointerScrollObserver observer;

    public AutoLabScrollScenario(
        InteractionRunner runner,
        UnityPointerScrollObserver observer)
    {
        this.runner = runner;
        this.observer = observer;
    }

    public bool Passed { get; private set; }

    public IEnumerator Run()
    {
        Passed = false;

        observer.ResetObservation();

        Debug.Log(
            "RUNNER SCROLL START");

        Task execution =
            runner.ExecuteAsync(
                    new PointerScrollRequest(
                        TargetId,
                        new ScrollDelta(
                            horizontal: 0,
                            vertical: -1)))
                .AsTask();

        yield return
            AutoLabScenarioSupport.WaitFor(
                execution);

        if (AutoLabScenarioSupport.ReportFault(
                "POINTER SCROLL",
                execution))
        {
            yield break;
        }

        yield return null;

        UnityPointerScrollObservation observation =
            observer.Capture();

        EvaluationReport verification =
            UnityPointerVerificationProfiles
                .ScrollObserved()
                .Evaluate(
                    TargetId.Value,
                    observation);

        if (AutoLabScenarioSupport.ReportVerificationFailure(
                "POINTER SCROLL",
                verification))
        {
            yield break;
        }

        Debug.Log(
            "POINTER SCROLL PASS\n" +
            $"Delta={observation.LastDelta}");

        Passed = true;
    }
}
