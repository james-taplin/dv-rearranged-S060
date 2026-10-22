#if B99_ALL_STANDARD
using System;
using System.Collections.Generic;
using System.Linq;
using DV.Simulation.Brake;
using DV.Simulation.Controllers;
using DV.Wheels;
using LocoSim.Implementations.Wheels;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RearrangedS060
{
    // 4-14-4 (Soviet AA20-1 layout): one rigid engine with seven coupled drivers.
    // The native drivetrain and its static cylinder assembly are scaled together,
    // as on the Mallet, so that four leading, seven driving and four trailing
    // wheels fit the S282 frame. The seventh driver, axle, rod segment, brake
    // and spark anchors are created lazily and hidden for every other layout.
    internal partial class WheelRearranger
    {
        private const float FourteenTruckSpacing = 1.1f, FourteenLeadingGap = .05f;
        // Drivers (and everything that turns with them) sit this far behind the
        // cylinders' native-scaled position; the crosshead side stays with the
        // cylinders and the main and radius rods are lengthened (DriveRetarget).
        // The trailing truck goes back to the 2-10-4/4-10-4 rear-axle position.
        private const float FourteenDriverShift = .25f;
        private static readonly Vector3 FourteenTrailingLimit = new Vector3(0f, .36f, -3.7f);
        private static readonly string[] FourteenParticleHolders = { "LocoS282A_Particles/CylCockSteam",
            "LocoS282A_Particles/CylCockWaterDrip", "LocoS282A_Particles/CylinderCrack" };

        private Transform driveAxle7, seventhLeftDriveWheel, seventhRightDriveWheel, leftSevenAxleSideRod, rightSevenAxleSideRod,
            leftSeventhBrakeCaliper, rightSeventhBrakeCaliper, leftSeventhBrakeShoe, rightSeventhBrakeShoe, spark7L, spark7R;
        private readonly List<Transform> fourteenAssets = new List<Transform>();

        private sealed class FourteenLayout
        {
            internal float Scale, Offset, DriveOffset, Radius, Pitch, Lead1, Lead2, Trail1, Trail2;
            internal readonly float[] DriverZ = new float[7], Drivers = new float[7];
            internal Matrix4x4 Map => EngineMap(Offset, Scale);            // cylinder side
            internal Matrix4x4 DriveMap => EngineMap(DriveOffset, Scale);  // drivers and their brakes/springs
        }

        // Largest scale at which seven drivers (native spacing, extended one pitch)
        // fit between the native cylinder front and a two-axle trailing truck ending
        // at the 12-coupled trailing position. Native drive frame = body frame.
        private FourteenLayout Measure4144()
        {
            var body = MalletBody;
            float cylinderFront = MeasureNativeCylinderFront();
            var native = new[] { firstLeftDriveWheel, secondLeftDriveWheel, thirdLeftDriveWheel, fourthLeftDriveWheel,
                fifthLeftDriveWheel, sixthLeftDriveWheel }.Select(w => w.localPosition.z).ToArray();
            var layout = new FourteenLayout { Pitch = native[4] - native[5] };
            for (int i = 0; i < 6; i++) layout.DriverZ[i] = native[i];
            layout.DriverZ[6] = native[5] - layout.Pitch;
            float radius = fourthLeftDriveWheel.localPosition.y;
            float trailingLimit = body.InverseTransformPoint(rearBogie.TransformPoint(TriplexTrailingLimit)).z;
            float s = (cylinderFront - trailingLimit - FourteenTruckSpacing - MalletTrailingRadius - MalletEngineGap)
                / (cylinderFront - layout.DriverZ[6] + radius);
            if (!(s > .4f && s < 1f)) throw new InvalidOperationException("4-14-4 drivers do not fit the S282 frame (scale " + s + ").");
            layout.Scale = s; layout.Radius = s * radius;
            layout.Offset = cylinderFront * (1f - s);
            layout.DriveOffset = layout.Offset - FourteenDriverShift;
            // Leading truck where it was before the drivers moved back.
            layout.Lead2 = layout.Offset + s * layout.DriverZ[0] + layout.Radius + MalletTrailingRadius + FourteenLeadingGap;
            layout.Lead1 = layout.Lead2 + FourteenTruckSpacing;
            for (int i = 0; i < 7; i++) layout.Drivers[i] = layout.DriveOffset + s * layout.DriverZ[i];
            layout.Trail2 = body.InverseTransformPoint(rearBogie.TransformPoint(FourteenTrailingLimit)).z;
            layout.Trail1 = layout.Trail2 + FourteenTruckSpacing;
            if (layout.Trail1 > layout.Drivers[6] - layout.Radius - MalletTrailingRadius - MalletEngineGap)
                throw new InvalidOperationException("4-14-4 trailing truck overlaps the last driver.");
            return layout;
        }

        private void Apply4144Geometry()
        {
            var layout = Measure4144();
            Ensure4144Assets(layout);
            float s = layout.Scale;
            var body = MalletBody;
            ResetBody(); ResetToFourCoupled(); HideLeadingWheels(); HideTrailingWheels();
            ShowFourLeadingWheels();
            ShowFourTrailingWheelsFor10Coupled();
            MoveTruckAxle(frontBogie, firstFrontAxle, layout.Lead1, firstFrontAxleSupport, firstFrontAxleSupportLOD);
            MoveTruckAxle(frontBogie, secondFrontAxle, layout.Lead2, secondFrontAxleSupport, secondFrontAxleSupportLOD);
            MoveTruckAxle(rearBogie, secondRearAxle, layout.Trail1, secondRearAxleSupport);
            MoveTruckAxle(rearBogie, thirdRearAxle, layout.Trail2);
            setWheelRadius(.712f * s);

            driveL.localPosition = driveR.localPosition = new Vector3(0f, 0f, layout.DriveOffset);
            driveL.localScale = new Vector3(-1f, s, s); driveR.localScale = new Vector3(1f, s, s);
            foreach (var wheel in new[] { fifthLeftDriveWheel, fifthRightDriveWheel, sixthLeftDriveWheel, sixthRightDriveWheel,
                seventhLeftDriveWheel, seventhRightDriveWheel }) wheel.gameObject.SetActive(true);
            // Flanged ends, blind centre drivers, native counterweighted main driver.
            var meshes = new[] { flangedDriver, blindDriver, blindDriverBigWeight, blindDriver, blindDriver, flangedDriver, flangedDriver };
            var left = new[] { firstLeftDriveWheel, secondLeftDriveWheel, thirdLeftDriveWheel, fourthLeftDriveWheel,
                fifthLeftDriveWheel, sixthLeftDriveWheel, seventhLeftDriveWheel };
            var right = new[] { firstRightDriveWheel, secondRightDriveWheel, thirdRightDriveWheel, fourthRightDriveWheel,
                fifthRightDriveWheel, sixthRightDriveWheel, seventhRightDriveWheel };
            for (int i = 0; i < 7; i++)
            {
                left[i].GetComponent<MeshFilter>().sharedMesh = right[i].GetComponent<MeshFilter>().sharedMesh = meshes[i];
                left[i].localPosition = new Vector3(left[i].localPosition.x, left[i].localPosition.y, layout.DriverZ[i]);
                right[i].localPosition = new Vector3(right[i].localPosition.x, right[i].localPosition.y, layout.DriverZ[i]);
            }
            // Stock rod drivers 1-4, the 12-coupled extension moved back one pitch: 4-7.
            leftSixAxleSideRod.gameObject.SetActive(false); rightSixAxleSideRod.gameObject.SetActive(false);
            leftSevenAxleSideRod.gameObject.SetActive(true); rightSevenAxleSideRod.gameObject.SetActive(true);
            leftReachRod.gameObject.SetActive(false); rightReachRod.gameObject.SetActive(false);
            driveR.Find("s282_mech_cutoff_rail/s282_mech_lubricator_rod").gameObject.SetActive(false);
            driveR.Find("LubricatorParts").gameObject.SetActive(false);
            foreach (var part in new[] { lDryPipe, rDryPipe, fOilLines }) part.gameObject.SetActive(false);

            foreach (var shoes in new[] { leftBrakeShoes, rightBrakeShoes })
                foreach (Transform shoe in shoes) shoe.gameObject.SetActive(true);
            leftSeventhBrakeShoe.localPosition = rightSeventhBrakeShoe.localPosition =
                rightSixthBrakeShoe.localPosition + Vector3.back * layout.Pitch;
            var calipersL = new[] { leftFirstBrakeCaliper, leftSecondBrakeCaliper, leftThirdBrakeCaliper, leftFourthBrakeCaliper,
                leftFifthBrakeCaliper, leftSixthBrakeCaliper, leftSeventhBrakeCaliper };
            var calipersR = new[] { rightFirstBrakeCaliper, rightSecondBrakeCaliper, rightThirdBrakeCaliper, rightFourthBrakeCaliper,
                rightFifthBrakeCaliper, rightSixthBrakeCaliper, rightSeventhBrakeCaliper };
            // Calipers 5-7 are copies of caliper 3, placed at their drivers.
            for (int i = 4; i < 7; i++)
            {
                calipersL[i].localPosition = leftThirdBrakeCaliper.localPosition + Vector3.forward * (layout.DriverZ[i] - layout.DriverZ[2]);
                calipersR[i].localPosition = rightThirdBrakeCaliper.localPosition + Vector3.forward * (layout.DriverZ[i] - layout.DriverZ[2]);
            }
            foreach (var caliper in calipersL.Concat(calipersR)) caliper.gameObject.SetActive(true);

            foreach (var part in new[] { lfCylinder, rfCylinder, cylinderCocks, lfCrossheadBracket, rfCrossheadBracket })
                MapMalletPart(part, layout.Map);
            var driverParts = new[] { lLiftingArmSupport, rLiftingArmSupport }.Concat(calipersL).Concat(calipersR)
                .Concat(new[] { "l", "r" }.SelectMany(side => Enumerable.Range(1, 4).Select(i => RequireArticulatedPart(splitLoco, "s282a_suspension_" + side + "_" + i))));
            foreach (var part in driverParts) MapMalletPart(part, layout.DriveMap);
            foreach (var collider in MalletColliders.Select(path => transform.Find(path)).Where(t => t != null)) MapMalletPart(collider, layout.Map);
            // Particle holders follow the cylinders without stretching their emitters.
            foreach (var holder in FourteenParticleHolders.Select(path => transform.Find(path)).Where(t => t != null))
            {
                var centre = holder.TransformPoint(PacificHudsonAffine.EmitterCentreLocal(holder));
                holder.position += body.TransformPoint(layout.Map.MultiplyPoint3x4(body.InverseTransformPoint(centre))) - centre;
            }

            Bind4144BrakeHeatAndSparks();
            driveAxle5.gameObject.SetActive(true); driveAxle6.gameObject.SetActive(true); driveAxle7.gameObject.SetActive(true);
            var axles = new[] { driveAxle1, driveAxle2, driveAxle3, driveAxle4, driveAxle5, driveAxle6, driveAxle7 };
            for (int i = 0; i < 7; i++) AlignAdditionalAxle(axles[i], right[i]);
            var sparksL = new[] { spark1L, spark2L, spark3L, spark4L, spark5L, spark6L, spark7L };
            var sparksR = new[] { spark1R, spark2R, spark3R, spark4R, spark5R, spark6R, spark7R };
            for (int i = 0; i < 7; i++)
            {
                var point = transform.InverseTransformPoint(axles[i].position);
                sparksL[i].position = transform.TransformPoint(new Vector3(-.75f, 0f, point.z));
                sparksR[i].position = transform.TransformPoint(new Vector3(.75f, 0f, point.z));
                sparksL[i].gameObject.SetActive(true); sparksR[i].gameObject.SetActive(true);
            }
            // Crosshead side back to the cylinders; main and radius rods lengthened.
            foreach (var drive in new[] { driveL, driveR })
                DriveRetarget.Bind(drive, FourteenDriverShift / s, DriveRetarget.NativeWalschaertsRole, transform);
            Main.Logger.Log($"4-14-4: scale {s:0.000}, {2f * layout.Radius:0.00} m drivers {layout.Drivers[0]:0.00}..{layout.Drivers[6]:0.00}, " +
                $"leading {layout.Lead1:0.00}/{layout.Lead2:0.00}, trailing {layout.Trail1:0.00}/{layout.Trail2:0.00} (body z).");
        }

        // Moves a truck axle to a body-frame Z and its support frame by the same amount.
        private void MoveTruckAxle(Transform bogie, Transform axle, float bodyZ, params Transform[] supports)
        {
            float target = bogie.InverseTransformPoint(MalletBody.TransformPoint(new Vector3(0f, 0f, bodyZ))).z;
            float shift = target - axle.localPosition.z;
            axle.localPosition += Vector3.forward * shift;
            foreach (var support in supports) if (support != null) support.localPosition += Vector3.forward * shift;
        }

        // Native arrays (brake renderers, spark definitions/anchors) are restored by
        // preparedGeometry on every switch, so the seventh driver is re-added here.
        private void Bind4144BrakeHeatAndSparks()
        {
            var brakes = GetComponent<BrakesOverheatingController>();
            if (brakes != null)
                brakes.brakeRenderers = brakes.brakeRenderers.Concat(new[] { leftSeventhBrakeShoe, rightSeventhBrakeShoe }
                    .Select(t => t.GetComponent<MeshRenderer>())).Distinct().ToArray();
            var wheelSparks = spark5L.parent;
            var slip = wheelSparks.GetComponent<WheelslipSparksController>();
            if (slip != null)
            {
                var sparks = new WheelslipSparksController.WheelSparksDefinition
                {
                    poweredWheel = driveAxle7.GetComponent<PoweredWheel>(),
                    sparksLeftAnchor = spark7L,
                    sparksRightAnchor = spark7R
                };
                sparks.Init();
                slip.wheelSparks = slip.wheelSparks.Append(sparks).ToArray();
            }
            var slide = wheelSparks.GetComponent<WheelSlideSparksController>();
            if (slide != null) { slide.sparkAnchors = slide.sparkAnchors.Append(spark7L).Append(spark7R).ToArray(); slide.sparks = null; }
        }

        // Lazily created from the prepared pose, the same way setup adds drivers 5/6.
        private void Ensure4144Assets(FourteenLayout layout)
        {
            if (driveAxle7 != null) return;
            var created = new List<Transform>();
            Transform Add(Transform t, string name) { t.name = name; t.gameObject.SetActive(false); created.Add(t); return t; }
            Transform Wheel(Transform fourth)
            {
                var wheel = Object.Instantiate(fourth.gameObject, fourth.parent).transform;
                wheel.GetChild(0).gameObject.SetActive(false);
                wheel.gameObject.AddComponent<PoweredWheelRotater>().Init(fourth.gameObject);
                return Add(wheel, "s282_wheels_driving_7");
            }
            Transform Rod(Transform six)
            {
                var rod = Object.Instantiate(six.gameObject, six.parent).transform;
                rod.localPosition = six.localPosition + Vector3.back * layout.Pitch;
                return Add(rod, "[Rearranged seven-axle rod]");
            }
            Transform Spark(string name)
            {
                var spark = new GameObject(name).transform;
                spark.SetParent(spark5L.parent, false);
                return Add(spark, name);
            }
            try
            {
                seventhLeftDriveWheel = Wheel(fourthLeftDriveWheel);
                seventhRightDriveWheel = Wheel(fourthRightDriveWheel);
                leftSevenAxleSideRod = Rod(leftSixAxleSideRod);
                rightSevenAxleSideRod = Rod(rightSixAxleSideRod);
                leftSeventhBrakeCaliper = Add(Object.Instantiate(leftThirdBrakeCaliper.gameObject, splitLoco).transform, "s282a_caliper_l_7");
                rightSeventhBrakeCaliper = Add(Object.Instantiate(rightThirdBrakeCaliper.gameObject, splitLoco).transform, "s282a_caliper_r_7");
                leftSeventhBrakeShoe = Add(Object.Instantiate(rightFirstBrakeShoe.gameObject, leftBrakeShoes).transform, "s282a_brake_shoe_7");
                rightSeventhBrakeShoe = Add(Object.Instantiate(rightFirstBrakeShoe.gameObject, rightBrakeShoes).transform, "s282a_brake_shoe_7");
                spark7L = Spark("spark13");
                spark7R = Spark("spark14");
                driveAxle7 = Add(Object.Instantiate(driveAxle4.gameObject, rearBogie).transform, "[axle] drive 4");
                fourteenAssets.AddRange(created);
            }
            catch
            {
                foreach (var t in created.Where(t => t != null)) { t.gameObject.SetActive(false); Object.Destroy(t.gameObject); }
                driveAxle7 = seventhLeftDriveWheel = seventhRightDriveWheel = leftSevenAxleSideRod = rightSevenAxleSideRod = null;
                leftSeventhBrakeCaliper = rightSeventhBrakeCaliper = leftSeventhBrakeShoe = rightSeventhBrakeShoe = spark7L = spark7R = null;
                throw;
            }
        }

        // Every switch runs this before preparedGeometry.Restore, which does not
        // know these lazily created objects.
        private void Suspend4144Geometry()
        {
            DriveRetarget.Unbind(driveL); DriveRetarget.Unbind(driveR);
            foreach (var asset in fourteenAssets) if (asset != null) asset.gameObject.SetActive(false);
        }
    }
}
#endif
