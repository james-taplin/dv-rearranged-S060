Installed on 16 November 2026 after James explicitly requested installation and
confirmed DV closed. All 14 payload files and preserved settings were verified.
The installation-pending text below is historical. No game launch or backup.

# Updated 0.3.6-mp release - 16 November 2026

The release now includes `00b129e`, the native heated-brake repair for 0-10-0,
0-8-0 and short-wheelbase 0-6-0. It retains the approved Shay/Meyer pipe sweeps,
0-4-0 middle-brake repair, 0.3.5 fixes and optional multiplayer integration.
Version remains 0.3.6-mp. Read
[CONVENTIONAL-BRAKES-0.3.6-mp.md](CONVENTIONAL-BRAKES-0.3.6-mp.md) for the cause,
implementation, native-contact baseline and exact validation limits.

James requested release/docs updates and installation. The new ZIP passed its
checks, but DV started before installation. The guarded installer refused all
game writes. James then explicitly selected **leave installation pending**.
Do not install automatically when the game closes; await a new request.

## Artifact and provenance

Canonical ZIP:
`build/release-0.3.6-mp-conventional-brakes-20261116/package/RearrangedS060-0.3.6-mp-UMM.zip`.
SHA256: `ADD909DEEDF9EEB2DBD4849DD7AC4D6DEEE42863D560A823C8BD33C8A1A51B63`.

This packages the exact tested core DLL, SHA256
`4379CFCE72ED1203B4D304205EA5A722BC612566F595499423BE2780148C4ED1`.
The 00b129e implementation already built with zero warnings/errors and passed
300 bounded native brake checks, 45 conversion-boundary checks and 49 managed
multiplayer-core checks. This release pass verifies the recorded build sources
and binary hashes and reuses that DLL; no unchanged production rebuild.

The existing optional bridge DLL is retained, SHA256
`41D8586309097BF7C03031A172E213AC5E15C58B56D9178BD3685966D94AE042`.
Its source/project hashes match its build receipt. This core change adds brake
presentation and does not change the bridge API or network schema. The matching
package check still requires every peer to use the identical complete ZIP.

All three asset bundles match the verified 0.3.5 seed and retained Unity reopening
proof. No native exports, private renders, game assemblies or API DLLs are shipped.
No backup or rollback copy was created; the existing built ZIPs are reinstall
sources. The prior release and its receipts remain intact.

## Release validation and documents

- The 14 ZIP entries were read back and compared with their staged hashes.
- Actual UMM fresh installation, update and migration passed 66 checks in an
  isolated fixture directory. No gameplay session was created.
- Core build inputs, retained bridge inputs and all three asset hashes match
  their recorded evidence. The final native brake previews and test reports
  remain under `build/conventional-brakes-20261116`.
- The packaged changelog and README describe fitted conventional brakes and
  retained native heating/material/LOD behavior. The player checklist now covers
  both sides, braking, heat/cooling, stock restoration and save/reload.

The 300 brake checks use native methods and rest-pose geometry. Eighteen existing
caliper/own-wheel contacts reproduce with the original profiles; no new rest-pose
contact pairs were found. This is not a full animation or gameplay-clearance
acceptance claim. Earlier articulation evidence applies to unchanged pipe and
Shay geometry only. James owns driving, actual heat development/cooling,
arrangement changes, save/reload and live peer acceptance.

## Installation state

Installation is pending by James's explicit choice. The live runtime remains
`E3959B44D4324AFEBEFCC8C2BA23C0A8FD534043952861EA3466D68C5AB6E252`,
the preceding pipe/0-4-0 release. Its version label is also 0.3.6-mp; use the hash
to distinguish it from this new conventional-brake package.

No installation, DV launch, push or publication occurred. The latest package
and evidence are in `build/release-0.3.6-mp-conventional-brakes-20261116`.
