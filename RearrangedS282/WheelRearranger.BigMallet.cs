#if B99_ALL_STANDARD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RearrangedS282
{
    // Eight-coupled rear-engine Mallets, built like the 2-6-6-2 (each engine a
    // uniformly scaled copy of the native drivetrain and its static cylinder
    // assembly) but with the native four-axle engine at the rear:
    // - 0-8-8-0 "Angus": two native four-axle engines, no carrying trucks; the
    //   front cylinders reach further over the (absent) leading truck.
    // - 2-6-8-0 (Great Northern M-1/M-2 style): the 2-6-6-2's three-axle front
    //   engine over its leading truck, no trailing truck.
    // Both end where the 2-2-2-2-2's trailing wheel does. The extra physical
    // powered axles (one per native bogie) are created lazily.
    internal partial class WheelRearranger
    {
        private const float AngusFrontExtra = .6f;

        private sealed class BigMalletLayout
        {
            internal WheelArrangementType Kind;
            internal int FrontCoupled;
            internal float Scale, FrontOffset, RearOffset, FrontLastDriver, RearCylinderFront, RearLastDriver, Radius, PinZ, FirstDriverShift;
            internal Matrix4x4 Front => EngineMap(FrontOffset, Scale);
            internal Matrix4x4 Rear => EngineMap(RearOffset, Scale);
        }

        private sealed class BigMalletAssets
        {
            internal float Scale = float.NaN;
            internal readonly List<Transform> Statics = new List<Transform>();
        }

        private Transform bigMalletRearL, bigMalletRearR, bigMalletFrontAxle, bigMalletRearAxle;
        private readonly List<Transform> bigMalletAxles = new List<Transform>();
        private readonly Dictionary<WheelArrangementType, BigMalletAssets> bigMalletStatics = new Dictionary<WheelArrangementType, BigMalletAssets>();
        private BigMalletLayout bigMallet;

        internal static bool IsBigMallet(int id) => id == (int)WheelArrangementType.s08880 || id == (int)WheelArrangementType.s2680;

        // Extra powered axles in use by the current layout (for the powered list).
        private IEnumerable<Transform> BigMalletExtraAxles() => bigMalletAxles.Where(t => t != null);

        private BigMalletLayout MeasureBigMallet(WheelArrangementType kind)
        {
            var body = MalletBody;
            float cylinderFront = MeasureNativeCylinderFront();
            float d2 = secondLeftDriveWheel.localPosition.z, d3 = thirdLeftDriveWheel.localPosition.z, d4 = fourthLeftDriveWheel.localPosition.z;
            float radius = secondLeftDriveWheel.localPosition.y;
            if (d2 <= d3 || d3 <= d4) throw new InvalidOperationException("Native S282 driver order is unexpected.");
            int frontCoupled = kind == WheelArrangementType.s08880 ? 4 : 3;
            float front = cylinderFront + (kind == WheelArrangementType.s08880 ? AngusFrontExtra : 0f);
            float frontLastNative = frontCoupled == 4 ? d4 : d3;
            // Last driver's rear edge where the 2-2-2-2-2's trailing wheel ends.
            float rearLimit = body.InverseTransformPoint(rearBogie.TransformPoint(TriplexOwnTrailingLimit)).z - MalletTrailingRadius;
            float s = (front - rearLimit - MalletEngineGap) / ((cylinderFront - frontLastNative) + (cylinderFront - d4) + 2f * radius);
            if (!(s > .4f && s < 1f)) throw new InvalidOperationException($"{kind} engines do not fit the S282 frame (scale {s}).");
            var layout = new BigMalletLayout { Kind = kind, FrontCoupled = frontCoupled, Scale = s, Radius = s * radius,
                FirstDriverShift = d2 + (d2 - d3) - firstLeftDriveWheel.localPosition.z };
            layout.FrontOffset = front - s * cylinderFront;
            layout.FrontLastDriver = layout.FrontOffset + s * frontLastNative;
            layout.RearCylinderFront = layout.FrontLastDriver - layout.Radius - MalletEngineGap;
            layout.RearOffset = layout.RearCylinderFront - s * cylinderFront;
            layout.RearLastDriver = layout.RearOffset + s * d4;
            layout.PinZ = (layout.FrontLastDriver + layout.RearCylinderFront) * .5f;
            return layout;
        }

        private void ApplyBigMalletGeometry(WheelArrangementType kind)
        {
            bigMallet = MeasureBigMallet(kind);
            EnsureBigMalletAssets(bigMallet);
            float s = bigMallet.Scale;
            bool angus = kind == WheelArrangementType.s08880;
            ResetBody(); ResetToFourCoupled(); HideLeadingWheels(); HideTrailingWheels();
            if (!angus) ShowTwoLeadingWheels();
            setWheelRadius(.712f * s);

            driveL.localPosition = driveR.localPosition = new Vector3(0f, 0f, bigMallet.FrontOffset);
            driveL.localScale = new Vector3(-1f, s, s); driveR.localScale = new Vector3(1f, s, s);
            if (angus) { ConfigureEightCoupledDrive(driveL, false); ConfigureEightCoupledDrive(driveR, false); }
            else { ConfigureMalletDrive(driveL, false); ConfigureMalletDrive(driveR, false); }
            leftReachRod.gameObject.SetActive(false); rightReachRod.gameObject.SetActive(false);
            foreach (var part in new[] { lDryPipe, rDryPipe, reachRodSupport, lubricatorSupport, fOilLines })
                part.gameObject.SetActive(false);
            foreach (var part in new[] { leftFifthBrakeCaliper, rightFifthBrakeCaliper, leftSixthBrakeCaliper, rightSixthBrakeCaliper })
                part.gameObject.SetActive(false);
            if (!angus)
            {
                leftFourthBrakeCaliper.gameObject.SetActive(false); rightFourthBrakeCaliper.gameObject.SetActive(false);
                RequireArticulatedPart(splitLoco, "s282a_suspension_l_4").gameObject.SetActive(false);
                RequireArticulatedPart(splitLoco, "s282a_suspension_r_4").gameObject.SetActive(false);
            }

            var frontStatic = angus ? EightCoupledStaticParts() : MalletStaticParts();
            foreach (var part in frontStatic)
            {
                if (!angus) PreShiftBigMalletPart(part);
                MapMalletPart(part, bigMallet.Front);
            }
            var colliders = MalletColliders.Select(path => transform.Find(path)).Where(t => t != null).ToArray();
            foreach (var collider in colliders) MapMalletPart(collider, bigMallet.Front);

            foreach (var asset in bigMalletStatics[kind].Statics) if (asset != null) asset.gameObject.SetActive(true);
            bigMalletRearL.localPosition = bigMalletRearR.localPosition = new Vector3(0f, 0f, bigMallet.RearOffset);
            bigMalletRearL.localScale = new Vector3(-1f, s, s); bigMalletRearR.localScale = new Vector3(1f, s, s);
            bigMalletRearL.gameObject.SetActive(true); bigMalletRearR.gameObject.SetActive(true);
            scaledEngineMaps = new[] { bigMallet.Front, bigMallet.Rear };
            BindScaledEngineAnimation(bigMalletRearL, bigMalletRearR);
            BindEngineBrakeHeat(new[] { bigMalletRearL, bigMalletRearR }, 1, 2, 3, 4);

            // Front engine axles in the native front bogie, rear engine in the rear.
            driveAxle5.gameObject.SetActive(true); driveAxle6.gameObject.SetActive(true);
            bigMalletRearAxle.gameObject.SetActive(true);
            bigMalletFrontAxle.gameObject.SetActive(angus);
            AlignNativeAxle(driveAxle1, firstLeftDriveWheel, true);
            AlignNativeAxle(driveAxle2, secondLeftDriveWheel, true);
            AlignNativeAxle(driveAxle6, thirdLeftDriveWheel, true);
            if (angus) AlignNativeAxle(bigMalletFrontAxle, fourthLeftDriveWheel, true);
            AlignNativeAxle(driveAxle3, RequireArticulatedPart(bigMalletRearL, "s282_wheels_driving_1"), false);
            AlignNativeAxle(driveAxle4, RequireArticulatedPart(bigMalletRearL, "s282_wheels_driving_2"), false);
            AlignNativeAxle(driveAxle5, RequireArticulatedPart(bigMalletRearL, "s282_wheels_driving_3"), false);
            AlignNativeAxle(bigMalletRearAxle, RequireArticulatedPart(bigMalletRearL, "s282_wheels_driving_4"), false);

            foreach (var part in new[] { driveL, driveR }.Concat(frontStatic).Concat(colliders))
                MoveArticulatedPart(part, articulatedFrontRoot);
            UpdateMalletSparks();
            ConfigureNativeArticulation((int)kind, bigMallet.PinZ);
            BindArticulatedLods();
            Main.Logger.Log($"Articulated S282 {(angus ? "0-8-8-0" : "2-6-8-0")}: scale {s:0.000}, {2f * bigMallet.Radius:0.00} m drivers; " +
                $"engines at {bigMallet.FrontOffset:0.00}/{bigMallet.RearOffset:0.00}, pin {bigMallet.PinZ:0.00}, " +
                $"rear cylinders {bigMallet.RearCylinderFront:0.00}, last driver {bigMallet.RearLastDriver:0.00} (body z).");
        }

        // The native four-axle engine: drivers 1-4 with the native rods.
        private Transform[] EightCoupledStaticParts() => new[] { lfCylinder, rfCylinder, cylinderCocks,
            lfCrossheadBracket, rfCrossheadBracket, lLiftingArmSupport, rLiftingArmSupport,
            leftFirstBrakeCaliper, leftSecondBrakeCaliper, leftThirdBrakeCaliper, leftFourthBrakeCaliper,
            rightFirstBrakeCaliper, rightSecondBrakeCaliper, rightThirdBrakeCaliper, rightFourthBrakeCaliper }
            .Concat(new[] { "l", "r" }.SelectMany(side => Enumerable.Range(1, 4).Select(i => RequireArticulatedPart(splitLoco, "s282a_suspension_" + side + "_" + i))))
            .ToArray();

        private void PreShiftBigMalletPart(Transform part)
        {
            string name = part.name;
            if (name.EndsWith("_1", StringComparison.Ordinal) && (name.Contains("caliper") || name.Contains("suspension")))
                part.localPosition += Vector3.forward * bigMallet.FirstDriverShift;
        }

        // Native four-axle engine as on the stock S282: drivers 1-4, the native
        // coupling rod, drivers 5-6 and the longer-rod variants hidden.
        private void ConfigureEightCoupledDrive(Transform drive, bool rearClone)
        {
            var wheels = Enumerable.Range(1, 6).Select(i => drive.Find("s282_wheels_driving_" + i)).ToArray();
            if (wheels.Any(w => w == null)) throw new InvalidOperationException("Eight-coupled S282 driver is missing.");
            for (int i = 0; i < 6; i++) wheels[i].gameObject.SetActive(i < 4);
            wheels[0].GetComponent<MeshFilter>().sharedMesh = flangedDriver;
            wheels[1].GetComponent<MeshFilter>().sharedMesh = blindDriver;
            wheels[2].GetComponent<MeshFilter>().sharedMesh = blindDriverBigWeight;
            wheels[3].GetComponent<MeshFilter>().sharedMesh = flangedDriver;
            var rod = RequireArticulatedPart(drive, "s282_mech_wheels_connect");
            rod.GetComponent<MeshRenderer>().enabled = true;
            foreach (Transform child in rod)
            {
                var filter = child.GetComponent<MeshFilter>();
                if ((filter != null && filter.sharedMesh == MeshFinder.Instance.ThreeAxleSideRodMesh) ||
                    child.name.StartsWith("s282_mech_wheels_connect", StringComparison.Ordinal) || child.name.StartsWith("[Rearranged", StringComparison.Ordinal))
                    child.gameObject.SetActive(false);
            }
            SetPartActive(drive, "s282_mech_cutoff_rod", false);
            SetPartActive(drive, "s282_mech_cutoff_rail/s282_mech_lubricator_rod", false);
            SetPartActive(drive, "LubricatorParts", false);
            var shoes = drive.Find("s282a_brake_shoes(Clone)");
            if (shoes != null)
                foreach (Transform shoe in shoes)
                    shoe.gameObject.SetActive(shoe.name == "s282a_brake_shoe_1" || shoe.name == "s282a_brake_shoe_2" ||
                        shoe.name == "s282a_brake_shoe_3" || shoe.name == "s282a_brake_shoe_4");
            if (rearClone)
                foreach (var child in drive.GetComponentsInChildren<Transform>(true))
                    if (child.name.EndsWith("LOD1", StringComparison.Ordinal)) child.gameObject.SetActive(false);
        }

        // Lazily clones the four-axle rear engine and the extra powered axles from
        // the prepared (stock) pose; the rear static parts are mapped once per
        // layout, as their positions depend on it.
        private void EnsureBigMalletAssets(BigMalletLayout layout)
        {
            var created = new List<Transform>();
            try
            {
                if (bigMalletRearL == null)
                {
                    bigMalletRearL = CloneEngineDrive(driveL, lfAnimator, "[Rearranged eight-coupled rear drivetrain L]", articulatedRearRoot, d => ConfigureEightCoupledDrive(d, true));
                    created.Add(bigMalletRearL);
                    bigMalletRearR = CloneEngineDrive(driveR, rfAnimator, "[Rearranged eight-coupled rear drivetrain R]", articulatedRearRoot, d => ConfigureEightCoupledDrive(d, true));
                    created.Add(bigMalletRearR);
                }
                if (bigMalletFrontAxle == null)
                {
                    bigMalletFrontAxle = Object.Instantiate(driveAxle2.gameObject, frontBogie).transform;
                    bigMalletFrontAxle.name = "[axle] drive mallet front"; bigMalletFrontAxle.gameObject.SetActive(false);
                    created.Add(bigMalletFrontAxle);
                    bigMalletRearAxle = Object.Instantiate(driveAxle4.gameObject, rearBogie).transform;
                    bigMalletRearAxle.name = "[axle] drive mallet rear"; bigMalletRearAxle.gameObject.SetActive(false);
                    created.Add(bigMalletRearAxle);
                    bigMalletAxles.Add(bigMalletFrontAxle); bigMalletAxles.Add(bigMalletRearAxle);
                }
                if (!bigMalletStatics.TryGetValue(layout.Kind, out var assets)) bigMalletStatics[layout.Kind] = assets = new BigMalletAssets();
                if (assets.Scale != layout.Scale)
                {
                    foreach (var old in assets.Statics.Where(t => t != null)) { old.gameObject.SetActive(false); Object.Destroy(old.gameObject); }
                    assets.Statics.Clear(); assets.Scale = float.NaN;
                    foreach (var source in EightCoupledStaticParts())
                    {
                        var clone = Object.Instantiate(source.gameObject, source.parent).transform;
                        created.Add(clone);
                        clone.name = "[Rearranged eight-coupled rear] " + source.name;
                        clone.gameObject.SetActive(true);
                        MapMalletPart(clone, layout.Rear);
                        clone.SetParent(articulatedRearRoot, true);
                        clone.gameObject.SetActive(false);
                        assets.Statics.Add(clone);
                    }
                    assets.Scale = layout.Scale;
                }
            }
            catch
            {
                foreach (var t in created.Where(t => t != null)) { t.gameObject.SetActive(false); Object.Destroy(t.gameObject); }
                if (created.Contains(bigMalletRearL)) bigMalletRearL = bigMalletRearR = null;
                if (created.Contains(bigMalletFrontAxle)) { bigMalletFrontAxle = bigMalletRearAxle = null; bigMalletAxles.Clear(); }
                foreach (var a in bigMalletStatics.Values) a.Statics.RemoveAll(t => created.Contains(t));
                if (bigMalletStatics.TryGetValue(layout.Kind, out var failed)) failed.Scale = float.NaN;
                throw;
            }
        }

        private IEnumerable<Transform> BigMalletPresentationRoots() =>
            new[] { bigMalletRearL, bigMalletRearR }.Concat(bigMalletStatics.Values.SelectMany(a => a.Statics)).Where(t => t != null);

        private void SuspendBigMalletGeometry()
        {
            foreach (var asset in bigMalletStatics.Values.SelectMany(a => a.Statics)) if (asset != null) asset.gameObject.SetActive(false);
            if (bigMalletRearL != null) bigMalletRearL.gameObject.SetActive(false);
            if (bigMalletRearR != null) bigMalletRearR.gameObject.SetActive(false);
            // Extra axles must never stay active: bogie axle lists are rebuilt from active [axle] objects.
            foreach (var axle in bigMalletAxles) if (axle != null) axle.gameObject.SetActive(false);
        }
    }
}
#endif
