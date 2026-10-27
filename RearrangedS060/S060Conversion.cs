using System;
using System.Collections.Generic;
using UnityEngine;

namespace RearrangedS060
{
    // One conversion at a time per locomotive. Subclasses supply geometry;
    // this owns the safety gate, resource lifetime and best-effort rollback.
    internal abstract class S060Conversion
    {
        private static readonly Dictionary<TrainCar, S060Conversion> active =
            new Dictionary<TrainCar, S060Conversion>();

        protected S060Parts Parts { get; private set; }
        protected B99PhysicsState Physics { get; private set; }
        protected S060DriverScale Scale { get; private set; }
        protected bool IsRestoring { get; private set; }
        internal bool CleanupPending { get; private set; }
        private float requestedScale;

        // Each entry pairs a live physics axle with the visual wheel it drives.
        // After any scaling the axles are moved onto their wheels, so the two
        // never drift apart whatever size the drivetrain ends up.
        protected readonly List<(Transform axle, Transform wheel)> AxlePairs =
            new List<(Transform, Transform)>();

        // The stock S060: three coupled axles, all powered.
        protected const int StockPoweredAxles = 3;

        internal abstract string Name { get; }
        internal S060Selection Selection => new S060Selection(
#if B99_S060_ARTICULATED
            this is S060Meyer ? S060Arrangement.Meyer : this is S060Dockside ? S060Arrangement.Dockside :
#endif
            this is S060Shay ? S060Arrangement.Shay : this is S060TenCoupled ? S060Arrangement.TenCoupled :
            this is S060FourCoupled ? S060Arrangement.FourCoupled :
            this is S060EightCoupled ? S060Arrangement.EightCoupled :
            this is S060SixTwo ? S060Arrangement.SixTwo : S060Arrangement.SixCoupled, requestedScale);

        protected abstract void Apply();
        protected abstract void Undo();
        protected virtual void ReleasePresentationMeshes() { }
        protected virtual float CylinderShift => 0;

        private S060CouplingRods fittedCouplingRods;

        private readonly List<(Transform axle, Vector3 position)> axleHome =
            new List<(Transform, Vector3)>();

        // A bogie's axles sit outside the drivetrain, so scaling does not move
        // them. Put each one under the wheel it drives, in the bogie's own space.
        private void AlignAxlesToWheels()
        {
            foreach (var (axle, wheel) in AxlePairs)
            {
                if (axle == null || wheel == null) continue;
                axleHome.Add((axle, axle.localPosition));
                float carZ = Parts.Car.transform.InverseTransformPoint(wheel.position).z;
                var local = axle.parent.InverseTransformPoint(
                    Parts.Car.transform.TransformPoint(new Vector3(0f, 0f, carZ)));
                axle.localPosition = new Vector3(axle.localPosition.x, axle.localPosition.y, local.z);
            }
            // Force both bogies to rebuild their axle lists from where the axles
            // now actually are.
            Parts.FrontBogie.axles = null;
            Parts.RearBogie.axles = null;
            S060RollingAudio.Refresh(Parts.Car);
        }

        private void RestoreAxlePositions()
        {
            foreach (var (axle, position) in axleHome)
                if (axle != null) axle.localPosition = position;
            axleHome.Clear();
            if (Parts.FrontBogie != null) Parts.FrontBogie.axles = null;
            if (Parts.RearBogie != null) Parts.RearBogie.axles = null;
            S060RollingAudio.Refresh(Parts.Car);
        }

        internal static S060Conversion Current(TrainCar car) =>
            car != null && active.TryGetValue(car, out var state) ? state : null;

        internal static string Describe(TrainCar car)
        {
            if (car == null) return "no car";
            var state = Current(car);
            return car.ID + ": " + (state == null ? "stock 0-6-0" : state.CleanupPending ? "cleanup required; retry the selection while stopped" : state.Name);
        }

        internal static void Convert<T>(TrainCar car, float driverScale, bool restoring = false) where T : S060Conversion, new()
        {
            if (car == null) throw new ArgumentNullException(nameof(car));
            if (active.ContainsKey(car))
                throw new InvalidOperationException(car.ID + " is already converted; restore it first.");
            if (!restoring) RequireStationary(car);

            if (typeof(T) == typeof(S060EightCoupled)) driverScale = S060EightLayout.FitScale(driverScale);
            var parts = S060Parts.Resolve(car);
            var state = new T
            {
                Parts = parts,
                Physics = new B99PhysicsState(car, StockPoweredAxles),
                Scale = new S060DriverScale(parts), IsRestoring = restoring, requestedScale = driverScale
            };
            // Retain the owner before the first mutation. A failed undo must
            // remain reachable for retry, pooling rejection or final disposal.
            active.Add(car, state);
            try
            {
                state.Scale.Apply(typeof(T) == typeof(S060TenCoupled) ? S060TenLayout.Scale : driverScale);
                state.Apply();
                if (state is S060FourCoupled || state is S060EightCoupled) state.fittedCouplingRods = S060CouplingRods.Apply(parts);
                state.Scale.PositionFittings(state.CylinderShift);
                state.AlignAxlesToWheels();
                S060Oiling.Select(car, state.Selection);
                S060OilCups.Refresh(car);
            }
            catch (Exception apply)
            {
                try
                {
                    state.ResetRunningGear();
                    S060Oiling.Select(car, S060Selection.Stock);
                    S060OilCups.Refresh(car);
                    active.Remove(car);
                }
                catch (Exception undo)
                {
                    state.CleanupPending = true;
                    throw new AggregateException("S060 apply failed and cleanup is incomplete. Retry the selection while stopped; the cleanup owner has been retained.", apply, undo);
                }
                throw;
            }
            Main.Logger.Log("S060 " + car.ID + " converted: " + state.Name +
                ", driver scale " + state.Scale.Factor.ToString("0.00") +
                " (" + state.Selection.DriverDiameter.ToString("0.00") + " m drivers)");
        }

