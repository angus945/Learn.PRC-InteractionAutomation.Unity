using Module.Verification.Oracle;

public sealed class DropObservedOracle :
    ITestOracle<DragObservation>
{
    public string Id =>
        "pointer.drag.drop-observed";

    public OracleResult Evaluate(
        DragObservation context)
    {
        bool passed =
            context.DropCount == 1;

        return new OracleResult(
            passed
                ? TestVerdict.Passed
                : TestVerdict.Failed,
            "pointer.drag.drop-observed",
            $"Drop={context.DropCount}");
    }
}
