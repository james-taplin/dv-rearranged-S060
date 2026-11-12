# Building Rearranged S060

The supported product is **Stage 7**, candidate **0.23.2**. The project compiles
only the current product and rejects a historical stage. Use `Build.Runtime.ps1`
to build and stage its DLL directly, or `Build.Release.ps1`
to regenerate fixtures, export both packs, run checks and stage the complete
package. Neither command installs files into the game.

## Local dependencies

- Windows PowerShell with access to an installed Derail Valley B99 build2702.
- An x64 .NET SDK capable of building net48 and net8.0 projects; development uses
  .NET SDK 10.0.401 and the .NET Framework 4.8 targeting assemblies.
- Installed Unity Mod Manager and Loco Mesh Splitter assemblies for references.
- Unity Editor **2019.4.40f1**, usable with the local Unity licence.
- A reference Unity project containing the CCL CarCreator SDK under
  `Assets/CarCreator`, plus its package manifest and ProjectVersion file.
- Python with **UnityPy** and its image dependencies for local native fixtures.
- The original custom Franklin source bundle at
  `<GamePath>/Mods/RearrangedS282/assets/s282_drivetrain` when exporting Dockside.
  This is an authoring dependency; the installed S060 runtime does not depend
  on the S282 DLL or bundle.

The C# project restores Krafs.Publicizer 2.2.1. Publicized game assemblies are
build-local copies; installed game DLLs are not changed. Inherited animation
source and custom pipe authoring data must also be present in this checkout.

## Configuration

`Build.Config.ps1` supplies paths and reads the candidate version from root
`Info.json`. Its optional override file is **`Build.Local.json`**, ignored by Git.
Create it in the repository root when local paths differ:

```json
{
  "GamePath": "D:\\SteamLibrary\\steamapps\\common\\Derail Valley",
  "Dotnet": "C:\\Program Files\\dotnet\\dotnet.exe",
  "Unity": "D:\\Unity\\2019.4.40f1\\Editor\\Unity.exe",
  "Python": "D:\\Tools\\dv-python\\Scripts\\python.exe",
  "ReferenceProject": "D:\\UnityProjects\\CarCreatorReference"
}
```

The settings are `GamePath`, `Dotnet`, `Unity`, `Python` and `ReferenceProject`.
Generated QA data goes to `build/qa`. Runtime assembly version in the project
must agree with `Info.json`; packaging checks the built assembly's version.

## Supported workflow

Run commands from the repository root. Do not run two Unity authoring/audit
processes against `Meyer/UnityProject` concurrently.

```powershell
# Current runtime only; stages build/stage7/RearrangedS060.dll.
.\Build.Runtime.ps1

# Complete preparation, sequential exports, native audits and packaging.
.\Build.Release.ps1
```

The complete command prepares `build/qa`, creates the ignored authoring project
from `ReferenceProject` if it is absent, exports Meyer then Dockside, validates
their evidence, runs native runtime audits and stages the runtime and complete
package. It does not launch the game. The hidden Unity Editor processes are
authoring/test processes.

For investigation, the constituent commands are:

```powershell
.\Prepare.QA.ps1                  # Reuses fixtures only when hashes still match.
.\Prepare.QA.ps1 -Force           # Regenerates them from the installed game.
.\Meyer\Setup.Authoring.ps1 -ReferenceProject 'D:\UnityProjects\CarCreatorReference'
.\Meyer\Run.Authoring.ps1 -UpdateDraft -ExportPrototype
.\Dockside\Run.Authoring.ps1
.\Test.Runtime.ps1
```

Run `Setup.Authoring.ps1` only when the authoring project does not exist; it
refuses to overwrite an existing project. Direct authoring commands accept
`-Unity`; Dockside also accepts `-FranklinBundle`. `Build.Release.ps1` passes
configured paths to them. Direct commands otherwise retain their own defaults.

To reuse completed, hash-bound asset exports while retesting the runtime:

```powershell
.\Build.Release.ps1 -SkipAuthoring `
  -MeyerEvidence 'D:\Source\rearranged-s060\build\meyer\authoring-TIMESTAMP' `
  -DocksideEvidence 'D:\Source\rearranged-s060\build\dockside\authoring-TIMESTAMP'
```

This still prepares/verifies native fixtures and reruns runtime tests. Both
evidence paths are required. Historical unbound reports cannot substitute.

## What the evidence establishes

`Prepare.QA.ps1` records input/script hashes and output hashes in
`build/qa/fixture-receipt.json`. Native body/wheels, oil cups, lamps and atlas
images are local-only inputs. Do not add these to a distributable asset bundle.

