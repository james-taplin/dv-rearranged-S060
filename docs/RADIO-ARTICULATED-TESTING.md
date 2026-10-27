# S060 radio 0.20.3 transition/effects candidate test

## 0.20.3 Shay gear check

Drive slowly: the pinions, line shafts, universal joints and bevel gear rings
must turn with the crank and wheels, in both directions and on curves.

## 0.20.2 Shay checks

Check the cab steps and the body around the right-hand engine are whole, and
that you cannot see through under the boiler or see the firebox fire from the
side. The engine, cylinders and truck frames should look dark gunmetal, not
the green S060 texture. Then drive the Shay with the regulator open for about
30 seconds; the diagnostics trace now records the gear clock for the gears and
U-joints report.

## Shay acceptance

Stop and close the regulator. Select Two-truck Shay in WHEEL REARRANGER or UMM.
Check both sides: four driven axles, unchanged gauge and no native side rods.
Drive slowly forward/reverse and through points/curves. The crank, sliding
shafts, joints, gears and wheels must move together, with both trucks following
the track. Check all three cylinders' jets while hot, and wet drips when cold;
cycle the cab lever repeatedly. Oil both cups on each axle and the engine cups.
Part-drain oil, leave different lids open, incur normal damage and save/reload.
Switch Shay -> stock -> Dockside -> Meyer -> Shay while stopped, observing the
usual rear-clearance requirements for Meyer. Identity/resources/damage and oil
quantities must persist, with no leftover meshes, cups, sound or particle jets.
Reload a pre-0.20 save too. This is a new arrangement awaiting gameplay acceptance.

Required packs: Meyer **0.7.0**, Franklin Dockside **0.7.0**. See the installation
receipt for actual installed hashes and installation mode. Restart/reload after
installation. The installer creates no backup, as requested; its receipt records
that mode and prior file hashes.
This build requires 0.7.0 packs. Their hash-bound validation receipts must match
the packaged bundle bytes; unchanged packs can reuse matching evidence. Version
numbers alone are insufficient.

## Quartering and oil-save acceptance

Cycle Stock -> Meyer -> Dockside -> Stock, then repeat with Short Wheelbase,
0-8-0 and 0-10-0. Observe both sides while moving slowly forward and backward:
left/right cranks must remain 90 degrees apart on return to native gear. Stop,
close the regulator and leave rear clearance before each switch. Repeat after
save/reload. Confirm existing oil levels/lids survive and the fifth-axle cups
retain their own state. Native six-point and earlier twelve-point oil saves
migrate to fourteen circuits without changing existing quantities or lids.
Unknown arrays are preserved as supplied to the native loader, not repaired.

## 0-10-0 Tank acceptance

Select 0-10-0 Tank on a stopped S060 with regulator closed. Inspect all five
driving axles, coupling rods, full-size cylinders, connected pipes and the cab
entry. Confirm ten big-end oil cups work, including the fifth axle. Test slow
forward/reverse travel and tight curves, slip/braking, switching through stock,
0-8-0, Meyer and Dockside, and save/reload. Existing saved fuel/water, condition,
identity, oil quantities/lids and old arrangements must be preserved.
The new axle uses a fifth wheel-condition slot and two appended oil circuits.

## Latest regression focus

Record the locomotive's existing condition and window state before testing; an
already damaged engine is not expected to repair itself. Stop, close the
regulator and apply brakes. Before entering/leaving Meyer, uncouple its rear,
disconnect the hose and leave clear track behind.

The rear shell's colliders now follow its existing native bogie Rigidbody, so
articulation no longer moves collider copies inside the main car's automatic
mass frame. Real rear impacts still use native collision callbacks. This fixes
an ownership defect, but isolated tests do not establish the exact cause or
resolution of the reported window damage/body bump. Do not dismiss a repeat as
normal game behaviour; retain its new transition trace.

