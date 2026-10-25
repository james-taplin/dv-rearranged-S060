# Session 4 prompt: build Shay truck, wheel and mounting visuals

Create original two-axle Shay truck visuals around reused DV wheelsets, using
the fit session's approved native-bogie attachment datums. Read:

- `B:/GitHub repo/rearranged-s060/docs/shay/README.md`
- `B:/LLW CONVERT/locos/s060-shay/shared/FIT-CONTRACT.md` and `fit-contract.json`.

Work only under `B:/LLW CONVERT/locos/s060-shay/trucks/`. No edits to the live
S060 checkout/project, shared contract, other sessions or installed mods. No
runtime feature, new physics bodies, install, push or publication. If the fit
contract is missing, return a labelled visual study instead of final dimensions.

## Model scope

Produce a coherent compact two-axle frame with bolster/pivot seat, side frames,
axleboxes, springs, transoms, brake beams/shoes/linkages, bearing caps, mounting
brackets and small supported lubrication points. Use one modular construction
for front/rear variants where possible. Avoid two flat planks over disconnected
wheels: every bracket and casting must have a believable load/support path.

Keep axlebox/frame parts truck-local and the fixed body attachment body-local.
Do not bridge the swivel gap with a rigid decorative beam. Use the existing
0-8-0's wheel inventory/dimensions as the starting reference, but the fit contract
controls final pivot/axle stations; the current rigid 0-8-0's centres are not proof
of symmetric Shay trucks. Do not relocate native pivots inside this model session.

James's explicit preference is to reuse DV wheelsets as much as possible.
Use the native S060 wheel/axle inventory through live mesh/material references;
do not rebuild treads, flanges or spokes for historical purity. Make small
original hub covers/adapters where they improve the fitted appearance, keeping
native bosses/counterweights if they cannot be covered cleanly. Do not add rods
to fake the geared drive. Hide separately rendered unused native linkage only
through the later conversion's reversible ownership path; do not destructively
edit native wheel assets or distribute extracted/cut copies.

The first fit uses 0.5697024 m rolling radius. Retain approved tread/flange/gauge,
existing native mirror convention and unit scale on NEW cosmetic parts. Never
scale X to fit the transmission. A replacement wheel is a fallback only after a
measured obstruction and review, not part of the default task. Transmission
session owns all bevel gears/pinions: supply gear-seat markers, not duplicate gears.

Expose `TRUCK_PIVOT`, two wheel/axle spin axes (+X), two right-hand gear-seat axes,
line-shaft bearing seats and brake-clearance volumes. Provide two supported
axlebox oil-cup mounting markers per driven axle (eight total across both trucks).
They must remain accessible and follow the axlebox/truck properly. Decorative
cups are not functional servicing; mapping to existing native circuits belongs
to integration, preserving all saved point IDs and quantities.

Test both complete trucks against native body/tank/cab steps, rail/flanges, the
transmission corridor and brake travel through the contract's articulation range.
Return measured clearances and any unsupported attachment, not merely a neat
straight-track screenshot. No colliders/Rigidbodies/ConfigurableJoints on these
cosmetic parts unless an integration owner has established a native requirement.

## Deliverables

Editable original modelling source plus Unity-2019-compatible meshes and modular
prefabs; native-wheel alias/attachment definitions, separate brakes/axleboxes
where motion needs it; consistent units,
unit export scale, logical live S060 material slots, UVs/normals, LOD0/LOD1 and
reported triangle counts (target <=12k LOD0 triangles for both frames and covers;
reused native wheel counts reported separately). Avoid distributing game reference exports, textures or
unlicensed parts. Include marker/axis JSON, the contract revision/hash, fitted
orthographic/articulated views and an integration handover with absolute paths.

Optional local footplate edge plates/covers may be modelled only if the fit
contract already identifies their boundaries. Do not redesign the S060 tank,
offset its boiler, obscure its cab entry or change the cab in this session.
