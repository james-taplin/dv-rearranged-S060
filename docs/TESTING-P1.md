# Rearranged S060 — P1 spike test (0-4-0)

Build: repo `rearranged-s060`, stage 1, 20 Oct 2026.
DLL SHA-256 `7AEA354A6D56840FCD305230F64FF11B867A96EFEAE2BBF27A4C8A200A2B87A1`.
ZIP SHA-256 `70D07FD0AEE736BAF2258C45BA8A3F1CA79C0E4F580E2457D55A40336F6B60A1`.

**Not installed.** Drop the `RearrangedS060` folder into `Derail Valley\Mods\`
when you want it, or install the zip through the mod manager. It needs
LocoMeshSplitter, which you already have. It can sit alongside Rearranged S282 —
different mod Id, different assembly, and it only patches `Bogie.Axles`.

## What it does

Adds one window to the mod manager with a single button. To find it: **Ctrl+F10**
→ the **Mods** tab → scroll to **Rearranged S060 - P1 0-4-0 spike** → click that
row to expand it. The buttons are inside that expanded entry; there is nothing on
the comms radio at this stage. The window reads **CONVERT TO 0-4-0**, and **RESTORE STOCK 0-6-0** once converted. No comms
radio, no saving, no spawn hooks, nothing automatic. Target is whatever S060 you
are looking at, else the nearest one.

It drops the **middle** driver and leaves drivers 1 and 3 coupled, 3.10 m apart.
Driver 2 had to be the one to go: `Axle_R` owns only driver 1, so taking driver 1
would leave a bogie with no axles, and the main rod runs forward from driver 3.

Alongside the visuals it deactivates that axle, rebuilds `Bogie.Axles` without it,
re-registers 2 powered axles (down from 3), writes `poweredAxles.NUM`, moves all
the adhesion weight onto the two remaining drivers, and hides the middle brake
caliper.

**The bogie pivots are deliberately left where DV put them** (+0.929 and −1.396),
so the car should ride and curve exactly as stock. Whether the offset between a
pivot and its one remaining axle looks wrong in motion is the main question this
test answers.

## Known cosmetic gaps, already expected

- The coupling rod keeps its middle pin boss, now hanging in air. Correct rod
  geometry for two axles is a later job.
- The brake *shoes* are a single mesh covering all three axles, so the middle shoe
  stays even though its caliper is hidden. Only the calipers are split per axle.

## Checklist

Stand next to a spawned S060, cold engine is fine.

1. **Convert while stationary.** Press the button. Expect: middle wheel gone both
   sides, drivers 1 and 3 still rodded together, no console errors.
2. **Rods and animation.** Open the regulator gently and watch both sides. The
   valve gear and the two remaining wheels should turn together and stay in time.
   Watch for a wheel that spins at the wrong rate — that would mean the radius or
   circumference write went wrong.
3. **Does it pull?** It should still move, with noticeably less grip: two driven
   axles carrying the same 25 t. Expect easier wheelslip, which is correct, not a
   bug.
4. **Ride and curves.** Drive it over points and through a tight curve. This is
   the real question: does it sit and track properly with a pivot offset from its
   axle, or does it look like it is riding on nothing?
5. **Brakes.** Apply the independent brake. The remaining shoes should still glow
   and slow it.
6. **Restore.** Press RESTORE. All three drivers back, three powered axles, grip
   back to normal. Then convert and restore a second time — the state bookkeeping
   is per car and should survive a round trip.
7. **Guards.** Try converting while rolling, and with the regulator open. Both
   should refuse with a message in the window rather than doing anything.
8. **Leave and return.** Walk away far enough to unload the interior, come back,
   check it is still an 0-4-0 and still restorable. Nothing is saved to the save
   file, so a reload returns it to stock — that is expected at this stage.

## What to tell me

The window's status line and anything in the UMM log. If something looks wrong,
the console text plus which checklist step it was. If step 4 looks fine, P2 (the
0-6-2 trailing truck) is unblocked; if the pivot offset looks bad, the next job is
moving the bogie pivot onto its axle instead.
