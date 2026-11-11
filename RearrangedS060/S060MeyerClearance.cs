using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RearrangedS060
{
    // Local relief proven necessary by the inward native-cylinder/body contact
    // audit. Per-car readable copies only; native casting dimensions stay exact.
    // Box subtraction and welded-island handling follow S060ShayBody's method.
    internal sealed class S060MeyerClearance : IDisposable
    {
        private readonly List<(MeshFilter filter, Mesh source, Mesh copy)> owned = new List<(MeshFilter, Mesh, Mesh)>();
        private readonly List<(MeshFilter filter, int[] triangles)> covers = new List<(MeshFilter, int[])>();
        internal IEnumerable<(MeshFilter filter, int[] triangles)> CoverTriangles => covers;
        private struct Vertex
        {
            internal Vector3 P, N; internal Vector2 U; internal Vector4 T;
            internal Vertex(Vector3 p, Vector2 u, Vector3 n, Vector4 t) { P = p; U = u; N = n; T = t; }
        }
        private const float FrameInner = .44f, FloorInner = .60f, CoverInner = .55f;
        internal static Bounds Notch(int side) => Shifted(Box(new Vector3(side > 0 ? FloorInner : -1.50f, .30f, -1.40f), new Vector3(side > 0 ? 1.50f : -FloorInner, 1.68f, .40f)));
        // Access for the existing HP manifold and its outward transfer elbow.
        // Opening follows the measured fitting section plus the +/-6-degree
        // bogie sweep; it is a purposeful pipe passage, not a sealed hood.
        internal static Bounds PipeAccess(int side) => Shifted(Box(new Vector3(side > 0 ? .48f : -1.34f, 1.54f, -1.08f), new Vector3(side > 0 ? 1.34f : -.48f, 1.72f, .46f)));
        // Cabward casting vertices sweep no farther inboard than |X| .473853.
        // Retain the .88 m central spine and trim only the old
        // outboard frame inside the inward engine's measured local envelope.
        internal static Bounds FrameRelief(int side) => Shifted(Box(new Vector3(side > 0 ? FrameInner : -1.60f, .20f, -1.35f), new Vector3(side > 0 ? 1.60f : -FrameInner, 1.60f, .40f)));
        private static Bounds Shifted(Bounds bounds) { bounds.center -= Vector3.forward * S060MeyerLayout.FrontCylinderShift; return bounds; }
        private static Bounds Box(Vector3 lo, Vector3 hi) { var b = new Bounds(); b.SetMinMax(lo, hi); return b; }
        internal static S060MeyerClearance Apply(Transform body)
        {
            var owner = new S060MeyerClearance();
            try
            {
                // LMS extracts the low tank rails and floor from s060_body.
                // Trim those original instances too; covers belong only to the
                // main shell so each LOD has exactly one pair, not four copies.
                foreach (var filter in body.GetComponentsInChildren<MeshFilter>(true).Where(f =>
                    (f.name == "s060_body" || f.name == "s060_tank_l" || f.name == "s060_tank_r" || f.name == "s060_tank_floor") &&
                    f.sharedMesh != null && f.sharedMesh.isReadable))
                {
                    var source = filter.sharedMesh; var local = source.vertices.Select(p => body.InverseTransformPoint(filter.transform.TransformPoint(p))).ToArray();
                    var uv = source.uv; if (uv.Length != local.Length) uv = new Vector2[local.Length];
                    var nativeNormals = source.normals; if (nativeNormals.Length != local.Length) nativeNormals = Enumerable.Repeat(Vector3.up, local.Length).ToArray();
                    var nativeTangents = source.tangents; if (nativeTangents.Length != local.Length) nativeTangents = Enumerable.Repeat(new Vector4(1, 0, 0, 1), local.Length).ToArray();
                    var triangles = Enumerable.Range(0, source.subMeshCount).Select(source.GetTriangles).ToArray();
                    var islands = Islands(local, triangles, out var bounds);
                    // These two narrow, obsolete native brake/frame hanger islands
                    // intersect the reversed barrel at every sampled pose. Remove
                    // whole islands, including their little lower retaining links.
                    bool Obsolete(Bounds b) => Mathf.Abs(b.center.x) > .57f && Mathf.Abs(b.center.x) < .68f &&
                        b.size.x < .12f && b.min.y >= .49f && b.max.y <= 1.65f && b.min.z >= -1.32f && b.max.z <= -.50f;
                    // Exact native longitudinal brake pull-rod islands (86
                    // vertices per side): their lower rear links also meet the
                    // inward casting. Stock calipers have already been retired.
                    bool StockBrakeLink(Bounds b) => Mathf.Abs(b.center.x) >= .49f && Mathf.Abs(b.center.x) <= .52f &&
                        b.size.x >= .19f && b.size.x <= .22f && b.min.y >= .58f && b.min.y <= .61f &&
                        b.max.y >= 1.07f && b.max.y <= 1.10f && b.min.z >= -1.18f && b.min.z <= -1.14f && b.max.z >= 3.79f && b.max.z <= 3.82f;
                    // Four original static slide bars (two heights per side).
                    // The fitted front engine carries its own guides cabward;
                    // these native body islands otherwise project into space.
                    bool StockSlideBar(Bounds b) => Mathf.Abs(b.center.x) > 1.04f && Mathf.Abs(b.center.x) < 1.07f && b.size.x < .04f &&
                        b.min.z > 1.10f && b.min.z < 1.12f && b.max.z > 2.72f && b.max.z < 2.75f &&
                        ((b.min.y > .85f && b.min.y < .88f && b.max.y > .94f && b.max.y < .96f) ||
                         (b.min.y > 1.01f && b.min.y < 1.03f && b.max.y > 1.09f && b.max.y < 1.12f));
                    bool StockValveSupport(Bounds b) => Mathf.Abs(b.center.x) > .99f && Mathf.Abs(b.center.x) < 1.02f &&
                        b.min.y > 1.08f && b.min.y < 1.11f && b.max.y > 1.48f && b.max.y < 1.51f &&
                        b.min.z > .72f && b.min.z < .75f && b.max.z > 1.12f && b.max.z < 1.15f;
                    bool StockSpring(Bounds b) =>
                        (Mathf.Abs(b.center.x) > .48f && Mathf.Abs(b.center.x) < .50f && b.size.x > .21f && b.size.x < .23f &&
                         b.min.y > 1.12f && b.min.y < 1.15f && b.max.y > 1.42f && b.max.y < 1.45f &&
                         ((b.min.z > -.57f && b.min.z < -.55f && b.max.z > .85f && b.max.z < .87f) ||
                          (b.min.z > .98f && b.min.z < 1.01f && b.max.z > 2.39f && b.max.z < 2.42f))) ||
                        (b.min.x > .31f && b.min.x < .33f && b.max.x > .53f && b.max.x < .55f &&
                         b.min.y > .96f && b.min.y < .99f && b.max.y > 1.26f && b.max.y < 1.29f &&
                         b.min.z > -2.05f && b.min.z < -2.02f && b.max.z > -.78f && b.max.z < -.75f);
                    // The retired slides/support left their separate end cheek,
                    // two bolts and outer pivot behind (four islands per side).
                    bool StockGuideFitting(Bounds b) =>
                        (Mathf.Abs(b.center.x) > 1.02f && Mathf.Abs(b.center.x) < 1.04f && b.min.z > 1.11f && b.max.z < 1.18f &&
                         b.min.y > .88f && b.max.y < 1.12f) ||
                        (Mathf.Abs(b.center.x) > 1.02f && Mathf.Abs(b.center.x) < 1.04f && b.min.z > 1.12f && b.max.z < 1.17f &&
                         ((b.min.y > .88f && b.max.y < .93f) || (b.min.y > 1.04f && b.max.y < 1.08f))) ||
                        (Mathf.Abs(b.center.x) > 1.14f && Mathf.Abs(b.center.x) < 1.15f && b.min.z > .75f && b.max.z < .82f &&
                         b.min.y > 1.12f && b.max.y < 1.19f);
                    var remove = bounds.Select(b => Obsolete(b) || StockBrakeLink(b) || StockSlideBar(b) || StockValveSupport(b) || StockSpring(b) || StockGuideFitting(b)).ToArray();
                    // Identified in the assembled reference/highlight preview:
                    // the stock forward hanger remained after its mechanism
                    // moved. Retire its low drop, retaining the long top frame.
                    var frontHanger = bounds.Select(b => Mathf.Abs(b.center.x) > .19f && Mathf.Abs(b.center.x) < .36f &&
                        b.min.y > .50f && b.min.y < .52f && b.max.y > 1.62f && b.max.y < 1.64f &&
                        b.min.z > .25f && b.min.z < .27f && b.max.z > 2.40f && b.max.z < 2.43f).ToArray();
                    var forwardDrop = Box(new Vector3(-.50f, .20f, 2.05f), new Vector3(.50f, 1.50f, 2.50f));
                    var vertices = new List<Vector3>(); var coords = new List<Vector2>(); var normals = new List<Vector3>(); var tangents = new List<Vector4>(); var output = new List<int>[triangles.Length];
                    Vector3 ToFilter(Vector3 p) => filter.transform.InverseTransformPoint(body.TransformPoint(p));
                    for (int sub = 0; sub < triangles.Length; sub++)
                    {
                        output[sub] = new List<int>();
                        for (int i = 0; i < triangles[sub].Length; i += 3)
                        {
                            var ids = new[] { triangles[sub][i], triangles[sub][i + 1], triangles[sub][i + 2] };
                            if (remove[islands[ids[0]]]) continue;
                            var polygons = new List<List<Vertex>> { ids.Select(j => new Vertex(local[j], uv[j], nativeNormals[j], nativeTangents[j])).ToList() };
                            foreach (int side in new[] { -1, 1 }) polygons = polygons.SelectMany(p => Subtract(p, FrameRelief(side))).ToList();
                            foreach (int side in new[] { -1, 1 }) polygons = polygons.SelectMany(p => Subtract(p, Notch(side))).ToList();
                            if (frontHanger[islands[ids[0]]]) polygons = polygons.SelectMany(p => Subtract(p, forwardDrop)).ToList();
                            foreach (var poly in polygons)
                            {
                                int start = vertices.Count; foreach (var v in poly) { vertices.Add(ToFilter(v.P)); coords.Add(v.U); normals.Add(v.N); tangents.Add(v.T); }
                                for (int j = 1; j + 1 < poly.Count; j++) output[sub].AddRange(new[] { start, start + j, start + j + 1 });
                            }
                        }
                    }
                    // Close actual section ends at the inboard cut. Each cap
                    // belongs to one original connected frame member; no large
                    // generic panel is placed across the engine bay.
                    void CapSections(int cutAxis, float plane, Bounds box, Func<int, bool> include, Vector3 outward)
                    {
                        var sections = new Dictionary<(int island, int sub), List<Vertex>>();
                        for (int sub = 0; sub < triangles.Length; sub++) for (int i = 0; i < triangles[sub].Length; i += 3)
                        {
                            var ids = new[] { triangles[sub][i], triangles[sub][i + 1], triangles[sub][i + 2] }; int id = islands[ids[0]];
                            if (!include(id) || bounds[id].min[cutAxis] >= plane || bounds[id].max[cutAxis] <= plane) continue;
                            var key = (id, sub); if (!sections.TryGetValue(key, out var points)) sections[key] = points = new List<Vertex>();
                            for (int edge = 0; edge < 3; edge++)
                            {
                                int a = ids[edge], b = ids[(edge + 1) % 3]; float da = local[a][cutAxis] - plane, db = local[b][cutAxis] - plane;
                                if ((da >= 0) == (db >= 0)) continue; float t = da / (da - db);
                                points.Add(new Vertex(Vector3.LerpUnclamped(local[a], local[b], t), Vector2.LerpUnclamped(uv[a], uv[b], t), Vector3.zero, Vector4.zero));
                            }
                        }
                        foreach (var section in sections)
                        {
                            var points = section.Value.GroupBy(v => new Vector3(Mathf.Round(v.P.x * 100000), Mathf.Round(v.P.y * 100000), Mathf.Round(v.P.z * 100000))).Select(g => g.First()).ToList();
                            if (points.Count < 3) continue; var centre = points.Aggregate(Vector3.zero, (sum, v) => sum + v.P) / points.Count;
                            int u = (cutAxis + 1) % 3, vAxis = (cutAxis + 2) % 3;
                            points = points.OrderBy(v => Mathf.Atan2(v.P[vAxis] - centre[vAxis], v.P[u] - centre[u])).ToList();
                            foreach (int axis in new[] { u, vAxis }) foreach (bool lower in new[] { true, false }) points = Inside(points, axis, lower ? box.min[axis] : box.max[axis], lower);
                            if (points.Count < 3) continue;
                            var normal = filter.transform.InverseTransformDirection(body.TransformDirection(outward)).normalized;
                            if (Vector3.Dot(Vector3.Cross(ToFilter(points[1].P) - ToFilter(points[0].P), ToFilter(points[2].P) - ToFilter(points[0].P)), normal) < 0) points.Reverse();
                            int start = vertices.Count;
                            var tangent = Vector3.Cross(Mathf.Abs(normal.y) > .9f ? Vector3.forward : Vector3.up, normal).normalized;
                            foreach (var point in points) { vertices.Add(ToFilter(point.P)); coords.Add(point.U); normals.Add(normal); tangents.Add(new Vector4(tangent.x, tangent.y, tangent.z, 1)); }
                            for (int i = 1; i + 1 < points.Count; i++) output[section.Key.sub].AddRange(new[] { start, start + i, start + i + 1 });
                        }
                    }
                    foreach (int side in new[] { -1, 1 }) CapSections(0, side * FrameInner, FrameRelief(side), id => !remove[id], new Vector3(side, 0, 0));
                    CapSections(1, forwardDrop.max.y, forwardDrop, id => frontHanger[id], Vector3.down);
                    CapSections(2, forwardDrop.min.z, forwardDrop, id => frontHanger[id], Vector3.forward);
                    // Closed raised covers reconnect the local plate edges. UV
                    // samples come from the actual native plate, preserving skins.
                    int paintSub = 0; Vector2 paint = Vector2.zero; float largest = -1;
                    for (int sub = 0; sub < triangles.Length; sub++) for (int i = 0; i < triangles[sub].Length; i += 3)
                    {
                        int a = triangles[sub][i], b = triangles[sub][i + 1], c = triangles[sub][i + 2]; var centre = (local[a] + local[b] + local[c]) / 3;
                        var normal = Vector3.Cross(local[b] - local[a], local[c] - local[a]); float area = normal.magnitude;
                        if (area <= largest || centre.y < 1.55f || centre.y > 1.70f || Mathf.Abs(normal.normalized.y) < .9f) continue;
                        largest = area; paintSub = sub; paint = (uv[a] + uv[b] + uv[c]) / 3;
                    }
                    void Quad(Vector3 n, params Vector3[] q)
                    {
                        q = q.Select(ToFilter).ToArray();
                        var normal = filter.transform.InverseTransformDirection(body.TransformDirection(n)).normalized;
                        if (Vector3.Dot(Vector3.Cross(q[1] - q[0], q[2] - q[0]), normal) < 0) q = new[] { q[0], q[3], q[2], q[1] };
                        var tangent = Vector3.Cross(Mathf.Abs(normal.y) > .9f ? Vector3.forward : Vector3.up, normal).normalized;
                        var polys = new List<List<Vertex>> { q.Select(p => new Vertex(body.InverseTransformPoint(filter.transform.TransformPoint(p)), paint, normal, new Vector4(tangent.x, tangent.y, tangent.z, 1))).ToList() };
                        foreach (int side in new[] { -1, 1 }) polys = polys.SelectMany(p => Subtract(p, PipeAccess(side))).ToList();
                        foreach (var poly in polys)
                        {
                            int start = vertices.Count;
                            foreach (var v in poly) { vertices.Add(ToFilter(v.P)); coords.Add(v.U); normals.Add(v.N); tangents.Add(v.T); }
                            for (int i = 1; i + 1 < poly.Count; i++) output[paintSub].AddRange(new[] { start, start + i, start + i + 1 });
                        }
                    }
                    int coverStart = output[paintSub].Count;
                    foreach (int side in filter.name == "s060_body" ? new[] { -1, 1 } : Array.Empty<int>())
                    {
                        float x0 = side > 0 ? CoverInner : -1.28f, x1 = side > 0 ? 1.28f : -CoverInner,
                            z0 = -1.40f - S060MeyerLayout.FrontCylinderShift, z1 = .40f - S060MeyerLayout.FrontCylinderShift, bottom = 1.55f, top = 1.69f;
                        Quad(Vector3.up, new Vector3(x0, top, z0), new Vector3(x0, top, z1), new Vector3(x1, top, z1), new Vector3(x1, top, z0));
                        Quad(Vector3.down, new Vector3(x0, top - .015f, z0), new Vector3(x1, top - .015f, z0), new Vector3(x1, top - .015f, z1), new Vector3(x0, top - .015f, z1));
                        foreach (float x in new[] { x0, x1 }) Quad(x == x0 ? Vector3.left : Vector3.right, new Vector3(x, bottom, z0), new Vector3(x, top, z0), new Vector3(x, top, z1), new Vector3(x, bottom, z1));
                        foreach (float z in new[] { z0, z1 }) Quad(z == z0 ? Vector3.back : Vector3.forward, new Vector3(x0, bottom, z), new Vector3(x1, bottom, z), new Vector3(x1, top, z), new Vector3(x0, top, z));
                        // Close the firebox base above the retained inboard
                        // strips. These fixed cheek plates stay inside the
                        // .44 m spine; the moving castings sweep no closer than
                        // .473853 m. Overlap the existing frame and upper shell
                        // so fire cannot show through an open side seam.
                        float inner = side > 0 ? .40f : -FrameInner, outer = side > 0 ? FrameInner : -.40f;
                        const float low = 1.28f, high = 1.72f;
                        foreach (float x in new[] { inner, outer })
                            Quad(x == inner ? Vector3.left : Vector3.right, new Vector3(x, low, z0), new Vector3(x, high, z0), new Vector3(x, high, z1), new Vector3(x, low, z1));
                        foreach (float y in new[] { low, high })
                            Quad(y == low ? Vector3.down : Vector3.up, new Vector3(inner, y, z0), new Vector3(inner, y, z1), new Vector3(outer, y, z1), new Vector3(outer, y, z0));
                        foreach (float z in new[] { z0, z1 })
                            Quad(z == z0 ? Vector3.back : Vector3.forward, new Vector3(inner, low, z), new Vector3(outer, low, z), new Vector3(outer, high, z), new Vector3(inner, high, z));
                    }
                    var copy = new Mesh { name = source.name + " (inward Meyer clearance)", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                    copy.SetVertices(vertices); copy.SetUVs(0, coords); copy.subMeshCount = output.Length;
                    copy.SetNormals(normals); copy.SetTangents(tangents);
                    for (int sub = 0; sub < output.Length; sub++) copy.SetTriangles(output[sub], sub);
                    copy.RecalculateBounds();
                    owner.owned.Add((filter, source, copy)); filter.sharedMesh = copy;
                    if (output[paintSub].Count > coverStart) owner.covers.Add((filter, output[paintSub].Skip(coverStart).ToArray()));
                }
                if (owner.owned.Count == 0) throw new InvalidOperationException("Readable native S060 shell required for inward Meyer clearance.");
                return owner;
            }
            catch { owner.Dispose(); throw; }
        }
        internal static int[] Islands(Vector3[] vertices, int[][] triangles, out Bounds[] bounds)
        {
            var parent = Enumerable.Range(0, vertices.Length).ToArray();
            int Find(int i) { while (parent[i] != i) { parent[i] = parent[parent[i]]; i = parent[i]; } return i; }
            var welded = new Dictionary<Vector3, int>();
            for (int i = 0; i < vertices.Length; i++)
            {
                var p = vertices[i]; var key = new Vector3(Mathf.Round(p.x * 100000), Mathf.Round(p.y * 100000), Mathf.Round(p.z * 100000));
                if (welded.TryGetValue(key, out int other)) parent[Find(i)] = Find(other); else welded[key] = i;
            }
            foreach (var indices in triangles) for (int i = 0; i < indices.Length; i += 3) { int a = Find(indices[i]); parent[Find(indices[i + 1])] = a; parent[Find(indices[i + 2])] = a; }
            var roots = new Dictionary<int, int>(); var ids = new int[vertices.Length]; var boxes = new List<Bounds>();
            for (int i = 0; i < vertices.Length; i++)
            {
                int root = Find(i); if (!roots.TryGetValue(root, out int id)) { roots[root] = id = boxes.Count; boxes.Add(new Bounds(vertices[i], Vector3.zero)); }
                ids[i] = id; var b = boxes[id]; b.Encapsulate(vertices[i]); boxes[id] = b;
            }
            bounds = boxes.ToArray(); return ids;
        }
        private static IEnumerable<List<Vertex>> Subtract(List<Vertex> polygon, Bounds box)
        {
            var pending = polygon;
            for (int side = 0; side < 6 && pending.Count >= 3; side++)
            {
                int axis = side / 2; bool lower = side % 2 == 0; float edge = lower ? box.min[axis] : box.max[axis];
                var inside = new List<Vertex>(); var outside = new List<Vertex>(); float Distance(Vertex v) => (lower ? 1 : -1) * (v.P[axis] - edge);
                for (int i = 0; i < pending.Count; i++)
                {
                    var a = pending[i]; var b = pending[(i + 1) % pending.Count]; float da = Distance(a), db = Distance(b); (da >= 0 ? inside : outside).Add(a);
                    if ((da >= 0) != (db >= 0)) { float t = da / (da - db); var cut = new Vertex(Vector3.LerpUnclamped(a.P, b.P, t), Vector2.LerpUnclamped(a.U, b.U, t), Vector3.LerpUnclamped(a.N, b.N, t).normalized, Vector4.LerpUnclamped(a.T, b.T, t)); inside.Add(cut); outside.Add(cut); }
                }
                if (outside.Count >= 3) yield return outside; pending = inside;
            }
        }
        private static List<Vertex> Inside(List<Vertex> polygon, int axis, float edge, bool lower)
        {
            var output = new List<Vertex>();
            for (int i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i]; var b = polygon[(i + 1) % polygon.Count]; float da = (lower ? 1 : -1) * (a.P[axis] - edge), db = (lower ? 1 : -1) * (b.P[axis] - edge);
                if (da >= 0) output.Add(a);
                if ((da >= 0) != (db >= 0)) { float t = da / (da - db); output.Add(new Vertex(Vector3.LerpUnclamped(a.P, b.P, t), Vector2.LerpUnclamped(a.U, b.U, t), Vector3.LerpUnclamped(a.N, b.N, t), Vector4.LerpUnclamped(a.T, b.T, t))); }
            }
            return output;
        }
        public void Dispose()
        {
            foreach (var home in owned) { if (home.filter != null && home.filter.sharedMesh == home.copy) home.filter.sharedMesh = home.source; if (home.copy != null) { if (Application.isPlaying) Object.Destroy(home.copy); else Object.DestroyImmediate(home.copy); } }
            owned.Clear();
            covers.Clear();
        }
    }
}
