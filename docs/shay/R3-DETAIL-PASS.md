# Shay R3 model and detail pass â€” 11 November 2026

The model pass is staged, not installed or accepted in Derail Valley. James
performs game testing. The active repository remains `rearranged-s060`; the
older Documents checkout and original LLW authoring delivery were not edited.

## Result

- Added deep perforated frame girders, seated cross-bearers, knees, rivets and a
  strapped domed air reservoir to fill the space below the boiler. The reservoir
  is cosmetic; no air, water, fuel or adhesion capacity changed.
- Replaced the thin steam runs with two 120 mm outside-diameter sleeve feeds,
  one 140 mm admission riser, two substantial engine-top manifolds and a separate
  140 mm exhaust return to the single smokebox/chimney. The offside feed returns
  alongside the frame, turns beneath the boiler and blends into the engine-side
  feed. Swept bends, flange rings, studs, clamps and support stays are authored.
- Both retained native sleeve ends have 32-sided smooth hollow tapered reducers,
  including inner walls and annular lips. The sleeve radius is measured from the
  actual cut native triangles. Shay alone leaves these native cut ends open;
  other users of `S060BodyPipes` retain their existing capped behavior.
- New pipe walls have cylindrical UVs measured along their circumference and
  length. Custom components use their own gunmetal, steel or authored materials.
  Native wheels retain the live S282 mesh, native UVs and native S282 material.
- Six cylinder-cock outlets are on the projecting end-head drain bosses and point
  downward. Actual native particle-system descendants are aligned at the port,
  removing their inherited sideways yaw. Jet size, speed and duration are reduced;
  water drips use compact 14 mm emitters. Native timing and steam-flow controls
  remain responsible for operation.

## Final authored fit

The agreed physical dimensions are 0.80 m wheels, three 252 x 300.808 mm cylinders,
0.15 m crank throw, 0.90 m rods and 86:20 bevel gears (4.3:1). The concurrent
approved physics tune is 1.6 kg/s, 45 L chest and 92% efficiency; this pass does
not retune it or alter save identities. The old procedural engine fitter remains
retired. `engineFitRevision=2` identifies the fully authored engine.

`Shay/ModelSource/shared/fit-contract.json` is the frozen R3 model contract,
SHA256 `617AE50BEBEC7759064FDDAED09CBE26D258072A1836A48BBEC2B52EC4407F77`.
Its original source/runtime fixture identities are provenance of the R2 input,
not the current candidate DLL. Current build hashes are in the candidate receipt.

The engine root is (1.10, 0.57, 0.11); cylinder pitch 0.35 m, valve axis X -0.249 m.
Axle Z stations are -2.18, -0.94, 1.20, 2.44 m. Each truck retains its 1.24 m
wheelbase; native pivots stay at -1.40 and +0.925 m. Wheel scaling affects Y/Z
equally and leaves gauge/X unchanged. Truck shaft X/Y are 0.96976744/0.40 m.
The larger front 1:1 reversing pair, relocated shaft endpoints, open yokes and
telescoping member dimensions are exported together in the 43-binding motion
metadata. No additional transmission ratio or physics hook was added.

## Validation and fixture corrections

- Runtime build: zero warnings/errors; candidate DLL SHA256
  `F594C549CC2FB9883B77E767F89686523E705092F92CA67169F39325EF6BDBA1`.
- Original-only reopened asset bundle: 43 transmission bindings, four empty
  native wheel aliases, two truck frames, no scripts/colliders/rigidbodies or
  redistributed native meshes/textures. Bundle SHA256
  `6084FA22374A031EBE5D92AC8909ACBABEDEB5F102BD8A270778836E6E88E189`.
- `build/shay/combined-contact.json`: 11,322 source geometry checks; two LODs,
  actual bevel surfaces, engine motion, native wheel/ring fit, shaft bores and
  articulated joint clearances. Pinion/shaft minimum margin is 4.315 mm.
- `build/shay-r3-20261111/assembly-validation.json`: 21,788 transmission/truck
  contact checks and supported fixed hangers, 49 poses x eight phases per truck.
  These are discrete samples, not a continuous-sweep proof.
- `native-geometry-20261111-215057`: 2,609 checks against the matching DLL/bundle,
  including native-body relief, exact pipe/body/fixed-engine triangle clearance,
  ten engine phases, three truck yaw poses, turning roles, materials and cleanup.
  Only tightly clipped real port/flange interface volumes permit pipe contacts.
  This legacy report's `tyreClearance` flag is overbroad: it is not a new full
  wheel-versus-native-body sweep. The fixture source now reports the scope correctly.
- `native-cocks-20261111-215357`: 284 checks against this exact candidate. The
  older installed runtime failed the added native rotated-child regression;
  the corrected fit passes. Appearance in DV remains for James to assess.
- `native-preview-20261111-215328`: 42 material/UV/relief/restore checks and fresh
  previews. This is deliberately a preview-only run, not another geometry suite.

The old private wheel fixture had projected UVs, and the preview assigned the
S060 body atlas to it. Both were fixture defects. `Prepare.NativeMaterial.py`
now reads the native mesh's actual UV0 and its owning renderer's S282 material.
Vertices and triangle indices were verified identical before correcting UVs;
the preceding geometry evidence therefore remains applicable. Runtime wheel
binding already retained the genuine native mesh/material and was not remapped.
Do not "fix" this preview error by changing production wheel UVs or importing
native assets into the original-only bundle.

The importer also exposed a real material identity mismatch: Unity imports
material names from filenames, so `material_S060_MACHINED.mat` did not match the
runtime finish slot. Material asset filenames now match their logical names,
and reopened-bundle validation checks them. The final geometry fixture removed
an obsolete reflective call to the retired fitter's `OnDestroy`; no runtime
rebuild was required for that fixture correction. The first full pipe fixture
timed out before using its existing regional triangle filter; the bounded rerun
uses that filter and passes. No licence changes or game launches were made.

## Reproduce and hand over

`Build.Shay.ps1` regenerates editable original sources, validates and exports in
the isolated `build/shay-r3-authoring/UnityProject`. It does not contend for the
concurrent Meyer authoring project. `Shay/Test.Native.ps1` reuses the existing
native fixtures there; `-Only preview` refreshes visual evidence without rerunning
unchanged geometry. Private native fixtures remain under ignored `build/qa`.

The matching DLL, Info.json and `assets/Shay/shay_running_gear` are together at
`build/shay-r3-20261111/candidate`, with receipts and a staged-only notice. This
includes concurrent unreleased Meyer/physics source and is not a complete UMM or
Nexus release package. Installed 0.23.4 was left unchanged, SHA256
`50D4C3D99678246B1350828B22DF4CAD2327EDCA78CB643B8F2BB365179EF1C7`.

Await James's testing after a separately authorised installation: inspect the
two sleeve reducers and under-boiler junction, wheel/material appearance under
game lighting, the six cock jets, curve articulation and return to other layouts.
Boiler sustainability and hill/adhesion acceptance belong to his physics testing.

Local source preservation uses branch codex/shay-r3-detail-checkpoint-20261111
with an isolated Git index. It captures the current integrated runtime (including
concurrent unreleased Meyer/physics changes), Shay authoring and its native
fixtures. The active branch, normal index and live working files stay in place.
No push, release package, installation or publication is implied.
