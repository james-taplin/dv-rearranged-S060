// Executes the production native-port adapter with narrow port/definition API
// substitutes. Live Multiplayer transport and rail dynamics need peer testing.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LocoSim.Definitions;
using LocoSim.Implementations;
using Newtonsoft.Json.Linq;
using RearrangedS060;
using UnityEngine;

internal static class PortProbe
{
    static readonly List<string> checks = new();
    static void Check(bool value, string label) { if (!value) throw new Exception(label); checks.Add(label); }
    static void Refuses(Action action, string label)
    {
        try { action(); } catch (InvalidOperationException) { Check(true, label); return; }
        throw new Exception(label);
    }
    static int Main(string[] args)
    {
        try
        {
            const long epoch = long.MaxValue;
            const string carId = "c2d9eb21-9e38-4b82-8e94-451d1d684ad3";
            var owner = new GameObject("native definitions");
            var original = owner.AddComponent<EmptyDefinition>(); original.ID = "stock";
            var order = S060IndependentDriveReplication.AppendDefinitions(owner, new SimComponentDefinition[] { original });
            Check(order.Length == 39 && ReferenceEquals(order[0], original), "Native registration preserves the stock definition and appends exactly 38 outputs");
            var defs = order.OfType<ConfigurablePortDefinition>().ToArray();
            Check(defs.All(d => d.value == 0 && d.port.Type == PortType.READONLY_OUT && d.port.ValueType == PortValueType.STATE && d.port.ID == "OUT"), "Every output is a dormant native configurable read-only state port");
            Check(defs.Select(d => d.ID).Distinct().Count() == 38, "Native port IDs are unique");
            Refuses(() => S060IndependentDriveReplication.AppendDefinitions(owner, order), "Repeated registration refuses duplicate native output definitions");
            var other = S060IndependentDriveReplication.AppendDefinitions(new GameObject("another car"), new SimComponentDefinition[0]);
            Check(other.Select(d => d.ID).SequenceEqual(defs.Select(d => d.ID)), "Independent cars and peers construct the same ordered native port IDs");
            var graph = owner.AddComponent<SimConnectionDefinition>(); graph.executionOrder = order;
            var flow = new SimulationFlow(graph); var ports = flow.OrderedSimComps.SelectMany(c => c.ports).ToArray();
            var replication = new S060IndependentDriveReplication(flow, carId);
            Check(!replication.Matches(epoch, 1, 2), "Dormant outputs cannot activate a replica before host state arrives");
            foreach (long revision in new[] { 1L, 65536L, 16777217L, 281474976710657L, long.MaxValue })
            {
                S060MultiplayerSync.IsSimulationAuthority = true;
                for (int i = 0; i < 3; i++) replication.Groups[i].Publish(true, 123 + i, .2f * i, .1f, .35f, 15000 + i, 20000 + i);
                replication.PublishMarker(epoch, revision, 3);
                Check(replication.Matches(epoch, revision, 3), "Positive 64-bit revision round-trips exactly through native floats: " + revision);
                Check(!replication.Matches(epoch, revision - 1, 3), "Older arrangement revision is rejected: " + revision);
            }
            Check(!replication.Matches(epoch, long.MaxValue, 2), "A different group layout cannot bind another layout's native outputs");
            Check(!replication.Matches(epoch - 1, long.MaxValue, 3), "Same-car outputs from a different Multiplayer session epoch are rejected");
            var pooled = new S060IndependentDriveReplication(flow, "8e1df210-166f-487a-8e70-3e94c5b20c88");
            Check(!pooled.Matches(epoch, long.MaxValue, 3), "A pooled flow retaining active outputs from another car GUID cannot activate the new car");
            Check(!replication.Matches(0, long.MaxValue, 3), "Pre-handshake session epoch cannot activate native replica outputs");
            var saved = ports.Select(p => p.Value).ToArray();
            S060MultiplayerSync.IsSimulationAuthority = false;
            replication.Groups[0].Publish(false, -99, 1, .9f, .15f, -10, 0);
            replication.PublishMarker(epoch, 123, 2); replication.Clear();
            Check(ports.Select(p => p.Value).SequenceEqual(saved), "Guest publication, clear and revision calls leave every native port untouched");
            S060MultiplayerSync.IsSimulationAuthority = true;
            var channel = replication.Groups[0];
            var valid = channel.Rpm.Value;
            channel.Rpm.Value = float.NaN;
            Check(!replication.Matches(epoch, long.MaxValue, 3), "Non-finite replicated RPM prevents replica activation"); channel.Rpm.Value = valid;
            channel.Slip.Value = 1.01f;
            Check(!replication.Matches(epoch, long.MaxValue, 3), "Out-of-range replicated slip prevents activation"); channel.Slip.Value = 0;
            channel.PhaseLeft.Value = 1f;
            Check(!replication.Matches(epoch, long.MaxValue, 3), "Out-of-range phase baseline prevents activation"); channel.PhaseLeft.Value = .1f;
            channel.Limit.Value = -1f;
            Check(!replication.Matches(epoch, long.MaxValue, 3), "Negative force limit prevents activation"); channel.Limit.Value = 20000;
            channel.Active.Value = 0;
            Check(!replication.Matches(epoch, long.MaxValue, 3), "Inactive host group prevents replica activation"); channel.Active.Value = 1;
            flow.TryGetPort(S060IndependentDriveReplication.Prefix + "Revision0.OUT", out var part);
            valid = part.Value; part.Value = 1.5f;
            Check(!replication.Matches(epoch, long.MaxValue, 3), "Fractional revision marker prevents activation"); part.Value = valid;
            Check(replication.Matches(epoch, long.MaxValue, 3), "Corrected native data can activate without recreating the adapter");
            flow.TryGetPort(S060IndependentDriveReplication.Prefix + "CarGuid7.OUT", out var guidPart);
            valid = guidPart.Value; guidPart.Value = (valid + 1) % 65536;
            Check(!replication.Matches(epoch, long.MaxValue, 3), "A mismatch in the final native GUID word prevents activation"); guidPart.Value = valid;
            flow.TryGetPort(S060IndependentDriveReplication.Prefix + "Epoch3.OUT", out var epochPart);
            valid = epochPart.Value; epochPart.Value = (valid + 1) % 65536;
            Check(!replication.Matches(epoch, long.MaxValue, 3), "A mismatch in the high native epoch word prevents activation"); epochPart.Value = valid;
            replication.Clear();
            Check(!replication.Matches(epoch, long.MaxValue, 3) && replication.Groups.All(g => g.Active.Value == 0), "Host teardown invalidates every group before another arrangement can bind");
            Check(channel.PhaseLeft.Value == .1f && channel.Rpm.Value == 123, "Host teardown preserves existing phase/RPM values and only clears ownership markers");
            Directory.CreateDirectory(args[0]);
            File.WriteAllText(Path.Combine(args[0], "native-port-adapter.json"), new JObject {
                ["passed"] = true, ["count"] = checks.Count, ["checks"] = new JArray(checks),
                ["limitations"] = "Production S060IndependentDriveReplication executes with narrow Unity/native API substitutes. Validates schema, exact revisions, data rejection and guest write suppression; no live Multiplayer transport, actual native port registration, animation, physics or lifecycle dispatch. Real graph and Unity animation fixtures run separately."
            }.ToString());
            Console.WriteLine("PASS " + checks.Count + " native port adapter checks"); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
