## 1.7.4 curve diagnostics fixes (Claude), RELEASED pre-release v1.7.4 (tag 6c3eaa2), INSTALLED, 19 Oct 2026

Installed from the released ZIP (DLL DB903797...87C93 == release, Info.json 1.7.4).
1.7.3 backup: build/runtime-trial-1.7.4/installed-backup-20261019-175717. Settings kept.

Diagnostics only; driving is unchanged from 1.7.3. James asked for this as the gate before Stage 2.
- Duplicate drivers: DescribeDriverGroups now skips names containing "LOD".
  The 1.7.2 4-14-4 capture listed 11 drivers for 7. The extra four were
  `..._LOD1` siblings, with a mesh other than the flanged one. That cause is
  inferred from the names and Articulated.cs:194, not verified in game.
- Scrub model: the old formula saturated at mu * W per axle once the angle
  reached 0.005 rad. That gave about 16 kN at about 2.5 km radius, and would
  give over 100 kN on tight curves.
  - New per-axle term: mu * W * (max(0, angle - 0.1 deg) in rad + 0.7175 * |curvature|),
    which is the along-track part of the sliding friction.
  - Hand estimates (UNVERIFIED): 4-14-4 at R=150 m gives about 2 kN; straight
    track gives about 50 N.
- New "reference" block per sample: mean bogie radius and the Rockl whole-car
  curve resistance (650/(R-55) N/kN for R >= 300 m, 500/(R-30) N/kN below),
  for calibrating against. At 150 m that is about 4.5 kN for 111 t.
- Session assumptions now log angleDeadbandDeg, halfGaugeM, rocklReference
  and the drivers source.
- Calibration capture still needed. Use the same track and speed for each run:
  straight, broad, tight and S-curve, on the 4-14-4 or 2-12-2, the 2-8-2, the
  2-6-6-2 and the triplex.
- James's clip of bogie knocking: I couldn't identify the cyan overlay. The
  motion looks like the native bogie (track-placed) against the body
  (on joints). Not changed.
- Validation: integration 136, additional 1,891, catalogue 1,366, articulated
  simulation and power diagnostics passed. ZIP SHA256
  59bc28ce11d8ccdb9f8766fc1c3f369d4c00273a5bdd0c5bd2b01cfbc77356e4, DLL DB903797...87C93.

## 1.7.3 driver-weight cap (Claude), RELEASED pre-release v1.7.3 (tag 6d43064), INSTALLED, 19 Oct 2026

Installed from the released ZIP (DLL 0B742F4B...F275E4 == release, Info.json 1.7.3).
1.7.2 backup: build/runtime-trial-1.7.3/installed-backup-20261019-175014. Settings kept.

James approved option A with these targets after the 1.7.2 runs.
- 1.7.2 results: the structural test passed all 40 layouts
  (Mods/RearrangedS282/stage2-runtime-checks.json, version 1.7.2.0, subject
  L-050). Power capture power-20261019-163827-276.jsonl has only the 4-14-4
  (216 s, low regulator, no full-regulator start). Player.log has no
  RearrangedS282 errors; its exceptions come from GlobalShopController.OnDestroy
  and DoubleTrack OnSaveGUI at quit.
- Curve diagnostics findings (diagnostics only, UNFIXED):
  - The 4-14-4 rigid group lists 11 driver entries for 7 drivers. The first
    four z positions appear twice, flanged in one copy and blind in the other.
    Likely cause: LOD1 meshes also match "s282_wheels_driving_" (unverified).
  - proposedScrubN is about 15.9 kN at about 2,500 m radius, from an angle of
    attack of only 0.05 deg against SaturationAngleRad .005 (CurveDiagnostics.cs:20,136).
  - Stage 2 needs a deadband or a curvature-scaled term before any force is
    applied.
- Cause of the grip: DV gives each axle TotalMass / NumberOfAxles
  (TrainMassController.cs:32). Mass stays at 111 t, so short layouts carried
  28-37 t per driven axle. The 2-4-0 and 2-6-0 had a starting pull / grip ratio
  of about 1.03.
- Change: ConventionalPhysics.MaxDrivenAxleLoadKg caps the load per coupled
  group at 18 t (single), 19 t (four-coupled) and 20 t (six-coupled); larger
  groups are uncapped. DriverWeightFraction = min(equal share, cap / CarMass).
  Articulated layouts are capped per engine. The cap only ever lowers the
  fraction. WheelRearranger.Conventional.cs feeds it to the existing
  FrictionForDriverWeight; mass is unchanged.
  - Results: 2-2-2 18 t, 2-4-0 / 4-4-0 / 4-4-2 38 t, 2-6-0 / 4-6-0 60 t,
    triplex 54 t; everything else unchanged.
  - New log line "Driver weight: X t on N driven axles (equal share Y t of 111 t)".
- Validation: integration 136 (1.7.3 DLL), additional 1,891, catalogue
  1,366, articulated simulation and power diagnostics passed. The new cap
  cases are in Validate.Stage2.ps1, but that script needs pwsh 7, which is
  not installed. They were run instead in a net8 harness (scratchpad
  capcheck), and all 16 cases plus the rejects passed.
- ZIP SHA256 3fe6302d35d40157f3ae418618792c005064c308afb3a5f80f97dc2727fd0cf4,
  DLL 0B742F4B...F275E4.

## 1.7.2 fixes + curve diagnostics Stage 1 (Claude), INSTALLED 19 Oct 2026

Installed from the released ZIP (DLL 1A18CC94...6D352 == release). 1.7.1 backup:
build/runtime-trial-1.7.2/installed-backup-20261019-151609. Settings kept.

RELEASED as GitHub PRE-release v1.7.2 (tag on 54b8690), ZIP SHA256
02f33c477d4d5a71ee6c7007d933a39d48cd18fb894afc43ed3dbc832be96550. Install from that ZIP.

James's 1.7.1 run: triplex had no visible cylinders, he wanted trailing trucks
back and bigger wheels; 4-14-4 cylinder-cock jets far forward. Also approved
curve-dynamics Stage 1 (diagnostics only). Feasibility report (read-only, local
only): evidence/curve-dynamics-feasibility-20261019/FEASIBILITY.md.
- Player.log render reports: Franklin cylinder enabled=False lodGroups=0 after
  the Mallet had been used (fine before). BindArticulatedLods now: native coarse
  = LOD1+ renderers not drawn as detail by ANY group and not under generated
  roots (GeneratedPresentationRoots); recorded once; restored in reverse. Franklin
  renderers (front pairs, rear Franklin/branches/reverse, triplex clone copies)
  are force-enabled when shown (EnableGeneratedRenderers).
- Triplex back to s22222 "2-2-2-2-2" with trailing axle at the 12-coupled
  position (1.7.1 log: scale .755 with no truck; expected ~.70 now).
- Jets: PacificHudsonAffine.EmitterCentreLocal (mean of leaf descendants).
  4-14-4 holders, Pacific/Hudson position-only holders and articulated
  ParticleBinding leaves now move so the emitter centre maps, not the origin
  (CylCockSteam/WaterDrip origins are at the car origin; LMS MoveCylinders).
  Pacific fixture holders are childless, so it does not exercise this path.
- CurveDiagnostics.cs (Stage 1): PowerDiagnostics.CurveSampler/CurveSessionInfo
  hooks (null in the PowerDiagnostics fixture). Per sample: bogie track/span/
  yaw/sampled curvature, inter-bogie curvature, native stress/slowBuildUp/
  derailBuildUp/threshold/rolling N/wheelslip, groups from
  WheelRearranger.DescribeDriverGroups (visible left drivers; flanged = mesh):
  rigid fit, pivot fit (+-6 deg, 25 steps), effective-wheelbase estimate,
  proposed (unapplied) scrub/flange N. No offline fixture for the geometry
  (native Bogie/PointSet); validate with a capture.
Validation: all suites pass (integration 136, catalogue 1,366). No game run.

## 1.7.1 triplex rework (Claude), INSTALLED 19 Oct 2026

Installed from the released ZIP itself (no rebuild): installed DLL equals the
release DLL 4595C66A...EB354. 1.6.1 backup (DLL 4D787B79...):
build/runtime-trial-1.7.1/installed-backup-20261019-124612. Settings kept.

RELEASED as GitHub PRE-release v1.7.1 (tag on e4e9c3a):
https://github.com/james-taplin/dv-rearranged-S282/releases/tag/v1.7.1
ZIP SHA256 610549780e9e69d90a8516b73761e64acea083ed6830ec8d596bcc97fb353dd4,
DLL 4595C66ADA6445EBAA9D9F701C8A28B1378751B1B543B213EFC9F70A364EB354.
Install from this exact ZIP (not a rebuild) so installed == released.

James saw the 1.6.1 triplex in game (0.53 scale: tiny wheels, very long rods)
and chose "2-2-2-2-0, short rods". ID 38 renamed s22220 "2-2-2-2-0 Articulated
Triplex" (toggle spawn22220; NumOfNondriven 2). Engines now reuse the 2-4-4-2
Franklin engine (EnsureArticulatedAssets first; frontFranklin on the front drive,
cloned franklin_type_b(Clone) on middle/rear clones, FranklinBAnimCtrl, duplex
short main rod, two-axle rod hidden) showing only driver 2. LMS cylinders/lifting
supports hidden; static parts per engine = duplex guides (0.065, articulated
guide meshes), caliper 2, spring 2. MeasureTriplex uses the Franklin cylinder
front (FranklinCylinderFront, ~10.64 drive z) to driver 2, rear limit = rear edge
of the 12-coupled trailing wheel, no trailing truck; expected scale ~0.70.
Front AnimatorPortReaders paused like the 2-4-4-2. MalletAnimatorMirror now
copies only parameters both controllers declare. Atlantic-size (2.07 m) drivers
were explained as impossible for three engines in the S282 frame.
Validation: all suites pass (catalogue 1,366, integration 136). No game run.
1.7.0 (4-14-4) was NOT installed: the game was still running.

## 1.7.0 4-14-4 built (Claude), NOT installed, 19 Oct 2026

RELEASED as GitHub PRE-release v1.7.0 (tag on e5b976b):
https://github.com/james-taplin/dv-rearranged-S282/releases/tag/v1.7.0
ZIP SHA256 9b0639d086f3a44af5a6993ab8853796bfa197615c3a0de6124f1de6bace979f,
DLL 1D49A40919F31862947800F584DE7AE70EE27002CAA869D0B50E6C17791B22BC. Not installed.

New ID 39 = s4144 "4-14-4", CONVENTIONAL (IsConventionalId includes 39; random
default off). WheelRearranger.FourteenCoupled.cs: Measure4144 scales the native
drive + LMS cylinder assembly (MapMalletPart, front cylinder face kept) so seven
drivers (native z plus z7 = z6 - pitch) fit ahead of a two-axle trailing truck
ending at the 12-coupled trailing position; leading axles placed just ahead of
driver 1. Expected scale ~0.68. Seventh driver wheels/axle ("[axle] drive 4" in
rear bogie)/rod (12-coupled six rod copied one pitch back)/caliper/shoe/sparks
are created lazily (Ensure4144Assets) and hidden by Suspend4144Geometry, called
in TrySwitchConventional and RestoreConventionalStock before GeometryState
restore (not inside Additional.cs: its fixture compiles that file alone).
Spark/brake-heat arrays re-added per apply (prepared restore resets them).
All 7 physical axles aligned to the visible right wheels. Particle holders
position-mapped only. Powered list now includes driveAxle7.
Validation: catalogue 1,366, integration 136, all suites pass. No game run.

## 1.6.1 duplex rear guide fix (Claude), INSTALLED 19 Oct 2026

RELEASED on James's request as GitHub PRE-release v1.6.1 on the fork:
https://github.com/james-taplin/dv-rearranged-S282/releases/tag/v1.6.1
(annotated tag on a7d9aeb; asset RearrangedS282-B99-1.6.1-Test.zip, SHA256
432a4c0ea0225f6f28a7310778be51e7d1f9c219cc3028fb4655f3437af35b00, contains the
installed DLL 4D787B79...). Marked pre-release: no in-game acceptance yet.

Installed on James's go-ahead; install rebuild of the same clean source gave DLL
4D787B79806A99FC1B306E4936EEFCC8CC7BFFC2058023DCBFD063B24B7F1D76 (integration
checks rerun on it). 1.5.1 backup: build/runtime-trial-1.6.1/installed-backup-20261019-114824.

James's 1.5.1 run: 0 LODGroup warnings and both duplexes show front cylinders,
so the LOD fix is confirmed in game. Black structure behind the cab remained.
Render report only flagged rear spring clones because LMS meshes keep the whole
body vertex buffer (renderer bounds span the loco); report now measures drawn
triangles of renderers under the articulated roots. Root cause found in code:
ApplyArticulatedGeometry set rearGuideL/R.localPosition = (0,0,-4.58), an
upstream value authored for clones parented to splitLoco (upstream line:
lrCrossheadBracket = Instantiate(lfCrossheadBracket.gameObject, splitLoco)); the
port's clones live under articulatedRearRoot, ~4.88 m offset. Now
SplitBodyOffsetInRearRoot() + (0,0,-4.58). Rear brake clone height had the same
mix-up (y=0 in the wrong frame). Includes all 1.6.0 triplex work.
DLL ADC24E9F1074A6030EA3503A7642A5AC9493E17C2B859E2E80A053D82D1FAB90; suites pass
(graph 40, catalogue 1,358, integration 130). Not verified in game.

## 1.6.0 2-2-2-2-2 triplex built (Claude), NOT installed, 19 Oct 2026

