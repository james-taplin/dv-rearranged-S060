REARRANGED S282 - B99
Version 1.9.2 (beta) | B99 port by JimTaplin
Based on Rearranged S282 by SatiricRug (Peter Schinske).

WHAT IT DOES
Rebuilds the S282 with a different wheel arrangement: 43 choices in all.
- 34 conventional layouts, from the 2-2-2 and 2-4-0 up to the 4-12-2 and 4-14-4,
  including the original mod's 31 and its high-speed 2-8-x variants.
- 8 articulated layouts with real swinging front engines and extra native steam
  engines: 2-4-4-2, 4-4-4-4, 2-6-6-2 Mallet, 0-8-8-0 "Angus", 2-6-8-0 Great
  Northern, and the 2-2-2-2-2 and 0-2-2-2-0 triplexes.
- The original "4-0-4 Drivers not found" novelty (no drivers, no power).

REQUIREMENTS
Windows, Derail Valley B99, Unity Mod Manager 0.27.3 or later, and
Locomotive Mesh Splitter 1.1.9: https://www.nexusmods.com/derailvalley/mods/867
Realistic Firing is not supported with this version.

INSTALLATION
Close the game. Remove or replace any older RearrangedS282 folder; never run two
copies. Install the archive through Unity Mod Manager, or extract its
RearrangedS282 folder into <Derail Valley>\Mods, so that the path is
Mods\RearrangedS282\Info.json.
Keep the included folders:
- AssetStudioModCLI_net472_win64_contained: the original mod's mesh exporter,
  used to make rod meshes from your own game files.
- assets\s282_drivetrain: the original mod's articulated drivetrain assets
  (Franklin valve gear), needed by the articulated layouts.
Restart the game to update or unload the mod.

USE
Use Wheel Rearranger on the comms radio, point at an S282, choose an arrangement
and confirm. Stop, close the regulator and let the wheels settle first. The first
conversion can pause briefly while rod meshes are prepared. Choose 2-8-2 Mikado to
return to stock. Each locomotive remembers its arrangement in the save.

SETTINGS (Ctrl+F10)
- Random spawning (off by default). When on, new S282s pick from the ticked
  arrangements. The default pool holds the useful conventional layouts; the
  high-speed variants, the 2-2-2, the 4-14-4, the 4-0-4 and every articulated
  layout are opt-in. Players upgrading keep their own choices, except that the
  high-speed variants are unticked once.
- Trailing truck styles, valve gear visibility and Pacific/Hudson valve gear.
- Curve drag and squeal for long rigid wheelbases (both on).
- 2-2-2 driver diameter (experimental layout).
- Optional power/curve diagnostics capture and a structural self-test.

HOW THEY DRIVE
- Each arrangement uses the S282's own steam engine and boiler. Articulated layouts
  add native second (and third) engines with their own chuffs, cylinder cocks,
  wear and save state.
- Grip follows the layout: small 2-, 4- and 6-coupled engines carry realistic axle
  loads, so they slip if you open the regulator fully from a stand.
- Long rigid wheelbases (for example the 4-14-4) drag and squeal on tight curves;
  articulated engines do not.

BETA STATUS
The structural self-test passed for 40 arrangements in game. Several later
changes have only passed offline checks and still need driving confirmation: the
0-2-2-2-0, 0-8-8-0 and 2-6-8-0, the larger triplex drivers and 4-14-4 spacing,
the grip limits, and curve drag. Use a test save first.

BUG REPORTS
Include the arrangement (and the previous one), what you did, a screenshot or
video, game and mod versions, other mods, and Player.log from
%USERPROFILE%\AppData\LocalLow\Altfuture\Derail Valley\.

See CHANGELOG.txt, CREDITS.txt, LICENSE and THIRD_PARTY_NOTICES.txt.
