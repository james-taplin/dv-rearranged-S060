#if B99_S060_ARTICULATED
using System;
using System.Collections.Generic;
using System.Linq;
using DV;
using DV.PointSet;
using HarmonyLib;
using UnityEngine;

namespace RearrangedS060
{
    // A longer two-bogie configuration of the SAME car. Native travellers,
    // rigidbodies and suspension joints remain; no car spawn or logic reset.
    internal sealed class S060MeyerSupport : MonoBehaviour
    {
        private sealed class Home
        {
            internal Bogie Bogie;
            internal Vector3 Start, Anchor;
            internal Quaternion Rotation;
        }
        private TrainCar car;
        private readonly List<Home> homes = new List<Home>();
        private readonly List<(CapsuleCollider collider, Vector3 centre, float height)> capsules = new List<(CapsuleCollider, Vector3, float)>();
        private readonly List<(Transform part, Vector3 position)> couplingVisuals = new List<(Transform, Vector3)>();
        private readonly List<(ChainCouplerInteraction chain, Vector3 position)> chainHomes = new List<(ChainCouplerInteraction, Vector3)>();
        private Bounds bounds;
        private float interCoupler;
        private float logicLength;
        private Vector3 rearAnchor;
        private S060MeyerCollision collision;
        internal bool Active { get; private set; }
        internal void BindCollisionFrame(Transform frame)
        {
            if (!Active || collision == null) throw new InvalidOperationException("Meyer support must be active before binding its collision frame.");
            collision.BindFrame(frame);
            // Legacy shell collision keeps its original body-space footprint;
            // only the power unit and its four physical axle halves move aft.
            collision.transform.localPosition = -S060MeyerLayout.RearVisualOffset;
            var extended = collision.Encapsulate(bounds, car.transform);
            extended.Encapsulate(car.RearCouplerAnchor.localPosition); car._bounds = extended;
        }
        internal Vector3 RearOffset => car.RearCouplerAnchor.localPosition - rearAnchor;
        internal void ReleaseCollision()
        {
            if (collision != null) collision.Dispose();
            collision = null;
        }
        internal Vector3 OriginalAxlePosition(Bogie bogie, Transform axle)
        {
            var home = homes.SingleOrDefault(h => h.Bogie == bogie);
            if (!Active || home == null) return car.transform.InverseTransformPoint(axle.position);
            // Save restoration prepares the longer supports before connections
            // load. The stock axle offsets still belong to the original pivot.
            return S060AxleGeometry.FromSupport(home.Start, home.Rotation,
                bogie.transform.InverseTransformPoint(axle.position));
        }
        internal static void Register(Harmony harmony) => harmony.CreateClassProcessor(typeof(PhysicsCreated)).Patch();
        internal static S060MeyerSupport For(TrainCar car)
        {
            var support = car.GetComponent<S060MeyerSupport>();
            if (support == null) support = car.gameObject.AddComponent<S060MeyerSupport>();
            support.car = car; return support;
        }
        internal static void RequireRearFree(TrainCar car)
        {
            if (car.rearCoupler == null || car.rearCoupler.coupledTo != null || car.rearCoupler.hoseAndCock.IsHoseConnected)
                throw new InvalidOperationException("Uncouple and disconnect the rear hose before adding or removing the Meyer steam bogie.");
        }
        internal void Apply(bool restoring = false)
        {
            if (Active) return;
            if (!restoring) RequireRearFree(car);
            var targets = new[] { (bogie: car.FrontBogie, z: MeyerPlan.FrontCentre), (bogie: car.RearBogie, z: MeyerPlan.RearCentre) };
            // Resolve both track positions before changing a joint or pivot.
            foreach (var target in targets) CheckTrack(target.bogie, new Vector3(0, target.bogie.startingLocalPosition.y, target.z));
            if (!restoring)
                foreach (var hit in UnityEngine.Physics.OverlapBox(car.transform.TransformPoint(new Vector3(0, 1.25f, MeyerPlan.RearCentre - .25f)), new Vector3(1.25f, 1.0f, 2.25f), car.transform.rotation))
                {
                    var other = hit.GetComponentInParent<TrainCar>();
                    if (other != null && other != car) throw new InvalidOperationException("Move clear of the vehicle behind before adding the Meyer steam bogie.");
                }
            bounds = car.Bounds; interCoupler = car._interCouplerDistance; rearAnchor = car.RearCouplerAnchor.localPosition;
            logicLength = car.logicCar != null ? car.logicCar.length : car.InterCouplerDistance;
            foreach (var target in targets)
                homes.Add(new Home { Bogie = target.bogie, Start = target.bogie.startingLocalPosition,
                    Rotation = target.bogie.startingLocalRotation,
                    Anchor = target.bogie.GetComponent<ConfigurableJoint>() != null ? target.bogie.initialConnectedAnchor : target.bogie.startingLocalPosition + Vector3.up });
            Active = true;
            try
            {
                foreach (var target in targets)
                    Relocate(target.bogie, new Vector3(0, target.bogie.startingLocalPosition.y, target.z), target.bogie.startingLocalRotation, null);
                MoveRearCoupling();
                // SetupTrainCar reparents collision out of [colliders] into
                // the live rigidbody root; use its cached native reference.
                var root = car.carColliders?.collisionRoot;
                if (root == null) throw new InvalidOperationException("Native S060 collision root is missing.");
                var materialSource = root.GetComponentsInChildren<Collider>(true).FirstOrDefault(c => !c.isTrigger);
                var rearFrame = car.RearBogie.transform.Find("bogie_car");
                if (rearFrame == null) throw new InvalidOperationException("Native Meyer rear bogie frame is missing.");
                collision = S060MeyerCollision.Create(root, rearFrame, materialSource);
                collision.BindNativeBogie(car.RearBogie);
                collision.BindNativeContacts(car.CollisionInfoDispenser);
                var bogieRoot = car.carColliders.GetBogies();
                if (bogieRoot != null)
                    foreach (var capsule in bogieRoot.GetComponentsInChildren<CapsuleCollider>(true))
                    {
                        capsules.Add((capsule, capsule.center, capsule.height));
                        var c = car.transform.InverseTransformPoint(capsule.transform.TransformPoint(capsule.center));
                        c.z = c.z >= 0 ? MeyerPlan.FrontCentre : MeyerPlan.RearCentre + S060MeyerLayout.RearVisualOffsetZ;
                        capsule.center = capsule.transform.InverseTransformPoint(car.transform.TransformPoint(c));
                        capsule.height = (float)MeyerPlan.AxlePitch + capsule.radius * 2;
                    }
                var extended = collision.Encapsulate(bounds, car.transform);
                extended.Encapsulate(car.RearCouplerAnchor.localPosition); car._bounds = extended;
                car._interCouplerDistance = Vector3.Distance(car.FrontCouplerAnchor.position, car.RearCouplerAnchor.position);
                SetLogicLength(car._interCouplerDistance);
                car.UpdateMassRatioWhenBogiesInitialized();
            }
            catch (Exception apply)
            {
                try { Restore(); }
                catch (Exception undo) { throw new AggregateException("Meyer support apply and restoration failed; support remains available for retry.", apply, undo); }
                throw;
            }
        }
        private void MoveRearCoupling()
        {
            // Keep the native hook, hose and physical anchor together at the
            // R2 hook pocket. Buffer heads are retained at their fixed beam by
            // S060MeyerLights when it binds the native buffer anchors.
            var anchor = rearAnchor;
            anchor.z = MeyerPlan.RearCouplerZ + MeyerPlan.RearCouplingCorrectionZ;
            car.RearCouplerAnchor.localPosition = anchor;
            MoveCouplingVisuals(car.transform.TransformVector(anchor - rearAnchor));
        }
        private void MoveCouplingVisuals(Vector3 delta)
        {
            var visual = car.rearCoupler.visualCoupler;
            if (visual == null) throw new InvalidOperationException("Native rear coupling visuals are not ready.");
            // VisualCouplerInit reparents the live chain and hoses to the cab
            // interior. Moving just the anchor or original rig leaves them at
            // the old end. Move the existing objects, preserving their controls.
            var parts = new[] { visual.transform, visual.chain, visual.hoses }.Where(t => t != null).Distinct().ToArray();
            var chain = visual.chain != null ? visual.chain.GetComponent<ChainCouplerInteraction>() : null;
            if (chain == null) throw new InvalidOperationException("Native rear chain interaction is not ready.");
            chainHomes.Add((chain, chain.originalLocalPosition));
            var positions = parts.Select(part => (part, world: part.position)).ToArray();
            foreach (var part in parts) couplingVisuals.Add((part, part.localPosition));
            // The native buffer controller, scanner and anchors share this rig.
            // Keep that complete datum at the beam; only the chain and hose
            // follow the additional coupling-pocket correction.
            visual.transform.position += delta - car.transform.TransformVector(
                new Vector3(0, 0, MeyerPlan.RearCouplingCorrectionZ));
            foreach (var item in positions.Where(item => item.part != visual.transform))
                item.part.position = item.world + delta;
            // Native visibility enable restores this cached local pose after
            // DV has reparented the chain into the interior.
            chain.originalLocalPosition = chain.transform.localPosition;
        }
        private static void CheckTrack(Bogie bogie, Vector3 position)
        {
            if (bogie == null || bogie.HasDerailed) throw new InvalidOperationException("Meyer requires two rerailed native bogies.");
            var world = bogie.Car.transform.TransformPoint(position);
            TrackPose(world);
        }
        internal void PreflightRestore()
        {
            foreach (var home in homes) if (home.Bogie != null) CheckTrack(home.Bogie, home.Start);
        }
        private static (RailTrack track, PointSetTraveller point) TrackPose(Vector3 world, ICollection<RailTrack> allTracks = null)
        {
            // GetClosest's second argument excludes track ENDS, not distant
            // tracks. Excluding two metres could move a stopped bogie over a
            // metre at a junction. Keep endpoints and interpolate the nearest
            // native segment rather than snapping onto a half-metre sample.
            var (track, closest) = RailTrack.GetClosest(world, 0f, allTracks);
            if (track == null || !closest.HasValue) throw new InvalidOperationException("No track beneath the requested S060 engine pivot.");
            var points = track.GetKinkedPointSet();
            Vector3 target = world - WorldMover.currentMove;
            double span = closest.Value.span; float best = float.PositiveInfinity;
            for (int i = Math.Max(0, closest.Value.index - 1); i <= Math.Min(points.points.Length - 2, closest.Value.index); i++)
            {
                var a = points.points[i]; var b = points.points[i + 1];
                Vector3 start = (Vector3)a.position, edge = (Vector3)(b.position - a.position);
                if (edge.sqrMagnitude <= 1e-12f) continue;
                float t = Mathf.Clamp01(Vector3.Dot(target - start, edge) / edge.sqrMagnitude);
                float distance = (start + edge * t - target).sqrMagnitude;
                if (distance < best) { best = distance; span = a.span + a.spanToNextPoint * t; }
            }
            if (best > .5f * .5f) throw new InvalidOperationException("No track within half a metre of the requested S060 engine pivot.");
            var point = new PointSetTraveller(points); point.MoveToSpan(span);
            return (track, point);
        }
        private void Relocate(Bogie bogie, Vector3 position, Quaternion rotation, Vector3? originalAnchor)
        {
            var desired = car.transform.TransformPoint(position);
            var (track, p) = TrackPose(desired);
            var forward = p.worldForward; if (Vector3.Dot(forward, car.transform.forward) < 0) forward = -forward;
            bogie.startingLocalPosition = position; bogie.startingLocalRotation = rotation;
            bogie.transform.position = (Vector3)p.worldPosition + WorldMover.currentMove;
            bogie.transform.rotation = Quaternion.LookRotation(forward, p.worldUp);
            if (bogie.rb != null) { bogie.rb.position = bogie.transform.position; bogie.rb.rotation = bogie.transform.rotation; }
            var joint = bogie.GetComponent<ConfigurableJoint>();
            if (joint != null)
            {
                bogie.initialConnectedAnchor = originalAnchor ?? position + rotation * joint.anchor;
                bogie.SetupJointSettings(car.massController.TotalMass);
            }
            bogie.SetTrack(track, p.Span); bogie.PreventSlopePositionHolding(); bogie.axles = null;
        }
        internal void Restore()
        {
            if (!Active) return;
            var errors = new List<Exception>();
            try { ReleaseCollision(); }
            catch (Exception ex) { errors.Add(ex); }
            foreach (var home in homes.ToArray())
                try
                {
                    if (home.Bogie != null) Relocate(home.Bogie, home.Start, home.Rotation, home.Anchor);
                    homes.Remove(home);
                }
                catch (Exception ex) { errors.Add(ex); }
            foreach (var home in capsules) if (home.collider != null) { home.collider.center = home.centre; home.collider.height = home.height; }
            capsules.Clear();
            foreach (var home in couplingVisuals) if (home.part != null) home.part.localPosition = home.position;
            couplingVisuals.Clear();
            foreach (var home in chainHomes) if (home.chain != null) home.chain.originalLocalPosition = home.position;
            chainHomes.Clear();
            if (car != null)
            {
                car.RearCouplerAnchor.localPosition = rearAnchor; car._bounds = bounds; car._interCouplerDistance = interCoupler;
                SetLogicLength(logicLength);
                car.UpdateMassRatioWhenBogiesInitialized();
            }
            // A failed track/joint restore must retain its captured home and
            // keep this owner active so a later cleanup can retry it.
            Active = errors.Count != 0;
            if (errors.Count != 0) throw new AggregateException("Meyer support restoration failed", errors);
        }
        private void SetLogicLength(float length)
        {
            // DV has no resize setter; the existing logical car's length is an
            // instance readonly field. Retain that car/GUID and change only this
            // geometry datum, with the original restored on removal.
            S060LogicLength.Set(car.logicCar, length);
        }
        [HarmonyPatch(typeof(Bogie), "SetupPhysics")]
        private static class PhysicsCreated
        {
            private static void Postfix(Bogie __instance)
            {
                var support = __instance.Car.GetComponent<S060MeyerSupport>();
                if (support == null || !support.Active) return;
                var joint = __instance.GetComponent<ConfigurableJoint>();
                if (joint == null) return;
                // Native Start/rerail can create the joint after save support
                // preparation. Keep its body-space anchor at the chosen pivot.
                __instance.initialConnectedAnchor = __instance.startingLocalPosition + __instance.startingLocalRotation * joint.anchor;
                __instance.SetupJointSettings(__instance.Car.massController.TotalMass);
            }
        }
    }
}
#endif
