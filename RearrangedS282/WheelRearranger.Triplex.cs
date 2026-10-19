#if B99_ALL_STANDARD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using Object = UnityEngine.Object;

namespace RearrangedS282
{
    // 2-2-2-2-2 triplex: three single-driver engines, each a uniformly scaled copy
    // of the native drivetrain showing only its main-rod driver (no coupling
    // rods), plus its static cylinder assembly. The front engine swings on the
    // existing pin/aim constraints, its hinge carried by the middle engine; the
    // middle engine pivots ahead of the rear cylinders and turns a fraction of
    // the front bogie's angle (visual only); the rear engine is boiler-fixed.
    internal partial class WheelRearranger
    {
        // Front cylinders may sit over the leading truck, as on the stock S282.
        private const float TriplexFrontAdvance = .5f, TriplexEngineGap = .08f, TriplexMiddleSwing = .4f;
        // ShowTwoTrailingWheelsFor12Coupled's trailing axle: the rearmost proven position.
        private static readonly Vector3 TriplexTrailingLimit = new Vector3(0f, .36f, -3.1f);

        private Transform articulatedMiddleFrame, articulatedMiddleRoot, triplexMiddlePin, triplexAimRest;
        private PositionConstraint triplexMiddlePosition;
        private AimConstraint triplexMiddleAim;
        private Transform triplexMiddleL, triplexMiddleR, triplexRearL, triplexRearR;
        private readonly List<Transform> triplexAssets = new List<Transform>();
        private TriplexLayout triplex;

        private sealed class TriplexLayout
        {
            internal float Scale, Radius, FrontPinZ, MiddlePinZ, TrailingZ;
            internal readonly float[] Offsets = new float[3], CylinderFronts = new float[3], Drivers = new float[3];
            internal Matrix4x4 Front => EngineMap(Offsets[0], Scale);
            internal Matrix4x4 Middle => EngineMap(Offsets[1], Scale);
            internal Matrix4x4 Rear => EngineMap(Offsets[2], Scale);
        }

        // Largest uniform scale at which three engines, the gaps between them and
        // the trailing truck fit between the advanced front cylinders and the
        // rearmost trailing-axle position. Everything in the body frame.
        private TriplexLayout MeasureTriplex()
        {
            var body = MalletBody;
            float cylinderFront = MeasureNativeCylinderFront();
            float driver = thirdLeftDriveWheel.localPosition.z, radius = thirdLeftDriveWheel.localPosition.y;
            float engineLength = cylinderFront - driver;
            float trailingLimit = body.InverseTransformPoint(rearBogie.TransformPoint(TriplexTrailingLimit)).z;
            float front = cylinderFront + TriplexFrontAdvance;
            float s = (front - trailingLimit - 2f * TriplexEngineGap - MalletTrailingRadius - MalletEngineGap) / (3f * engineLength + 3f * radius);
            if (!(s > .3f && s < 1f)) throw new InvalidOperationException("Triplex S282 engines do not fit the frame (scale " + s + ").");
            var layout = new TriplexLayout { Scale = s, Radius = s * radius };
            float cylinder = front;
            for (int e = 0; e < 3; e++)
            {
                layout.CylinderFronts[e] = cylinder;
                layout.Offsets[e] = cylinder - s * cylinderFront;
                layout.Drivers[e] = layout.Offsets[e] + s * driver;
                cylinder = layout.Drivers[e] - layout.Radius - TriplexEngineGap;
            }
            layout.FrontPinZ = (layout.Drivers[0] + layout.CylinderFronts[1]) * .5f;
            layout.MiddlePinZ = (layout.Drivers[1] + layout.CylinderFronts[2]) * .5f;
            layout.TrailingZ = layout.Drivers[2] - layout.Radius - MalletTrailingRadius - MalletEngineGap;
            return layout;
        }

