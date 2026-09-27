using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Framework.InteractionAutomation.Monkey;
using Framework.InteractionAutomation.Monkey.Unity3D;
using Framework.InteractionAutomation.Runner;
using Framework.InteractionAutomation.Runner.Unity3D;
using Module.InteractionAutomation.Availability;
using Module.InteractionAutomation.Coordinates.Unity3D;
using Module.InteractionAutomation.Observation.Unity3D;
using Module.InteractionAutomation.PhysicalInput;
using Module.InteractionAutomation.Targets;
using Module.InteractionAutomation.Targets.Unity3D;
using Module.InteractionAutomation.Timing;
using Module.InteractionAutomation.Verification.Unity3D;
using Module.Verification.Oracle;
using Project.InteractionAutomationLab.Automation;
using Project.InteractionAutomationLab.Domain;
using Project.InteractionAutomationLab.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.InteractionAutomationLab.Scenarios
{
    public sealed class LabAcceptanceSuite
    {
        private readonly LabProductController product;
        private readonly LabTargetBindingSource bindings;
        private readonly CountingPhysicalInputDriver input;
        private readonly IHostInputProcessingBoundary boundary;
        private readonly int pointerDeviceId;
        private readonly UnityInteractionAutomationRuntime runtime;
        private readonly UnityInteractionAutomationRuntime negativeRuntime;

        public LabAcceptanceSuite(LabProductController product, LabTargetBindingSource bindings,
            CountingPhysicalInputDriver input, IHostInputProcessingBoundary boundary, int pointerDeviceId)
        {
            this.product = product ?? throw new ArgumentNullException(nameof(product));
            this.bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            this.boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
            this.pointerDeviceId = pointerDeviceId;
            runtime = LabComposition.CreateProductionRuntime(product, input, boundary, bindings);
            negativeRuntime = LabComposition.CreateProductNegativeInputRuntime(input, boundary, bindings);
        }

        public async ValueTask<LabAcceptanceReport> RunAsync(CancellationToken cancellationToken)
        {
            LabAcceptanceReport report = new LabAcceptanceReport();
            report.runId = "scripted-" + Guid.NewGuid().ToString("N");
            report.startedUtc = DateTime.UtcNow.ToString("O");
            await RunScenarioAsync(report, "S01", RunS01Async, cancellationToken);
            await RunScenarioAsync(report, "S02", RunS02Async, cancellationToken);
            await RunScenarioAsync(report, "S03", RunS03Async, cancellationToken);
            await RunScenarioAsync(report, "S04", RunS04Async, cancellationToken);
            await RunScenarioAsync(report, "S05", RunS05Async, cancellationToken);
            await RunScenarioAsync(report, "S06", RunS06Async, cancellationToken);
            await RunScenarioAsync(report, "S07", RunS07Async, cancellationToken);
            await RunScenarioAsync(report, "S08", RunS08Async, cancellationToken);
            await RunScenarioAsync(report, "S10", RunS10Async, cancellationToken);
            report.completedUtc = DateTime.UtcNow.ToString("O");
            return report;
        }

        private async ValueTask RunScenarioAsync(LabAcceptanceReport report, string id,
            Func<CancellationToken, ValueTask<string>> scenario, CancellationToken cancellationToken)
        {
            int before = input.SubmissionCount;
            LabScenarioResult result = new LabScenarioResult();
            result.id = id;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                result.detail = await scenario(cancellationToken);
                result.status = LabScenarioStatus.Passed;
            }
            catch (OperationCanceledException)
            {
                result.status = LabScenarioStatus.Cancelled;
                result.detail = "Cancelled after cleanup.";
                report.scenarios.Add(result);
                throw;
            }
            catch (Exception exception)
            {
                result.status = LabScenarioStatus.Failed;
                result.detail = exception.GetType().Name + ": " + exception.Message;
            }
            finally
            {
                result.physicalSubmissionDelta = input.SubmissionCount - before;
                if (!report.scenarios.Contains(result)) report.scenarios.Add(result);
                RestoreBaseline();
                await boundary.WaitAsync(CancellationToken.None);
            }
        }

        private async ValueTask<string> RunS01Async(CancellationToken cancellationToken)
        {
            RestoreBaseline();
            Button useButton = GetBinding(LabTargetCatalog.UsePotion).GetComponent<Button>();
            useButton.interactable = false;
            await boundary.WaitAsync(cancellationToken);
            InteractionAvailability click = await QueryAsync(LabTargetCatalog.UsePotion, PhysicalInteractionCapabilities.PointerClick, cancellationToken);
            InteractionAvailability hover = await QueryAsync(LabTargetCatalog.UsePotion, PhysicalInteractionCapabilities.PointerHover, cancellationToken);
            Require(!click.IsAvailable && HasReason(click, InteractionUnavailableReasons.NotInteractable), "UsePotion Click must be NotInteractable.");
            Require(hover.IsAvailable, "UsePotion Hover must remain available.");
            int beforeInput = input.SubmissionCount;
            int beforePotion = product.State.PotionCount;
            int beforeAttempts = product.State.UseAttemptCount;
            await ExpectDeniedAsync(runtime.Runner.ExecuteAsync(new PointerClickRequest(Id(LabTargetCatalog.UsePotion))), cancellationToken);
            Require(input.SubmissionCount == beforeInput, "Denied click submitted physical input.");
            Require(product.State.PotionCount == beforePotion && product.State.UseAttemptCount == beforeAttempts, "Denied click mutated product state.");
            await ExecuteHoverAsync(LabTargetCatalog.UsePotion, cancellationToken);
            useButton.interactable = true;
            await boundary.WaitAsync(cancellationToken);
            await ExecuteClickAsync(runtime, LabTargetCatalog.UsePotion, cancellationToken);
            Require(product.State.PotionCount == beforePotion - 1 && product.State.UseSuccessCount == 1, "Enabled UsePotion did not consume exactly one potion.");
            InteractionAvailability canaryClick = await QueryAsync(LabTargetCatalog.DisabledSelectable, PhysicalInteractionCapabilities.PointerClick, cancellationToken);
            InteractionAvailability canaryHover = await QueryAsync(LabTargetCatalog.DisabledSelectable, PhysicalInteractionCapabilities.PointerHover, cancellationToken);
            Require(HasReason(canaryClick, InteractionUnavailableReasons.Disabled), "Disabled selectable Click must report Disabled.");
            Require(canaryHover.IsAvailable, "Disabled selectable Hover must remain available.");
            return "Click/Hover split, zero-input denial and product mutation verified.";
        }

        private async ValueTask<string> RunS02Async(CancellationToken cancellationToken)
        {
            RestoreBaseline();
            CanvasGroup root = product.GameplayLockGroup;
            CanvasGroup bypass = product.QuickBypassGroup;
            await AssertLockRowAsync(true, true, true, true, true, cancellationToken);
            await AssertLockRowAsync(false, false, true, false, false, cancellationToken);
            await AssertLockRowAsync(false, true, true, false, true, cancellationToken);
            await AssertLockRowAsync(false, true, false, false, false, cancellationToken);
            await AssertLockRowAsync(true, true, true, true, true, cancellationToken);
            root.interactable = false;
            bypass.ignoreParentGroups = true;
            bypass.interactable = true;
            await boundary.WaitAsync(cancellationToken);
            int beforeHelp = product.State.HelpOpenCount;
            await ExecuteClickAsync(runtime, LabTargetCatalog.Help, cancellationToken);
            Require(product.State.HelpOpenCount == beforeHelp + 1, "Bypass Help did not execute.");
            InteractionAvailability hover = await QueryAsync(LabTargetCatalog.BuyPotion, PhysicalInteractionCapabilities.PointerHover, cancellationToken);
            Require(hover.IsAvailable, "CanvasGroup lock incorrectly denied Hover.");
            return "Five CanvasGroup/bypass rows and representative input verified.";
        }

        private async ValueTask<string> RunS03Async(CancellationToken cancellationToken)
        {
            RestoreBaseline();
            product.GameplayLockGroup.interactable = false;
            product.QuickBypassGroup.ignoreParentGroups = true;
            product.QuickBypassGroup.interactable = true;
            await boundary.WaitAsync(cancellationToken);
            await ExecuteClickAsync(runtime, LabTargetCatalog.Help, cancellationToken);
            await ExecuteClickAsync(runtime, LabTargetCatalog.Emergency, cancellationToken);
            int emergency = product.State.EmergencyActionCount;
            product.SetFixtureState(LabGameMode.Cutscene, false, product.State.Gold, product.State.PotionCount);
            await ExecuteClickAsync(runtime, LabTargetCatalog.Help, cancellationToken);
            int beforeInput = input.SubmissionCount;
            await ExpectDeniedAsync(runtime.Runner.ExecuteAsync(new PointerClickRequest(Id(LabTargetCatalog.Emergency))), cancellationToken);
            Require(input.SubmissionCount == beforeInput && product.State.EmergencyActionCount == emergency, "Cutscene Emergency denial was not clean.");
            product.GameplayLockGroup.interactable = true;
            Button buy = GetBinding(LabTargetCatalog.BuyPotion).GetComponent<Button>();
            buy.interactable = false;
            await boundary.WaitAsync(cancellationToken);
            InteractionAvailability combined = await QueryAsync(LabTargetCatalog.BuyPotion, PhysicalInteractionCapabilities.PointerClick, cancellationToken);
            Require(HasReason(combined, InteractionUnavailableReasons.NotInteractable) && HasReason(combined, InteractionUnavailableReasons.PolicyDenied), "Composite reasons did not retain UI and policy denials.");
            return "UI bypass and game policy remained independent; composite denial retained both reasons.";
        }

        private async ValueTask<string> RunS04Async(CancellationToken cancellationToken)
        {
            RestoreBaseline();
            await RunSelectionInvalidationAsync(PhysicalInteractionCapabilities.PointerClick, LabTargetCatalog.UsePotion, string.Empty, cancellationToken);
            RestoreBaseline();
            await RunSelectionInvalidationAsync(PhysicalInteractionCapabilities.PointerClick, LabTargetCatalog.Emergency, string.Empty, cancellationToken);
            RestoreBaseline();
            await RunSelectionInvalidationAsync(PhysicalInteractionCapabilities.PointerDrag, LabTargetCatalog.Sword, LabTargetCatalog.Weapon, cancellationToken);
            return "Three real Monkey selections were invalidated before execution with zero input and zero coverage.";
        }

        private async ValueTask<string> RunS05Async(CancellationToken cancellationToken)
        {
            RestoreBaseline();
            await ExecuteDragAsync(LabTargetCatalog.Sword, LabTargetCatalog.Weapon, cancellationToken);
            Require(product.State.EquippedWeaponId == "sword", "Sword was not equipped.");
            await ExecuteDragAsync(LabTargetCatalog.Shield, LabTargetCatalog.Offhand, cancellationToken);
            Require(product.State.EquippedOffhandId == "shield", "Shield was not equipped.");
            await ExecuteDragAsync(LabTargetCatalog.Sword, LabTargetCatalog.Weapon, cancellationToken);
            Require(product.State.EquippedWeaponId == "sword", "Repeated equip changed the item.");
            LabItemCardControl sword = GetBinding(LabTargetCatalog.Sword).GetComponent<LabItemCardControl>();
            sword.interactable = false;
            await boundary.WaitAsync(cancellationToken);
            InteractionAvailability source = await QueryDragAsync(LabTargetCatalog.Sword, LabTargetCatalog.Weapon, true, cancellationToken);
            Require(HasReason(source, InteractionUnavailableReasons.NotInteractable), "Disabled drag source was not rejected.");
            sword.interactable = true;
            LabEquipmentSlotControl weapon = GetBinding(LabTargetCatalog.Weapon).GetComponent<LabEquipmentSlotControl>();
            weapon.interactable = false;
            await boundary.WaitAsync(cancellationToken);
            InteractionAvailability destination = await QueryDragAsync(LabTargetCatalog.Sword, LabTargetCatalog.Weapon, false, cancellationToken);
            Require(destination.IsAvailable, "Destination incorrectly inherited source Selectable rules.");
            await ExecuteDragAsync(LabTargetCatalog.Sword, LabTargetCatalog.Weapon, cancellationToken);
            product.SetFixtureState(LabGameMode.Normal, true, product.State.Gold, product.State.PotionCount);
            int before = input.SubmissionCount;
            await ExpectDeniedAsync(runtime.Runner.ExecuteAsync(new PointerDragRequest(Id(LabTargetCatalog.Sword), Id(LabTargetCatalog.Weapon))), cancellationToken);
            Require(input.SubmissionCount == before, "Equipment lock denial submitted input.");
            return "Source, destination, legitimate no-op and equipment policy responsibilities verified.";
        }

        private async ValueTask<string> RunS06Async(CancellationToken cancellationToken)
        {
            RestoreBaseline();
            product.SetFixtureState(LabGameMode.Normal, false, 10, 0);
            await ExecuteClickAsync(runtime, LabTargetCatalog.BuyPotion, cancellationToken);
            Require(product.State.Gold == 0 && product.State.PotionCount == 1, "First purchase arithmetic failed.");
            await ExecuteClickAsync(runtime, LabTargetCatalog.BuyPotion, cancellationToken);
            Require(product.State.Gold == 0 && product.State.PotionCount == 1 && product.State.LastOutcome == "InsufficientCurrency", "Insufficient-currency rejection failed.");
            await ExecuteClickAsync(runtime, LabTargetCatalog.UsePotion, cancellationToken);
            await ExecuteClickAsync(runtime, LabTargetCatalog.UsePotion, cancellationToken);
            Require(product.State.PotionCount == 0 && product.State.LastOutcome == "NoPotion", "No-potion rejection failed.");
            InteractionAvailability buy = await QueryAsync(LabTargetCatalog.BuyPotion, PhysicalInteractionCapabilities.PointerClick, cancellationToken);
            Require(buy.IsAvailable && product.State.BuyAttemptCount == 2 && product.State.BuySuccessCount == 1, "Resource state incorrectly changed availability or counters.");
            return "Resource exhaustion remained an available product attempt with correct rejection outcomes.";
        }

        private async ValueTask<string> RunS07Async(CancellationToken cancellationToken)
        {
            RestoreBaseline();
            await ExecuteClickAsync(runtime, LabTargetCatalog.Sword, cancellationToken);
            await ExecuteHoverAsync(LabTargetCatalog.Key, cancellationToken);
            await ExecuteDoubleClickAsync(LabTargetCatalog.Inspect, cancellationToken);
            Require(product.State.DetailsExpanded, "DoubleClick did not expand details.");
            await ExecuteHoldAsync(LabTargetCatalog.Charge, cancellationToken);
            Require(product.State.ChargeCompletedCount == 1, "Hold did not complete charge exactly once.");
            await ExecuteDragAsync(LabTargetCatalog.Sword, LabTargetCatalog.Weapon, cancellationToken);
            await ExecuteClickAsync(runtime, LabTargetCatalog.NavigationSettings, cancellationToken);
            await ExecuteScrollAsync(LabTargetCatalog.LogScroll, cancellationToken);
            bool music = product.State.MusicEnabled;
            bool sfx = product.State.SfxEnabled;
            await ExecuteClickAsync(runtime, LabTargetCatalog.Music, cancellationToken);
            await ExecuteClickAsync(runtime, LabTargetCatalog.Sfx, cancellationToken);
            Require(product.State.MusicEnabled != music && product.State.SfxEnabled != sfx, "Toggles did not change exactly once.");
            return "Click, Hover, DoubleClick, Hold, Drag, Scroll and Toggle host/product paths verified.";
        }

        private async ValueTask<string> RunS08Async(CancellationToken cancellationToken)
        {
            RestoreBaseline();
            PointerClickRequest staleRequest = new PointerClickRequest(Id(LabTargetCatalog.UsePotion));
            await ExecuteClickAsync(runtime, LabTargetCatalog.NavigationSettings, cancellationToken);
            Require(!ContainsBinding(LabTargetCatalog.UsePotion) && ContainsBinding(LabTargetCatalog.Music), "Page discovery did not update.");
            int before = input.SubmissionCount;
            await ExpectDeniedAsync(runtime.Runner.ExecuteAsync(staleRequest), cancellationToken);
            Require(input.SubmissionCount == before, "Missing stale target submitted input.");
            await ExecuteClickAsync(runtime, LabTargetCatalog.NavigationInventory, cancellationToken);
            Require(ContainsBinding(LabTargetCatalog.UsePotion) && !ContainsBinding(LabTargetCatalog.Music), "Returning page did not rediscover targets.");
            return "Active/inactive page discovery and stale request rejection verified without scene transition.";
        }

        private async ValueTask<string> RunS10Async(CancellationToken cancellationToken)
        {
            RestoreBaseline();
            product.SetFixtureState(LabGameMode.Cutscene, false, 30, 2);
            UnityPointerClickObserver observer = GetBinding(LabTargetCatalog.BuyPotion).GetComponent<UnityPointerClickObserver>();
            observer.ResetObservation();
            int beforeGold = product.State.Gold;
            int beforePotion = product.State.PotionCount;
            await negativeRuntime.Runner.ExecuteAsync(new PointerClickRequest(Id(LabTargetCatalog.BuyPotion)), cancellationToken);
            await boundary.WaitAsync(cancellationToken);
            EvaluationReport host = UnityPointerVerificationProfiles.ClickObserved().Evaluate("S10", observer.Capture());
            Require(host.Verdict == TestVerdict.Passed, "Negative runtime click was not observed by host.");
            Require(product.State.Gold == beforeGold && product.State.PotionCount == beforePotion && product.State.LastOutcome == "GameplayInputLocked", "Product failed to reject structurally delivered Cutscene input.");
            return "Structural-only negative runtime delivered input and product defense rejected mutation.";
        }

        private async ValueTask AssertLockRowAsync(bool rootInteractable, bool bypassIgnoresParent, bool bypassInteractable,
            bool expectedBuy, bool expectedHelp, CancellationToken cancellationToken)
        {
            product.GameplayLockGroup.interactable = rootInteractable;
            product.QuickBypassGroup.ignoreParentGroups = bypassIgnoresParent;
            product.QuickBypassGroup.interactable = bypassInteractable;
            await boundary.WaitAsync(cancellationToken);
            InteractionAvailability buy = await QueryAsync(LabTargetCatalog.BuyPotion, PhysicalInteractionCapabilities.PointerClick, cancellationToken);
            InteractionAvailability help = await QueryAsync(LabTargetCatalog.Help, PhysicalInteractionCapabilities.PointerClick, cancellationToken);
            Require(buy.IsAvailable == expectedBuy, "Unexpected Buy availability in CanvasGroup matrix.");
            Require(help.IsAvailable == expectedHelp, "Unexpected Help availability in CanvasGroup matrix.");
        }

        private async ValueTask RunSelectionInvalidationAsync(PhysicalInteractionCapabilities capability, string sourceId,
            string destinationId, CancellationToken cancellationToken)
        {
            LabActionScopePolicy scopePolicy = new LabActionScopePolicy(capability, sourceId, destinationId);
            UnityInteractionAutomationRuntime scopedRuntime = LabComposition.CreateProductionRuntime(product, input, boundary, bindings, scopePolicy);
            SeededPointerMonkey monkey = UnityPointerMonkeyFactory.Create(scopedRuntime,
                new PointerMonkeyConfiguration(12345, new ScrollDelta(0, -1), TimeSpan.FromMilliseconds(250)), new LabDragDestinationPolicy());
            PointerMonkeyStep selected = await monkey.SelectNextAsync(1, cancellationToken);
            Require(selected.TargetId.Value == sourceId && selected.Capability == capability, "Scoped Monkey selected an unexpected action.");
            int beforeInput = input.SubmissionCount;
            LabBusinessSnapshot beforeState = product.CaptureBusinessState();
            InvalidateSelection(sourceId, capability);
            await ExpectDeniedAsync(monkey.ExecuteAsync(selected, cancellationToken), cancellationToken);
            Require(input.SubmissionCount == beforeInput, "Invalidated selection submitted physical input.");
            Require(monkey.GetCoverageSnapshot().CoveredCount == 0, "Invalidated selection increased coverage.");
            Require(BusinessEquivalent(beforeState, product.CaptureBusinessState()), "Invalidated selection mutated business state.");
            RestoreSelection(sourceId, capability);
            PointerMonkeyStep retry = await monkey.SelectNextAsync(2, cancellationToken);
            await monkey.ExecuteAsync(retry, cancellationToken);
            Require(monkey.GetCoverageSnapshot().CoveredCount == 1, "Fresh selection did not execute after state restoration.");
        }

        private void InvalidateSelection(string sourceId, PhysicalInteractionCapabilities capability)
        {
            if (sourceId == LabTargetCatalog.UsePotion) GetBinding(sourceId).GetComponent<Button>().interactable = false;
            else if (sourceId == LabTargetCatalog.Emergency) product.SetFixtureState(LabGameMode.Cutscene, false, product.State.Gold, product.State.PotionCount);
            else if (capability == PhysicalInteractionCapabilities.PointerDrag) product.SetFixtureState(LabGameMode.Normal, true, product.State.Gold, product.State.PotionCount);
        }

        private void RestoreSelection(string sourceId, PhysicalInteractionCapabilities capability)
        {
            if (sourceId == LabTargetCatalog.UsePotion) GetBinding(sourceId).GetComponent<Button>().interactable = true;
            else product.SetFixtureState(LabGameMode.Normal, false, product.State.Gold, product.State.PotionCount);
        }

        private async ValueTask ExecuteClickAsync(UnityInteractionAutomationRuntime selectedRuntime, string targetId, CancellationToken cancellationToken)
        {
            InteractionTargetBinding binding = GetBinding(targetId);
            UnityPointerClickObserver observer = binding.GetComponent<UnityPointerClickObserver>();
            observer.ResetObservation();
            await selectedRuntime.Runner.ExecuteAsync(new PointerClickRequest(Id(targetId)), cancellationToken);
            await boundary.WaitAsync(cancellationToken);
            EvaluationReport report = UnityPointerVerificationProfiles.ClickObserved().Evaluate(targetId, observer.Capture());
            string raycast = await DescribeRaycastAsync(targetId, cancellationToken);
            string detail = "Click host verification failed for " + targetId + ". Events=" + observer.EventCount +
                " Screen=" + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height + " LastOutcome=" + product.State.LastOutcome + " " + raycast;
            Require(report.Verdict == TestVerdict.Passed, detail);
        }

        private async ValueTask ExecuteHoverAsync(string targetId, CancellationToken cancellationToken)
        {
            InteractionTargetBinding binding = GetBinding(targetId);
            UnityPointerHoverObserver observer = binding.GetComponent<UnityPointerHoverObserver>();
            observer.ResetCountersPreservingHoverState();
            await runtime.Runner.ExecuteAsync(new PointerHoverRequest(Id(targetId)), cancellationToken);
            await boundary.WaitAsync(cancellationToken);
            EvaluationReport report = UnityPointerVerificationProfiles.HoverObserved().Evaluate(targetId, observer.Capture(pointerDeviceId));
            Require(report.Verdict == TestVerdict.Passed, "Hover host verification failed for " + targetId +
                ". EnterCount=" + observer.GetEnterCount(pointerDeviceId) + " Device=" + pointerDeviceId + ".");
        }

        private async ValueTask ExecuteDoubleClickAsync(string targetId, CancellationToken cancellationToken)
        {
            InteractionTargetBinding binding = GetBinding(targetId);
            UnityPointerClickObserver observer = binding.GetComponent<UnityPointerClickObserver>();
            observer.ResetObservation();
            await runtime.Runner.ExecuteAsync(new PointerDoubleClickRequest(Id(targetId)), cancellationToken);
            await boundary.WaitAsync(cancellationToken);
            EvaluationReport report = UnityPointerVerificationProfiles.DoubleClick().Evaluate(targetId, observer.Capture());
            Require(report.Verdict == TestVerdict.Passed, "DoubleClick host verification failed.");
        }

        private async ValueTask ExecuteHoldAsync(string targetId, CancellationToken cancellationToken)
        {
            InteractionTargetBinding binding = GetBinding(targetId);
            UnityPointerPressObserver observer = binding.GetComponent<UnityPointerPressObserver>();
            observer.ResetObservation();
            await runtime.Runner.ExecuteAsync(new PointerHoldRequest(Id(targetId), TimeSpan.FromMilliseconds(250)), cancellationToken);
            await boundary.WaitAsync(cancellationToken);
            EvaluationReport report = UnityPointerVerificationProfiles.HoldLifecycle().Evaluate(targetId, observer.Capture());
            Require(report.Verdict == TestVerdict.Passed, "Hold host verification failed.");
        }

        private async ValueTask ExecuteDragAsync(string sourceId, string destinationId, CancellationToken cancellationToken)
        {
            InteractionTargetBinding source = GetBinding(sourceId);
            InteractionTargetBinding destination = GetBinding(destinationId);
            UnityPointerDragObserver sourceObserver = source.GetComponent<UnityPointerDragObserver>();
            UnityPointerDropObserver destinationObserver = destination.GetComponent<UnityPointerDropObserver>();
            sourceObserver.ResetObservation();
            destinationObserver.ResetObservation();
            await runtime.Runner.ExecuteAsync(new PointerDragRequest(Id(sourceId), Id(destinationId)), cancellationToken);
            await boundary.WaitAsync(cancellationToken);
            EvaluationReport report = UnityPointerVerificationProfiles.DragLifecycle().Evaluate(sourceId, sourceObserver.Capture());
            Require(report.Verdict == TestVerdict.Passed && destinationObserver.DropCount > 0, "Drag/drop host verification failed.");
        }

        private async ValueTask ExecuteScrollAsync(string targetId, CancellationToken cancellationToken)
        {
            InteractionTargetBinding binding = GetBinding(targetId);
            UnityPointerScrollObserver observer = binding.GetComponent<UnityPointerScrollObserver>();
            observer.ResetObservation();
            await runtime.Runner.ExecuteAsync(new PointerScrollRequest(Id(targetId), new ScrollDelta(0, -1)), cancellationToken);
            await boundary.WaitAsync(cancellationToken);
            EvaluationReport report = UnityPointerVerificationProfiles.ScrollObserved().Evaluate(targetId, observer.Capture());
            Require(report.Verdict == TestVerdict.Passed, "Scroll host verification failed.");
        }

        private async ValueTask<InteractionAvailability> QueryAsync(string targetId, PhysicalInteractionCapabilities capability, CancellationToken cancellationToken)
        {
            InteractionTargetSnapshot target = await GetSnapshotAsync(targetId, cancellationToken);
            return runtime.Availability.Evaluate(InteractionAvailabilityContext.ForTarget(target, capability));
        }

        private async ValueTask<InteractionAvailability> QueryDragAsync(string sourceId, string destinationId, bool sourceRole, CancellationToken cancellationToken)
        {
            InteractionTargetSnapshot source = await GetSnapshotAsync(sourceId, cancellationToken);
            InteractionTargetSnapshot destination = await GetSnapshotAsync(destinationId, cancellationToken);
            InteractionAvailabilityContext context = sourceRole ? InteractionAvailabilityContext.ForDragSource(source, destination) :
                InteractionAvailabilityContext.ForDragDestination(source, destination);
            return runtime.Availability.Evaluate(context);
        }

        private async ValueTask<InteractionTargetSnapshot> GetSnapshotAsync(string targetId, CancellationToken cancellationToken)
        {
            IReadOnlyList<InteractionTargetSnapshot> targets = await runtime.TargetSource.GetTargetsAsync(cancellationToken);
            foreach (InteractionTargetSnapshot target in targets)
            {
                if (target.Id.Value == targetId) return target;
            }
            throw new InvalidOperationException("Target was not discovered: " + targetId);
        }

        private InteractionTargetBinding GetBinding(string targetId)
        {
            foreach (InteractionTargetBinding binding in bindings.CaptureBindings())
            {
                if (binding.TargetId == targetId) return binding;
            }
            throw new InvalidOperationException("Binding was not discovered: " + targetId);
        }

        private bool ContainsBinding(string targetId)
        {
            foreach (InteractionTargetBinding binding in bindings.CaptureBindings())
            {
                if (binding.TargetId == targetId) return true;
            }
            return false;
        }

        private static async ValueTask ExpectDeniedAsync(ValueTask operation, CancellationToken cancellationToken)
        {
            try
            {
                await operation;
            }
            catch (InvalidOperationException)
            {
                return;
            }
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Expected the interaction to be denied.");
        }

        private void RestoreBaseline()
        {
            product.ResetProduct();
            Button use = TryGetActiveBinding(LabTargetCatalog.UsePotion)?.GetComponent<Button>();
            if (use != null) use.interactable = true;
            Button buy = TryGetActiveBinding(LabTargetCatalog.BuyPotion)?.GetComponent<Button>();
            if (buy != null) buy.interactable = true;
            LabItemCardControl sword = TryGetActiveBinding(LabTargetCatalog.Sword)?.GetComponent<LabItemCardControl>();
            if (sword != null) sword.interactable = true;
            LabEquipmentSlotControl weapon = TryGetActiveBinding(LabTargetCatalog.Weapon)?.GetComponent<LabEquipmentSlotControl>();
            if (weapon != null) weapon.interactable = true;
        }

        private InteractionTargetBinding TryGetActiveBinding(string targetId)
        {
            foreach (InteractionTargetBinding binding in bindings.CaptureBindings())
            {
                if (binding.TargetId == targetId) return binding;
            }
            return null;
        }

        private static bool BusinessEquivalent(LabBusinessSnapshot left, LabBusinessSnapshot right)
        {
            return left.gold == right.gold && left.potionCount == right.potionCount &&
                left.equippedWeaponId == right.equippedWeaponId && left.equippedOffhandId == right.equippedOffhandId &&
                left.buyAttemptCount == right.buyAttemptCount && left.useAttemptCount == right.useAttemptCount &&
                left.emergencyActionCount == right.emergencyActionCount;
        }

        private static bool HasReason(InteractionAvailability availability, InteractionUnavailableReasons reason)
        {
            return (availability.Reasons & reason) != 0;
        }

        private async ValueTask<string> DescribeRaycastAsync(string targetId, CancellationToken cancellationToken)
        {
            InteractionTargetSnapshot snapshot = await GetSnapshotAsync(targetId, cancellationToken);
            UnityEngine.Vector2 position = UnityApplicationCoordinates.ToUnity(snapshot.Bounds.Center, UnityEngine.Screen.height);
            PointerEventData data = new PointerEventData(EventSystem.current);
            data.position = position;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, results);
            string top = results.Count == 0 ? "none" : results[0].gameObject.name;
            return "Point=" + position + " Raycast=" + top + " EventSystems=" + UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length;
        }

        private static InteractionTargetId Id(string value)
        {
            return new InteractionTargetId(value);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
