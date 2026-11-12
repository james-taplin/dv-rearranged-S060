using System;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace RearrangedS060
{
    // Native simCarState owns this payload, so GUID reuse and separate saves
    // cannot inherit a process-global dictionary's stale selection.
    internal sealed class S060SavedState
    {
        internal const string SaveKey = "RearrangedS060";
        private JToken unrecognized;
        internal int[] WheelStates { get; private set; }
        internal string Guid { get; private set; }
        internal bool HasSelection { get; private set; }
        internal S060Selection? Pending { get; private set; }
        internal S060Selection Applied { get; private set; } = S060Selection.Stock;

        internal void BeginIdentity(string guid)
        {
            if (string.IsNullOrEmpty(guid)) throw new ArgumentException("A car GUID is required.");
            if (string.Equals(Guid, guid, StringComparison.Ordinal)) return;
            Reset();
            Guid = guid;
        }

        internal void Reset()
        {
            Guid = null;
            HasSelection = false;
            Pending = null;
            Applied = S060Selection.Stock;
            unrecognized = null;
            WheelStates = null;
        }

        internal void SelectInitial(S060Selection selection)
        {
            if (HasSelection) return;
            Pending = selection;
            HasSelection = true;
        }

        internal bool Load(JToken payload)
        {
            HasSelection = true;
            Applied = S060Selection.Stock;
            unrecognized = null;
            WheelStates = null;
            if (payload == null)
            {
                // Existing saves without mod data stay stock, even when random
                // spawning is enabled. Only newly created cars are randomized.
                Pending = S060Selection.Stock;
                return true;
            }
            var data = payload as JObject;
            var version = data?["version"];
            var id = data?["arrangement"];
            var scale = data?["driverScale"];
            var wheelToken = data?["wheelStates"];
            int[] wheels = null;
            bool validWheels = wheelToken == null;
            if (wheelToken is JArray array && (array.Count == 4 || array.Count == 5))
            {
                wheels = new int[array.Count]; validWheels = true;
                for (int i = 0; i < array.Count; i++)
                    if (array[i].Type != JTokenType.Integer || !int.TryParse(array[i].ToString(), out wheels[i])) validWheels = false;
                validWheels &= S060WheelSlots.Valid(wheels);
            }
            if (version?.Type == JTokenType.Integer && (version.ToString() == "1"
#if B99_S060_ARTICULATED
                || version.ToString() == "2" || version.ToString() == "3" || version.ToString() == "4"
#endif
                ) &&
                id?.Type == JTokenType.Integer &&
                int.TryParse(id.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) &&
                S060Selection.IsSupported(value) && validWheels &&
                (version.ToString() == "1" ? value <= 3 && wheelToken == null :
                    version.ToString() == "2" ? value <= 5 && (wheels == null || wheels.Length == 4) && (value >= 4 || wheels != null) :
                    version.ToString() == "3" ? value <= 6 && (value == 6 || wheels?.Length == 5) : value == 7) &&
                (scale?.Type == JTokenType.Float || scale?.Type == JTokenType.Integer) &&
                float.TryParse(scale.ToString(Newtonsoft.Json.Formatting.None), NumberStyles.Float, CultureInfo.InvariantCulture, out float factor) &&
                S060Selection.IsValidScale(factor) && (!S060Selection.RequiresFixedSize((S060Arrangement)value) || factor == 1f))
            {
                Pending = new S060Selection((S060Arrangement)value, factor);
                WheelStates = wheels;
                return true;
            }
            Pending = null;
            unrecognized = payload.DeepClone();
            return false;
        }

        internal void RecordApplied(S060Selection selection)
        {
            if (!S060Selection.IsSupported((int)selection.Arrangement) || !S060Selection.IsValidScale(selection.Scale))
                throw new ArgumentException("Invalid S060 selection.", nameof(selection));
            Applied = selection;
            Pending = null;
            HasSelection = true;
            unrecognized = null;
        }

        internal void RecordWheelStates(int[] states)
        {
            if (unrecognized != null) return;
            if (!S060WheelSlots.Valid(states)) throw new ArgumentException("Invalid S060 wheel condition.");
            WheelStates = (int[])states.Clone();
        }
        internal JToken Save()
        {
            if (unrecognized != null) return unrecognized.DeepClone();
            var data = new JObject
            {
                ["version"] = (Pending ?? Applied).Arrangement == S060Arrangement.Shay ? 4 : WheelStates?.Length == 5 || (Pending ?? Applied).Arrangement == S060Arrangement.TenCoupled ? 3 :
                    WheelStates != null || (int)(Pending ?? Applied).Arrangement >= 4 ? 2 : 1,
                ["arrangement"] = (int)(Pending ?? Applied).Arrangement,
                ["driverScale"] = (Pending ?? Applied).Scale
            };
            if (WheelStates != null) data["wheelStates"] = new JArray(WheelStates);
            return data;
        }
    }
}
