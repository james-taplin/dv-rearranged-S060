# S282 branch-pipe source data

These are readable authoring copies of the three custom meshes in the existing
Rearranged S282 `branch_pipe` prefab: intake_manifold_f, intake_manifold_r and
dry_pipe_new. They are reused under this fork's existing LICENSE; they contain
no Derail Valley meshes, textures, materials, audio or game code.

The static source bundle disables CPU access. Prepare.BranchPipeSources.py reads
its serialized custom geometry with the existing local UnityPy authoring tool;
SteamPipeAuthoring reconstructs readable Unity Mesh assets and checks the source
bundle SHA256. Runtime uses the resulting meshes in the S060 packs and has no
S282 DLL or installed-asset dependency. Original bundle files are read only.

Source bundle SHA256:
44A1B9EAE0DDA58BE97B9AEDF3877522FBFFAEC060FE02A8385158E095BDB22D

Native S060 preview fixtures under build/radio-0.11.0 are separate local evidence.
They are not part of this source data or any package.
