#if B99_ALL_STANDARD
using System;
using System.Collections.Generic;
using System.Linq;
using DV.Wheels;
using DV.Simulation.Brake;
using LocoSim.Implementations.Wheels;
using UnityEngine;
using UnityEngine.Animations;
using Object = UnityEngine.Object;

namespace RearrangedS282
{
    internal partial class WheelRearranger
    {
        internal const float ArticulatedEngineSeparation = 4.65f;
        private static readonly string[] ArticulatedSplitFrames = { "s282a_frame_left", "s282a_frame_right" };
        private Transform articulatedFrontFrame, articulatedFrontRoot, articulatedRearRoot, articulatedPin;
        private PositionConstraint articulatedPosition;
        private AimConstraint articulatedAim;
        private Transform articulatedRearL, articulatedRearR;
        private readonly List<Transform> articulatedAssets = new List<Transform>();
        private readonly List<MovedEnginePart> articulatedMoved = new List<MovedEnginePart>();
        private readonly List<Mesh> articulatedMeshes = new List<Mesh>();
        private readonly List<RendererState> articulatedCoarseRenderers = new List<RendererState>();
        private readonly List<ArticulatedLodState> articulatedLods = new List<ArticulatedLodState>();
        private readonly List<PoweredAnchorState> articulatedPoweredAnchors = new List<PoweredAnchorState>();
        private readonly List<EnabledReaderState> articulatedReaders = new List<EnabledReaderState>();
        private readonly Dictionary<int, Transform[]> articulatedFrameMeshes = new Dictionary<int, Transform[]>();
        private PoweredWheelRotationViaAnimation articulatedRotation;
        private PoweredWheelRotationViaAnimation.AnimatorStartTimeOffsetPair[] articulatedOriginalAnimators;
        private Mesh articulatedSideRod, articulatedGuideL, articulatedGuideR;
        private Transform rearGuideL, rearGuideR;
        private Transform[] frontFranklin, frontFranklin4444, rearBranches, rearBranches4444, rearReverse, rearReverse4444;
        private bool articulatedAssetsReady;
        private Action restoreArticulatedBrakeRenderers;

        internal Transform ArticulatedFrontFrame => articulatedFrontFrame;
        internal Transform ArticulatedRearFrame => articulatedRearRoot;
        // These two anchors have the original body coordinate system at rest.
        // Particles/audio can preserve their native world pose when attached.
        internal Transform ArticulatedFrontRoot => articulatedFrontRoot;
        internal Transform ArticulatedRearRoot => articulatedRearRoot;
        // Triplex only: the swinging middle engine. Null for other arrangements.
        internal Transform ArticulatedMiddleRoot => articulatedMiddleRoot != null && articulatedMiddleRoot.gameObject.activeInHierarchy ? articulatedMiddleRoot : null;
        internal bool ArticulatedGeometryActive => articulatedFrontFrame != null &&
            articulatedFrontFrame.gameObject.activeInHierarchy && articulatedPosition != null &&
            articulatedPosition.constraintActive && articulatedAim != null && articulatedAim.constraintActive &&
            articulatedRearRoot != null && articulatedRearRoot.gameObject.activeInHierarchy;

        // Skeleton only: a missing optional asset must never prevent a standard
        // conversion. Native bogies and axle components remain where DV finds them.
        private void SetupArticulatedGeometry()
        {
            if (articulatedFrontFrame != null) return;
            var parent = driveL.parent;
            articulatedFrontFrame = NewArticulatedRoot("[Rearranged articulated front frame]", parent);
            articulatedFrontRoot = NewArticulatedRoot("[Rearranged front engine]", articulatedFrontFrame);
            articulatedRearRoot = NewArticulatedRoot("[Rearranged rear engine]", parent);
            articulatedPin = NewArticulatedRoot("[Rearranged articulation pin]", parent);
            articulatedPosition = articulatedFrontFrame.gameObject.AddComponent<PositionConstraint>();
            articulatedAim = articulatedFrontFrame.gameObject.AddComponent<AimConstraint>();
            articulatedPosition.constraintActive = false;
            articulatedAim.constraintActive = false;
            articulatedFrontFrame.gameObject.SetActive(false);
            articulatedRearRoot.gameObject.SetActive(false);
            articulatedPin.gameObject.SetActive(false);
        }

        private static Transform NewArticulatedRoot(string name, Transform parent)
        {
            var result = new GameObject(name).transform;
            result.SetParent(parent, false);
            return result;
        }

