using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

// Offline mesh-surface intersection test. Mesh and triangle AABBs only reject
// impossible pairs; overlapping bounds are checked against actual triangle
// edges (including coplanar faces), never against vertex proximity alone.
public static class SteamMeshContact
{
    public sealed class Shell
    {
        public readonly string Name;
        public readonly Bounds Bounds;
        private readonly Triangle[] triangles;
        public Shell(MeshFilter filter, Transform space, Bounds? region = null)
        {
            Name = filter.name;
            var matrix = space.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            var vertices = filter.sharedMesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
            var indices = filter.sharedMesh.triangles;
            triangles = Enumerable.Range(0, indices.Length / 3)
                .Select(i => new Triangle(vertices[indices[3 * i]], vertices[indices[3 * i + 1]], vertices[indices[3 * i + 2]]))
                .Where(t => !region.HasValue || t.Bounds.Intersects(region.Value)).ToArray();
            // Split native meshes can retain unused source vertices. Bounds
            // must describe the selected faces, including their existing pad.
            var bounds = triangles.Length == 0 ? new Bounds() : triangles[0].Bounds;
            foreach (var triangle in triangles) bounds.Encapsulate(triangle.Bounds);
            Bounds = bounds;
        }
        public bool Intersects(Shell other)
        { return Intersects(other, out _); }
        public bool Intersects(Shell other, out string contact)
        {
            contact = null;
            if (triangles.Length == 0 || other.triangles.Length == 0 || !Bounds.Intersects(other.Bounds)) return false;
            // A triangle outside the complete target bounds cannot touch any
            // target face. Include the existing per-triangle bounds padding;
            // this only rejects impossible pairs before the exact tests below.
            var targetRegion = other.Bounds; targetRegion.Expand(.000004f);
            for (int i = 0; i < triangles.Length; i++)
            {
                var first = triangles[i];
                if (!first.Bounds.Intersects(targetRegion)) continue;
                for (int j = 0; j < other.triangles.Length; j++)
                {
                    var second = other.triangles[j];
                    if (!first.Bounds.Intersects(second.Bounds) || !first.Intersects(second)) continue;
                    contact = "Body-space triangle contact: " + Name + "[" + i + "] " + first.Describe() +
                        " / " + other.Name + "[" + j + "] " + second.Describe();
                    return true;
                }
            }
            return false;
        }
    }
    private struct Triangle
    {
        private readonly Vector3 a, b, c;
        public readonly Bounds Bounds;
        public Triangle(Vector3 a, Vector3 b, Vector3 c)
        { this.a = a; this.b = b; this.c = c; var bounds = new Bounds(a, Vector3.zero); bounds.Encapsulate(b); bounds.Encapsulate(c); bounds.Expand(.000002f); Bounds = bounds; }
        public string Describe() => "a=" + Point(a) + " b=" + Point(b) + " c=" + Point(c) + " centroid=" + Point((a + b + c) / 3f) +
            " bounds=" + Point(Bounds.min) + ".." + Point(Bounds.max);
        private static string Point(Vector3 value) => "(" + value.x.ToString("R", CultureInfo.InvariantCulture) + "," +
            value.y.ToString("R", CultureInfo.InvariantCulture) + "," + value.z.ToString("R", CultureInfo.InvariantCulture) + ")";
        public bool Intersects(Triangle other)
        {
            if (Segment(a, b, other) || Segment(b, c, other) || Segment(c, a, other) ||
                Segment(other.a, other.b, this) || Segment(other.b, other.c, this) || Segment(other.c, other.a, this)) return true;
            var normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude < 1e-14f) return false;
            // Vector3.Normalize zeros vectors shorter than 1e-5. Valid small
            // clipped faces can have area normals below that, despite passing
            // our explicit degeneracy threshold. Divide by the measured length.
            normal /= Mathf.Sqrt(normal.sqrMagnitude);
            if (Mathf.Abs(Vector3.Dot(other.a - a, normal)) > .000001f ||
                Mathf.Abs(Vector3.Dot(other.b - a, normal)) > .000001f || Mathf.Abs(Vector3.Dot(other.c - a, normal)) > .000001f) return false;
            int axis = Mathf.Abs(normal.x) > Mathf.Abs(normal.y) ? (Mathf.Abs(normal.x) > Mathf.Abs(normal.z) ? 0 : 2) : (Mathf.Abs(normal.y) > Mathf.Abs(normal.z) ? 1 : 2);
            Vector2 P(Vector3 v) => axis == 0 ? new Vector2(v.y, v.z) : axis == 1 ? new Vector2(v.x, v.z) : new Vector2(v.x, v.y);
            var first = new[] { P(a), P(b), P(c) }; var second = new[] { P(other.a), P(other.b), P(other.c) };
            if (Contains(first[0], second) || Contains(second[0], first)) return true;
            for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++)
                if (Crosses(first[i], first[(i + 1) % 3], second[j], second[(j + 1) % 3])) return true;
            return false;
        }
        private static bool Segment(Vector3 from, Vector3 to, Triangle target)
        {
            var direction = to - from; var edge1 = target.b - target.a; var edge2 = target.c - target.a;
            var h = Vector3.Cross(direction, edge2); float determinant = Vector3.Dot(edge1, h);
            if (Mathf.Abs(determinant) < 1e-10f) return false;
            float inverse = 1 / determinant;
            var s = from - target.a; float u = inverse * Vector3.Dot(s, h);
            if (u < -.000001f || u > 1.000001f) return false;
            var q = Vector3.Cross(s, edge1); float v = inverse * Vector3.Dot(direction, q);
            if (v < -.000001f || u + v > 1.000001f) return false;
            float distance = inverse * Vector3.Dot(edge2, q);
            return distance >= -.000001f && distance <= 1.000001f;
        }
        private static float Side(Vector2 a, Vector2 b, Vector2 p) => (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
        private static bool Contains(Vector2 p, Vector2[] tri)
        { float a = Side(tri[0], tri[1], p), b = Side(tri[1], tri[2], p), c = Side(tri[2], tri[0], p); return (a >= 0 && b >= 0 && c >= 0) || (a <= 0 && b <= 0 && c <= 0); }
        private static bool Crosses(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        { return Side(a, b, c) * Side(a, b, d) < 0 && Side(c, d, a) * Side(c, d, b) < 0; }
    }
    public static int ValidateAlgorithm()
    {
        var baseTriangle = new Triangle(Vector3.zero, Vector3.right, Vector3.up);
        if (!baseTriangle.Intersects(new Triangle(new Vector3(.25f, .25f, -1), new Vector3(.25f, .25f, 1), Vector3.right))) throw new InvalidOperationException("Crossing triangle control failed.");
        if (baseTriangle.Intersects(new Triangle(Vector3.forward, Vector3.right + Vector3.forward, Vector3.up + Vector3.forward))) throw new InvalidOperationException("Separated triangle control failed.");
        if (!baseTriangle.Intersects(new Triangle(new Vector3(.1f, .1f), new Vector3(.2f, .1f), new Vector3(.1f, .2f)))) throw new InvalidOperationException("Contained coplanar triangle control failed.");
        if (!baseTriangle.Intersects(new Triangle(new Vector3(.3f, -.1f), new Vector3(.7f, -.1f), new Vector3(.5f, .7f)))) throw new InvalidOperationException("Coplanar edge-crossing control failed.");
        var small = new Triangle(new Vector3(-1.07834017f,.5091537f,-.5141494f), new Vector3(-1.07834017f,.509810746f,-.513212264f), new Vector3(-1.07834017f,.513872445f,-.5174455f));
        var shaft = new Triangle(new Vector3(-1.13335454f,.496970057f,-.50981766f), new Vector3(-1.13335454f,.5155992f,-.522265255f), new Vector3(-.900900841f,.515598834f,-.522265434f));
        if (small.Intersects(shaft) || shaft.Intersects(small)) throw new InvalidOperationException("Separated small bearing-face and shaft triangles must stay clear in both test directions.");
        return 6;
    }
}
