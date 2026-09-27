using System.Collections;
using System.Threading.Tasks;
using Module.InteractionAutomation.Observation.Unity3D;
using Module.InteractionAutomation.PhysicalInput.Unity3D;
using Framework.InteractionAutomation.Runner;
using Module.InteractionAutomation.Targets;
using Module.InteractionAutomation.Verification.Unity3D;
using Module.Verification.Oracle;
using UnityEngine;

public sealed class AutoLabHoverScenario
{
    private static readonly InteractionTargetId TargetId =
        new InteractionTargetId(
            "button.confirm");

    private readonly InteractionRunner runner;
    private readonly UnityPhysicalInputDriver physicalInput;
    private readonly UnityPointerHoverObserver observer;

    public AutoLabHoverScenario(
        InteractionRunner runner,
        UnityPhysicalInputDriver physicalInput,
        UnityPointerHoverObserver observer)
    {
        this.runner = runner;
        this.physicalInput = physicalInput;
        this.observer = observer;
    }

    public bool Passed { get; private set; }

    public IEnumerator Run()
    {
        Passed = false;

        observer.ResetObservation();

        Debug.Log(
            "RUNNER HOVER START");

        Task execution =
            runner.ExecuteAsync(
                    new PointerHoverRequest(
                        TargetId))
                .AsTask();

        yield return
            AutoLabScenarioSupport.WaitFor(
                execution);

        if (AutoLabScenarioSupport.ReportFault(
                "POINTER HOVER",
                execution))
        {
            yield break;
        }

        yield return null;

        int deviceId =
            physicalInput.Mouse.deviceId;

        EvaluationReport verification =
            UnityPointerVerificationProfiles
                .HoverObserved()
                .Evaluate(
                    TargetId.Value,
                    observer.Capture(
                        deviceId));

        if (AutoLabScenarioSupport.ReportVerificationFailure(
                "POINTER HOVER",
                verification))
        {
            yield break;
        }

        Debug.Log(
            "POINTER HOVER PASS");

        Passed = true;
    }
}