        private void EnsureArticulatedAssets()
        {
            if (articulatedAssetsReady) return;
            AssetBundleLoader.ValidateArticulatedAssets();
            // Complete mesh validation before touching any original drivetrain.
            var shortRod = MeshFinder.Instance.DuplexMainRodMesh;
            articulatedSideRod = CreateArticulatedSideRod();
            articulatedGuideL = MeshFinder.alterCrossheadGuideLMesh(lCrossheadGuide);
            articulatedGuideR = MeshFinder.alterCrossheadGuideRMesh(rCrossheadGuide);
            articulatedGuideL.RecalculateBounds(); articulatedGuideR.RecalculateBounds();
            articulatedMeshes.Add(articulatedGuideL); articulatedMeshes.Add(articulatedGuideR);
            try
            {
                articulatedRearL = CloneRearDrive(driveL, "[Rearranged rear drivetrain L]");
                articulatedRearR = CloneRearDrive(driveR, "[Rearranged rear drivetrain R]");
                var material = firstRightDriveWheel.GetComponent<MeshRenderer>().sharedMaterial;
                frontFranklin = AddPair(AssetBundleLoader.FranklinBValveGear, driveL, driveR, material);
                frontFranklin4444 = AddPair(AssetBundleLoader.FranklinBValveGear4444, driveL, driveR, material);
                AddPair(AssetBundleLoader.FranklinBValveGear, articulatedRearL, articulatedRearR, material);
                rearBranches = AddPair(AssetBundleLoader.BranchPipe, articulatedRearL, articulatedRearR, material);
                rearBranches4444 = AddPair(AssetBundleLoader.BranchPipe4444, articulatedRearL, articulatedRearR, material);
                rearReverse = AddPair(AssetBundleLoader.FranklinBReverseGear, articulatedRearL, articulatedRearR, material);
                rearReverse4444 = AddPair(AssetBundleLoader.FranklinBReverseGear4444, articulatedRearL, articulatedRearR, material);
                AddAsset(AssetBundleLoader.FranklinBReverseGearRear, articulatedRearR, material);
                rearGuideL = CloneRearPart(lfCrossheadBracket, "[Rearranged rear crosshead guide L]");
                rearGuideR = CloneRearPart(rfCrossheadBracket, "[Rearranged rear crosshead guide R]");
                rearGuideL.GetComponent<MeshFilter>().sharedMesh = articulatedGuideL;
                rearGuideR.GetComponent<MeshFilter>().sharedMesh = articulatedGuideR;
                foreach (var source in new[] { leftFirstBrakeCaliper, rightFirstBrakeCaliper,
                    leftSecondBrakeCaliper, rightSecondBrakeCaliper })
                {
                    var brake = CloneRearPart(source, "[Rearranged rear brake] " + source.name);
                    brake.localPosition = new Vector3(brake.localPosition.x, 0f, brake.localPosition.z);
                }
                foreach (string side in new[] { "l", "r" })
                    for (int i = 1; i <= 2; i++)
                    {
                        var suspension = RequireArticulatedPart(splitLoco, "s282a_suspension_" + side + "_" + i);
                        CloneRearPart(suspension, "[Rearranged rear suspension] " + suspension.name);
                    }
                ConfigureEngineDrive(articulatedRearL, shortRod, true);
                ConfigureEngineDrive(articulatedRearR, shortRod, true);
                articulatedAssetsReady = true;
            }
            catch
            {
                // Deferred destruction is safe because every incomplete clone is
                // disabled first, and originals have not yet been reparented.
                foreach (var asset in articulatedAssets.Where(t => t != null).ToArray())
                { asset.gameObject.SetActive(false); Object.Destroy(asset.gameObject); }
                articulatedAssets.Clear();
                if (articulatedRearL != null) { articulatedRearL.gameObject.SetActive(false); Object.Destroy(articulatedRearL.gameObject); }
                if (articulatedRearR != null) { articulatedRearR.gameObject.SetActive(false); Object.Destroy(articulatedRearR.gameObject); }
                articulatedRearL = null; articulatedRearR = null;
                throw;
            }
        }

        private Transform CloneRearDrive(Transform source, string name)
        {
            var clone = Object.Instantiate(source.gameObject, articulatedRearRoot).transform;
            clone.name = name;
            clone.gameObject.SetActive(false);
            clone.localPosition = new Vector3(0f, 0f, -ArticulatedEngineSeparation);
            // Rear copies are presentation only. In particular their copied oil
            // providers/port readers must not create duplicate native circuits.
            foreach (var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
            { behaviour.enabled = false; Object.Destroy(behaviour); }
            return clone;
        }

        private Transform CloneRearPart(Transform source, string name)
        {
            var clone = Object.Instantiate(source.gameObject, articulatedRearRoot).transform;
            clone.name = name;
            clone.position = source.position - transform.forward * ArticulatedEngineSeparation;
            clone.rotation = source.rotation;
            articulatedAssets.Add(clone);
            return clone;
        }

        private Transform[] AddPair(GameObject prefab, Transform left, Transform right, Material material)
            => new[] { AddAsset(prefab, left, material), AddAsset(prefab, right, material) };

        private Transform AddAsset(GameObject prefab, Transform parent, Material material)
        {
            var part = Object.Instantiate(prefab, parent).transform;
            // The original animation clips bind to these authored Clone names.
            foreach (var renderer in part.GetComponentsInChildren<MeshRenderer>(true)) renderer.sharedMaterial = material;
            articulatedAssets.Add(part);
            return part;
        }

        private Mesh CreateArticulatedSideRod()
        {
            var mesh = Object.Instantiate(MeshFinder.Instance.TwoAxleSideRodMesh);
            var vertices = mesh.vertices;
            if (vertices.Length <= 475) { Object.Destroy(mesh); throw new InvalidOperationException("Articulated side rod topology is unsupported."); }
            int[,] ranges = { {34,68}, {269,270}, {349,380}, {413,445}, {471,473}, {475,476}, {312,314}, {316,317} };
            for (int range = 0; range < ranges.GetLength(0); range++)
                for (int i = ranges[range, 0]; i < ranges[range, 1]; i++) vertices[i].z += .17f;
            for (int i = 152; i < 211; i += 2) vertices[i].z += .17f;
            foreach (int i in new[] {173,175,237,239}) vertices[i].z += .17f;
            mesh.name = "Rearranged articulated two-axle side rod";
            mesh.vertices = vertices; mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            articulatedMeshes.Add(mesh);
            return mesh;
        }

        private void ConfigureEngineDrive(Transform drive, Mesh shortRod, bool rear)
        {
            foreach (var child in drive.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.EndsWith("LOD1", StringComparison.Ordinal)) child.gameObject.SetActive(false);
                if (child.name == "s282_wheels_driving_3" || child.name == "s282_wheels_driving_4" ||
                    child.name == "s282_wheels_driving_5" || child.name == "s282_wheels_driving_6") child.gameObject.SetActive(false);
            }
            foreach (string name in new[] { "s282_mech_cutoff_boomerang_high", "s282_mech_cutoff_rail",
                "s282_mech_cutoff_rod", "s282_mech_cutoff_rod_hinge", "s282_mech_cylinder_rod", "LubricatorParts" })
                SetPartActive(drive, name, false);
            SetPartActive(drive, "s282_mech_push_rod/s282_mech_push_rod_to_cutoff_base", false);
            var rod = RequireArticulatedPart(drive, "s282_mech_wheels_connect");
            rod.GetComponent<MeshRenderer>().enabled = false;
            // Every prepared conventional rod copy stays hidden. A separate
            // short rod follows the same animated parent, as in the original.
            foreach (Transform child in rod) child.gameObject.SetActive(false);
            var main = RequireArticulatedPart(rod, "s282_mech_push_rod_to_connect");
            main.gameObject.SetActive(true);
            main.GetComponent<MeshFilter>().sharedMesh = shortRod;
            var cutoff = RequireArticulatedPart(rod, "s282_mech_connect_to_cutoff_base");
            cutoff.gameObject.SetActive(true);
            cutoff.localPosition = new Vector3(.15f, 0, 0);
            SetPartActive(cutoff, "s282_mech_connect_to_cutoff_rod", false);
            var shortSide = rod.Find("[Rearranged two-axle rod]");
            if (shortSide == null)
            {
                shortSide = NewArticulatedRoot("[Rearranged two-axle rod]", rod);
                shortSide.localEulerAngles = new Vector3(0, 0, 180);
                shortSide.gameObject.AddComponent<MeshFilter>().sharedMesh = articulatedSideRod;
                shortSide.gameObject.AddComponent<MeshRenderer>().sharedMaterials = rod.GetComponent<MeshRenderer>().sharedMaterials;
                articulatedAssets.Add(shortSide);
            }
            shortSide.gameObject.SetActive(true);
            var shoes = drive.Find("s282a_brake_shoes(Clone)");
            if (shoes != null)
                foreach (Transform shoe in shoes)
                    shoe.gameObject.SetActive(shoe.name == "s282a_brake_shoe_1" || shoe.name == "s282a_brake_shoe_2");
            if (rear)
            {
                RequireArticulatedPart(drive, "s282_wheels_driving_1").GetComponent<MeshFilter>().sharedMesh = blindDriver;
                RequireArticulatedPart(drive, "s282_wheels_driving_2").GetComponent<MeshFilter>().sharedMesh = flangedDriver;
                drive.GetComponent<Animator>().runtimeAnimatorController = AssetBundleLoader.FranklinBAnimCtrl;
            }
        }

