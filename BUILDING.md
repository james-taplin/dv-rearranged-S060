# Building Rearranged S060

The supported product is **Stage 7**, candidate **0.15.0**. The project compiles
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

Runtime staging is under `build/radio-VERSION`; the complete package and receipt
are under `build/articulated-VERSION`. The complete archive contains the runtime
and two asset-pack folders. Each receipt identifies the actual files tested.

```powershell
# Separate update of an existing installation; game must be closed.
# No backup is created; this mode was explicitly requested for development.
.\Install.Articulated.ps1 -Version 0.15.0 -GamePath 'D:\SteamLibrary\steamapps\common\Derail Valley'
```

The installer expects all three previous mod folders to exist. It verifies the
canonical staged file manifest/hashes, checks that the game is still closed,
installs and verifies installed hashes while retaining protected settings/other
mods. This is an update tool, not a general first-install wizard. As explicitly
requested, it makes **no backup**; its receipt records `backupCreated: false`
and prior file hashes only. Twelve old S060 installation-backup directories and
five independent source/bundle/restore copies were removed; removal receipts are
under `build/cleanup-0.15.0`. The canonical repository/Git history, current install
and canonical builds were retained. The handover records actual installation;
this source update alone does not establish that 0.15.0 is installed.

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
