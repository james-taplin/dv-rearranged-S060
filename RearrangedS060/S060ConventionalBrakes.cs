using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RearrangedS060
{
    // Keep the native renderers, LOD ownership, paint and overheating bindings.
    // Extra axle sections share those renderers rather than adding a heat system.
    internal sealed class S060ConventionalBrakes : IDisposable
    {
        private readonly List<(MeshFilter filter, Mesh original, Mesh owned)> meshes =
            new List<(MeshFilter, Mesh, Mesh)>();

        internal static S060ConventionalBrakes Apply(S060Parts parts, float factor, IEnumerable<Transform> wheels)
        {
            var owner = new S060ConventionalBrakes();
            try
            {
                var source = Enumerable.Range(1, 3).Select(i => parts.RightWheel(i).localPosition).ToArray();
                var targets = wheels.ToArray();
                if (targets.Length < 3 || targets.Length > 5 || targets.Any(w => w == null))
                    throw new InvalidOperationException("Three to five conventional brake axle stations required.");
                var stations = targets.Select(w => parts.Body.InverseTransformPoint(w.position)).ToArray();
                var filters = parts.Body.GetComponentsInChildren<MeshFilter>(true);
                foreach (var filter in filters.Where(f => f.name.IndexOf("brake_shoes", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                          f.name.StartsWith("s060_caliper_", StringComparison.Ordinal)))
                {
                    bool shoes = filter.name.IndexOf("brake_shoes", StringComparison.OrdinalIgnoreCase) >= 0;
                    int section = shoes ? -1 : 3 - int.Parse(filter.name.Substring(filter.name.LastIndexOf('_') + 1));
                    if (section < -1 || section > 2) throw new InvalidOperationException("Unknown native caliper: " + filter.name);
                    var original = filter.sharedMesh;
                    if (original == null || !original.isReadable)
                        throw new InvalidOperationException("Readable native brake mesh required: " + filter.name);
                    var fitted = Fit(filter, parts.Body, source, stations, factor, section);
                    owner.meshes.Add((filter, original, fitted));
                    filter.sharedMesh = fitted;
                }
                if (!owner.meshes.Any(m => m.filter.name.IndexOf("brake_shoes", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    !owner.meshes.Any(m => m.filter.name.StartsWith("s060_caliper_", StringComparison.Ordinal)))
                    throw new InvalidOperationException("Native conventional brake shoes and split calipers required.");
                return owner;
            }
            catch { owner.Dispose(); throw; }
        }

        private static Mesh Fit(MeshFilter filter, Transform body, Vector3[] source, Vector3[] targets, float factor, int section)
        {
            var mesh = filter.sharedMesh;
            var vertices = mesh.vertices; var normals = mesh.normals; var uv = mesh.uv; var tangents = mesh.tangents;
            var toBody = body.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            var points = vertices.Select(toBody.MultiplyPoint3x4).ToArray();
            var positions = new List<Vector3>(); var ns = new List<Vector3>(); var coords = new List<Vector2>(); var ts = new List<Vector4>();
            var triangles = Enumerable.Range(0, mesh.subMeshCount).Select(_ => new List<int>()).ToArray();
            // Shoes are three disconnected pairs. The installed LMS caliper
            // numbering runs front-to-rear (1..3), opposite the wheel numbering.
            int Region(Vector3 p) => p.z < 0 ? 0 : p.z < 1.5f ? 1 : 2;
            for (int axle = 0; axle < targets.Length; axle++)
            {
                int native = axle < 3 ? axle : 0;
                if (section >= 0 && section != native) continue;
                var matrix = filter.transform.worldToLocalMatrix * body.localToWorldMatrix *
                    Matrix4x4.TRS(targets[axle], Quaternion.identity, new Vector3(1, factor, factor)) *
                    Matrix4x4.Translate(-source[native]) * toBody;
                var normalMatrix = matrix.inverse.transpose;
                var map = new Dictionary<int, int>();
                int Vertex(int id)
                {
                    if (map.TryGetValue(id, out int found)) return found;
                    int index = positions.Count; map.Add(id, index);
                    positions.Add(matrix.MultiplyPoint3x4(vertices[id]));
                    if (normals.Length == vertices.Length) ns.Add(normalMatrix.MultiplyVector(normals[id]).normalized);
                    if (uv.Length == vertices.Length) coords.Add(uv[id]);
                    if (tangents.Length == vertices.Length)
                    {
                        var t = tangents[id]; var direction = matrix.MultiplyVector(new Vector3(t.x, t.y, t.z)).normalized;
                        ts.Add(new Vector4(direction.x, direction.y, direction.z, t.w));
                    }
                    return index;
                }
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    var indices = mesh.GetTriangles(sub);
                    for (int i = 0; i < indices.Length; i += 3)
                    {
                        if (section < 0 && (Region(points[indices[i]]) != native || Region(points[indices[i + 1]]) != native || Region(points[indices[i + 2]]) != native)) continue;
                        triangles[sub].Add(Vertex(indices[i])); triangles[sub].Add(Vertex(indices[i + 1])); triangles[sub].Add(Vertex(indices[i + 2]));
                    }
                }
            }
            if (positions.Count == 0) throw new InvalidOperationException("Native brake section is empty: " + filter.name);
            var result = new Mesh { name = mesh.name + " (fitted conventional brakes)", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            result.SetVertices(positions); if (ns.Count != 0) result.SetNormals(ns); if (coords.Count != 0) result.SetUVs(0, coords); if (ts.Count != 0) result.SetTangents(ts);
            result.subMeshCount = triangles.Length;
            for (int sub = 0; sub < triangles.Length; sub++) result.SetTriangles(triangles[sub], sub);
            if (ns.Count == 0) result.RecalculateNormals();
            result.RecalculateBounds();
            return result;
        }

        public void Dispose()
        {
            foreach (var home in meshes)
            {
                if (home.filter != null) home.filter.sharedMesh = home.original;
                if (home.owned != null) { if (Application.isPlaying) Object.Destroy(home.owned); else Object.DestroyImmediate(home.owned); }
            }
            meshes.Clear();
        }
    }
}
