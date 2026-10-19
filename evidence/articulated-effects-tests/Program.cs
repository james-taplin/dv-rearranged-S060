using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LocoSim.Implementations;
using RearrangedS282;
using UnityEngine;

internal static class Program
{
    private static int checks,scenarios;
    private static void Check(bool condition,string label) {checks++;if(!condition)throw new InvalidOperationException(label);}
    private static void Main(string[] args)
    {
        var car=new TrainCar{id="S282 A",interior=new Component()};
        var bodyClip=new ClipController();var interiorClip=new ClipController{active=false};var layer=new LayerController();
        car.children.Add(bodyClip);car.interior.children.Add(interiorClip);car.interior.children.Add(layer);
        var clips=ArticulatedEffects.Discover<ClipController>(car);
        Check(clips.Length==2&&clips.Contains(bodyClip)&&clips.Contains(interiorClip),"find initialized controllers in body and separate inactive interior root");
        Check(ArticulatedEffects.Discover<LayerController>(car).SequenceEqual(new[]{layer}),"find layered controller in separate interior root");
        Check(car.includeInactiveRequests.All(v=>v)&&car.interior.includeInactiveRequests.All(v=>v),"both native roots include inactive controllers");
        scenarios++;
        car.children.Add(car.interior);
        Check(ArticulatedEffects.Discover<ClipController>(car).Length==2,"interior already below body produces no duplicate clip controllers");
        Check(ArticulatedEffects.Discover<LayerController>(car).Length==1,"overlapping roots produce no duplicate layered controllers");
        car.children.Remove(car.interior);
        car.interior=car;
        Check(ArticulatedEffects.Discover<ClipController>(car).SequenceEqual(new[]{bodyClip}),"identical roots produce one controller reference");
        car.interior=null;
        Check(ArticulatedEffects.Discover<ClipController>(car).SequenceEqual(new[]{bodyClip}),"missing interior safely retains body controller");
        scenarios++;
        var flow=new SimulationFlow{id="flow"};
        var otherCar=new TrainCar{id=car.id};var otherFlow=new SimulationFlow{id=flow.id};
        Check(car.Equals(otherCar)&&flow.Equals(otherFlow),"fixture provides equal identity labels on distinct instances");
        var binding=new ArticulatedEffects.AudioBinding{Car=car,Flow=flow,Ready=true};
        Check(ArticulatedEffects.Matches(binding,car,flow),"ready exact car/flow binding is eligible");
        Check(!ArticulatedEffects.Matches(binding,otherCar,flow),"equal car label on different instance is rejected");
        Check(!ArticulatedEffects.Matches(binding,car,otherFlow),"equal flow label on different instance is rejected");
        binding.Ready=false;Check(!ArticulatedEffects.Matches(binding,car,flow),"deinitialized controller is rejected");
        binding.Ready=true;binding.Car=null;Check(!ArticulatedEffects.Matches(binding,car,flow),"pooled controller without owner is rejected");
        binding.Car=car;binding.Flow=null;Check(!ArticulatedEffects.Matches(binding,car,flow),"stale controller without current flow is rejected");
        scenarios++;
        var random=new Random(741135);
        for(int iteration=0;iteration<300;iteration++)
        {
            var current=new TrainCar{id="current",interior=new Component()};
            var currentFlow=new SimulationFlow{id="current flow"};
            int layout=iteration%3;
            if(layout==1)current.children.Add(current.interior);
            if(layout==2)current.interior=current;
            var expected=new List<ClipController>();
            var records=new Dictionary<ClipController,ArticulatedEffects.AudioBinding>();
            int count=random.Next(1,20);
            for(int index=0;index<count;index++)
            {
                var controller=new ClipController{active=random.Next(2)==0};
                var target=random.Next(2)==0?current:(Component)current.interior;
                target.children.Add(controller);expected.Add(controller);
                records.Add(controller,new ArticulatedEffects.AudioBinding{Car=current,Flow=currentFlow,Ready=true});
            }
            var stale=new ClipController();current.interior.children.Add(stale);
            records.Add(stale,new ArticulatedEffects.AudioBinding{Car=current,Flow=new SimulationFlow{id=currentFlow.id},Ready=true});
            for(int refresh=0;refresh<15;refresh++)
            {
                var discovered=ArticulatedEffects.Discover<ClipController>(current);
                Check(discovered.Length==count+1&&discovered.Distinct().Count()==discovered.Length,"repeated root discovery has no duplicate controller references");
                var eligible=discovered.Where(c=>ArticulatedEffects.Matches(records[c],current,currentFlow)).ToArray();
                Check(eligible.Length==count&&eligible.All(expected.Contains),"each repeated discovery excludes stale flow and retains exactly the eligible references");
            }
            scenarios++;
        }
        var report=new{passed=true,checks,scenarios,validation="Actual extracted AudioControllers and AudioBindingMatches production methods with minimal component-tree stand-ins. Covers separate/overlapping/missing roots, inactive controllers, exact car/flow identity and repeated discovery. Native Init/Deinit, clone creation, audio mixing and Unity scene execution are not exercised."};
        File.WriteAllText(args[0],JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("PASS: "+checks+" assertions across "+scenarios+" discovery/identity scenarios.");
    }
}
