#if B99_ALL_STANDARD
using System;
using System.Linq;
using DV.ModularAudioCar;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RearrangedS282
{
    // Curving Stage 2: wheelbase-dependent extra curve drag and squeal
    // (CurveDragModel). Derail stress is a later stage. Stock (s282) and
    // anything curving no worse than stock get nothing extra.
    internal partial class WheelRearranger
    {
        private const float CurveSampleInterval = .1f, CurveGroupRefresh = 2f, CurveSmoothTime = .3f;
        private CurveDragModel.Axle[][] curveGroups;
        private WheelArrangementType curveGroupsFor;
        private float curveGroupsAt = float.NegativeInfinity, curveSampleAt = float.NegativeInfinity;
        private float curveCurvature, curveTargetN, curveSquealTarget;
        private CurveDragModel.Result curveModel, curveStock;

        internal float CurveExtraN { get; private set; }   // smoothed; applied when curve drag is on
        internal float CurveSqueal { get; private set; }   // smoothed stress-equivalent squeal level

        private void FixedUpdate()
        {
            var car = loco != null ? loco : GetComponent<TrainCar>();
            bool drag = Main.settings != null && Main.settings.curveDrag, squeal = Main.settings != null && Main.settings.curveSqueal;
            float dt = Time.fixedDeltaTime;
            if (car == null || (!drag && !squeal) || currentWA == WheelArrangementType.s282 || car.derailed ||
                car.Bogies == null || car.Bogies.Length == 0 || car.massController == null)
            {
                curveTargetN = curveSquealTarget = 0f; curveCurvature = 0f;
                CurveExtraN = Smooth(CurveExtraN, 0f, dt); CurveSqueal = Smooth(CurveSqueal, 0f, dt);
                return;
            }
            float now = Time.fixedTime;
            if (curveGroups == null || curveGroupsFor != currentWA || now - curveGroupsAt > CurveGroupRefresh)
            {
                bool changed = curveGroups == null || curveGroupsFor != currentWA;
                curveGroups = BuildCurveGroups(); curveGroupsFor = currentWA; curveGroupsAt = now;
                if (changed) LogCurveGroups(car);
            }
            float locoWeight = car.massController.TotalMass * -Physics.gravity.y;
            if (now - curveSampleAt >= CurveSampleInterval)
            {
                curveSampleAt = now;
                float sum = 0f; int count = 0;
                foreach (var bogie in car.Bogies)
                {
                    if (bogie == null || bogie.traveller == null) continue;
                    float k = CurveDiagnostics.SampledCurvature(bogie, car.transform.up);
                    if (!float.IsNaN(k) && !float.IsInfinity(k)) { sum += Mathf.Abs(k); count++; }
                }
                curveCurvature = count > 0 ? sum / count : 0f;
                curveModel = CurveDragModel.Groups(curveGroups, curveCurvature, car.massController.WeightPerAxle);
                curveStock = CurveDragModel.Stock(curveCurvature, locoWeight);
                curveTargetN = (float)Math.Max(0, curveModel.TotalN - curveStock.TotalN);
                curveSquealTarget = squeal ? (float)CurveDragModel.SquealLevel(curveTargetN, locoWeight, car.GetAbsSpeed()) : 0f;
            }
            CurveExtraN = Smooth(CurveExtraN, curveTargetN, dt);
            CurveSqueal = Smooth(CurveSqueal, curveSquealTarget, dt);
            if (!drag || CurveExtraN <= 0f) return;
            // Follow native rolling resistance: none while wheelslipping. Ramp to
            // zero at standstill so it can only ever oppose motion, never propel.
            if (car.SimController?.wheelslipController != null && car.SimController.wheelslipController.IsWheelslipping) return;
            float perBogie = CurveExtraN / car.Bogies.Length;
            foreach (var bogie in car.Bogies)
            {
                var rb = bogie != null ? bogie.rb : null;
                if (rb == null || rb.isKinematic || rb.IsSleeping()) continue;
                float v = bogie.transform.InverseTransformDirection(rb.velocity).z;
                float ramp = Mathf.Clamp01(Mathf.Abs(v) / .1f);
                if (ramp > 0f) rb.AddForce(bogie.transform.forward * (-Mathf.Sign(v) * perBogie * ramp));
            }
        }

        private static float Smooth(float current, float target, float dt) =>
            current + (target - current) * (1f - Mathf.Exp(-dt / CurveSmoothTime));

        // Driver groups as body-frame z (each in its engine's rest frame), with
        // flanged flags as authored and assumed lateral play.
        private CurveDragModel.Axle[][] BuildCurveGroups()
        {
            try
            {
                return DescribeDriverGroups()
                    .Select(g => CurveDragModel.WithPlay(
                        g.Wheels.Select(w => (double)g.EngineRoot.InverseTransformPoint(w.wheel.position).z).ToArray(),
                        g.Wheels.Select(w => w.flanged).ToArray()))
                    .ToArray();
            }
            catch (Exception ex)
            {
                Main.Logger.Warning($"Curve groups unavailable on {currentWA}: {ex.Message}");
                return Array.Empty<CurveDragModel.Axle[]>();
            }
        }

        private void LogCurveGroups(TrainCar car)
        {
            double weight = car.massController.TotalMass * -Physics.gravity.y, perAxle = car.massController.WeightPerAxle;
            string groups = string.Join(" + ", curveGroups.Select(g => $"{g.Length} drivers ({g.Count(a => a.Flanged)} flanged)"));
            Main.Logger.Log($"Curve drag {currentWA}: {(groups.Length > 0 ? groups : "no rigid driver groups")}; extra over stock " +
                $"R100 {CurveDragModel.ExtraN(curveGroups, .01, perAxle, weight) / 1000:F1} kN, R50 {CurveDragModel.ExtraN(curveGroups, .02, perAxle, weight) / 1000:F1} kN.");
        }

        internal JObject CurveDragReport() => new JObject
        {
            ["enabled"] = Main.settings != null && Main.settings.curveDrag, ["squealEnabled"] = Main.settings != null && Main.settings.curveSqueal,
            ["curvaturePerM"] = curveCurvature, ["groups"] = new JArray((curveGroups ?? Array.Empty<CurveDragModel.Axle[]>())
                .Select(g => new JArray(g.Select(a => new JObject { ["z"] = a.Z, ["flanged"] = a.Flanged, ["freeM"] = a.FreeM })))),
            ["scrubN"] = curveModel.ScrubN, ["flangeN"] = curveModel.FlangeN,
            ["stockScrubN"] = curveStock.ScrubN, ["stockFlangeN"] = curveStock.FlangeN,
            ["extraTargetN"] = curveTargetN, ["extraAppliedN"] = CurveExtraN, ["squeal"] = CurveSqueal
        };

        // Squeal: raise each bogie's native squeal layer to our level, never lower it.
        [HarmonyPatch(typeof(CarRollingAudioModule), nameof(CarRollingAudioModule.UpdateModule))]
        internal static class CurveSquealPatch
        {
            private static void Postfix(CarRollingAudioModule __instance)
            {
                var car = __instance.car;
                if (car == null || __instance.bogieAudioControllers == null) return;
                var rearranger = car.GetComponent<WheelRearranger>();
                if (rearranger == null || !(rearranger.CurveSqueal > 0f)) return;
                float level = Mathf.Max(car.stress != null ? car.stress.slowBuildUpStress : 0f, rearranger.CurveSqueal);
                foreach (var controller in __instance.bogieAudioControllers)
                    controller?.currentSquealingAudio?.Set(level);
            }
        }
    }
}
#endif
