#if B99_ALL_STANDARD
using System;
using System.Collections.Generic;
using System.Linq;
using DV.Simulation.Brake;
using DV.Wheels;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RearrangedS060
{
    // 2-6-6-2 Mallet: two three-axle engines, each a uniformly scaled copy of the
    // native S282 drivetrain AND its static cylinder assembly, so piston, guides,
    // valve gear and wheels stay mutually consistent. Articulation, simulation and
    // effects reuse the 2-4-4-2 pin/aim constraints and the native rear engine.
    internal partial class WheelRearranger
    {
        // 0.75 x 1.42 m = 1.07 m drivers: the largest scale at which leading truck,
        // both engines (rear cylinders between them) and the trailing truck fit the
        // S282 frame. Front cylinders keep their native front face.
        internal const float MalletScale = .75f;
        private const float MalletEngineGap = .1f, MalletTrailingRadius = .355f;
        private static readonly string[] MalletEngineSuspension = { "s282a_suspension_l_1", "s282a_suspension_l_2",
            "s282a_suspension_l_3", "s282a_suspension_r_1", "s282a_suspension_r_2", "s282a_suspension_r_3" };
        private static readonly string[] MalletColliders = { "[colliders]/[walkable]/[old]/Exterior/cylinder",
            "[colliders]/[walkable]/[old]/Exterior/cylinder (1)", "[colliders]/[walkable]/[old]/Exterior/area_big_cylinder",
            "[colliders]/[items]/[old]/Exterior/area_big_cylinder" };

        private readonly List<Transform> malletAssets = new List<Transform>();
        private Transform malletRearL, malletRearR;
        private MalletLayout mallet;
        // Body-frame map of each scaled engine while the Mallet or triplex is
        // active: 0 front, 1 rear, 2 middle. Null for every other arrangement.
        private Matrix4x4[] scaledEngineMaps;

        private sealed class MalletLayout
        {
            internal float FrontOffset, RearOffset, FrontLastDriver, RearCylinderFront, RearLastDriver, Radius, PinZ, TrailingZ, FirstDriverShift;
            internal Matrix4x4 Front => EngineMap(FrontOffset, MalletScale);
            internal Matrix4x4 Rear => EngineMap(RearOffset, MalletScale);
        }

        // Body-frame mapping of one engine: native drive coordinates scaled in
        // Y/Z about the rail-level origin, then moved along the track axis.
        private static Matrix4x4 EngineMap(float offset, float scale) =>
            Matrix4x4.TRS(new Vector3(0f, 0f, offset), Quaternion.identity, new Vector3(1f, scale, scale));

        // Front face of the native cylinders in the body frame.
        private float MeasureNativeCylinderFront()
        {
            var body = MalletBody;
            var bounds = lfCylinder.GetComponent<MeshFilter>().sharedMesh.bounds;
            return Mathf.Max(
                body.InverseTransformPoint(lfCylinder.TransformPoint(new Vector3(bounds.center.x, bounds.center.y, bounds.max.z))).z,
                body.InverseTransformPoint(lfCylinder.TransformPoint(new Vector3(bounds.center.x, bounds.center.y, bounds.min.z))).z);
        }

        private Transform MalletBody => articulatedFrontFrame.parent;

        // All values are in the drivetrain parent's (body) frame, measured from the
        // prepared native geometry rather than hard-coded LMS coordinates.
        private MalletLayout MeasureMallet()
        {
            float cylinderFront = MeasureNativeCylinderFront();
            float d2 = secondLeftDriveWheel.localPosition.z, d3 = thirdLeftDriveWheel.localPosition.z;
            float s = MalletScale;
            var layout = new MalletLayout { Radius = s * secondLeftDriveWheel.localPosition.y,
                FirstDriverShift = d2 + (d2 - d3) - firstLeftDriveWheel.localPosition.z };
            layout.FrontOffset = cylinderFront * (1f - s);
            layout.FrontLastDriver = layout.FrontOffset + s * d3;
            layout.RearCylinderFront = layout.FrontLastDriver - layout.Radius - MalletEngineGap;
            layout.RearOffset = layout.RearCylinderFront - s * cylinderFront;
            layout.RearLastDriver = layout.RearOffset + s * d3;
            // Hinge just ahead of the rear cylinders, as on a real Mallet.
            layout.PinZ = (layout.FrontLastDriver + layout.RearCylinderFront) * .5f;
            layout.TrailingZ = layout.RearLastDriver - layout.Radius - MalletTrailingRadius - MalletEngineGap;
            if (d2 <= d3) throw new InvalidOperationException("Native S282 driver order is unexpected.");
            return layout;
        }

        private void ApplyMalletGeometry()
        {
            mallet = MeasureMallet();
            EnsureMalletAssets();
            float s = MalletScale;
            ResetBody(); ResetToFourCoupled(); HideLeadingWheels(); HideTrailingWheels();
            ShowTwoLeadingWheels();
            ShowTwoTrailingWheelsFor12Coupled();
            float trailingLocal = rearBogie.InverseTransformPoint(MalletBody.TransformPoint(new Vector3(0f, 0f, mallet.TrailingZ))).z;
            float trailingShift = trailingLocal - secondRearAxle.localPosition.z;
            secondRearAxle.localPosition += Vector3.forward * trailingShift;
            secondRearAxleSupport.localPosition += Vector3.forward * trailingShift;
            setWheelRadius(.712f * s);

            driveL.localPosition = driveR.localPosition = new Vector3(0f, 0f, mallet.FrontOffset);
            driveL.localScale = new Vector3(-1f, s, s); driveR.localScale = new Vector3(1f, s, s);
            ConfigureMalletDrive(driveL, false); ConfigureMalletDrive(driveR, false);
            leftReachRod.gameObject.SetActive(false); rightReachRod.gameObject.SetActive(false);
            foreach (var part in new[] { lDryPipe, rDryPipe, reachRodSupport, lubricatorSupport, fOilLines })
                part.gameObject.SetActive(false);
            foreach (var part in new[] { leftFourthBrakeCaliper, rightFourthBrakeCaliper, leftFifthBrakeCaliper,
                rightFifthBrakeCaliper, leftSixthBrakeCaliper, rightSixthBrakeCaliper })
                part.gameObject.SetActive(false);
            RequireArticulatedPart(splitLoco, "s282a_suspension_l_4").gameObject.SetActive(false);
            RequireArticulatedPart(splitLoco, "s282a_suspension_r_4").gameObject.SetActive(false);

            var frontStatic = MalletStaticParts();
            foreach (var part in frontStatic) { PreShiftMalletPart(part); MapMalletPart(part, mallet.Front); }
            var colliders = MalletColliders.Select(path => transform.Find(path)).Where(t => t != null).ToArray();
            foreach (var collider in colliders) MapMalletPart(collider, mallet.Front);

            foreach (var asset in malletAssets) if (asset != null) asset.gameObject.SetActive(true);
            malletRearL.localPosition = malletRearR.localPosition = new Vector3(0f, 0f, mallet.RearOffset);
            malletRearL.localScale = new Vector3(-1f, s, s); malletRearR.localScale = new Vector3(1f, s, s);
            malletRearL.gameObject.SetActive(true); malletRearR.gameObject.SetActive(true);
            scaledEngineMaps = new[] { mallet.Front, mallet.Rear };
            BindMalletAnimation();
            BindMalletBrakeHeat();

            driveAxle5.gameObject.SetActive(true); driveAxle6.gameObject.SetActive(true);
            // Front engine axles stay in the native front bogie, rear in the rear bogie.
            AlignNativeAxle(driveAxle1, firstLeftDriveWheel, true);
            AlignNativeAxle(driveAxle2, secondLeftDriveWheel, true);
            AlignNativeAxle(driveAxle6, thirdLeftDriveWheel, true);
            AlignNativeAxle(driveAxle3, RequireArticulatedPart(malletRearL, "s282_wheels_driving_1"), false);
            AlignNativeAxle(driveAxle4, RequireArticulatedPart(malletRearL, "s282_wheels_driving_2"), false);
            AlignNativeAxle(driveAxle5, RequireArticulatedPart(malletRearL, "s282_wheels_driving_3"), false);

            foreach (var part in new[] { driveL, driveR }.Concat(frontStatic).Concat(colliders))
                MoveArticulatedPart(part, articulatedFrontRoot);
            UpdateMalletSparks();
            ConfigureNativeArticulation((int)WheelArrangementType.s2662, mallet.PinZ);
            BindArticulatedLods();
            Main.Logger.Log($"Articulated S282 2-6-6-2: {2f * mallet.Radius:0.00} m drivers; engines at {mallet.FrontOffset:0.00}/{mallet.RearOffset:0.00}, " +
                $"pin {mallet.PinZ:0.00}, rear cylinders {mallet.RearCylinderFront:0.00}, last driver {mallet.RearLastDriver:0.00}, trailing axle {mallet.TrailingZ:0.00} (body z).");
        }

        // LMS static pieces belonging to one engine. Order matches the rear clones.
        private Transform[] MalletStaticParts() => new[] { lfCylinder, rfCylinder, cylinderCocks,
            lfCrossheadBracket, rfCrossheadBracket, lLiftingArmSupport, rLiftingArmSupport,
            leftFirstBrakeCaliper, leftSecondBrakeCaliper, leftThirdBrakeCaliper,
            rightFirstBrakeCaliper, rightSecondBrakeCaliper, rightThirdBrakeCaliper }
            .Concat(MalletEngineSuspension.Select(name => RequireArticulatedPart(splitLoco, name))).ToArray();

        // The first driver moves back to equal pitch (see ConfigureMalletDrive);
        // its caliper and spring follow it before the engine mapping.
        private void PreShiftMalletPart(Transform part)
        {
            string name = part.name;
            if (name.EndsWith("_1", StringComparison.Ordinal) && (name.Contains("caliper") || name.Contains("suspension")))
                part.localPosition += Vector3.forward * mallet.FirstDriverShift;
        }

        private void MapMalletPart(Transform part, Matrix4x4 bodyMap)
        {
            var body = MalletBody;
            var local = part.parent.worldToLocalMatrix * body.localToWorldMatrix * bodyMap * body.worldToLocalMatrix * part.localToWorldMatrix;
            if (PacificHudsonAffine.TryDecompose(local, part.localScale, out var position, out var rotation, out var scale))
            { part.localPosition = position; part.localRotation = rotation; part.localScale = scale; return; }
            Main.Logger.Log("2-6-6-2: " + part.name + " cannot be scaled without shear; moved only.");
            part.localPosition = local.MultiplyPoint3x4(Vector3.zero);
        }

        // Lazily clones the rear engine from the prepared (stock) pose. Called after
        // preparedGeometry.Restore and before any front-engine edits.
        private void EnsureMalletAssets()
        {
            if (malletRearL != null && malletRearR != null) return;
            var created = new List<Transform>();
            try
            {
                malletRearL = CloneEngineDrive(driveL, lfAnimator, "[Rearranged Mallet rear drivetrain L]", articulatedRearRoot, d => ConfigureMalletDrive(d, true));
                created.Add(malletRearL);
                malletRearR = CloneEngineDrive(driveR, rfAnimator, "[Rearranged Mallet rear drivetrain R]", articulatedRearRoot, d => ConfigureMalletDrive(d, true));
                created.Add(malletRearR);
                foreach (var source in MalletStaticParts())
                {
                    var clone = Object.Instantiate(source.gameObject, source.parent).transform;
                    created.Add(clone);
                    clone.name = "[Rearranged Mallet rear] " + source.name;
                    PreShiftMalletPart(clone);
                    MapMalletPart(clone, mallet.Rear);
                    clone.SetParent(articulatedRearRoot, true);
                    clone.gameObject.SetActive(false);
                    malletAssets.Add(clone);
                }
            }
            catch
            {
                foreach (var t in created.Where(t => t != null)) { t.gameObject.SetActive(false); Object.Destroy(t.gameObject); }
                malletAssets.Clear(); malletRearL = malletRearR = null;
                throw;
            }
        }

        private Transform CloneEngineDrive(Transform source, Animator sourceAnimator, string name, Transform parent, Action<Transform> configure)
        {
            var clone = Object.Instantiate(source.gameObject, parent).transform;
            clone.name = name;
            clone.gameObject.SetActive(false);
            // Presentation only: copied oil providers/port readers must not create
            // duplicate native circuits. The native Animator itself is retained.
            foreach (var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
            { behaviour.enabled = false; Object.Destroy(behaviour); }
            configure(clone);
            var mirror = clone.gameObject.AddComponent<MalletAnimatorMirror>();
            mirror.Source = sourceAnimator; mirror.Target = clone.GetComponent<Animator>();
            return clone;
        }

        // One three-axle engine: drivers 1-3 at equal pitch (the native main rod
        // already drives axle 3), the existing three-axle rods moved forward one
        // pitch, drivers 4-6 hidden. Valve gear stays native.
        private void ConfigureMalletDrive(Transform drive, bool rearClone)
        {
            var wheels = Enumerable.Range(1, 6).Select(i => drive.Find("s282_wheels_driving_" + i)).ToArray();
            if (wheels.Any(w => w == null)) throw new InvalidOperationException("Mallet S282 driver is missing.");
            float pitch = wheels[1].localPosition.z - wheels[2].localPosition.z;
            float firstShift = wheels[1].localPosition.z + pitch - wheels[0].localPosition.z;
            wheels[0].localPosition += Vector3.forward * firstShift;
            for (int i = 0; i < 6; i++) wheels[i].gameObject.SetActive(i < 3);
            wheels[0].GetComponent<MeshFilter>().sharedMesh = flangedDriver;
            wheels[1].GetComponent<MeshFilter>().sharedMesh = blindDriver;
            wheels[2].GetComponent<MeshFilter>().sharedMesh = blindDriverBigWeight;

            var rod = RequireArticulatedPart(drive, "s282_mech_wheels_connect");
            rod.GetComponent<MeshRenderer>().enabled = false;
            var forward = rod.InverseTransformVector(drive.TransformVector(Vector3.forward * pitch));
            int threeAxle = 0;
            foreach (Transform child in rod)
            {
                var filter = child.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh == MeshFinder.Instance.ThreeAxleSideRodMesh)
                { child.gameObject.SetActive(true); child.localPosition += forward; threeAxle++; }
                else if (child.name.StartsWith("s282_mech_wheels_connect", StringComparison.Ordinal))
                    child.gameObject.SetActive(false);
            }
            if (threeAxle != 2) throw new InvalidOperationException("Mallet S282 three-axle side rods were not found.");
            SetPartActive(drive, "s282_mech_cutoff_rod", false);
            SetPartActive(drive, "s282_mech_cutoff_rail/s282_mech_lubricator_rod", false);
            SetPartActive(drive, "LubricatorParts", false);
            var shoes = drive.Find("s282a_brake_shoes(Clone)");
            if (shoes != null)
                foreach (Transform shoe in shoes)
                {
                    shoe.gameObject.SetActive(shoe.name == "s282a_brake_shoe_1" || shoe.name == "s282a_brake_shoe_2" || shoe.name == "s282a_brake_shoe_3");
                    if (shoe.name == "s282a_brake_shoe_1") shoe.localPosition += Vector3.forward * firstShift;
                }
            if (rearClone)
                foreach (var child in drive.GetComponentsInChildren<Transform>(true))
                    if (child.name.EndsWith("LOD1", StringComparison.Ordinal)) child.gameObject.SetActive(false);
        }

        private void BindMalletAnimation() => BindScaledEngineAnimation(malletRearL, malletRearR);

        // Adds cloned engines (alternating left, right) to the native wheel
        // animation, with the same per-side start offsets as the front engine.
        private void BindScaledEngineAnimation(params Transform[] clones)
        {
            articulatedRotation = loco.GetComponentsInChildren<PoweredWheelRotationViaAnimation>(true)
                .FirstOrDefault(r => r.animatorSetups != null && r.animatorSetups.Any(s => s.animator == lfAnimator));
            if (articulatedRotation == null) throw new InvalidOperationException("Native S282 wheel animation controller was not found.");
            articulatedOriginalAnimators = articulatedRotation.animatorSetups;
            float Offset(Animator a) => articulatedOriginalAnimators.First(s => s.animator == a).startTimeOffset;
            var pairs = clones.Select((clone, i) => (source: i % 2 == 0 ? lfAnimator : rfAnimator, target: clone.GetComponent<Animator>())).ToArray();
            articulatedRotation.animatorSetups = articulatedOriginalAnimators.Concat(pairs.Select(p =>
                new PoweredWheelRotationViaAnimation.AnimatorStartTimeOffsetPair {animator=p.target, startTimeOffset=Offset(p.source)})).ToArray();
            // Every engine shares the native crank phase, as on the 2-4-4-2.
            foreach (var pair in pairs)
            {
                var state = pair.source.GetCurrentAnimatorStateInfo(0);
                pair.target.Play(state.shortNameHash, 0, state.normalizedTime % 1f);
            }
        }

        private void BindMalletBrakeHeat() => BindEngineBrakeHeat(new[] { malletRearL, malletRearR }, 1, 2, 3);

        private void BindEngineBrakeHeat(Transform[] drives, params int[] shoes)
        {
            var controller = GetComponent<BrakesOverheatingController>();
            if (controller == null) return;
            var original = controller.brakeRenderers;
            var added = drives.SelectMany(d => shoes
                .Select(i => RequireArticulatedPart(d, "s282a_brake_shoes(Clone)/s282a_brake_shoe_" + i).GetComponent<MeshRenderer>()));
            restoreArticulatedBrakeRenderers = () => { if (controller != null) controller.brakeRenderers = original; };
            controller.brakeRenderers = original.Concat(added).Distinct().ToArray();
        }

        private void UpdateMalletSparks()
        {
            var axles = new[] { driveAxle1, driveAxle2, driveAxle3, driveAxle4, driveAxle5, driveAxle6 };
            var left = new[] { spark1L, spark2L, spark3L, spark4L, spark5L, spark6L };
            var right = new[] { spark1R, spark2R, spark3R, spark4R, spark5R, spark6R };
            for (int i = 0; i < 6; i++)
            {
                bool front = i == 0 || i == 1 || i == 5;
                var point = transform.InverseTransformPoint(axles[i].position); point.y = 0f;
                left[i].position = transform.TransformPoint(new Vector3(-.75f, 0f, point.z));
                right[i].position = transform.TransformPoint(new Vector3(.75f, 0f, point.z));
                left[i].gameObject.SetActive(true); right[i].gameObject.SetActive(true);
                MoveArticulatedPart(left[i], front ? articulatedFrontRoot : articulatedRearRoot);
                MoveArticulatedPart(right[i], front ? articulatedFrontRoot : articulatedRearRoot);
            }
        }

        // Cylinder-cock, drip and crack outlets follow the scaled engines.
        internal bool TryMapArticulatedOutlet(Vector3 nativeWorld, int engine, out Vector3 engineLocal) =>
            TryMapArticulatedOutlet(nativeWorld, nativeWorld, engine, out engineLocal);

        // Moves an outlet so that its emitter centre (not necessarily its origin)
        // lands on the engine-mapped position. Origin and centre in world space.
        internal bool TryMapArticulatedOutlet(Vector3 nativeOrigin, Vector3 nativeCentre, int engine, out Vector3 engineLocal)
        {
            engineLocal = default;
            if (scaledEngineMaps == null || engine < 0 || engine >= scaledEngineMaps.Length || articulatedFrontFrame == null) return false;
            var origin = MalletBody.InverseTransformPoint(nativeOrigin);
            var centre = MalletBody.InverseTransformPoint(nativeCentre);
            engineLocal = origin + scaledEngineMaps[engine].MultiplyPoint3x4(centre) - centre;
            return true;
        }

        // Curve diagnostics: each rigid driver group as currently displayed. Wheels
        // are the visible left drivers (physical axles are not reliably placed for
        // legacy layouts). Flanged is the authored mesh choice, a modelling input.
        internal sealed class DriverGroup
        {
            internal string Name;
            internal Transform Body, EngineRoot;
            internal Vector3? Pivot;
            internal (Transform wheel, bool flanged)[] Wheels;
        }

        internal IEnumerable<DriverGroup> DescribeDriverGroups()
        {
            if (driveL == null) yield break;
            // Conventional layouts never build the articulated frames; measure them in the car frame.
            var body = articulatedFrontFrame != null ? MalletBody : transform;
            DriverGroup Make(string name, Transform drive, Transform engineRoot, Transform pin) => new DriverGroup
            {
                Name = name, Body = body, EngineRoot = engineRoot,
                Pivot = pin != null ? body.InverseTransformPoint(pin.position) : (Vector3?)null,
                Wheels = drive == null ? Array.Empty<(Transform, bool)>() : drive.GetComponentsInChildren<Transform>(false)
                    .Where(t => t.name.StartsWith("s282_wheels_driving_", StringComparison.Ordinal) && !t.name.Contains("LOD") && t.gameObject.activeInHierarchy)
                    .Select(t => (t, t.GetComponent<MeshFilter>()?.sharedMesh == flangedDriver)).ToArray()
            };
            switch (currentWA)
            {
                case WheelArrangementType.s2442:
                case WheelArrangementType.s4444:
                    yield return Make("front engine", driveL, articulatedFrontRoot, articulatedPin);
                    yield return Make("rear engine", articulatedRearL, articulatedRearRoot, null);
                    break;
                case WheelArrangementType.s2662:
                    yield return Make("front engine", driveL, articulatedFrontRoot, articulatedPin);
                    yield return Make("rear engine", malletRearL, articulatedRearRoot, null);
                    break;
                case WheelArrangementType.s08880:
                case WheelArrangementType.s2680:
                case WheelArrangementType.s28882:
                    yield return Make("front engine", driveL, articulatedFrontRoot, articulatedPin);
                    yield return Make("rear engine", bigMalletRearL, articulatedRearRoot, null);
                    break;
                case WheelArrangementType.s22222:
                case WheelArrangementType.s02220:
                    yield return Make("front engine", driveL, articulatedFrontRoot, articulatedPin);
                    yield return Make("middle engine", triplexMiddleL, articulatedMiddleRoot, triplexMiddlePin);
                    yield return Make("rear engine", triplexRearL, articulatedRearRoot, null);
                    break;
                case WheelArrangementType.s222:
                    // One giant renderer-only driver: a single self-steering axle, no group.
                    break;
                default:
                    yield return Make("rigid", driveL, body, null);
                    break;
            }
        }

        private void SuspendMalletGeometry()
        {
            scaledEngineMaps = null;
            SuspendTriplexGeometry();
            DetachTenderEngine();
            SuspendBigMalletGeometry();
            foreach (var asset in malletAssets) if (asset != null) asset.gameObject.SetActive(false);
            if (malletRearL != null) malletRearL.gameObject.SetActive(false);
            if (malletRearR != null) malletRearR.gameObject.SetActive(false);
        }
    }

    // The rear engine's Animator has no native port reader; follow the front
    // engine's cutoff/reverser parameters on the same side.
    internal sealed class MalletAnimatorMirror : MonoBehaviour
    {
        internal Animator Source, Target;
        private AnimatorControllerParameter[] parameters;
        private RuntimeAnimatorController sourceController, targetController;

        private void Update()
        {
            if (Source == null || Target == null || !Target.isActiveAndEnabled) return;
            // Copy only parameters both controllers declare (the duplex Franklin
            // clip has none of the native ones); refresh when either changes.
            if (parameters == null || sourceController != Source.runtimeAnimatorController || targetController != Target.runtimeAnimatorController)
            {
                sourceController = Source.runtimeAnimatorController; targetController = Target.runtimeAnimatorController;
                var targetHashes = new HashSet<int>(Target.parameters.Select(x => x.nameHash));
                parameters = Source.parameters.Where(x => targetHashes.Contains(x.nameHash)).ToArray();
            }
            foreach (var parameter in parameters)
                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Float: Target.SetFloat(parameter.nameHash, Source.GetFloat(parameter.nameHash)); break;
                    case AnimatorControllerParameterType.Int: Target.SetInteger(parameter.nameHash, Source.GetInteger(parameter.nameHash)); break;
                    case AnimatorControllerParameterType.Bool: Target.SetBool(parameter.nameHash, Source.GetBool(parameter.nameHash)); break;
                }
        }
    }
}
#endif
