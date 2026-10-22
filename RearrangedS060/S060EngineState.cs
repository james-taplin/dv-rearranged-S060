#if B99_S060_ARTICULATED
using System;
using System.Linq;
using System.Reflection;
using LocoSim.Implementations;
using Newtonsoft.Json.Linq;

namespace RearrangedS060
{
    // Native DV saves only cylinder cracks. Save the native transient state too,
    // so a load or an arrangement change cannot empty a chest or cure damage.
    internal static class S060EngineState
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly string[] StateFields = {
            "steamChestSteamMass", "crankRotation", "prevCrankRotation", "torqueVelocity",
            "steamMassPerChuff", "admissionPressure", "waterInCylindersDamageMeter",
            "temperatureInCylinders", "cutoff", "condensationExpansionRatio", "isReverserForward"
        };
        private static readonly string[] CylinderFields = {
            "isInletValveOpen", "positionWithinStroke", "crankOffset",
            "isCrankAboveHorizontal", "prevIsCrankAboveHorizontal", "admissionPressure"
        };
        private sealed class Schema
        {
            internal readonly FieldInfo[] Engine, Cylinder;
            internal Schema()
            {
                Engine = CheckedFields(typeof(ReciprocatingSteamEngine), StateFields,
                    name => name == "isReverserForward", false);
                var cylinder = typeof(ReciprocatingSteamEngine).GetNestedType("CylinderData", BindingFlags.Public | BindingFlags.NonPublic);
                Cylinder = CheckedFields(cylinder, CylinderFields,
                    name => name == "isInletValveOpen" || name == "isCrankAboveHorizontal" || name == "prevIsCrankAboveHorizontal", true);
            }
        }
        // Version 1 is an explicit B99 schema. New native fields must be assessed
        // and migrated deliberately, rather than silently changing saved JSON.
        private static readonly Lazy<Schema> SavedSchema = new Lazy<Schema>(() => new Schema());
        internal static FieldInfo[] CheckedFields(Type type, string[] names, Func<string, bool> isBoolean, bool exactScalars)
        {
            const string message = "S060 version-1 engine-save schema is incompatible with this native game build: ";
            if (type == null) throw new InvalidOperationException(message + "CylinderData type missing.");
            var result = names.Select(name => type.GetField(name, Fields)).ToArray();
            for (int i = 0; i < result.Length; i++)
                if (result[i] == null || result[i].IsInitOnly || result[i].FieldType != (isBoolean(names[i]) ? typeof(bool) : typeof(float)))
                    throw new InvalidOperationException(message + type.Name + "." + names[i] + " missing or changed type.");
            if (exactScalars && !type.GetFields(Fields).Where(f => f.FieldType == typeof(float) || f.FieldType == typeof(bool))
                .Select(f => f.Name).OrderBy(n => n, StringComparer.Ordinal).SequenceEqual(names.OrderBy(n => n, StringComparer.Ordinal)))
                throw new InvalidOperationException(message + type.Name + " scalar fields changed; a save migration is required.");
            return result;
        }
        internal static JObject Save(ReciprocatingSteamEngine engine)
        {
            var schema = SavedSchema.Value;
            var fields = new JObject();
            foreach (var field in schema.Engine) fields[field.Name] = JToken.FromObject(field.GetValue(engine));
            var ports = new JObject();
            foreach (var port in engine.GetAllPorts().Where(p => p != engine.healthStateExtIn))
                ports[Suffix(port.id)] = port.Value;
            var cylinders = new JArray();
            foreach (var cylinder in engine.cylinderData)
            {
                var state = new JObject();
                foreach (var f in schema.Cylinder)
                    state[f.Name] = JToken.FromObject(f.GetValue(cylinder));
                cylinders.Add(state);
            }
            return new JObject { ["version"] = 1, ["fields"] = fields, ["ports"] = ports, ["cylinders"] = cylinders };
        }
        internal static void Load(ReciprocatingSteamEngine engine, JObject data)
        {
            var schema = SavedSchema.Value;
            // Validate everything before touching this native engine.
            if ((int?)data?["version"] != 1 || !(data["fields"] is JObject fields) ||
                !(data["ports"] is JObject ports) || !(data["cylinders"] is JArray cylinders) || cylinders.Count != engine.numCylinders)
                throw new InvalidOperationException("Unsupported or incomplete S060 native engine state.");
            foreach (var field in schema.Engine) Validate(fields[field.Name], field.FieldType);
            foreach (var port in engine.GetAllPorts().Where(p => p != engine.healthStateExtIn)) Validate(ports[Suffix(port.id)], typeof(float));
            for (int i = 0; i < cylinders.Count; i++)
                foreach (var f in schema.Cylinder)
                    Validate(cylinders[i][f.Name], f.FieldType);
            if ((float)fields["steamChestSteamMass"] < 0 || (float)fields["waterInCylindersDamageMeter"] < 0)
                throw new InvalidOperationException("Negative S060 stored steam or cylinder damage.");
            foreach (var f in schema.Engine) f.SetValue(engine, fields[f.Name].ToObject(f.FieldType));
            foreach (var port in engine.GetAllPorts().Where(p => p != engine.healthStateExtIn)) port.Value = (float)ports[Suffix(port.id)];
            for (int i = 0; i < cylinders.Count; i++)
                foreach (var f in schema.Cylinder)
                    f.SetValue(engine.cylinderData[i], cylinders[i][f.Name].ToObject(f.FieldType));
        }
        internal static string Suffix(string id) => id.Substring(id.LastIndexOf('.') + 1);
        private static void Validate(JToken token, Type type)
        {
            if (type == typeof(bool)) { if (token?.Type == JTokenType.Boolean) return; }
            else if (token?.Type == JTokenType.Integer || token?.Type == JTokenType.Float)
            {
                double value = (double)token;
                if (!double.IsNaN(value) && !double.IsInfinity(value) && Math.Abs(value) < 1e12) return;
            }
            throw new InvalidOperationException("Invalid S060 native engine state value.");
        }
        internal static void CopyCondition(ReciprocatingSteamEngine source, ReciprocatingSteamEngine target, float phase)
        {
            target.waterInCylindersNormalizedReadOut.Value = source.waterInCylindersNormalizedReadOut.Value;
            target.waterInCylindersDamageMeter = source.waterInCylindersDamageMeter;
            target.temperatureInCylinders = source.temperatureInCylinders;
            target.cylinderTemperatureReadOut.Value = source.cylinderTemperatureReadOut.Value;
            target.isCylinderCrackedReadOut.Value = source.isCylinderCrackedReadOut.Value;
            target.crankRotation = target.prevCrankRotation = phase;
            target.crankRotationReadOut.Value = phase;
            // Stored steam and energy belong to each circuit and stay in place.
        }
    }
}
#endif