        internal static void Restore(TrainCar car)
        {
            if (car == null) throw new ArgumentNullException(nameof(car));
            if (!active.TryGetValue(car, out var state))
                throw new InvalidOperationException(car.ID + " is not converted.");
            RequireStationary(car);
            state.CleanupPending = true;
            try
            {
                state.ResetRunningGear();
                S060Oiling.Select(car, S060Selection.Stock);
                S060OilCups.Refresh(car);
            }
            catch { state.CleanupPending = true; throw; }
            active.Remove(car);
            Main.Logger.Log("S060 " + car.ID + " restored to stock 0-6-0.");
        }

        // Switching a drivetrain under load is how the S282 mod earned its
        // stationary-only rule; these are the same conditions its radio checks.
        internal static void RequireStationary(TrainCar car)
        {
            if (car.derailed || car.isExploded)
                throw new InvalidOperationException("Use a repaired, rerailed locomotive.");
            float speed = Mathf.Abs(car.GetForwardSpeed());
            if (speed > 0.01f)
                throw new InvalidOperationException("Stop the locomotive first (moving at " +
                    speed.ToString("0.00") + " m/s).");
            var sim = car.SimController;
            if (sim?.simFlow == null)
                throw new InvalidOperationException("The native simulation is not ready yet.");
            if (sim.simFlow.TryGetPort("throttle.EXT_IN", out var throttle) && throttle.Value > 0.001f)
                throw new InvalidOperationException("Close the regulator first.");
            if (sim.drivingForce != null && Mathf.Abs(sim.drivingForce.generatedForce) > 500f)
                throw new InvalidOperationException("Wait for the driving force to settle.");
            if (sim.wheelslipController != null && sim.wheelslipController.IsWheelslipping)
                throw new InvalidOperationException("Wait for the wheelslip to stop.");
            if (car.adhesionController != null && car.adhesionController.IsWheelSliding)
                throw new InvalidOperationException("Wait for the wheel slide to stop.");
        }

        private void ResetRunningGear()
        {
            CleanupPending = true;
            var errors = new List<Exception>();
            void Attempt(Action action) { try { action(); } catch (Exception ex) { errors.Add(ex); } }
            Attempt(() => { fittedCouplingRods?.Dispose(); fittedCouplingRods = null; });
            Attempt(Undo);
            Attempt(Scale.Restore);
            Attempt(RestoreAxlePositions);
            if (errors.Count != 0) throw new AggregateException("S060 running-gear cleanup is incomplete; its owner is retained.", errors);
            CleanupPending = false;
        }

        // Pool teardown is already deleting the car; speed/regulator gates do
        // not apply. A failed reset must prevent reuse of this pooled instance.
        internal static void Release(TrainCar car)
        {
            var errors = new List<Exception>();
            void Attempt(Action action) { try { action(); } catch (Exception ex) { errors.Add(ex); } }
            active.TryGetValue(car, out var state);
            if (state != null) state.CleanupPending = true;
            Attempt(() => S060Oiling.Select(car, S060Selection.Stock));
            Attempt(() => S060OilCups.Release(car));
            if (state != null) Attempt(state.ResetRunningGear);
            if (errors.Count != 0)
            {
                if (state != null) state.CleanupPending = true;
                throw new AggregateException("S060 pool cleanup failed; this car must not be reused.", errors);
            }
            active.Remove(car);
        }

        internal static void ForgetDestroyed(TrainCar car)
        {
            if (ReferenceEquals(car, null) || !active.TryGetValue(car, out var state)) return;
            active.Remove(car);
            // Unity may already have destroyed child components. Release only
            // owned resources here, never restore physics or move native parts.
            try { state.fittedCouplingRods?.Dispose(); }
            finally
            {
                state.fittedCouplingRods = null;
                try { state.Scale?.ReleaseResources(); }
                finally { state.ReleasePresentationMeshes(); }
            }
        }

