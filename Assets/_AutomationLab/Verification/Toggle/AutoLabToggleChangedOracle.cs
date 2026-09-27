using Module.Verification.Oracle;

public sealed class AutoLabToggleChangedOracle :
    ITestOracle<AutoLabToggleObservation>
{
    public string Id =>
        "autolab.toggle.music.changed";

    public OracleResult Evaluate(
        AutoLabToggleObservation context)
    {
        bool passed =
            context.ChangeCount == 1 &&
            context.Value;

        string detail =
            $"ChangeCount={context.ChangeCount}, " +
            $"Value={context.Value}";

        return new OracleResult(
            passed
                ? TestVerdict.Passed
                : TestVerdict.Failed,
            Id,
            detail);
    }
}
