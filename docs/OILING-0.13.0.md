# Running gear and oiling fit — 21 October 2026

The existing S282 implementation was inspected before extending the S060. Its
working pattern copies native external-interactable oil cups and extends native
`ManualOilingPointsDefinition` before the flow is created. This is necessary here:
the S060 has six native circuits, but its fourth driven axle and additional
powered engine unit do not have spare defined big-end cups.

The native S060 six cups are **two fixed guide, two crosshead and two main-rod
big-end** oiling points. They are different functions and must remain. James's
two cups per driven axle refers to its left/right big ends; it does not justify
deleting guide/crosshead lubrication. The expected visible counts are:

| Arrangement | Driven axles | Big-end cups | Guide/crosshead cups |
|---|---:|---:|---:|
| 0-4-0 | 2 | 4 | 4 |
| Stock/short 0-6-0, 0-6-2 | 3 | 6 | 4 |
| 0-8-0, Meyer, Dockside | 4 | 8 | 4 |

Six extra native circuits (three per side) provide the maximum required capacity
of twelve. Original port IDs and saved quantities remain at indices 0–5. Extra
points start full once when the native flow is first created; selections never
refill them. Older six-point saves are fitted to twelve while retaining their
existing values. Dormant points keep their exact quantity, normalized level,
lid and refill state. They are temporarily masked during native tick/service
operations and restored afterwards, including nested native callbacks and thrown
exceptions. Dormant reservoirs neither consume service oil nor create hidden
damage. Active cups retain native oil depletion, oil-can refill, lid controls,
oil indicators, refill audio and save/load behaviour. No traction, cylinder or
adhesion parameters change.

Position binding follows the S282 delayed initialization recipe so native
external-control startup does not subscribe copies twice. Owned readers and
controls unsubscribe during rebuild, streaming destruction and pooling. Cup
mounts use the actual animated wheel crank and bogie frame, including quartering
and articulation. Guide/crosshead cups move onto their corresponding supported
front-engine components when the native mechanisms are hidden.

The native mesh origins are not their mounting surfaces: the normal cup's stem
ends 17.515 mm below its origin, and the crosshead cup's extended stem reaches
110.538 mm below. Mounts use those actual bounds, a 6 mm threaded engagement and
the native crosshead's 87.7 mm outboard offset. Small mounting necks use an owned
shared mesh, a dark S060 atlas patch and the current live body material. They
have no active colliders. Native game cup meshes/materials remain aliases from
the actual external-interactable prefab, never distributed in the mod.

The probe also corrects a stale comment: the native S060 main rod's crank is on
the **rear driver 1**, not front driver 3. In report `195812`, the right main-rod
pivot at Z −1.064587 follows driver 1 at Z −1.399902 plus its crank throw; the
left quartered pivot remains around the same rear driver. Selecting the wrong
native axle would duplicate front cups and leave the rear axle unoiled.

Meyer coupling/main bars now use the shared shaped, bored-eye rod recipe. The
front full-size native cylinder exposed a prior clearance error: the previous
crosshead's front edge reached unit Z 1.00035, while the measured native gland's
rear envelope begins at 0.780774. A 1.15 m visual connecting rod and fitted guide
rails put the front edge at 0.75035, leaving about 30 mm. Stroke and simulation
parameters stay unchanged. The revised check uses the actual crosshead edge,
rather than the previous permissive centre-only test.

The 0-4-0 and 0-8-0 retain native animated/provider transforms and live materials.
Their owned coupling meshes have exactly two or four bosses at the actual live
wheel pins. This removes the unsupported middle boss on 0-4-0 and doubled
overlapping three-boss rods on 0-8-0. Both LODs restore the exact original source
mesh on undo. Stock three-boss rods remain untouched.

Validation combines actual native oil-consumption/save tests with reopened-bundle
cup/mount sweeps and production mesh-swap tests. `SteamOilCupAudit` uses actual
local native cup mesh fixtures and the production mounting functions through 129
poses at three articulation angles; preview cups update after every sampled
animation pose. `SteamNormalRodAudit` measures the actual generated boss surface
centres over the full crank rotation on both mirrored sides and fixed scale
factors, then verifies source-mesh restoration. These checks do not establish
live oil-can interaction or actual native animation behaviour in the running
game; those remain a gameplay test after installation.

Final native fit uses wheel01 long pins on each articulated rear axle, wheel02
in front, coupling X 0.938477 m, main rod X 1.047852 m, and full-size piston
axis X 1.075 m. Bearing holes are 59.5 mm radius (Meyer and conventional) or
42 mm (Dockside). Actual native triangle intersections verify both bearing
faces and eccentric shoulder clearance independently of Y/Z motion checks.
The native bent crosshead cup foot is offset 35.664 mm in X; preserved native
right-side mesh rotation reverses that offset inward. Audits measure full XYZ
thread support inside the 130 mm crosshead blocks, not only minimum height.
Actual S060 guides use OilCup01, crossheads OilCup02; the helper also handles
OilCup03 defensively.

Final reopened exports: Meyer `authoring-20261021-222629` and Dockside
`authoring-20261021-222710` both pass. Each reports 68 native-triangle bearing
checks, 14,719 cup/support sweep assertions and 3,120 conventional rod checks.
Minimum shaft-to-bore radial clearance is 2.076 mm on Meyer and 1.681 mm on
Dockside; main-rod eccentric shoulder clearance is 10.238 mm. Largest measured
cup-support gap is 0.718 micrometres. Conventional boss alignment error stays
below 2.14 micrometres, and the source mesh restores exactly. Motion reports
17,028 counted assertions on Meyer and 15,972 on Dockside; Dockside additionally
executes 3,096 explicit Y/Z native crank checks that no longer increment the
Near-helper counter after X validation moved to actual native triangle checks.
PNG1/2 of the final Dockside preview and PNG1 of Meyer were visually inspected.
The native simulation suite separately passes 212 oiling checks.
