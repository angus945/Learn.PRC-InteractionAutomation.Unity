# InteractionAutomation production integration — staged execution

Date: 2026-09-27

This Project owns the integration sequence, source locations, exact revision pins and runtime acceptance. The reusable repositories do not check out, mirror or rewrite one another. Each stage is a separately reviewable change. Do not claim a later stage is supported because its extension point exists.

## Status

| Stage | Scope | Status |
| --- | --- | --- |
| IA-1 | Canonical Framework extraction, Timing separation, shared Unity composition and AutoLab consumer migration | Implemented; Unity/.NET runtime acceptance pending |
| IA-2 | Capability-aware availability, project permission and execution revalidation | Planned |
| IA-3 | uGUI hit-test/occlusion and current UI-state availability | Planned |
| IA-4 | Dynamic view resolution, multiple cameras and camera-stack acceptance | Planned |
| IA-5 | Additive scenes, transitions, readiness, cancellation and target incarnation lifecycle | Planned |
| IA-6 | Adopter defaults, target authoring ergonomics and acceptance matrix | Planned |

The earlier P10/P11/P12 AutoLab baseline was reported runtime-passing by the user before extraction. That report is historical evidence, not proof that the migrated assemblies compile or run. The older learning roadmap remains phase history; this file tracks production-integration work.

## IA-1 — Framework/source identity and adoption entrypoint

### Ownership

- `Workspace.InteractionAutomation`: Coordinates, Targets, Availability, PhysicalInput and Timing Modules only.
- `Workspace.InteractionAutomation.Unity3D`: Unity capability adapters, including Timing and live target-binding capture. No Runner/Monkey workflow implementation remains under a Module identity.
- `Framework.InteractionAutomation.Runner`: typed request dispatch and complete pointer execution workflows; reusable registry/build API.
- `Framework.InteractionAutomation.Monkey`: coverage-first seeded selection/execution; one-way dependency on Runner.
- `Framework.InteractionAutomation.Runner.Unity3D`: explicit builder and immutable shared runtime. Dependencies are borrowed; the Project owns device lifetime.
- `Framework.InteractionAutomation.Monkey.Unity3D`: complete select/prepare/execute/verify/evidence/stop loop. The Project supplies presentation/pacing and product-verification hooks, not a duplicate loop.

The Project mounts all six repositories directly under `Assets/CraftyRacoon`. No old namespace aliases, bridge assemblies or source copies are used. `Directory.Build.props` supplies neutral Framework dependency-root properties; source owners retain their canonical asmdefs.

### Revision set

| Repository | Pinned revision |
| --- | --- |
| Workspace.InteractionAutomation | `666db89d7a9ebfe66c62d9a2a61589b629b726d8` |
| Workspace.InteractionAutomation.Unity3D | `1da731004fdff13fb1034e8d1410ff32aef0a6fb` |
| Framework.InteractionAutomation.Runner | `23e51fbc803050b66461984b7dfcd0ce8c081ea1` |
| Framework.InteractionAutomation.Monkey | `bc79acb0c54db41b132a69280f9af3290918c468` |
| Framework.InteractionAutomation.Runner.Unity3D | `81c459c342d9acbfd4c280103d7fa5f57f187d76` |
| Framework.InteractionAutomation.Monkey.Unity3D | `26ed9a0aed74f4cc8f7cdaa990c37e8b4f8b0031` |
| Workspace.Verification (unchanged) | `c85081e463233f92b014b19faec92b8d66b1e3a8` |

Project gitlinks are authoritative. Do not update with `--remote` when reproducing this stage.

### AutoLab changes

`AutomationLabInteractionComposition.CreateRuntime` uses the Unity Runner builder. `UnityPointerMonkeyFactory.Create` consumes that same runtime, so selection and execution share the TargetSource, Availability and input-processing boundary.

`AutoLabSeededPointerMonkeyProbe` starts one Framework QA run. `AutoLabMonkeyRunAdapter` owns only the existing HUD/pacing and concrete `button.confirm -> toggle.music` drop expectation. All scene field names, script GUID references and target IDs are retained. Timing's serialized assembly/type identity is changed to `Module.InteractionAutomation.Timing.Unity3D`.

The Monkey lifecycle adapter cancels on disable/destroy and disposes its device after awaited gesture cleanup. It never labels cancellation PASS. This is not acceptance of arbitrary scene-destructing clicks. The older P11 coroutine probe remains a stable-scene regression fixture, not a transition-lifetime implementation.

### Verification performed during authoring

- The architecture checker's 12 positive/negative Python fixture tests passed.
- GitHub compared the candidate Project commit against its parent; the existing HUD changed only its two namespace imports, and existing scenario sources changed only their Runner imports.
- The scene diff was reviewed for retained script GUIDs and object references. Besides the Timing type identity, one quaternion negative-zero textual representation normalized to zero; the rotation values are equivalent.
- No full initialized-checkout architecture scan, C# compilation, .NET test run, Unity EditMode/PlayMode test run or Player build was executed in the authoring environment. Unity Editor and the .NET SDK were unavailable. Remote source inspection and checker-fixture tests do not replace those checks.

### Acceptance gate

Close Unity while updating this coordinated source revision set, then run from the Project root:

