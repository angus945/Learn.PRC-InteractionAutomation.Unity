using Module.Verification.Oracle;

public sealed class DragLifecycleOracle :
    ITestOracle<DragObservation>
{
    public string Id =>
        "pointer.drag.lifecycle";

    public OracleResult Evaluate(
        DragObservation context)
    {
        bool passed =
            context.BeginDragCount == 1 &&
            context.DragCount >= 1 &&
            context.EndDragCount == 1;

        string detail =
            $"BeginDrag={context.BeginDragCount}, " +
            $"Drag={context.DragCount}, " +
            $"EndDrag={context.EndDragCount}";

        return new OracleResult(
            passed
                ? TestVerdict.Passed
                : TestVerdict.Failed,
            "pointer.drag.lifecycle",
            detail);
    }
}
