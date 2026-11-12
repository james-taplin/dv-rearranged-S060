using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

internal static class NativeSlipKernel
{
    internal static int CheckThresholds()
    {
        string managed = @"B:\SteamLibrary\steamapps\common\Derail Valley\DerailValley_Data\Managed";
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            string path = Path.Combine(managed, new AssemblyName(args.Name).Name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
        var native = Assembly.LoadFrom(Path.Combine(managed, "Assembly-CSharp.dll"));
        var system = native.GetType("DV.ECS.Train.WheelslipControllerSystem", true);
        var kernel = system.GetNestedTypes(BindingFlags.NonPublic).Single(t => t.Name.Contains("Calculate_Wheelslip"));
        var method = kernel.GetMethod("OriginalLambdaBody", BindingFlags.Instance | BindingFlags.NonPublic);
        Console.WriteLine("Native slip kernel: " + kernel.FullName);
        var cases = new[] {
            new { Name = "60 kN / 100 kN grip", Force = 60000f, Allowed = true, Prevent = false, Brake = 0f, Slip = 0f },
            new { Name = "120 kN / 100 kN grip", Force = 120000f, Allowed = true, Prevent = false, Brake = 0f, Slip = .2f },
            new { Name = "120 kN with difficulty disabling slip", Force = 120000f, Allowed = false, Prevent = false, Brake = 0f, Slip = 0f },
            new { Name = "120 kN with preventWheelslip", Force = 120000f, Allowed = true, Prevent = true, Brake = 0f, Slip = 0f },
            new { Name = "120 kN with 20 kN brake force", Force = 120000f, Allowed = true, Prevent = false, Brake = 10000f, Slip = 0f }
        };
        foreach (var test in cases)
        {
            object job = Activator.CreateInstance(kernel);
            foreach (var field in kernel.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.Name == "wheelslipAllowed") field.SetValue(job, test.Allowed);
                if (field.Name == "deltaTime") field.SetValue(job, .02f);
            }
            var args = method.GetParameters().Select(p => Activator.CreateInstance(p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType)).ToArray();
            float steadySlip = test.Slip;
            Set(args[0], "wheelslip", steadySlip);
            Set(args[3], "generatedForce", test.Force);
            Set(args[3], "preventWheelslip", test.Prevent);
            Set(args[3], "brakingForce", test.Brake);
            Set(args[3], "numberOfPoweredAxles", (ushort)4);
            Set(args[3], "wheelslipToAdhesionDrop", 1f);
            Set(args[3], "maxWheelslipRpm", 600f);
            Set(args[5], "bogieCount", (byte)2);
            Set(args[5], "numOfAxles", (byte)4);
            Set(args[6], "wheelslipForceLimitPerAxle", 25000f);
            // Test the exact steady state either side of the native threshold.
            // A wrong equilibrium would change slip and request an ECB tag,
            // which requires a real Unity world. No native code is replaced.
            method.Invoke(job, args);
            float slip = (float)args[0].GetType().GetField("wheelslip").GetValue(args[0]);
            if (slip != steadySlip) throw new Exception("Native ECS failed grip threshold equilibrium");
            Console.WriteLine($"Actual B99 ECS slip kernel: {test.Name} -> steady slip {slip:F6}.");
        }
        return cases.Length + CheckNativeAdhesion(native);
    }
    private static int CheckNativeAdhesion(Assembly native)
    {
        int checks = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
        var adhesion = native.GetType("DV.ECS.Train.AdhesionControllerSystem", true);
        var kernel = adhesion.GetNestedTypes(BindingFlags.NonPublic).Single(t => t.Name.Contains("Calculate_Adhesion"));
        var calculate = kernel.GetMethod("OriginalLambdaBody", BindingFlags.Instance | BindingFlags.NonPublic);
        var slipKernel = native.GetType("DV.ECS.Train.WheelslipControllerSystem", true).GetNestedTypes(BindingFlags.NonPublic).Single(t => t.Name.Contains("Calculate_Wheelslip"));
        var slipMethod = slipKernel.GetMethod("OriginalLambdaBody", BindingFlags.Instance | BindingFlags.NonPublic);
        var carType = native.GetType("TrainCar", true); var massType = native.GetType("TrainMassController", true);
        var car = FormatterServices.GetUninitializedObject(carType); var massController = FormatterServices.GetUninitializedObject(massType);
        Set(massController, "car", car);
        foreach (float mass in new[] { 50000f, 56600f })
        {
            Set(massController, "<TotalMass>k__BackingField", mass);
            float previousDryGrip = 0;
            foreach (byte axles in new byte[] { 3, 4 })
            {
                Set(car, "numberOfAxles", (int)axles);
                float weight = (float)massType.GetProperty("WeightPerAxle").GetValue(massController);
                Check(Math.Abs(weight * axles - mass * 9.81f) < .1f, "Native mass getter must use current three/four axle denominator");
                foreach (float wetness in new[] { 0f, .5f })
                foreach (float sand in new[] { 1f, 1.25f })
                {
                    object job = Activator.CreateInstance(kernel); Set(job, "wetness", wetness); Set(job, "wheelSlideAllowed", false); Set(job, "deltaTime", .02f);
                    var args = Arguments(calculate);
                    var matrixField = args[4].GetType().GetField("Value"); object matrix = Activator.CreateInstance(matrixField.FieldType);
                    for (int c = 0; c < 4; c++)
                    {
                        var columnField = matrix.GetType().GetField("c" + c); object column = Activator.CreateInstance(columnField.FieldType);
                        for (int r = 0; r < 4; r++) Set(column, new[] { "x", "y", "z", "w" }[r], c == r ? 1f : 0f);
                        columnField.SetValue(matrix, column);
                    }
                    matrixField.SetValue(args[4], matrix);
                    Set(args[5], "weightPerAxle", weight); Set(args[5], "sandCoef", sand);
                    Set(args[6], "wheelslipFrictionCoef", .26f); Set(args[6], "wheelSlideFrictionCoef", .169f);
                    Set(args[7], "numOfAxles", axles); Set(args[7], "bogieCount", (byte)2);
                    calculate.Invoke(job, args);
                    float perAxle = (float)args[1].GetType().GetField("wheelslipForceLimitPerAxle").GetValue(args[1]);
                    float total = perAxle * axles, expected = .26f * mass * 9.81f * sand * (wetness == 0 ? 1 : .5f);
                    Check(Math.Abs(total - expected) < .1f, "Actual native adhesion kernel must preserve mass, wetness and sanding grip");
                    if (wetness == 0 && sand == 1)
                    { if (previousDryGrip > 0) Check(Math.Abs(previousDryGrip - total) < .1f, "Four powered axles cannot invent extra native grip over three"); previousDryGrip = total; }
                    foreach (bool brakes in new[] { false, true })
                    {
                        object slipJob = Activator.CreateInstance(slipKernel); Set(slipJob, "wheelslipAllowed", true); Set(slipJob, "deltaTime", .02f);
                        var s = Arguments(slipMethod); float steady = brakes ? 0f : .2f;
                        Set(s[0], "wheelslip", steady); Set(s[3], "generatedForce", total * 1.2f);
                        Set(s[3], "brakingForce", brakes ? total * .12f : 0f); Set(s[3], "numberOfPoweredAxles", (ushort)axles);
                        Set(s[3], "wheelslipToAdhesionDrop", 1f); Set(s[3], "maxWheelslipRpm", 600f);
                        Set(s[5], "bogieCount", (byte)2); Set(s[5], "numOfAxles", axles); Set(s[6], "wheelslipForceLimitPerAxle", perAxle);
                        slipMethod.Invoke(slipJob, s);
                        float limit = (float)s[0].GetType().GetField("totalForceLimit").GetValue(s[0]);
                        float actual = (float)s[0].GetType().GetField("wheelslip").GetValue(s[0]);
                        Check(Math.Abs(limit - total) < .1f && Math.Abs(actual - steady) < .00001f,
                            "Native slip output must clip at the actual adhesion limit and respect bogie braking");
                    }
                    Console.WriteLine($"Native adhesion: {mass:F0} kg / {axles} driven axles / wet {wetness:F1} / sand {sand:F2}: grip {total:F1} N.");
                }
            }
        }
        return checks;
    }
    private static object[] Arguments(MethodInfo method) => method.GetParameters().Select(p => Activator.CreateInstance(p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType)).ToArray();
    private static void Set(object instance, string field, object value) => instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(instance, value);
}
