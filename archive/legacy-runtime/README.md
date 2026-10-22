# Historical runtime reference

These 74 files were moved byte-for-byte out of the live source tree during the
0.15.0 audit cleanup. `moved-files.json` records their original paths and SHA256.
They include inherited S282 code, the old S060 spike/prototype entry points,
their import/resource helpers and obsolete build/validation scripts.

This is retained source reference, excluded from the current project. Historical
scripts describe earlier layouts and paths; they are not supported build commands.
Use root `Build.Runtime.ps1`, `Build.Release.ps1` and `Test.Runtime.ps1`.

Original licence and attribution notices remain applicable. See root LICENSE
and CREDITS.md. Do not copy these old entry points back into the current build
to resolve a defect without checking current native functionality first.
