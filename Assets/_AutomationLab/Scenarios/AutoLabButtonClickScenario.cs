using System.Collections;
using System.Threading.Tasks;
using Module.InteractionAutomation.Runner;
using Module.InteractionAutomation.Targets;
using Module.Verification.Oracle;
using UnityEngine;

public sealed class AutoLabButtonClickScenario
{
    private static readonly InteractionTargetId TargetId =
        new InteractionTargetId(
            "button.confirm");

    private readonly InteractionRunner runner;
    private readonly ButtonClickCounter clickCounter;

    public AutoLabButtonClickScenario(
        InteractionRunner runner,
        ButtonClickCounter clickCounter)
    {
        this.runner = runner;
        this.clickCounter = clickCounter;
    }

    public bool Passed { get; private set; }

    public IEnumerator Run()
    {
        Passed = false;

        clickCounter.ResetCount();

        Debug.Log(
            "RUNNER BUTTON START");

        Task execution =
            runner.ExecuteAsync(
                    new PointerClickRequest(
                        TargetId))
                .AsTask();

        yield return
            AutoLabScenarioSupport.WaitFor(
                execution);

        if (AutoLabScenarioSupport.ReportFault(
                "RUNNER BUTTON",
                execution))
        {
            yield break;
        }

        yield return null;

        EvaluationReport verification =
            AutoLabButtonClickVerificationProfile
                .Create()
                .Evaluate(
                    TargetId.Value,
                    new AutoLabButtonClickObservation(
                        clickCounter.Count));

        if (AutoLabScenarioSupport.ReportVerificationFailure(
                "RUNNER BUTTON",
                verification))
        {
            yield break;
        }

        Debug.Log(
            "RUNNER BUTTON PASS");

        Passed = true;
    }
}