Built while James tests 1.5.1 (installed); do not install over his test.
James also had the CR-style smoke deflectors mod moved to the Recycle Bin.
New ID 38 = s22222 "2-2-2-2-2 Articulated Triplex" (NEW_LAYOUTS_PLAN Phase 2).
Simulation: every S282 graph now has a dormant third engine
rearrangedMiddleSteamEngine (copy of the rear aux definition), sums take
MAIN/REAR/MIDDLE, damage/health IDs appended, LoadLegacyState adds empty
rear AND middle state when missing. Apply(38) activates rear+middle; other
articulated IDs activate rear only. IsDuplexId literal 31/32/37/38.
Effects: bindings now hold one copy per secondary engine (rear, plus middle
when active); outlets via TryMapArticulatedOutlet(world, engineIndex 0/1/2).
Geometry (WheelRearranger.Triplex.cs): MeasureTriplex picks the largest scale
fitting 3 engines (cylinder front -> main driver d3 = 5.48 m native) between the
front cylinders advanced 0.5 m and the 12-coupled trailing-axle position;
~0.53 expected. Single-driver engines show d3 only (no coupling rods). Front =
native drive; middle/rear = cloned drives + LMS static clones. Middle frame has
its own Position/Aim constraints (aim = 0.4 Axle_F + 0.6 rest point);
articulatedPin is reparented under the middle root so the front hinge follows.
Frame split is now N-way (pins list). Powered axles: driveAxle1 front, 2 middle
(both front bogie), 3 rear; 5 axles total. Powered-count formula generalised to
sum of middle Whyte entries (TrySwitchConventional and Stage2RuntimeChecks).
Validation: GraphProbe updated (40 checks), RandomLifecycle 1,358,
ArticulatedIntegration 130, all other suites pass. No game run.
DLL 9DD5FAEF86F4E83BA9EC9AA4B56CC3EE444CD6773ED13F9D013BF3E4799E9DBD.

## 1.5.1 articulated LOD fix + render report built (Claude), INSTALLED 19 Oct 2026

Installed on James's go-ahead; install rebuild of the same clean source gave DLL
C74CE6E714E322F3726D5ECE3DC1B76DC8D2909C643B95C816AB8BC0B1066A43 (125 integration
checks rerun on it, pass). 1.5.0 backup: build/runtime-trial-1.5.1/installed-backup-20261019-110514.

James's 1.5.0 run: Mallet renders and works well. 2-4-4-2/4-4-4-4 show a large
black structure jutting out behind the cab, and the 4-4-4-4 front cylinders are
missing (his 2-4-4-2 screenshot still shows a front block). Player.log after every
articulated switch: ~555x "Renderer ... registered with more than one LODGroup"
pairs. Old BindArticulatedLods put EVERY front/rear-root renderer into EVERY
LODGroup under the body, including each drivetrain's own group and the (inactive)
rear drivetrain clones' groups, and also promoted active native LOD1 proxies to
detail. Fixed: one owner per renderer (native membership, else nearest enclosing
group, else body group); native coarse proxies excluded. Bundle check (UnityPy on
assets/s282_drivetrain): franklin_type_b and _4444 both contain an active
"cylinder" at drive (1.09,.71,9.49); _4444 root at z .35. So the asset is fine;
duplicate LOD culling is the leading (unconfirmed) cause. Jutting structure NOT
identified: not in franklin/branch/reverse prefabs (all drive-frame z 4.9..9.8,
y > 1.4). New LogArticulatedRenderReport logs every visible renderer reaching
>0.5 m behind the subframe's rear end, plus front Franklin cylinder state.
Offline suites pass (none exercise LOD binding). DLL CA2823AA...87808D0.

## 1.5.0 2-6-6-2 Mallet built (Claude), INSTALLED 19 October 2026

Installed on James's go-ahead (game closed). Install rebuild recompiled the same
committed source (git clean) to DLL 824D3FDA396AA96D76ADEBD09C1B110BACE5A2224508F4DD0A0827EEA77447E2
(earlier candidate hash 2316F171... differed only by recompilation); the 125
compiled integration checks were rerun against the installed DLL and pass.
1.4.5 backup (DLL 3ADA1658...): build/runtime-trial-1.5.0/installed-backup-20261019-100918.
No game run yet.

James's 1.4.5 structural run: 37/37 pass, no Pacific/Hudson "moved only" log line,
so the 1.4.4 shear came from a particle holder (now position-only). Everything
rendered right. Open: 2-2-2 did not slip at full regulator/cutoff. By native
WheelslipControllerSystem/AdhesionControllerSystem it compares force per powered
axle with mu*weightPerAxle; 2.8 m halves force but 1 of 3 axles doubles weight per
driver vs stock 4 of 6, so threshold ~= stock. No power capture of that drive yet.
Real singles carried 15-20 t on the driver; offered a lighter 2-2-2 driver load.

New ID 37 = s2662 "2-6-6-2 Articulated Mallet" (plan: NEW_LAYOUTS_PLAN.md, Phase 1).
WheelRearranger.Mallet.cs:
- Each engine = native drivetrain + LMS static cylinder assembly (cylinders,
  cocks, guides, lifting supports, calipers 1-3, springs 1-3, cylinder collider
  shells) under ONE body-frame map: Y/Z scale 0.75 about rail-level origin, then a
  Z offset. Front offset keeps the native cylinder front face; rear cylinders sit
  0.1 m behind the front last driver. All measured at runtime (MeasureMallet).
- Drivers 1-3 per engine; d1 moved to equal pitch so the existing three-axle rods
  (6-coupled d2-d4 pieces) are shifted forward one pitch. Main rod = native d3.
- Rear engine: lazily cloned drivetrain (behaviours stripped) + static clones under
  articulatedRearRoot, tracked in malletAssets (separate from articulatedAssets).
  MalletAnimatorMirror copies the front animator's parameters (cutoff/reverser).
- Physical axles: driveAxle1/2/6 front bogie, 3/4/5 rear; leading = native, one
  small trailing axle placed behind rear d3. 8 axles, 6 powered.
- Pin = midway between front d3 and rear cylinder front; same constraints.
- ArticulatedEffects outlets go through WheelRearranger.TryMapArticulatedOutlet;
  legacy 2-4-4-2/4-4-4-4 offsets unchanged when the Mallet is not active.
- ArticulatedSimulation.IsDuplexId now 31/32/37 (literal: graph probe compiles alone).
Unverified in game: rod shift direction, rear valve gear following cutoff, rear
crosshead/guide alignment, trailing wheel clearance to cab, effects positions.

Validation: native build 0/0. RandomLifecycle PORTED from Windows PowerShell
Add-Type (needed PS7 $PSHOME\ref paths; failed here even on HEAD) to a dotnet
project; 1,350 checks incl. ID 37. ArticulatedIntegration updated: 125 checks.
ArticulatedSimulation 40+31, ArticulatedEffects 9,014, geometry 3,947,
Pacific/Hudson 29,973, Additional 1,891, RestoredFeatures 286, RollingAudio 198,
PowerDiagnostics 81, Delta 129 all PASS. Validate.FrontCouplingMounts.ps1 still
uses Add-Type and cannot compile the catalogue under Windows PowerShell 5.1
(fails on HEAD too); not run. Build.RuntimeTrial default version 1.5.0.

## 1.4.5 candidate built (Claude), NOT installed, 19 October 2026

Claude took over from Codex. James approved both fixes, build only, no install.

Articulated IDs31/32 ROOT CAUSE (confirmed from LocoMeshSplitter 1.1.9 decompiled
S282ALOD0MeshSplitter): s282a_subframe_left/right is only the firebox section,
vertex z -4.287..-1.596 (body space). The main bar frame is s282a_frame_left/right,
z -1.67..6.48. The pin (~midway between front driver2 and rear-engine driver1) lies
ahead of the whole subframe, so the front clip was empty. Fix:
WheelRearranger.Articulated.cs now clips/hides ArticulatedSplitFrames =
s282a_frame_left/right; subframe stays boiler-fixed and visible. The error message
now includes the local pin and mesh bounds. s282a_frame_box_front still boiler-fixed.

Pacific/Hudson IDs4/5: the six-driver (1,s,s) stretch skews any part rotated off the
drive X axis. Offending part was not logged in 1.4.4 (still unknown). Fix in
WheelRearranger.Walschaerts.cs: particle holders (CylCockSteam/WaterDrip/CylinderCrack)
are position-only (pivot mapped, native rotation/scale), matching LMS translate-only.
Any other part that would shear falls back to position-only and logs
"Pacific/Hudson: <path> cannot be stretched without shear; moved only." once per
car. Decompose still throws on shear; new TryDecompose returns false instead.
Check Player.log for that line to learn which part sheared.

Missing trailing wheels: NOT fixed. Structural report shows both generated trailing
axles active and physical on every x-x-4. Pattern: every flagged layout puts one
trailing axle at rear bogie_car z -3.5/-3.7 (4-4-4 line ~2685, x-10-4 ~2717,
284Big ~2706). Estimated to land ~1m behind the loco frame end, under the tender.
That estimate ASSUMES Axle_R sits near driver 4 (not measured). Asked James to
check a 4-4-4 with the tender uncoupled.

Validation: native-reference build 0 warnings/errors. Pacific/Hudson fixture PASS
29,973 checks (outlet assertion changed to pivot-only; new TryDecompose checks;
fixture Stubs.cs gained a Main.Logger stub). Articulated clipper fixture PASS 3,947
(unchanged clipper; it never used real frame coordinates). Integration/IL fixtures
not rerun. No game run. Packaged docs (RUNTIME_TRIAL_TESTING/ARTICULATED_TEST_STATUS)
unchanged and still describe 1.4.4.
DLL SHA256 3ADA165826D113278C5E76E69EC897D2BB8B13DB610887E28C14F4D489D15C42.
ZIP: C:/Users/james/Desktop/Derail Valley Mods/RearrangedS282-B99-1.4.5-Test/
RearrangedS282-B99-1.4.5-Test.zip. Candidate: build/runtime-trial-1.4.5/.
INSTALLED 19 Oct 2026 on James's go-ahead (game closed). Installed DLL hash matches
above; Info.json 1.4.5; Settings.xml kept. 1.4.4 backup (DLL F0729132...)
at build/runtime-trial-1.4.5/installed-backup-20261019-082743. No game run yet.

GIT: work committed as 7bb0fbf on branch b99-port and pushed to James's PUBLIC
fork https://github.com/james-taplin/dv-rearranged-S282 (remote "james"; origin
is still upstream pjschinske). Fork default branch is still upstream main.
evidence/ is gitignored except the offline test fixtures and the two files the
scripts read; logs, tester material and video stay local. Commit new work on
b99-port and push to james. Do not push to origin.

## 1.4.4 runtime structural acceptance FAILED, 19 October 2026

James ran the expanded structural trial. Installed/report version1.4.4 (assembly
1.4.4.0), installed DLL SHA256 F072913235E6C500B81044218F312D6B3AE926FFE263CD731757C156A969AEBE.
Latest report: subject L-026, control L-062, 33/37 passed. Failures:
- IDs4/5 Pacific/Hudson: PacificHudsonAffine.Decompose rejects the native support
  transform with "Pacific/Hudson native support cannot be mapped without shear."
  This is the new polish mapping, not evidence that native Walschaerts cannot work.
  Offending part/matrix is not recorded, so precise numeric/transform cause is unknown.
- IDs31/32 articulated: BindSplitArticulatedFrame rejects s282a_subframe_left with
  "S282 frame does not span its measured articulation pin" (empty clipped half).
  Inspect actual source mesh coordinates versus measured pin before changing the
  guard; this does not establish a gap in native articulation/traction support.

Both errors repeat in Player.log, including outside the automated run. Failed
conversions restore their captured stock state exactly. IDs34/35/36 (2-6-0,
2-4-0, giant222) pass structural checks; giant222 reports2.8m diameter and0 driver
colliders. These are stationary checks, not driving/geometry acceptance.

James also saw missing trailing wheels on unspecified layouts. Existing axle counts
are correct on passing entries, but the test does not capture mesh visibility/LOD.
Do NOT dismiss the observation as a false alarm from passing counts. Rear small
axles clone the front axle and explicitly disable axleF_modelLOD1; inspect actual
LOD0/LOD1 renderer states and a working native wheel before claiming a cause/fix.
James clarified that several x-x-4 layouts appeared to have only one trailing axle,
especially layouts he recalled as x-12-x/x-14-x, plus the entry immediately before
4-6-0. The latter is confirmed from the current name table as4-4-4 Reading (ID2),
which should have two trailing axles. Current build has no14-driver choices; its
2-12-2/4-12-2 correctly have one trailing axle by design. Preserve this distinction:
do not treat all one-axle observations as missing geometry or invent extra layouts.
Distance/LOD not specified. ID2 and the other x-x-4 paths explicitly activate both
secondRearAxle and thirdRearAxle at distinct positions; this rules out a simple
missing SetActive call in those helpers but not later state/renderer/LOD defects.
Active settings use small_wheels for x82/x102. Visual failure remains unresolved.

The extra power capture contains52 stationary samples over2.9s, stock Mikado only,
no escaping simulation-update faults; it provides no driving/articulated evidence.
Raw report, both Player logs and power capture preserved with SHA256 manifest in
evidence/structural-1.4.4-20261019. Other mods log unrelated startup/cleanup errors;
none explains the four explicit RearrangedS282 conversion failures. No production
code edits, build or reinstall performed during this inspection. Prior offline
passes below do not supersede the failed runtime acceptance.

## 1.4.4 expanded trial installed, 18 October 2026

CURRENT USER SCOPE COMPLETED IN SOURCE/TEST PACKAGE: polish Walschaerts on4-6-2
and4-6-4 first; add2-6-0 and2-4-0 together; add a deliberately unfinished2-2-2
with massive non-colliding drivers to explore geometry; install a test build.
1.4.4 is installed. No game acceptance claimed. No triplex/4-14-4/new steam model.
All older features and original IDs0-33 retained; other previously disclosed parity
differences (custom stock appearance, old derail-threshold transpiler) remain.

