using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RearrangedS060
{
    // Visual air-brake equipment on the rigid front extension. Uses the live
    // S060 finish; native compressor/brake simulation and resource ports stay
    // with the original locomotive. No second compressor simulation is added.
    internal sealed class S060MeyerFrontEquipment :
#if !UNITY_EDITOR
        MonoBehaviour,
#endif
        IDisposable
    {
        internal Transform Root { get; private set; }
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<MeshRenderer> renderers = new List<MeshRenderer>();
        private MeshRenderer source;
        private Material material;
        private bool disposed;
#if !UNITY_EDITOR
        private bool destroying;
#endif

        internal static S060MeyerFrontEquipment Bind(Transform car, Transform body)
        {
#if UNITY_EDITOR
            var owner = new S060MeyerFrontEquipment();
#else
            var owner = car.gameObject.AddComponent<S060MeyerFrontEquipment>();
#endif
            try
            {
                owner.source = body.GetComponentsInChildren<MeshRenderer>(true)
                    .Where(r => r.name == "s060_body" && r.sharedMaterial != null)
                    .OrderByDescending(r => r.enabled && r.gameObject.activeInHierarchy).First();
                owner.material = owner.source.sharedMaterial;
                var root = new GameObject("[Meyer front air-brake equipment]");
                root.transform.SetParent(car, false); owner.Root = root.transform;
                owner.Build(); owner.Combine(); return owner;
            }
            catch { owner.Dispose(); throw; }
        }

        private void Build()
        {
            foreach (int side in new[] { -1, 1 })
            {
                // Close the vacated cylinder slots with a connected steel
                // running-plate extension and two transverse mounting saddles.
                Box("Front running plate " + side, new Vector3(side < 0 ? -1.30f : .48f, 1.54f, 2.55f),
                    new Vector3(side < 0 ? -.48f : 1.30f, 1.62f, 3.70f));
                foreach (float z in new[] { 2.80f, 3.42f })
                {
                    Box("Equipment bearer " + side + " " + z,
                        new Vector3(side < 0 ? -1.27f : .38f, .72f, z - .055f),
                        new Vector3(side < 0 ? -.38f : 1.27f, .80f, z + .055f));
                    Box("Equipment hanger " + side + " " + z,
                        new Vector3(side < 0 ? -.66f : .55f, .77f, z - .055f),
                        new Vector3(side < 0 ? -.55f : .66f, 1.56f, z + .055f));
                }
            }

            const float x = -1.04f, zc = 3.08f;
            Box("Pump mounting plinth", new Vector3(-1.29f, .77f, 2.75f), new Vector3(-.79f, .81f, 3.48f));
            // A vertical steam-driven pump: separate air and steam barrels,
            // exposed piston neck, tie rods, head flanges and service pipework.
            Barrel("Pump air cylinder", new Vector3(x, 0, zc), Vector3.up,
                new[] { .80f, .82f, .84f, 1.08f, 1.10f, 1.12f }, new[] { .23f, .23f, .21f, .21f, .23f, .23f });
            Barrel("Pump steam cylinder", new Vector3(x, 0, zc), Vector3.up,
                new[] { 1.27f, 1.29f, 1.31f, 1.48f, 1.50f, 1.52f }, new[] { .18f, .18f, .16f, .16f, .18f, .18f });
            Pipe("Pump piston neck", .027f, new Vector3(x, 1.10f, zc), new Vector3(x, 1.29f, zc));
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
            {
                var offset = new Vector3(sx * .115f, 0, sz * .115f);
                Pipe("Pump tie rod " + sx + " " + sz, .012f,
                    new Vector3(x, 1.10f, zc) + offset, new Vector3(x, 1.30f, zc) + offset);
            }
            foreach (float y in new[] { .825f, 1.105f, 1.285f, 1.505f })
                foreach (int i in Enumerable.Range(0, 8))
                {
                    float angle = i * Mathf.PI / 4;
                    var p = new Vector3(x + Mathf.Cos(angle) * (y < 1.2f ? .192f : .145f), y, zc + Mathf.Sin(angle) * (y < 1.2f ? .192f : .145f));
                    Pipe("Pump head bolt " + y + " " + i, .016f, p - Vector3.up * .016f, p + Vector3.up * .016f);
                }
            Pipe("Pump steam service", .022f, new Vector3(-.70521827f, 2.10f, 3.16791177f),
                new Vector3(-.70521827f, 1.77f, 3.16791177f), new Vector3(x, 1.77f, zc), new Vector3(x, 1.50f, zc));
            Pipe("Pump exhaust service", .025f, new Vector3(x + .13f, 1.39f, zc),
                new Vector3(-.72f, 1.39f, zc), new Vector3(-.72f, 1.88f, 3.48f), new Vector3(-.52f, 1.88f, 3.48f));

            // Main reservoir on the opposite side, with band straps seated on
            // the same bearers and an air delivery pipe from the pump.
            Barrel("Main air reservoir", new Vector3(1.04f, 1.10f, 0), Vector3.forward,
                new[] { 2.72f, 2.75f, 2.80f, 3.52f, 3.57f, 3.60f }, new[] { .03f, .15f, .23f, .23f, .15f, .03f });
            foreach (float z in new[] { 2.80f, 3.42f })
            {
                Barrel("Reservoir strap " + z, new Vector3(1.04f, 1.10f, 0), Vector3.forward,
                    new[] { z - .025f, z + .025f }, new[] { .244f, .244f });
                Box("Reservoir saddle " + z, new Vector3(.90f, .78f, z - .035f), new Vector3(1.18f, .89f, z + .035f));
            }
            Pipe("Pump air delivery", .025f, new Vector3(x, .95f, zc + .20f),
                new Vector3(x, .95f, 3.48f), new Vector3(1.04f, .95f, 3.48f), new Vector3(1.04f, 1.10f, 3.56f));
        }

        private void Barrel(string name, Vector3 origin, Vector3 axis, float[] stations, float[] radii)
            => Render(name, S060MeyerExhaustPipe.Tube(stations.Select(t => origin + axis * t).ToArray(), radii));
        private void Pipe(string name, float radius, params Vector3[] points)
            => Render(name, S060MeyerExhaustPipe.PathMesh(points, radius));
        private void Combine()
        {
            // Rigid equipment shares one native material. Keep it to a single
            // draw rather than leaving every head bolt and bracket a renderer.
            var parts = Root.GetComponentsInChildren<MeshFilter>();
            var combined = new Mesh { name = "Meyer mounted air-brake equipment", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            try
            {
                combined.CombineMeshes(parts.Select(f => new CombineInstance {
                    mesh = f.sharedMesh, transform = Root.worldToLocalMatrix * f.transform.localToWorldMatrix }).ToArray(), true, true);
                foreach (var part in parts) { part.gameObject.SetActive(false); Release(part.gameObject); }
                foreach (var mesh in meshes) Release(mesh);
                meshes.Clear(); renderers.Clear();
                Render("Mounted steam pump and air reservoir", combined);
            }
            catch { if (!meshes.Contains(combined)) Release(combined); throw; }
        }
        private void Render(string name, Mesh mesh)
        {
            meshes.Add(mesh); mesh.name = "Meyer air equipment " + name;
            var go = new GameObject(name); go.transform.SetParent(Root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderers.Add(renderer);
        }
        private void Box(string name, Vector3 lo, Vector3 hi)
        {
            var points = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            void Face(Vector3 n, params Vector3[] q)
            {
                if (Vector3.Dot(Vector3.Cross(q[1] - q[0], q[2] - q[0]), n) < 0) q = new[] { q[0], q[3], q[2], q[1] };
                int a = points.Count; points.AddRange(q); normals.AddRange(Enumerable.Repeat(n, 4)); uv.AddRange(Enumerable.Repeat(new Vector2(.6868f, .8948f), 4));
                triangles.AddRange(new[] { a, a + 1, a + 2, a, a + 2, a + 3 });
            }
            foreach (float a in new[] { lo.x, hi.x }) Face(a == lo.x ? Vector3.left : Vector3.right, new Vector3(a, lo.y, lo.z), new Vector3(a, hi.y, lo.z), new Vector3(a, hi.y, hi.z), new Vector3(a, lo.y, hi.z));
            foreach (float a in new[] { lo.y, hi.y }) Face(a == lo.y ? Vector3.down : Vector3.up, new Vector3(lo.x, a, lo.z), new Vector3(lo.x, a, hi.z), new Vector3(hi.x, a, hi.z), new Vector3(hi.x, a, lo.z));
            foreach (float a in new[] { lo.z, hi.z }) Face(a == lo.z ? Vector3.back : Vector3.forward, new Vector3(lo.x, lo.y, a), new Vector3(hi.x, lo.y, a), new Vector3(hi.x, hi.y, a), new Vector3(lo.x, hi.y, a));
            var mesh = new Mesh(); mesh.SetVertices(points); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds(); Render(name, mesh);
        }
        private void LateUpdate()
        {
            if (disposed || source == null || source.sharedMaterial == material) return;
            material = source.sharedMaterial;
            foreach (var renderer in renderers) if (renderer != null) renderer.sharedMaterial = material;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
#if !UNITY_EDITOR
            enabled = false;
#endif
            if (Root != null) { Root.gameObject.SetActive(false); Release(Root.gameObject); }
            foreach (var mesh in meshes) if (mesh != null) Release(mesh);
            meshes.Clear(); renderers.Clear(); source = null; material = null;
#if !UNITY_EDITOR
            if (!destroying) Release(this);
#endif
        }
        private static void Release(Object value) { if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
#if !UNITY_EDITOR
        private void OnDestroy() { destroying = true; Dispose(); }
#endif
    }
}
