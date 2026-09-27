using System;
using System.Threading;
using System.Threading.Tasks;
using Framework.InteractionAutomation.Monkey;
using Framework.InteractionAutomation.Monkey.Unity3D;
using Framework.InteractionAutomation.Runner;
using Framework.InteractionAutomation.Runner.Unity3D;
using Module.InteractionAutomation.Availability;
using Module.InteractionAutomation.Coordinates;
using Module.InteractionAutomation.Observation.Unity3D;
using Module.InteractionAutomation.PhysicalInput;
using Module.InteractionAutomation.PhysicalInput.Unity3D;
using Module.InteractionAutomation.Targets;
using Module.InteractionAutomation.Targets.Unity3D;
using Module.InteractionAutomation.Timing.Unity3D;
using Module.InteractionAutomation.Verification.Unity3D;
using Module.Verification.Oracle;
using UnityEngine;

/// <summary>
/// Opt-in IA-2 acceptance fixture. Add to the existing AutoLab Probe object and disable
/// the other input-driving probes before Play. No scene or product callbacks are invoked directly.
/// </summary>
public sealed class AutoLabAvailabilityProbe : MonoBehaviour
{
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private UnityFrameInputProcessingBoundary inputProcessingBoundary;
    private CancellationTokenSource cancellation;
    private UnityPhysicalInputDriver input;

    private async void Start()
    {
        cancellation = new CancellationTokenSource();
        CancellationToken token = cancellation.Token;
        try
        {
            RequireNoActiveProbe<AutoLabSeededPointerMonkeyProbe>();
            RequireNoActiveProbe<InteractionRunnerProbe>();
            RequireNoActiveProbe<VirtualMouseEventSystemProbe>();
            if (inputProcessingBoundary == null)
                inputProcessingBoundary = GetComponent<UnityFrameInputProcessingBoundary>();
            Require(inputProcessingBoundary != null, "Add this probe to the existing AutoLab Probe object, or assign its frame boundary.");

            input = new UnityPhysicalInputDriver();
            var trackedInput = new CountingInput(input);
            var state = new AutoLabInteractionPermissionState();
            var runtime = AutomationLabInteractionComposition.CreateRuntime(
                interactionCamera, trackedInput, inputProcessingBoundary,
                new AutoLabInteractionAvailabilityPolicy(state));
            await inputProcessingBoundary.WaitAsync(token);

            InteractionTargetBinding binding = ResolveBinding("button.confirm");
            var clickObserver = Required<UnityPointerClickObserver>(binding);
            var hoverObserver = Required<UnityPointerHoverObserver>(binding);
            var clickCounter = Required<ButtonClickCounter>(binding);
            InteractionTargetSnapshot target = await CaptureTarget(runtime.TargetSource, binding.TargetId, token);
            var clickContext = InteractionAvailabilityContext.ForTarget(target, PhysicalInteractionCapabilities.PointerClick);
            var hoverContext = InteractionAvailabilityContext.ForTarget(target, PhysicalInteractionCapabilities.PointerHover);

            state.SetConfirmClickAllowed(false);
            Require(!runtime.Availability.Evaluate(clickContext).IsAvailable, "Click should be denied by the Project policy.");
            Require(runtime.Availability.Evaluate(hoverContext).IsAvailable, "Click denial must not remove Hover.");
            int originalClickCount = clickCounter.Count;
            await ExpectDenied(
                () => runtime.Runner.ExecuteAsync(new PointerClickRequest(target.Id), token).AsTask(),
                trackedInput, "autolab.confirm.click.locked");
            Require(clickCounter.Count == originalClickCount, "Denied Click changed product state.");

            hoverObserver.ResetObservation();
            await runtime.Runner.ExecuteAsync(new PointerHoverRequest(target.Id), token);
            await inputProcessingBoundary.WaitAsync(token);
            RequirePassed(UnityPointerVerificationProfiles.HoverObserved().Evaluate(
                target.Id.Value, hoverObserver.Capture(input.Mouse.deviceId)));
            Debug.Log("IA2 HOVER AVAILABLE / CLICK DENIED PASS");

            state.SetConfirmClickAllowed(true);
            Require(runtime.Availability.Evaluate(clickContext).IsAvailable, "A permission change must be visible without refresh or rebuild.");
            clickObserver.ResetObservation();
            int beforeClick = clickCounter.Count;
            await runtime.Runner.ExecuteAsync(new PointerClickRequest(target.Id), token);
            await inputProcessingBoundary.WaitAsync(token);
            RequirePassed(UnityPointerVerificationProfiles.ClickObserved().Evaluate(target.Id.Value, clickObserver.Capture()));
            Require(clickCounter.Count == beforeClick + 1, "Enabled Click did not reach the ordinary product callback exactly once.");
            Debug.Log("IA2 LIVE PROJECT PERMISSION / PRODUCT CLICK PASS");

            // This policy scopes this explicit test to a single eligible action. It is not a product rule.
            var selectionRuntime = UnityInteractionAutomationBuilder.Create()
                .UseTargetSource(runtime.TargetSource)
                .UseAvailability(runtime.Availability)
                .AddAvailabilityPolicy(new ConfirmClickOnlyPolicy())
                .UsePhysicalInput(trackedInput)
                .UseInputProcessingBoundary(inputProcessingBoundary)
                .Build();
            var monkey = UnityPointerMonkeyFactory.Create(selectionRuntime,
                new PointerMonkeyConfiguration(12345, new ScrollDelta(0, -1), TimeSpan.FromMilliseconds(1)));
            PointerMonkeyStep selected = await monkey.SelectNextAsync(1, token);
            Require(selected.Capability == PhysicalInteractionCapabilities.PointerClick, "The stale-selection test must select Click.");
            state.SetConfirmClickAllowed(false);
            await ExpectDenied(() => monkey.ExecuteAsync(selected, token).AsTask(), trackedInput, "autolab.confirm.click.locked");
            Require(monkey.GetCoverageSnapshot().CoveredCount == 0, "A rejected selected step must not receive coverage credit.");
            Require(clickCounter.Count == beforeClick + 1, "Rejected selected Click reached the product callback.");
            Debug.Log("IA2 STALE SELECTION REJECTED BEFORE INPUT PASS");

            token.ThrowIfCancellationRequested();
            Debug.Log("IA2 CAPABILITY-AWARE AVAILABILITY PASS\nNo occlusion, camera or scene-transition acceptance is claimed.");
        }
        catch (OperationCanceledException)
        {
            Debug.Log("IA2 AVAILABILITY CANCELLED — no PASS result was produced.");
        }
        catch (Exception exception)
        {
            Debug.LogError("IA2 AVAILABILITY FAILED");
            Debug.LogException(exception);
        }
        finally
        {
            input?.Dispose();
            input = null;
            var completed = cancellation;
            cancellation = null;
            completed?.Dispose();
        }
    }

