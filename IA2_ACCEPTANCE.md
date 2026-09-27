# IA-2 acceptance — capability-aware availability

Status: implemented, new execution acceptance pending. IA-1 was reported fully passing by the user on 2026-09-27. Occlusion is explicitly deferred.

## Update the coordinated source set

Close Unity while updating. Run from this Project root:

```shell
git pull --ff-only
git submodule sync --recursive
git submodule update --init --recursive
python scripts/test_interaction_architecture.py
python scripts/validate-interaction-architecture.py
python scripts/test_import_policy.py
python scripts/validate-unity-imports.py
```

Do not use `git submodule update --remote`: Project gitlinks select the compatible IA-2 revision set. This is a breaking evaluator signature change; a half-updated checkout is not supported.

## Focused test gate

Open Unity, regenerate IDE project files as necessary, and require zero compile errors. Run all existing tests plus these focused fixtures in Test Runner:

- `InteractionAvailabilityTests`: structural reasons, atomic contexts, drag roles, policy composition, live-state queries and failures.
- `PointerAvailabilityTests`: all six actions forward the correct capability/point; rejection occurs before any input; drag destination denial is checked before moving or pressing the source.
- `PointerMonkeyAvailabilityTests`: Hover remains eligible when Click is denied; unlock changes candidates without refresh; a selected action is revalidated against changed permission/structural state; rejected execution has zero input and zero coverage credit.
- `UnityInteractionAutomationBuilderTests`: shared composition, freezing, additive policies and preserved structural checks.

The existing `SeededPointerMonkeyTests` golden sequence and pointer execution/cancellation suites must also pass. Neutral tests can additionally run through the existing .NET test projects, with this Project supplying dependency-root properties.

## Live AutoLab acceptance (opt-in, separate from P12)

1. Open `Assets/_AutomationLab/AutoLab.unity` outside Play Mode.
2. On the existing `Probe` GameObject, disable `AutoLabSeededPointerMonkeyProbe`. Keep `InteractionRunnerProbe` and `VirtualMouseEventSystemProbe` disabled. Do not disable `UnityFrameInputProcessingBoundary`.
3. Add `AutoLabAvailabilityProbe` to that same GameObject. It finds the local frame boundary and existing `button.confirm` observers/counter. Its Camera field may stay empty for this existing MainCamera fixture, or be assigned explicitly.
4. Enter Play Mode without operating the fixture with another pointer. The new probe rejects an enabled competing AutoLab input probe rather than silently running both.

Expected messages:

```text
IA2 HOVER AVAILABLE / CLICK DENIED PASS
IA2 LIVE PROJECT PERMISSION / PRODUCT CLICK PASS
IA2 STALE SELECTION REJECTED BEFORE INPUT PASS
IA2 CAPABILITY-AWARE AVAILABILITY PASS
```

The probe starts with Project Click permission denied while Hover stays allowed. It confirms denied Click makes zero physical submissions, performs a real virtual-mouse Hover, unlocks permission on the same policy/state instance, and performs one real Click through EventSystem to the product counter. A separately scoped single-action Monkey test selects Click, revokes permission, and requires rejection without input or coverage.

The live fixture never calls product callbacks directly, forces InputSystem.Update or changes Button.interactable. It demonstrates Project policy updates, not automatic Unity UI-state or occlusion detection. A cancelled run is not PASS. Missing observers, unexpected errors and competing input probes are failures.

## Baseline regression

Exit Play Mode. Disable/remove `AutoLabAvailabilityProbe`, restore the original Monkey probe, and rerun seed 12345 / 25 iterations. Require the original seven coverage pairs and passing evidence/verification. Run P11 separately. The committed scene, P12 probe, existing target declarations and capabilities are unchanged by IA-2.

## Adopter usage

```csharp
var state = new AutoLabInteractionPermissionState();
var policy = new AutoLabInteractionAvailabilityPolicy(state);
var runtime = AutomationLabInteractionComposition.CreateRuntime(
    camera, physicalInput, frameBoundary, policy);

// Real products read their existing game-state/query model from their own evaluator.
state.SetConfirmClickAllowed(false);
// The next availability query / Runner admission reads the changed state.
// No RefreshAvailability call and no extra per-target component are required.
```

At the reusable Framework boundary, use `.AddAvailabilityPolicy(projectPolicy)` to retain base structural/host checks. `.UseAvailability(...)` explicitly replaces the base evaluator. Project permission controls whether input may be attempted, not whether its product outcome will succeed.

IA-2 revalidates before starting a gesture. It does not continuously monitor permission inside an admitted Hold/Drag, auto-retry stale selections, classify expected denials as QA success, or support new camera/scene behavior. Occlusion and automatic Selectable/CanvasGroup interpretation remain out of scope.

## Authoring evidence

Remote source and Git diffs were reviewed. Unity Editor/.NET SDK were unavailable, so no IA-2 C# compilation, focused test execution, initialized-checkout architecture scan or live probe run was performed in the authoring environment. Record actual results before calling IA-2 accepted.
