using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Framework.InteractionAutomation.Monkey;
using Framework.InteractionAutomation.Monkey.Unity3D;
using Module.InteractionAutomation.Observation.Unity3D;
using Module.Verification.Oracle;
using UnityEngine;

/// <summary>AutoLab presentation and product expectations; never selects or executes input.</summary>
public sealed class AutoLabMonkeyRunAdapter :
    IUnityPointerMonkeyScenarioVerification,
    IUnityPointerMonkeyRunObserver
{
    private readonly AutoLabMonkeyPresentation presentation;
    private readonly UnityPointerDropObserver dropObserver;
    private readonly TimeSpan previewDelay;
    private readonly TimeSpan completedDelay;
    private PointerMonkeyStep currentStep;

    public AutoLabMonkeyRunAdapter(
        AutoLabMonkeyPresentation presentation,
        UnityPointerDropObserver dropObserver,
        float previewSeconds,
        float completedSeconds)
    {
        this.presentation = presentation != null ? presentation :
            throw new ArgumentNullException(nameof(presentation));
        this.dropObserver = dropObserver != null ? dropObserver :
            throw new ArgumentNullException(nameof(dropObserver));
        previewDelay = TimeSpan.FromSeconds(previewSeconds);
        completedDelay = TimeSpan.FromSeconds(completedSeconds);
    }

    public void ResetObservation(PointerMonkeyStep step)
    {
        AutoLabMonkeyVerification.ResetScenarioObservation(step, dropObserver);
    }

    public EvaluationReport AddVerification(PointerMonkeyStep step, EvaluationReport hostReport)
    {
        return AutoLabMonkeyVerification.AddScenarioVerification(step, hostReport, dropObserver);
    }

    public void OnSelected(PointerMonkeyStep step, PointerMonkeyCoverageSnapshot coverage)
    {
        currentStep = step;
        if (presentation != null)
        {
            presentation.SelectStep(step);
            presentation.UpdateCoverage(coverage);
        }
        Debug.Log($"MONKEY STEP SELECTED\nStep={step}\nCoverageKey={step.CoverageKey}");
    }

    public async ValueTask BeforeExecutionAsync(PointerMonkeyStep step, CancellationToken cancellationToken)
    {
        if (previewDelay > TimeSpan.Zero)
            await Task.Delay(previewDelay, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (presentation != null)
            presentation.BeginStep(step);
    }

    public void OnCompleted(UnityPointerMonkeyStepResult result)
    {
        if (presentation != null)
        {
            presentation.UpdateCoverage(result.Coverage);
            if (result.Verdict == TestVerdict.Passed)
                presentation.CompleteStep(currentStep, result.Verification);
            else
                presentation.FailStep(currentStep, FormatVerification(result.Verification));
        }

        if (result.Verdict != TestVerdict.Passed)
        {
            Debug.LogError(
                "MONKEY VERIFICATION FAILED\n" +
                $"Reproduction={result.ReproductionKey}\n" +
                FormatVerification(result.Verification));
        }
    }

    public async ValueTask AfterExecutionAsync(PointerMonkeyStep step, CancellationToken cancellationToken)
    {
        if (completedDelay > TimeSpan.Zero)
            await Task.Delay(completedDelay, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
    }

    public void OnSelectionFailure(int sequence, string detail)
    {
        if (presentation != null)
            presentation.FailSelection(sequence, detail);
        Debug.LogError($"MONKEY SELECTION FAILED\nIteration={sequence}\n{detail}");
    }

    private static string FormatVerification(EvaluationReport report)
    {
        var message = new StringBuilder();
        message.Append("Verdict=").Append(report.Verdict);
        foreach (OracleResult result in report.Results)
            message.Append("\n").Append(result.Code).Append(": ")
                .Append(result.Verdict).Append(" — ").Append(result.Detail);
        foreach (EvaluationError error in report.Errors)
            message.Append("\nOracleError ").Append(error.OracleId).Append(": ")
                .Append(error.ExceptionType).Append(" — ").Append(error.Message);
        return message.ToString();
    }
}
