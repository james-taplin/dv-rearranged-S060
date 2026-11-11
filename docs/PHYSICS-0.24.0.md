# S060 0.24.0 physics candidate

## Meyer starting and adhesion pass — 11 November 2026

The Meyer now requests low-speed torque from DV's actual two-cylinder crank,
valve and trapped-pressure mechanics. It uses instantaneous mechanics below
45 RPM, the native blend to 60 RPM, then native mean torque. The existing finite
HP chest → receiver → LP chest circuit still owns all steam and energy: HP works
against receiver pressure, LP receives no boiler feed, and discharged steam's
available energy caps moving work. Cylinder dimensions, chest/receiver volumes,
flow limits, resources, boiler and engine-slot identities are unchanged.

An empty circuit starts on HP alone; LP builds as HP exhaust reaches it. No
starting bypass or arbitrary force multiplier was added. Native minimum cutoff
is 5%: a centred reverser is not a guaranteed zero-torque neutral at every crank
angle. Native water, cock, lubrication and damage behavior remains in the adapter.

New Meyer saves optionally retain signed cylinder admission pressures under
`mechanicsVersion: 1` and `cylinderAdmissionBar`. This prevents a hot partial-cutoff
reload changing the first starting torque. Existing `meyerVersion: 1`, vessel and
ledger fields stay unchanged. Old saves load without resetting stored steam;
their mechanical cache is seeded from retained circuit pressure on the first
active tick. Selecting Meyer after another arrangement reseeds the cache for the
new crank phase. No new arrangement/oil identifiers or save migration is required.

All four Meyer axles remain powered using native adhesion. Installed B99's actual
dry coefficient is 0.20 × 1.3 = 0.26. At an illustrative 50 t this gives 127.53 kN
dry unsanded grip; at 56.6 t, 144.36 kN. Wetness and sand change this through the
native kernel; adding axles does not multiply total grip. Native resource mass
uses actual litres/kilograms, including the Meyer's larger supplies.
Capacity restoration and return-to-stock clamping now immediately call the
existing native mass refresh, avoiding up to 120 seconds of stale mass after
those amount changes. Ordinary resource consumption/refilling retains DV's
normal update behavior.

The fixed-supply benchmark (16 bar absolute, 200 C, full gear/regulator) gives
unclipped mean Meyer wheel force of 150.4 kN at 0.1 km/h, 169.9 kN at 1 km/h,
149.3 kN at 5 km/h and 115.0 kN at 10 km/h. These values exceed dry unsanded grip
at the illustrative masses above: they are available engine force, not guaranteed
drawbar pull. The 0.1 km/h figure is not an all-crank-angle static starting rating.

Diagnostics now include native/live adhesion coefficients and native bogie
rolling-resistance/stress data at the existing bounded capture cadence. This
helps distinguish wheel slip, supply weight and native stress drag in James's
next running test; no speculative rolling-drag adjustment was made.

Offline evidence and final candidate status are recorded in `docs/HANDOVER.md`.
These are machinery/native-kernel checks, not measured gameplay haulage ratings.
Independent HP/LP wheel-slip clocks and full signed braking thermodynamics are
outside this pass; the existing common wheel clock and conservative energy
ledger remain. James performs DV acceptance. Do not install this candidate yet.

## Shay specification and earlier evidence

James approved the1.6kg/s Shay ceiling on11November. This is now applied and
staged only, with unchanged.80m wheels,252mm bores,45L chest and4.3:1 gearing.
The16barabs/200C fullgear benchmark gives156.7kN near-start,152.6kN at1km/h,
138.4kN at5km/h,91.7kN at10km/h and57.6kN at15km/h. Use normal cab cutoff
control after starting:70%reverser(~64.5%cutoff) gives127.7kN at10km/h;
40%reverser(~39%cutoff) gives106.5kN at15km/h. At13bar those shorter-cutoff
points give111.1/87.1kN. Fixed supply does not establish boiler sustainability.

The previous1.2kg/s fullgear table's64.1/38.9kN at10/15km/h was not a maximum
operating curve: even that tune gave113.0/75.0kN with shorter cutoff. Preserve
this distinction when comparing arrangements. No automatic cutoff added.
Evidence: build/shay-flow-1600-20261111/engine-envelope.json,2365engine/save
checks plus918checks across three affected native Unity suites. Actual prior
1.2kg/s active/dormant saves and older43:19 saves load unchanged. No save,
wheel, gear, model or boiler-capacity changes are required by this flow retune.

CURRENT STATUS: James selected .80 m wheels and approximately150 kN starting effort.
The final physics specification is252 x300.808 mm,45 L chest,1.6 kg/s flow and86:20
gearing. The staged runtime compiles without warnings/errors. Complete matched
model/geometry acceptance remains separate from this tune. The earlier
full-Unity candidate below is SUPERSEDED and must not be installed or released.
See [the modelling handoff](shay/REGEAR-MODELLING-BRIEF.md).

FIRST ITERATION / HISTORICAL EVIDENCE FOLLOWS (63 kN Shay, .71 m wheels):

Prepared for James's testing; do not install without separate authorisation.
Includes the uninstalled 0.23.5 Meyer visual repairs. This is an authored first
physics pass, not a claim of measured DV haulage ratings.

