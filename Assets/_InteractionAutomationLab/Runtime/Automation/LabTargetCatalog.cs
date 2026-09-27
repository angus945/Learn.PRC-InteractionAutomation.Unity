using System.Collections.Generic;
using Module.InteractionAutomation.Targets;

namespace Project.InteractionAutomationLab.Automation
{
    public sealed class LabTargetDescriptor
    {
        public LabTargetDescriptor(string id, PhysicalInteractionCapabilities capabilities)
        {
            Id = id;
            Capabilities = capabilities;
        }

        public string Id { get; }
        public PhysicalInteractionCapabilities Capabilities { get; }
    }

    public static class LabTargetCatalog
    {
        public const string NavigationInventory = "lab.nav.inventory";
        public const string NavigationSettings = "lab.nav.settings";
        public const string Sword = "lab.inventory.item.sword";
        public const string Shield = "lab.inventory.item.shield";
        public const string Potion = "lab.inventory.item.potion";
        public const string Key = "lab.inventory.item.key";
        public const string Sort = "lab.inventory.sort";
        public const string Weapon = "lab.equipment.weapon";
        public const string Offhand = "lab.equipment.offhand";
        public const string Inspect = "lab.details.inspect";
        public const string Charge = "lab.actions.charge";
        public const string UsePotion = "lab.actions.use-potion";
        public const string Help = "lab.quick.help";
        public const string Emergency = "lab.quick.emergency";
        public const string BuyPotion = "lab.shop.buy-potion";
        public const string Music = "lab.settings.music";
        public const string Sfx = "lab.settings.sfx";
        public const string LogScroll = "lab.settings.logscroll";
        public const string DisabledSelectable = "lab.canary.disabled-selectable";
        public const string NonSelectable = "lab.canary.nonselectable";

        private const PhysicalInteractionCapabilities ClickHover = PhysicalInteractionCapabilities.PointerClick | PhysicalInteractionCapabilities.PointerHover;
        private const PhysicalInteractionCapabilities DragClickHover = ClickHover | PhysicalInteractionCapabilities.PointerDrag;

        public static IReadOnlyList<LabTargetDescriptor> All { get; } = new LabTargetDescriptor[]
        {
            new LabTargetDescriptor(NavigationInventory, ClickHover),
            new LabTargetDescriptor(NavigationSettings, ClickHover),
            new LabTargetDescriptor(Sword, DragClickHover),
            new LabTargetDescriptor(Shield, DragClickHover),
            new LabTargetDescriptor(Potion, ClickHover),
            new LabTargetDescriptor(Key, ClickHover),
            new LabTargetDescriptor(Sort, ClickHover),
            new LabTargetDescriptor(Weapon, PhysicalInteractionCapabilities.PointerHover),
            new LabTargetDescriptor(Offhand, PhysicalInteractionCapabilities.PointerHover),
            new LabTargetDescriptor(Inspect, PhysicalInteractionCapabilities.PointerDoubleClick | PhysicalInteractionCapabilities.PointerHover),
            new LabTargetDescriptor(Charge, PhysicalInteractionCapabilities.PointerHold | PhysicalInteractionCapabilities.PointerHover),
            new LabTargetDescriptor(UsePotion, ClickHover),
            new LabTargetDescriptor(Help, ClickHover),
            new LabTargetDescriptor(Emergency, ClickHover),
            new LabTargetDescriptor(BuyPotion, ClickHover),
            new LabTargetDescriptor(Music, ClickHover),
            new LabTargetDescriptor(Sfx, ClickHover),
            new LabTargetDescriptor(LogScroll, PhysicalInteractionCapabilities.Scroll | PhysicalInteractionCapabilities.PointerHover),
            new LabTargetDescriptor(DisabledSelectable, ClickHover),
            new LabTargetDescriptor(NonSelectable, ClickHover)
        };

        public static LabTargetDescriptor Get(string id)
        {
            foreach (LabTargetDescriptor descriptor in All)
            {
                if (descriptor.Id == id) return descriptor;
            }
            throw new KeyNotFoundException("Unknown lab target id: " + id);
        }
    }
}
