#if B99_ALL_STANDARD
using System;
using System.Collections.Generic;
using System.Linq;
using DV;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RearrangedS282
{
    // Stage 1 of wheelbase-dependent curving: MEASUREMENT ONLY. Adds a "curve"
    // block to power-diagnostics samples. Applies no force, sound or stress.
    // Native B99 positions bogies on the track centre-line and derives curve
    // effects from car-body stress alone (see evidence/curve-dynamics-feasibility).
    internal static class CurveDiagnostics
    {
        // Provisional, adjustable modelling assumptions. Logged with every capture.
        internal const float FlangewayM = .010f, SideplayM = .003f, LeadDriverExtraM = .022f;
        internal const int LongGroupDrivers = 5;
        internal const float ScrubFriction = .25f, SaturationAngleRad = .005f, FlangeTermFraction = .10f, FlangeSaturationM = .005f;
        internal const float SwingLimitDeg = 6f, CurveHalfSpanM = 3f;
        private const int SearchPoints = 60, SwingSteps = 25;

        internal static JObject SessionInfo() => new JObject {
            ["meaning"] = "Measured only; nothing is applied. Offsets/angles are of each driver centre against the sampled native track centre-line.",
            ["assumptions"] = new JObject {
                ["flangewayM"] = FlangewayM, ["sideplayM"] = SideplayM, ["leadDriverExtraM"] = LeadDriverExtraM,
                ["leadDriverExtraAppliesFromDrivers"] = LongGroupDrivers, ["scrubFriction"] = ScrubFriction,
                ["saturationAngleRad"] = SaturationAngleRad, ["flangeTermFraction"] = FlangeTermFraction,
                ["flangeSaturationM"] = FlangeSaturationM, ["swingLimitDeg"] = SwingLimitDeg, ["curveHalfSpanM"] = CurveHalfSpanM,
                ["flangedSource"] = "visual driver mesh (modelling assumption, not native physics)",
                ["weightSource"] = "native equal weight per physical axle"
            }
        };

        internal static JToken Sample(TrainCar car)
        {
            try
            {
                var rearranger = car.GetComponent<WheelRearranger>();
                var bogies = car.Bogies;
                if (bogies == null || bogies.Length == 0 || bogies.Any(b => b == null || b.traveller == null)) return JValue.CreateNull();
                var up = car.transform.up;
                var result = new JObject();
                var bogieRows = new JArray();
                foreach (var b in bogies)
                    bogieRows.Add(new JObject {
                        ["track"] = b.track != null ? b.track.name : null, ["span"] = F((float)b.traveller.Span),
                        ["direction"] = b.TrackDirectionSign, ["bodyZ"] = F(car.transform.InverseTransformPoint(b.transform.position).z),
                        ["yawDegVsBody"] = F(Vector3.SignedAngle(Flat(car.transform.forward, up), Flat(b.transform.forward, up), up)),
                        ["curvaturePerM"] = F(SampledCurvature(b, up))
                    });
                result["bogies"] = bogieRows;
                if (bogies.Length >= 2)
                {
                    float spacing = Vector3.Distance(Flat(bogies[0].transform.position, up), Flat(bogies[bogies.Length - 1].transform.position, up));
                    float yaw = Vector3.SignedAngle(Flat(bogies[bogies.Length - 1].transform.forward, up), Flat(bogies[0].transform.forward, up), up);
                    result["interBogie"] = new JObject { ["spacingM"] = F(spacing), ["yawDeg"] = F(yaw),
                        ["curvaturePerM"] = F(spacing > .5f ? yaw * Mathf.Deg2Rad / spacing : float.NaN) };
                }
                var stress = car.stress;
                float slow = stress != null ? stress.slowBuildUpStress : float.NaN;
                float coefficient = car.carLivery?.parentType != null ? car.carLivery.parentType.RollingResistanceCoef : float.NaN;
                result["native"] = new JObject {
                    ["stress"] = stress != null ? F(stress.stress) : JValue.CreateNull(), ["slowBuildUpStress"] = F(slow),
                    ["derailBuildUp"] = stress != null ? F(stress.derailBuildUp) : JValue.CreateNull(),
                    ["derailStressThreshold"] = F(Globals.G?.GameParams != null ? Globals.G.GameParams.DerailStressThreshold : float.NaN),
                    ["rollingResistanceN"] = new JArray(bogies.Select(b => F(coefficient * (1f + 25f * Mathf.Clamp01(slow)) * b.rb.mass * -Physics.gravity.y))),
                    ["wheelslip"] = car.SimController?.wheelslipController != null ? new JValue(car.SimController.wheelslipController.IsWheelslipping) : JValue.CreateNull()
                };
                float weight = car.massController != null ? car.massController.WeightPerAxle : float.NaN;
                var groups = new JArray();
                if (rearranger != null)
                    foreach (var group in rearranger.DescribeDriverGroups()) groups.Add(Group(car, bogies, up, group, weight));
                result["groups"] = groups;
                return result;
            }
            catch (Exception ex) { return new JObject { ["error"] = ex.GetType().Name + ": " + ex.Message }; }
        }

        private static JObject Group(TrainCar car, Bogie[] bogies, Vector3 up, WheelRearranger.DriverGroup group, float weight)
        {
            // Engine roots coincide with the body frame at rest, so this is the rest pose.
            var axles = group.Wheels.Select(w => (body: group.EngineRoot.InverseTransformPoint(w.wheel.position),
                flanged: w.flanged)).OrderByDescending(a => a.body.z).ToArray();
            var row = new JObject { ["name"] = group.Name, ["pivoted"] = group.Pivot.HasValue };
            if (axles.Length == 0) { row["note"] = "no measurable drivers"; return row; }
            var free = axles.Select((a, i) => FlangewayM + SideplayM + (i == 0 && axles.Length >= LongGroupDrivers ? LeadDriverExtraM : 0f)).ToArray();
            row["axles"] = new JArray(axles.Select((a, i) => new JObject { ["bodyZ"] = F(a.body.z), ["flanged"] = a.flanged, ["lateralFreeM"] = F(free[i]) }));
            row["rigid"] = Fit(car, bogies, up, group, axles, free, 0f, weight);
            if (group.Pivot.HasValue)
            {
                JObject best = null; float bestScore = float.PositiveInfinity;
                for (int k = 0; k < SwingSteps; k++)
                {
                    float yaw = -SwingLimitDeg + 2f * SwingLimitDeg * k / (SwingSteps - 1);
                    var fit = Fit(car, bogies, up, group, axles, free, yaw, weight);
                    float score = fit.Value<float?>("sumSquaredOffsetM2") ?? float.PositiveInfinity;
                    if (score < bestScore) { bestScore = score; best = fit; best["yawDeg"] = F(yaw); }
                }
                row["pivotFit"] = best;
            }
            // Effective-wheelbase alternative: curvature at the group centre only.
            var centre = group.Body.TransformPoint(new Vector3(0f, 0f, axles.Average(a => a.body.z)));
            var nearest = bogies.OrderBy(b => (b.transform.position - centre).sqrMagnitude).First();
            float curvature = SampledCurvature(nearest, up);
            var flangedZ = axles.Where(a => a.flanged).Select(a => a.body.z).ToArray();
            float span = flangedZ.Length >= 2 ? flangedZ.Max() - flangedZ.Min() : 0f;
            float mid = axles.Average(a => a.body.z);
            row["effectiveWheelbase"] = new JObject {
                ["curvaturePerM"] = F(curvature), ["flangedSpanM"] = F(span),
                ["midOrdinateMismatchM"] = F(Mathf.Abs(curvature) * span * span / 16f),
                ["angleOfAttackDeg"] = new JArray(axles.Select(a => F(Mathf.Abs(curvature * (a.body.z - mid)) * Mathf.Rad2Deg)))
            };
            return row;
        }

        // Driver centres held rigidly in the group frame (optionally yawed about the
        // pivot), projected onto the sampled track. Proposed values are NOT applied.
        private static JObject Fit(TrainCar car, Bogie[] bogies, Vector3 up, WheelRearranger.DriverGroup group,
            (Vector3 body, bool flanged)[] axles, float[] free, float yawDeg, float weight)
        {
            var rows = new JArray(); float sumSq = 0f, resistance = 0f, flangeTerm = 0f, maxResidual = 0f, maxAngle = 0f; int measured = 0;
            var rotation = Quaternion.AngleAxis(yawDeg, Vector3.up);
            for (int i = 0; i < axles.Length; i++)
            {
                var local = axles[i].body;
                if (group.Pivot.HasValue) local = group.Pivot.Value + rotation * (local - group.Pivot.Value);
                var world = group.Body.TransformPoint(local);
                var axis = group.Body.TransformDirection(rotation * Vector3.forward);
                if (!TryProject(bogies, world, up, out float lateral, out Vector3 tangent)) { rows.Add(new JObject { ["measured"] = false }); continue; }
                measured++;
                float angle = Vector3.Angle(Flat(axis, up), Flat(tangent, up)); angle = Mathf.Min(angle, 180f - angle);
                float residual = axles[i].flanged ? Mathf.Max(0f, Mathf.Abs(lateral) - free[i]) : 0f;
                sumSq += lateral * lateral; maxResidual = Mathf.Max(maxResidual, residual); maxAngle = Mathf.Max(maxAngle, angle);
                resistance += weight * ScrubFriction * Mathf.Min(1f, angle * Mathf.Deg2Rad / SaturationAngleRad);
                flangeTerm += weight * FlangeTermFraction * Mathf.Min(1f, residual / FlangeSaturationM);
                rows.Add(new JObject { ["lateralOffsetM"] = F(lateral), ["angleOfAttackDeg"] = F(angle), ["flangeResidualM"] = F(residual) });
            }
            return new JObject {
                ["axles"] = rows, ["measuredAxles"] = measured, ["sumSquaredOffsetM2"] = F(measured > 0 ? sumSq : float.NaN),
                ["maxFlangeResidualM"] = F(maxResidual), ["maxAngleOfAttackDeg"] = F(maxAngle),
                ["proposedScrubN"] = F(resistance), ["proposedFlangeN"] = F(flangeTerm)
            };
        }

        // Nearest point on the native centre-line of either bogie's current track.
        private static bool TryProject(Bogie[] bogies, Vector3 world, Vector3 up, out float lateral, out Vector3 tangent)
        {
            lateral = 0f; tangent = Vector3.zero; float best = float.PositiveInfinity; bool found = false;
            foreach (var bogie in bogies.OrderBy(b => (b.transform.position - world).sqrMagnitude))
            {
                var points = bogie.traveller.pointSet.points;
                int centre = bogie.traveller.curPoint.index;
                for (int i = Math.Max(0, centre - SearchPoints); i < Math.Min(points.Length - 1, centre + SearchPoints); i++)
                {
                    var a = (Vector3)points[i].position + WorldMover.currentMove;
                    var b = (Vector3)points[i + 1].position + WorldMover.currentMove;
                    var ab = Flat(b - a, up); float length = ab.sqrMagnitude; if (length < 1e-6f) continue;
                    float t = Vector3.Dot(Flat(world - a, up), ab) / length;
                    if (t < 0f || t > 1f) continue;
                    var nearest = a + (b - a) * t;
                    float distance = Flat(world - nearest, up).magnitude;
                    if (distance < best)
                    {
                        best = distance; found = true; tangent = ab.normalized;
                        lateral = Vector3.Dot(Flat(world - nearest, up), Vector3.Cross(up, tangent).normalized);
                    }
                }
                if (found) return true;
            }
            return false;
        }

        // Signed curvature from the track's own forward vectors either side of the bogie.
        private static float SampledCurvature(Bogie bogie, Vector3 up)
        {
            var points = bogie.traveller.pointSet.points;
            int i = bogie.traveller.curPoint.index, a = i, b = i;
            double span = points[i].span;
            while (a > 0 && span - points[a].span < CurveHalfSpanM) a--;
            while (b < points.Length - 1 && points[b].span - span < CurveHalfSpanM) b++;
            double length = points[b].span - points[a].span;
            if (length < .5) return float.NaN;
            float angle = Vector3.SignedAngle(Flat(points[a].forward, up), Flat(points[b].forward, up), up);
            return angle * Mathf.Deg2Rad / (float)length;
        }

        private static Vector3 Flat(Vector3 v, Vector3 up) => Vector3.ProjectOnPlane(v, up);
        private static JToken F(float value) => float.IsNaN(value) || float.IsInfinity(value) ? JValue.CreateNull() : new JValue(value);
    }
}
#endif
