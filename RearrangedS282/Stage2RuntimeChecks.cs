#if B99_STAGE2 || B99_ALL_STANDARD
using System;
using System.Collections;
using System.Linq;
using DV.ECS.Train;
using DV.ThingTypes;
using DV.Wheels;
using LocoSim.Implementations;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RearrangedS282
{
    // Explicitly invoked development test; never runs on load or writes a save.
    internal static class Stage2RuntimeChecks
    {
        internal static bool Running;
        internal static string Status = "Runtime smoke test has not run.";

        internal static void Begin()
        {
            if (Running) return;
#if B99_ALL_STANDARD
            if (PowerDiagnostics.Recording)
            {
                Status = "Stop power diagnostics before running the structural test.";
                return;
            }
#endif
            var cars = UnityEngine.Object.FindObjectsOfType<TrainCar>()
                .Where(c => c.carType == TrainCarType.LocoSteamHeavy && !c.derailed && !c.isExploded &&
                    c.entity != null && c.entity.IsConverted && Mathf.Abs(c.GetForwardSpeed()) <= .01f &&
                    (c.GetComponent<WheelRearranger>() == null || c.GetComponent<WheelRearranger>().currentWA == WheelArrangementType.s282))
                .OrderBy(c => c.ID, StringComparer.Ordinal).Take(2).ToArray();
            if (cars.Length != 2)
            {
                Status = "Need two stationary S282s in stock 2-8-2 arrangement. Close both regulators and use a disposable sandbox.";
                Main.Logger.Warning(Status);
                System.IO.File.WriteAllText(System.IO.Path.Combine(Main.ModPath,"stage2-runtime-checks.json"),
                    new JObject { ["passed"] = false, ["started"] = false, ["error"] = Status, ["eligibleCars"] = cars.Length }.ToString());
                return;
            }
            var runner = cars[0].GetComponent<WheelRearranger>() ?? cars[0].gameObject.AddComponent<WheelRearranger>();
            Running = true;
            Status = "Test running. Close the mod manager so the native simulation can tick.";
#if B99_ALL_STANDARD
            runner.StartCoroutine(RunAll(runner, cars[0], cars[1]));
#else
            runner.StartCoroutine(Run(runner, cars[0], cars[1]));
#endif
        }

#if B99_ALL_STANDARD
        private static IEnumerator RunAll(WheelRearranger runner, TrainCar subject, TrainCar control)
        {
            var results = new JArray();
            var report = new JObject { ["build"] = "articulated-trial", ["version"] = typeof(Main).Assembly.GetName().Version.ToString(), ["subject"] = subject.ID,
                ["control"] = control.ID, ["passed"] = false, ["arrangements"] = results };
            JObject before = null, controlBefore = null;
            try { before = Capture(subject); controlBefore = Capture(control); }
            catch (Exception ex) { report["error"] = ex.ToString(); }
            if (before != null && controlBefore != null)
            {
                report["before"] = before; report["controlBefore"] = controlBefore;
                for (int id = 0; id < WheelArrangement.SupportedCount; id++)
                {
                    Status = "Structural test: " + WheelArrangement.WheelArrangementNames[id];
                    var result = new JObject { ["id"] = id, ["name"] = WheelArrangement.WheelArrangementNames[id], ["passed"] = false };
                    results.Add(result);
                    if (runner.ConventionalSetupBlocked)
                    {
                        result["status"] = "blocked-by-initialization";
                        result["error"] = runner.LastConventionalError;
                        continue;
                    }
                    bool switched = false;
                    try { switched = runner.TrySwitchStage2(id); }
                    catch (Exception ex) { result["error"] = ex.ToString(); }
                    yield return new WaitForFixedUpdate();
                    yield return new WaitForFixedUpdate();
                    try
                    {
                        if (!switched) throw new InvalidOperationException(runner.LastConventionalError ?? "Conversion refused; check the game log.");
                        var changed = Capture(subject); result["converted"] = changed;
                        if (!JToken.DeepEquals(before["oilcups"], changed["oilcups"]))
                            throw new InvalidOperationException("Native oilcup circuit or position binding changed.");
                        var wheels = WheelArrangement.WheelArrangementNames[id].Split(' ')[0].Split('-').Select(int.Parse).ToArray();
                        int total = wheels.Sum() / 2;
                        int powered = WheelArrangement.IsArticulatedId(id) ? (wheels[1] + wheels[2]) / 2 : wheels[1] / 2;
                        if ((int)changed["axles"] != total || (int)changed["entityAxles"] != total ||
                            (int)changed["powered"] != powered || (int)changed["poweredPort"] != powered)
                            throw new InvalidOperationException("Axle counts disagree with the selected arrangement.");
                        if ((bool)changed["articulated"] != WheelArrangement.IsArticulatedId(id))
                            throw new InvalidOperationException("Articulated frame or native constraints do not match the selected arrangement.");
                        if (powered == 0 && ((bool)changed["tractionEnabled"] ||
                            (bool)changed["engineBrakingReaderBound"] || subject.SimController.drivingForce.generatedForce != 0f))
                            throw new InvalidOperationException("The zero-driver novelty retained traction or engine braking.");
                        result["explosionMaterialRenderers"] = runner.ExplosionMaterialRendererCount;
                        if (id == (int)WheelArrangementType.s222)
                        {
                            result["prototypeDriverDiameterMetres"] = runner.GiantSingleDriverDiameter;
                            result["prototypeDriverColliderCount"] = runner.GiantSingleDriverColliderCount;
                            float expectedDiameter = WheelRearranger.ClampGiantDriverDiameter(Main.settings.giant222DriverDiameterMetres);
                            if (Mathf.Abs(runner.GiantSingleDriverDiameter - expectedDiameter) > .001f ||
                                runner.GiantSingleDriverColliderCount != 0 ||
                                Mathf.Abs(2f * subject.SimController.drivingForce.wheelRadius - expectedDiameter) > .001f)
                                throw new InvalidOperationException("Clearance prototype radius or collider-free geometry is inconsistent.");
                        }
                        if (id != 10 && runner.ExplosionMaterialRendererCount == 0)
                            throw new InvalidOperationException("Added running gear has no native explosion material registration.");
                        if (!JToken.DeepEquals(before["steam"], changed["steam"]) ||
                            !JToken.DeepEquals(before["type"], changed["type"]) || !JToken.DeepEquals(before["mass"], changed["mass"]))
                            throw new InvalidOperationException("Native engine, shared type or base mass changed.");
                        if (!JToken.DeepEquals(controlBefore, Capture(control))) throw new InvalidOperationException("Control locomotive changed.");
                        if (!runner.TrySwitchStage2(10)) throw new InvalidOperationException("Return to stock failed.");
                        result["passed"] = true;
                    }
                    catch (Exception ex) { result["error"] = ex.ToString(); }
                    // Always attempt stock restoration, including after a failed conversion.
                    try { if (runner.currentWA != WheelArrangementType.s282) runner.TrySwitchStage2(10); }
                    catch (Exception ex) { result["error"] = ex.ToString(); result["passed"] = false; }
                    yield return new WaitForFixedUpdate();
                    yield return new WaitForFixedUpdate();
                    try
                    {
                        result["restored"] = Capture(subject);
                        if (!JToken.DeepEquals(before, result["restored"]) || !JToken.DeepEquals(controlBefore, Capture(control)))
                            throw new InvalidOperationException("Stock restoration or second-car isolation failed.");
                    }
                    catch (Exception ex) { result["error"] = ex.ToString(); result["passed"] = false; }
                    // Save progress after every arrangement so an interrupted run has evidence.
                    System.IO.File.WriteAllText(System.IO.Path.Combine(Main.ModPath, "stage2-runtime-checks.json"), report.ToString());
                }
                report["passed"] = results.Count == WheelArrangement.SupportedCount && results.All(r => (bool)r["passed"]);
            }
            report["limitations"] = "Stationary structural checks only. Driving, loaded pull, slip onset, rods, audio, particles and distant/unload behaviour need manual testing.";
            System.IO.File.WriteAllText(System.IO.Path.Combine(Main.ModPath, "stage2-runtime-checks.json"), report.ToString());
            Running = false;
            Status = (bool)report["passed"] ? "All " + WheelArrangement.SupportedCount + " structural checks passed. See stage2-runtime-checks.json."
                : "Structural checks failed. See stage2-runtime-checks.json and the game log.";
            Main.Logger.Log(Status);
        }
#endif

        private static IEnumerator Run(WheelRearranger runner, TrainCar subject, TrainCar control)
        {
            var report = new JObject { ["subject"] = subject.ID, ["control"] = control.ID, ["passed"] = false };
            JObject before;
            JObject controlBefore;
            try
            {
                before = Capture(subject);
                controlBefore = Capture(control);
            }
            catch (Exception ex)
            {
                Status = "Could not capture runtime baseline: " + ex;
                Main.Logger.Log(Status);
                Running = false;
                System.IO.File.WriteAllText(System.IO.Path.Combine(Main.ModPath,"stage2-runtime-checks.json"),
                    new JObject { ["passed"] = false, ["error"] = Status }.ToString());
                yield break;
            }
            report["before"] = before;
            report["controlBefore"] = controlBefore;
            string failure = null;
            bool converted = false;
            try { converted = runner.TrySwitchStage2((int)WheelArrangementType.s284); }
            catch (Exception ex) { failure = ex.ToString(); }
            if (!converted) failure = failure ?? "Conversion refused; check regulators, driving force and the mod log.";
            if (failure == null)
            {
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                try
                {
                    var changed = Capture(subject);
                    report["berkshire"] = changed;
                    if ((int)changed["axles"] != 7 || (int)changed["entityAxles"] != 7 ||
                        (int)changed["powered"] != 4 || (int)changed["poweredPort"] != 4)
                        throw new InvalidOperationException("Converted car/manager/port/entity axle counts disagree.");
                    if (!JToken.DeepEquals(controlBefore, Capture(control)))
                        throw new InvalidOperationException("The second locomotive's configuration changed.");
                    if (!JToken.DeepEquals(before["steam"],changed["steam"]) || !JToken.DeepEquals(before["type"],changed["type"]))
                        throw new InvalidOperationException("Steam engine or shared car type changed.");
                    if (!runner.TrySwitchStage2((int)WheelArrangementType.s282))
                        throw new InvalidOperationException("Return to stock was refused.");
                }
                catch (Exception ex) { failure = ex.ToString(); }
            }
            if (failure == null)
            {
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                try
                {
                    var restored = Capture(subject);
                    report["restored"] = restored;
                    report["controlAfter"] = Capture(control);
                    if (!JToken.DeepEquals(before, restored)) throw new InvalidOperationException("Stock configuration was not restored exactly.");
                    if (!JToken.DeepEquals(controlBefore, report["controlAfter"])) throw new InvalidOperationException("Control locomotive changed.");
                    // Repeated same-frame transitions test duplicate prevention and drift.
                    for (int i = 0; i < 10; i++)
                    {
                        if (!runner.TrySwitchStage2(11) || !runner.TrySwitchStage2(10))
                            throw new InvalidOperationException("Repeated transition failed.");
                    }
                    if (!JToken.DeepEquals(before, Capture(subject))) throw new InvalidOperationException("Repeated switching accumulated changes.");
                    if (!JToken.DeepEquals(controlBefore, Capture(control))) throw new InvalidOperationException("Repeated switching changed the control car.");
                    report["passed"] = true;
                }
                catch (Exception ex) { failure = ex.ToString(); }
            }
            try
            {
                if (runner.currentWA != WheelArrangementType.s282) runner.TrySwitchStage2(10);
                if (failure != null) report["error"] = failure;
                report["limitations"] = "Structural runtime regression only: no loaded pull, wheelslip onset, audio or visual inspection measured.";
                var path = System.IO.Path.Combine(Main.ModPath, "stage2-runtime-checks.json");
                System.IO.File.WriteAllText(path, report.ToString());
                Status = failure == null ? "Runtime structural checks passed. See stage2-runtime-checks.json." : "Runtime checks failed: " + failure;
                Main.Logger.Log(Status);
            }
            finally { Running = false; }
        }

        private static JObject Capture(TrainCar car)
        {
            var sim = car.SimController;
            var shared = car.entity.GetComponentData<AdhesionControllerSystem.StaticSharedAdhesionCalculationData>();
            var friction = car.entity.GetComponentData<AdhesionControllerSystem.StaticAdhesionCalculationData>();
            var engines = sim.simFlow.OrderedSimComps.OfType<ReciprocatingSteamEngine>().ToArray();
            var engine = engines.First();
            var rotations = car.GetComponentsInChildren<PoweredWheelRotationViaAnimation>(true);
            return new JObject
            {
                ["axles"] = car.NumberOfAxles, ["entityAxles"] = shared.numOfAxles,
                ["bogies"] = new JArray(car.Bogies.Select(b => new JArray(b.Axles.Select(a => a.transform.GetInstanceID())))),
                ["powered"] = sim.poweredWheels.poweredWheels.Length, ["poweredPort"] = sim.wheelslipController.NumberOfPoweredAxles,
                ["poweredReferences"] = new JArray(sim.poweredWheels.poweredWheels.Select(w => w.GetInstanceID())),
                ["poweredStates"] = new JArray(sim.poweredWheels.poweredWheels.Select(w => (int)w.state)),
                ["forceRadius"] = sim.drivingForce.wheelRadius,
                ["tractionEnabled"] = sim.drivingForce.enabled,
                ["engineBrakingReaderBound"] = sim.wheelslipController.engineBrakingActivePort != null,
                ["rpmConstant"] = sim.tractionPortsFeeder.speedMsToWheelRpmConst,
                ["speedConstant"] = sim.tractionPortsFeeder.wheelRpmToSpeedKmhConst,
                ["animationRadii"] = new JArray(rotations.Select(r => r.wheelRadius)),
                ["circumferences"] = new JArray(rotations.Select(r => r.wheelCircumference)),
                ["slipCoefficient"] = friction.wheelslipFrictionCoef, ["slideCoefficient"] = friction.wheelSlideFrictionCoef,
                ["mass"] = car.massController.CarMass,
#if B99_ALL_STANDARD
                ["oilcups"] = CaptureOilcups(car),
                ["frontCouplingMounts"] = CaptureFrontCouplingMounts(car),
                ["articulated"] = car.GetComponent<WheelRearranger>()?.ArticulatedGeometryActive == true,
                ["engines"] = new JArray(engines.Select(e => new JObject {
                    ["eventPort"] = e.chuffEventReadOut.id, ["cylinders"] = e.numCylinders,
                    ["bore"] = e.cylinderBore, ["stroke"] = e.pistonStroke
                })),
#endif
                ["steam"] = new JArray(engine.numCylinders, engine.cylinderBore, engine.pistonStroke, engine.minCutoff, engine.maxCutoff),
                ["type"] = new JArray(car.carLivery.parentType.GetInstanceID(), car.carLivery.parentType.wheelRadius,
                    car.carLivery.parentType.mass, car.carLivery.parentType.WheelslipFrictionCoef)
            };
        }
#if B99_ALL_STANDARD
        private static JObject CaptureFrontCouplingMounts(TrainCar car)
        {
            var visual = car.frontCoupler?.visualCoupler;
            JToken Position(Transform mount) => mount == null ? JValue.CreateNull()
                : new JArray(mount.localPosition.x, mount.localPosition.y, mount.localPosition.z);
            return new JObject { ["chain"] = Position(visual?.chain), ["hoses"] = Position(visual?.hoses) };
        }

        private static JObject CaptureOilcups(TrainCar car)
        {
            var providers = car.GetComponent<DV.Util.PositionSyncProviderController>()?.entries;
            var external = car.loadedExternalInteractables;
            if (external == null) return new JObject { ["externalLoaded"] = false };
            var cups = external.GetComponent<DV.Simulation.Controllers.OilingPointsPortController>()?.entries;
            if (providers == null || cups == null || cups.Length != 6)
                throw new InvalidOperationException("Native six-cup oiling setup is missing.");
            return new JObject { ["externalLoaded"] = true, ["cups"] = new JArray(cups.Select(c => {
                var consumer = c.GetComponentInParent<DV.Util.PositionSyncConsumer>();
                if (consumer == null || providers.Count(p => p != null && p.syncTag == consumer.syncTag && p.transform == consumer.providerTransform) != 1)
                    throw new InvalidOperationException("Oilcup has lost its unique native position binding.");
                return new JObject { ["id"] = c.GetInstanceID(), ["tag"] = consumer.syncTag,
                    ["provider"] = consumer.providerTransform.GetInstanceID(),
                    ["refillPort"] = c.refillPortId, ["flowPort"] = c.refillingFlowNormalizedPortId };
            })) };
        }
#endif
    }
}
#endif
