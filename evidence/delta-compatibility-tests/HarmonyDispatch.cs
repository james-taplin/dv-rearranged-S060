#if ACTUAL_HARMONY
using System;
using System.IO;
using System.Reflection;
using CarChanger.Common.Configs;
using CarChanger.Game.Components;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using RearrangedS282;
using S282ModelTweaks;

internal static partial class Program {
    public static int Main(string[] args) {
        var harmony=new Harmony("Rearranged.Delta.OfflineDispatch");
        try {
            // Recreate the installed patch order: optional mod hooks base methods
            // first; our adapter then patches those callback methods themselves.
            harmony.Patch(AccessTools.Method(typeof(AppliedChange),"ApplyS282730A"),
                postfix:new HarmonyMethod(typeof(PatchAppliedChange),"ApplyS282730A"));
            harmony.Patch(AccessTools.Method(typeof(AppliedChange),"ReturnToDefault"),
                postfix:new HarmonyMethod(typeof(PatchAppliedChange),"ReturnToDefault"));
            DeltaTrailingTruckCompatibility.Register(harmony);
            var delta=new Fixture("H-001");var config=delta.Owner.Config;
            delta.Owner.ApplyS282730A(config);
            Check(delta.Owner.BaseApplyCalls==1,"Real Harmony: Car Changer base apply remains active");
            Check(PatchAppliedChange.PhysicalApplyCalls==0,"Real Harmony: exact Delta callback body is skipped even when installed as a base postfix");
            Check(delta.Roots().Length==2,"Real Harmony: patched static postfix callback invokes Delta visual binding");
            Check(CarChanger.Game.ComponentProcessor.HideCalls==0,"Real Harmony: base apply context suppresses Delta native body hiding");
            Check(ReferenceEquals(delta.Owner.Config,config),"Real Harmony: optional mod's persistent Config object remains unchanged");
            delta.NativeIntact("Real Harmony Delta apply");
            delta.Owner.ReturnToDefault();
            Check(delta.Owner.BaseResetCalls==1&&PatchAppliedChange.PhysicalResetCalls==0,"Real Harmony: base removal runs while incompatible Delta reset callback is skipped");
            Check(delta.Roots().Length==0&&!delta.Body.activeSelf,"Real Harmony: base removal cleans generated presentation before body destruction");delta.Restored("Real Harmony removal");
            var other=new Fixture("H-002");other.Owner.Config=new LocoS282730AConfig{ModificationId="OtherAxles"};
            other.Owner.ApplyS282730A(other.Owner.Config);
            Check(PatchAppliedChange.PhysicalApplyCalls==1&&other.Roots().Length==0,"Real Harmony: unrelated modification still executes its native callback");
            Check(CarChanger.Game.ComponentProcessor.HideCalls==1,"Real Harmony: unrelated modification keeps native body hiding");
            other.Owner.ReturnToDefault();
            Check(PatchAppliedChange.PhysicalResetCalls==1,"Real Harmony: unrelated modification keeps its native reset callback");
            var report=new JObject{["passed"]=true,["checks"]=new JArray(Checks),["harmonyVersion"]=typeof(Harmony).Assembly.GetName().Version.ToString(),
                ["limitations"]="Actual installed Harmony runtime patches production Delta adapter and visual source. Optional-mod methods and Unity/native hierarchy are stand-ins; no game/Mono execution or rendering tested."};
            Directory.CreateDirectory(args[0]);File.WriteAllText(System.IO.Path.Combine(args[0],"validation.json"),report.ToString());
            Console.WriteLine(Checks.Count+" actual Harmony dispatch checks passed.");return 0;
        }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
        finally{
            try{harmony.UnpatchAll("Rearranged.Delta.OfflineDispatch");}
            catch{ /* Preserve the original unsupported-host initialization failure. */ }
        }
    }
}
#endif
