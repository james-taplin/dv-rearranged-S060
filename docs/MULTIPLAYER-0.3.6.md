# S060 0.3.6-mp — installed 15 November 2026

James requested rolling `f835781` into the multiplayer release and installing it.
The existing multiplayer export was reused. The canonical dirty checkout,
the separate 0.4 source and the 0.3.5 repair worktree were not edited.

## Integrated repairs

Includes `f835781bd0b300ec4729e11bc3fa499e85b76282` and its prerequisite buffer
hotfix `f35266f702bb5de1b243556cb351873cb44b9859`. Read
[the repair record](REPAIR-0.3.5.md) and [buffer record](BUFFER-HOTFIX-0.3.5.md).
The optional multiplayer implementation remains the port documented in
[MULTIPLAYER-0.3.5.md](MULTIPLAYER-0.3.5.md).

- Native buffer controller, scanner and anchors move together; hook, chain and
  hose retain their corrected pocket positions.
- Axles are identified in their authored support frame, independent of live
  bogie displacement or yaw.
- Bogie restoration is preflighted. Track placement permits endpoints,
  interpolates native samples and rejects tracks farther than half a metre.
- Closed-regulator static cylinder torque no longer blocks a stationary change.
  Host speed, regulator, slipping, sliding, damage and coupling gates remain.
- Dockside uses the verified 0.3.5 baked sleeves and body-fixed manifold caps.
  Runtime code neither regenerates nor destroys shared sleeve meshes.
- Host/client authority, permissions, late-join state, independent traction
  replication and Shay phase replication remain intact. No new dependency.

Four repaired runtime files match f835781 exactly after line-ending
normalisation. Conversion/support differ only by the five existing multiplayer
authority/replay guards; see the recorded integration patch. Other runtime
sources match the previous multiplayer build except version metadata. Review
and bounded offline checks found no new production issue; this is not a claim
of live multiplayer or gameplay acceptance.

## Build and validation

One production build: zero warnings and zero errors. Evidence is in
`build/multiplayer-0.3.6/validation.json`, with exact DLL hashes and report paths.

- Managed multiplayer: 300 checks; transaction/rollback: 45 across 20 scenarios.
- Exact DLL isolated native traction/authority: 114; axle/track placement: 61.
- Baked sleeves: 4,623; native buffer compression: 248; lamp/stem/LOD fixture:
  7,596; pipe recovery: 1,118; cleanup: 145; donor/radio preflight: 32.
- Existing engine/save: 2,419; oiling/migration: 628.
- Final ZIP native UMM: 66; bundle reopening: 39; guarded installer fixture: 23.

The first buffer run used an obsolete MeyerLightsAudit helper already present
in the Unity test project. Test.Runtime.ps1 now copies the current helper from
the repair commit. The corrected buffer/light checks passed. Production did
not change and was not rebuilt. The installer fixture now checks optional
bridge presence against its package instead of assuming every release omits it.

The final documentation-only repackage updated the player checklist to mention
the 0.3.5 repairs. Native UMM was rerun on that exact ZIP. Its three asset hashes
match the already reopened extraction, so that bundle evidence was retained.
No asset rebuild was necessary. Existing 0.3.5 mesh-delta and timing evidence
describes those identical assets; it is not a new gameplay performance result.

## Installation and handoff

Installed 14 verified package files in the existing `Mods/RearrangedS060`.
Settings and unrelated mod manifests retained their hashes. Obsolete UMM cache
was removed by the existing guarded installer. No backup, game launch, profile,
save, push or publication. Installation receipt:
`build/nexus-0.3.6-mp-single/install-receipt-latest.json`.

Final ZIP: `build/multiplayer-0.3.6/package-final/RearrangedS060-0.3.6-mp-UMM.zip`,
also copied to the Documents workspace's `outputs` folder. Both peers need this
identical package. API target remains DV Multiplayer 0.1.16.0-beta / API 1.1;
Multiplayer is optional for single-player use and its API DLL is not bundled.

James owns game testing. Follow MULTIPLAYER_TESTING.txt, particularly fresh
Meyer or Shay -> stop/close regulator -> Dockside -> drive; coupled buffer
compression; matching host/guest selections, permissions and moving late join.
Actual transport, rail dynamics, audible sound and saved-world acceptance
remain unverified. Audio work remains paused; no separate 0.4 features added.
