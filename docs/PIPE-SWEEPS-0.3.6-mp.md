James approved these renders afterward and authorized full articulation checks,
a local commit and staging. See [FINAL-STAGE-0.3.6-mp.md](FINAL-STAGE-0.3.6-mp.md).
The preview-only status below is historical.

# Pipe sweep preview — staged only, 15 November 2026

James requested one last visual repair for 0.3.6-mp: reduce the abrupt Shay
smokebox entry turn and the Meyer rear cab-roof downpipe turn. He requested
renders before committing and then explicitly said **stage only**.

Current source is `current-0.3.x/source`. The changes are uncommitted. No package
was installed, no game was launched and no source was pushed or published.
The installed 0.3.6-mp DLL remains SHA256
`654B476F40C99C45A4C3221BC9A09454A371CE85F95CD9EA5C180EBD9A6FD4D3`.

## Geometry

- Shay's upper exhaust course now approaches the smokebox along the body
  earlier. It enters on a 45-degree diagonal rather than overshooting the
  socket and turning back toward a radial entry. Its existing flange follows
  the new pipe axis. A short swept branch retains the original compressor
  takeoff connection. Hollow walls, existing materials and disposal ownership
  use the existing pipe builder; the Shay bundle does not change.
- Meyer's rear upper trunk now rises over a longer diagonal course in profile.
  It remains outside the cab sheet before turning in over the roof. The two
  roof saddles move onto the new straight roof course. The lower union, flexible
  connection and requested passage through both native step brackets remain.

Only S060ShayPlumbing.cs and S060MeyerExhaustPipe.cs change production behavior.
Native geometry, simulation, saves, networking and asset bundles are untouched.
The two existing native audit files add preview views and focused clearance
checks. Preview.PipeSweeps.ps1 reuses the existing private Unity fixtures.

## Evidence and limits

`build/pipe-sweeps-0.3.6-mp/before` contains renders of the installed DLL.
`after` is the first candidate render; `checked` contains the same candidate
after focused clearance checks were added to the fixtures. These are actual
Unity renders using local native body meshes and production pipe code.

One production build, zero warnings/errors. Candidate core SHA256:
`696DA42DC9B1E73D9CDC5361D74F1625FC0377CB05399F53B622948A7DBB9662`.

Shay's preview suite passed 722 checks, including native body/engine/frame
clearance for its pipes and the new branch, original compressor takeoff
continuity, branch-to-main continuity, material recovery and body restoration.
Meyer's focused preview checks confirm upper rear pipe clearance against the
actual native cab/roof triangles and contact with both native step brackets.
The original preview helper was baselined against the installed DLL first.

This is a static visual candidate. The complete articulation sweep, full release
regression and multiplayer acceptance were not rerun. No gameplay acceptance
is claimed. Do not install or commit without new authorization. Keep the
candidate separate from the existing released package despite the same version.

Repeat the preview with:

    .\Preview.PipeSweeps.ps1 -RuntimePath build\pipe-sweeps-0.3.6-mp\candidate\RearrangedS060.dll -OutputDirectory build\pipe-sweeps-0.3.6-mp\review-next

The final receipts and source hashes are in
`build/pipe-sweeps-0.3.6-mp/staging-receipt.json`.
