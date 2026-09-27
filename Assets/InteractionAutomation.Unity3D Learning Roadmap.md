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
904baba3a489b5a28051ff27640a0c5d0fde3898

Workspace.InteractionAutomation.Unity3D
d0291247cd4a32eacab9c18fa5444eca14f94656
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
d0291247cd4a32eacab9c18fa5444eca14f94656
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

# Phase 12 — Deterministic Monkey QA Runner

Status: **P12.6 IMPLEMENTED — final runtime acceptance pending**

Core revision:

```text
Workspace.InteractionAutomation
904baba3a489b5a28051ff27640a0c5d0fde3898
```

Unity integration revision:

```text
Workspace.InteractionAutomation.Unity3D
d0291247cd4a32eacab9c18fa5444eca14f94656
```

## P12.1 — deterministic selection / execution

```text
target snapshots
→ availability
→ stable candidate ordering
→ frozen SplitMix64 seeded selection
→ typed Pointer*Request
→ InteractionRunner
```

No direct physical-input execution exists in Monkey.

## P12.2 — presentation

AutoLab displays the actual virtual Input System mouse:

```text
cursor
trail
button DOWN / UP
seed
iteration
action
target
status
recent steps
```

Presentation pacing remains outside Core Monkey.

## P12.3 — per-step verification

Every selected action now follows:

```text
reset target-local observer
→ execute typed request
→ capture Unity observation
→ UnityPointerVerificationProfiles
→ EvaluationReport
```

Reusable host contracts:

```text
Click       → exactly one EventSystem click event
DoubleClick → >= 2 click events and clickCount >= 2
Drag        → BeginDrag == 1, Drag >= 1, EndDrag == 1
Scroll      → one scroll event
Hold        → Down == 1, Up == 1, pressed >= 1 frame, released
Hover       → virtual pointer is currently hovered
```

Any non-Passed verdict stops the run.

AutoLab adds one project-specific Drag expectation:

```text
button.confirm → toggle.music
→ DropCount == 1
```

## P12.4 — evidence / trace

`Module.InteractionAutomation.Monkey.Unity3D` records immutable step results through:

```text
TraceBuffer<UnityPointerMonkeyStepResult>
+
EvidenceBuilder
```

Each step retains:

```text
Seed
Sequence
ActionId
TargetId
DestinationTargetId
Oracle EvaluationReport
Coverage snapshot
ReproductionKey
```

Failure reproduction format:

```text
seed=<seed>;iteration=<n>;action=<action>;target=<id>;destination=<id>
```

Evidence is bounded and in-memory. `EvidenceBundle` stores references and metadata rather than duplicating Oracle payloads.

## P12.5 — deterministic Drag

Drag selection is enabled only through:

```text
IPointerMonkeyDragDestinationPolicy
```

Core does not assume all targets are valid drop destinations.

AutoLab policy:

```text
source      = button.confirm
destination = toggle.music
```

The resulting execution is still a typed:

```text
PointerDragRequest(source, destination)
```

The 3D `chest.001` target remains discoverable for Renderer geometry verification but no longer advertises `PointerClick`, because the scene does not currently provide a PhysicsRaycaster/EventSystem click route for it.

## P12.6 — coverage-first policy

Coverage identity:

```text
single-target:
ActionId + TargetId

drag:
ActionId + SourceId + DestinationId
```

Selection rule:

```text
eligible pairs
→ choose only uncovered pairs while any remain
→ after 100% coverage, seeded random may repeat
```

Final acceptance requires:

```text
Run Verdict = Passed
Coverage = 100%
No InfrastructureError
No failed Oracle
```

AutoLab currently has 7 eligible interaction pairs:

```text
button.confirm
├─ click
├─ double-click
├─ drag → toggle.music
├─ hold
└─ hover

toggle.music
├─ click
└─ scroll
```

Default run remains:

```text
Seed = 12345
Iterations = 25
```

Expected final marker:

```text
P12 DETERMINISTIC MONKEY COMPLETE
Seed=12345
Coverage=7/7 (100%)
Selection / Drag / Verification / Evidence / Coverage PASS
```

When this marker is observed in AutoLab, P12 is fully accepted.