        private void ApplyTriplexGeometry()
        {
            triplex = MeasureTriplex();
            EnsureTriplexAssets();
            float s = triplex.Scale;
            ResetBody(); ResetToFourCoupled(); HideLeadingWheels(); HideTrailingWheels();
            ShowTwoLeadingWheels();
            ShowTwoTrailingWheelsFor12Coupled();
            float trailingLocal = rearBogie.InverseTransformPoint(MalletBody.TransformPoint(new Vector3(0f, 0f, triplex.TrailingZ))).z;
            float trailingShift = trailingLocal - secondRearAxle.localPosition.z;
            secondRearAxle.localPosition += Vector3.forward * trailingShift;
            secondRearAxleSupport.localPosition += Vector3.forward * trailingShift;
            setWheelRadius(.712f * s);

            driveL.localPosition = driveR.localPosition = new Vector3(0f, 0f, triplex.Offsets[0]);
            driveL.localScale = new Vector3(-1f, s, s); driveR.localScale = new Vector3(1f, s, s);
            ConfigureSingleDriverDrive(driveL, false); ConfigureSingleDriverDrive(driveR, false);
            leftReachRod.gameObject.SetActive(false); rightReachRod.gameObject.SetActive(false);
            foreach (var part in new[] { lDryPipe, rDryPipe, reachRodSupport, lubricatorSupport, fOilLines,
                leftFirstBrakeCaliper, rightFirstBrakeCaliper, leftSecondBrakeCaliper, rightSecondBrakeCaliper,
                leftFourthBrakeCaliper, rightFourthBrakeCaliper, leftFifthBrakeCaliper, rightFifthBrakeCaliper,
                leftSixthBrakeCaliper, rightSixthBrakeCaliper })
                part.gameObject.SetActive(false);
            foreach (string side in new[] { "l", "r" })
                foreach (int i in new[] { 1, 2, 4 })
                    RequireArticulatedPart(splitLoco, "s282a_suspension_" + side + "_" + i).gameObject.SetActive(false);

            var frontStatic = TriplexStaticParts();
            foreach (var part in frontStatic) MapMalletPart(part, triplex.Front);
            var colliders = MalletColliders.Select(path => transform.Find(path)).Where(t => t != null).ToArray();
            foreach (var collider in colliders) MapMalletPart(collider, triplex.Front);

            foreach (var asset in triplexAssets) if (asset != null) asset.gameObject.SetActive(true);
            foreach (var (drive, offset, mirrored) in new[] { (triplexMiddleL, 1, true), (triplexMiddleR, 1, false),
                (triplexRearL, 2, true), (triplexRearR, 2, false) })
            {
                drive.localPosition = new Vector3(0f, 0f, triplex.Offsets[offset]);
                drive.localScale = new Vector3(mirrored ? -1f : 1f, s, s);
                drive.gameObject.SetActive(true);
            }
            scaledEngineMaps = new[] { triplex.Front, triplex.Rear, triplex.Middle };
            BindScaledEngineAnimation(triplexMiddleL, triplexMiddleR, triplexRearL, triplexRearR);
            BindEngineBrakeHeat(new[] { triplexMiddleL, triplexMiddleR, triplexRearL, triplexRearR }, 3);

            // One powered axle per engine: front and middle in the native front
            // bogie, rear in the native rear bogie.
            driveAxle4.gameObject.SetActive(false);
            AlignNativeAxle(driveAxle1, thirdLeftDriveWheel, articulatedFrontRoot);
            AlignNativeAxle(driveAxle2, RequireArticulatedPart(triplexMiddleL, "s282_wheels_driving_3"), articulatedMiddleRoot);
            AlignNativeAxle(driveAxle3, RequireArticulatedPart(triplexRearL, "s282_wheels_driving_3"), articulatedRearRoot);

            foreach (var part in new[] { driveL, driveR }.Concat(frontStatic).Concat(colliders))
                MoveArticulatedPart(part, articulatedFrontRoot);
            UpdateTriplexSparks();
            ConfigureTriplexMiddleArticulation();
            ConfigureNativeArticulation(ArticulatedSimulation.TriplexId, triplex.FrontPinZ);
            // The front engine's hinge rides on the middle engine.
            MoveArticulatedPart(articulatedPin, articulatedMiddleRoot);
            BindArticulatedLods();
            Main.Logger.Log($"Articulated S282 2-2-2-2-2: scale {s:0.000}, {2f * triplex.Radius:0.00} m drivers at " +
                $"{triplex.Drivers[0]:0.00}/{triplex.Drivers[1]:0.00}/{triplex.Drivers[2]:0.00}, pins {triplex.FrontPinZ:0.00}/{triplex.MiddlePinZ:0.00}, " +
                $"trailing axle {triplex.TrailingZ:0.00} (body z).");
        }

        // LMS static pieces belonging to one single-driver engine.
        private Transform[] TriplexStaticParts() => new[] { lfCylinder, rfCylinder, cylinderCocks,
            lfCrossheadBracket, rfCrossheadBracket, lLiftingArmSupport, rLiftingArmSupport,
            leftThirdBrakeCaliper, rightThirdBrakeCaliper,
            RequireArticulatedPart(splitLoco, "s282a_suspension_l_3"), RequireArticulatedPart(splitLoco, "s282a_suspension_r_3") };

