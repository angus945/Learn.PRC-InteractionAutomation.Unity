using Module.InteractionAutomation.Observation.Unity3D;
using Module.Verification.Oracle;

public sealed class AutoLabDropObservedOracle :
    ITestOracle<UnityPointerDropObservation>
{
    public string Id =>
        "autolab.drag.drop-on-toggle";

    public OracleResult Evaluate(
        UnityPointerDropObservation context)
    {
        bool passed =
            context.DropCount == 1;

        return new OracleResult(
            passed
                ? TestVerdict.Passed
                : TestVerdict.Failed,
            Id,
            $"Drop={context.DropCount}");
    }
}
