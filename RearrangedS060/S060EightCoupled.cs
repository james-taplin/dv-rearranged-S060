using System;
using System.Collections.Generic;
using System.Linq;
using LocoSim.Implementations.Wheels;
using DV.Simulation.Controllers;
using DV.ModularAudioCar;
using UnityEngine;

namespace RearrangedS060
{
    // 0-8-0: compact four-coupled group, capped at .80 native driver scale.
    // Re-centre forwards so its rearmost axle stays at the stock rear-driver
    // station. The previous full-size extension under the cab is superseded.
    //
    // The fourth wheel is built behind driver 1 in source coordinates; after
    // radial scaling, the whole group is translated forwards. X is unchanged.
    //
    // Nothing is extracted or imported: the new wheel is a clone of driver 2's
    // mesh. One author-owned four-boss coupling mesh replaces the old doubled
    // three-boss extension, following the untouched native animated rod parent. Added wheels are not
    // in the native animation clips, so they copy
    // an animated part's pose in LateUpdate via S060FollowPose.
    internal sealed class S060EightCoupled : S060Conversion
    {
        // Driver 1 sits at Z -1.3978 and spacing is 1.5493 (the mean of the two
        // measured gaps), so the fourth driver lands here.
        private const float Spacing = 1.5493f;
        internal const float Driver4Z = S060Parts.Driver1Z - Spacing;   // -2.9471
        private const string AxleName = "[axle] rearranged 4";
        private const string WheelName = "s060_Wheels_04 (rearranged)";

        private readonly List<GameObject> added = new List<GameObject>();
        private Transform addedAxle;
        private readonly List<(Transform mechanism, Vector3 position)> mechanismHome = new List<(Transform, Vector3)>();
        private float cylinderShift;
        protected override float CylinderShift => cylinderShift;

        internal override string Name => "0-8-0 (small drivers, rear axle at stock station)";

        protected override void Apply()
        {
            if (Scale.Factor > S060EightLayout.MaximumScale)
                throw new InvalidOperationException("The fitted 0-8-0 requires driver scale .80 or smaller.");
            var wheels = new Dictionary<Transform, Transform>();   // side -> new wheel

            // 1. A fourth wheel per side, cloned from driver 2 and following
            //    driver 1's rotation. All coupled wheels share a crank angle, so
            //    copying the rotation outright is correct.
            foreach (var template in Parts.Wheels(2))
            {
                var side = template.parent;
                var wheel = UnityEngine.Object.Instantiate(template.gameObject, side);
                added.Add(wheel);
                wheel.name = WheelName;
                wheel.transform.localPosition = new Vector3(
                    template.localPosition.x, template.localPosition.y, Driver4Z);
                wheel.transform.localRotation = template.localRotation;
                wheel.transform.localScale = template.localScale;

                var follower = Parts.Wheels(1).FirstOrDefault(w => w.parent == side);
                if (follower == null)
                    throw new InvalidOperationException("Driver 1 is missing on " + side.name + ".");
                wheel.AddComponent<S060FollowPose>().Follow(follower, Vector3.zero, false);

                wheels[side] = wheel.transform;
            }

            // The base conversion fits one four-boss owned mesh to the native
            // coupling-rod parent once every wheel is in its final position.

            // 3. A fourth physics axle. Axle_R had one axle and now has two, which
            //    balances the bogies 2 + 2. Its pivot stays on driver 1, as in the
            //    0-4-0 where leaving the pivots alone proved out in game, so the
            //    new axle carries the offset instead.
            var template1 = Parts.AxleFor(1)
                ?? throw new InvalidOperationException("Driver 1 has no [axle].");
            var rearBogie = Parts.BogieFor(1);
            var axle = UnityEngine.Object.Instantiate(template1.gameObject, template1.parent);
            axle.name = AxleName;
            // Position is only a starting point: the base class puts every axle
            // under its wheel once the drivetrain scale is known.
            axle.transform.localPosition = template1.localPosition + new Vector3(0f, 0f, -Spacing);
            axle.transform.localRotation = template1.localRotation;
            added.Add(axle);
            addedAxle = axle.transform;

            var addedPowered = axle.GetComponent<PoweredWheel>()
                ?? throw new InvalidOperationException("The cloned axle has no PoweredWheel.");
            // Point it at the new right-side wheel, matching how DV binds the
            // stock three to DrivingMechanism_R.
            addedPowered.wheelTransform = wheels[Parts.MechanismRight];
            rearBogie.axles = null;

            // Work in car coordinates so body/mechanism origins and a rotated
            // locomotive do not change the required rear-axle station.
            float currentRear = Parts.Car.transform.InverseTransformPoint(wheels[Parts.MechanismRight].position).z;
            float shift = S060Parts.Driver1Z - currentRear;
            cylinderShift = shift;
            foreach (var mechanism in new[] { Parts.MechanismLeft, Parts.MechanismRight })
            {
                mechanismHome.Add((mechanism, mechanism.localPosition));
                mechanism.position += Parts.Car.transform.TransformVector(new Vector3(0, 0, shift));
            }
            // 4. Four powered axles out of four, all the weight on them.
            var powered = new[] { Parts.PoweredFor(1), Parts.PoweredFor(2), Parts.PoweredFor(3), addedPowered };
            if (powered.Any(p => p == null))
                throw new InvalidOperationException("All three stock drivers must keep a PoweredWheel.");
            foreach (int driver in new[] { 1, 2, 3 })
                AxlePairs.Add((Parts.AxleFor(driver), Parts.RightWheel(driver)));
            AxlePairs.Add((addedAxle, wheels[Parts.MechanismRight]));
            Physics.Apply(Scale.DriverRadius, powered, totalAxles: 4, driverWeightFraction: 1f);
        }

        protected override void Undo()
        {
            try { Physics.Restore(); }
            finally
            {
                foreach (var home in mechanismHome) if (home.mechanism != null) home.mechanism.localPosition = home.position;
                mechanismHome.Clear();
                if (addedAxle != null)
                {
                    // Deactivate before destroying so the rebuilt axle list cannot
                    // pick up an object that is on its way out this frame.
                    addedAxle.gameObject.SetActive(false);
                    addedAxle = null;
                }
                foreach (var part in added)
                    if (part != null) { part.SetActive(false); UnityEngine.Object.Destroy(part); }
                added.Clear();
                Parts.BogieFor(1).axles = null;
            }
        }
    }
}
