# Rearranged S060 — spike tests

Covers the 0-4-0 (P1, passed 20 Oct 2026) and the 0-8-0 (P2, untested).

Find the buttons: **Ctrl+F10** → the **Mods** tab → **Rearranged S060 - P1 0-4-0
spike** → click that row to expand it. Nothing is on the comms radio at this
stage; that is P4 work. The target is whatever S060 you are looking at, else the
nearest one. Nothing is saved, so a reload always returns a loco to stock.

## P1 — 0-4-0: passed

Converted and restored cleanly, and it rode well. Two things that came out of it:

- **The bogie pivots can stay where DV put them.** This was the open question, and
  leaving them alone turned out to be fine in motion. P2 relies on that result.
- **No extra wheelslip, light engine at full regulator and full cutoff** — which is
  correct, and my earlier note in this file saying to expect easier slip was
  wrong. A tank engine carries every tonne of its 25 t on coupled axles, so total
  adhesion is the same whether that weight sits on two axles or three: `2 ×
  (M/2) × μ` equals `3 × (M/3) × μ`. Only the per-axle load changes. If you want
  to confirm nothing is broken, open the regulator the same way on a **stock**
  0-6-0 — it should not slip either. If the stock one slips and the 0-4-0 does
  not, tell me, because that would mean the coefficient write is wrong.

## P2 — 0-8-0: what to look for

Adds a fourth driver **behind** driver 1, at the cab end, 1.549 m back (car-local
Z −2.947). Backwards because the measurements say so: 1.31 m of clear low space
behind driver 1 against 1.01 m in front of driver 3, where the valve gear fills
the space to Z +3.460. Going forwards would mean moving the cylinders; going
backwards touches nothing that moves.

Nothing is extracted or imported. The new wheel is a clone of driver 2's mesh and
the rod extension is a clone of the stock coupling rod shifted back one spacing,
so its three pins land on drivers 4, 1 and 2 — the same trick the S282 mod uses
for its twelve-coupled extension. Added parts are not in the native animation
clips, so they copy an animated part's pose every frame in LateUpdate.

Axle_R goes from one axle to two, which balances the bogies 2 + 2. Its pivot stays
on driver 1 and the new axle carries the 1.549 m offset, which is the pattern P1
proved.

1. **Convert while stationary.** Expect a fourth wheel each side, behind the
   existing three, under the cab.
2. **Rod sync — the main thing.** Open the regulator gently and watch one side
   closely. The extension rod and the fourth wheel should move in lockstep with
   the stock rod and wheels, with no stutter, no lag of one frame, and no drift
   as speed rises. This is the part most likely to be wrong: if the extension
   rod jitters or sits still, the LateUpdate follower is not firing.
3. **Clocking.** The fourth wheel's crank pin should sit at the same angle as the
   others throughout a revolution. A wheel that is a quarter-turn out will be
   obvious as a rod that visibly bends.
4. **The rear end.** Look under the cab and the rear buffer beam. The new wheel
   sits where the rear coupler's trigger box is (Z −3.470 .. −4.470); it should be
   enclosed by the body shell, but check it does not poke through the frame or the
   beam, and that coupling to a wagon behind still works normally.
5. **Does it pull?** Four powered axles now. Again, expect no change in total
   adhesion — same weight, same grip — but it should pull a load happily.
6. **Brakes.** The brake shoes are a single mesh covering the three stock axles, so
   the fourth driver has no shoe at all. Cosmetic and known; just confirm braking
   still works.
7. **Ride and curves.** Points and a tight curve, both directions. Four axles in
   a 4.65 m rigid wheelbase on DV's curves is the interesting case.
8. **Restore, then convert the other way.** Restore to stock, check all four
   clones are gone and three powered axles are back, then try 0-4-0 on the same
   loco, restore again, and 0-8-0 once more. Only one arrangement can be active at
   a time and the buttons enforce it.
9. **A probe run while converted** would be genuinely useful: it would show the
   rebuilt `Bogie.axles`, `poweredAxles.NUM = 4` and the fourth axle's position,
   so I can check the state rather than infer it.

## What to tell me

The window's status line, anything in the UMM log, and for step 2 whether the
extension rod tracks cleanly. If the rod sync is wrong, that blocks every later
arrangement, so it is worth a careful look before anything else.
