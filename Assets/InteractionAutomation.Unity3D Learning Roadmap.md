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

Completion means input submission, not host/UI/gameplay completion. Sequential submissions compose without caller-inserted processing barriers.

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

Reusable host-neutral workflow:

```text
Capture
↓
Resolve by InteractionTargetId
↓
Validate capability
↓
derive interaction point
↓
submit physical input
```

`InteractionRunner` does not know Unity, EventSystem, Button, Toggle, Renderer, Collider, or specific target kinds.

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
InteractionRunner
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

Status: **NEXT**

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

Do not create a separate learning milestone for every input enum value.

## Phase 11 — Observation / Verification

Status: Pending

Separate execution from correctness:

```text
Runner → Action
Observer / Verification → Result
```

## Phase 12 — Monkey / Exploration

Status: Pending

Monkey is a selection policy over existing target, capability, availability, and runner primitives.

## Phase 13 — Remote / OS Automation

Status: Pending

Only after the local Runner model is stable:

```text
External process
↓
transport
↓
Runner API
↓
Target / Physical Input
```

This is where `OSSpace`, serialization, and stable cross-process kind identity may become real requirements.

---

## Current Revisions

```text
Workspace.InteractionAutomation
fff18e83df0922daa3d1d5b5e6c417588e490b99

Workspace.InteractionAutomation.Unity3D
1396d829c6a1f741a3cca7cf3b22f1468aade382
```

## Current Learning Position

```text
Phases 1–9  COMPLETE
Phase 10      START HERE
```