- Switch Stock -> Meyer -> Dockside -> Stock, waiting at least three seconds
  after each selection. Watch the body, wheels and cab while stopped. There
  should be no conversion-induced body jump, new broken windows or condition
  loss. Record the exact transition if any changes. Repeat from a loaded Meyer
  save and on a gentle curve; a straight-track pass alone is insufficient.
- On a warm Meyer, allow the LP receiver/stage to build pressure and repeat
  cock cycles, both stopped and moving slowly. In the preceding capture at
  7.14% regulator, LP cock flow remained below the native 0.01 emission threshold
  and its chest stayed near atmospheric pressure. That capture did not establish
  a failed rear effect. Assess pressure/flow and each unit's inlet state alongside
  what is visible; a stopped crank need not emit from every outlet.
- Check water drips when cylinder water is actually present, then close/open
  the cocks several times. Native drips require both water and the cab control;
  a hot, dry engine may produce none. Repeat on both Dockside units and check
  that inlet changes at a fixed crank refresh the correct steam outlets.

Automatic diagnostics remain bounded: each changed articulated arrangement gets
a fresh file, at two samples per second for the first three seconds even with
the regulator closed, then while the regulator is open, up to 120 samples or
180 seconds from its first sample. Stock ends the articulated session. Repeating
the same selection does not extend the budget. Keep the files for both modes
and Player.log after a failed transition. They now include cylinder water and
temperature, body health/windows, native forward speed, body/bogie Rigidbody
pose/velocity, joint anchors/forces and stress. The immediate selection record
is taken inside conversion, so its geometry may still be in progress; subsequent
settling samples show the later state. Sampling can miss a brief force peak and
does not replace native physics reproduction.

## Meyer LP exhaust-return focus

Meyer now has a dedicated visible pipe from the rear LP cylinder collector back
to the smokebox. This is exhaust return, separate from the existing admission/
interstage pipework. The body route rises above the cab roof edge, with brackets
on the roof edge and forward footplate; it must leave the cab entry/steps usable.
Period-style ball/slip joints allow the rear section to move relative to the
body. It is a fictional engineering layout,
not a reconstruction of a specific historical locomotive.

- Select Meyer while stopped, uncoupled at the rear and clear of the vehicle
  behind. Inspect both LP cylinder connections and the collector, the moving
  joint/span, supported body run and smokebox entry. The designed cylinder/
  valve-chest exhaust takeoffs and smokebox entries should meet their fittings;
  other parts must not float or pass accidentally through the cab/firebox,
  tank, existing admission pipe or running gear.
- Drive slowly through left and right curves, then reverse over the same route.
  Check joint collars remain connected, the sliding part stays engaged and the
  return clears rods, oil cups, steps, deck and track. Inspect from both sides;
  a straight-track view alone does not establish articulated clearance.
- Confirm exhaust still emerges only from the locomotive's **single chimney**,
  with Meyer LP timing. There must be no added rear stack or exhaust plume.
  Both units' cylinder cocks remain independent outlets controlled from the cab.
- Save/reload Meyer and check the return remains attached. Restore Stock or
  another layout while stopped/uncoupled: the return, collector, joints and
  brackets must disappear with the rear section. Repeat several times and check
  Player.log for cleanup, material or mesh errors.

The new return is visual geometry. Compound stages, receiver conservation,
boiler draw, blast/chuff routing, fuel/water, damage and save schema remain
unchanged. It uses live S060 materials. Exact route/clearance and mesh-lifetime
results belong to the frozen candidate's receipts; the preceding fit tests do
not prove this new geometry.

## Effects and motion focus

This revision uses Verde's native effect ownership and motion patterns as a
reference. No Verde meshes, textures or audio are copied. Compound simulation,
tuning and save IDs/schema remain unchanged.

- Inspect each cylinder's front and rear cock outlets, water drips and sound
  position, on both sides of both engine units. They should sit on the measured
  lower cylinder-cap locations and stay attached through articulation, reverse
  travel and save/reload. Check Meyer's native front castings separately from
  its custom rear set and both custom Dockside sets.
