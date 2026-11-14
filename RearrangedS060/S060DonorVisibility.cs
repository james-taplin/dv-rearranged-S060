#if B99_S060_ARTICULATED
using DV;
using HarmonyLib;

namespace RearrangedS060
{
    // Keep the CCL donors registered for radio layouts and Shay effects.
    // Set the native visibility flag before the spawner caches its choices.
    [HarmonyPatch(typeof(CommsRadioCarSpawner), "UpdateCarLiveriesToSpawn")]
    internal static class S060DonorVisibility
    {
        private static void Prefix()
        {
            var types = Globals.G?.Types;
            if (types == null) return;
            if (types.TryGetLivery("RS060_Meyer_0440", out var meyer)) meyer.isHidden = true;
            if (types.TryGetLivery("RS060_Franklin_Dockside_0440", out var dockside)) dockside.isHidden = true;
        }
    }
}
#endif
