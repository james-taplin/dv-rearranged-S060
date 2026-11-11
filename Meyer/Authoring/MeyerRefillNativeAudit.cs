#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

// Native servicing cache, purchase limits, refill ports and fraction saves.
// Only the scene/flow host and inert body-health fields are seeded, not gameplay.
public static class MeyerRefillNativeAudit
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static int checks;
    public static void RunBatch()
    {
        string output = Environment.GetEnvironmentVariable("S060_MEYER_REFILL_OUTPUT");
        string dll = Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME");
        string managed = Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED");
        Directory.CreateDirectory(output);
        typeof(ExhaustNativeAudit).GetField("managed", All).SetValue(null, managed);
        ResolveEventHandler resolve = (sender, args) => (Assembly)typeof(ExhaustNativeAudit).GetMethod("Resolve", All).Invoke(null, new object[] { sender, args });
        AppDomain.CurrentDomain.AssemblyResolve += resolve;
        var root = new GameObject("Meyer native refill audit"); root.SetActive(false);
        object owner = null;
        try
        {
            var sim = Assembly.LoadFrom(Path.Combine(managed, "DV.Simulation.dll"));
            var game = Assembly.LoadFrom(Path.Combine(managed, "Assembly-CSharp.dll"));
            var mod = Assembly.LoadFrom(dll);
            var containerType = sim.GetType("LocoSim.Implementations.ResourceContainer", true);
            var componentType = sim.GetType("LocoSim.Implementations.SimComponent", true);
            var portDef = sim.GetType("LocoSim.Definitions.PortDefinition", true);
            var portType = sim.GetType("LocoSim.Definitions.PortType", true);
            var valueType = sim.GetType("LocoSim.Definitions.PortValueType", true);
            var resourceType = sim.GetType("LocoSim.Resources.ResourceContainerType", true);
            object Definition(string name, bool input) => Activator.CreateInstance(portDef,
                Enum.Parse(portType, input ? "EXTERNAL_IN" : "READONLY_OUT"), Enum.Parse(valueType, "GENERIC"), name);
            object Container(string name, string type, float capacity) => Activator.CreateInstance(containerType,
                name, Enum.Parse(resourceType, type), capacity, capacity,
                Definition("REFILL", true), Definition("CONSUME", true), Definition("AMOUNT", false), Definition("NORMALIZED", false), Definition("CAPACITY", false));
            var water = Container("water", "WATER", 4500); var coal = Container("coal", "COAL", 1200);
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(componentType)); list.Add(water); list.Add(coal);
            var flowType = sim.GetType("LocoSim.Implementations.SimulationFlow", true);
            var flow = FormatterServices.GetUninitializedObject(flowType); Set(flow, "OrderedSimComps", list);
            // Inactive native host: actual resource aggregate and native mass
            // method run, while derail mode avoids constructing bogie joints.
            // No game world, track, ticking physics or native timer is needed.
            var trainCarType = game.GetType("TrainCar", true);
            var trainCar = root.AddComponent(trainCarType);
            var rigidbody = root.GetComponent<Rigidbody>();
            if (rigidbody == null) rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            Set(trainCar, "rb", rigidbody); Set(trainCar, "derailed", true);
            Set(trainCar, "logicCar", FormatterServices.GetUninitializedObject(game.GetType("DV.Logic.Job.Car", true)));
            var simControllerType = game.GetType("DV.Simulation.Cars.SimController", true);
            var simController = root.AddComponent(simControllerType);
            var massType = game.GetType("TrainMassController", true);
            var mass = FormatterServices.GetUninitializedObject(massType); Set(mass, "car", trainCar);
            Set(mass, "<CarMass>k__BackingField", 41000f); Set(trainCar, "massController", mass);
            var readerType = game.GetType("DV.Simulation.Cars.ResourceMassPortReader", true);
            var resourceControllerType = game.GetType("DV.Simulation.Cars.ResourceContainerController", true);
            var resourceController = Activator.CreateInstance(resourceControllerType, flow, Array.CreateInstance(readerType, 0));
            Set(simController, "resourceContainerController", resourceController);
            massType.GetMethod("SetResourceContainerController").Invoke(mass, new[] { resourceController });
            void RefreshNativeMass() => massType.GetMethod("UpdateTrainCarMass", Type.EmptyTypes).Invoke(mass, null);
            float NativeMass() => (float)massType.GetProperty("TotalMass").GetValue(mass);
            RefreshNativeMass();
            Check(NativeMass() == 46700f && rigidbody.mass == 46700f, "Actual native resource aggregate uses water/coal AMOUNT at stock baseline.");
            var health = FormatterServices.GetUninitializedObject(game.GetType("DV.Damage.CarDamageModel", true));
            Set(health, "currentHealth", 100f); Set(health, "maxHealth", 100f); Set(health, "effectiveMaxHealth", 100f);
            var damage = FormatterServices.GetUninitializedObject(game.GetType("DV.Damage.DamageController", true)); Set(damage, "bodyDamage", health);
            var serviceType = game.GetType("DV.PitStops.SimulatedCarPitStopParameters", true);
            var service = root.AddComponent(serviceType);
            var containers = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(containerType)); containers.Add(water); containers.Add(coal);
            serviceType.GetMethod("Initialize").Invoke(service, new[] { containers, damage });
            var dvType = serviceType.GetMethod("UpdateCarPitStopParameter").GetParameters()[0].ParameterType;
            var waterKey = Enum.Parse(dvType, "Water"); var coalKey = Enum.Parse(dvType, "Coal");
            IDictionary Report() => (IDictionary)serviceType.GetMethod("GetCarPitStopParameters").Invoke(service, null);
            var initial = Report(); var waterData = initial[waterKey]; var coalData = initial[coalKey];
            Check((float)Get(waterData, "maxValue") == 4500 && (float)Get(coalData, "maxValue") == 1200, "Native service starts with stock limits.");
            // Reproduce the original integration gap without stubbing native refresh:
            // update the real containers exactly as 0.22.2 did, then ask native UI.
            Set(water, "capacity", 8800f); Set(coal, "capacity", 3500f);
            Check((float)Get(Report()[waterKey], "maxValue") == 4500 && (float)Get(Report()[coalKey], "maxValue") == 1200,
                "Native RefreshParameters leaves the cached stock maximum unchanged.");
            Set(water, "capacity", 4500f); Set(coal, "capacity", 1200f);
            var capacityType = mod.GetType("RearrangedS060.S060MeyerCapacity", true);
            var apply = capacityType.GetMethod("Apply", All);
            object Apply(bool restoring, object targetService) => apply.Invoke(null, apply.GetParameters().Length == 3
                ? new object[] { flow, restoring, root } : new object[] { flow, restoring });
            var logger = Assembly.LoadFrom(Path.Combine(managed, "UnityModManager", "UnityModManager.dll"))
                .GetType("UnityModManagerNet.UnityModManager+ModEntry+ModLogger", true);
            mod.GetType("RearrangedS060.Main", true).GetField("<Logger>k__BackingField", All).SetValue(null, Activator.CreateInstance(logger, "Meyer refill audit"));
            var moduleType = game.GetType("LocoResourceModule", true);
            var module = FormatterServices.GetUninitializedObject(moduleType);
            Set(module, "locoParamData", waterData);
            owner = Apply(false, service);
            Check(ReferenceEquals(Report()[waterKey], waterData) && ReferenceEquals(Report()[coalKey], coalData), "Existing station data objects survive the conversion.");
            Check((float)Get(waterData, "maxValue") == 8800 && (float)Get(coalData, "maxValue") == 3500, "Meyer exposes enlarged capacities to native servicing.");
            Check((float)moduleType.GetProperty("BuyMaxLimit").GetValue(module) == 4300f, "Already-bound native water station offers the extra 4300 litres.");
            Set(module, "locoParamData", coalData);
            Check((float)moduleType.GetProperty("BuyMaxLimit").GetValue(module) == 2300f, "Native coal station offers the extra 2300 kg.");
            var update = serviceType.GetMethod("UpdateCarPitStopParameter");
            update.Invoke(service, new object[] { waterKey, 4300f }); update.Invoke(service, new object[] { coalKey, 2300f });
            Check(Amount(water) == 8800 && Amount(coal) == 3500, "Native servicing/refill ports fill the full combined stores.");
            Report();
            Check((float)moduleType.GetProperty("BuyMaxLimit").GetValue(module) == 0f, "Native purchase limit stops at the enlarged full capacity.");
            foreach (var container in new[] { water, coal })
            {
                var save = containerType.GetMethod("GetSaveStateData").Invoke(container, null);
                Value(Get(container, "consumeExtIn"), 300f);
                containerType.GetMethod("SetSaveStateData").Invoke(container, new[] { save });
                Check(Amount(container) == (float)Get(container, "capacity"), "Native fractional save restores the enlarged full store.");
            }
            RefreshNativeMass(); // Model an already-refreshed full Meyer before exit.
            Check(NativeMass() == 53300f, "Native mass uses enlarged actual stores rather than stock normalized capacity.");
            ((IDisposable)owner).Dispose(); owner = null;
            Check(Amount(water) == 4500 && Amount(coal) == 1200, "Return to stock clamps full stores without replacing containers.");
            Check(NativeMass() == 46700f && rigidbody.mass == 46700f, "Stock clamp immediately refreshes native total and body mass without waiting 120 seconds.");
            Check((float)Get(waterData, "maxValue") == 4500 && (float)Get(coalData, "maxValue") == 1200, "Existing station references restore their stock maxima.");
            for (int cycle = 0; cycle < 3; cycle++)
            {
                owner = Apply(false, service);
                Check(Amount(water) == 4500 && Amount(coal) == 1200, "Switching into Meyer preserves physical contents rather than granting supplies.");
                ((IDisposable)owner).Dispose(); owner = null;
                Check((float)Get(waterData, "maxValue") == 4500, "Repeated switches restore servicing limits.");
            }
            // Test both native save-order possibilities, using actual fraction setters.
            foreach (bool loadFirst in new[] { true, false })
            {
                if (loadFirst) SetFraction(.5f);
                owner = Apply(true, service);
                if (loadFirst) Check(NativeMass() == 47150f && rigidbody.mass == 47150f, "Restore-first Meyer conversion immediately refreshes native mass from expanded half-filled stores.");
                if (!loadFirst) SetFraction(.5f);
                Check(Amount(water) == 4400 && Amount(coal) == 1750, "Half-filled Meyer save restores correct quantities in either load order.");
                Value(Get(coal, "consumeExtIn"), 600f); // Stay within stock capacity for this teardown.
                ((IDisposable)owner).Dispose(); owner = null;
                void SetFraction(float fraction)
                {
                    foreach (var container in new[] { water, coal })
                    {
                        var save = containerType.GetMethod("GetSaveStateData").Invoke(container, null);
                        // JObject's string indexer accepts a JToken made by its own assembly.
                        var token = Activator.CreateInstance(save.GetType().Assembly.GetType("Newtonsoft.Json.Linq.JValue", true), new object[] { (double)fraction });
                        save.GetType().GetProperty("Item", new[] { typeof(string) }).SetValue(save, token, new object[] { "filled" });
                        containerType.GetMethod("SetSaveStateData").Invoke(container, new[] { save });
                    }
                }
            }
            // Before native resource ownership has initialized, capacity work
            // must not zero a pre-existing mass cache or throw during teardown.
            Set(simController, "resourceContainerController", null);
            float beforeUninitialized = NativeMass();
            owner = Apply(false, service);
            Check(NativeMass() == beforeUninitialized, "Missing not-yet-initialized native resource controller safely defers mass refresh.");
            ((IDisposable)owner).Dispose(); owner = null;
            Check(NativeMass() == beforeUninitialized, "Early-load teardown also safely defers mass refresh.");
            Set(simController, "resourceContainerController", resourceController);
            UnityEngine.Object.DestroyImmediate(service);
            owner = Apply(false, null); // Native controller created after conversion reads capacities directly.
            service = root.AddComponent(serviceType);
            serviceType.GetMethod("Initialize").Invoke(service, new[] { containers, damage });
            Check((float)Get(Report()[waterKey], "maxValue") == 8800, "Late native service initialization obtains Meyer limits without an added hook.");
            ((IDisposable)owner).Dispose(); owner = null;
            Check((float)Get(Report()[waterKey], "maxValue") == 4500, "A late-created native station cache restores stock capacity on exit too.");
            string hash;
            using (var sha = SHA256.Create()) using (var file = File.OpenRead(dll)) hash = BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "");
            File.WriteAllText(Path.Combine(output, "meyer-refill-validation.json"), "{\"passed\":true,\"runtimeSha256\":\"" + hash + "\",\"checks\":" + checks +
                ",\"oldStockLimitReproduced\":true,\"nativePurchaseAndRefill\":true,\"cachedStationIdentityRetained\":true,\"nativeFractionSaveOrders\":true,\"immediateNativeMassRefresh\":true,\"uninitializedMassOwnerSafe\":true,\"gameScene\":false}");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(output, "failure.txt"), error.ToString()); Debug.LogException(error); EditorApplication.Exit(1); }
        finally { if (owner != null) ((IDisposable)owner).Dispose(); UnityEngine.Object.DestroyImmediate(root); AppDomain.CurrentDomain.AssemblyResolve -= resolve; }
    }
    private static object Get(object o, string name) => o.GetType().GetField(name, All).GetValue(o);
    private static void Set(object o, string name, object value) => o.GetType().GetField(name, All).SetValue(o, value);
    private static float Amount(object container) => (float)Get(container, "amountReadOut").GetType().GetProperty("Value").GetValue(Get(container, "amountReadOut"));
    private static void Value(object port, float value) => port.GetType().GetMethod("ExternalValueUpdate").Invoke(port, new object[] { value });
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); checks++; }
}
#endif