Each asset export reopens its bundle in Unity. `Complete.BundleEvidence.ps1`
records the bundle, pack metadata and required validation report hashes.
`Test.BundleEvidence.ps1` requires the exact canonical report set and matches
the reopened bundle SHA256 to the exported binary. Changed bytes, missing
reports or stale reports fail packaging.

For independent powered groups, run `Test.IndependentSlip.ps1 -RuntimePath <DLL>`.
It late-loads the candidate after native TypeManager initialization in a private
Unity release player. The native ECS, engines, RPM leases, phase, effects and
traction owner are real; the final bogie-force boundary is intercepted. See
tests/IndependentDrive/README.md for scope and repeatable commands. Pass its
hash-bound `independent-slip-validation.json` to Build.Radio.ps1 using
`-IndependentEvidence`; the package gate requires it in addition to the Editor
suite. Build.Release.ps1 runs both automatically. For an unpublished test
candidate use `Package.Nexus.ps1 -StagedOnly` to retain the public listing copy.

`Test.Runtime.ps1` runs exhaust, cylinder-cock, direct-cleanup and Meyer collision
audits in the authoring Editor, plus native contact delivery and parent-deletion
cleanup in an isolated PlayMode scene. The contact runner uses a uniquely named
QA assembly to avoid shadowing the game's `Assembly-CSharp`. Its receipt binds
report hashes to the exact runtime DLL. `Build.Radio.ps1` requires those matching
native reports, including the initial-smoke-before-owner, detached-audio,
inlet-publication and collision ownership regressions. It also runs the
native oiling/engine suites, bounded diagnostic budget checks, functional checks, axle transitions, measured
geometry and the runtime IL audit. `Package.Articulated.ps1` verifies the core
file receipt and both asset receipts before producing the complete archive.

Assertion totals include repeated sweeps/iterations. They do not measure audible
balance, a full driving scene, manual oil-can interaction or long-session memory.
Use [the gameplay guide](docs/RADIO-ARTICULATED-TESTING.md) for acceptance.

## Outputs and installation

The supported public artifact is `build/nexus-VERSION-single/RearrangedS060-VERSION-UMM.zip`.
It contains one mod root/Info.json, the runtime, two native CCL bundles under
assets/Meyer and assets/Dockside, and original Shay models under assets/Shay.
There are no nested archives. CCL discovers its donor bundles recursively.

For a runtime/docs-only update, retain the previously verified bundle exports.
Run Test.BundleEvidence for the Meyer and Dockside exports, Build.Runtime and
Test.Runtime for the candidate, then Build.Radio with that exact native evidence,
Package.Articulated with both bundle-evidence paths, and Package.Nexus. Build.Radio
also runs the functional/save, oiling, engine, geometry and transition suites.
Build.Release is the full authoring route; do not regenerate unchanged models
merely to refresh a runtime or documentation package.

Run tests/Nexus/NativeInstaller.ps1 using Windows PowerShell to exercise actual
UMM extraction/update/uninstall and old-pack migration in isolated scratch folders.
CombinedPackageAudit.RunBatch reopens those extracted bundles in Unity using
S060_COMBINED_PACKAGE and S060_COMBINED_REPORT. These do not load Derail Valley.

Installation is separate and requires James's authorization. The current single-mod
installer is Install.Nexus.ps1, which checks the game is closed and preserves
settings without creating a backup. James performs gameplay acceptance; never
launch DV or create a game profile as an inferred validation step.

## Historical source preservation

The following archive/checkpoint notes describe the earlier 0.15.0 cleanup, not
current candidate validation or installation.

Seventy-four inactive source/resource/script files are under
`archive/legacy-runtime`, with original paths and SHA256 in `moved-files.json`.
This includes old Build.Stage1/2, Functional, Meyer/Dockside package routes and
prototype validators. They are excluded from the current project. Root
`Build.Runtime.ps1` contains the actual builder; it no longer calls an old-stage
wrapper. Old scripts are reference material, not a supported release workflow.

Local source checkpoint `2d3baf5` contains the reviewed runtime, authoring, tests
and build scripts. A clean Git export of runtime/build-helper dependencies
compiled with zero warnings/errors and matched the tested candidate's executable
IL/type/field definitions; see the handover and
`build/cleanup-0.15.0/clean-source/clean-source-validation.json`.
The complete authoring/release pipeline passed in the main checkout. It was not
repeated in a wholly fresh clone with a newly imported CarCreator project.
Documented game/tool/SDK inputs are still required; native fixtures and that
authoring project remain local/generated inputs. Nothing was pushed.
