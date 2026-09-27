public sealed class DragObservation
{
    public DragObservation(
        int beginDragCount,
        int dragCount,
        int endDragCount,
        int dropCount)
    {
        BeginDragCount = beginDragCount;
        DragCount = dragCount;
        EndDragCount = endDragCount;
        DropCount = dropCount;
    }

    public int BeginDragCount { get; }
    public int DragCount { get; }
    public int EndDragCount { get; }
    public int DropCount { get; }
}
