# IA-1 validation record

Date: 2026-09-27.

Reviewed integration commit: `ac4ce76aeae72e55c67a9e3840aa169759406020`.

Status: **implementation integrated; Unity/.NET acceptance pending**.

The staged implementation and acceptance authority remain in `INTEGRATION_PLAN.md`. This record supplements that plan; it does not change Framework revisions, production behavior, scene configuration, or the IA-2 start gate.

## Checks actually executed

The following two files were read from the exact reviewed commit and copied into an isolated Python test directory. Their Git blob SHA-1 values were recomputed from the complete UTF-8 bytes and matched the repository values before execution:

| File | Verified Git blob SHA-1 |
| --- | --- |
| `scripts/validate-interaction-architecture.py` | `ae9014c13be0405b1faea985a51f23ce94c7833d` |
| `scripts/test_interaction_architecture.py` | `2a9f1bece3069ae41b156c5730299090c47c0eba` |

Executed `python test_interaction_architecture.py` in that isolated directory.

Result: **12 tests, 12 passed, 0 failures/errors**.

These fixtures cover valid Framework-to-Module references, forbidden Module-to-Framework references (including GUID references), dependency cycles, legacy source and serialized identities, duplicate assemblies/GUIDs, missing source checkouts/dependencies, neutral Framework-to-Unity reverse dependencies, and exclusion of historical Markdown from active-source checks.

This result validates the checker against its fixtures. It is **not** a successful scan of a fully initialized Project checkout, a C# unit-test result, or a Unity runtime verdict.

## Source review

The reviewed `AutoLabSeededPointerMonkeyProbe` uses `AutomationLabInteractionComposition.CreateRuntime`, `UnityPointerMonkeyFactory.Create`, and `UnityPointerMonkeyQaRunner.RunAsync`, rather than rebuilding a second select/execute/verify/evidence loop in the Project.

`AutoLabMonkeyRunAdapter` supplies the product-specific Drop expectation and presentation/pacing hooks. The Probe requests cancellation on disable/destroy, and its awaited run path disposes the virtual input device in `finally`. Actual runtime cleanup remains part of the Unity acceptance gate.

The implementation already present on `main` was retained. No alternate migration based on the older `6c5f4c4` baseline was applied over it.

## Checks not executed

- Full initialized-checkout architecture and Unity import-policy scans.
- .NET restore/build and Runner/Monkey NUnit suites.
- Unity compilation, EditMode/PlayMode tests, and Player builds.
- Migrated P11/P12 AutoLab runtime and cancellation acceptance.

The validation environment did not have Unity Editor or the .NET SDK. A previous P12 runtime PASS before extraction is not transferred to the migrated revision set.

## Remaining IA-1 acceptance

Follow the exact pinned-submodule update and validation commands in `INTEGRATION_PLAN.md`; do not use `git submodule update --remote` for this acceptance.

Require zero compile errors/missing scripts, run P12 with seed `12345` and `25` iterations with the configured `7/7` coverage and passing verification/evidence, run P11 separately, and verify disabling the Monkey during Hold/Drag cleans up without reporting cancellation as PASS.

IA-2 through IA-6 remain planned. Capability-aware availability, UI occlusion, dynamic cameras/camera-stack routing, scene-transition readiness, and target-authoring defaults are not claimed as implemented by this record.
