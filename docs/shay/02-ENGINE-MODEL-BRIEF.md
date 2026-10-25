# Session 2 prompt: build the Shay side-engine module

Build an original, mechanically coherent three-cylinder vertical simple-expansion
Shay engine for the fictional S060 conversion. First read:

- `B:/GitHub repo/rearranged-s060/docs/shay/README.md`
- `B:/LLW CONVERT/locos/s060-shay/shared/FIT-CONTRACT.md` and `fit-contract.json`.

If the shared contract is missing, produce a clearly marked engine-only design
study and a list of required datums; do not invent final mounting dimensions or
claim it will attach correctly. The fit session must finish before final export.

Work under `B:/LLW CONVERT/locos/s060-shay/engine/` in your own authoring project.
Do not modify the live S060 source/project, shared contract, installed mods or
the transmission/truck sessions' files. No runtime implementation/install/push.

## Geometry and rig

Use the RIGHT-side engine envelope and marker axes from the shared contract.
Three vertical cylinders sit above a longitudinal (+Z) crankshaft. Model castings,
bolted cylinder covers, valve chests, glands, piston rods, crossheads/guides,
connecting rods, three crank throws, bearing pedestals, mounting feet and sensible
local guards. Crank phases are 0/120/240 degrees; this is not two-cylinder
90-degree quartering and is not a compound HP/LP engine.

Keep all moving pieces separate with pivots on the true mechanical axes. Provide
a deterministic preview driven by one `engineTurns` input, working in both
directions and across the loop seam. The crank throws, rod ends and crossheads
must meet over a complete revolution; do not stretch closed castings or make
rods float. Valve motion can be a clearly labelled visual approximation until
the integration session defines cutoff linkage. Do not advertise simulated valve
events from an animated decorative linkage.

Expose named empties `ENGINE_MOUNT`, `ENGINE_CRANK_FRONT`, `ENGINE_CRANK_REAR`,
`STEAM_IN`, `EXHAUST_OUT`, `COCK_C1_FRONT/REAR`, `COCK_C2_FRONT/REAR`,
`COCK_C3_FRONT/REAR` and engine lubrication markers. The six drain locations must
sit on the lower appropriate cylinder-end faces and point clear of the mechanism.
They are attachment markers; native particle/audio/controller binding belongs
to later integration. Return the required pipe interface diameters and normals.

## Materials and deliverables

Use original mesh geometry, straightforward UVs, smooth/flat normals where
appropriate and logical slots `S060_PAINT`, `S060_DARK_STEEL`, `S060_MACHINED`,
`S060_PIPE`. Follow the contract's live material sources; simple local preview
materials are acceptable, extracted game texture copies are not. Keep fasteners
and piping sensible for the viewing distance. Aim for roughly <=15k LOD0 triangles
for this module, with a simpler LOD1; report counts rather than hiding overages.

Deliver editable source (`.blend` if Blender is used, or reproducible Unity mesh
generation source), Unity-2019-compatible importable meshes/prefab with positive
unit scale, original material-slot definitions, rig/marker JSON, and the shared
contract revision/hash used. Include a full-turn forward/reverse preview, an
endpoint-error report and views of the actual fitting scene. No baked whole-loco
mesh, flattened rig, game DLL, extracted game asset or unlicensed third-party part.

Use historical references linked in the Shay README for construction. This is a
fictional S060-sized module, not a tracing/copy of someone else's model. Return
absolute paths and a short integration handover. Do not build a custom steam
solver: B99 already has a three-cylinder native engine path, still to be wired.