        // Lazily builds the middle articulation and clones the middle and rear
        // engines from the prepared (stock) pose, before any front-engine edits.
        private void EnsureTriplexAssets()
        {
            var body = MalletBody;
            if (articulatedMiddleFrame == null)
            {
                articulatedMiddleFrame = NewArticulatedRoot("[Rearranged articulated middle frame]", body);
                articulatedMiddleRoot = NewArticulatedRoot("[Rearranged middle engine]", articulatedMiddleFrame);
                triplexMiddlePin = NewArticulatedRoot("[Rearranged middle articulation pin]", body);
                triplexAimRest = NewArticulatedRoot("[Rearranged middle aim rest]", body);
                triplexMiddlePosition = articulatedMiddleFrame.gameObject.AddComponent<PositionConstraint>();
                triplexMiddleAim = articulatedMiddleFrame.gameObject.AddComponent<AimConstraint>();
                triplexMiddlePosition.constraintActive = false; triplexMiddleAim.constraintActive = false;
                foreach (var t in new[] { articulatedMiddleFrame, triplexMiddlePin, triplexAimRest }) t.gameObject.SetActive(false);
            }
            if (triplexRearR != null) return;
            var created = new List<Transform>();
            try
            {
                triplexMiddleL = CloneEngineDrive(driveL, lfAnimator, "[Rearranged triplex middle drivetrain L]", articulatedMiddleRoot, d => ConfigureSingleDriverDrive(d, true));
                created.Add(triplexMiddleL);
                triplexMiddleR = CloneEngineDrive(driveR, rfAnimator, "[Rearranged triplex middle drivetrain R]", articulatedMiddleRoot, d => ConfigureSingleDriverDrive(d, true));
                created.Add(triplexMiddleR);
                triplexRearL = CloneEngineDrive(driveL, lfAnimator, "[Rearranged triplex rear drivetrain L]", articulatedRearRoot, d => ConfigureSingleDriverDrive(d, true));
                created.Add(triplexRearL);
                triplexRearR = CloneEngineDrive(driveR, rfAnimator, "[Rearranged triplex rear drivetrain R]", articulatedRearRoot, d => ConfigureSingleDriverDrive(d, true));
                created.Add(triplexRearR);
                foreach (var source in TriplexStaticParts())
                    foreach (var (label, root, map) in new[] { ("middle", articulatedMiddleRoot, triplex.Middle), ("rear", articulatedRearRoot, triplex.Rear) })
                    {
                        var clone = Object.Instantiate(source.gameObject, source.parent).transform;
                        created.Add(clone);
                        clone.name = "[Rearranged triplex " + label + "] " + source.name;
                        MapMalletPart(clone, map);
                        clone.SetParent(root, true);
                        clone.gameObject.SetActive(false);
                        triplexAssets.Add(clone);
                    }
            }
            catch
            {
                foreach (var t in created.Where(t => t != null)) { t.gameObject.SetActive(false); Object.Destroy(t.gameObject); }
                triplexAssets.Clear(); triplexMiddleL = triplexMiddleR = triplexRearL = triplexRearR = null;
                throw;
            }
        }

        // One single-driver engine: only driver 3 (the native main-rod crank) with
        // its main rod, crosshead and valve gear. No coupling rods.
        private void ConfigureSingleDriverDrive(Transform drive, bool clone)
        {
            var wheels = Enumerable.Range(1, 6).Select(i => drive.Find("s282_wheels_driving_" + i)).ToArray();
            if (wheels.Any(w => w == null)) throw new InvalidOperationException("Triplex S282 driver is missing.");
            for (int i = 0; i < 6; i++) wheels[i].gameObject.SetActive(i == 2);
            wheels[2].GetComponent<MeshFilter>().sharedMesh = flangedDriver;
            var rod = RequireArticulatedPart(drive, "s282_mech_wheels_connect");
            rod.GetComponent<MeshRenderer>().enabled = false;
            // Keeps the main rod and return crank; hides every coupling-rod copy.
            foreach (Transform child in rod)
                if (child.name.StartsWith("s282_mech_wheels_connect", StringComparison.Ordinal)) child.gameObject.SetActive(false);
            SetPartActive(drive, "s282_mech_cutoff_rod", false);
            SetPartActive(drive, "s282_mech_cutoff_rail/s282_mech_lubricator_rod", false);
            SetPartActive(drive, "LubricatorParts", false);
            var shoes = drive.Find("s282a_brake_shoes(Clone)");
            if (shoes != null) foreach (Transform shoe in shoes) shoe.gameObject.SetActive(shoe.name == "s282a_brake_shoe_3");
            if (clone)
                foreach (var child in drive.GetComponentsInChildren<Transform>(true))
                    if (child.name.EndsWith("LOD1", StringComparison.Ordinal)) child.gameObject.SetActive(false);
        }

