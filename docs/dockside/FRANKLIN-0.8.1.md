# Franklin Dockside 0.8.1 — wider wheelbase and fitted smokebox sleeves

11 November 2026. STAGED ONLY. James authorized the appearance transfer,
offline build/validation and staging, then explicitly requested increased
wheelbase to reduce inter-engine contact and the fitted Shay smokebox sleeves.
This supersedes the earlier contact-limited 0.8.0 stage. No installation, DV
launch, push or publication was performed. James owns gameplay acceptance.

## Final result

The later S282 2.1 Franklin presentation is adapted to S060 dimensions: continuous
rod/bearing skins with intentional normals/UVs/tangents, wheel return cranks,
input shafts, hub sleeves, gearbox collars, forked crossheads, split guides,
small-end pins and round piston rods. Both existing native absolute-crank
Animators, original shaft/joint motion and 90-degree side/45-degree engine
phasing remain. No second motion clock or extra DLL/dependency was introduced.

Bogie-frame/engine centres move from ±1.40 to ±1.75 m: 350 mm outward per unit.
Four driven axle centres are Z -2.35/-1.15/+1.15/+2.35 m. Overall driven-axle
wheelbase increases 4.00 → 4.70 m. The two axles within each engine retain
1.20 m pitch and 1 m wheels, with unchanged gauge and cylinder calibration.
The existing S060Articulated.Apply clones native physics axles at these actual
authored wheel positions. Native support/traveller/joint owners remain the
existing offset-frame mechanism (as in the proven S060 0-4-0), with restoration
owned by the existing conversion lifecycle. No new support or physics service.

The clearance fixture reads the installed native LocoS060/Axle_F and Axle_R
transforms: Z +.925/-1.40 m, not another locomotive's coordinates. It rotates
the offset frames about these measured native supports. An earlier model-centre
sweep was corrected before final acceptance. Final referenced mechanical
vertices/bounds have minimum longitudinal separation **33.4479 mm** through
128 phases at synthetic opposing support yaws -12/-2/0/+2/+12 degrees. Separate
phase extrema cover independent sampled engine-phase combinations. Deliberate
steam bridges are excluded from that mechanical envelope and tested separately.
Maximum mechanical |Z| is 3.920306 m within the retained body-length budget.
The actual exported axle positions are checked without overriding the anchors.

The targeted actual-triangle boundary sweep is now **zero contacts** for both
retained/revised main rods and new cranks against the neighbouring barrel,
front cover and flange: 32 phases × two sides × five support yaws = 320 cases.
This corrects the earlier 0.8.0 contacts; the old-spacing trial remains a negative
control. These are bounded offline poses, not live track/derailment acceptance.

Both smokebox ports carry body-mounted hollow reducers with the Shay rolled-lip
style and its existing dirty-gunmetal finish. The default circular Shay factory
output remains bit-identical to its original mesh recipe.

**Corrected after James's screenshot:** the first Dockside adaptation was an
oversized circular cup around an angled pipe. The earlier maximum-radius/bore
clearance test proved containment, not a fitted joint, and was insufficient.
That cup and its previews are superseded. Do not reuse the .180/.172 m mouth or
claim that a large positive clearance establishes a fitted sleeve.

Dockside now uses actual native triangle sections at the cut and 12 mm above it.
The connected pipe-wall arc defines the snug upper opening; projecting native
mounting edges sit within the outer annular shoulder. This retains the native
facets and inclination instead of replacing the profile with a circular bound.
The lower cuff follows actual manifold sections 95/110 mm below the joint, with
0.7 mm insertion overlap and an 8 mm outer wall. Its shape updates only when the
existing manifold fitter refits; no independent motion clock or simulation.
Native cut caps are open for Dockside. Both sleeve objects/meshes belong to the
existing pipe owner and are destroyed on removal, restoring the native mesh.

## Exact stage and evidence

Candidate: `build/dockside-franklin-wheelbase-20261111/candidate/RearrangedS060`.
Runtime 0.24.0 SHA256: `0AAF86EE687473EF87665C695EA85AC6E86D34EB1773D0F5CEE0297A5B91D65F`.
Dockside 0.8.1 bundle SHA256: `5672D551FB02DF36143810B98774E4F9B7334CDE9B3F73C41BA1CB118D7253FF`.
Export: `build/dockside/authoring-20261111-230620`.
Full payload/archive/fixture/source receipts: stage `validation.json`.

