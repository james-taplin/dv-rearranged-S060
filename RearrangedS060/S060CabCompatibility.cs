#if B99_MEYER
using System;
using System.Linq;
using System.Reflection;
using DV.Simulation.Cars;
using DV.Simulation.Controllers;
using DV.Util;
using LocoSim.Definitions;
using UnityEngine;

namespace RearrangedS060
{
    // The generic CCL steam wizard does not supply S060 cab doors/windows or
    // body-to-cab pose anchors. Reuse that cab's actual native configuration.
    internal static class S060CabCompatibility
    {
        internal static void Prepare(TrainCar car, GameObject donor)
        {
            var sim = car.GetComponent<SimController>();
            var sourceSim = donor.GetComponent<SimController>();
            if (sim?.connectionsDefinition == null || sourceSim?.connectionsDefinition == null)
                throw new InvalidOperationException("Native S060 cab simulation definitions are missing.");
            if (car.transform.Find("[S060 cab compatibility]") != null) return;
            var root = new GameObject("[S060 cab compatibility]"); root.transform.SetParent(car.transform, false);
            var original = sim.connectionsDefinition;
            var graph = root.AddComponent<SimConnectionDefinition>();
            var order = original.executionOrder.ToList();
            var refs = original.portReferenceConnections.Select(r => new PortReferenceConnection(r.portReferenceId, r.portId)).ToList();
            var native = sourceSim.connectionsDefinition;
            void Include(string id)
            {
                if (order.Any(d => d.ID == id)) return;
                var definition = native.executionOrder.SingleOrDefault(d => d.ID == id)
                    ?? throw new InvalidOperationException("Native S060 cab definition missing: " + id);
                // Definition data is read-only. Each native Instantiate call
                // creates a separate implementation, as for original graph data.
                order.Add(definition);
                foreach (var reference in native.portReferenceConnections.Where(r => r.portReferenceId.StartsWith(id + ".", StringComparison.Ordinal)))
                {
                    Include(reference.portId.Split('.')[0]);
                    refs.Add(new PortReferenceConnection(reference.portReferenceId, reference.portId));
                }
            }
            foreach (string id in new[] { "hatchL", "hatchR", "windowL", "windowR", "sunroof", "doorL", "doorR" }) Include(id);
            foreach (var control in car.GetComponentsInChildren<OverridableBaseControl>(true).Where(c => string.IsNullOrEmpty(c.portId)))
            {
                var source = donor.GetComponentsInChildren<OverridableBaseControl>(true).SingleOrDefault(c => c.ControlType == control.ControlType && !string.IsNullOrEmpty(c.portId));
                if (source == null) throw new InvalidOperationException("Native S060 control binding missing: " + control.ControlType);
                control.portId = source.portId; Include(source.portId.Split('.')[0]);
            }
            graph.executionOrder = order.ToArray(); graph.portReferenceConnections = refs.ToArray();
            graph.connections = original.connections.Select(c => new Connection(c.fullPortIdOut, c.fullPortIdIn)).ToArray();
            sim.connectionsDefinition = graph;

            var provider = car.GetComponent<PositionSyncProviderController>() ?? car.gameObject.AddComponent<PositionSyncProviderController>();
            var anchors = donor.GetComponentsInChildren<PositionSyncProvider>(true);
            if (anchors.Length == 0) throw new InvalidOperationException("Native S060 cab pose anchors missing.");
            provider.entries = anchors.Select(source =>
            {
                var go = new GameObject("S060 cab anchor " + source.syncTag); go.transform.SetParent(root.transform, false);
                go.transform.localPosition = donor.transform.InverseTransformPoint(source.transform.position);
                go.transform.localRotation = Quaternion.Inverse(donor.transform.rotation) * source.transform.rotation;
                var entry = go.AddComponent<PositionSyncProvider>(); entry.syncTag = source.syncTag; return entry;
            }).ToArray();
            if (car.GetComponentInChildren<LubricatorRatchetDriver>(true) == null)
            {
                var wheel = car.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Contains("driver R"));
                if (wheel == null) throw new InvalidOperationException("Animated wheel for native lubricator marker missing.");
                wheel.gameObject.AddComponent<LubricatorRatchetDriver>();
            }
            // The native explosion helper needs a point in this car's space.
            // Copy its datum, never the donor Transform reference itself.
            foreach (var component in car.GetComponentsInChildren<Component>(true).Where(c => c != null))
            {
                var field = component.GetType().GetField("explosionAnchor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field?.FieldType != typeof(Transform) || field.GetValue(component) != null) continue;
                var originalComponent = donor.GetComponentsInChildren<Component>(true).FirstOrDefault(c => c != null && c.GetType() == component.GetType());
                var source = originalComponent == null ? null : field.GetValue(originalComponent) as Transform;
                if (source == null) continue;
                var point = new GameObject("S060 explosion datum").transform; point.SetParent(root.transform, false);
                point.localPosition = donor.transform.InverseTransformPoint(source.position);
                point.localRotation = Quaternion.Inverse(donor.transform.rotation) * source.rotation; field.SetValue(component, point);
            }
            Main.Logger.Log("S060 native cab compatibility: seven hatch/door/window definitions and " + anchors.Length + " pose providers, headlight controls and lubricator marker.");
        }
    }
}
#endif
