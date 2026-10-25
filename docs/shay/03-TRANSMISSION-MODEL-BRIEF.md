# Session 3 prompt: build articulated Shay shafts and bevel gears

Build an original, rigged transmission kit for the two-truck S060 Shay. Read
`B:/GitHub repo/rearranged-s060/docs/shay/README.md` and the completed contract at
`B:/LLW CONVERT/locos/s060-shay/shared/FIT-CONTRACT.md` / `fit-contract.json`.
Work only in `B:/LLW CONVERT/locos/s060-shay/transmission/`. Do not change shared
datums or another model session's files; do not edit/install the live S060 runtime.
If the contract is absent, stop final fitting/export and return a labelled study.

## Build these independent parts

1. Engine-output collars/coupling flanges at the supplied front/rear crank datums.
2. Front and rear drive runs, each with universal-joint yokes/crosses and a sliding
   square shaft/sleeve. The sleeve must have actual insertion length and clearance,
   not two ends of a rod that pull apart on curves.
3. A longitudinal outside-RIGHT line shaft and bearing supports for each truck.
4. Four bevel pinions and four matching outside-right wheel ring gears, one pair
   per axle, with proper pitch-cone/apex/shaft-axis geometry and correct handedness.
5. Small guards/support brackets where required without hiding the mechanism.

James wants maximum DV wheelset reuse. The four wheel ring gears are original
bolt-on additions to the approved native wheel meshes, with hub adapters/covers
where needed. Fit their mounting clearances to the native wheel faces and crank
bosses; do not supply replacement wheel treads, spokes, flanges or axles and do
not stretch the wheel laterally. Return any unavoidable obstruction to the fit
owner with dimensions before proposing a wheel change.

A provisional 50:20 wheel-gear/pinion pair gives G=2.5. Confirm the contract before
using it; this ratio is a fictional design choice, not a historical measurement.
Do not model spur gears at 90 degrees and call them bevel gears. Tooth count,
pitch surfaces, cone angle, axle-to-line-shaft offset, backlash/visual clearance
and the resulting signs/phase need one consistent solution across all four axles.
Explain any geometry simplification. Do not silently change wheel size or shaft
line to make your parts fit independently of the other sessions.

Keep shafts, yokes, crosses, sleeves, bearings, pinions and wheel gears separate.
Each spins about its true local axis with positive exported scale. Wheel gears
belong to the truck/wheel frames, while engine outlets belong to the body frame.
Provide a preview driven by engineTurns and truck yaw/pitch/roll plus displacement,
not independent unsynchronised looping clips. Joint bends and slip length must
respond to those transforms; no runtime physics joints/Rigidbodies are required
for these visual parts.

Show straight track and the full articulation envelope defined by the contract,
forward/reverse and the loop seam. Report engagement, joint angle, mesh contact
and minimum clearances in metres. Include the kinematic relation G*wheelTurns
to engineTurns and signs. Universal-joint angular behaviour/paired-yoke phasing
must be stated honestly; a shaft just aimed between endpoints is not proof that
gear teeth stay phased throughout articulation. Return any unresolved constraint
to the fit owner rather than conceal it with clipping.

## Export and handover

Deliver editable original source, Unity-2019-compatible meshes/prefab, named
joint/gear axes, attachment markers matching the contract, measured pitch radii,
tooth counts, ratio and a readable kinematic/marker JSON. Supply sensible LODs
(target <=15k triangles for the complete LOD0 kit), UVs/normals and logical live
S060 material slots. Own these meshes; do not include copied game/third-party
geometry or textures. Use a separate preview project, never the shared live
S060 authoring project.

The integration owner will bind this kit to native bogies and a geared native
steam graph. Do not create a second force model, fake extra power, oil-save IDs,
an installer or a standalone powered vehicle. Return absolute paths, contract
revision/hash and a concise mechanical/animation handover with evidence.
