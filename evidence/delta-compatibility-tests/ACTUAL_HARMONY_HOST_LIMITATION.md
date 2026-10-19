# Actual Harmony dispatch smoke: unavailable in this host

18 October 2026. A single optional `Validate.DeltaCompatibility.ps1 -ActualHarmony`
attempt used the installed game's `0Harmony.dll` in the separate net8.0 harness.
It failed before the first method could be patched:

```
System.TypeInitializationException: HarmonyLib.HarmonySharedState
System.TypeInitializationException: MonoMod.Utils.ReflectionHelper
System.MethodAccessException: attempted access to
System.Reflection.Emit.SignatureHelper.GetMethodSigHelper(
    System.Reflection.Module,
    System.Runtime.InteropServices.CallingConvention,
    System.Type)
```

This establishes an incompatibility between the installed Harmony/MonoMod build
and this .NET 8 test host. It does not establish a defect in the Delta adapter.
No replacement runtime, library download, production change, game launch or retry
with a workaround was performed. Actual Harmony callback dispatch under the game's
Unity/Mono runtime remains unvalidated and requires an in-game check.

The default test mode independently passed **129 observable hierarchy, visibility,
driver registration, lifecycle and adapter-policy checks**, executing both actual
production source files with narrow Unity/native/Harmony API stand-ins. Result:

`build/delta-compatibility-validation/harness-run-20261018-010943-865/validation.json`

The optional real-dispatch source is retained as a clear experiment, not counted
among passing checks. Default test mode does not load the installed Harmony DLL.
