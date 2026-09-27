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
using UnityEngine.UI;

/// <summary>Opt-in IA-3A runtime acceptance. Disable the other AutoLab input probes first.</summary>
public sealed class AutoLabUiStateProbe : MonoBehaviour
{
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private UnityFrameInputProcessingBoundary inputProcessingBoundary;
    private CancellationTokenSource cancellation;
    private UnityPhysicalInputDriver input;

    private async void Start()
    {
        cancellation = new CancellationTokenSource();
        CancellationToken token = cancellation.Token;
        Button button = null;
        bool originalInteractable = true;
        bool originalEnabled = true;
        GroupLease outer = null;
        GroupLease inner = null;
        try
        {
            RequireNoOtherProbe<AutoLabUiStateProbe>();
            RequireNoOtherProbe<AutoLabAvailabilityProbe>();
            RequireNoOtherProbe<AutoLabSeededPointerMonkeyProbe>();
            RequireNoOtherProbe<InteractionRunnerProbe>();
            RequireNoOtherProbe<VirtualMouseEventSystemProbe>();
            if (inputProcessingBoundary == null)
                inputProcessingBoundary = GetComponent<UnityFrameInputProcessingBoundary>();
            Require(inputProcessingBoundary != null && inputProcessingBoundary.isActiveAndEnabled,
                "Assign an active frame boundary, or add this probe to the existing AutoLab Probe object.");

            input = new UnityPhysicalInputDriver();
            var trackedInput = new CountingInput(input);
            var runtime = AutomationLabInteractionComposition.CreateRuntime(
                interactionCamera, trackedInput, inputProcessingBoundary);
            var source = runtime.TargetSource as UnityInteractionTargetSource;
            Require(source != null, "This fixture requires the composed Unity target source.");
            await inputProcessingBoundary.WaitAsync(token);

            InteractionTargetBinding binding = ResolveBinding(source.BindingSource, "button.confirm");
            button = Required<Button>(binding);
            originalInteractable = button.interactable;
            originalEnabled = button.enabled;
            var clickObserver = Required<UnityPointerClickObserver>(binding);
            var hoverObserver = Required<UnityPointerHoverObserver>(binding);
            var counter = Required<ButtonClickCounter>(binding);
            var target = await CaptureTarget(source, binding.TargetId, token);
            var click = InteractionAvailabilityContext.ForTarget(target, PhysicalInteractionCapabilities.PointerClick);
            var hover = InteractionAvailabilityContext.ForTarget(target, PhysicalInteractionCapabilities.PointerHover);

            button.enabled = true;
            button.interactable = false;
            await inputProcessingBoundary.WaitAsync(token);
            Require(runtime.Availability.Evaluate(click).Reasons == InteractionUnavailableReasons.NotInteractable,
                "A non-interactable Button must produce the UI-state failure, not Project policy denial.");
            Require(runtime.Availability.Evaluate(hover).IsAvailable, "Click denial must not remove Hover.");
            int clicksBeforeDenial = counter.Count;
            await ExpectDenied(() => runtime.Runner.ExecuteAsync(new PointerClickRequest(target.Id), token).AsTask(), trackedInput);
            Require(counter.Count == clicksBeforeDenial, "Denied Click reached the product.");
            hoverObserver.ResetObservation();
            await runtime.Runner.ExecuteAsync(new PointerHoverRequest(target.Id), token);
            await inputProcessingBoundary.WaitAsync(token);
            RequirePassed(UnityPointerVerificationProfiles.HoverObserved().Evaluate(
                target.Id.Value, hoverObserver.Capture(input.Mouse.deviceId)));
            Debug.Log("IA3A SELECTABLE DENIAL / HOVER PASS");

            button.interactable = true;
            await inputProcessingBoundary.WaitAsync(token);
            Require(runtime.Availability.Evaluate(click).IsAvailable, "UI unlock was not read by the existing evaluator.");
            await VerifyRealClick(runtime, target.Id, clickObserver, counter, token);
            Debug.Log("IA3A LIVE UI UNLOCK / PRODUCT CLICK PASS");

            Require(binding.transform.parent != null, "The AutoLab control must have a parent for the CanvasGroup fixture.");
            outer = GroupLease.Acquire(binding.transform.parent.gameObject);
            inner = GroupLease.Acquire(binding.gameObject);
            outer.Group.enabled = true;
            outer.Group.interactable = false;
            inner.Group.enabled = true;
            inner.Group.interactable = true;
            inner.Group.ignoreParentGroups = false;
            await inputProcessingBoundary.WaitAsync(token);
            Require(!button.IsInteractable(), "Unity did not apply the parent CanvasGroup lock.");
            Require(runtime.Availability.Evaluate(click).Reasons == InteractionUnavailableReasons.NotInteractable,
                "Parent CanvasGroup lock was not observed.");
            await ExpectDenied(() => runtime.Runner.ExecuteAsync(new PointerClickRequest(target.Id), token).AsTask(), trackedInput);
            inner.Group.ignoreParentGroups = true;
            await inputProcessingBoundary.WaitAsync(token);
            Require(button.IsInteractable() && runtime.Availability.Evaluate(click).IsAvailable,
                "ignoreParentGroups did not restore the declared control interaction.");
            await VerifyRealClick(runtime, target.Id, clickObserver, counter, token);
            Debug.Log("IA3A CANVAS GROUP / IGNORE PARENT GROUPS PASS");

            outer.Group.interactable = true;
            inner.Group.ignoreParentGroups = false;
            await inputProcessingBoundary.WaitAsync(token);
            var selectionRuntime = UnityInteractionAutomationBuilder.Create()
                .UseTargetSource(runtime.TargetSource).UseAvailability(runtime.Availability)
                .AddAvailabilityPolicy(new ConfirmClickOnlyPolicy())
                .UsePhysicalInput(trackedInput).UseInputProcessingBoundary(inputProcessingBoundary).Build();
            var monkey = UnityPointerMonkeyFactory.Create(selectionRuntime,
                new PointerMonkeyConfiguration(12345, new ScrollDelta(0, -1), TimeSpan.FromMilliseconds(1)));
            var selected = await monkey.SelectNextAsync(1, token);
            Require(selected.Capability == PhysicalInteractionCapabilities.PointerClick, "The stale-selection fixture must select Click.");
            button.interactable = false;
            await inputProcessingBoundary.WaitAsync(token);
            int clicksBeforeStaleExecution = counter.Count;
            await ExpectDenied(() => monkey.ExecuteAsync(selected, token).AsTask(), trackedInput);
            Require(monkey.GetCoverageSnapshot().CoveredCount == 0 && counter.Count == clicksBeforeStaleExecution,
                "A stale rejected action received coverage or reached the product.");
            Debug.Log("IA3A STALE UI SELECTION / ZERO INPUT PASS");
            token.ThrowIfCancellationRequested();
            Debug.Log("IA3A AUTOMATIC UI STATE PASS\nNo occlusion, raycast, camera or scene-transition acceptance is claimed.");
        }
        catch (OperationCanceledException)
        {
            Debug.Log("IA3A UI STATE CANCELLED — no PASS result was produced.");
        }
        catch (Exception exception)
        {
            Debug.LogError("IA3A UI STATE FAILED");
            Debug.LogException(exception);
        }
        finally
        {
            if (button != null)
            {
                button.interactable = originalInteractable;
                button.enabled = originalEnabled;
            }
            inner?.Restore();
            outer?.Restore();
            input?.Dispose();
            input = null;
            var completed = cancellation;
            cancellation = null;
            completed?.Dispose();
        }
    }

