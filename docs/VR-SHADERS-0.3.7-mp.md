# VR running-gear shaders — staged 0.3.7-mp, 19 November 2026

James reported that in VR the modded textures, including all of the Shay's
new running gear, look wrong and shift as the head moves. He traced it back to
0.3.3 and asked for the fix to be included in a staged 0.3.7-mp alongside the
existing 0.3.6/0.3.7 fixes. **Staged only, not installed.**

## Cause

Each of the three S060 asset bundles carries its own copy of Unity's built-in
Standard shader, compiled in the project that built the bundle. None of those
projects had VR enabled, so Unity stripped the single-pass stereo variants.
Derail Valley renders VR in single-pass stereo. Its own shaders all include
`UNITY_SINGLE_PASS_STEREO`, and `resources.assets` ships Standard with it.
Without those variants, a material samples with one eye's transform for both
eyes, so textures appear to swim with head movement.

Evidence, read with UnityPy from the shipped bundles:

| Bundle | Standard has `UNITY_SINGLE_PASS_STEREO` |
|---|---|
| `assets/Shay/shay_running_gear` (since 0.3.x) | no |
| `assets/Meyer/ccl_bundle` | no |
| `assets/Dockside/ccl_bundle` | no |
| Installed working CCL loco, Class 20 `ccl_bundle` | **yes** |
| Game `resources.assets` Standard | **yes** |

A probe in Unity 2019.4.40f1 built the same Standard material twice. With VR
off, the shader matched the Shay bundle's (307,239-byte typetree, no stereo).
With single-pass VR on, it gained the stereo variants (528,923 bytes).

## Fix

Re-authoring Meyer or Dockside would also bake current runtime geometry into
their bundles, so all three existing bundles are left byte-identical.

- `Build.StereoShaders.ps1` and `Shared/Authoring/StereoShaderBuild.cs` build
  `assets/Shared/s060_stereo_standard`. It holds one material with Standard,
  compiled with single-pass VR enabled in the same Unity 2019.4.40f1.
  `Shared/tools/Check.StereoShader.py` fails the build unless the shader
  has the stereo variants.
- `S060StereoShaders` loads that shader once and keeps a static reference so
  `UnloadUnusedAssets` cannot drop it. When the Shay prefab or a Meyer/Dockside
  donor prefab is first resolved, it collects that bundle's Standard shader
  instance and re-points every loaded material using it. Keywords and render
  queue are preserved; textures and properties stay on the material. The
  property interface of the new shader is identical to all three bundled copies.
  Shay finish copies made from these materials inherit the new shader.
- The multiplayer build identity now includes the new bundle, so peers with
  the earlier 0.3.7-mp package are detected as different.

If the shader bundle is missing, the original shaders are kept and a log line
explains why. Logging is null-safe, so that fallback also holds outside UMM
(editor fixtures), as found by the 0.4 port.

## Package and validation

- ZIP: `build/release-0.3.7-mp-vr-20261119/package/RearrangedS060-0.3.7-mp-UMM.zip`
- ZIP SHA256: `B0B668D80358B7095B27221C6987C0C0F4DAB8ABB284FFDF58C2E5A7E013123B`
- Core: `D38CED73DFB2D5BD81A8817C1189B1A5987CAAAE7BED715D45A6DA35930FF8EE`
- Adapter: `7867E387F6716390189E85FD8458CC8285EB6FFE0481D96EC74411831F5F339F`
- Stereo bundle: `BE6B565D1AD26E946EE7ACA65927FB344D1449760BD4E9EEBAD39B757C2FE9F5`

The package has 15 files: the previous 14 plus the stereo bundle. The Shay,
Meyer and Dockside bundles are unchanged, so their reopening evidence is
retained.

- Production build: zero warnings, zero errors.
- Native UMM fresh install, update, uninstall and 0.3.0 migration on the
  exact ZIP: **69 assertions pass** (`nexus/umm-proof-20261119-093149`).
- Managed multiplayer: bridge 219, core 49, ports 32 pass.
- Native cleanup audit on the staged core D38CED73: **145 checks pass**
  (`build/radio-0.3.7-mp/runtime-audits-20261119-105002/cleanup`). Its
  synthetic rod fixture now carries UVs like native rods, matching 0.4; before
  this, the audit threw on that fixture with every core since a5cc9f7.

**Not proven offline:** that the swap renders correctly in VR, or exactly
which materials it reaches in the running game. An editor fixture could not
enumerate the bundles' materials, so it does not stand in for the player. The
Player.log line `S060 VR shaders: N bundle materials ...` reports the count in
game. James must confirm in VR, desktop look included.

No install, game launch, push or publication. The installed game currently has
the separate 0.4.6.3-indev; the 0.4 line still needs the same fix.
