using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using LocoSim.Definitions;
using LocoSim.Implementations;
using Newtonsoft.Json.Linq;
using RearrangedS060;

internal static class NativeProbe
{
    private static readonly BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static int checks;
    private static void Check(bool value, string text) { if (!value) throw new Exception(text); checks++; }
    private static void Set(object obj, string name, object value)
    {
        for (var t = obj.GetType(); t != null; t = t.BaseType)
        { var f = t.GetField(name, Flags); if (f != null) { f.SetValue(obj, value); return; } }
        throw new MissingFieldException(name);
    }
    private static string Name(string field) => Regex.Replace(field.Replace("ReadOut", ""), "([a-z])([A-Z])", "$1_$2").ToUpperInvariant();
    private static MeyerStageDefinition Definition(JObject values, string id, MeyerCycleOwner owner, bool hp)
    {
        var def = (MeyerStageDefinition)FormatterServices.GetUninitializedObject(typeof(MeyerStageDefinition));
        def.ID = id; def.Owner = owner; def.HighPressure = hp;
        foreach (var f in typeof(ReciprocatingSteamEngineDefinition).GetFields(Flags))
        {
            if ((f.FieldType == typeof(float) || f.FieldType == typeof(int)) && values[f.Name] != null)
                f.SetValue(def, values[f.Name].ToObject(f.FieldType));
            else if (f.FieldType == typeof(PortReferenceDefinition)) f.SetValue(def, new PortReferenceDefinition(PortValueType.GENERIC, Name(f.Name)));
            else if (f.FieldType == typeof(PortDefinition))
                f.SetValue(def, new PortDefinition(f.Name == "torqueOut" ? PortType.OUT : f.Name == "healthStateExtIn" ? PortType.EXTERNAL_IN : PortType.READONLY_OUT,
                    PortValueType.GENERIC, f.Name == "torqueOut" ? "TORQUE_OUT" : Name(f.Name)));
        }
        return def;
    }
    private static void Value(PortReference reference, float value) => Set(reference, "defaultValue", value);
    private static void Link(PortReference reference, Port port) => Set(reference, "port", port);
    private static MeyerCycleOwner New(JObject hp, JObject lp)
    {
        var o = new MeyerCycleOwner { Cycle = new MeyerCompoundCycle((double)hp["cylinderBore"], (double)lp["cylinderBore"],
            (double)hp["pistonStroke"], (double)hp["steamChestVolume"] / 1000, (double)lp["steamChestVolume"] / 1000, (double)hp["throttleMaxFlow"]) };
        Definition(hp, "steamEngine", o, true).InstantiateImplementation(); Definition(lp, "meyerLpEngine", o, false).InstantiateImplementation();
        foreach (var e in new[] { o.Hp, o.Lp })
        {
            e.SetGameParams(new SimGameParams(true, true, true, 1, 1));
            Value(e.throttleControl, 1); Value(e.reverserControl, .6f); Value(e.crankRpm, 80); Value(e.cylinderCockControl, 1);
            Value(e.intakeQuality, 1); Value(e.lubricationNormalized, 1); e.healthStateExtIn.ExternalValueUpdate(1);
        }
        Value(o.Hp.intakePressure, 16); Value(o.Hp.intakeTemperature, 200);
        Link(o.Lp.intakePressure, o.Hp.exhaustPressureReadOut); Link(o.Lp.intakeTemperature, o.Hp.exhaustTemperatureReadOut);
        return o;
    }
    private static void Tick(MeyerCycleOwner o, int count)
    { for (int i = 0; i < count; i++) { o.Hp.Tick(.02f); o.Lp.Tick(.02f); } }
    private static int Main(string[] args)
    {
        try
        {
            var hpDocument = JObject.Parse(File.ReadAllText(Path.Combine(args[0], "steamEngine-calibration.json")));
            var lpDocument = JObject.Parse(File.ReadAllText(Path.Combine(args[0], "meyerLpEngine-calibration.json")));
            var hp = hpDocument["MonoBehaviour"] as JObject ?? hpDocument;
            var lp = lpDocument["MonoBehaviour"] as JObject ?? lpDocument;
            Check((float)hp["cylinderBore"] == S060SteamCalibration.Meyer.NativeHpBore && (float)lp["cylinderBore"] == S060SteamCalibration.Meyer.NativeLpBore &&
                (float)hp["pistonStroke"] == S060SteamCalibration.Meyer.NativeStroke && (float)lp["pistonStroke"] == S060SteamCalibration.Meyer.NativeStroke,
                "Exported HP/LP dimensions match the shared native-float calibration");
            Check((float)hp["steamChestVolume"] == S060SteamCalibration.Meyer.HpChestLitres && (float)lp["steamChestVolume"] == S060SteamCalibration.Meyer.LpChestLitres &&
                (float)hp["throttleMaxFlow"] == (float)S060SteamCalibration.Meyer.HpFlow && (float)lp["throttleMaxFlow"] == (float)S060SteamCalibration.Meyer.LpFlow,
                "Exported chest/flow calibration matches runtime definition constants");
            var o = New(hp, lp);
            Check(o.Hp.waterInCylindersNormalizedReadOut.Value == 1 && o.Lp.waterInCylindersNormalizedReadOut.Value == 1, "Both native cylinder sets begin wet");
            var lever = new Port("cab", new PortDefinition(PortType.EXTERNAL_IN, PortValueType.CONTROL, "cab"));
            Link(o.Hp.cylinderCockControl, lever); Link(o.Lp.cylinderCockControl, lever); lever.Value = 1;
            Tick(o, 6000);
            Check(o.Hp.cylinderCockFlowNormalizedReadOut.Value > 0 && o.Lp.cylinderCockFlowNormalizedReadOut.Value > 0, "One actual cab port opens both native cock readouts");
            Check(o.Hp.waterInCylindersNormalizedReadOut.Value < .001 && o.Lp.waterInCylindersNormalizedReadOut.Value < .001, "Both actual native water simulations purge through the shared cab control");
            Check(o.Hp.cylinderTemperatureReadOut.Value > 100 && o.Lp.cylinderTemperatureReadOut.Value > 50, "Each native cylinder set heats from its own supply temperature");
            Check(o.Hp.generatedMechanicalDamageReadOut.Value > 0 && o.Lp.generatedMechanicalDamageReadOut.Value > 0, "Both physical engine sets retain native lubrication/wear integration");
            lever.Value = 0; Tick(o, 1);
            Check(o.Hp.cylinderCockFlowNormalizedReadOut.Value == 0 && o.Lp.cylinderCockFlowNormalizedReadOut.Value == 0, "Cab closure stops both native cock flow ports");
            double intake = o.Cycle.BoilerMass; Value(o.Hp.throttleControl, 0); Tick(o, 200);
            Check(o.Cycle.BoilerMass == intake, "HP regulator closure stops boiler draw while receiver is reused");
            Value(o.Hp.throttleControl, 1); Tick(o, 2000);
            o.Hp.waterInCylindersNormalizedReadOut.Value = .5f; o.Lp.waterInCylindersNormalizedReadOut.Value = .1f;
            o.Hp.isCylinderCrackedReadOut.Value = 1; o.Lp.isCylinderCrackedReadOut.Value = 0;
            var hpSave = o.Hp.GetSaveStateData(); var lpSave = o.Lp.GetSaveStateData();
            var restored = New(hp, lp); restored.Hp.SetSaveStateData(hpSave); restored.Lp.SetSaveStateData(lpSave);
            Check(restored.Hp.waterInCylindersNormalizedReadOut.Value == .5f && restored.Lp.waterInCylindersNormalizedReadOut.Value == .1f, "Distinct water states survive native save/load");
            Check(restored.Hp.isCylinderCrackedReadOut.Value == 1 && restored.Lp.isCylinderCrackedReadOut.Value == 0, "Independent cylinder cracks survive save/load");
            Check(restored.Cycle.Receiver.Mass == o.Cycle.Receiver.Mass && restored.Cycle.Receiver.Energy == o.Cycle.Receiver.Energy, "Receiver saves without pressure/mass reset or refill");
            Check(restored.Hp.crankRotationReadOut.Value == o.Hp.crankRotationReadOut.Value && restored.Lp.crankRotationReadOut.Value == o.Lp.crankRotationReadOut.Value, "Both native crank phases survive save/load");
            Value(restored.Hp.cylinderCockControl, 0); Value(restored.Lp.cylinderCockControl, 0);
            Tick(o, 1); Tick(restored, 1);
            Check(Math.Abs(restored.Cycle.StoredEnergy - o.Cycle.StoredEnergy) < 1e-6, "Restored compound follows the same next-tick energy trajectory");
            double before = restored.Cycle.StoredMass;
            var future = (JObject)hpSave.DeepClone(); future["meyerVersion"] = 99;
            bool rejected = false; try { restored.Hp.SetSaveStateData(future); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected && restored.Cycle.StoredMass == before, "Unknown save version refuses replacement without changing stored steam");
            var corrupt = (JObject)hpSave.DeepClone(); ((JArray)corrupt["ledger"])[0] = 0;
            rejected = false; try { restored.Hp.SetSaveStateData(corrupt); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected && restored.Cycle.StoredMass == before, "Inconsistent saved ledger is rejected atomically");
            double energyBefore = restored.Cycle.StoredEnergy; restored.Hp.Tick(0); restored.Lp.Tick(0);
            Check(restored.Cycle.StoredEnergy == energyBefore, "Paused zero-duration ticks cannot consume or create steam");
            Check(Math.Abs(o.Cycle.MassResidual) < 1e-8 && Math.Abs(o.Cycle.EnergyResidual) < .01, "Native adapter preserves compound mass/energy accounting");
            Console.WriteLine(checks + " actual B99 native engine/port/save checks passed. No Unity scene, cab interaction or effect playback executed.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
