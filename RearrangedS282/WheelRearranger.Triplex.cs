#if B99_ALL_STANDARD
using System;
using System.Collections.Generic;
using System.Linq;
using DV.Wheels;
using UnityEngine;
using UnityEngine.Animations;
using Object = UnityEngine.Object;

namespace RearrangedS282
{
    // 2-2-2-2-2 triplex: three single-driver engines. Each is a uniformly scaled
    // copy of the 2-4-4-2's engine (Franklin poppet gear, its cylinder carried by
    // the drivetrain, and the duplex short main rod driving the second-driver
    // position) showing only that driver, which keeps the engines short. The
    // front engine swings on the existing pin/aim constraints, its hinge carried
    // by the middle engine; the middle engine pivots ahead of the rear cylinders
    // and turns a fraction of the front bogie's angle (visual only); the rear
    // engine is boiler-fixed. One-axle trailing truck at the 12-coupled position.
    internal partial class WheelRearranger
    {
        // Front cylinders may sit over the leading truck, as on the stock S282.
        private const float TriplexEngineGap = .08f, TriplexMiddleSwing = .4f;
        private const string FranklinDriveState = "LocoS282A_AnimationDriveGear";
        // ShowTwoTrailingWheelsFor12Coupled's trailing axle: the rearmost proven
        // position. The triplex's trailing axle sits there, behind its last driver.
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
        // the trailing wheel fit between the native Franklin cylinder front and the
        // 12-coupled trailing-axle position. Everything in the body frame.
        private TriplexLayout MeasureTriplex()
        {
            var body = MalletBody;
            float cylinderFront = FranklinCylinderFront();
            float driver = secondLeftDriveWheel.localPosition.z, radius = secondLeftDriveWheel.localPosition.y;
            float engineLength = cylinderFront - driver;
            float trailingLimit = body.InverseTransformPoint(rearBogie.TransformPoint(TriplexTrailingLimit)).z;
            float front = cylinderFront;
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

        // Front face of the duplex Franklin cylinder in the native drive frame.
        private float FranklinCylinderFront()
        {
            var cylinder = frontFranklin[0].Find("cylinder");
            if (cylinder == null) throw new InvalidOperationException("Franklin cylinder asset is missing.");
            float front = float.NegativeInfinity;
            foreach (var filter in cylinder.GetComponentsInChildren<MeshFilter>(true))
            {
                var b = filter.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                    front = Mathf.Max(front, driveL.InverseTransformPoint(filter.transform.TransformPoint(corner)).z);
                }
            }
            if (float.IsInfinity(front)) throw new InvalidOperationException("Franklin cylinder has no mesh.");
            return front;
        }

