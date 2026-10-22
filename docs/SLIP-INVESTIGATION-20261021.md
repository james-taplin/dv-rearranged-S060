# Articulated slip investigation — 21 October 2026

James reports no visible wheelslip in Meyer or Dockside on 0.11.0. No grip,
mass, engine calibration or difficulty override has been changed to force it.

## Evidence and limits

The two preserved probes are `195535` (Meyer L-009) and `195812` (Dockside
L-011). Both captured boiler pressure 1 bar absolute, regulator closed, neutral
reverser, zero torque and closed cocks. They validate selected arrangements,
but do not record the powered run or establish why it did not slip.

The production graph sums both stable engine-slot TORQUE_OUT ports into
rs060Torque.OUT, then rewires traction.TORQUE_IN to that sum. This is the same
native traction wiring pattern as the working S282 fork's ArticulatedSimulation.
Native B99 DrivingForce resolves its serialized torqueGeneratedPortId and
calculates generatedForce = port.Value / wheelRadius. Native ECS copies that
generatedForce and evaluates it against native per-axle adhesion, powered axle
count, bogie braking, wheel slide, preventWheelslip and GameParams.WheelslipAllowed.
The actual DrivingForce input binding and these live driving values were not
recorded by the supplied probes; a powered capture is still required to verify
the entire live chain.

Both articulated layouts retain four powered axles and full native S060 mass
as adhesive weight. The runtime neither sets preventWheelslip nor changes the
difficulty option. Native thresholds must remain tied to actual mass, friction,
wetness, sanding, braking and torque, rather than an invented visual slip effect.

## Actual native offline experiments

`tests/Articulated/NativeProbe.cs` runs the production S060 engine bank with
actual B99 simple engines and the production Meyer compound adapter. At a
constant 16 bar absolute / 200 C inlet, full regulator and full gear, cocks
closed, preheated cylinders, after 120 simulated seconds:

| RPM | Meyer force min–max, kN | Dockside force min–max, kN |
| --- | --- | --- |
| 1 | 164.5–168.1 | 81.7–93.2 |
| 10 | 163.3 | 83.2–92.8 |
| 40 | 110.1 | 81.2–83.5 |
| 80 | 66.0 | 62.5–64.1 |

These are available engine force at held inlet conditions, not sustainable
boiler power, observed track pull, or proof of in-game slip. The Dockside has
considerably less available force than the Meyer. It can remain below dry grip
despite full regulator; lower pressure/cutoff or brakes reduce excess force
further. Grip numbers must be read from the actual native car/ECS state.

`NativeSlipKernel.cs` invokes the actual installed B99 generated ECS calculation
by reflection. Exact steady equilibria avoid any ECB tag writes, so the test
needs no simulated Unity world and does not replace the native calculation:

- 60 kN generated force against 100 kN grip: zero steady slip.
- 120 kN against 100 kN: .2 steady slip.
- The same excess force with native difficulty disallowing slip: zero.
- The same excess force with native preventWheelslip: zero.
- The same force with 20 kN aggregate brakes: zero.

The reflection test is deliberately B99-specific and fails if that installed
native kernel changes. It tests native thresholds and inhibition, not time
evolution, world tagging or visual wheel animation.

Ten hot-pressure close/reopen cycles on each layout additionally establish
that both actual normalized cock flow ports close to zero and reopen positive
on every cycle, while retaining one shared cab control port. This isolates the
reported intermittent visible cocks from the stable engine-bank control path.

**193 production engine-bank/native-kernel checks passed**. Exact output is
`build/radio-0.12.0/slip-native/traction-envelope.txt`. Native decompilation is
local investigation evidence in that directory, not distributed game source.

## One useful next driving capture

At a safe low-speed start, warm the engine and release brakes fully. Record a
single regulator ramp in full gear with sanding off, with the game's wheelslip
option enabled. Preserve the real steam and rail conditions rather than changing
mass, friction or difficulty to manufacture a result. Sample both engine torques,
the summed/traction input, DrivingForce.generatedForce, native ECS generated
force and live grip, slip/road RPM, pressure, cocks, reverser, brake forces,
sanding, preventWheelslip and WheelslipAllowed together. A native zero or low
force value would explain no slip immediately; excess force with native slip
enabled but zero response would narrow the remaining live binding issue.

The candidate now automatically collects this read-only block during the first
powered articulated start, through the existing S060 simulation owner's Update.
It writes `Mods/RearrangedS060/diagnostics/articulated-<car>-<UTC>.jsonl`, at most
120 samples, 2 Hz while the regulator is open, with a 180-second overall limit.
It flushes at five-second intervals and closes on completion, mode return to
conventional, owner disable/destroy or failure. No new UI, Harmony hook, physics
write or repeated blind probe capture is required. The rows identify the native
DrivingForce input and compare both selected/published cock flows and crank masks.
Compilation is clean; this passive trace still requires the next actual driving
run to produce evidence of the reported live issue.
