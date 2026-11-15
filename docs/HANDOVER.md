## Final 0.3.6-mp installed - 15 November 2026

James explicitly authorized installing the staged final package after commit
5d81079. This supersedes the stage-only restriction below. Installed the exact
ZIP with SHA256 6FA55A03422B12DC68FECD774F040CC210F5D188DF041C9B4C2CE951F6B02A2B.
All 14 installed files match the validated payload. Settings and unrelated mod
manifests retain their hashes. Runtime SHA256 is
E3959B44D4324AFEBEFCC8C2BA23C0A8FD534043952861EA3466D68C5AB6E252.
The guarded installer accepts an explicit PackageDirectory so it selects the
final stage and retains previous package receipts. All three bundled assets
match the retained Unity reopening evidence. No rebuild or DV launch.
Installation receipt: build/release-0.3.6-mp-final/nexus/install-receipt-latest.json.
James owns the next gameplay test of the pipe sweeps and 0-4-0 brakes.

---
## Final 0.3.6-mp pipe and 0-4-0 stage — 15 November 2026

James approved the pipe renders and authorized full articulation checks, commit
and stage. He also requested removing the floating 0-4-0 middle brake gear.
Read [FINAL-STAGE-0.3.6-mp.md](FINAL-STAGE-0.3.6-mp.md). The calipers were
already hidden; the separate six-shoe native mesh kept the middle pair. The
repair removes that pair in an owned copy and restores the exact original on
cleanup. Full results and source checkpoint are in build/release-0.3.6-mp-final.
Do not install this stage without separate authorization. No DV game test.

---
## Pipe sweep preview — STAGED ONLY, 15 November 2026

James requested shallower Shay smokebox-entry and Meyer rear upper cab-pipe
sweeps, with renders before committing. He then explicitly said stage only.
Read [PIPE-SWEEPS-0.3.6-mp.md](PIPE-SWEEPS-0.3.6-mp.md). The two production
geometry changes and preview fixtures are uncommitted. One clean build,
focused native clearance checks and actual before/after Unity renders are in
build/pipe-sweeps-0.3.6-mp. No commit, installation or DV launch. The installed
release remains unchanged. Await James's visual review; do not treat this as a
new released package or full articulation/gameplay acceptance.

---
## Approved old-source cleanup — 15 November 2026

James approved all 12 old working folders and all 14 loose release files.
Fresh SHA256 checks verified all 41,152 source/workspace files plus the 14
loose files against their preserved copies or documented pre-relocation backups.
The latest Documents Git metadata and old shared-worktree metadata are preserved
under `_consolidation/2026-11-15/at-removal`.

All 14 loose files and 11 old folders were deleted. The old Documents workspace
`C:\Users\james\Documents\ChatGPT\REARRANGED S060` contains no files or folders;
Windows refused removal of its empty outer directory because another process
has it open. No delayed deletion is queued. The old linked worktree was removed
through native Git; its branch/history remains. All eight consolidated junctions
resolve and all three source repositories pass Git connectivity checks.
Read `_consolidation/2026-11-15/removal-receipt.json` for exact paths and status.
The earlier pending-approval consolidation entry below is historical.

---
## Source consolidation — 15 November 2026

Canonical home: `B:\GitHub repo\rearranged-s060`. Read its `START HERE.md`.
Current 0.3.6-mp source: `current-0.3.x/source`; plain 0.3.5:
`source-0.3.5`; separate 0.4 development: `development-0.4/v0.4-work`.
The older source at the outer repository root is preserved history.

All 12 discovered old working folders are copied with SHA256 verification,
including ignored/untracked source, Git metadata, authoring, build evidence
and shared staging. Eight junctions are recreated; six operational scripts
have path-only updates. The historical chart output dependency now points to
the currently installed Codex runtime because its old runtime target was gone.
Historical receipts keep their original paths. No production build, install,
DV launch, push or publication was performed. James reports 0.3.x working great;
0.4 and untested multiplayer scenarios are not newly certified by this move.

Originals remain pending James's explicit approval of
`_consolidation/2026-11-15/deletion-list.md`. Reverify originals before removal;
do not follow junctions into shared targets or queue delayed removal.
The Documents original Git metadata is also sealed in the audit ZIP.
Current 0.3.6 snapshot retained its as-found packaging/document differences
from checkpoint 6d6a7cc; runtime/authoring comparison with end-of-line whitespace
ignored found no differences. Use its specialised multiplayer packager and
retained release evidence; the pre-existing plain packager gate is unchanged.

---
# Rearranged S060 — handover

## 0.3.6-mp — installed 15 November 2026

James requested integrating f835781 into the multiplayer release and installing.
Completed with prerequisite f35266f buffer repairs and the verified baked-sleeve
Dockside bundle. Read [MULTIPLAYER-0.3.6.md](MULTIPLAYER-0.3.6.md) for the code
review, exact build/test/package evidence, fixture correction and installation.
One production build, zero warnings/errors; offline regression checks passed.
Settings preserved; no DV launch. Live multiplayer/game acceptance belongs to
James. Audio work remains paused and separate 0.4 work remains untouched.

## 0.3.5-mp.1 multiplayer port — STAGED ONLY, 14 November 2026

Ported the current S282 3.0.4-indev optional multiplayer integration onto this
0.3.4 release source. Read [MULTIPLAYER-0.3.5.md](MULTIPLAYER-0.3.5.md) for
source provenance, S060 adaptation, exact validation and remaining acceptance.
The candidate adds host-authoritative radio/UMM requests, permissions, late-join
state, host-only independent traction and native guest presentation, including
Shay's multi-turn gear clock. Existing assets and saved identities are retained.
Final core/bridge build has zero warnings/errors; 3,500 bounded offline checks
pass. The 14-entry UMM ZIP is staged in build/multiplayer-0.3.5/package and the
Documents project outputs folder. No install, game launch, publication or Git
commit/push. Source remains here; separate 0.4 development is untouched by this
task. James owns live peer/gameplay acceptance. Do not treat offline fixtures
as multiplayer acceptance or install the candidate without separate consent.

## 0.3.4 donor visibility — STAGED ONLY, 14 November 2026

James requested hiding the Meyer and Dockside asset donors while keeping the
radio arrangements and Shay working. He then rescinded installation: **stage
only**. Do not install this candidate. The initial 0.3.3 installation was later
replaced by 0.4.4-indev during the run; no real installation was performed by
this task. Leave that live 0.4 installation and its settings alone. Read
[DONOR-VISIBILITY-0.3.4.md](DONOR-VISIBILITY-0.3.4.md) for the native flag,
the narrow existing-runtime change, focused native checks and evidence paths.

The patch sets the two native livery hidden flags before the standard spawner
caches choices. Donor registration, IDs, prefabs and all three bundles remain.
Use build/release-0.3.4/validation.json for the final staged ZIP evidence and
source-checkpoint.json for its isolated local commit. No DV launch, installation,
push or publication. Separate 0.4 work is outside this change. James owns game
acceptance; a generic validation request does not authorise gameplay.

## 0.3.3 clean release — INSTALLED, 14 November 2026

James requested a clean Nexus UMM ZIP, a code review, ASD-STE100-style public
documents and installation of that exact package. This new authorisation
supersedes the earlier installation rescission below. Read
[RELEASE-0.3.3.md](RELEASE-0.3.3.md) for the confirmed scaled 0-4-0 cleanup repair,
exact offline evidence, package hashes and the installed state.

The release ZIP contains 12 files and a full concise changelog through 0.3.3.
Public documents are plain text without Git references, BBCode or testing notes.
Installed bytes match the verified ZIP; settings preserved, no backup created.
Separate Documents 0.4 work and the canonical active branch/index are untouched.
No DV launch, gameplay testing, push or publication. James owns game acceptance.

## Earlier 0.3.3 integration — staged checkpoint, 14 November 2026

James requested combining a008bee, 08c8500 and 5c12c1d into 0.3.3, then
explicitly rescinded installation: **commit and stage only**. Do not install
this candidate without new authorisation. The repaired installed 0.3.2 stays.
Read [INTEGRATION-0.3.3.md](INTEGRATION-0.3.3.md) for source provenance, exact
validation and package evidence. Separate Documents 0.4 work was not modified.
No DV launch, game testing, push or publication. James owns gameplay acceptance.

The existing 0.3.2 export checkout is reused for this release, so its directory
name still contains 032. Current source/version is 0.3.3; historical 0.3.2
packages and reports remain separately named. This integration inherits the
approved rear-drop Meyer route from 08c8500, superseding the earlier upper-line
fallback described in the historical sections below.

## 0.3.2 Shay/Dockside/Meyer polish — commit only, 13 November 2026

Read [GEARED-POLISH-0.3.2.md](GEARED-POLISH-0.3.2.md). Shay has circular pipe
elbows and collared stays clear of the measured native information board.
Dockside reuses the proven native return-crank removal and restores the actual
Franklin gearbox/shaft vertex data lost by the old streamed-mesh copy. All 17
custom source meshes now reopen with exact original positions. Runtime/version
remain 0.3.2. James's follow-up moves the Meyer rear return down earlier beside
the bunker, through the native steps as brackets, then back into the unchanged
manifold. This follow-up is amended into the same commit; the corrected lower
bend and updated native geometry evidence are documented in that handover.
Source/build evidence only; no installation, push or publication.
The installed detail revision below remains installed. James owns DV testing.

## Current 0.3.2 detail revision — INSTALLED, 13 November 2026

James requested the tender foothold texture smears and apparent missing
smokebox door be addressed and installed in 0.3.2. Read
[DETAIL-REPAIRS-0.3.2.md](DETAIL-REPAIRS-0.3.2.md). The tank/hopper now map to
clean native paint. The door was omitted from the offline exporter; the real
separate native door is now included in previews and checked for preservation.
No production door replacement or hook was needed.

Installed at 21:34 GMT with Install.Nexus.ps1, no backup, settings preserved.
All 11 payload files match the package. DLL SHA256:
13B4278E275096B1741818734F7E2EAB481B1781FAD48E16F5BBBB6AD49B3777.
Info.json is byte-identical to the earlier 0.3.2 candidate. All prior 0.3.2
repairs remain. Native suites: 16 / 28,651 checks; private ECS: 101; native UMM:
57; extracted bundle reopening: 38; installer fixtures: 18 and 24.
Evidence and receipt: build/details-0.3.2/validation.json and install-receipt.json.
No DV launch, game testing, push or publication. James tests in game. Concurrent
Documents 0.4 work was not edited or reverted.

## Earlier 0.3.2 visual repair candidate — superseded staging checkpoint

Read [VISUAL-REPAIRS-0.3.2.md](VISUAL-REPAIRS-0.3.2.md). This starts from the
installed repaired 0.3.1 source, not Documents 0.4. James authorised the previous
smooth upper Meyer return as fallback; it is restored exactly after lower-route
fitting contacts. Keep the pinned drawgear/open upper gap and rear coupler fix.
Shay custom finishes now retain ownership across paint/material replacement.
The loose fixed curved links and mountings are retired on short-wheelbase,
Dockside, 0-8-0 and 0-10-0, with exact stock restoration.

Source: B:/GitHub repo/rearranged-s060-032-polish-20261113/source.
Evidence: build/polish-0.3.2/validation.json. Version/assembly become 0.3.2;
other metadata, save identities and bundles are retained. The installed 0.3.1
below remains the installed build. No install, DV launch, push or publication.
James performs in-game testing. Leave the active Documents 0.4 work alone.

## Current 0.3.1 correction — heavy link and pin, INSTALLED

James explicitly authorised installation of this repaired build without a
backup on 13 November 2026. Installed at 12:46 GMT using Install.Nexus.ps1;
all 11 payload files were hash-verified, original metadata and Settings.xml
preserved, and no backup created. Installed DLL SHA256 is
BBA33D317AE8A46BE7B0C36DAD4C8C9908C93630BEA17580F97CF7880EF5269A.
All 7,413 Documents 0.4 files match their immediate pre-install hashes, with
no additions. No DV launch or gameplay testing. James will do in-game testing.
Installation evidence: build/link-pin-0.3.1/install-20261113-124647/validation.json.

Read [MEYER-LINK-PIN-0.3.1.md](MEYER-LINK-PIN-0.3.1.md) first. James rejected
the footplate bridge: there is no foot access there. Cut the plate, lips and
projecting upper bearers back to the tank edge. A heavy centre drawbar with
retained vertical pins, fork ends and short sliding drawgear underneath is the
visible connection. This supersedes the continuous plate/bearer acceptance
below. Do not revive that bridge as a structural member.

Work is isolated at B:/GitHub repo/rearranged-s060-031-link-pin-20261113/source.
This revision replaces the previous repair after James's separate installation
authorisation. Keep original 0.3.1 metadata and bundles. Do not edit the
canonical prototype checkout or Documents 0.4.
Final evidence: build/link-pin-0.3.1/validation.json. DV testing belongs to James.

## 0.3.1 Meyer repairs — 13 November 2026, STAGED ONLY

James authorised the three reported Meyer repairs, unchanged 0.3.1 metadata,
offline build/validation and staging only. Source is isolated at
B:/GitHub repo/rearranged-s060-031-repairs-20261113/source, exported from the
preserved 0.3.1 commit c982c8824ad74ca21f503abeb525317b33ce6f1c. The canonical
checkout's installed Car Changer prototype work and the Documents 0.4 work
were not edited. No game launch, real installation, push or publication.
All 51 installed prototype files match the starting snapshot, with no additions.

Repairs: update/restore the native chain interaction's cached local position
with the moved rear visual; extend both fixed bearers and the running plate
(including its collision/walking/item copies) into the native cab frame at
Z -3.92; replace the roof return with a 190 mm low LP return, a local dip below
the moving HP admission bend, native plate hangers and a short smokebox rise.
Original supports, steam circuit, oil/saves, capacity and independent drive
remain. No new hook, DLL, dependency, save identity or bundle export.

Final DLL SHA256:
4F09184300863A26018BE77317A452575A83789F0F7A3FDB344CC9793E212EDA.
Stage: build/nexus-0.3.1-single/RearrangedS060-0.3.1-UMM.zip.
ZIP SHA256:
8AAB0ED8F49ACA5A5027C976C37EAE0E015C1A2FD61DFCC05FFD80CC1862A36B.
Info.json is byte-identical to the original combined 0.3.1 ZIP:
61F1A136148BAF8E18B3DE7339EC0D5943F7B13F5597435989D16645DA943DD4.
All three donor/model bundles retain the original release hashes.

Validation: clean final runtime build, 0 warnings/errors. Sixteen exact-DLL
native suites pass 23,393 checks; focused coupling 96 and pipe/frame 3,666,
including all 27 existing articulation poses and actual frame/plate joins.
Original DLL reproduces the native visibility-enable reset; the repaired DLL
retains the hook through repeated native enable-entry calls and restores stock.
Physical plate factory reaches the same repaired front datum. HP/LP pipe
separation, original radii, cab clearance, native livery and disposal pass.
The private native ECS fixture passes 101 checks against the final DLL.

Managed/native engine and package gates: 2,419 engine-bank, 201 functional,
89 compound, 1,151 geometry/length/pipe, 628 oil migration, 162 diagnostic budget,
39 conversion-boundary checks across 15 scenarios, and the existing zero-byte
100,000-call axle cache check pass. Packaged engine smoke checks pass 18 Meyer
and 17 Dockside assertions. Native UMM extraction/update/uninstall/migration
57, extracted-bundle reopen 38, isolated combined installer 18, and legacy
installer 24 assertions pass. These are offline fixtures, not DV driving.
Semantic IL/fields of 356 existing types / 1,682 methods outside the three
repair owners match original 7A9AED64 exactly; 84 other production source files
match the preserved source. Unchanged simulation/save/calibration code is established by the independent
source and IL proofs; the plate collider extension is an intentional repair.

Evidence is under build/repair-0.3.1. The final native aggregate retains eleven
passing reports from runtime-audits-20261113-034027 and five from 034534, all
bound to the same final DLL. The first collision run lacked an editable donor;
its fixture now reads the exact released Meyer bundle, and the affected suite
passes without a production rebuild. Original failure evidence remains.

Fresh-project helpers were completed in Test.Runtime; private source-fitting
previews are labelled and rejected by Build.Radio. Native intersection tests
retain exact predicates/tolerances, bound selected faces instead of unused
vertices and reject impossible pairs earlier. Their original-release baseline
passes 3,394 checks while failing the five new repair assertions as expected.
The rear-wheel-fit script now fingerprints raw source bytes, preventing CRLF/LF
false mismatches; its fresh 27,864-pose result retains the original 21.5446 mm
beam clearance. Two production builds were made: initial integrated repair and
final geometry correction. Fixture/report/doc corrections did not rebuild it.

Detailed decisions, test limits and James's manual acceptance sequence are in
docs/MEYER-REPAIRS-0.3.1.md. Source preservation ref:
refs/heads/codex/meyer-repairs-0.3.1-20261113 (see source-checkpoint.json).
James owns actual teleport/LOD, coupling controls, curves, save/reload and stock
restoration acceptance after a separately authorised install. No QA game profile
or gameplay session was created; this is awaiting James's testing.

## 0.3.1 independent powered-group wheelslip — 12 November 2026, INSTALLED

James authorised porting independent wheelslip, Dockside first then Meyer,
using agents, preserving the compound/saves, offline tests and initially staging
only. After all checks passed he additionally requested installation. Installed
0.3.1 through Install.Nexus.ps1 after both host-side closed-game gates, no backup,
settings preserved. No DV launch, push or publication. The separate future 0.4.0
experiment is unchanged. Canonical source remains B:/GitHub repo/rearranged-s060.

Ported the audited S282 native ECS group-drive pattern into S060's DLL, using
existing native components and DVConvertToEntity/ECB lifetime. Each group owns
its engine-slot and selected-engine RPM inputs, radius and actual native bogie
contacts. Contact declarations are checked against the live powered-wheel
hierarchy. Forces are adhesion-limited per group and distributed once; original
native force is replaced only after a completed native sample. Invalid state,
derailment and teardown restore the native path. A partial bogie-force exception
cannot run pooled native force in that same step. No S282 runtime dependency.

Dockside declares rear/front groups; Meyer declares rear LP/front HP. Existing
native phase readers and inward-facing mapping remain responsible for motion,
cocks and chuffs. Native sparks bind all four replacement axles; audio maps
native bogie definitions to their actual new wheels. Original arrays/list and
RPM references restore. Switching waits for both powered groups to settle.
Shay remains one shaft-linked drive; conventional/stock traction is unchanged.
Native wheel-slide/braking and global cab slip indications remain car-wide.
Unused grip is no longer pooled, so uneven demand can reduce total pull.

Meyer cycle accepts separate HP/LP speeds without changing its finite receiver,
pressure backreaction or steam/work conservation. The equal-speed API remains.
Unequal-speed hot-save testing exposed constructor-cold HP exhaust temperature
being used before LP expansion preparation after load; restore that derived
receiver pressure/temperature before the first tick. No new save fields, IDs,
cylinder dimensions, chest/receiver volumes, boiler capacity or flow tune.
SavedState, EngineState, EngineMapping, OilingTopology and SteamCalibration
sources hash-match the pre-change 0.3.0 baseline.

Clean build, 0 warnings/errors. Final DLL SHA256:
7A9AED643B80A47D58A75CEFB1F4F62D64FC207D06D47878174176942389F1B7.
All 16 existing exact-DLL native suites pass 23,002 checks at
build/radio-0.3.1/runtime-audits-20261112-205642. Native group fixture passes
101 checks at build/independent-slip-0.3.1/native-20261112-205530-877, including
Dockside/Meyer asymmetry, phase/RPM, force-once, effects restoration, invalid
contacts, partial-force failure and destroyed DrivingForce/TrainCar cleanup.
Independent review found the destroyed-object registry cleanup gap; the final
candidate fixes it with managed identity checks. The first 9AFCA913 candidate
fails that regression as expected; its otherwise passing 16-suite reports and
the negative control are retained, not substituted for final-DLL proof.

Managed machinery/save checks: 2,443 (baseline 2,365), plus 89 pure compound
checks (baseline 40). Includes actual older Meyer, old 43:19 Shay and preceding
1.2 kg/s Shay active/dormant saves, seven unequal/stopped/opposed native Meyer
speed cases and exact hot continuation. Equal-speed force envelopes unchanged.
Evidence: build/independent-slip-0.3.1/meyer/validation.json.

Private native fixture uses the actual late-loaded candidate and native ECS,
engines, ports and effect components in a Unity release player. The car host is
inactive, helpers active, and final Bogie.ApplyForce intercepted for recording
and faults. It does not establish real rail dynamics, audible output, native
car startup, actual weather, frame time or gameplay acceptance. Fixture-only
corrections (Harmony native parameter name/helper-host activation) are retained
in failed evidence; production was not rebuilt for them. James owns DV testing.

Future arrangements: follow docs/INDEPENDENT-WHEELSLIP-0.3.1.md. Build.Radio now
requires exact-DLL independent-slip evidence alongside the existing suites;
Build.Release invokes the reusable Test.IndependentSlip runner. Original mesh
bundles are reused. Public 0.3.0 listing copy remains unchanged.

Final packaging passes 57 native UMM extraction/update/uninstall/migration
checks, 38 extracted-bundle reopening checks and 18 isolated installer checks.
The conversion-boundary fixture needed its new readiness test double; corrected
fixture now passes 39 checks/15 scenarios, including refusal while either set
is unsettled. No runtime rebuild was made for that fixture correction.
Before the later installation request, all eight installed runtime/Info/asset/
settings payload files checked were byte-identical to the pre-change baseline.
After authorised installation all 11 payload files match the 0.3.1 package;
Settings.xml is unchanged. Installed DLL is 7A9AED643B80A47D58A75CEFB1F4F62D64FC207D06D47878174176942389F1B7.
Install receipt: build/nexus-0.3.1-single/install-receipt-latest.json. Package
documents retain their original pre-install staging provenance; the install
receipt and this handover describe the later installation. James owns DV tests.

Stage: build/nexus-0.3.1-single/RearrangedS060-0.3.1-UMM.zip.
ZIP SHA256: 9B5FF35FC4870C1860E1B9607584639C14299731DE4A2B02E005625706B81F2F.
Final aggregate: build/independent-slip-0.3.1/validation.json. Local source
checkpoint details: build/independent-slip-0.3.1/source-checkpoint.json. The
snapshot includes the preceding integrated 0.3.0 source on a separate checkpoint
branch; active branch/index are preserved. No private references, game DLLs or
generated packages are selected into the source checkpoint. Not pushed.

## 0.3.0 public listing copy — 12 November 2026

Replaced docs/nexus/NEXUS_RELEASE_COPY.txt with current plain-text 0.3.0 copy,
removing BBCode, historical release bodies and internal checkpoint/test-build
headings. Added FILE_DESCRIPTION-0.3.0.txt (221 characters, under 250) and
CHANGELOG-0.3.0.txt in James's supplied lowercase, clipped style, treating
0.22.4 as the prior changelog entry. The matching loose files in
build/nexus-0.3.0-single are updated. Full text was supplied inline in chat.
No runtime, model, installed file or ZIP was changed; prior package/install
receipts retain their original document provenance. No push or publication.


## Integrated 0.3.0 — 12 November 2026, INSTALLED

James authorised integrating Meyer `8293d76`, Shay `db91158` and Dockside
`1b39170` with the staged physics, offline testing and installation. Canonical
source is B:/GitHub repo/rearranged-s060. Runtime and Info.json use 0.3.0.
Installed through Install.Nexus.ps1 after its two host-side closed-game checks;
all 11 package files were hash verified. No backup, DV launch or gameplay test.
Settings.xml remained byte-identical to the pre-integration baseline. James
owns the next driving, appearance, curve, servicing and save/reload acceptance.

The isolated, unfitted original-layout/six-axle experiment moved from Documents
`REARRANGED S060/v0.3-work` to `v0.4-work`, with 0.4.0 metadata and
WORK_0.4_START.md. Geometry and gates are unchanged; historical build evidence
keeps its original version. That future experiment is not in installed 0.3.0.

Applied the Meyer visual-integration patch for Articulated, Bodywork,
ExhaustPipe, InwardPipes and the new RunningGear owner. Manually merged the
inward-only SteamPipes branch while retaining Dockside's fitted sleeves. Meyer
has connected smooth rear LP piping, native heated shoes, supported leaf packs
and both underframes. Shay has its 0.80 m wheels, 86:20 gearing, visible coils,
brakes and compressor; Dockside retains the wider 4.70 m wheelbase and fitted
smokebox sleeves. The authored physics is unchanged from the staged baseline,
including the 1.6 kg/s Shay tune and finite-receiver Meyer starting/adhesion work.
Protected physics, save, oil and calibration source hashes still match baseline.

The combined native fixture found a real Shay compressor steam-service leg
crossing the main right admission feed at x1.30/y1.68/z3.17. Rerouted only that
short model leg to z3.02 and reseated its collar/stay. This is the sole model
correction to the frozen Shay checkpoint: no wheel, gearing, runtime, physics
or save change. New original-only bundle passed 11,322 engine/gear checks,
24,472 detail checks and the integrated native assembly test. No contact waiver.

Fixture-only corrections: cleanup now uses the exact native body rather than a
cube for roof saddles; the conventional body assertion recognizes the new
fixed bearers and moving rear frame while retaining the old upper envelope and
actual contact gates; NativeGeometry.csproj now includes S060SteamSleeve, used
by the Dockside work. These corrections did not change the tested runtime DLL.
Failed runs remain in their original evidence folders.

Validation: clean build (0 warnings/errors); all 16 exact-DLL native suites,
23,002 checks; 2,365 production engine/save checks including real old active
and dormant saves; oil migration, zero-allocation axle cache, transitions,
conversion boundaries, compound and measured geometry checks all pass. Native
UMM extraction/update/uninstall/migration: 57 checks; extracted bundle reopening:
38; isolated installer preservation/gating: 18. These are offline checks, not
gameplay acceptance. No simulation or save schema was changed for integration.

Final DLL SHA256: 614D97114270006530E68790536B1FC170CAB2EF32E6A85A7ED7E4853BF34419.
Shay bundle: C9443F2514B2F54D1BA15001980A786AB31669B267771D042F2CDB2D7E00767F.
Meyer 0.7.0 and Dockside 0.8.1 donor bundles retain their validated hashes.
Proof: build/integrated-0.3.0-20261112/validation.json and
build/radio-0.3.0/integrated-native-evidence/runtime-validation.json (aggregates
only passing exact-DLL reports with explicit run provenance).
Single-folder ZIP: build/nexus-0.3.0-single/RearrangedS060-0.3.0-UMM.zip.
Install receipt: build/nexus-0.3.0-single/install-receipt-latest.json.
Nothing was pushed or published; the pre-existing dirty checkout is preserved.


