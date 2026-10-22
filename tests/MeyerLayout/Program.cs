using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.IO;
using System.Text.Json;
using UnityEngine;
using DV.Logic.Job;
using RearrangedS060;

internal static class Program
{
    private static int checks;
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    private static int Main(string[] args)
    {
        try
        {
            // Measured native support/axle stations from FEASIBILITY.md. Saved
            // Meyer support preparation relocates the live pivots before the
            // original three axles are resolved. Lookup must use their homes.
            foreach (var sample in new[] {
                (pivot: .929f, offset: .775f, expected: 1.7006f),
                (pivot: .929f, offset: -.775f, expected: .1498f),
                (pivot: -1.396f, offset: 0f, expected: -1.3978f) })
            {
                var original = S060AxleGeometry.FromSupport(new Vector3(0, 0, sample.pivot), Quaternion.identity, new Vector3(0, .712128f, sample.offset));
                Check(Math.Abs(original.z - sample.expected) < .01, "Saved Meyer stock axle lookup retains original driver station despite relocated live pivot.");
            }
            Check(Math.Abs(S060AxleGeometry.FromSupport(new Vector3(0, 0, -1.396f), Quaternion.identity, new Vector3(0, .712128f, 0)).z - MeyerPlan.RearCentre) > 4,
                "Rear stock axle must not be identified using the prepared Meyer rear pivot.");
            Check(Math.Abs(S060AxleGeometry.FromSupport(new Vector3(0, 0, -1.396f), Quaternion.identity, new Vector3(0, .712128f, .011f)).z + 1.3978f) < .25f,
                "Reported live rear axle at -6.089 maps back to the stock driver after saved support preparation.");
            double r = MeyerPlan.FlangeRadius;
            Check(MeyerPlan.RearCentre + MeyerPlan.AxlePitch / 2 + r < -4.363802, "Both rear drivers must lie behind measured S060 body rear.");
            Check(MeyerPlan.FrontCentre - MeyerPlan.AxlePitch / 2 - r > -4.363802 && MeyerPlan.FrontCentre + MeyerPlan.AxlePitch / 2 + r < 4.092099, "Both front drivers remain under the measured S060 body.");
            Check(1.55 - .05 > MeyerPlan.DriverRadius + r, "Raised pannier deck clears driver flanges.");
            Check(MeyerPlan.TankCentreY - MeyerPlan.TankHeight / 2 >= 1.60 - .00001, "Panniers sit above the deck.");
            Check(-1.80 + .08 < -MeyerPlan.AxlePitch / 2 - r, "Rear buffer beam clears the rearmost wheel flange.");
            Check(MeyerPlan.RearCouplerZ < MeyerPlan.RearCentre - 2.14, "Rear coupler remains behind buffer heads.");
            // Exercise the production setter against DV's actual readonly Car
            // field, bypassing only construction that needs the running game.
            var car = (Car)FormatterServices.GetUninitializedObject(typeof(Car));
            var strings = typeof(Car).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(f => f.FieldType == typeof(string)).ToArray();
            foreach (var field in strings) field.SetValue(car, "preserve-" + field.Name);
            var identity = strings.Select(f => f.GetValue(car)).ToArray();
            for (int cycle = 0; cycle < 100; cycle++)
            {
                S060LogicLength.Set(car, 4.57f - MeyerPlan.RearCouplerZ);
                Check(Math.Abs(car.length - 12.84f) < .001, "Native logic length must reflect the rear extension.");
                S060LogicLength.Set(car, 9.14f);
                Check(Math.Abs(car.length - 9.14f) < .00001, "Native stock length must restore.");
            }
            Check(strings.Select((f, i) => Equals(f.GetValue(car), identity[i])).All(x => x), "Native identity strings remain unchanged.");
            foreach (float bad in new[] { 0, -1, float.NaN, float.PositiveInfinity })
            { bool refused = false; try { S060LogicLength.Set(car, bad); } catch (ArgumentOutOfRangeException) { refused = true; } Check(refused && car.length == 9.14f, "Invalid geometry must not mutate native length."); }
            foreach (float length in new[] { 1.8f, 3.0988f, 6.1f })
            {
                Check(Math.Abs(S060PipeGeometry.SpanZ(0, length)) < .00001f && Math.Abs(S060PipeGeometry.SpanZ(S060PipeGeometry.PipeLength, length) - length) < .00001f, "Pipe end datums must follow the sockets.");
                Check(Math.Abs(S060PipeGeometry.SpanZ(.1f, length) - .1f) < .00001f && Math.Abs(S060PipeGeometry.SpanZ(S060PipeGeometry.PipeLength - .1f, length) - (length - .1f)) < .00001f, "Pipe collars retain their length.");
            }
            var offset = S060PipeGeometry.CylinderOffset(.8f, S060EightLayout.ForwardOffset(.8f));
            Check(Math.Abs(offset.y + .1424256f) < .00001f && Math.Abs(offset.z - .39648008f) < .00001f, "Full-size cylinder follows the fitted piston-axis translation.");
            var upper = new Vector3(.8f, 2.35f, 3.16f); var lower = new Vector3(.8f, 1.56f, 3.16f);
            Check(S060BodyPipes.Bend(upper, offset) == upper && S060BodyPipes.Bend(lower, offset) == lower + offset, "Upper pipe remains on boiler, lower end follows unscaled cylinder.");
            if (args.Length == 1)
            {
                using var json = JsonDocument.Parse(File.ReadAllText(args[0]));
                var body = json.RootElement.GetProperty("meshes").EnumerateArray().Single(m => m.GetProperty("name").GetString() == "s060_body");
                var vertices = body.GetProperty("vertices").EnumerateArray().Select(p => new Vector3(p.GetProperty("x").GetSingle(), p.GetProperty("y").GetSingle(), p.GetProperty("z").GetSingle())).ToArray();
                var cylinders = vertices.Where(p => Math.Abs(p.x) > .77 && p.y < 1.57 && p.z > 2.55 && p.z < 3.7).ToArray();
                Check(cylinders.Length > 100, "Actual native cylinder fixture required.");
                foreach (float shift in new[] { 0, S060EightLayout.ForwardOffset(.8f) })
                {
                    var delta = S060PipeGeometry.CylinderOffset(.8f, shift); var moved = cylinders.Select(v => v + delta).ToArray();
                    for (int axis = 0; axis < 3; axis++)
                    {
                        int a = axis;
                        Check(Math.Abs((moved.Max(v => v[a]) - moved.Min(v => v[a])) - (cylinders.Max(v => v[a]) - cylinders.Min(v => v[a]))) < .00001,
                            "Native cylinder dimensions must be invariant under the arrangement fit.");
                    }
                    Check(moved.Max(v => v.z) < S060EightLayout.BodyFront && moved.Min(v => v.y) > 0, "Full-size cylinder/branch retains front and rail clearance.");
                }
                foreach (var point in vertices.Where(S060BodyPipes.InBranch))
                    Check(Math.Abs(S060BodyPipes.Bend(point, offset).x - point.x) < .00001, "Native steam branch does not change gauge/width.");
            }
            Console.WriteLine(checks + " measured geometry/native length/steam-pipe checks passed. No Unity physics, driving or radio interaction executed.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