        internal static void SwitchTo(TrainCar car, S060Selection selection, bool restoring = false)
        {
            if (!S060Parts.IsS060(car)) throw new InvalidOperationException("Select an S060.");
            if (!S060Selection.IsSupported((int)selection.Arrangement) || !S060Selection.IsValidScale(selection.Scale))
                throw new ArgumentException("Invalid S060 arrangement or driver size.", nameof(selection));
            if (!restoring) RequireStationary(car);
            else if (Current(car) != null) throw new InvalidOperationException("Saved geometry must restore on a fresh car instance.");
            // A previous failed cleanup is not a valid selected arrangement.
            // Finish it before equality checks or applying another conversion.
            if (Current(car)?.CleanupPending == true) Restore(car);
            var previous = Current(car)?.Selection ?? S060Selection.Stock;
#if B99_S060_ARTICULATED
            var support = car.GetComponent<S060MeyerSupport>();
            bool extended = support != null && support.Active;
            if (!restoring && ((extended && selection.Arrangement != S060Arrangement.Meyer) ||
                (!previous.Equals(selection) && selection.Arrangement == S060Arrangement.Meyer && !extended)))
            {
                S060MeyerSupport.RequireRearFree(car);
            }
#endif
            if (previous.Equals(selection))
            {
                S060Oiling.Select(car, selection);
#if B99_S060_ARTICULATED
                // A saved articulated flow can be restored before its deferred
                // geometry. An explicit stock choice must cancel that mode too.
                if ((selection.Arrangement <= S060Arrangement.SixTwo || selection.Arrangement == S060Arrangement.TenCoupled))
                {
                    if (Current(car) == null) support?.Restore();
                    SelectSimpleEngine(car);
                }
#endif
                return;
            }
#if B99_S060_ARTICULATED
            // Refuse unavailable assets before dismantling the current layout.
            if (selection.Arrangement == S060Arrangement.Meyer || selection.Arrangement == S060Arrangement.Dockside)
            {
                S060ArticulatedSimulation.Ensure(car);
                S060Articulated.Preflight(selection.Arrangement == S060Arrangement.Meyer);
            }
#endif
            if (selection.Arrangement == S060Arrangement.Shay) { S060ArticulatedSimulation.Ensure(car); S060ShayAssets.Resolve(); S060Articulated.Preflight(false); }
            if (selection.Arrangement == S060Arrangement.SixTwo) S060PonyAssets.Resolve();
            if (Current(car) != null) Restore(car);
            try { ApplySelection(car, selection, restoring); }
            catch
            {
                // Recover only after the failed conversion fully returned to
                // stock. Never stack a replacement over uncleared running gear.
                if (Current(car) == null)
                    try { ApplySelection(car, previous, restoring); }
                    catch (Exception ex) { Main.Logger.LogException("Could not recover the prior S060 selection", ex); }
                throw;
            }
        }

        private static void ApplySelection(TrainCar car, S060Selection selection, bool restoring = false)
        {
#if B99_S060_ARTICULATED
            if (selection.Arrangement != S060Arrangement.Meyer && Current(car) == null)
                car.GetComponent<S060MeyerSupport>()?.Restore();
            if ((selection.Arrangement <= S060Arrangement.SixTwo || selection.Arrangement == S060Arrangement.TenCoupled)) SelectSimpleEngine(car);
#endif
            if (selection.IsStock) { S060Oiling.Select(car, selection); return; }
            switch (selection.Arrangement)
            {
                case S060Arrangement.FourCoupled: Convert<S060FourCoupled>(car, selection.Scale, restoring); break;
                case S060Arrangement.EightCoupled: Convert<S060EightCoupled>(car, selection.Scale, restoring); break;
                case S060Arrangement.TenCoupled: Convert<S060TenCoupled>(car, 1f, restoring); break;
                case S060Arrangement.SixTwo: Convert<S060SixTwo>(car, selection.Scale, restoring); break;
#if B99_S060_ARTICULATED
                case S060Arrangement.Shay: Convert<S060Shay>(car, 1f, restoring); break;
                case S060Arrangement.Meyer: Convert<S060Meyer>(car, 1f, restoring); break;
                case S060Arrangement.Dockside: Convert<S060Dockside>(car, 1f, restoring); break;
#endif
                default: Convert<S060Rescaled>(car, selection.Scale, restoring); break;
            }
        }
#if B99_S060_ARTICULATED
        private static void SelectSimpleEngine(TrainCar car)
        {
            var simulation = car.GetComponent<S060ArticulatedSimulation>();
            if (simulation?.Flow != null && simulation.Flow == car.SimController.simFlow)
                simulation.Select(S060EngineMode.Simple);
        }
#endif
    }

    // Added running gear is not in the native animation clips, so it copies the
    // pose of a part that is, after the Animator has finished for the frame.
    // Coupled wheels all share a crank angle, so a wheel copies a wheel exactly;
    // a coupling rod translates without rotating, so a rod copies a rod with a
    // fixed offset along the frame.
    internal sealed class S060FollowPose : MonoBehaviour
    {
        private Transform source;
        private Vector3 offset;
        private bool followPosition;

        internal void Follow(Transform source, Vector3 offset, bool followPosition)
        {
            this.source = source;
            this.offset = offset;
            this.followPosition = followPosition;
        }

        private void LateUpdate()
        {
            if (source == null) return;
            transform.localRotation = source.localRotation;
            if (followPosition) transform.localPosition = source.localPosition + offset;
        }
    }
}
