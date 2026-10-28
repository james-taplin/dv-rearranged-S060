#if B99_S060_ARTICULATED
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RearrangedS060
{
    // Meyer R2 (docs/meyer/R2-PLAN.md): the rear tanks, coal hopper and rear end are
    // rigid bodywork on the S060 body, not on the swivelling rear engine unit. The
    // unit sweeps to 1.61 m, so everything sits on a running plate at 1.62-1.66 m,
    // continuous with the S060's own. Built procedurally in car space; paint comes
    // from the S060 atlas (sampled faces), so liveries follow; nothing is shipped.
    internal sealed class S060MeyerBodywork : IDisposable
    {
        internal const float PlateY = 1.62f, PlateTop = 1.66f, TankTop = 2.82f, HopperFront = -4.38f, HopperBack = -5.55f,
            HopperTop = 3.45f, TankRear = -7.70f, BulkheadRear = -7.96f, BulkheadTop = 2.95f,
            // The beam stays where the native buffer stems expect it (old rear beam, z -7.98..-7.82):
            // the relocated buffer heads sit ~0.45 m behind its face, at -8.28..-8.57.
            BeamRear = -7.98f, BeamFront = -7.82f;
        private readonly List<GameObject> owned = new List<GameObject>();
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<(GameObject part, bool active)> hidden = new List<(GameObject, bool)>();
        internal Transform Root { get; private set; }

        private sealed class Builder
        {
            internal readonly List<Vector3> V = new List<Vector3>(); internal readonly List<int> T = new List<int>(); internal readonly List<Vector2> UV = new List<Vector2>();
            internal Vector2 Paint; internal bool Planar;
            internal void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0) { var t = b; b = c; c = t; }
                int i = V.Count; V.Add(a); V.Add(b); V.Add(c);
                foreach (var p in new[] { a, b, c }) UV.Add(Planar ? new Vector2(p.x + p.z, p.y + p.z) * .5f : Paint);
                T.Add(i); T.Add(i + 1); T.Add(i + 2);
            }
            internal void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward) { Tri(a, b, c, outward); Tri(a, c, d, outward); }
            internal void Box(Vector3 lo, Vector3 hi)
            {
                var c = (lo + hi) / 2;
                Vector3 P(int x, int y, int z) => new Vector3(x == 0 ? lo.x : hi.x, y == 0 ? lo.y : hi.y, z == 0 ? lo.z : hi.z);
                Quad(P(0,0,0), P(0,1,0), P(0,1,1), P(0,0,1), Vector3.left); Quad(P(1,0,0), P(1,0,1), P(1,1,1), P(1,1,0), Vector3.right);
                Quad(P(0,0,0), P(0,0,1), P(1,0,1), P(1,0,0), Vector3.down); Quad(P(0,1,0), P(1,1,0), P(1,1,1), P(0,1,1), Vector3.up);
                Quad(P(0,0,0), P(1,0,0), P(1,1,0), P(0,1,0), Vector3.back); Quad(P(0,0,1), P(0,1,1), P(1,1,1), P(1,0,1), Vector3.forward);
            }
            // Profile in (z,y), extruded across x0..x1; convex profiles only.
            internal void PrismX(float x0, float x1, IList<Vector2> zy)
            {
                var centre = new Vector2(zy.Average(p => p.x), zy.Average(p => p.y));
                for (int i = 1; i < zy.Count - 1; i++)
                {
                    Tri(new Vector3(x0, zy[0].y, zy[0].x), new Vector3(x0, zy[i].y, zy[i].x), new Vector3(x0, zy[i + 1].y, zy[i + 1].x), Vector3.left);
                    Tri(new Vector3(x1, zy[0].y, zy[0].x), new Vector3(x1, zy[i].y, zy[i].x), new Vector3(x1, zy[i + 1].y, zy[i + 1].x), Vector3.right);
                }
                for (int i = 0; i < zy.Count; i++)
                {
                    var a = zy[i]; var b = zy[(i + 1) % zy.Count]; var mid = (a + b) / 2 - centre;
                    Quad(new Vector3(x0, a.y, a.x), new Vector3(x1, a.y, a.x), new Vector3(x1, b.y, b.x), new Vector3(x0, b.y, b.x), new Vector3(0, mid.y, mid.x));
                }
            }
            // Profile in (x,y), extruded along z0..z1; convex profiles only.
            internal void PrismZ(float z0, float z1, IList<Vector2> xy)
            {
                var centre = new Vector2(xy.Average(p => p.x), xy.Average(p => p.y));
                for (int i = 1; i < xy.Count - 1; i++)
                {
                    Tri(new Vector3(xy[0].x, xy[0].y, z0), new Vector3(xy[i].x, xy[i].y, z0), new Vector3(xy[i + 1].x, xy[i + 1].y, z0), Vector3.back);
                    Tri(new Vector3(xy[0].x, xy[0].y, z1), new Vector3(xy[i].x, xy[i].y, z1), new Vector3(xy[i + 1].x, xy[i + 1].y, z1), Vector3.forward);
                }
                for (int i = 0; i < xy.Count; i++)
                {
                    var a = xy[i]; var b = xy[(i + 1) % xy.Count]; var mid = (a + b) / 2 - centre;
                    Quad(new Vector3(a.x, a.y, z0), new Vector3(b.x, b.y, z0), new Vector3(b.x, b.y, z1), new Vector3(a.x, a.y, z1), new Vector3(mid.x, mid.y, 0));
                }
            }
            internal void CylinderY(Vector3 baseCentre, float radius, float height, int sides = 12)
            {
                var top = baseCentre + Vector3.up * height;
                for (int i = 0; i < sides; i++)
                {
                    float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
                    var pa = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius; var pb = new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)) * radius;
                    Quad(baseCentre + pa, baseCentre + pb, top + pb, top + pa, (pa + pb) / 2);
                    Tri(top, top + pa, top + pb, Vector3.up); Tri(baseCentre, baseCentre + pb, baseCentre + pa, Vector3.down);
                }
            }
        }

        internal static S060MeyerBodywork Apply(TrainCar trainCar, Transform body, Transform liveRearFrame)
        {
            var owner = Build(trainCar.transform, body, liveRearFrame);
            try { owner.Colliders(trainCar); return owner; }
            catch { owner.Dispose(); throw; }
        }

        // Visual bodywork only (also used by the native Editor audit's render).
        internal static S060MeyerBodywork Build(Transform car, Transform body, Transform liveRearFrame)
        {
            var owner = new S060MeyerBodywork();
            try
            {
                // The old low deck, panniers and bunker box rode on the rear unit. They go.
                foreach (var name in new[] { "[Attached rear platform]", "[Meyer rear pannier L]", "[Meyer rear pannier R]", "[Period rear chassis]" })
                {
                    var part = liveRearFrame.Find(name);
                    if (part == null) throw new InvalidOperationException("Meyer rear-unit bodywork missing: " + name);
                    owner.hidden.Add((part.gameObject, part.gameObject.activeSelf)); part.gameObject.SetActive(false);
                }
                var bodyFilter = body.GetComponentsInChildren<MeshFilter>(true).First(f => f.name == "s060_body" && f.sharedMesh != null);
                var bodyMaterial = bodyFilter.GetComponent<MeshRenderer>().sharedMaterial;
                var local = bodyFilter.sharedMesh.vertices.Select(p => car.InverseTransformPoint(bodyFilter.transform.TransformPoint(p))).ToArray();
                var uv = bodyFilter.sharedMesh.uv; var tris = bodyFilter.sharedMesh.triangles;
                Vector2 Sample(Func<Vector3, Vector3, bool> match)
                {
                    float best = -1; var found = Vector2.zero;
                    for (int i = 0; i < tris.Length; i += 3)
                    {
                        Vector3 a = local[tris[i]], b = local[tris[i + 1]], c = local[tris[i + 2]];
                        var cross = Vector3.Cross(b - a, c - a); float size = cross.magnitude;
                        if (size <= best || size == 0 || !match((a + b + c) / 3, cross / size)) continue;
                        best = size; found = (uv[tris[i]] + uv[tris[i + 1]] + uv[tris[i + 2]]) / 3;
                    }
                    if (best < 0) throw new InvalidOperationException("S060 paint sample for the Meyer bodywork is missing.");
                    return found;
                }
                var green = Sample((p, n) => Mathf.Abs(n.x) > .9f && p.x < -1.1f && p.y > 1.8f && p.y < 2.5f);
                var black = Sample((p, n) => Mathf.Abs(n.y) > .9f && p.y > 1.55f && p.y < 1.70f && Mathf.Abs(p.x) < 1.3f);
                var red = Sample((p, n) => Mathf.Abs(n.z) > .9f && p.z > 3.85f && p.y > .8f && p.y < 1.3f && Mathf.Abs(p.x) < 1.2f);
                var coalRenderer = car.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r => r.sharedMaterial != null &&
                    r.sharedMaterial.name.IndexOf("coal", StringComparison.OrdinalIgnoreCase) >= 0);

                owner.Root = new GameObject("[Meyer R2 bodywork]").transform; owner.owned.Add(owner.Root.gameObject);
                owner.Root.SetParent(car, false);
                var paint = new Builder { Paint = green }; var dark = new Builder { Paint = black }; var beam = new Builder { Paint = red }; var coal = coalRenderer != null ? new Builder { Planar = true } : new Builder { Paint = black };  // without the native coal material, plain black, never atlas noise
                Build(paint, dark, beam, coal);
                owner.Add("Meyer R2 paintwork", paint, bodyMaterial);
                owner.Add("Meyer R2 ironwork", dark, bodyMaterial);
                owner.Add("Meyer R2 buffer beam", beam, bodyMaterial);
                owner.Add("Meyer R2 hopper coal", coal, coalRenderer != null ? coalRenderer.sharedMaterial : bodyMaterial);
                return owner;
            }
            catch { owner.Dispose(); throw; }
        }

        private static void Build(Builder paint, Builder dark, Builder beam, Builder coal)
        {
            // Running plate continuous with the S060's, with angle-iron edges.
            dark.Box(new Vector3(-1.40f, PlateY, BulkheadRear), new Vector3(1.40f, PlateTop, HopperFront));
            foreach (float s in new[] { -1f, 1f })
                dark.Box(new Vector3(s > 0 ? 1.37f : -1.41f, PlateY, BulkheadRear), new Vector3(s > 0 ? 1.41f : -1.37f, PlateTop + .03f, HopperFront)); // raised lip: the cylinder swing reaches 1.61 m below
            // High coal hopper behind the cab; sloped back keeps a view from the rear windows.
            paint.PrismX(-1.28f, 1.28f, new[] { new Vector2(HopperFront, PlateTop), new Vector2(HopperBack, PlateTop), new Vector2(HopperBack, 3.05f),
                new Vector2(HopperBack + .25f, HopperTop), new Vector2(HopperFront, HopperTop) });
            dark.Box(new Vector3(-1.31f, HopperTop, HopperBack + .22f), new Vector3(1.31f, HopperTop + .05f, HopperFront + .02f));
            foreach (float s in new[] { -1f, 1f })
                dark.Box(new Vector3(s * 1.28f - .015f, PlateTop, HopperFront - .02f), new Vector3(s * 1.28f + .015f, HopperTop, HopperFront + .03f));
            // Coal heaped in the hopper and a chute forward into the cab-back bunker.
            coal.PrismX(-1.20f, 1.20f, new[] { new Vector2(HopperFront - .08f, HopperTop - .04f), new Vector2(HopperBack + .30f, HopperTop - .04f),
                new Vector2(HopperBack + .45f, HopperTop + .14f), new Vector2(-4.85f, HopperTop + .22f), new Vector2(HopperFront - .20f, HopperTop + .10f) });
            dark.PrismX(-.45f, .45f, new[] { new Vector2(HopperFront + .02f, 2.92f), new Vector2(HopperFront + .02f, 3.12f), new Vector2(-4.22f, 3.00f), new Vector2(-4.22f, 2.86f) });
            // Long side tanks with the S060 front tanks' section and top line, rounded outer top edge.
            foreach (float s in new[] { -1f, 1f })
            {
                var section = new List<Vector2>();
                float inner = s * .74f, outer = s * 1.28f, r = .09f;
                section.Add(new Vector2(inner, PlateTop)); section.Add(new Vector2(outer, PlateTop));
                for (int k = 0; k <= 4; k++) { float a = k * Mathf.PI / 8; section.Add(new Vector2(outer - s * r * (1 - Mathf.Cos(a)), TankTop - r + r * Mathf.Sin(a))); }
                section.Add(new Vector2(inner, TankTop));
                if (s < 0) section.Reverse();
                paint.PrismZ(TankRear, HopperBack, section);
                dark.Box(new Vector3(Mathf.Min(inner, outer) - .01f, TankTop, TankRear), new Vector3(Mathf.Max(inner, outer) - s * r + .01f * s, TankTop + .025f, HopperBack));
                foreach (float z in new[] { -6.2f, -7.3f })
                {
                    dark.CylinderY(new Vector3(s * 1.0f, TankTop, z), .11f, .07f);
                    dark.CylinderY(new Vector3(s * 1.0f, TankTop + .07f, z), .06f, .03f, 8);
                }
                // Rivet rows along the tank bottom and top and at the ends.
                for (float z = TankRear + .06f; z < HopperBack - .03f; z += .12f)
                    foreach (float y in new[] { PlateTop + .06f, TankTop - .14f })
                        dark.Box(new Vector3(outer - .006f, y - .009f, z - .009f), new Vector3(outer + .006f, y + .009f, z + .009f));
                for (float y = PlateTop + .18f; y < TankTop - .2f; y += .14f)
                    foreach (float z in new[] { TankRear + .06f, HopperBack - .06f })
                        dark.Box(new Vector3(outer - .006f, y - .009f, z - .009f), new Vector3(outer + .006f, y + .009f, z + .009f));
                // Tank-top handrail on stanchions.
                for (float z = TankRear + .25f; z < HopperBack; z += .55f) dark.Box(new Vector3(s * 1.16f - .012f, TankTop, z - .012f), new Vector3(s * 1.16f + .012f, TankTop + .32f, z + .012f));
                dark.Box(new Vector3(s * 1.16f - .016f, TankTop + .30f, TankRear + .2f), new Vector3(s * 1.16f + .016f, TankTop + .332f, HopperBack - .05f));
            }
            // Centre well tank between the side tanks.
            paint.Box(new Vector3(-.74f, PlateTop, TankRear), new Vector3(.74f, 2.30f, HopperBack));
            dark.Box(new Vector3(-.75f, 2.30f, TankRear), new Vector3(.75f, 2.32f, HopperBack));
            // Rear bulkhead with beaded top, lamp brackets, steps and ladders.
            paint.Box(new Vector3(-1.30f, PlateTop, BulkheadRear), new Vector3(1.30f, BulkheadTop, TankRear));
            dark.Box(new Vector3(-1.32f, BulkheadTop, BulkheadRear - .02f), new Vector3(1.32f, BulkheadTop + .05f, TankRear + .02f));
            dark.Box(new Vector3(-.16f, BulkheadTop + .05f, BulkheadRear - .02f), new Vector3(.16f, BulkheadTop + .09f, BulkheadRear + .10f));
            foreach (float s in new[] { -1f, 1f })
            {
                dark.Box(new Vector3(s * 1.15f - .16f, 1.40f, BulkheadRear - .03f), new Vector3(s * 1.15f + .16f, 1.43f, BulkheadRear + .06f));
                foreach (float x in new[] { s * 1.24f, s * 1.36f })
                    dark.Box(new Vector3(x - .015f, .42f, BeamRear - .02f), new Vector3(x + .015f, PlateY, BeamFront));  // behind the rear unit's sweep (z < -7.77)
                foreach (float y in new[] { .55f, .88f, 1.21f })
                    dark.Box(new Vector3(s * 1.30f - .07f, y, BeamRear - .02f), new Vector3(s * 1.30f + .07f, y + .025f, BeamFront));
                dark.Box(new Vector3(s * 1.33f - .012f, PlateTop, BulkheadRear + .05f), new Vector3(s * 1.33f + .012f, BulkheadTop + .35f, BulkheadRear + .08f));
            }
            // Buffer beam and frame extensions on the body, clear of the rear unit's sweep (z > -7.77).
            beam.Box(new Vector3(-1.25f, .90f, BeamRear), new Vector3(1.25f, 1.25f, BeamFront));
            foreach (float s in new[] { -1f, 1f })
                dark.Box(new Vector3(s * .70f - .07f, 1.0f, BeamRear), new Vector3(s * .70f + .07f, PlateY, BeamFront + .01f));
        }

        // Static bodywork belongs to the car body's own collision compound (it is part of
        // the car, like the cab); the rear unit keeps its own bogie-owned boxes.
        private void Colliders(TrainCar trainCar)
        {
            var root = trainCar.carColliders?.collisionRoot ?? throw new InvalidOperationException("Native S060 collision root is missing.");
            var source = root.GetComponentsInChildren<Collider>(true).FirstOrDefault(c => !c.isTrigger);
            var group = new GameObject("[Meyer R2 bodywork collision]"); owned.Add(group);
            group.layer = source != null ? source.gameObject.layer : root.gameObject.layer;
            group.transform.SetParent(root, false);
            group.transform.position = trainCar.transform.position; group.transform.rotation = trainCar.transform.rotation;
            void Box(string name, Vector3 lo, Vector3 hi)
            {
                var part = new GameObject(name); part.layer = group.layer; part.transform.SetParent(group.transform, false);
                var box = part.AddComponent<BoxCollider>(); box.center = (lo + hi) / 2; box.size = hi - lo;
                if (source != null) box.sharedMaterial = source.sharedMaterial;
            }
            Box("Coal hopper", new Vector3(-1.28f, PlateTop, HopperBack), new Vector3(1.28f, HopperTop, HopperFront));
            Box("Rear tanks", new Vector3(-1.28f, PlateTop, TankRear), new Vector3(1.28f, TankTop, HopperBack));
            Box("Rear bulkhead", new Vector3(-1.30f, PlateY, BulkheadRear), new Vector3(1.30f, BulkheadTop, TankRear));
            Box("Rear running plate", new Vector3(-1.40f, PlateY, BulkheadRear), new Vector3(1.40f, PlateTop, HopperFront));
            Box("Rear buffer beam", new Vector3(-1.25f, .90f, BeamRear), new Vector3(1.25f, 1.25f, BeamFront));
        }

        private void Add(string name, Builder b, Material material)
        {
            if (b.V.Count == 0) return;
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(b.V); mesh.SetUVs(0, b.UV); mesh.SetTriangles(b.T, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
            meshes.Add(mesh);
            var part = new GameObject(name); part.transform.SetParent(Root, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh; part.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        public void Dispose()
        {
            foreach (var h in hidden) if (h.part != null) h.part.SetActive(h.active); hidden.Clear();
            foreach (var go in owned) if (go != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(go); else UnityEngine.Object.DestroyImmediate(go); } owned.Clear();
            foreach (var m in meshes) if (m != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(m); else UnityEngine.Object.DestroyImmediate(m); } meshes.Clear();
            Root = null;
        }
    }
}
#endif