## Franklin Dockside 0.8.1 — wider wheelbase and Shay sleeves, 11 November 2026, STAGED ONLY

James revised the earlier decision: increase the wheelbase to reduce bogie
contact, and add the fitted Shay smokebox sleeves. Completed on the appearance
transfer; this supersedes the contact-limited 0.8.0 entry below. No installation
or DV launch. [Detailed handover](dockside/FRANKLIN-0.8.1.md).

Engine-frame centres ±1.75 m (3.50 m apart), axle centres -2.35/-1.15/+1.15/+2.35:
4.70 m driven wheelbase, 1.20 m intra-unit pitch. The fixture reads actual native
support pivots +.925/-1.40 and rotates the offset frames about those supports.
Final mechanical gap >=33.4479 mm across sampled native-pivot stresses; targeted
rod/crank/cylinder contacts 0/320. Existing native axle/support lifecycle retained.

Both native smokebox ports carry hollow rolled-lip sleeves with the Shay finish.
James rejected the first oversized circular cup: a clearance/envelope pass was
not evidence of a fitted joint. It is superseded by actual native-profile upper
lips and lower cuffs following the existing moving manifold. Both-side close-up
renders were checked. Independent profile distances stay within 0.71 mm through
sampled ±15-degree native support poses; default Shay geometry is bit-identical.
Build: 0 warnings/errors. Corrected exact-DLL checks: sleeves4,602/pipes1,118/
cleanup145/Shay-effects23 =5,888. Earlier clock/cock checks retain their original
provenance; no gameplay acceptance. Asset geometry/motion/oil/effect checks and
native engine17 pass. No simulator, save schema, oil identity or cylinder tune change.

Candidate: build/dockside-franklin-wheelbase-20261111/candidate/RearrangedS060.
Runtime0.24.0 0AAF86EE687473EF87665C695EA85AC6E86D34EB1773D0F5CEE0297A5B91D65F; Dockside0.8.1 bundle 5672D551FB02DF36143810B98774E4F9B7334CDE9B3F73C41BA1CB118D7253FF.
This carries the latest frozen Shay R3 running-gear candidate and preceding staged
Meyer/physics work; no whole-release acceptance is implied. James owns actual
curve/appearance/servicing/save-reload tests after separately authorized installation.
Final ZIP, fitted-preview, receipts and local source checkpoint are in the stage folder.


## Shay visible bogies and front compressor — 11 November 2026, STAGED ONLY

James clarified coil springs rather than leaves. The model now has raised paired
coils, substantial tread shoes and connected brake rigging, deeper chassis detail
and a mounted front steam air-compressor assembly. Latest matching model/runtime:
`build/shay-r3-runninggear-20261111/candidate`. See [running-gear handover](shay/R3-RUNNING-GEAR-DETAIL.md).
24,472 wheel/body/equipment checks and 33,789 transmission/truck checks pass;
the original source engine/gear suite passes 11,322 and the reopened native
preview 42. Runtime is unchanged from the preceding validated R3 candidate.
No installation, game launch or publication; James owns DV testing.


## Franklin Dockside 0.8.0 appearance pass — 11 November 2026, STAGED ONLY

James authorized implementation, offline build/validation and staging only.
Later explicitly chose staging with the measured engine-boundary contacts
recorded rather than expanding into cylinder/engine fitting. No install or DV
launch. See [Franklin appearance handover](dockside/FRANKLIN-0.8.0.md).

The later S282 2.1 donor supplies continuous rod/bearing construction and fitted
wheel-input presentation, adapted in authoring to S060 datums. Forked crossheads,
split guides, small-end pins and round piston rods use the existing native
absolute-crank clips. No runtime source/calibration/save/identity changes.
Candidate: build/dockside-franklin-20261111/candidate/RearrangedS060; runtime
0.24.0 F594C549CC2FB9883B77E767F89686523E705092F92CA67169F39325EF6BDBA1,
byte-identical to the staged Shay R3 runtime. Dockside bundle 0.8.0:
600A9B6FAA8C743DCDA36FBB4B991F44A179E3019EDAC6C517DE0A3530F4763D.

Offline: clean runtime build; 23,883 detailed geometry checks; 24,925 motion +
8,923 seek/isolation; native pin68/oil10,849/pipes423/effects177; native engine17;
exact-DLL native suites quartering115/pipes1,118/cocks284/cleanup145. Reports,
source provenance, fixture corrections and limits are in the detailed handover.
A real inter-unit contact limitation remains: 59 old/revised rod and 49 new
crank contact cases in 320 sampled side/poses. Matching totals do not prove
identical contacts or complete clearance. Explicitly accepted for staging;
James owns gameplay/appearance acceptance. Candidate includes preceding staged
Meyer/Shay work; it is not a complete release acceptance or publication.


## Shay R3 detail/model pass — 11 November 2026, STAGED ONLY

The fully authored 0.80 m / 86:20 Shay model, substantial steam plumbing,
hollow sleeve reducers, frame detail and corrected cock emitters are staged.
Matching runtime and original-only asset: `build/shay-r3-20261111/candidate`.
Source/native geometry checks pass; see [R3 detail handover](shay/R3-DETAIL-PASS.md)
for exact hashes, scopes, preview-fixture UV/material corrections and remaining
James-owned DV testing. Current 1.6 kg/s physics is retained. No install,
game launch, push or publication. This candidate includes concurrent unreleased
Meyer/physics work; it is not a complete release package.

## Shay 1.6 kg/s tune — 11 November 2026, STAGED ONLY

James approved the 1.6 kg/s Shay tune, retaining 0.80 m wheels, roughly 150 kN
starting effort and save compatibility. No install, DV launch or model rebuild.
The production calibration change is solely Shay.MaxFlow from 1.2 to 1.6.
Wheel radius, 252 x 300.808 mm cylinders, 45 L chest, 86:20 gears / 4.3:1 ratio,
92% transmission efficiency, saved 43-turn clock and native boiler are unchanged.
No automatic cutoff or additional force/adhesion mechanism was introduced.

Staged runtime: build/shay-flow-1600-20261111/candidate/RearrangedS060.dll,
SHA256 FF51E3A4C310745576BA007B2306662C2268AE920F9CD95A6771D93303E57E4B.
Clean runtime build: 0 warnings/errors. It includes preceding unreleased Meyer
work and current concurrent Shay authoring; this physics stage does not establish
complete model/geometry or UMM/Nexus release readiness. Do not install it.

Offline proof: build/shay-flow-1600-20261111/validation.json and engine-envelope.
2,365 engine-bank checks pass, including old 43:19 saves, actual prior 1.2 kg/s
Shay saves active AND dormant, exact serialized state round-trip before ticking,
new-calibration finite continuation, the unchanged Meyer suite and native slip.
The prior save producer is preserved under previous-1200-engine-bank/Native.dll.
The first new round-trip assertion compared parsed JSON doubles to live float
JValues; corrected it to compare serialized JSON on both sides, following the
existing cross-version fixture. This was a fixture precision mismatch, not a
production save change. No runtime rebuild was needed for that correction.
Native Unity suites exhaust/cocks/Shay-effects pass 611/284/23 checks (918 total)
at build/radio-0.24.0/runtime-audits-20261111-211839 against the exact staged DLL.
Earlier Meyer-specific evidence remains source-applicable; do not report those
older DLL tests as newly executed against this candidate.

At 16 bar absolute / 200 C, full gear, mean available kN at .1/1/5/10/15/20/30 km/h:
156.68 / 152.64 / 138.41 / 91.71 / 57.62 / 38.99 / 20.29.
Normal shorter cutoff: 70% reverser (~64.5% cutoff) at 10 km/h gives 127.69 kN;
40% reverser (~39% cutoff) at 15 km/h gives 106.47 kN. At 13 bar these operating
points give 111.06 / 87.05 kN. These are fixed-supply, unclipped machinery values.
Steam flow is bounded by 1.6 kg/s; James still needs to test boiler sustainability
and actual grade/adhesion behavior. Existing cab cutoff remains manual.

The modelling brief, integration note, physics report and README now reflect
1.6 kg/s. Earlier entries below are chronological evidence, not current approval
to keep 1.2 kg/s. Installed 0.23.4 remains SHA256
50D4C3D99678246B1350828B22DF4CAD2327EDCA78CB643B8F2BB365179EF1C7.

## Shay speed-curve clarification — 11 November 2026, feasibility only

James asked whether the sharp10km/h decline is inevitable with.80m wheels and
approximately150kN start. READ-ONLY production investigation: no tuning/model/
installation changes authorised or made. Earlier all-layout force tables used
FULL GEAR throughout; do not present them as the maximum useful operating curve.
The current1.2kg/s limit is saturated by10km/h at90%cutoff. Existing native cab
cutoff control substantially improves the curve without any code/asset change.

Bounded45-point native study: build/physics-20261111/shay-flow-study/results.json.
The folder freezes copies of the existing native fixture/production inputs; its
FlowStudy entry changes only the test-instance throttleMaxFlow field and cab
reverser, using unchanged.8m/252mm/4.3:1/45L machinery at16barabs/200C. Same
120s settling+60s averaging as the accepted envelope. No real boiler simulated.
At current1.2kg/s fullgear10/15kmh yields64.07/38.88kN;40%reverser(~39%cutoff)
yields112.96/74.98kN. Start in fullgear retains156.50kN at.1kmh. At an UNAPPLIED
1.6kg/s ceiling, fullgearstart156.68kN;70%reverser(~64.5%cutoff) gives127.69kN
at10kmh,40%reverser gives106.47kN at15kmh. At2kg/s fullgear10kmh119.71kN;
shortenedcutoff can also improve it. Any ceiling increase consumes more steam
under load and requires James's boiler/haulage testing; do not enlarge boiler
or silently change the approved model contract. Gearing need not change for
these improvements. Keep normal cab control, no automatic cutoff feature added.

## Meyer starting/adhesion pass — 11 November 2026, NOT INSTALLED

James authorised Meyer physics while his separate Shay modelling continues:
preserve the compound circuit/saves, test offline, do NOT install. This section
supersedes the older statement below that crank-resolved Meyer starting was
outside the first physics iteration. No DV launch/control or installation.

Confirmed defect: Meyer used crank-independent mean work even at rest, unlike
native cylinder/valve mechanics. MeyerNativeStage now prepares both native crank
states once before the cycle and requests actual instantaneous torque below
45 RPM, native blend to 60 RPM, then mean torque. MeyerCompoundCycle still caps
moving work using the discharged parcel's enthalpy drop, transfers conserved HP
exhaust to the receiver/LP, and uses actual receiver backpressure. No LP boiler
bypass, cylinder/chest/receiver/flow recalibration or arbitrary force boost.
An empty circuit starts on HP alone; the native 5% minimum cutoff means centred
gear is not zero torque at every crank angle. Full signed rollback thermodynamics
and independent HP/LP slip clocks remain outside scope.

New native trapped-cylinder pressure memory exposed a real reload regression:
one hot partial-cutoff case changed first-tick HP torque by 16% without saving
it. Optional mechanicsVersion=1/cylinderAdmissionBar now saves signed pressures,
strictly validates before mutation and retains existing meyerVersion=1, vessels,
ledger and engine IDs. Actual older saves round-trip unchanged before ticking;
their cache seeds from retained circuit pressure. Meyer reselection reseeds for
the copied phase; a fresh empty stage keeps native 1-bar admission history.
No changes to S060SavedState, engine mapping, oil topology or shared native
adhesion code relative to this task's baseline.

Adhesion proof: installed native dry coefficient is .20*1.3=.26; all four axles
already use current native WeightPerAxle correctly. Keep full adhesive share:
127.53kN grip at50t,144.36kN at56.6t, with native wet/sand/slip behavior. Native
resource mass uses actual water/coal amounts. Capacity Apply/Dispose now calls
native TrainMassController.UpdateTrainCarMass immediately after amount changes,
instead of leaving stale mass until the native120s timer. Missing early-load
owners safely defer to native initialization. Trace adds native/live coefficients
and bogie rolling/stress values at its existing bounded cadence; no drag patch.

Frozen candidate: build/meyer-physics-20261111/candidate/RearrangedS060.dll,
SHA256 20946D1620AB423CDF2D05BE8808BB68D459F51FB7DAF96383CD804925793340.
Build has0warnings/errors. Test.Runtime -RuntimePath isolates it from concurrent
Shay builds. This is NOT a complete matched release/UMM/Nexus package. Source
still includes ongoing Shay authoring; do not claim Meyer proof accepts it.

Proof manifest: build/meyer-physics-20261111/validation.json. All13 Meyer-relevant
native Unity suites passed27,633checks against that same DLL. First7 proofs are
in runtime-audits-20261111-210143; refill+remaining5 in210408. The new refill host
first failed because C#?? misses Unity's fake-null Rigidbody; corrected fixture
to Unity==null, then reran only affected/remaining suites. No production rebuild
for this fixture correction. Native mass/refill28, cocks248 and collision7062
include actual installed native implementations in an offline Editor host.
Managed:2346engine-bank checks(includes59native adhesion),40compound,18native
Meyer adapter,201functional,162capture-window; transition/getter/allocation checks
also pass. The functional Shay diameter assertion was stale at.7105064 and now
checks the approved.80m. Independent final mechanics/save review found no issue.

Fixed-supply16barabs/200C/fullgear mean Meyer available kN at.1/1/5/10/15/20/30kmh:
150.42/169.91/149.32/114.95/88.87/70.56/47.63. These are unclipped machinery
benchmarks, NOT measured drawbar or guaranteed all-phase static TE. Native grip
limits apply; expect James to verify starts/rollback, wet/dry/sanded slip and the
same known consist on1.5% grade, plus hot save/reload and arrangement switching.
Current installed0.23.4 hash remains50D4C3D99678246B1350828B22DF4CAD2327EDCA78CB643B8F2BB365179EF1C7.
Do not install until separately authorised. See docs/PHYSICS-0.24.0.md.

## 0.24.0 physics work / final Shay model handoff — 11 November 2026

James authorised the physics pass, agents and offline tests, explicitly NO
installation. The installed runtime remains 0.23.4, SHA256
50D4C3D99678246B1350828B22DF4CAD2327EDCA78CB643B8F2BB365179EF1C7.
The source includes the uninstalled 0.23.5 visual work below; never reset it.

Fixtures corrected FIRST: Build.Radio previously passed the Dockside donor as
stock and the force envelope used an obsolete Shay radius. New
tests/Articulated/Prepare.NativeCalibration.py extracts actual installed
LocoS060/[sim]/steamEngine (pathID331222,420x610mm,200L,2kg/s) with native hashes.
The managed fixture compares all9 arrangements at the same road speeds, actual
radii, steam rates and explicit grade budgets. Corrected pre-tuning evidence:
build/physics-20261111/fixture-baseline-envelope.json. Preserve its separately
built baseline-engine-bank/Native.dll: it produces actual old Shay/Dockside slot
saves for cross-version loading tests, not a manufactured JSON approximation.

Initial candidate tuned Dockside to340x470.919mm,115L and1kg/s per engine
(combined stock-engine2kg/s ceiling), and Shay to210x300.808mm/30L/1.2kg/s with
the old43:19 gearset. Private source meshes fitted its engine motion. That
candidate19385781... passed all15 native suites/30,099checks at
build/radio-0.24.0/runtime-audits-20261111-195306 plus managed437 engine,201
functional,162 trace,628 oil,1151 geometry,35 transaction and25 compound checks.
Earlier actual geometry failures justified guide windows, valve-chest movement
and connected branch clearances; no tolerance/geometry checks were waived.
The native rear-wheel fit gate was stale only because the0.23.5 native fixture
added rod meshes; rerunning Audit.InwardLayout.py retained21.5446mm clearance.

James correctly challenged the initial63kN Shay versus116kN Dockside role.
Do not preserve43:19 just because the prototype mesh used it. He has a separate
modelling session waiting, so agents STOPPED further gear authoring/export.
He then selected0.80m wheels and larger cylinders for approximately150kN start.
FINAL PHYSICS SPEC: three252x300.808mm cylinders,45L combined chest,1.2kg/s cap,
86:20 gears(reduced43:10=4.3:1),92% efficiency; native gauge and wheel width.
The actual native benchmark passes480 checks including old43:19 saves loaded
unchanged, native slip, flow budgets and43-engine-turn=10-wheel-turn period.
Evidence: build/physics-20261111/final-080-150kn-engine-envelope.json and checks.
At16barabs/200C/fullgear, meanShay kN at.1/1/5/10/15/20/30kmh is
156.5/151.6/135.2/64.1/38.9/25.1/11.3; at13bar124.6/122.0/108.1/64.1/38.9/25.1/11.3.
Nominal effort is not guaranteed drawbar pull: native adhesion still limits it.
Stock and Meyer compound calibration stay unchanged; conventional040 has.76
effective adhesive share at50t design datum, other conventional shares retain
1 or.75. No mass/CoM/boiler/identity or saved-schema changes. Diagnostics now
start their180s active budget at first regulator-open sample, retaining2Hz/120cap.

AUTHORITATIVE HANDOFF: docs/shay/REGEAR-MODELLING-BRIEF.md. The model session
must return a NEW fully authored engine/truck/transmission bundle. Old procedural
engine fitter is archived at archive/physics-first-fit/S060ShayPowerGeometry.cs;
it must not be applied twice. Motion reads cylinderPitch/valveAxisX from new
metadata. S060ShayAssets rejects missing/mismatching revision2/bore/stroke/radius/
gear and shaft metadata before conversion. Runtime radially sizes live DV wheel
aliases to.80m, retaining X/gauge. Engine/shaft/pivot fit still needs the modeller's
final contract and integration; compilation is not geometric acceptance.

The draftregear-source directory under build/physics-20261111 is an UNACCEPTED
earlier.71m study, not the final.80m contract. Its first source-only fixture
flagged132 gear contacts and narrowbore margin; no Unity export or new bundle
was made. Original LLW modules are unchanged. Read its README-DRAFT-NOT-ACCEPTED.

CURRENT STATUS: source version0.24.0 compiles,480 managed engine/save checks pass,
new assets/full native geometry/package acceptance PENDING modelling handoff.
Do NOT claim the earlier15-suite proof validates this new.80m/150kN source.
First candidate build/physics-pass-20261111/candidate-0.24.0 is marked SUPERSEDED;
do not install it. Final-spec evidence is in080-150kn-spec alongside it. No
game launch, installation, push or publication. When models return, integrate
actual datums, update candidate geometry fixtures to the new contract, validate
both LODs/gear mesh/shaft articulation/native wheels/saves, then make a complete
matched UMM package. James performs DV testing; installation needs his separate
instruction. Existing CORE_README/Nexus release copy still describe0.23.4 and
must be revised before packaging0.24.0; don't bypass those version guards.

## 0.23.5 Meyer visual repair candidate — 11 November 2026, not installed

James requested four visual repairs using his screenshots: rear beam z-fighting,
the tall hopper-side texture seam, remaining fixed loose gear, and insubstantial
rods. Work applies to the installed 0.23.4 maintenance line in this canonical
repository. The Documents workspace's separate v0.3-work six-axle experiment is
unchanged. James owns gameplay acceptance; no DV session or installation occurred.

Confirmed causes and bounded corrections:

- The R2 beam's iron bearers shared its exposed front/rear planes. Recess those
  faces by 6 mm inside the beam; ladder ends also avoid the shared front plane.
  Beam dimensions, buffer locations and coupling alignment remain fixed.
- SkinMap wrapped only vertices above the native tank's height, so the hopper's
  fan triangles interpolated discontinuous UV rows. Map the whole tall side
  affinely into the native tank island. This is a UV correction, not a normal
  recalculation or new texture. Both side fits have maximum UV error 1.23e-7.
- Retire eight exact native s060_body islands: the old guide end cheek, two
  separate bolts and outer valve-support pivot on each side. Their 228 source
  triangles become zero. Existing main journals/caps and new brackets remain.
- Native B99 s060_mech_wheels_connect and s060_mech_push_rod_to_connect are
  CPU-readable. Reuse their actual profiles, bevels, UV islands and live material
  instead of adding another model bundle or guessing thicker primitive rods.
  Keep two terminal coupling eyes and one web; shorten the main web while keeping
  its bearing ends. Both units retain their original animated pin planes and
  lengths, native wheel size/gauge/phase and steam simulation.

Stock bearing faces are closed at their original offset. Bore 59.5 mm-radius
eyes for the Meyer's existing through-pins. The thicker coupling web initially
contacted the small native axle-centre caps: compress only its hidden inboard
half to 12 mm (outer half stays 36.683 mm), leaving 5.48 mm lateral clearance.
Apply the inboard choice by actual side, not a shared mesh-X assumption. Move
existing guide rails onto the outer crosshead face; adapt cup mounting heights
to the actual larger bosses. Meshes and all moved guide transforms restore on
disposal. No new simulation ports, save fields, colliders or dependencies.

Final runtime SHA256:
339722A415545D8946C9897097B1CD2A2CD812A30D4B98C80BFCB62E91E2E509.
Build.Runtime.ps1 passes with zero warnings/errors. Final offline Unity suites:
phase/full gear 2,332 and inward pipes/bodywork 15,655 (17,987 total), including
both engines/sides, actual rod/pin/mount/guide triangles, wrapped native clips,
exact mesh/guide restoration, 228 -> 0 obsolete triangles, no shared beam faces,
and the continuous hopper UV map. Evidence:
build/radio-0.23.5/runtime-audits-20261111-155312. Four lit detail previews were
visually reviewed. They use the native green QA atlas, not James's blue skin or
an actual DV scene. Only the existing S060SteamPipes.destroying Editor warning
is known; the runtime build itself is clean.

Baseline: installed 0.23.4 DLL 50D4C3D... passed the existing pipe (15,642) and
phase (1,966) suites before production edits. Earlier candidate 89B19344... passed
body 119 and rear-coupling 64 checks at runtime-audits-20261111-154122. Those
classes stayed unchanged during later rod-only fitting; these are retained
earlier-candidate reports, not reruns on the final DLL. Managed functional 194,
measured geometry 1,151 and oil migration 628 pass; later rod-only changes do
not enter these targets. This task did not repeat the full release/fleet suite.

Fixture corrections: an initial spatial loose-part count also included four
retained crossmember faces; identify the eight source islands instead. Contact
testing exposed a second independent fixture defect: Vector3.Normalize zeros
area normals shorter than 1e-5, falsely treating valid small clipped faces as
coplanar. SteamMeshContact now divides by the measured nonzero length after its
existing degeneracy guard. A regression uses the actual separated rod/shaft
triangles in both directions. Rod/pin sweeps use sampled donor clips and the
front fitter, never unsampled prefab defaults. These corrections do not relax
production geometry, journal retention or acceptance tolerances.

Candidate files: build/meyer-visual-polish-20261111/candidate-0.23.5, containing
the DLL, Info.json, build receipt, validation.json, four previews and READ-ME.txt.
This is a two-file runtime update candidate, not a full Nexus ZIP. All model
bundles are retained; the installed DLL still hash-matches 0.23.4. Only five
runtime source files differ from its build receipt (bodywork, clearance, gear
mounts, rods and oil-cup visual mounting); protected simulation/save sources match.

Local preservation branch: codex/meyer-visual-polish-20261111. It snapshots the
current maintenance source, including earlier uncommitted 0.23.3/0.23.4 work;
the active branch and normal index are retained. No push, package publication or
installation. James's checks after separately authorised installation: inspect
the four defects on both sides, roll both directions through a full wheel cycle,
inspect on a curve, check eight oil cups, and switch to stock/Dockside to verify
native restoration. Gameplay acceptance remains pending.

## 0.23.4 Nexus package prepared — 11 November 2026, not published

James requested packaging the installed 0.23.4 for Nexus. Canonical archive:
build/nexus-0.23.4-single/RearrangedS060-0.23.4-UMM.zip. SHA256:
681EB4E1500FAA9072F02925DF4578318B58EB43E03135A4B0950C78D79A109E.
Desktop copy: C:/Users/james/Desktop/Rearranged S060 0.23.4-beta.zip.
Nexus release copy and pinned installation FAQ are alongside it, with the
RearrangedS060-0.23.4 prefix. Release copy compares against public 0.22.2-beta.

The exact installed/tested DLL 50D4C3D99678246B1350828B22DF4CAD2327EDCA78CB643B8F2BB365179EF1C7
was reused; no runtime rebuild or model export. Three bundles are byte-identical
to the verified 0.23.3 package. Updated CORE_README, INSTALL, Meyer README and
Nexus text describe 0.23.4 coupling alignment and CCL dependency troubleshooting.
One UMM mod root, eleven payload files, no nested archives, Settings.xml, native
game DLLs or private fixtures. Uses the established single-folder manifest and
native ZIP writer; receipts distinguish new runtime evidence from retained
0.23.3 evidence rather than claiming every native suite reran on 0.23.4.

Exact ZIP passes 57 native UMM read/install/update/uninstall/migration checks.
The fixture now accepts -LegacyArchive so migration uses the genuine existing
0.23.3 three-part archive instead of manufacturing a 0.23.4 legacy package.
Hidden command-line Unity reopens the actual UMM-extracted bundles: 38 checks
pass. No mouse control, DV launch, real installation or publication. Provenance:
build/nexus-0.23.4-single/nexus-package-receipt.json, release-validation.json,
umm-proof-20261111-134323/native-umm-validation.json and
combined-bundle-validation.json. Desktop copy hashes match the tested archive.
Source/docs checkpoint: codex/nexus-0.23.4-20261111, active branch/index retained.
Gameplay acceptance remains James's; packaging does not establish it.

## 0.23.4 installed for James's coupling test — 11 November 2026

James explicitly authorised installation. Two host process gates confirmed DV
closed. Installed only the previously validated 0.23.4 DLL and Info.json from
build/meyer-rear-coupling-20261111/candidate-0.23.4; both installed hashes match.
Runtime SHA256: 50D4C3D99678246B1350828B22DF4CAD2327EDCA78CB643B8F2BB365179EF1C7.
All three installed bundles match the original 0.23.3 release evidence and were
retained. Settings and every other protected mod file are hash-unchanged. Removed
only RearrangedS060.dll.50935.cache. No backup, rebuild, release ZIP, publication,
UI automation or game launch. Receipt: build/meyer-rear-coupling-20261111/
candidate-0.23.4/install-receipt-latest.json. Offline validation.json retains its
historical pre-install status; the installation receipt is the current authority.
This is the existing 0.23 maintenance line, with the flipped front Meyer unit;
the isolated v0.3-work copy was not modified. Floor remains unchanged. James's
rear-hook/air-connection gameplay test is pending. Earlier not-installed notes
below describe preparation, superseded by this installation record.

