# Shay remodel handoff — 11 November 2026

## Outcome and current status

James selected **0.80 m wheels and approximately 150 kN nominal starting effort**.
The approved physics specification is three 252 x 300.808 mm cylinders, 45 L
combined steam chest, 1.6 kg/s flow ceiling, 86:20 bevel gears (43:10 reduced,
4.3:1), 92% transmission efficiency. Do not add boiler capacity or adhesion.

James approved the 1.6 kg/s tune on 11 November; this needs no model/gear change.
The native engine bench passes 2,365 checks. At 16 bar absolute/200 C/full gear,
mean pull at .1/1/5/10/15/20/30 km/h is 156.7/152.6/138.4/91.7/57.6/39.0/20.3 kN.
At 13 bar it is 124.6/121.8/113.6/91.7/57.6/39.0/20.3 kN. Normal cutoff control
improves 16-bar output to127.7 kN at10 km/h (70%reverser) and106.5 kN at15 km/h
(40%reverser). Dockside retains115.8 kN
at1 km/h and79.0 kN at15 km/h. These are fixed-supply engine results, not DV hill
runs. Native adhesion may limit usable pull before150 kN; do not fake extra mass.

Retain DV wheel geometry, native gauge, width, lateral placement and materials.
Scale radial dimensions about the axle equally perpendicular to the axle axis;
no whole-axle/locomotive X scaling. The source native rolling diameter is
.7105064 m, so the radial factor for .80 m is approximately1.12596. Runtime uses
.400 m rolling radius. Refit axle height, wheel/gear/rail/frame clearance, shaft
height and connected supports together. The original .71 m draft is reference
only and is not an accepted .80 m fit.

## Fixed requirements

- One vehicle and the existing S060 identity, engine slots, resources and saves.
- Native DV wheelset reuse; no redistributed native mesh/texture copies.
- Exactly 43 transmission motion bindings. This part count is unrelated to the
  old 43:19 gearing and does not change when teeth counts change.
- 86-tooth rings and 20-tooth pinions, ratio 4.3:1, with physically matching
  tooth pitch/cones, root/hub/shaft clearance and connected adapters/supports.
- Original engine-side shaft endpoints stay at rear `(1.10,.52,-.19)` and
  front drive `(1.10,.37,.42)` unless a revised fit is explicitly coordinated.
- No new runtime physics components, colliders or rigidbodies inside the bundle.

## Engine remodel and runtime ownership

The model session now owns a **new fully authored252 x300.808 mm engine**,
including larger castings and suitable cylinder/crank/rod spacing. The previous
236 mm outside jackets/240 mm pitch cannot fit this bore unchanged. Around
300–320 mm outside castings and340–360 mm pitch are plausible starting geometry,
not acceptance dimensions: prove actual walls, covers, rods and body fit.

Keep150 mm crank throws,900 mm rods and the120-degree cylinder phasing. The
piston-axis template is localX-.065; engine origin is currently(1.10,.52,.11).
Return the final cylinderPitch and valveAxisX and any changed engine/body/shaft
endpoint datums in the contract. The motion template retains valve centreY1.22,
15 mm valve travel and90-degree phase lead. If remodelled pivots need different
values, report them explicitly rather than silently compensating mesh positions.

**Export the new engine at its final dimensions with engineFitRevision:2.** The
old procedural fitter has been retired to archive/physics-first-fit; the runtime
will not apply it again. Preserve Engine/LOD*/CRANK, FRONT_REVERSED, ROD_1..3,
CROSSHEAD_1..3 and VALVE_VISUAL_ONLY_1..3 pivots, or provide an explicit binding
map. Castings, valve chests, pipes and frame supports must be connected and clear
of the full moving geometry on both LODs.

## Reference inputs and provisional coordinates

- Original R2 source: `B:/LLW CONVERT/locos/s060-shay/` (leave intact).
- Copied draft: `build/physics-20261111/regear-source/` in this repository.
- Draft preparation tool: `Shay/tools/Prepare.Regear.py`.
- Original procedural gear source: `transmission/source/build.py`.
- Current bundle builder: `Build.Shay.ps1`, `Shay/Authoring/ShayBundleBuild.cs`.
- Current persistent prefab: `Meyer/UnityProject/Assets/ShayIntegration/S060Shay.prefab`.
- Native mesh evidence: `build/qa/native-preview.json` and `s282-pony-native.json`.
- Revised engine proof: `build/physics-20261111/revised-shay43-10-engine-envelope.json`.

For the present 0.71 m draft, ring pitch radius is .300 m, pinion pitch radius
.06976744 m, ring plane X.900, common apex/line-shaft X.96976744. Ring radius
may remain useful with a larger wheel, but the whole fit must be checked again.
The runtime currently reflects these provisional gear X datums and .400 m
rolling radius; truck shaft/axle Y must follow the .400 m wheel centre. Integration must update every datum to the final accepted contract.

## Unaccepted draft findings — resolve before export

The copied recipe is a starting reference, not finished geometry. The first
draft check flagged 132 gear-pair contacts across four pairs/33 phases; their
cause was not yet isolated. Polygonal pinion-bore margin against a 100 mm shaft
was only .228 mm. Maximum articulation joint angles were 38.24 degrees rear /
20.88 front. Investigate flank/root/backlash, web/hub/shaft fit and U-joint fork
clearance; do not waive the failures or treat source bounds as full acceptance.

Retain useful existing native methods and fixtures; repair stale assumptions
before trusting old validators. Some external R2 validation still hardcodes
43/19, old tooth-pitch sampling and an obsolete scaled wheel fixture.

## Deliverable

Return versioned source/recipe/fit-contract, generated meshes, prefab and
motion JSON, plus a concise fit report. Preserve alias placeholders and the
43 motion paths or provide an explicit old-to-new binding map.

Motion JSON must declare `ringTeeth:86`, `pinionTeeth:20`, `engineFitRevision:2`, `rollingRadius:0.4`, `engineBore:0.252`, `engineStroke:0.30080814`, `cylinderPitch`, `valveAxisX`, `truckShaftY:0.4`, and the accepted
`truckShaftX`; current runtime rejects mismatching gear assets before conversion.
If a wheel-height or shaft-coordinate change is needed, include every final
datum in the fit contract for runtime integration, not only in a screenshot.

Validate gear pair contact/backlash through a tooth period; native-wheel, rail,
body and frame clearance; 49 articulation poses; telescopic engagement and joint
clearance; connected bearing/support meshes; both LODs. Test the 43-engine-turn
period returning to the same wheel/gear pose (10 wheel turns with this ratio).
No saves need a new gear-turn range. James owns actual DV gameplay testing.
