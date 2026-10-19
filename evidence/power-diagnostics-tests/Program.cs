using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DV.Simulation.Cars;
using LocoSim.Implementations;
using Newtonsoft.Json.Linq;
using RearrangedS282;
using UnityEngine;
using UnityModManagerNet;
using DV;
using ModMain = RearrangedS282.Main;

internal static class Program {
    private sealed class LimitReportedStream : MemoryStream {
        // Reach the byte-limit branch without allocating or writing 64 MiB.
        public override long Position { get => 64L * 1024 * 1024; set => base.Position = value; }
    }
    private static readonly List<string> Checks = new();
    private static string Output;
    private static int caseNumber;
    private static void Check(bool condition, string name) {
        if (!condition) throw new Exception(name);
        Checks.Add(name);
    }
    private static void Near(float actual, float expected, string name) => Check(Math.Abs(actual - expected) <= Math.Max(.001f, Math.Abs(expected) * .00001f), name);
    private static void Tick(float seconds) {
        Time.realtimeSinceStartup = Time.time = seconds;
        Time.frameCount++;
    }
    private static void NewCase() {
        PowerDiagnostics.Stop();
        ModMain.ModPath = Path.Combine(Output, "case-" + (++caseNumber));
        Stage2RuntimeChecks.Running = false;
        TimeUtil.IsFlowing = true;
        Tick(0);
    }
    private static (SimController sim, ReciprocatingSteamEngine engine, Port wheel) Car(string id = "L-001") {
        var car = new TrainCar { ID = id };
        var sim = new SimController { train = car };
        var engine = new ReciprocatingSteamEngine();
        var wheel = new Port();
        sim.simFlow.OrderedSimComps.Add(engine);
        sim.simFlow.Ports.Add(sim.tractionPortsFeeder.wheelRpmPortId, wheel);
        engine.throttleControl.Value = .14f;
        engine.reverserControl.Value = .33f;
        engine.cutoff = .3305f;
        return (sim, engine, wheel);
    }
    private static JObject[] End(string reason = "test complete") {
        PowerDiagnostics.Stop(reason);
        Check(!PowerDiagnostics.Recording, "Capture stops and releases writer, case " + caseNumber);
        var records = File.ReadAllLines(PowerDiagnostics.LastPath).Select(JObject.Parse).ToArray();
        Check(records[0].Value<string>("record") == "session" && records[^1].Value<string>("record") == "end", "Valid standalone JSONL session/end, case " + caseNumber);
        Check(records[^1].Value<int>("rows") == records.Count(r => r.Value<string>("record") == "sample"), "End count matches samples, case " + caseNumber);
        return records;
    }
    private static JObject[] Samples(JObject[] records) => records.Where(r => r.Value<string>("record") == "sample").ToArray();
    private static Exception FinalizeTick(SimController sim, Exception original) =>
        (Exception)typeof(PowerDiagnosticsTick).GetMethod("Finalizer", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { sim, original });
    private static JObject[] ReadOpenRecords(string path) {
        var records = new List<JObject>();
        // A live reader must allow the existing writer's write access on Windows.
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(file);
        string line;
        while ((line = reader.ReadLine()) != null) records.Add(JObject.Parse(line));
        return records.ToArray();
    }
    public static int Main(string[] args) {
        try {
            Output = Path.GetFullPath(args[0]);
            Directory.CreateDirectory(Output);
            UnityModManager.modEntries.Add(new UnityModManager.ModEntry { Active = true, Info = new UnityModManager.ModInfo { Id = "RealisticFiring", Version = "test-version" } });
            UnityModManager.modEntries.Add(new UnityModManager.ModEntry { Active = false, Info = new UnityModManager.ModInfo { Id = "InactiveMod", Version = "0" } });

            NewCase();
            var inactive = Car("L-OFF");
            var originalFailure = new NullReferenceException("Original engine failure");
            inactive.engine.crankRotationReadOut.Value = .25f;
            inactive.engine.torqueOut.Value = 4200f;
            Check(ReferenceEquals(FinalizeTick(inactive.sim, originalFailure), originalFailure), "Recording-off finalizer returns the exact original exception object");
            Check(FinalizeTick(inactive.sim, null) == null, "Recording-off normal finalizer retains a null exception");
            Check(!Directory.Exists(ModMain.ModPath) && !PowerDiagnostics.Recording, "Recording-off normal/fault finalizers perform no capture-file I/O");
            Check(inactive.engine.crankRotationReadOut.Value == .25f && inactive.engine.torqueOut.Value == 4200f, "Recording-off finalizer leaves engine phase and torque untouched");

            NewCase();
            Stage2RuntimeChecks.Running = true;
            PowerDiagnostics.Begin();
            Check(!PowerDiagnostics.Recording && !Directory.Exists(ModMain.ModPath), "Structural test excludes capture start without opening a file");
            Stage2RuntimeChecks.Running = false;
            PowerDiagnostics.Begin();
            string firstPath = PowerDiagnostics.LastPath;
            PowerDiagnostics.Begin();
            Check(PowerDiagnostics.Recording && PowerDiagnostics.LastPath == firstPath && Directory.GetFiles(Path.Combine(ModMain.ModPath, "diagnostics")).Length == 1, "Duplicate start retains exactly one capture writer");
            var a = Car();
            a.sim.train.Speed = 30;
            a.sim.tractionPortsFeeder.wheelRpm = a.engine.crankRpm.Value = a.wheel.Value = 401.7f;
            PowerDiagnostics.Capture(a.sim);
            Tick(.05f); PowerDiagnostics.Capture(a.sim);
            Tick(.11f); PowerDiagnostics.Capture(a.sim);
            var b = Car("L-002"); PowerDiagnostics.Capture(b.sim);
            Stage2RuntimeChecks.Running = true;
            Tick(.3f); PowerDiagnostics.Capture(a.sim);
            Stage2RuntimeChecks.Running = false;
            TimeUtil.IsFlowing = false;
            PowerDiagnostics.Capture(a.sim);
            TimeUtil.IsFlowing = true;
            var nonSteam = Car("D-001"); nonSteam.sim.train.carType = DV.ThingTypes.TrainCarType.Other;
            PowerDiagnostics.Capture(nonSteam.sim);
            PowerDiagnostics.Capture(null);
            var noCar = Car(); noCar.sim.train = null; PowerDiagnostics.Capture(noCar.sim);
            var noFlow = Car(); noFlow.sim.simFlow = null; PowerDiagnostics.Capture(noFlow.sim);
            var noFeeder = Car(); noFeeder.sim.tractionPortsFeeder = null; PowerDiagnostics.Capture(noFeeder.sim);
            var noForce = Car(); noForce.sim.drivingForce = null; PowerDiagnostics.Capture(noForce.sim);
            var noEngine = Car(); noEngine.sim.simFlow.OrderedSimComps.Clear(); PowerDiagnostics.Capture(noEngine.sim);
            var records = End();
            var samples = Samples(records);
            Check(samples.Length == 3 && samples.Count(s => s.Value<string>("car") == "L-001") == 2, "Ten-Hz per-car gating; second car independent; paused/structural/ineligible/uninitialized skipped");
            Check(samples[0].Value<int>("arrangementId") == 10 && samples[0].Value<string>("arrangement").StartsWith("2-8-2"), "Native S282 captured without a WheelRearranger component");
            Near(samples[0].Value<float>("roadRpm"), 30f * 60f / (2f * Mathf.PI * .712f), "Road RPM uses per-car radius");
            Near(samples[0].Value<float>("wheelRpm"), 401.7f, "Wheel port captured directly");
            Near(samples[0].Value<float>("crankRpm"), 401.7f, "Engine reference captured directly");
            Check(records[0]["mods"].Count() == 1 && records[0]["mods"][0].Value<string>("id") == "RealisticFiring", "Metadata includes active mod versions only");
            string endText = File.ReadAllText(firstPath);
            PowerDiagnostics.Stop();
            Check(File.ReadAllText(firstPath) == endText, "Repeated stop is idempotent");
            using (File.Open(firstPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            Check(true, "Stopped trace is available for exclusive access");

            NewCase(); PowerDiagnostics.Begin();
            var high = Car();
            high.sim.train.Speed = 376f / 3.6f;
            high.sim.drivingForce.wheelRadius = .6096f;
            high.sim.train.Rearranger = new WheelRearranger { currentWA = WheelArrangementType.s4122 };
            high.sim.wheelslipController.wheelslip = 1;
            high.sim.wheelslipController.OrientedMaxWheelslipRpm = 600;
            high.sim.tractionPortsFeeder.wheelRpm = high.engine.crankRpm.Value = high.wheel.Value = 600;
            PowerDiagnostics.Capture(high.sim);
            Tick(.11f); high.sim.wheelslipController.wheelslip = .5f; PowerDiagnostics.Capture(high.sim);
            Tick(.22f); high.sim.train.adhesionController.wheelSlide = .75f; PowerDiagnostics.Capture(high.sim);
            Tick(.33f); high.sim.train.adhesionController.wheelSlide = 0; high.sim.train.Speed *= -1; high.sim.wheelslipController.wheelslip = 1; high.sim.wheelslipController.OrientedMaxWheelslipRpm = -600; PowerDiagnostics.Capture(high.sim);
            samples = Samples(End());
            Near(samples[0].Value<float>("roadRpm"), 1636.107f, "High-speed 4-12-2 rolling RPM is approximately 1636");
            Near(samples[0].Value<float>("slipTargetRpm"), 600, "Full high-speed slip records native lower target without replacing crank RPM");
            Near(samples[1].Value<float>("slipTargetRpm"), (samples[1].Value<float>("roadRpm") + 600) / 2f, "Partial high-speed slip target is reconstructed");
            Near(samples[2].Value<float>("slipTargetRpm"), samples[2].Value<float>("roadRpm") * .25f, "Slide target takes precedence over simultaneous slip");
            Near(samples[3].Value<float>("slipTargetRpm"), -600, "Reverse slip preserves signed RPM direction");
            Check(high.sim.wheelslipController.wheelslip == 1 && high.sim.wheelslipController.OrientedMaxWheelslipRpm == -600 && high.sim.drivingForce.wheelRadius == .6096f && high.engine.crankRpm.Value == 600, "Capture does not modify sampled native inputs");

            NewCase(); PowerDiagnostics.Begin();
            var articulated = Car("L-ART");
            articulated.sim.train.Rearranger = new WheelRearranger { currentWA = WheelArrangementType.s2442 };
            articulated.engine.chuffEventReadOut.id = "steamEngine.CHUFF_EVENT";
            articulated.engine.crankRpm.Value = 200;
            articulated.engine.chuffFrequencyReadOut.Value = 200f / 60 * 4;
            var rear = new ReciprocatingSteamEngine();
            rear.chuffEventReadOut.id = "rearrangedRearSteamEngine.CHUFF_EVENT";
            rear.crankRpm.Value = 180; rear.crankRotationReadOut.Value = .125f;
            rear.chuffFrequencyReadOut.Value = 12; rear.torqueOut.Value = 123;
            rear.intakeFlowReadOut.Value = .7f;
            articulated.sim.simFlow.OrderedSimComps.Add(rear);
            PowerDiagnostics.Capture(articulated.sim);
            var articulatedRecords = End();
            var articulatedSample = Samples(articulatedRecords).Single();
            Check(articulatedRecords[0].Value<int>("schema") == 2, "Schema identifies per-engine capture extension");
            Check(articulatedSample.Value<string>("arrangement").Contains("Articulated"), "Articulated catalogue name captured");
            Check(articulatedSample["engines"].Count() == 2 && articulatedSample["engines"][1].Value<string>("eventPort") == rear.chuffEventReadOut.id, "Both named engine sources are captured separately");
            Near(articulatedSample["engines"][1].Value<float>("crankRpm"), 180, "Rear crank RPM is read directly without a front-engine assumption");
            Near(articulatedSample["engines"][1].Value<float>("crankPhase"), .125f, "Rear crank phase is preserved");
            Near(articulatedSample["engines"][1].Value<float>("torqueNm"), 123, "Rear torque is recorded separately");
            Near(articulatedSample["engines"][1].Value<float>("intakeKgPerSecond"), .7f, "Rear steam demand is recorded separately");
            Check(rear.crankRpm.Value == 180 && rear.torqueOut.Value == 123, "Per-engine capture does not mutate its inputs");

            NewCase(); PowerDiagnostics.Begin();
            var failed = Car("L-FAULT");
            failed.sim.train.Rearranger = new WheelRearranger { currentWA = WheelArrangementType.s080 };
            failed.engine.crankRotationReadOut.Value = .375f;
            failed.engine.chuffEventReadOut.Value = 17f;
            failed.engine.torqueOut.Value = 12345f;
            failed.engine.crankRpm.Value = failed.sim.tractionPortsFeeder.wheelRpm = failed.wheel.Value = 400f;
            failed.sim.drivingForce.generatedForce = 12345f / .712f;
            var engineFailure = new NullReferenceException(new string('x', 4096));
            Check(ReferenceEquals(FinalizeTick(failed.sim, engineFailure), engineFailure), "Fault finalizer returns exact original engine exception while recording");
            Tick(.05f);
            Check(ReferenceEquals(FinalizeTick(failed.sim, engineFailure), engineFailure), "Throttled fault finalizer still preserves exact original exception");
            Tick(.11f);
            failed.sim.train.Speed = 250f / 3.6f;
            failed.engine.crankRpm.Value = failed.sim.tractionPortsFeeder.wheelRpm = failed.wheel.Value = 931.38f;
            Check(ReferenceEquals(FinalizeTick(failed.sim, engineFailure), engineFailure), "Later failed update preserves original exception identity");
            Tick(.22f);
            Check(FinalizeTick(failed.sim, null) == null, "Normal finalizer while recording does not create an engine exception");
            var faultRecords = End();
            var faultSamples = Samples(faultRecords);
            Check(faultRecords[0].Value<int>("schema") == 2 && faultRecords[0].Value<string>("phase").Contains("finalizer") && faultRecords[0].Value<string>("phase").Contains("fault"), "Additive schema retains version two and accurately describes completion/fault timing");
            Check(faultSamples.Length == 3 && faultSamples.Take(2).All(s => s.Value<bool>("simUpdateFailed")), "Fault snapshots obey the same ten-Hz per-car sampling gate");
            Check(faultSamples.All(s => s.Value<string>("car") == "L-FAULT" && s.Value<int>("arrangementId") == 6), "Fault snapshots retain exact car and arrangement identity");
            Check(faultSamples[0]["simUpdateFault"].Value<string>("type") == typeof(NullReferenceException).FullName && faultSamples[0]["simUpdateFault"].Value<string>("type").Length <= 200, "Fault type is captured with a bounded type field");
            Check(faultSamples[0]["simUpdateFault"].Value<string>("message").Length == 512 && engineFailure.Message.Length == 4096, "Fault message is bounded without modifying the original exception");
            Check(!faultSamples[2].Value<bool>("simUpdateFailed") && faultSamples[2]["simUpdateFault"].Type == JTokenType.Null, "A subsequent normal completion has false failure state and null fault data");
            foreach (var snapshot in faultSamples.Take(2)) {
                Near(snapshot["engines"][0].Value<float>("crankPhase"), .375f, "Failed update records the sampled engine crank phase");
                Near(snapshot.Value<float>("torqueNm"), 12345f, "Failed update records sampled torque instead of assuming zero");
                Near(snapshot.Value<float>("forceN"), 12345f / .712f, "Failed update records sampled driving force");
            }
            Near(faultSamples[1].Value<float>("crankRpm"), 931.38f, "Fault capture can show a rising RPM input beside unchanged sampled phase and torque");
            Check(failed.engine.crankRotationReadOut.Value == .375f && failed.engine.torqueOut.Value == 12345f && failed.engine.chuffEventReadOut.Value == 17f, "Fault capture leaves phase, torque and event ports unchanged");

            NewCase(); PowerDiagnostics.Begin();
            var faultyWriterField = typeof(PowerDiagnostics).GetField("writer", BindingFlags.NonPublic | BindingFlags.Static);
            ((StreamWriter)faultyWriterField.GetValue(null)).Dispose();
            ModMain.Logger.ThrowOnLogException = true;
            try {
                Check(ReferenceEquals(FinalizeTick(Car("L-IO-FAULT").sim, originalFailure), originalFailure), "Capture I/O and logger failures cannot replace the exact engine exception");
                Check(!PowerDiagnostics.Recording, "Capture failure closes diagnostics even when its logger also throws");
            } finally { ModMain.Logger.ThrowOnLogException = false; }
            Check(FinalizeTick(null, null) == null, "Normal null-instance completion remains null after diagnostic failure");

            NewCase(); PowerDiagnostics.Begin();
            var missing = Car();
            missing.sim.simFlow.Ports.Clear(); missing.sim.wheelslipController = null; missing.sim.train.adhesionController = null;
            missing.sim.drivingForce.wheelRadius = 0; missing.engine.intakeFlowReadOut = null;
            missing.engine.crankRpm.Value = float.NaN; missing.engine.intakePressure.Value = float.PositiveInfinity;
            PowerDiagnostics.Capture(missing.sim);
            var sample = Samples(End()).Single();
            foreach (string key in new[] { "roadRpm", "wheelRpm", "crankRpm", "slip", "isSlipping", "slide", "isSliding", "intakePressureBarAbs", "intakeKgPerSecond" })
                Check(sample[key].Type == JTokenType.Null, "Unavailable/nonfinite " + key + " stays null");

            NewCase(); PowerDiagnostics.Begin();
            Tick(.2f); PowerDiagnostics.Capture(Car().sim);
            Tick(1.1f); PowerDiagnostics.Update();
            Check(ReadOpenRecords(PowerDiagnostics.LastPath).Length == 2, "Periodic flush makes in-progress JSONL readable");
            Tick(1800.1f); PowerDiagnostics.Update();
            Check(!PowerDiagnostics.Recording, "Maximum duration stops capture");
            records = File.ReadAllLines(PowerDiagnostics.LastPath).Select(JObject.Parse).ToArray();
            Check(records[^1].Value<string>("reason") == "capture limit reached", "Time-limit stop retains a valid footer");

            NewCase(); PowerDiagnostics.Begin();
            typeof(PowerDiagnostics).GetField("rows", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, 99999);
            PowerDiagnostics.Capture(Car().sim);
            Check(!PowerDiagnostics.Recording, "Sample count limit closes immediately at the boundary");
            records = File.ReadAllLines(PowerDiagnostics.LastPath).Select(JObject.Parse).ToArray();
            Check(records[^1].Value<int>("rows") == 100000 && records[^1].Value<string>("reason") == "sample limit reached", "Count-limit footer reflects the boundary (counter seeded for bounded test)");

            NewCase(); PowerDiagnostics.Begin();
            var writerField = typeof(PowerDiagnostics).GetField("writer", BindingFlags.NonPublic | BindingFlags.Static);
            ((StreamWriter)writerField.GetValue(null)).Dispose();
            var limitStream = new LimitReportedStream();
            writerField.SetValue(null, new StreamWriter(limitStream, new UTF8Encoding(false)));
            PowerDiagnostics.Update();
            Check(!PowerDiagnostics.Recording, "Byte-limit boundary stops capture without large test I/O");
            var byteFooter = JObject.Parse(Encoding.UTF8.GetString(limitStream.ToArray()).Trim());
            Check(byteFooter.Value<string>("record") == "end" && byteFooter.Value<string>("reason") == "capture limit reached", "Byte-limit branch writes valid JSON footer (reported stream position seeded)");

            NewCase();
            File.WriteAllText(ModMain.ModPath, "file blocks output directory");
            PowerDiagnostics.Begin();
            Check(!PowerDiagnostics.Recording && PowerDiagnostics.Status.Contains("error"), "Output failure leaves capture stopped with visible status");
            var report = new JObject { ["passed"] = true, ["checks"] = new JArray(Checks),
                ["limitations"] = "Actual PowerDiagnostics.cs and private finalizer executed with narrow Unity/native/Harmony stand-ins. Exception identity, fault sampling and output values are exercised; this does not inject a fault into the real engine or validate Harmony invocation/order, in-game UI, quit hook or crash durability." };
            File.WriteAllText(Path.Combine(Output, "validation.json"), report.ToString());
            Console.WriteLine(Checks.Count + " diagnostic checks passed; actual production capture source executed.");
            return 0;
        } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
