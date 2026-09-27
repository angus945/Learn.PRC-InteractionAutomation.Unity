using System;
using System.Collections;
using System.Threading.Tasks;
using Module.InteractionAutomation.Observation.Unity3D;
using Module.InteractionAutomation.Runner;
using Module.InteractionAutomation.Targets;
using Module.InteractionAutomation.Verification.Unity3D;
using Module.Verification.Oracle;
using UnityEngine;

public sealed class AutoLabHoldScenario
{
    private static readonly InteractionTargetId TargetId =
        new InteractionTargetId(
            "button.confirm");

    private readonly InteractionRunner runner;
    private readonly UnityPointerPressObserver observer;

    public AutoLabHoldScenario(
        InteractionRunner runner,
        UnityPointerPressObserver observer)
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
            "RUNNER HOLD START");

        Task execution =
            runner.ExecuteAsync(
                    new PointerHoldRequest(
                        TargetId,
                        TimeSpan.FromMilliseconds(
                            200)))
                .AsTask();

        yield return
            AutoLabScenarioSupport.WaitFor(
                execution);

        if (AutoLabScenarioSupport.ReportFault(
                "POINTER HOLD",
                execution))
        {
            yield break;
        }

        yield return null;

        UnityPointerPressObservation observation =
            observer.Capture();

        EvaluationReport verification =
            UnityPointerVerificationProfiles
                .HoldLifecycle()
                .Evaluate(
                    TargetId.Value,
                    observation);

        if (AutoLabScenarioSupport.ReportVerificationFailure(
                "POINTER HOLD",
                verification))
        {
            yield break;
        }

        Debug.Log(
            "POINTER HOLD PASS\n" +
            $"HeldFrames={observation.PressedFrameCount}");

        Passed = true;
    }
}
