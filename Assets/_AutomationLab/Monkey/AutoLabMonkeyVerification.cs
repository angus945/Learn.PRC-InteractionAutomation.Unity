using System.Collections.Generic;
using Module.InteractionAutomation.Monkey;
using Module.InteractionAutomation.Observation.Unity3D;
using Module.InteractionAutomation.Targets;
using Module.Verification.Oracle;

public static class AutoLabMonkeyVerification
{
    public static void ResetScenarioObservation(
        PointerMonkeyStep step,
        UnityPointerDropObserver dropObserver)
    {
        if (step.Capability !=
            PhysicalInteractionCapabilities.PointerDrag)
        {
            return;
        }

        dropObserver.ResetObservation();
    }

    public static EvaluationReport AddScenarioVerification(
        PointerMonkeyStep step,
        EvaluationReport hostVerification,
        UnityPointerDropObserver dropObserver)
    {
        if (step.Capability !=
            PhysicalInteractionCapabilities.PointerDrag)
        {
            return hostVerification;
        }

        EvaluationReport dropVerification =
            AutoLabDropVerificationProfile
                .Create()
                .Evaluate(
                    step.CoverageKey.ToString(),
                    dropObserver.Capture());

        var results =
            new List<OracleResult>();

        results.AddRange(
            hostVerification.Results);

        results.AddRange(
            dropVerification.Results);

        var errors =
            new List<EvaluationError>();

        errors.AddRange(
            hostVerification.Errors);

        errors.AddRange(
            dropVerification.Errors);

        return new EvaluationReport(
            "autolab.pointer.drag",
            step.CoverageKey.ToString(),
            Combine(
                hostVerification.Verdict,
                dropVerification.Verdict),
            results,
            errors);
    }

    private static TestVerdict Combine(
        TestVerdict left,
        TestVerdict right)
    {
        return Rank(right) >
               Rank(left)
            ? right
            : left;
    }

    private static int Rank(
        TestVerdict verdict)
    {
        if (verdict == TestVerdict.InfrastructureError)
            return 5;

        if (verdict == TestVerdict.Failed)
            return 4;

        if (verdict == TestVerdict.Inconclusive)
            return 3;

        if (verdict == TestVerdict.Passed)
            return 2;

        return 1;
    }
}
