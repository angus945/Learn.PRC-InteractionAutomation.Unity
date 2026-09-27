# InteractionAutomation.Unity3D Learning Roadmap

## Goal

Learn host-neutral physical interaction automation through normal Unity input routes without turning every implementation detail into a separate milestone.

## Phases 1–9

Status: **COMPLETE**

Established boundaries:

```text
Target discovery
→ capability
→ availability
→ concrete interaction use case
→ physical input
→ host input processing
→ product behavior
```

Key contracts remain:

- Kind describes what a target is.
- Capability describes how automation may interact with it.
- Availability describes whether it is currently operable.
- Physical-input completion means submission, not product completion.
- Host processing boundaries are explicit only where interaction shape requires them.

# Phase 10 — Pointer Execution

Keyboard and TextInput are intentionally deferred. Phase 10 now scopes only mouse/pointer execution.

Implemented pointer surface:

```text
PointerClick
PointerDoubleClick
PointerDrag
Scroll
PointerHold
PointerHover
```

## Click

```text
Move(target center)
→ Down
→ Up
```

## DoubleClick

```text
Move(target center)
→ click
→ host processing boundary
→ click
```

The boundary allows the host to observe the first click before the second. No generic double-click timeout policy is introduced yet.

## Drag

```text
Move(source center)
→ Down
→ host processing boundary
→ Move(destination center) while held
→ host processing boundary
→ Up
```

The first no-boundary Unity attempt produced `BeginDrag=0`. Adding `IHostInputProcessingBoundary` fixed the lifecycle, and the rerun passed.

## Scroll

```text
Move(target center)
→ Scroll(delta)
```

Unity `ScrollAsync` submits a transient wheel delta and clears that delta from the driver's durable submitted-state baseline so later mouse events do not replay it.

## Hold

```text
Move(target center)
→ Down
→ host processing boundary
→ Delay(duration)
→ host processing boundary
→ Up
```

Once Down succeeds, cancellation/failure still attempts a non-cancellable Up cleanup.

## Hover

```text
Move(target center)
```

Hover intentionally reuses pointer movement rather than adding another physical-input primitive.

## Current AutoLab capabilities

```text
button.confirm
Kind = ButtonTargetKind
Capabilities =
    PointerClick
  | PointerDoubleClick
  | PointerDrag
  | PointerHold
  | PointerHover

toggle.music
Kind = ToggleTargetKind
Capabilities =
    PointerClick
  | Scroll
```

This deliberately proves that target Kind does not dispatch interaction behavior.

## Typed request registry

Pointer execution now uses typed requests and an immutable registry:

```text
Pointer*Request
↓
InteractionRunner.ExecuteAsync<TRequest>
↓
InteractionRegistry
↓
IInteractionHandler<TRequest>
↓
Concrete Pointer*Interaction
```

Registry keys are request types, never `PhysicalInteractionCapabilities`.

`AutomationLabInteractionComposition` is the composition root. It creates all six pointer handlers, registers them through `PointerInteractionRegistryProfile.CreateBuilder()`, validates completeness at `Build()`, and passes the built registry into `InteractionRunner`.

Runtime code does not resolve handlers directly.

## Acceptance

Drag runtime acceptance: **PASS**

Full pointer execution runtime acceptance: **PASS**

Expected final probe result:

```text
PHASE 10 POINTER EXECUTION PASS
Click / DoubleClick / Drag / Scroll / Hold / Hover
Normal Unity Input System / EventSystem route observed.
```

## Current revisions

```text
Workspace.InteractionAutomation
03653c7be0adde8c87771f075d8e8ee9bb8b8344

Workspace.InteractionAutomation.Unity3D
599f676111f59388d2375adc3644a81f524ee138
```

# Phase 11 — Observation / Verification

Status: **COMPLETE**

Installed verification workspace:

```text
Assets/CraftyRacoon/Workspace.Verification
c85081e463233f92b014b19faec92b8d66b1e3a8
```

Unity integration revision:

```text
Workspace.InteractionAutomation.Unity3D
599f676111f59388d2375adc3644a81f524ee138
```

## Boundaries

```text
InteractionRunner
= execute typed interaction requests

Module.InteractionAutomation.Observation.Unity3D
= capture what Unity EventSystem observed

Module.InteractionAutomation.Verification.Unity3D
= judge reusable Unity host contracts

AutoLab Verification
= judge AutoLab-specific product/scenario expectations
```

Reusable Unity host verification covers:

```text
Drag lifecycle
Scroll observed
Hover observed
Double-click EventSystem semantics
Hold press/release lifecycle
```

AutoLab retains only:

```text
button.confirm product callback
toggle.music product state change
button.confirm → toggle.music drop expectation
```

## Scenario orchestration

`InteractionRunnerProbe` is now only the composition/sequencing entry point.

Concrete AutoLab scenarios:

```text
AutoLabButtonClickScenario
AutoLabToggleClickScenario
AutoLabDragScenario
AutoLabScrollScenario
AutoLabHoverScenario
AutoLabDoubleClickScenario
AutoLabHoldScenario
```

Each scenario owns one vertical slice:

```text
Arrange
→ Execute typed request
→ allow Unity processing
→ Capture immutable observation
→ Evaluate Oracle
→ expose Passed
```

No generic `IScenario`, scenario registry, or scenario pipeline has been introduced.

## Legacy cleanup

Removed experiments already superseded by current formal execution scenarios:

```text
TargetDiscoveryProbe
VirtualMouseClickSeparationProbe
MusicToggleExperimentProbe
```

Previously removed:

```text
VirtualMouseProbe
```

Still retained because they cover distinct AutoLab contracts:

```text
VirtualMouseEventSystemProbe
= virtual/physical pointer separation

WorldTargetDiscoveryProbe
= AutoLab chest.001 renderer geometry
```

Expected final runtime marker:

```text
PHASE 11 OBSERVATION / VERIFICATION PASS
Execution / observation / host verification / AutoLab product verification are separated.
```

Runtime acceptance: **PASS**

# Phase 12 — Deterministic Monkey

Status: **P12.1 IMPLEMENTED — runtime acceptance pending**

P12.1 introduces the host-neutral:

```text
Module.InteractionAutomation.Monkey
```

Selection:

```text
current target snapshots
→ SnapshotInteractionAvailabilityEvaluator
→ supported capability/action pairs
→ stable sort by TargetId + ActionId
→ frozen SplitMix64 seeded selection
→ PointerMonkeyStep
```

Supported actions:

```text
PointerClick
PointerDoubleClick
Scroll
PointerHold
PointerHover
```

`PointerDrag` is intentionally excluded until source/destination selection is introduced.

Execution remains on the formal path:

```text
PointerMonkeyStep
→ typed Pointer*Request
→ InteractionRunner
→ concrete interaction handler
→ UnityPhysicalInputDriver
→ host processing boundary
```

The monkey never calls physical input directly and does not dispatch by capability enum.

AutoLab default probe:

```text
AutoLabSeededPointerMonkeyProbe
Seed = 12345
Iterations = 25
```

The previous P11 `InteractionRunnerProbe` remains in the scene but is disabled while the Monkey probe owns the virtual input run.

Expected runtime marker:

```text
P12.1 SEEDED POINTER MONKEY PASS
Seed=12345
Iterations=25
All selected actions executed through InteractionRunner.
```
