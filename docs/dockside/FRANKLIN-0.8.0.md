# Franklin Dockside 0.8.0 appearance candidate — 11 November 2026

> Superseded by [0.8.1](FRANKLIN-0.8.1.md): James subsequently requested the
> wider wheelbase and fitted Shay sleeves. The earlier contact-limited stage
> below is historical, not the current handoff.

STAGED ONLY. James owns Derail Valley testing. No installation, DV launch,
push or publication. James explicitly chose staging with the measured engine-
boundary limitation recorded, rather than expanding into cylinder/engine layout.

## Result and scope

The donor is the later S282 multiplayer checkout's installed 2.1.1-beta.1
presentation, not the older b99-port 2.0.7 handover. Four donor presentation
sources match the installed-source receipt at build/audit-fixes-2.1.1.
DocksideDetailMesh adapts the continuous bearing/web skin, deliberate normals,
48-segment eyes and finished wheel-input concept to S060 dimensions. No S282
runtime or new DLL/dependency is added. The generated geometry is exported into
Dockside's existing CCL pack and driven by its two absolute-crank Animators.

Added a wheel crank, hollow hub sleeve, input shaft, gearbox collar, small-end
pin, forked crosshead, split guide rails and round piston rod. Existing shaped
rods now have a continuous skin. The big-end bosses retain .067 m radius,
.042 m holes and .048 m depth for existing native pins and oil seats. Main-rod
small ends use .050 m bosses/.022 m holes around .020 m bearing pins. Mesh UVs
sample a measured S060 native rod-face triangle, not S282 atlas coordinates.
The static gearbox has a solid cover; internal gears and a machined opening
are not modelled, following the donor's explicit presentation boundary.

The new 5 mm wheel crank fits X1.123..1.128 m, outside the retained native
shoulder and inside the original custom box. The conservative box gap is
0.986934 mm. The source-native shoulder ends at about X1.12139 m. This is a
rigid same-engine fit, not a live suspension or every-part clearance claim.
Wheel gauge, 1 m drivers, axle pitch/positions, rod length, engine phasing,
90-degree quartering, original five shaft channels/ratio, oil identities and
normal S060 simulation/save ownership are retained.

## Exact artifacts

Candidate folder: build/dockside-franklin-20261111/candidate/RearrangedS060.
Bundle export: build/dockside/authoring-20261111-222732.
Runtime 0.24.0: F594C549CC2FB9883B77E767F89686523E705092F92CA67169F39325EF6BDBA1.
Dockside 0.8.0 bundle: 600A9B6FAA8C743DCDA36FBB4B991F44A179E3019EDAC6C517DE0A3530F4763D.
The runtime is byte-identical to the staged Shay R3 candidate. Every runtime
source, Info.json and project file matches this task's baseline; no production
runtime source changed. This combined candidate therefore also contains preceding
unreleased Meyer physics and Shay R3 work. This pass is not whole-release acceptance.
The staged pack includes the retained installed Meyer 0.7.0 bundle and frozen
R3 Shay model from build/shay-r3-20261111/candidate, with individual hashes.

## Offline evidence

Baseline: the actual installed 0.7.0 Dockside bundle was reopened and audited
before geometry edits; baseline reports are under build/dockside-franklin-20261111/baseline.
Its SHA256 E239243D369A2BBD900AC05989A2B4F7E9AD3C1C0C8D6682013C57D371F69273
matches the installed bundle. No native mesh Read/Write setting was changed.

Final source build: 0 runtime warnings/errors. Reopened 0.8.0 bundle:
- Detailed meshes: 23,883 checks, ten unique generated meshes; actual triangle
  winding, welded closed skins (fork consists of deliberately separate solids),
  normals/tangents/UV containment, crank seating and rod/fork/guide surfaces.
- Existing motion: 24,925 pose assertions plus 8,923 seek/isolation assertions;
  129 samples per unit/side at synthetic yaw -12/0/+12 degrees. Maximum original
  rod endpoint error 0.000267 mm; new crank-pin error 0.000307 mm.
- Actual native pin fit: 68 checks; minimum bore gap 1.6814 mm. Oil geometry:
  10,849 checks/eight cups; maximum support error 0.000718 mm.
- Production pipe fitting: 423 checks; cylinder effects: 177 checks.
- Native standalone engine/control/flow probe: 17 checks using this export's
  definitions/wiring. It is not cab interaction or gameplay.
- Exact candidate native Unity suites: quartering 115, pipes 1,118 and cocks
  284 at build/radio-0.24.0/runtime-audits-20261111-222849. Cleanup 145 at
  runtime-audits-20261111-223144. Combined 1,662 checks; same runtime hash.

The retained 0.7.0 bundle's debug standalone definitions were stale (.30 m bore,
.8 kg/s, 90 L); regeneration correctly uses the already-current runtime constants
(.34 m, 1 kg/s per unit, 115 L). Normal radio-owned S060 operation already installs
those runtime definitions, and that runtime is byte-identical. Authored stroke
retains its existing one-ULP difference from the runtime literal. Steam wiring
JSON is byte-identical. Do not describe changed debug bundle fields as a new
radio-operation physics tune, or claim the old debug values match this export.

Two failed exports are retained: the first caught a folded short-crank web;
the second caught main-rod/central-guide contact. The web is now radius-based
and rails split around the fork. Final geometry preflight runs before export
and the same checks run on the reopened bundle. No rebuild was done for later
preview/report/tooling changes. Test.BundleEvidence now requires the additional
0.8.0 detail report while retaining old evidence compatibility. Cleanup's
reflection fixture was stale after S060BodyPipes gained capSockets; supplying
its existing default true fixed the fixture, with no production rebuild.

## Explicit fit limitation accepted for staging

inter-unit-measurement.json checks actual triangle surfaces for the front
engine's main rod and new crank against the rear engine's barrel/front cover/
flange. It samples 32 phases, both sides, synthetic opposing support yaws
-12/-2/0/+2/+12 degrees: 320 side/pose cases. The retained original rod recipe
contacts in 59 cases; the revised rod also contacts in 59; the new crank
contacts in 49. These totals do NOT establish identical contact locations,
all-body clearance, a live track reproduction or an acceptable operating yaw.
The first triangle witness is preserved. Correcting this needs separate
cylinder/engine fitting; James selected staging the appearance work with this
limitation recorded. No wheelbase/casting relocation was silently introduced.

## Preview and James-owned acceptance

Five native-material stills and a 16-frame straight-track cycle come from the
reopened pack. Native S060 preview geometry/textures remain ignored local QA
fixtures and are not shipped. Preview shader/lighting differs from DV.

James checks both-side shading, full forward/reverse motion, the documented
engine boundary, slow curves, cylinder cocks, oil-can servicing, arrangement
switching and save/reload after any separately authorized installation.
