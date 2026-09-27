using Module.InteractionAutomation.Monkey;
using Module.InteractionAutomation.Targets;

public sealed class AutoLabMonkeyDragDestinationPolicy :
    IPointerMonkeyDragDestinationPolicy
{
    private const string SourceId =
        "button.confirm";

    private const string DestinationId =
        "toggle.music";

    public bool CanDragTo(
        InteractionTargetSnapshot source,
        InteractionTargetSnapshot destination)
    {
        return
            source.Id.Value == SourceId &&
            destination.Id.Value == DestinationId;
    }
}
