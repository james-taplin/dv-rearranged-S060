using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RearrangedS060
{
    // Refit the existing engine, not its gear train or mount. Tall jackets have
    // room for the longer stroke after shortening their internal head spigots.
    // Every edit is an owned mesh copy; the bundle and shared native assets stay intact.
    internal sealed class S060ShayPowerGeometry : IDisposable
    {
        internal const float ValveShift = -.025f;
        private readonly List<(MeshFilter filter, Mesh source, Mesh copy)> owned = new List<(MeshFilter, Mesh, Mesh)>();
        private bool disposed;
        internal static S060ShayPowerGeometry Apply(Transform engine)
        {
            var owner = new S060ShayPowerGeometry();
            try
            {
                int pins = 0, cheeks = 0, castings = 0, spigots = 0, pistons = 0;
                foreach (var f in engine.GetComponentsInChildren<MeshFilter>(true))
                {
                    string name = f.name;
                    bool valve = name.StartsWith("Valve_chest_", StringComparison.Ordinal) || name.StartsWith("Valve_cover_", StringComparison.Ordinal) || name.StartsWith("Valve_gland_", StringComparison.Ordinal);
                    bool steamBranch = name.StartsWith("Steam_branch_", StringComparison.Ordinal);
                    bool exhaustBranch = name.StartsWith("Exhaust_branch_", StringComparison.Ordinal);
                    if (valve || steamBranch || exhaustBranch)
                    {
                        var original = f.sharedMesh;
                        if (original == null || !original.isReadable) throw new InvalidOperationException("Readable Shay valve source required.");
                        var fitted = Object.Instantiate(original); fitted.name = original.name + " (Shay valve clearance)";
                        var v = original.vertices; var n = original.normals;
                        for (int k = 0; k < v.Length; k++)
                        {
                            float y = v[k].y;
                            float shift = exhaustBranch ? .01f : ValveShift, height = exhaustBranch ? .1f : .09f;
                            v[k].x += valve ? ValveShift : shift * Mathf.Clamp01((1.72f - y) / height);
                            // The header endpoint stays fixed; shear only the
                            // existing diagonal branch towards the moved chest.
                            if (!valve && n.Length == v.Length && y > 1.72f-height && y < 1.72f)
                                n[k] = new Vector3(n[k].x, n[k].y + shift / height * n[k].x, n[k].z).normalized;
                        }
                        owner.owned.Add((f, original, fitted)); fitted.vertices = v;
                        if (n.Length == v.Length) fitted.normals = n;
                        fitted.RecalculateBounds(); f.sharedMesh = fitted;
                        continue;
                    }
                    bool guide = name.StartsWith("Guide_(", StringComparison.Ordinal);
                    bool yoke = name.StartsWith("Guide_lower_yoke_", StringComparison.Ordinal);
                    bool rearTie = name.StartsWith("Guide_upper_tie_(", StringComparison.Ordinal) && name.EndsWith(", -1)", StringComparison.Ordinal);
                    if (guide || yoke || rearTie)
                    {
                        var original = f.sharedMesh;
                        if (original == null || !original.isReadable) throw new InvalidOperationException("Readable Shay guide source required.");
                        float centre = original.bounds.center.z + (rearTie ? .04f : 0);
                        // Clear the complete rigid rod's offset plane and wrist
                        // eye while retaining front/back guide-bearing faces.
                        var sections = rearTie ? new[] { new Vector2(centre - .08f, original.bounds.max.z) }
                            : new[] { new Vector2(centre - .08f, centre - .049f), new Vector2(centre + .025f, centre + (guide ? .05f : .056f)) };
                        var fitted = Rebox(original, sections);
                        owner.owned.Add((f, original, fitted)); f.sharedMesh = fitted;
                        continue;
                    }
                    bool pin = name.StartsWith("Crank_pin_", StringComparison.Ordinal);
                    bool cheek = name.StartsWith("Crank_cheek_", StringComparison.Ordinal);
                    bool casting = name.StartsWith("Cylinder_casting_", StringComparison.Ordinal);
                    bool lower = name.StartsWith("Lower_head_spigot_", StringComparison.Ordinal);
                    bool upper = name.StartsWith("Upper_head_spigot_", StringComparison.Ordinal);
                    bool piston = name.StartsWith("Piston_", StringComparison.Ordinal) && !name.StartsWith("Piston_rod_", StringComparison.Ordinal);
                    if (!(pin || cheek || casting || lower || upper || piston)) continue;
                    var source = f.sharedMesh;
                    if (source == null || !source.isReadable) throw new InvalidOperationException("Readable Shay authoring mesh required: " + name);
                    var points = source.vertices;
                    float cylinderZ = casting || lower || upper ? source.bounds.center.z : 0;
                    float delta = S060SteamCalibration.Shay.CrankRadius - .075f;
                    for (int i = 0; i < points.Length; i++)
                    {
                        var p = points[i];
                        if (pin || (cheek && p.y > .024f)) p.y += delta;
                        if (casting || lower || upper || piston)
                        {
                            float centreX = piston ? 0 : -.065f;
                            var radial = new Vector2(p.x - centreX, p.z - cylinderZ);
                            float old = radial.magnitude;
                            float before = casting ? .085f : piston ? .083f : .0848f;
                            float after = S060SteamCalibration.Shay.Bore / 2 - (casting ? 0 : piston ? .002f : .0002f);
                            // End annuli and cylindrical walls share these exact rings.
                            // Keep the outer jacket (.118 m), gland and piston rod fixed.
                            if (Mathf.Abs(old - before) < .00001f)
                            { radial *= after / old; p.x = centreX + radial.x; p.z = cylinderZ + radial.y; }
                            if (lower) p.y = 1.18f + (p.y - 1.18f) * (.05f / .13f);
                            if (upper) p.y = 1.59f + (p.y - 1.51f) * (.07f / .15f);
                        }
                        points[i] = p;
                    }
                    var copy = Object.Instantiate(source); copy.name = source.name + " (Shay authored power fit)";
                    owner.owned.Add((f, source, copy)); copy.vertices = points;
                    // All edits preserve axis-aligned faces or translate radial rings;
                    // preserve the authored hard/smooth normals, UVs and material slots.
                    copy.RecalculateBounds(); f.sharedMesh = copy;
                    if (pin) pins++; if (cheek) cheeks++; if (casting) castings++; if (lower || upper) spigots++; if (piston) pistons++;
                }
                if (pins != 6 || cheeks != 12 || castings != 6 || spigots != 12 || pistons != 6)
                    throw new InvalidOperationException("Both Shay LODs must receive the complete power fit: " + pins + "/" + cheeks + "/" + castings + "/" + spigots + "/" + pistons);
                return owner;
            }
            catch { owner.Dispose(); throw; }
        }
        private static Mesh Rebox(Mesh source, Vector2[] sections)
        {
            var copy = Object.Instantiate(source); copy.name = source.name + " (Shay open motion guide)";
            int count = source.vertexCount; var before = source.vertices;
            var vertices = sections.SelectMany(section => before.Select(p => new Vector3(p.x, p.y,
                Mathf.Lerp(section.x, section.y, (p.z - source.bounds.min.z) / source.bounds.size.z)))).ToArray();
            copy.vertices = vertices;
            copy.normals = sections.SelectMany(_ => source.normals).ToArray();
            copy.uv = sections.SelectMany(_ => source.uv).ToArray();
            if (source.tangents.Length == count) copy.tangents = sections.SelectMany(_ => source.tangents).ToArray();
            for (int sub = 0; sub < source.subMeshCount; sub++)
            { var triangles = source.GetTriangles(sub); copy.SetTriangles(Enumerable.Range(0, sections.Length).SelectMany(i => triangles.Select(t => t + i * count)).ToArray(), sub); }
            copy.RecalculateBounds(); return copy;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            foreach (var item in owned)
            {
                if (item.filter != null) item.filter.sharedMesh = item.source;
                if (item.copy != null) { if (Application.isPlaying) Object.Destroy(item.copy); else Object.DestroyImmediate(item.copy); }
            }
            owned.Clear();
        }
    }
}
