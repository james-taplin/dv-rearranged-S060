# Franklin geometry experiment — 21 October 2026

James reported that the working animated Franklin gear did not fit the small
S060 wheelsets convincingly, and asked to try scaled S282 wheels or correct the
geometry. This work keeps one Dockside locomotive with both units beneath the
tank and its fixed 1 m drivers. Wheel width and gauge remain unscaled.

## Native wheel comparison

The local B99 `resources.assets` meshes establish that the proposed replacement
uses essentially the same tread and crank dimensions:

| Native mesh | Vertices | Outer pin face X | Crank radius from cap bounds |
|---|---:|---:|---:|
| `s060_Wheels_02` | 1,307 | 0.987302 m | 0.335355 m |
| `s282_wheels_driving_1` | 1,751 | 1.012687 m | 0.335625 m |

Both have the same principal tread X rings and approximately 0.71745 m tread
radius. The S282 wheel changes counterweight/detail and extends its pin face
about 25 mm farther outboard. It does not move the axle centres or make the
Franklin shafts, bearings or rods fit. The authoring audit renders a temporary
S282 flanged-wheel substitution at the same radial scale (0.702120967), existing
absolute crank animation and unchanged X scale for comparison. Production keeps
the live S060 wheel aliases. The comparison is a development experiment, not a
wheel-selection feature.

`Dockside/tools/Prepare.WheelComparison.py` creates the ignored local QA fixture.
The fixture and transient meshes stay outside Unity Assets and the package. The
exported bundle audit prohibits native game wheel/body meshes and textures.
Comparison PNGs are explicitly named `dockside-s282-experiment-*`; these use the
S060 preview atlas to compare geometry, not S282 livery appearance.

## Fitted geometry

- Preserve the existing author-owned Franklin shaft hierarchy, five animated
  shaft/joint channels, native crank-port drive and 2:1 transmission motion.
- Fit the custom gear in X and Y at .70, with its existing longitudinal fit.
  This corrects previously flattened shaft sections and reduces the outboard
  envelope. It does not change native wheel width, gauge or cylinder dimensions.
- Add a transverse bearing sleeve between the axle and original transmission
  case, gearbox mounting feet, and rotary-valve drive housings/caps seated on the
  round cylinder chest. Previously the retained shaft assembly lost its support
  when the original oversized cuboid cylinder casting was removed.
- Replace rectangular coupling/main bars with author-owned narrow webs and
  bored circular eyes. Keep the measured crank-pin endpoint animation and
  physical lengths, and add open guide brackets with clearance for the crosshead
  and piston rod.

These are visual fit changes. Engine bore, stroke, driver diameter, axle pitch,
phase, locomotive identity and save behaviour are unchanged.

The reopened candidate's reused Franklin geometry has a measured static half
width of **1.37044 m**, reduced from **1.49705 m** in 0.4.0. This measurement is
for the custom Franklin source components, not the entire swivelling unit's
swept clearance; the latter still includes native wheels and frame parts.

## Validation boundary

The source compiles against the installed Unity 2019.4 SDK. Export evidence is
`build/dockside/authoring-20261021-194029`: reopened-bundle audits passed **19,066
motion assertions**, max crank/rod endpoint error **3.04e-7 m**, loop/reverse
seeks, quartering, bogie yaw, fixed cylinder axes, all five shaft channels, fitted
supports and shaped rods. Production pipe fitting passed **81 checks** across
five yaw angles. The bundle contains **zero native body/wheel meshes, textures
or audio**.

Normal and temporary S282 substitution previews were rendered from the reopened
bundle and visually compared. They support keeping the S060 wheels and fitting
the surrounding parts. Numerical endpoint agreement alone does not establish
appearance. In-game appearance remains a user test after the complete candidate
is installed.
