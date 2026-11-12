using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RearrangedS060
{
    // Conventional four/five-axle extension of the native S060 mechanism. The
    // ten-coupled variant changes only its axle count and fitted rear station.
    internal class S060EightCoupled : S060Conversion
    {
        internal const float Driver4Z = S060Parts.Driver1Z - S060EightLayout.NativePitch;
        protected virtual int DriverCount => 4;
        protected virtual float RearStation => S060Parts.Driver1Z;
        private readonly List<GameObject> added = new List<GameObject>();
        private readonly List<(Transform mechanism, Vector3 position)> mechanismHome = new List<(Transform, Vector3)>();
        private float cylinderShift;
        protected override float CylinderShift => cylinderShift;
        internal override string Name => "0-8-0 (small drivers, rear axle at stock station)";

        protected override void Apply() => Physics.Apply(Scale.DriverRadius, BuildGeometry(), totalAxles: DriverCount,
            driverWeightFraction: ConventionalPhysics.RatedDriverWeightFraction(DriverCount, DriverCount));

        internal LocoSim.Implementations.Wheels.PoweredWheel[] BuildGeometry()
        {
            if (Scale.Factor > S060EightLayout.MaximumScale)
                throw new InvalidOperationException("The fitted coupled layout requires small drivers.");
            var powered = new List<LocoSim.Implementations.Wheels.PoweredWheel>();
            foreach (int driver in new[] { 1, 2, 3 })
            {
                var wheel = Parts.PoweredFor(driver) ?? throw new InvalidOperationException("Native driven axle missing.");
                powered.Add(wheel); AxlePairs.Add((Parts.AxleFor(driver), Parts.RightWheel(driver)));
            }
            Transform lastWheel = null;
            for (int driver = 4; driver <= DriverCount; driver++)
            {
                float z = S060Parts.Driver1Z - (driver - 3) * S060EightLayout.NativePitch;
                Transform right = null;
                foreach (var template in Parts.Wheels(2))
                {
                    var wheel = UnityEngine.Object.Instantiate(template.gameObject, template.parent);
                    added.Add(wheel); wheel.name = "s060_Wheels_0" + driver + " (rearranged)";
                    wheel.transform.localPosition = new Vector3(template.localPosition.x, template.localPosition.y, z);
                    var follower = Parts.Wheels(1).Single(w => w.parent == template.parent);
                    wheel.AddComponent<S060FollowPose>().Follow(follower, Vector3.zero, false);
                    if (template.parent == Parts.MechanismRight) right = wheel.transform;
                }
                var original = Parts.AxleFor(1) ?? throw new InvalidOperationException("Native rear physics axle missing.");
                var axle = UnityEngine.Object.Instantiate(original.gameObject, original.parent);
                added.Add(axle); axle.name = "[axle] rearranged " + driver;
                axle.transform.localPosition = original.localPosition + Vector3.back * ((driver - 3) * S060EightLayout.NativePitch);
                var wheelPhysics = axle.GetComponent<LocoSim.Implementations.Wheels.PoweredWheel>() ?? throw new InvalidOperationException("Cloned physics axle is not powered.");
                wheelPhysics.wheelTransform = right; powered.Add(wheelPhysics);
                AxlePairs.Add((axle.transform, right)); lastWheel = right;
            }
            float rear = Parts.Car.transform.InverseTransformPoint(lastWheel.position).z;
            cylinderShift = RearStation - rear;
            foreach (var mechanism in new[] { Parts.MechanismLeft, Parts.MechanismRight })
            {
                mechanismHome.Add((mechanism, mechanism.localPosition));
                mechanism.position += Parts.Car.transform.TransformVector(Vector3.forward * cylinderShift);
            }
            // Native two bogies carry 2+2 or 2+3 real powered axles; no added
            // rigidbody or separate locomotive is introduced.
            Parts.RearBogie.axles = null;
            return powered.ToArray();
        }
        protected override void Undo()
        {
            try { Physics.Restore(); }
            finally { RestoreGeometry(); }
        }
        internal void RestoreGeometry()
        {
            foreach (var home in mechanismHome) if (home.mechanism != null) home.mechanism.localPosition = home.position;
            mechanismHome.Clear();
            foreach (var part in added) if (part != null)
            {
                part.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(part); else UnityEngine.Object.DestroyImmediate(part);
            }
            added.Clear(); Parts.RearBogie.axles = null;
        }
    }
    internal sealed class S060TenCoupled : S060EightCoupled
    {
        protected override int DriverCount => 5;
        protected override float RearStation => S060TenLayout.Rear;
        internal override string Name => "0-10-0 Tank (1.00 m drivers)";
    }
}
