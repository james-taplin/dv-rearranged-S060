#if B99_ALL_STANDARD
using System;
using DV.Wheels;
using LocoSim.Implementations.Wheels;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RearrangedS282
{
    internal partial class WheelRearranger
    {
        // Clearance prototype only: retain the native animation/oil-provider
        // hierarchy, but do not present the stock rods as fitted to huge cranks.
        private readonly Transform[] giantSingleWheels = new Transform[2];
        internal float GiantSingleDriverDiameter { get; private set; }
        internal int GiantSingleDriverColliderCount
        {
            get
            {
                int count = 0;
                foreach (var wheel in giantSingleWheels)
                    if (wheel != null) count += wheel.GetComponentsInChildren<Collider>(true).Length;
                return count;
            }
        }

        internal static float ClampGiantDriverDiameter(float diameter)
            => float.IsNaN(diameter) || float.IsInfinity(diameter) ? 2.8f : Mathf.Clamp(diameter, 2.1f, 3.6f);

        private void ApplyGiantSingleDriverPrototype()
        {
            float radius = ClampGiantDriverDiameter(Main.settings.giant222DriverDiameterMetres) * .5f;
            float stockRadius = stage2Physics.DriverRadius;
            // Scale only new renderer objects. Whole-drive scaling would also
            // stretch native oilcup anchors and animate the crank in the wrong place.
            driveL.localPosition = driveR.localPosition = Vector3.zero;
            driveL.localScale = new Vector3(-1f, 1f, 1f);
            driveR.localScale = Vector3.one;
            driveL.gameObject.SetActive(true); driveR.gameObject.SetActive(true);
            foreach (var drive in new[] {driveL, driveR})
                foreach (var renderer in drive.GetComponentsInChildren<MeshRenderer>(true)) renderer.enabled = false;

            // Leave Oiling0/1/2 and the native crank/connecting-rod transforms
            // alive. Their meshes are hidden for this explicitly unfinished rig.
            foreach (var axle in new[] {driveAxle1, driveAxle2, driveAxle4, driveAxle5, driveAxle6})
                axle.gameObject.SetActive(false);
            driveAxle3.gameObject.SetActive(true);
            foreach (var brake in new[] {leftFirstBrakeCaliper, leftSecondBrakeCaliper, leftThirdBrakeCaliper,
                leftFourthBrakeCaliper, leftFifthBrakeCaliper, leftSixthBrakeCaliper,
                rightFirstBrakeCaliper, rightSecondBrakeCaliper, rightThirdBrakeCaliper,
                rightFourthBrakeCaliper, rightFifthBrakeCaliper, rightSixthBrakeCaliper})
                brake.gameObject.SetActive(false);

            var drives = new[] {driveL, driveR};
            var sources = new[] {thirdLeftDriveWheel, thirdRightDriveWheel};
            float middleZ = (secondRightDriveWheel.localPosition.z + thirdRightDriveWheel.localPosition.z) * .5f;
            for (int i = 0; i < giantSingleWheels.Length; i++)
            {
                if (giantSingleWheels[i] == null)
                {
                    var visual = new GameObject("[Rearranged giant single driver " + (i == 0 ? "L" : "R") + "]");
                    visual.transform.SetParent(drives[i], false);
                    visual.AddComponent<MeshFilter>().sharedMesh = flangedDriver;
                    visual.AddComponent<MeshRenderer>().sharedMaterials = sources[i].GetComponent<MeshRenderer>().sharedMaterials;
                    visual.AddComponent<PoweredWheelRotater>().Init(sources[i].gameObject);
                    giantSingleWheels[i] = visual.transform;
                }
                var wheel = giantSingleWheels[i];
                wheel.localPosition = new Vector3(sources[i].localPosition.x,
                    sources[i].localPosition.y + radius - stockRadius, middleZ);
                wheel.localScale = new Vector3(1f, radius / stockRadius, radius / stockRadius);
                wheel.localRotation = sources[i].localRotation;
                wheel.GetComponent<MeshRenderer>().enabled = true;
                wheel.gameObject.SetActive(true);
            }
            setWheelRadius(radius);
            GiantSingleDriverDiameter = radius * 2f;

            // Manager index zero is the sole retained powered axle after commit.
            // Spark arrays use that index, regardless of the original axle number.
            var contact = transform.InverseTransformPoint(giantSingleWheels[1].position);
            contact.y -= radius;
            spark1L.localPosition = new Vector3(-.75f, contact.y, contact.z);
            spark1R.localPosition = new Vector3(.75f, contact.y, contact.z);
            spark1L.gameObject.SetActive(true); spark1R.gameObject.SetActive(true);
            foreach (var spark in new[] {spark2L, spark2R, spark3L, spark3R, spark4L, spark4R,
                spark5L, spark5R, spark6L, spark6R}) spark.gameObject.SetActive(false);
            Main.Logger.Log("2-2-2 clearance prototype: " + GiantSingleDriverDiameter.ToString("F2") +
                " m tread diameter; renderer-only drivers, hidden unfinished rods, unchanged native body colliders.");
        }

        private void AlignAdditionalPoweredAxles(int id)
        {
            if (id == (int)WheelArrangementType.s260)
            {
                AlignAdditionalAxle(driveAxle2, secondRightDriveWheel);
                AlignAdditionalAxle(driveAxle3, thirdRightDriveWheel);
                AlignAdditionalAxle(driveAxle4, fourthRightDriveWheel);
            }
            else if (id == (int)WheelArrangementType.s240)
            {
                AlignAdditionalAxle(driveAxle2, secondRightDriveWheel);
                AlignAdditionalAxle(driveAxle3, thirdRightDriveWheel);
            }
            else if (id == (int)WheelArrangementType.s222)
                AlignAdditionalAxle(driveAxle3, giantSingleWheels[1]);
        }

        private static void AlignAdditionalAxle(Transform axle, Transform visual)
        {
            axle.position = visual.position;
            var powered = axle.GetComponent<PoweredWheel>();
            if (powered == null) throw new InvalidOperationException("Additional arrangement is missing a native powered axle.");
            powered.wheelTransform = visual;
        }

        private void SuspendAdditionalGeometry()
        {
            foreach (var wheel in giantSingleWheels) if (wheel != null) wheel.gameObject.SetActive(false);
            GiantSingleDriverDiameter = 0f;
        }

        private void DisposeAdditionalGeometry()
        {
            SuspendAdditionalGeometry();
            for (int i = 0; i < giantSingleWheels.Length; i++)
            {
                if (giantSingleWheels[i] != null) Object.Destroy(giantSingleWheels[i].gameObject);
                giantSingleWheels[i] = null;
            }
        }
    }
}
#endif