        private static Transform RequireArticulatedPart(Transform root, string path)
        {
            var result = root.Find(path);
            if (result == null) throw new InvalidOperationException("Articulated S282 part is missing: " + path);
            return result;
        }
        private static void SetPartActive(Transform root, string path, bool active)
        { var part = root.Find(path); if (part != null) part.gameObject.SetActive(active); }
        private static void SetPairActive(Transform[] parts, bool active)
        { if (parts != null) foreach (var part in parts) if (part != null) part.gameObject.SetActive(active); }

        private void ApplyArticulatedGeometry(int id)
        {
            if (id != 31 && id != 32) throw new ArgumentOutOfRangeException(nameof(id));
            EnsureArticulatedAssets();
            bool fourLeading = id == 32;
            foreach (var asset in articulatedAssets) if (asset != null) asset.gameObject.SetActive(true);
            ResetBody(); ResetToFourCoupled(); HideLeadingWheels(); HideTrailingWheels();
            if (fourLeading) { ShowFourLeadingWheels(); ShowFourTrailingWheelsFor10Coupled(); }
            else { ShowTwoLeadingWheels(); ShowTwoTrailingWheelsFor12Coupled(); }
            driveAxle1.gameObject.SetActive(true); driveAxle2.gameObject.SetActive(true);
            driveAxle3.gameObject.SetActive(true); driveAxle4.gameObject.SetActive(true);
            driveAxle5.gameObject.SetActive(false); driveAxle6.gameObject.SetActive(false);
            lfCylinder.gameObject.SetActive(false); rfCylinder.gameObject.SetActive(false);
            cylinderCocks.gameObject.SetActive(false); lDryPipe.gameObject.SetActive(false); rDryPipe.gameObject.SetActive(false);
            lLiftingArmSupport.gameObject.SetActive(false); rLiftingArmSupport.gameObject.SetActive(false);
            reachRodSupport.gameObject.SetActive(false); lubricatorSupport.gameObject.SetActive(false);
            driveL.localPosition = driveR.localPosition = Vector3.zero;
            driveL.localScale = new Vector3(-1, 1, 1); driveR.localScale = Vector3.one;
            articulatedRearL.localPosition = articulatedRearR.localPosition = new Vector3(0, 0, -ArticulatedEngineSeparation);
            articulatedRearL.localScale = new Vector3(-1, 1, 1); articulatedRearR.localScale = Vector3.one;
            ConfigureEngineDrive(driveL, MeshFinder.Instance.DuplexMainRodMesh, false);
            ConfigureEngineDrive(driveR, MeshFinder.Instance.DuplexMainRodMesh, false);
            if (fourLeading)
            {
                firstLeftDriveWheel.localPosition = firstRightDriveWheel.localPosition = new Vector3(0, .71f, 7.76f);
                secondLeftDriveWheel.localPosition = secondRightDriveWheel.localPosition = new Vector3(0, .71f, 6.06f);
                leftCrosshead.localScale = rightCrosshead.localScale = new Vector3(1, 1, 1.35f);
                leftFirstBrakeCaliper.localPosition = rightFirstBrakeCaliper.localPosition = new Vector3(0, 0, -.21f);
                leftSecondBrakeCaliper.localPosition = rightSecondBrakeCaliper.localPosition = new Vector3(0, 0, -1.91f);
                leftFirstBrakeShoe.localPosition = rightFirstBrakeShoe.localPosition = new Vector3(0, 0, 7.92f);
                leftSecondBrakeShoe.localPosition = rightSecondBrakeShoe.localPosition = new Vector3(0, 0, 6.21f);
            }
            lfCrossheadBracket.GetComponent<MeshFilter>().sharedMesh = articulatedGuideL;
            rfCrossheadBracket.GetComponent<MeshFilter>().sharedMesh = articulatedGuideR;
            lfCrossheadBracket.localPosition = rfCrossheadBracket.localPosition = new Vector3(0, 0, fourLeading ? -.92f : .065f);
            lfCrossheadBracket.localScale = rfCrossheadBracket.localScale = new Vector3(-1, 1, fourLeading ? 1.33f : 1f);
            rearGuideL.localPosition = rearGuideR.localPosition = new Vector3(0, 0, -4.58f);
            rearGuideL.localScale = rearGuideR.localScale = new Vector3(-1, 1, 1);
            SetPairActive(frontFranklin, !fourLeading); SetPairActive(frontFranklin4444, fourLeading);
            SetPairActive(rearBranches, !fourLeading); SetPairActive(rearBranches4444, fourLeading);
            SetPairActive(rearReverse, !fourLeading); SetPairActive(rearReverse4444, fourLeading);
            // All other rear components were explicitly authored as active.
            articulatedRearL.gameObject.SetActive(true); articulatedRearR.gameObject.SetActive(true);
            lfAnimator.runtimeAnimatorController = rfAnimator.runtimeAnimatorController = fourLeading
                ? AssetBundleLoader.FranklinBAnimCtrl4444 : AssetBundleLoader.FranklinBAnimCtrl;
            BindArticulatedAnimation();
            BindArticulatedBrakeHeat();
            AlignNativeAxle(driveAxle1, firstLeftDriveWheel, true);
            AlignNativeAxle(driveAxle2, secondLeftDriveWheel, true);
            AlignNativeAxle(driveAxle3, RequireArticulatedPart(articulatedRearL, "s282_wheels_driving_1"), false);
            AlignNativeAxle(driveAxle4, RequireArticulatedPart(articulatedRearL, "s282_wheels_driving_2"), false);
            foreach (var part in new[] {driveL, driveR, lfCrossheadBracket, rfCrossheadBracket,
                leftFirstBrakeCaliper, rightFirstBrakeCaliper, leftSecondBrakeCaliper, rightSecondBrakeCaliper,
                fOilLines, rOilLines}) MoveArticulatedPart(part, articulatedFrontRoot);
            MoveFrontBranchFittings(fourLeading ? rearBranches4444 : rearBranches);
            foreach (string side in new[] { "l", "r" })
                for (int i=1; i<=4; i++)
                {
                    var suspension=RequireArticulatedPart(splitLoco, "s282a_suspension_"+side+"_"+i);
                    if(i<=2)
                    {
                        MoveArticulatedPart(suspension,articulatedFrontRoot);
                        if(fourLeading) suspension.localPosition += Vector3.back*.24f;
                    }
                    else suspension.gameObject.SetActive(false);
                }
            foreach (var part in new[] {leftThirdBrakeCaliper, rightThirdBrakeCaliper, leftFourthBrakeCaliper, rightFourthBrakeCaliper,
                leftFifthBrakeCaliper, rightFifthBrakeCaliper, leftSixthBrakeCaliper, rightSixthBrakeCaliper}) part.gameObject.SetActive(false);
            MoveNativeCylinderColliders(fourLeading);
            ConfigureNativeArticulation(id);
            BindArticulatedLods();
            UpdateArticulatedSparks();
            Main.Logger.Log("Articulated S282 " + (fourLeading ? "4-4-4-4" : "2-4-4-2") +
                ": native pin/aim constraints; four physical powered axles retained in two native bogies.");
        }

