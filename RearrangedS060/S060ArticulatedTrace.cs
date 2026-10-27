#if B99_S060_ARTICULATED
using System;
using System.IO;
using System.Linq;
using System.Text;
using DV;
using DV.Damage;
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
        private readonly S060TraceCaptureWindow window = new S060TraceCaptureWindow();
        private S060EngineMode? selectedMode;
        private int captureNumber;
        private float started, lastFlush;

        // Called at the simulation-mode boundary. Geometry/physics conversion
        // may still be in progress; the following bounded settling samples show
        // the post-conversion state without changing it.
        internal void SelectionChanged(TrainCar car, S060EngineMode mode)
        {
            if (selectedMode == mode) return;
            Finish("arrangement changed");
            selectedMode = mode;
            window.Select((int)mode, Time.realtimeSinceStartup);
            if (mode == S060EngineMode.Simple || car == null) return;
            try
            {
                Open(car);
                Write(new JObject { ["record"] = "selection", ["utc"] = DateTime.UtcNow.ToString("O"),
                    ["car"] = car.ID, ["mode"] = mode.ToString(),
                    ["phase"] = "Simulation mode selected; geometry conversion may still be in progress",
                    ["vehicle"] = Vehicle(car) });
                writer.Flush();
            }
            catch (Exception error) { Failed(error); }
        }

        internal void Update(TrainCar car, S060ArticulatedSimulation owner)
        {
            if (owner.Bank == null || owner.Flow == null) return;
            SelectionChanged(car, owner.Bank.Mode);
            if (window.Stopped) return;
            float now = Time.realtimeSinceStartup;
            string limit = window.Limit(now);
            if (limit != null) { Finish(limit); return; }
            if (!window.TrySample(now, owner.Bank.Main.throttleControl.Value > .001f)) return;
            try
            {
                if (writer == null) Open(car);
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
                    ["rear"] = Engine(owner.Bank.Main), ["front"] = Engine(owner.Bank.Auxiliary),
                    ["physicalFrontId"] = S060EngineMapping.PhysicalFrontId(owner.Bank.Mode),
                    ["physicalRearId"] = S060EngineMapping.PhysicalRearId(owner.Bank.Mode),
                    ["vehicle"] = Vehicle(car) };
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
                if (owner.Bank.Mode == S060EngineMode.Shay) sample["shay"] = Shay(car, owner.Bank);
                Write(sample);
                if (now - lastFlush >= 5) { writer.Flush(); lastFlush = now; }
                if (window.Limit(now) != null) Finish(window.Limit(now));
            }
            catch (Exception error) { Failed(error); }
        }
        private void Open(TrainCar car)
        {
            string directory = Path.Combine(Main.ModPath, "diagnostics");
            Directory.CreateDirectory(directory);
            string safeId = string.Join("_", car.ID.Split(Path.GetInvalidFileNameChars()));
            string path = Path.Combine(directory, "articulated-" + safeId + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + (++captureNumber).ToString("D3") + ".jsonl");
            writer = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
            started = lastFlush = Time.realtimeSinceStartup;
            Write(new JObject { ["record"] = "session", ["schema"] = 2, ["car"] = car.ID,
                ["version"] = typeof(Main).Assembly.GetName().Version.ToString(),
                ["phase"] = "Passive S060 owner Update; native components/ECS can be from different ticks",
                ["limits"] = "New window per changed arrangement; 2 Hz for first 3 seconds, then while articulated regulator open; maximum 120 samples or 180 seconds since first sample",
                ["units"] = "bar absolute, newtons, Nm, RPM, kg; positions metres in current shifted Unity world, angular velocity rad/s; null means unavailable/non-finite" });
            Main.Logger.Log("S060 articulated driving trace started: " + path);
        }
        private void Failed(Exception error)
        {
            Finish("capture failed");
            Main.Logger.Log("S060 articulated trace stopped: " + error.GetType().Name + ": " + error.Message);
        }
        private static JObject Engine(S060EngineSlot slot) => new JObject {
            ["id"] = slot.id, ["regulator"] = N(slot.throttleControl.Value), ["reverser"] = N(slot.reverserControl.Value),
            ["cocks"] = N(slot.cylinderCockControl.Value), ["cockFlow"] = N(slot.cylinderCockFlowNormalizedReadOut.Value),
            ["chestBarAbs"] = N(slot.steamChestPressureReadOut.Value), ["torqueNm"] = N(slot.torqueOut.Value),
            ["crankRpm"] = N(slot.crankRpm.Value), ["crankRotation"] = N(slot.crankRotationReadOut.Value),
            ["inletValveMask"] = N(slot.cylindersInletValveOpen.Value),
            ["waterInCylindersNormalized"] = N(slot.waterInCylindersNormalizedReadOut.Value),
            ["cylinderTemperature"] = N(slot.cylinderTemperatureReadOut.Value),
            ["selectedWaterInCylindersNormalized"] = slot.Selected == null ? JValue.CreateNull() : N(slot.Selected.waterInCylindersNormalizedReadOut.Value),
            ["selectedCylinderTemperature"] = slot.Selected == null ? JValue.CreateNull() : N(slot.Selected.cylinderTemperatureReadOut.Value),
            ["selectedCockFlow"] = slot.Selected == null ? JValue.CreateNull() : N(slot.Selected.cylinderCockFlowNormalizedReadOut.Value) };
        // Gear clock chain: simulated Shay engine -> motion component -> transmission
        // solver -> actual transforms. Shows where visible rotation stops.
        private static JObject Shay(TrainCar car, S060EngineBank bank)
        {
            var simulated = bank.Main.Engines[3] as S060ShayEngine;
            var motion = car.GetComponentInChildren<S060ShayMotion>(true);
            var data = new JObject {
                ["simulatedEngineIsSelected"] = simulated != null && ReferenceEquals(bank.Main.Selected, simulated),
                ["simulatedGearTurns"] = simulated == null ? JValue.CreateNull() : D(simulated.GearTurns),
                ["simulatedCrankRotation"] = simulated == null ? JValue.CreateNull() : N(simulated.crankRotation),
                ["simulatedCrankRpm"] = simulated == null ? JValue.CreateNull() : N(simulated.crankRpm.Value),
                ["gearedRpmPort"] = bank.ShayRpm == null ? JValue.CreateNull() : N(bank.ShayRpm.Value),
                ["motionFound"] = motion != null, ["frame"] = Time.frameCount };
            if (motion == null) return data;
            var transmission = motion.Transmission;
            data["motionEnabled"] = motion.enabled; data["motionActive"] = motion.gameObject.activeInHierarchy;
            data["motionUpdates"] = motion.Updates; data["motionLastFrame"] = motion.LastFrame;
            data["motionLastTurns"] = D(motion.LastTurns);
            data["motionEngineIsSimulated"] = motion.Engine != null && ReferenceEquals(motion.Engine, simulated);
            data["motionEngineGearTurns"] = motion.Engine == null ? JValue.CreateNull() : D(motion.Engine.GearTurns);
            data["crankLocal"] = T(motion.Crank); data["rearAxleLocal"] = T(motion.RearAxle);
            if (transmission == null) return data;
            data["transmissionEngineTurns"] = D(transmission.engineTurns);
            data["rearWheelTurns"] = D(transmission.RearWheelTurns); data["frontWheelTurns"] = D(transmission.FrontWheelTurns);
            data["rearPhaseErrorDegrees"] = D(transmission.rearPhaseErrorDegrees); data["frontPhaseErrorDegrees"] = D(transmission.frontPhaseErrorDegrees);
            data["rearJointAngle"] = N(transmission.rearJointAngle); data["frontJointAngle"] = N(transmission.frontJointAngle);
            var parts = new JObject();
            foreach (var role in new[] { "outlet", "line", "pinion", "wheel", "cross", "member" })
            {
                var part = transmission.parts?.FirstOrDefault(p => p.role == role);
                parts[role] = part == null ? JValue.CreateNull() : new JObject { ["name"] = part.transform == null ? null : part.transform.name, ["transform"] = T(part.transform) };
            }
            data["parts"] = parts;
            return data;
        }
        private static JToken T(Transform t) => t == null ? JValue.CreateNull() : new JObject {
            ["active"] = t.gameObject.activeInHierarchy, ["localRotation"] = Q(t.localRotation),
            ["localEuler"] = V(t.localEulerAngles), ["parent"] = t.parent == null ? null : t.parent.name };
        private static JToken D(double value) => double.IsNaN(value) || double.IsInfinity(value) ? JValue.CreateNull() : new JValue(value);
        private static JObject Vehicle(TrainCar car)
        {
            var damage = car.GetComponent<DamageController>();
            var stress = car.stress;
            return new JObject {
                ["bodyHealth"] = car.CarDamage == null ? JValue.CreateNull() : N(car.CarDamage.currentHealth),
                ["bodyMaxHealth"] = car.CarDamage == null ? JValue.CreateNull() : N(car.CarDamage.maxHealth),
                ["wheelHealth"] = damage?.wheels == null ? JValue.CreateNull() : N(damage.wheels.CurrentHitPoints),
                ["mechanicalHealth"] = damage?.mechanicalPT == null ? JValue.CreateNull() : N(damage.mechanicalPT.CurrentHitPoints),
                ["electricalHealth"] = damage?.electricalPT == null ? JValue.CreateNull() : N(damage.electricalPT.CurrentHitPoints),
                ["windowsBroken"] = damage?.windows == null ? JValue.CreateNull() : new JValue(damage.windows.windowsBroken),
                ["derailed"] = car.derailed, ["exploded"] = car.isExploded,
                ["body"] = Body(car.rb), ["frontBogie"] = BogieState(car.FrontBogie), ["rearBogie"] = BogieState(car.RearBogie),
                ["stress"] = stress == null ? JValue.CreateNull() : new JObject {
                    ["enabled"] = stress.enabled, ["magnitude"] = N(stress.stress),
                    ["target"] = V(stress.targetStress), ["vector"] = V(stress.stressVector) } };
        }
        private static JToken Body(Rigidbody body) => body == null ? JValue.CreateNull() : new JObject {
            ["position"] = V(body.position), ["rotation"] = Q(body.rotation),
            ["velocity"] = V(body.velocity), ["angularVelocity"] = V(body.angularVelocity),
            ["massKg"] = N(body.mass), ["centreOfMass"] = V(body.centerOfMass),
            ["worldCentreOfMass"] = V(body.worldCenterOfMass),
            ["inertiaTensor"] = V(body.inertiaTensor), ["inertiaRotation"] = Q(body.inertiaTensorRotation),
            ["kinematic"] = body.isKinematic, ["sleeping"] = body.IsSleeping() };
        private static JToken BogieState(Bogie bogie)
        {
            if (bogie == null) return JValue.CreateNull();
            var joint = bogie.GetComponent<ConfigurableJoint>();
            return new JObject { ["body"] = Body(bogie.rb), ["startingLocalPosition"] = V(bogie.startingLocalPosition),
                ["initialConnectedAnchor"] = V(bogie.initialConnectedAnchor), ["joint"] = Joint(joint) };
        }
        private static JToken Joint(ConfigurableJoint joint)
        {
            if (joint == null) return JValue.CreateNull();
            var world = joint.transform.TransformPoint(joint.anchor);
            var connected = joint.connectedBody == null ? joint.connectedAnchor : joint.connectedBody.transform.TransformPoint(joint.connectedAnchor);
            return new JObject { ["anchor"] = V(joint.anchor), ["connectedAnchor"] = V(joint.connectedAnchor),
                ["worldAnchor"] = V(world), ["worldConnectedAnchor"] = V(connected),
                ["anchorErrorM"] = N(Vector3.Distance(world, connected)),
                ["currentForceN"] = V(joint.currentForce), ["currentTorqueNm"] = V(joint.currentTorque) };
        }
        private static JObject V(Vector3 value) => new JObject { ["x"] = N(value.x), ["y"] = N(value.y), ["z"] = N(value.z) };
        private static JObject Q(Quaternion value) => new JObject { ["x"] = N(value.x), ["y"] = N(value.y), ["z"] = N(value.z), ["w"] = N(value.w) };
        private static JToken Port(SimulationFlow flow, string id) => flow.TryGetPort(id, out var port) ? N(port.Value) : JValue.CreateNull();
        private static JToken N(float value) => float.IsNaN(value) || float.IsInfinity(value) ? JValue.CreateNull() : new JValue(value);
        private void Write(JObject value) => writer.WriteLine(value.ToString(Formatting.None));
        private void Finish(string reason)
        {
            window.Stop();
            if (writer == null) return;
            try { Write(new JObject { ["record"] = "end", ["reason"] = reason, ["samples"] = window.Samples }); writer.Dispose(); }
            catch { try { writer.Dispose(); } catch { } }
            finally { writer = null; }
        }
        public void Dispose() => Finish("owner disposed");
        internal void Disabled() { if (writer != null) Finish("owner disabled"); }
    }
}
#endif

