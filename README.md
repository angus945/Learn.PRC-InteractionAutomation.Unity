# Learn.PRC-InteractionAutomation.Unity

Unity / .NET Console interaction-automation learning Host.

## Communication source composition

The communication stack is source-first under Assets/CraftyRacoon:

```text
Module.Communication.JsonRpc
Module.Communication.JsonRpc.WebSocket
Module.Communication.Unity3D
```

The Project does not import DLL builds of those three modules.

`Module.Communication.JsonRpc.WebSocket` now contains the small JSON-RPC 2.0
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
└─ Workspace.InteractionAutomation/
```

The Project gitlinks are the revision authority. Update them explicitly; do not use
`git submodule update --remote` as an implicit version-selection mechanism.

After pulling:

```shell
git submodule sync --recursive
git submodule update --init --recursive
git submodule status --recursive
```

## Unity compilation boundary

Unity compiles the reusable communication modules directly from their C# 9 source and
canonical asmdefs.

- JsonRpc: no third-party dependency.
- JsonRpc.WebSocket: references JsonRpc source + Newtonsoft.Json.
- Unity3D: references JsonRpc source; selected Host code may reference WebSocket.
- No Communication module DLL should coexist with these source assemblies.

.NET SDK test/tool projects remain separate from Unity assembly composition.

## RpcLab

The current single-scene experiment lives under `Assets/_RpcLab`. It first validates
local state read/write, then adds one external .NET Console peer over loopback WebSocket.

This Project is still a learning Host. Target discovery, physical-input automation,
Monkey orchestration and Verification integration are later slices.

## Validation

Static policy checks:

```shell
python scripts/validate-unity-imports.py
python scripts/test_import_policy.py
```

Execution acceptance still requires the actual Unity Editor and .NET SDK. Static checks
do not establish runtime networking, IL2CPP/AOT or platform compatibility.
