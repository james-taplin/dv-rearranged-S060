# Two-truck Shay integration — 27 October 2026

R1 is integrated as the ninth S060 radio/UMM choice, appended arrangement ID 7.
Stock and all previous IDs remain unchanged. No new car/livery/companion vehicle
is introduced. Random spawning retains its previous pool.

The native solution is sufficient: DV's existing three-cylinder engine branch
provides 120-degree piston phasing and six chuffs per engine revolution. A native
ConfigurableMultiplier supplies engine RPM = wheel RPM * 43/19; published wheel
torque is engine torque * 43/19 * 0.92. The existing native DrivingForce, ECS
adhesion, wheelslip, boiler, resources and damage controllers remain responsible
for physics. Bore 0.17 m and stroke 0.150395513 m match the authored side engine.
This compact engine has considerably less power than the stock S060; do not
silently inflate its output to imitate stock performance.

Four live native PoweredWheel objects use complete S060 01/02/02/03 meshes and
their native distant LODs. Only Y/Z are scaled 0.8; X/gauge/mirror stay native.
Axle Z stations are -1.98, -0.74, 1.00, 2.24 m. Native support pivots stay at
-1.4 and +0.925 m. Body relief uses measured R1 per-car mesh copies, restored on
exit. No native mesh, texture, sound or DLL is embedded in the Shay bundle.

Mechanical policy: native traction uses the average 43:19 ratio. Ordinary paired
Hooke joints have phase ripple at compound articulation. The visible shafts and
wheels follow that actual solver, rather than concealing it: worst original R1
sampled phase errors are 8.6645/11.4457 degrees. These are visual kinematics, not
an independently simulated second transmission/force system. The bevel teeth
are simplified visual flanks with sampled clearance, not certified conjugate
contact geometry. Keep the 43-engine-turn/19-wheel-turn phase seam continuous.

Original module sources remain in B:/LLW CONVERT/locos/s060-shay. Build.Shay.ps1
accepts -SourcesRoot and combines their frozen R1 prefabs. The fit-contract hash
is 3a6a104d1f12c49c3e399001e67f9f095c10eecd5af2f39677ac44d70f37e8a7.
The transmission Unity builder has now compiled, reopened its scene and verified
all 43 part references. This supersedes its earlier handover's Editor limitation.
Shay/tools/Check.Assembly.py adds the missing transmission-to-finished-truck
triangle checks: 49 poses and eight phases per truck. Fixed hanger seats touching
the truck castings are deliberate mounting interfaces. No continuous collision
sweep or manufacturing tolerance certification is claimed.

Save schema 4 applies only to Shay. Schemas 1–3 and prior engine circuits retain
their meanings. The optional appended dormant engine state may be absent from
old saves. All five possible wheel-condition slots survive, including a dormant
fifth axle. The full geared phase is saved separately with the native cylinder
state. Invalid geared state rolls the whole slot back. Existing fourteen oil
circuits stay at the same indices: eight axlebox cups and four engine servicing
points reuse twelve of them, leaving the last two dormant. The engine cups cover
three big ends and a common bearing lubricator; extra decorative guide cups are
not extra oil reservoirs.

Validation combines the original R1 numeric evidence, actual Unity bundle
reopening/native-mesh fixtures, production conversion-boundary tests, native
engine/save/oiling tests and actual UMM extraction/update/migration tests.
These isolated tests cannot confirm whole-car DV driving, tight-curve behaviour,
sound balance or gameplay performance. Follow RADIO-ARTICULATED-TESTING.md in
game before describing this new arrangement as gameplay accepted.
