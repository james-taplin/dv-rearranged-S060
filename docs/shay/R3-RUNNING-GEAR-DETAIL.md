# Shay visible running gear and front compressor â€” 11 November 2026

This follows the R3 pipe/engine pass. James reiterated that visible brakes,
suspension and a convincing chassis were still the main priority. He then
preferred coil springs to leaf springs. The resulting model uses coils.

## Visible changes

- Sixteen large coils in eight paired spring packs, above the wheel silhouette,
  with lower saddles, upper seats, guide rods, horn guides and supporting posts.
  The journals remain inboard to clear the bevel gears and longitudinal drive.
- Eight substantial curved tread shoes, separate cast backs, hanger eyes/pins,
  braced hanger brackets, cross beams, pull links and equalizers. The shoes face
  inward between each truck's two wheelsets, clearing the original cab-end frame
  and front structure. They show a released running clearance.
- One modelled brake cylinder per truck, with end flanges, piston rod,
  crossshaft and reach rods connected to the equalizers.
- Tapered bolster cheeks and deeper rigid chassis channels with separate
  flanges and rivets, extending through the rear as well as the front bay.
- A steam-driven air compressor behind the front buffers on the engine side:
  separate steam/air barrels, cooling ribs, head studs, piston neck, columns,
  intake filter, governor and service unions. Two knees seat its foot on the
  actual retained frame. Its auxiliary steam feed and exhaust tee into the
  existing large courses; the air delivery reaches the existing offside tank.
  Pipe-support feet seat on the new chassis channel and carry diagonal stays.
- Separate dark castings, lighter spring steel, oily machined components and
  subdued copper/brass service work; no native body atlas on custom geometry.

This is model detail. Native brake/compressor operation and the existing physics
remain responsible for the locomotive. Springs and brake rigging follow their
truck; no new independent spring, brake, compressor or capacity simulation was
introduced. Dimensions, ratio, engine motion, save identities and servicing
quantities remain the R3 specification. Native wheel meshes/UVs/materials remain
live references.

## Fit decisions and evidence

The first attempted outboard journals interfered with the right-side bevels,
shaft and universal joint. They were withdrawn in favour of raised inboard
spring packs. The enlarged native axle needed 112 mm journal/cap bores; the
previous 85 mm source bore intersected it. These are actual mesh checks, not an
assumption from the wheel bounds. The axle is already present as long native
triangles between its end vertices; no duplicate custom axle was retained.

The inward-facing shoes avoid the real end-frame contacts found with larger
outer shoes. The outer bolster cheeks were lowered to clear the fixed sill in
compound suspension poses. The air delivery crosses below the sills, and the
left chassis channels clear the existing oil-cup shelves. Pipe stays clear the
compressor barrels and are attached to supported feet.

`Shay/tools/Validate.Details.py` uses the existing exact-export SAT functions:
two LODs/eight wheel phases, plus 49 discrete yaw/pitch/roll/heave poses per
truck against the fitted native body and rigid equipment. Four real mounting
seats must intersect their supporting frame. Other equipment contact below
the small knee mounting seat is prohibited. Native clipping leaves some
collapsed triangles with duplicate edges shorter than two micrometres; those
are excluded by the fixture, without altering production geometry.

Current detail report: `build/shay-r3-runninggear-20261111/details-validation-final.json`
passes 24,472 checks, zero unwanted contacts. The source engine/gear checks and
the transmission/truck assembly pass (33,789 checks) are recorded alongside the final
candidate. The final source engine/gear check passes 11,322 checks; the reopened
original-only bundle/native preview passes 42 material, alias, relief and restore
checks. Final bundle: `build/shay/bundle-20261111-224213`; preview:
`build/shay-r3-20261111/native-preview-20261111-224305`.
These are sampled offline checks, not continuous-sweep or gameplay acceptance.

The native preview loads the reopened original-only bundle and the same validated
runtime as the preceding R3 pass. It includes bogie and compressor close-ups.
The earlier engine/cock evidence applies to unchanged code/engine geometry;
it is not represented as a newly executed full suite against this model revision.

The native assembly fixture now recognises only two small physical auxiliary
compressor tee regions in addition to the original engine/smokebox flange ports.
It still clips actual triangles, rather than exempting entire pipe bounds.

## Delivery

The candidate is staged at `build/shay-r3-runninggear-20261111/candidate`, with
the matching original asset, unchanged validated runtime, hashes and evidence.
It supersedes the preceding model candidate for this detail work. No install,
game launch, push or publication. James owns Derail Valley testing, including
appearance in game lighting, curve articulation and brake/compressor operation.

This is a Shay model candidate, including the prior integrated unreleased
Meyer/physics runtime; it is not a complete UMM/Nexus release package.

Local source checkpoint branch: `codex/shay-running-gear-checkpoint-20261111`,
based on the prior R3 checkpoint. The active branch and index remain unchanged.
