# B99 all-standard development trial (1.3.3)

All 31 stationary structural tests passed in James's 1.3.3 run on 17 October 2026,
including native axle counts, stock restoration, second-car isolation and all six
oilcup bindings. Use the included `MANUAL_TEST_CHECKLIST.txt` for the remaining
appearance, driving, oilcup refill and lifecycle observations.

1.3.3 fixes the next setup failure: drivetrain roots now use the current native
`LocoS282A_Drivetrain L/R` names, matching installed Loco Mesh Splitter. It also
removes the retired static-body lubricator handle placement; B99's external
lubricator pivot already owns and positions that control.

B99 oilcups are included: the six native crosshead/main-rod oiling points keep
their existing simulation ports, oil quantities, refill controls and position-sync
providers. No per-driver circuits are invented for the added axles. The runtime
report records native cup/provider identities and refill port IDs when external
interactables are loaded, and checks their preservation while switching. Manually
check that all six cups follow the mechanism, can be opened/refilled with the oil
can, close correctly, and retain their oil after a switch and return to stock.
Also check them while moving and after walking away and returning. These behaviours
still need in-game confirmation.

1.3.2 fixes the B99 external lubricator/compressor bindings that blocked initial
setup in 1.3.1. It moves the native lubricator pivot and compressor control from
captured stock positions; the retired hinge-joint bindings are removed. This fixes
the confirmed setup failure; the expanded arrangements still need in-game testing.

All 31 original conventional arrangements are selectable, including the high-speed
variants. Duplexes and the old 4-0-4 diagnostic arrangement are excluded.
This expands the earlier stage 2 trial; it does not certify the remaining arrangements.
The all-standard structural pass does not certify the remaining manual behaviours.

## Install the trial

Close the game. Extract this ZIP's `RearrangedS282` folder into the game's `Mods`
folder, replacing the previous trial's DLL and Info.json. Keep the included
`AssetStudioModCLI_net472_win64_contained` folder: this is the original mod's rod
mesh exporter, needed by the full geometry path. Keep Loco Mesh Splitter enabled.
Do not keep a second copy of the old RearrangedS282 DLL enabled elsewhere.
The build scripts do not install anything or launch the game.

Use a disposable sandbox. Select **Wheel Rearranger** on the comms radio, aim at a
stationary, railed native S282, select it, cycle through arrangements and confirm.
Close the regulator and wait for force/slip to settle before every change.
The first conversion extracts the rod mesh from your installed game and may pause
for up to 60 seconds. Failed conversions log the exception and attempt stock restoration.

## Where the structural test button is

1. Spawn two native S282s; leave both stopped, railed, regulators closed. Return
   any already converted engine to **2-8-2 Mikado** first.
2. Press **Ctrl+F10** to open Unity Mod Manager.
3. Find **Rearranged S282 - B99 All Standard Trial** and open its settings panel
   (the settings/gear control on its row).
4. Press **RUN STRUCTURAL TEST — two stationary S282s**.
5. Close the manager with Ctrl+F10 so the simulation can tick. Wait for the run to
   finish, then reopen the same settings panel to read the result.

The test is optional for manual radio use; it is a separate automated check.
It chooses the first two eligible S282s by ID, cycles one through all 31 arrangements,
waits for native ticks, checks physical/entity/powered axle counts, engine/type/mass
preservation, stock restoration and the other locomotive's isolation. Each result is
written to `Mods\RearrangedS282\stage2-runtime-checks.json` (name retained for continuity).
An unsuccessful start also writes a failure report. An interrupted run leaves partial
results with `passed: false`. Final success requires 31 passing arrangement entries.

## What to check manually

For each arrangement, inspect wheels, rods, valve gear, brakes, leading/trailing
supports, sparks and distance/LOD views; roll slowly forward and backward. Then test
loaded pull, sanding and wheelslip under comparable conditions. Check switching
directly between different arrangements as well as returning to stock. The automated
test cannot assess appearance, actual slip onset, chuff timing or loaded performance.

The adapter retains native S282 base mass and engine dimensions. Driver count/radius
and physical axle count change per car. B99 handles traction, braking, weather,
sanding and slip. The load assumption is equal weight per physical axle, using the
Derailroader conversion convention; suspension loads are not calibrated to prototypes.
Derailroader and the separate chuff patcher have not been modified.

Selections remain temporary. Random spawning, arrangement persistence and the old
spawn/save/repair/stress/global-bogie patches are disabled. Explosion/repair, external
interactable unload/reload, gadgets and broad mod compatibility remain unverified.
Save the JSON and game logs for review. A missing/partial report does not mean a pass.

## Build again

Run `Build.AllStandard.ps1`, then run `Validate.AllStandard.ps1` in a fresh PowerShell
process. Output: `build\RearrangedS282-B99-all-standard-trial.zip`.
