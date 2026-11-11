using System;
using System.Collections.Generic;
using DV.Wheels;
using UnityEngine;

namespace RearrangedS060
{
    // Native S282 precedent: unpowered axle presentation and physics live under
    // the existing rear bogie. Do not add an unsupported third support bogie.
    internal sealed class S060SixTwo : S060Conversion
    {
        private readonly List<GameObject> added = new List<GameObject>();
        private WheelRotationViaCode rotation;
        internal override string Name => "0-6-2 (stock drivers, unpowered rear pony axle)";

        protected override void Apply()
        {
            if (Scale.Factor != 1f) throw new InvalidOperationException("The first 0-6-2 layout uses stock-size drivers.");
            var donor = S060PonyAssets.Resolve();
            var parent = Parts.AxleFor(1).parent;
            var axle = UnityEngine.Object.Instantiate(donor.Axle.gameObject, parent);
            added.Add(axle);
            axle.name = "[axle] rearranged S060 trailing";
            axle.SetActive(false);
            // Stock S282 uses radius .355 and axle height .36. Retain the native
            // rail-height offset, wheel mesh width and complete wheelset.
            float height = donor.Radius + .005f;
            var centre = new Vector3(0f, height, S060TrailingLayout.CentreZ);
            axle.transform.localPosition = parent.InverseTransformPoint(Parts.Car.transform.TransformPoint(centre));
            axle.transform.localRotation = donor.Axle.localRotation;
            axle.transform.localScale = donor.Axle.localScale;

            AddSupport(donor.Support, donor, centre, "[Rearranged S060 pony support]");
            // The stock LOD0 support alone is retained initially; its tiny mesh
            // avoids a second unregistered LOD renderer drawing on top of it.
            rotation = Parts.Car.gameObject.AddComponent<WheelRotationViaCode>();
            rotation.wheelRadius = donor.Radius;
            rotation.wheelCircumference = 2f * Mathf.PI * donor.Radius;
            rotation.rotationAxis = donor.RotationAxis;
            rotation.affectedByWheelSlide = donor.AffectedBySlide;
            rotation.transformsToRotate = new[] { axle.transform };
            axle.SetActive(true);
            Parts.RearBogie.axles = null;

            foreach (int driver in new[] { 1, 2, 3 })
                AxlePairs.Add((Parts.AxleFor(driver), Parts.RightWheel(driver)));
            Physics.Apply(Scale.DriverRadius,
                new[] { Parts.PoweredFor(1), Parts.PoweredFor(2), Parts.PoweredFor(3) },
                S060TrailingLayout.TotalAxles, ConventionalPhysics.RatedDriverWeightFraction(
                    S060TrailingLayout.PoweredAxles, S060TrailingLayout.TotalAxles));
            Main.Logger.Log("S060 0-6-2 donor: " + donor.Axle.name + ", native pony radius " + donor.Radius +
                ", rear centre Z " + S060TrailingLayout.CentreZ + ". In-game truck/frame clearance remains unverified.");
        }

        private void AddSupport(Transform source, S060PonyAssets donor, Vector3 centre, string name)
        {
            if (source == null) return;
            var support = UnityEngine.Object.Instantiate(source.gameObject, Parts.Body);
            added.Add(support);
            support.name = name;
            var offset = donor.Root.InverseTransformPoint(source.position) - donor.Root.InverseTransformPoint(donor.Axle.position);
            support.transform.position = Parts.Car.transform.TransformPoint(centre + offset);
            support.transform.localRotation = source.localRotation;
            support.transform.localScale = source.localScale;
            support.SetActive(true);
        }

        protected override void Undo()
        {
            try { Physics.Restore(); }
            finally
            {
                if (rotation != null)
                {
                    rotation.enabled = false;
                    rotation.transformsToRotate = Array.Empty<Transform>();
                    // Native Awake subscribes to this event; native destruction
                    // has no matching unsubscribe. Remove our transient reader
                    // before destroying it, including failed conversion cleanup.
                    Parts.Car.MovementStateChanged -= rotation.OnMovementStateChanged;
                    UnityEngine.Object.Destroy(rotation);
                    rotation = null;
                }
                foreach (var item in added)
                    if (item != null) { item.SetActive(false); UnityEngine.Object.Destroy(item); }
                added.Clear();
                Parts.RearBogie.axles = null;
            }
        }
    }
}
