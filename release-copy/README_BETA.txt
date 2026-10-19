REARRANGED S282 - B99 CONVENTIONAL ARRANGEMENTS BETA
Version 1.3.3 - Beta 1 | B99 port by JimTaplin
Based on Rearranged S282 by SatiricRug.

IMPORTANT BETA LIMITS
Conventional arrangements only: 31 choices. No duplexes.
Arrangement selections are temporary and do not persist after reloading the game.
Random arrangement spawning is not enabled. This beta is for sandbox testing.

REQUIREMENTS
Windows, Derail Valley B99 (tested on 99-build2702), Unity Mod Manager, and
Locomotive Mesh Splitter (tested with 1.1.9).
Loco Mesh Splitter: https://www.nexusmods.com/derailvalley/mods/867
Derailroader is not a runtime dependency. No CCL dependency is introduced.

INSTALLATION
Close the game. Back up any existing RearrangedS282 installation.
Install this archive through Unity Mod Manager's mod installation interface, or
extract its RearrangedS282 folder into <Derail Valley>\Mods.
The resulting path must be Mods\RearrangedS282\Info.json, with RearrangedS282.dll
beside it. Replace an older copy; do not enable both old and new copies.
Keep the included AssetStudioModCLI_net472_win64_contained folder. It is the
original mod's mesh exporter (trimmed to its tested mesh-export dependencies), used to generate connecting-rod meshes locally from
your installed game. No extracted game meshes or drivetrain bundle are included.

USE
Use Wheel Rearranger on the comms radio, select a native S282, choose an
arrangement and confirm. Stop, close the regulator and let force/slip settle
before every switch. First conversion may pause while rod meshes are prepared.
Select 2-8-2 Mikado to return to the captured stock configuration.
Both engines can be rearranged independently during manual use.

B99 OILCUPS
The six native crosshead/main-rod oiling circuits retain their native refill
controls, simulation ports and position-sync bindings. There is no new oil
circuit for each extra driving axle. Cup alignment/refilling during normal
operation remains part of beta feedback.

VALIDATION
All 31 stationary structural checks passed in game on 17 October 2026: physical
and simulation axle counts, return to stock, isolation from a second locomotive,
and preservation of all six native oilcup bindings. The included DLL is the exact
binary from that successful run. Visual appearance has received positive initial
user feedback. Full driving, pulling, wheelslip, audio/VFX, oilcup refill behaviour,
LOD/unload-reload, repair/explosion and wider mod compatibility are still beta
validation areas. Structural success does not certify every operating condition.
Native S282 mass and engine dimensions are retained. Equal weight per physical
axle is the current load approximation; these are not calibrated prototypes.

OPTIONAL STRUCTURAL TEST
Use a disposable sandbox with two stationary, railed native S282s in stock 2-8-2,
regulators closed. Ctrl+F10 -> this mod's settings -> RUN STRUCTURAL TEST.
Close UMM so simulation ticks can run. The test changes one engine through all
31 choices and leaves the other as a control. Do not operate either during it.
It writes stage2-runtime-checks.json in the mod folder.
For manual testing, use MANUAL_TEST_CHECKLIST.txt; the automated test is optional.

BUG REPORTS
Include arrangement and previous arrangement, locomotive ID, reproduction steps,
screenshot/video, game/mod versions and relevant other mods. Attach Player.log,
UMM Log.txt and stage2-runtime-checks.json if the structural test was run.
Player.log: %USERPROFILE%\AppData\LocalLow\Altfuture\Derail Valley\Player.log
UMM log: <game>\DerailValley_Data\Managed\UnityModManager\Log.txt
Copy logs before another game launch overwrites them.

UNINSTALL / ROLLBACK
Close the game and remove this mod folder, or restore your previous backed-up
version. Test on a sandbox save; saved arrangement persistence is not part of
this beta. Keep unrelated mods and saves intact.

CREDITS AND LICENSE
Original mod: SatiricRug / Peter Schinske.
B99 conventional-arrangement port: JimTaplin.
Independent MIT-licensed fork. Original notices are retained. See CREDITS.txt,
LICENSE, THIRD_PARTY_NOTICES.txt and licenses/ for the applicable terms.