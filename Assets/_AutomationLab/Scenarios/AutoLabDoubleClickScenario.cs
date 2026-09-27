using System.Collections;
using System.Threading.Tasks;
using Module.InteractionAutomation.Observation.Unity3D;
using Module.InteractionAutomation.Runner;
using Module.InteractionAutomation.Targets;
using Module.InteractionAutomation.Verification.Unity3D;
using Module.Verification.Oracle;
using UnityEngine;

public sealed class AutoLabDoubleClickScenario
{
    private static readonly InteractionTargetId TargetId =
        new InteractionTargetId(
            "button.confirm");

    private readonly InteractionRunner runner;
    private readonly UnityPointerClickObserver observer;

    public AutoLabDoubleClickScenario(
        InteractionRunner runner,
        UnityPointerClickObserver observer)
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
            "RUNNER DOUBLE CLICK START");

        Task execution =
            runner.ExecuteAsync(
                    new PointerDoubleClickRequest(
                        TargetId))
                .AsTask();

        yield return
            AutoLabScenarioSupport.WaitFor(
                execution);

        if (AutoLabScenarioSupport.ReportFault(
                "POINTER DOUBLE CLICK",
                execution))
        {
            yield break;
        }

        yield return null;

        EvaluationReport verification =
            UnityPointerVerificationProfiles
                .DoubleClick()
                .Evaluate(
                    TargetId.Value,
                    observer.Capture());

        if (AutoLabScenarioSupport.ReportVerificationFailure(
                "POINTER DOUBLE CLICK",
                verification))
        {
            yield break;
        }

        Debug.Log(
            "POINTER DOUBLE CLICK PASS");

        Passed = true;
    }
}
