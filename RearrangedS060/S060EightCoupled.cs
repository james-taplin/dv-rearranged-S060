using System;
using System.Collections.Generic;
using System.Linq;
using LocoSim.Implementations.Wheels;
using UnityEngine;

namespace RearrangedS060
{
    // 0-8-0: add a fourth driver BEHIND driver 1, at the stock 1.549 m spacing.
    //
    // Why backwards rather than forwards. Measured clearances (feasibility §3):
    // behind driver 1 there is 1.31 m of clear low space, in front of driver 3
    // only 1.01 m and the valve gear fills it to Z +3.460. Going forwards would
    // mean moving the cylinders and re-timing the gear; going backwards touches
    // nothing that moves. The alternative — re-centring all four drivers
    // symmetrically — is tidier on paper but moves every wheel, both rods, the
    // main rod and the gear, so it is not the cheapest way to find out whether a
    // fourth driver works at all.
    //
    // Nothing is extracted or imported: the new wheel is a clone of driver 2's
    // mesh and the rod extension is a clone of the stock coupling rod, offset by
    // one spacing, exactly the trick the S282 mod uses for its twelve-coupled
    // extension. Added parts are not in the native animation clips, so they copy
    // an animated part's pose in LateUpdate via S060FollowPose.
    internal sealed class S060EightCoupled : S060Conversion
    {
        // Driver 1 sits at Z -1.3978 and spacing is 1.5493 (the mean of the two
        // measured gaps), so the fourth driver lands here.
        private const float Spacing = 1.5493f;
        internal const float Driver4Z = S060Parts.Driver1Z - Spacing;   // -2.9471
        private const string AxleName = "[axle] rearranged 4";
        private const string WheelName = "s060_Wheels_04 (rearranged)";
        private const string RodName = "s060_mech_wheels_connect (rearranged)";

        private readonly List<GameObject> added = new List<GameObject>();
        private Transform addedAxle;

        internal override string Name => "0-8-0 (fourth driver at the cab end)";

        protected override void Apply()
        {
            var wheels = new Dictionary<Transform, Transform>();   // side -> new wheel

            // 1. A fourth wheel per side, cloned from driver 2 and following
            //    driver 1's rotation. All coupled wheels share a crank angle, so
            //    copying the rotation outright is correct.
            foreach (var template in Parts.Wheels(2))
            {
                var side = template.parent;
                var wheel = UnityEngine.Object.Instantiate(template.gameObject, side);
                wheel.name = WheelName;
                wheel.transform.localPosition = new Vector3(
                    template.localPosition.x, template.localPosition.y, Driver4Z);
                wheel.transform.localRotation = template.localRotation;
                wheel.transform.localScale = template.localScale;

                var follower = Parts.Wheels(1).FirstOrDefault(w => w.parent == side);
                if (follower == null)
                    throw new InvalidOperationException("Driver 1 is missing on " + side.name + ".");
                wheel.AddComponent<S060FollowPose>().Follow(follower, Vector3.zero, false);

                added.Add(wheel);
                wheels[side] = wheel.transform;
            }

            // 2. A second coupling rod per side, shifted back one spacing, so its
            //    three pins land on drivers 4, 1 and 2. Its pins overlap the stock
            //    rod's on drivers 1 and 2; that doubled boss is cosmetic.
            foreach (var side in new[] { Parts.MechanismLeft, Parts.MechanismRight })
            {
                var rod = side.Find("s060_mech_wheels_connect");
                if (rod == null)
                    throw new InvalidOperationException("The coupling rod is missing on " + side.name + ".");
                var extension = UnityEngine.Object.Instantiate(rod.gameObject, side);
                extension.name = RodName;
                extension.transform.localRotation = rod.localRotation;
                extension.transform.localScale = rod.localScale;
                extension.transform.localPosition = rod.localPosition + new Vector3(0f, 0f, -Spacing);
                extension.AddComponent<S060FollowPose>()
                    .Follow(rod, new Vector3(0f, 0f, -Spacing), true);
                added.Add(extension);
            }

            // 3. A fourth physics axle. Axle_R had one axle and now has two, which
            //    balances the bogies 2 + 2. Its pivot stays on driver 1, as in the
            //    0-4-0 where leaving the pivots alone proved out in game, so the
            //    new axle carries the offset instead.
            var template1 = Parts.AxleFor(1)
                ?? throw new InvalidOperationException("Driver 1 has no [axle].");
            var rearBogie = Parts.BogieFor(1);
            var axle = UnityEngine.Object.Instantiate(template1.gameObject, template1.parent);
            axle.name = AxleName;
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

            // 4. Four powered axles out of four, all the weight on them.
            var powered = new[] { Parts.PoweredFor(1), Parts.PoweredFor(2), Parts.PoweredFor(3), addedPowered };
            if (powered.Any(p => p == null))
                throw new InvalidOperationException("All three stock drivers must keep a PoweredWheel.");
            Physics.Apply(S060Parts.DriverRadius, powered, totalAxles: 4, driverWeightFraction: 1f);
        }

        protected override void Undo()
        {
            try { Physics.Restore(); }
            catch (Exception ex) { Main.Logger.LogException("Restoring S060 physics failed", ex); }

            if (addedAxle != null)
            {
                // Deactivate before destroying so the rebuilt axle list cannot pick
                // up an object that is on its way out this frame.
                addedAxle.gameObject.SetActive(false);
                Parts.BogieFor(1).axles = null;
                addedAxle = null;
            }
            foreach (var part in added) if (part != null) UnityEngine.Object.Destroy(part);
            added.Clear();
            Parts.BogieFor(1).axles = null;
        }
    }
}