Native-first gear decision: inspected native High/Med/Low cutoff clips and LMS
geometry. All three clips bind crosshead/linkage scale, so the old Show6Drivers
crosshead scale edit is overwritten by the Animator. The scaled animated mechanism
is internally coherent; its static cylinders/guides/supports were not receiving
the same mapping. New WheelRearranger.Walschaerts.cs captures authored part frames
relative to the native drivetrain before preparedGeometry. Apply maps existing LMS
LOD0/LOD1 cylinders/chests, cocks, guides, lifting bearings, particle holders and
native walk/item cylinder shells to the same six-driver Y/Z1.2879/Z-2.1 mapping.
Four per-car cloned LMS dry-pipe meshes blend the cylinder mapping toward the fixed
boiler end. Shared source meshes remain immutable; native animation controllers,
ports, cutoff, phase and physics/cylinder parameters are unchanged. Long cab reach
rods and mechanical lubricator linkage remain hidden, including unused support at
both LODs. This is first geometry polish, not a new complete custom linkage retarget.
Pipe tests pin highest/lowest-Y endpoint extrema; they do NOT prove every vertex
of an angled socket ring remains rigid. Final pipe seating needs game inspection.
Setup catches optional preparation failures; only requested polish rejects if not
ready. Geometry snapshots restore poses/meshes on switching/pooling; per-car meshes
dispose on deletion. Local geometry disposal now precedes OnDestroy's Unity-null
TrainCar guard so separately allocated meshes are still freed after car destruction.

Settings.polishedPacificHudsonValveGear=true enables polish for IDs4/5, independently
of the saved old show6CoupledValveGear=false. Disabling the new setting and reapplying
restores old behaviour; other six-coupled layouts keep the old global show6 toggle.
No unselected stock car/prefab/shared material is edited by this new geometry path.

New stable appended IDs34=s260(2-6-0 Mogul),35=s240(2-4-0),36=s222(2-2-2 Giant-driver
clearance prototype); SupportedCount37. Original ID0 remains s440; do not activate
the old commented s240=0 line. Existing original standard helper remains0-30 for
historical tools; IsConventionalId includes the new IDs, active save/radio paths use
IsSupportedId. New260/240 reuse Show6/Show4Drivers plus one leading axle/no trailers.
Their retained native physical axles/PoweredWheel anchors align to the visible
right-side driver transforms before native axle-array/physics commit. New normal
choices default true in the random pool; total default ordinary pool33. Random
spawning still defaults off; giant222/articulated/404 choices remain separate opt-in.

WheelRearranger.Additional.cs giant222: default2.8m tread diameter, normal UMM slider
2.1-3.6m, finite/clamp enforcement on apply. Reapply after adjustment. It creates
only two GameObjects/MeshFilters/MeshRenderers plus the unchanged LateUpdate rotation
follower, using shared native flanged-wheel mesh/material. No Collider/Rigidbody/
PoweredWheel clone or enlarged native body collider. Parent drive frames retain
unit magnitude/mirrored X; radial renderer scaling preserves gauge width and moves
the tread centre by radius delta to retain rail height. Source third-wheel animation
drives angle; prototype centres are independent, so resizing does not cause orbit.
Original drivetrain meshes/unfinished rods/calipers are hidden, while Transform
hierarchy/Animators and all six oilcup providers remain active. Only native axle3
is powered, reindexed0; spark1 is moved to its contact and other spark pairs hidden.
Single leading/trailing small axles give total3. Native force/radius/RPM/adhesion
adapter is reused, with unchanged boiler/cylinder calibration and mass. This is a
clearance prototype, not calibrated final handling or rigging. Giant visual wheels
can visibly intersect body/equipment without generating wheel collision forces;
native body collision still exists. Flange outer diameter exceeds selected tread
diameter. No giant LOD/rod redesign or native body cutouts added. Diameter is a
global reapply setting; saved data retains arrangement identity, not per-car diameter.
SuspendAdditionalGeometry precedes every prepared/stock snapshot restore; Dispose
destroys only owned visuals, never shared native meshes/materials. Native wheel
Transform references restore through the existing GeometryState.

Stage2RuntimeChecks now covers37 entries. Prototype fields report configured tread
diameter, native radius consistency and zero descendant colliders; they are not a
measurement of outer mesh diameter or triangle clearance. Normal selection needs
no structural button. Test runner still requires two stationary STOCK S282s.

Validation, all passing (managed geometry tests are not Unity/game acceptance):
- Native B99-reference1.4.4 Release build:0 warnings/errors.
- Pacific/Hudson actual-source fixture:29,970 assertions across96 native cutoff-clip
  poses and1,344 native dry-pipe vertices, including both LOD supports, signed affine
  mapping, endpoint extrema, source immutability, reuse/restoration/disposal and
  optional failure paths. build/pacific-hudson-valve-gear-validation/validation.json;
  evidence/pacific-hudson-valve-gear/native-animation.json contains native samples.
- Additional actual partial + native rotation-follower fixture:1,891 assertions,
  including resize2.1/3.6/2.8/2.1, rail-centre pose, rotated bogie alignment, native
  providers/animators, no component cloning, dense sparks, restoration/disposal and
  second-car isolation. Many are repeated transform assertions, not independent
  game scenarios. build/additional-arrangements-validation/run-20261018-210421-901/.
- Actual catalogue/settings/save-policy fixture:1,340 checks with37 ID roundtrips.
- Compiled integration:120 checks, original0-33 IDs stable, appended routes34-36,
  optional gear wiring, minimal new visual components/no extra simulation, matching
  native targets, unchanged ConventionalPhysics/PoweredWheelRotater IL.
- Native-algorithm physics/explosion fixture:286 checks/28 transitions/11 explosions;
  extended to one powered axle. Actual adapter source unchanged from1.4.3.
- Rolling/brake audio:198 assertions/33 axle transitions incl total3,10 old-cache
  failures reproduced; coupling:608 assertions incl four37-entry sweeps.
- Final source hash receipt, package/root/license/backup/settings/native integrity
  audit pass. No source changed after the tested candidate was built.

Installed/candidate/ZIP DLL SHA256:
F072913235E6C500B81044218F312D6B3AE926FFE263CD731757C156A969AEBE
ZIP: C:/Users/james/Desktop/Derail Valley Mods/RearrangedS282-B99-1.4.4-Test/
RearrangedS282-B99-1.4.4-Test.zip
ZIP SHA256:49F6FC104F0D0BFF607901A12016859852B74FD2494CC5718E069F02A0CE83A8
34 packaged files/56 source hashes;14 original credit/license/notice files match
the audited1.3.3 package byte-for-byte. Original drivetrain bundle unchanged.
Build receipt and package-audit.json: build/runtime-trial-1.4.4/.
Previous installed1.4.3 preserved at installed-backup-20261018-211147, DLL SHA
5F6D61E2968A07E5CC60D49395DCE2C876D4E662BBFB7F78189F04DC6230331E.
Existing Settings.xml and installed native Assembly-CSharp preserved, as are the
protected1.3.3/1.3.6 baselines. No game launched, saves/other mods edited or upload.

James tests live Unity rendering, cutoff/reverse, pipe socket seating, near/far
appearance, actual oilcup interactions, driving/braking and save/reload. For222,
try2.1/2.4/2.8/3.2/3.6m tread diameters, reapply stopped/regulator closed and record
the first body/frame/equipment overlap. Test stock/new/articulated transitions and
second-car isolation. RUNTIME_TRIAL_TESTING.txt has the ordered current checklist.

## Historical 1.4.3 restoration trial installed, 18 October 2026

CURRENT: James requested restoration of added-axle exploded-model/material treatment
and the original 4-0-4 Drivers not found. Both are implemented, packaged and installed
as 1.4.3. All original enum IDs 0-33 are now selectable. This does NOT certify game
acceptance or restore the two other disclosed parity differences (custom stock
appearance and the old derail-threshold transpiler). No new locomotive designs.

Native-first evidence: installed B99 ExplosionModelHandler source and the actual
resources.assets LocoS282A serialized configuration were inspected, plus Car Changer's
existing explosion integrations. B99 replaces only an exploded boiler-tubes
placeholder; the S282 body/running gear uses materialSwaps. There is no replacement
stock drivetrain/body prefab to recreate. Do NOT revive Spawner.ExplodeDriveAxles or
the obsolete FixExplodedModel/index1/static guard. Native source and targeted binary
asset probe/results: build/b99-inspection/ExplosionModelHandler.cs,
inspect_explosion.py, native-s282-explosion.txt. The initial broad typetree attempt
could not deserialize stripped custom MonoBehaviour fields; the final probe checks
the exact MonoScript class and parses the 316-byte handler payload completely.

New B99ExplosionMaterials is one plain per-car registration owner, no MonoBehaviour
or Harmony hooks. It finds the existing body's material-swap entry by renderer
ancestry, copies the per-car array and appends one initialized native material-swap
entry. Existing entries and their lazy caches are untouched. It seeds inactive
original body gear and registers generated MeshRenderers (including inactive LODs)
after successful arrangement commits, excluding owned Delta/effects presentation.
New articulated parts are discovered after their lazy creation; repeated conversion
does not duplicate entries. Native HandleExplosionModelChange and repair restore
exact material references; no shared Material properties or prefabs are mutated.
Seeding inactive original drivers matters if first exploded as a 4-0-4: native lazy
discovery otherwise misses all original drivers for later powered arrangements.
The native geometry/constraints remain in place during explosions. No new wreck
mesh authoring, separate physics/animation controller or exploded-body clones.
Manual switching now requires a repaired locomotive, matching the pre-existing
automatic-load condition. Already-exploded saved choices remain pending until
native repair and other safe-switch conditions permit conversion.

4-0-4: catalogue count34, stable ID33/name retained, leading/trailing geometry reused,
ShowNoDrivers now called by the active geometry branch with B98 sim writes excluded.
Four carrying axles, no powered wheels. B99PhysicsState supports the explicit zero
case without weakening the positive-driver friction formula's guards. It preserves
slide friction and rolling/mass allocation, disables only the native DrivingForce
component, clears its stale force and zeroes native powered-axle inputs. The optional
engine-braking reader is detached while unpowered to avoid native force/0; its exact
Port reference and original DrivingForce.enabled are restored on powered selection,
stock, rollback and pooling. Native boiler/auxiliary/brake simulation is retained;
no cylinder dimension/cutoff change or custom force solver. Shared type data unchanged.
Existing native PoweredWheelsManager empty-array iteration/save/repair was inspected.
Settings adds spawn404=false; manual selections/save metadata accept33; random
defaults remain the31 conventional choices. Stage2RuntimeChecks now runs34 entries,
captures native traction enable/engine-braking reader state, checks the zero-driver
state and material registration. It does not explode locos or prove game visuals.

Validation, all passing:
- B99 native-reference Release build: 0 warnings/errors.
- Validate.RestoredFeatures.ps1:257 assertions,24 transitions,11 explosion/repair
  cycles. Executes actual B99PhysicsState, B99ExplosionMaterials, ShowNoDrivers and
  installed native ExplosionModelHandler/DrivingForce algorithms plus wheelslip
  properties/reset, with visibility changes only. Covers extreme torque/no traction,
  zero-driver engine-braking safety, exact stock references/coefficients/restoration,
  second-car isolation, hidden drivers, inactive LODs, late rear parts, repeated repair
  and pre-existing native caches. Managed Unity/entity/dispatch stand-ins, not Unity
  scene/rigidbody integration or rendering. Evidence: evidence/restored-features-tests,
  build/restored-features-validation/validation.json.
- Random/catalogue/save actual-source fixture:1239 checks incl ID33 roundtrip/opt-in.
- Compiled/native integration:42 checks. B99PhysicsState is deliberately changed;
  its old byte-for-byte comparison is replaced by executed transition tests, while
  existing conventional conversion-math/rod methods still match the earlier binary.
- Coupling:560 assertions including four34-layout sweeps; rolling/brake audio:136
  assertions/22 transitions. Those source helpers are unchanged.
No game launched, saves modified, RF work performed, other mod edited or upload made.

Installed/candidate/ZIP DLL SHA256:
5F6D61E2968A07E5CC60D49395DCE2C876D4E662BBFB7F78189F04DC6230331E
ZIP: C:/Users/james/Desktop/Derail Valley Mods/RearrangedS282-B99-1.4.3-Test/
RearrangedS282-B99-1.4.3-Test.zip
ZIP SHA256: A1EADD85C59BBD5902321DE22DA767B054733C7AFF4B8A050C452DCD66E7A6E4
34 packaged files,54 source hashes, original drivetrain bundle and licenses/credits
retained; no settings/log/diagnostic/PDB files. Final build/install receipt and package
audit: build/runtime-trial-1.4.3/. The REAL previous1.4.2 backup is
installed-backup-20261018-203755, SHA C8B7BD5C10C205402188562FEA8D1D4E922F2506D348C3C79485CE528C00D2D4.
The later203826 backup contains intermediate1.4.3 before a UI label correction.
Protected1.3.3 DLL/ZIP and1.3.6 ZIP retain their documented hashes. Assembly-CSharp
SHA remains CF00E7C8CECB1A30BA93AD8DEC926090FFC9CB876365027020F8C3BC64F1D0B4.

User test: restart; 4-0-4 selection/no propulsion/towing/brakes, saved identity,
stock/powered recovery; explode4-0-4 FIRST, repair, select2-12-2, explode/repair,
then lazily create both articulated layouts and repeat. Inspect near/far burned
parts, normal material restoration, active wheel counts and second-car isolation.
RUNTIME_TRIAL_TESTING.txt contains the ordered disposable-sandbox checklist. Game
acceptance is outstanding; neither older structural passes nor managed tests prove it.

## Historical feature-parity clarification before new designs, 18 October 2026

James asked whether EVERY original feature is now ported before starting new
features. Read-only comparison against upstream HEAD README/source and current
active build paths: do not claim literal100percent parity. All33 original powered
arrangements are implemented (31 conventional plus2 original duplexes deliberately
changed to articulated), along with random choices, native per-car persistence,
style/valve-gear settings and the listed B99 fixes. Runtime acceptance of the new
articulated/audio work remains outstanding; implementation is not a game pass.

Explicit remaining legacy differences/gaps:
- ID33 zero-driver4-0-4 novelty remains unavailable.
- Selecting stock2-8-2 restores the native snapshot; upstream's customised stock
  Mikado appearance/settings path is not reinstated for that stock selection.
- Original ChangeDerailThreshold transpiler is not registered in the active B99
  Main branch. Native B99 handling/axle physics is used; calculating/logging
  derailModifier does not mean the legacy arrangement threshold is applied.
