REARRANGED S060 - 0.3.2 VISUAL REPAIR CANDIDATE

Change the native S060's running gear through WHEEL REARRANGER on the comms
radio or the UMM arrangement chooser. Identity, cab, fuel, water, damage, oil
and save ownership remain with the same locomotive.

IN THIS BUILD

Meyer: the previous smooth upper LP steam return is restored. The rear chain
retains its relocated position when DV re-enables its visuals. The heavy pinned
drawbar and short drawgear remain beneath an open gap; no footplate or upper
bearer bridges the gap to the tank.
The new tank panels use clean weathered paint instead of projecting the stock
tank's foothold and handle shadows onto the rear bodywork.

Shay: custom metal finishes are assigned before activation and restored after
paint/material replacement, so the native body atlas cannot repeat across the
new parts' metric UVs. Native wheel UVs and materials remain separate.

Loose static expansion-link castings and mountings are retired on the short-
wheelbase, Dockside, 0-8-0 and 0-10-0 layouts. Returning to stock restores them.
Save identities, independent drive and compound calibration are unchanged.

Dockside and Meyer now have independently slipping powered groups, using DV's
native adhesion/slip calculations. Each group drives its own crank phase,
running gear and slip effects. Meyer retains its finite HP -> receiver -> LP
compound circuit, with separate HP/LP speeds and compatible old saves.
The shaft-linked Shay remains one mechanically connected drive.
Prepared for James's gameplay testing; this package has not been published.
Native braking/wheel-slide and global cab slip indications remain
car-wide. Uneven group demand can give less pull than the former pooled grip.

Combined Meyer visual checkpoint 8293d76, Shay checkpoint db91158, Dockside
checkpoint 1b39170 and the staged arrangement physics fixes.

Meyer: inward-facing HP/LP units, smoother connected steam return, supported
rear underframe, eight native heated brake shoes and eight supported visible
leaf packs. Compound starting follows native crank/valve mechanics while
retaining the conserved HP -> receiver -> LP circuit and single chimney.
The common cab control operates both cylinder-cock sets. Supply-mass changes
refresh native mass immediately during arrangement restoration/removal.

Shay: 0.80 m live native wheelsets, three 252 x 300.808 mm cylinders, 4.3:1
gearing, 45 L chest and 1.6 kg/s flow ceiling. Approximately 150 kN warm near-
start effort before grip limits. Shorten cutoff as speed rises. Coils, connected
brake rigging, chassis, fitted steam/exhaust piping and front compressor detail
are included. Boiler sustainability and haulage remain gameplay tests.

Dockside: one articulated 0-4-4-0T with two simple-expansion units, 1 m wheels,
Franklin bearing/rod/crosshead presentation, fitted smokebox sleeves and 4.70 m
driven wheelbase. Existing cylinder calibration and 2 kg/s combined flow remain.

ARRANGEMENTS

0-6-0 Stock; 0-4-0; 0-6-0 Short Wheelbase; 0-8-0; 0-10-0 Tank; 0-6-2;
Meyer-style 0-4+4-0T; Franklin Dockside 0-4-4-0T; two-truck Shay.

SAVES AND INSTALLATION

Existing arrangement, engine and oil IDs are retained. Optional Meyer trapped-
cylinder pressure fields preserve hot-save continuation; older saves still load.
Stop and close the regulator before changing layouts. Returning from Meyer
clamps water/coal above stock capacity according to the existing behavior.

One UMM package includes all model assets. Install Loco Mesh Splitter and Custom
Car Loader, including its required dependencies, separately. See INSTALL.txt.
No native game meshes or textures are redistributed in the Shay model bundle.

This is the integrated 0.3.0 line. The separate original-layout/six-axle Meyer
experiment has been reassigned to future 0.4.0 and is not included here.
Offline checks do not establish in-game appearance or haulage acceptance.
