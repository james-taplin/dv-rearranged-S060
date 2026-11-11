# Shay R3 editable model source

Original parametric engine, truck and transmission sources adapted from the R2
authoring delivery. `r2-source-provenance.json` records the unchanged LLW inputs.
The LLW delivery remains intact. No native DV geometry or textures belong here.

The R3 contract is `shared/fit-contract.json`. The model uses native DV pony
wheelsets, scaled only radially to 0.80 m rolling diameter, and 86:20 gears.
The three-cylinder engine is authored at 252 mm bore and 300.808 mm actual stroke.
Generated interchange and Unity files stay in ignored authoring subdirectories.
Use the repository's `Build.Shay.ps1` and `Shay/tools/Validate.R3.py`. The copied
R2 validator supplies the proven geometry/Cardan functions; its standalone
historical fixture entry point is not the R3 acceptance test.

See `docs/shay/R3-DETAIL-PASS.md` for the finished fit, material policy,
candidate receipts and remaining James-owned game testing.
