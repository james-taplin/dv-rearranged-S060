# Stage 2 trial — stock S282 ↔ 2-8-4

This is a development test build, not the complete B99 update. Other arrangements,
random spawns and arrangement persistence are intentionally inactive. No duplexes.
The separate chuff patcher and Derailroader remain unchanged.

## Build and offline checks

```powershell
.\Build.Stage2.ps1
pwsh -NoProfile -File .\Validate.Stage2.ps1
```

Output: `build\stage2\RearrangedS282.dll`, `Info.json`, `LICENSE`.
Only this DLL and Info.json are needed in a test mod folder named `RearrangedS282`.
The existing Loco Mesh Splitter dependency must be available. Stage 2 does not execute
AssetStudio or load the old drivetrain bundle, so those tools/assets are unnecessary
for this trial. Do not install the old 1.2.9 DLL alongside the trial.

No script installs the build or launches the game. No in-game validation is claimed
by the offline scripts. The stage 2 module registers only the radio-mode patch;
it does not register the original spawn/save/repair/stress/bogie patches.

## Disposable sandbox runtime check

1. With the game closed, install the trial files into the test mod folder. Use a
   disposable sandbox save. Spawn two fresh native S282s and keep both stopped,
   railed, with regulator closed and driving force settled. Leave the second alone.
2. In the comms radio, select Wheel Rearranger. Select the first S282, choose 2-8-4
   and confirm. Inspect the two small trailing axles, animation and distance views.
   Select stock 2-8-2 to restore its original trailing axle.
3. A refused switch plays the cancel sound and records a reason in the mod log.
   Moving, derailed, uninitialized, powered or slipping locomotives are refused.
4. For automated structural regression, use a new pair of native S282s untouched
   by the rearranger. Open the mod manager and click **Run structural checks on two
   fresh S282s**. Close the manager so fixed simulation ticks can run.
5. The test picks the first two fresh stationary S282s by ID, reports both IDs,
   converts only the first, waits for native ticks, returns it to stock, and checks
   ten repeated round trips. It compares the second locomotive's configuration,
   engine dimensions, shared type, per-car radius/friction, axle references and
   powered registration. It writes `stage2-runtime-checks.json` in the mod folder.
6. Inspect the report's `passed` value and the Player/UMM logs. An incomplete or
   missing report does not count as a pass. Reopen the manager to see the status.
7. Separately inspect visual geometry, roll both versions slowly, and test loaded
   pull, sanding and actual wheelslip onset under matched conditions. The structural
   test does not establish those behaviors or tune the physics.

The 2-8-4 uses the original mod's trailing-axle positions. It keeps the native S282
mass, cylinders, pressure and driver radius. Equal load per physical axle is the
explicit approximation: with four drivers and seven total axles, native dry adhesive
allocation is 6/7 of the stock four-driver/six-axle layout at unchanged mass. This is
not a measured S282 suspension load distribution. The material friction coefficient
is preserved; B99 still handles weather, sanding, braking and slip.

## Rollback

Close the game, remove only the trial mod folder (or restore your prior copy), then
restart. Selections are temporary; they are not stored under the mod's arrangement
save key. Resume the disposable sandbox only for testing. Repair/explosion, gadgets,
save/reload and broad mod compatibility remain later lifecycle validation work.
