Current runtime: 0.22.4 uses Shay R2 with live S282 pony wheelsets (about 0.71 m rolling diameter), 43:19 gearing and six upright servicing cups. The three-cylinder native graph and transmission are implemented. See ../../README.md and ../RADIO-ARTICULATED-TESTING.md for current behavior and James's acceptance checks. The original planning/R1 material below is historical.

# S060 Shay feasibility and session briefs

**27 October integration update:** the four R1 module sessions have been joined
into the 0.20.0 candidate. See [integration decisions and validation](INTEGRATION.md).
The feasibility notes below document the earlier planning baseline.

Prepared for James on 25 October 2026. This is design preparation only. No Shay
arrangement, model, physics or save entry is included in the 0.19.1 release.

**Feasible:** reuse the 0-8-0's four-powered-axle inventory, fixed wheel size,
native S060 identity/cab/resources and conversion/save infrastructure. A complete
Shay needs more than a side-engine mesh: the visible wheels must belong to two
swivelling trucks and drive through bevel gears, jointed shafts and a reduction.

Recommended first design: a fictional compact **two-truck, three-cylinder,
simple-expansion S060 Shay**. Keep the S060 tank/cab and one chimney; no attached
Meyer extension or separate vehicle. Model the engine on the locomotive's right.
Use the current 0-8-0's 1.1394 m wheel diameter for the first fitting study. That
is larger than the 32-inch wheels on IRM's J. Neils 5, so this is a DV conversion,
not a replica. Do not promise historical proportions before the fitting study.

James's explicit priority: **reuse DV wheelsets as much as possible**. Native
S060 wheels/axles and live aliases are the default deliverable, not disposable
placeholders. Build original bolt-on gear rings, hub covers and truck parts
around them. Do not commission replacement tread/flange/spoke meshes unless a
measured, unresolved obstruction requires it. Retain stock lateral gauge; small
radial scaling follows the existing fitted layout. S282 drivers are not a free
smaller alternative: the earlier native audit measured the same mesh diameter.

## Start these sessions in order

1. [Fit and mounting contract](01-FIT-CONTRACT-BRIEF.md) must finish first.
2. Then [side engine](02-ENGINE-MODEL-BRIEF.md),
   [shafts and gears](03-TRANSMISSION-MODEL-BRIEF.md) and
   [trucks and wheels](04-TRUCK-MODEL-BRIEF.md) can run independently against
   that same approved contract. Each file is a ready-to-paste session prompt.

Use a private authoring workspace under `B:/LLW CONVERT/locos/s060-shay/`.
Do not let separate sessions edit the live S060 checkout, its ignored Unity
project, installed mods or another session's files. No automatic install/push.
Return editable model sources, importable meshes, marker/axis metadata and
measured fit/motion evidence. A rendered image alone is not a deliverable.

## What is confirmed from our code and B99

- `S060EightCoupled.BuildGeometry` registers four PoweredWheel objects on two
  native bogies, 2+2. Its visible mechanisms remain beneath `LocoS060_Body`;
  they do not become articulated Shay trucks merely by adding shaft meshes.
- `S060EightLayout` and `S060DriverScale` fix radial scale at 0.80 and retain
  lateral gauge/mirroring. Stock rolling radius 0.712128 m gives 0.5697024 m
  fitted radius. Never scale the whole locomotive or its gauge to fit a model.
- Native B99 `ReciprocatingSteamEngineDefinition` exposes `numCylinders` and
  crank-RPM/torque ports. Inspection of the installed `DV.Simulation.dll`
  constructor confirms an explicit three-cylinder branch: piston lead 1/3
  turn and six chuffs per engine revolution. A bespoke steam solver is not
  justified. A geared three-cylinder S060 graph has not yet been built/tested.
- Existing Meyer/Dockside code already demonstrates per-engine native crank
  animation, aliased materials and visible frames following native bogies.
  Reuse those techniques, not the Meyer's compound cycle or rear extension.

## Existing wheel stations: reference, not a final truck design

Coordinates are metres in TrainCar local space: +X right, +Y up, +Z smokebox.
Actual native mesh origins from `build/qa/conventional-native.json`, transformed
by the current 0-8-0 scale/offset, give approximately:

| Axle, rear to front | Z | Current physical owner |
|---|---:|---|
| Added fourth driver | -1.39780 | Rear bogie |
| Native driver 1 | -0.16012 | Rear bogie |
| Native driver 2 | +1.07988 | Front bogie |
| Native driver 3 | +2.31988 | Front bogie |

Wheel mesh-origin Y is approximately 0.569704 m. Nominal pitch is 1.23944 m;
native mesh-origin rounding produces small differences. These values are
derived references, not a replacement for executing the current geometry.

Native support pivots were probed near Z -1.396 and +0.929 m. Those are not the
pair midpoints above (about -0.77896 and +1.69988 m). Therefore the fitting
session must establish a coherent truck/pivot/axle layout, rather than pretend
that the present conventional 0-8-0 already has symmetric Shay trucks. Prefer
existing native support/attachment methods; any revised axle stations remain
local to the future Shay arrangement, leaving the released 0-8-0 unchanged.

## Integration still required after the models

One reduction clock must connect engine RPM, shafts, bevel gears and wheel RPM.
A provisional 50:20 tooth pair gives ratio 2.5; this is a design proposal, not a
measured Lima specification. Keep it editable until pitch-cone geometry fits.
For ratio G, engineRPM = G * wheelRPM and ideal wheelTorque = G * engineTorque;
losses may reduce power, never create it. Keep native wheel/slip and brake RPM
separate from the faster engine/chuff clock. Derive signs from the actual axes.

Integrate three native cylinder channels and correctly placed cocks/drains,
exhaust back to the single chimney, native oiling/servicing, axle animation on
curves, collision/LOD ownership and reversible radio/save switching. Append any
new arrangement/save IDs; do not renumber old ones or change the current saves.
Verify existing native graph components/ports before adding a custom hook.

The biggest unmeasured fit risk is the right-side vertical engine against the
S060 tank, footplate and cab access. Retain the centred boiler/cab in the first
study. If a local tank/footplate cutout is needed, return a measured proposal;
do not silently offset the whole boiler or shrink the locomotive. A traditional
Shay's off-centre boiler is a reference, not an instruction to break the S060.

## References

- [Illinois Railway Museum, J. Neils Lumber 5](https://www.irm.org/player/jnl5/):
  owner/operator description of the vertical engine, shaft and bevel-wheel drive;
  also lists its 32-inch wheels. This is a three-truck example, not our dimensions.
- [ASME geared-locomotive landmark](https://www.asme.org/about-asme/engineering-history/landmarks/134-geared-locomotives):
  high-speed engines reduced to four-wheel trucks and a preserved two-truck Shay.
- [Accucraft three-cylinder Shay manual](https://www.accucraft.com/manuals/AT%2020p3%20Shay%20No%205%20Web%20Manual.pdf):
  manufacturer's working-model explanation of 120-degree cranks, universal and
  sliding square shafts. Useful motion reference, not full-size dimension data.
- [Accucraft two-cylinder Shay manual](https://www.accucraft.com/manuals/AT%2020p3%20Shay%202%20Cyl%20LS%20Manual.pdf):
  useful comparison of alternative cylinder counts and the offset boiler.

These references inform original modelling. Do not redistribute their photos,
drawings, models, audio or textures without permission.
