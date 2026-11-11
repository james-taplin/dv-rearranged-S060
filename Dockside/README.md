# Franklin Dockside 0-4-4-0T — 0.8.1 staged appearance pack

Staged with RearrangedS060 0.24.0 and Custom Car Loader; not installed. Select **Franklin Dockside**
from the native S060's WHEEL REARRANGER radio or UMM arrangement chooser.
Both engine units stay beneath one tank locomotive, with fixed 1 m drivers and
no rear appendage or companion vehicle. The native ID, cab, resources, damage
and save ownership remain. Stop and close the regulator before switching.

Both cylinder pairs receive boiler steam and share the cab cylinder-cock lever.
Runtime 0.13.1 gives each unit its own native chuff reader and forwards both
units' pressure-gated events to the single chimney. Verde by discordraconequus informed
the compound/exhaust review; no Verde art or audio is included. See CREDITS.md
in the core package for complete attribution.
The animated Franklin shafts retain their original 2:1 transmission, 90-degree
side quartering and 45-degree unit offset. The S282 2.1 presentation supplies the continuous rod/bearing design, wheel-driven
return crank, input shaft/sleeve/collar and small-end pin. A forked crosshead, split
guides and round piston rod use the existing absolute-crank Animator. A local scaled-S282 wheel experiment showed
nearly identical tread/crank datums; the live S060 wheel aliases remain.

Pipework reuses the author-owned S282 front/rear manifolds and transfer pipe.
Actual mesh faces connect the retained boiler fittings and both cylinder sets,
with rigid collars, correct mirrored winding and articulation checks. No S282
runtime dependency or native game meshes/textures/audio are distributed.

Each driven axle has two functional big-end oil cups, including the rear pair.
The current runtime has eight cups in this arrangement. Mounts follow the correct pins and
native threaded-stem geometry. Use the native oil can, lids and level indicators.
Oil quantities/lids survive mode changes and save/load; unused cups retain state.

Hot 0.12 testing showed working cock flows and native torque below dry adhesion;
this revision does not force slip or change grip. Offline geometry/native tests
pass. Retest pipe joins, complete wheel rotations, oil-can controls and save/
reload in game. The runtime captures a short read-only powered trace automatically.
The optional CCL standalone entry is a debug prototype; normal testing uses the
native S060 radio arrangement.

The engine-unit centres are now 3.50 m apart, with each frame moved outward by
350 mm. The driven-axle wheelbase is 4.70 m; each unit retains 1.20 m axle pitch.
The corrected native-support-pivot sweep has at least 33.4 mm longitudinal
mechanical separation and zero targeted rod/crank/cylinder contacts. Both native
smokebox connections have hollow sleeves with the Shay rolled-lip style and
finish. The upper mouths follow the actual angled native pipe profile; lower
cuffs follow the moving Dockside manifolds. The earlier oversized circular cups
are superseded. Both joins pass a 1 mm profile-fit tolerance in sampled poses.

This is staged only, not installed or accepted in DV. See
`docs/dockside/FRANKLIN-0.8.1.md` for exact artifact hashes, test limits and
James-owned game checks. Use the matching staged runtime and asset pack.
