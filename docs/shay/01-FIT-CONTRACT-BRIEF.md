# Session 1 prompt: fit the S060 Shay and freeze the asset contract

Read `B:/GitHub repo/rearranged-s060/docs/shay/README.md` and the repository's
AGENTS.md/handover. Prepare a private Unity 2019.4-compatible fitting scene and
a dimensional/marker contract for a fictional two-truck, three-cylinder Shay
arrangement based on Rearranged S060's current 0-8-0. This session prepares assets
only; do not implement or install a new runtime arrangement.

Work only under `B:/LLW CONVERT/locos/s060-shay/fit/`; put approved shared output
under `B:/LLW CONVERT/locos/s060-shay/shared/`. Read the S060 checkout and local
native QA references without modifying them or sharing its Unity project with
other sessions. Use an isolated authoring project. Do not run the game, install,
push or publish. Generated native reference meshes/textures stay private.

## Required investigation

Execute/inspect the actual current four-axle geometry, using the existing
`S060EightCoupled`, `S060EightLayout` and `ConventionalNativeAudit` as guidance.
Use local native references `build/qa/conventional-native.json` and
`build/qa/native-preview.json`, rebuilding them through Prepare.QA only if needed
and without changing shipped assets. The table in the Shay README is a reference,
not permission to assume the final pivot positions.

Establish two coherent two-axle visible truck frames and their relationship to
native bogie pivots, axle offsets and the body's swept envelope. Current 0-8-0
pair midpoints do not coincide with native support pivots. Compare the existing
Meyer/Dockside bogie attachment pattern before proposing a new physics object.
Do not alter the released 0-8-0 or invent a separate powered vehicle.

Test a three-cylinder vertical engine envelope on the RIGHT, near the cab front,
with a longitudinal crankshaft, four outside-right wheel ring gears, front/rear
line shafts, universal joints and sliding shaft sections. Keep cab entry clear.
Keep the current fixed 1.1394 m rolling diameter/gauge for this first study.
James explicitly prioritises DV wheelset reuse. Fit the original native S060
wheels/axles and live aliases, not new wheel meshes. Check native crank bosses,
gear-seat planes and brake clearance; prefer author-owned bolt-on hub covers or
gear-ring spacers, or hiding an existing separately rendered unused detail, to
rebuilding a tread/flange/spoke. If an integral boss remains visible, document
the fictional converted-wheel appearance rather than replace the whole wheel
for cosmetic purity. Do not redistribute an extracted/cut native wheel mesh.
If this size prevents credible gear/engine fit, document the measured obstruction
and a separate fixed smaller-wheel alternative; do not add a size slider or
silently change the common dimensions while downstream sessions are modelling.

## Deliver the shared contract before other model sessions start

- `shared/FIT-CONTRACT.md` and machine-readable `shared/fit-contract.json`, with
  a revision, source commit/runtime hash, units, coordinate system, exact native
  body/rail datums, wheel stations/radii, gauge and final proposed truck pivots.
- A local reference `.unity` scene/prefab with labelled marker transforms:
  `BODY_ROOT`, `ENGINE_MOUNT`, `ENGINE_CRANK_FRONT`, `ENGINE_CRANK_REAR`,
  `TRUCK_FRONT_PIVOT`, `TRUCK_REAR_PIVOT`, four `AXLE_*` wheel axes,
  `FRONT_LINESHAFT_IN`, `REAR_LINESHAFT_IN`, all four bevel pitch-cone datums,
  `STEAM_IN`, `EXHAUST_OUT`, `SMOKEBOX_RETURN`, six cylinder drain markers and
  footplate mounting/cab-entry exclusion volumes. Give all marker rotations.
- Separate engine/frame keep-out envelopes and transmission swept corridors.
  Report actual front/rear truck yaw tested (minimum +/-12 degrees as a model
  stress study, not a claim that this covers all DV curves), vertical/pitch/roll
  travel tested, telescopic engagement and min clearances in metres.
- A bill of author-owned parts required, logical material slots, and local live
  S060 material sources/alias method. Inspect `SteamBogieVisuals.LiveMaterial`
  and runtime live-material refresh patterns; do not bake an extracted game skin.
- Orthographic right/left/top/front views plus a curve/articulation view, showing
  actual S060 reference geometry, dimensions and clearance measurements.

Freeze shared markers only after clearances are measured. If the engine cannot
fit the centred tank/footplate, propose the smallest local cutout/cover change
and show it; do not move the cab/boiler, widen the whole vehicle or shrink gauge.
Do not claim a physical driving test from this authoring scene. Return absolute
paths and a self-contained handover for the three model sessions.
