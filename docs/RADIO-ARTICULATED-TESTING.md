# S060 radio 0.15.0 cleanup candidate test

Required packs: Meyer **0.6.0**, Franklin Dockside **0.6.0**. See the installation
receipt for actual installed hashes and installation mode. Restart/reload after
installation. The installer creates no backup, as requested; its receipt records
that mode and prior file hashes.
Pack versions remain 0.6.0. Fresh authoring exports and their exact bundle
hashes must be validated before packaging this candidate; matching version
numbers alone are insufficient.

## Cleanup regression focus

This candidate reorganizes existing responsibilities; saved circuit/locomotive
IDs, save versions, water/fuel quantities, damage and calibration precision must
remain unchanged. No new arrangement is introduced.

- Repeat servicing after streaming and radio changes: native cup construction
  is now separate from mount geometry and synchronous circuit topology. Check
  lids, indicators, oil-can refill and sounds on both sides/rear units.
- Load saves created by 0.14.0 and earlier supported versions. Switch among
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
   the cab cocks: both units vent/stop. Drive through a gentle curve and watch
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
Conversion-boundary development tests pass **33 assertions across 13 scenarios**,
linking the production conversion/selection code with scene/native discovery
test doubles. They cover preflight and retained retryable cleanup ownership;
they do not prove arbitrary native physics or destruction recovery.
These totals include repeated iterations; they are not independent game scenes.

The final runtime/asset receipts and handover record the complete candidate's
fresh runs and exact hashes. Previous 0.13.x/0.14.0 report totals are historical and
cannot establish the new candidate's status. Local native body/wheel/atlas
previews are transient QA inputs and are not packaged. Gameplay, sound balance,
oil-can/lid interaction, lamps, collision and coupled-save acceptance still need
the above game test.