## 0.23.4 rear coupling alignment — 11 November 2026, not installed

James authorised the rear hook/air connection repair, offline build and Unity
Editor checks, with no mouse control or installation. Floor is explicitly out
of scope. Exact native geometry confirms the old hook/hose shift was -3.700 m,
but the R2 hook plate moved -3.995 m. This left a 295 mm pocket mismatch and the
angle-cock lever inward of the new sill. Move the native physical anchor and
hook/hose rig together to Z -8.565, preserving their stock relative offsets.
The existing native buffer anchors are rebound with inverse compensation, so
buffer heads and their fixed native stem ends stay at their proven positions.
The existing support path updates native bounds/intercoupler/logical length
(13.135 m) and prepares the anchor before saved connections load. No new save
field, loading hook or bundle export. Legacy donor metadata remains at -8.27;
normal play copies its engine frames onto the native S060. Floor/bodywork source
is hash-identical to before this repair. The earlier CCL warning change is included.

Build.Runtime.ps1: zero warnings/errors, runtime 0.23.4 SHA256
50D4C3D99678246B1350828B22DF4CAD2327EDCA78CB643B8F2BB365179EF1C7.
Focused exact-native baseline reproduced the old 295.007 mm pocket error and
223.561 mm inward cock position; its other buffer/restoration checks passed.
Fixture correction before relying on baseline: keep references to the native
buffer heads after the production binder reparents their anchors. The corrected
baseline and candidate use identical exported native fixture bytes. Candidate
passes 64 checks, including nested and native-reparented chain/hoses, rotated
car roots, repeated apply/restore, front coupling and unchanged rear buffers.
Maximum pocket error 0.006 mm, buffer-position error 0.005 mm; all lever vertices
are at least 71.442 mm behind the sill. Existing lamp/stem/buffer audit passes
7,596 checks including compression, yaw, LODs and restoration. Measured geometry/
native logical-length checks 1,151 and conversion-boundary checks 35 also pass.
The Unity Editor logs only the existing S060SteamPipes.destroying CS0414 warning.

Evidence: build/meyer-rear-coupling-20261111/baseline-host-fixed-fixture and
build/radio-0.23.4/runtime-audits-20261111-131733/meyer-rear-coupling. The focused
case is reusable through Test.Runtime.ps1 -SkipBuild -Only meyer-rear-coupling.
Initial sandboxed Unity licensing failed; normal host licence access allowed
the hidden command-line Editor checks. No licence changes or UI automation.
Older full-suite/bundle evidence below remains bound to its original binaries;
this task reran the affected checks, not the entire fleet or all 14 suites.

Built candidate: build/meyer-rear-coupling-20261111/candidate-0.23.4. Installed
0.23.3 DLL hash is unchanged. No game launch, release ZIP, installation, push or
publication. James owns gameplay acceptance: inspect and operate the rear hook
and angle cock, couple/uncouple, save/reload a coupled Meyer, then switch back to
stock/Dockside and check native restoration. Local checkpoint branch:
codex/meyer-rear-coupling-20261111; active branch/index retained.

## Asset-pack warning and Nexus installation FAQ — 11 November 2026

James requested clearer guidance after a user reported installed packs being
unavailable. The existing ResolveFrames lookup reports absent/unloaded prefabs;
it does not establish missing files or a specific CCL failure. Replace only its
generic install instruction with guidance naming Custom Car Loader (CCL), its
dependencies including DV Language Helper, a game restart, bundled packs and
Player.log. The same exception reaches both radio and UMM through TryApply.
No loader, dependency, conversion or save behavior changes.

Nexus-ready draft: docs/nexus/PINNED_INSTALLATION_FAQ.txt (BBCode). It covers the
single-ZIP install, dependencies, pack-load troubleshooting, legacy standalone
pack removal and the log needed for support. The reported user's cause remains
unconfirmed without their log. Existing local Player.log records both donors
loading successfully with installed 0.23.3 and CCL 3.1.11; this is evidence from
James's installation, not reproduction of that user's problem.

Build.Runtime.ps1 passes with zero warnings/errors. This is a message-only source
change on the current 0.23.3 working tree; build/stage7 contains the candidate and
build receipt. No new tests or geometry/bundle audits were needed for the text
edit. Existing full-suite evidence below remains bound to its original DLL.
The new warning's in-game layout has not been checked. Version is unchanged;
no release ZIP, installation, DV session, push or Nexus publication performed.
The narrow text-only checkpoint is on codex/asset-pack-warning-faq-20261111,
based on tracked HEAD; it excludes unrelated existing 0.23.3 work. Build evidence
applies to the live working tree. The active branch and normal index are retained.

## 0.23.3 Meyer running-gear supports — 9 November 2026

James identified unsupported guides/springs and a loose short crank arm on BOTH
engine units. Native mesh inspection confirmed two unused native01 return-crank
islands, two obsolete body valve-gear supports and FIVE stock spring assemblies.
Keep the 51-vertex main crank journal and closed bearing cap; do not substitute
wheel02 or assume six stock spring assemblies. The old parts retire Meyer-only.

S060MeyerGearMounts adds one combined live-material support mesh to each powered
frame: gusseted between-axle crossbeam above the rod sweep, outer guide carriers
and end returns, spring seats and hangers. Existing leaves and upper frame edges
move inboard to absX .61 (leaf outer .66; measured wheel back plane .685701).
Per-unit owned native wheel copies remove only the two unused return-crank
islands; original wheel meshes and suspension positions restore on disposal.
The firebox enclosure, native wheel size/gauge/clocking and compound sim remain.
No new colliders, ports or save fields. Runtime compiles without warnings/errors.

Completed offline validation: all 14 native suites / 27,738 checks pass against
runtime SHA256 83FE0005C9DDEA1E97C2BCA8FBC68FBE8BC8827CD4C193B03679309B8AC72F5D.
Evidence: build/radio-0.23.3/runtime-audits-20261109-111433. Phase audit now covers
both frames over 14 phases, actual mount/wheel/main-rod/coupling-rod/crosshead
clearance, physical guide/frame/seat connections, main journal/end-cap retention,
return-crank removal and exact mesh/position restoration. Pipe audit includes
mounts/leaves in the existing 27-pose clearance sweep and posed native preview.
Managed checks pass: functional194, native engines351, oil628, geometry1151,
transactions35, compound25 and existing transition/allocation cases. Native
donor18/17, UMM57, extracted bundles38 and isolated installer18 also pass.
The first isolated installer attempt ran before its required bundle receipt
existed; no game files touched. Repeated after the bundle check: all18 passed.

Installed 0.23.3 only after both host process gates confirmed DV closed, no backup.
All11 installed payload files hash-match; Settings.xml preserved. Three asset
bundles remain byte-identical to 0.23.2. Ten protected sim/save/oiling/refill source
hashes still match the prior compatibility proof. Exact evidence and receipt:
build/nexus-0.23.3-single/release-validation.json and install-receipt-latest.json.
ZIP: build/nexus-0.23.3-single/RearrangedS060-0.23.3-UMM.zip, SHA256
76E3909C99162088FF695C9B638809BA73E7F3B4CD2D97D359662ECF4023BF9F.
Reviewed offline posed image: same folder/Meyer-0.23.3-offline-preview.png.
No DV session, commit, push or Nexus publication performed. James checks both
units at several crank positions, on curves, and restoration when returning to
stock/Dockside. Gameplay acceptance remains with James.

## 0.23.2 Meyer firebox-base enclosure — 9 November 2026

James reported 0.23.1 looks good but native fire particles show through the
opened firebox-base side gaps. Bounded repair: two fixed closed cheek bars added
to the existing S060MeyerClearance body copy, using its live S060 material and
existing renderer. No particle, fire simulation, geometry fitting, wheel, pipe,
control or save changes. Bars occupy absX .40.. .44, Y1.28..1.72 and
Z-2.25..-.45; overlap the retained frame and upper shell to close seams. The
moving casting sweep remains outside absX .473853. Root checked the original
native fire emitter under LocoS060_Interior/Indicators/I_FireboxCoal; its authored
settings were only read, never changed. Normal cab fire remains intended.

Candidate 0.23.2 builds with zero runtime warnings/errors. All 14 native suites /
26,142 checks pass, including new covers against actual cylinder and pipe sweeps:
build/radio-0.23.2/runtime-audits-20261109-020503. Runtime SHA256
97208B76F82F985EE10EAC71DB4976579A4FF2DE2FC0C5122E323C082DE6C819.
Installed 0.23.2 after host process gates confirmed DV closed; no backup and all 11 installed payload files hash-match. Settings preserved. Managed tests passed (functional 194, engine 351, oil 628, geometry 1,151, transaction 35, compound 25 and existing transition/allocation cases). Native donor 18/17, UMM 57, bundle reopening 38 and isolated installer 18 pass. Receipts: build/nexus-0.23.2-single/install-receipt-latest.json and release-validation.json. ZIP SHA256: 8C779433D61A59B4D71C5A0340AD4E431ABB3BDA1CBEFCBFE85D9735EFEB2318. James owns actual
lit-fire side-view acceptance; no DV session was launched by the agent.
## 0.23.1 Meyer front fit and redundant stock gear — 9 November 2026

James reported full-size HP cylinders overlapping drivers, a stray forward
hanger/slide bars, empty original cylinder space and duplicated high steam pipe.
The user-owned screenshots are the acceptance reference. Work is bounded to
those defects. No DV session is launched; James owns gameplay acceptance.

Front wheels/supports and rear unit remain fixed. S060MeyerLayout now places the
HP castings 850 mm cabward (local cylinderZ2.23), with 2 m main rods. New
S060MeyerFrontMotion is a cached late geometric pass after native animation,
using actual animated wheel pins. It fits rods/crossheads/piston rods, moves
existing guides/saddles/effect holders and extends the existing frame rails.
Crank phase, steam model, save fields and driver gauge remain unchanged.
Conservative native-flange clearance is 50.9 mm. Native shaft axis was checked
directly (.712937 m); do not infer piston height from raised stock guide rails.

The new cylinder lever arm changes its yaw sweep. Measured all5,010 referenced
vertices /108poses before sizing clearance: frameinner.44, floorinner.60,
coverreturninner.55. Native floor guard29.54mm; frame guard33.85mm. Old0.23.0
clearance coordinates cannot simply be moved aft while keeping the old widths.
Evidence: build/meyer-polish-20261109/front-casting-sweep-measurement.json.
S060MeyerClearance retains UVs/normals/tangents/material sections and caps actual
cut sections. Steam-joint openinginner.48 follows its rendered27pose sweep;
service-bandminabsX.50536 leaves25mm. Source-only bodypreflight passed108poses.

Confirmed forward strays: two connected native frame/hanger islands retain an
unsupported low forward drop after the stock mechanism is hidden; the highlighted
assembled native preview identified the exact pieces. OnlyZ>2.05/Y<1.50 drop is
retired; upper frame is kept and cut ends capped. James then circled FOUR separate
20-vertex native s060_body slide-bar islands: absX1.037513..1.067644,
Z1.111250..2.733013; Y.865920.. .947393 or1.021003..1.102477. These are obsolete
stock static guides, not the new moving rods or fitted cabward guides. Retire
those exact islands in Meyer only. Native body fixture proves40triangles before,
zero afterwards, and original Mesh identities restored on exit.

Pipe fitter trims the former S282 front upper course above lower+.33 (aboutY1.82)
while retaining the chest/transfer flange. One lower admission course X±1.50,
Y1.60 ends atZ-.30/Y1.42 and a small swivelling feed to each measured HP stem.
The LP single-chimney roof return and Dockside routing stay unchanged. New
S060MeyerFrontEquipment fills the original front cylinder space with a mounted
steam air pump (left), reservoir (right), closed plate extensions and bearers.
It uses one merged rigid mesh/renderer and the live S060 material. Visual only:
no second compressor, capacity or new brake simulation. Reference for equipment
category: https://collection.sciencemuseumgroup.org.uk/objects/co207667/air-compressor-westinghouse-type-f
No external model or texture was copied.

New pipe fixture checks actual triangle/box contact, not AABB overlap as final
cab-entry rejection. The air pump steam service intentionally shares the native
left boiler tee: exact bounded triangle clipping permits that connection only;
every other equipment/admission intersection remains checked. Preview samples
actual donor clips and reapplies the production front fitter before rendering;
unsampled prefab rods otherwise lie at authored default positions. Camera-side
lighting was corrected in the existing RenderWide utility. These are fixture
corrections, not changes to the native simulation or loosened entry limits.

Final validation and installation completed. All 14 native suites / 26,142 checks
pass against runtime SHA256
63F422BA53F9FAB30F46B75FD6AD7B22E7E29E0FE0C69E851E25B40FD6BEDE1D.
Evidence: build/radio-0.23.1/runtime-audits-20261109-012811. Affected native suites:
pipes 14,936; body 119 (108 poses, zero contacts, old slide triangles 40 ->0,
original four mesh identities restored); phase/full gear 1,076. Managed checks
pass: functional 194, native engines 351, oil 628, geometry 1,151, transactions 35,
compound 25, and existing transition/allocation checks. Donor component tests 18/17,
actual UMM 57, extracted bundle reopening 38 and isolated installer 18 also pass.
All three donor model bundles are byte-identical to 0.23.0. Ten protected sim,
save, oil topology, selection and refill source hashes match the installed 0.23.0
baseline: build/meyer-polish-20261109/compatibility-validation.json.

Installed 0.23.1 after both host process gates confirmed DV closed. No backup;
all 11 installed payload files hash-match the package. Settings.xml remains
A89B908441E9E9984D0D08D8B7C5B516475D22BAE344ED7E21CE770631E78BD9.
Evidence: build/nexus-0.23.1-single/install-receipt-latest.json and
release-validation.json. Nexus ZIP:
build/nexus-0.23.1-single/RearrangedS060-0.23.1-UMM.zip; SHA256
C32BCB48AE60D1941D9F5690B4B8691006755D7FFC34EE7A0608D30275445508.
Reviewed posed offline preview (native materials, sampled donor clips):
build/nexus-0.23.1-single/Meyer-0.23.1-offline-preview.png.
No DV session, commit, push or Nexus publication was performed. James tests
actual driving, curves, cocks/drips, arrangement switching and save/reload.
Older partial candidate evidence is historical and cannot validate a later DLL.
## 0.23.0 inward-facing Meyer — 8 November 2026

James authorised the inward-facing arrangement, offline validation and installation
with no backup, only with DV closed. Gameplay belongs to James; do not launch DV.
This section is the current task; older candidate/install notes below are history.

The front visual engine turns Y180, while native bogie/support orientation stays
forward. The complete rear running gear is 65 mm aft; its real native axles follow
those visual axle positions. Bodywork, rear buffer beam, lamps, coupler, native
support pivots and save IDs stay fixed. The guarded exported-native wheel sweep
passes 27,864 poses with minimum rear-beam gap 21.5446 mm. A 75 mm draft was rejected
after correcting a fixture that used the old collision beam height instead of R2.
Evidence: build/meyer-inward-layout/rear-wheel-fit.json and Audit.InwardLayout.py.

Actual inward native castings contacted the stock frame/footplate in the first
108-pose test. LMS masks were checked against the installed splitter; the contact
was real. S060MeyerClearance uses reversible per-car mesh copies for exact obsolete
hanger islands and local plate relief, including split floor/tank-rail ownership.
The measured local lower-frame relief retains a 1.06 m central spine and caps its cut crossmember ends. The complete casting sweep retains 32.69 mm to that spine. Native normals/tangents/UV/material sections survive; pipe service openings use
measured donor geometry. Apply clearance before S060SteamPipes, unwind pipes first.
Do not shrink the castings, widen the wheel gauge or relax masks to hide contacts.

Front presentation uses the native absolute-time multiplier -1/offset .75. B99
FloorMod(1f) wraps this before Animator.Play. Physical cylinder outlet/hiss holders
swap with the reversed local sides; native valve masks, crank ports and reverser
are unchanged. Oil cup local geometry follows each wheel, but persistent circuit
assignment uses physical car side, avoiding swapped left/right front circuits.

S060MeyerInwardPipes adds body admission feeds and short local transfer elbows,
reusing existing manifold/drypipe spans and LP chimney return. All three model
bundles remain byte-identical. The new runtime is 0.23.0; donor packs remain 0.7.0.
Compound cycle, engine mapping/state, calibration, save schemas, selection, oil
circuit topology and native refill logic match the installed rebuilt 0.22.4 source
receipt; evidence: build/meyer-inward-layout/compatibility-source-validation.json.

Final offline validation: all 14 native suites / 22,149 assertions pass against
runtime E3FE9A0BC6DB95B079EF32B935D643C8C2694BDDCBD03545BA3FA5BAC05810EA.
Evidence: build/radio-0.23.0/runtime-audits-20261108-120034. New suites: inward
pipes 11,684; body 117 (108 poses, zero contacts); phase/oil-side mapping 337.
Managed checks also pass: functional/save 194, native engine bank 351, oil migration
628, geometry 1,151, conversion transaction 35, compound 25, and production axle
cache/transition/allocation checks. Both retained donor native component suites
pass (Meyer 18, Dockside 17). Exact ZIP passes UMM 57, bundle reopening 38 and
isolated installer 18 checks. No DV session was launched.

Installed 0.23.0 after host process gates confirmed DV closed. No backup created;
all eleven real installed payload files match the package. Settings.xml remains
A89B908441E9E9984D0D08D8B7C5B516475D22BAE344ED7E21CE770631E78BD9.
Receipts: build/nexus-0.23.0-single/install-receipt-latest.json and
release-validation.json. Single-folder Nexus ZIP:
build/nexus-0.23.0-single/RearrangedS060-0.23.0-UMM.zip, SHA256
0607F3DD45454996391B14F89792CE051E5B2C58576BE4AC8AF1F7705A35366A.
It remains unpublished. Changelog is since public 0.22.2-beta. All earlier
uncommitted source work was preserved; no commit or push was made by this task.

James's gameplay acceptance remains pending: drive both directions, cycle cocks,
inspect rods and pipes on curves, inspect eight oil cups, save/reload Meyer and
switch Meyer -> stock/Dockside while retaining vehicle/resources/damage. See
docs/RADIO-ARTICULATED-TESTING.md. Offline geometry/native fixtures do not establish
cab FPS, driving, native-world import or audible playback acceptance.

Fixture lessons: the original rear-clearance measurement used the old collision
beam height; actual R2 dimensions limited the safe shift to 65 mm. The asymmetric
native boiler cut's area centroid is not its pipe axis: retain the measured cut
boundary axis, not a guessed centroid. Phase validation compares actual original
and reversed donor world quaternion deltas rather than assuming a sign or relying
on a tiny-angle extraction. The first contact fixture timed out; compact actual
used cylinder vertices and restrict candidates by the measured sweep, retaining
the existing triangle test. Validate actual LMS split ownership for floor/rails.

## Rebuilt 0.22.4 refill fix installed — 7 November 2026

James confirmed DV was closed and authorised the rebuilt 0.22.4 installation
with no backup. Install.Nexus.ps1 completed its host process gates and verified
all eleven installed files; Settings.xml is unchanged, backupCreated=false.
Installed runtime SHA256:
93F07680D4DECE5A9ADBE0839324442EF19048A81F1D9A90CA51DAAD34BE5755.
Receipt: build/nexus-0.22.4-single/install-receipt-latest.json.
James will test Meyer coal/water filling through the original S060 points,
expecting combined limits of 8,800 L water and 3,500 kg coal. Gameplay acceptance
is pending; no game was launched. Earlier uninstalled/pre-fix notes below are
historical. The release remains unpublished by this chat.

## 0.22.4 Meyer refill correction — 6 November 2026, unpublished rebuild

James reported a 0.22.2 user unable to refill above stock amounts. Confirmed B99
integration defect: SimulatedCarPitStopParameters caches LocoParameterData.maxValue
at initialization, while RefreshParameters updates only current amounts. The
Meyer enlarged ResourceContainer.capacity/CAPACITY but left station maxima at
4,500 L / 1,200 kg. Native purchase limits therefore blocked the extra supplies.

S060MeyerCapacity now refreshes those existing cache objects on apply/restore,
using the same native resource containers. The original S060 filling points
serve combined 8,800 L water / 3,500 kg coal stores. No extra filling point,
separate bogie store, servicing hook, CCL change or save migration is required.
Late-created native servicing components initialize from the enlarged containers;
lookup on teardown restores their stock maxima too. Full Meyer stores still
clamp to stock capacity when switching away, as previously documented.

Baseline fixture build/meyer-refill-baseline-224557 reproduced the failure with
the installed pre-fix 0.22.4 DLL. The focused candidate native fixture passes
22 checks: retained station-data identities, actual purchase limits and refill
ports, native fraction save/load ordering, repeated switches and late servicing.
Current native evidence: build/radio-0.22.4/runtime-audits-20261106-224723.
The release remains 0.22.4 because it is unpublished; the rebuilt ZIP is under
build/nexus-0.22.4-single. The installed pre-refill-fix DLL is still
7F6C5071EE4B4AF280CFEC6A3965CFD6C3E01188B2AF98CAAAC068B85AD70618.
No new installation or DV gameplay test is authorised or performed by this task.

Rebuilt refill-fix runtime SHA256: 93F07680D4DECE5A9ADBE0839324442EF19048A81F1D9A90CA51DAAD34BE5755.
Final ZIP SHA256: 5909CD473EC3FFAD2C01CDAA3B7B16F371F47F79D35D7F08781E23956C0A0ABE.
All 11 native suites / 10,011 checks pass, including refill 22. Managed/native
component checks pass (functional/save 194, engine 351, oiling 628 and existing
transition/geometry/trace suites); final ZIP passes actual UMM 57 checks.
Proof: build/nexus-0.22.4-single/umm-proof-20261106-225004.
Unchanged model hashes retain the earlier 38-check bundle-reopen evidence.
The installed pre-fix 0.22.4 is unchanged; this rebuilt release is not installed.

## 0.22.4 installed for James's testing — 6 November 2026

James confirmed DV was closed and authorised installing 0.22.4 with no backup.
Install.Nexus.ps1 checked the host game process before preparation and again
before copying. Installation completed with backupCreated=false; all eleven
installed files match the final package receipt and Settings.xml is unchanged.
Installed runtime SHA256:
7F6C5071EE4B4AF280CFEC6A3965CFD6C3E01188B2AF98CAAAC068B85AD70618.
Receipt: build/nexus-0.22.4-single/install-receipt-latest.json.
James will test cab performance, oil cups and arrangement switching. Gameplay
acceptance remains pending; no game was launched and no release was published.
The preparation/earlier installation statements below are historical.

## 0.22.4 release candidate preparation — 6 November 2026

James authorised updating current docs, correcting the Shay wheel description,
building/testing offline and preparing the single UMM Nexus ZIP. No installation
or publication is authorised. This supersedes the earlier hold on packaging the
0.22.3 performance work. James performs all DV gameplay acceptance.

The Shay description and native wheel implementation now share the unchanged
0.3552532 m rolling-radius datum (about 0.71 m diameter). Runtime metadata is
0.22.4 to distinguish it from the installed pre-performance 0.22.3. Save schemas,
arrangement/circuit IDs, wheel physics and steam calibration remain unchanged.
Current docs describe Meyer R2's real capacities and stock-capacity overflow
clamp, nine choices, six-cup Shay exception and included model bundles. R1 Shay
material is explicitly historical. Changelog baseline is public 0.22.2-beta, corrected by James on 6 November. The earlier 0.19.1 baseline was stale.

The installed DLL remains 373C455B9A15FDEB34FC227D460E188E1553558182D53444ED9E42A4435AC833.
Candidate runtime/native evidence is under build/radio-0.22.4 and the final UMM
archive/receipts under build/nexus-0.22.4-single. Reuse the verified unchanged
Meyer/Dockside/Shay model bytes; no model rebuild is justified by these edits.
Final offline validation passes: ten native suites / 9,989 checks, functional/save
194 (including the Shay diameter regression), engine/save/ECS 351, oiling 628,
geometry 1,151, axle/cache regressions, bounded trace 145, transactions 35 and
compound 25. Asset-pack native checks pass Meyer 18 / Dockside 17. The final ZIP
passes actual UMM extraction/update/uninstall/migration 57 and extracted-bundle
reopen 38; isolated installer fixtures pass 24 legacy / 18 single-folder checks.
No DV scene, real installation or publication was performed. Runtime compile
has zero warnings/errors; unchanged model bytes retain prior export evidence.

Runtime SHA256: 7F6C5071EE4B4AF280CFEC6A3965CFD6C3E01188B2AF98CAAAC068B85AD70618.
Documentation-only package correction: changelog now compares against James's last public 0.22.2-beta. Runtime/native and model reopen evidence is retained for identical binary payload; the corrected archive passed fresh UMM 57 checks.
Final UMM ZIP SHA256: 30B41EA98AB6D624F7F260F3ECF7EF412F45589D3F37E06AD4F2104F75536A3C.
Native evidence: build/radio-0.22.4/runtime-audits-20261106-122415.
UMM evidence: build/nexus-0.22.4-single/umm-proof-20261106-123235.
Bundle reopen: build/nexus-0.22.4-single/combined-bundle-validation.json.
Release copy/changelog: docs/nexus/NEXUS_RELEASE_COPY.txt and CHANGELOG-0.22.4.txt.
The independent agent review covered the shared-radius change, save/ID stability,
current docs, oil/capacity exceptions and package contents/receipt consistency.

## Open 0.22.3 candidate: performance audit fixes — 31 October 2026

James authorised the axle-allocation, pipe-update/error and exhaust-lifecycle
fixes, with save compatibility and all available checks short of loading DV.
Keep this candidate open for more changes: no release packaging, installation,
push or publication. Existing uncommitted 0.22.3 bodywork/oiling edits remain.
See [the performance-fix record](PERFORMANCE-FIXES-2026-10-31.md) for scope,
native-first cleanup rationale, exact validation evidence and limitations.

Save schemas, arrangement/circuit IDs and simulation calibration are unchanged.
Repeated clean axle reads now retain the cache without allocating. Pipe updates
recover from invalid poses and use a bounded fitting budget. Audio bindings undo
partial native subscriptions and handle repeated Init/Deinit without accumulating
copies/listeners. Two existing R2 layout constants now live in MeyerPlan so the
authoring helper can compile without importing native TrainCar code; values are
unchanged. Test.Runtime and future Build.Radio require the new pipe and exhaust
failure-injection audits. Cab FPS/user-report attribution remains unverified.

