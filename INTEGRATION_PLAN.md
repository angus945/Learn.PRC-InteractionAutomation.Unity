# InteractionAutomation production integration — staged execution

Date: 2026-09-27

This Project owns the integration sequence, source locations, exact revision pins and runtime acceptance. The reusable repositories do not check out, mirror or rewrite one another. Each stage is a separately reviewable change. Do not claim a later stage is supported because its extension point exists.

## Status

| Stage | Scope | Status |
| --- | --- | --- |
| IA-1 | Canonical Framework extraction, Timing separation, shared Unity composition and AutoLab consumer migration | COMPLETE — user reported all acceptance checks passed on 2026-09-27 |
| IA-2 | Capability-aware availability, project permission and execution revalidation | COMPLETE — user reported success on 2026-09-27 |
| IA-3A | Automatic Selectable / CanvasGroup interaction-state reading | Implemented; new C#/Unity execution acceptance pending |
| IA-3B | uGUI hit-test, raycast reachability and occlusion | DEFERRED at the user's explicit request; not part of IA-3A |
| IA-4 | Dynamic view resolution, multiple cameras and camera-stack acceptance | Planned |
| IA-5 | Additive scenes, transitions, readiness, cancellation and target incarnation lifecycle | Planned |
| IA-6 | Adopter defaults, target authoring ergonomics and acceptance matrix | Planned |

The earlier P10/P11/P12 AutoLab baseline was reported runtime-passing by the user before extraction. The user subsequently reported IA-1 and IA-2 successful, then approved automatic UI-state reading while continuing to defer occlusion. These are user-reported acceptance records, not independently executed tests in the authoring environment. The older learning roadmap remains phase history; this file tracks production-integration work.

## IA-1 — Framework/source identity and adoption entrypoint

### Ownership

- `Workspace.InteractionAutomation`: Coordinates, Targets, Availability, PhysicalInput and Timing Modules only.
- `Workspace.InteractionAutomation.Unity3D`: Unity capability adapters, including Timing and live target-binding capture. No Runner/Monkey workflow implementation remains under a Module identity.
- `Framework.InteractionAutomation.Runner`: typed request dispatch and complete pointer execution workflows; reusable registry/build API.
- `Framework.InteractionAutomation.Monkey`: coverage-first seeded selection/execution; one-way dependency on Runner.
- `Framework.InteractionAutomation.Runner.Unity3D`: explicit builder and immutable shared runtime. Dependencies are borrowed; the Project owns device lifetime.
- `Framework.InteractionAutomation.Monkey.Unity3D`: complete select/prepare/execute/verify/evidence/stop loop. The Project supplies presentation/pacing and product-verification hooks, not a duplicate loop.

The Project mounts all six repositories directly under `Assets/CraftyRacoon`. No old namespace aliases, bridge assemblies or source copies are used. `Directory.Build.props` supplies neutral Framework dependency-root properties; source owners retain their canonical asmdefs.

### IA-1 accepted revision set (historical)

| Repository | Accepted revision |
| --- | --- |
| Workspace.InteractionAutomation | `666db89d7a9ebfe66c62d9a2a61589b629b726d8` |
| Workspace.InteractionAutomation.Unity3D | `1da731004fdff13fb1034e8d1410ff32aef0a6fb` |
| Framework.InteractionAutomation.Runner | `23e51fbc803050b66461984b7dfcd0ce8c081ea1` |
| Framework.InteractionAutomation.Monkey | `bc79acb0c54db41b132a69280f9af3290918c468` |
| Framework.InteractionAutomation.Runner.Unity3D | `81c459c342d9acbfd4c280103d7fa5f57f187d76` |
| Framework.InteractionAutomation.Monkey.Unity3D | `26ed9a0aed74f4cc8f7cdaa990c37e8b4f8b0031` |
| Workspace.Verification | `c85081e463233f92b014b19faec92b8d66b1e3a8` |

Project baseline: `ac4ce76aeae72e55c67a9e3840aa169759406020`. Current gitlinks are authoritative; the table above records the accepted IA-1 baseline, not current pins. Do not update with `--remote` when reproducing a stage.

### AutoLab changes

`AutomationLabInteractionComposition.CreateRuntime` uses the Unity Runner builder. `UnityPointerMonkeyFactory.Create` consumes that same runtime, so selection and execution share the TargetSource, Availability and input-processing boundary.

`AutoLabSeededPointerMonkeyProbe` starts one Framework QA run. `AutoLabMonkeyRunAdapter` owns only the existing HUD/pacing and concrete `button.confirm -> toggle.music` drop expectation. All scene field names, script GUID references and target IDs are retained. Timing's serialized assembly/type identity is changed to `Module.InteractionAutomation.Timing.Unity3D`.

