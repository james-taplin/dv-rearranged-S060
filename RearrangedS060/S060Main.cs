#if B99_S060_FUNCTIONAL
using System;
using HarmonyLib;
using UnityModManagerNet;

namespace RearrangedS060
{
    public static class Main
    {
        public static UnityModManager.ModEntry.ModLogger Logger { get; private set; }
        public static string ModPath { get; private set; }
        internal static S060Settings Options { get; private set; }

        private static bool Load(UnityModManager.ModEntry entry)
        {
            Logger = entry.Logger;
            ModPath = entry.Path;
            var harmony = new Harmony(entry.Info.Id);
            try
            {
                Options = UnityModManager.ModSettings.Load<S060Settings>(entry);
                harmony.CreateClassProcessor(typeof(Patches.BogieAxlesPatch)).Patch();
                harmony.CreateClassProcessor(typeof(S060RadioRegistration)).Patch();
                S060Oiling.Register(harmony);
                S060Lifecycle.Register(harmony);
#if B99_S060_ARTICULATED
                S060ArticulatedSimulation.Register(harmony);
                S060ExhaustPresentation.Register(harmony);
                S060NativeAudio.Register(harmony);
                S060StockCocks.Register(harmony);
                S060WheelCondition.Register(harmony);
                S060MeyerSupport.Register(harmony);
#endif
#if B99_MEYER
                MeyerRuntime.Register(harmony);
#endif
                entry.OnGUI = mod => S060OptionsWindow.Draw();
                entry.OnSaveGUI = mod => Options.Save(mod);
                // Conversions and per-car state must not outlive their patches.
                entry.OnUnload = mod => { Logger.Log("Restart the game to unload Rearranged S060."); return false; };
                S060RadioRegistration.AttachExisting();
                Logger.Log("S060 radio/settings build loaded: fitted layouts and per-car saves. Articulated choices require the matching Meyer/Dockside asset packs.");
                return true;
            }
            catch (Exception ex)
            {
                harmony.UnpatchAll(entry.Info.Id);
                Logger.LogException("S060 initialization failed", ex);
                return false;
            }
        }
    }
}
#endif
