# 0.3.4 donor visibility — staged only

James requested hiding both CCL donor entries while retaining the Meyer,
Dockside and Shay radio arrangements, then building, validating, committing
and installing 0.3.4. He subsequently rescinded installation: **stage only**.
No installation is to be performed by this task. No DV launch,
gameplay testing, push or publication is authorised by this work.
The separate Documents 0.4 source is outside the task.

The live runtime was verified as 0.3.3 at the start. During final inspection it
had changed to 0.4.4-indev, with a different DLL hash and added DVCarChanger
requirements. This task did not run a real installer; all installer fixtures
use explicit private fake-game paths. Leave the concurrent 0.4 installation
alone. Settings.xml still matches the initial hash. Final validation records
the observed live version and hash, rather than claiming 0.3.3 was retained.

Source: `B:/GitHub repo/rearranged-s060-032-polish-20261113/source`.
Parent: 058e74c15b2f38164623a71c959e227e57c8e73d, the clean 0.3.3 release.
The export directory name is historical; source and package metadata are 0.3.4.
Use an isolated Git index and a new local checkpoint ref. Do not change the
canonical active branch, index or its concurrent prototype source.

## Native decision

Installed B99 CommsRadioCarSpawner.UpdateCarLiveriesToSpawn excludes a livery
when TrainCarLivery.isHidden is true. DVObjectModel's livery-ID dictionary keeps
hidden liveries. S060Articulated.ResolveFrames uses TryGetLivery by the original
ID and the registered prefab; it does not use the spawner's visible list.
Shay also uses Dockside preflight as its cylinder-effects source.

Installed CCL 3.1.11's CustomCarVariant does not expose isHidden. Its existing
LocoSpawnGroups and work-train settings serve different spawning paths. Both
release donor bundles already have empty LocoSpawnGroups and work trains off.
No supported bundle-only hide switch was found in the installed implementation
or the corresponding CCL source. Do not remove the donors from the registry,
disable their bundles, change their IDs or build a replacement spawner.

S060DonorVisibility adds one Harmony prefix to the native list-building method.
Before the game caches choices, it sets isHidden only on RS060_Meyer_0440 and
RS060_Franklin_Dockside_0440. Missing donors or an unavailable types registry are
left alone. Main.Load registers this patch with the existing Harmony instance.
The native registry, prefabs, save identities and all three bundles are retained.
No new DLL, dependency, per-frame service or asset rebuild is required.

Production source comparison against 0.3.3 found 87 existing C# files unchanged.
The sole changed existing C# file is S060Main.cs, with one registration line.
The other runtime source addition is the small visibility patch itself.
Info.json and assembly version advance to 0.3.4; other metadata is retained.

## Focused validation

The production DLL built once with zero warnings and zero errors.
DonorVisibilityNativeAudit first demonstrated the old 0.3.3 visibility in
7 checks, then passed 32 checks against the 0.3.4 DLL. It exercises:

- The real native spawner list builder with the production Harmony prefix.
- Both hidden donors remaining addressable by their original IDs and prefabs.
- Actual Meyer and Dockside Preflight and all three production radio selections.
- Shay's retained Dockside effects-source frame, model, transmission and sampled audio.
- Absent and late donors, repeated list construction, stock S060, an unrelated
  locomotive with a similar ID, existing hidden entries and an unavailable registry.

The fixture seeds native livery records with exact bundled prefabs. It suppresses
only destination-display reset, which requires a game world. This is not CCL
world-import or gameplay acceptance. The native fixtures do not establish audio
playback quality, rail dynamics or saved-world acceptance.

Fixture setup was corrected before relying on it: the native spawner type is
DV.CommsRadioCarSpawner, and donor bundles must be loaded sequentially with
Unload(false), as CCL does, because they share internal bundle filenames.
The initial failed fixture runs remain private evidence; no production change
was made for those setup failures. The independent-drive baseline path was
resolved to its canonical 0.3.0 package; its hash matches the prior baseline.
The proven private drive player passed 101 checks against the 0.3.4 DLL.

Private evidence, artifact hashes and the final local commit are recorded in
`build/release-0.3.4/validation.json` and `source-checkpoint.json`.
Public documents retain the plain ASD-STE100 style and full release history.
Testing notes and source provenance belong here, not in the release ZIP.

James's in-game acceptance, when he chooses to install: verify the two donor
entries are absent from the standard spawner, then use WHEEL REARRANGER on an
ordinary S060 to select Meyer, Dockside and Shay. Check Shay's effects and sound.
Do not launch or automate DV to perform those checks.

## Completed offline release checks

All 16 native suites passed against the 0.3.4 target: 28,602 checks.
The existing Meyer pipe geometry sweep first reached a 300-second timeout
with no assertion failure, then passed its 8,859 checks under a 600-second
limit. The two earlier successful exact-DLL reports were retained by hash;
only the remaining suites were run in the second sequence. No production
rebuild or asset change was made for this timeout.

Shay audio passed 6,037 native checks. Native UMM extraction passed 60 checks;
all three extracted bundles reopened with 39 checks. Fake-game installer
fixtures passed 19 and 24 checks. Finally, the 32 donor-visibility checks were
repeated against the actual UMM-extracted DLL and assets, including both radio
preflights, all three radio selections and Shay's assets and effects source.

The ZIP contains exactly 12 files, including plain instructions, full history
through 0.3.4, credits and unchanged licence. Metadata differs from 0.3.3 only
in Version. The three bundle hashes are identical to the 0.3.3 package. The
final inspector was corrected to normalize Windows line endings in its history
check; the ZIP and runtime did not need changes.

Runtime SHA256: 66DD0F9AC1174065E7D10C4055BDA779043DF30124B800B279E99B5D05FF0666

ZIP SHA256: F81DAEC9CCA093B86F994B139F74A0AF9AE780B034D6B7C92B002D564D9F9FBB

Delivery: C:\Users\james\Documents\ChatGPT\REARRANGED S060\outputs\RearrangedS060-0.3.4-UMM.zip

Status: built, offline-validated, locally committed and staged only. No real
installation, game launch, gameplay testing, push or publication from this task.
The independently changed live 0.4.4-indev installation was left alone.
