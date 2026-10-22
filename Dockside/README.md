# Franklin Dockside 0-4-4-0T — 0.7.0 test pack

Requires RearrangedS060 0.16.0 and Custom Car Loader. Select **Franklin Dockside**
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
side quartering and 45-degree unit offset. Shaped rods, guides and supported
drive housings fit the S060 wheels. A local scaled-S282 wheel experiment showed
nearly identical tread/crank datums; the live S060 wheel aliases remain.

Pipework reuses the author-owned S282 front/rear manifolds and transfer pipe.
Actual mesh faces connect the retained boiler fittings and both cylinder sets,
with rigid collars, correct mirrored winding and articulation checks. No S282
runtime dependency or native game meshes/textures/audio are distributed.

Each driven axle has two functional big-end oil cups, including the rear pair,
plus four native guide/crosshead lubricators. Mounts follow the correct pins and
native threaded-stem geometry. Use the native oil can, lids and level indicators.
Oil quantities/lids survive mode changes and save/load; unused cups retain state.

Hot 0.12 testing showed working cock flows and native torque below dry adhesion;
this revision does not force slip or change grip. Offline geometry/native tests
pass. Retest pipe joins, complete wheel rotations, oil-can controls and save/
reload in game. The runtime captures a short read-only powered trace automatically.
The optional CCL standalone entry is a debug prototype; normal testing uses the
native S060 radio arrangement.
