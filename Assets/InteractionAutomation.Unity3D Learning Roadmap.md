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

## Acceptance

Drag runtime acceptance: **PASS**

Full pointer execution runtime acceptance: **PENDING**

Expected final probe result:

```text
PHASE 10 POINTER EXECUTION PASS
Click / DoubleClick / Drag / Scroll / Hold / Hover
Normal Unity Input System / EventSystem route observed.
```

## Current revisions

```text
Workspace.InteractionAutomation
7f38feb3db280312327b14c572724019e28f5d8d

Workspace.InteractionAutomation.Unity3D
bfce40cd0ac7b99020eecf6320d6f91493470f3e
```

## Next after acceptance

```text
Phase 11 — Observation / Verification
```

Execution should remain separate from product-specific correctness assertions.