    private static async Task ExpectDenied(Func<Task> execute, CountingInput input, string reason)
    {
        int before = input.Submissions;
        try { await execute(); }
        catch (InvalidOperationException exception) when (exception.Message.Contains(reason))
        {
            Require(input.Submissions == before, "A denied request submitted physical input.");
            return;
        }
        throw new InvalidOperationException("The expected availability rejection was not produced.");
    }

    private static async Task<InteractionTargetSnapshot> CaptureTarget(IInteractionTargetSource source, string id, CancellationToken token)
    {
        var targets = await source.GetTargetsAsync(token);
        bool found = false;
        InteractionTargetSnapshot result = default;
        foreach (var target in targets)
        {
            if (target.Id.Value != id) continue;
            Require(!found, "Duplicate target identity in the acceptance scope.");
            found = true;
            result = target;
        }
        Require(found, "The AutoLab target was not found.");
        return result;
    }

    private static InteractionTargetBinding ResolveBinding(string id)
    {
        InteractionTargetBinding result = null;
        foreach (var binding in new UnityLoadedSceneTargetBindingSource().CaptureBindings())
        {
            if (binding == null || binding.TargetId != id) continue;
            Require(result == null, "Duplicate AutoLab binding identity.");
            result = binding;
        }
        Require(result != null, "The existing AutoLab binding was not found.");
        return result;
    }

    private static T Required<T>(InteractionTargetBinding binding) where T : Component
    {
        T component = binding.GetComponent<T>();
        Require(component != null, $"AutoLab target requires {typeof(T).Name}.");
        return component;
    }

    private static void RequireNoActiveProbe<T>() where T : Behaviour
    {
        foreach (T probe in UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            Require(!probe.isActiveAndEnabled, $"Disable {typeof(T).Name} before running IA-2 acceptance.");
    }

    private static void RequirePassed(EvaluationReport report)
        => Require(report.Verdict == TestVerdict.Passed, $"Unity host observation verification returned {report.Verdict}.");

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private void OnDisable() => cancellation?.Cancel();
    private void OnDestroy() => cancellation?.Cancel();

    private sealed class ConfirmClickOnlyPolicy : IInteractionAvailabilityEvaluator
    {
        public InteractionAvailability Evaluate(InteractionAvailabilityContext context)
            => context.Target.Id.Value == "button.confirm" && context.Capability == PhysicalInteractionCapabilities.PointerClick
                ? InteractionAvailability.Available : InteractionAvailability.Denied("autolab.acceptance.out-of-scope");
    }

    private sealed class CountingInput : IPhysicalInputDriver
    {
        private readonly IPhysicalInputDriver inner;
        public int Submissions { get; private set; }
        public CountingInput(IPhysicalInputDriver inner) { this.inner = inner; }
        public ValueTask MovePointerAsync(InteractionPoint point, CancellationToken cancellationToken = default)
        { Submissions++; return inner.MovePointerAsync(point, cancellationToken); }
        public ValueTask PointerDownAsync(PointerButton button, CancellationToken cancellationToken = default)
        { Submissions++; return inner.PointerDownAsync(button, cancellationToken); }
        public ValueTask PointerUpAsync(PointerButton button, CancellationToken cancellationToken = default)
        { Submissions++; return inner.PointerUpAsync(button, cancellationToken); }
        public ValueTask ScrollAsync(ScrollDelta delta, CancellationToken cancellationToken = default)
        { Submissions++; return inner.ScrollAsync(delta, cancellationToken); }
        public ValueTask KeyDownAsync(PhysicalKey key, CancellationToken cancellationToken = default)
        { Submissions++; return inner.KeyDownAsync(key, cancellationToken); }
        public ValueTask KeyUpAsync(PhysicalKey key, CancellationToken cancellationToken = default)
        { Submissions++; return inner.KeyUpAsync(key, cancellationToken); }
    }
}