- Upstream FixExplodedModel() adds extra axle roots to explosion material swaps,
  but its only call is in the excluded legacy SwitchWheelArrangement branch.
  Custom exploded-model/material presentation parity is not complete/certified.
  Do not confuse this with disabling native damage or the new native rear engine's
  tested damage/save-state handling.

These are disclosed remaining parity items, not an invented prerequisite to new
design work. James has expressed interest in moving on; no new designs specified
or implemented by this audit. No source/build/install changes; installed1.4.2
remains the same. Own new designs/three-cylinder variants are still future work.

## 1.4.2 native brake-audio fix installed, 18 October 2026

James supplied a beta tester's two Player logs, brake video and F4 screenshot from
C:/Users/james/Downloads. They identify Rearranged1.3.6, RF1.13.4, B99build2702,
Utilities1.1.2. Player(1).log is an EXACT1,852,218-byte prefix of Player(2).log;
one cumulative session, not two tests. Larger log SHA256:
88EEFEE4FA132D1BE06CE87C2B5946246595B137C9FE4F4FE95D51364A3EF4BB.
Evidence notes: evidence/thomas-followup-20261018/BRAKE_LOG_FINDINGS.md,
F4_FINDINGS.md, OVERSPEED_LOG_FINDINGS.md; decoded video frames/overview in video/.
Frames establish multiple locos/states, not an independently heard missing hiss:
AAC is present, but this model runtime cannot listen to audio input. Do not claim
audio listening or a live game reproduction. Original downloads remain unchanged.

USER STEERING: James will recommend not using Realistic Firing and requested
continued fixes for unrelated issues. RF-specific compatibility/overspeed work is
set aside. Do not modify or investigate RF further without a renewed request.
The general passive failed-tick diagnostics already prepared remain useful and
are included. No RF code, settings or installation were changed; RF-disabled
testing is a recommendation, not an automatically applied local setting.

CONFIRMED native brake-audio defect: larger log contains3,247 rolling-module
IndexOutOfRangeExceptions. Reporter/native Assembly-CSharp MVID matches exactly;
offset0xc4 accesses lastJointStep[i][j]. Changing physical axle counts left this
cache at the old per-bogie sizes. The original mod's ResetJointSoundValues call
was in the old build-gated branch; the active B99 conversion missed it. Native
S282 audio updates Rolling before Brakes with no per-module catch, so this error
prevents brake-audio updates while physical brakes can still work. No-JointsSpan/
LOD gates explain intermittent indexing. This supersedes the old vague modstack
or geometry-snapshot audio hypothesis. Exceptions lack a car ID, so do not assert
which locomotive threw every instance or claim an audible runtime pass.

Small native fix: new WheelRearranger.Audio.cs refreshes exact-owner initialized
CarRollingAudioModules under car AND detached interior, deduplicated, via existing
ResetJointSoundValues. Calls follow final axle/physics commits in
TrySwitchConventional and stock/rollback/pool restoration. Native Initialize owns
late audio creation. No new audio controller, hook, clip or custom cache solver.
Actual helper plus native reset/update/dispatch fixture passed136 assertions across
22 transitions, reproducing6 original failures and verifying brake-update recovery.
Evidence: Validate.RollingAudio.ps1, build/rolling-audio-validation/validation.json.

F4 screenshot clarification: circled WATER/COAL boxes are boiler/firebox level
bars, whose numeric text is intentionally empty. Tender quantities30.0water and
10.0coal are visibly present on the right. Native prefab does include fill bars;
persistently missing fill remains a valid separate unresolved symptom. RF glass%
is not the same raw native boiler indicator range. No speculative HUD patch.
Check native bars with RF disabled before extending this investigation.

The later0-8-0 infinite-steam run has6,413 RFInjectorRig.StepOutlet nullrefs
escaping Boiler.Tick before engine refresh. Native physics can reuse old positive
torque independently. This is a source-supported HYPOTHESIS for that run, not a
confirmed overspeed cause; no speed/RPM/torque trace or exception car IDs provided.
RPM input may still update while crank phase/chuff/torque outputs freeze. Infinite
steam is explicitly reported for this new run only, not retroactively for400+km/h.
Utilities also has255 missing-field errors; no third-party workaround implemented.

General diagnostic gap corrected: original Update postfix missed failed ticks.
Same target now uses a finalizer which captures normal/fault completion and
returns the EXACT original exception. Schema2 gains simUpdateFailed and bounded
simUpdateFault(type/message),200/512 chars, retaining10Hz and off/no-I/O behaviour.
Diagnostic I/O/logger failures cannot mask the original fault. This changes no
physics/ports and suppresses no native errors. It samples faults, not an exhaustive
exception counter. Actual-source fixture passed81 checks (previously53), including
exception identity, normal/off/throttled paths and I/O+logger failure. Report:
build/power-diagnostics-fault-validation/run-20261018-192654-294/validation.json.
PowerDiagnostics source hashAB8E941F2EEB6E6896BE20E8C0CF30AC90F8C4F31296CCD124008DF169F7222E.

Installed1.4.2DLL SHA256:
C8B7BD5C10C205402188562FEA8D1D4E922F2506D348C3C79485CE528C00D2D4.
Desktop ZIP: C:/Users/james/Desktop/Derail Valley Mods/RearrangedS282-B99-1.4.2-Test/
RearrangedS282-B99-1.4.2-Test.zip.
Final ZIP SHA256:71D6AEF23D54CF12316F044AB6AF3EF6F2158D2CB4994AE39DAD795D9FE93A37.
Receipt: build/runtime-trial-1.4.2/build-receipt.json, runtimeValidated=false.
Previous1.4.1 installation is in installed-backup-20261018-193023 in that folder.
Latest193225 backup is the intermediate1.4.2 before the native-only doc update.
Build.RuntimeTrial default1.4.2. Native reference build0warnings/errors;48 compiled/
native integration checks pass against final DLL (build/runtime-trial-1.4.2/
integration.json), including native reset calls, call order and finalizer hook.
Prior native physics/rod methods unchanged by IL comparison. Existing chain/hose,
rods, Delta, both articulated arrangements and save/random features retained.

Audit passed: installed/staged/archive DLL+bundle match, all53 C# source hashes
match receipt,34 files under one UMM root, all27 licence/credit/notice/exporter
files identical to seed. No captured user data/logs/media/settings/game DLLs.
Compiler CodeView debug paths remain embedded (generic B:/GitHub repo and vendor
C:/sources/cecil); no PDB is shipped. Do not claim literally zero absolute paths.
Final doc-only repack verified all4 installed/staged/archive manuals match and
recommend RF-disabled native testing. No further production changes after tests.
No game launch, save changes, external message, upload or Git commit. Runtime
brake listening, lack of new rolling errors and native HUD fill still need tests.

## 1.4.1 coupling/audio trial installed, 18 October 2026

James relayed3 issues from the large-stack/overspeed reporter: sometimes missing
F4 coal/water quantities, sometimes silent independent/train brake release, and
front chain/hose pushed into the frame on converted S282s. Version and whether
this starts on spawn or after switching are explicitly UNKNOWN. No new reporter
log/clip/reproduction. Do not treat James's old local logs as reporter evidence.
Detailed native traces, hypotheses, validation and next observations:
evidence/beta-reporter-three-issues-20261018/FINDINGS.md.

CONFIRMED published1.3.3/current code defect: car snapshot excludes DV's detached
interior. Native VisualCouplerInit.chain/hoses mount roots live there. Each
no-leading MovePilot(-.47m) affects them, but prepared/stock geometry restore
resets only the physical pilot/anchor. currentWA is reset to stock, so the old
positive offset undo is skipped. Seven choices accumulate3.29m rearward drift.
GeometryState now additionally captures/restores ONLY native front chain/hose
mount localPositions. Keep native MovePilot, IK, coupling state, rotation,
activity and parenting unchanged. Structural JSON captures mount positions too.
Native chain visibility re-enable can reset its cached mount; check walk-away/
return manually before extending into any new coupling/visibility workaround.

Separate1.4.0 defect corrected: ArticulatedEffects discovery now scans both car
and detached interior roots, deduplicates controllers and requires exact ready
car/SimulationFlow ownership. This fixes missed already-initialized articulated
rear audio readers. It is not a diagnosis of the standard brake-audio report.
Native brake sounds read actual exhaust/charge flow beneath separate interior,
not wheel RPM/chuff ports. F4 uses separate interior native Indicators; no
confirmed Rearranged cause for either intermittent report. No speculative fixes.

Validation: actual mount helper + native movement model reproduces old drift,
passes544 checks including four33-layout sweeps/stock/pool-style restoration and
unchanged IK/parent/activity/rotation; actual audio discovery/ownership methods
pass9014 assertions/303 scenarios with component-tree stand-ins. Native-reference
build0warnings/0errors;40 compiled/native integration checks pass. These are
OFFLINE checks, not a reporter reproduction or actual game/audio/IK pass.
Reports: build/front-coupling-validation/validation.json;
build/articulated-effects-validation/validation.json;
build/runtime-trial-1.4.1/integration.json. Prior engine/geometry/rod fixtures
remain applicable to unchanged source; no repeat gameplay validation is claimed.

Installed1.4.1DLL SHA256:
40A0FA7BD3684DD9F14BB1CC6011FA0C39783ED5C7CBFC6B9CBA51B945AD1483.
ZIP: C:/Users/james/Desktop/Derail Valley Mods/RearrangedS282-B99-1.4.1-Test/
RearrangedS282-B99-1.4.1-Test.zip.
ZIP SHA256:F824E515DC4BDA7F335D1E84671E3AC56986D629D6AF6CF36C39E807F908FF39.
Receipt: build/runtime-trial-1.4.1/build-receipt.json; runtimeValidated=false.
Previous1.4.0 installation backup: build/runtime-trial-1.4.1/
installed-backup-20261018-092932 (DLL793DEF2A...730D0E).
All31 conventional + both articulated choices retained; runtime instructions/
test-status docs updated. Restart and retest coupling swaps/stock/walk-away and
both articulated engine sounds. Other mods, saves and public ZIPs untouched.
No game launch, Git commit, upload or external message. Build.RuntimeTrial default
is now1.4.1. Earlier entries below describe historical state and archives.

## 1.4.0 combined articulated trial installed, 18 October 2026

James requested BOTH original two-driver-group arrangements together for his
in-game test, overriding the earlier one-at-a-time prototype plan. Implemented
31=2-4-4-2 Articulated and 32=4-4-4-4 Articulated alongside conventional IDs0-30.
No rigid intermediate, new own designs or zero-driver novelty in this trial.
New designs follow after James tests these two. The research/estimates below are
historical and must not be mistaken for outstanding implementation work.

Native-first decision: inspected/reused L74ArticulationPolish.cs from the tested
Heavy Mallet. Native DV bogies retain all four PoweredWheel components as direct
bogie children. Their presentation anchors follow the engine frames. A native
PositionConstraint holds the rear pin; a native AimConstraint follows Axle_F.
No new Rigidbody, custom curve solver, ECS traction/slip group or compound receiver.
Both groups use accepted native shared traction/RPM/slip. Split only the existing
continuous rendered subframes at the measured pin; preserve UVs/material groups.
Whole front gear, cylinders, suspension, brake fittings, oil lines, sparks and
named cylinder walk/item shells move together. Rear frame stays boiler-fixed.

ArticulatedSimulation prepares a per-car native graph before SimController builds
its one flow/save owner. The unchanged main engine plus gated native rear engine
feed typed native torque/intake/exhaust sums; both steam demands reach the boiler.
Dormant rear adds no steam/torque/wear and retains damage/save state; native repair
is observed while dormant. Old saves receive an empty rear state, never a false
native cylCracked key (presence itself means cracked). S282 body/type guard leaves
L74 and other CCL vehicle graphs alone. Rear capacity is a conservative
source-informed native equivalent, not an exact B98 or historical compound claim.

ArticulatedEffects owns rear native chuff/cock readers and authored particle
copies, uses actual rear ports, and restores/deinitializes ownership on switching,
controller unload, pool and deletion. Both water drips and cylinder-crack outlets
follow their engine; the rear crack outlet uses its actual rear crack-flow port.
Shared chimney pulses keep native main pressure response with rear-pressure gate.
No separate chuff plugin changes/install: MultiCylinderChuffFix remains its prior
installed version. Existing six native oilcup circuits/providers stay functional;
rear shares native lubrication, with no separate rear refill circuits added.

Saved arrangement/native radio selection supports both; optional random toggles
for them default off. Diagnostics schema2 adds engines[] with per-engine crank,
chuff, torque and steam flow while preserving old top-level main metrics. Optional
structural test covers all33; James does not need to run it to try both manually.
Prior conventional physics and LateUpdate rod methods remain byte-equivalent IL.

Build/package/install: Build.RuntimeTrial.ps1 -Version 1.4.0 -CandidateDirectory
build/articulated-candidate -Install. Final DLL1.4.0.0 SHA256:
793DEF2A35DF90F344AA7649DA00253F27CFB71D088324267239642B6E730D0E.
Installed into B:/SteamLibrary/steamapps/common/Derail Valley/Mods/RearrangedS282.
Desktop ZIP: C:/Users/james/Desktop/Derail Valley Mods/
RearrangedS282-B99-1.4.0-Test/RearrangedS282-B99-1.4.0-Test.zip.
ZIP SHA256: 192BBBE213B6F63F801EBE7A91340C87B87C11CF270EB93CCFFB2412D7EFDBEE.
34 files under one RearrangedS282 root, including original assets/s282_drivetrain
(SHA256 44A1B9EAE0DDA58BE97B9AEDF3877522FBFFAEC060FE02A8385158E095BDB22D).
Original MIT, credits, notices, 11 licences and 13 exporter tools byte-match the
audited1.3.3 seed. No settings/logs/OBJ/game DLLs/private paths in the ZIP.
Receipt: build/articulated-candidate/build-receipt.json; runtimeValidated=false.
Original1.3.6 installation backup: installed-backup-20261018-021652 in that folder
(DLL89F99281247B67B1EB806465AE6C681F73620BE9CF93ED237CBA0D581FCDC538).
Final rebuild backup021945 contains the intermediate1.4.0 trial, not1.3.6.
Protected1.3.3 DLL/ZIP and1.3.6 Beta2 ZIP hashes rechecked unchanged.

