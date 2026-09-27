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
            context.Drag.BeginDragCount == 1 &&
            context.Drag.DragCount >= 1 &&
            context.Drag.EndDragCount == 1;

        string detail =
            $"BeginDrag={context.Drag.BeginDragCount}, " +
            $"Drag={context.Drag.DragCount}, " +
            $"EndDrag={context.Drag.EndDragCount}";

        return new OracleResult(
            passed
                ? TestVerdict.Passed
                : TestVerdict.Failed,
            "pointer.drag.lifecycle",
            detail);
    }
}
