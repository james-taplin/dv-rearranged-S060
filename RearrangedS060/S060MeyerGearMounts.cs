using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RearrangedS060
{
    // Fixed motion brackets and suspension fittings on each powered frame.
    // The native wheel, crank journal and simulation stay authoritative.
    internal sealed class S060MeyerGearMounts :
#if !UNITY_EDITOR
        MonoBehaviour,
#endif
        IDisposable
    {
        private readonly List<(Transform part, Vector3 position)> homes = new List<(Transform, Vector3)>();
        private readonly List<(Transform part, Vector3 scale)> guideScales = new List<(Transform, Vector3)>();
        private readonly List<(MeshFilter filter, Mesh original, Mesh fitted)> rods = new List<(MeshFilter, Mesh, Mesh)>();
        private readonly List<(MeshFilter filter, Mesh original)> wheels = new List<(MeshFilter, Mesh)>();
        private readonly Dictionary<Mesh, Mesh> wheelCopies = new Dictionary<Mesh, Mesh>();
        private readonly List<Mesh> meshes = new List<Mesh>();
        private Transform root;
        private MeshRenderer renderer, materialSource;
        private Material material;
        private bool disposed;
#if !UNITY_EDITOR
        private bool destroying;
#endif
        internal static S060MeyerGearMounts Bind(Transform frame, Transform body)
        {
#if UNITY_EDITOR
            var owner = new S060MeyerGearMounts();
#else
            var owner = frame.gameObject.AddComponent<S060MeyerGearMounts>();
#endif
            try
            {
                owner.materialSource = body.GetComponentsInChildren<MeshRenderer>(true)
                    .Where(r => r.name == "s060_body" && r.sharedMaterial != null).OrderByDescending(r => r.enabled).First();
                owner.material = owner.materialSource.sharedMaterial;
                var group = new GameObject("[Meyer motion brackets and spring mounts]"); group.transform.SetParent(frame, false); owner.root = group.transform;
                owner.FitNativeRods(frame, body); owner.Build(frame); return owner;
            }
            catch { owner.Dispose(); throw; }
        }
        private void FitNativeRods(Transform frame, Transform body)
        {
            foreach (bool coupling in new[] { true, false })
            {
                string meshName = coupling ? "s060_mech_wheels_connect" : "s060_mech_push_rod_to_connect";
                var source = body.GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh)
                    .FirstOrDefault(m => m != null && m.name == meshName);
                if (source == null) throw new InvalidOperationException("Native Meyer rod source missing: " + meshName);
                foreach (int sign in new[] { -1, 1 })
                {
                    var group = frame.Find("[Meyer rods " + (sign < 0 ? "L" : "R") + "]");
                    var part = group.Find(coupling ? "Coupling rod" : "Main rod"); var filter = part.GetComponent<MeshFilter>();
                    var fitted = S060RodMesh.NativeMeyer(source, part.localScale.z, coupling, sign);
                    rods.Add((filter, filter.sharedMesh, fitted)); filter.sharedMesh = fitted;
                    if (!coupling) foreach (var guide in group.Cast<Transform>().Where(t => t.name.StartsWith("Crosshead guide ", StringComparison.Ordinal)))
                    {
                        // Keep the native eye and rotating journal clear; the
                        // existing guides bear on the sliding block's outer face.
                        MoveInboard(guide, guide.localPosition.x + sign * .085f);
                        guideScales.Add((guide, guide.localScale)); var scale = guide.localScale; scale.x = .05f; guide.localScale = scale;
                    }
                }
            }
        }
        private void Build(Transform frame)
        {
            var structure = frame.Find("[Powered bogie frame]") ?? throw new InvalidOperationException("Meyer frame missing.");
            foreach (int side in new[] { -1, 1 })
            {
                var edge = structure.Find("Visible upper frame edge " + side); MoveInboard(edge, side * .61f);
                foreach (int axle in new[] { -1, 1 })
                {
                    foreach (var leaf in structure.Cast<Transform>().Where(t => t.name.StartsWith("Leaf spring " + side + " " + axle + " ", StringComparison.Ordinal))) MoveInboard(leaf, side * .61f);
                    float z = axle * (float)MeyerPlan.AxlePitch / 2;
                    Box(side, .56f, .66f, 1.09f, 1.135f, z - .065f, z + .065f);
                    foreach (int end in new[] { -1, 1 })
                    {
                        float at = z + end * .30f;
                        Box(side, .57f, .65f, 1.02f, 1.14f, at - .025f, at + .025f);
                        Cylinder(new Vector3(side * .55f, 1.128f, at), new Vector3(side * .67f, 1.128f, at), .019f);
                    }
                }
                var rods = frame.Find("[Meyer rods " + (side < 0 ? "L" : "R") + "]");
                var ties = rods.Cast<Transform>().Where(t => t.name == "Guide lower tie").ToArray();
                if (ties.Length != 2) throw new InvalidOperationException("Two actual Meyer guide end ties required.");
                float first = ties.Min(t => t.localPosition.z), last = ties.Max(t => t.localPosition.z);
                // Motion bracket crosses BETWEEN the two wheel envelopes.
                // The outer longitudinal carrier stays beyond the long native
                // journal, with end returns into the existing guide ties.
                Box(side, .58f, .66f, .98f, 1.23f, -.017f, .017f);
                Box(side, .60f, 1.24f, 1.20f, 1.25f, -.017f, .017f);
                Gusset(side);
                Box(side, 1.17f, 1.24f, 1.20f, 1.25f, Mathf.Min(0, first) - .025f, last + .025f);
                foreach (float z in new[] { first, last }) Box(side, 1.14f, 1.24f, .59f, 1.25f, z - .025f, z + .025f);
            }
            FitWheels(frame);
            Combine();
        }
        // Dockside uses the same native01 wheel and displaces the same two
        // original return-crank islands. Keep the native journal and restoration.
        internal static S060MeyerGearMounts BindWheelCranks(Transform frame)
        {
#if UNITY_EDITOR
            var owner = new S060MeyerGearMounts();
#else
            var owner = frame.gameObject.AddComponent<S060MeyerGearMounts>();
#endif
            try { owner.FitWheels(frame); return owner; }
            catch { owner.Dispose(); throw; }
        }
        private void FitWheels(Transform frame)
        {
            foreach (var filter in frame.Find("[axle] rear").GetComponentsInChildren<MeshFilter>(true))
            {
                var source = filter.sharedMesh;
                if (source == null || !source.name.StartsWith("s060_Wheels_01", StringComparison.Ordinal)) continue;
                if (!source.isReadable) throw new InvalidOperationException("Readable native01 wheel required for articulated journal fitting.");
                if (!wheelCopies.TryGetValue(source, out var copy))
                {
                    var triangles = Enumerable.Range(0, source.subMeshCount).Select(source.GetTriangles).ToArray();
                    var ids = S060MeyerClearance.Islands(source.vertices, triangles, out var bounds);
                    bool ReturnCrank(Bounds b) =>
                        (b.min.x > 1.07f && b.min.x < 1.09f && b.max.x > 1.11f && b.max.x < 1.13f && b.min.y > -.48f && b.min.y < -.45f && b.max.y > 0 && b.max.y < .02f && b.min.z > -.20f && b.min.z < -.18f && b.max.z > .12f && b.max.z < .14f) ||
                        (b.min.x > 1.06f && b.min.x < 1.08f && b.max.x > 1.16f && b.max.x < 1.19f && b.min.y > -.06f && b.max.y < 0 && b.min.z > -.17f && b.max.z < -.12f);
                    var remove = bounds.Select(ReturnCrank).ToArray();
                    if (remove.Count(b => b) != 2) throw new InvalidOperationException("Both measured unused native return-crank islands required.");
                    copy = Object.Instantiate(source); copy.name = source.name + " (main journal retained)";
                    wheelCopies.Add(source, copy);
                    for (int sub = 0; sub < triangles.Length; sub++) copy.SetTriangles(Enumerable.Range(0, triangles[sub].Length / 3).Where(i => !remove[ids[triangles[sub][3 * i]]]).SelectMany(i => new[] { triangles[sub][3 * i], triangles[sub][3 * i + 1], triangles[sub][3 * i + 2] }).ToArray(), sub);
                }
                wheels.Add((filter, source)); filter.sharedMesh = copy;
            }
            if (wheels.Count != 2) throw new InvalidOperationException("Both main-rod wheels must retain their native01 journals.");
        }
        private void MoveInboard(Transform part, float x)
        { if (part == null) throw new InvalidOperationException("Meyer suspension datum missing."); homes.Add((part, part.localPosition)); var p = part.localPosition; p.x = x; part.localPosition = p; }
        private void Cylinder(Vector3 from, Vector3 to, float radius) => Add(S060MeyerExhaustPipe.PathMesh(new[] { from, to }, radius));
        private void Box(int side, float minX, float maxX, float loY, float hiY, float loZ, float hiZ)
        {
            float a = side > 0 ? minX : -maxX, b = side > 0 ? maxX : -minX;
            var v = new List<Vector3>(); var t = new List<int>();
            void Face(Vector3 n, params Vector3[] p) { if (Vector3.Dot(Vector3.Cross(p[1] - p[0], p[2] - p[0]), n) < 0) p = new[] { p[0], p[3], p[2], p[1] }; int s = v.Count; v.AddRange(p); t.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 }); }
            foreach (float x in new[] { a, b }) Face(x == a ? Vector3.left : Vector3.right, new Vector3(x, loY, loZ), new Vector3(x, hiY, loZ), new Vector3(x, hiY, hiZ), new Vector3(x, loY, hiZ));
            foreach (float y in new[] { loY, hiY }) Face(y == loY ? Vector3.down : Vector3.up, new Vector3(a, y, loZ), new Vector3(a, y, hiZ), new Vector3(b, y, hiZ), new Vector3(b, y, loZ));
            foreach (float z in new[] { loZ, hiZ }) Face(z == loZ ? Vector3.back : Vector3.forward, new Vector3(a, loY, z), new Vector3(b, loY, z), new Vector3(b, hiY, z), new Vector3(a, hiY, z));
            Mesh(v.ToArray(), t.ToArray());
        }
        private void Gusset(int side)
        {
            var v = new[] { new Vector3(side * .64f, 1.02f, -.015f), new Vector3(side * .64f, 1.225f, -.015f), new Vector3(side * 1.22f, 1.225f, -.015f), new Vector3(side * .64f, 1.02f, .015f), new Vector3(side * .64f, 1.225f, .015f), new Vector3(side * 1.22f, 1.225f, .015f) };
            var t = new[] { 0, 2, 1, 3, 4, 5, 0, 1, 4, 0, 4, 3, 1, 2, 5, 1, 5, 4, 2, 0, 3, 2, 3, 5 };
            if (side > 0) for (int i = 0; i < t.Length; i += 3) { int swap = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = swap; }
            Mesh(v, t);
        }
        private void Mesh(Vector3[] vertices, int[] triangles)
        { var mesh = new Mesh(); mesh.vertices = vertices; mesh.triangles = triangles; mesh.uv = Enumerable.Repeat(new Vector2(.6868f, .8948f), vertices.Length).ToArray(); mesh.RecalculateNormals(); mesh.RecalculateBounds(); Add(mesh); }
        private void Add(Mesh mesh) => meshes.Add(mesh);
        private void Combine()
        {
            var combined = new Mesh { name = "Meyer connected motion and suspension mounts", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            try { combined.CombineMeshes(meshes.Select(m => new CombineInstance { mesh = m, transform = Matrix4x4.identity }).ToArray()); }
            catch { Release(combined); throw; }
            foreach (var mesh in meshes) Release(mesh); meshes.Clear(); meshes.Add(combined);
            root.gameObject.AddComponent<MeshFilter>().sharedMesh = combined;
            renderer = root.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        }
        private void LateUpdate() { if (!disposed && materialSource != null && materialSource.sharedMaterial != material) { material = materialSource.sharedMaterial; renderer.sharedMaterial = material; } }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
#if !UNITY_EDITOR
            enabled = false;
#endif
            foreach (var wheel in wheels) if (wheel.filter != null) wheel.filter.sharedMesh = wheel.original;
            foreach (var rod in rods) { if (rod.filter != null && rod.filter.sharedMesh == rod.fitted) rod.filter.sharedMesh = rod.original; Release(rod.fitted); } rods.Clear();
            foreach (var guide in guideScales) if (guide.part != null) guide.part.localScale = guide.scale; guideScales.Clear();
            foreach (var mesh in wheelCopies.Values) Release(mesh); wheelCopies.Clear(); wheels.Clear();
            foreach (var home in homes) if (home.part != null) home.part.localPosition = home.position; homes.Clear();
            if (root != null) { root.gameObject.SetActive(false); Release(root.gameObject); }
            foreach (var mesh in meshes) Release(mesh); meshes.Clear();
#if !UNITY_EDITOR
            if (!destroying) Release(this);
#endif
        }
#if !UNITY_EDITOR
        private void OnDestroy() { destroying = true; Dispose(); }
#endif
        private static void Release(Object value) { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
