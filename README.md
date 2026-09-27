# Learn.PRC-InteractionAutomation.Unity

Unity / .NET Console interaction-automation learning Project.

## InteractionAutomation integration

The current integration sequence and acceptance gates are in [INTEGRATION_PLAN.md](INTEGRATION_PLAN.md).

IA-1 extracts pointer and Monkey workflows into canonical Framework repositories, keeps capability Modules independent, and migrates AutoLab to shared composition. The code migration is implemented; Unity/.NET runtime acceptance is pending. Later availability, camera-stack and scene-transition stages are not claimed complete.

```text
Project AutoLab
  -> Framework.InteractionAutomation.Monkey.Unity3D (optional QA workflow)
  -> Framework.InteractionAutomation.Runner.Unity3D (shared composition)
  -> neutral Frameworks
  -> capability Modules + Unity adapters
```

The Framework owns the reusable QA loop. The Project owns presentation, product expectations, actual source locations, revision pins and device lifetime.

## Communication source composition

The communication stack is source-first under Assets/CraftyRacoon:

```text
Module.Communication.JsonRpc
Module.Communication.JsonRpc.WebSocket
Module.Communication.Unity3D
```

The Project does not import DLL builds of those three modules.

`Module.Communication.JsonRpc.WebSocket` contains the small JSON-RPC 2.0
request/response/notification implementation directly and uses
`System.Net.WebSockets` for transport. StreamJsonRpc and its former transitive
runtime graph have been removed.

The only third-party communication library owned by this Unity Project is:

```text
com.unity.nuget.newtonsoft-json 3.2.2
```

Its Newtonsoft.Json assembly is referenced by the WebSocket source asmdef. Do not add
another Newtonsoft.Json DLL under Assets.

## Submodules

```text
Assets/CraftyRacoon/
├─ Module.Communication.JsonRpc/
├─ Module.Communication.JsonRpc.WebSocket/
├─ Module.Communication.Unity3D/
├─ Workspace.InteractionAutomation/
├─ Workspace.InteractionAutomation.Unity3D/
├─ Workspace.Verification/
├─ Framework.InteractionAutomation.Runner/
├─ Framework.InteractionAutomation.Monkey/
├─ Framework.InteractionAutomation.Runner.Unity3D/
└─ Framework.InteractionAutomation.Monkey.Unity3D/
```

The Project gitlinks are the revision authority. Update them explicitly; do not use
`git submodule update --remote` as an implicit version-selection mechanism.

Close Unity before updating the coordinated assembly revision set. After pulling:

```shell
git submodule sync --recursive
git submodule update --init --recursive
git submodule status --recursive
```

## Unity compilation boundary

Unity compiles reusable source through canonical asmdefs. The Project does not rewrite
those assemblies or keep compatibility copies. Neutral Framework SDK projects receive
`InteractionAutomationRoot` and `InteractionRunnerRoot` from `Directory.Build.props`.
.NET SDK artifacts go outside Assets under `.artifacts`.

- JsonRpc: no third-party dependency.
- JsonRpc.WebSocket: references JsonRpc source + Newtonsoft.Json.
- Unity3D: references JsonRpc source; selected Host code may reference WebSocket.
- No Communication module DLL should coexist with these source assemblies.

.NET SDK test/tool projects remain separate from Unity assembly composition.

## Labs

`Assets/_RpcLab` contains the local state / loopback WebSocket experiment.
`Assets/_AutomationLab/AutoLab.unity` contains the P10/P11/P12 pointer and Monkey fixture.
Do not run multiple input-driving probes concurrently on the same fixture.

## Validation

```shell
python scripts/test_interaction_architecture.py
python scripts/validate-interaction-architecture.py
python scripts/test_import_policy.py
python scripts/validate-unity-imports.py
```

The architecture checker has 12 positive/negative fixture tests. Running those verifies
the checker, not this entire Project or its C# code. Run the full scan after initializing
the pinned submodules, then use Unity Test Runner and the AutoLab acceptance gate.

Execution acceptance requires the actual Unity Editor and .NET SDK. Static checks do
not establish runtime networking, serialized loading, IL2CPP/AOT or platform compatibility.