        private void AlignNativeAxle(Transform axle, Transform wheel, bool front) =>
            AlignNativeAxle(axle, wheel, front ? articulatedFrontRoot : articulatedRearRoot);

        private void AlignNativeAxle(Transform axle, Transform wheel, Transform engineRoot)
        {
            axle.position = wheel.position;
            var powered = axle.GetComponent<PoweredWheel>();
            if (powered == null) throw new InvalidOperationException("Native articulated powered axle is missing its component.");
            var anchor = NewArticulatedRoot("[powered axle anchor]", engineRoot);
            anchor.position = wheel.position;
            anchor.rotation = transform.rotation;
            articulatedPoweredAnchors.Add(new PoweredAnchorState {wheel=powered, original=powered.wheelTransform, generated=anchor});
            powered.wheelTransform = anchor;
        }

        private void BindArticulatedAnimation()
        {
            articulatedRotation = loco.GetComponentsInChildren<PoweredWheelRotationViaAnimation>(true)
                .FirstOrDefault(r => r.animatorSetups != null && r.animatorSetups.Any(s => s.animator == lfAnimator));
            if (articulatedRotation == null) throw new InvalidOperationException("Native S282 wheel animation controller was not found.");
            articulatedOriginalAnimators = articulatedRotation.animatorSetups;
            var rearLeft = articulatedRearL.GetComponent<Animator>(); var rearRight = articulatedRearR.GetComponent<Animator>();
            articulatedRotation.animatorSetups = articulatedOriginalAnimators.Concat(new[] {
                new PoweredWheelRotationViaAnimation.AnimatorStartTimeOffsetPair {animator=rearLeft, startTimeOffset=0f},
                new PoweredWheelRotationViaAnimation.AnimatorStartTimeOffsetPair {animator=rearRight, startTimeOffset=.75f}
            }).ToArray();
            float time = lfAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f;
            foreach (var setup in articulatedRotation.animatorSetups)
            {
                string state = setup.animator.runtimeAnimatorController == AssetBundleLoader.FranklinBAnimCtrl4444
                    ? "LocoS282A_AnimationDriveGear4444" : "LocoS282A_AnimationDriveGear";
                setup.animator.Play(state, 0, (time + setup.startTimeOffset) % 1f);
            }
            foreach (var drive in new[] {driveL, driveR})
                foreach (var reader in drive.GetComponents<MonoBehaviour>().Where(b => b.GetType().Name == "AnimatorPortReader"))
                { articulatedReaders.Add(new EnabledReaderState {reader=reader, enabled=reader.enabled}); reader.enabled=false; }
        }

        private void BindArticulatedBrakeHeat()
        {
            var controller=GetComponent<BrakesOverheatingController>();
            if(controller==null) return;
            var original=controller.brakeRenderers;
            var rearShoes=new[] {articulatedRearL,articulatedRearR}
                .SelectMany(d=>new[] {
                    RequireArticulatedPart(d,"s282a_brake_shoes(Clone)/s282a_brake_shoe_1").GetComponent<MeshRenderer>(),
                    RequireArticulatedPart(d,"s282a_brake_shoes(Clone)/s282a_brake_shoe_2").GetComponent<MeshRenderer>()
                });
            restoreArticulatedBrakeRenderers=()=>{if(controller!=null) controller.brakeRenderers=original;};
            controller.brakeRenderers=original.Concat(rearShoes).Distinct().ToArray();
        }

