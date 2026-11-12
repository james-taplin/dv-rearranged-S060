using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RearrangedS060
{
    // Author-owned rod geometry. Native wheel/rod animation and live material
    // remain authoritative; no unreadable game mesh export is needed.
    public static class S060RodMesh
    {
        // B99 native rods are readable. Retain their bearing ends, bevels,
        // normals and atlas islands; change only the distance between pins.
        // The stock coupling rod has three eyes: use its first web and two
        // terminal eyes, omitting the middle eye and redundant second web.
        public static Mesh NativeMeyer(Mesh source, float length, bool coupling, int side = 1)
        {
            if (source == null || !source.isReadable || length < .5f)
                throw new ArgumentException("Readable native S060 rod and fitted pin distance required.");
            var v = source.vertices; var normals = source.normals; var uv = source.uv; var tangents = source.tangents;
            var selected = new List<int[]>(); var used = new HashSet<int>();
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                var t = source.GetTriangles(sub); var keep = new List<int>();
                for (int i = 0; i < t.Length; i += 3)
                {
                    float lo = Mathf.Min(v[t[i]].z, Mathf.Min(v[t[i + 1]].z, v[t[i + 2]].z));
                    float hi = Mathf.Max(v[t[i]].z, Mathf.Max(v[t[i + 1]].z, v[t[i + 2]].z));
                    if (coupling && !(hi <= 1.4292f || lo >= 2.9790f)) continue;
                    for (int k = 0; k < 3; k++) { keep.Add(t[i + k]); used.Add(t[i + k]); }
                }
                selected.Add(keep.ToArray());
            }
            var ids = new List<int>(used); ids.Sort(); var map = new Dictionary<int, int>();
            var points = new Vector3[ids.Count]; var ns = new Vector3[ids.Count]; var coords = new Vector2[ids.Count];
            var ts = tangents.Length == v.Length ? new Vector4[ids.Count] : null;
            for (int i = 0; i < ids.Count; i++)
            {
                int j = ids[i]; map.Add(j, i); var p = v[j]; float stretch = 1, widthStretch = 1;
                if (coupling)
                {
                    // Native axle-centre caps reach X .920988. Keep a 5.48 mm
                    // gap at the established coupling plane, retaining the full
                    // stock outer face and profile; only the hidden inner half narrows.
                    if (p.x * side < 0) { widthStretch = .012f / .036683f; p.x *= widthStretch; }
                    if (p.z >= 2.9790f) p.z += length - 3.1f;
                    else if (p.z > .120852f)
                    {
                        stretch = (length - 2 * .120851f) / (1.429072f - .120851f);
                        p.z = .120851f + (p.z - .120851f) * stretch;
                    }
                }
                else
                {
                    const float pinDistance = 3.117108f, start = .18f, end = 2.94f;
                    if (p.z >= end) p.z += length - pinDistance;
                    else if (p.z > start)
                    {
                        stretch = (length - (pinDistance - end) - start) / (end - start);
                        p.z = start + (p.z - start) * stretch;
                    }
                }
                p.z -= length / 2; points[i] = p; coords[i] = uv[j];
                var n = normals[j]; ns[i] = new Vector3(n.x / widthStretch, n.y, n.z / stretch).normalized;
                if (ts != null) { var t = tangents[j]; var direction = new Vector3(t.x * widthStretch, t.y, t.z * stretch).normalized; ts[i] = new Vector4(direction.x, direction.y, direction.z, t.w); }
            }
            var mesh = new Mesh { name = source.name + " (Meyer fitted native rod)", vertices = points, normals = ns, uv = coords, subMeshCount = selected.Count };
            if (ts != null) mesh.tangents = ts;
            for (int sub = 0; sub < selected.Count; sub++) mesh.SetTriangles(Array.ConvertAll(selected[sub], j => map[j]), sub);
            // Stock faces close over the pins at their original lateral offset.
            // The Meyer's existing journals pass through its rod planes, so bore
            // those two eyes instead of moving pins, gauge or animation paths.
            Bore(mesh, length);
            points = mesh.vertices; ns = mesh.normals;
            for (int i = 0; i < points.Length; i++) { points[i].z /= length; ns[i] = new Vector3(ns[i].x, ns[i].y, ns[i].z * length).normalized; }
            mesh.vertices = points; mesh.normals = ns; mesh.RecalculateBounds(); mesh.RecalculateTangents(); return mesh;
        }
        private struct RodVertex
        {
            internal Vector3 P, N; internal Vector2 U;
            internal RodVertex(Vector3 p, Vector3 n, Vector2 u) { P = p; N = n; U = u; }
        }
        private static List<RodVertex> ClipRod(List<RodVertex> polygon, Vector3 normal, float edge, bool inside)
        {
            var result = new List<RodVertex>();
            for (int i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i]; var b = polygon[(i + 1) % polygon.Count];
                float da = Vector3.Dot(normal, a.P) - edge, db = Vector3.Dot(normal, b.P) - edge;
                if (inside ? da <= 0 : da >= 0) result.Add(a);
                if ((da < 0) != (db < 0)) { float t = da / (da - db); result.Add(new RodVertex(Vector3.LerpUnclamped(a.P, b.P, t), Vector3.LerpUnclamped(a.N, b.N, t).normalized, Vector2.LerpUnclamped(a.U, b.U, t))); }
            }
            return result;
        }
        private static void Bore(Mesh mesh, float length)
        {
            const int sides = 32; const float hole = .0595f;
            float radius = hole / Mathf.Cos(Mathf.PI / sides);
            var vertices = mesh.vertices; var normals = mesh.normals; var uv = mesh.uv;
            var output = new List<RodVertex>(); var triangles = new List<int>[mesh.subMeshCount];
            void Polygon(List<RodVertex> p, List<int> t)
            {
                for (int i = 1; i + 1 < p.Count; i++)
                {
                    if (Vector3.Cross(p[i].P - p[0].P, p[i + 1].P - p[0].P).sqrMagnitude < 1e-16f) continue;
                    int j = output.Count; output.Add(p[0]); output.Add(p[i]); output.Add(p[i + 1]); t.AddRange(new[] { j, j + 1, j + 2 });
                }
            }
            for (int sub = 0; sub < triangles.Length; sub++)
            {
                var source = mesh.GetTriangles(sub); var t = triangles[sub] = new List<int>();
                for (int i = 0; i < source.Length; i += 3)
                {
                    var polygons = new List<List<RodVertex>> { new List<RodVertex>() };
                    for (int k = 0; k < 3; k++) { int j = source[i + k]; polygons[0].Add(new RodVertex(vertices[j], normals[j], uv[j])); }
                    foreach (float z in new[] { -length / 2, length / 2 })
                    {
                        var kept = new List<List<RodVertex>>();
                        foreach (var polygon in polygons)
                        {
                            var remainder = polygon;
                            for (int plane = 0; plane < sides && remainder.Count >= 3; plane++)
                            {
                                float angle = (plane + .5f) * 2 * Mathf.PI / sides;
                                var n = new Vector3(0, Mathf.Cos(angle), Mathf.Sin(angle)); float edge = hole + n.z * z;
                                var outside = ClipRod(remainder, n, edge, false); if (outside.Count >= 3) kept.Add(outside);
                                remainder = ClipRod(remainder, n, edge, true);
                            }
                        }
                        polygons = kept;
                    }
                    foreach (var polygon in polygons) Polygon(polygon, t);
                }
            }
            float inner = float.MaxValue, outer = float.MinValue;
            foreach (var p in vertices) { inner = Mathf.Min(inner, p.x); outer = Mathf.Max(outer, p.x); }
            foreach (float z in new[] { -length / 2, length / 2 }) for (int i = 0; i < sides; i++)
            {
                float a = i * 2 * Mathf.PI / sides, b = (i + 1) * 2 * Mathf.PI / sides;
                RodVertex P(float x, float angle) => new RodVertex(new Vector3(x, radius * Mathf.Cos(angle), z + radius * Mathf.Sin(angle)), new Vector3(0, -Mathf.Cos(angle), -Mathf.Sin(angle)), new Vector2(.6868f, .8948f));
                var p = new List<RodVertex> { P(inner, a), P(outer, a), P(outer, b), P(inner, b) };
                if (Vector3.Dot(Vector3.Cross(p[1].P - p[0].P, p[2].P - p[0].P), p[0].N) < 0) p.Reverse();
                Polygon(p, triangles[0]);
            }
            mesh.Clear(); mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(output.ConvertAll(p => p.P)); mesh.SetNormals(output.ConvertAll(p => p.N)); mesh.SetUVs(0, output.ConvertAll(p => p.U));
            mesh.subMeshCount = triangles.Length; for (int sub = 0; sub < triangles.Length; sub++) mesh.SetTriangles(triangles[sub], sub);
        }
        public static Mesh Build(string name, Vector2[] pins, float bossRadius = .067f, float hole = .042f)
        {
            if (pins == null || pins.Length < 2 || pins.Length > 5) throw new ArgumentException("Two to five driven pins required.");
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            { int i = vertices.Count; vertices.AddRange(new[] { a, b, c, d }); triangles.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 }); }
            const float depth = .024f;
            foreach (var pin in pins) for (int segment = 0; segment < 32; segment++)
            {
                float a = segment * 2 * Mathf.PI / 32, b = (segment + 1) * 2 * Mathf.PI / 32;
                Vector3 P(float x, float r, float angle) => new Vector3(x, pin.x + Mathf.Cos(angle) * r, pin.y + Mathf.Sin(angle) * r);
                Quad(P(depth, bossRadius, a), P(depth, bossRadius, b), P(depth, hole, b), P(depth, hole, a));
                Quad(P(-depth, bossRadius, b), P(-depth, bossRadius, a), P(-depth, hole, a), P(-depth, hole, b));
                Quad(P(-depth, bossRadius, a), P(depth, bossRadius, a), P(depth, bossRadius, b), P(-depth, bossRadius, b));
                Quad(P(-depth, hole, b), P(depth, hole, b), P(depth, hole, a), P(-depth, hole, a));
            }
            for (int pin = 0; pin + 1 < pins.Length; pin++)
            {
                var direction = (pins[pin + 1] - pins[pin]).normalized;
                if (Vector2.Distance(pins[pin + 1], pins[pin]) <= 2 * bossRadius) throw new ArgumentException("Rod pin bosses overlap.");
                var width = new Vector2(direction.y, -direction.x) * .023f;
                var a = pins[pin] + direction * (bossRadius * .78f); var b = pins[pin + 1] - direction * (bossRadius * .78f);
                Vector3 P(float x, Vector2 p) => new Vector3(x, p.x, p.y);
                Quad(P(-depth, a + width), P(-depth, b + width), P(depth, b + width), P(depth, a + width));
                Quad(P(-depth, b - width), P(-depth, a - width), P(depth, a - width), P(depth, b - width));
                Quad(P(depth, a - width), P(depth, a + width), P(depth, b + width), P(depth, b - width));
                Quad(P(-depth, b - width), P(-depth, b + width), P(-depth, a + width), P(-depth, a - width));
            }
            var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            var uv = new Vector2[vertices.Count]; for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(.6868f, .8948f); mesh.uv = uv;
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        public static Mesh Normalized(string name, float length, float bossRadius = .067f, float hole = .042f)
        {
            var mesh = Build(name, new[] { new Vector2(0, -length / 2), new Vector2(0, length / 2) }, bossRadius, hole);
            var points = mesh.vertices; for (int i = 0; i < points.Length; i++) points[i].z /= length;
            mesh.vertices = points; mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
    public sealed class S060RodMeshSwap : IDisposable
    {
        private readonly MeshFilter filter; private readonly Mesh original;
        private Mesh owned;
        public S060RodMeshSwap(MeshFilter filter, Vector2[] pins)
        {
            this.filter = filter; original = filter.sharedMesh; owned = S060RodMesh.Build("[S060 fitted coupling rod]", pins, .105f, .0595f); filter.sharedMesh = owned;
        }
        public void Dispose()
        {
            if (filter != null && filter.sharedMesh == owned) filter.sharedMesh = original;
            if (owned != null)
            {
                if (Application.isPlaying) Object.Destroy(owned); else Object.DestroyImmediate(owned);
                owned = null;
            }
        }
    }
}
