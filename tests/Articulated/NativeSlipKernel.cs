using System;
using System.IO;
using System.Linq;
using System.Reflection;

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
        return cases.Length;
    }
    private static void Set(object instance, string field, object value) => instance.GetType().GetField(field).SetValue(instance, value);
}
