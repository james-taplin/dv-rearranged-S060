> Working copy: `Desktop\Derail Valley Mods\RearrangedS060\FEASIBILITY-TENDER.md`.

# Stretch goal — cut the bunker off and give the S060 a tender

Claude, 20 Oct 2026. Question: can the back and bunker be cut off the S060's cab
and a small sloped tender added, to give it a switcher / camelback silhouette?

**Verdict: the cutting is cheap and already half-done for us; the tender is a
separate car and that is where all the cost is.** Three tiers below, in rising
order of work. Tier 2 is the one I would recommend: it gets the switcher
silhouette without a new car, because the bunker is only 0.87 m deep and removing
it does not free up room for a tender anyway.

A *true* camelback — cab astride the boiler — is the one thing that is not
feasible cheaply, and the reason is in §4.

Evidence: mesh figures measured from
`Derail Valley\Mods\LocoMeshSplitter\assets\s060_body.obj` (the vanilla mesh the
splitter ships, extracted from the game); probe figures from the P0 run
`RearrangedS060Probe-report-20261020-011059.txt`, line references into that file.
Car-local space, **+Z forward, cab at −Z**.

## 1. The bunker is already a separate renderer — removing it is a one-liner

LocoMeshSplitter 1.1.9 splits the S060 body into ~50 named child renderers, and
the rear end is among them (probe `:376`, `:322`, splitter methods at `:904`–`:941`):

| Part | Method |
|---|---|
| `s060_bunker` | `GetBunkerMesh` |
| `s060_tank_l`, `s060_tank_r` | `GetTankLMesh`, `GetTankRMesh` |
| `s060_tank_floor`, `s060_tank_mount`, `s060_tank_mount_upper_mount`, `s060_tank_mount_lower_mount` | one method each |
| `s060_rear_subframe_left/right`, `s060_bumper`, `s060_taillight_upper`, `s060_taillight_lower_left/right` | one method each |

Both LOD0 and LOD1 are split (`S060LOD0MeshSplitter` / `S060LOD1MeshSplitter`),
and with the splitter installed the vanilla `s060_body` and `s060_body_LOD1`
renderers are **deactivated** in favour of `s060_split_body(Clone)` and
`s060_split_body_LOD1(Clone)` (probe `:351`, `:352`, `:410`, `:414`). So hiding the
bunker is `renderer.enabled = false` on one object per LOD — no mesh surgery, no
new art, and the rear buffer beam, subframe and tail lights survive because they
are their own parts.

Two bounded consequences, both measured:

- **LOD2 and LOD3 are not split.** `s060_body_LOD2` and `s060_body_LOD3` are
  single active meshes (probe `:406`, `:412`), so the bunker reappears as the car
  LODs out. Either accept the pop or do our own triangle cut on those two meshes
  (§2 does it anyway).
- **The split remainder already stops at Z −3.800** (probe `:414`), while the
  bunker part runs back to −4.364. So hiding `s060_bunker` removes roughly
  **Z −4.36 .. −3.80**, about 0.56 m, and leaves the cab rear wall and the ~0.3 m
  of structure in front of it standing. That is the half-done part: the game has
  effectively pre-cut the bunker box for us, but not the wall.

## 2. How much "back" there is to cut: 0.87 m

Every vertex in the body mesh behind the cab rear wall (Z ≤ −3.49) lies between
**Z −4.364 and −3.500** — 0.87 m — spread over X ±1.403, Y 0.790 .. 3.870. Broken
down by height:

| Y band | verts | what it is |
|---|---|---|
| 0.79 – 1.35 | 709 | rear buffer beam / coupler pocket |
| 1.35 – 2.50 | 2113 | bunker sides and back sheet |
| 2.50 – 3.00 | 174 | bunker coaming |
| 3.00 – 3.87 | 926 | cab roof overhang |

(3,922 verts of 56,048; measured from `s060_body.obj`.)