## 0.20.2: Shay gear-clock trace — 27 October 2026 (Claude)

James reports the Shay gears, pinions and U-joints do not turn. The 0.20.1
trace showed the main slot's crank advancing and his clip shows the wheels
turning, so the break is somewhere between the simulated S060ShayEngine and
the transforms. The articulated trace (schema 2) now adds a "shay" block to
every Shay sample: whether the simulated Shay engine is the selected one, its
GearTurns/crank/geared RPM port; whether S060ShayMotion exists, is enabled
and active, how many LateUpdates it has run and on which frame, and whether
its engine is the same object the simulation ticks; the transmission's input
turns, wheel turns and phase errors; and the live local rotations of the
engine crank, rear axle and the first outlet/line/pinion/wheel/cross/member
part. Shay/tools/Read.GearTrace.py prints one line per sample (newest trace
in the installed diagnostics folder by default). Samples are written at 2 Hz for
the first three seconds after selection and then only while the regulator is
open (120 samples / 180 s per window). Diagnostics only.

## 0.20.2: Shay body relief and finishes — 27 October 2026 (Claude)

Claude took the repository over from Codex at fa64f34 (shared board X121).
James's 0.20.1 game report: the old reverser was cut in half, the front of the
cab ladders was cut off, a lengthwise hole under the boiler showed the firebox
fire, and the whole S060 texture sheet was wrapped over the Shay cylinders and
engine block. The gears/U-joints not turning is a separate, still open report.

Causes. S060ShayBody clipped every s060_body triangle against the R1 relief
boxes, so any part crossing a box edge was sliced: the bay box's rear face at
z=-2.82 runs through the cab steps and the tank notch through the reverser
reach rod. Nothing replaced the removed frame below 1.285 m, leaving an open
band up to the deck underside (1.62 m). S060Shay.RefreshMaterials swapped every
PAINT/painted slot (engine, truck and transmission frames) to the live body
material, whose texture is the S060 UV atlas the Shay meshes were not made for.

Relief now works on whole native pieces (connected islands, welded across UV
seams). A piece overlapping the cab doorway/step exclusions is kept whole; the
outside reverser and the conventional cylinder blocks are removed whole; a
piece inside a relief, reaching less than 0.25 m beyond it or smaller than
0.3 m² is removed whole; only larger structural plates (frames, deck, tank
side, saddle) are trimmed. Closures are added to the per-car copy: a belly
plate at y=1.285, x ±0.80 from z -2.82 to 3.10, side sills at x ±0.80 from the
belly up to the deck underside between the cab front and the front end, and a
four-face pocket lining the engine notch. With the cylinders gone, trimmed
plates also lose their foot below the belly as far as z=3.55 (the saddle
would otherwise hang in front of the front truck), the belly runs to 3.55, and
the cylinder drain-cock fittings go with the cylinders. The reverser box was
widened to z=-0.52 after the audit showed one 16-triangle reverser part
reaching 4 mm past it. Their UVs sample the centre of the
largest deck-underside and tank-side faces, so they carry the current livery.
Offline, against the native body dump and the shipping bundle swept through
the contract's 49 truck poses, no kept or trimmed triangle meets the swept
trucks, transmission or engine; the swept volume tops out at 1.28 m under the
belly. ConventionalNativeAudit gained relief checks (ladders whole, reverser
gone whole, belly/sills/pocket present, nothing left inside the truck bay).

Finishes: painted Shay slots now use a shared run-time "dirty gunmetal"
(Standard, metallic .55, smoothness .28) and gear/shaft/machined slots an
"oily steel" (.75/.42); both multiply a generated tileable grime texture.
The bundle's brass-coloured gear_steel is no longer visible. Pipes keep the
body material because their tube UVs point at a fixed atlas spot. No asset
bundle, save, arrangement ID, oil circuit or simulation change.

## 0.20.1: processed-CCL Shay effects hotfix — 27 October 2026

James's radio screenshot reports "Native Shay drip templates missing". The
reproduction log is preserved privately at build/shay-drip-bug/Player-repro.log.
It also contains repeated native Doppler.Awake null references and Unity's
duplicate DVConvertToEntity diagnostic at S060ShayEffects.Fit's audio clones.
The previous selection (including 0-10-0 Tank) is restored after refusal.

Confirmed cause: installed CCL's ObjectInstancerProcessor clones the native
CylCockWaterDripParticles system and ReplaceInstancedObjects substitutes that
object for the authored "Water drip N" reference. Its name therefore changes.
Matching display names in the 0.20.0 Shay fitter was wrong. Select the two drip
readers through their WATER_IN_CYLINDERS_NORMALIZED and cylinderCock.EXT_IN
ports instead; preserve the native gate definitions when making three copies.
Validate templates before starting cloning. No new aliases or CCL pack needed.

The second cause is nested cloning after activation. Doppler.Awake adds the
DisallowMultipleComponent DVConvertToEntity. Cloning the now-awakened LayeredAudio
subtree copies that converter, so the next native Awake cannot add its own and
dereferences null. Build the entire FX subtree under an inactive staging root,
complete every jet/audio/drip clone, then activate exactly once before binding
the existing presentation. Fit explicitly rejects an active hierarchy and never
activates a partially constructed tree. Keep the child controller's existing
"[Meyer engine effects]" name for its established teardown guard.

The original fixture missed CCL's replaced names and used empty LayeredAudio
objects without native Doppler components. Corrected the native drip fixture to
use processed native names. Added an actual Unity Play Mode regression with
native Doppler.Awake/DVConvertToEntity creation: reproduce the old duplicate and
exception, then exercise shipping Fit and twelve reactivations with exactly one
converter per fitted emitter and no activation errors. This test has 23 checks.
It seeds the native managed reader/audio configuration because the isolated
Editor's external-MonoBehaviour serializer does not copy those nested lists;
it does not claim whole-CCL import or audible mixer validation. Native archetypes
use an isolated ECS World. The absent game's conversion/removal queue and the
unbound fixture particle-controller teardown are skipped; actual Doppler
component creation and Unity Awake ordering run normally.
The new shay-effects receipt is mandatory in Test.Runtime/Build.Radio.

Only runtime FX construction changes. All arrangement IDs (including 0-10-0),
save versions, native wheel aliases, oil circuits, compound/traction calibration
and the three asset bundles retain their 0.20.0 meanings/bytes. This hotfix uses
the existing hash-bound bundle evidence; rebuilding model meshes is unnecessary.
Use build/radio-0.20.1 for final runtime receipts, build/nexus-0.20.1-single for
the single UMM ZIP and its exact-hash installer/bundle/install receipts. The
installation remains gated on DerailValley being closed, preserves settings,
and creates no backup as requested. Original 0.20.0 builds remain on disk but
are superseded. Gameplay acceptance of the repaired Shay remains pending.

Final native evidence: build/radio-0.20.1/runtime-audits-20261027-170730,
including cocks248 and the new actual Doppler Play Mode23; all original native
suites pass too. Runtime SHA256 9F21DE0E51DE0F03B149EBD71578B5C60861E9A95514D3B4BDE43D554215C419.
Managed/native release tests retain functional193, engine351, oil627 and the
other 0.20.0 checks. Exact ZIP native UMM extraction/update/migration passes57.

## 0.20.0: integrated two-truck Shay — 27 October 2026

James authorised completing the four R1 module sessions' transmission checks,
integrating the Shay into the existing S060 radio, reusing DV wheels, keeping
saves compatible and installing only while the game is closed. Persistent
preference: no installation backup. Vehicle targeting is unchanged.

Ninth radio/UMM choice, arrangement ID 7 appended; no separate spawned vehicle.
Complete native 01/02/02/03 S060 wheel halves and native distant LODs are aliased
live, with Y/Z radial factor 0.8 and unchanged X gauge. Four native PoweredWheel
objects register directly beneath the existing two native bogie_car transforms.
Original mechanisms, cylinders/readers are hidden, not destroyed; per-car R1
body relief/pipe mesh copies and all detached truck frames restore on exit.
Live painted materials track the S060 body. See docs/shay/INTEGRATION.md.

Native-first decision: B99 already supports three-cylinder ReciprocatingSteamEngine.
Use it with the existing stable main slot and an appended native RPM multiplier,
43/19 reduction and 0.92 transmission efficiency. No extra steam, adhesion or
force solver/dependency. Auxiliary slot is dormant in Shay. Native model bore
0.17 m/stroke 0.150395513 m matches the original kit. Measured native full-gear,
16-bar traction mean is approximately 13.7 kN at 1 wheel RPM and 12.6 kN at 80;
expect substantially less pulling force than stock, not guaranteed wheelslip.
Existing Meyer compound calibration and Dockside engines are untouched.

Visible shafts/joints/gears/wheels follow the kit's actual Hooke-joint solver.
Traction uses its average reduction. Compound poses have instantaneous phase
ripple (original study worst 8.6645/11.4457 degrees), not concealed exact CV
motion. Bevel flanks are a visual approximation, not certified tooth contact.
Whole assembly repeats at 43 engine/19 wheel turns. Native cylinder phase and
that full animation cycle are saved together and validated for agreement.

Selection schema 4 is Shay-only. Schemas 1–3, old arrangement IDs, all five
possible wheel-state slots and fourteen oil circuit IDs retain meaning. A
missing appended dormant engine circuit is accepted from pre-Shay saves;
malformed present state and invalid phase roll the whole engine slot back.
Eight axlebox cups, three engine big ends and a common bearing lubricator reuse
twelve existing servicing points; the last two stay dormant. The native
six/twelve-to-fourteen oil migration is unchanged.

Effects reuse ONLY the processed donor's native effects subtree. Three jets,
three independent water-drip readers and three native sound emitters replace
the donor pair; no spare truck scripts are cloned. Existing cab cocks, flow,
water gates, 120-degree phase curves and teardown ownership remain native.
Admission/exhaust tubes connect to boiler and smokebox; one chimney remains.

Original-only asset bundle: build/shay/bundle-20261027-014426/shay_running_gear,
SHA256 C5BE7FCDAA5BE70E2A6E067CF082FDC3191F5F8C7EBAB296E5445F915BBDF462.
Frozen fit-contract SHA256 3A6A104D1F12C49C3E399001E67F9F095C10EECD5AF2F39677AC44D70F37E8A7.
Original editable module sources remain B:/LLW CONVERT/locos/s060-shay; the
bundle contains no game meshes/textures/sounds/DLLs or new CCL car identity.
Build.Shay.ps1 copies the original kits into the ignored authoring project.
Keep its S060.Shay.ModelSource.asmdef: without it Unity creates Assembly-CSharp
and can shadow the actual native game assembly in offline audits.

The transmission's isolated Unity builder now compiles, reopens its scene and
validates all 43 references (transmission/evidence/unity-validation.json).
This supersedes the old module handover's Editor/licensing limitation. Added
combined triangle audit: 13,151 narrow-phase checks, zero unexpected contacts,
all four fixed hangers supported in all 49 poses x eight phases per truck.
Fixed hanger/casting seat contacts are intentional. No continuous sweep claimed.

Final native suite: build/radio-0.20.0/runtime-audits-20261027-015616.
Exact runtime SHA256 ACBE6C63F1D9C59F3118CF72CC9F53AFC10C2AF594D04521DAC55227B7C31965.
Passed exhaust611, cocks247 (including three-cylinder fitting/masks, repeated
cycles and independent drips), cleanup145, Meyer collision7062, native contact
PlayMode21, conventional+Shay native-mesh/motion505, quartering PlayMode115.
Managed/native checks: functional/save193, engine351, oiling627, axle100,
geometry1151, conversion35/14 failure scenarios, compound25, diagnostic145,
Meyer18 and Dockside17. Runtime compiles with zero warnings/errors.

Public output: build/nexus-0.20.0-single/RearrangedS060-0.20.0-UMM.zip.
One UMM root/Info.json, both unchanged 0.7.0 CCL donor bundles and the extra
Shay asset-only bundle; no nested archives. Actual UMM extraction, update,
settings preservation and old-three-part migration pass 57 assertions. Unity
reopens the extracted three bundles with 38 checks. Install.Nexus.ps1 is the
new deployment path; tests/Installer/Nexus.ps1 exercises its running-game gate,
tampered-package rejection, legacy removal and repeated settings preservation
with 18 fixture checks. Raw articulated-test.zip remains a legacy three-part
migration fixture, not the complete Shay installation; use the Nexus ZIP.

Installation uses Install.Nexus.ps1 with closed-game checks immediately before
mutation, exact receipt/hash checks and no backup. It preserves Settings.xml
and unrelated mods, retires the two old standalone donor folders after verifying
the replacement bytes, and clears only this runtime's generated DLL cache.
Inspect build/nexus-0.20.0-single/install-receipt-latest.json for final installed
hash/version evidence. Desktop copy goes in C:/Users/james/Desktop/Derail Valley Mods.
No push or Nexus publication is authorised/performed for this update.

Whole-car DV driving, sound balance, curve behaviour, gameplay performance and
radio operation remain for James's in-game acceptance. See the explicit Shay
steps at the top of docs/RADIO-ARTICULATED-TESTING.md. Do not describe isolated
Unity/native fixtures as a completed driving test or claim historical fidelity.

## 0.19.1: native quartering and oil-save migration — 25 October 2026

James authorized fixing these two upstream-review findings, preserving saves,
running the full checks and rebuilding the single Nexus ZIP. Vehicle targeting
was explicitly excluded. S060Parts/Targeting, save schema/arrangement IDs, engine
bank, compound calibration and both 0.7.0 asset bundles are unchanged.

S060NativeQuartering temporarily sets the two ORIGINAL drivetrain Animators'
keepAnimatorControllerStateOnDisable flags before articulated Apply hides them.
Undo restores the native mechanisms/readers before returning each original flag;
failed hierarchy restoration retains the owner for retry. Disposal is idempotent
and tolerates destroyed native components. No Animator.Play/Update/Rebind or new
per-frame hook is added. The authored Meyer/Dockside absolute-crank gear stays
unchanged. Native-first decision: S060 does not swap these native controllers,
so retaining their existing states is sufficient; the broader S282 controller/
phase-seeking machinery is unnecessary here.

Oil migration now hooks ManualOilingPoints.SetSaveStateData, the native per-
component boundary, and requires the existing owned-component weak-table entry.
Only recognised six/twelve-point arrays of objects migrate to fourteen. Each old
quantity/lid/index and unknown object property survives; only newly added points
start full/closed. Current fourteen-point, unknown-count, missing/non-array and
malformed arrays are passed through unchanged. Unsupported input is not repaired
or promised loadable by the native loader. Mod save version 3 and older v1/v2
compatibility are unchanged; there is no renumbering or state migration for wheels.

Full Build.Release passed with zero warnings/errors. Final exact-candidate native
evidence: build/radio-0.19.1/runtime-audits-20261025-132409, including exhaust611,
cocks95, cleanup145, collision7062, actual native contact PlayMode21, conventional
native mesh/motion437, and NEW actual Unity quartering PlayMode115. Quartering
fixture first reproduces the old disabled-state reset, then exercises the shipping
retention owner over sixteen hide/return cycles, all four original flag pairs,
forward/reverse playback, actual sampled wheel poses, repeated disposal and a
destroyed side. It is an isolated Unity scene, not a DV driving/whole-car test.
Release IL checks require retention before Hide, forbid animation-seeking calls,
and verify the per-component oil hook. One complete run timed out during the last
Unity fixture; its evidence is retained. Explicit unpaused/background fixture
settings were added and the entire release suite passed on rerun. An earlier
fixture state-name typo was corrected to the generated controller's actual name.

Other passing checks: functional/save185, engine/save/ECS241, oiling612,
same-frame axle100, native geometry1151, conversion35 over14 failure scenarios,
compound25, trace145, Meyer18, Dockside17, installer24. The added oiling cases run
production migration with the actual native loader, preserving old six/twelve
values/lids, fourteen-point round trips, foreign unregistered components and
unknown/malformed input. build/radio-0.19.1/oil-save-baseline.txt independently
compares the old and new DLL helper in isolated Windows PowerShell processes:
unknown fifteen points became fourteen in0.19.0 and remain fifteen with the same
reference in0.19.1. Do not compare both same-name assemblies in one LoadFrom
context: .NET Framework can return the first assembly and invalidate that fixture.

Single ZIP: build/nexus-0.19.1-single/RearrangedS060-0.19.1-UMM.zip.
ZIP SHA25679FC9ACF8E9806380AF5FC6C7292AD033DBD2F953EBBF15F4486D61876DA7269.
Runtime SHA25608CB2EAC7FA164CB97910C24185113CB318723E6742626940F052D081689938D.
Actual UMM fresh/update/uninstall/legacy migration54 passes at
umm-proof-20261025-132655; actual UMM-extracted bundles reopen in Unity with32
checks at combined-unity-20261025-132655. Asset bytes match the prior release.
Desktop delivery is RearrangedS060-0.19.1-Nexus, including friendly beta ZIP,
changelog, full Nexus copy, installation and credits. No game installation, save
edit or Nexus publication. In-game switching, wheel-pose observation, old-save
loading and the 0-10-0 driving/curve acceptance remain James's tests.

James also requested Shay feasibility and ready-to-use model-session briefs.
docs/shay/ contains the feasibility/index and four session prompts: fit contract
first, then independent engine, transmission and truck sessions. No Shay runtime
or asset is in0.19.1 and no new sessions were created. Explicit priority is maximum
DV wheelset reuse; native aliases are the default, with original bolt-on gear
rings/hub covers instead of replacement tread/flange/spoke meshes. Current080
physics is2+2 but its visuals remain rigid, and native support pivots differ from
the axle-pair midpoints; the first session must measure/freeze the actual contract.
Installed B99 engine constructor has an explicit three-cylinder lead1/3 branch
and six chuffs/cycle, so prefer native three-cylinder steam plus a measured geared
port graph before any custom solver. Gear ratio2.5 is provisional, not a Lima
specification. Historical primary references are linked in the feasibility file.

## 0.19.0 built for testing: 0-10-0 Tank — 25 October 2026

James requested implementation, build, source push and a Nexus-ready single ZIP
now, rather than a queued feature. New appended arrangement ID **6**, radio
choice 7, is **0-10-0 Tank**. Existing IDs/choices are unchanged. It uses five
real powered axles on the two native bogies (2+3), fixed **1.00 m drivers**, ten
big-end oil cups and a fitted five-boss coupling rod on each side. No extra
vehicle, engine simulation, native mass, resource capacity or gauge scaling.
The optional random-spawn entry defaults off.

The current 1.14m 0-8-0 wheels do not fit a fifth axle within both cab and nose
limits. The one-metre layout reuses native S060 wheels at the Dockside radius.
Rear axle centre is car Z-2.0, nominal pitch 1.0878m; the rearmost flange stays
forward of the Z-2.7 cab-entry station. Cylinder castings remain full-sized and
translate with their pipes, steam outlets and audio through the existing fitting
owner. Native direct-drive wheel animation remains; both added wheels follow its
crank. Shared conventional geometry retains the existing 0-8-0 behaviour.

Save compatibility: the original four wheel keys stay 0..3 and the fifth appends
as 4. Mod save version 3 carries the fifth condition/selection; versions 1 and 2
remain readable, with unknown/corrupt payload preservation retained. Old mod
versions cannot restore the new arrangement. The original twelve oil circuit/
port IDs remain at their old indexes (including right-side 9..11); new left/right
fifth-axle reservoirs append at 12/13. Native six- and twelve-point saves pad to
fourteen without changing existing quantities or lids. Existing compound model,
engine bank, calibration, resource and native identity handling are unchanged.

Full Build.Release passed against the exact candidate, with zero runtime build
warnings/errors. `build/radio-0.19.0/runtime-audits-20261025-000252` records
exhaust611, cocks95, cleanup145, collision7062, native PlayMode contacts21 and
**conventional437**. The new audit executes candidate geometry, rod swaps and
fittings using local native mechanism meshes/transforms, checks tyre/body/cylinder
triangle clearance through eight forward/reverse crank poses, every fitted boss,
gauge, cylinder size, ten cup sites and repeated geometry restoration. Native
four-axle static regression also passes; native previews were inspected.

Other passing checks: functional/save185, native engine/save/ECS241, oiling564
(including old twelve-point migration and fifth-axle masks), axle100, geometry1151,
conversion35 across 14 injected failure cases, compound25, trace budget145,
Meyer18/Dockside17 and installer24. Existing 0.7.0 bundles remain byte-identical;
their earlier hash-bound authoring evidence is reused.

Fixture lesson: an initial whole-mesh bound included unused vertices after face
selection; compacting the selected native casting fixed the fixture, not the
runtime. The first exhaustive sweep timed out, then conservative per-wheel
triangle pruning and caching Unity mesh arrays removed repeated whole-array
copies. No contact assertion or release gate was removed. Final geometry audit
passes within the normal timeout. Native fixture exports stay ignored and are
not part of either Git source or the release archive.

Single UMM package: `build/nexus-0.19.0-single/RearrangedS060-0.19.0-UMM.zip`.
Actual UMM fresh/update/uninstall/legacy migration: **54** passing assertions at
`umm-proof-20261025-000553`. Unity reopens both actual extracted CCL bundles:
**32** assertions at `combined-unity-20261025-000553`. Public files are copied to
Desktop/Derail Valley Mods/RearrangedS060-0.19.0-Nexus, including the friendly
`Rearranged S060 0.19.0-beta.zip`, full Nexus copy and separate changelog.
One mod folder/Info.json, both nested bundles, no nested ZIPs. Normal single-folder
0.18.0 updates retain settings; older three-part installs use the documented
one-time removal of only the two standalone asset packs through UMM.

This is a tested public beta candidate, not full driving acceptance. James still
needs to test curves/points, forward/reverse running, oil-can/lid operation,
wheel damage, save/reload and switching through stock/080/Meyer/Dockside before
uploading. No game was launched or installation changed. Source push is explicitly
authorized; Nexus upload/publication remains James's action.

## Corrected Nexus release: one directly installable UMM ZIP — 23 October 2026

James superseded the deferral and requested the combined release now, with no
nested ZIPs. The previous ZIP-in-ZIP draft was the wrong delivery format and
is superseded. Version remains **0.18.0 initial public beta**, since this is a
packaging correction to an unpublished draft, with the same tested DLL and
both 0.7.0 bundles. No simulation, save schema, car/livery ID or game code changed.

Canonical release: `build/nexus-0.18.0-single/RearrangedS060-0.18.0-UMM.zip`.
SHA256: `1EFE740BA53A74B59CEED6026FE09C0C41A5FC895385748FB85B7ED78C16C8DD`.
One `RearrangedS060/` root and Info.json contain the runtime and native CCL
bundles under `assets/Meyer/ccl_bundle` and `assets/Dockside/ccl_bundle`, plus
readmes, INSTALL.txt, retained MIT licence and credits. No nested archives,
extra DLLs, custom installer, merged asset bundle or new loading hook.
Info.json now includes the normal DVCustomCarLoader dependency alongside LMS.

Native evidence: installed CCL 3.1.11 recursively scans ccl_bundle under each
active mod entry, and resolves these same donor IDs. CCL's appended Save callback
saves to CCLPlugin.Instance rather than the S060 entry, so it does not replace
S060's settings. These exact installed IL paths are checked in the new tests.

`build/nexus-0.18.0-single/umm-proof-20261023-144912/native-umm-validation.json`
passes **54 assertions** against actual UMM 0.32.4 reader/install/update/uninstall
methods: single identity/root, no nested ZIPs, all ten file hashes, fresh install,
repeat update, settings/unrelated-mod preservation and native legacy migration.
Migration follows standard UMM: uninstall ONLY the two old standalone asset-pack
entries, keep RearrangedS060/settings, then install the combined ZIP. UMM does
not remove sibling mods automatically. The fixture proves exactly two bundle
copies remain, instead of duplicates; no real game folders were altered.

`build/nexus-0.18.0-single/combined-unity-20261023-144912/combined-package-validation.json`
passes **32 Unity assertions** reopening both actual UMM-extracted nested bundles,
retaining their canonical IDs, both powered-frame prefabs, animations, fitted
markers and axles, and no bundled native/Verde audio or native S060 meshes.
Bundle SHA256 values match the tested package exactly. This is native Unity
bundle reopening/serialized inspection, not full CCL world import or driving.
The sole editor warning is the pre-existing S060SteamPipes.destroying CS0414.

Public download, install instructions and Nexus copy now say to drag the single
ZIP straight into UMM; the one-time old-pack migration and existing beta gameplay
limitations remain explicit. The Desktop release copy uses James's friendly
filename `Rearranged S060 0.18.0-beta.zip`; its old nested-archive contents were
verified against the old receipt before replacement. The old autogenerated
Desktop nested ZIP was removed after hash verification. Old canonical build
evidence remains ignored for traceability. No game installation, push or Nexus
publication was performed.

## Prepared initial Nexus release draft — 23 October 2026

Requested release copy and download prepared as **0.18.0 initial public beta**,
with both 0.7.0 asset packs. Runtime DLL and both bundles remain byte-for-byte
identical to the tested 0.18.0 package below. No game installation changed and
nothing was uploaded, pushed or published. Outstanding gameplay confirmation of
the transition-damage/stationary-bob issue is explicit in the draft beta notes.

`Package.Nexus.ps1` validates the tested package/native receipts, then creates
`build/nexus-0.18.0/RearrangedS060-0.18.0-Initial-Beta-UMM-Bundle.zip`. This outer
download contains INSTALL.txt and three individually UMM-installable ZIPs. Each
inner ZIP has one canonical mod folder/Info.json, original payload bytes, a
public README, retained LICENSE and updated CREDITS.md. No Settings.xml, game
DLLs, third-party dependency binaries, diagnostics or editor/source fixtures ship.
Public text lives in `docs/nexus/`; the full Nexus copy includes description,
initial feature changelog, main/inner file descriptions, credits and permissions.

Native-first packaging finding: UMM reads one mod identity per archive. Its
installed-mod folder rename in InstallMod collapses all sibling folders of the
old three-folder archive into the first mod on an update. Do not advertise the
old articulated-test.zip as a safe one-drop UMM updater. Users extract the new
outer download and drag all three inner ZIPs into UMM together. No custom
installer, hook or combined mod identity was added.

`tests/Nexus/NativeInstaller.ps1` runs the actual installed UMM 0.32.4 native
ReadModInfoFromZip/InstallMod methods in .NET Framework against isolated scratch
folders, without constructing/showing its form or targeting the real game.
`build/nexus-0.18.0/umm-proof-20261023-122920/native-umm-validation.json` passes
46 assertions: outer contents and extracted inner hashes, fresh install, repeated
update, all 15 payload/document files, settings/other-mod preservation and the
old-layout update negative control. Native UMM copies stay in ignored evidence.

