#if B99_S060_ARTICULATED
using System;
using System.Collections.Generic;
using System.Linq;
using LocoSim.Definitions;
using LocoSim.Implementations;
using UnityEngine;

namespace RearrangedS060
{
    // Native configurable outputs are registered before the simulation is built.
    // Their Tick is empty; Multiplayer's normal port stream owns replication.
    internal sealed class S060IndependentDriveReplication
    {
        internal const int MaximumGroups = 3;
        internal const string Prefix = "rs060IndependentDrive";
        private static readonly string[] Fields = { "Active", "Rpm", "Slip", "PhaseLeft", "PhaseRight", "Force", "Limit" };
        internal sealed class Channel
        {
            internal Port Active, Rpm, Slip, PhaseLeft, PhaseRight, Force, Limit;
            internal bool Valid => Active.Value == 1f && Finite(Rpm.Value) && Finite(Slip.Value) &&
                Slip.Value >= 0f && Slip.Value <= 1f && Finite(PhaseLeft.Value) && Finite(PhaseRight.Value) &&
                PhaseLeft.Value >= 0f && PhaseLeft.Value < 1f && PhaseRight.Value >= 0f && PhaseRight.Value < 1f &&
                Finite(Force.Value) && Finite(Limit.Value) && Limit.Value >= 0f;
            internal void Publish(bool active, float rpm, float slip, float phaseLeft, float phaseRight, float force, float limit)
            {
                Set(Rpm, rpm); Set(Slip, slip); Set(PhaseLeft, phaseLeft); Set(PhaseRight, phaseRight);
                Set(Force, force); Set(Limit, limit); Set(Active, active ? 1f : 0f);
            }
        }
        internal readonly Channel[] Groups;
        private readonly Port count;
        private readonly Port[] revision, epoch, carGuid;
        private readonly ushort[] identity;

        internal static SimComponentDefinition[] AppendDefinitions(GameObject owner, SimComponentDefinition[] order)
        {
            if (owner == null || order == null || order.Any(d => d == null || string.IsNullOrEmpty(d.ID) || d.ID.StartsWith(Prefix, StringComparison.Ordinal)))
                throw new InvalidOperationException("Independent drive replication definitions already exist or their owner is missing.");
            var result = new List<SimComponentDefinition>(order);
            void Add(string suffix)
            {
                var definition = owner.AddComponent<ConfigurablePortDefinition>();
                definition.ID = Prefix + suffix;
                definition.value = 0f;
                definition.port = new PortDefinition(PortType.READONLY_OUT, PortValueType.STATE, "OUT");
                result.Add(definition);
            }
            for (int group = 0; group < MaximumGroups; group++) foreach (string field in Fields) Add(group + field);
            Add("GroupCount");
            for (int part = 0; part < 4; part++) Add("Revision" + part);
            for (int part = 0; part < 4; part++) Add("Epoch" + part);
            for (int part = 0; part < 8; part++) Add("CarGuid" + part);
            return result.ToArray();
        }

        internal S060IndependentDriveReplication(SimulationFlow flow, string carIdentity)
        {
            if (flow == null) throw new ArgumentNullException(nameof(flow));
            if (!Guid.TryParse(carIdentity, out var guid)) throw new ArgumentException("Independent drive requires the native car GUID.", nameof(carIdentity));
            byte[] bytes = guid.ToByteArray();
            identity = Enumerable.Range(0, 8).Select(i => (ushort)(bytes[2*i] | bytes[2*i+1] << 8)).ToArray();
            Port Find(string suffix)
            {
                if (!flow.TryGetPort(Prefix + suffix + ".OUT", out var port))
                    throw new InvalidOperationException("Independent drive replication output is missing: " + suffix);
                return port;
            }
            Groups = Enumerable.Range(0, MaximumGroups).Select(i => new Channel {
                Active = Find(i + "Active"), Rpm = Find(i + "Rpm"), Slip = Find(i + "Slip"),
                PhaseLeft = Find(i + "PhaseLeft"), PhaseRight = Find(i + "PhaseRight"),
                Force = Find(i + "Force"), Limit = Find(i + "Limit")
            }).ToArray();
            count = Find("GroupCount");
            revision = Enumerable.Range(0, 4).Select(i => Find("Revision" + i)).ToArray();
            epoch = Enumerable.Range(0, 4).Select(i => Find("Epoch" + i)).ToArray();
            carGuid = Enumerable.Range(0, 8).Select(i => Find("CarGuid" + i)).ToArray();
        }

        internal bool Matches(long expectedEpoch, long expectedRevision, int expectedGroups)
        {
            if (expectedEpoch <= 0 || expectedRevision <= 0 || count.Value != expectedGroups || expectedGroups < 2 || expectedGroups > MaximumGroups) return false;
            for (int i = 0; i < identity.Length; i++)
            {
                if (!Word(carGuid[i].Value, out var word) || word != identity[i]) return false;
            }
            return MatchesWords(epoch, expectedEpoch) && MatchesWords(revision, expectedRevision) && Groups.Take(expectedGroups).All(g => g.Valid);
        }

        internal void PublishMarker(long currentEpoch, long currentRevision, int groups)
        {
            if (!S060MultiplayerSync.IsSimulationAuthority) return;
            for (int i = 0; i < identity.Length; i++) Set(carGuid[i], identity[i]);
            PublishWords(epoch, currentEpoch);
            PublishWords(revision, currentRevision);
            Set(count, groups);
        }

        private static bool Word(float value, out ushort word)
        {
            word = 0;
            if (!Finite(value) || value < 0 || value > ushort.MaxValue || value != (ushort)value) return false;
            word = (ushort)value;
            return true;
        }
        private static bool MatchesWords(Port[] ports, long expected)
        {
            ulong received = 0;
            for (int i = 0; i < ports.Length; i++)
            {
                if (!Word(ports[i].Value, out var word)) return false;
                received |= (ulong)word << (16*i);
            }
            return received == (ulong)expected;
        }
        private static void PublishWords(Port[] ports, long value)
        {
            ulong positive = (ulong)Math.Max(0, value);
            for (int i = 0; i < ports.Length; i++) Set(ports[i], (ushort)(positive >> (16*i)));
        }

        internal void Clear()
        {
            if (!S060MultiplayerSync.IsSimulationAuthority) return;
            foreach (var group in Groups) Set(group.Active, 0f);
            Set(count, 0f);
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static void Set(Port port, float value)
        {
            if (!S060MultiplayerSync.IsSimulationAuthority) return;
            if (!Finite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (port.Value != value) port.Value = value;
        }
    }
}
#endif