    private async Task VerifyRealClick(UnityInteractionAutomationRuntime runtime, InteractionTargetId id,
        UnityPointerClickObserver observer, ButtonClickCounter counter, CancellationToken token)
    {
        observer.ResetObservation();
        int before = counter.Count;
        await runtime.Runner.ExecuteAsync(new PointerClickRequest(id), token);
        await inputProcessingBoundary.WaitAsync(token);
        RequirePassed(UnityPointerVerificationProfiles.ClickObserved().Evaluate(id.Value, observer.Capture()));
        Require(counter.Count == before + 1, "Click did not reach the ordinary product callback exactly once.");
    }

    private static async Task ExpectDenied(Func<Task> execute, CountingInput input)
    {
        int before = input.Submissions;
        try { await execute(); }
        catch (InvalidOperationException exception) when (exception.Message.Contains("unity.ui.selectable.not-interactable"))
        {
            Require(input.Submissions == before, "A UI-state rejection submitted physical input.");
            return;
        }
        throw new InvalidOperationException("The expected UI-state rejection was not produced.");
    }

    private static async Task<InteractionTargetSnapshot> CaptureTarget(IInteractionTargetSource source, string id, CancellationToken token)
    {
        bool found = false;
        InteractionTargetSnapshot result = default;
        foreach (var target in await source.GetTargetsAsync(token))
        {
            if (target.Id.Value != id) continue;
            Require(!found, "Duplicate target identity.");
            found = true;
            result = target;
        }
        Require(found, "The AutoLab target was not captured.");
        return result;
    }

    private static InteractionTargetBinding ResolveBinding(IUnityInteractionTargetBindingSource scope, string id)
    {
        InteractionTargetBinding result = null;
        foreach (var binding in scope.CaptureBindings())
        {
            if (binding == null || binding.TargetId != id) continue;
            Require(result == null, "Duplicate binding identity.");
            result = binding;
        }
        Require(result != null, "The AutoLab binding was not found in the composed scope.");
        return result;
    }
    private static T Required<T>(InteractionTargetBinding binding) where T : Component
    {
        T component = binding.GetComponent<T>();
        Require(component != null, $"AutoLab target requires {typeof(T).Name}.");
        return component;
    }
    private void RequireNoOtherProbe<T>() where T : Behaviour
    {
        foreach (T probe in UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            if (!ReferenceEquals(probe, this))
                Require(!probe.isActiveAndEnabled, $"Disable {typeof(T).Name} before IA-3A acceptance.");
    }
    private static void RequirePassed(EvaluationReport report)
        => Require(report.Verdict == TestVerdict.Passed, $"Unity observation returned {report.Verdict}.");
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
    private void OnDisable() => cancellation?.Cancel();
    private void OnDestroy() => cancellation?.Cancel();

    private sealed class GroupLease
    {
        public CanvasGroup Group { get; }
        private readonly bool created;
        private readonly bool interactable;
        private readonly bool ignoreParents;
        private readonly bool enabled;
        private GroupLease(CanvasGroup group, bool created)
        {
            Group = group;
            this.created = created;
            interactable = group.interactable;
            ignoreParents = group.ignoreParentGroups;
            enabled = group.enabled;
        }
        public static GroupLease Acquire(GameObject go)
        {
            CanvasGroup[] groups = go.GetComponents<CanvasGroup>();
            Require(groups.Length <= 1, "This fixture requires at most one CanvasGroup per tested object.");
            return groups.Length == 1 ? new GroupLease(groups[0], false)
                : new GroupLease(go.AddComponent<CanvasGroup>(), true);
        }
        public void Restore()
        {
            if (Group == null) return;
            Group.interactable = interactable;
            Group.ignoreParentGroups = ignoreParents;
            Group.enabled = enabled;
            if (created) UnityEngine.Object.Destroy(Group);
        }
    }
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
