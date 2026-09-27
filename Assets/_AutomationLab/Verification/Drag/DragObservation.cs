using Module.InteractionAutomation.Observation.Unity3D;

public sealed class DragObservation
{
    public DragObservation(
        UnityPointerDragObservation drag,
        UnityPointerDropObservation drop)
    {
        Drag = drag;
        Drop = drop;
    }

    public UnityPointerDragObservation Drag { get; }
    public UnityPointerDropObservation Drop { get; }
}
