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
    private static readonly JArray envelope = new JArray();
    private static readonly JObject meyerStarting = new JObject();
    private static readonly JArray shayOperating = new JArray();
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
                def.Shay=Def<ReciprocatingSteamEngineDefinition>(id);def.Shay.numCylinders=3;def.Shay.cylinderBore=S060SteamCalibration.Shay.Bore;
                def.Shay.pistonStroke=S060SteamCalibration.Shay.Stroke;def.Shay.steamChestVolume=S060SteamCalibration.Shay.ChestLitres;def.Shay.throttleMaxFlow=S060SteamCalibration.Shay.MaxFlow;
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
        Check((bool?)json["source"]?["nativeDefinition"] == true && (string)json["source"]?["prefabPath"] == "LocoS060/[sim]/steamEngine", "Calibration must come from the exact stock S060 prefab, not an authored donor");
        Check(calibration.Value<int>("numCylinders") == 2 && Math.Abs(calibration.Value<double>("cylinderBore") - .42) < .000001 && Math.Abs(calibration.Value<double>("pistonStroke") - .61) < .000001, "Stock S060 engine dimensions are measured native values");
        var bank = Bank(); Connect(bank); Tick(bank, 100);
        SemanticMapping(bank);
        CalibrationPrecision();
        SaveSchemaCompatibility(bank);
        if (args.Length > 2) CrossVersionCondition(args[2]);
        if (args.Length > 4) ShayFlowSaveCompatibility(args[4]);
        MeyerStartingChecks();
        MeyerMechanicsContinuationChecks();
        MeyerIndependentSpeedChecks();
        if (args.Length > 3) MeyerLegacyStartingSave(args[3]);
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
        AuthoredAcceptance();
        ShayOperatingCurve();
        if (args.Length > 1) File.WriteAllText(args[1], new JObject { ["nativeCalibration"] = json, ["envelope"] = envelope, ["meyerStarting"] = meyerStarting, ["shayOperating"] = shayOperating,
            ["limitations"] = "Prescribed road speed, fixed supply 16 bar absolute/200 C plus Shay 13-bar speed sensitivity and one Dockside 13-bar point, full regulator/cutoff; no boiler, train, ECS adhesion, gradient or brakes simulated. Grade budgets use declared dry coefficient .20 and rolling allowance .002 of weight, not measured route resistance. Engine steam flow excludes the native boiler water-consumption multiplier." }.ToString());
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
        Check(Math.Abs(43f / S060ShayEngine.Ratio - 10f) < .00001f, "Forty-three saved engine turns complete ten wheel turns with the new reduction");
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
    private static S060EngineBank EmptyMeyer(float pressure, float regulator, float reverse, float wheelRpm, float phase = 0)
    {
        var bank = Bank(); Connect(bank); bank.Select(S060EngineMode.Meyer);
        Check(bank.Compound.Cycle.StoredMass == 0 && bank.Compound.Cycle.StoredEnergy == 0,
            "Starting fixture begins with a genuinely empty compound circuit");
        bank.Main.intakePressure.port.Value = pressure;
        bank.Main.throttleControl.port.Value = regulator;
        bank.Main.reverserControl.port.Value = reverse;
        bank.Main.crankRpm.port.Value = wheelRpm;
        bank.Main.cylinderCockControl.port.Value = 0;
        foreach (var slot in new[] { bank.Main, bank.Auxiliary })
        {
            slot.Selected.temperatureInCylinders = 200;
            slot.Selected.waterInCylindersNormalizedReadOut.Value = 0;
            slot.Selected.crankRotation = (phase + (ReferenceEquals(slot, bank.Auxiliary) ? .125f : 0)) % 1;
        }
        return bank;
    }
    private static void MeyerBalance(S060EngineBank bank, string scenario)
    {
        var cycle = bank.Compound.Cycle;
        Check(Math.Abs(cycle.MassResidual) < 1e-7 && Math.Abs(cycle.EnergyResidual) < Math.Max(.01, cycle.BoilerEnergy * 1e-9),
            "Native compound starting preserves mass/energy accounting: " + scenario);
        Check(cycle.Hp.Chest.Mass >= 0 && cycle.Lp.Chest.Mass >= 0 && cycle.Receiver.Mass >= 0 &&
            cycle.Hp.Chest.Energy >= 0 && cycle.Lp.Chest.Energy >= 0 && cycle.Receiver.Energy >= 0,
            "Native compound starting cannot create negative vessels: " + scenario);
    }
    private static float NativeHpStaticTorque(S060EngineBank bank, float phase, float reverse)
    {
        var definition = Def<ReciprocatingSteamEngineDefinition>("nativeStartingReference");
        definition.cylinderBore = S060SteamCalibration.Meyer.NativeHpBore;
        definition.pistonStroke = S060SteamCalibration.Meyer.NativeStroke;
        var native = new ReciprocatingSteamEngine(definition);
        foreach (var reference in native.GetAllPortReferences())
            reference.SetPortReference(bank.Main.GetAllPortReferences().Single(r => S060EngineState.Suffix(r.id) == S060EngineState.Suffix(reference.id)).port);
        native.crankRotation = native.prevCrankRotation = phase;
        native.isReverserForward = reverse >= 0;
        native.cutoff = definition.minCutoff + (definition.maxCutoff - definition.minCutoff) * Math.Abs(reverse);
        var type = typeof(ReciprocatingSteamEngine);
        native.condensationExpansionRatio = (float)type.GetMethod("CondensationExpansionRatio", Flags).Invoke(native, null);
        for (int i = 0; i < native.numCylinders; i++) native.cylinderData[i].SimulateCylinder(native.pistonRotationLead * i);
        double pressure = bank.Compound.Cycle.Hp.Chest.Pressure;
        double back = Math.Max(MeyerCompoundCycle.Atmosphere, bank.Compound.Cycle.Receiver.Pressure);
        return (float)type.GetMethod("SimulateInstantaneousTorque", Flags).Invoke(native,
            new object[] { (float)(1 + Math.Max(0, pressure - back) / 100000) });
    }
    private static void MeyerStartingChecks()
    {
        var oldThrottle = throttle; var oldCocks = cocks; var oldRpm = rpm;
        try
        {
            var starts = new JArray(); int controlCases = 0, phaseCases = 0;
            foreach (float pressure in new[] { 0f, 13f, 16f })
            foreach (float regulator in new[] { 0f, .1f, .3f, 1f })
            foreach (float reverse in new[] { -1f, 0f, 1f })
            foreach (float wheelRpm in new[] { -.000001f, 0f, .000001f })
            {
                var b = EmptyMeyer(pressure, regulator, reverse, wheelRpm, .0625f); Tick(b, 500); controlCases++;
                Check(!float.IsNaN(b.Main.torqueOut.Value) && !float.IsInfinity(b.Main.torqueOut.Value) &&
                    !float.IsNaN(b.Auxiliary.torqueOut.Value) && !float.IsInfinity(b.Auxiliary.torqueOut.Value),
                    "Cold HP-only zero/tiny-RPM controls produce finite native force");
                if (regulator == 0 || pressure == 0)
                    Check(b.Compound.Cycle.StoredMass == 0 && b.Compound.Cycle.Work == 0 && b.Main.torqueOut.Value == 0 && b.Auxiliary.torqueOut.Value == 0,
                        "An empty unpressurised or regulator-closed Meyer cannot pull");
                else if (reverse != 0)
                    Check(b.Main.torqueOut.Value * reverse > 0 && b.Auxiliary.torqueOut.Value == 0,
                        "An empty Meyer starts on HP in either gear direction without manufacturing LP steam");
                if (wheelRpm == 0) Check(b.Compound.Cycle.Work == 0 && b.Compound.Cycle.Hp.Exhaust == 0 && b.Compound.Cycle.Lp.Exhaust == 0,
                    "Static starting force consumes no swept exhaust or mechanical work");
                MeyerBalance(b, "empty controls");
                if (regulator == 1 && reverse != 0 && wheelRpm == 0 && pressure > 0)
                    starts.Add(new JObject { ["pressureBarAbs"] = pressure, ["reverser"] = reverse,
                        ["hpTorqueNm"] = b.Main.torqueOut.Value, ["lpTorqueNm"] = b.Auxiliary.torqueOut.Value });
            }
            var phases = new JArray();
            foreach (float pressure in new[] { 13f, 16f })
            foreach (float reverse in new[] { -1f, 0f, 1f })
            {
                float minimum = float.PositiveInfinity, maximum = float.NegativeInfinity; int zero = 0;
                for (int p = 0; p < 64; p++)
                {
                    float phase = p / 64f;
                    var b = EmptyMeyer(pressure, 1, reverse, 0, phase); Tick(b, 500); phaseCases++;
                    float expected = NativeHpStaticTorque(b, phase, reverse), actual = b.Main.torqueOut.Value;
                    Check(Math.Abs(actual - expected) <= Math.Max(.02f, Math.Abs(expected) * .000002f),
                        "Meyer static HP torque follows actual native cylinder/valve geometry at phase " + phase +
                        "; pressure=" + pressure + ", gear=" + reverse + ", expected=" + expected + ", actual=" + actual);
                    Check(b.Auxiliary.torqueOut.Value == 0 && b.Compound.Cycle.Work == 0,
                        "Static phase sampling neither bypasses the receiver nor creates mechanical work");
                    minimum = Math.Min(minimum, actual); maximum = Math.Max(maximum, actual); if (Math.Abs(actual) < .1) zero++;
                }
                Check(maximum - minimum > 1000, "Meyer low-speed torque is phase-dependent rather than the old fixed average");
                if (reverse == 0) Check(zero > 0 && zero < 64, "Native centred-gear five-percent cutoff retains both closed and short admission phases");
                phases.Add(new JObject { ["pressureBarAbs"] = pressure, ["reverser"] = reverse, ["minimumHpTorqueNm"] = minimum,
                    ["maximumHpTorqueNm"] = maximum, ["zeroTorquePhases"] = zero });
            }
            var moving = EmptyMeyer(16, 1, 1, 40, .125f); Tick(moving, 3000);
            Check(moving.Main.torqueOut.Value > 0 && moving.Auxiliary.torqueOut.Value > 0, "Both native-shaped compound stages work after the receiver fills");
            float hpPhase = moving.Main.crankRotationReadOut.Value, lpPhase = moving.Auxiliary.crankRotationReadOut.Value;
            Tick(moving, 1);
            Check(Math.Abs(moving.Main.crankRotationReadOut.Value - ((hpPhase + 40f / 60 * .02f) % 1)) < .00001 &&
                Math.Abs(moving.Auxiliary.crankRotationReadOut.Value - ((lpPhase + 40f / 60 * .02f) % 1)) < .00001,
                "Each compound native crank advances exactly once per common tick");
            moving.Main.cylinderCockControl.port.Value = 1; Tick(moving, 100);
            Check(moving.Main.cylinderCockFlowNormalizedReadOut.Value > 0 && moving.Auxiliary.cylinderCockFlowNormalizedReadOut.Value > 0,
                "One cab command still vents both working native compound cylinder groups");
            moving.Main.cylinderCockControl.port.Value = 0; moving.Main.throttleControl.port.Value = 0;
            double boilerBefore = moving.Compound.Cycle.BoilerMass, storedBefore = moving.Compound.Cycle.StoredMass;
            Tick(moving, 6000);
            Check(moving.Compound.Cycle.BoilerMass == boilerBefore && moving.Compound.Cycle.StoredMass < storedBefore &&
                Math.Abs(moving.Main.torqueOut.Value) < 1 && Math.Abs(moving.Auxiliary.torqueOut.Value) < 1,
                "Closed-regulator native compound rundown is finite and never draws replacement boiler steam");
            MeyerBalance(moving, "native rundown");
            meyerStarting["controlCases"] = controlCases; meyerStarting["nativePhaseCases"] = phaseCases;
            meyerStarting["coldHpOnlyStarts"] = starts; meyerStarting["nativePhaseSummaries"] = phases;
            meyerStarting["fullBoilerAndGameplayValidated"] = false;
            Console.WriteLine("Meyer genuine-empty controls " + controlCases + ", native static phases " + phaseCases + ": passed; finite receiver/cock/rundown checks passed.");
        }
        finally { throttle = oldThrottle; cocks = oldCocks; rpm = oldRpm; }
    }
    private static void MeyerLegacyStartingSave(string fixture)
    {
        var saved = JObject.Parse(File.ReadAllText(fixture));
        var oldThrottle = throttle; var oldCocks = cocks; var oldRpm = rpm;
        try
        {
            var b = Bank(); Connect(b); var main = (JObject)saved["main"]; var auxiliary = (JObject)saved["auxiliary"];
            b.Main.SetSaveStateData(main); b.Auxiliary.SetSaveStateData(auxiliary);
            Check(b.Mode == S060EngineMode.Meyer && JToken.DeepEquals(main, JObject.Parse(b.Main.GetSaveStateData().ToString())) &&
                JToken.DeepEquals(auxiliary, JObject.Parse(b.Auxiliary.GetSaveStateData().ToString())),
                "Actual old hot-receiver Meyer save restores every stored phase, condition, vessel and ledger unchanged before its first tick");
            double boilerMass = b.Compound.Cycle.BoilerMass;
            b.Main.throttleControl.port.Value = 0; b.Main.cylinderCockControl.port.Value = 0; b.Main.crankRpm.port.Value = 0;
            double work = b.Compound.Cycle.Work; Tick(b, 500);
            Check(b.Compound.Cycle.Work == work && b.Compound.Cycle.BoilerMass == boilerMass,
                "An old hot stopped circuit produces no extra work or boiler consumption while stationary");
            b.Main.reverserControl.port.Value = -1; b.Main.crankRpm.port.Value = -40; Tick(b, 6000);
            Check(b.Compound.Cycle.BoilerMass == boilerMass && Math.Abs(b.Main.torqueOut.Value) < 1 && Math.Abs(b.Auxiliary.torqueOut.Value) < 1,
                "An old stored receiver runs down safely under reverse native motion after loading");
            MeyerBalance(b, "actual old receiver save");
            meyerStarting["actualOldMeyerSaveRoundTrip"] = true;
            meyerStarting["oldSaveProducerSha256"] = saved["producerAssemblySha256"];
        }
        finally { throttle = oldThrottle; cocks = oldCocks; rpm = oldRpm; }
    }
    private static void MeyerIndependentSpeedChecks()
    {
        var oldThrottle = throttle; var oldCocks = cocks; var oldRpm = rpm;
        void Bind(S060EngineSlot slot, float speed)
        {
            var input = Input(slot.id + ".independentFixtureRpm", speed);
            slot.crankRpm.port = input; slot.Selected.crankRpm.port = input;
        }
        float Cycle(float phase) => (phase % 1 + 1) % 1;
        float PhaseError(float expected, float actual) => Math.Min(Cycle(actual - expected), Cycle(expected - actual));
        int cases = 0;
        try
        {
            foreach (var speeds in new[] { (hp:0f, lp:80f), (hp:80f, lp:0f), (hp:20f, lp:140f),
                (hp:140f, lp:20f), (hp:-20f, lp:-140f), (hp:20f, lp:-140f), (hp:-140f, lp:20f) })
            {
                var source = EmptyMeyer(16, 1, .3f, 40); Tick(source, 3000);
                Bind(source.Main, speeds.hp); Bind(source.Auxiliary, speeds.lp);
                float hpBefore = source.Main.Selected.crankRotation, lpBefore = source.Auxiliary.Selected.crankRotation;
                source.Main.Tick(.02f);
                double massAfterHp = source.Compound.Cycle.BoilerMass, workAfterHp = source.Compound.Cycle.Work;
                source.Auxiliary.Tick(.02f);
                Check(PhaseError(Cycle(hpBefore + speeds.hp / 60 * .02f), source.Main.crankRotationReadOut.Value) < 1e-6 &&
                    PhaseError(Cycle(lpBefore + speeds.lp / 60 * .02f), source.Auxiliary.crankRotationReadOut.Value) < 1e-6,
                    "Each native Meyer stage advances exactly once at its own RPM: " + speeds);
                Check(source.Compound.Cycle.BoilerMass == massAfterHp && source.Compound.Cycle.Work == workAfterHp,
                    "LP publication does not tick the shared compound circuit or consume boiler steam twice");
                Tick(source, 500);
                Check(!float.IsNaN(source.Main.torqueOut.Value) && !float.IsInfinity(source.Main.torqueOut.Value) &&
                    !float.IsNaN(source.Auxiliary.torqueOut.Value) && !float.IsInfinity(source.Auxiliary.torqueOut.Value),
                    "Actual native mechanics remain finite with unequal/stopped/opposed group speeds");
                Check(source.Main.exhaustFlowReadOut.Value == 0 && source.Auxiliary.intakeFlowReadOut.Value == 0,
                    "Independent speeds retain HP-only boiler draw and LP-only chimney exhaust");
                if (speeds.lp == 0) Check(source.Compound.Cycle.Lp.Exhaust == 0,
                    "A stationary native LP stage does not use HP speed to sweep steam");
                MeyerBalance(source, "native independent speeds");

                var primary = JObject.Parse(source.Main.GetSaveStateData().ToString());
                var auxiliary = JObject.Parse(source.Auxiliary.GetSaveStateData().ToString());
                var loaded = Bank(); Connect(loaded); loaded.Main.SetSaveStateData(primary); loaded.Auxiliary.SetSaveStateData(auxiliary);
                Check(JToken.DeepEquals(primary, JObject.Parse(loaded.Main.GetSaveStateData().ToString())) &&
                    JToken.DeepEquals(auxiliary, JObject.Parse(loaded.Auxiliary.GetSaveStateData().ToString())),
                    "Unequal-speed hot save restores exact independent phases, cylinder pressures, vessels and ledger");
                Bind(loaded.Main, speeds.hp); Bind(loaded.Auxiliary, speeds.lp);
                foreach (var b in new[] { source, loaded })
                {
                    b.Main.throttleControl.port.Value = 0;
                    b.Main.reverserControl.port.Value = .3f;
                    b.Main.cylinderCockControl.port.Value = 0;
                }
                Tick(source, 1); Tick(loaded, 1);
                Check(Math.Abs(source.Main.torqueOut.Value - loaded.Main.torqueOut.Value) < .02 &&
                    Math.Abs(source.Auxiliary.torqueOut.Value - loaded.Auxiliary.torqueOut.Value) < .02 &&
                    Math.Abs(source.Compound.Cycle.Work - loaded.Compound.Cycle.Work) < .01 &&
                    Math.Abs(source.Compound.Cycle.StoredEnergy - loaded.Compound.Cycle.StoredEnergy) < .01,
                    "Hot unequal-speed continuation preserves first-tick mechanical work and both native torques: " + speeds +
                    " HP=" + source.Main.torqueOut.Value + "/" + loaded.Main.torqueOut.Value +
                    " LP=" + source.Auxiliary.torqueOut.Value + "/" + loaded.Auxiliary.torqueOut.Value +
                    " work=" + source.Compound.Cycle.Work + "/" + loaded.Compound.Cycle.Work +
                    " energy=" + source.Compound.Cycle.StoredEnergy + "/" + loaded.Compound.Cycle.StoredEnergy);
                MeyerBalance(loaded, "reloaded unequal-speed native stages");
                cases++;
            }
            meyerStarting["independentStageSpeeds"] = true;
            meyerStarting["independentSpeedCases"] = cases;
            Console.WriteLine("Meyer independent native HP/LP speeds, once-only circuit tick and hot save continuation: " + cases + " cases passed.");
        }
        finally { throttle = oldThrottle; cocks = oldCocks; rpm = oldRpm; }
    }
    private static void MeyerMechanicsContinuationChecks()
    {
        var oldThrottle = throttle; var oldCocks = cocks; var oldRpm = rpm;
        try
        {
            int cases = 0, invalidCases = 0;
            S060EngineBank corruptionTarget = null;
            foreach (float phase in new[] { .3f, .4f })
            foreach (float speed in new[] { 0f, -.000001f, .000001f })
            {
                var source = EmptyMeyer(16, 1, .3f, 40); Tick(source, 3000);
                // Reach the stopped phase through native motion so the closed
                // chamber pressure has a real valve history, not a teleported
                // crank with an artificial pressure/end mismatch.
                float remaining = (phase - source.Main.crankRotationReadOut.Value + 1) % 1;
                int approach = 0;
                while (remaining > 40f / 60 * .02f + .000001f && approach++ < 80)
                { Tick(source, 1); remaining = (phase - source.Main.crankRotationReadOut.Value + 1) % 1; }
                Check(approach < 80, "Native motion reaches the selected closed-valve stop within one turn");
                source.Main.crankRpm.port.Value = remaining * 60 / .02f; Tick(source, 1);
                source.Main.crankRpm.port.Value = 0; source.Main.throttleControl.port.Value = 0;
                Tick(source, 1);
                var primary = JObject.Parse(source.Main.GetSaveStateData().ToString());
                var auxiliary = JObject.Parse(source.Auxiliary.GetSaveStateData().ToString());
                foreach (var data in new[] { primary, auxiliary })
                    Check(data["1"].Value<int>("mechanicsVersion") == 1 && data["1"]["cylinderAdmissionBar"] is JArray pressures && pressures.Count == 2,
                        "A ticked Meyer save retains both native signed cylinder admission pressures");
                var loaded = Bank(); Connect(loaded); loaded.Main.SetSaveStateData(primary); loaded.Auxiliary.SetSaveStateData(auxiliary);
                Check(JToken.DeepEquals(primary, JObject.Parse(loaded.Main.GetSaveStateData().ToString())) &&
                    JToken.DeepEquals(auxiliary, JObject.Parse(loaded.Auxiliary.GetSaveStateData().ToString())),
                    "New Meyer mechanics save restores exactly before its first tick");
                foreach (var b in new[] { source, loaded })
                {
                    b.Main.throttleControl.port.Value = 0; b.Main.reverserControl.port.Value = .3f;
                    b.Main.crankRpm.port.Value = speed; b.Main.cylinderCockControl.port.Value = 0;
                }
                Tick(source, 1); Tick(loaded, 1);
                Check(Math.Abs(source.Main.torqueOut.Value - loaded.Main.torqueOut.Value) < .02 &&
                    Math.Abs(source.Auxiliary.torqueOut.Value - loaded.Auxiliary.torqueOut.Value) < .02,
                    "First-tick Meyer torque at a closed partial-cutoff phase survives save/reload: " + phase + "/" + speed);
                Check(Math.Abs(source.Compound.Cycle.Work - loaded.Compound.Cycle.Work) < .01 &&
                    Math.Abs(source.Compound.Cycle.StoredEnergy - loaded.Compound.Cycle.StoredEnergy) < .01,
                    "Restoring native cylinder mechanics does not alter vessel/work continuation");
                MeyerBalance(loaded, "new mechanics save continuation");
                corruptionTarget = loaded; cases++;
            }
            var before = JObject.Parse(corruptionTarget.Main.GetSaveStateData().ToString());
            var beforeAux = JObject.Parse(corruptionTarget.Auxiliary.GetSaveStateData().ToString());
            foreach (bool auxiliarySlot in new[] { false, true })
            foreach (string mutation in new[] { "version", "missingPressure", "missingVersion", "short", "long", "boolean", "string", "nan", "infinity" })
            {
                var corrupt = (JObject)(auxiliarySlot ? beforeAux : before).DeepClone();
                var stage = (JObject)corrupt["1"];
                switch (mutation)
                {
                    case "version": stage["mechanicsVersion"] = 2; break;
                    case "missingPressure": stage.Remove("cylinderAdmissionBar"); break;
                    case "missingVersion": stage.Remove("mechanicsVersion"); break;
                    case "short": stage["cylinderAdmissionBar"] = new JArray(1); break;
                    case "long": stage["cylinderAdmissionBar"] = new JArray(1, 2, 3); break;
                    case "boolean": stage["cylinderAdmissionBar"] = new JArray(true, 1); break;
                    case "string": stage["cylinderAdmissionBar"] = new JArray("12", 1); break;
                    case "nan": stage["cylinderAdmissionBar"] = new JArray(double.NaN, 1); break;
                    case "infinity": stage["cylinderAdmissionBar"] = new JArray(double.PositiveInfinity, 1); break;
                }
                bool rejected = false;
                try { (auxiliarySlot ? corruptionTarget.Auxiliary : corruptionTarget.Main).SetSaveStateData(corrupt); }
                catch (Exception) { rejected = true; }
                Check(rejected && JToken.DeepEquals(before, JObject.Parse(corruptionTarget.Main.GetSaveStateData().ToString())) &&
                    JToken.DeepEquals(beforeAux, JObject.Parse(corruptionTarget.Auxiliary.GetSaveStateData().ToString())),
                    "Invalid optional Meyer cylinder mechanics are rejected transactionally: " + mutation);
                invalidCases++;
            }
            meyerStarting["newMechanicsContinuationCases"] = cases;
            meyerStarting["invalidOptionalMechanicsCases"] = invalidCases;
            foreach (bool moveStock in new[] { false, true })
            {
                var source = corruptionTarget;
                source.Main.throttleControl.port.Value = 0; source.Main.crankRpm.port.Value = 0;
                double mass = source.Compound.Cycle.StoredMass, energy = source.Compound.Cycle.StoredEnergy;
                source.Select(S060EngineMode.Simple);
                if (moveStock) { source.Main.crankRpm.port.Value = 30; Tick(source, 50); source.Main.crankRpm.port.Value = 0; }
                float selectedPhase = source.Main.crankRotationReadOut.Value;
                source.Select(S060EngineMode.Meyer);
                Check(source.Compound.Cycle.StoredMass == mass && source.Compound.Cycle.StoredEnergy == energy &&
                    Math.Abs(source.Main.crankRotationReadOut.Value - selectedPhase) < .00001 &&
                    Math.Abs(source.Auxiliary.crankRotationReadOut.Value - ((selectedPhase + .125f) % 1)) < .00001,
                    "Returning to Meyer rephases native mechanics without changing retained steam: moved stock=" + moveStock);
                var primary = JObject.Parse(source.Main.GetSaveStateData().ToString());
                var auxiliary = JObject.Parse(source.Auxiliary.GetSaveStateData().ToString());
                Check(primary["1"]["mechanicsVersion"] == null && auxiliary["1"]["mechanicsVersion"] == null,
                    "A pending rephase does not serialize stale trapped-cylinder pressure signs");
                var loaded = Bank(); Connect(loaded); loaded.Main.SetSaveStateData(primary); loaded.Auxiliary.SetSaveStateData(auxiliary);
                foreach (var b in new[] { source, loaded })
                { b.Main.throttleControl.port.Value = 0; b.Main.reverserControl.port.Value = .3f; b.Main.crankRpm.port.Value = 0; b.Main.cylinderCockControl.port.Value = 0; }
                Tick(source, 1); Tick(loaded, 1);
                Check(Math.Abs(source.Main.torqueOut.Value - loaded.Main.torqueOut.Value) < .02 &&
                    Math.Abs(source.Auxiliary.torqueOut.Value - loaded.Auxiliary.torqueOut.Value) < .02,
                    "Immediate save/reload during Meyer re-selection preserves the first rephased torque");
                MeyerBalance(loaded, "selected mechanics rephase");
                corruptionTarget = loaded;
            }
            meyerStarting["selectionRephaseCases"] = 2;
            Console.WriteLine("Meyer mechanics save continuation " + cases + ", transactional invalid optional states " + invalidCases + ": passed.");
        }
        finally { throttle = oldThrottle; cocks = oldCocks; rpm = oldRpm; }
    }
    private static void CrossVersionCondition(string assemblyPath)
    {
        // The preserved, already-built pre-tuning fixture contains the actual
        // old engine dimensions. Generate saves with that implementation, not
        // a handwritten approximation of its JSON or its state values.
        var old = Assembly.LoadFile(Path.GetFullPath(assemblyPath));
        Check(!ReferenceEquals(old, typeof(NativeProbe).Assembly), "Old save producer is a separately loaded pre-tuning assembly");
        var probe = old.GetType("NativeProbe", true);
        var all = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        probe.GetField("calibration", all).SetValue(null, calibration);
        var oldMode = old.GetType("RearrangedS060.S060EngineMode", true);
        Check(Math.Abs(Convert.ToSingle(old.GetType("RearrangedS060.S060ShayEngine", true).GetField("Ratio", all).GetRawConstantValue()) - 43f / 19f) < .000001f,
            "Legacy save producer retains the actual 43:19 ratio");
        foreach (var mode in new[] { S060EngineMode.Shay, S060EngineMode.Dockside })
        {
            var bank = probe.GetMethod("Bank", all).Invoke(null, null);
            probe.GetMethod("Connect", all).Invoke(null, new[] { bank });
            bank.GetType().GetMethod("Select", Flags).Invoke(bank, new[] { Enum.Parse(oldMode, mode.ToString()) });
            probe.GetMethod("Tick", all).Invoke(null, new[] { bank, (object)100 });
            var main = bank.GetType().GetField("Rear", Flags).GetValue(bank);
            var aux = bank.GetType().GetField("Front", Flags).GetValue(bank);
            var oldEngine = main.GetType().GetProperty("Selected", Flags).GetValue(main);
            float oldBore = (float)oldEngine.GetType().GetField("cylinderBore", Flags).GetValue(oldEngine);
            Check(Math.Abs(oldBore - (mode == S060EngineMode.Shay ? .17f : .3f)) < .000001,
                "Old save producer retains the actual pre-tuning bore: " + mode);
            var before = JObject.Parse(main.GetType().GetMethod("GetSaveStateData", Flags).Invoke(main, null).ToString());
            var beforeAux = JObject.Parse(aux.GetType().GetMethod("GetSaveStateData", Flags).Invoke(aux, null).ToString());
            var current = Bank(); Connect(current);
            current.Main.SetSaveStateData(before); current.Auxiliary.SetSaveStateData(beforeAux);
            Check(current.Mode == mode && JToken.DeepEquals(before, JObject.Parse(current.Main.GetSaveStateData().ToString())) &&
                JToken.DeepEquals(beforeAux, JObject.Parse(current.Auxiliary.GetSaveStateData().ToString())), "Actual old " + mode + " serialized save restores unchanged into retuned dimensions");
            Tick(current, 1);
            Check(!float.IsNaN(current.Main.torqueOut.Value), "Loaded old engine condition ticks at new dimensions without corrupting saved state");
        }
    }
    private static void ShayFlowSaveCompatibility(string assemblyPath)
    {
        var previous = Assembly.LoadFile(Path.GetFullPath(assemblyPath));
        Check(!ReferenceEquals(previous, typeof(NativeProbe).Assembly), "1.2 kg/s save producer is a preserved previous assembly");
        var probe = previous.GetType("NativeProbe", true);
        var all = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        probe.GetField("calibration", all).SetValue(null, calibration);
        var modes = previous.GetType("RearrangedS060.S060EngineMode", true);
        foreach (bool dormant in new[] { false, true })
        {
            var bank = probe.GetMethod("Bank", all).Invoke(null, null);
            probe.GetMethod("Connect", all).Invoke(null, new[] { bank });
            var select = bank.GetType().GetMethod("Select", Flags);
            select.Invoke(bank, new[] { Enum.Parse(modes, "Shay") });
            var main = bank.GetType().GetField("Rear", Flags).GetValue(bank);
            var aux = bank.GetType().GetField("Front", Flags).GetValue(bank);
            var engine = (ReciprocatingSteamEngine)main.GetType().GetProperty("Selected", Flags).GetValue(main);
            Check(engine.throttleMaxFlow == 1.2f && engine.cylinderBore == .252f && engine.pistonStroke == S060SteamCalibration.Shay.Stroke,
                "Previous producer uses actual 1.2 kg/s tune and the same cylinder geometry");
            Check(Math.Abs(Convert.ToSingle(previous.GetType("RearrangedS060.S060ShayEngine", true).GetField("Ratio", all).GetRawConstantValue()) - 4.3f) < .000001,
                "Previous producer retains the approved 4.3:1 gearbox");
            ((Port)probe.GetField("cocks", all).GetValue(null)).Value = 0;
            engine.temperatureInCylinders = 200; engine.waterInCylindersNormalizedReadOut.Value = 0;
            probe.GetMethod("Tick", all).Invoke(null, new[] { bank, (object)1001 });
            engine.waterInCylindersNormalizedReadOut.Value = .23f;
            engine.waterInCylindersDamageMeter = .4f;
            engine.isCylinderCrackedReadOut.Value = 1;
            if (dormant) select.Invoke(bank, new[] { Enum.Parse(modes, "Simple") });
            var savedMain = JObject.Parse(main.GetType().GetMethod("GetSaveStateData", Flags).Invoke(main, null).ToString());
            var savedAux = JObject.Parse(aux.GetType().GetMethod("GetSaveStateData", Flags).Invoke(aux, null).ToString());
            var current = Bank(); Connect(current);
            current.Main.SetSaveStateData(savedMain); current.Auxiliary.SetSaveStateData(savedAux);
            Check(JToken.DeepEquals(savedMain, JObject.Parse(current.Main.GetSaveStateData().ToString())) &&
                JToken.DeepEquals(savedAux, JObject.Parse(current.Auxiliary.GetSaveStateData().ToString())),
                "Actual 1.2 kg/s active/dormant Shay save restores exactly, including steam, wetness, damage and full geared phase");
            current.Select(S060EngineMode.Shay);
            Check(current.Main.Selected.throttleMaxFlow == 1.6f, "Loaded old condition uses the new 1.6 kg/s calibration");
            Tick(current, 1);
            Check(!float.IsNaN(current.Main.torqueOut.Value) && !float.IsInfinity(current.Main.torqueOut.Value), "Loaded previous Shay state ticks finitely after the flow retune");
        }
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
        foreach (var profile in new[] {
            (name:"Stock060", mode:S060EngineMode.Simple, radius:S060Selection.StockDiameter / 2, powered:3, total:3, share:1f),
            (name:"040", mode:S060EngineMode.Simple, radius:S060Selection.StockDiameter / 2, powered:2, total:2, share:ConventionalPhysics.RatedDriverWeightFraction(2, 2)),
            (name:"Short060", mode:S060EngineMode.Simple, radius:S060Selection.StockDiameter / 2 * S060Selection.SmallSixScale, powered:3, total:3, share:ConventionalPhysics.RatedDriverWeightFraction(3, 3)),
            (name:"080", mode:S060EngineMode.Simple, radius:S060Selection.StockDiameter / 2 * S060EightLayout.MaximumScale, powered:4, total:4, share:ConventionalPhysics.RatedDriverWeightFraction(4, 4)),
            (name:"0100", mode:S060EngineMode.Simple, radius:S060TenLayout.Diameter / 2, powered:5, total:5, share:ConventionalPhysics.RatedDriverWeightFraction(5, 5)),
            (name:"062", mode:S060EngineMode.Simple, radius:S060Selection.StockDiameter / 2, powered:3, total:4, share:S060TrailingLayout.DriverWeightFraction),
            (name:"Meyer", mode:S060EngineMode.Meyer, radius:S060Selection.StockDiameter / 2, powered:4, total:4, share:1f),
            (name:"Dockside", mode:S060EngineMode.Dockside, radius:S060SteamCalibration.Dockside.DriverRadius, powered:4, total:4, share:1f),
            (name:"Shay", mode:S060EngineMode.Shay, radius:S060Selection.ShayRollingRadius, powered:4, total:4, share:1f) })
        foreach (float speed in new[] { .1f, 1f, 5f, 10f, 15f, 20f, 30f })
        foreach (float pressure in new[] { 16f, 13f })
        {
            if (pressure == 13 && !(profile.name == "Shay" || (profile.name == "Dockside" && speed == 15))) continue;
            var mode = profile.mode;
            var bank = Bank(); Connect(bank); bank.Select(mode);
            float radius = profile.radius;
            cocks.Value = 0; rpm.Value = speed / 3.6f * 60f / (2 * (float)Math.PI * radius);
            bank.Rear.reverserControl.port.Value = 1;
            bank.Rear.intakePressure.port.Value = pressure;
            foreach (var slot in new[] { bank.Rear, bank.Front }.Where(s=>s.Selected!=null))
            {
                slot.Selected.waterInCylindersNormalizedReadOut.Value = 0;
                slot.Selected.temperatureInCylinders = 200;
            }
            Tick(bank, 6000);
            double minimum = double.PositiveInfinity, maximum = 0, sum = 0, steam = 0;
            for (int i = 0; i < 3000; i++)
            {
                Tick(bank, 1);
                double force = (bank.Rear.torqueOut.Value + bank.Front.torqueOut.Value) / radius;
                minimum = Math.Min(minimum, force); maximum = Math.Max(maximum, force); sum += force;
                steam += bank.Main.intakeFlowReadOut.Value + bank.Auxiliary.intakeFlowReadOut.Value;
            }
            Check(minimum >= 0 && maximum > 0 && !double.IsInfinity(maximum), "Articulated production engine delivers finite positive traction");
            double mean = sum / 3000;
            envelope.Add(new JObject { ["arrangement"] = profile.name, ["mode"] = mode.ToString(), ["speedKmh"] = speed, ["radiusM"] = radius,
                ["boilerBarAbs"] = pressure, ["poweredAxles"] = profile.powered, ["totalAxles"] = profile.total, ["ratedDriverWeightFraction"] = profile.share,
                ["wheelRpm"] = rpm.Value, ["engineRpm"] = bank.Main.Selected.crankRpm.Value,
                ["minimumForceN"] = minimum, ["meanForceN"] = mean, ["maximumForceN"] = maximum,
                ["wheelPowerKw"] = mean * speed / 3.6 / 1000,
                ["meanEngineSteamKgPerSecond"] = steam / 3000 });
            Console.WriteLine($"Traction envelope {profile.name}, {pressure} bar absolute/full regulator/full gear/{speed} km/h: min={minimum:F1}, mean={mean:F1}, max={maximum:F1} N; radius={radius:F7} m, wheelRPM={rpm.Value:F3}, steam={steam / 3000:F4} kg/s.");
        }
    }

    private static JObject Point(string arrangement, double speed, double pressure = 16) => envelope.OfType<JObject>().Single(p =>
        p.Value<string>("arrangement") == arrangement && Math.Abs(p.Value<double>("speedKmh") - speed) < .00001 && p.Value<double>("boilerBarAbs") == pressure);
    private static void AuthoredAcceptance()
    {
        Check(S060Selection.ShayRollingRadius == .4f && S060SteamCalibration.Shay.Bore == .252f && S060SteamCalibration.Shay.ChestLitres == 45 &&
            S060SteamCalibration.Shay.MaxFlow == 1.6f && Math.Abs(S060ShayEngine.Ratio - 4.3f) < .000001f,
            "Final authored Shay retains 0.80 m wheels, 252 mm bores, 45 L chest and 4.3:1 gearing with a 1.6 kg/s ceiling");
        foreach (var target in new[] { (name:"Shay", speed:1d, force:145000d), (name:"Shay", speed:5d, force:125000d),
            (name:"Shay", speed:10d, force:85000d), (name:"Dockside", speed:1d, force:100000d),
            (name:"Dockside", speed:10d, force:70000d), (name:"Dockside", speed:15d, force:50000d) })
            Check(Point(target.name, target.speed).Value<double>("meanForceN") >= target.force,
                $"Authored {target.name} warm reference pull at {target.speed} km/h must exceed {target.force} N");
        double shayStarting = Point("Shay", 1).Value<double>("meanForceN"), docksideStarting = Point("Dockside", 1).Value<double>("meanForceN");
        foreach (double speed in new[] { .1d, 1d })
        {
            double starting = Point("Shay", speed).Value<double>("meanForceN");
            Check(starting >= 145000 && starting <= 160000, "Shay warm mean nominal starting effort stays in the authored 145-160 kN band at " + speed + " km/h");
        }
        Check(shayStarting >= docksideStarting, "Shay nominal starting pull exceeds Dockside before the shared adhesion limit");
        const double commonDryGrip = .20 * 50000 * 9.81;
        Check(Math.Abs(Math.Min(commonDryGrip, shayStarting) / Math.Min(commonDryGrip, docksideStarting) - 1) <= .05,
            "Equal-mass dry grip-limited starting pull remains within five percent of Dockside");
        foreach (double speed in new[] { 15d, 20d, 30d })
            Check(Point("Shay", speed).Value<double>("meanForceN") < Point("Dockside", speed).Value<double>("meanForceN"),
                "Shay trades operating speed for starting pull through native gearing: " + speed + " km/h");
        foreach (var point in envelope.OfType<JObject>().Where(p => p.Value<string>("arrangement") == "Shay"))
            Check(point.Value<double>("meanEngineSteamKgPerSecond") <= S060SteamCalibration.Shay.MaxFlow + .00001,
                "Shay reference performance respects the approved 1.6 kg/s flow ceiling");
        foreach (var grade in new[] { (name:"Shay", speed:10d, trailing:150000d), (name:"Dockside", speed:15d, trailing:200000d) })
        {
            const double loco = 50000, gravity = 9.81, gradeFraction = .015, rollingAllowance = .002, dryCoefficient = .20;
            double resistance = (loco + grade.trailing) * gravity * (gradeFraction + rollingAllowance);
            var point = Point(grade.name, grade.speed);
            double grip = dryCoefficient * loco * gravity * point.Value<double>("ratedDriverWeightFraction");
            double usable = Math.Min(grip, point.Value<double>("meanForceN"));
            Check(usable >= 1.2 * resistance, "Declared steady grade-force budget has 20% reserve: " + grade.name);
            point["gradeBudget"] = new JObject { ["locoMassKg"] = loco, ["trailingMassKg"] = grade.trailing,
                ["gradeFraction"] = gradeFraction, ["rollingAllowanceFractionOfWeight"] = rollingAllowance,
                ["assumedDryCoefficient"] = dryCoefficient, ["resistanceN"] = resistance, ["usableForceN"] = usable,
                ["reserveRatio"] = usable / resistance, ["actualTrainSimulation"] = false };
            var lower = Point(grade.name, grade.speed, 13);
            double lowerUsable = Math.Min(grip, lower.Value<double>("meanForceN"));
            Check(lowerUsable >= 1.2 * resistance, "Declared grade-force budget retains 20% reserve at 13 bar: " + grade.name);
            lower["gradeBudget"] = new JObject { ["locoMassKg"] = loco, ["trailingMassKg"] = grade.trailing,
                ["gradeFraction"] = gradeFraction, ["rollingAllowanceFractionOfWeight"] = rollingAllowance,
                ["assumedDryCoefficient"] = dryCoefficient, ["resistanceN"] = resistance, ["usableForceN"] = lowerUsable,
                ["reserveRatio"] = lowerUsable / resistance, ["actualTrainSimulation"] = false, ["lowerPressureSensitivityOnly"] = true };
        }
        foreach (var point in envelope.Where(p => p.Value<string>("arrangement") == "Shay" || p.Value<string>("arrangement") == "Dockside"))
        {
            double ceiling = point.Value<string>("arrangement") == "Shay" ? S060SteamCalibration.Shay.MaxFlow : 2 * S060SteamCalibration.Dockside.MaxFlow;
            Check(point.Value<double>("meanEngineSteamKgPerSecond") <= ceiling + .00001,
                "Native engine steam demand remains within the authored machinery flow ceiling");
        }
        Check(Point("040", 1).Value<double>("ratedDriverWeightFraction") == ConventionalPhysics.RatedDriverWeightFraction(2, 2), "Two-axle design uses its production rated adhesive load");
        Check(Math.Abs(Point("062", 1).Value<double>("ratedDriverWeightFraction") - .75) < .00001, "Trailing axle retains its authored adhesive share");
        foreach (double speed in new[] { 1d, 5d, 10d })
            Check(Math.Abs(Point("Short060", speed).Value<double>("meanForceN") - Point("080", speed).Value<double>("meanForceN")) < .001,
                "Equal native engine and rolling diameter retain equal pull, irrespective of added driver count");
    }

    private static void ShayOperatingCurve()
    {
        foreach (var point in new[] { (speed:10f, gear:.7f, pressure:16f, target:120000d),
            (speed:15f, gear:.4f, pressure:16f, target:100000d),
            (speed:10f, gear:.7f, pressure:13f, target:100000d),
            (speed:15f, gear:.4f, pressure:13f, target:80000d) })
        {
            var bank = Bank(); Connect(bank); bank.Select(S060EngineMode.Shay);
            cocks.Value = 0; rpm.Value = point.speed / 3.6f * 60 / (2 * (float)Math.PI * S060Selection.ShayRollingRadius);
            bank.Main.reverserControl.port.Value = point.gear; bank.Main.intakePressure.port.Value = point.pressure;
            bank.Main.Selected.waterInCylindersNormalizedReadOut.Value = 0; bank.Main.Selected.temperatureInCylinders = 200;
            Tick(bank, 6000);
            double force = 0, steam = 0;
            for (int i = 0; i < 3000; i++) { Tick(bank, 1); force += bank.Main.torqueOut.Value / S060Selection.ShayRollingRadius; steam += bank.Main.intakeFlowReadOut.Value; }
            force /= 3000; steam /= 3000;
            Check(force >= point.target, "Native shorter-cutoff Shay sustains the authored mid-speed force at " + point.speed + " km/h / " + point.pressure + " bar");
            Check(steam <= S060SteamCalibration.Shay.MaxFlow + .00001 && steam > 0, "Shorter-cutoff operation respects the 1.6 kg/s steam ceiling");
            shayOperating.Add(new JObject { ["speedKmh"] = point.speed, ["boilerBarAbs"] = point.pressure, ["reverser"] = point.gear,
                ["cutoff"] = .05 + .85 * point.gear, ["meanForceN"] = force, ["meanEngineSteamKgPerSecond"] = steam });
            Console.WriteLine($"Shay operating curve {point.speed} km/h, gear {point.gear}, {point.pressure} bar: {force:F1} N, {steam:F4} kg/s.");
        }
    }
}
