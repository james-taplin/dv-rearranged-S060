using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RearrangedS060
{
    // Fixed admission and unit-local interstage elbows for the inward-facing
    // Meyer. The existing donor dry-pipe meshes still make the moving spans;
    // no simulation ports or meshes in the asset pack are changed.
    internal sealed class S060MeyerInwardPipes : IDisposable
    {
        internal const float AdmissionX = 1.50f, AdmissionY = 1.42f, AdmissionCourseY = 1.60f, AdmissionRadius = .055f;
        internal const float TransferX = 1.55f, TransferY = 1.34f;
        internal const float TransferJointRadius = .13f;
        internal Transform BodyRoot { get; private set; }
        internal Transform FrontRoot { get; private set; }
        internal Transform RearRoot { get; private set; }
        private readonly Dictionary<int, Vector3> admission = new Dictionary<int, Vector3>();
        private readonly Dictionary<int, Transform> fronts = new Dictionary<int, Transform>(), rears = new Dictionary<int, Transform>();
        private readonly Dictionary<int, Transform> admissionEnds = new Dictionary<int, Transform>();
        private sealed class Feed
        { internal Transform Part, End; internal Vector3 Start, PreviousA, PreviousB; internal bool Initialized; }
        private readonly List<Feed> feeds = new List<Feed>();
        private readonly List<GameObject> objects = new List<GameObject>();
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<MeshRenderer> renderers = new List<MeshRenderer>();
        private MeshRenderer materialSource;
        private Material material;
        private bool disposed;
        internal Vector3 AdmissionSocket(int physicalSide) => admission[physicalSide];
        internal Transform FrontTransfer(int physicalSide) => fronts[physicalSide];
        internal Transform RearTransfer(int physicalSide) => rears[physicalSide];
        internal Transform FrontAdmission(int physicalSide) => admissionEnds[physicalSide];
        internal static bool IsInward(Transform body, Transform front) => front.Find("[Native S060 cylinders]") != null &&
            Vector3.Dot(body.forward, front.forward) < -.5f;

        internal static S060MeyerInwardPipes Bind(Transform body, Transform front, Transform rear, Transform nativeShell,
            Vector3 leftBoiler, Vector3 rightBoiler, Vector3 leftStem, Vector3 rightStem)
        {
            var owner = new S060MeyerInwardPipes();
            try
            {
                owner.materialSource = nativeShell.GetComponentsInChildren<MeshRenderer>(true)
                    .Where(r => r.name == "s060_body" && r.sharedMaterial != null)
                    .OrderByDescending(r => r.enabled && r.gameObject.activeInHierarchy).FirstOrDefault();
                if (owner.materialSource == null) throw new InvalidOperationException("Live S060 material required for inward Meyer steam pipes.");
                owner.material = owner.materialSource.sharedMaterial;
                owner.BodyRoot = owner.Group(body, "[Meyer inward admission]");
                owner.FrontRoot = owner.Group(front, "[Meyer HP transfer elbows]");
                owner.RearRoot = owner.Group(rear, "[Meyer LP transfer elbows]");
                foreach (int side in new[] { -1, 1 })
                {
                    var boiler = side < 0 ? leftBoiler : rightBoiler;
                    var socket = new Vector3(side * AdmissionX, AdmissionY, -.30f);
                    owner.admission.Add(side, socket);
                    // One low course: drop beside the native boiler neck,
                    // clear the front equipment outboard, then turn inboard
                    // ahead of the cab through a short swivelling feed.
                    owner.Path(owner.BodyRoot, "HP boiler admission " + side, AdmissionRadius, new[] {
                        boiler, boiler + Vector3.down * .18f,
                        new Vector3(side * AdmissionX, boiler.y - .18f, boiler.z),
                        new Vector3(side * AdmissionX, AdmissionCourseY, boiler.z),
                        new Vector3(side * AdmissionX, AdmissionCourseY, socket.z), socket });
                    owner.Joint(owner.BodyRoot, "[HP admission swivel " + side + "]", socket, .09f);
                    var stem = owner.Joint(owner.FrontRoot, "[HP admission stem " + side + "]", side < 0 ? leftStem : rightStem, .12f);
                    owner.admissionEnds.Add(side, stem);
                    var feed = owner.Render(owner.BodyRoot, "HP admission flex " + side,
                        S060MeyerExhaustPipe.PathMesh(new[] { Vector3.zero, Vector3.forward }, AdmissionRadius, false));
                    owner.feeds.Add(new Feed { Part = feed, End = stem, Start = socket });

                    // Yaw180 swaps authored L/R. The existing transfer faces
                    // keep their actual normals: HP leaves local-back, LP
                    // receives local-forward, before each turns out/down.
                    var f = front.Find("[S060 manifold " + (side < 0 ? "R" : "L") + "]");
                    var r = rear.Find("[S060 manifold " + (side < 0 ? "L" : "R") + "]");
                    if (f == null || r == null) throw new InvalidOperationException("Existing Meyer interstage manifolds required.");
                    var fc = front.InverseTransformPoint(f.Find("[transfer socket]").position);
                    var rc = rear.InverseTransformPoint(r.Find("[transfer socket]").position);
                    var fn = front.InverseTransformDirection(f.TransformDirection(Vector3.back));
                    var rn = rear.InverseTransformDirection(r.TransformDirection(Vector3.forward));
                    var fj = new Vector3(-side * TransferX, TransferY, fc.z - .20f);
                    var rj = new Vector3(side * TransferX, TransferY, rc.z + .20f);
                    owner.Path(owner.FrontRoot, "HP interstage elbow " + side, .075f, new[] { fc, fc + fn * .16f, fj });
                    owner.Path(owner.RearRoot, "LP interstage elbow " + side, .075f, new[] { rc, rc + rn * .16f, rj });
                    owner.fronts.Add(side, owner.Joint(owner.FrontRoot, "[HP interstage swivel " + side + "]", fj, TransferJointRadius));
                    owner.rears.Add(side, owner.Joint(owner.RearRoot, "[LP interstage swivel " + side + "]", rj, TransferJointRadius));
                }
                owner.UpdateGeometry(); return owner;
            }
            catch { owner.Dispose(); throw; }
        }
        internal void UpdateMaterial()
        {
            if (disposed || materialSource == null || materialSource.sharedMaterial == material) return;
            material = materialSource.sharedMaterial;
            foreach (var renderer in renderers) if (renderer != null) renderer.sharedMaterial = material;
        }
        internal void UpdateGeometry()
        {
            UpdateMaterial();
            if (disposed || BodyRoot == null) return;
            foreach (var feed in feeds)
            {
                if (feed.Part == null || feed.End == null) continue;
                var a = BodyRoot.TransformPoint(feed.Start); var b = feed.End.position;
                if (feed.Initialized && a.Equals(feed.PreviousA) && b.Equals(feed.PreviousB)) continue;
                if (!S060PipePose.Span(a, b, BodyRoot.up, out var line, out float length)) continue;
                feed.Part.SetPositionAndRotation(a, Quaternion.LookRotation(line, BodyRoot.up));
                feed.Part.localScale = new Vector3(1, 1, length);
                feed.PreviousA = a; feed.PreviousB = b; feed.Initialized = true;
            }
        }
        internal static Mesh TrimUpperCourse(Mesh source, float cutY, out Vector3 axis)
        {
            // Use the actual lower stem section, preserving complete lower
            // casting/transfer faces and each original material section.
            var vertices = source.vertices.ToList(); var uv = source.uv.ToList();
            bool hasUv = uv.Count == vertices.Count;
            var sections = new List<Vector3>(); var output = new List<int>[source.subMeshCount];
            for (int sub = 0; sub < output.Length; sub++)
            {
                output[sub] = new List<int>(); var old = source.GetTriangles(sub);
                for (int i = 0; i < old.Length; i += 3)
                {
                    var ids = new[] { old[i], old[i + 1], old[i + 2] };
                    if (ids.All(j => vertices[j].y <= cutY)) { output[sub].AddRange(ids); continue; }
                    var poly = new List<(Vector3 p, Vector2 uv)>();
                    for (int e = 0; e < 3; e++)
                    {
                        int ia = ids[e], ib = ids[(e + 1) % 3]; var a = vertices[ia]; var b = vertices[ib];
                        var auv = hasUv ? uv[ia] : Vector2.zero; var buv = hasUv ? uv[ib] : Vector2.zero;
                        if (a.y <= cutY) poly.Add((a, auv));
                        if ((a.y <= cutY) == (b.y <= cutY)) continue;
                        float t = (cutY - a.y) / (b.y - a.y); var p = Vector3.LerpUnclamped(a, b, t);
                        sections.Add(p); poly.Add((p, Vector2.LerpUnclamped(auv, buv, t)));
                    }
                    if (poly.Count < 3) continue;
                    int start = vertices.Count;
                    foreach (var item in poly) { vertices.Add(item.p); if (hasUv) uv.Add(item.uv); }
                    for (int j = 1; j + 1 < poly.Count; j++) output[sub].AddRange(new[] { start, start + j, start + j + 1 });
                }
            }
            var ring = sections.GroupBy(p => new Vector3(Mathf.Round(p.x * 100000), Mathf.Round(p.y * 100000), Mathf.Round(p.z * 100000))).Select(g => g.First()).ToArray();
            if (ring.Length < 6) throw new InvalidOperationException("Measured Meyer admission stem section is missing.");
            axis = new Vector3((ring.Min(p => p.x) + ring.Max(p => p.x)) / 2, cutY, (ring.Min(p => p.z) + ring.Max(p => p.z)) / 2);
            var centre = axis;
            ring = ring.OrderBy(p => Mathf.Atan2(p.z - centre.z, p.x - centre.x)).ToArray();
            for (int i = 0; i < ring.Length; i++)
            {
                var a = ring[i]; var b = ring[(i + 1) % ring.Length];
                if (Vector3.Cross(a - axis, b - axis).y < 0) { var swap = a; a = b; b = swap; }
                int start = vertices.Count; vertices.AddRange(new[] { axis, a, b }); output[0].AddRange(new[] { start, start + 1, start + 2 });
                if (hasUv) uv.AddRange(Enumerable.Repeat(new Vector2(.6868f, .8948f), 3));
            }
            var copy = Object.Instantiate(source);
            try
            {
                var used = output.SelectMany(t => t).Distinct().ToArray();
                var map = used.Select((old, current) => (old, current)).ToDictionary(v => v.old, v => v.current);
                copy.name = source.name + " (retained Meyer chest)"; copy.Clear(); copy.subMeshCount = output.Length;
                copy.SetVertices(used.Select(j => vertices[j]).ToList()); if (hasUv) copy.SetUVs(0, used.Select(j => uv[j]).ToList());
                for (int sub = 0; sub < output.Length; sub++) copy.SetTriangles(output[sub].Select(j => map[j]).ToArray(), sub);
                copy.RecalculateNormals(); if (hasUv) copy.RecalculateTangents();
                copy.RecalculateBounds();
                return copy;
            }
            catch { Release(copy); throw; }
        }
        private Transform Group(Transform parent, string name)
        { var go = new GameObject(name); objects.Add(go); go.transform.SetParent(parent, false); return go.transform; }
        private Transform Render(Transform parent, string name, Mesh mesh)
        {
            meshes.Add(mesh); mesh.name = "Meyer inward " + name;
            var part = Group(parent, name); part.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderers.Add(renderer);
            return part;
        }
        private Transform Path(Transform parent, string name, float radius, Vector3[] points)
            => Render(parent, name, S060MeyerExhaustPipe.PathMesh(points, radius));
        private Transform Joint(Transform parent, string name, Vector3 point, float radius)
        { var joint = Render(parent, name, S060MeyerExhaustPipe.JointMesh(radius)); joint.localPosition = point; return joint; }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            foreach (var item in objects) if (item != null) { item.SetActive(false); Release(item); }
            objects.Clear();
            foreach (var mesh in meshes) if (mesh != null) Release(mesh);
            meshes.Clear(); renderers.Clear(); fronts.Clear(); rears.Clear(); admission.Clear(); admissionEnds.Clear(); feeds.Clear(); materialSource = null; material = null;
        }
        private static void Release(Object value) { if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