Validation: native-reference build0warnings/0errors;40 actual native engine
constructor/Tick/port/save checks;31 actual-source graph/lifecycle adapter checks;
3,947 actual-clipper assertions/205 partitions with Unity mesh/vector stand-ins;
53 diagnostic checks;1,233 catalogue/random/save policy checks;40 compiled/native
metadata integration checks;6,720 simulated rod poses plus compiled IL inspection.
The final integration and rod reports match the installed final DLL hash.
Evidence: build/articulated-simulation-validation/native-run-20261018-021247-745/;
build/articulated-geometry-validation/validation.json; build/articulated-validation/;
build/random-lifecycle-validation/validation.json. Independent native init/save
and final package audits found no material blocker. This is OFFLINE validation,
not actual Harmony dispatch, native constraints evaluation or a game driving pass.

Runtime acceptance belongs to James's combined test: curves/reverse/clearance,
gear/outlet alignment, sound balance, stock switching, save/reload, pooling/deletion,
oilcup interaction and second-car isolation. Fused native boiler/body collision
hull remains fixed; only named cylinder shells move. Frame split is uncapped.
Realistic Firing overspeed remains unresolved; no claim of an overspeed fix here.
RUNTIME_TRIAL_TESTING.txt and ARTICULATED_TEST_STATUS.txt ship in the mod folder/ZIP.
No game launch, save changes, commits, uploads or modifications to other mods.

## Revised requirement: every two-driver-group layout articulated, 18 October 2026

James explicitly changed the plan: he does not want the original duplexes restored
as rigid engines. ALL arrangements with two driver groups must be articulated,
including 2-4-4-2 and 4-4-4-4, from the first prototype. Use the tested L74 method.
Historical descriptions of the original rigid implementation below are evidence
about the asset donor only; they are not the target or an intermediate milestone.
Also add our own multicylinder variants in rigid and articulated forms. Rigid
variants can have one coupled driver group with three/four cylinders; articulated
variants have two engine groups. State cylinders per engine and per locomotive.

Updated evidence/full-arrangements-research-20261018/FEASIBILITY_AND_PLAN.txt in
place. First milestone is articulated2-4-4-2, provisionally3-5developmentdays.
Both original two-group layouts articulated/fullcataloguebeta approximately
8-15days total plus gameplay iterations. The old1-3/5-10day rigid-first estimates
are superseded. A new2-6-6-2 family follows on the shared articulated foundation.
Ownsix-cylinderarticulatedoption may use2nativeN=3engines (3+3) after native3cy
mesh/phase/audio is proven; no generic one-engineN=6 assumption. Actual variant
selection/details remain planning, not implemented work.

Concrete extra work: whole engine assembly follows measured hinge/aim, including
cylinders/gear/rods/visual anchors/oil/drain outlets and moving collision; joints
for pipes crossing frames. Preserve native bogie contact-axle ownership/discovery.
CurrentGeometryState does not snapshotparent/siblingownership, so articulated
switch/rollback/stock/pool restoration must add explicit hierarchy and owned
constraint cleanup. Curve/reversecurve/points/clearance checks are acceptance
criteria for every two-group layout. Existingacceptednative traction/slip method
remains the base; no new independentgroup/ECS orcompoundreceiver prerequisite.

Documentation update only. No implementation, build, install, game launch or
changes to L74/Derailroader/chuff source or installed1.3.6 in this turn.

## Full catalogue and multicylinder research, 18 October 2026

James requested research into all original arrangements including duplexes,
Mallet/articulated additions, new multicylinder/chest meshes and integration with
his chuff fix. Research report:
evidence/full-arrangements-research-20261018/FEASIBILITY_AND_PLAN.txt.
No new runtime implementation, build, installation or game launch in this pass.

IMPORTANT CORRECTED DECISION: James explicitly confirmed that he tested the Heavy
Mallet in game and its slip, adhesion, power and articulation worked very well.
Use that accepted implementation as the native reference for the base port.
Codex initially elevated independent driver-group slip into a prerequisite James
had not set. This assumption was withdrawn. Although native source shows one
traction/slip group, that is not evidence the tested behaviour is unacceptable.
Do not repeat this tangent by requiring custom ECS physics or a true compound
receiver before restoring the arrangements. Further detail is optional and needs
a demonstrated problem or explicit requirement. Reuse L74's native graph,
typed torque/intake/exhaust sums, existing adhesion and constraint articulation.

Confirmed catalogue: IDs 0-30 conventional, 31=2-4-4-2 rigid duplex,
32=4-4-4-4 rigid duplex, 33=4-0-4 zero-driver novelty; all 34 were selectable.
2-6-6-2/2-6-8-0/4-6-6-4 were commented ideas, not finished original Mallets.
Current zero-powered path rejects an empty set; restore novelty deliberately.
Original compiled drivetrain bundle, CAD/Blender/prefabs/animations are retained.
Its editable Unity mesh/GUID/material dependencies are incomplete: test existing
bundle first; existing runtime OBJ importer may suffice for new static chests.

Native B99 explicitly supports three cylinders with 120-degree lead/six events,
making a distinct three-cylinder 4-12-2 a useful first new profile. Constructor
caches arrays, area, volume, phase and chuff count: do not just mutate public
numCylinders/bore/stroke after construction. Safe per-car native definition and
activation/switching is the first technical spike, preserving stock snapshots.

Chuff source/candidate0.2.0 found at C:/Users/james/Desktop/Derail Valley Mods/
Leader/plugin/MultiCylinderChuffFix. Installed remains0.1.0. Candidate matches
exact native event-port ownership, uses matched native Hz/count and adapts running
loops;106fixture cases/1560loop checks recorded, actual Unity audio unverified.
It does not create/register readers, phase maps, particles or drain outlets.
Our future integration must own/deinit secondary readers once per audio instance
and use actual per-engine events, not summed integer CHUFF_EVENT counters.
Do not install/change the separate chuff project during source research.

Suggested delivery: original2-4-4-2 prototype1-3development days; bothduplexes/full
originalcatalogue beta5-10days total plus gameplay iterations. Actual3cyprototype
another2-4days plus art/polish; firstL74-style articulated familyanother3-7days
after commonfoundation plus curve/clearance tests. These are provisional effort
estimates, not elapsed guarantees. True compound receiver, independent slip
groups, custom4/6cyphasing or triplex are optional separate scope. RFoverspeed
remainsunresolved; RF first-engine-only superheat attachment is a multi-engine
compatibility check, not a diagnosed cause. Keep baseline/native and RF-on tests.

Installed1.3.6DLL andBeta2ZIPunchanged. No saves, L74, Derailroader, chuff code,
Git commits, external messages or uploads changed/sent by this research.
The Desktop ZIP was independently renamed to Rearranged S282 B99 Fork 1.3.6b.zip
in its existing Beta2 folder; hash still matches the canonical Beta2 ZIP above.
Research did not perform the rename. Use the actual filename for future links.

## 1.3.6 Beta 2 prepared for James's upload review, 18 October 2026

James requested an explanation of the tests and confidence plus a UMM/Nexus-ready
ZIP to consider uploading. Prepared locally; nothing uploaded or published.
Package.RuntimeBeta.ps1 repackages the existing hash-verified 1.3.6 trial ZIP,
without rebuilding or changing the installation. New player README, changelog,
test-status document, updated optional-mod credits and companion Nexus draft are
under release-copy/1.3.6-Beta2. The old 1.3.3 release copy and seed are unchanged.
UMM numeric version remains 1.3.6; Beta 2 is the release label. DisplayName now
says Conventional Beta, with original author and JimTaplin credit retained.

Canonical ZIP: release/1.3.6-Beta2/RearrangedS282-B99-1.3.6-Beta2.zip.
Desktop copy: C:/Users/james/Desktop/Derail Valley Mods/
RearrangedS282-B99-1.3.6-Beta2/RearrangedS282-B99-1.3.6-Beta2.zip.
ZIP SHA256: 0B2E1B36BA525D0BA38A7140E03F37893D163A972261CF7A4AD1656197847440.
DLL remains 89F99281247B67B1EB806465AE6C681F73620BE9CF93ED237CBA0D581FCDC538,
matching installed 1.3.6. Archive is 1,638,554 bytes with 34 files under the
single RearrangedS282 root. Original MIT, 13 exporter dependencies and 11 licence
files byte-match the audited 1.3.3 seed. No game assets, logs, settings, personal
paths, diagnostic output or Nexus draft inside the ZIP. Independent final audit
also passed. CHECKSUMS.txt, release-manifest.json, TEST_STATUS.txt and the Nexus
upload draft accompany the Desktop ZIP; upload only the ZIP as the mod file.
Explorer was launched with the ZIP selected; the Nexus text tab was queued.

This is packaging-ready for a clearly labelled testing beta. No new 1.3.6 game
validation is claimed. Explained 6,720 rod poses as 56 scenarios x120 frames,
actual source with Unity stand-ins plus installed IL inspection, rather than
6,720 in-game tests. Good technical confidence in correcting the phase lag, but
no defensible probability or visual confirmation of the original symptom yet.
Delta's 129 checks also use stand-ins; actual hooks/render/save/delete await game.
The earlier in-game structural pass belongs only to 1.3.3. Realistic Firing
overspeed remains unresolved, with diagnostics included. No steam-power fix,
speed cap, duplex support, new build, install or Git operation was performed.

## Installed 1.3.6 rod fix verified, 18 October 2026

James asked whether the first beta report (2-12-0 rods separating from the last
two axles at high speed) is addressed in this version. Inspected the actual
installed 1.3.6 DLL, SHA256
89F99281247B67B1EB806465AE6C681F73620BE9CF93ED237CBA0D581FCDC538.
The helper has LateUpdate, no Update, and copies localRotation as a quaternion.
Its compiled method matches the original 1.3.4 rod candidate. SetupDrivers binds
all four added wheels: fifth/sixth left follow fourth left, right follow right.
Independent source review found no competing added-wheel pose writer.

Validate.RodTiming.ps1 now accepts an assembly and report path. Against the
installed DLL it passed 6,720 source/stand-in frame-sequence checks at 20, 60,
100, 120, 200, 376 and 462 km/h, 20/30/60/144 fps, both directions. The old
Update helper lagged in all 6,720 moving poses; LateUpdate matched all poses.
Report: evidence/beta1-high-speed-rod-report/installed-1.3.6-validation.json.
These checks establish inclusion and frame-sequence logic; extreme speeds only
exercise synchronization mathematics. They do not prove actual Unity scheduling
or rendered rod attachment. No new 1.3.6 in-game visual retest is available;
latest inspected log loaded 1.3.5 and structural report predates this version.
James still needs to inspect both sides of a fast 2-12-0, ideally also 4-12-2,
under steady running and wheelslip. No DLL, package or installed file changed
during this verification, and no reinstall is needed.

## Delta compatibility1.3.6 installed for testing,18 October2026

James authorized fixing Delta trailing truck compatibility and reinstalling.
Implemented inside existing RearrangedDLL; optionalreflectionintegration with
S282ModelTweaks1.1.0/DVCarChanger1.5.4, no newharddependency, no thirdpartymod
DLL/bundle edits or shipped meshes. MainexplicitregistersDelta bridge. Package
LoadAfter includesGauge,DVCarChanger,S282ModelTweaks toensuretypesloadedfirst.

The bridge intercepts exactDeltaTrailingTruckAxles only. NativeCarChanger
baseapply/materials/Config/save/repaint/remove lifecycle retained. SMT's own
whole-bogie swap/reset bypassed, preventing6->5poweredregistration and5->6state
arraycopy failure. NativebodyHideTransforms suppressed onlyforDelta_body within
scopedbaseApplyprefix/finalizer, so hiddenpart restoration nevercontaminatesstock.
NewDeltaTrailingTruckVisuals render-onlycopies existingloadedDelta wheel/frame
ontoONEactiveunpoweredrearaxle; wheelinheritsnativeaxlespin,framefollowsAxle_R.
Sourcewheel.459m matchesnativeWRCradius byYZscaling, gaugeXpreserved. No axle,
poweredstate, physicsports/radius/RPM edits.0or2trailingaxles suppressDelta
presentation butretainCarChangerselection. Switchingbackresumesgraphics.
Clear/Suspendrestoreoriginalrendererenabledand4nativeLMSbodyactiveflags;
inactivecopiesdetachedbeforedelayedDestroy. Allownedbody/graphicsmarkedand
excludedfromGeometryState snapshots. RearrangerSuspendbeforeconversion and
Refreshfinally(success/rollback); poolclearsgraphicsbeforenativestockrestore.
ClearownerusesReferenceEquals sodeadoldowner cannotclearanewbinding. Cleanup
contained/idempotent duringearlydelete,remove,repaint,OnDestroy;explosionhide/
unexploderefresh included. NativeaudioearlyreturnfollowedbyoldDeltaexception
explainsreportedquietghost, thoughreporterstacktracewasnotavailable.

Files: DeltaTrailingTruckCompatibility.cs,DeltaTrailingTruckVisuals.cs;
Main.cs/WheelRearranger.Conventional.cs integration;Build.RuntimeTrial.ps1
versiondefault1.3.6+versionedoutputfolders/optionalLoadAfter;updatedtestchecklist.
Buildoutput:build/delta-compatibility-candidate/bin. DoNOToverwritebuild/stage3.

Installed89F99281247B67B1EB806465AE6C681F73620BE9CF93ED237CBA0D581FCDC538
(version1.3.6). Backup1.3.5previousfiles:
build/delta-compatibility-candidate/installed-backup-20261018-011009.
Desktopzip:C:/Users/james/Desktop/Derail Valley Mods/RearrangedS282-B99-1.3.6-Test/
RearrangedS282-B99-1.3.6-Test.zip. InstalledDLL/packagehashverified.1.3.3published
baseline and1.3.5ZIPunchanged. No game launch/commits/publishing occurred.

