using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DV.ModularAudioCar;
using RearrangedS282;

internal static class Program
{
    private static int checks,transitions,oldFailures;
    private static void Check(bool ok,string label) {checks++;if(!ok)throw new InvalidOperationException(label);}
    private static bool IndexFailure(Action action)
    {try{action();return false;}catch(IndexOutOfRangeException){return true;}}
    private static void ShapeMatches(CarRollingAudioModule module,TrainCar car,string label)
    {
        Check(module.Cache.Length==car.Bogies.Length,label+" bogie count");
        for(int index=0;index<car.Bogies.Length;index++)
            Check(module.Cache[index].Length==car.Bogies[index].Axles.Length,label+" per-bogie axle count");
    }
    private static void Main(string[] args)
    {
        foreach(int initial in new[]{3,4,6,7,9})foreach(int target in new[]{3,4,6,7,9})
        {
            var car=new TrainCar();car.SetAxles(initial);
            var rolling=new CarRollingAudioModule();car.interior.children.Add(rolling);rolling.InitializeForFixture(car);
            int[] oldSizes=rolling.Cache.Select(row=>row.Length).ToArray();car.SetAxles(target);
            bool expectedFailure=car.Bogies.Select((b,i)=>b.Axles.Length>oldSizes[i]).Any(v=>v);
            bool failed=IndexFailure(()=>rolling.UpdateModule(.02f));
            Check(failed==expectedFailure,"native old cache failure agrees with per-bogie growth "+initial+"->"+target);
            if(failed)oldFailures++;
            new WheelRearranger(car).RefreshForFixture();
            ShapeMatches(rolling,car,"native reset "+initial+"->"+target);
            Check(rolling.Cache.SelectMany(row=>row).All(value=>value==-1),"native reset initializes joint indices to -1");
            Check(!IndexFailure(()=>rolling.UpdateModule(.02f)),"native update succeeds after reset "+initial+"->"+target);
            transitions++;
        }
        var owner=new TrainCar();owner.SetAxles(6);
        var module=new CarRollingAudioModule();owner.interior.children.Add(module);module.InitializeForFixture(owner);
        var rearranger=new WheelRearranger(owner);
        foreach(var state in new[]{("arrangement",9),("stock restore",6),("arrangement",7),("rollback",6),("new single-driver/two-driver total",3),("new Mogul total",4),("arrangement",4),("pool reset",6)})
        {owner.SetAxles(state.Item2);rearranger.RefreshForFixture();ShapeMatches(module,owner,state.Item1);Check(!IndexFailure(()=>module.UpdateModule(.02f)),state.Item1+" update succeeds");transitions++;}
        owner.children.Add(owner.interior);int reads=owner.Bogies.Sum(b=>b.axleReads);
        rearranger.RefreshForFixture();Check(owner.Bogies.Sum(b=>b.axleReads)-reads==2,"overlapping car/interior roots reset each exact module once");
        owner.children.Remove(owner.interior);
        var other=new TrainCar();other.SetAxles(9);var wrong=new CarRollingAudioModule();wrong.InitializeForFixture(other);owner.interior.children.Add(wrong);
        var wrongCache=wrong.Cache;rearranger.RefreshForFixture();Check(ReferenceEquals(wrong.Cache,wrongCache),"wrong-car pooled module is untouched");
        var uninitialized=new CarRollingAudioModule();owner.interior.children.Add(uninitialized);rearranger.RefreshForFixture();Check(uninitialized.Cache==null,"uninitialized module is untouched");
        owner.SetAxles(9);uninitialized.InitializeForFixture(owner);ShapeMatches(uninitialized,owner,"late native initialization");
        Check(!IndexFailure(()=>uninitialized.UpdateModule(.02f)),"late native initialization requires no extra reset hook");
        var bodyModule=new CarRollingAudioModule();owner.children.Add(bodyModule);bodyModule.InitializeForFixture(owner);owner.SetAxles(7);rearranger.RefreshForFixture();ShapeMatches(bodyModule,owner,"body-hierarchy fallback");
        new WheelRearranger(null).RefreshForFixture();Check(true,"missing locomotive is a no-op");
        // Exercise actual native modular dispatch with rolling before brakes,
        // matching the S282 prefab's real module order.
        var dispatchCar=new TrainCar();dispatchCar.SetAxles(6);var first=new CarRollingAudioModule();first.InitializeForFixture(dispatchCar);dispatchCar.interior.children.Add(first);dispatchCar.SetAxles(9);
        var brakes=new BrakeUpdateProbe();var host=new CarModularAudio();host.audioModules.Add(first);host.audioModules.Add(brakes);
        Check(IndexFailure(()=>host.DoUpdate(.02f)),"actual native rolling exception escapes modular dispatch");
        Check(brakes.Updates==0,"rolling exception prevents following brake module update");
        foreach(var bogie in dispatchCar.Bogies)bogie.track.jointsSpan=0;
        Check(!IndexFailure(()=>host.DoUpdate(.02f))&&brakes.Updates==1,"native no-joints track gate bypasses stale cache and reaches brakes");
        foreach(var bogie in dispatchCar.Bogies)bogie.track.jointsSpan=20;
        Check(IndexFailure(()=>host.DoUpdate(.02f))&&brakes.Updates==1,"jointed track restores old-cache failure and brake-update starvation");
        new WheelRearranger(dispatchCar).RefreshForFixture();
        Check(!IndexFailure(()=>host.DoUpdate(.02f))&&brakes.Updates==2,"native reset repairs modular dispatch through brake update");
        var report=new{passed=true,checks,transitions,oldFailures,validation="Actual production WheelRearranger.Audio.cs plus reset/update/dispatch methods decompiled from installed B99 Assembly-CSharp.dll. Native reset visibility changed only for fixture access, matching the mod's existing publicizer. Lightweight hierarchy, axle, track and audio stand-ins; no Unity audio output, Harmony/game lifecycle or audible acceptance tested."};
        File.WriteAllText(args[0],JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("PASS: "+checks+" assertions; "+transitions+" axle transitions; "+oldFailures+" original cache failures reproduced.");
    }
}
