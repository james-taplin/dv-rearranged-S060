using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RearrangedS060
{
    // The existing splitter leaves the upper branch in s060_body, while its
    // lower end belongs to the separate cylinder. Own per-car mesh copies only.
    internal sealed class S060BodyPipes : IDisposable
    {
        private readonly List<(MeshFilter filter, Mesh original, Mesh owned)> meshes = new List<(MeshFilter, Mesh, Mesh)>();
        private readonly Dictionary<int, Vector3> sockets = new Dictionary<int, Vector3>();
        private readonly Dictionary<int, float> socketRadii = new Dictionary<int, float>();
        private bool capSockets;
        private Transform body;
        internal Vector3 BoilerSocket(int side) => sockets.TryGetValue(side, out var value) ? value : throw new InvalidOperationException("Actual S060 boiler neck section missing.");
        internal float BoilerSocketRadius(int side) => socketRadii[side];
        internal static bool InBranch(Vector3 p) => Math.Abs(p.x) >= .52f && Math.Abs(p.x) <= 1.50f &&
            p.y >= 1.56f && p.y <= 2.35f && p.z >= 2.98f && p.z <= 3.35f;
        internal static Vector3 Bend(Vector3 p, Vector3 delta)
        {
            if (!InBranch(p)) return p;
            float t = Math.Max(0, Math.Min(1, (2.35f - p.y) / (2.35f - 1.56f)));
            return p + delta * (t * t * (3 - 2 * t));
        }
        internal static S060BodyPipes Apply(Transform body, Vector3 delta, bool remove = false, bool capSockets = true)
        {
            var owner = new S060BodyPipes { capSockets = capSockets, body = body };
            try
            {
                foreach (var filter in body.GetComponentsInChildren<MeshFilter>(true).Where(f => f.name == "s060_body").OrderByDescending(f => f.sharedMesh == null ? 0 : f.sharedMesh.vertexCount))
                {
                    var source = filter.sharedMesh;
                    if (source == null || !source.isReadable) continue; // disabled original alias; split copies are readable
                    var vertices = source.vertices;
                    var local = vertices.Select(v => body.InverseTransformPoint(filter.transform.TransformPoint(v))).ToArray();
                    if (!local.Any(InBranch)) continue;
                    var copy = UnityEngine.Object.Instantiate(source); copy.name = source.name + " (S060 pipe fit)";
                    owner.meshes.Add((filter, source, copy));
                    if (remove)
                    {
                        owner.KeepBoilerNecks(copy, source, local, filter.transform, body);
                    }
                    else
                    {
                        for (int i = 0; i < vertices.Length; i++) if (InBranch(local[i]))
                            vertices[i] = filter.transform.InverseTransformPoint(body.TransformPoint(Bend(local[i], delta)));
                        copy.vertices = vertices; copy.RecalculateNormals();
                    }
                    copy.RecalculateBounds(); filter.sharedMesh = copy;
                }
                if (owner.meshes.Count == 0) throw new InvalidOperationException("Readable split S060 upper steam pipes are unavailable.");
                return owner;
            }
            catch { owner.Dispose(); throw; }
        }
        private void KeepBoilerNecks(Mesh copy, Mesh source, Vector3[] local, Transform filter, Transform body)
        {
            // Keep the genuine native fitting where it enters the boiler. Cut
            // only its lower shaft, using actual triangle/plane intersections.
            var vertices = source.vertices.ToList(); var uv = source.uv.ToList();
            bool hasUv = uv.Count == vertices.Count;
            var sections = new Dictionary<int, List<Vector3>> { [-1] = new List<Vector3>(), [1] = new List<Vector3>() };
            var output = new List<int>[source.subMeshCount];
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                var old = source.GetTriangles(sub); var kept = output[sub] = new List<int>(old.Length);
                for (int i = 0; i < old.Length; i += 3)
                {
                    var ids = new[] { old[i], old[i + 1], old[i + 2] };
                    if (!ids.All(j => InBranch(local[j]))) { kept.AddRange(ids); continue; }
                    var polygon = new List<(Vector3 p, Vector2 uv)>();
                    for (int e = 0; e < 3; e++)
                    {
                        int ia = ids[e], ib = ids[(e + 1) % 3]; var a = local[ia]; var b = local[ib];
                        Vector2 auv = hasUv ? uv[ia] : Vector2.zero, buv = hasUv ? uv[ib] : Vector2.zero;
                        if (a.y >= S060PipeGeometry.BoilerCutY) polygon.Add((a, auv));
                        if ((a.y >= S060PipeGeometry.BoilerCutY) == (b.y >= S060PipeGeometry.BoilerCutY)) continue;
                        float fraction = (S060PipeGeometry.BoilerCutY - a.y) / (b.y - a.y);
                        var point = Vector3.LerpUnclamped(a, b, fraction);
                        polygon.Add((point, Vector2.LerpUnclamped(auv, buv, fraction)));
                        sections[point.x < 0 ? -1 : 1].Add(point);
                    }
                    for (int e = 1; e + 1 < polygon.Count; e++)
                    {
                        foreach (int j in new[] { 0, e, e + 1 })
                        {
                            kept.Add(vertices.Count); vertices.Add(filter.InverseTransformPoint(body.TransformPoint(polygon[j].p)));
                            if (hasUv) uv.Add(polygon[j].uv);
                        }
                    }
                }
            }
            foreach (var section in sections)
            {
                var points = section.Value.GroupBy(p => new Vector3(Mathf.Round(p.x * 100000), Mathf.Round(p.y * 100000), Mathf.Round(p.z * 100000))).Select(g => g.First()).ToArray();
                if (points.Length < 6) throw new InvalidOperationException("S060 boiler neck has no usable actual mesh section.");
                var centre = new Vector3((points.Min(p => p.x) + points.Max(p => p.x)) / 2, S060PipeGeometry.BoilerCutY, (points.Min(p => p.z) + points.Max(p => p.z)) / 2);
                if (!sockets.ContainsKey(section.Key)) sockets.Add(section.Key, centre);
                socketRadii[section.Key] = points.Max(p => new Vector2(p.x-centre.x,p.z-centre.z).magnitude);
                if (!capSockets) continue;
                var ring = points.OrderBy(p => Mathf.Atan2(p.z - centre.z, p.x - centre.x)).ToArray();
                for (int i = 0; i < ring.Length; i++)
                {
                    var a = ring[i]; var b = ring[(i + 1) % ring.Length];
                    if (Vector3.Cross(a - centre, b - centre).y > 0) { var swap = a; a = b; b = swap; }
                    foreach (var p in new[] { centre, a, b })
                    {
                        output[0].Add(vertices.Count); vertices.Add(filter.InverseTransformPoint(body.TransformPoint(p)));
                        if (hasUv) uv.Add(new Vector2(.6868f, .8948f));
                    }
                }
            }
            copy.vertices = vertices.ToArray(); if (hasUv) copy.uv = uv.ToArray();
            for (int sub = 0; sub < output.Length; sub++) copy.SetTriangles(output[sub], sub);
            copy.RecalculateNormals(); if (hasUv) copy.RecalculateTangents();
        }
        // Unmodified native triangle sections, including the mounting edges.
        // Keep the segment connectivity: a bounding radius loses the pipe shape.
        internal Vector3[] BoilerSection(int side, float y)
        {
            foreach (var home in meshes)
            {
                var local = home.original.vertices.Select(v => body.InverseTransformPoint(home.filter.transform.TransformPoint(v))).ToArray();
                var indices = home.original.triangles; var section = new List<Vector3>();
                for (int t = 0; t < indices.Length; t += 3)
                {
                    int a = indices[t], b = indices[t + 1], c = indices[t + 2];
                    if (!InBranch(local[a]) || !InBranch(local[b]) || !InBranch(local[c]) || Math.Sign(local[a].x) != side) continue;
                    S060SteamSleeve.CutTriangle(local[a], local[b], local[c], y, section);
                }
                if (section.Count >= 12) return section.ToArray();
            }
            throw new InvalidOperationException("Actual native boiler pipe profile missing.");
        }
        public void Dispose()
        {
            foreach (var home in meshes)
            { if (home.filter != null) home.filter.sharedMesh = home.original; if (home.owned != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(home.owned); else UnityEngine.Object.DestroyImmediate(home.owned); } }
            meshes.Clear();
        }
    }
}
