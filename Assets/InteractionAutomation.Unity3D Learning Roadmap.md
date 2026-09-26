# InteractionAutomation.Unity3D Learning Roadmap

## Goal

Learn the architecture of physical interaction automation in Unity without turning implementation details into separate learning milestones.

Current model:

```text
Application-defined Target Kind
        ↓
InteractionTargetSnapshot
        ├─ Id
        ├─ Kind
        ├─ Bounds
        └─ Capabilities
        ↓
Capability-driven interaction
        ↓
IPhysicalInputDriver
        ↓
Unity normal input / product route
```

Automation decides **where and how to input**. It does not directly invoke product behavior.

---

## Phase 1 — Interaction Target Model

Status: **Complete**

- Target exposure is explicit through `InteractionTargetBinding`.
- Target discovery produces host-neutral snapshots.
- Unity objects do not leak into the neutral target contract.

## Phase 2 — Canonical Coordinate Boundary

Status: **Complete**

```text
ApplicationSpace
OSSpace
```

Unity local automation uses `ApplicationSpace`. Host-specific screen/window/DPI details stay inside adapters.

## Phase 3 — Physical Input Adapter

Status: **Complete**

```text
IPhysicalInputDriver
↓
UnityPhysicalInputDriver
↓
Unity Input System virtual mouse
```

Operation completion means **submitted**, not host/UI/gameplay processed. Sequential `Move → Down → Up` submissions compose without consumer-inserted processing barriers.

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

Physical and automation pointers can remain separate with `AllPointersAsIs`.

## Phase 5 — Capability Generalization

Status: **Complete**

Button and Toggle both use:

```text
PointerClick
↓
Bounds.Center
↓
Move
Down
Up
```

Widget/product type does not choose the physical action implementation.

## Phase 6 — Geometry Strategy

Status: **Complete**

Geometry is a Unity adapter strategy rather than a `RectTransform` assumption:

```text
IUnityInteractionTargetGeometryProvider
        ├─ RectTransform → ApplicationSpace Rect
        └─ Renderer      → ApplicationSpace Rect
```

`InteractionTargetSnapshot.Bounds` now represents interaction geometry rather than one Unity geometry technology.

Detailed clipping, occlusion, renderer aggregation, and multi-camera policy are deferred until they become actual requirements.

## Phase 7 — Same Interaction, Different Product Domain

Status: **Complete — architecture consolidated**

The closed `InteractionTargetRole` enum has been removed from the neutral core.

Target taxonomy is now open and process-local:

```text
IInteractionTargetKind
        ↑
        ├─ ButtonTargetKind
        ├─ ToggleTargetKind
        └─ ChestTargetKind

InteractionTargetKindRegistry
        ↓
InteractionTargetSnapshot.Kind
```

Unity composition maps host component types to registered application kinds:

```text
Button   → ButtonTargetKind
Toggle   → ToggleTargetKind
Renderer → ChestTargetKind
```

The architecture now makes the distinction explicit:

```text
Kind
= what the target is

Capabilities
= how automation may interact with it
```

Physical action selection remains capability-driven. Do not dispatch physical actions by target kind.

The registry intentionally uses CLR `Type` identity only. Persistence, serialization, replay compatibility, and stable cross-process kind IDs are not current requirements.

A separate micro-milestone for a 3D click is not required before continuing. The reusable Runner is the next place to exercise the same capability across different product domains.

---

# Phase 8 — Interaction Runner

Status: **NEXT**

Goal:

> Stop writing one-off probes for each interaction and introduce the smallest reusable application workflow.

Concept:

```text
Capture targets
↓
Select target
↓
Validate requested capability
↓
Derive interaction point
↓
Submit physical input
```

The first Runner should know only:

```text
IInteractionTargetSource
IPhysicalInputDriver
InteractionTargetSnapshot
PhysicalInteractionCapabilities
```

It must not know:

```text
Unity
Button
Toggle
Renderer
Collider
EventSystem
specific Target Kind
```

The first reusable operation is `PointerClick`.

## Phase 9 — Target Availability

Status: Pending

Separate:

```text
Exists
HasGeometry
Visible
InputReachable
GameplayEligible
```

Discovery and operability are not the same concept.

## Phase 10 — New Action Shapes

Status: Pending

Add only actions that introduce a new architectural problem:

```text
Drag
Keyboard / TextInput
Scroll
```

Do not create a learning milestone for every input enum value.

## Phase 11 — Observation / Verification

Status: Pending

Separate execution from correctness:

```text
Runner → Action
Observer / Verification → Result
```

## Phase 12 — Monkey / Exploration

Status: Pending

Monkey is selection policy over existing target/capability/runner primitives, not another interaction framework.

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
4d7247eb4d0ca049e1ccb5cc1f73c3d1d875caca

Workspace.InteractionAutomation.Unity3D
5edfeab613ffbdd0a7830882bc424cece0143b7b
```

## Current Learning Position

```text
Phases 1–7  COMPLETE
Phase 8       START HERE
```
