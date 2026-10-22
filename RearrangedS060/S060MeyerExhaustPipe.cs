using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RearrangedS060
{
    // Visual LP exhaust return only. The existing compound owner and single
    // chimney already carry LP exhaust; this introduces no ports or readers.
    internal sealed class S060MeyerExhaustPipe : IDisposable
    {
        internal const float Radius = .09f, HeaderY = 1.68f, BodyRunY = 2f, SleeveLength = 1.55f;
        internal static readonly Vector3 RearBallPosition = new Vector3(-1.75f, HeaderY, .85f);
        internal static readonly Vector3 BodyBallPosition = new Vector3(-1.63f, BodyRunY, -3.1f);
        internal static readonly Vector3 SmokeboxPosition = new Vector3(-.656f, 2.25f, 3.7f);
        internal Transform BodyRoot { get; private set; }
        internal Transform RearRoot { get; private set; }
        internal Transform RearJoint { get; private set; }
        internal Transform BodyJoint { get; private set; }
        internal Transform SmokeboxJoint { get; private set; }
        internal Transform InnerSleeve { get; private set; }
        internal Transform OuterSleeve { get; private set; }
        internal Transform SlipCollar { get; private set; }
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<MeshRenderer> renderers = new List<MeshRenderer>();
        private readonly List<GameObject> objects = new List<GameObject>();
        private MeshRenderer materialSource;
        private Material material;
        private Vector3 previousRear, previousBody;
        private bool initialized, disposed;

        internal static S060MeyerExhaustPipe Bind(Transform body, Transform rear, Transform nativeShell)
        {
            var owner = new S060MeyerExhaustPipe();
            try
            {
                owner.materialSource = nativeShell.GetComponentsInChildren<MeshRenderer>(true)
                    .Where(r => r.name == "s060_body" && r.sharedMaterial != null)
                    .OrderByDescending(r => r.enabled && r.gameObject.activeInHierarchy).FirstOrDefault();
                if (owner.materialSource == null) throw new InvalidOperationException("Live native S060 material required for LP exhaust return.");
                owner.material = owner.materialSource.sharedMaterial;
                owner.BodyRoot = owner.Group(body, "[Meyer LP exhaust return]");
                owner.RearRoot = owner.Group(rear, "[Meyer LP exhaust collector]");
                foreach (int side in new[] { -1, 1 })
                {
                    var chest = rear.Find("[Round cylinders " + (side < 0 ? "L" : "R") + "]/Valve chest");
                    if (chest == null) throw new InvalidOperationException("Actual LP valve-chest casting required for exhaust takeoff.");
                    var filter = chest.GetComponent<MeshFilter>(); var b = filter.sharedMesh.bounds;
                    var cap = rear.InverseTransformPoint(filter.transform.TransformPoint(new Vector3(b.center.x, b.min.y, b.center.z)));
                    owner.Marker(owner.RearRoot, "[LP exhaust takeoff " + side + "]", cap);
                    owner.Pipe(owner.RearRoot, "LP exhaust riser " + side, .065f, new[] { cap, cap + Vector3.back * .15f, new Vector3(cap.x, HeaderY, .85f) });
                    owner.Ball(owner.RearRoot, "LP collector elbow " + side, new Vector3(cap.x, HeaderY, .85f), .105f);
                }
                owner.Pipe(owner.RearRoot, "LP exhaust collector header", Radius,
                    new[] { new Vector3(1.03f, HeaderY, .85f), RearBallPosition });
                owner.RearJoint = owner.Ball(owner.RearRoot, "[LP rear ball joint]", RearBallPosition, .145f);
                owner.BodyJoint = owner.Ball(owner.BodyRoot, "[LP body ball joint]", BodyBallPosition, .145f);
                owner.Pipe(owner.BodyRoot, "LP smokebox return", Radius,
                    new[] { BodyBallPosition, new Vector3(-1.63f, BodyRunY, 3.7f), new Vector3(-1.63f, 2.25f, 3.7f), SmokeboxPosition });
                owner.SmokeboxJoint = owner.Marker(owner.BodyRoot, "[LP smokebox socket]", SmokeboxPosition);
                owner.Pipe(owner.BodyRoot, "LP smokebox flange", .145f,
                    new[] { SmokeboxPosition - Vector3.right * .07f, SmokeboxPosition + Vector3.right * .02f }, false);
                // Three connected brackets attach the external run to the
                // existing footplate/frame edge; no free-floating long pipe.
                foreach (float z in new[] { -1f, 0f, 2.75f })
                {
                    // Leave the native plate from its underside, then rise
                    // outside it. The front foot is ahead of the low manifold.
                    owner.Pipe(owner.BodyRoot, "LP body support " + z, .025f, new[] {
                        new Vector3(-1.29f, 1.62f, z), new Vector3(-1.38f, 1.55f, z),
                        new Vector3(-1.63f, 1.55f, z), new Vector3(-1.63f, BodyRunY - Radius, z) });
                    owner.Pipe(owner.BodyRoot, "LP support collar " + z, .105f, new[] {
                        new Vector3(-1.63f, BodyRunY, z - .035f), new Vector3(-1.63f, BodyRunY, z + .035f) }, false);
                }
                owner.InnerSleeve = owner.Pipe(owner.BodyRoot, "[LP telescopic inner]", Radius, new[] { Vector3.zero, Vector3.forward }, false);
                owner.OuterSleeve = owner.Pipe(owner.BodyRoot, "[LP telescopic outer]", .11f, new[] { Vector3.zero, Vector3.forward }, false);
                owner.InnerSleeve.localScale = owner.OuterSleeve.localScale = new Vector3(1, 1, SleeveLength);
                owner.SlipCollar = owner.Pipe(owner.BodyRoot, "[LP sliding gland collar]", .13f,
                    new[] { new Vector3(0, 0, -.045f), new Vector3(0, 0, .045f) }, false);
                owner.UpdateGeometry(); return owner;
            }
            catch { owner.Dispose(); throw; }
        }

        internal void UpdateGeometry()
        {
            if (disposed || BodyRoot == null || RearJoint == null || BodyJoint == null || InnerSleeve == null || OuterSleeve == null || SlipCollar == null) return;
            if (materialSource != null && materialSource.sharedMaterial != material)
            {
                material = materialSource.sharedMaterial;
                foreach (var renderer in renderers) if (renderer != null) renderer.sharedMaterial = material;
            }
            var a = RearJoint.position; var b = BodyJoint.position;
            if (initialized && (a - previousRear).sqrMagnitude < 1e-10f && (b - previousBody).sqrMagnitude < 1e-10f) return;
            var line = b - a; float length = line.magnitude;
            // A derailed/transient rerail pose can exceed the physical sleeve
            // travel. Keep the rigid parts at their own ends without stretching
            // or throwing every LateUpdate; normal-track travel is audited.
            if (float.IsNaN(length) || float.IsInfinity(length) || length < .00001f) return;
            var direction = line / length;
            // Both tubes retain their fixed physical length and radial size.
            // Articulation changes overlap inside the sleeve, never tube scale.
            InnerSleeve.SetPositionAndRotation(a, Quaternion.LookRotation(direction, BodyRoot.up));
            OuterSleeve.SetPositionAndRotation(b, Quaternion.LookRotation(-direction, BodyRoot.up));
            SlipCollar.SetPositionAndRotation(b - direction * SleeveLength, Quaternion.LookRotation(direction, BodyRoot.up));
            previousRear = a; previousBody = b; initialized = true;
        }

        private Transform Group(Transform parent, string name)
        { var go = new GameObject(name); objects.Add(go); go.transform.SetParent(parent, false); return go.transform; }
        private Transform Marker(Transform parent, string name, Vector3 position)
        { var part = Group(parent, name); part.localPosition = position; return part; }
        private Transform Ball(Transform parent, string name, Vector3 position, float radius)
        {
            // Build the joint as lathe rings rather than importing a model or
            // giving a primitive the locomotive's complete paint atlas.
            const int rings = 9; var centres = new Vector3[rings]; var radii = new float[rings];
            for (int i = 0; i < rings; i++)
            { float angle = Mathf.PI * i / (rings - 1); centres[i] = new Vector3(0, 0, -radius * Mathf.Cos(angle)); radii[i] = Mathf.Max(.001f, radius * Mathf.Sin(angle)); }
            var part = Render(parent, name, Tube(centres, radii)); part.GetComponent<MeshFilter>().sharedMesh.RecalculateNormals();
            part.localPosition = position; return part;
        }
        private Transform Pipe(Transform parent, string name, float radius, Vector3[] points, bool rounded = true)
        {
            var centres = rounded ? Rounded(points) : points;
            var radii = new float[centres.Length]; for (int i = 0; i < radii.Length; i++) radii[i] = radius;
            return Render(parent, name, Tube(centres, radii));
        }
        private Transform Render(Transform parent, string name, Mesh mesh)
        {
            meshes.Add(mesh); mesh.name = "Meyer LP return " + name;
            var part = Group(parent, name); part.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderers.Add(renderer);
            return part;
        }
        private static Vector3[] Rounded(Vector3[] points)
        {
            var result = new List<Vector3> { points[0] };
            for (int i = 1; i < points.Length - 1; i++)
            {
                float cut = Mathf.Min(.18f, Mathf.Min(Vector3.Distance(points[i - 1], points[i]), Vector3.Distance(points[i], points[i + 1])) * .3f);
                var a = points[i] + (points[i - 1] - points[i]).normalized * cut;
                var b = points[i] + (points[i + 1] - points[i]).normalized * cut;
                for (int j = 0; j <= 4; j++)
                { float t = j / 4f; result.Add((1 - t) * (1 - t) * a + 2 * (1 - t) * t * points[i] + t * t * b); }
            }
            result.Add(points[points.Length - 1]); return result.ToArray();
        }
        private static Mesh Tube(Vector3[] centres, float[] radii)
        {
            const int sides = 16;
            var vertices = new Vector3[centres.Length * sides + 2]; var normals = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length]; var triangles = new int[(centres.Length - 1) * sides * 6 + sides * 6];
            int index = 0;
            Vector3 previousTangent = Vector3.forward, previousRight = Vector3.right;
            for (int ring = 0; ring < centres.Length; ring++)
            {
                var tangent = (centres[Math.Min(ring + 1, centres.Length - 1)] - centres[Math.Max(0, ring - 1)]).normalized;
                var reference = Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > .95f ? Vector3.forward : Vector3.up;
                var right = ring == 0 ? Vector3.Cross(reference, tangent).normalized : (Quaternion.FromToRotation(previousTangent, tangent) * previousRight).normalized;
                var up = Vector3.Cross(tangent, right);
                previousTangent = tangent; previousRight = right;
                for (int side = 0; side < sides; side++)
                {
                    float angle = 2 * Mathf.PI * side / sides; int v = ring * sides + side;
                    var normal = right * Mathf.Cos(angle) + up * Mathf.Sin(angle);
                    vertices[v] = centres[ring] + normal * radii[ring]; normals[v] = normal;
                    uv[v] = new Vector2(.6868f, .8948f) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * .004f;
                    if (ring == 0) continue;
                    int a = (ring - 1) * sides + side, b = (ring - 1) * sides + (side + 1) % sides;
                    int c = v, d = ring * sides + (side + 1) % sides;
                    triangles[index++] = a; triangles[index++] = b; triangles[index++] = c;
                    triangles[index++] = b; triangles[index++] = d; triangles[index++] = c;
                }
            }
            int start = vertices.Length - 2, end = vertices.Length - 1;
            vertices[start] = centres[0]; vertices[end] = centres[centres.Length - 1];
            normals[start] = (centres[0] - centres[1]).normalized; normals[end] = (centres[centres.Length - 1] - centres[centres.Length - 2]).normalized;
            uv[start] = uv[end] = new Vector2(.6868f, .8948f);
            for (int side = 0; side < sides; side++)
            {
                triangles[index++] = start; triangles[index++] = (side + 1) % sides; triangles[index++] = side;
                triangles[index++] = end; triangles[index++] = (centres.Length - 1) * sides + side; triangles[index++] = (centres.Length - 1) * sides + (side + 1) % sides;
            }
            var mesh = new Mesh();
            try { mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uv; mesh.triangles = triangles; mesh.RecalculateBounds(); return mesh; }
            catch { Release(mesh); throw; }
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            foreach (var part in objects) if (part != null) { part.SetActive(false); Release(part); }
            objects.Clear();
            foreach (var mesh in meshes) if (mesh != null) Release(mesh);
            meshes.Clear(); renderers.Clear(); materialSource = null; material = null;
        }
        private static void Release(Object value) { if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
