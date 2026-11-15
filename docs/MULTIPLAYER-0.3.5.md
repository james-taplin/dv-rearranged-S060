# S060 0.3.5-mp.1 multiplayer port — staged 14 November 2026

James requested porting multiplayer support from the current Rearranged S282
into S060 0.3.x. The result is a staged candidate, not an installed release.
James performs live multiplayer and Derail Valley acceptance. No game was
launched, no profile/save was created, and nothing was installed or published.
The active Documents 0.4 work and canonical Git branch/index were not edited.

## Source and native decision

Target: `B:/GitHub repo/rearranged-s060-032-polish-20261113/source`, starting
from staged S060 0.3.4, checkpoint `9b2036814fa47566d48ae7c73febcceffb76bf8e`.
The directory's 032 name is historical. The original S060 fork predates S282's
multiplayer integration; subsequent geometry/physics ports did not include it.

Donor: `B:/GitHub repo/rearranged-s282-multiplayer`, current 3.0.4-indev
working-tree implementation at inspection. Git HEAD was
`29b6f6a81bebc008036263697d4879bbdae262cc`, with existing uncommitted work.
Use the recorded donor source hashes in `build/multiplayer-0.3.5/validation.json`,
not the commit alone, to identify the imported implementation.

Reused the donor's optional API bridge and bounded packet codecs, host request
validation, matching-package handshake, guest permission controls, revisions,
epochs, late-join queues, recycled-peer guards, deletion tombstones and session
cleanup. S060 has no per-arrangement user settings to replicate: its fixed-size
selection is the complete arrangement contract. Stock and short-wheelbase 0-6-0
have separate wire IDs; existing save IDs/scale pairs remain unchanged.

The extra DLL follows the existing donor boundary: only the optional bridge
references MultiplayerAPI. The core loads it only when the Multiplayer mod is
active. Native Multiplayer continues to own transport, train movement and port
replication. No alternate transport, motion protocol or multiplayer framework
was introduced. Required API file hash is recorded in the build receipt; it is
API 1.1 from DV Multiplayer 0.1.16.0-beta, not shipped inside this package.

S060 and S282 have distinct mod IDs, bridge assembly names, packet namespaces
and native replication port prefixes. LiteNetLib 1.3.1 derives packet IDs from
the full type string; see its [primary implementation](https://raw.githubusercontent.com/RevenantX/LiteNetLib/1.3.1/LiteNetLib/Utils/NetPacketProcessor.cs).
This supports their separate packet identities; live coexistence remains in
James's checklist, not an accepted gameplay claim.

## S060 integration

- Radio and UMM requests use the host-authoritative path. Guest preferences
  remain local; automatic random/save geometry is deferred to host snapshots.
- Guest replay waits for native bogies, simulation and split geometry. It may
  restore a moving late-join car; operating conditions are checked on the host.
  Meyer replay does not repeat host-approved coupling/overlap clearance checks.
- Refusals that leave the same arrangement do not manufacture a new revision.
  A conversion failure which actually falls back to another layout publishes
  the recovered result instead of the failed request.
- Meyer/Dockside helper traction entities exist only on the authority. Guests
  read GUID/epoch/revision-qualified group slip/RPM ports for sparks and audio.
  Guest teardown and engine-mode changes preserve replicated port values.
- S060's stable engine-slot ports already drive articulated crank/effect
  presentation. Shay additionally needs its 43-turn transmission phase, which
  was private in 0.3.4. `GEARED_TURNS` now carries that phase through the native
  port stream; the guest visual driver consumes it without simulating steam.
- Stock, all eight converted choices, donor hiding, all three bundles, oil
  topology, engine calibration and saved engine schemas are retained.

## Validation and limits

Final build: zero warnings/errors. Core and bridge assembly versions are
0.3.5.0; manifest/package version is 0.3.5-mp.1. The staged core has no MPAPI
assembly reference; the optional bridge has the expected API reference.

Bounded offline checks: 3,500 assertions across these suites:

| Suite | Checks | Evidence |
|---|---:|---|
| Ported production bridge/packet code | 219 | `build/multiplayer-0.3.5/Bridge.json` |
| Production S060 sync and selection policy | 49 | `build/multiplayer-0.3.5/Core.json` |
| Production native port adapter schema/validation | 32 | `build/multiplayer-0.3.5/native-port-adapter.json` |
| Exact final DLL, native engines/ECS/ports/drive authority | 114 | `build/independent-slip-0.3.5-mp.1/native-20261114-173901-718/receipt.json` |
| Existing engine/save regression | 2,419 | `build/multiplayer-0.3.5/engine-regression.log` |
| Existing native oiling migration/mask regression | 628 | `build/multiplayer-0.3.5/oiling.log` |
| Production conversion rollback boundaries | 39 | `build/multiplayer-0.3.5/transactions.log` |

The donor suite first passed 413 checks. The preserved 0.3.4 DLL passed 101
native-drive baseline checks with the adapted fixture. These baseline counts
are excluded from the 3,500 candidate total.

Bridge/core tests substitute managed Unity, API and conversion boundaries.
The port schema suite substitutes native port definitions. The stronger native
suite loads the exact final DLL after native ECS registration in the existing
isolated Unity fixture player. It uses real native engines, ports, helper ECS
systems and authority paths; the inactive car and intercepted final rail-force
boundary are not a running game. No live network, actual rail dynamics, audible
sound, saved game world or real multi-peer session was tested.

The engine/oiling/rollback source suites preceded the last two core/support
review corrections. Their production inputs are unchanged, so their evidence
is retained with that provenance. The final DLL native suite and core/bridge/
port suites ran after those corrections. Existing 0.3.4 geometry/bundle evidence
applies to the unchanged asset bytes only; it is not relabelled as new gameplay
acceptance. No asset authoring build was performed.

Fixture corrections: sandboxed NuGet atomic file replacement failed on B:;
the identical commands succeeded outside the sandbox. The native fixture
needed a real port registry/GUID for the new replication contract. Its later
Meyer guard case exposed the fixture's absent two-slot coupler array; only the
fixture was repaired, and the same production DLL then passed. Failed evidence
remains under the timestamped native run. No production change masked it.

## Delivery and continuation

ZIP: `build/multiplayer-0.3.5/package/RearrangedS060-0.3.5-mp.1-UMM.zip`.
Also copied to the Documents project `outputs` folder. It contains 14 verified
entries, one UMM mod root, both S060 DLLs, and the original 0.3.4 asset bytes.
No MultiplayerAPI DLL, settings, game DLLs or development fixtures are packaged.
SHA256: `DDCB502C6E79EEEDA6F316133DF0194D089371E8F24AE82008D000AB012EA133`.

Build with `Build.Multiplayer.ps1`; run policy/transport-boundary checks with
`Test.Multiplayer.ps1`. `Package.Multiplayer.ps1` verifies the fixed 0.3.4 seed
hash and build receipt, then compares every ZIP entry to its staged bytes.
Use a new output directory when repackaging to preserve existing evidence.
The native player reuse command is recorded in `validation.json`.

Current source remains in this release export folder, with a review patch and
source hashes recorded alongside validation. No new Git commit/push was made.
Do not merge this into 0.4 implicitly: that is separate ongoing development.

Next acceptance belongs to James, using `MULTIPLAYER_TESTING.txt`: matching
host/guest builds, permissions, all nine selections, moving late join, separate
Meyer/Dockside slip, Shay transmission/audio, save/reconnect and optional S282
coexistence. Installation requires separate authorization.
