using Framework.InteractionAutomation.Monkey;
using Module.InteractionAutomation.Targets;

namespace Project.InteractionAutomationLab.Automation
{
    public sealed class LabDragDestinationPolicy : IPointerMonkeyDragDestinationPolicy
    {
        public bool CanDragTo(InteractionTargetSnapshot source, InteractionTargetSnapshot destination)
        {
            if (source.Id.Value == LabTargetCatalog.Sword) return destination.Id.Value == LabTargetCatalog.Weapon;
            if (source.Id.Value == LabTargetCatalog.Shield) return destination.Id.Value == LabTargetCatalog.Offhand;
            return false;
        }
    }
}