The Monkey lifecycle adapter cancels on disable/destroy and disposes its device after awaited gesture cleanup. It never labels cancellation PASS. This is not acceptance of arbitrary scene-destructing clicks. The older P11 coroutine probe remains a stable-scene regression fixture, not a transition-lifetime implementation.

### IA-1 authoring record and acceptance

The IA-1 authoring record reported 12 passing positive/negative Python checker fixtures, reviewed namespace-only changes in existing scenarios/HUD, and reviewed the retained scene GUIDs. One quaternion negative-zero textual representation normalized to zero alongside the intended Timing type identity change.

No full initialized-checkout architecture scan, C# compilation, .NET test run, Unity EditMode/PlayMode test run or Player build was executed in that authoring environment. The user later reported all IA-1 acceptance checks passed. This closes IA-1; it does not validate later-stage code.

### Regression gate retained for later stages

Close Unity while updating a coordinated source revision set, then run from the Project root:

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

The architecture checker checks active source/serialized identities, source-assembly presence, duplicate assemblies/GUIDs, Module-to-Framework reverse edges and assembly cycles. It is a static guard, not a compiler. Historical Markdown is not treated as active source.

Open Unity and regenerate IDE project files. Require zero compile errors and zero missing scripts in `Assets/_AutomationLab/AutoLab.unity`. Run Framework/Timing tests, then AutoLab with seed 12345 and 25 iterations. Require six pointer actions, seven coverage pairs, passing verification, and retained evidence. Run P11 separately, not concurrently with Monkey. During a separate Monkey run, disable the probe during Hold/Drag and verify cleanup without PASS-on-cancellation.

## IA-2 — Availability contract and Project policy

### Implemented scope

`Module.InteractionAutomation.Availability` owns the host-neutral `InteractionAvailabilityContext`, structural evaluator, policy-denial result and immutable evaluator composition. No Module depends on Framework request types.

The context carries one atomic capability, a captured target, its existing bounds-center point and, for drag, `DragSource` / `DragDestination` plus the counterpart snapshot. A drag destination need not advertise PointerDrag. `ForTarget`, `ForDragSource` and `ForDragDestination` prevent incomplete/default context use at standard entrypoints.

`CompositeInteractionAvailabilityEvaluator` queries each policy on every evaluation and accumulates structural reasons and stable Project reason codes. An allowing policy cannot undo another denial. Evaluator errors propagate. Product permission is not cached, synchronized from UI events, or inferred from whether a purchase/action would succeed.

Runner recaptures current targets and revalidates capability-specific permission before the first physical submission of each request. Monkey evaluates individual action candidates and drag pairs; it no longer removes the whole target just because one capability is denied. Rejected execution gets no coverage credit. The existing deterministic generator, candidate comparer, pointer sequence and cleanup order are retained.

`UnityInteractionAutomationBuilder.AddAvailabilityPolicy` adds Project restrictions while retaining the base evaluator. `UseAvailability` explicitly replaces only the base. The final composed instance is shared by Runner, runtime queries and the Monkey factory. The builder's policy list freezes at Build; the external game state read by those policies can continue changing.

`AutomationLabInteractionComposition.CreateRuntime/CreateRunner` accept an optional Project evaluator. `AutoLabInteractionAvailabilityPolicy` demonstrates a Project-owned permission state and target-specific rules. The opt-in `AutoLabAvailabilityProbe` exercises the real virtual mouse/EventSystem path and a stale selected Click. It is not added to the scene automatically. IA-3A subsequently adds UI-state reading to the same composition without changing the IA-2 Probe.

### IA-2 accepted revision set (historical)

| Repository | Accepted revision |
| --- | --- |
| Workspace.InteractionAutomation | `0691798c7f990bc6a4d54f5a1b604ba6b0955a3d` |
| Framework.InteractionAutomation.Runner | `637072c1135bcf9cb44044e6c9d8cdaaabfdda56` |
| Framework.InteractionAutomation.Monkey | `21e1bddb5e4bc53ce531d89629aff9a24b96c116` |
| Framework.InteractionAutomation.Runner.Unity3D | `d5c09a04760b76b4fb036524eeea58a47e7d8ac7` |
| Workspace.InteractionAutomation.Unity3D | `1da731004fdff13fb1034e8d1410ff32aef0a6fb` |
| Framework.InteractionAutomation.Monkey.Unity3D | `26ed9a0aed74f4cc8f7cdaa990c37e8b4f8b0031` |
| Workspace.Verification | `c85081e463233f92b014b19faec92b8d66b1e3a8` |

