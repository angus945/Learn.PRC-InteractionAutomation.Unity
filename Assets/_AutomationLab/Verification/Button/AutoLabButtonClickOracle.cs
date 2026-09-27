using Module.Verification.Oracle;

public sealed class AutoLabButtonClickOracle :
    ITestOracle<AutoLabButtonClickObservation>
{
    public string Id =>
        "autolab.button.confirm.callback";

    public OracleResult Evaluate(
        AutoLabButtonClickObservation context)
    {
        bool passed =
            context.CallbackCount == 1;

        return new OracleResult(
            passed
                ? TestVerdict.Passed
                : TestVerdict.Failed,
            Id,
            $"CallbackCount={context.CallbackCount}");
    }
}