Validation:native-referencebuild0warnings/errors;129actual-sourceDelta checks
withUnity/Harmonystand-ins;42diagnostic/1157savedrandompolicychecksremainPASS.
CompiledIL: B99PhysicsState,ConventionalPhysics,PowerDiagnostics,andarrangement
save-policybodiesunchanged;nooptionalassemblyreference/noDeltaPortwrites.
SeeValidate.DeltaCompatibility.ps1 andbuild/delta-compatibility-validation.
ActualHarmonyoutsidegameattemptfailedunderNET8host duringHarmonyinit
(MonoModReflectionHelper.MethodAccessException);noadapterpatchranthere. This
isnotagamevalidationpass. RealUnityrender/Harmony/save/repaint/delete/pool
runtimevalidationstillJames'stask. No furthertestingexpansionnecessarybeforehisrun.

Next:restartgame,fresh/disposablesandboxS282;4-12-2->Deltaapply/remove twice,
repeat2-12-2;reverseorderDeltafirstthenarrangement;save/reload&repaint;delete
Deltaengineandchecknohitbox/soundghost;secondcarisolation;stockrestore/oilcups.
UseRUNTIME_TRIAL_TESTING.txt packagedandinstalled,updatedREADME/checklisttoo.
Oldalreadybrokenin-memoryenginesarenotrepairtargets;restart/freshspawn.
RFoverspeedinvestigationremainsseparate,powerdiagnosticsstillavailable.

## New reporter evidence and Delta truck incompatibility, 18 October 2026

James reports original author is happy for port work to continue; no contact was
sent by Codex. Reporter mod list: Rearranged1.3.3, RF1.12.0, DVCarChanger1.5.4,
S282ModelTweaks1.1.0, CruzLocoTweaks1.2.2, Utilities1.1.2, others. Do not treat
that as the local installed configuration. Reporter says RF cylindersOFF stops
overspeed. ExactRF1.12 released source verified against1.11.2/1.13.2: cylinder
model/patch/adapter/testfiles identical. See new pinned source audit note in
evidence/beta1-overspeed-report/REALISTIC-FIRING-1.12-SOURCE-AUDIT.md.
Do not claim latestRF fixesoverspeed; actualRPM/sliptrace stillneeded.

James explicitly clarified Delta truck issue is SEPARATE fromoverspeed. Repro:
4-12-2or2-12-2 ->applyDelta ->geometrymessedup, cannotremoveDelta ->deletecar
leavesdrivablecollidablelocowithoutsound. In inspectedpackagesDeltaTrailingTruckAxles
is supplied by S282ModelTweaks1.1.0 (Nitro) throughDVCarChanger, notCruzer1.2.2.
Inspection-onlycopies underbuild/cruz-tweaks-inspection; no game/source/install
changes. Bogie changer uses stocksix-axle index5 as trailing and replaces entire
rear bogie. On12coupled replacement has2powered vs our3rear: currentpoweredmanager
becomes5; removalrestores6physicalpowered butcopiescurrentstatearraylength5 into
all6 ->predictedIndexOutOfRange on6thassignment. Removal is also hooked to native
car-destructionevent, so same failure caninterruptdeletion. Reporter log needed
toconfirm exactexception andwhy audio stops. Do not claim runtime reproduction
or that installed1.3.5 fixes this newlyidentifiedcollision. KeepDelta offconverted
12coupledenginespendingcompatibilitywork. Existing1.3.5 installhash unchanged.
User has NOT requested a Deltafix yet; investigation remainedreadonly.

## 1.3.5 runtime trial installed, 17 October 2026

James requested diagnostic capture plus restored random conventional arrangements,
then explicitly selected manual arrangement persistence across save/reload too.
Implemented and installed1.3.5 (no game launch). No duplexes or power fix/cap.
Native B99PhysicsState/ConventionalPhysics method bodies unchanged vs tested1.3.3.
Includes earlier1.3.4 LateUpdate rod candidate; runtime visual confirmation pending.

Installed DLL SHA25623B058D6A9653D02E0C5593A9CA6F1A2CFD2C6903A0E657EECE5B39BE7B35AB1.
Backup previous installed files: build/diagnostics-random-candidate/
installed-backup-20261017-211015. Settings/assets/old diagnostics retained.
Published1.3.3 ZIP and build/stage3 baseline hashes remain unchanged.
Desktop ZIP: C:/Users/james/Desktop/Derail Valley Mods/RearrangedS282-B99-1.3.5-Test/
RearrangedS282-B99-1.3.5-Test.zip. Build.RuntimeTrial.ps1 builds/packages from
hash-locked audited1.3.3 seed, optional-Install; no publishing occurred.
Receipt: build/diagnostics-random-candidate/build-receipt.json.

PowerDiagnostics.cs explicitly registered SimController.Update postfix,10Hz per
activeS282 including native stock ones. Captures road-derived/wheel-port/crankRPM,
slip+slide, targetRPM (indicative timing), torque, force, controls/cutoff, chest/
admission pressure, steam mass/flow, active mod versions, frame/time. JSONL in
Mods/RearrangedS282/diagnostics. Start/stop buttons in UMM; one-second flush,
normal quit stop;30min/100krows/~64MiB limits. Structural tests mutually exclude.
No RF dependency; James will install latest RF himself. Normal0.2s RPM smoothing
and ECS sampling phase can cause transient mismatches. Do not infer opticalRPM.

Settings now load/save and have31 conventional random toggles with corrected
10-coupled IDs. Random opt-in defaultsOFF; onlynewspawnsrollonce, emptypoolstock.
Oldsavecarswithoutpayloadstock; savedmanual/randomchoicespreservedper-car.
ConventionalArrangementState stores namespaced versioned native simCarState key.
ConventionalSpawn hooks TrainCar Start/newlogic/pool and native saves/restore;
manual radio success cancels pendingautomaticchoice. Pendingrestorewaits until
stationary/regulatorclosed/settled; movingload temporarilystockuntilsafe. Unknown
future/corrupt payload retained until explicitmanualchoice. Structuraltest
transientconversions don't update durable selection. Poolprefix restores stock;
rare restorationfailure uses verifiednativeunpooled destruction path after
caller already began deletion. Hotunloadrefused; restartrequired.

Validation: native-reference build0warnings/errors;42 actual-source diagnostics
checks,1157 actual-source settings/identity/save-policy checks,2160 rod poses.
Compiled newDLL LateUpdate and explicitpatchregistrationchecked; noPatchAll.
Tests are managed stand-ins/static/native-API inspection, not game execution.
Runtime save/reload, deferredrestores, pooledreuse, UI, quitflush, RFdriving and
rod appearance still require James's tests. RUNTIME_TRIAL_TESTING.txt is packaged
and installed, also copied as updated readme/manualchecklist. No source commits.

Next: user Ctrl+F10 -> RearrangedS282 -> START POWER DIAGNOSTICS; close manager and
drive, STOP/SAVE afterwards. Separate stock/4-12-2 RFcylindersOFF/ON captures.
Forrandom: expand arrangement/random settings, enable spawn toggle and choosepool.
Forpersistence: disposable save, two distinctmanual/randomarrangements, save/reload.
Then stopcapture, two stockstationaryS282 -> structuraltest; nativeoilcups check.
Inspect resulting JSONL and Player.log, confirm reporter versions/options before
assigningoverspeedcause. Multiplayer sync remains outofscope/unvalidated.

## Realistic Firing read-only cylinder audit, 17 October 2026

See evidence/beta1-overspeed-report/REALISTIC-FIRING-CODE-AUDIT.md.
Found local Downloads ZIP1.11.2; decompiled inspection copies under build/.
Cylinder patch reads native crankRpm directly: no double radius conversion.
It replaces stock admission limit, retaining native capped steam-chest refill.
Offline actual-Core DLL probe plus relevant native chest/chuff logic (300 sampled
cases) gives no sustained positive torque at376/462 under tested assumptions.
Not a full-game reproduction or proof reporter uses this version/options.
Native slip interpolation toward600RPM can under-report crankRPM above that
road RPM; actual slip/RPM telemetry needed. F6 RF Engine page has mean pressure
and steam used, NOT RPM. No mod code/settings/install changed. Installed1.3.3
hash unchanged. Do not blame RF or implement speed cap on this evidence.

## Overspeed report: read-only power audit, 17 October 2026

Supplied66.7s MP4 inspected: evidence/beta1-overspeed-report/video/OBSERVATIONS.md.
Visible remote panel confirms274-282km/h, regulator14%, cutoff71->48->33%,
boiler14.0bar/chest10.9bar in sampled interval. Empty tender/firing stopped;
residual fire remains. No proof of unlimited resources or uphill propulsion.
ENGINE tab/RPM/torque not displayed;376/462/crash not verified from frames.
Onscreen lowRAM warning also appears; origin/crash relationship unknown.

See evidence/beta1-overspeed-report/FINDINGS.md and static-power-audit.json.
Conventional beta retains native steam torque; radius/RPM conversions agree.
No sustained stock-model steam torque at reported376/462 km/h if crank RPM
tracks road speed and stock flow/model remain active. This is an analytic
conditional bound, not an in-game reproduction or terminal-speed prediction.
Installed Utilities1.1.2 no-derail only raises derail stress threshold.
Important: Realistic Firing current1.5.0 includes a replacement cylinder model
on every steam loco, beyond automatic firing. Reporter version/toggle unknown;
do not assume native steam equations apply or blame that mod without A/B data.
Next compare stock/4-12-2 with realistic cylinders off/on; inspect actual RPM,
torque, flow, cutoff, slip and grade. No power fix/cap/build/install performed.
Existing rod timing candidate and tested beta remain unchanged.

## First public beta report: high-speed added-wheel phase lag; 1.3.4 candidate

Reporter: at least 2-12-0 coupling rods detach visually from the last two axles at
high speed. James confirmed no numerical speed or slip-condition details were
supplied. Do not invent a reproduction speed or claim this report reproduced in game.
Report/baseline helper saved at evidence/beta1-high-speed-rod-report/.

Source/native investigation confirms a frame-order defect: added fifth/sixth wheels
use PoweredWheelRotater.Update to copy fourth-wheel Euler angles; normal native
Animator evaluation happens later. Native rods/original wheels advance while added
wheels retain the pre-animation pose. This predicts a speed/frame-rate-dependent
one-frame phase error, matching the symptom. It also applies to ten-coupled layouts.
Native source PoweredWheelRotationViaAnimation updates Animator speed in Update;
Unity execution-order documentation places animation evaluation before LateUpdate.
No custom traction or new animation architecture is needed.

Small candidate change: PoweredWheelRotater.Update -> LateUpdate; copy localRotation
quaternion directly; use Unity != null to handle destroyed source objects correctly.
Compiled method comparison with published 1.3.3 finds ONLY removal of the old Update
callback and addition of LateUpdate changed. Physics, geometry and oilcup method
bodies are unchanged. Clean build, version 1.3.4.0 via command-line override:
dotnet build RearrangedS282/RearrangedS282.csproj -c Release -p:Stage=3 -p:Version=1.3.4
-o build/rod-sync-candidate/bin.

Validate.RodTiming.ps1 compiles actual before/after helper code with minimal Unity
stand-ins, simulates Update -> Animator pose -> LateUpdate, and inspects candidate
IL. 2160 poses pass across forward/reverse, 20/60/100 km/h-equivalent angular rates
and 30/60/144 FPS. Old helper lags on every moving sample; new helper equals current
source quaternion. This validates scheduling logic, NOT native runtime appearance.
No high-speed Unity/game test has been performed; cause remains a strongly supported
explanation of this particular reporter's symptom until an in-game retest.

Candidate test ZIP and instructions:
C:/Users/james/Desktop/Derail Valley Mods/RearrangedS282-B99-1.3.4-RodSync-Test/.
Candidate DLL SHA256 9805D9013F30547F4D05540ACC1E7B7F18E4078FD524C327BE50AB9636847D40.
Includes the audited licence/exporter subset and explicit candidate notes that the
1.3.3 structural pass is not a 1.3.4 pass. No install, game launch or upload occurred.
Published 1.3.3 ZIP and installed DLL remain unchanged. Keep both as rollback.
Root csproj/Build.AllStandard still default to 1.3.3: do NOT rebuild that path over
baseline artifacts with this candidate source. Package.NexusBeta still hash-locks
the published 1.3.3 baseline correctly. Use the separate candidate build path above.

Next test: drive 2-12-0 fast, inspect both sides/final two drivers, forward/reverse,
steady/slipping; check another 10-coupled and all four 12-coupled variants. Rerun
stationary structural regression too. If it persists, obtain video/speed/frame-rate/
mod list; do not assume timing is the sole possible cause. Do not release as a
confirmed fix until visual/runtime retest. User has not requested installing this
new candidate yet.

## MIT fork / dependency-notice audit completed, 17 October 2026

James requested explicit MIT requirements and a complete credits review. Confirmed
upstream 24e24d5 LICENSE unchanged in release ZIP. JimTaplin remains credited for
B99 port; SatiricRug / Peter Schinske for original work; independent fork/no invented
endorsement; MIT-covered port and separately licensed dependencies distinguished.
MIT does not mandate public source/GitHub fork/notification/payment or separate
personal permission for the MIT-granted rights. Nexus/game-EULA requirements remain
separate. No blanket legal clearance was promised.

Corrected a release-packaging gap: retaining the full original 25-file exporter did
not itself establish all third-party notice obligations. Reduced Nexus package to
13 unchanged files actually needed for OBJ mesh export. Removed FMOD, FBX/native
texture libraries, SixLabors libraries and Cecil debug helpers. No runtime code
changed. Both rod OBJ outputs from the extracted final ZIP byte-match the original
full-exporter baseline. Mod DLL retains the exact structurally passed hash.
Full upstream licence texts now shipped in licenses/ with THIRD_PARTY_NOTICES.txt:
version-pinned AssetStudioMod 0.17.3 MIT, CSspv BSD-2, K4os MIT/LZ4 BSD-2,
Cecil MIT, Json.NET MIT, Brotli MIT, .NET MIT and supplementary notices. LZMA SDK
public-domain source identified. No missing licence was silently treated as MIT.