So **cutting the back off buys 0.87 m at most, and the loco stays 8.46 m long.**
That is the number that decides the whole question: a slope-back tender is 3–4 m,
the rear coupler box sits at Z −3.470 .. −4.470 (probe `:640`–`:724`), and there is
nowhere on this car to put one. Anything tender-shaped has to be its own car.

The cab rear wall itself (windows `s060_cab_window_RL/RR` at Z −3.487 .. −3.479,
probe `:674`–`:677`) is **not** a splitter part — it is inside the remainder
`s060_body` mesh. Cutting it means filtering triangles, which is exactly what
LocoMeshSplitter does: axis-aligned `RangeFloat` limit boxes per part. We can run
the same filter ourselves on the remainder mesh with a box of roughly
**X ±1.41, Y 1.35 .. 3.00, Z ≤ −3.46**, which from the table above takes the wall
and the bunker coaming while leaving the buffer beam and the roof. Two routes,
both open:

- **runtime triangle filter** — our own cut, applied to all four LODs, works with
  or without LocoMeshSplitter installed (it only has to find whichever body
  renderer is live). Uniform, no art pipeline, reversible per arrangement.
- **shipped replacement mesh** — author the cut in Blender from the extracted
  `s060_body.obj` and ship it in the AssetBundle, which this repo's pipeline
  already does (README, "Repackaging custom meshes"). Cleaner result, but it
  fights LocoMeshSplitter over who owns the body mesh.

I would take the runtime filter.

### Tier 2, the recommendation: cut it down, do not cut it off

Keep the cab rear wall and its windows, delete the bunker's upper back and sides,
and drop a short sloped deck in behind the cab. That is the USRA 0-6-0 /
slope-back-tender switcher silhouette, built inside the existing car:

- it needs **one small custom mesh** (a sloped deck about 2.8 × 0.8 m), through the
  pipeline the repo already has;
- the coal pile stays where it is and keeps working. This matters: the game's own
  interior hierarchy calls the bunker **`Tender`**, with `Shovel slide helper top`
  at Z −3.530 and the `Coal` pile and its `ShovelCoalPile` trigger at Z −3.660
  (probe `:2508`–`:2509`, `:3152`–`:3154`). Delete the bunker outright and that
  coal pile is left floating in the open behind the cab;
- no sim work at all (§3);
- the interior's own rear-wall colliders (`Cube (57)`–`Cube (60)` at Z −3.471,
  probe `:2500`–`:2503`) can be left alone, so the player still cannot walk out of
  the back of a cab whose wall we kept.

The honest cost is that 0.87 m reads as a *very* short slope-back. It is a
silhouette change, not a scale-correct tender.

## 3. The sim is free — the S060 carries its own coal and water

`[sim]/coal` is 1,200 kg and `[sim]/water` 4,500 L, both `CoalContainerDefinition`
/ `WaterContainerDefinition` on the loco itself, with no tender car anywhere in the
graph (probe `:1298`, `:1295`, `:1394`–`:1398`, `:1532`–`:1536`). So **a tender,
whether grafted or separate, needs no simulation work whatsoever** — it is
decoration, the loco keeps its own fuel, and the player keeps filling it from the
cab. That removes the largest cost I expected to find here.

One loose end if the side tanks are hidden too: the water-fill gadget colliders
`[GadgetCollider][water_tank]` stay where they are (probe `:2375`–`:2377`,
`:2688`–`:2690`), so the refill point would hang in mid-air. Invisible and
harmless, but it is there.

## 4. A real camelback: not feasible cheaply

A camelback puts the cab astride the boiler. On this car the cab occupies
Z −3.487 .. −1.313 at Y 2.399 .. 3.695 (probe `:246`), and:

- the cab **shell** is inside the remainder `s060_body` mesh — the splitter exposes
  no cab part (methods, probe `:904`–`:941`), so moving it means authoring a new
  body mesh, not toggling renderers;
