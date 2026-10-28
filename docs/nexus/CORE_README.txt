REARRANGED S060 - 0.22.0 PUBLIC BETA

Change the native S060's running gear through WHEEL REARRANGER on the comms
radio, or the mod's UMM arrangement chooser. All arrangements remain attached
to the same locomotive identity, cab and save ownership.

0.22.0 rebuilds the Meyer's rear as one long articulated tank engine: a high coal
hopper behind the cab, long side and well tanks in line with the S060's own, and a
proper rear end with all three rear lamps, carried on the locomotive body while the
rear engine unit swings underneath. The Meyer now holds about 8,800 L of water and
3,500 kg of coal.

0.21.0 rebuilt the Shay: smaller stock S282 pony wheels on lower trucks, full-size
shafts, universal joints, toothed bevel gears, rods and pistons, the engine moved
down and inboard, the S060's own frame and springs kept above the trucks, and six
upright oil cups.

0.20.3 made the Shay's pinions, line shafts, universal joints and bevel gears
turn with the engine; in 0.20.2 only the crank and wheels moved.

0.20.2 rebuilt the Shay's body cut-outs from whole parts, so the cab steps and
old reverser are no longer sliced and the boiler underside is closed, and gives
the Shay engine and frames a dirty gunmetal finish. It also adds a gear-clock
block to the driving trace.

0.20.1 fixed Shay selection with CCL's native water-drip aliases and prevents
duplicate Doppler/ECS initialization while assembling its audio. Existing
arrangements, oil circuit IDs and vehicle targeting retain their behaviour.

ARRANGEMENTS

  0-6-0 Stock - original 1.42 m drivers.
  0-4-0 - four-coupled layout with stock-sized drivers.
  0-6-0 Short Wheelbase - closer axles and fitted 1.14 m drivers.
  0-8-0 - four powered axles with fitted 1.14 m drivers.
  0-10-0 Tank - five powered axles with fitted 1.00 m drivers.
  0-6-2 - stock-sized drivers and a trailing carrying axle.
  Meyer-style 0-4+4-0T - compound locomotive with an attached pannier steam bogie.
  Franklin Dockside 0-4-4-0T - two simple-expansion units beneath one tank body.
  Two-truck Shay - three vertical cylinders, geared shafts and four driven axles.

SHAY

A fictional S060 conversion using the complete native DV wheelsets, fitted to
1.14 m diameter without changing the gauge. Two swivelling trucks carry eight
working axlebox oil cups. A vertical three-cylinder engine drives jointed,
sliding shafts and bevel gears with a 43:19 reduction. Steam and exhaust pipes
connect the side engine to the boiler and single chimney. The normal cab lever
controls all three cylinder-cock channels; drips require actual cylinder water.
Select it in the radio or UMM chooser. It is not in the random-spawn pool.
This first Shay version needs gameplay acceptance, especially on tight curves.

MEYER

Two driven axles remain beneath the S060 and two beneath the attached rear
pannier section. It is one locomotive, with no separate companion vehicle.
Steam passes through HP cylinders, a finite receiver and LP cylinders. LP
exhaust and chuffs feed one chimney. A jointed return pipe follows the cab
roofline to leave the entry clear. Panniers are visual and add no water capacity.
Front cylinders retain the full-sized native S060 castings. Two lower rear
lamps move with the rear section; the upper lamp stays on the cab.

DOCKSIDE

Two powered units, small 1.00 m drivers, animated Franklin gear and fitted rods,
cylinders and steam pipes stay beneath the locomotive. Both units receive
boiler steam directly and contribute exhaust to the single chimney. There is
no rear appendage or companion vehicle.

SHARED FEATURES

Both cylinder-cock sets use the normal cab control. Effects follow their own
engine's steam flow and inlet timing. LP jets may be weak at low receiver
pressure; water drips require actual cylinder water. Each driven axle has two
functional big-end oil cups, alongside the original guide/crosshead lubricators.
Oil quantities and lid states survive arrangement changes and save/load.
The 0-10-0 adds ten big-end cups; old oil and wheel-state slots retain their IDs.
New fifth-wheel state uses mod save version 3. This version reads earlier saves;
older mod versions cannot restore the new 0-10-0 arrangement.
The new Shay selection uses version 4; previous mod versions cannot restore it.
Original arrangement and oil circuit IDs retain their saved meanings.
Arrangements use fixed, fitted wheel sizes; there is no driver-size slider.

Switching preserves native identity, livery, fuel, water, damage and save state.
Stop and close the regulator first. Before entering/leaving Meyer, uncouple
the rear, disconnect the hose and leave clear track. Changing away removes
the rear assembly and restores the stock rear supports and coupling position.

INSTALLATION

Requires Derail Valley B99, UMM 0.27.3+, Loco Mesh Splitter and Custom Car Loader.
Drag RearrangedS060-0.22.0-UMM.zip directly into UMM. One mod folder contains
the runtime, both 0.7.0 asset bundles and original Shay models; there are no nested ZIPs. Earlier
three-part test installs must first uninstall the two old standalone asset
packs in UMM, keeping RearrangedS060 and Settings.xml. See INSTALL.txt.
Rearranged S282 is not a runtime dependency.
Close the game before installation or updates. No Settings.xml is supplied.

This is a public beta. The latest Meyer-to-Dockside damage and stopped
body-bob reports need gameplay confirmation after the collision correction.
Automatic diagnostics capture a bounded trace for each changed articulated
arrangement; report it with Player.log and the exact steps if a problem recurs.
The mod does not repair damage already present on your locomotive.

MIT fork of JimTaplin's Rearranged S282 B99/Re:Arranged fork, based on Peter
Schinske / SatiricRug's original Rearranged S282. Meyer sound tuning is informed
by discorddraconequus's Verde. No Verde audio or artwork is distributed.
See LICENSE and CREDITS.md for the retained notices and full credits.
