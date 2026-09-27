using System;
using System.Collections.Generic;
using Module.InteractionAutomation.Availability;
using Module.InteractionAutomation.Targets;
using Project.InteractionAutomationLab.Domain;

namespace Project.InteractionAutomationLab.Automation
{
    public sealed class LabProjectAvailabilityPolicy : IInteractionAvailabilityEvaluator
    {
        public const string GameplayLockedCode = "lab.policy.gameplay-input-locked";
        public const string EquipmentLockedCode = "lab.policy.equipment-locked";

        private static readonly HashSet<string> GameplayTargets = new HashSet<string>(StringComparer.Ordinal)
        {
            LabTargetCatalog.Sword,
            LabTargetCatalog.Shield,
            LabTargetCatalog.Potion,
            LabTargetCatalog.Key,
            LabTargetCatalog.Sort,
            LabTargetCatalog.Weapon,
            LabTargetCatalog.Offhand,
            LabTargetCatalog.Inspect,
            LabTargetCatalog.Charge,
            LabTargetCatalog.UsePotion,
            LabTargetCatalog.Emergency,
            LabTargetCatalog.BuyPotion
        };

        private readonly LabState state;

        public LabProjectAvailabilityPolicy(LabState state)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public InteractionAvailability Evaluate(InteractionAvailabilityContext context)
        {
            context.EnsureValid();
            if (context.Capability == PhysicalInteractionCapabilities.PointerHover) return InteractionAvailability.Available;
            string targetId = context.Target.Id.Value;
            if (state.GameMode == LabGameMode.Cutscene && GameplayTargets.Contains(targetId))
            {
                return InteractionAvailability.Denied(GameplayLockedCode);
            }
            bool equipmentDestination = context.Role == InteractionTargetRole.DragDestination &&
                (targetId == LabTargetCatalog.Weapon || targetId == LabTargetCatalog.Offhand);
            if (state.EquipmentLocked && equipmentDestination)
            {
                return InteractionAvailability.Denied(EquipmentLockedCode);
            }
            return InteractionAvailability.Available;
        }
    }
}
