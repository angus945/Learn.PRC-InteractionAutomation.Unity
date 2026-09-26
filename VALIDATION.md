# Validation status: Unity source compatibility correction

Executed in the authoring environment: eight Python fixture tests for the Project
import-policy checker. They cover supported syntax, file-scoped namespace rejection,
unscoped test rejection, explicit .NET-test exclusion, duplicate RPC core DLL detection,
generated obj detection, tilde sample exclusion and obsolete DLL references.

These fixtures are synthetic and do not establish that the complete repository builds.
The checker is a policy scanner, not a C# parser/compiler. Run it on the initialized
working tree, then perform actual .NET and Unity validation.

Not executed: dotnet restore/build/test, Unity source compile, EditMode/PlayMode tests,
transport plugin provisioning, runtime networking, IL2CPP, AOT or platform validation.
No .NET SDK or Unity Editor is installed in the authoring environment. An SDK-download
attempt failed DNS resolution. No test/compile success is inferred from committed code.

The changes do not create a Unity project skeleton, configure a scene, import binaries,
or delete local generated files. Third-party/runtime assembly resolution is a separate
Host integration step, not a consequence of using C# 9 syntax.
