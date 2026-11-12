# Independent wheelslip native fixture

Run `Test.IndependentSlip.ps1 -RuntimePath <frozen candidate DLL>`. This builds a
private Unity 2019.4 Mono player and then late-loads the exact candidate after
native ECS TypeManager initialization, matching UMM's component-registration
boundary. No Derail Valley executable, profile, save or installed mod is changed.

The native calculation/helper cases reuse the S282 `native-slip-world` probe.
Reflection adapters call the candidate's actual internal classes; the fixture
does not compile copies of production slip or traction algorithms.

The coordinator cases attach the exact candidate owner to inactive native
TrainCar, SimController, PoweredWheel, bogie and engine-slot hosts. Native
calculation helpers are kept active outside the inactive host, as they are under
an active native car during play. The last `Bogie.ApplyForce` boundary is
intercepted for recording and fault injection; rail dynamics are not simulated.

Coverage includes different torque and controlled reduced-grip asymmetry,
separate RPM leases on slots and underlying engines, native phase publication,
once-per-contact force routing, no borrowed grip, sparks/audio-definition
binding and exact restoration, release/rebind, invalid contacts, a fault after
the first bogie force, compound HP/LP speed separation, retained HP-only boiler
draw/hot compound save state, and destroyed native-component cleanup.

Actual weather, audible playback, loaded track behaviour, native-car startup,
and gameplay saves are outside this fixture. The separate engine/save suite
checks old saves and hot compound state continuation. Existing model/quartering
and presentation suites cover the fitted mechanisms.

`-ReusePlayerFrom <prior run>` reuses an unchanged private player after checking
the bootstrap source hash. Only the probe assembly changes for fixture-only
corrections. Production is not rebuilt because a fixture/report changes.

The preserved 0.3.0 DLL is a negative baseline: it has no group traction owner.
During development, the first 0.3.1 candidate also fails the new destroyed-force
regression; its result is retained separately from the reviewed passing DLL.
