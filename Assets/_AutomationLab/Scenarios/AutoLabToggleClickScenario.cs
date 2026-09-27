using System.Collections;
using System.Threading.Tasks;
using Module.InteractionAutomation.Runner;
using Module.InteractionAutomation.Targets;
using Module.Verification.Oracle;
using UnityEngine;

public sealed class AutoLabToggleClickScenario
{
    private static readonly InteractionTargetId TargetId =
        new InteractionTargetId(
            "toggle.music");

    private readonly InteractionRunner runner;
    private readonly ToggleStateProbe stateProbe;

    public AutoLabToggleClickScenario(
        InteractionRunner runner,
        ToggleStateProbe stateProbe)
    {
        this.runner = runner;
        this.stateProbe = stateProbe;
    }

    public bool Passed { get; private set; }

    public IEnumerator Run()
    {
        Passed = false;

        stateProbe.ResetObservation(
            initialValue: false);

        Debug.Log(
            "RUNNER TOGGLE START");

        Task execution =
            runner.ExecuteAsync(
                    new PointerClickRequest(
                        TargetId))
                .AsTask();

        yield return
            AutoLabScenarioSupport.WaitFor(
                execution);

        if (AutoLabScenarioSupport.ReportFault(
                "RUNNER TOGGLE",
                execution))
        {
            yield break;
        }

        yield return null;

        EvaluationReport verification =
            AutoLabToggleVerificationProfile
                .Create()
                .Evaluate(
                    TargetId.Value,
                    new AutoLabToggleObservation(
                        stateProbe.ChangeCount,
                        stateProbe.LastValue));

        if (AutoLabScenarioSupport.ReportVerificationFailure(
                "RUNNER TOGGLE",
                verification))
        {
            yield break;
        }

        Debug.Log(
            "RUNNER TOGGLE PASS");

        Passed = true;
    }
}