Current credit spelling follows James's latest request and his S282 Nexus copy:
**discorddraconequus**. Credit is for Verde-derived stage-specific chuff/sound
tuning and reference patterns, not redistributed recordings or artwork. The MIT
fork lineage is Peter Schinske / pjschinske / SatiricRug -> JimTaplin's S282 B99 /
Re:Arranged fork -> Rearranged S060. Original licence bytes remain unchanged.

## Current installed transition/effects correction: 0.18.0 / packs 0.7.0 — 23 October 2026

Installed and hash verified at 10:53 UK / 09:53 UTC, game closed and checked again
immediately before copying. **No backup created**, per James's instruction.
Eleven installed files and five protected files verified. Receipt:
`build/articulated-0.18.0/install-receipt-latest.json`. Runtime SHA256:
`A15190B3B9C52EB2160FD871F285E2792E082FC1C55638442C13E7871D5CF3C1`.

The LP return now rises behind the cab, follows its roof edge at Y3.85 and drops
ahead of the cab to the existing smokebox run. Measured roof brackets and forward
footplate supports stay connected. Body ball is at (-1.63,3.05,-4.6); fixed sleeve
halves are 1.18m. Drawn pipe triangles clear the cab-side entry volume through the
tested poses. Single chimney, existing LP exhaust/simulation routing and packs
0.7.0 remain.

Cock investigation distinguished evidence from symptoms. Preserved Player.log,
120-sample L-021 trace and video frames are under `build/defects-0.17.0`.
That trace used 7.14% regulator; rear LP chest stayed at 1.000–1.086 bar absolute
and rear cock flow peaked at 0.006739493, below native jets' 0.01 threshold. It
does not prove failed rear jets. Cylinder water was not recorded, so absent drips
cannot be diagnosed from it. Native wet-drip tests confirm water plus the cab
control are required; no simulated water or forced steam flow has been added.
Separately, the installed DLL reproduced a real inlet-publication defect: native
crank events precede the inlet mask, and the reader did not refresh on inlet-only
changes. Owned timing now observes that mask too, using the actual crank while
retaining native curves, flow thresholds and teardown ownership.

Confirmed physics defect: rear shell colliders were continuously repositioned
inside the main car's automatically computed compound mass frame. Their proper
owner is the **existing native rear bogie Rigidbody**, with fixed local geometry.
The earlier requirement to retain car-compound ownership was our assumption, not
James's identity/save requirement. Native rb/readiness/derail policy now gates
the shell, preventing fallback into the main body while native physics is absent.
No extra Rigidbody, COM/inertia pin, damage suppression or physics freeze was
added. The bogie retains native mass/FreezeRotation; its own automatic mass
distribution reflects the fixed shell, while the main car's frame is preserved.

The bogie had no native collision dispenser. A small owned relay uses the native
dispenser on that Rigidbody and forwards only shell contacts to the existing car
dispenser, preserving native impact/pause handling. Explicit cleanup precedes
parent deletion; direct-parent destruction and same-frame rebinding are tested.
No custom damage rules or global collision patch is installed.

The exact cause/resolution of the reported Meyer bob and Meyer->Dockside window
damage remains **unconfirmed in gameplay**. The attempted suspension/stress
fixture had an unstable stock/no-relocation control and was quarantined outside
release authoring; it is not reproduction evidence. Native stress uses its own
joint. Do not dismiss repeats as DV weirdness or claim this fixture proved a cure.
New bounded traces re-arm per changed arrangement, include the first three
seconds at closed regulator, and add water/temperature, physical unit mapping,
health/windows, body/bogie pose/mass/COM/inertia and joint/stress data. Existing
schema1 fields remain; the immediate selection record is inside conversion and
later settling samples show its aftermath.

Full `Build.Release.ps1` passed, zero runtime warnings/errors:

- Meyer `build/meyer/authoring-20261023-085857`: 96,555 return checks across 18
  poses/72 crank samples, actual cab-entry clearance and roof seats. Max drawn
  endpoint gap 0.0281mm; minimum sleeve overlap 511mm. Assembled previews reviewed.
  Matching unchanged Dockside evidence: `build/dockside/authoring-20261022-223705`.
- `build/radio-0.18.0/runtime-audits-20261023-095142`: exhaust611, cocks95,
  cleanup145, collision7062; actual Unity PlayMode contacts21 covers native
  Enter/Stay/Exit, pause flag, owned filtering, teardown, same-frame rebinding and
  exactly-once parent-destruction cleanup. The EditMode audit no longer makes
  unsupported runtime OnDestroy claims.
- Functional173, native engine/save/ECS241, oiling527, axle100, geometry1151,
  conversion35 across 14 injected scenarios, compound25, standalone Meyer18/
  Dockside17, trace budget145 and installer24. Reports bind to the exact DLL;
  totals include repeated comparisons and do not imply a full driving test.

Compound model/stage, engine bank, calibration and saved identity/state sources
match `8cc2abc`: `build/radio-0.18.0/simulation-save-contract.json` names seven
unchanged files. `S060ArticulatedSimulation.Select` adds only a passive trace
notification after exhaust refresh. No save migration, resource change or repair
of the already damaged L-021 was performed.

Next acceptance: record a healthy engine's condition/windows, stop and safely
switch Stock->Meyer->Dockside->Stock, waiting three seconds each; repeat on a
gentle curve and from a loaded Meyer save. Build LP pressure before evaluating
rear jets and record actual cylinder water when evaluating drips. Retain both
arrangements' new trace files plus Player.log after any failure. No game launched,
no save edited, no push/publication. Reviewed source/docs/tests preserved locally.

## Current installed LP exhaust return: 0.17.0 / packs 0.7.0 — 22 October 2026

Installed and hash verified at 23:38 UK / 22:38 UTC, with the game closed and
checked again immediately before copying. **No backup created**, as requested.
Eleven installed files and five protected files verified. Receipt:
`build/articulated-0.17.0/install-receipt-latest.json`. Runtime SHA256:
`93DC8ABB5A87E06E3A80BBC6251ECBBA71EA26F76CB4532F43A6078B546476EB`.

Meyer keeps one chimney. New runtime-only `S060MeyerExhaustPipe` connects both
actual rear LP valve-chest rear caps to a collector, then a dedicated large
return to the native smokebox. It uses the live S060 material and follows material
replacement. Two ball joints and fixed 1.55m sleeve halves articulate with variable
overlap; no radial scaling. The body run is at Y2.0, supported from the actual
native plate underside at Z-1/0/+2.75; rear collector stays at Y1.68. The inlet
seats at X-.656/Y2.25/Z3.70. Existing packs remain 0.7.0 because no authored
vehicle assets changed. Existing admission/interstage pipework remains separate.

Ownership belongs to the existing `S060SteamPipes` update/dispose/OnDestroy path,
only for Meyer. No new component tick, collider, Rigidbody, steam port, sound or
particle reader. Failed partial binding and both live/destroyed-parent cleanup
release owned geometry without destroying native meshes/materials. Extreme
derailment can exceed physical sleeve travel; the rigid pieces remain at their
ends without stretching or throwing each LateUpdate. Normal articulation is
validated below; arbitrary derailment connectivity is not claimed.

Compound model, engine graph, calibration and save-contract sources match
`15b2a92` exactly: `build/radio-0.17.0/simulation-save-contract.json`. Fuel/water,
damage, native identity, save schema and LP chimney/chuff routing are unchanged.

Complete `Build.Release.ps1` passed with zero runtime warnings/errors:

- Meyer `build/meyer/authoring-20261022-223620`: integrated pipe report includes
  86,761 LP-return assertions across 18 body/bogie/yaw/rerail poses and 72 crank
  samples. Actual cap centres, native smokebox surface, sleeve engagement,
  body/step/deck clearance for moving sleeves, all exhaust-vs-admission contacts,
  running wheels/rods, supports, materials and disposal passed. Max endpoint gap
  0.0393mm; minimum sleeve overlap 366.2mm. These are repeated comparisons,
  not that many scenes. Left-view ±15° assembled previews were inspected.
- Dockside `build/dockside/authoring-20261022-223705`: existing 423 pipe checks
  passed, no Meyer return created. Both reopened bundles passed existing effects,
  motion, pin/rod, oil-cup geometry and lamp (Meyer) audits.
- `build/radio-0.17.0/runtime-audits-20261022-223721`: exhaust611, cocks56,
  cleanup145 (including new candidate-DLL return lifetimes/material replacement,
  failed binding and overtravel), collision6942. Functional173, native engine/
  save/ECS241, oiling527, repeated axle getter100, geometry1151, conversion35 over
  14 injected scenarios, compound25, standalone Meyer18/Dockside17 and installer24.
- Packaging now requires the new hash-bound return-fit report and candidate-DLL
  lifetime result; old passing evidence cannot qualify this runtime.

Investigation lesson: first strict sweep found a rear support/intake contact and
the bracket moved forward. A later whole-mesh error was incorrectly inferred to
mean the smokebox bend; shifting the inlet did not fix it. Exact intersecting
triangle coordinates proved the straight lower run touched the swung front
manifold near Z2.1. Raising that run and routing supports resolved it without
extra width or relaxed checks. The Z3.70 inlet was retained for its separately
verified cleaner native skin connection, not as the collision fix. Future mesh
contact investigation should localize actual faces before changing the route.

Manual game acceptance remains: hot operation and left/right curves, save/reload,
radio switching/removal, streaming and visual clearance in actual gameplay.
The new audit does not claim continuous sweeps or oil-cup interaction coverage.
No game launched, no save edited, no push or publication. Source checkpoint is
local; canonical packages/build evidence retained, no rollback copies created.

## Current installed effects update: 0.16.0 / packs 0.7.0 — 22 October 2026

Installed and hash verified, game closed and checked again before copying.
**No backup created**, per James's current instruction. Receipt:
`build/articulated-0.16.0/install-receipt-latest.json`. Eleven installed package
files and five protected files verified; native settings and other mods retained.
Runtime SHA256: `845129C2B78F2CF27A38C317E5202AEC4CA05F826CA49C613A1E66D04DACC05A`.

Verde reference: the supplied Verdpack Unity package was inspected read-only in
memory. No Verde meshes, clips, textures or audio were imported. Native effect
ownership, per-cylinder timing and native absolute animation patterns informed
this work; discordraconequus is credited in the installed CREDITS.md.

Steam holders now meet the actual drain-valve cap centres. Meyer front uses
measured local native body mesh data; custom cylinders use their actual cap
bounds. Water drips sit on rear drains and hiss sources between the drain ends.
Both frames carry an effects-0.7 marker; preflight rejects older packs before
dismantling the previous selection. Existing fitted-gear-0.6 markers remain
because wheel geometry has not changed.

B99's CylinderCockParticlePortReader subscribes the same phase-valued callback
to crank, reverser and flow. A flow/reverser-only update can therefore substitute
that value for the real crank phase. S060CockTiming replaces those three delegates
only on radio-owned articulated readers with wrappers reading the actual crank.
Native curves, inlet gating, flow, reverser logic and sound gain remain native.
Disposal restores native ownership before teardown; teardown-first disposal does
not resurrect callbacks. The legacy standalone CCL debug presentation is outside
this correction's scope; supported product operation is the native S060 radio.

Verde's reversing clips target its own armature. Current Meyer rods and Franklin
drive shafts have no modeled variable-cutoff actuator; no false neutral/reverse
blend was added. Absolute own-unit crank motion is preserved. New motion checks
cover negative/multi-turn seeking, arbitrary seek order, quartering, endpoint and
seam continuity, and independent front/rear clocks.

Full Build.Release passed with zero runtime compile warnings/errors:

- `build/meyer/authoring-20261022-215949` and
  `build/dockside/authoring-20261022-220008`: 177 drain-fit/binding assertions each,
  including bogie/body yaw, maximum outlet gap 0.00000763m. Required new report:
  `steam-effects-validation.json`, bound to each reopened bundle's hash.
- Motion: Meyer 22,789 pose plus 5,761 seek/isolation assertions; Dockside 24,421
  pose plus 8,447 seek/isolation assertions. Counts are repeated sweeps, not scenes.
- `build/radio-0.16.0/runtime-audits-20261022-220025`: 611 exhaust/audio,
  56 native cock (including reproduction-before-fix and both disposal orders),
  117 cleanup and 6,942 collision assertions.
- 173 functional, 241 native engine/save/ECS, 527 oiling, 100 repeated axle getter,
  1,151 geometry, 35 conversion assertions over 14 injected scenarios, 25 compound,
  18 Meyer and 17 Dockside native assertions; 24 installer fixture assertions.
- Compound model, native engine graph, calibration and save-state sources are
  unchanged from 0.15.0: `build/radio-0.16.0/simulation-save-contract.json`.

Manual playback/driving acceptance remains: hot stationary flow/reverser cycling,
sound location/balance, articulated drain placement while moving and streaming,
and forward/reverse motion through the loop seam. No game was launched by Codex,
no saves edited and nothing pushed or published.

## Previous installed cleanup: 0.15.0 — 22 October 2026

**Installed and hash verified: S060 0.15.0, Meyer/Dockside 0.6.0**, at 22:44 UK /
21:44 UTC. Game closed and checked again immediately before copying. **No backup
created.** All 11 installed package files and five protected files match their
receipts; settings, S282 DLL/bundle, LocoMeshSplitter and probe are retained.
Receipt: `build/articulated-0.15.0/install-receipt-latest.json`.

User authorized remaining audit cleanup, save compatibility, full testing and
installation only with the game closed, **without a new backup**. The old backup
instruction below is superseded. Twelve S060 install-backup folders and five
independent S060 source/bundle/restore copies were deleted at the user's request;
receipts: `build/cleanup-0.15.0/backup-removal.json` and
`build/cleanup-0.15.0/independent-backup-removal.json`. Canonical source/Git history,
the live installation and canonical builds remain. Historical backup paths below
describe past actions and are no longer recovery locations.

M01 oil-cup responsibilities split between topology, interactable/controller
ownership and geometric mounts. M02 engine roles use shared semantic mappings
while stable native IDs, old aliases and trace/save JSON remain unchanged.
M04 runtime and authoring share calibration with exact legacy float/double values,
including the existing one-ULP authored/runtime Dockside stroke difference.
M05 incomplete cleanup retains ownership and native attachment frames for retry;
missing donors are rejected before dismantling the old arrangement. This is
best-effort recovery, not a claim of atomic geometry after arbitrary failures.
M06 moved 74 inactive source/resource/script files byte-for-byte to
`archive/legacy-runtime` with a verified manifest. Current build is explicit and
only supports Stage7; Build.Runtime contains the real builder. Save versions,
port/circuit IDs, livery/resource/wheel/damage identities and tuning are retained.

Fresh complete Build.Release run passed:

- Runtime: `A9DEEB25495A5A3B1AD5A8308B2A9FCF37B9D3E514216B6AD671404B1022CB7C`.
  Zero compile warnings/errors.
- `build/radio-0.15.0/runtime-audits-20261022-213928`: 611 exhaust/audio,
  41 cock, 117 cleanup and 6,942 collision assertions. The eight new cleanup
  assertions use the shipping articulated Undo with a transient lamp-owner
  disposal failure, actual native Transform restoration and retained frame.
  Physics is explicitly skipped in that failure fixture.
- 173 functional, 241 native engine/save/ECS, 527 native oiling, 100 repeated
  same-frame axle-getter, 1,151 geometry, 35 conversion-boundary assertions over
  14 injected scenarios, 25 compound physics, 18 Meyer native and 17 Dockside
  native assertions. Conversion scenario discovery/scene services use test
  doubles; these are not fourteen live native-world tests.
- Installer: 24 assertions over three isolated fixture scenarios (running-game
  rejection, tampered-package rejection, no-backup install/protected hashes),
  `build/installer-proof-20261022-213927`.
- Fresh reopened packs: `build/meyer/authoring-20261022-213840` and
  `build/dockside/authoring-20261022-213914`. Motion, rods/pins, pipes, oil cups,
  native aliases and Meyer lamps pass all required hash-bound reports.
- Complete package: `build/articulated-0.15.0`, packs retain version 0.6.0.

Local source checkpoint: `2d3baf5e02c7b296803a6b97f2708eb573009309` on the existing
`rearranged-s060` branch. This present-day checkpoint includes the previously
uncommitted S060 implementation as well as this maintenance pass. Existing Git
history/preservation refs were retained. Nothing pushed or published.

Clean Git source export of runtime/build-helper dependencies compiled with zero
warnings/errors using the documented local SDK/game inputs. All executable IL,
type and field definitions match the tested candidate; binary SHA differs with
build/debug provenance. Proof:
`build/cleanup-0.15.0/clean-source/clean-source-validation.json`.
The temporary source copy/ZIP was removed; its build DLL/log/receipt remain.
The complete authoring/release pipeline was tested in the main checkout, not
repeated in a wholly fresh clone with a newly imported CarCreator project.
Save/selection/engine-state/oiling/wheel-condition source files are identical to
the 0.14.0 preservation checkpoint; the 241 native tests also cover legacy JSON
compatibility and unchanged semantic mapping/calibration. Gameplay acceptance
remains pending.

## Previous installed audit fixes: 0.14.0 — 22 October 2026

**Installed and hash verified: S060 0.14.0, Meyer/Dockside packs 0.6.0.**
Installed at 20:54 UK / 19:54 UTC with the game closed, checked before backup and
again immediately before copying. Host process checks and Unity runs used normal
host permissions; sandbox-only process enumeration did not expose host Unity.
Receipt: `build/articulated-0.14.0/install-receipt-latest.json`.
Backup: `C:\Users\james\Desktop\Derail Valley Mods\RearrangedS060\installed-backup-0.13.1-20261022-195432Z\mods`.
All 18 previous files in the three mod folders were backed up and hash verified;
11 package files installed and hash verified. All five protected files retained:
S060 Settings.xml, S282 DLL/source bundle, LocoMeshSplitter DLL and probe DLL.
No game launched, saves edited, public release, Git commit or tree cleanup done.

Final runtime SHA256:
`6D5B365C31A054B0381912C57D6D19741A6AE14CE42697672C868B6816920146`.
Both normal default project build and explicit Stage7 build produce this DLL,
with zero warnings/errors. The project now defaults to the supported Stage7.

### Corrected boundaries

- Native chimney initialization occurs inside SimController.Initialize before
  our owner postfix. Enroll already initialized readers after publishing the
  owner; retain Init/Deinit hooks for later readers. Regression starts smoke
  before owner registration. Meyer chimney/chuffs remain LP-sourced; Dockside
  retains independent simple-unit pulses and pressure gates.
- Native TrainAudio can be detached under car.interior. Discovery now includes
  that root, placement survives late SetupForCar, and ReturnToPool restores
  original parents/poses. Only native source holders move. Stock cock muting
  covers control as well as flow to suppress native moving cold-air hiss;
  articulated unit copies retain their own live readers.
- Select oil topology synchronously when a conversion commits or returns to
  stock/release, before delayed cup construction. Cup geometry no longer owns
  the active mask. Newly initialized stock flows activate their extra big ends.
  Dormant circuit oil/lid/save values remain intact.
- Native PrepareForDestroy attempts normal release while the hierarchy exists.
  Direct destruction has a mesh-only fallback that removes conversion ownership
  and disposes generated rods/body pipes/rear lamp-buffer meshes idempotently.
- Meyer rear colliders follow the actual articulated rear frame in physics and
  render updates while remaining beneath the native car collision root. They
  retain the car Rigidbody, material and layer; removal disables them immediately.
  Donor frames are validated before dismantling the prior arrangement.
- Version1 engine JSON uses an explicit cached, checked native field map;
  legacy names/order/schema remain identical. Shared production calibration
  retains existing precision, dimensions and tuning.

### Final validation evidence

`Build.Release.ps1 -SkipAuthoring` completed against these fresh exports:

- Meyer: `build/meyer/authoring-20261022-195230`, bundle SHA256
  `9CDD9B70602B8834498DC74103188D0A41B65026FC3B27BF69297BC20B59BD87`.
- Dockside: `build/dockside/authoring-20261022-195249`, bundle SHA256
  `C577111E6F876E57BB0EFC41CC3C11C595019C243E6DF586D517F7B64B16DC0B`.
- Final native lifecycle suite:
  `build/radio-0.14.0/runtime-audits-20261022-195319/runtime-validation.json`.
  611 exhaust/audio, 41 cock, 109 cleanup and 6,942 collision assertions passed
  against the shipping candidate. Collision sweeps use 90 pose/cycle combinations
  with corner/body-ownership checks, not 6,942 separate driving scenes.
- Package regressions: 173 functional, 223 native engine/save/ECS, 501 oiling,
  100 repeated same-frame axle-getter, 1,151 measured geometry, 16 Meyer native
  and 17 Dockside native checks. Separate pure compound suite: 25 passed.
- Both reopened bundles pass all required motion, pin/rod fit, pipes, oil-cup and
  native-alias checks; Meyer lamp checks pass. No native game meshes/audio baked
  into the distributable bundles. Art dimensions/tuning were not intentionally
  changed in this audit-fix build; pack versions remain 0.6.0.
- Evidence tampering proofs: healthy fixture accepted; changed bundle, changed
  report, missing canonical report and replacement before receipt rejected.
  `build/script-proof-533df91d4c8e4fc295ceccee5887e438/negative-validation.json`.
  Staging guards reject extra files/subdirectories. All core/asset receipts are
  canonical manifests; native reopening reports bind the exact bundle SHA256.

`Build.Config.ps1` / optional ignored `Build.Local.json` centralize paths.
`Prepare.QA.ps1` regenerates native fixtures under ignored build/qa instead of
relying on older version directories. Current commands are Build.Runtime,
Build.Release and Test.Runtime; Test.Exhaust forwards to the complete suite.
Prototype package commands are retired with explicit replacement messages.
See BUILDING.md and AUDIT-2026-10-22.md for scope and remaining maintainability
items. A clean clone has not been proven: inherited untracked implementation
must be preserved and committed before it can exist in a clone.

**Gameplay acceptance remains pending:** first-load hot Meyer/Dockside smoke and
chuffs, hot/cold cab-cock cycling after audio streaming, oil-can/lid interaction,
Meyer contact on curves/rerail and longer deletion/pooling sessions. Editor/native
fixtures do not establish audible balance, full-scene physics or long-session
memory. Use RADIO-ARTICULATED-TESTING.md. All requested runtime audit defects F01–F08
are addressed; broad source archiving, semantic slot-name cleanup and further
authoring calibration consolidation remain documented follow-up work.
Verde credit is **discordraconequus**, including the installed CREDITS.md.

## Historical handover

The original handover began with Claude on 20 October 2026, at commit `cbbdcfe`
(version 0.3.0). The following entries record earlier states and evidence.

## Previous exhaust correction — 22 October 2026 (superseded by 0.14.0)

**Installed and hash verified: S060 0.13.1**, unchanged Meyer/Dockside **0.6.0**
bundles. Installed 22 October at 10:57 UK, game closed and checked twice.
Receipt: `build/articulated-0.13.1/install-receipt-latest.json`.
Backup: `C:\Users\james\Desktop\Derail Valley Mods\RearrangedS060\installed-backup-0.13.0-20261022-095755Z\mods`.
Eleven package files and five protected files verified; settings, S282 DLL/source
bundle, Loco Mesh Splitter and probe retained. No game launched, saves edited or
public release performed. Core package includes CREDITS.md.

James supplied discordraconequus's Verde archive with the author's invitation to use
it as a compound template. Its serialized native HP/MP/LP controls, lower LP
chuff pressure bands (3/4.5 bar) and LP chimney source informed this correction.
No Verde art/audio copied. CREDITS.md is now included in the core package;
both asset README files also credit the reference. Do not blindly copy Verde's
pressure-to-quality connections or assume its serialized graph was runtime
validated. The local read-only archive inspection is under
`build/verde-reference-20261022` (ignored, not packaged).

Confirmed defect: native S060 chimney/chuff readers remained bound to
`steamEngine`, which is HP in Meyer mode. LP occupies `rs060FrontEngine`
(the name is a stable logical slot, not its physical location). S060ExhaustPresentation
now rebinds the original native readers to LP including frequency, water and ash
inputs; original IDs and pressure bands restore on deinit/stock selection.
Local cylinder-cock readers remain independently bound to their own stages.

Dockside retains the original native chuff reader for one simple engine and
creates a second reader from the authored native audio subtree, following the
working S282 precedent. Mutable curve/loop/config objects are independently
owned; clips remain native references. The single original smoke reader receives
both engines' events, each gated by that engine's own exhaust pressure. A local
presentation-only pressure port exposes max(P1,P2), not summed absolute pressure.
Events call the native handler directly: a synthetic change-only event port
would incorrectly collapse equal indices from the two units. No simulation
graph, resources, engine calibration, saved schema or bundled geometry changed.

Native reader Init/Deinit hooks cover streamed and detached audio instances.
Mode changes rebind tracked live readers. Deinit removes owned callbacks,
stops cloned audio, clears native smoke coroutines and restores cached particle
baselines before re-enumerating inactive leaves. Native LayeredAudio.Stop alone
retains its cached volume input; call native Reset as well so unchanged input can
restart stopped loops. Extra-reader failure cleanup never calls Deinit before
its Init completed.

`Test.Exhaust.ps1` builds Stage7 and runs the production Harmony hooks with
installed B99 readers in the isolated hidden Unity editor. Final evidence:
`build/radio-0.13.1/exhaust-native-20261022-095556` — **584 checks pass**,
12 Stock/Meyer/Dockside cycle sequences plus detached interior readers,
subscription cleanup, equal-index pulse forwarding, separate hot/cold unit gates,
actual native chimney coroutine starts and native loop cache reset. Exact runtime
SHA256 `E280B447F193D2072CA297E710F8ADC5A0B19E84487249C2CEFBB9C96CF1468F`.
Build.Radio now requires this audit to match its exact DLL hash. Initial test
fixtures exposed reflected editor component clones losing nested serialized
configuration; explicit authored configuration copying and fresh mutable objects
avoid relying on that clone state. All final audit assertions remain enabled.
Final Stage7 build: zero warnings/errors. Existing regressions rerun successfully:
173 functional/save/selection, 193 native engine/ECS, 100 axle transitions,
1,151 measured geometry, 212 native oiling, 16 Meyer and 17 Dockside native
engine checks. Reused unchanged 0.6.0 bundle geometry evidence. Existing 41 native
cock checks remain applicable; cylinder-cock code and authored assets unchanged.

