#if B99_ALL_STANDARD
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using DV;
using DV.Simulation.Cars;
using DV.ThingTypes;
using DV.Utils;
using HarmonyLib;
using LocoSim.Implementations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace RearrangedS282
{
    // Passive sampled Update completion/fault capture. Never changes ports or
    // physics, and never suppresses or replaces an engine exception.
    internal static class PowerDiagnostics
    {
        private const float SampleInterval = .1f;
        private const int MaximumRows = 100000;
        private const long MaximumBytes = 64L * 1024 * 1024;
        private const float MaximumSeconds = 1800f;
        private const int MaximumFaultTypeLength = 200, MaximumFaultMessageLength = 512;
        private static StreamWriter writer;
        private static ConditionalWeakTable<SimController, Target> targets;
        private static float started, lastFlush;
        private static int rows;
        internal static bool Recording => writer != null;
        internal static string Status { get; private set; } = "Power diagnostics are stopped.";
        internal static string LastPath { get; private set; }
        internal static string Latest { get; private set; }
        // Optional extra per-sample block (curve diagnostics) and its session notes.
        internal static Func<TrainCar, JToken> CurveSampler;
        internal static Func<JObject> CurveSessionInfo;

        private sealed class Target
        {
            internal ReciprocatingSteamEngine Engine;
            internal ReciprocatingSteamEngine[] Engines;
            internal Port WheelPort;
            internal float NextSample;
        }

        internal static void Begin()
        {
            if (Recording || Stage2RuntimeChecks.Running) return;
            try
            {
                string directory = Path.Combine(Main.ModPath, "diagnostics");
                Directory.CreateDirectory(directory);
                LastPath = Path.Combine(directory, "power-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".jsonl");
                writer = new StreamWriter(new FileStream(LastPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
                targets = new ConditionalWeakTable<SimController, Target>();
                rows = 0;
                started = lastFlush = Time.realtimeSinceStartup;
                Latest = null;
                var mods = new JArray();
                foreach (var mod in UnityModManager.modEntries)
                    if (mod.Active) mods.Add(new JObject { ["id"] = mod.Info.Id, ["version"] = mod.Info.Version });
                var session = new JObject {
                    ["record"] = "session", ["schema"] = 2, ["startedUtc"] = DateTime.UtcNow.ToString("O"),
                    ["modVersion"] = typeof(Main).Assembly.GetName().Version.ToString(), ["sampleHzPerCar"] = 10,
                    ["maximumRows"] = MaximumRows, ["maximumSeconds"] = MaximumSeconds, ["maximumBytes"] = MaximumBytes, ["mods"] = mods,
                    ["phase"] = "SimController.Update finalizer snapshot on normal completion or fault; a normal return can skip the simulation tick during native pauses",
                    ["updateFaultMeaning"] = "simUpdateFailed records an exception escaping Update; sampled ports may contain values left by the last completed component tick; the original exception is preserved",
                    ["scope"] = "All actively simulated S282s, including stock locomotives; skips structural tests",
                    ["rpmMeaning"] = "wheelRpm is the simulated traction port, not optical wheel-angle tracking; roadRpm assumes rolling without slip",
                    ["targetMeaning"] = "slipTargetRpm is reconstructed from sampled slip state; ECS may have updated slip since the feeder tick",
                    ["units"] = "signed km/h, RPM, metres, newtons, Nm, kg/s, kg; all pressures bar absolute; controls/slip normalized",
                    ["missingValues"] = "null means unavailable/non-finite, never an assumed zero"
                };
                if (CurveSessionInfo != null) session["curve"] = CurveSessionInfo();
                Write(session);
                writer.Flush();
                Status = "Recording S282 power diagnostics. Close the mod manager and drive; stop capture when finished.";
                Main.Logger.Log("Power diagnostics started: " + LastPath);
            }
            catch (Exception ex) { Fail(ex); }
        }

        internal static void Stop(string reason = "stopped by user")
        {
            if (!Recording) return;
            try
            {
                Write(new JObject { ["record"] = "end", ["reason"] = reason, ["rows"] = rows,
                    ["elapsedSeconds"] = Time.realtimeSinceStartup - started, ["utc"] = DateTime.UtcNow.ToString("O") });
                writer.Flush();
                Status = "Power capture saved: " + rows + " samples. " + reason;
                Main.Logger.Log(Status + " — " + LastPath);
            }
            catch (Exception ex) { Status = "Could not finish power capture: " + ex.Message; Main.Logger.LogException(Status, ex); }
            finally
            {
                try { writer.Dispose(); } catch (Exception ex) { Main.Logger.LogException("Could not close power diagnostics", ex); }
                writer = null;
                targets = null;
            }
        }

        internal static void Update()
        {
            if (!Recording) return;
            try
            {
                float now = Time.realtimeSinceStartup;
                if (now - started >= MaximumSeconds || rows >= MaximumRows || writer.BaseStream.Position >= MaximumBytes) { Stop("capture limit reached"); return; }
                if (now - lastFlush >= 1f) { writer.Flush(); lastFlush = now; }
            }
            catch (Exception ex) { Fail(ex); }
        }

        internal static void Capture(SimController sim, Exception simulationException = null)
        {
            if (!Recording) return;
            try
            {
                if (Stage2RuntimeChecks.Running || sim == null || !TimeUtil.IsFlowing) return;
                var car = sim.train;
                if (car == null || car.carType != TrainCarType.LocoSteamHeavy || sim.simFlow == null ||
                    sim.tractionPortsFeeder == null || sim.drivingForce == null) return;
                var target = targets.GetValue(sim, controller => {
                    var engines = controller.simFlow.OrderedSimComps.OfType<ReciprocatingSteamEngine>().ToArray();
                    var result = new Target { Engine = engines.FirstOrDefault(), Engines = engines };
                    controller.simFlow.TryGetPort(controller.tractionPortsFeeder.wheelRpmPortId, out result.WheelPort);
                    return result;
                });
                float now = Time.realtimeSinceStartup;
                if (now < target.NextSample || target.Engine == null) return;
                target.NextSample = now + SampleInterval;
                var engine = target.Engine;
                var feeder = sim.tractionPortsFeeder;
                var slip = sim.wheelslipController;
                var adhesion = car.adhesionController;
                float radius = sim.drivingForce.wheelRadius;
                float speed = car.GetForwardSpeed();
                float roadRpm = radius > 0f ? speed * 60f / (2f * Mathf.PI * radius) : float.NaN;
                float slipTarget = roadRpm;
                if (adhesion != null && adhesion.wheelSlide > 0f) slipTarget = Mathf.Lerp(roadRpm, 0f, adhesion.wheelSlide);
                else if (slip != null && slip.wheelslip > 0f) slipTarget = Mathf.Lerp(roadRpm, slip.OrientedMaxWheelslipRpm, slip.wheelslip);
                var rearranger = car.GetComponent<WheelRearranger>();
                int arrangement = rearranger != null ? (int)rearranger.currentWA : (int)WheelArrangementType.s282;
                var sample = new JObject {
                    ["record"] = "sample", ["elapsedSeconds"] = F(now - started), ["frame"] = Time.frameCount,
                    ["simUpdateFailed"] = simulationException != null, ["simUpdateFault"] = Fault(simulationException),
                    ["gameTime"] = F(Time.time), ["frameDelta"] = F(Time.deltaTime), ["simTimeMultiplier"] = F(sim.simTimeMultiplier),
                    ["car"] = car.ID, ["arrangementId"] = arrangement,
                    ["arrangement"] = WheelArrangement.IsSupportedId(arrangement) ? WheelArrangement.WheelArrangementNames[arrangement] : "unknown",
                    ["speedKmh"] = F(speed * 3.6f), ["driverRadiusM"] = F(radius), ["roadRpm"] = F(roadRpm),
                    ["feederWheelRpm"] = F(feeder.wheelRpm), ["wheelRpm"] = P(target.WheelPort), ["crankRpm"] = F(engine.crankRpm.Value),
                    ["slip"] = slip != null ? F(slip.wheelslip) : JValue.CreateNull(),
                    ["isSlipping"] = slip != null ? new JValue(slip.IsWheelslipping) : JValue.CreateNull(),
                    ["slide"] = adhesion != null ? F(adhesion.wheelSlide) : JValue.CreateNull(),
                    ["isSliding"] = adhesion != null ? new JValue(adhesion.IsWheelSliding) : JValue.CreateNull(),
                    ["slipTargetRpm"] = F(slipTarget), ["orientedMaximumSlipRpm"] = slip != null ? F(slip.OrientedMaxWheelslipRpm) : JValue.CreateNull(),
                    ["maximumSlipRpm"] = slip != null ? F(slip.maxWheelslipRpm) : JValue.CreateNull(),
                    ["regulator"] = F(engine.throttleControl.Value), ["reverser"] = F(engine.reverserControl.Value), ["cutoff"] = F(engine.cutoff),
                    ["torqueNm"] = P(engine.torqueOut), ["forceN"] = F(sim.drivingForce.generatedForce),
                    ["intakePressureBarAbs"] = F(engine.intakePressure.Value), ["chestPressureBarAbs"] = P(engine.steamChestPressureReadOut),
                    ["admissionPressureBarAbs"] = F(engine.admissionPressure), ["chestSteamKg"] = F(engine.steamChestSteamMass),
                    ["intakeKgPerSecond"] = P(engine.intakeFlowReadOut), ["exhaustKgPerSecond"] = P(engine.exhaustFlowReadOut),
                    ["engines"] = new JArray(target.Engines.Select(e => new JObject {
                        ["eventPort"] = e.chuffEventReadOut?.id,
                        ["cylinders"] = e.numCylinders, ["crankRpm"] = F(e.crankRpm.Value),
                        ["crankPhase"] = P(e.crankRotationReadOut), ["chuffEvent"] = P(e.chuffEventReadOut),
                        ["chuffHz"] = P(e.chuffFrequencyReadOut), ["torqueNm"] = P(e.torqueOut),
                        ["chestPressureBarAbs"] = P(e.steamChestPressureReadOut),
                        ["intakeKgPerSecond"] = P(e.intakeFlowReadOut), ["exhaustKgPerSecond"] = P(e.exhaustFlowReadOut)
                    })),
                    ["cylinderCocks"] = F(engine.cylinderCockControl.Value), ["derailed"] = car.derailed,
                    ["trackSlopeApprox"] = F(car.transform.forward.y)
                };
                if (CurveSampler != null) sample["curve"] = CurveSampler(car);
                Write(sample);
                rows++;
                Latest = car.ID + ": road " + (speed * 3.6f).ToString("F1") + " km/h; wheel " + feeder.wheelRpm.ToString("F0") +
                    "; crank " + engine.crankRpm.Value.ToString("F0") + " RPM; slip " + (slip != null ? slip.wheelslip.ToString("F2") : "n/a");
                if (rows >= MaximumRows) Stop("sample limit reached");
            }
            catch (Exception ex) { Fail(ex); }
        }

        private static JToken P(Port port) => port != null ? F(port.Value) : JValue.CreateNull();
        private static JToken F(float value) => float.IsNaN(value) || float.IsInfinity(value) ? JValue.CreateNull() : new JValue(value);
        private static string Bounded(string text, int maximum) => text == null || text.Length <= maximum ? text : text.Substring(0, maximum);
        private static JToken Fault(Exception error) => error == null ? (JToken)JValue.CreateNull() : new JObject {
            ["type"] = Bounded(error.GetType().FullName, MaximumFaultTypeLength),
            ["message"] = Bounded(error.Message, MaximumFaultMessageLength)
        };
        private static void Write(JObject record) => writer.WriteLine(record.ToString(Formatting.None));
        internal static void Fail(Exception error)
        {
            var closing = writer;
            writer = null;
            targets = null;
            try { closing?.Dispose(); } catch { /* A diagnostic I/O failure stays diagnostic-only. */ }
            try { Status = "Power diagnostics stopped after an error: " + Bounded(error.Message, MaximumFaultMessageLength); }
            catch { Status = "Power diagnostics stopped after a capture error."; }
            try { Main.Logger.LogException(Status, error); }
            catch { /* A broken logger must not replace the native engine exception. */ }
        }
    }

    [HarmonyPatch(typeof(SimController), "Update")]
    internal static class PowerDiagnosticsTick
    {
        private static Exception Finalizer(SimController __instance, Exception __exception)
        {
            try { PowerDiagnostics.Capture(__instance, __exception); }
            catch (Exception captureError) { PowerDiagnostics.Fail(captureError); }
            return __exception;
        }
    }
}
#endif
