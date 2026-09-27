# InteractionAutomation.Unity3D Learning Roadmap

## Goal

Learn the architecture of physical interaction automation in Unity without turning implementation details into separate learning milestones.

Current model:

```text
Interaction Target
        ↓
InteractionTargetSnapshot
        ├─ Id
        ├─ Kind
        ├─ Bounds
        ├─ Capabilities
        ├─ IsVisible
        └─ IsEnabled
        ↓
Availability Evaluation
        ↓
InteractionRunner
        ↓ delegates
Concrete Interaction Use Case
        ↓
IPhysicalInputDriver
        ↓
Unity normal input / product route
```

Automation decides where and how to submit input. It does not directly invoke product behavior.

---

## Phase 1 — Interaction Target Model

Status: **Complete**

Explicit target exposure through `InteractionTargetBinding`; discovery produces host-neutral snapshots.

## Phase 2 — Canonical Coordinate Boundary

Status: **Complete**

```text
ApplicationSpace
OSSpace
```

Unity local automation uses `ApplicationSpace`. Host-specific coordinate conversion stays inside adapters.

## Phase 3 — Physical Input Adapter

Status: **Complete**

```text
IPhysicalInputDriver
↓
UnityPhysicalInputDriver
↓
Unity Input System virtual mouse
```

Completion means input submission, not host/UI/gameplay completion. Ordinary sequential submissions compose without caller-inserted processing barriers. Sustained gestures may require an explicit host-processing boundary between gesture phases.

## Phase 4 — Application Input Route

Status: **Complete**

```text
Virtual Mouse
↓
Unity Input System
↓
InputSystemUIInputModule
↓
EventSystem
↓
normal product behavior
```

## Phase 5 — Capability Generalization

Status: **Complete**

Button and Toggle both use the same `PointerClick` physical interaction route. Product type does not choose the input implementation.

## Phase 6 — Geometry Strategy

Status: **Complete**

```text
IUnityInteractionTargetGeometryProvider
        ├─ RectTransform → ApplicationSpace Rect
        └─ Renderer      → ApplicationSpace Rect
```

Target bounds represent interaction geometry rather than a specific Unity geometry technology.

## Phase 7 — Same Interaction, Different Product Domain

Status: **Complete**

The closed `InteractionTargetRole` enum was removed.

Target taxonomy is open and process-local:

```text
IInteractionTargetKind
        ↑
        ├─ ButtonTargetKind
        ├─ ToggleTargetKind
        └─ ChestTargetKind
```

`Kind` describes what the target is. `Capabilities` describe how automation may interact with it.

The registry currently uses CLR `Type` identity only. Stable serialized/cross-process kind IDs are deferred.

## Phase 8 — Interaction Runner

Status: **Complete**

Reusable host-neutral workflow was introduced.

The Phase 10 preparation refactor clarified the boundary:

```text
InteractionRunner
= thin facade

PointerClickInteraction
= concrete PointerClick use case

InteractionTargetResolver
= shared uniqueness resolution over one captured target set
```

Action-specific sequencing must not accumulate inside `InteractionRunner`.

## Phase 9 — Target Availability

Status: **Complete**

Discovery facts and operability are separate concerns:

```text
InteractionTargetSnapshot
        ↓
IInteractionAvailabilityEvaluator
        ↓
InteractionAvailability
        ↓
Concrete Interaction Use Case
```

Current reasons:

```text
NotVisible
Disabled
InvalidGeometry
```

Availability uses flags so multiple causes can be reported at once.

Current scope deliberately does not yet model:

```text
occlusion
modal blocking
offscreen clipping
gameplay eligibility
Button.interactable / product-specific rules
```

Those are future evaluator concerns if evidence requires them.

Unavailable targets are rejected before any physical input is submitted.

---

# Phase 10 — New Action Shapes

Status: **DRAG ACCEPTED — continue Keyboard / TextInput**

Add only actions that introduce a genuinely different interaction shape.

Recommended order:

```text
Drag
Keyboard / TextInput
Scroll
```

First target: **Drag**, because it introduces a sustained interaction sequence:

```text
Move to source
↓
PointerDown
↓
one or more Move operations while held
↓
PointerUp
```

Drag is implemented as `PointerDragInteraction` behind the thin `InteractionRunner` facade.

Current core invariants:

```text
one captured target set
↓
resolve source + destination
↓
source requires PointerDrag
↓
source + destination availability
↓
Move(source center)
↓
Left Down
↓
host input processing boundary
↓
Move(destination center) while held
↓
host input processing boundary
↓
Left Up
```

After PointerDown succeeds, cancellation or failure during the held portion still attempts a non-cancellable PointerUp cleanup.

The first Unity runtime attempt queued Down → Move-held → Up before EventSystem had an intermediate processing opportunity. Evidence: `BeginDrag=0` and the destination Toggle received a normal click instead. Phase 10 therefore introduced `IHostInputProcessingBoundary`; Unity supplies `UnityFrameInputProcessingBoundary` from `Module.InteractionAutomation.Runner.Unity3D`.

This does not change `IPhysicalInputDriver` completion semantics. The boundary only provides a host processing opportunity between sustained gesture phases.

The rerun after introducing `IHostInputProcessingBoundary` passed in Unity. Drag runtime acceptance is therefore complete.

The AutoLab runtime probe uses `button.confirm` as the source and `toggle.music` as the destination. The Button keeps its ButtonTargetKind and gains PointerDrag capability; Kind does not dispatch the action.

Do not create a generic action bus, handler registry, Drop capability, or drag/drop relationship model before another real case requires one.

Do not create a separate learning milestone for every input enum value.

## Phase 11 — Observation / Verification

Status: Pending

Separate execution from correctness:

```text
Runner / Interaction Use Case → Action
Observer / Verification       → Result
```

## Phase 12 — Monkey / Exploration

Status: Pending

Monkey is a selection policy over existing target, capability, availability, and interaction-use-case primitives.

## Phase 13 — Remote / OS Automation

Status: Pending

Only after the local interaction model is stable:

```text
External process
↓
transport
↓
application interaction API
↓
Target / Physical Input
```

This is where `OSSpace`, serialization, and stable cross-process kind identity may become real requirements.

---

## Current Revisions

```text
Workspace.InteractionAutomation
010789b2d0bcd0ba5d652916db6271de64c7154f

Workspace.InteractionAutomation.Unity3D
b9d0f99ef079180c376dccd9d89be003bf8c8267
```

## Current Learning Position

```text
Phases 1–9  COMPLETE
P10 boundary refactor COMPLETE
Phase 10 Drag ACCEPTED
Phase 10 Keyboard / TextInput START HERE
```
