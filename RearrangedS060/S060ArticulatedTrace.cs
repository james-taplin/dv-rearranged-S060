#if B99_S060_ARTICULATED
using System;
using System.IO;
using System.Text;
using DV;
using DV.ECS.Train;
using LocoSim.Implementations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RearrangedS060
{
    // Bounded passive capture for the reported traction/cock faults. No UI,
    // ports, difficulty settings, resources or physics values are changed.
    internal sealed class S060ArticulatedTrace : IDisposable
    {
        private StreamWriter writer;
        private int samples;
        private float nextSample, started, lastFlush;
        private bool finished;

        internal void Update(TrainCar car, S060ArticulatedSimulation owner)
        {
            if (finished || owner.Bank == null || owner.Flow == null) return;
            float now = Time.realtimeSinceStartup;
            if (writer != null && (now - started > 180 || owner.Bank.Mode == S060EngineMode.Simple))
            { Finish("time limit or conventional selection"); return; }
            if (owner.Bank.Mode == S060EngineMode.Simple || owner.Bank.Main.throttleControl.Value <= .001f || now < nextSample) return;
            nextSample = now + .5f;
            try
            {
                if (writer == null)
                {
                    string directory = Path.Combine(Main.ModPath, "diagnostics");
                    Directory.CreateDirectory(directory);
                    string safeId = string.Join("_", car.ID.Split(Path.GetInvalidFileNameChars()));
                    string path = Path.Combine(directory, "articulated-" + safeId + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".jsonl");
                    writer = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
                    started = lastFlush = now;
                    Write(new JObject { ["record"] = "session", ["schema"] = 1, ["car"] = car.ID,
                        ["version"] = typeof(Main).Assembly.GetName().Version.ToString(),
                        ["phase"] = "Passive S060 owner Update; native components/ECS can be from different ticks",
                        ["limits"] = "2 Hz while articulated regulator open; maximum 120 samples or 180 seconds since first open",
                        ["units"] = "bar absolute, newtons, Nm, RPM, kg; null means unavailable/non-finite" });
                    Main.Logger.Log("S060 articulated driving trace started: " + path);
                }
                var sim = car.SimController;
                var slip = sim.wheelslipController;
                var sample = new JObject { ["record"] = "sample", ["utc"] = DateTime.UtcNow.ToString("O"),
                    ["elapsedSeconds"] = N(now - started), ["car"] = car.ID, ["mode"] = owner.Bank.Mode.ToString(),
                    ["gameAllowsSlip"] = Globals.G.GameParams.WheelslipAllowed,
                    ["preventWheelslip"] = slip.preventWheelslip, ["engineBraking"] = slip.IsEngineBraking,
                    ["poweredAxles"] = slip.NumberOfPoweredAxles, ["totalAxles"] = car.NumberOfAxles,
                    ["massKg"] = N(car.massController.TotalMass), ["weightPerAxleN"] = N(car.massController.WeightPerAxle),
                    ["generatedForceN"] = N(sim.drivingForce.generatedForce), ["driverRadiusM"] = N(sim.drivingForce.wheelRadius),
                    ["drivingForceInputPort"] = sim.drivingForce.torqueGeneratedPortId,
                    ["drivingForceInputTorqueNm"] = Port(owner.Flow, sim.drivingForce.torqueGeneratedPortId),
                    ["summedTorqueNm"] = Port(owner.Flow, "rs060Torque.OUT"), ["tractionTorqueNm"] = Port(owner.Flow, "traction.TORQUE_IN"),
                    ["boilerBarAbs"] = Port(owner.Flow, "boiler.PRESSURE"),
                    ["slip"] = N(slip.wheelslip), ["wheelSlide"] = N(car.adhesionController.wheelSlide),
                    ["controllerForceLimitN"] = N(slip.TotalForceLimit), ["sandCoefficient"] = N(slip.SandCoef),
                    ["speedMs"] = N(car.GetForwardSpeed()), ["wheelRpm"] = Port(owner.Flow, "traction.WHEEL_RPM_EXT_IN"),
                    ["rearBogieBrakeN"] = N(car.RearBogie.brakingForce), ["frontBogieBrakeN"] = N(car.FrontBogie.brakingForce),
                    // Legacy schema1 names mean main/auxiliary slots. Preserve
                    // their keys and values so existing diagnostics still read.
                    ["rear"] = Engine(owner.Bank.Main), ["front"] = Engine(owner.Bank.Auxiliary) };
                if (car.entity != null && car.entity.IsConverted)
                {
                    var input = car.entity.GetComponentData<WheelslipControllerSystem.WheelslipInputData>();
                    var grip = car.entity.GetComponentData<AdhesionControllerSystem.AdhesionWheelslipOutputData>();
                    sample["ecsGeneratedForceN"] = N(input.generatedForce);
                    sample["ecsPreventWheelslip"] = input.preventWheelslip;
                    sample["ecsPoweredAxles"] = input.numberOfPoweredAxles;
                    sample["ecsGripPerAxleN"] = N(grip.wheelslipForceLimitPerAxle);
                    sample["ecsGripTotalN"] = N(grip.wheelslipForceLimitPerAxle * input.numberOfPoweredAxles);
                }
                Write(sample); samples++;
                if (now - lastFlush >= 5) { writer.Flush(); lastFlush = now; }
                if (samples >= 120) Finish("sample limit");
            }
            catch (Exception error)
            {
                Finish("capture failed");
                Main.Logger.Log("S060 articulated trace stopped: " + error.GetType().Name + ": " + error.Message);
            }
        }
        private static JObject Engine(S060EngineSlot slot) => new JObject {
            ["id"] = slot.id, ["regulator"] = N(slot.throttleControl.Value), ["reverser"] = N(slot.reverserControl.Value),
            ["cocks"] = N(slot.cylinderCockControl.Value), ["cockFlow"] = N(slot.cylinderCockFlowNormalizedReadOut.Value),
            ["chestBarAbs"] = N(slot.steamChestPressureReadOut.Value), ["torqueNm"] = N(slot.torqueOut.Value),
            ["crankRpm"] = N(slot.crankRpm.Value), ["crankRotation"] = N(slot.crankRotationReadOut.Value),
            ["inletValveMask"] = N(slot.cylindersInletValveOpen.Value),
            ["selectedCockFlow"] = slot.Selected == null ? JValue.CreateNull() : N(slot.Selected.cylinderCockFlowNormalizedReadOut.Value) };
        private static JToken Port(SimulationFlow flow, string id) => flow.TryGetPort(id, out var port) ? N(port.Value) : JValue.CreateNull();
        private static JToken N(float value) => float.IsNaN(value) || float.IsInfinity(value) ? JValue.CreateNull() : new JValue(value);
        private void Write(JObject value) => writer.WriteLine(value.ToString(Formatting.None));
        private void Finish(string reason)
        {
            finished = true;
            if (writer == null) return;
            try { Write(new JObject { ["record"] = "end", ["reason"] = reason, ["samples"] = samples }); writer.Dispose(); }
            catch { try { writer.Dispose(); } catch { } }
            finally { writer = null; }
        }
        public void Dispose() => Finish("owner disposed");
        internal void Disabled() { if (writer != null) Finish("owner disabled"); }
    }
}
#endif

