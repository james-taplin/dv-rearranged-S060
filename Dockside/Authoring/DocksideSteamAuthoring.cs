using System;
using System.Linq;
using CCL.Types.Components;
using CCL.Types.Proxies.Ports;
using CCL.Types.Proxies.Simulation.Steam;
using UnityEngine;

// Configuration only: both actual native engines receive the same boiler and
// cab controls. No compound adapter or second steam simulation is required.
public static class DocksideSteamAuthoring
{
    public static void Apply(GameObject root, Transform front, Transform rear)
    {
        // Reuse the proven wizard/control, lubrication, damage and cock recipe,
        // then change only the serial-compound supply/consumer connections.
        MeyerSteamAuthoring.Apply(root, front, rear, false);
        var graph = root.GetComponentInChildren<SimConnectionsDefinitionProxy>(true);
        foreach (var engine in root.GetComponentsInChildren<ReciprocatingSteamEngineDefinitionProxy>(true))
        {
            engine.numCylinders = 2; engine.cylinderBore = DocksidePlan.Bore;
            engine.pistonStroke = DocksidePlan.Stroke; engine.steamChestVolume = DocksidePlan.ChestLitres;
            engine.throttleMaxFlow = DocksidePlan.MaxFlow;
            engine.OnValidate();
        }
        foreach (string suffix in new[] { "THROTTLE_CONTROL", "INTAKE_PRESSURE", "INTAKE_TEMPERATURE", "INTAKE_QUALITY" })
            Reference(graph, MeyerSteamAuthoring.Lp + "." + suffix,
                graph.portReferenceConnections.Single(c => c.portReferenceId == MeyerSteamAuthoring.Hp + "." + suffix).portId);
        Sum(graph, "docksideIntake", DVPortValueType.MASS_RATE, "INTAKE_FLOW");
        Sum(graph, "docksideExhaust", DVPortValueType.MASS_RATE, "EXHAUST_FLOW");
        Sum(graph, "docksideMaxFlow", DVPortValueType.MASS_RATE, "MAX_FLOW");
        Reference(graph, "steamConsumptionCalculator.ENGINE", "docksideIntake.OUT");
        Reference(graph, "exhaust.EXHAUST_FLOW", "docksideExhaust.OUT");
        Reference(graph, "exhaust.ENGINE_MAX_FLOW", "docksideMaxFlow.OUT");
        // The two constant ports from the serial recipe are not part of this circuit.
        foreach (var component in graph.executionOrder.Where(c => c.ID == "meyerLpAdmission" || c.ID == "meyerReceiverQuality").ToArray())
        {
            graph.executionOrder.Remove(component);
            UnityEngine.Object.DestroyImmediate(component.gameObject);
        }
        graph.OnValidate();
        Validate(root);
    }

    private static void Reference(SimConnectionsDefinitionProxy graph, string reference, string source)
        => graph.portReferenceConnections.Single(c => c.portReferenceId == reference).portId = source;

    private static void Sum(SimConnectionsDefinitionProxy graph, string id, DVPortValueType type, string output)
    {
        var sum = graph.executionOrder.OfType<MultiplePortSumDefinitionProxy>().SingleOrDefault(c => c.ID == id);
        if (sum == null)
        {
            var go = new GameObject(id); go.transform.SetParent(graph.transform, false);
            sum = go.AddComponent<MultiplePortSumDefinitionProxy>(); sum.ID = id;
            sum.inputs = new[] { new PortReferenceDefinition(type, "REAR"), new PortReferenceDefinition(type, "FRONT") };
            sum.output = new PortDefinition(DVPortType.OUT, type, "OUT"); sum.OnValidate();
            graph.portReferenceConnections.Add(new PortReferenceConnectionProxy { portReferenceId = id + ".REAR", portId = MeyerSteamAuthoring.Hp + "." + output });
            graph.portReferenceConnections.Add(new PortReferenceConnectionProxy { portReferenceId = id + ".FRONT", portId = MeyerSteamAuthoring.Lp + "." + output });
        }
        graph.executionOrder.RemoveAll(c => c == sum);
        graph.executionOrder.Insert(graph.executionOrder.FindIndex(c => c.ID == MeyerSteamAuthoring.Lp) + 1, sum);
    }

