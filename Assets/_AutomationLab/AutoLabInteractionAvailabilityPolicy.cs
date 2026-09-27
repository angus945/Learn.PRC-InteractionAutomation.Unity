using System;
using Module.InteractionAutomation.Availability;
using Module.InteractionAutomation.Targets;

/// <summary>Small Project-owned stand-in for a game's input-permission read model.</summary>
public sealed class AutoLabInteractionPermissionState
{
    public bool ConfirmClickAllowed { get; private set; } = true;
    public bool ConfirmDropAllowed { get; private set; } = true;

    public void SetConfirmClickAllowed(bool allowed) => ConfirmClickAllowed = allowed;
    public void SetConfirmDropAllowed(bool allowed) => ConfirmDropAllowed = allowed;
}

/// <summary>
/// Reads current Project permission; it neither duplicates UI state nor sends input.
/// These target IDs and game rules belong to AutoLab, not to reusable components.
/// </summary>
public sealed class AutoLabInteractionAvailabilityPolicy : IInteractionAvailabilityEvaluator
{
    private readonly AutoLabInteractionPermissionState state;

    public AutoLabInteractionAvailabilityPolicy(AutoLabInteractionPermissionState state)
    {
        this.state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public InteractionAvailability Evaluate(InteractionAvailabilityContext context)
    {
        context.EnsureValid();
        if (context.Target.Id.Value == "button.confirm" &&
            (context.Capability == PhysicalInteractionCapabilities.PointerClick ||
             context.Capability == PhysicalInteractionCapabilities.PointerDoubleClick) &&
            !state.ConfirmClickAllowed)
        {
            return InteractionAvailability.Denied("autolab.confirm.click.locked");
        }

        if (context.Role == InteractionTargetRole.DragDestination &&
            context.Target.Id.Value == "toggle.music" &&
            context.OtherTarget.HasValue &&
            context.OtherTarget.Value.Id.Value == "button.confirm" &&
            !state.ConfirmDropAllowed)
        {
            return InteractionAvailability.Denied("autolab.confirm.drop.locked");
        }

        return InteractionAvailability.Available;
    }
}
