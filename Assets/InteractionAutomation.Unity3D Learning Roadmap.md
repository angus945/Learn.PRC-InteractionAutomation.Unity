# InteractionAutomation.Unity3D Learning Roadmap

## 1. Current Goal

Project:

`https://github.com/angus945/Learn.PRC-InteractionAutomation.Unity`

Current learning line:

> Build a Unity Play Mode interaction-automation path that discovers explicit interaction targets and operates them through Unity's normal input / EventSystem route rather than directly invoking product behavior.

Current route:

```text
Unity Scene
    ↓
Interaction Target Discovery
    ↓
InteractionTargetSnapshot
    ↓
Target.Bounds.Center
    ↓
IPhysicalInputDriver
    ↓
Unity Input System Virtual Mouse
    ↓
InputSystemUIInputModule
    ↓
EventSystem / GraphicRaycaster
    ↓
Product UI behavior
```

Automation decides where and how to submit physical-style input. It does not call:

```text
Button.onClick.Invoke()
ExecuteEvents.Execute()
Gameplay callbacks
```

---

## 2. Coordinate Contract

The neutral coordinate model has been collapsed to two spaces:

```text
InteractionCoordinateSpace
├─ ApplicationSpace
└─ OSSpace
```

The old public spaces are removed:

```text
Viewport
Window
Screen
```

There are no compatibility aliases.

Both spaces use the same canonical orientation:

```text
Origin      = TopLeft
Positive X  = Right
Positive Y  = Down
Unit        = pixel-equivalent interaction unit
```

### ApplicationSpace

Coordinates understood by the automated application's interaction surface.

Current Unity EXP-IA route:

```text
RectTransform
    ↓
Unity native projection
    ↓
ApplicationSpace InteractionRect
    ↓
Bounds.Center
    ↓
ApplicationSpace InteractionPoint
    ↓
UnityPhysicalInputDriver
```

### OSSpace

Coordinates understood by an OS-global physical-input surface.

Reserved for future adapters such as:

```text
Windows SendInput
macOS CGEvent
other OS-level input injection
```

EXP-IA-001 does not use OSSpace.

Runner code does not perform ApplicationSpace ↔ OSSpace conversion. Window offsets, Game View offsets, editor chrome, DPI, display origins, letterboxing, and native client rectangles remain adapter details.

---

## 3. Current Repository Revisions

### Workspace.InteractionAutomation

```text
7d85d5994c5926158b4abb54e7bcc642b9629ce5
Collapse interaction coordinates to application and OS spaces
```

Provides host-neutral contracts:

```text
Module.InteractionAutomation.Coordinates
Module.InteractionAutomation.Targets
Module.InteractionAutomation.PhysicalInput
```

Important physical-input semantics:

```text
operation completion
=
input submission completed

!= host input processed
!= EventSystem processed
!= product behavior completed
```

Sequential submissions are required to compose without host-processing barriers.

### Workspace.InteractionAutomation.Unity3D

```text
86198c4ddccdb670724ad33fee2846589c26a642
Align Unity adapters with application-space coordinates
```

Current modules:

```text
Workspace.InteractionAutomation.Unity3D
│
├─ Module.InteractionAutomation.Coordinates.Unity3D
│  └─ UnityApplicationCoordinates
│
├─ Module.InteractionAutomation.Targets.Unity3D
│  ├─ InteractionTargetBinding
│  ├─ UnityInteractionTargetSource
│  └─ UnityUiApplicationGeometry
│
└─ Module.InteractionAutomation.PhysicalInput.Unity3D
   └─ UnityPhysicalInputDriver
```

The Unity local pointer driver accepts:

```text
ApplicationSpace
```

and rejects:

```text
OSSpace
```

---

## 4. EXP-IA-001A — First Unity Interaction Target

Status: **Complete**

Implemented:

```text
GameObject
↓
InteractionTargetBinding
↓
UnityInteractionTargetSource
↓
IInteractionTargetSource
↓
InteractionTargetSnapshot
```

First explicit target:

```text
button.confirm
Role = Button
Capabilities = PointerClick
```

Important boundary:

```text
Unity Button exists
!=
Automation target exists

InteractionTargetBinding exists
=
Automation target is explicitly exposed
```

---

## 5. EXP-IA-001B — UI Target Geometry

Status: **Complete**

Implemented:

```text
RectTransform
↓
World Corners
↓
Canvas / Camera projection
↓
Unity native screen coordinates
↓
UnityApplicationCoordinates
↓
ApplicationSpace InteractionRect
↓
InteractionTargetSnapshot.Bounds
```

Verified:

- top-left canonical convention;
- +Y points downward;
- Bounds.Center preserves ApplicationSpace;
- RectTransform movement changes target geometry correctly;
- target discovery exposes ApplicationSpace geometry.

Not included yet:

- occlusion;
- UI Toolkit;
- 3D target geometry;
- multi-display semantics;
- complex availability rules.

---

## 6. EXP-IA-001C — Virtual Pointer

Status: **Complete**

Implemented:

```text
ApplicationSpace InteractionPoint
↓
UnityPhysicalInputDriver
↓
Virtual Mouse
↓
Unity Input System
↓
InputSystemUIInputModule
↓
EventSystem
↓
PointerEnter
```

Verified:

- virtual Mouse creation;
- ApplicationSpace → Unity native coordinate conversion;
- pointer move submission;
- Input System state processing;
- EventSystem observes the virtual pointer;
- physical Mouse and virtual Mouse can remain separate with:
  `InputSystemUIInputModule.PointerBehavior = AllPointersAsIs`.