        private void ApplyTriplexGeometry()
        {
            // The Franklin engine assets, duplex guide meshes and short main rod
            // are the 2-4-4-2's; the drives are cloned after they exist.
            EnsureArticulatedAssets();
            triplex = MeasureTriplex();
            EnsureTriplexAssets();
            float s = triplex.Scale;
            float phase = lfAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f;
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
            SetPairActive(frontFranklin, true); SetPairActive(frontFranklin4444, false);
            foreach (var part in frontFranklin) EnableGeneratedRenderers(part);
            ConfigureSingleDriverDrive(driveL, false); ConfigureSingleDriverDrive(driveR, false);
            lfAnimator.runtimeAnimatorController = rfAnimator.runtimeAnimatorController = AssetBundleLoader.FranklinBAnimCtrl;
            leftReachRod.gameObject.SetActive(false); rightReachRod.gameObject.SetActive(false);
            // The Franklin cylinders replace the native ones and Walschaerts supports.
            foreach (var part in new[] { lfCylinder, rfCylinder, cylinderCocks, lDryPipe, rDryPipe, reachRodSupport,
                lubricatorSupport, fOilLines, lLiftingArmSupport, rLiftingArmSupport,
                leftFirstBrakeCaliper, rightFirstBrakeCaliper, leftThirdBrakeCaliper, rightThirdBrakeCaliper,
                leftFourthBrakeCaliper, rightFourthBrakeCaliper, leftFifthBrakeCaliper, rightFifthBrakeCaliper,
                leftSixthBrakeCaliper, rightSixthBrakeCaliper })
                part.gameObject.SetActive(false);
            foreach (string side in new[] { "l", "r" })
                foreach (int i in new[] { 1, 3, 4 })
                    RequireArticulatedPart(splitLoco, "s282a_suspension_" + side + "_" + i).gameObject.SetActive(false);
            PrepareTriplexGuide(lfCrossheadBracket, articulatedGuideL); PrepareTriplexGuide(rfCrossheadBracket, articulatedGuideR);

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
            BindFranklinTriplexAnimation(phase);
            BindEngineBrakeHeat(new[] { triplexMiddleL, triplexMiddleR, triplexRearL, triplexRearR }, 2);

            // One powered axle per engine: front and middle in the native front
            // bogie, rear in the native rear bogie.
            driveAxle4.gameObject.SetActive(false);
            AlignNativeAxle(driveAxle1, secondLeftDriveWheel, articulatedFrontRoot);
            AlignNativeAxle(driveAxle2, RequireArticulatedPart(triplexMiddleL, "s282_wheels_driving_2"), articulatedMiddleRoot);
            AlignNativeAxle(driveAxle3, RequireArticulatedPart(triplexRearL, "s282_wheels_driving_2"), articulatedRearRoot);

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

        // LMS static pieces belonging to one single-driver engine: the duplex
        // crosshead guides and the second driver's brake and spring.
        private Transform[] TriplexStaticParts() => new[] { lfCrossheadBracket, rfCrossheadBracket,
            leftSecondBrakeCaliper, rightSecondBrakeCaliper,
            RequireArticulatedPart(splitLoco, "s282a_suspension_l_2"), RequireArticulatedPart(splitLoco, "s282a_suspension_r_2") };

        // The 2-4-4-2's front guide: lengthened mesh, as placed for that engine.
        private static void PrepareTriplexGuide(Transform guide, Mesh mesh)
        {
            guide.GetComponent<MeshFilter>().sharedMesh = mesh;
            guide.localPosition = new Vector3(0f, 0f, .065f);
            guide.localScale = new Vector3(-1f, 1f, 1f);
        }

        // Every engine runs the duplex Franklin clip with the native start offsets.
        // That controller has no native cutoff parameters, so the front readers
        // are paused while it is bound, exactly as on the 2-4-4-2.
        private void BindFranklinTriplexAnimation(float phase)
        {
            articulatedRotation = loco.GetComponentsInChildren<PoweredWheelRotationViaAnimation>(true)
                .FirstOrDefault(r => r.animatorSetups != null && r.animatorSetups.Any(x => x.animator == lfAnimator));
            if (articulatedRotation == null) throw new InvalidOperationException("Native S282 wheel animation controller was not found.");
            articulatedOriginalAnimators = articulatedRotation.animatorSetups;
            float Offset(Animator a) => articulatedOriginalAnimators.First(x => x.animator == a).startTimeOffset;
            var clones = new[] { triplexMiddleL, triplexMiddleR, triplexRearL, triplexRearR };
            articulatedRotation.animatorSetups = articulatedOriginalAnimators.Concat(clones.Select((clone, i) =>
                new PoweredWheelRotationViaAnimation.AnimatorStartTimeOffsetPair
                { animator = clone.GetComponent<Animator>(), startTimeOffset = Offset(i % 2 == 0 ? lfAnimator : rfAnimator) })).ToArray();
            foreach (var setup in articulatedRotation.animatorSetups)
                if (setup.animator.runtimeAnimatorController == AssetBundleLoader.FranklinBAnimCtrl)
                    setup.animator.Play(FranklinDriveState, 0, (phase + setup.startTimeOffset) % 1f);
            foreach (var drive in new[] { driveL, driveR })
                foreach (var reader in drive.GetComponents<MonoBehaviour>().Where(b => b.GetType().Name == "AnimatorPortReader"))
                { articulatedReaders.Add(new EnabledReaderState { reader = reader, enabled = reader.enabled }); reader.enabled = false; }
        }

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
                        if (source == lfCrossheadBracket) PrepareTriplexGuide(clone, articulatedGuideL);
                        if (source == rfCrossheadBracket) PrepareTriplexGuide(clone, articulatedGuideR);
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

        // One single-driver engine in the 2-4-4-2's Franklin form: only driver 2,
        // which the duplex short main rod drives. No coupling rods.
        private void ConfigureSingleDriverDrive(Transform drive, bool clone)
        {
            var wheels = Enumerable.Range(1, 6).Select(i => drive.Find("s282_wheels_driving_" + i)).ToArray();
            if (wheels.Any(w => w == null)) throw new InvalidOperationException("Triplex S282 driver is missing.");
            for (int i = 0; i < 6; i++) wheels[i].gameObject.SetActive(i == 1);
            wheels[1].GetComponent<MeshFilter>().sharedMesh = flangedDriver;
            foreach (string name in new[] { "s282_mech_cutoff_boomerang_high", "s282_mech_cutoff_rail", "s282_mech_cutoff_rod",
                "s282_mech_cutoff_rod_hinge", "s282_mech_cylinder_rod", "LubricatorParts" })
                SetPartActive(drive, name, false);
            SetPartActive(drive, "s282_mech_push_rod/s282_mech_push_rod_to_cutoff_base", false);
            var rod = RequireArticulatedPart(drive, "s282_mech_wheels_connect");
            rod.GetComponent<MeshRenderer>().enabled = false;
            foreach (Transform child in rod) child.gameObject.SetActive(false);
            var main = RequireArticulatedPart(rod, "s282_mech_push_rod_to_connect");
            main.gameObject.SetActive(true);
            main.GetComponent<MeshFilter>().sharedMesh = MeshFinder.Instance.DuplexMainRodMesh;
            var cutoff = RequireArticulatedPart(rod, "s282_mech_connect_to_cutoff_base");
            cutoff.gameObject.SetActive(true);
            cutoff.localPosition = new Vector3(.15f, 0f, 0f);
            SetPartActive(cutoff, "s282_mech_connect_to_cutoff_rod", false);
            var shoes = drive.Find("s282a_brake_shoes(Clone)");
            if (shoes != null) foreach (Transform shoe in shoes) shoe.gameObject.SetActive(shoe.name == "s282a_brake_shoe_2");
            foreach (Transform child in drive)
            {
                if (child.name == "franklin_type_b(Clone)") { child.gameObject.SetActive(true); EnableGeneratedRenderers(child); }
                else if (child.name == "franklin_type_b_4444(Clone)") child.gameObject.SetActive(false);
            }
            if (clone)
            {
                foreach (var child in drive.GetComponentsInChildren<Transform>(true))
                    if (child.name.EndsWith("LOD1", StringComparison.Ordinal)) child.gameObject.SetActive(false);
                drive.GetComponent<Animator>().runtimeAnimatorController = AssetBundleLoader.FranklinBAnimCtrl;
            }
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