```shell
git pull --ff-only
git submodule sync --recursive
git submodule update --init --recursive
git submodule status --recursive
python scripts/test_interaction_architecture.py
python scripts/validate-interaction-architecture.py
python scripts/test_import_policy.py
python scripts/validate-unity-imports.py
```

The new architecture checker checks active source/serialized identities, source-assembly presence, duplicate assemblies/GUIDs, Module-to-Framework reverse edges and assembly cycles. It is a static guard, not a compiler. Historical Markdown is not treated as active source.

Open Unity and regenerate the IDE project files. Require zero compile errors and zero missing scripts in `Assets/_AutomationLab/AutoLab.unity`. Run the new Framework/TI​ming tests available in Test Runner; then run AutoLab with seed 12345 and 25 iterations. Require six pointer actions, the configured seven coverage pairs, passing verification, and retained evidence. Run the existing P11 probe separately, not concurrently with Monkey. During a separate Monkey run, disable the probe during Hold/Drag and verify cleanup without PASS-on-cancellation. Record actual results before starting IA-2.

## IA-2 — Availability contract

Place host-neutral evaluation values/contracts in `Module.InteractionAutomation.Availability`; keep request execution ownership in Runner. Evaluate the requested capability, and include only the interaction-point/route context required to keep selection and actual execution consistent. Do not make a generic object-payload bus or make Modules depend on Framework request types.

Selection filters candidates; execution revalidates before input. Shared policy semantics do not imply an immutable world between those queries. Project implementations read game-state permissions without copying product business rules into reusable code. Physical interaction permission is distinct from whether a purchase/action succeeds.

Acceptance: the same target can permit Hover and reject Click; changing project/UI state affects the next query without a manually synchronized availability cache; execution rejects a stale selected action safely.

## IA-3 — Unity reachability

Add `Module.InteractionAutomation.Availability.Unity3D` to the existing Unity workspace. Use the configured production input route to evaluate the actual intended point and event recipient, not a visual-overlap guess. Account for child graphics/event handlers, Selectable state, CanvasGroup behavior, masks, transparent blockers and modal overlays. Observers remain facts; Oracles remain judgment; neither becomes the availability source of truth.

Acceptance: a modal blocks the target behind it and closing it restores eligibility without explicit refresh. A transparent raycast blocker is effective. Disabled Click need not disable Hover. Negative interaction tests remain possible through an explicitly chosen test policy; eligible-action Monkey is not proof that blocked product actions are correctly rejected.

## IA-4 — Cameras and views

Keep Unity camera/view geometry facts in `Targets.Unity3D` and input reachability in `Availability.Unity3D`. Compose a deterministic route-selection policy without a reverse module dependency. Rendering order and EventSystem input routing are different concerns; `RaycastAll` alone is not evidence that arbitrary camera-stack compositing is covered.

Replace AutoLab's fixed-camera assumption only in this stage. Cover Overlay/Camera/WorldSpace canvases, different camera viewports, active camera changes, and the chosen camera-stack configuration with explicit fixtures. Resolve ambiguity rather than silently selecting whichever camera enumeration returns first. A RenderTexture minimap or multiple displays requires its own pointer-coordinate/input-route mapping and is not automatically supported by discovering cameras. Add a pipeline-specific adapter only if actual APIs require it, preserving the modular specification's integration boundary.

Acceptance: the executed point/route is the point/route evaluated as reachable; camera changes invalidate stale geometry; ambiguous/unsupported routes produce diagnostics, not false availability.

## IA-5 — Additive scenes and transitions

Capture current bindings/views; do not retain startup-only scene dictionaries. Target IDs must be unique in the configured interaction scope; prefab instances require an explicit stable identity policy. Target replacement under the same ID must not silently reuse a previous object's observation.

The Project supplies scene/game readiness; the Framework owns whether an automation run waits, cancels or times out. Scene-loaded is not synonymous with product-ready. Transition handling must include missing/replaced EventSystems, device lifetime, pending waits and gesture release cleanup. Caches with explicit invalidation may be introduced only when required by measured cost; polling every object is not a mandatory architecture rule.

Acceptance: additive HUD/modal targets enter and leave discovery; duplicate IDs fail clearly; unload during Hold/Drag releases input or cancels with a diagnostic; no stale observer/binding survives into the next scene; an expected transition is not automatically classified as a product failure.

## IA-6 — Adopter ergonomics

Keep target opt-in explicit, supply conservative defaults and overrides, validate IDs/required components, and centralize composition. Avoid requiring a game-rule component on every Button. Framework build APIs may offer convenient defaults but cannot silently choose product policy or create a global service locator.

Accept with a second concrete Project fixture that integrates through public APIs without reconstructing Runner/Monkey loops. Full editor tooling and pipeline-specific work require the appropriate integration identity, not an unbounded generic Composition Module.

## Boundaries that must remain unchanged

Capabilities are static supported interaction routes, availability is a current derived query, host reachability is not domain success, and Oracle PASS is distinct from execution coverage. A seed is deterministic selection input, not a replay of arbitrary camera/scene/game-state evolution. Input must traverse the ordinary product route. CI remains manual-only; this migration introduces no automatic workflow trigger.