## Corrected reference

The former engine fixture loaded the Dockside donor as its stock definition and
used an obsolete Shay rolling radius. Prepare.NativeCalibration.py now extracts
the exact installed LocoS060/[sim]/steamEngine definition and records native file
hashes/prefab identity. Stock is two 420 x 610 mm cylinders, 200 L chest and
2 kg/s flow ceiling. Comparisons use the same road speed and actual rolling
radii, not the same wheel RPM. All nine arrangement choices are covered.

Corrected pre-tuning evidence is build/physics-20261111/fixture-baseline-envelope.json.
The preserved pre-tuning managed engine-bank assembly produces actual old slot
saves for cross-version loading checks; it is test evidence, not a game DLL or
an installation rollback.

## Authored choices

| Arrangement | Machinery and adhesive-load policy |
|---|---|
| Stock 0-6-0 | Unchanged native engine and adhesion |
| 0-4-0 | Stock engine/wheels; 38 t effective adhesive design load at 50 t reference mass |
| Short 0-6-0 | Stock engine; 1.1394 m wheels; all axles driven |
| 0-8-0 | Same wheel size/engine as short 0-6-0; extra axle adds no power or mass |
| 0-10-0 | Stock engine with 1 m wheels; greater leverage, not extra boiler power |
| 0-6-2 | Stock engine/wheels; conservative 75% adhesive share |
| Meyer | Existing HP/receiver/LP circuit and all-driven native adhesion retained |
| Dockside | Two 340 x 470.919 mm simple engines; 115 L chests and 1 kg/s ceiling each |
| Shay | Three 210 x 300.808 mm cylinders; 30 L chest, 1.2 kg/s ceiling, unchanged 43:19 reduction/92% efficiency |

The fixed 50 t design mass only selects an effective native friction coefficient;
it does not change actual mass, CoM, inertia, weather or resource mass. The
0-4-0 share becomes .76. Other conventional shares stay unchanged. Existing
native DrivingForce, adhesion, slip and braking remain authoritative.

Dockside's 440 mm outside cylinder castings accommodate the larger bores without
moving the wheels, piston stroke or gear. Its combined flow ceiling remains the
native stock engine's 2 kg/s; no boiler capacity is added.

Shay's existing 236 mm outside jackets retain their stations and pitch. Owned
mesh copies fit 210 mm bores, longer crank throws and matching piston travel;
internal head spigots are shortened for chamber clearance. Existing guides,
native wheelsets, shaft journals, transmission anchors and reduction remain.
Motion and simulation dimensions change together; there is no force multiplier
other than the existing mechanical reduction and efficiency.

The original solid lower guide yoke already intersected its rod. The existing
guide/yoke meshes now form supported front/back bearing strips around an open
rod passage. The larger bore also requires the valve chests 25 mm inboard and
lower exhaust branches 10 mm outboard, with steam/exhaust branches reshaped to
their fixed headers. These are measured fit corrections; external header ports
and transmission anchors stay fixed. Both LODs restore their original meshes
when the owned engine is destroyed.

Meyer is a compound/range choice, not a promise of twice stock pull. A shared
native adhesion limit still caps its combined engine torque. Independent power
unit slip and crank-resolved compound starting torque are not added in this pass.

## Reference performance and limits

At 16 bar absolute, 200 C, full regulator/cutoff and prescribed road speed:

| Mean available wheel force | 1 km/h | 5 km/h | 10 km/h | 15 km/h |
|---|---:|---:|---:|---:|
| Stock | 77.5 kN | 74.7 | 70.3 | 67.1 |
| Shay before | 21.8 | 21.2 | 20.0 | see baseline evidence |
| Shay candidate | 62.9 | 58.7 | 55.7 | 47.1 |
| Dockside before | 90.3 | 85.7 | 79.4 | see baseline evidence |
| Dockside candidate | 115.8 | 109.7 | 101.4 | 79.0 |

The grade-budget checks declare a 50 t locomotive, 1.5% gradient, rolling
allowance .002 times total weight, dry adhesion coefficient .20 and at least
20% force reserve: Shay with 150 t trailing at 10 km/h; Dockside with 200 t at
15 km/h. Both pass at 16 and 13 bar. These are design checks, not DV train runs.

Mean engine steam demand at the 16-bar duty points is approximately .96 kg/s for Shay and
2.0 kg/s for Dockside. These are engine steam rates before native boiler water
consumption multipliers. Boiler pressure/fire management, actual train/curve
resistance, wet adhesion and gameplay acceptance remain unverified offline.

## Saves and diagnostics

Arrangement/engine/oil IDs, save versions and field schemas remain unchanged.
Old Shay/Dockside slot saves are loaded into the new machinery and compared
before ticking: stored steam mass, wetness, temperature, damage and phases are
retained. No free refilling or resource changes are introduced.

Automatic diagnostics retain their initial three-second settling capture and
120-sample/2 Hz cap. Their 180-second driving clock now starts at the first
regulator-open sample, so warming up does not exhaust the useful capture.

James's acceptance: same known consist/resources/pressure, level start, grade
start, sustained 1.5% climb and restart; compare stock, Shay and Dockside. Check
Shay motion on both LODs and both directions, then save/reload and switch layouts.
