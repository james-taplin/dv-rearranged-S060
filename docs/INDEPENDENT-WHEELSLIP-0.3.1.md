# Independent powered groups — 0.3.1

This candidate ports the S282 native group-slip method into the existing S060
DLL. No S282 runtime dependency, new simulation service, vehicle identity or
mesh bundle is introduced. This remains a test candidate; the handover records
installation status and James owns DV acceptance.

## Arrangement contract

Dockside declares one group per simple engine. Meyer declares front HP and rear
LP groups. The shaft-linked Shay and conventional coupled arrangements retain
one native drive. A future arrangement with mechanically independent engines
must declare its groups rather than feed them through a pooled traction limit.

`S060IndependentDrive.Definition` supplies an existing engine slot, visual root,
rolling radius, role and driven-axle counts per actual native bogie. Attach checks
those counts against the live powered-wheel hierarchy; a physical axle cannot
borrow another group's grip. Different group radii are supported by the owner.
Adding a new engine still requires its ordinary graph/slot/steam integration;
the traction owner is not a new engine factory.

Create the owner after fitting, powered-wheel registration and engine selection.
It waits for native conversion and a completed sample before replacing native
traction. Each group leases both its stable slot's RPM reference and selected
native implementation's reference. Existing absolute-crank presentation, inward
Meyer mapping, native quartering, cocks and exhaust remain authoritative.

The carrier/group helpers use only already-registered DV ECS components and
DVConvertToEntity lifetime boundaries. Native weather, weight, sand and adhesion
calculations remain responsible for grip and slip. Forces are limited per group
and distributed once over declared contacts; native braking is retained. Invalid
contacts/calculations or derailment release ownership to the native fallback.
An exception after a bogie force has already been applied cannot also run pooled
traction in that step. Release the owner before selecting another engine,
restoring the physical axles or destroying any engine/effect hierarchy.

The port reuses S282 IndependentDrive, NativeSlipRuntimeState,
NativeSlipEntitySchema, GroupForceRouting and GroupSlipEffects concepts. Donor
core files match audited checkpoint 0ab7f2a in the current S282 checkout.
S060 already has native per-engine phase readers, so its existing presentation
is retained rather than adding the donor's second animation-binding layer.

## Compound and saves

MeyerCompoundCycle accepts HP and LP speeds independently; its former equal-speed
entry remains compatible. Each stage advances once. HP alone draws boiler
steam; HP exhaust feeds the finite receiver; LP receives receiver steam and
alone supplies the blast. Receiver backpressure and steam/work conservation
remain in place. A hot-load correction restores derived receiver exhaust
pressure/temperature before native LP expansion preparation.

Arrangement IDs, slot IDs, oil topology and save schemas are unchanged. Native
engine phases, condition and existing compound vessel/pressure memory remain
saved through their existing owners. Transient slip helpers are recreated from
native motion rather than serialized as new cars or resource stores. Old Meyer,
old 43:19 Shay and preceding 1.2 kg/s Shay saves remain supported. No cylinder,
flow, gearing, wheel-size, boiler or adhesion calibration is retuned here.

## Presentation and limits

Native slip leaves are bound to the replacement physical axles and restored on
release. Native audio definitions map to their actual bogie's wheels; an emitter
covering several groups uses their maximum mapped slip. Original arrays/list
references are restored, including teardown after native components are destroyed.
No additional slip audio sources or native assets are distributed.

Native wheel slide/braking and global cab slip indications remain car-wide.
Individual powered sets cannot pool unused grip, so unequal engine demand can
reduce total pull relative to the old combined limit. Reapply after derailment
once repaired/rerailed if the independent owner has relinquished control.

## Validation

The exact candidate passes 101 native offline checks: late TypeManager loading,
native helper lifecycle, asymmetric slip/recovery, sand/slide/prevent-slip,
actual slot and underlying RPM leases, phase publication, force distribution,
effects restoration, release/rebind, invalid contacts, partial-force exceptions
and destroyed-component cleanup. Three mixed-contact routing assertions cover
three groups; this is not acceptance of an unimplemented three-engine graph.
The first candidate fails the added destroyed-force regression as expected.

Meyer passes 2,443 native engine/save checks and 89 pure compound checks,
including seven unequal/stopped/opposed-speed cases and hot/old-save continuation.
Equal-speed force envelopes match the previous baseline.

Native fixture details: tests/IndependentDrive/README.md. The private release
player uses actual candidate/native ECS, engine, port and effect code, with an
inactive car host and an intercepted final Bogie.ApplyForce boundary. It does
not establish loaded rail dynamics, actual weather, audible playback, frame
time, full native-car startup or gameplay acceptance. See docs/HANDOVER.md for
final exact-DLL regression, packaging and staging receipts. Do not install or
launch DV automatically.