The audit uses an inert managed car event source and real native ports/readers;
it does not load the game world or validate audible mixing. Before public release,
James must listen to both arrangements under load, watch the chimney through
switches/coasting, and repeat streaming/save-reload. The 0.13.0 running-gear,
oil-can/lamp-control gameplay checks also remain pending.

## Current polish revision — 21 October 2026, late evening

**Installed and hash verified: S060 0.13.0 / Meyer and Dockside 0.6.0.**
Installed 21 October at 23:31 UK with the game closed (checked twice).
This section supersedes earlier current headings. Receipt:
`build/articulated-0.13.0/install-receipt-latest.json`.
Backup: `C:\Users\james\Desktop\Derail Valley Mods\RearrangedS060\installed-backup-0.12.0-20261021-223125Z\mods`.
Ten installed package files and five protected files hash verified; existing
settings, S282 DLL/source bundle, mesh splitter and probe retained. No game
launched or save edited. Runtime SHA256:
`F8E677D156E2125BDDA6689FDD952665ED191ADA5AA45001ECD3A969B6209396`.

Final exports: Meyer `build/meyer/authoring-20261021-222629`, Dockside
`build/dockside/authoring-20261021-222710`. Preview and reopened-bundle checks
pass: motion 17,028/15,972; pipes 423 each (maximum face gap .0325 mm);
native pin fits 68 each; supported oil cups 14,719 each; conventional rods
3,120; Meyer lamps/buffers including native LOD restoration 7,596.
Runtime build has zero warnings/errors. Functional 173, native engines/ECS193,
axle transitions100, measured geometry1,151, native oiling212, separate native
Meyer16/Dockside17 and native cock lifecycle41 checks pass. Fresh cock evidence:
`build/radio-0.13.0/cock-native-final` (hidden licensed editor; batch-mode
attempt refused licensing and did not execute the audit). Complete in-game
acceptance remains pending: native oil-can/lid interaction, lamp switches/LODs,
full running-gear animation and saved/coupled Meyer reload. See testing guide.

James tested 0.12 hot and cycled the cocks, reporting that it looked good. The
preserved automatic traces confirm positive cock flow in both units of both
layouts. Meyer L-024: 25 powered samples, 13.05–13.23 bar abs, peak 88.68 kN
against at least 128.62 kN grip. Dockside L-095: 53 samples, 13.26–14.08 bar abs,
peak 66.45 kN against at least 128.76 kN grip. Native torque binding is live;
recorded absence of slip is expected. No adhesion/performance rebalance.
No rolling-audio or empty-particle-parent flood in the latest log. Evidence:
`build/radio-0.13.0/test-logs-hot`, including machine-readable trace-summary.json.

A real save/reload defect was present: Meyer support preparation moves bogies
before S060Parts.Resolve maps original stock axles by car Z. L-024 failed at
Z=-6.089. Resolution now uses captured original support position/rotation plus
the unchanged stock bogie-local axle pose. New headless regression covers the
three measured native stations; complete live save/reload remains a test.

### Governing polish decisions

- Meyer remains one native S060, one identity, two native bogies. The front uses
  original full-size split S060 cylinder renderers reparented onto its frame,
  preserving native material/paint and LOD references. Their original transforms
  restore before frame teardown. Stock crack outlets follow as well. Cock
  outlets match the native castings; shared cab control and compound sim stay.
- Body and pipes follow current S282 guidance: keep the genuine inclined boiler
  fittings, cut/cap only the lower neck and mate actual mesh faces. Found and
  corrected guessed marker/socket centres, mirrored left inside-out triangles,
  and deformation of the upper rim of a transfer flange despite a fixed centre.
  Audits now measure real cap triangles/normals and lower casting intersections,
  not just guide markers. Dockside boiler supply reaches both cylinder sets.
- Rear Meyer has a raised sloped open bunker/equipment top, pannier mounts,
  connected steps/frame fittings and lamp pedestals. No resource capacity added.
  Native lower lamp housings at both LODs, paired glass and controlled low-light
  groups move to the bogie; original upper lamp stays on the cab. Native rear
  buffer heads retain compression through an owned wrapper. Rear-only stem
  geometry follows the beam; front geometry stays original. Hook plate moves
  with it. Remove duplicate authored heads. Logical rear coupler is unchanged.
- Full native front cylinder gland clearance required shortening the visual
  Meyer main rod to 1.15 m and fitting its guides. Cylinder stroke/engine
  calibration remain unchanged. Actual crosshead front edge clears the broader
  native casting by about 30 mm; old centre-only checks missed its intrusion.
- Preserve the four native guide/crosshead cups in addition to two functional
  big-end cups per driven axle, including rear units. S060's six original oil
  circuits are guide/crosshead/big-end on each side. Extend native capacity to
  twelve points for four-axle layouts, retaining existing IDs/state. Clone native
  interactable cup prefabs/controllers, fit their measured threaded stems to
  supported crank/guide mounts, and retain oil/lid state for dormant points.
  Native simulation handles consumption, refill, damage and save. No decorative
  dummy cups, new oil simulation, or wheel-size release knobs. Dormant points
  are masked only during native ticks/service and restored with nested-callback
  and exception protection; no per-tick buffer allocations. Original six-point
  saves migrate to twelve without resetting original quantities/lids.
- Native probe measurements establish the S060 main big end follows REAR
  driver 1; old comments claiming front driver 3 were wrong. Use measurements,
  not those comments. 0-4-0/0-8-0 receive owned two/four-boss coupling-rod meshes
  on the existing animated parents, avoiding a floating middle boss or duplicate
  overlapping three-boss rods. Stock three-boss geometry remains untouched.

Preview correction: the conspicuous green panel at the cab/rear-section join was
identified as the CCL SDK DummyPlate (X1.345,Y1.01,Z-3.231), not a native buffer
beam. An experimental twelve-triangle native beam cut was completely reverted
when that premise failed; no actual beam/cylinder collision had been established.
The original native crossmember remains. Only SDK placeholder plates are hidden
in offline previews; actual native identification plates are unchanged. Do not
revive that cut based on the old preview screenshot.
Actual pin fit correction: native S060 wheel 01 has the long main-rod pin
(outer X1.177509), while 02/03 end at X.987302. Each articulated unit therefore
uses 01 on its rear/main-rod axle and 02 on its front axle. Coupling rods use
native X.938477, main-rod plane X1.047852, and cylinder/piston axis remains
X1.075 (the small offset fits inside the crosshead). The native shaft radius is
.05742, so earlier .027-radius rod holes clipped the shaft. Use the measured
shaft with clearance: full-size holes .0595, scaled Dockside holes .042.
The old Y/Z-only endpoint checks did not establish lateral shaft engagement.
Strict native X/shoulder/hole checks accompany the new geometry. Driver gauge,
tread and crank throw remain unchanged; this is native part reuse, not global
width scaling or a new user tuning option.
Three agents handled pipe/native-cylinder, Meyer body/lamps, and rods/oil cups;
root reviewed hot logs and integrated save restoration, tests and packaging.
Native assets in preview fixtures remain ignored/local and must never enter a
CCL pack. Debug standalone CCL Meyer still uses its authored cylinder fallback;
the production native S060 radio path uses the actual vanilla castings.

## Current test revision — 21 October 2026, evening

**Installed and hash verified: S060 0.12.0 / Meyer and Dockside 0.5.0.** This addresses James's
0.11 test. Installation status and hashes are recorded in the candidate receipt.
The prior 0.11 section below remains historical evidence, not the latest state.
Installation completed 21 October at 20:44 UK with game closed (checked twice).
Backup: `C:\Users\james\Desktop\Derail Valley Mods\RearrangedS060\installed-backup-0.11.0-20261021-194406Z\mods`.
Receipt: `build/articulated-0.12.0/install-receipt-latest.json`. Ten package files
and five protected files verified: settings, S282 DLL and source bundle,
LocoMeshSplitter and probe remain unchanged. No game launched or save changed.
Runtime compile: zero warnings/errors. 173 functional, 193 actual engine/ECS,
100 axle transition, 1,146 native geometry, 16 Meyer and 17 Dockside native
checks passed; native Unity particle audit 41. Authoring compile retains only
the pre-existing editor-only pipe CS0414 warning. Full gameplay remains pending.

James reports arrangements otherwise ran well, but no expected articulated
slip, rear Meyer cocks stopped after cycling, Dockside cocks absent, and Franklin
gear did not fit convincingly. Three agents investigated simulation, particles,
and geometry. Preserve the single-identity Meyer, all-under-body Dockside,
fixed release wheel sizes and full-size moved stock cylinders. Do not revive
separate vehicles, release sliders, global width scaling or extra tank capacity.

- Preserved both new probes (195535/195812) and latest Player logs under
  `build/radio-0.12.0/test-logs-20261021-2008`. Both snapshots had boiler pressure
  around atmosphere, closed throttle/cocks and zero torque. They cannot establish
  the reported powered slip/VFX failure. Do not dismiss the user's observation
  or claim zero cold flow proves a bug.
- Found **17,703 rolling-audio IndexOutOfRange errors**, beginning just after
  Meyer conversion. Actual native CarRollingAudioModule caches lastJointStep
  per bogie axle. Stage7 omitted the existing S282 refresh after axle changes.
  S060RollingAudio now calls native ResetJointSoundValues after final converted
  or restored axle arrays, including detached interior modules. No new hook,
  exception suppression or audio subsystem. Build IL audit checks both paths.
- Confirmed native closed cock readers deactivate stopped particle leaves;
  native Init enumerates active children. Stock mute/restore could therefore
  replace its cache with empty arrays, exactly matching logged warnings.
  Reactivate stopped leaves before native Init; restore their original emission
  baselines. Owned controllers rebuild entries from their actual cloned children
  and validate all outlet caches, and disable unwanted playOnAwake. Both packs
  now alias native Wispy + Wave rather than only Wispy, matching CCL's native
  authoring pattern. Importer handles multiple copier components on one holder.
- Native reader audit in the actual Unity editor passed **41 checks**, including
  20 repeated close/reopen cycles with all eight particle systems. It loads the
  installed reader from Assembly-CSharp (ports/flow in DV.Simulation); no game
  assets/assemblies are distributed. Evidence `build/radio-0.12.0/cock-native`.
  This establishes basic reader lifecycle, not complete in-game controller/audio
  playback. Owned reported failure remains subject to a hot game retest.
- Actual engine-bank hot cocks cycle tests and native ECS kernel tests pass.
  At 16 bar absolute/full regulator/full gear/1 RPM Meyer delivers roughly
  164–168 kN and Dockside 82–93 kN. A ~50 t car at native dry coefficient .24
  has ~119 kN adhesion; Dockside not slipping dry is physically consistent.
  No arbitrary grip reduction or forced wheelspin. Native difficulty,
  prevention and braking can suppress slip. Do not claim a proven slip defect
  from the two cold/closed snapshots. Bounded read-only powered traces are
  included to distinguish live torque/grip/control conditions on the next run.
- Actually rendered a radially scaled native S282 wheel substitution locally.
  Measured tread/crank datums are nearly identical, so swapping wheels alone
  does not fix Franklin geometry. Keep live S060 aliases. Fit custom Franklin
  shaft sections in X/Y (native wheel X unchanged), support valve/axle drives,
  replace cube rods with shaped bored-eye rods and fit crosshead guides. See
  `Dockside/GEOMETRY-0.5.0.md`. Experiment meshes/previews remain ignored QA data;
  no native meshes, textures or audio enter the packs.

New export evidence: Meyer `build/meyer/authoring-20261021-193936` and Dockside
`build/dockside/authoring-20261021-194029`. Reopened bundles pass 15,480/19,066
motion checks and 81 moving-pipe checks each. Root and geometry agent inspected
both normal and S282-comparison images. Preview body lacks separate cab/door/
light components and is not an in-game screenshot.

Gameplay acceptance remains: hot boiler, both cock sets repeatedly open/close,
Franklin motion/clearance in both directions and curves, native slip with actual
pressure/force/difficulty conditions. Candidate guide is
`docs/RADIO-ARTICULATED-TESTING.md`. Install only with game closed and verified
backup; never launch or terminate the game merely to install.

## Current installation and governing release geometry — 21 Oct 2026, 15:53 UK

**Installed and hash verified: S060 0.11.0, Meyer 0.4.0, Dockside 0.4.0.**
James confirmed the .10 arrangement works, then reported missing front pipes,
stock-sized wheels on the non-stock 0-6-0 and overly small .70 / 1 m drivers on
0-8-0, plus cylinders being scaled too far. His latest steering is decisive:
**granular wheel-size tweaks are development tools, not release features.**
Normal controls must offer believable fixed arrangements. The second 0-6-0
choice is explicitly **0-6-0 Short Wheelbase**, with the same wheels as 0-8-0.
Do not reintroduce the normal UI slider or silently return this choice to stock.
The single-identity Meyer and all-under-body Dockside requirements below remain.

### Confirmed cause and changes

- The preserved .10 Player.log shows 0-6-0 choices reapplying stock 1.42 m wheels;
  installed Settings.xml had driverScale=1. Radio and UMM passed that same value
  into the non-stock option. ForConversion now makes a distinct short-wheelbase
  selection at .80 scale / **1.1394048 m tread**, shared with 0-8-0. Both fixed
  choices ignore obsolete tuning values. Stock stays 1.42 m. Four-coupled uses
  stock wheels, 0-6-2 stock drivers/pony, Dockside its authored 1 m wheels, and
  Meyer its authored native-size drivers. No unrelated wheel-size changes.
- The release UMM/radio slider is removed. Legacy XML is preserved/readable;
  old resized selection values normalize to each authored arrangement on load.
  No ID/GUID, fuel/water, damage, wheel-condition or engine-state reset. The
  release IL audit refuses HorizontalSlider calls and car-spawn/identity writes.
- Old S060EightCoupled explicitly parented cylinders under a (1,f,f) group and
  radially mapped outlets. This is removed. S060CylinderFittings translates
  complete stock cylinder pieces and all cock steam/drip/audio plus crack
  outlets by the same offset. Their original dimensions, emitter sizes,
  mirroring and width remain. The native upper branch stays fixed at its boiler
  end and bends to meet the translated lower branch, using per-car copies of
  the existing splitter geometry. Originals restore on mode changes/pooling.
- Measured native cylinder/branch front is **3.6917117 m**. At fixed .80 with
  the fourth axle held at the stock rear-driver station, cylinder translation
  is Y=-.1424256, Z=+.3964801: front **4.0881918 m**, inside the measured body
  front **4.0921 m**. The old 3.470 m 'coupler inner band' is a broad trigger box,
  not the stock cylinder's structural front limit (stock geometry already
  extends into it). Keep this distinction; do not re-shrink cylinders merely
  to satisfy that discarded over-conservative band. Driving fit remains a test.
- James pointed out the existing S282 Franklin pipes. Inspected branch_pipe
  has separate intake_manifold_f, intake_manifold_r and dry_pipe_new custom
  meshes. Dockside's authoring copied only franklin_type_b, omitting these.
  Both .4 packs now reuse those shapes. S060SteamPipes keeps their moving
  sockets attached, preserves radial bore/end collars, stretches only the dry
  pipe centre and bends the front manifold to the boiler. Native upper branch
  remnants are removed on owned split-body copies while this plumbing is active.
  Compound HP->receiver->LP and Dockside's parallel boiler supply are unchanged.
- Reopened-bundle testing caught the S282 static meshes' isReadable=false flag.
  Instantiating inherited it; reading them at runtime failed. No workaround DLL
  or GPU readback was added. AuthoringData/S282BranchPipes.json contains only
  the three hash-verified custom source meshes, reconstructed as CPU-readable
  Unity Mesh assets by the existing authoring recipe. Reopened bundles validate
  all six fitted meshes as readable/nonempty. No S282 runtime dependency.
  Source bundle SHA256 remains
  `44A1B9EAE0DDA58BE97B9AEDF3877522FBFFAEC060FE02A8385158E095BDB22D`.
- Meyer rear detailing now includes riveted frame/pannier seams, axleboxes/oil
  caps, spring hangers, keeper plates, brake hangers/blocks/pull rods/crossshaft,
  tank straps, rear steps and handrails. New static details are combined into
  one mesh. All Meyer engine-unit renderers carry live S060 material aliases;
  appropriate atlas patches avoid smearing the complete cab texture over a
  small casting. Radio clones use the selected native S060's live material.
  This is a fictional period-style machine, not a historical class replica.
  Panniers still add no water capacity. Dockside remains one body, no extension.

### Evidence and remaining game test

- Log preserved at `build/radio-0.11.0/test-logs-20261021-141712/Player.log`.
  It confirms successful .10 S060 layout transitions with no logged S060
  conversion exception; unrelated CCL/DoubleTrack/exit errors also exist. James's
  'works' confirms basic use, not every coupled-save/streaming acceptance case.