- Runtime build: zero warnings/errors. Sleeve presentation changes only
  S060SteamPipes, native profile extraction in S060BodyPipes, the shared sleeve
  factory and Shay's call to the same factory;
  simulation, force, save, selection and oil-circuit source hashes are retained.
- Reopened pack: 23,883 detailed mesh checks, ten generated meshes; 24,925 motion
  assertions + 8,923 absolute seek/isolation checks; native pin68, oil10,849,
  pipe423, effects177 and existing normal-rod3,120 checks. Native body/wheel
  meshes, textures and audio are not embedded in the exported pack.
- Native engine/control/flow17 against this export's calibration and wiring.
  Both calibration JSONs and wiring match the initial appearance candidate.
  The old installed debug pack had stale .30/.8/90 L definitions; regeneration
  matches already-current radio runtime .34 m/1 kg/s/115 L per engine. No new
  normal radio-operation tune is implied by that debug-data refresh.
- Corrected exact-DLL suites: sleeves4,602, pipes1,118, cleanup145 and Shay
  effects23 = **5,888 checks**. Evidence: `runtime-audits-20261111-234719`,
  with the final independent section-pair fixture in `runtime-audits-20261111-234935`.
  Both folders are under `build/radio-0.24.0` and bind this exact DLL hash.
- Independent triangle cuts/point-to-segment distances check the exposed native
  upper join and the whole manifold lower join at native support yaws
  ±15/±12/0 and car-world orientations 0/73 degrees. Maximum profile distances:
  upper **0.7002 mm**, lower **0.7051 mm**, within the 1 mm fit tolerance. These
  measure proximity, replacing the rejected cup's clearance-only test. Annular
  lip winding, default Shay mesh equality, cleanup/restoration/rebind also pass.
- Earlier quartering115/cocks284 evidence at `runtime-audits-20261111-232112`
  is retained for unchanged clock/cock sources, not described as a rerun against
  the corrected DLL. No DV scene, audio playback or rail-dynamics acceptance.
- Pipe caps retain exact angular/plane tolerances. The transfer-face fixture
  now locates the authored socket plane: after the wheelbase change a fitted
  upper boiler course can lie behind the transfer socket, invalidating the
  old global-Z-extreme lookup. No geometry rebuild was done for that fixture fix.
- The earlier cleanup fixture's stale S060BodyPipes reflection call was corrected
  to supply its existing default capSockets argument. No production fix was
  inferred from that fixture exception.

The final runtime includes preceding staged Meyer/physics and Shay R3 work.
The stage carries the retained Meyer 0.7.0 bundle and latest frozen Shay R3 running-gear model (receipt SHA256
D531005459B6C7D9B31F20D3418C4537A2FAEAA3AF2FB4DDA5B8EF65A9042B81),
with individual hashes. This is not a complete release acceptance of that work.
The installed mod is hash-checked unchanged. All 11 ZIP payload entries are
checked against staged hashes; no settings, logs, native QA extracts or PDBs ship.

## Preview and handoff

`fitted-preview` contains seven native-source stills (including close-ups of both
smokebox connections) and sixteen cycle frames; the stage provides an animated GIF.
The preview uses the production geometry source with local native textures.
The close-ups at zero bogie yaw were visually checked on both sides. The earlier
`final-preview` cups are marked superseded. Editor sleeve lighting/finish is an approximation; the actual DLL test checks
the real shared Shay finish. Native preview resources remain local ignored QA.

James tests both-side appearance, the wider gear on actual curves, sleeve joins,
cocks, oil servicing, save/reload and arrangement switching after a separately
authorized installation. No agent-controlled DV/profile/session was used.

Local source checkpoint branch: `codex/franklin-dockside-stage-20261111`, created
with an isolated index on the existing Shay running-gear checkpoint
`db9115843843ad66d0a5f27f1f7bcede267cefc4`. Exact commit is in the stage source-checkpoint receipt.