- Stop a hot engine while residual steam is available. Keep its wheel pose
  fixed, cycle the cocks and move the reverser. Changing flow must not make the
  emitting end jump as if the wheels turned; changing direction should exchange
  front/rear activity at that same physical pose. Not every outlet must emit at
  every stopped crank position: the owning cylinder's inlet state still gates it.
  Repeat while moving slowly forward and backward, then close the cocks.
- Watch rods and Franklin shafts through a complete forward/reverse revolution
  and across the crank wrap. Each unit must follow its own crank without a jump
  or a free-running visual clock. These are timing/continuity checks; this build
  does not add a Franklin cutoff actuator animation.

The production radio path's owned readers normalize event values to the
reader's actual crank phase and refresh after inlet-mask publication. The standalone CCL debug presentation is unchanged
in this step. Native activity curves, inlet masks, gains and steam flow remain.
Drips sit at the rear drain; each cylinder's shared hiss source sits between its
front and rear drains.

The reopened `steam-effects-validation.json` checks stage-coherent bindings,
particle/audio aliases and measured outlet fit through ±15° bogie yaw and a
73° body orientation. Motion reports include seek/order/wrap and unit-isolation
assertions. Native timing tests first reproduce the callback-value bug, then
exercise fixed-crank flow/reverser changes and both teardown orders. Their final
candidate results/counts are recorded in the receipts/handover; source changes
alone do not establish a passing integrated run or sound balance.

## Cleanup regression focus

This candidate reorganizes existing responsibilities; saved circuit/locomotive
IDs, save versions, water/fuel quantities, damage and calibration precision must
remain unchanged. No new arrangement is introduced.

- Repeat servicing after streaming and radio changes: native cup construction
  is now separate from mount geometry and synchronous circuit topology. Check
  lids, indicators, oil-can refill and sounds on both sides/rear units.
- Load saves created by 0.14.0/0.15.0 and earlier supported versions. Switch among
  Stock, Meyer and Dockside, then reload. Retained dormant oil/cylinder/receiver
  state must survive the cleanup.
- If assets are unavailable, preflight should refuse the switch before removing
  the previous layout. If cleanup itself fails, look for the explicit cleanup
  state; selecting a layout again while stopped retries cleanup first. Do not
  assume any reported failure means all geometry/physics was restored; retain
  the log.

The earlier audit-fix acceptance below still applies. Current receipts identify
which controlled failure cases were exercised and what remains a gameplay check.

## Audit-fix acceptance

Test first load as well as a radio change. The earlier isolated exhaust audit
initialized readers in a convenient order and missed the real startup defect.

1. Save an S060 as Meyer, reload, then open the regulator and move slowly.
   Its chimney and regular chuffs should follow LP exhaust immediately, without
   switching layouts first. Repeat a first-load Dockside: both engine units
   should contribute pulses/chuffs to the single chimney.
2. Cycle the cab cocks several times hot and cold in both layouts. Listen from
   the cab and beside both units for duplicated or stuck stock hissing. Walk
   away/back to stream pooled audio and repeat. Return to stock and confirm its
   original cock/chuff sounds and smoke return. In Short Wheelbase/0-8-0, verify
   moved cylinder sounds come from the cylinders; check existing crack sounds
   too if the test locomotive already has cylinder damage.
3. Use the oil can, lid controls and indicators on every driven axle, including
   the rear units. Switch 0-8-0 -> Stock -> 0-8-0 and save/reload. Extra points'
   oil quantities/lids must survive inactivity; changing layouts must not refill
   them. Inspect Player.log for servicing-unavailable errors after streaming.
4. Drive Meyer slowly through curves in both directions and inspect the rear
   deck/pannier collision against its turning visible shell. Check after rerail
   and save/reload. Switch away while uncoupled and confirm the extension's
   collision disappears with its geometry.
5. On test locomotives, repeat conversion/deletion and normal pool/stream reuse.
   Check for exceptions or retained cups, loops, lamps and rear collision.
   Automated direct-destruction checks cover owned meshes; a longer gameplay
   session is still needed to judge memory/performance.