Nexus current official File Submission Guidelines (updated 29 September 2026)
require AI tagging/disclosure. Credits/release copy disclose Codex-assisted B99
code and JimTaplin testing. Publisher notes explain AI-Generated Content for the
substantial generated port code and AI Media if the generated page description is
used; do not imply MIT exempts platform rules or invent limited AI involvement.

Updated Desktop release folder now contains corrected ZIP, updated Nexus copy,
checksums/manifest, MIT_FORK_CHECKLIST.txt and standalone CREDITS.txt. Replaces prior
ZIP at same name. New SHA256:
4A8F1660AAFA1A03A2607B843AB20FB6DE7C625FFD08A94369767DBD8558FCBD.
Still 31 files total (13 exporter files plus licence/notices/docs/mod files).
No upload, installed-game change, commit or push. Package.NexusBeta.ps1 now uses
an allow-list and includes notices; developer trial packaging still keeps its old
full exporter and must not be substituted for the audited Nexus ZIP.

## Nexus beta files prepared for JimTaplin, 17 October 2026

James wants to upload a beta on Nexus and explicitly selected JimTaplin for the
B99 port credit. Prepared a local release only; nothing uploaded or published,
no messages sent, no commit/push, no installed game changes. Original SatiricRug /
Peter Schinske credit and LICENSE retained. Homepage/update-feed fields are omitted
from beta Info.json so a new upload does not direct updates to the old upstream page.
Mod ID stays RearrangedS282, numeric version 1.3.3, title B99 Conventional Beta.
Public release/file label is 1.3.3 Beta 1; no runtime code rebuild/change is needed.

Script Package.NexusBeta.ps1 packages only the exact runtime-tested DLL (SHA256
DA6184725FFB341D13BDAFCC04048F677B973937066144361B5DCF671DABC1DD), original LICENSE,
new README_BETA.txt, CREDITS.txt, blank manual checklist and the original release's
25-file Windows exporter. Validates original archive hash, passed runtime evidence,
31-file inventory, every exporter file against original, license hash and DLL hash.
241 existing offline checks also pass; independent stationary runtime pass remains
recorded separately. No logs/JSON diagnostics/game meshes/game assemblies/duplex
bundle/cache/save/internal handover are included in the playable download.

Nexus ZIP SHA256 C7959EE03068360582E0844267E8B047417ACB80E02D79EA39098CDC4AA897A3.
Project outputs: release/1.3.3-Beta1/.
Desktop upload folder:
C:/Users/james/Desktop/Derail Valley Mods/RearrangedS282-B99-1.3.3-Beta1/.
Files: RearrangedS282-B99-1.3.3-Beta1.zip, NEXUS_RELEASE_COPY.txt, CHECKSUMS.txt,
release-manifest.json. All four Desktop copies hash-verified.
NEXUS_RELEASE_COPY.txt supplies title/summary/description/requirements/installation,
file description/changelog/credits and publisher notes. Explicitly no duplexes,
no saved selections/random spawning, native equal-axle-load approximation, six
native oiling circuits, remaining driving/refill/lifecycle beta test limitations.
User's positive visual report is described only as initial positive feedback.
No new Nexus URL was invented. Original source link is explicitly upstream, not
represented as the unpublished port repository. Existing installed DLL still matches
the tested 1.3.3 hash. User uploads the ready ZIP; source publishing was not requested.

## Manual checklist installed, 17 October 2026

James requested a manual arrangement/oilcup checklist and installation. Added
MANUAL_TEST_CHECKLIST.txt to the repository, future package script, rebuilt ZIP and
installed mod folder. Includes V/M/O/T/D/R results for all 31 arrangements, six-cup
open/refill/close/motion/retention record, loaded slip/sanding, LOD/unload, isolation,
direct-switch stress checks and issue evidence instructions. Updated testing notes
to reflect the confirmed structural pass. Installed DLL is unchanged from the
structurally passed hash DA6184725FFB341D13BDAFCC04048F677B973937066144361B5DCF671DABC1DD.
All 30 packaged installed files verified. Prior passing JSON preserved. Only two
documents were installed/updated. No game launch. Receipt/previous notes/report in
B:\GitHub repo\rearranged-s282-b99\evidence\manual-checklist-install-20261017-010419.

## Confirmed all-standard structural pass: 1.3.3, 17 October 2026

James ran the installed 1.3.3 structural suite. JSON version 1.3.3.0, subject L-021,
control L-052, passed=true, all IDs 0–30 passing with no per-arrangement errors.
Installed DLL SHA256 remains DA6184725FFB341D13BDAFCC04048F677B973937066144361B5DCF671DABC1DD.
Player.log:8763 and UMM log both say All 31 structural checks passed. No RearrangedS282
error/exception line was found in this run.

Independent review of saved JSON: all 31 converted snapshots have expected physical,
entity, powered-manager and powered-port axle counts; all 31 restored snapshots
exactly equal the baseline. External interactables were loaded, and all six native
oilcups were captured. All 31 conversions preserved the cup/provider identities,
sync tags and refill/flow port IDs. Runner success also includes unchanged native
mass/engine/shared type and second-car isolation. Five driver-radius groups appear
as expected: 1.03376 (3 arrangements), .9144 (3), .8509 (3), .712 (18), .6096 (4).

This supersedes the earlier outstanding stationary structural validation. It does
not establish moving cup alignment, opening/refilling/oil consumption, rod/LOD visuals,
loaded pulling/wheelslip/sanding, audio/particles, external unload/reload or repair/
explosion behaviour. Those remain manual/lifecycle tests; persistence is still off.
No source/build/install changes made in this review.

Preserved JSON, Player.log, UMM-Log.txt and review.json:
evidence/all-standard-1.3.3-passed-20261017-005225/.

## Current installed trial: 1.3.3 — drivetrain hierarchy and native oilcups, 17 October 2026

James reported 1.3.2 still failed and added B99 oilcup support to the ongoing fix and
reinstall request. Latest report: version 1.3.2.0, subject L-009, control L-052;
first conversion failed in SetupDrivers at IL 0x004c, remaining 30 cases blocked by
initialization. Player.log:8928–8932 confirms this. The previous external-control
fix got past its old failure. The new stack maps to dereferencing driveL after the
old DriveMechanism L path lookup. No arrangement had yet been exercised.
Failed-run mod backup/report/logs: evidence/all-standard-1.3.2-failed-20261017-003720/.

Native-first audit: extracted the full installed S282 transform/component hierarchy
with the original AssetStudio library; decompiled installed LMS S282AMeshLoader.
Both use LocoS282A_Drivetrain L/R rather than DriveMechanism L/R. Updated all four
absolute root/side-rod lookups. Native roots have Animator and AnimatorPortReader.
Audited all literal driveL/driveR child lookups against actual native data; LMS
creates the extra brake-shoe child (confirmed in its installed loader).
The old static-body s282_handle_lubricator is also absent: removed its field, lookup
and position writes. Current external lubricator pivot already handles that control.

B99 oilcup requirement: native S282 has six circuits, three points per side:
Oiling0 on drivetrain root, Oiling1 under crosshead s282_mech_push_rod, Oiling2 under
main rod s282_mech_wheels_connect/s282_mech_push_rod_to_connect. Native external cups
have PositionSyncConsumer and OilingPointPortFeederReader; PositionSyncConsumerController
pairs tags with the native PositionSyncProviderController and syncs positions late
in the frame. Native OilingPointsPortController initializes refill ports. Preserve
this system; do not add per-driver circuits or invent another oil simulation.

Added B99Oilcups.Bind: uses existing controller entries, requires six native oiling
points, validates refill/normalized-flow ports and exactly one provider per cup tag,
rebinds via native SetProviderTransform and Sync. Runs on external load and arrangement
switches. The original provider entries remain authoritative; cloned rod fragments
do not register extra providers. Existing oil quantities/ports and refill control
behaviour are untouched. Runtime report captures cup/provider identities, tags and
port IDs, verifies preservation on conversion/restoration when external controls
are loaded. Actual refill/consumption and moving alignment still need user tests.

Installed 1.3.3 with game closed; no launch or save modification. All 29 package files
verified. DLL SHA256 DA6184725FFB341D13BDAFCC04048F677B973937066144361B5DCF671DABC1DD.
Clean compile; 241 offline checks pass, including native drivetrain child paths and
all six provider/consumer locations. This is not an in-game pass.
Backup, evidence and install receipt: evidence/drivetrain-oilcups-fix-20261017-004016/.
Old JSON renamed stage2-runtime-checks.failed-1.3.2-20261017-004016.json. Next test makes
a fresh stage2-runtime-checks.json. Rerun two-stock-S282 structural test, then manually
open/refill/close all six cups, check retained oil after switching and cup alignment
while moving and after external unload/reload. Geometry/driving remain experimental.

## Current installed trial: 1.3.2 — B99 external controls fix, 17 October 2026

James authorized fixing and reinstalling the failed all-standard trial. Completed
with the game closed; no game launch or save modification. All 29 installed package
files match the rebuilt ZIP. Version 1.3.2 installed DLL SHA256:
B811908DDE8A5CC0BC5A0BF620C40BA24FD6B22F8FC5941F699DA8A1E801EA2C.

Native-first evidence: read the installed resources.assets using the original
AssetStudio library in a build-only probe. The current native S282 hierarchy has
Interactables/lubricator pivot/C_Lubricator, whose components include Button,
ButtonUseKeyboardInput, InteractablePortFeeder and RotatorPortReader. Its parent has
LubricatorRatchet. The compressor remains Interactables/C_BrakeCompressor, with
Rotary, ValveHandSnapper, ToggleValueKeyboardInput and InteractablePortFeeder.
Neither control has a HingeJoint. Both normal and exploded prefab trees show this.
The 1.3.1 failure was therefore both an obsolete path and an obsolete joint model.

Fix: replace legacy joint binding with the native lubricator pivot and compressor
Transform; capture native positions for those controls, labels and gauges. Apply
body offsets once from captured baselines after geometry changes; restore exact
captured positions for stock. Rebind on external-interactable reload. Remove the
five legacy blocks that accumulated label/anchor offsets and hardcoded joint anchors.
Missing required paths now produce descriptive exceptions before partial binding.
No new DLL, hook, dependency or alternate control implementation was added.
Runner report now includes assembly version and the actual conversion exception;
subsequent cases blocked by failed initialization are labelled as blocked instead
of producing 31 generic conversion failures.

Verification: 0 build warnings/errors, 196 offline checks passed, including actual
native hierarchy matches, absent legacy hinges/path and baseline-based placement
in the compiled binary. These do not establish in-game success. Rerun the same
all-standard structural suite on two stationary stock S282s, regulators closed,
then inspect driving/visuals manually. Controls should also be manually checked
on a small-driver arrangement and after returning to stock.

Backup, native hierarchy evidence, probe, failed report/logs, validation and receipt:
evidence/b99-controls-fix-20261017-000553/.
The old installed JSON was renamed stage2-runtime-checks.failed-1.3.1-20261017-000553.json.
The next test creates a fresh stage2-runtime-checks.json. The all-standard ZIP under
build now contains 1.3.2; previous 1.3.1 mod files are in the backup above.

## All-standard runtime review: initial setup blocked, 16 October 2026

The user ran the all-standard structural test on installed 1.3.1, verified DLL hash
53785A1EE3FC507263DFD4DC4AC7AD932E0BA4C3A2FDE41D5B3596C54007EF21.
Report subject L-017, control L-091: passed=false, 31 failed entries, zero converted
snapshots. This is one shared initialization failure, not 31 independently observed
arrangement defects. Player.log:7513–7517 and UMM-Log.txt:3645–3646 record the failure.

Confirmed: LoadExternalInteractables throws NullReferenceException at IL 0x006f.
Inspection of the exact installed binary maps this to GetComponent<HingeJoint>()
immediately following Find("Interactables/C_Lubricator"). Thus that transform lookup
returned null in the supplied loaded-external-interactables object. The current
replacement path/type and whether the supplied root differs remain unknown; do not
invent a B99 rename or blame another mod. Setup aborts before SetupFrontBogie,
SetupRearBogie or SetupDrivers, and conventionalFault refuses subsequent switches,
including stock ID 10. This explains every generic conversion-refused JSON entry.

All 31 recorded restored configuration snapshots equal the initial subject snapshot.
No separate stock-restoration exception was found. This does not establish full
visual geometry restoration or validate the arrangements. The earlier two-arrangement
stage 2 pass remains valid for its narrower path, which did not bind these controls.

Evidence preserved at evidence/all-standard-user-run-20261016-235152/:
stage2-runtime-checks.json, Player.log, UMM-Log.txt and review.json.
This turn reviewed evidence only; no source/build/install changes were made.
Next implementation work: inspect the native current S282 external controls and a
working comparable binding before updating the legacy control lookup; improve the
runner to report initialization-blocked subsequent cases clearly, then rerun.

## Installation and previous structural test confirmed, 16 October 2026

James explicitly authorized installation of the all-standard trial. The game was
closed. Version 1.3.1 is now installed at the game's Mods/RearrangedS282 folder;
all 29 packaged files match the verified ZIP. Installed DLL SHA256:
53785A1EE3FC507263DFD4DC4AC7AD932E0BA4C3A2FDE41D5B3596C54007EF21.
The game was not launched. Earlier statements that no installation occurred describe
preparation before this explicit authorization and are superseded by this section.

The newly supplied previous-stage report PASSED: subject L-006, control L-085.
The report confirms Berkshire 7/7 total and 4/4 powered axles, exact stock restoration
and unchanged control configuration; the previous runner's passed result includes
ten repeated round trips. Player.log and UMM Log.txt both record structural success.
This validates the previous two-arrangement stage 2 path, not the expanded geometry.
The all-standard suite and manual driving/visual tests remain outstanding.

Complete pre-install mod backup, previous diagnostics, logs and installation receipt:
evidence/stage2-passed-before-all-standard-20261016-231402/.
The old report was also renamed in the installed mod to
stage2-runtime-checks.previous-trial-20261016-231402.json, so it cannot be confused
with a new all-standard result. The next run creates stage2-runtime-checks.json.