    public static void Validate(GameObject root)
    {
        var graph = root.GetComponentInChildren<SimConnectionsDefinitionProxy>(true);
        var engines = graph.executionOrder.OfType<ReciprocatingSteamEngineDefinitionProxy>().ToArray();
        Require(engines.Length == 2 && engines.All(e => e.numCylinders == 2 && e.cylinderBore == DocksidePlan.Bore && e.pistonStroke == DocksidePlan.Stroke), "Two equal native simple-expansion cylinder pairs required.");
        foreach (string suffix in new[] { "THROTTLE_CONTROL", "REVERSER_CONTROL", "CYLINDER_COCK_CONTROL", "INTAKE_PRESSURE", "INTAKE_TEMPERATURE", "INTAKE_QUALITY", "CRANK_RPM", "LUBRICATION_NORMALIZED" })
            Require(Source(graph, MeyerSteamAuthoring.Hp + "." + suffix) == Source(graph, MeyerSteamAuthoring.Lp + "." + suffix), "Both engines must share native supply/control: " + suffix);
        Require(Source(graph, MeyerSteamAuthoring.Hp + ".CYLINDER_COCK_CONTROL") == "cylinderCock.EXT_IN", "Both cocks must use the cab lever.");
        Require(Source(graph, "steamConsumptionCalculator.ENGINE") == "docksideIntake.OUT", "Both engines must be charged to boiler demand.");
        Require(Source(graph, "exhaust.EXHAUST_FLOW") == "docksideExhaust.OUT", "Both exhausts must reach the blast pipe.");
        Require(Source(graph, "exhaust.ENGINE_MAX_FLOW") == "docksideMaxFlow.OUT", "Blast normalization must include both engines' maximum flow.");
        Require(graph.connections.Single(c => c.fullPortIdIn == "traction.TORQUE_IN").fullPortIdOut == "meyerTorque.OUT", "Both engine torques must feed the native traction input.");
        foreach (var spec in new[] { new[] { "docksideIntake", "INTAKE_FLOW" }, new[] { "docksideExhaust", "EXHAUST_FLOW" }, new[] { "docksideMaxFlow", "MAX_FLOW" }, new[] { "meyerTorque", "TORQUE_OUT" } })
        {
            var sum = graph.executionOrder.OfType<MultiplePortSumDefinitionProxy>().Single(c => c.ID == spec[0]);
            Require(sum.inputs.Length == 2, "Each summed quantity requires both engines.");
            var sources = graph.portReferenceConnections.Where(c => c.portReferenceId.StartsWith(sum.ID + ".", StringComparison.Ordinal)).Select(c => c.portId).ToArray();
            Require(sources.Length == 2 && sources.Contains(MeyerSteamAuthoring.Hp + "." + spec[1]) && sources.Contains(MeyerSteamAuthoring.Lp + "." + spec[1]), "Wrong summation inputs: " + sum.ID);
            Require(graph.executionOrder.IndexOf(sum) > graph.executionOrder.FindIndex(c => c.ID == MeyerSteamAuthoring.Lp), "Sum must run after both engines.");
        }
        Require(graph.executionOrder.Select(c => c.ID).Distinct().Count() == graph.executionOrder.Count, "Execution IDs must be unique.");
        Require(!graph.portReferenceConnections.Any(c => c.portId == MeyerSteamAuthoring.Hp + ".EXHAUST_PRESSURE" || c.portId == "meyerLpAdmission.VALUE" || c.portId == "meyerReceiverQuality.VALUE"), "Serial-compound connections must not remain.");
    }
    private static string Source(SimConnectionsDefinitionProxy graph, string id) => graph.portReferenceConnections.Single(c => c.portReferenceId == id).portId;
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