        private void ConfigureNativeArticulation(int id)
        {
            var support = transform.Find("Axle_F");
            if (support == null) throw new InvalidOperationException("Native front articulation support was not found.");
            var parent = articulatedFrontFrame.parent;
            Vector3 supportLocal = parent.InverseTransformPoint(support.position);
            Vector3 lastFront = parent.InverseTransformPoint(secondLeftDriveWheel.position);
            Vector3 firstRear = parent.InverseTransformPoint(RequireArticulatedPart(articulatedRearL, "s282_wheels_driving_1").position);
            ConfigureNativeArticulation(id, (lastFront.z + firstRear.z) * .5f);
        }

        // pinZ is in the drivetrain parent's coordinates, i.e. the body frame.
        private void ConfigureNativeArticulation(int id, float pinZ)
        {
            var support = transform.Find("Axle_F");
            if (support == null) throw new InvalidOperationException("Native front articulation support was not found.");
            var parent = articulatedFrontFrame.parent;
            Vector3 supportLocal = parent.InverseTransformPoint(support.position);
            Vector3 pin = new Vector3(supportLocal.x, supportLocal.y, pinZ);
            BindSplitArticulatedFrame(id, parent.TransformPoint(pin));
            articulatedPin.localPosition = pin;
            articulatedFrontFrame.localPosition = pin;
            articulatedFrontFrame.localRotation = Quaternion.identity;
            // Keep every previously moved original at its authored straight-track
            // world pose while relocating the parent's pivot to its rear pin.
            articulatedFrontRoot.localPosition = -pin;
            articulatedFrontRoot.localRotation = Quaternion.identity;
            articulatedPosition.translationAtRest = pin;
            articulatedPosition.translationOffset = Vector3.zero;
            articulatedPosition.translationAxis = Axis.X | Axis.Y | Axis.Z;
            while (articulatedPosition.sourceCount > 0) articulatedPosition.RemoveSource(0);
            articulatedPosition.AddSource(new ConstraintSource {sourceTransform=articulatedPin, weight=1f});
            articulatedPosition.weight=1f; articulatedPosition.locked=true;
            articulatedAim.aimVector = articulatedFrontFrame.InverseTransformDirection(transform.forward).normalized;
            articulatedAim.upVector = articulatedFrontFrame.InverseTransformDirection(transform.up).normalized;
            articulatedAim.worldUpType = AimConstraint.WorldUpType.ObjectRotationUp;
            articulatedAim.worldUpObject = transform; articulatedAim.worldUpVector=Vector3.up;
            articulatedAim.rotationAtRest=Vector3.zero; articulatedAim.rotationOffset=Vector3.zero;
            articulatedAim.rotationAxis=Axis.X | Axis.Y | Axis.Z;
            while (articulatedAim.sourceCount > 0) articulatedAim.RemoveSource(0);
            articulatedAim.AddSource(new ConstraintSource {sourceTransform=support, weight=1f});
            articulatedAim.weight=1f; articulatedAim.locked=true;
            articulatedPin.gameObject.SetActive(true);
            articulatedRearRoot.gameObject.SetActive(true); articulatedFrontFrame.gameObject.SetActive(true);
            articulatedPosition.constraintActive=true; articulatedAim.constraintActive=true;
        }

        private void BindSplitArticulatedFrame(int id, Vector3 worldPin)
        {
            if(!articulatedFrameMeshes.ContainsKey(id))
            {
                var parts=new List<Transform>();
                try
                {
                    // LMS frame_* is the main bar frame (z -1.67..6.48) which spans
                    // the pin. subframe_* is only the firebox section behind it
                    // (z -4.29..-1.60), so it stays boiler-fixed and uncut.
                    // Cut front to back: one cut per pin, one piece per engine. The
                    // triplex adds its middle hinge as a second cut.
                    var pins=new List<Vector3> {worldPin};
                    var owners=new List<(Transform root,string label)> {(articulatedFrontRoot,"front")};
                    if(id==ArticulatedSimulation.TriplexId && triplex!=null)
                    { pins.Add(articulatedFrontFrame.parent.TransformPoint(new Vector3(0f,0f,triplex.MiddlePinZ))); owners.Add((articulatedMiddleRoot,"middle")); }
                    owners.Add((articulatedRearRoot,"rear"));
                    foreach(string name in ArticulatedSplitFrames)
                    {
                        var source=RequireArticulatedPart(splitLoco,name);
                        var mesh=source.GetComponent<MeshFilter>().sharedMesh;
                        var normal=source.localToWorldMatrix.transpose.MultiplyVector(transform.forward).normalized;
                        var remaining=mesh;
                        for(int piece=0;piece<owners.Count;piece++)
                        {
                            Mesh split;
                            if(piece<pins.Count)
                            {
                                var point=source.InverseTransformPoint(pins[piece]);
                                split=ArticulatedFrameClipper.Clip(remaining,point,normal,true);
                                var rest=ArticulatedFrameClipper.Clip(remaining,point,normal,false);
                                articulatedMeshes.Add(split); articulatedMeshes.Add(rest);
                                if(split.triangles.Length==0 || rest.triangles.Length==0)
                                    throw new InvalidOperationException("S282 frame does not span its measured articulation pin: "+name+
                                        " (local pin "+point.ToString("F3")+", mesh bounds "+mesh.bounds.min.ToString("F3")+".."+mesh.bounds.max.ToString("F3")+")");
                                remaining=rest;
                            }
                            else split=remaining;
                            var part=Object.Instantiate(source.gameObject,source.parent).transform;
                            part.name="[Rearranged "+owners[piece].label+" frame "+id+"] "+name;
                            part.GetComponent<MeshFilter>().sharedMesh=split;
                            part.SetParent(owners[piece].root,true);
                            parts.Add(part); articulatedAssets.Add(part);
                        }
                    }
                    articulatedFrameMeshes.Add(id,parts.ToArray());
                }
                catch
                {
                    foreach(var part in parts) if(part!=null) {part.gameObject.SetActive(false);Object.Destroy(part.gameObject);}
                    throw;
                }
            }
            foreach(var pair in articulatedFrameMeshes)
                foreach(var part in pair.Value) part.gameObject.SetActive(pair.Key==id);
            foreach(string name in ArticulatedSplitFrames)
                RequireArticulatedPart(splitLoco,name).gameObject.SetActive(false);
        }

