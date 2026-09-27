# IA-3A acceptance — automatic Unity UI state

Date: 2026-09-27. Status: implemented; new C#/Unity execution acceptance pending.

The user reported IA-2 successful and approved automatic UI-state reading only. Occlusion remains deferred as IA-3B. `INTEGRATION_PLAN.md` remains the stage authority.

## Implemented boundary

`Module.InteractionAutomation.Availability.Unity3D` is in the existing Unity workspace. It implements the IA-2 query contract without depending on Runner/Monkey. It resolves the current binding in the supplied scope and reads the Selectable on that binding's own GameObject. It does not infer a control from a parent/child or infer static capabilities.

| Query | Selectable disabled / not interactable |
| --- | --- |
| Click, DoubleClick, Hold | Reject |
| DragSource | Reject |
| Hover, Scroll, DragDestination | No rejection from this Selectable rule |

An inactive/disabled binding itself is still rejected for every capability. A non-Selectable target receives no control-specific restriction. This is a conservative declared-control policy, not proof that an arbitrary custom input handler obeys Selectable semantics.

`Selectable.IsInteractable()` supplies parent CanvasGroup.interactable and ignoreParentGroups semantics after Unity's normal lifecycle updates. There is no duplicated parent traversal or manual availability refresh. A disabled Selectable returns `Disabled`; non-interactable state returns `NotInteractable` with `unity.ui.selectable.not-interactable`. Project policy continues to use `PolicyDenied`. Typed reasons and codes accumulate.

Not inspected: CanvasGroup.alpha, blocksRaycasts, Graphic.raycastTarget, masks, raycasters, EventSystem routing, camera stacks, UI Toolkit or product game state. An available result is not a reachability claim. There is no continuous permission monitoring inside an already-admitted gesture.

## Composition

```csharp
var runtime = UnityInteractionAutomationBuilder.Create()
    .UseLoadedSceneTargets(kinds, geometryProviders, bindings)
    .UseUnityUiStateAvailability()
    .AddAvailabilityPolicy(projectPolicy)
    .UsePhysicalInput(input)
    .UseInputProcessingBoundary(boundary)
    .Build();
```

The builder obtains the exact binding source from the final UnityInteractionTargetSource at Build. It does not independently discover global bindings. Custom target sources with unknown binding scope fail at Build when automatic UI-state composition is selected; those consumers must explicitly supply their matching evaluator. Base, UI-state and Project policies share the same final evaluator with Runner and the existing Monkey factory.

The UI-state feature is opt-in for Framework consumers. AutoLab enables it in its standard CreateRuntime method. No extra component is required on existing targets. Initial implementation recaptures the supplied binding scope per evaluation; it has no startup cache and does not claim a full-scene polling performance optimization.

## Coordinated revision set

| Repository | Pinned revision |
| --- | --- |
| Workspace.InteractionAutomation | `df36d2e599eac7a01dbacbfed7dd886c3736fdff` |
| Workspace.InteractionAutomation.Unity3D | `64abeaba424c7b781e0f716d0c72a935b9377a1d` |
| Framework.InteractionAutomation.Runner.Unity3D | `ee69d656a8a05f32aae9fc90bfd665fb9b4bc9b9` |
| Framework.InteractionAutomation.Runner (unchanged) | `637072c1135bcf9cb44044e6c9d8cdaaabfdda56` |
| Framework.InteractionAutomation.Monkey (unchanged) | `21e1bddb5e4bc53ce531d89629aff9a24b96c116` |
| Framework.InteractionAutomation.Monkey.Unity3D (unchanged) | `26ed9a0aed74f4cc8f7cdaa990c37e8b4f8b0031` |
| Workspace.Verification (unchanged) | `c85081e463233f92b014b19faec92b8d66b1e3a8` |

New explicit dependency: Runner.Unity3D Framework -> Availability.Unity3D Module. There is no reverse Module -> Framework edge. No new repository, source copy, nested dependency checkout or CI trigger is introduced. Project gitlinks remain authoritative.

## Update and focused tests

Save local modifications and close Unity, then run from the Project root:

```shell
git pull --ff-only
git submodule sync --recursive
git submodule update --init --recursive
python scripts/test_interaction_architecture.py
python scripts/validate-interaction-architecture.py
python scripts/test_import_policy.py
python scripts/validate-unity-imports.py
```

Do not use `--remote`. Reopen Unity, regenerate IDE project files and require zero compilation errors/missing scripts. Run existing suites and these new fixtures:

- `InteractionAvailabilityReasonTests`: typed host reasons, Project denial composition, read-only codes and factory validation.
- `UnityUiStateAvailabilityTests`: control/role mapping, live changes, disabled components, parent groups, ignoreParentGroups, exact scopes and deliberately ignored raycast/alpha properties.
- `UnityUiStateCompositionTests`: scope reuse, source changes before Build, query/Runner agreement, zero input on rejection, policy accumulation and unsupported-scope fail-fast behavior.

## Live Probe

1. Open `Assets/_AutomationLab/AutoLab.unity` outside Play Mode.
2. Disable `AutoLabAvailabilityProbe` (IA-2), `AutoLabSeededPointerMonkeyProbe`, `InteractionRunnerProbe` and `VirtualMouseEventSystemProbe`. Keep `UnityFrameInputProcessingBoundary` enabled.
3. Add `AutoLabUiStateProbe` to the existing Probe GameObject. It uses the existing frame boundary and button.confirm components. Camera can stay empty in this existing MainCamera fixture.
4. Enter Play Mode without simultaneous manual pointer operations. The Probe rejects competing enabled input probes.

Expected messages:

```text
IA3A SELECTABLE DENIAL / HOVER PASS
IA3A LIVE UI UNLOCK / PRODUCT CLICK PASS
IA3A CANVAS GROUP / IGNORE PARENT GROUPS PASS
IA3A STALE UI SELECTION / ZERO INPUT PASS
IA3A AUTOMATIC UI STATE PASS
```

The Probe toggles Button.interactable as fixture setup, verifies Hover while Click is denied, restores interaction and verifies real EventSystem-driven product clicks. It temporarily borrows/adds CanvasGroups on the button and its parent to test group inheritance and ignoreParentGroups. Finally it selects a Click, locks the Button, and requires rejection with zero physical submissions and zero coverage credit. It never calls product click callbacks directly or forces InputSystem.Update.

Original Button/CanvasGroup settings are restored in finally; groups created by the fixture are destroyed. The virtual mouse is disposed after the awaited run exits. Cancellation is not PASS. The committed scene, target declarations, existing Probes and HUD are unchanged.

Exit Play Mode and disable/remove the new Probe. Rerun IA-2, P11 and P12 separately. P12 uses seed 12345, 25 iterations and the original 7/7 coverage with passing verification/evidence. Parent state changes should be observed after normal Unity processing; do not insert a framework RefreshAvailability call.

## Authoring evidence

Source and candidate Git diffs were reviewed. New metadata GUID format/within-change uniqueness was checked with Python; an invalid candidate GUID was corrected before integration. This is not a complete checkout GUID scan. The authoring environment has no Unity Editor, .NET SDK or network access for a complete source checkout. No C# compile, NUnit run, Unity EditMode/PlayMode test, full initialized-checkout architecture scan or runtime Probe execution was performed here. Presence of test code is not execution evidence. Record actual results before marking IA-3A accepted.
