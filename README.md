# Rearranged S060

Change the S060's running gear through a dedicated comms-radio mode in Derail
Valley. This project forks the Rearranged S282 B99/Re:Arranged branch, based on
Peter Schinske / SatiricRug's original Rearranged S282.

## Current candidate: S060 0.3.6-mp

Optional multiplayer support from the current S282 integration, based on the
0.3.5 repairs, including buffers, switching and baked Dockside sleeves. Includes host-checked radio/UMM changes, guest permissions,
late-join synchronization and native replication of independent slip and Shay
motion. Normal single-player use does not require Multiplayer.

The final pipe-sweep and 0-4-0 brake-shoe repair is staged separately from the installed build. See [final staging record](docs/FINAL-STAGE-0.3.6-mp.md). James owns gameplay and live peer acceptance.
See [implementation and validation](docs/MULTIPLAYER-0.3.6.md) and
[James's checklist](MULTIPLAYER_TESTING.txt). Build with `Build.Multiplayer.ps1`
and package with `Package.Multiplayer.ps1`; asset bundles are reused unchanged.

## Previous release: S060 0.3.3, Meyer assets 0.7.0, Dockside assets 0.8.1

0.3.3 combines original geared Shay exhaust audio and smoother pipe bends,
the requested Meyer rear steam-return route through its step brackets,
restored Franklin gearbox/shaft geometry and twin 0-4-0 air reservoirs.
The 0-4-0 retains its working animated reverser while retiring surplus fixed gear.
Earlier finish-binding and loose-gear repairs are retained.
The repaired rear coupler and pinned drawgear remain. This build is staged for
review and James's gameplay testing; installation state is in the handover and
future 0.4 work is separate. The tender paint now avoids borrowed foothold
decals. Native smokebox-door geometry is included in the offline previews and
checked for preservation during conversion.

## Retained 0.3.1 powered-group drive

0.3.1 adds reusable native powered-group wheelslip for Dockside and Meyer.
Each engine has its own wheel RPM, adhesion limit and phase; the Meyer keeps
its finite compound receiver with separate HP/LP speeds. The mechanically
shaft-linked Shay remains one drive. Native braking/wheel-slide and global cab
indications remain car-wide. Older saves retain their engine/resource/oil IDs.
This is an unpublished test candidate; installed version and evidence are
recorded in the handover. James performs driving acceptance.

## Previous integrated 0.3.0 build

The integrated 0.3.0 build combines Meyer visual checkpoint 8293d76, Shay db91158 and Dockside 1b39170 with the physics fixes. The former v0.3-work experiment is now future v0.4-work / 0.4.0. The physics tune retains 0.80 m Shay wheels and approximately 150 kN starting effort with a 1.6 kg/s steam-flow ceiling. It passes 2,365 native-engine/save checks and the affected offline Unity suites; installation and gameplay status are recorded in the handover.
It corrects the native stock comparison and Shay test radius, fits a longer-stroke
Shay engine, tunes Dockside cylinder capacity/flow, and authors conventional
adhesive-load allowances. It includes the 0.23.5 Meyer visual repairs. See the
[physics design and reference results](docs/PHYSICS-0.24.0.md). Existing saves,
stock machinery and Meyer compound calibration remain supported.

Earlier maintenance work aligns the Meyer's rear hook and air connection with its hook
pocket, retaining the existing buffers and floor. It also improves the missing
asset warning with CCL dependency guidance. It retains the mounted Meyer guide rails and springs on its powered frames
and removes unused return cranks. It retains the inward-facing Meyer layout,
two-truck Shay R2, Meyer R2 bodywork and capacity,
and the performance, servicing and audio-lifecycle fixes. It is prepared for James's
in-game testing; build/package evidence does not establish gameplay acceptance.

The planned Shay uses live S282 pony wheelsets at stock gauge, radially resized to a
rolling diameter of 0.80 m. Its three-cylinder side engine drives two
swivelling trucks through animated shafts, universal joints and bevel gears at
86:20 (4.3:1) reduction. Six upright oil cups serve its existing native oil circuits.
The engine, trucks and transmission retain their finishes across save reloads.

The Meyer carries its rear tanks, tarpaulin-covered coal hopper and all three
rear lamps on rigid bodywork above the swivelling rear engine unit. Its capacity
is 8,800 L water and 3,500 kg coal. Switching away retains up to the stock
4,500 L / 1,200 kg; any excess is discarded with a log message. The LP exhaust
returns along the cab roof/tank top to the single chimney through an articulated
connection. Both cylinder-cock sets use the S060 cab control.

Dockside remains one tank locomotive with two simple-expansion engine units
and animated Franklin gear. Both units contribute to the single chimney.
Native readers continue to own sound, particles and cylinder-cock timing.

0.23.3 retains the axle cache during unchanged reads, skips invalid steam-pipe
poses safely and avoids mesh rebuilds for tiny movements within the fitting
budget. Exhaust readers now clean up partial initialization and repeated
Init/Deinit without accumulating listeners or copied readers. Shay status text
uses the same rolling-radius datum as its physics.

Save schemas, arrangement IDs, wheel/oil circuit identities and simulation
calibration are unchanged by this update. Supported old saves remain readable;
Shay selections use the existing schema 4. Returning to older mod versions does
not add support for arrangements those versions never implemented.

See [the current handover](docs/HANDOVER.md), [testing guide](docs/RADIO-ARTICULATED-TESTING.md)
and [changelog since public 0.22.2-beta](docs/nexus/CHANGELOG-0.23.3.txt).

## Arrangements

- **0-6-0 Stock:** original 1.42 m drivers.
- **0-4-0:** four-coupled running gear with stock-sized drivers.
- **0-6-0 Short Wheelbase:** closer axle spacing and 1.14 m drivers.
- **0-8-0:** four fitted axles using the same 1.14 m drivers.
- **0-6-2:** stock drivers with a native trailing pony axle.
- **0-10-0 Tank:** five powered axles, 1.00 m drivers and ten big-end oil cups.
- **Meyer-style 0-4+4-0T:** two powered axles beneath the S060, plus two beneath
  an attached pannier rear section, with HP -> receiver -> LP compound steam.
- **Two-truck Shay:** native S282 pony wheelsets and a geared three-cylinder engine.
- **Franklin Dockside 0-4-4-0T:** two simple-expansion units beneath the tank body,
  1.00 m drivers and animated Franklin gear.

The Meyer is one S060 identity. Changing away removes its rear section and
restores the stock supports, rear coupling point and length. Its tanks provide the increased capacity described above. Dockside has no rear extension.

Choices have fixed, fitted wheel sizes. There is no release wheel-size slider.
Switching retains the native locomotive identity, livery, cab, fuel/water within the selected capacity,
damage and save ownership. Both articulated cylinder-cock sets use the existing
cab control. Conventional, Meyer and Dockside layouts have two functional cups per driven axle. The geared Shay has six upright servicing cups.

Meyer exhaust presentation follows its LP stage, with lower-pressure chuff bands
informed by discorddraconequus's Verde compound. Its new return is separate from
the existing admission/interstage pipework. Both Dockside units contribute
exhaust events to its single chimney. Native readers still control particles and
sound; the visible Meyer return does not change compound simulation or resources.

## Requirements and installation

Developed against Derail Valley **B99 build2702**, Unity Mod Manager, Loco Mesh
Splitter and Custom Car Loader. S282 is not a runtime dependency. The authoring
workflow does need its custom Franklin source bundle; see [BUILDING.md](BUILDING.md).

The Nexus download is one ZIP to drag directly into UMM. It installs one
`RearrangedS060` mod folder, containing the runtime and the two native CCL bundles
under `assets/Meyer` and `assets/Dockside`, plus original Shay models under `assets/Shay`. There are no nested ZIPs. CCL is a
normal dependency; Rearranged S282 is not required.

If upgrading a previous three-part test install, first uninstall the old
`RS060_Meyer_0440` and `RS060_Franklin_Dockside_0440` entries through UMM. Keep
`RearrangedS060` and its settings, then install the combined ZIP. This one-time
step avoids duplicate asset IDs. See [installation instructions](docs/nexus/INSTALL.txt).

Close the game before updating. The runtime and asset bundles retain independent
version numbers. Repository development packages/installer retain their older
three-folder layout; use `Package.Nexus.ps1` for the public single-mod archive.
The repository's installer validates package hashes
and refuses a running game; it does not publish a release. The installer now
creates no backup, as requested; it records prior hashes and verifies installed
files. Old S060 backup copies have been removed. Exact cleanup and installed
state are recorded in the handover.

Stop and close the regulator before selecting a layout. Before entering or
leaving Meyer, uncouple the rear, disconnect its hose and leave clear track.
Follow [the test guide](docs/RADIO-ARTICULATED-TESTING.md) for servicing,
streaming, curves and save/reload checks.

## Development

Use `Build.Runtime.ps1` for the current DLL and `Build.Release.ps1` for a tested
complete development package, then `Package.Nexus.ps1` for the single UMM ZIP.
The project builds only the current product (Stage 7);
requesting historical stages fails explicitly.

[BUILDING.md](BUILDING.md) describes local paths, SDK/Unity dependencies, native
fixture preparation, hash-bound evidence and the separate installation step.
Native game body/wheel/texture fixtures remain in ignored local QA outputs and
are not distributed. Current packs include inherited custom Franklin/pipe meshes
and alias native S060 materials at runtime.

Seventy-four inactive source/resource/script files were moved byte-for-byte to
[archive/legacy-runtime](archive/legacy-runtime/README.md). Its manifest records
original paths and hashes. Archived scripts are reference material, not supported
build commands. Historical measurements, decisions and earlier prototype
results are retained in [docs/FEASIBILITY.md](docs/FEASIBILITY.md),
[docs/TESTING.md](docs/TESTING.md) and [docs/HANDOVER.md](docs/HANDOVER.md).

## Licence and credit

Retain the MIT copyright/licence notices in [LICENSE](LICENSE), including Peter
Schinske's original work. See [CREDITS.md](CREDITS.md) for attribution and the
Verde reference credit to **discorddraconequus**. Credits do not replace the
respective third-party asset permissions. No Verde art or audio is distributed.

Meyer refilling: use the original S060 coal and water filling points. They
serve combined stores of 3,500 kg coal and 8,800 L water, including the extra
rear capacity; no separate bogie fill point is required. This candidate corrects
the native servicing cache that previously stopped purchases at stock limits.