# Rearranged S282 B99 port — all-standard development trial

## Current state: all 31 conventional arrangements exposed for testing

James explicitly requested expanding the trial to the remaining non-duplex
arrangements so he can test all of them, and asked where the structural button is.
The earlier instruction remains: prepare the build; James runs the game tests.
No game installation, launch, save selection or save modification was performed.

`Build.AllStandard.ps1` builds `B99_ALL_STANDARD` (Stage=3), version 1.3.1, and creates
`build/RearrangedS282-B99-all-standard-trial.zip`. It reuses the original 1.2.9
conventional geometry and rod exporter, with the existing per-car B99 physics adapter.
All IDs 0–30 are selectable; duplex IDs 31/32 and diagnostic ID 33 remain excluded.
The original archive's exporter is packaged verbatim; game meshes, assemblies and
the old duplex drivetrain bundle are excluded. Derailroader remains read-only and
the separate chuff patcher is unchanged. No new traction implementation was added.

The selected car is initialized lazily, snapshots stock and prepared geometry,
rebuilds active physical bogie axles and powered wheel registration, checks counts
against the selected arrangement and updates native radius/RPM/circumference/entity
data. Stock restores captured state; failed conversions attempt rollback. An
incomplete geometry setup is not retried over partially generated objects.
Old mass/cylinder reset constants are removed from the active geometry path.
The explicit equal-load approximation remains; these are not calibrated prototypes.
Only the radio patch is registered. Spawn/save/repair/stress/global-bogie hooks remain
inactive. External interactable unload/reload, explosion/repair, gadgets and broad
compatibility are unverified and must not be described as complete.

The button is **Ctrl+F10 → Rearranged S282 → settings → RUN STRUCTURAL TEST — two
stationary S282s**. This is separate from manual radio operation. Put both engines
in stock 2-8-2, stopped, railed, regulators closed, then start and close UMM so native
ticks run. The runner cycles one subject through all 31 arrangements, returning to
stock each time and checking configuration and second-car isolation. It retains the
filename `stage2-runtime-checks.json`, writes each arrangement's progress, and writes
a failure report when insufficient eligible engines prevent starting. The old trial
did not run automatically; the previous instructions did not make the button clear.
See `ALL_STANDARD_TESTING.md` for installation and test instructions.

Offline verification: Stage 3 compiles with 0 warnings/errors; 184 checks cover
catalogue/radio cycles, numerical conversions, reachable binary paths and packaging.
The original exporter was actually run against the installed B99 resources in a
build-only probe: rod OBJ has 508 vertices and 524 faces. This confirms extraction
and source topology size, not the resulting in-game appearance or Unity importer.
Only the earlier Stage 2 manual Berkshire conversion below has runtime evidence.
All-standard geometry, restoration/isolation, driving and visuals await James's run.

Native-first decision: reuse original supported geometry and existing B99 per-car
adapter; do not introduce a custom force model, global hook or new runtime dependency.
The previous stage 2 ZIP remains available separately.

---

Date: 16 October 2026. Scope authorized: stages 1 and 2 of the conventional-only port.
No duplexes. No game installation, save modification, or game launch was performed.

## Latest state: stage 2

### User runtime review, 16 October 2026, after the first trial

James chose to run the game tests himself. He installed the exact trial DLL (SHA256
`3052623DF331B53BC99CBDE979598E60304CB5CE896BE4C9865CA5D22ADB3AA6`). Current
Player.log and UMM Log.txt confirm successful mod activation and one manual conversion:
L-024 → s284, car/entity axles 7/7, powered manager/port 4/4, driver radius 0.712 m,
friction coefficient 0.240000024, base mass 111000 kg. His screenshot shows the two
small trailing axles and four driver axles. No RearrangedS282 exception was found.

`stage2-runtime-checks.json` was absent from the mod folder and the searched game/
Altfuture locations. Neither log records the automated runner's completion/failure,
return to stock, or repeated round trips. Therefore stock restoration, second-car
isolation and repeated switching are **not yet confirmed**. Do not report the full
structural suite as passed. The runner currently returns without a report when fewer
than two fresh eligible S282s exist; that is a possible explanation, not an observed
cause. A manually converted car is excluded from the runner's fresh-car search.

The repeated `TrainCar.isEligibleForSleep` MissingFieldExceptions are attributed by
their stack traces to `UtilitiesMod.UtilitiesMod.Update`, not this port. Other errors
include CCL missing liveries, a DE6 Mesh Splitter warning, AI Traffic despawning and
shutdown errors. No causal relationship to the S282 conversion was established.

Evidence copies: `evidence/stage2-user-run-20261016/Player.log`, `UMM-Log.txt`,
`review.json`. Review only; no mod/source fix or game changes were made in this review.
Next runtime check: two newly spawned, stationary native S282s, before manually
converting either, then use the UMM structural-check button. Loaded performance,
actual slip onset and moving/distance visual validation also remain outstanding.

Stage 2 source and build are implemented, with native runtime validation still pending.
The game was closed. James was asked whether to use a disposable sandbox or prepare
the test build for him to run; no answer had arrived at the time of this handover.
Do not treat that unanswered choice as permission to select a career save.

- `Build.Stage2.ps1` produces a B99_STAGE2 DLL and a development trial ZIP under `build`.
- Stage 2 compiles with 0 warnings / 0 errors. Its 73 offline checks pass.
- The stage 1 build and its 146 checks remain available independently.
- `Main.Load` registers **only** `CommsRadio_Awake_Patch`. No PatchAll, spawn, save,
  repair, stress or global bogie patch is registered. No custom traction DLL or CCL
  dependency was added.
- Only stock ID 10 and Berkshire ID 11 are exposed by the stage 2 radio.
- `WheelRearranger` now has a partial stage 2 implementation. Initialization is lazy
  on selected-car conversion; its automatic Start is empty in stage 2.
- Stage 2 bypasses the old broad geometry/mesh-extraction path. It reuses the original
  two small trailing-axle positions by cloning the native leading axle twice, and
  uses that axle's native unpowered rotation component. The original trailing axle
  and native array references are restored when returning to stock.
- `B99PhysicsState` captures per-car entity friction/axle data, driver registration,
  live powered axle port, force radius, feeder conversions and driver animation
  circumference. Apply/restore writes the selected car's instance data only.
- Native ECS `StaticSharedAdhesionCalculationData.numOfAxles` is updated along with
  car/bogie caches; stale slip/slide output is reset. Native weather, sanding and
  braking continue to run. Driver radius and four powered axles remain unchanged for
  Berkshire; generic conversions are present for later radius/driver-count work,
  which is not yet runtime validated.
- No mass/cylinder/reset constants are applied in the active path. Native mass,
  cylinders, cutoff and shared car type are left alone.
- `ConventionalPhysics` reuses Derailroader G29/C21 driver-weight compensation and
  native RPM conversion. Tests execute those profile examples. Equal axle loading
  is the explicit prototype assumption here: four driven axles out of seven,
  versus stock four out of six, yields 6/7 adhesive allocation at unchanged mass.
  No suspension loading measurements or loaded performance tuning are claimed.
- Radio confirmation now reflects success/refusal; it owns a cloned highlight
  object instead of moving the vanilla deleter's object. Materials/highlighter are
  cleaned up with the mode.
- Motion, derailment, open regulator, substantial residual force, wheel slip and
  incomplete initialization refuse the conversion. Failures attempt captured-stock
  rollback; the transition lock releases in finally.
- `Stage2RuntimeChecks` is an explicitly invoked test in the mod manager. It runs on
  two fresh native S282s, waits for native ticks, compares snapshots, repeats ten
  round trips and writes `stage2-runtime-checks.json`. This test has **not run**.
  Its successful result would establish structural restoration/isolation, not visual
  correctness, loaded pull, steam consumption or actual slip onset.

Read `STAGE2_TESTING.md` for the disposable sandbox procedure. Do not mark stage 2
fully validated until the native test and representative driving/visual checks pass.
Do not enable the other arrangements as part of this trial.

## Source and environment

- Project: `B:\GitHub repo\rearranged-s282-b99`
- Branch: `codex/b99-standard-stage1`
- Upstream: `https://github.com/pjschinske/dv-rearranged-S282`, commit `24e24d5`.
- Upstream Info.json identifies version 1.2.9, matching the supplied release metadata.
  This is not a claim of byte-for-byte reproduction of the original DLL.
- Supplied archive: `C:\Users\james\Desktop\new mods\Rearranged S282-752-1-2-9-1710020512.zip`
- Archive SHA256: `18B2CA6B73EBFECE33401DB924DDFECDACFEBCFDF193E69707CB436AB56E778C`.
- Game reference installation: `B:\SteamLibrary\steamapps\common\Derail Valley`.
- Prior game log identifies `99-build2702`, Unity 2019.4.40f1.
- Installed Loco Mesh Splitter: 1.1.9. Its B99 gadget collision APIs already exist;
  do not introduce a replacement framework.

## Stage 1 implementation retained as a separate build

The conventional controller is compiled against actual B99 references. Duplex simulation
classes, duplex Harmony patches, the unused cylinder-count patch, and the duplex asset
loader are excluded by the project. Duplex creation, rear drivetrain setup, cloned
Franklin gear, particles, and duplex mesh generation have been removed from the
conventional controller/mesh loader. Original excluded source and model files remain
in the checkout as upstream history; they are not active support.

All original enum values 0–33 and name-table positions remain stable. Conventional IDs
are 0–30. IDs 31/32 are retired duplex entries, 33 is the old no-drivers diagnostic.
Radio traversal excludes retired IDs; unsupported IDs normalize to stock ID 10.
Random settings contain no duplex choices and default random spawning is off.
Empty random selection falls back to stock. These paths are not activated in stage 1.

The stage 1 entry point is gated by the project's `B99_STAGE1` define: it only registers a
status label and logs the baseline state. It applies **no Harmony patches**, does
not load arrangement settings, does not initialize meshes, and does not read/write
arrangement save data. This preserves a stock baseline by construction, not by an
in-game test. Do not claim the arrangements work in B99 yet.

The B99 rename `SimulationFlow.orderedSimComps` → `OrderedSimComps` is updated. Old
AdhesionController friction writes have been removed; the per-car B99 replacement
is stage 2 work. Do not enable the runtime paths before completing that work.

The original Publicizer 2.2.1 dependency is retained. It publicizes **local build copies**
of game references, never the installed assemblies. Harmony, Unity Mod Manager and
Mesh Splitter references point to the installed game; no new runtime dependency or
CCL conversion was added. The old automatic packaging/post-build actions are absent
from the updated project. Upstream auto-update metadata was removed from this port.

## Reproduce and validate

Run PowerShell in the project directory:

```powershell
.\Build.Stage1.ps1
.\Validate.Stage1.ps1
```

Both accept `-GamePath`. Build output is `build\stage1\RearrangedS282.dll`, with
`build.log` and `build-receipt.json`. Offline validation records `validation.json`.
The scripts do not install into Mods, invoke AssetStudio, or launch the game.
Do not use the upstream `package.ps1` for this development baseline.

Validation covers original ID stability, conventional radio traversal in both
directions, retired/invalid ID fallback, compiled absence of duplex simulation and
asset loader, entry-point isolation, and existence of prospective B99 Harmony targets.
Latest run: Release build succeeded with **0 warnings and 0 errors**; **146 offline
checks passed**, 0 failed. Final method-inventory review confirms only duplex-specific
controller methods and the duplex timing helper were removed; conventional methods remain.
Method existence does not prove Harmony signature matching or transpiler correctness.
In-game UMM loading, Console behavior and stock driving are not runtime validated.

## Stage 2: existing reference implementation first

Derailroader was inspected read-only and must remain unchanged:

- `B:\GitHub repo\dads-derailroader-exp\tooling\builder\tools\unity\CclLocoBuild.cs`:
  driven axle registration, powered animation radius, unpowered truck rotation,
  mass/radius/friction configuration, sparks.
- `tooling\locos\g29\profile\G29Config.cs` and `tooling\locos\c21\profile\C21Config.cs`:
  friction compensation for driver weight versus DV axle allocation.
- `tooling\builder\tools\audit_new_loco.py`: agreement between powered-wheel count,
  simulation poweredAxles and animation radius.
- `src\rr2dv\record.py`, `enginemetrics.py`: unit handling and TE calculations.
  The driver-weight UI estimate itself does not update runtime axle loads.

Keep the existing S282 runtime rearrangement design. Reuse those conventions without
importing the full converter or converting the mod to CCL. The remaining gap is how
to refresh already initialized per-car B99 entity/cached state safely. Never change
shared S282 type data to update just one locomotive.

Prove stock ↔ 2-8-4 first, then 4-6-2 and 2-12-2. Verify a second S282 is unaffected.
James is developing a separate chuff patcher; do not replace or expand it here.

## Known outstanding audit findings before enabling patches

- Runtime switching needs per-car driver radius, powered-wheel registration, axle
  caches and B99 entity adhesion updates using the existing native simulation.
- Reset values are B98 constants (mass/bore/stroke/cutoff). Capture and restore the
  current B99 per-car baseline rather than assuming these are correct.
- Upstream ten-coupled random flags incorrectly map to eight-coupled IDs in Settings.
- The sixth-axle sparks definition currently points at driveAxle5. Correct alongside
  powered-wheel registration and verify the resulting effects.
- BogieAxlesPatch currently affects all bogies; narrow it to rearranged S282s.
- Repair restoration currently has no S282 guard; narrow it before activation.
- Check every Harmony signature/initialization order and the TrainStress transpiler
  against B99 before registering patches.
- Persistence load retains a static map across loads; validate/reset it at the right
  boundary before enabling save writes. Keep enum numbers and the legacy misspelled
  save keys stable. Do not erase saved choices merely to test the stock baseline.
- Current radio mode shares a vanilla highlighter; audit ownership before activation.
- Inspect object paths/mesh vertex ranges, LODs and gadget collision refreshes.

No change to Derailroader, the user's chuff patcher, installed mods or saves was made.
The only ZIP is a local stage 2 development trial. No release, commit, push or
publication was performed.
