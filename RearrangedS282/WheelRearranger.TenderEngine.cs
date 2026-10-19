#if B99_ALL_STANDARD
using System;
using System.Collections.Generic;
using System.Linq;
using DV.ThingTypes;
using DV.Wheels;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RearrangedS282
{
    // 2-8-8-8-2 Erie triplex: the third engine rides under the S282 tender, a
    // separate car. Its steam comes from the loco's native graph (the middle
    // engine in TenderMode); its pull is applied to the tender's bogies and is
    // limited by the tender's own weight, so grip falls as coal and water are
    // used, as on the real P-1. Its exhaust leaves the loco's blast.
    //
    // Nothing is saved on the tender: the link is rebuilt whenever the loco is
    // in this arrangement with an S282 tender coupled behind it, and removed on
    // uncoupling, switching, derailment or destruction (polled every physics tick).
    internal partial class WheelRearranger
    {
        // Tender engine cylinders just behind the tender's front buffer beam.
        private const float TenderCylinderFrontZ = -.35f;
        // Four of the tender's five visible axles (drivers plus trailing wheel) carry the drivers.
        private const float TenderDriverWeightShare = .8f, TenderEngineFriction = .24f;

        private TrainCar tenderEngineCar;
        private Transform tenderEngineRoot, tenderDriveL, tenderDriveR;
        private readonly List<GameObject> tenderHidden = new List<GameObject>();
        private PoweredWheelRotationViaAnimation.AnimatorStartTimeOffsetPair[] tenderAnimatorsBefore;
        private float tenderForceN, tenderLimitN, tenderDemandN;
        private bool tenderSlipping;

        private static bool IsS282Tender(TrainCar car) =>
            car != null && CarTypes.IsTender(car.carLivery) && car.carType == TrainCarType.Tender;

        // Called every physics tick from FixedUpdate.
        private void TickTenderEngine(TrainCar car)
        {
            TrainCar wanted = null;
            if (car != null && currentWA == WheelArrangementType.s28882 && bigMallet != null && articulatedSimulation != null && !car.derailed)
            {
                var other = car.rearCoupler != null && car.rearCoupler.coupledTo != null ? car.rearCoupler.coupledTo.train : null;
                if (IsS282Tender(other) && !other.derailed) wanted = other;
            }
            if (wanted != tenderEngineCar)
            {
                DetachTenderEngine();
                if (wanted != null)
                {
                    try { AttachTenderEngine(wanted); }
                    catch (Exception ex)
                    {
                        Main.Logger.Error($"Erie triplex tender engine could not be attached to {wanted.ID}: {ex}");
                        DetachTenderEngine();
                        tenderEngineCar = wanted;   // do not retry every tick; re-armed by uncoupling
                        return;
                    }
                }
            }
            if (tenderEngineRoot != null) ApplyTenderForce(car);
        }

        private void AttachTenderEngine(TrainCar tender)
        {
            if (bigMalletRearL == null || bigMalletRearR == null) throw new InvalidOperationException("The rear engine has not been built.");
            tenderEngineCar = tender;
            float s = bigMallet.Scale;
            float cylinderFront = MeasureNativeCylinderFront();
            // The tender is rail-level at its front coupler, like the body frame.
            float offset = TenderCylinderFrontZ - s * cylinderFront;
            tenderEngineRoot = new GameObject("[Rearranged tender engine]").transform;
            tenderEngineRoot.SetParent(tender.transform, false);

            // Drives: copies of the configured four-axle rear engine.
            tenderDriveL = CloneEngineDrive(bigMalletRearL, lfAnimator, "[Rearranged tender drivetrain L]", tenderEngineRoot, d => { });
            tenderDriveR = CloneEngineDrive(bigMalletRearR, rfAnimator, "[Rearranged tender drivetrain R]", tenderEngineRoot, d => { });
            foreach (var (drive, mirrored) in new[] { (tenderDriveL, true), (tenderDriveR, false) })
            {
                drive.localPosition = new Vector3(0f, 0f, offset);
                drive.localRotation = Quaternion.identity;
                drive.localScale = new Vector3(mirrored ? -1f : 1f, s, s);
                drive.gameObject.SetActive(true);
            }

            // Cylinders, guides, brakes and springs: the rear engine's, moved by
            // the rear-to-tender engine offset (body frame = tender frame axes).
            var body = MalletBody;
            var shift = Matrix4x4.Translate(new Vector3(0f, 0f, offset - bigMallet.RearOffset));
            foreach (var source in bigMalletStatics[currentWA].Statics.Where(t => t != null))
            {
                var bodyPose = shift * body.worldToLocalMatrix * source.localToWorldMatrix;
                var clone = Object.Instantiate(source.gameObject, tenderEngineRoot).transform;
                clone.name = "[Rearranged tender] " + source.name.Replace("[Rearranged eight-coupled rear] ", "");
                if (PacificHudsonAffine.TryDecompose(bodyPose, source.lossyScale, out var position, out var rotation, out var scale))
                { clone.localPosition = position; clone.localRotation = rotation; clone.localScale = scale; }
                else clone.localPosition = bodyPose.MultiplyPoint3x4(Vector3.zero);
                clone.gameObject.SetActive(true);
                EnableGeneratedRenderers(clone);
            }

            // The tender's own bogie wheels and frames give way to the engine,
            // except the rearmost wheelset, which becomes the trailing truck.
            var rearmost = tender.Bogies.SelectMany(b => b.Axles.Select(a => a.transform))
                .OrderBy(a => tender.transform.InverseTransformPoint(a.position).z).FirstOrDefault();
            foreach (var bogie in tender.Bogies)
            {
                var frame = bogie.transform.Find("bogie_car");
                if (frame == null) continue;
                foreach (Transform child in frame)
                {
                    bool axle = child.name.StartsWith("[axle]", StringComparison.Ordinal);
                    if (axle && child == rearmost) continue;
                    var targets = axle ? child.Cast<Transform>().Select(t => t.gameObject) : new[] { child.gameObject };
                    foreach (var go in targets)
                        if (go.activeSelf && go.name != "ContactPoints") { go.SetActive(false); tenderHidden.Add(go); }
                }
            }

            // Drivers turn with the loco's wheel animation, in the native phase.
            if (articulatedRotation != null)
            {
                tenderAnimatorsBefore = articulatedRotation.animatorSetups;
                float Offset(Animator a) => tenderAnimatorsBefore.First(x => x.animator == a).startTimeOffset;
                var pairs = new[] { (src: lfAnimator, dst: tenderDriveL.GetComponent<Animator>()), (src: rfAnimator, dst: tenderDriveR.GetComponent<Animator>()) };
                articulatedRotation.animatorSetups = tenderAnimatorsBefore.Concat(pairs.Select(p =>
                    new PoweredWheelRotationViaAnimation.AnimatorStartTimeOffsetPair { animator = p.dst, startTimeOffset = Offset(p.src) })).ToArray();
                foreach (var p in pairs)
                {
                    var state = p.src.GetCurrentAnimatorStateInfo(0);
                    p.dst.Play(state.shortNameHash, 0, state.normalizedTime % 1f);
                }
            }
            articulatedSimulation.SetTenderEngine(true);
            Main.Logger.Log($"Erie triplex: tender engine attached to {tender.ID}; {2f * bigMallet.Radius:0.00} m drivers, " +
                $"cylinders at {TenderCylinderFrontZ:0.00}, drivers {offset + s * firstLeftDriveWheel.localPosition.z:0.00}..{offset + s * fourthLeftDriveWheel.localPosition.z:0.00} (tender z).");
        }

        private void DetachTenderEngine()
        {
            articulatedSimulation?.SetTenderEngine(false);
            if (articulatedRotation != null && tenderAnimatorsBefore != null)
            {
                // Also drops animators destroyed with a deleted tender (Unity null).
                var ours = new[] { tenderDriveL != null ? tenderDriveL.GetComponent<Animator>() : null,
                    tenderDriveR != null ? tenderDriveR.GetComponent<Animator>() : null };
                articulatedRotation.animatorSetups = articulatedRotation.animatorSetups
                    .Where(x => x.animator != null && !ours.Contains(x.animator)).ToArray();
            }
            tenderAnimatorsBefore = null;
            foreach (var go in tenderHidden) if (go != null) go.SetActive(true);
            tenderHidden.Clear();
            if (tenderEngineRoot != null) { tenderEngineRoot.gameObject.SetActive(false); Object.Destroy(tenderEngineRoot.gameObject); }
            if (tenderEngineCar != null && tenderEngineRoot != null) Main.Logger.Log($"Erie triplex: tender engine detached from {tenderEngineCar.ID}.");
            tenderEngineRoot = tenderDriveL = tenderDriveR = null;
            tenderEngineCar = null;
            tenderForceN = tenderLimitN = tenderDemandN = 0f; tenderSlipping = false;
        }

        // Pull at the tender's rails, limited by the tender's own adhesion.
        private void ApplyTenderForce(TrainCar loco)
        {
            var tender = tenderEngineCar;
            if (tender == null || tender.massController == null) return;
            float radius = .712f * bigMallet.Scale;
            tenderDemandN = radius > 0f ? articulatedSimulation.TenderTorque / radius : 0f;
            tenderLimitN = TenderEngineFriction * tender.massController.TotalMass * -Physics.gravity.y * TenderDriverWeightShare;
            tenderSlipping = Mathf.Abs(tenderDemandN) > tenderLimitN;
            tenderForceN = Mathf.Clamp(tenderDemandN, -tenderLimitN, tenderLimitN);
            if (tenderForceN == 0f || tender.Bogies == null || tender.Bogies.Length == 0) return;
            if (tender.isEligibleForSleep) tender.ForceOptimizationState(sleep: false);
            float perBogie = tenderForceN / tender.Bogies.Length;
            var forward = loco.transform.forward;
            foreach (var bogie in tender.Bogies)
            {
                if (bogie == null || !bogie.enabled || bogie.rb == null || bogie.rb.isKinematic) continue;
                var axis = bogie.transform.forward;
                bogie.rb.AddForce(axis * (Mathf.Sign(Vector3.Dot(axis, forward)) * perBogie));
            }
        }

        internal JObject TenderEngineReport() => currentWA != WheelArrangementType.s28882 ? null : new JObject
        {
            ["tender"] = tenderEngineCar != null ? tenderEngineCar.ID : null, ["attached"] = tenderEngineRoot != null,
            ["demandN"] = tenderDemandN, ["limitN"] = tenderLimitN, ["appliedN"] = tenderForceN, ["slipping"] = tenderSlipping,
            ["tenderMassKg"] = tenderEngineCar != null && tenderEngineCar.massController != null ? tenderEngineCar.massController.TotalMass : (float?)null
        };
    }
}
#endif
