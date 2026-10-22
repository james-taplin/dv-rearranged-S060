using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using LocoSim.Definitions;
using LocoSim.Implementations;
using Newtonsoft.Json.Linq;
using RearrangedS060;

// Executes actual native engines and sums, using calibration and port bindings
// read back from the exported CCL bundle. No replacement engine implementation.
internal static class NativeProbe
{
    private static readonly BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static int checks;
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
    private static T Detached<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
    private static void Link(PortReference reference, Port port) => typeof(PortReference).GetField("port", Flags).SetValue(reference, port);
    private static string Name(string field) => Regex.Replace(field.Replace("ReadOut", ""), "([a-z])([A-Z])", "$1_$2").ToUpperInvariant();
    private static ReciprocatingSteamEngine Engine(string evidence, string id)
    {
        var document = JObject.Parse(File.ReadAllText(Path.Combine(evidence, id + "-calibration.json")));
        var values = document["MonoBehaviour"] as JObject ?? document;
        var def = Detached<ReciprocatingSteamEngineDefinition>(); def.ID = id;
        foreach (var field in typeof(ReciprocatingSteamEngineDefinition).GetFields(Flags))
        {
            if ((field.FieldType == typeof(float) || field.FieldType == typeof(int)) && values[field.Name] != null) field.SetValue(def, values[field.Name].ToObject(field.FieldType));
            else if (field.FieldType == typeof(PortReferenceDefinition)) field.SetValue(def, new PortReferenceDefinition(PortValueType.GENERIC, Name(field.Name)));
            else if (field.FieldType == typeof(PortDefinition)) field.SetValue(def,
                new PortDefinition(field.Name == "torqueOut" ? PortType.OUT : field.Name == "healthStateExtIn" ? PortType.EXTERNAL_IN : PortType.READONLY_OUT,
                    PortValueType.GENERIC, field.Name == "torqueOut" ? "TORQUE_OUT" : Name(field.Name)));
        }
        var engine = new ReciprocatingSteamEngine(def); engine.SetGameParams(new SimGameParams(true, true, true, 1, 1));
        engine.healthStateExtIn.ExternalValueUpdate(1); return engine;
    }
    private static int Main(string[] args)
    {
        try
        {
            var wiring = JObject.Parse(File.ReadAllText(Path.Combine(args[0], "native-wiring.json")));
            var sources = ((JArray)wiring["references"]).ToDictionary(c => (string)c["reference"], c => (string)c["source"]);
            string[] order = wiring["execution"].Values<string>().ToArray();
            var rear = Engine(args[0], "steamEngine"); var front = Engine(args[0], "meyerLpEngine");
            DocksidePhasing.Seed(new SimComponent[] { rear, front });
            Check(rear.crankRotationReadOut.Value == 0 && front.crankRotationReadOut.Value == .125f, "Actual production phase seed staggers native engines by 45 degrees");
            var ports = new Dictionary<string, Port>();
            Port External(string id, float value)
            { var p = new Port(id, new PortDefinition(PortType.EXTERNAL_IN, PortValueType.GENERIC, "VALUE")); p.Value = value; ports.Add(id, p); return p; }
            var throttle = External("throttle.EXT_IN", 1); var reverser = External("reverser.REVERSER", .6f); var cocks = External("cylinderCock.EXT_IN", 1);
            External("boiler.PRESSURE", 16); External("boiler.TEMPERATURE", 200); External("boiler.OUTLET_STEAM_QUALITY", 1);
            var rpm = External("traction.WHEEL_RPM_EXT_IN", 80); External("lubricator.LUBRICATION_NORMALIZED", 1);
            foreach (var pair in new[] { ("steamEngine", rear), ("meyerLpEngine", front) })
                foreach (var field in typeof(ReciprocatingSteamEngine).GetFields(Flags))
                    if (field.FieldType == typeof(PortReference)) Link((PortReference)field.GetValue(pair.Item2), ports[sources[pair.Item1 + "." + Name(field.Name)]]);
            foreach (var pair in new[] { ("steamEngine", rear), ("meyerLpEngine", front) })
                foreach (var field in typeof(ReciprocatingSteamEngine).GetFields(Flags))
                    if (field.FieldType == typeof(Port)) ports.Add(pair.Item1 + "." + (field.Name == "torqueOut" ? "TORQUE_OUT" : Name(field.Name)), (Port)field.GetValue(pair.Item2));
            var sums = new Dictionary<string, MultiplePortSum>();
            foreach (string id in new[] { "docksideIntake", "docksideExhaust", "docksideMaxFlow", "meyerTorque" })
            {
                var refs = sources.Keys.Where(k => k.StartsWith(id + ".", StringComparison.Ordinal)).ToArray();
                var def = Detached<MultiplePortSumDefinition>(); def.ID = id;
                def.inputs = refs.Select(k => new PortReferenceDefinition(PortValueType.GENERIC, k.Substring(id.Length + 1))).ToArray();
                def.output = new PortDefinition(PortType.OUT, PortValueType.GENERIC, "OUT");
                var sum = new MultiplePortSum(def);
                for (int i = 0; i < refs.Length; i++) Link(sum.inputs[i], ports[sources[refs[i]]]);
                sums.Add(id, sum); ports.Add(id + ".OUT", sum.output);
            }
            void Tick(int count)
            {
                for (int i = 0; i < count; i++) foreach (string id in order)
                    if (id == "steamEngine") rear.Tick(.02f); else if (id == "meyerLpEngine") front.Tick(.02f); else if (sums.TryGetValue(id, out var sum)) sum.Tick(.02f);
            }
            Check(rear.GetType() == typeof(ReciprocatingSteamEngine) && front.GetType() == typeof(ReciprocatingSteamEngine), "Both engines must use the actual native simple implementation");
            Check(rear.numCylinders == 2 && front.numCylinders == 2 && rear.cylinderBore == front.cylinderBore && rear.pistonStroke == front.pistonStroke, "Equal two-cylinder engines from exported calibration");
            Tick(6000);
            Check(Math.Abs(((front.crankRotationReadOut.Value - rear.crankRotationReadOut.Value + 1) % 1) - .125f) < .0002f, "Native integration preserves the 45-degree unit stagger");
            Check(rear.cylinderCockFlowNormalizedReadOut.Value > 0 && front.cylinderCockFlowNormalizedReadOut.Value > 0, "One exported cab cock input opens both native engines");
            Check(rear.waterInCylindersNormalizedReadOut.Value < .001 && front.waterInCylindersNormalizedReadOut.Value < .001, "Both native cylinder sets purge water");
            Check(rear.cylinderTemperatureReadOut.Value > 100 && front.cylinderTemperatureReadOut.Value > 100, "Both engines heat directly from boiler supply");
            cocks.Value = 0; Tick(20);
            Check(rear.cylinderCockFlowNormalizedReadOut.Value == 0 && front.cylinderCockFlowNormalizedReadOut.Value == 0, "Cab closure stops both native cock flows");
            Check(rear.intakeFlowReadOut.Value > 0 && front.intakeFlowReadOut.Value > 0, "Both engines independently draw boiler steam");
            Check(ports[sources["steamConsumptionCalculator.ENGINE"]].Value == rear.intakeFlowReadOut.Value + front.intakeFlowReadOut.Value, "Actual native sum charges both intakes to the exported boiler consumer");
            Check(ports[sources["exhaust.EXHAUST_FLOW"]].Value == rear.exhaustFlowReadOut.Value + front.exhaustFlowReadOut.Value && front.exhaustFlowReadOut.Value > 0, "Both native exhausts feed the exported blast pipe");
            Check(ports[sources["exhaust.ENGINE_MAX_FLOW"]].Value == rear.maxFlowReadOut.Value + front.maxFlowReadOut.Value, "Blast normalization includes both engine capacities");
            Check(sums["meyerTorque"].output.Value == rear.torqueOut.Value + front.torqueOut.Value && rear.torqueOut.Value > 0 && front.torqueOut.Value > 0, "Both native torques reach the sum");
            throttle.Value = 0; Tick(20);
            Check(rear.intakeFlowReadOut.Value == 0 && front.intakeFlowReadOut.Value == 0, "Shared regulator closure stops both boiler intakes");
            throttle.Value = 1; reverser.Value = -.6f; rpm.Value = -80; Tick(500);
            Check(rear.torqueOut.Value < 0 && front.torqueOut.Value < 0, "Shared native reverser drives both engines backwards");
            Check(Math.Abs(((front.crankRotationReadOut.Value - rear.crankRotationReadOut.Value + 1) % 1) - .125f) < .0002f, "Reverse preserves physical quartering/unit phasing");
            rear.isCylinderCrackedReadOut.Value = 1;
            var rearSave = rear.GetSaveStateData(); var frontSave = front.GetSaveStateData();
            var savedRear = Engine(args[0], "steamEngine"); var savedFront = Engine(args[0], "meyerLpEngine");
            savedRear.SetSaveStateData(rearSave); savedFront.SetSaveStateData(frontSave);
            Check(savedRear.isCylinderCrackedReadOut.Value == 1 && savedFront.isCylinderCrackedReadOut.Value == 0, "Native per-engine crack saves remain independent");
            Console.WriteLine(checks + " native dockside engine/control/flow checks passed. No Unity scene, cab interaction, animation or driving executed.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