        private void UpdateTriplexSparks()
        {
            var axles = new[] { driveAxle1, driveAxle2, driveAxle3 };
            var roots = new[] { articulatedFrontRoot, articulatedMiddleRoot, articulatedRearRoot };
            var left = new[] { spark1L, spark2L, spark3L };
            var right = new[] { spark1R, spark2R, spark3R };
            for (int i = 0; i < 3; i++)
            {
                var point = transform.InverseTransformPoint(axles[i].position); point.y = 0f;
                left[i].position = transform.TransformPoint(new Vector3(-.75f, 0f, point.z));
                right[i].position = transform.TransformPoint(new Vector3(.75f, 0f, point.z));
                left[i].gameObject.SetActive(true); right[i].gameObject.SetActive(true);
                MoveArticulatedPart(left[i], roots[i]);
                MoveArticulatedPart(right[i], roots[i]);
            }
            foreach (var spark in new[] { spark4L, spark4R, spark5L, spark5R, spark6L, spark6R }) spark.gameObject.SetActive(false);
        }

        // The middle engine pivots on a boiler-fixed pin ahead of the rear
        // cylinders and aims at a blend of the front bogie and its rest point.
        private void ConfigureTriplexMiddleArticulation()
        {
            var support = transform.Find("Axle_F");
            if (support == null) throw new InvalidOperationException("Native front articulation support was not found.");
            var body = MalletBody;
            Vector3 supportLocal = body.InverseTransformPoint(support.position);
            var pin = new Vector3(supportLocal.x, supportLocal.y, triplex.MiddlePinZ);
            triplexMiddlePin.localPosition = pin;
            triplexAimRest.localPosition = supportLocal; triplexAimRest.localRotation = Quaternion.identity;
            articulatedMiddleFrame.localPosition = pin; articulatedMiddleFrame.localRotation = Quaternion.identity;
            articulatedMiddleRoot.localPosition = -pin; articulatedMiddleRoot.localRotation = Quaternion.identity;
            triplexMiddlePosition.translationAtRest = pin;
            triplexMiddlePosition.translationOffset = Vector3.zero;
            triplexMiddlePosition.translationAxis = Axis.X | Axis.Y | Axis.Z;
            while (triplexMiddlePosition.sourceCount > 0) triplexMiddlePosition.RemoveSource(0);
            triplexMiddlePosition.AddSource(new ConstraintSource { sourceTransform = triplexMiddlePin, weight = 1f });
            triplexMiddlePosition.weight = 1f; triplexMiddlePosition.locked = true;
            triplexMiddleAim.aimVector = articulatedMiddleFrame.InverseTransformDirection(transform.forward).normalized;
            triplexMiddleAim.upVector = articulatedMiddleFrame.InverseTransformDirection(transform.up).normalized;
            triplexMiddleAim.worldUpType = AimConstraint.WorldUpType.ObjectRotationUp;
            triplexMiddleAim.worldUpObject = transform; triplexMiddleAim.worldUpVector = Vector3.up;
            triplexMiddleAim.rotationAtRest = Vector3.zero; triplexMiddleAim.rotationOffset = Vector3.zero;
            triplexMiddleAim.rotationAxis = Axis.X | Axis.Y | Axis.Z;
            while (triplexMiddleAim.sourceCount > 0) triplexMiddleAim.RemoveSource(0);
            triplexMiddleAim.AddSource(new ConstraintSource { sourceTransform = support, weight = TriplexMiddleSwing });
            triplexMiddleAim.AddSource(new ConstraintSource { sourceTransform = triplexAimRest, weight = 1f - TriplexMiddleSwing });
            triplexMiddleAim.weight = 1f; triplexMiddleAim.locked = true;
            foreach (var t in new[] { triplexMiddlePin, triplexAimRest, articulatedMiddleFrame }) t.gameObject.SetActive(true);
            triplexMiddlePosition.constraintActive = true; triplexMiddleAim.constraintActive = true;
        }

        private void SuspendTriplexGeometry()
        {
            if (triplexMiddlePosition != null) triplexMiddlePosition.constraintActive = false;
            if (triplexMiddleAim != null) triplexMiddleAim.constraintActive = false;
            foreach (var asset in triplexAssets) if (asset != null) asset.gameObject.SetActive(false);
            foreach (var drive in new[] { triplexMiddleL, triplexMiddleR, triplexRearL, triplexRearR })
                if (drive != null) drive.gameObject.SetActive(false);
            if (articulatedMiddleFrame != null)
            {
                articulatedMiddleFrame.gameObject.SetActive(false);
                articulatedMiddleFrame.localPosition = Vector3.zero; articulatedMiddleFrame.localRotation = Quaternion.identity;
            }
            if (articulatedMiddleRoot != null) { articulatedMiddleRoot.localPosition = Vector3.zero; articulatedMiddleRoot.localRotation = Quaternion.identity; }
            if (triplexMiddlePin != null) triplexMiddlePin.gameObject.SetActive(false);
            if (triplexAimRest != null) triplexAimRest.gameObject.SetActive(false);
        }
    }
}
#endif
