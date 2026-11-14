using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RearrangedS060
{
    // 0-4-0: drop the middle driver, leaving drivers 1 and 3 coupled 3.10 m apart.
    //
    // Driver 2 is the one that goes, for two measured reasons: Axle_R owns only
    // driver 1, so taking driver 1 would leave a bogie with no axles, and the native
    // main-rod crank is on driver 1 (probe pivot follows its measured pin),
    // so driver 1 must remain to keep the gear driven. The native coupling-rod
    // animation stays in place, with an owned two-boss
    // mesh fitted to drivers 1 and 3; no unsupported middle boss remains.
    //
    // The bogie pivots are deliberately left where DV put them (+0.929, -1.396).
    // Proven in game on 20 Oct 2026: the car rides and curves as stock.
    internal sealed class S060FourCoupled : S060Conversion
    {
        private const int DroppedDriver = 2;

        private readonly List<Renderer> hiddenCalipers = new List<Renderer>();
        private readonly List<Transform> hiddenWheels = new List<Transform>();
        private Transform droppedAxle;
        private S060FourCoupledAirEquipment airEquipment;

        internal override string Name => "0-4-0 (drivers 1 and 3)";

        protected override void Apply()
        {
            foreach (var wheel in Parts.Wheels(DroppedDriver))
            {
                wheel.gameObject.SetActive(false);
                hiddenWheels.Add(wheel);
            }

            // That axle's brake caliper, which LocoMeshSplitter has already separated.
            foreach (var caliper in Parts.CaliperRenderers(DroppedDriver))
            {
                if (!caliper.enabled) continue;
                caliper.enabled = false;
                hiddenCalipers.Add(caliper);
            }

            // BogieAxlesPatch drops inactive axles from the rebuilt list, and nulling
            // the field is what forces that rebuild.
            droppedAxle = Parts.AxleFor(DroppedDriver)
                ?? throw new InvalidOperationException("No [axle] maps to driver " + DroppedDriver + ".");
            droppedAxle.gameObject.SetActive(false);
            Parts.BogieFor(DroppedDriver).axles = null;

            // Two physical powered axles remain. Use the authored 38 t effective
            // adhesive allowance at the fixed 50 t design datum, rather than
            // granting stock grip to two overloaded driver axles. Native mass,
            // brakes and suspension are not changed by this design allowance.
            var powered = new[] { Parts.PoweredFor(1), Parts.PoweredFor(3) };
            if (powered.Any(p => p == null))
                throw new InvalidOperationException("Drivers 1 and 3 must both keep a PoweredWheel.");
            foreach (int driver in new[] { 1, 3 })
                AxlePairs.Add((Parts.AxleFor(driver), Parts.RightWheel(driver)));
            Physics.Apply(Scale.DriverRadius, powered, totalAxles: 2,
                driverWeightFraction: ConventionalPhysics.RatedDriverWeightFraction(2, 2));
            airEquipment = S060FourCoupledAirEquipment.Bind(Parts.Body, Scale.Factor);
        }

        protected override void ReleasePresentationMeshes()
        { airEquipment?.Dispose(); airEquipment = null; }

        protected override void Undo()
        {
            try { ReleasePresentationMeshes(); }
            finally
            {
                try { Physics.Restore(); }
                finally
                {
                    if (droppedAxle != null)
                    {
                        droppedAxle.gameObject.SetActive(true);
                        Parts.BogieFor(DroppedDriver).axles = null;
                        droppedAxle = null;
                    }
                    foreach (var caliper in hiddenCalipers) if (caliper != null) caliper.enabled = true;
                    hiddenCalipers.Clear();
                    foreach (var wheel in hiddenWheels) if (wheel != null) wheel.gameObject.SetActive(true);
                    hiddenWheels.Clear();
                }
            }
        }
    }

    // Rigid visual brake equipment in the vacated middle-driver pockets. Native
    // air/brake simulation stays with the S060; nothing is attached to its rods.
    internal sealed class S060FourCoupledAirEquipment : MonoBehaviour, IDisposable
    {
        internal Transform Root { get; private set; }
        private Mesh ownedMesh;
        private MeshRenderer source, equipmentRenderer;
        private S060FixedValveGear fixedValveGear;
        private bool disposed;

        internal static S060FourCoupledAirEquipment Bind(Transform body, float scale)
        {
            var go = new GameObject("[0-4-0 twin air reservoirs]");
            go.transform.SetParent(body, false);
            var owner = go.AddComponent<S060FourCoupledAirEquipment>();
            owner.Root = go.transform;
            try
            {
                owner.source = body.GetComponentsInChildren<MeshRenderer>(true)
                    .Where(r => r.name == "s060_body" && r.sharedMaterial != null)
                    .OrderByDescending(r => r.enabled && r.gameObject.activeInHierarchy).First();
                owner.fixedValveGear = S060FixedValveGear.Apply(body);
                // Only the surplus fixed casting is removed. The working
                // animated reverser (s060_mech_03 and its LOD) stays visible.
                owner.ownedMesh = BuildMesh(scale);
                go.AddComponent<MeshFilter>().sharedMesh = owner.ownedMesh;
                owner.equipmentRenderer = go.AddComponent<MeshRenderer>();
                owner.equipmentRenderer.sharedMaterial = owner.source.sharedMaterial;
                return owner;
            }
            catch { owner.Dispose(); throw; }
        }

        private static Mesh BuildMesh(float scale)
        {
            var pieces = new List<Mesh>();
            // Gauge and frame attachments stay fixed. Smaller optional layouts
            // need proportionately smaller/lower equipment below their reach
            // rod; the standard 0-4-0 uses the full-size vessels.
            const float enlargement = 1.5f;
            float fit = Mathf.Min(1f, scale), size = enlargement * fit;
            Vector3 P(float x, float y, float z)
            {
                float side = Mathf.Sign(x);
                return new Vector3(side * .55f + (x - side * .665f) * size,
                    .85f * fit + (y - .85f) * size, (.15f + (z - .15f) * enlargement) * scale);
            }
            void Tube(Vector3[] points, float[] radii, int sides = 24)
                => pieces.Add(S060MeyerExhaustPipe.Tube(points, radii.Select(r => r * size).ToArray(), sides));
            void Pipe(float radius, params Vector3[] points)
                => pieces.Add(S060MeyerExhaustPipe.SmoothPathMesh(points, radius * size, .045f * size));
            void Barrel(float x, float y, float[] z, float[] r, int sides = 24)
                => Tube(z.Select(t => P(x, y, t)).ToArray(), r, sides);
            void Box(Vector3 lo, Vector3 hi)
            {
                var b = new S060MeyerBodywork.Builder { Paint = new Vector2(.6868f, .8948f) };
                b.Box(lo, hi);
                var mesh = new Mesh(); mesh.SetVertices(b.V); mesh.SetUVs(0, b.UV); mesh.SetTriangles(b.T, 0);
                mesh.RecalculateNormals(); pieces.Add(mesh);
            }
            try
            {
                foreach (int side in new[] { -1, 1 })
                {
                    float x = side * .665f;
                    // Pressed dished ends, rolled shell and raised circumferential
                    // end seams. Outermost strap face is |x|=.855, behind the
                    // stock coupling rod's innermost face at |x|=.902.
                    Barrel(x, .85f,
                        new[] { -.39f, -.38f, -.355f, -.32f, -.275f, -.235f, .535f, .575f, .62f, .655f, .68f, .69f },
                        new[] { .025f, .065f, .115f, .150f, .177f, .190f, .190f, .177f, .150f, .115f, .065f, .025f }, 32);
                    foreach (float z in new[] { -.235f, .535f })
                        Barrel(x, .85f, new[] { z - .008f, z + .008f }, new[] { .194f, .194f }, 32);

                    foreach (float z in new[] { -.13f, .30f })
                    {
                        Barrel(x, .85f, new[] { z - .027f, z + .027f }, new[] { .203f, .203f }, 32);
                        // Steel hanger plates embed in the existing fixed frame.
                        // The tanks cover the old journal recess without editing
                        // a shared native mesh or covering the remaining axles.
                        var hangerTop = P(Mathf.Max(side * .715f, side * .83f), 1.23f, z + .055f);
                        hangerTop.y = 1.23f;
                        Box(P(Mathf.Min(side * .715f, side * .83f), 1.005f, z - .055f), hangerTop);
                        Box(P(Mathf.Min(side * .59f, side * .78f), .637f, z - .043f),
                            P(Mathf.Max(side * .59f, side * .78f), .675f, z + .043f));
                        foreach (float boltX in new[] { x - .105f, x + .105f })
                            Tube(new[] { P(boltX, 1.07f, z), P(boltX, 1.103f, z) }, new[] { .022f, .022f }, 6);
                    }

                    // End bosses, hex unions and small-bore charging/return lines
                    // terminate inside the rigid underframe, with visible clips.
                    foreach (int end in new[] { -1, 1 })
                    {
                        float z = end < 0 ? -.385f : .685f;
                        Barrel(x, .85f, new[] { z, z + end * .025f, z + end * .052f }, new[] { .037f, .037f, .025f }, 6);
                        Pipe(.014f, P(x, .85f, z + end * .045f), P(x, 1.12f, z + end * .045f),
                            P(side * .42f, 1.12f, z + end * .045f), P(side * .42f, 1.20f, z + end * .045f));
                    }
                    foreach (float z in new[] { -.43f, .73f })
                        Box(P(Mathf.Min(side * .41f, x) - .015f, 1.105f, z - .015f),
                            P(Mathf.Max(side * .41f, x) + .015f, 1.135f, z + .015f));

                    // Low-point drain with square valve body, transverse handle
                    // and turned-down discharge; all kept within the rod plane.
                    Tube(new[] { P(x, .675f, .15f), P(x, .60f, .15f) }, new[] { .025f, .025f }, 6);
                    Box(P(x - .035f, .585f, .12f), P(x + .035f, .625f, .18f));
                    Pipe(.012f, P(x, .60f, .15f), P(x, .60f, .22f), P(x, .555f, .22f));
                    Pipe(.009f, P(x, .635f, .15f), P(x, .675f, .15f));
                    Pipe(.009f, P(x - .055f, .675f, .15f), P(x + .055f, .675f, .15f));

                    // A compact isolation cock and bolted manifold above each
                    // vessel add service detail without a second compressor.
                    Box(P(x - .075f, 1.055f, .075f), P(x + .075f, 1.13f, .225f));
                    Pipe(.018f, P(x, 1.02f, .15f), P(x, 1.17f, .15f));
                    Pipe(.010f, P(x - .060f, 1.17f, .15f), P(x + .060f, 1.17f, .15f));
                    Pipe(.013f, P(x, 1.09f, .20f), P(x, 1.09f, .43f), P(side * .42f, 1.09f, .43f));
                    foreach (float z in new[] { .095f, .205f })
                        Tube(new[] { P(side * .74f, 1.09f, z), P(side * .77f, 1.09f, z) }, new[] { .016f, .016f }, 6);
                }
                var combined = new Mesh { name = "0-4-0 strapped reservoirs and brake fittings", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                try
                {
                    combined.CombineMeshes(pieces.Select(m => new CombineInstance { mesh = m, transform = Matrix4x4.identity }).ToArray(), true, false);
                    combined.RecalculateBounds(); return combined;
                }
                catch { Release(combined); throw; }
            }
            finally { foreach (var mesh in pieces) Release(mesh); }
        }

        private void LateUpdate()
        {
            if (!disposed && source != null && equipmentRenderer != null && equipmentRenderer.sharedMaterial != source.sharedMaterial)
                equipmentRenderer.sharedMaterial = source.sharedMaterial;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            try { RestoreFixedGear(); }
            finally
            {
                if (Root != null) { Root.gameObject.SetActive(false); Release(Root.gameObject); }
                if (ownedMesh != null) Release(ownedMesh);
                ownedMesh = null; source = null; equipmentRenderer = null;
            }
        }
        private void OnDestroy()
        {
            try { RestoreFixedGear(); }
            finally { if (ownedMesh != null) Release(ownedMesh); ownedMesh = null; }
        }
        private void RestoreFixedGear()
        {
            fixedValveGear?.Dispose(); fixedValveGear = null;
        }
        private static void Release(UnityEngine.Object value)
        { if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value); }
    }
}