- the whole interior — controls, levers, firebox, coal pile, player and item
  colliders, `s060_body_player_collider` — is laid out against the cab's present
  position. Moving the cab forward ~2.5 m means moving all of it and re-cutting
  the player collider.

So a camelback is a custom-body job. If that is where this is heading, it belongs
in the CCL pipeline as a new car, not in a runtime mod on the stock S060.

## 5. Tier 3: a real tender, as a CCL car

If the short slope-back of Tier 2 is not enough, the scale-correct version is a
**separate CCL car**, and the toolchain for it already exists in this workspace:
the LLW conversion pipeline builds loco + tender pairs as CCL cars (e.g.
`PROTO5AT_tender_template` in
`B:\LLW CONVERT\analysis\bogie-smoothing-rebuild-20261011\unity-visual-validation\export_models.py:89`,
and the pilot audit's "tender placed at z −7.445" spacing check). A short
slope-back tender is a small car by that pipeline's standards.

What it would take, and what it would not:

- **not needed:** fuel plumbing, auto-spawn pairing, sim links (§3). The player
  couples it by hand like any car.
- **needed:** the car itself (body, bogie, couplers, buffers, brake rigging — CCL's
  job), a matching paint, and spawner/radio availability.
- **the mod's side is already written, in spirit.** The inherited Erie-triplex code
  does exactly this detection: it watches `car.rearCoupler.coupledTo.train` every
  physics tick, identifies the car type, parents a new root to it, clones meshes
  onto it, hides the host's own objects in a `tenderHidden` list and tears the lot
  down on uncoupling, switching, derailment or destruction
  (`RearrangedS060/WheelRearranger.TenderEngine.cs:37`–`:80`). Re-pointing that at
  "my slope-back tender is coupled behind, so hide the bunker" is a small edit to
  a pattern that is already proven.

A fourth option — graft the tender mesh onto a *stock* coupled car and hide its
body — works with the same machinery, but no stock car is the right length, so
there would be a visible gap between the little tender and the first wagon. I
would not do it.

## 6. What is not yet measured

Three cheap things, all probe additions, none blocking the verdict:

1. **The splitter's cut boxes.** `bunkerLowerLimitX/Y/Z`, `bunkerUpperLimit*`,
   `tankLimit*` etc. are private `RangeFloat` fields (probe `:764`–`:878`); the
   probe dumped their *names* but not their *values*. Reading them by reflection
   gives us the exact boxes to reuse for our own rear-wall cut instead of the
   approximate one in §2.
2. **Behaviour with LocoMeshSplitter absent.** Every figure here is from a car with
   it installed. The mod must find the live body renderer either way; untested.
3. **What the cut actually looks like.** Hiding `s060_bunker` and the tanks is a
   five-minute experiment and would show at once whether the boiler and frame are
   modelled behind the tanks or whether removing them opens holes. Nothing in the
   probe answers that — it needs eyes in game.

## 7. Suggested staging

- **T1** — hide `s060_bunker`, then the tanks and tank mounts, from the existing P1
  mod-manager window. One session's work, no art, answers §6.3 and shows whether
  the look is worth chasing.
- **T2** — the recommendation: runtime triangle cut of the cab rear wall and bunker
  coaming across all four LODs, plus one sloped-deck mesh through the AssetBundle
  pipeline. The switcher silhouette, in one car, coal shovelling intact.
- **T3** — only if T2's 0.87 m reads too stubby: a short slope-back tender as a CCL
  car, with the mod hiding the bunker when it sees that car coupled behind.

Where this sits against the wheel-arrangement work: T1/T2 touch only renderers and
one mesh, so they are independent of P2/P3 and could land in any order. Note that
T2's cut is **behind** driver 1 in the same Z band as the P2 trailing truck
(1.31 m of clear low space behind the rear driver, main feasibility doc §3) — the
sloped deck sits above it at Y 1.35+, so the two do not collide, but a 0-6-2 with
a slope-back deck wants checking in one go rather than separately.

Nothing has been changed in the game by this assessment, and nothing has been
built.
