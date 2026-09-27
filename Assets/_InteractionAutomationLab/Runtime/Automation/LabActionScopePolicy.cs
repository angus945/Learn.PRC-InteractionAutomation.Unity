using System;
using Module.InteractionAutomation.Availability;
using Module.InteractionAutomation.Targets;

namespace Project.InteractionAutomationLab.Automation
{
    public sealed class LabActionScopePolicy : IInteractionAvailabilityEvaluator
    {
        private readonly PhysicalInteractionCapabilities capability;
        private readonly string sourceId;
        private readonly string destinationId;

        public LabActionScopePolicy(PhysicalInteractionCapabilities capability, string sourceId, string destinationId = "")
        {
            this.capability = capability;
            this.sourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId));
            this.destinationId = destinationId ?? string.Empty;
        }

        public InteractionAvailability Evaluate(InteractionAvailabilityContext context)
        {
            context.EnsureValid();
            if (context.Capability != capability) return InteractionAvailability.Denied("lab.test-scope.excluded");
            if (capability != PhysicalInteractionCapabilities.PointerDrag)
            {
                return context.Target.Id.Value == sourceId ? InteractionAvailability.Available : InteractionAvailability.Denied("lab.test-scope.excluded");
            }
            bool source = context.Role == InteractionTargetRole.DragSource && context.Target.Id.Value == sourceId &&
                context.OtherTarget.HasValue && context.OtherTarget.Value.Id.Value == destinationId;
            bool destination = context.Role == InteractionTargetRole.DragDestination && context.Target.Id.Value == destinationId &&
                context.OtherTarget.HasValue && context.OtherTarget.Value.Id.Value == sourceId;
            return source || destination ? InteractionAvailability.Available : InteractionAvailability.Denied("lab.test-scope.excluded");
        }
    }
}
