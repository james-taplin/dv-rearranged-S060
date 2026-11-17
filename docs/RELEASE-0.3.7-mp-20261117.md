# Installed 0.3.7-mp release — 17 November 2026

James asked for the staged Meyer wheel-slip effects and coupled-buffer repair
to be rebuilt as **0.3.7-mp** and installed. This release is that repair with a
new version label. It carries forward everything in installed 0.3.6-mp: native
rods and oil cups, native heated brakes, pipe sweeps, 0-4-0 repairs, the 0.3.5
fixes and optional multiplayer. James owns gameplay and live peer acceptance.

Read [the Meyer repair and its validation limits](MEYER-EFFECTS-BUFFERS-0.3.6-mp.md).

## What changed from the staged repair

Version metadata only: `Info.json`, both project `<Version>` values, the
package changelog heading and the player checklist title/peer note. The build
receipt shows every runtime/adapter `.cs` source is byte-identical to the
validated Meyer repair source (`1ebdbb8`); only the two `.csproj` files differ.
DLL file versions are 0.3.7.0; product versions are 0.3.7-mp.
The packager's default output now points at `build/release-0.3.7-mp-20261117`.

## Package

- ZIP: `build/release-0.3.7-mp-20261117/package/RearrangedS060-0.3.7-mp-UMM.zip`
- ZIP SHA256: `0B01AA86AB5EBF61C72FF4521FC54D426FB96CC77AC3706F0157A09C6FA630B8`
- Core SHA256: `4D2A20B9932FD6CDC3D2B3C564F3683E6800D5FB5AF17E110E291046EE7A17EA`
- Adapter SHA256: `642A3CE5416CE859B5266953A8474FB91C62FF1263782B1F4F21837B5EF473F5`

All 14 archive entries were read back against staging. The Dockside, Meyer and
Shay bundles are byte-identical to 0.3.6-mp (`FD37566F…`, `6EDE55EE…`,
`73F7BEBE…`).

## Validation on this build

- One production build: zero warnings, zero errors.
- Native UMM fresh install, repeated update, uninstall and migration from the
  genuine 0.3.0 three-mod archive: **66 assertions pass** on the exact ZIP,
  `nexus/umm-proof-20261117-115201/native-umm-validation.json`.
- Managed multiplayer: bridge 219, core 49, native ports 32 checks pass
  (`build/release-0.3.7-mp-20261117/multiplayer`). These compile the fixtures
  from source with managed doubles; they do not certify live peers.

Retained, not rerun: the Unity native fixtures for spark placement (162),
rear coupling (280) and lamp/buffer geometry (7,596) ran on the 0.3.6-mp
labelled core `6A7732FD…`, built from identical runtime sources. Bundle
reopening evidence is retained from 0.3.6-mp because the bundles are unchanged
(`nexus/retained-bundle-evidence.json`). The known synthetic-rod
CleanupNativeAudit fixture failure is unchanged and still not claimed as a pass.

## Installation

James authorized installation. Derail Valley was not running. The guarded
installer installed all 14 payload files, verified every installed hash and
confirmed `Settings.xml` and unrelated mod manifests kept their hashes. No
obsolete or legacy files were removed. Receipt:
`build/release-0.3.7-mp-20261117/nexus/install-receipt-latest.json`.

Installed `Mods/RearrangedS060/Info.json` reads 0.3.7-mp; installed core is
`4D2A20B9…`. Previous installed core `1FADD847…` (0.3.6-mp rods/cups) remains
reinstallable from `build/release-0.3.6-mp-oil-rods-20261116`.

No game launch, backup, push or publication. James should check Meyer sparks on
both powered groups, both directions and curves; rear buffer contact with a
coupled carriage under compression; loose-chain slack; uncoupling; coupled
save/reload; and live peers on identical 0.3.7-mp packages
(`MULTIPLAYER_TESTING.txt`).