Project baseline: `bf3223f0942c4c3dc330f42328ec5e37ad8df5fe`. The user reported this stage successful; current gitlinks select the later stage.

### Limits and acceptance

This is operation-admission revalidation, not an authorization lease or continuous permission monitor. An already-admitted gesture retains its existing cleanup semantics. No automatic retry/reselection, no atomicity across asynchronous input submissions, and no new expected-denial QA verdict classification are introduced. The existing QA wrapper stops on execution errors; explicit negative tests assert the expected rejection.

IA-2 itself included no hit-test/occlusion, automatic Selectable/CanvasGroup inspection, camera/view routing, scene readiness or transition handling. The point remains the existing bounds center. Occlusion remains deferred until the user resumes that work.

New/expanded IA-2 tests cover contexts and denial composition, all six Runner capabilities, live state changes, source recapture, destination permissions, and selected-action rejection with no input/coverage. The pre-existing golden seeded-sequence and gesture lifecycle tests remain regression gates.

IA-2 authoring verification was remote source/diff review only; Unity Editor and .NET SDK were unavailable. The subsequent user report closes IA-2 acceptance, not IA-3A. `IA2_ACCEPTANCE.md` retains its original procedure and authoring record for regression.

## IA-3A — Automatic Unity UI state

Implemented in `Module.InteractionAutomation.Availability.Unity3D`, in the existing Unity workspace. `UnityUiStateAvailabilityEvaluator` reads the current Selectable on the binding's own GameObject using the same binding scope as target capture. `Selectable.IsInteractable()` owns parent CanvasGroup.interactable and ignoreParentGroups interpretation. There is no second traversal algorithm or manually maintained availability cache.

Click, DoubleClick, Hold and DragSource are rejected when the Selectable is disabled or not interactable. Hover, Scroll and DragDestination are not rejected by this control-state rule. Inactive/disabled bindings are rejected independently. Non-Selectable controls are not inferred from parents or children. Capabilities remain explicit and unchanged. The mapping is a conservative declared-control policy, not an assertion about arbitrary custom handlers.

Core gains additive `NotInteractable` and a typed diagnostic factory; Project denial remains `PolicyDenied`. `UseUnityUiStateAvailability()` composes the Module rule using the final Unity target source's exact binding scope. `UseAvailability` cannot erase that selected layer. Unknown custom source scopes fail at Build instead of guessing global bindings. Default Framework use is opt-in; AutoLab explicitly enables the rule in CreateRuntime.

Current changed revisions: Workspace.InteractionAutomation `df36d2e599eac7a01dbacbfed7dd886c3736fdff`; Workspace.InteractionAutomation.Unity3D `64abeaba424c7b781e0f716d0c72a935b9377a1d`; Framework.InteractionAutomation.Runner.Unity3D `ee69d656a8a05f32aae9fc90bfd665fb9b4bc9b9`. Neutral Runner/Monkey, Monkey.Unity3D and Verification retain the IA-2 pins. The new explicit dependency is Runner.Unity3D -> Availability.Unity3D; no Module-to-Framework edge is introduced.

`AutoLabUiStateProbe` is opt-in and preserves the committed scene, existing Probes, HUD, target IDs and capabilities. It tests live Button state, Hover with denied Click, parent groups, ignoreParentGroups, real product clicks and stale selected-action rejection. It restores borrowed fixture state and removes groups it created in finally.

Not interpreted: alpha, blocksRaycasts, Graphic.raycastTarget, masks, occlusion, cameras, UI Toolkit or game state. Available means this rule did not reject, not that the pointer can reach the target. The initial evaluator recaptures the injected scope per query; no scan-cost optimization or startup cache is claimed. Continuous gesture permission monitoring remains out of scope.

New fixtures: InteractionAvailabilityReasonTests, UnityUiStateAvailabilityTests and UnityUiStateCompositionTests. Authoring environment: no Unity/.NET execution or full initialized-checkout scan; source/diff review and new metadata format checking only. See `IA3A_ACCEPTANCE.md` for exact update commands, revision set and runtime acceptance. IA-3A remains pending execution acceptance.

## IA-3B — Unity reachability (deferred)

Extend the existing Availability.Unity3D boundary when this stage is resumed. Use the configured production input route to evaluate the actual intended point and event recipient, not a visual-overlap guess. Account for child graphics/event handlers, CanvasGroup.blocksRaycasts, masks, transparent blockers and modal overlays. Observers remain facts; Oracles remain judgment; neither becomes the availability source of truth.

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
