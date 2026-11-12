#if B99_S060_FUNCTIONAL
using System.Collections.Generic;
using System.Reflection;
using LocoSim.Definitions;

namespace RearrangedS060
{
    // Native cup components serialize their port IDs as string fields. Rewrite
    // only the five exact source IDs; labels, sync tags and unrelated ports stay.
    internal static class S060OilCupPorts
    {
        internal static Dictionary<string, string> Map(string component,
            ManualOilingPointsDefinition.OilingPointDefinition from,
            ManualOilingPointsDefinition.OilingPointDefinition to) => new Dictionary<string, string>
        {
            [component + "." + from.oilLevelReadOut.ID] = component + "." + to.oilLevelReadOut.ID,
            [component + "." + from.oilLevelNormalizedReadOut.ID] = component + "." + to.oilLevelNormalizedReadOut.ID,
            [component + "." + from.pointDoorExtIn.ID] = component + "." + to.pointDoorExtIn.ID,
            [component + "." + from.refillExtIn.ID] = component + "." + to.refillExtIn.ID,
            [component + "." + from.refillingFlowNormalizedReadOut.ID] = component + "." + to.refillingFlowNormalizedReadOut.ID
        };

        internal static void Rewrite(object component, Dictionary<string, string> map)
        {
            foreach (var field in component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                if (field.FieldType == typeof(string) && field.GetValue(component) is string value && map.TryGetValue(value, out var replacement))
                    field.SetValue(component, replacement);
        }
    }
}
#endif
