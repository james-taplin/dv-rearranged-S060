REARRANGED S060 MEYER ASSET PACK - 0.7.0

Included component of Rearranged S060 0.3.0. Runtime visuals now include the smooth single rear steam trunk, supported underframe, eight native heated brake shoes and eight visible supported leaf packs. The retained donor bundle remains 0.7.0. Custom Car Loader
discovers this bundle inside RearrangedS060/assets/Meyer. It has no separate
UMM entry. Normal play selects Meyer through the native S060's
WHEEL REARRANGER radio or the Rearranged S060 UMM panel.

Meyer-style 0-4+4-0T: two driven axles beneath the S060 and two on a
swinging rear engine unit. It remains one locomotive identity and save,
with shared cab controls, a compound HP -> receiver -> LP steam circuit and
one chimney. The runtime adds rear
bodywork with extra water and coal capacity; see the main README.

S060 0.23.0 turns the front engine inward to face the rear cylinders, with
matched animation and steam connections. The rear running gear moves 65 mm aft;
the bodywork, coupler and lamp positions stay fixed. The asset bundle remains
0.7.0 because these changes are fitted by the S060 runtime.

S060 0.23.1 moves those front cylinders another 850 mm towards the cab, fitting
2 m main rods and their guides/piston rods together. A compact lower admission
feed replaces redundant upper pipework. Mounted air-brake equipment fills the
original front cylinder space; the native brake/compressor simulation is retained.

S060 0.23.3 adds frame-mounted guide carriers and inboard spring seats/hangers
to both engines. Redundant native suspension/supports and unused return cranks
are retired in Meyer only; the native main crank journals remain.

S060 0.23.4 aligns the rear hook and brake-hose/angle-cock assembly with its
mounting pocket. Native buffer heads and stems remain in place.

Stop and close the regulator before switching. Before adding/removing Meyer,
uncouple its rear, disconnect the hose and leave clear track behind. Native
identity, fuel, water, damage and saved state are retained.

The CCL standalone entry is a debug asset donor, not a second supported play
mode or a separate powered B vehicle. Use the native S060 radio arrangement.

This pack uses live S060 materials and native references from your installed
game. See the core README for the initial beta's known issues and diagnostics.
Retained MIT notices and complete attribution are in LICENSE and CREDITS.md.

RESOURCE CAPACITY NOTE: Leaving Meyer retains fuel/water only up to stock
capacity (4,500 L water / 1,200 kg coal). Any excess is discarded with a log
message. Switching does not repair damage or create replacement resources.

Meyer refilling: use the original S060 coal and water filling points. They
serve combined stores of 3,500 kg coal and 8,800 L water, including the extra
rear capacity; no separate bogie fill point is required. This candidate corrects
the native servicing cache that previously stopped purchases at stock limits.
