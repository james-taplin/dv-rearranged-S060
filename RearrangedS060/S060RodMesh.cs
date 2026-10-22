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
        public static Mesh Build(string name, Vector2[] pins, float bossRadius = .067f, float hole = .042f)
        {
            if (pins == null || pins.Length < 2 || pins.Length > 4) throw new ArgumentException("Two to four driven pins required.");
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