No force/grip tuning, resource capacity or save-version change is intended.
Retain logs and automatic diagnostics for failures, recording the layout and
whether it occurred on first load, after switching or after streaming.

## Exhaust correction to check

- Meyer: chimney pulses and regular chuffs follow the final LP stage, not the
  front HP stage/receiver. LP sound pressure bands use Verde's 3/4.5 bar example.
  Both units retain their own cylinder-cock sounds and particles.
- Dockside: both simple-expansion units have native chuff readers and can pulse
  the same chimney independently. A cold unit must not borrow the other unit's
  pressure to create pulses. Shared plume strength uses the larger final exhaust
  pressure, not a sum of absolute pressures.
- Switch Stock -> Meyer -> Dockside -> Stock several times hot and cold. Listen
  for duplicate/stuck loops; watch smoke stop normally as exhaust dies away.
  Walk away/back to stream audio, then save/reload and repeat. Stock must regain
  its original sounds, pressure bands and smoke behaviour.

The revised native audit specifically initializes smoke before owner registration
and exercises detached, pooled TrainAudio. Exact current-DLL evidence is required
by packaging. These controlled fixtures do not establish audible balance or the
complete in-game plume appearance.

Verde reference credit: **discordraconequus**, with the author's invitation to use the
engine as a compound template. No Verde meshes/textures/audio are included.

## Arrangement and previous polish checks

The normal radio/UMM controls now offer fitted arrangements, with no wheel-size
slider or granular resizing. Old size settings remain readable for compatibility;
old resized geometry migrates to the arrangement's fitted size on load.

- **0-6-0 (Stock):** original 1.42 m drivers and geometry.
- **0-6-0 Short Wheelbase:** 1.14 m drivers and shorter axle pitch, the same
  driver size/pitch as the 0-8-0.
- **0-4-0:** stock-sized drivers; **0-8-0:** four fitted 1.14 m drivers, rear axle
  at the stock rear-driver station. **0-6-2:** stock drivers and native pony axle.
- **Meyer:** one S060 with two axles beneath the body and two beneath the attached
  rear pannier section; serial HP -> receiver -> LP compound.
- **Franklin Dockside:** two simple-expansion engine units entirely under the
  tank body, with its authored 1.00 m wheels. No rear section.

The original native S060 ID/GUID, livery, cab, fuel/water, servicing, damage and
save ownership remain. Stock cylinders keep their original complete dimensions
and move with the fitted gear. Their steam, drip, crack and cock-audio outlets
use the same translation; upper steam branches bend between the fixed boiler
and moving cylinder. Width/gauge and left/right mirroring stay native.

Both articulated packs now reuse the S282 branch-pipe prefab's front/rear intake
manifolds and connecting dry pipe. The pipe span follows both engine units;
its bore/end collars retain their size while the centre changes length. The
front manifold joins the fixed boiler. These visuals do not change the existing
compound/simplex circuit or cab cylinder-cock control.

Meyer's rear unit adds riveted frame and tank details, axleboxes/oil caps, spring
hangers, brake rigging, tank straps, access steps and handrails. Its surfaces
alias live S060 materials with appropriate atlas patches. Added static details
are combined into one mesh. Panniers remain visual; water capacity is unchanged.

## Focused test

The hot 0.12 game test showed both cock flows working, normal native traction
binding and no rolling-audio exception flood. Its Meyer saved-layout restore
failed because support preparation relocated the stock-axle lookup pivots; this
revision uses their captured original support poses for identification.

This revision focuses on mesh fit and servicing:

- Dockside: actual pipe socket faces meet the retained boiler neck and both
  cylinder sets throughout articulation. Check near/far and left/right views.
- Meyer front: complete original S060 cylinder castings, full size, moved with
  the front power frame; connected boiler feed and cock outlets.
- Meyer rear: sloped bunker/equipment top, connected deck/steps/frame fittings,
  visible articulation gap and one native set of rear buffers. The two lower
  rear lamp housings, glass and controlled effects move onto the rear section;
  the upper cab lamp stays in place. Check switches, curves and restoration.
