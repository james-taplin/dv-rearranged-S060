#if B99_ALL_STANDARD || CURVE_MODEL_TEST
using System;

namespace RearrangedS282
{
    // Stage 2 of wheelbase-dependent curving: extra curve drag (and the squeal
    // level derived from it). Pure maths so it can be checked offline.
    //
    // Each driver group is treated as a rigid frame on a steady curve of
    // curvature k (track lateral y = k z^2 / 2 in the group frame):
    // - Scrub: the frame lines up with its own wheels (least-squares chord).
    //   Each axle slides at its angle of attack; only the along-track part of
    //   that friction resists motion: W * mu * (angle - deadband). The rigid-axle
    //   longitudinal slip (half gauge * k) acts on every wheelset, carrying axles
    //   included, and total weight is the same for every layout, so it cancels
    //   against stock and is left out.
    // - Flange: only flanged wheels guide the frame. With three or more in a
    //   group, the middle ones must take up the mid-ordinate within their
    //   lateral play; the excess loads the flanges. Blind drivers never do.
    // - Single-axle groups (triplex engines, the 2-2-2) self-steer: zero.
    // The stock 2-8-2's own value is subtracted, so stock and anything that
    // curves no worse than stock gets nothing extra. No negative bonus.
    // A gameplay approximation calibrated against Rockl's curve resistance,
    // not a wheel-rail dynamics simulation.
    public static class CurveDragModel
    {
        public const double Mu = .25, AngleDeadbandRad = .1 * Math.PI / 180;
        public const double FlangeFraction = .05, FlangeRampM = .02;
        public const double FlangewayM = .010, SideplayM = .003, LeadDriverExtraM = .022;
        public const int LongGroupDrivers = 5;
        // Stock S282 drivers (body z), flanged as authored (2 blind, 3 flangeless), equal axle load over 6 axles.
        public static readonly double[] StockZ = { 8.0, 6.3, 4.75, 3.2 };
        public static readonly bool[] StockFlanged = { true, false, false, true };
        public const int StockAxles = 6;
        // Squeal: stress-equivalent level per unit of extra drag / loco weight.
        public const double SquealPerDragFraction = 12;

        public struct Axle
        {
            public double Z; public bool Flanged; public double FreeM;
            public Axle(double z, bool flanged, double freeM) { Z = z; Flanged = flanged; FreeM = freeM; }
        }

        public struct Result
        {
            public double ScrubN, FlangeN;
            public double TotalN => ScrubN + FlangeN;
        }

        // Lateral play per axle, ordered front to back: flangeway + sideplay,
        // with a lateral-motion device on the lead driver of long groups.
        public static Axle[] WithPlay(double[] z, bool[] flanged)
        {
            if (z == null || flanged == null || z.Length != flanged.Length) throw new ArgumentException("Driver data mismatch.");
            var order = new int[z.Length];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            Array.Sort(order, (a, b) => z[b].CompareTo(z[a]));
            var axles = new Axle[z.Length];
            for (int i = 0; i < order.Length; i++)
                axles[i] = new Axle(z[order[i]], flanged[order[i]],
                    FlangewayM + SideplayM + (i == 0 && z.Length >= LongGroupDrivers ? LeadDriverExtraM : 0));
            return axles;
        }

        public static Result Group(Axle[] axles, double curvature, double weightPerAxleN)
        {
            var result = new Result();
            double k = Math.Abs(curvature);
            if (axles == null || axles.Length < 2 || !(k > 0) || double.IsInfinity(k) || !(weightPerAxleN > 0)) return result;
            Fit(axles, k, false, out double a, out double b);
            foreach (var axle in axles)
            {
                double angle = Math.Abs(k * axle.Z - b);
                result.ScrubN += weightPerAxleN * Mu * Math.Max(0, angle - AngleDeadbandRad);
            }
            int flanged = 0;
            foreach (var axle in axles) if (axle.Flanged) flanged++;
            if (flanged >= 3)
            {
                Fit(axles, k, true, out a, out b);
                foreach (var axle in axles)
                {
                    if (!axle.Flanged) continue;
                    double offset = Math.Abs(k * axle.Z * axle.Z / 2 - (a + b * axle.Z));
                    double excess = Math.Max(0, offset - axle.FreeM);
                    result.FlangeN += weightPerAxleN * FlangeFraction * Math.Min(1, excess / FlangeRampM);
                }
            }
            return result;
        }

        public static Result Groups(Axle[][] groups, double curvature, double weightPerAxleN)
        {
            var total = new Result();
            if (groups == null) return total;
            foreach (var group in groups)
            {
                var r = Group(group, curvature, weightPerAxleN);
                total.ScrubN += r.ScrubN; total.FlangeN += r.FlangeN;
            }
            return total;
        }

        public static Result Stock(double curvature, double locoWeightN) =>
            Group(WithPlay(StockZ, StockFlanged), curvature, locoWeightN / StockAxles);

        // Extra drag over the stock 2-8-2 on the same curve. Never negative.
        public static double ExtraN(Axle[][] groups, double curvature, double weightPerAxleN, double locoWeightN) =>
            Math.Max(0, Groups(groups, curvature, weightPerAxleN).TotalN - Stock(curvature, locoWeightN).TotalN);

        public static double SquealLevel(double extraN, double locoWeightN, double speedMs)
        {
            if (!(extraN > 0) || !(locoWeightN > 0)) return 0;
            double gate = Math.Max(0, Math.Min(1, (Math.Abs(speedMs) - .5) / 1.5));
            return Math.Min(1, extraN / locoWeightN * SquealPerDragFraction) * gate;
        }

        // Least-squares line y = a + b z through the track lateral at each axle
        // (all axles, or flanged only): the frame's self-aligned pose.
        private static void Fit(Axle[] axles, double k, bool flangedOnly, out double a, out double b)
        {
            double n = 0, zm = 0, ym = 0;
            foreach (var x in axles) if (!flangedOnly || x.Flanged) { n++; zm += x.Z; ym += k * x.Z * x.Z / 2; }
            zm /= n; ym /= n;
            double sxy = 0, sxx = 0;
            foreach (var x in axles)
                if (!flangedOnly || x.Flanged) { sxy += (x.Z - zm) * (k * x.Z * x.Z / 2 - ym); sxx += (x.Z - zm) * (x.Z - zm); }
            b = sxx > 0 ? sxy / sxx : 0; a = ym - b * zm;
        }
    }
}
#endif