        private void MoveArticulatedPart(Transform part, Transform destination)
        {
            if (part == null) return;
            articulatedMoved.Add(new MovedEnginePart(part));
            part.SetParent(destination, true);
        }

        private void MoveNativeCylinderColliders(bool fourLeading)
        {
            // These are the native walk/item cylinder shells, not the single
            // boiler/body collision mesh. Preserve their active state and layers.
            foreach (string path in new[] {
                "[colliders]/[walkable]/[old]/Exterior/cylinder",
                "[colliders]/[walkable]/[old]/Exterior/cylinder (1)",
                "[colliders]/[walkable]/[old]/Exterior/area_big_cylinder",
                "[colliders]/[items]/[old]/Exterior/area_big_cylinder" })
            {
                var part = transform.Find(path);
                MoveArticulatedPart(part, articulatedFrontRoot);
                if (part != null && fourLeading) part.localPosition += Vector3.forward * .35f;
            }
        }

        private void MoveFrontBranchFittings(Transform[] branches)
        {
            // Original rigid duplexes put both manifolds in a rear-owned prefab.
            // Only its front manifold/dry-pipe pieces belong to the swinging
            // front assembly; the authored rear manifold remains boiler-fixed.
            foreach (var branch in branches)
                foreach (var child in branch.Cast<Transform>().ToArray())
                    if (child.name.StartsWith("intake_manifold_f", StringComparison.Ordinal) ||
                        child.name.StartsWith("dry_pipe_new", StringComparison.Ordinal))
                        MoveArticulatedPart(child, articulatedFrontRoot);
        }

        private void UpdateArticulatedSparks()
        {
            var axles = new[] {driveAxle1, driveAxle2, driveAxle3, driveAxle4};
            var left = new[] {spark1L,spark2L,spark3L,spark4L};
            var right = new[] {spark1R,spark2R,spark3R,spark4R};
            for (int i=0;i<4;i++)
            {
                var point = transform.InverseTransformPoint(axles[i].position); point.y=0f;
                left[i].position=transform.TransformPoint(new Vector3(-.75f,0f,point.z));
                right[i].position=transform.TransformPoint(new Vector3(.75f,0f,point.z));
                MoveArticulatedPart(left[i], i<2 ? articulatedFrontRoot : articulatedRearRoot);
                MoveArticulatedPart(right[i], i<2 ? articulatedFrontRoot : articulatedRearRoot);
            }
        }

        private void BindArticulatedLods()
        {
            var body=transform.Find("LocoS282A_Body");
            if (body == null) return;
            var groups=body.GetComponentsInChildren<LODGroup>(true).Select(g=>(group:g, lods:g.GetLODs())).Where(g=>g.lods.Length!=0).ToArray();
            // A renderer must belong to exactly one LODGroup (Unity rejects and
            // unpredictably culls duplicates). Keep native membership; otherwise
            // use the nearest enclosing group (each drivetrain and rear clone has
            // its own), falling back to the body group.
            var owner=new Dictionary<Renderer,LODGroup>();
            foreach(var g in groups) foreach(var r in g.lods.SelectMany(l=>l.renderers)) if(r!=null && !owner.ContainsKey(r)) owner[r]=g.group;
            var bodyGroup=body.GetComponent<LODGroup>() ?? groups.Select(g=>g.group).FirstOrDefault();
            // Native coarse proxies stay coarse: promoting them would draw them
            // on top of the detailed parts they stand in for.
            var coarse=new HashSet<Renderer>(groups.SelectMany(g=>g.lods.Skip(1).SelectMany(l=>l.renderers)).Where(r=>r!=null));
            coarse.ExceptWith(groups.SelectMany(g=>g.lods[0].renderers).Where(r=>r!=null));
            var extras=articulatedFrontRoot.GetComponentsInChildren<Renderer>(true).Concat(articulatedRearRoot.GetComponentsInChildren<Renderer>(true))
                .Concat(articulatedMiddleRoot!=null ? articulatedMiddleRoot.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>())
                .Where(r=>r!=null && !(r is ParticleSystemRenderer) && r.gameObject.activeInHierarchy && r.enabled && !coarse.Contains(r)).Distinct().ToArray();
            foreach(var r in extras)
                if(!owner.ContainsKey(r)) owner[r]=r.GetComponentsInParent<LODGroup>(true).FirstOrDefault(g=>groups.Any(x=>x.group==g)) ?? bodyGroup;
            foreach (var (group, lods) in groups)
            {
                articulatedLods.Add(new ArticulatedLodState {group=group, lods=lods});
                var detail=lods[0].renderers.Where(r=>r!=null && owner[r]==group)
                    .Concat(extras.Where(r=>owner[r]==group)).Distinct().ToArray();
                foreach(var renderer in lods.Skip(1).SelectMany(l=>l.renderers).Where(r=>r!=null && !detail.Contains(r)).Distinct())
                { articulatedCoarseRenderers.Add(new RendererState {renderer=renderer, enabled=renderer.enabled}); renderer.enabled=false; }
                // As in L74, retain the detailed, correctly articulated engine
                // through the native far-cull threshold instead of rigid proxies.
                group.SetLODs(new[] {new LOD(lods[lods.Length-1].screenRelativeTransitionHeight, detail)});
                group.RecalculateBounds();
            }
            LogArticulatedRenderReport();
        }

