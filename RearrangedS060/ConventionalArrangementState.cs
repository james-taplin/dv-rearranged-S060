#if B99_ALL_STANDARD
using System;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace RearrangedS060
{
    // Identity and selection policy is separate from Unity initialization. A
    // pending choice survives saving, while an explicit radio choice replaces it.
    internal sealed class ConventionalArrangementState
    {
        internal const string SaveKey = "RearrangedS060";
        private const int SaveVersion = 1;
        private JToken unrecognizedPayload;
        internal string Guid { get; private set; }
        internal bool HasSelection { get; private set; }
        internal int? PendingId { get; private set; }
        internal int AppliedId { get; private set; } = (int)WheelArrangementType.s282;

        internal bool BeginIdentity(string guid)
        {
            if (string.IsNullOrEmpty(guid) || string.Equals(Guid, guid, StringComparison.Ordinal)) return false;
            Reset();
            Guid = guid;
            return true;
        }

        internal void Reset()
        {
            Guid = null;
            HasSelection = false;
            PendingId = null;
            AppliedId = (int)WheelArrangementType.s282;
            unrecognizedPayload = null;
        }

        internal void SelectInitial(Settings settings, Random random)
        {
            if (HasSelection) return;
            PendingId = settings != null && settings.spawnRandomWA
                ? settings.PickRandomArrangement(random) : (int)WheelArrangementType.s282;
            HasSelection = true;
        }

        // Unknown/corrupt future data is retained verbatim until the player
        // explicitly chooses a supported arrangement.
        internal bool LoadPayload(JToken payload)
        {
            HasSelection = true;
            if (payload == null)
            {
                // A save predating this extension is an existing stock car,
                // not a new spawn to randomize retroactively.
                PendingId = (int)WheelArrangementType.s282;
                unrecognizedPayload = null;
                return true;
            }
            var data = payload as JObject;
            var version = data?["version"];
            var arrangement = data?["arrangement"];
            if (version?.Type == JTokenType.Integer && arrangement?.Type == JTokenType.Integer &&
                int.TryParse(version.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int format) &&
                int.TryParse(arrangement.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) &&
                format == SaveVersion && WheelArrangement.IsSupportedId(id))
            {
                PendingId = id;
                unrecognizedPayload = null;
                return true;
            }
            PendingId = null;
            unrecognizedPayload = payload.DeepClone();
            return false;
        }

        internal void RecordApplied(int id)
        {
            if (!WheelArrangement.IsSupportedId(id)) throw new ArgumentOutOfRangeException(nameof(id));
            AppliedId = id;
            HasSelection = true;
            PendingId = null;
            unrecognizedPayload = null;
        }

        internal JToken SavePayload()
        {
            return unrecognizedPayload?.DeepClone() ?? new JObject
            {
                ["version"] = SaveVersion,
                ["arrangement"] = PendingId ?? AppliedId
            };
        }
    }
}
#endif
