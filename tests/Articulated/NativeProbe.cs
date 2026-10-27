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
    private static int checks;
    private static readonly BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static JObject calibration;
    private static void Check(bool value, string text) { if (!value) throw new Exception(text); checks++; }
    private static string Name(string field) => Regex.Replace(field.Replace("ReadOut", ""), "([a-z])([A-Z])", "$1_$2").ToUpperInvariant();
    private static T Def<T>(string id) where T : ReciprocatingSteamEngineDefinition
    {
        var def = (T)FormatterServices.GetUninitializedObject(typeof(T)); def.ID = id;
        foreach (var f in typeof(ReciprocatingSteamEngineDefinition).GetFields(Flags))
        {
            if ((f.FieldType == typeof(float) || f.FieldType == typeof(int)) && calibration[f.Name] != null) f.SetValue(def, calibration[f.Name].ToObject(f.FieldType));
            else if (f.FieldType == typeof(PortReferenceDefinition)) f.SetValue(def, new PortReferenceDefinition(PortValueType.GENERIC, Name(f.Name)));
            else if (f.FieldType == typeof(PortDefinition)) f.SetValue(def, new PortDefinition(f.Name == "torqueOut" ? PortType.OUT : f.Name == "healthStateExtIn" ? PortType.EXTERNAL_IN : PortType.READONLY_OUT, PortValueType.GENERIC, f.Name == "torqueOut" ? "TORQUE_OUT" : Name(f.Name)));
        }
        return def;
    }
    private static S060EngineBank Bank()
    {
        var bank = new S060EngineBank { Compound = new MeyerCycleOwner { Cycle = S060SteamCalibration.Meyer.CreateCycle() } };
        foreach (bool front in new[] { false, true })
        {
            string id = front ? "rs060FrontEngine" : "steamEngine";
            var def = Def<S060EngineSlotDefinition>(id); def.Bank = bank; def.IsFront = front;
            def.Simple = Def<ReciprocatingSteamEngineDefinition>(id);
            def.Dockside = Def<ReciprocatingSteamEngineDefinition>(id);
            def.Dockside.cylinderBore = S060SteamCalibration.Dockside.Bore; def.Dockside.pistonStroke = S060SteamCalibration.Dockside.Stroke;
            def.Dockside.steamChestVolume = S060SteamCalibration.Dockside.ChestLitres; def.Dockside.throttleMaxFlow = S060SteamCalibration.Dockside.MaxFlow;
            if(!front)
            {
                def.Shay=Def<ReciprocatingSteamEngineDefinition>(id);def.Shay.numCylinders=3;def.Shay.cylinderBore=.17f;
                def.Shay.pistonStroke=.150395513f;def.Shay.steamChestVolume=12;def.Shay.throttleMaxFlow=.8f;
            }
            def.Meyer = Def<MeyerStageDefinition>(id); def.Meyer.Owner = bank.Compound; def.Meyer.HighPressure = !front;
            def.Meyer.cylinderBore = (float)(front ? S060SteamCalibration.Meyer.LpBore : S060SteamCalibration.Meyer.HpBore);
            def.Meyer.pistonStroke = (float)S060SteamCalibration.Meyer.Stroke;
            def.Meyer.steamChestVolume = front ? S060SteamCalibration.Meyer.LpChestLitres : S060SteamCalibration.Meyer.HpChestLitres;
            def.Meyer.throttleMaxFlow = (float)(front ? S060SteamCalibration.Meyer.LpFlow : S060SteamCalibration.Meyer.HpFlow);
            def.InstantiateImplementation();
        }
        return bank;
    }
    private static Port Input(string id, float value) => new Port("cab", new PortDefinition(PortType.EXTERNAL_IN, PortValueType.GENERIC, id), value);
    private static Port throttle, cocks, rpm;
    private static readonly System.Collections.Generic.Dictionary<S060EngineBank,ConfigurableMultiplier> Gears=new System.Collections.Generic.Dictionary<S060EngineBank,ConfigurableMultiplier>();
    private static void Connect(S060EngineBank bank)
    {
        var values = new System.Collections.Generic.Dictionary<string, Port> {
            ["THROTTLE_CONTROL"] = throttle = Input("throttle", 1), ["REVERSER_CONTROL"] = Input("reverser", .6f),
            ["CYLINDER_COCK_CONTROL"] = cocks = Input("cocks", 1), ["INTAKE_PRESSURE"] = Input("pressure", 16),
            ["INTAKE_TEMPERATURE"] = Input("temperature", 200), ["INTAKE_QUALITY"] = Input("quality", 1),
            ["CRANK_RPM"] = rpm = Input("rpm", 80), ["LUBRICATION_NORMALIZED"] = Input("oil", 1)
        };
        var multiplier=(ConfigurableMultiplierDefinition)FormatterServices.GetUninitializedObject(typeof(ConfigurableMultiplierDefinition));multiplier.ID="nativeShayRpm";
        multiplier.aReader=new PortReferenceDefinition(PortValueType.RPM,"A");multiplier.bReader=new PortReferenceDefinition(PortValueType.GENERIC,"B");
        multiplier.mulReadOut=new PortDefinition(PortType.OUT,PortValueType.RPM,"OUT");
        var gear=new ConfigurableMultiplier(multiplier);gear.aReader.SetPortReference(rpm);gear.bReader.SetPortReference(Input("ratio",S060ShayEngine.Ratio));
        gear.Tick(0);bank.ShayRpm=gear.mulReadOut;Gears[bank]=gear;
        foreach (var slot in new[] { bank.Rear, bank.Front })
        {
            foreach (var reference in slot.GetAllPortReferences()) reference.SetPortReference(values[S060EngineState.Suffix(reference.id)]);
            slot.SetGameParams(new SimGameParams(true, true, true, 1, 1)); slot.InitializationAfterConnecting();
        }
    }
    private static void Tick(S060EngineBank bank, int count)
    { for (int i = 0; i < count; i++) { Gears[bank].Tick(.02f);bank.Rear.Tick(.02f); bank.Front.Tick(.02f); } }
    private static void Main(string[] args)
    {
        var json = JObject.Parse(File.ReadAllText(args[0])); calibration = json["MonoBehaviour"] as JObject ?? json;
        var bank = Bank(); Connect(bank); Tick(bank, 100);
        SemanticMapping(bank);
        CalibrationPrecision();
        SaveSchemaCompatibility(bank);
        var rearPort = bank.Rear.torqueOut; var cockPort = bank.Rear.cylinderCockControl.port;
        Check(bank.Rear.Selected.GetType() == typeof(ReciprocatingSteamEngine), "Stock delegates to the real native engine");
        Check(bank.Front.torqueOut.Value == 0 && bank.Front.intakeFlowReadOut.Value == 0, "Dormant front engine draws no steam and generates no torque");
        bank.Rear.Selected.steamChestSteamMass = .123f;
        bank.Rear.Selected.waterInCylindersDamageMeter = .3f; bank.Rear.Selected.isCylinderCrackedReadOut.Value = 1;
        float water = bank.Rear.Selected.waterInCylindersNormalizedReadOut.Value;
        float temperature = bank.Rear.Selected.temperatureInCylinders;
        var stockSave = S060EngineState.Save(bank.Rear.Selected);
        bank.Select(S060EngineMode.Dockside);
        Check(ReferenceEquals(bank.Rear.torqueOut, rearPort) && ReferenceEquals(bank.Rear.cylinderCockControl.port, cockPort), "Mode changes retain the same cab and traction port objects");
        Check(bank.Rear.Selected.isCylinderCrackedReadOut.Value == 1 && bank.Front.Selected.isCylinderCrackedReadOut.Value == 1, "Cylinder damage cannot be cured by selecting an articulated engine");
        Check(bank.Rear.Selected.waterInCylindersDamageMeter == .3f && bank.Rear.Selected.waterInCylindersNormalizedReadOut.Value == water && bank.Rear.Selected.temperatureInCylinders == temperature, "Primary wetness, damage and temperature survive switching");
        Tick(bank, 6000);
        Check(bank.Rear.cylinderCockFlowNormalizedReadOut.Value > 0 && bank.Front.cylinderCockFlowNormalizedReadOut.Value > 0, "Both Dockside cylinder sets respond to the existing cab cocks");
        Check(Math.Abs(((bank.Front.crankRotationReadOut.Value - bank.Rear.crankRotationReadOut.Value + 1) % 1) - .125f) < .0002, "Dockside units retain the 45-degree crank stagger");
        bank.Rear.healthStateExtIn.Value = .5f; bank.Front.healthStateExtIn.Value = .5f;
        bank.Rear.healthStateExtIn.Value = 1; bank.Front.healthStateExtIn.Value = 1;
        Check(bank.Rear.Engines.Where(e => e != null).All(e => e.isCylinderCrackedReadOut.Value == 0) && bank.Front.Engines.Where(e => e != null).All(e => e.isCylinderCrackedReadOut.Value == 0), "Native service repairs dormant engines too");
        bank.Select(S060EngineMode.Simple);
        Check(bank.Rear.Selected.steamChestSteamMass == .123f, "Stock chest steam survives while its circuit is dormant");
        bank.Select(S060EngineMode.Meyer); Tick(bank, 6000);
        Check(bank.Rear.exhaustFlowReadOut.Value == 0 && bank.Front.intakeFlowReadOut.Value == 0, "Meyer charges only HP boiler draw and sends only LP exhaust to blast");
        Check(bank.Rear.BlastCapacity.Value == 0 && bank.Front.BlastCapacity.Value == 4, "Meyer blast capacity counts only the LP outlet, without changing the HP max-flow port");
        Check(bank.Rear.cylinderCockFlowNormalizedReadOut.Value > 0 && bank.Front.cylinderCockFlowNormalizedReadOut.Value > 0, "Both Meyer cylinder sets respond to the same cab lever");
        Check(Math.Abs(bank.Compound.Cycle.MassResidual) < 1e-7 && Math.Abs(bank.Compound.Cycle.EnergyResidual) < .1, "Meyer compound circuit conserves mass and energy");
        var before = bank.Rear.GetSaveStateData().ToString(); double receiverMass = bank.Compound.Cycle.Receiver.Mass;
        bank.Select(S060EngineMode.Dockside); Tick(bank, 100); bank.Select(S060EngineMode.Meyer);
        Check(bank.Compound.Cycle.Receiver.Mass == receiverMass, "Switching out and back retains stored receiver steam");
        var mainSave = bank.Rear.GetSaveStateData(); var frontSave = bank.Front.GetSaveStateData();
        var loaded = Bank(); Connect(loaded); loaded.Rear.SetSaveStateData(mainSave); loaded.Front.SetSaveStateData(frontSave);
        Check(loaded.Mode == S060EngineMode.Meyer && JToken.DeepEquals(mainSave, loaded.Rear.GetSaveStateData()) && JToken.DeepEquals(frontSave, loaded.Front.GetSaveStateData()), "Every dormant circuit and the selected mode survive exact disk round trip");
        var corrupt = (JObject)mainSave.DeepClone(); corrupt["0"]["fields"]["steamChestSteamMass"] = -1;
        bool refused = false; try { loaded.Rear.SetSaveStateData(corrupt); } catch (InvalidOperationException) { refused = true; }
        Check(refused && JToken.DeepEquals(mainSave, loaded.Rear.GetSaveStateData()), "Corrupt engine state is rejected without partial mutation");
        cocks.Value = 0; throttle.Value = 0; Tick(loaded, 100);
        Check(loaded.Rear.intakeFlowReadOut.Value == 0 && loaded.Rear.cylinderCockFlowNormalizedReadOut.Value == 0 && loaded.Front.cylinderCockFlowNormalizedReadOut.Value == 0, "Shared cab closure stops both cocks and boiler draw after reload");
        var legacy = Bank(); Connect(legacy); legacy.Rear.SetSaveStateData(JObject.Parse("{\"cylCracked\":false}"));
        Check(legacy.Rear.isCylinderCrackedReadOut.Value == 0, "Legacy false crack key is migrated without DV's presence-key bug");
        legacy.Rear.SetSaveStateData(JObject.Parse("{\"cylCracked\":true}"));
        Check(legacy.Rear.isCylinderCrackedReadOut.Value == 1, "Legacy cylinder damage remains damaged");
        var savedReceiver = bank.Compound.Cycle.Receiver.Mass;
        var savedLedger = bank.Compound.Cycle.BoilerMass;
        var sameRear = bank.Rear; var sameFront = bank.Front;
        for (int i = 0; i < 100; i++)
        {
            bank.Select(S060EngineMode.Simple); bank.Select(S060EngineMode.Dockside); bank.Select(S060EngineMode.Meyer);
            Check(ReferenceEquals(sameRear, bank.Rear) && ReferenceEquals(sameFront, bank.Front) && bank.Compound.Cycle.Receiver.Mass == savedReceiver && bank.Compound.Cycle.BoilerMass == savedLedger,
                "Repeated in-place switches retain engine slots and stored circuit mass");
        }
        bank.Front.Selected.waterInCylindersNormalizedReadOut.Value = .23f;
        bank.Front.Selected.waterInCylindersDamageMeter = .7f;
        bank.Select(S060EngineMode.Simple);
        var dormantSave = bank.Rear.GetSaveStateData(); var dormantFront = bank.Front.GetSaveStateData();
        var dormantReload = Bank(); Connect(dormantReload); dormantReload.Rear.SetSaveStateData(dormantSave); dormantReload.Front.SetSaveStateData(dormantFront);
        dormantReload.Select(S060EngineMode.Dockside);
        Check(dormantReload.Front.Selected.waterInCylindersNormalizedReadOut.Value == .23f && dormantReload.Front.Selected.waterInCylindersDamageMeter == .7f,
            "Front cylinder condition survives a stock-layout save/reload and a different articulated selection");
        TractionEnvelope();
        CockCycles();
        checks += NativeSlipKernel.CheckThresholds();
        ShayChecks();
        Console.WriteLine(checks + " production engine-bank checks passed against actual B99 engines. No Unity scene or driving executed.");
    }

    private static void ShayChecks()
    {
        var bank=Bank();Connect(bank);bank.Select(S060EngineMode.Shay);cocks.Value=1;rpm.Value=60;
        var geared=(S060ShayEngine)bank.Main.Selected;geared.temperatureInCylinders=200;geared.waterInCylindersNormalizedReadOut.Value=0;Tick(bank,100);cocks.Value=0;
        Check(geared.numCylinders==3&&Math.Abs(geared.pistonRotationLead-1f/3)<1e-6,"Native Shay has three 120-degree cylinder channels");
        for(int i=0;i<2200;i++)
        {
            try{Tick(bank,1);}catch{Console.WriteLine("Shay failing tick="+i+" water="+geared.waterInCylindersNormalizedReadOut.Value+" temp="+geared.temperatureInCylinders+" pressure="+geared.steamChestPressureReadOut.Value);throw;}
            if(i%100==0)
            {
                Check(Math.Abs(bank.ShayRpm.Value-rpm.Value*S060ShayEngine.Ratio)<1e-4,"Native RPM multiplier retains the gearbox ratio");
                Check(Math.Abs(geared.GearTurns-Math.Floor(geared.GearTurns)-geared.crankRotation)<1e-5,"Full 43-turn gear phase stays aligned with the native crank");
                Check(bank.Auxiliary.torqueOut.Value==0&&bank.Auxiliary.intakeFlowReadOut.Value==0,"Single Shay engine does not draw steam or torque from a second unit");
                Check(Math.Abs(bank.Main.torqueOut.Value-geared.torqueOut.Value*S060ShayEngine.Ratio*S060ShayEngine.Efficiency)<.01,"Geared output trades RPM for torque with power loss, without an extra force model");
            }
        }
        var saved=bank.Main.GetSaveStateData();var auxiliary=bank.Auxiliary.GetSaveStateData();
        var restored=Bank();Connect(restored);restored.Main.SetSaveStateData(saved);restored.Auxiliary.SetSaveStateData(auxiliary);
        Check(restored.Mode==S060EngineMode.Shay&&JToken.DeepEquals(saved,restored.Main.GetSaveStateData()),"Three-cylinder state and full geared phase round trip exactly");
        cocks.Value=1;Tick(restored,50);Check(restored.Main.cylinderCockFlowNormalizedReadOut.Value>0,"Shay cylinder cocks draw native steam from the cab control");
        rpm.Value=-60;Tick(restored,100);var reverse=(S060ShayEngine)restored.Main.Selected;
        Check(reverse.GearTurns>=0&&reverse.GearTurns<43&&Math.Abs(reverse.GearTurns-Math.Floor(reverse.GearTurns)-reverse.crankRotation)<1e-5,"Reverse gear phase wraps without a discontinuity");
        restored.Select(S060EngineMode.Simple);Check(restored.Main.Selected.numCylinders==2,"Stock restores its original two-cylinder native engine");
        var legacy=(JObject)restored.Main.GetSaveStateData().DeepClone();legacy.Remove("3");var oldBank=Bank();Connect(oldBank);oldBank.Main.SetSaveStateData(legacy);
        Check(oldBank.Mode==S060EngineMode.Simple,"Pre-Shay saves may omit only the new dormant engine circuit");
        var corrupt=(JObject)saved.DeepClone();corrupt["3"]["gearTurns"]=-1;var before=bank.Main.GetSaveStateData();bool refused=false;
        try{bank.Main.SetSaveStateData(corrupt);}catch(InvalidOperationException){refused=true;}
        Check(refused&&JToken.DeepEquals(before,bank.Main.GetSaveStateData()),"Invalid geared phase rolls back the complete native slot state");
        var outOfPhase=(JObject)saved.DeepClone();outOfPhase["3"]["gearTurns"]=((double)outOfPhase["3"]["gearTurns"]+.25)%43;
        refused=false;try{bank.Main.SetSaveStateData(outOfPhase);}catch(InvalidOperationException){refused=true;}
        Check(refused&&JToken.DeepEquals(before,bank.Main.GetSaveStateData()),"Disagreeing native/visual crank saves roll back atomically");
        var malformed=(JObject)legacy.DeepClone();malformed["3"]="invalid";var oldBefore=oldBank.Main.GetSaveStateData();
        refused=false;try{oldBank.Main.SetSaveStateData(malformed);}catch(InvalidOperationException){refused=true;}
        Check(refused&&JToken.DeepEquals(oldBefore,oldBank.Main.GetSaveStateData()),"Only an absent dormant Shay state is legacy-compatible; malformed state is rejected");
    }

    // Retained pre-cleanup serializer as a compatibility oracle. It is
    // intentionally independent of the production map and its declared names.
    private static JObject LegacyEngineSave(ReciprocatingSteamEngine engine)
    {
        string[] names = { "steamChestSteamMass", "crankRotation", "prevCrankRotation", "torqueVelocity",
            "steamMassPerChuff", "admissionPressure", "waterInCylindersDamageMeter", "temperatureInCylinders",
            "cutoff", "condensationExpansionRatio", "isReverserForward" };
        var fields = new JObject();
        foreach (string name in names) fields[name] = JToken.FromObject(typeof(ReciprocatingSteamEngine).GetField(name, Flags).GetValue(engine));
        var ports = new JObject();
        foreach (var port in engine.GetAllPorts().Where(p => p != engine.healthStateExtIn)) ports[S060EngineState.Suffix(port.id)] = port.Value;
        var cylinders = new JArray();
        foreach (var cylinder in engine.cylinderData)
        {
            var saved = new JObject();
            foreach (var field in cylinder.GetType().GetFields(Flags).Where(f => f.FieldType == typeof(float) || f.FieldType == typeof(bool)))
                saved[field.Name] = JToken.FromObject(field.GetValue(cylinder));
            cylinders.Add(saved);
        }
        return new JObject { ["version"] = 1, ["fields"] = fields, ["ports"] = ports, ["cylinders"] = cylinders };
    }

    private static void SemanticMapping(S060EngineBank bank)
    {
        var mainState = bank.Main.GetSaveStateData(); var auxiliaryState = bank.Auxiliary.GetSaveStateData();
        Check(S060EngineMapping.MainId == "steamEngine" && S060EngineMapping.AuxiliaryId == "rs060FrontEngine", "Persisted/native slot IDs remain exact");
        Check(ReferenceEquals(bank.Main, bank.Rear) && ReferenceEquals(bank.Auxiliary, bank.Front), "Semantic aliases retain the original native slot objects");
        foreach (var mode in new[] { S060EngineMode.Simple, S060EngineMode.Meyer, S060EngineMode.Dockside })
        {
            bank.RestoreMode((int)mode, (int)mode);
            Check(bank.PhysicalFront.id == (mode == S060EngineMode.Dockside ? "rs060FrontEngine" : "steamEngine"), "Physical front maps explicitly in " + mode);
            Check(bank.PhysicalRear?.id == (mode == S060EngineMode.Simple ? null : mode == S060EngineMode.Meyer ? "rs060FrontEngine" : "steamEngine"), "Physical rear maps explicitly in " + mode);
            Check(bank.HighPressure?.id == (mode == S060EngineMode.Meyer ? "steamEngine" : null) && bank.LowPressure?.id == (mode == S060EngineMode.Meyer ? "rs060FrontEngine" : null), "Pressure-stage names apply only to compound mode");
            Check(S060EngineMapping.ExhaustId(mode) == (mode == S060EngineMode.Meyer ? "rs060FrontEngine" : "steamEngine") &&
                S060EngineMapping.AdditionalExhaustId(mode) == (mode == S060EngineMode.Dockside ? "rs060FrontEngine" : null), "Named exhaust mapping matches the original subscriptions");
        }
        bank.RestoreMode(0, 0);
        Check(JToken.DeepEquals(mainState, bank.Main.GetSaveStateData()) && JToken.DeepEquals(auxiliaryState, bank.Auxiliary.GetSaveStateData()), "Semantic views and mapping leave every saved key/value unchanged");
    }
    private static void CalibrationPrecision()
    {
        Check(S060SteamCalibration.Meyer.HpBore == .42 && S060SteamCalibration.Meyer.LpBore == .594 && S060SteamCalibration.Meyer.Stroke == .6707097 &&
            S060SteamCalibration.Meyer.HpChest == .2 && S060SteamCalibration.Meyer.LpChest == .4 && S060SteamCalibration.Meyer.HpFlow == 2 && S060SteamCalibration.Meyer.LpFlow == 4, "Shared cycle dimensions retain the original double constants exactly");
        Check(S060SteamCalibration.Meyer.NativeHpBore == .42f && S060SteamCalibration.Meyer.NativeLpBore == .594f &&
            S060SteamCalibration.Meyer.NativeStroke == 2 * .33535485f && S060SteamCalibration.Meyer.HpChestLitres == 200f && S060SteamCalibration.Meyer.LpChestLitres == 400f, "Shared proxy/native dimensions retain the original float constants exactly");
        Check(BitConverter.SingleToInt32Bits(S060SteamCalibration.Dockside.Stroke) == BitConverter.SingleToInt32Bits(.47091934f) &&
            BitConverter.SingleToInt32Bits(S060SteamCalibration.Dockside.AuthoredStroke) == BitConverter.SingleToInt32Bits(2 * (.33535485f * .5f / .712128f)), "Runtime and authored Dockside stroke preserve their distinct legacy float bit patterns");
    }
    private static void SaveSchemaCompatibility(S060EngineBank bank)
    {
        foreach (var engine in bank.Rear.Engines.Concat(bank.Front.Engines).Where(e => e != null && !(e is MeyerNativeStage)))
        {
            var legacy = LegacyEngineSave(engine);
            Check(legacy.ToString(Newtonsoft.Json.Formatting.None) == S060EngineState.Save(engine).ToString(Newtonsoft.Json.Formatting.None),
                "Explicit cached schema preserves exact pre-cleanup version1 JSON including native field names and order");
            S060EngineState.Load(engine, legacy);
            Check(JToken.DeepEquals(legacy, S060EngineState.Save(engine)), "Previous version1 engine data loads unchanged");
            foreach (string field in new[] { "isInletValveOpen", "positionWithinStroke", "crankOffset", "isCrankAboveHorizontal", "prevIsCrankAboveHorizontal", "admissionPressure" })
            {
                var invalid = (JObject)legacy.DeepClone();
                ((JObject)invalid["cylinders"][1]).Remove(field);
                bool refused = false;
                try { S060EngineState.Load(engine, invalid); } catch (InvalidOperationException) { refused = true; }
                Check(refused && JToken.DeepEquals(legacy, S060EngineState.Save(engine)), "Missing native cylinder " + field + " is rejected before mutation");
            }
            var wrong = (JObject)legacy.DeepClone(); wrong["cylinders"][1]["isInletValveOpen"] = 1;
            bool typeRefused = false;
            try { S060EngineState.Load(engine, wrong); } catch (InvalidOperationException) { typeRefused = true; }
            Check(typeRefused && JToken.DeepEquals(legacy, S060EngineState.Save(engine)), "Wrong cylinder scalar type is rejected before mutation");
        }
        foreach (var test in new[] {
            new { Type = typeof(SchemaProbe), Names = new[] { "absent" }, Bool = false, Exact = false },
            new { Type = typeof(SchemaProbe), Names = new[] { "Value" }, Bool = true, Exact = false },
            new { Type = typeof(SchemaProbe), Names = new[] { "Value" }, Bool = false, Exact = true } })
        {
            bool refused = false;
            try { S060EngineState.CheckedFields(test.Type, test.Names, _ => test.Bool, test.Exact); }
            catch (InvalidOperationException ex) { refused = ex.Message.Contains("engine-save schema is incompatible"); }
            Check(refused, "Missing/retyped/additional native scalar fields fail clearly rather than changing the saved schema");
        }
    }

    private sealed class SchemaProbe { public float Value = 0; public bool Added = false; }

    private static void CockCycles()
    {
        foreach (var mode in new[] { S060EngineMode.Meyer, S060EngineMode.Dockside })
        {
            var bank = Bank(); Connect(bank); bank.Select(mode);
            Tick(bank, 6000);
            var rearControl = bank.Rear.cylinderCockControl.port;
            var frontControl = bank.Front.cylinderCockControl.port;
            for (int cycle = 0; cycle < 10; cycle++)
            {
                cocks.Value = 0; Tick(bank, 100);
                Check(bank.Rear.cylinderCockFlowNormalizedReadOut.Value == 0 && bank.Front.cylinderCockFlowNormalizedReadOut.Value == 0,
                    "Closing cab cocks stops both hot engine flows on every cycle");
                cocks.Value = 1; Tick(bank, 100);
                Check(bank.Rear.cylinderCockFlowNormalizedReadOut.Value > 0 && bank.Front.cylinderCockFlowNormalizedReadOut.Value > 0,
                    "Reopening cab cocks resumes both hot engine flows on every cycle");
                Check(ReferenceEquals(rearControl, bank.Rear.cylinderCockControl.port) && ReferenceEquals(frontControl, bank.Front.cylinderCockControl.port) && ReferenceEquals(rearControl, frontControl),
                    "Repeated cock cycles retain one shared cab-control port");
            }
        }
    }

    private static void TractionEnvelope()
    {
        // Run the production bank and unmodified DV engines, rather than an
        // invented torque formula. This reports available force; it does not
        // pretend to reproduce ECS weather, braking or the player's controls.
        foreach (var mode in new[] { S060EngineMode.Meyer, S060EngineMode.Dockside, S060EngineMode.Shay })
        foreach (float speed in new[] { 1f, 10f, 40f, 80f })
        {
            var bank = Bank(); Connect(bank); bank.Select(mode);
            cocks.Value = 0; rpm.Value = speed;
            bank.Rear.reverserControl.port.Value = 1;
            foreach (var slot in new[] { bank.Rear, bank.Front }.Where(s=>s.Selected!=null))
            {
                slot.Selected.waterInCylindersNormalizedReadOut.Value = 0;
                slot.Selected.temperatureInCylinders = 200;
            }
            Tick(bank, 6000);
            double minimum = double.PositiveInfinity, maximum = 0, sum = 0;
            for (int i = 0; i < 3000; i++)
            {
                Tick(bank, 1);
                double force = (bank.Rear.torqueOut.Value + bank.Front.torqueOut.Value) /
                    (mode == S060EngineMode.Shay ? .5697024 : mode == S060EngineMode.Meyer ? .712128 : .5);
                minimum = Math.Min(minimum, force); maximum = Math.Max(maximum, force); sum += force;
            }
            Check(minimum >= 0 && maximum > 0 && !double.IsInfinity(maximum), "Articulated production engine delivers finite positive traction");
            Console.WriteLine($"Traction envelope {mode}, 16 bar absolute/full regulator/full gear/{speed} RPM: min={minimum:F1}, mean={sum / 3000:F1}, max={maximum:F1} N.");
        }
    }
}