        // Test-build diagnostic: name every visible part reaching behind the cab
        // (the native firebox subframe's rear end), and the front engine's
        // cylinders, so in-game reports can be traced to an object.
        private void LogArticulatedRenderReport()
        {
            try
            {
                var body=articulatedFrontFrame.parent;
                float BodyZ(Bounds b, bool min)
                {
                    float best=min?float.PositiveInfinity:float.NegativeInfinity;
                    for(int i=0;i<8;i++)
                    {
                        var corner=new Vector3((i&1)==0?b.min.x:b.max.x,(i&2)==0?b.min.y:b.max.y,(i&4)==0?b.min.z:b.max.z);
                        float z=body.InverseTransformPoint(corner).z; best=min?Mathf.Min(best,z):Mathf.Max(best,z);
                    }
                    return best;
                }
                string Path(Transform t){var names=new List<string>();for(;t!=null&&t!=transform;t=t.parent)names.Add(t.name);names.Reverse();return string.Join("/",names);}
                var subframe=splitLoco.Find("s282a_subframe_left")?.GetComponent<Renderer>();
                float cabRear=subframe!=null?BodyZ(subframe.bounds,true):0f;
                var lines=new List<string>();
                foreach(var r in GetComponentsInChildren<Renderer>(true))
                {
                    if(r is ParticleSystemRenderer || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                    float min=BodyZ(r.bounds,true);
                    if(min<cabRear-.5f && lines.Count<40)
                        lines.Add($"  behind cab {min:0.00}..{BodyZ(r.bounds,false):0.00}: {Path(r.transform)} [{r.GetComponent<MeshFilter>()?.sharedMesh?.name}]");
                }
                foreach(var pair in new[] {frontFranklin, frontFranklin4444})
                    if(pair!=null) foreach(var part in pair)
                        if(part!=null && part.gameObject.activeInHierarchy)
                            foreach(var r in part.GetComponentsInChildren<Renderer>(true).Where(r=>r.transform.parent!=null && r.transform.parent.name=="cylinder"))
                                lines.Add($"  front cylinder {Path(r.transform)} active={r.gameObject.activeInHierarchy} enabled={r.enabled} z={BodyZ(r.bounds,true):0.00}..{BodyZ(r.bounds,false):0.00} lodGroups={GetComponentsInChildren<LODGroup>(true).Count(g=>g.GetLODs().Any(l=>l.renderers.Contains(r)))}");
                Main.Logger.Log($"Articulated render report (cab rear {cabRear:0.00} body z):\n"+string.Join("\n",lines));
            }
            catch(Exception ex){Main.Logger.Log("Articulated render report failed: "+ex.Message);}
        }

        private void SuspendArticulatedGeometry()
        {
            if (articulatedPosition != null) articulatedPosition.constraintActive=false;
            if (articulatedAim != null) articulatedAim.constraintActive=false;
            if (articulatedRotation != null && articulatedOriginalAnimators != null)
                articulatedRotation.animatorSetups=articulatedOriginalAnimators;
            articulatedOriginalAnimators=null;
            restoreArticulatedBrakeRenderers?.Invoke(); restoreArticulatedBrakeRenderers=null;
            foreach (var entry in articulatedReaders) if(entry.reader!=null) entry.reader.enabled=entry.enabled;
            articulatedReaders.Clear();
            foreach (var entry in articulatedLods) if(entry.group!=null) {entry.group.SetLODs(entry.lods); entry.group.RecalculateBounds();}
            articulatedLods.Clear();
            foreach (var entry in articulatedCoarseRenderers) if(entry.renderer!=null) entry.renderer.enabled=entry.enabled;
            articulatedCoarseRenderers.Clear();
            foreach (var entry in articulatedPoweredAnchors)
            {
                if(entry.wheel!=null) entry.wheel.wheelTransform=entry.original;
                if(entry.generated!=null) {entry.generated.gameObject.SetActive(false); Object.Destroy(entry.generated.gameObject);}
            }
            articulatedPoweredAnchors.Clear();
            for(int i=articulatedMoved.Count-1;i>=0;i--) articulatedMoved[i].Restore();
            articulatedMoved.Clear();
            foreach (var asset in articulatedAssets) if(asset!=null) asset.gameObject.SetActive(false);
            SuspendMalletGeometry();
            if(articulatedRearL!=null) articulatedRearL.gameObject.SetActive(false);
            if(articulatedRearR!=null) articulatedRearR.gameObject.SetActive(false);
            if(articulatedFrontFrame!=null) {articulatedFrontFrame.gameObject.SetActive(false); articulatedFrontFrame.localPosition=Vector3.zero; articulatedFrontFrame.localRotation=Quaternion.identity;}
            if(articulatedFrontRoot!=null) {articulatedFrontRoot.localPosition=Vector3.zero; articulatedFrontRoot.localRotation=Quaternion.identity;}
            if(articulatedRearRoot!=null) articulatedRearRoot.gameObject.SetActive(false);
            if(articulatedPin!=null) articulatedPin.gameObject.SetActive(false);
        }

        private void DisposeArticulatedGeometry()
        {
            SuspendArticulatedGeometry();
            foreach(var mesh in articulatedMeshes) if(mesh!=null) Object.Destroy(mesh);
            articulatedMeshes.Clear();
        }

        private sealed class MovedEnginePart
        {
            private readonly Transform part,parent;
            private readonly Vector3 position,scale;
            private readonly Quaternion rotation;
            private readonly int sibling;
            internal MovedEnginePart(Transform part)
            {this.part=part;parent=part.parent;position=part.localPosition;rotation=part.localRotation;scale=part.localScale;sibling=part.GetSiblingIndex();}
            internal void Restore()
            {if(part==null)return;part.SetParent(parent,false);part.localPosition=position;part.localRotation=rotation;part.localScale=scale;part.SetSiblingIndex(sibling);}
        }
        private sealed class RendererState {internal Renderer renderer;internal bool enabled;}
        private sealed class ArticulatedLodState {internal LODGroup group;internal LOD[] lods;}
        private sealed class PoweredAnchorState {internal PoweredWheel wheel;internal Transform original,generated;}
        private sealed class EnabledReaderState {internal MonoBehaviour reader;internal bool enabled;}
    }

    // LMS provides whole rigid subframes, whereas the native Heavy Mallet/L74
    // already has separate engine-frame meshes. Clip only this missing visual
    // subdivision. The hidden joint cut is uncapped; collision remains native.
    internal static class ArticulatedFrameClipper
    {
        private struct Vertex
        {
            internal Vector3 position, normal;
            internal Vector4 tangent;
            internal Color color;
            internal Vector4[] uv;
        }

