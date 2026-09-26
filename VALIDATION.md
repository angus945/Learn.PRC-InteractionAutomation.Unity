# Validation status

Communication composition is now source-first:

- Module.Communication.JsonRpc: source.
- Module.Communication.JsonRpc.WebSocket: source.
- Module.Communication.Unity3D: source.
- com.unity.nuget.newtonsoft-json 3.2.2: Project-owned third-party package.

StreamJsonRpc and its transitive runtime dependency graph are no longer part of the
communication stack.

The Project policy checker was updated to reject Communication module DLL duplicates,
WebSocket Unity exclusion, missing JsonRpc source reference, wrong Newtonsoft assembly
reference and missing Newtonsoft package ownership.

The authoring environment does not contain Unity Editor or the .NET SDK, so actual C#
compilation, package restore, EditMode/PlayMode tests and runtime WebSocket exchange were
not executed here. Static policy checks are not execution evidence.
