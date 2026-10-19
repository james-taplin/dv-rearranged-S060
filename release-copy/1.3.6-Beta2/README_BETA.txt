REARRANGED S282 - B99 CONVENTIONAL ARRANGEMENTS
Version 1.3.6 - Beta 2 | B99 port by JimTaplin
Based on Rearranged S282 by SatiricRug / Peter Schinske.

31 conventional arrangements, B99 oilcup support, optional random arrangements
and saved selections. No duplexes. This beta includes rod timing and Delta
compatibility changes awaiting in-game confirmation. See TEST_STATUS.txt.

REQUIREMENTS
Windows, Derail Valley B99, Unity Mod Manager 0.27.3 or later, and Locomotive
Mesh Splitter 1.1.9. The earlier 1.3.3 baseline was tested on 99-build2702.
Locomotive Mesh Splitter: https://www.nexusmods.com/derailvalley/mods/867
Derailroader, Custom Car Loader and Realistic Firing are not required.

INSTALLATION
1. Close the game and back up any existing RearrangedS282 installation.
2. Install this ZIP through Unity Mod Manager, or extract its RearrangedS282
   folder into the game's Mods folder.
3. Check that Mods\RearrangedS282\Info.json and RearrangedS282.dll are beside
   each other. Replace the older copy; enable only one RearrangedS282 installation.
4. Keep the included AssetStudioModCLI_net472_win64_contained folder. The mod
   uses it to prepare rod meshes from your own installed game.
5. Restart the game. Try the new features on a disposable sandbox save first.

USE
Select Wheel Rearranger on the comms radio, target a native S282, choose an
arrangement and confirm. Stop, close the regulator and let force/slip settle
before switching. The first conversion may pause while rod meshes are prepared.
Select the normal 2-8-2 Mikado to return to stock. Locomotives can have different
arrangements independently.

The arrangements retain native S282 engine dimensions and base mass. Driver size
changes gearing, and adhesion uses an equal-load-per-physical-axle approximation.
These are arrangement conversions rather than calibrated historical prototypes.

B99 OILCUPS
The six native crosshead/main-rod oiling circuits retain their refill controls,
simulation ports and position bindings. Extra driving axles do not create extra
oiling circuits. Refill, alignment and operation remain beta test areas.

RANDOM ARRANGEMENTS AND SAVED CHOICES
Open Unity Mod Manager with Ctrl+F10, select this mod and expand Show arrangement
and random-spawn settings. Random spawning is OFF by default. Enable it and tick
the conventional layouts you want; an empty pool falls back to stock 2-8-2.
Only newly spawned S282s roll a choice. Existing locomotives loaded from an old
save remain stock unless they already have saved arrangement data.

Manual and random choices are stored per locomotive. On reload, restoration waits
until the engine is stationary, its regulator is closed and force/slip has settled.
A moving locomotive may temporarily appear stock. A manual radio choice replaces
any pending saved choice. Save/reload and pooled reuse still need in-game testing
in this version. Older mod builds may discard the new data when resaving.

OPTIONAL DELTA TRAILING TRUCK
The compatibility bridge targets S282 Model Tweaks 1.1.0 by Nitro with DV Car
Changer 1.5.4 by Wiz. Install those separately, including their own dependencies,
if you want the Delta option. They are optional for this mod.

For DeltaTrailingTruckAxles, the bridge uses the Delta appearance on the existing
trailing axle and keeps the arrangement's physical axles, radii and power model.
It targets the reported conflict when applying/removing Delta on 2-12-2/4-12-2.
Delta is displayed only with ONE unpowered trailing axle (x-x-2). On x-x-0 or
x-x-4, its selection remains saved but its graphics are hidden until a compatible
arrangement is selected. Delta wheel visuals scale to the existing wheel radius.
No Delta meshes or other mods' DLLs are included. This is awaiting in-game tests;
it does not repair a previously half-deleted engine still present in memory.

POWER DIAGNOSTICS AND KNOWN ISSUE
Reported extreme acceleration with Realistic Firing's cylinder model remains
under investigation. This release does not fix that report or impose a speed cap.
The reporter found that switching off Realistic Firing's cylinder feature stopped
their overspeed issue; the exact interaction has not been established.

For useful traces, open this mod's UMM panel, click START POWER DIAGNOSTICS, close
the manager and drive. Then click STOP AND SAVE POWER DIAGNOSTICS. JSONL files are
written to Mods\RearrangedS282\diagnostics. They record simulated wheel/crank RPM,
road-derived RPM, slip, controls, torque, pressures and steam flow. Simulated wheel
RPM is not an optical measurement of the rendered wheels. Separate captures with
Realistic Firing cylinders off/on help compare conditions.

TESTING AND BUG REPORTS
Compilation and standalone checks pass. The 31-arrangement structural pass belongs
to 1.3.3; it does not certify this 1.3.6 DLL. See TEST_STATUS.txt for exact limits
and MANUAL_TEST_CHECKLIST.txt / RUNTIME_TRIAL_TESTING.txt for test steps.

The optional structural test is in this mod's UMM panel. It needs two stationary,
railed S282s in stock 2-8-2 with regulators closed. Stop power capture first.
Close UMM during the test so simulation can tick. Results go to
Mods\RearrangedS282\stage2-runtime-checks.json.

Reports should include arrangements, reproduction steps, game/mod versions and
screenshots/video where useful. Attach Player.log, the UMM log, and any relevant
structural JSON or diagnostic JSONL. Copy logs before another launch overwrites them.
Player.log: %USERPROFILE%\AppData\LocalLow\Altfuture\Derail Valley\Player.log
UMM log: <game>\DerailValley_Data\Managed\UnityModManager\Log.txt

UNINSTALL / ROLLBACK
Close the game, then remove this mod folder or restore your backed-up version.
Restart to update or unload the mod; hot unloading is refused. Removing it leaves
native S282s with stock running gear. Multiplayer synchronization is not included.

See CREDITS.txt, LICENSE, THIRD_PARTY_NOTICES.txt and licenses/ for credits and terms.