        internal static Mesh Clip(Mesh source, Vector3 point, Vector3 normal, bool keepForward)
        {
            if(source==null || !source.isReadable)
                throw new InvalidOperationException("Articulated S282 frame mesh is not CPU readable.");
            if(normal.sqrMagnitude<1e-12f)
                throw new ArgumentException("Articulation frame cut requires a non-zero plane normal.");
            normal=normal.normalized*(keepForward?1f:-1f);
            var positions=source.vertices; var normals=source.normals; var tangents=source.tangents; var colors=source.colors;
            bool hasNormals=normals.Length!=0, hasTangents=tangents.Length!=0, hasColors=colors.Length!=0;
            if((hasNormals&&normals.Length!=positions.Length)||(hasTangents&&tangents.Length!=positions.Length)||
                (hasColors&&colors.Length!=positions.Length)||source.blendShapeCount!=0||source.boneWeights.Length!=0)
                throw new InvalidOperationException("Unsupported articulated frame vertex attributes.");
            var channels=new List<Vector4>[8];
            var resultChannels=new List<Vector4>[8];
            for(int channel=0;channel<8;channel++)
            {
                channels[channel]=new List<Vector4>(); source.GetUVs(channel,channels[channel]);
                if(channels[channel].Count!=0&&channels[channel].Count!=positions.Length)
                    throw new InvalidOperationException("Articulated frame UV channel has an invalid vertex count.");
                if(channels[channel].Count!=0) resultChannels[channel]=new List<Vector4>();
            }
            var resultPositions=new List<Vector3>(); var resultNormals=new List<Vector3>();
            var resultTangents=new List<Vector4>(); var resultColors=new List<Color>();
            var triangles=new List<int>[source.subMeshCount];
            Vertex Read(int index)
            {
                if(index<0||index>=positions.Length) throw new InvalidOperationException("Articulated frame triangle index is invalid.");
                var vertex=new Vertex {position=positions[index],normal=hasNormals?normals[index]:Vector3.zero,
                    tangent=hasTangents?tangents[index]:Vector4.zero,color=hasColors?colors[index]:Color.white,uv=new Vector4[8]};
                for(int channel=0;channel<8;channel++)
                    if(channels[channel].Count!=0) vertex.uv[channel]=channels[channel][index];
                return vertex;
            }
            void Emit(Vertex vertex,List<int> indices)
            {
                indices.Add(resultPositions.Count); resultPositions.Add(vertex.position);
                if(hasNormals) resultNormals.Add(vertex.normal);
                if(hasTangents) resultTangents.Add(vertex.tangent);
                if(hasColors) resultColors.Add(vertex.color);
                for(int channel=0;channel<8;channel++)
                    if(resultChannels[channel]!=null) resultChannels[channel].Add(vertex.uv[channel]);
            }
            for(int submesh=0;submesh<source.subMeshCount;submesh++)
            {
                if(source.GetTopology(submesh)!=MeshTopology.Triangles)
                    throw new InvalidOperationException("Articulated frame submesh does not contain triangles.");
                var indices=source.GetTriangles(submesh); triangles[submesh]=new List<int>();
                if(indices.Length%3!=0) throw new InvalidOperationException("Articulated frame triangle data is incomplete.");
                for(int i=0;i<indices.Length;i+=3)
                {
                    var polygon=new List<Vertex>(4);
                    var input=new[] {Read(indices[i]),Read(indices[i+1]),Read(indices[i+2])};
                    for(int edge=0;edge<3;edge++)
                    {
                        var from=input[edge]; var to=input[(edge+1)%3];
                        float fromDistance=Vector3.Dot(from.position-point,normal);
                        float toDistance=Vector3.Dot(to.position-point,normal);
                        bool fromInside=fromDistance>=0f, toInside=toDistance>=0f;
                        if(fromInside) polygon.Add(from);
                        if(fromInside!=toInside)
                        {
                            float t=fromDistance/(fromDistance-toDistance);
                            polygon.Add(Interpolate(from,to,t));
                        }
                    }
                    for(int vertex=1;vertex+1<polygon.Count;vertex++)
                    {
                        var a=polygon[0];var b=polygon[vertex];var c=polygon[vertex+1];
                        if(Vector3.Cross(b.position-a.position,c.position-a.position).sqrMagnitude<=1e-12f) continue;
                        Emit(a,triangles[submesh]);Emit(b,triangles[submesh]);Emit(c,triangles[submesh]);
                    }
                }
            }
            var result=new Mesh {name=source.name+(keepForward?" articulated front":" articulated rear")};
            try
            {
                if(resultPositions.Count>65535) result.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
                result.SetVertices(resultPositions);
                if(hasNormals) result.SetNormals(resultNormals);
                if(hasTangents) result.SetTangents(resultTangents);
                if(hasColors) result.SetColors(resultColors);
                for(int channel=0;channel<8;channel++)
                    if(resultChannels[channel]!=null) result.SetUVs(channel,resultChannels[channel]);
                result.subMeshCount=source.subMeshCount;
                for(int submesh=0;submesh<triangles.Length;submesh++) result.SetTriangles(triangles[submesh],submesh);
                if(!hasNormals && resultPositions.Count>0) result.RecalculateNormals();
                result.RecalculateBounds();
                return result;
            }
            catch {Object.Destroy(result);throw;}
        }

        private static Vertex Interpolate(Vertex from,Vertex to,float t)
        {
            var tangent=Vector4.LerpUnclamped(from.tangent,to.tangent,t);
            var direction=new Vector3(tangent.x,tangent.y,tangent.z).normalized;
            var result=new Vertex {position=Vector3.LerpUnclamped(from.position,to.position,t),
                normal=Vector3.LerpUnclamped(from.normal,to.normal,t).normalized,
                tangent=new Vector4(direction.x,direction.y,direction.z,tangent.w<0f?-1f:1f),
                color=Color.LerpUnclamped(from.color,to.color,t),uv=new Vector4[8]};
            for(int channel=0;channel<8;channel++) result.uv[channel]=Vector4.LerpUnclamped(from.uv[channel],to.uv[channel],t);
            return result;
        }
    }
}
#endif