- Every driven axle has two functional big-end oil cups, rear sets included,
  plus the four original guide/crosshead lubricators. Cups must sit on supported
  mounts and clear rods/castings through a complete revolution. Try the lids,
  oil can, oil indicators and save/reload. Switching layouts must retain oil
  quantities and lid states; dormant axle cups must not consume or damage gear.

No tractive-force or grip calibration changes. The hot captures reached about
89 kN Meyer / 66 kN Dockside against about 129 kN grip, so their recorded lack
of slip was expected. Automatic bounded traces remain available.

1. Record the native S060 ID, paint, fuel/water, pressure and damage. Select
   **0-6-0 Short Wheelbase**. Confirm clearly smaller 1.14 m wheels, full-size
   cylinders, rod/gland alignment, attached upper steam pipes, and steam/drip/
   sound at the moved cocks. Restore **0-6-0 (Stock)** and check everything returns.
2. Select **0-8-0**. Its wheels should match Short Wheelbase and be larger than
   the previous 1 m version. Check the fourth wheel stays forward of the cab,
   both rail contacts, full-size cylinder/front-buffer clearance, rods and steam
   outlets. Drive slowly forward/reverse; then restore stock.
3. Select **Franklin Dockside**. Check both manifolds and connecting pipes,
   round cylinders, thin piston rods and animated Franklin shafts. Open/close
   the cab cocks: both units vent/stop when their native pressure/valve gates
   permit; check water drips only with cylinder water present. Drive through a gentle curve and watch
   the pipes stay attached to both units. Keep all gear beneath the tank body.
4. Stop, close regulator, uncouple the rear and disconnect its hose, leaving
   clear straight track behind. Select **Meyer**. Check the detailed rear unit,
   green panniers, frame fittings, same ID/paint, front HP/rear LP cocks and
   connected steam pipes through a curve. LP can lag while its receiver charges.
5. Save/reload Short Wheelbase, 0-8-0 and Meyer; also test a rear-coupled Meyer
   save. Confirm the layout, rear coupling location, resources and damage stay.
   Stop/uncouple/disconnect before changing out of Meyer. Its rear section and
   pipes must disappear; stock rear controls, supports and car length return.

Switching must not refill fuel, water or steam, repair damage or replace the car.
Stored compound/cylinder and four-slot wheel-condition state survives dormant
arrangements. Rear coupling controls remain native. UMM randomization defaults
off; saved locomotives keep their arrangement, with legacy sizes normalized.
Optional CCL spawner entries remain independent of radio use.

Validation evidence: native oiling development tests pass **527 assertions**,
including immediate 0-8-0 -> Stock masking without a visual cup owner, deferred
and failed-build windows, dormant state preservation and native reload. Added
checks use actual native refill-reader, indicator and lid-feeder fields for all
six extra circuits, retaining source definition IDs and saved quantities/lids.
Native engine development tests pass **241 assertions**, including compatibility
with the previous version-1 serializer, malformed-cylinder rejection and
unchanged saved data/precision under the new unit-role mapping. Development
Meyer tests pass **18 assertions** and the **25-assertion** physics suite now uses
the production calibration. The final candidate receipt records the frozen
source's integrated reruns and asset export validation.
Conversion-boundary development tests pass **35 assertions across 14 scenarios**,
linking the production conversion/selection code with scene/native discovery
test doubles. They cover preflight and retained retryable cleanup ownership;
they do not prove arbitrary native physics or destruction recovery.
These totals include repeated iterations; they are not independent game scenes.

The final runtime/asset receipts and handover record the complete candidate's
fresh runs and exact hashes. Previous 0.13.x–0.15.0 report totals are historical and
cannot establish the new candidate's status. Local native body/wheel/atlas
previews are transient QA inputs and are not packaged. Gameplay, sound balance,
oil-can/lid interaction, lamps, collision and coupled-save acceptance still need
the above game test.