- Clean .11 runtime compile; **173** functional/save/selection checks, **120**
  production B99 engine-bank checks, **100** production same-frame axle-getter
  checks, **1,146** actual native geometry/length/steam-pipe checks. Last source
  cleanup after the tested build only trimmed EOF whitespace in EightCoupled
  and csproj (no C# tokens or behavior changed); installed hashes remain below.
- Meyer export `build/meyer/authoring-20261021-144539`: reopened bundle, **15,480**
  motion assertions and **81** production pipe checks; maximum socket error
  0.0000076294 m over ±15° unit yaw and rotated/translated body poses.
- Dockside export `build/dockside/authoring-20261021-144641`: reopened bundle,
  **19,018** motion assertions and **81** production pipe checks; maximum socket
  error 0.0000107896 m. Existing Franklin shafts, quartering and phasing retained.
- Combined packaging additionally passes **16** actual B99 Meyer and **17**
  Dockside engine/control/save checks. Independent serialized-bundle inspection
  finds six readable custom pipe meshes per pack and zero textures/AudioClips.
- Inspected side/angled/yawed renders use transient local native S060 body,
  wheels and atlas from `build/radio-0.11.0/native-preview.json`/PNG. They are not
  imported as project assets or packaged. Cab interiors/doors, live steam/audio,
  rail physics and actual radio interaction are not executed by those previews.
- Next game checks: Short Wheelbase vs stock (actual wheel size, full cylinders,
  upper-pipe joins and moved outlets), matching 0-8-0 wheels/rear-wheel/cab and
  front clearance, articulated pipes/cocks through curves, then stock restoration
  and save/reload/coupled Meyer. Use docs/RADIO-ARTICULATED-TESTING.md. No game
  was launched by Codex. Offline checks do not establish live acceptance.

### Installation receipt

The game was checked closed before backup and immediately before copying.
All **10 package files** and **5 protected files** were hash-verified. Preserved
Settings.xml, S282 DLL and its source asset bundle, LocoMeshSplitter and probe.
Only this runtime's backed-up generated cache was removed.

- Backup of .10 runtime and prior .3 packs:
  `C:\Users\james\Desktop\Derail Valley Mods\RearrangedS060\installed-backup-0.10.0-20261021-145342Z\mods`.
- Receipt: `build/articulated-0.11.0/install-receipt-latest.json`, also retained
  beside that backup. Combined ZIP/package and test receipts remain in build.
- Runtime DLL SHA256:
  `705ED70B612B88A1479BA5E925FB263772AC255F3E8E54F81912A1967A8AF296`.
- Meyer bundle SHA256:
  `C72EE23E08CF167371D9069F2010FB27FF54CCF26A6C23B01D2E6CD2145601F5`.
- Dockside bundle SHA256:
  `8704AD9D07163EBEAA765EDE82241424B513A5D9901826A4EA4644B6CC579383`.

Reproduce native QA fixtures with Meyer/tools/Prepare.GeometryEvidence.py in the
existing UnityPy authoring environment before Build.Radio.ps1. Reuse the checked-in
custom pipe source JSON; Prepare.BranchPipeSources.py refreshes it only when the
actual custom source bundle changes. Build/package scripts stage only; installation
still requires the game closed and a verified backup. Do not infer a separate
powered B car or revive the granular release size controls in later sessions.

## Governing Meyer clarification — radio-owned rear unit, 21 Oct 2026

James clarified the physical/logical distinction explicitly: **one TrainCar,
one S060 identity**, with two powered axles beneath the locomotive and two
powered axles beneath a visually separate pannier B section behind its body.
The rear section is part of the S060 arrangement, not a separately spawned,
coupled or saved car. Selecting Meyer on the radio adds the complete arrangement;
stock or another layout removes the rear section and restores the native car.
Both cylinder-cock sets use the cab lever; preserve compound HP/receiver/LP,
fuel/water, damage and saves. Dockside remains entirely under one locomotive,
with no rear extension. Installation is allowed only with the game closed.

This supersedes the separate-powered-vehicle interpretation immediately below.
The unbuilt cross-car experiment was reversed; all four affected simulation
sources match the previously built 0.9 source hashes. No game/mod/save files
were changed by that experiment. Reuse the existing one-car compound circuit.
The actual defect is the missing rearward articulated section and its geometry,
support, coupling/bounds and radio add/remove lifecycle. Validate those against
native bogie/rerail behavior before changing simulation architecture again.

## Previous installation — attached rear unit, 21 Oct 2026, 12:50 UK

**Installed and hash verified: S060 0.10.0, Meyer 0.3.0, Dockside 0.3.0.**
Derail Valley was checked closed both before backup and immediately before
copying. All 10 installed package files match their staged hashes. Settings.xml,
S282, LocoMeshSplitter and the probe match their pre-install hashes. No game was
launched; gameplay acceptance remains pending.

- Verified backup of the previous .9 runtime and both prior packs:
  `C:\Users\james\Desktop\Derail Valley Mods\RearrangedS060\installed-backup-0.9.0-20261021-115021Z\mods`.
- Receipt: `build/articulated-0.10.0/install-receipt-latest.json`, also retained
  beside the backup as install-receipt.json. Staged ZIP remains available.
- Runtime DLL SHA256:
  `E41A7B3E4E672DBAAE41ADC16B0C223A45042253555669C635C72B890534F5FE`.
- Meyer bundle SHA256:
  `1F8E5BD7D7F1CF8112D0E28AE8900F8ADDC3A12699F957C675CB36F3F15BC58B`.
- Dockside bundle unchanged:
  `14E672BD811E7D03658736736E7AB034E2CE434DC4F990BA9C46B7811CBC61E7`.

## Attached rear-unit implementation — 21 Oct 2026

Runtime **0.10.0**, Meyer **0.3.0**, unchanged Dockside bundle **0.3.0**.
Development, packaging and installation are complete as recorded above;
no game session was launched. Current scope is the governing one-identity
arrangement above. The earlier separate-car design section is historical only.

### Implemented and native evidence

- Radio/UMM Meyer uses the same native car. Two powered axles are under the body
  at front pivot +.90 m; the other two are beneath the attached rear pannier
  frame at -6.10 m, 1.60 m axle pitch. Rear wheel/flange envelopes clear the
  measured body rear (-4.363802 m). Driver X scale/gauge stays native. Rear deck
  and buffer beam clear the wheel flanges; the rear coupling point is -8.27 m.
- S060MeyerSupport retains both native Bogies, rigidbodies, ConfigurableJoints,
  travellers and logical-car identity. It changes their selected support/start
  positions, native joint connected anchors and track spans, and updates the
  native car bounds, coupling distance and logical length. Four live powered
  axles point at the authored wheel transforms. No spawn, rerail, livery change,
  sim initialization or new logical car is used for a radio conversion.
- Native B99 TrainCarColliders.SetupTrainCar moves collision and bogie roots
  out of [colliders]. Use its cached collisionRoot and instance GetBogies(),
  not the uninitialized prefab hierarchy. New collision sits under the existing
  collision root, so native collision toggling finds it. Original capsules,
  bounds, anchors and lengths are snapshotted and restored.
- Native Coupler creates its joint from the live anchor when coupled; moving
  that existing anchor while uncoupled is sufficient. VisualCouplerInit.Init
  reparents chain and hoses to interior, so the support helper also translates
  those live objects and their visual root, skipping descendants to avoid double
  translation. Restore returns their local positions. Rear must be uncoupled
  and hose disconnected before user add/remove; preflight checks tail clearance.
- Native Car.length is readonly and exposes no resize setter. A narrow
  S060LogicLength helper updates/restores only this field on the same Car object.
  Actual B99 Car field/identity behavior is exercised by the geometry test.
  Native Bogie.SetupPhysics can run after early save preparation or rerailing;
  its scoped postfix reasserts the selected joint pivot only for active Meyer.
- CarsSaveManager restores car states before connections. The save prefix
  prepares longer supports and rear anchor before native coupling restoration;
  deferred visual selection follows native initialization/MeshSplitter. Fresh
  saved geometry may restore despite a saved open regulator; user switching
  still requires stationary, regulator closed, no slip/slide or traction load.
  Explicit stock selection and pool teardown also remove early pending support.
- HP/front S060 and LP/rear pannier animation/effects now match their physical
  locations. Both cocks share the existing cab lever. The four compound/engine
  source files were restored exactly to .9 source hashes after the separate-car
  tangent; the existing serial compound, resource/save ownership and dormancy
  remain. Panniers do not add water capacity; no proposed 400 L was adopted.
- Dockside's bundle and all-under-body arrangement are unchanged. No new
  car/livery ID or dependency was added. The optional canonical CCL Meyer entry
  remains a standalone one-car prototype/geometry donor, not a companion car.

### Validation and limits

- Runtime clean compile; 164 functional/save/layout checks; 120 actual B99
  production engine-bank checks; 100 same-frame production axle-getter checks;
  211 measured-clearance/native readonly-length checks, including 100 expand/
  restore cycles with identity unchanged. Native geometry tests bypass Car
  construction only; they do not invoke Unity physics or the radio.
- Unity export `build/meyer/authoring-20261021-113959`: reopened bundle passes
  one car/livery, four powered axles, front HP/rear LP animator mapping, attached
  rear layout/coupler/frame checks, two cock readers, four drips/sound aliases,
  zero bundled native meshes/audio. All 15,480 motion assertions pass.
- Inspected side and angled/yawed previews show separated rear unit, clear
  deck/beam and round cylinders/thin rods. Blue outline is the measured native
  body envelope; grey discs stand for unresolved native wheel aliases. These
  renders do not establish in-game final body/texture/gauge or steam playback.
- Reuse Dockside `build/dockside/authoring-20261021-100810`, 19,002 motion checks.
  Combined packaging also executes 16 actual B99 Meyer and 17 Dockside checks.
  Exact build/package hashes and source receipts are in `build/radio-0.10.0`
  and `build/articulated-0.10.0`. Build/package scripts stage only.
- In-game acceptance remains required: native body/rod clearance, bogie/yaw and
  rear coupling on curves, live cocks/audio, coupled save loading, streaming,
  deletion/pooling, UMM performance and 0-8-0 clearance. Both engines still share
  native adhesion/slip. Do not equate headless/asset tests with driving results.
  First conversion uses stopped, clear straight track with rear disconnected.

Install.Articulated.ps1 now resolves the requested package version, validates
all staged hashes and IDs, checks the game closed before preparation and again
before copying, and creates a hash-verified backup named for the installed
runtime version. It preserves Settings.xml, S282, LocoMeshSplitter and probe;
only this mod's backed-up assembly-cache files may be removed. Final actual
installed state is recorded in `build/articulated-0.10.0/install-receipt-latest.json`.

## Superseded interpretation — separate cars, 21 Oct 2026

James explicitly clarified that his intended **Meyer is two powered vehicles**:
the S060/A vehicle and a separate powered B vehicle behind it, with small
pannier water tanks and compound operation. Both cylinder-cock sets must be
controlled from the S060 cab. **Dockside is one tank engine with its two engine
units underneath, with no companion B vehicle.** This distinction supersedes
all single-body Meyer design instructions below.

Confirmed installed state remains S060 0.9.0 / Meyer 0.2.0 / Dockside 0.3.0.
The installed Meyer is **not the requested two-vehicle design**. Its two powered
bogies and decorative panniers are attached to one car; it supplies no separate
B vehicle or usable pannier-water capacity. Passing its previous offline checks
does not establish compliance with James's intended design. Do not describe it
as completed, or use the Dockside's one-body layout as the Meyer specification.

Investigation checked the earlier chat, authoring, current installed mod names,
repository history and relevant project documentation. The earlier chat and
handover recorded a single-body interpretation after the "one vehicle, got it"
reply. That explains the implementation path; it does not override James's
current clarification. No separate B-unit asset was located in the checked
project/install locations. Do not claim an existing B asset was deleted, hidden
or restored without finding evidence of it. Codex's earlier description of a
rear powered unit did not establish that a separate powered vehicle existed.

Before implementing the correction, inspect the installed native powered-tender
and working S282 companion-vehicle paths. Prefer standard CCL vehicle data and
native coupling/spawn/save/resource functionality. Establish the concrete gap
before adding any cross-vehicle compound/cock coordination. Preserve the A
vehicle's identity, resources, damage and save state, plus the companion's own
identity/state. Reconcile wheel counts and water plumbing with the actual
two-vehicle layout; the old provisional 400 L is not an approved capacity.

This correction updates the design record only. No runtime/asset files were
rebuilt or installed in response to this clarification. The installation and
backup evidence below remains accurate; its Meyer design decision is superseded.

## Previous installation — visual/cab correction, 21 Oct 2026, 11:12 UK

**Installed and hash verified: S060 0.9.0, Meyer 0.2.0, Franklin Dockside 0.3.0.**
This entry supersedes the staged-only .9 and installed .8 notes below. James
closed the game after reporting the Meyer lacked a visible power bogie and the
Dockside showed cardboard boxes. Prior authorization covered installing the
completed correction with a backup. No game was launched by Codex.

### Confirmed causes and corrected decision

- Original Meyer authoring deleted the generic bogie frames and supplied only
  wheel aliases and buried pannier cubes. It had no visible powered frames or
  connecting rods. Now both units have frames, springs, round cylinders and
  native crank-port-driven main/coupling/piston rods. Rear panniers moved outboard
  and have rounded ends. Keep the previously agreed **one vehicle on two power
  bogies**, not an auxiliary vehicle. An optional clarification was unanswered;
  no new approval was inferred. Panniers remain decorative, capacity unchanged.
- The Dockside source had a cuboid casting, now removed without removing its
  five animated Franklin shaft/joint channels per side. More significantly,
  the piston rod clip keyed only Z scale: actual Unity sampling reset X and Y
  to 1, turning each rod into a metre-wide box. Both animation authors now key
  all scale axes; audits check the 35 mm rod cross-section at every sampled pose.
  This defect was missed by the old endpoint-only audit. **Passing numerical
  motion checks did not validate appearance.** Inspected side/angled/yawed renders
  are now part of the export evidence. Native aliases use grey preview discs,
  so these previews do not establish full body/rail fit in game.
- Meyer uses measured native crank throw .33535485 m, matching piston stroke
  .6707097 m in both standalone calibration and the in-place compound engine.
  Wheel X scale and rail gauge are unchanged. Reuse LocoMeshSplitter's existing
  native shell for both standalone cars to suppress stock cylinders/brakes.
- The .8 log's missing cab definitions are addressed by copying the native
  S060's actual seven hatch/door/window definitions, headlight control bindings,
  six position-provider datums, lubricator marker and explosion datum into the
  standalone cars. No new cab controller, service or DLL was introduced. Native
  radio conversions keep their complete existing cab. Effect startup checks
  include inactive particle aliases and wait for active bogies before binding;
  inactive startup remains a hypothesis for the logged outlet failure, pending
  an actual game test. Do not claim cab/effect playback has been verified.
- Radio selections now reject obsolete asset packs before hiding stock gear.
  Both updated packs are required for the .9 Meyer/Dockside choices. The .9
  in-place identity/resource/save and compact .70-scale 0-8-0 work described
  below is now installed, still awaiting in-game acceptance.

### Evidence and installation

Reopened exports: `build/meyer/authoring-20261021-100836` and
`build/dockside/authoring-20261021-100810`. Their `*-running-gear-0/1/2.png`
renders were inspected; old blocks are absent. Unity motion assertions:
15,480 Meyer and 19,002 Dockside, including ±12-degree yaw. Maximum endpoint
errors below 0.5 micrometres. These are geometry checks, not physical clearance.

Clean Stage 7 compile, 164 functional checks, 120 actual B99 engine-bank checks,
100 production axle-getter checks, 16 Meyer native and 17 Dockside native checks
passed. Native engine checks cover shared cocks, serial compound conservation,
damage/repair and saves; no Unity driving or cab interaction executed.

Combined test ZIP and package/install receipts: `build/articulated-0.9.0/`.
Use `Build.Radio.ps1`, then `Package.Articulated.ps1` with explicit completed
export directories; old Build.Meyer/Build.Dockside scripts stage older versions.
`Install.Articulated.ps1` verifies the package, checks the game is closed,
backs up all three prior folders and verifies each backup file before copying.

Verified backup:
`C:\Users\james\Desktop\Derail Valley Mods\RearrangedS060\installed-backup-0.8.0-20261021-101217Z\mods`.
All ten installed files match the package receipt. Only the backed-up S060
generated assembly cache was removed. Settings.xml, S282, LocoMeshSplitter and
probe DLL match their prior hashes. Game saves and Player.log were not edited.
S282 was already **1.13.5** before this installation, with DLL hash
`3FEBE4F4FADA33C8A7DE0C6FF55582AD521F45845197B2A5B74EEC71F3F9AC4C`;
that exact hash is unchanged afterwards. Older handovers record an earlier S282
build; this installation did not update it.
Runtime DLL: `735350AE55CC61265A05BBBFAF6FE3B237BFC1D7431C8E92369EAD6BA23E7AF6`.
Meyer bundle: `F2E417F93808E6121508CB7D35B6410EA48D79DA4E2F6EF178698D15179DC680`.

Next: follow `docs/RADIO-ARTICULATED-TESTING.md`; check visible frames/rods,
both cylinder-cock sets, standalone cab/headlights/windows, curves, compact
0-8-0 fit, retained resources/damage and save/reload. UMM performance is still
unmeasured. Preserve the next Player.log before another launch overwrites it.

## Latest development — S060 radio 0.9.0 staged, 21 Oct 2026

James requested Meyer and Franklin Dockside on the native S060 wheel changer,
retaining ID, resources, damage and saves, and a smaller/fitted 0-8-0. He is
testing the installed 0.8.0. **No installation in this turn.** Installed DLL
still hashes `65CCDAF33D8ADE6D534B59FF2C52741B0BE39F02D884429A08B25DC393CA6BB7`.
The following development notes supersede older statements that articulated
choices are necessarily spawner-only. The installed 0.8.0 is still spawner-only.

### Native/reference decision

Read current S282 `ArticulatedSimulation.cs` and installed B99 implementation.
Follow its create-once native-flow pattern. Keep the selected TrainCar,
logicCar/GUID/ID, livery, cab, boiler, fuel/water/resource components, damage and
save ownership. Do not replace/spawn a CCL locomotive or call Initialize on use.
Registered, CCL-processed prefabs are read-only visual/effect donors. Confirmed
CCL's ProxyScriptProcessor runs native proxy replacement on its model prefabs.
No new DLL/dependency, mesh extraction or global steam-engine Tick patch.

S282's unchanged main engine cannot itself implement the Meyer: native RSE has
cached readonly cylinder calibration and no receiver/back-pressure model.
`S060EngineSlot` therefore keeps stable native port contracts while selecting
once-created calibrated native simple engines or the existing Meyer adapter.
Original eight cab/supply inputs remain. Only two active slots tick; dormant
circuits retain chest mass, receiver mass/energy, condensate, temperature, crack,
damage meter and phase. Main/front modes and all internal native states save
through their existing native SimComponent save contract. Condition transfers
retain cracks across layout changes, and service repairs dormant engines.
HP alone charges boiler demand; LP alone exhausts to blast. Sums initialize
capacity before SteamExhaust caches normalization, and update on mode switches.
The same confirmed cache-order correction also supports the separate Dockside.

### Code and geometry

- Stage 7 / `B99_S060_ARTICULATED` produces **0.9.0**. Radio has seven choices:
  stock, 0-4-0, resized 0-6-0, fitted 0-8-0, 0-6-2, Meyer and Franklin Dockside.
  UMM has apply buttons and optional articulated spawn-pool entries, default off.
- Native S060 bogies, rigidbodies, joints and track travellers remain. Clone
  only two donor bogie frames. Authored visual axles stay in their Animator
  hierarchy; four native powered axle records live directly under the original
  bogie_car parents, as required by DV's getter. Pivots remain stock, with axle
  offsets like the in-game-proven 0-4-0. Actual curve behavior remains untested.
- Original gear/cylinders/brakes/readers are captured and restored. Native stock
  cock outlets temporarily read a zero-flow port while both owned engine units
  supply the cocks. Owned readers unsubscribe before destruction; all cleanup
  steps attempt restoration even when another cleanup reports an error.
- Meyer reuses the existing wheel/pannier frames and split native cylinder
  meshes with unchanged X width. Wheel rotation uses actual crank ports. Detailed
  Meyer main rods and valve gear are **unfinished**, as in its original prototype.
  The cylinder fit remains a candidate pending in-game inspection.
- 0-8-0 caps driver scale at **.70**, including old full-size saves/direct spike
  calls. Rolling diameter approximately **.996979 m**; rear centre **-1.3978 m**,
  the stock rear-driver station. Whole four-axle group shifts forwards; moving
  gear front at .70 is **3.08717 m**, behind the measured 3.470 m coupler band.
  Split cylinders get the same Y/Z scaling/translation, and native cock outlets
  move accordingly. X, gauge and mirrors are retained. Rear wheel may overlap
  the stock cab/footplate envelope just as the stock rear driver does; do not
  claim the entire wheel is ahead of the cab. Full-body clearance is unverified.

### Wheel damage and saves

Native PoweredWheelsManager saves broken **active array indexes** and loads them
before deferred geometry. A fourth index would be out of range on the original
three-wheel S060. Confirmed in native code; configuration cannot solve it.
`S060WheelCondition` maintains three canonical native wheel identities and one
retained added-axle slot, syncing states when Physics.Apply/Restore change arrays.
Native saving receives the original three indexes; the mod's v2 selection save
retains all four enum states. Full native wheel repair also services the hidden
slot. GUID/pool teardown clears the ledger. v1 saves still load, and invalid or
unsupported selection/wheel payloads remain verbatim until explicit selection.
An explicit stock/conventional choice also cancels a saved articulated engine
mode even when its geometry has not yet applied.

### Validation and handoff

`Build.Radio.ps1` stages `build/radio-0.9.0/RearrangedS060-0.9.0-radio-test.zip`.
Hash receipt beside it; this turn does **not** install it. Existing Meyer 0.1.0
and Dockside 0.2.0 visual packs are required and unchanged.

- Clean Stage 7 compilation; 164 functional selection/layout/save checks.
- 120 production engine-bank checks against actual publicized B99 engine
  implementations (method bodies unchanged): shared cocks, quartering/unit phase,
  serial mass/energy, condition/repair, frozen receiver, exact all-circuit save
  round trip, corrupt-save rollback and 100 repeated mode cycles.
- Existing 16 Meyer-native and 17 Dockside-native engine/control checks pass.
- Existing 100 same-frame axle-getter contract cases pass.
- IL audit disallows spawn/reinitialize/native identity writes in switching code.

**Not proven:** in-game conversion, complete resource/damage/save/streaming
round trip, wheel-repair ledger integration, native cock suppression/rebind,
articulated rail/yaw behavior, collision/pipe/rod fit and UMM performance. No Unity
scene or gameplay execution occurred. Do not present native headless checks as
in-game acceptance. Focused next test is in `docs/RADIO-ARTICULATED-TESTING.md`.

### Latest 0.8.0 test log arrived during this work

Preserved `build/radio-0.9.0/test-logs-20261021-102545/Player.log` (last write
10:25:45 UK). Confirmed native S060 L-050 switches 0-4-0 -> stock -> full-size
0-8-0 -> 0-6-2 -> stock. No S060 selection-refused entry in this log. The .8
0-8-0 really was scale 1.00; that is what this .9 fit replaces. Both CCL types
were spawned, and the Meyer compound prepared successfully.

The standalone CCL vehicles report missing headlight override input, hatch/
window/sunroof/door ports, no PositionSyncProviderController, no lubricator
ratchet and missing explosion anchor. These are genuine outstanding standalone
cab/configuration gaps. The in-place .9 path retains all of those original
native S060 systems; it does not copy that standalone cab setup.

Both spawns also report cock outlets with no active particle systems. Confirmed
native CockParticlePortReader uses GetComponentsInChildren without includeInactive.
Inactive aliases/startup activation are a hypothesis, not a proven sole cause.
Development runtime now checks includeInactive alias contents, activates only
owned effect subtrees, and delays standalone binding until bogies/effect roots
are active. The in-place path uses the same validation before conversion commits.
This change has compiled, but effect playback is not game-verified. Other log
exceptions belong to AI traffic/DoubleTrack/shutdown; no new cause was inferred.

## Current installation and test handover — 21 Oct 2026

James authorized finishing Dockside rods/Franklin motion, crank phasing and fit,
then installing with a backup. He also confirmed the previous radio worked,
requested S282-like radio wording, and asked about the Meyer. **S060 runtime
0.8.0 and Franklin Dockside 0.2.0 are now installed. Meyer remains 0.1.0.**
This entry supersedes all earlier staged-only and installation notes below.

### Log findings and radio/transition correction

Preserved test Player.log at `build/dockside/test-logs-20261021/Player.log`.
It shows S060 0.6.0 active, successful 0-4-0/0-6-0/0-6-2/0-8-0 selections and
two `S060 selection refused` errors at an axle Z -2.800 while switching.
Confirmed cause against native getter IL: Bogie.Axles returns its cached array;
the inherited postfix replaced only the field, leaving the current return
value unfiltered. `ref Bogie.AxleInfo[] __result` now filters/updates both the
return value and cache, excludes inactive hierarchy/destroyed transforms.
The production postfix passes 100 same-frame getter-contract regression cases.
No speculative deferred-destruction workaround or new native Tick patch added.

S060 radio keeps its independent registration and targeting, but now matches
the S282 `WHEEL REARRANGER` title and `Select Wheel Arrangement:` chooser. The
starting prompt identifies S060 versus S282. Short labels replace long status/
instruction blocks; resized choices retain a diameter line. Confirm/cancel and
stationary/regulator safeguards remain. Native per-car save format stays v1.

CCL logged `Successfully loaded car type RS060_Meyer_0440`; no Meyer spawn or
compound-prepared line was present. James then clarified he had not checked
the ordinary spawner. No spawner defect was established or patched. Meyer and
Dockside are separate CCL vehicles in the normal locomotive spawner, not S060
wheel-arrangement choices and not hidden spike modes.

### Dockside animation and fitting

- Two native simple engine sets under the S060 body, four powered axles, .50 m
  rolling radius, 1.20 m axle pitch and pivots Z +/-1.40 m. No rear appendage.
- Read-only native wheel measurement found the pin-cap centre at source
  (X .98730177, Y -.33535485, Z approximately 0), resources Mesh path ID 5438.
  Evidence `build/dockside/native-crank-measurement.json`, no geometry exported.
  The retained-width, radial-only wheel scaling gives .23545967 m crank throw;
  physical piston stroke now .47091934 m, matching the actual visual pin.
- Native AnimatorPortReader SET_NORMALIZED_TIME drives one Animator per unit
  from that unit's actual native crank port. Removed the competing code rotator;
  powered wheels reference animated right-side visual transforms. Wheel meshes,
  rods, crossheads, guides and piston rods share a single authored crank clock.
- Right/left quartering is 90 degrees; production DocksidePhasing seeds the
  front native engine +.125 turn (45 degrees) once per new SimulationFlow.
  Rebinding presentation/pooling does not reseed an existing flow. Reverse
  preserves the relative phase. Native simple-engine save behavior retained.
- CCL template axles both had name `[axle]`, so animation initially addressed
  just one. Corrected to `[axle] rear` and `[axle] front`, preserving the native
  discovery prefix. Reopened-bundle samples now check both actual wheel pins.
- Original authored Franklin clip is imported locally; only its custom shaft/
  joint channels are copied, retaining original angles and 2:1 motion. Built
  bundles omit editor curve bindings, so they are not the motion source. Fixed
  source clip path `cylinder/u_joint` to the actual `u_joint_mapped` node. No
  native wheel animation clip or original full clip is bundled.
- Cylinder axis X +/-1.075, Y .50, unit Z1.60; gearbox Z-.60, main rod length
  1.40 m. Coupling rods X +/-1.015 and offset main-rod planes prevent overlap;
  crank-pin bushings bridge them. Cock outlets now follow the fitted cylinder
  positions. Franklin straight bounds X +/-1.497052, Y .296311..1.189461,
  car Z -2.229411..3.407567, within the S060 body envelope.
- The alias body contains baked stock cylinders/calipers. Verified the existing
  LocoMeshSplitter cache and native loader, then reuse its split S060 LOD0 shell
  for this car only, hiding exactly its two stock cylinder parts and six stock
  calipers. Cache is initialized in splitter Main.Load. Clone/material changes
  affect this car only; no shared prefab mutation or alternate extraction path.

### Evidence, installation and remaining test

- Runtime/authoring compile clean, zero warnings/errors. S060 119 behavior / 32
  metadata checks; Meyer 25 model / 16 native / 37 metadata checks pass. Dockside
  17 native engine/control/flow/phasing checks and 100 transition checks pass.
- Actual Unity export and reopened-bundle audit, including 17,268 pose checks
  through full cycles and yaw -12/0/+12 degrees: rods meet pins/crossheads,
  pistons meet axes, all five shafts/joints move on each side, loop/reverse
  seeking closes. Recorded sampled max endpoint error 3.04e-7 m. Sampled swept
  half-width at 12 degrees: 1.883637 m; actual track/platform clearance unverified.
- Latest authoring evidence `build/dockside/authoring-20261021-083732`; local
  kinematic previews use grey stand-in wheel discs, not native game mesh exports
  or gameplay screenshots. Package `build/dockside/S060-Franklin-Dockside-0.2.0-test.zip`.
- Game confirmed closed before backup and install; eight package/ZIP/installed
  files SHA-verified. All nine previous S060/Meyer files backed up under
  `C:/Users/james/Desktop/Derail Valley Mods/RearrangedS060/installed-backup-0.6.0-20261021-084302Z/mods`.
  Receipt beside it and `build/dockside/install-receipt-latest.json`.
- Installed runtime SHA-256:
  `65CCDAF33D8ADE6D534B59FF2C52741B0BE39F02D884429A08B25DC393CA6BB7`.
  Dockside bundle:
  `D09BBC64DAC30770E97CFA214A2D72FAABCC85060D77D4F3E7743A879097C8D8`.
- Settings.xml preserved byte-for-byte (C455F4BB...C10AD). Removed only backed-up
  old generated `RearrangedS060.dll.29145.cache`. Existing Meyer package, S282,
  S060 probe and LocoMeshSplitter DLLs verified unchanged. No game launch.
- Ready for James's acceptance tests in `Dockside/README.md`: normal-spawner
  Dockside, both cock systems, all wheels/rods/shafts forward/reverse, gentle
  curves, save/reload/respawn; ordinary S060 repeated 0-6-2 -> 0-4-0 -> 0-8-0
  -> stock radio switching and separate S282 mode. Cab interaction, playback,
  body/track clearance and driving remain unverified in game.
- Prototype limits: simple new rod/guide geometry, shared native traction/slip
  and RPM, unfinished exterior styling/general exhaust presentation. Native
  simple-engine save state remains limited; do not claim Meyer's detailed
  receiver/water-state persistence. No commit, push or publication performed.

## Earlier development — Franklin Dockside first draft, 21 Oct 2026

James requested "a 0-4-4-0 small driver franklin simplex meme version/dockside
tank", then clarified **"articulated duplex, nothin new out back. all under
the loco"**. Implement this as one vehicle, two pivoting two-axle engine units
under the S060 body, both simple expansion. No rear appendage/vehicle/pannier.
This is a separate CCL car/livery `RS060_Franklin_Dockside_0440`, not a radio
conversion of a saved S060 or a change to the compound Meyer's identity.

First draft is authored, exported, inspected and staged, **not installed**:
`build/dockside/S060-Franklin-Dockside-0.1.0-draft.zip`. Requires the matching
staged S060 runtime 0.7.0 (Stage 6). Installed S060 0.6.0 and Meyer 0.1.0 stay
unchanged; installation hashes were rechecked. The game was not launched.

- Candidates: rolling driver diameter 1.0 m, axle pitch 1.20 m, unit pivots Z
  +/-1.40 m, two equal cylinder pairs (bore .30 m / stroke .40 m, chest 90 L,
  maximum admission .8 kg/s each). Body/cab/couplers retain S060 dimensions.
  Only driver Y/Z scales change; mirrored X scale remains exactly +/-1.
- Native-first: both native ReciprocatingSteamEngine implementations receive
  boiler P/T/quality and shared native cab regulator/reverser/cock controls.
  Native sum definitions combine intakes, exhausts, MAX_FLOW for blast
  normalization, and torques. No receiver or custom simple-engine calculation.
- Existing native cab/presentation lifecycle hooks admit the exact dockside
  ID only in Stage 6. Compound definitions are still replaced ONLY for the
  original Meyer ID. No new Harmony hook class, DLL or runtime dependency.
- Reused the fork's actual custom Franklin Type B geometry through an editor
  copy from the installed S282 asset bundle (read-only, source hash recorded).
  Nineteen custom mesh assets, four static assemblies, material aliases; no
  native body/wheel mesh, texture, sound or animation extraction. Standard SDK
  coupler meshes and Unity built-in Cube/Cylinder shapes remain. Native copies
  are not evidence of physical/animated fit; measure the saved bundle.
- Saved Franklin bounds, car-local: X +/-1.372052 m; Y .296311..1.189461 m;
  Z -2.175678..2.697895 m. All inside measured S060 body envelope. Reopened
  bundle confirms four powered axles, radius agreement, no X scaling, both
  cock/audio systems, direct boiler supplies and all four summed quantities.
  CCL pack-root LoadAllAssets alone omits mesh dependencies; follow the saved
  prefab's actual MeshFilter references when auditing them.
- Fourteen actual B99 native engine/control/flow checks passed using exported
  calibration and read-back graph wiring. Includes both cocks, purging/heating,
  both boiler intakes, exhaust/max-flow/torque sums, regulator closure, reverse,
  independent native crack saves. No Unity scene interaction or playback.
- Shared runtime clean compile. Meyer regression still passes 25 model, 16
  native adapter and 37 metadata/bundle checks; prior S060 119/32 checks pass.
  The newly rebuilt Stage 5 file is a development output, NOT the installed
  0.6.0 or its original receipt. Use installation receipts for installed hashes.
- Unfinished: rods and crank-to-Franklin animation, unit phase offsets, final
  cylinder/cock endpoint fitting, frame/support geometry and driving/curves.
  One shared native traction/slip group and RPM currently; not independent
  bogie slip. Native simple-engine save behavior retains its crack-state
  limits; it does not inherit the compound model's detailed state persistence.

Sources/reproduction: `Dockside/Authoring`, `Dockside/Run.Authoring.ps1`,
`Build.Dockside.ps1`, `tests/Dockside`, `Dockside/README.md`. Actual latest
authoring path and package hashes are in `build/dockside/package-receipt.json`.
Continue with motion and fit work before treating this static fit draft as a
finished Franklin locomotive. No commit, push, publication or game launch.

## Current installation — 21 Oct 2026, Codex

This section supersedes all earlier installation and unfinished-Meyer notes
below. James instructed: "keep going on the Meyer, get both sets of cylinder
cocks working from the cab and sort the compound sim", then "great, install
when done". **RearrangedS060 0.6.0 and RS060_Meyer_0440 0.1.0 are installed.**
The former includes the 0.5.0 S060 radio/UMM/0-6-2 features and the Meyer module.
The Meyer is still a development prototype; no game session was launched.

### Installation and rollback evidence

- Verified the game closed immediately before installation. Verified package
  and ZIP contents, all seven installed file hashes, and six prior-mod backup
  file hashes. Existing Settings.xml is byte-for-byte unchanged. Removed only
  the backed-up generated `RearrangedS060.dll.37290.cache`.
- Full prior 0.4.0 backup:
  `C:/Users/james/Desktop/Derail Valley Mods/RearrangedS060/installed-backup-0.4.0-20261021-003124Z/mod`.
  Receipt beside it, also `build/meyer/install-receipt-latest.json`.
- Installed runtime SHA-256:
  `F682A029FFA4941759010BFEB001D2D1E672E7101A1187606082EBE465FC88DC`.
- Installed CCL bundle SHA-256:
  `C6902C49EBCD33055E82E80CC944C82E326EC4A84C0F98EB738EE3C35F7639DE`.
- S282, S060 probe and LocoMeshSplitter DLLs verified unchanged. CCL 3.1.11 and
  LocoMeshSplitter 1.1.9 are installed. No dependency added or updated.
- To roll back with DV closed: preserve any new test settings if wanted,
  restore the six backed-up S060 files and remove the new RS060_Meyer_0440
  folder. The earlier 0.3.0 backup remains available too.

### Meyer implementation and native-first decision

- One vehicle, 0-4+4-0T, HP rear / LP front. Standard CCL steam wizard, one
  canonical type/livery, native S060 mesh/material/audio aliases, four powered
  axles and native powered-wheel rotation. No exported native mesh/audio data.
- Both native cylinder-cock definitions use `cylinderCock.EXT_IN`. Each engine
  retains independent native condensate, temperature, crank/inlet masks, wear,
  crack and water-damage behavior. Two native cock particle readers, four water
  drip aliases and four sound aliases move on the actual bogie frames.
- Runtime owns native particle/audio Init/Deinit, pause registration and
  restoring particle baseline parameters on reuse. S060 cab/LOD assignment is
  scoped to the Meyer livery and occurs at TrainCar.Awake entry, before native
  SetupInterior. Native cab fields do the actual control binding.
- Confirmed in the actual B99 engine: no receiver/back-pressure input, piston
  work subtracts atmosphere, and exhaust-temperature Tick output is never
  assigned. Native parallel engines cannot supply serial compound behavior.
  The smallest extension is two scoped engine implementations in the existing
  S060 DLL, not another dependency or a global engine Tick patch.
- `MeyerCompoundCycle` holds HP chest -> receiver -> LP chest with finite
  vapour mass/internal energy. Only HP intake is boiler demand; LP remains open
  for receiver rundown. Receiver pressure opposes HP work; LP alone exhausts
  into the blast pipe. Both torques feed native traction. The two bogies still
  share one native traction/slip group.
- `MeyerNativeStage` retains native cylinder water/damage/chuff methods without
  running native simple-engine steam calculation over the compound circuit.
  Native per-component save data includes stored vapour/energy, water/cracks,
  temperature, damage and crank. Unknown/corrupt state is refused atomically;
  there is no implicit receiver refill.
- Thermodynamics are an ideal-vapour finite-volume approximation, constant
  gamma 4/3, no two-phase receiver model or historical/IF97 calibration.
  Native cylinder condensation/drainage remains active.

### Validation, limits and next test

- Clean runtime and authoring compilation. S060 regression: 119 behavior and
  32 metadata/package checks. Meyer: 25 conservation/compound checks, 16 actual
  B99 native port/water/damage/save checks, 37 metadata/bundle checks. Native
  tests execute unchanged B99 method bodies from a build-local publicized
  reference; they do not execute Unity scenes, Harmony dispatch or playback.
- Actual Unity export and reopened-bundle audit passed: one car/livery, four
  powered axles, two cock readers, four drips, four sound aliases, zero bundled
  native meshes/AudioClips. Latest authoring evidence:
  `build/meyer/authoring-20261021-002309`. Package:
  `build/meyer/S060-Meyer-0.1.0-prototype.zip`; receipts under `build/meyer`.
- Corrected authoring lessons: CCL OnValidate auto-registers definitions; remove
  added references before explicit execution ordering, then reopen the bundle
  to check unique IDs. New wizard graphs already contain live connection lists;
  call AfterImport only for existing serialized graphs to avoid erasing wires
  with unsaved empty JSON. Actual template bogies are direct car children.
- Rod animation and finished cylinder/frame meshes remain unfinished. Panniers
  are placeholders; proposed 400 L adds no water yet. Cock endpoints and bogie
  collision capsules are fit candidates. Full boiler/exhaust presentation beyond
  cylinder cocks is unfinished. Rail alignment, driving/curves, physical cab
  access, effect playback, save/reload and pooling require game acceptance.
- James's test steps: `Meyer/TESTING.md`, also installed as the vehicle README.
  Spawn the separate Meyer, open the one cab cock lever, check HP then LP
  discharge once the receiver fills; purge and close both, check regulator
  rundown, reverse/curves, save/reload and respawn. Ordinary S060 must retain
  its radio/UMM behavior and the Meyer must be excluded from that radio.

## Earlier implementation history

### Previous 0.4.0 installation — 21 Oct 2026, Codex

James explicitly confirmed the game was closed and authorized installing the
latest S060 build with a backup. **0.4.0 is now installed**, superseding earlier
"staged only / 0.3.0 installed" notes below. It includes the S060 radio/settings,
native per-car saving, cached UMM targeting and X/gauge-preserving scale fix.

- Installed DLL SHA-256:
  `5453DC8B4D998C237BEBFD863B74AD2A08FFBAA73A18524EBCD4C8C2230467E6`.
- Verified full prior-mod backup:
  `C:/Users/james/Desktop/Derail Valley Mods/RearrangedS060/installed-backup-0.3.0-20261021-002637/mod`.
- Install receipt: `install-receipt.json` beside that backup, also copied to
  `build/functional/install-receipt-latest.json`.
- Verified package/ZIP, source and game-reference hashes before installation;
  four backup files and four installed package files passed SHA-256 checks.
  No existing S060 settings XML was present. Removed one backed-up, generated
  0.3.0 assembly cache so UMM cannot reuse that cached DLL.
- S282 and S060 probe DLL hashes were checked unchanged. No game launch or
  runtime validation was performed. James is testing radio interaction, UMM
  performance and wheel alignment next; use `docs/FUNCTIONAL-TESTING.md`.

### Earlier development — 21 Oct 2026, Codex

This section supersedes the older "not started" / "0.4 staged" notes below.
James authorized starting 0-6-2 and Meyer while testing installed 0.4.0.
At this earlier checkpoint 0.5.0 was staged only. James subsequently authorized
the 0.6.0/Meyer installation recorded at the top of this document.

#### 0-6-2 prototype

- Added stable arrangement ID 3 (`SixTwo`), radio/UMM choice, persistence and
  opt-in random pool. Fixed stock drivers for this first layout; other slider
  settings cannot silently resize it.
- Three stock powered axles plus one unpowered pony axle at candidate car Z
  -2.8 m, using the existing rear bogie, four total axles and nominal .75
  adhesion-weight share. Frame, coupling and curve fit still require testing.
- Donor comes from `TrainCarType.LocoSteamHeavy.ToV2().prefab`, with guards on
  native S282 body, mesh, component types and wheel-rotation membership. No
  mesh export, runtime extractor, donor spawning or shared-prefab mutation.
- Read-only `Meyer/tools/Audit.PonyDonor.py` confirmed the actual B99 resources
  asset: `LocoS282A/Axle_F/bogie_car/[axle]/axleF_model`, axle transform 135137,
  only Transform/MeshFilter/MeshRenderer components. The native rotator points
  to that exact axle with .355 m radius, X axis, affectedByWheelSlide=false.
  Evidence `build/meyer/native-pony.json`. Stripped behavior fields were read
  using the verified managed field layout and strict serialized-length guards.
- The transient native wheel rotator unsubscribes its native
  MovementStateChanged handler before destruction; native WheelRotationBase
  subscribes in Awake but supplies no destruction unsubscribe.
- Latest package `build/functional/RearrangedS060-0.5.0.zip`; clean compile,
  119 behavior checks / 32 metadata-package checks passed. This is offline
  evidence, not proof that conversion/rotation/driving works in game.
- Installed 0.4.0 DLL hash rechecked unchanged:
  `5453DC8B4D998C237BEBFD863B74AD2A08FFBAA73A18524EBCD4C8C2230467E6`.

#### Meyer scope and cylinder-cock requirement

James chose **0-4+4-0T**, two powered axles on each bogie. He asked for small
pannier tanks on the rear bogie; these should provide usable water. 400 L
total is a provisional fit candidate, not finalized or implemented capacity.

Latest requirement: working cylinder cocks on the rear unit, operated from
the S060 cab. Asked whether "tender vehicle" meant a separate powered car;
James clarified **"i misunderstood, theyre one vehicle, got it."** Keep the
single-body Meyer. Both engine sets must respond to the existing cab lever,
with independent native cylinder-water/flow state, steam, drips and audio at
their actual cylinders. Do not invent a remote tender control system.

Inspected current S282 fork's ArticulatedSimulation and ArticulatedEffects.
The auxiliary definitions reuse native cab references. Cock/drip/audio binding
is separate from engine physics. Its Erie tender effects are explicitly
excluded from Secondaries while IsTenderEngine, so that powered-tender example
does **not** prove working tender effects. For this one-car Meyer, use native
cab/CCL wiring for both engines before considering any custom control hook.

Authoring started in `Meyer/`, with README describing scope and unfinished work.
`Setup.Authoring.ps1` copied the existing CCL 3.1.9 SDK into a new ignored Unity
2019.4.40 project; L74/game untouched. `MeyerDraftBuilder` uses standard CCL
wizard, one canonical type/variant, native mesh/material grabbers, custom
two-axle bogies and rear pannier shape placeholders. Native names/materials
were audited without exporting game meshes/textures/audio.

Offline authoring compilation passes without warnings/errors. Actual Editor
run first exposed a wrong guessed visual-bogie path. Fixed against the actual
generated CCL template (BogieF/BogieR are direct car children, collision bogies
are separate) and preserved ContactPoints. The rerun succeeded:
`build/meyer/authoring-20261020-234918/authoring-result.json`. No editor process
left running by that script. The initial sandbox IPC failure required running
the installed Editor outside sandbox; no new software/license changes.

This is **an authoring draft, not a drivable Meyer**. Rod animation, cab,
collision fit, water circuit, compound receiver and cylinder-cock bindings
remain unfinished. Result flags explicitly say false. No bundle exported or
installed; S060 ZIP excludes Meyer. Native parallel engines are not true
compound expansion; preserve the unresolved HP back-pressure / receiver mass,
heat and LP feed requirements. Do not silently substitute L74's simpling
equivalent or treat aliased mesh names as proof of correct animated fit.

### Historical development update — Codex, 20 Oct 2026

#### 21 Oct follow-up: 0.3.0 UMM slowdown and three probe captures

**Subsequent width correction, 21 Oct:** James supplied
`C:/Users/james/Pictures/Screenshots/Screenshot 2026-10-20 235658.png` showing
the resized wheel/rail misalignment and rejected indiscriminate whole-mechanism
scaling. This is now a confirmed defect, not an unverified rail-contact question:
the probe records X scale 0.824038 as well as Y/Z, narrowing the whole assembly
by about 17.6%. Inspection of the working S282 source found the native-compatible
precedent at `WheelRearranger.cs:2431-2432`: X stays -1/+1 while Y/Z change.
S060DriverScale now preserves original X/mirroring and scales only Y/Z; the
false comment claiming uniform scaling matched the S282 was removed.

This is the narrow gauge correction. **Independent diameter editing remains
unfinished**: Y/Z group scaling still changes axle pitch and cylinder placement,
and proportionally shrinking pitch and wheels does not create more relative
wheel clearance. UMM/test instructions state this explicitly. Stop treating
whole-group scaling as the finished solution to the cramped 0-8-0. Check native
animation/rod geometry and deliberate wheel/cylinder positions before extending
resizing. The correction is staged only; installed 0.3.0 is unchanged and the
new lateral alignment still needs in-game verification.
Both the Stage 1 prototype and Stage 4 functional code compile after these
corrections with zero warnings/errors. Functional checks remain 99 behavior /
29 metadata-package checks; this does not measure the FPS improvement or wheel
contact in game.

James reports severe slowdown with the spike UMM window expanded and recovery
when switching to the read-only probe window. Evidence is preserved locally
under `build/investigation-20261021-umm/` (Player.log, UMM-Log.txt, perfprobe.log
and all three probe reports); these diagnostic artifacts are not committed.

All three reports target L-086 / GUID 0437ecd8-5ce2-4f32-8717-1c8c39af1054:
- `RearrangedS060Probe-report-20261020-235542.txt:1479,1588`: 0-4-0,
  poweredAxles.NUM=2 and two registered powered wheels.
- `...-235604.txt:1501,1610`: full-size 0-8-0, poweredAxles.NUM=4 and four
  registered powered wheels.
- `...-235639.txt:1501,1610,1878`: 0-8-0, four powered axles, mechanism scale
  0.824038 (rolling diameter approximately 1.17 m).
Player.log snapshot lines 7425–7516 corroborate conversion > restore >
conversion > restore > resized conversion, without an S060 exception. The
probe summary lists only three visual wheel positions on the 0-8-0 because
GeometrySection.DriverExtents picks one renderer per stock wheel name and the
clone reuses wheels_02's mesh/child name. Full hierarchy and PoweredWheelsManager
confirm four; do not infer a missing middle driver from that summary.
Static captures cannot validate rod synchronisation in motion or rail contact.

Performance evidence: perfprobe.log:57792 records 4.1 FPS at 23:56:03 and
:57833 records 30.7 FPS at 23:57:04. GUI/frame-rendering scopes dominate the slow
windows; native physics is around 2–3 ms/frame. DVSeasons' separate whole-game
sample in Player.log also reaches 2.4 FPS. This confirms the reported slowdown,
but `timed_scripts=0` means no individual mod callback was profiled.

Strong suspect from source: S060SpikeWindow.Target called
Object.FindObjectsOfType<TrainCar> and sorted candidates on every IMGUI event.
The read-only probe does that work only when capturing. The staged functional
window had inherited the same scene-wide search; Codex corrected both call
sites. S060Targeting now uses native CarSpawner.AllCars (verified against the
installed B99 assembly), a single nearest-car pass, and a 0.25 s cache refreshed
on GUI layout only. No extra service, hook, package or profiling DLL was added.

The rebuilt 0.4.0 package passes 99 behavior and 29 metadata/package checks,
including the car-registry/no-scene-search checks. Compilation is clean. This
removes the identified avoidable work; improved real FPS remains unverified.
Acceptance step 9 now compares the expanded S060 window, probe window and
closed UMM for 20 seconds each. Installed 0.3.0 and the probe remain untouched.

James authorized the separate S060 radio and UMM implementation while testing
0.3.0. Priority is the functional S060 version first, then 0-6-2, then the
separate Meyer. No 0-6-2 or Meyer implementation has started.

**0.4.0 is staged, not installed.** Build with `Build.Functional.ps1`, validate
with `Validate.Functional.ps1`. Package and receipt:
`build/functional/RearrangedS060-0.4.0.zip` and `build-receipt.json`.
The game's installed DLL remains 0.3.0, SHA-256
`47F12FE2CD489FF30363241ACE972BF25106D915152595F3D9BE5E0C2693309F`.

The functional build uses Stage=4/B99_S060_FUNCTIONAL and excludes inherited
S282 entry/radio/catalogue/save implementations entirely. New S060 files:
`S060Main`, `S060Radio`, `S060OptionsWindow`, `S060Settings`, `S060Selection`,
`S060SavedState` and `S060Lifecycle`. Existing 0-4-0/0-6-0/0-8-0 conversion
geometry is retained. Direct switching restores the prior gear before applying
the next choice, and attempts recovery on failure. Cleanup now restores scale
and axle positions during rollback and deactivates clones before destruction.

Native precedents were checked before implementation: the current S282 fork's
CommsRadioS282Rearranger and ConventionalSpawn/ConventionalArrangementState.
Persistence uses native per-car simCarState with the independent key
`RearrangedS060`, version 1, arrangement ID and driverScale. No global save map,
new DLL/dependency or custom vehicle registration was introduced. Unknown save
payloads are retained. Pending selections wait for initialized stationary gear
and a closed regulator. Pool return restores stock; failed restoration discards
the already-deleted instance rather than reusing modified gear.

Radio choices: restore stock, 0-4-0, 0-6-0 and 0-8-0. Each non-restore choice
uses the UMM size captured when selecting the car. Only S060s can be targeted;
each controller owns its S060 mode/highlight. UMM offers size, apply/restore,
random spawning and individual pool toggles. Random spawning defaults off,
0-8-0 defaults out of the pool, and old saved cars remain stock unless they
already carry a saved mod choice.

Validation: real B99 assembly compile, zero warnings/errors; 99 offline behavior
checks including save round trips, pending intent, malformed/future payloads,
identity reuse, all spawn-pool combinations, and actual UMM XML serialization;
27 metadata/package checks verify native patch signatures, radio interface,
excluded S282 implementation and output hashes. The installed Harmony lacks
UnpatchSelf; failure cleanup uses its supported UnpatchAll(mod ID).

**Not runtime verified:** radio input/display, save/load/streaming/pool behaviour
and S282 coexistence. James still needs to test these with 0.4.0 after an
explicitly authorized install. Use `docs/FUNCTIONAL-TESTING.md`. Original rod-sync,
resizing/rail-contact and cylinder-placement questions remain open. Build/metadata
checks do not prove those behaviours. No game launch or install was performed.

A Derail Valley mod that changes the wheel arrangement of the **S060**, the B99
0-6-0 tank engine, at runtime — the Rearranged S282 idea applied to the shunter.
Forked from the Rearranged S282 B99 port at `65c8e8a`, MIT, Copyright (c) 2023
Peter Schinske.

- Repo: `B:\GitHub repo\rearranged-s060`, branch `rearranged-s060`, public at
  `github.com/james-taplin/dv-rearranged-S060`.
- Remotes: `origin` (this mod), `s282` (james-taplin/dv-rearranged-S282) and
  `s282-upstream` (pjschinske) for cherry-picking.
- Build: `powershell -ExecutionPolicy Bypass -File .\Build.Stage1.ps1` →
  `build\stage1\`, with a hash receipt. It installs nothing.
- Installed in game: **0.3.0**, DLL SHA-256 `47F12FE2CD489FF3…`. Earlier builds
  are backed up under `Desktop\Derail Valley Mods\RearrangedS060\`.
- House rule: **never copy into the game's Mods folder without James's explicit
  go-ahead**, and he runs the game, not us.

## How it is built

Stage 1 (the default) is the spike build: a mod-manager window
(Ctrl+F10 → Mods → expand the entry), no comms radio, no persistence, no spawn
hooks. The only Harmony patch is the inherited `Bogie.Axles` filter, which drops
deactivated axles from the rebuilt list. The inherited S282 geometry (the
2,736-line `WheelRearranger.cs` and friends) still compiles but is never reached.

Our own files:

| File | What it does |
|---|---|
| `S060Parts.cs` | Resolves body, both `DrivingMechanism_L/_R`, bogies, wheels, axles, PoweredWheels. Maps axles to wheels **by car-local Z**, because the S060's axles are all named plainly `[axle]`. |
| `S060Conversion.cs` | Base: one conversion per car, stationary gate, all-or-nothing rollback, axle/wheel pairing, and `S060FollowPose` so added parts copy an animated part's pose in `LateUpdate`. |
| `S060DriverScale.cs` | Scales both `DrivingMechanism` transforms (wheels + rods + gear + cylinders together). Hides brake shoes and calipers at non-stock sizes. |
| `S060FourCoupled.cs` | 0-4-0: drops the middle driver. |
| `S060EightCoupled.cs` | 0-8-0: adds a fourth driver behind driver 1. |
| `S060Rescaled.cs` | Stock 0-6-0 at a different driver size, nothing else. |
| `S060SpikeWindow.cs` | The window: driver-size slider plus the conversion buttons. |
| `B99PhysicsState.cs` | Inherited, now takes the expected powered-axle count (S060 = 3, S282 = 4). Does all the sim work: powered wheels, `poweredAxles.NUM`, wheel radius, adhesion, axle counts. |

## Measured facts (do not re-derive these)

From the read-only probe mod `RearrangedS060Probe` (source under
`Desktop\Derail Valley Mods\Mods (code)\RearrangedS060Probe`, installed in game,
reports in its own folder). Game `99-build2702`.

- Drivers: **1.424 m over the tread** (rolling radius 0.712128), 1.527 m mesh
  over flanges. Same wheel size as the S282's.
- Driver car-local Z: **−1.3978, +0.1498, +1.7006**. Spacing 1.5476 and 1.5508.
  Rigid wheelbase 3.0985 m. Mass 25,230 kg. Cab is at **−Z**; +Z is the front.
- Physics axles split **2 + 1**: `Axle_F` (pivot +0.929) holds drivers 3 and 2 at
  `distanceFromBogiePivot` ±0.775; `Axle_R` (pivot −1.396) holds driver 1 alone.
  **`Axle_R` cannot give up its axle** — that is why the 0-4-0 drops driver 2.
- Only **7 colliders** on the car. The body box (Z −4.200..+4.000, Y 0.420..3.820)
  already contains the drivers, so it does not constrain placement. The coupler
  boxes at **Z ±3.470..4.470, Y 0.825..1.275** do, as does the valve gear, which
  reaches **Z +3.460**.
- Clear low space: **1.31 m behind driver 1**, **1.01 m in front of driver 3**.
  A full-size wheel centre can sit between Z −2.758 and +2.758.
- `PoweredWheelsManager` **is** used here (unlike the S282, whose TODO says
  otherwise), and `[sim]/poweredAxles` reads `poweredAxles.NUM` into traction and
  `WheelslipController`. Both must be updated when the driver count changes;
  `B99PhysicsState` does it.
- Animation: two `Animator`s, one per mechanism, running `LocoS060_Drivetrain`
  with clips `LocoS060_AnimationLow/Med/High` driven by float parameters
  `SpeedMultiplier` and `Cutoff`. Added parts are not in those clips.
- LocoMeshSplitter 1.1.9 is live on the car as `S060MeshLoader` and exposes
  `GetCylinderLMesh`/`GetCylinderRMesh`, `GetPilotMesh`, `GetBumperMesh`,
  `GetRearSubframeLeft/RightMesh` and **per-axle brake calipers**
  (`s060_caliper_l_1..3`, `_r_1..3`). There is no `MoveCylinders` helper for the
  S060, only for the S282.

## State: what is proven and what is not

**Proven in game (20 Oct):**
- 0-4-0 converts, restores and **rides well** — driven light engine at full
  regulator and full cutoff. This settled the open question:
  the bogie pivots can be left where DV put them, with an axle offset from its
  pivot. Everything since relies on that.
- 0-8-0 converts cleanly. Probe confirms `poweredAxles.NUM = 4`, two axles on
  `Axle_R`, 4.648 m wheelbase. No exceptions in the log.
- No extra wheelslip on the 0-4-0 light engine at full regulator and full cutoff,
  **which is correct**: a tank engine has all its weight on coupled axles, so
  total adhesion is the same on two axles as on three. Only per-axle load changes.

**Not verified:**
- **Driving status: 0-4-0 driven successfully once; 0-8-0 and 0.3.0 resizing not
  yet tested in motion.** The single most important unknown is **rod sync**, and
  it only exists on the 0-8-0, so it has never moved under power:
  the 0-8-0's extension rod and fourth wheel are not in the native clips and copy
  an animated part's pose in `LateUpdate` (`S060FollowPose`). If that stutters,
  lags or drifts, it blocks every later arrangement.
- The driver-size slider (0.3.0) has not been tried at all.
- Whether the fourth driver clashes with the rear coupler trigger box in practice.

**James's verdict on the look:** the 0-8-0 at stock size is cramped — four
1.527 m wheels at 1.549 m centres leave 22 mm between tyres. He asked for either
smaller wheels or the cylinders moved forward. 0.3.0 does the first.

## Next jobs, in the order they make sense

### James's requested direction, 20 Oct 2026 (Codex handover follow-up)

James wants a functional Rearranged S060 with its own comms-radio mode and UMM
options similar to the S282 B99 fork. Requested scope includes finishing the
0-8-0 and resizing, adding 0-6-2, and a separate Meyer compound tank locomotive
with two swivelling powered bogies, reusing S060 assets/textures where supported.
The Meyer wheel arrangement has not been specified; 0-4-4-0T is a proposed first
layout, not an agreed requirement. Asset alias coverage remains unverified.

Proposed delivery order: validate existing conversions in motion; adapt the
S282 radio/settings/save lifecycle to S060 conversions; add the trailing truck;
then build the separate Meyer vehicle. The radio must coexist with the S282
mode and target only S060s. Save the arrangement and driver size per vehicle;
verify reload, streaming, restore and coexistence before calling it functional.
Current stage-1 build still has no radio, persistence or random-spawn hooks.

Important source finding: the current S282 fork's
`RearrangedS282/ArticulatedSimulation.cs:147` explicitly excludes compound
expansion. Its extra-engine graph is a reference, not proof of a working
high-pressure/receiver/low-pressure compound steam circuit. Check native sim
components and working compound examples before adding a custom component.

Current test request: stock-size 0-8-0 forwards/reverse, slow/normal yard speed,
coasting/braking, curves/points and coupled movement; observe both sides' added
wheel/rod sync and rail contact. Separately resize stock 0-6-0, then 0-8-0, to
about 0.90 and 0.80, restoring between conversions. Observe rail contact on both
sides, rod alignment, cylinder placement and rotation versus travel. Non-stock
size intentionally hides brake meshes; braking itself still needs validation.
Whole-mechanism scaling includes lateral scale, so rail contact/gauge is a test
question, not a confirmed success. Restore stock and check geometry and driving.
No new code or installation has been performed during this planning discussion.

1. **Cylinder shift.** Scaling drags the cylinders towards the car centre (~0.5 m
   at 0.80), and moving them back out independently is the obvious fix. The
   inherited `DriveRetarget.cs` does exactly this for the S282 — shifts the
   cylinder-side parts and stretches the rods spanning the gap, without touching
   the animation — but it is behind `#if B99_ALL_STANDARD` and written against
   the S282's hierarchy, so it needs adapting. **Ask James first whether the
   shrunk-together look actually bothers him**; he may not want this built.
2. **0-6-2 trailing truck.** 1.31 m of clear space behind driver 1, and the
   splitter can move the rear subframe. It needs the S282's 0.773 m
   `s282_wheels_front` mesh, which means packaging the runtime AssetStudio
   extractor the way the S282 mod does (16 MB, in its mod folder). Do not commit
   extracted game meshes to the public repo — the upstream author extracts on the
   user's machine for that reason.
3. **0-10-0**, once rescaling is judged good: five drivers at ~1.2 m need 5.20 m
   of the 5.74 m band that size gives.
4. **0-2-2-0 Franklin duplex**, the joke James wants: the fork already carries the
   S282's duplex sim (`Sim/Duplex*.cs`, `Patches/DuplexSimPatches.cs`,
   `Patches/AddTwoCylinders.cs`) and the Franklin cylinder meshes in `models/` and
   the `s282_drivetrain` bundle — but the csproj currently excludes those sim
   files from the build.
5. **P4 shell**: comms radio, saved arrangements, spawn pool, diagnostics, tests,
   all portable from the S282 mod.

## Conventions worth keeping

- Cite `path:line` or a probe report line for any claim; mark anything unverified
  as unverified.
- Every build gets a hash receipt; every install gets a backup of what it replaced
  and a hash check afterwards.
- The validators (`Validate.Stage1/2.ps1`) need PowerShell 7, which is not
  installed on this machine. They fail identically in the S282 repo — inherited,
  not a regression.