Driver contract tests use `InputTestFixture` so they are isolated from Editor focus and real-device state.

A previously explored `UnityInputSystemLifecycle` synchronization helper was removed. The experiments did not establish a need for a processing-barrier abstraction.

---

## 7. EXP-IA-001D — First Real Automated Click

Status: **Complete**

Implemented:

```text
Capture target
↓
select button.confirm
↓
Bounds.Center
↓
Move
↓
PointerDown Left
↓
PointerUp Left
↓
InputSystemUIInputModule
↓
EventSystem
↓
normal Button callback
↓
ClickCount++
```

The automation does not access:

```text
Button
UnityEvent
ButtonClickCounter.HandleClick()
```

except that the project-side counter observes the normal product callback for experimental verification.

### Important finding: submitted state

The first button implementation created later events from host-observed state. This made pending operations non-composable:

```text
Move queued
Down created from stale host state
Up created from stale host state
```

The Unity driver now maintains its own submitted `MouseState`:

```text
Move
position = target
button = up

Down
position = target
button = down

Up
position = target
button = up
```

This preserves ordered logical state even when all events are queued before Unity processes them.

Contract test:

```text
Move
Down
Up
↓
single InputSystem.Update
↓
position preserved
button released
```

passes.

### Processing-separation experiment

After the submitted-state fix:

```text
A — no separation                  PASS
B — explicit frame/state settling PASS
C — state-processing separation   PASS
```

Therefore the current Unity UI route does **not** require a formal processing synchronizer between ordinary Move / Down / Up submissions.

Do not introduce:

```text
IPhysicalInputProcessingSynchronizer
IInteractionStepSynchronizer
```

without new evidence.

---

## 8. Current Physical Input Contract

`IPhysicalInputDriver` remains host-neutral:

```csharp
await input.MovePointerAsync(point);
await input.PointerDownAsync(PointerButton.Left);
await input.PointerUpAsync(PointerButton.Left);
```

The Unity implementation must:

1. fail fast when its virtual device is removed or disabled;
2. accept only ApplicationSpace pointer positions;
3. reject OSSpace rather than guessing conversion;
4. preserve logical effects across sequential submissions;
5. stop its responsibility at host input submission.

---

## 9. Current Project Experiment Scene

`Assets/_AutomationLab/AutoLab.unity` currently contains:

```text
ConfirmButton
├─ Button
├─ InteractionTargetBinding
├─ PointerHoverProbe
└─ ButtonClickCounter

EventSystem
├─ EventSystem
└─ InputSystemUIInputModule
   └─ Pointer Behavior = All Pointers As Is

Probe
└─ VirtualMouseClickSeparationProbe
```

Earlier probes remain available as experiment history but are disabled.

---

## 10. Next Slice — EXP-IA-001E

Next goal:

> Prove the automation path is not specialized for one Button.

Add:

```text
MusicToggle
├─ Toggle
└─ InteractionTargetBinding
   ├─ TargetId = toggle.music
   ├─ Role = Toggle
   └─ Capabilities = PointerClick
```

Automation still sees only:

```text
Target
Bounds
Capabilities
```

Expected behavior:

```text
False → True
True  → False
```

through the same:

```text
Move
Down
Up
```

physical-input path.

---

## 11. Later Roadmap

### EXP-IA-002A — 3D World Target

```text
Chest
├─ Collider / Renderer
└─ InteractionTargetBinding
```

Project geometry to:

```text
ApplicationSpace InteractionRect
```

Do not expose Unity world/camera internals to the neutral coordinate contract.

### EXP-IA-002B — Same Pointer, 3D Target

Product side performs its normal picking:

```text
Mouse position
↓
Camera.ScreenPointToRay
↓
Physics.Raycast
↓
Chest
```

Automation still submits only physical input.

### Target Availability

Later distinguish:

```text
Existence
Geometry validity
Visual visibility
Input reachability
Occlusion
Gameplay eligibility
```

Do not block the current click path on these semantics.

### Interaction Runner

After the current experiments are stable, introduce the smallest reusable runner:

```csharp
targets = await targetSource.GetTargetsAsync();

target = SelectTarget(targets);

point = target.Bounds.Center;

await input.MovePointerAsync(point);
await input.PointerDownAsync(PointerButton.Left);
await input.PointerUpAsync(PointerButton.Left);
```

Runner does not know Unity, EventSystem, Button, native screen orientation, or OS coordinate conversion.

### Monkey / Verification / Remote

Only after deterministic explicit-target interaction is stable:

```text
Runner
↓
Monkey
↓
Observation / Verification
↓
Remote transport
```

Communication remains transport-only and does not own interaction semantics.

---

## 12. Architecture Boundary Summary

```text
Workspace.InteractionAutomation
    host-neutral contracts
    ├─ ApplicationSpace / OSSpace
    ├─ Targets
    └─ PhysicalInput

Workspace.InteractionAutomation.Unity3D
    Unity adapters
    ├─ native Unity geometry ↔ ApplicationSpace
    ├─ target discovery
    └─ virtual physical input

Learn.PRC-InteractionAutomation.Unity
    experiment composition
    ├─ scenes
    ├─ probes
    ├─ product test UI
    └─ exact submodule revisions
```

Core rule:

> Coordinate Space identifies who understands a coordinate. It does not expose how many host-specific Window / Viewport / client-area transforms exist internally.
