using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityEngine;
using DV.ECS.Train;
using LocoSim.Implementations.Wheels;
using RearrangedS282;

internal static class Program
{
    private static int checks,transitions,explosions;
    private static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
    private static void Near(float a,float b,string label)=>Check(float.IsFinite(a)&&Math.Abs(a-b)<.002f,label);
    private static void Reject(Action action,string label){bool refused=false;try{action();}catch(ArgumentException){refused=true;}Check(refused,label);}
    private static MeshRenderer Renderer(Transform parent,Material mat,bool active=true)
    {var go=new GameObject();go.transform.parent=parent;go.SetActive(active);var r=go.Add<MeshRenderer>();r.sharedMaterial=mat;return r;}
    private static void Physics()
    {
        var car=TrainCar.Create();var control=TrainCar.Create();var sim=car.SimController;var force=sim.drivingForce;
        var original=sim.poweredWheels.poweredWheels;var braking=sim.wheelslipController.engineBrakingActivePort;
        var brakes=car.brakeSystem;var adapter=new B99PhysicsState(car);
        var torque=sim.simFlow.ports["torque"];var originalRadius=force.wheelRadius;
        foreach(int count in new[]{0,1,0,2,0,3,0,4,0,5,0,6,0,4})
        {
            int total=count==0?4:count+2;
            var wheels=Enumerable.Range(0,count).Select(_=>new PoweredWheel()).ToArray();
            adapter.Apply(.712f,wheels,total,count/(float)total);transitions++;
            Check(sim.poweredWheels.poweredWheels.Length==count&&sim.wheelslipController.NumberOfPoweredAxles==count,"native powered registration");
            Check(car.NumberOfAxles==total&&car.entity.GetComponentData<AdhesionControllerSystem.StaticSharedAdhesionCalculationData>().numOfAxles==total,"native axle mass allocation");
            Near(car.entity.GetComponentData<AdhesionControllerSystem.StaticAdhesionCalculationData>().wheelSlideFrictionCoef,.18f,"slide friction retained");
            Near(car.entity.GetComponentData<AdhesionControllerSystem.AdhesionCalculationData>().weightPerAxle,137000f/total,"native mass per axle refreshed");
            Near(sim.tractionPortsFeeder.speedMsToWheelRpmConst*sim.tractionPortsFeeder.wheelRpmToSpeedKmhConst,3.6f,"RPM conversion remains finite");
            Check(ReferenceEquals(car.brakeSystem,brakes),"native brake system retained");
            braking.Value=1;torque.Value=1e9f;
            foreach(var b in car.Bogies)b.rb.force=0;
            if(force.enabled)force.FixedUpdate(); // Unity dispatches enabled behaviours.
            if(count==0)
            {
                Check(!force.enabled&&force.generatedForce==0,"zero driver force remains zero under arbitrary torque");
                Check(car.Bogies.All(b=>b.rb.force==0),"zero driver receives no native tractive force");
                Near(sim.wheelslipController.EngineBrakingForcePerAxle,0,"zero driver avoids native engine-braking 0/0");
                Check(!car.entity.GetComponentData<WheelslipControllerSystem.WheelslipInputData>().isEngineBraking,"stale ECS engine-braking state cleared");
                car.derailed=true;if(force.enabled)force.FixedUpdate();Near(car.rb.force,0,"zero driver cannot propel derailed body");car.derailed=false;
            }
            else
            {
                Check(force.enabled&&ReferenceEquals(sim.wheelslipController.engineBrakingActivePort,braking),"positive driver restores native force and engine-braking reader");
                sim.wheelslipController.TotalForceLimit=30000;force.FixedUpdate();
                Near(car.Bogies.Sum(b=>b.rb.force),30000,"native traction resumes and retains adhesion limit");
            }
            Check(control.NumberOfAxles==6&&control.SimController.poweredWheels.poweredWheels.Length==4&&control.SimController.drivingForce.enabled,"other car untouched");
            adapter.Restore();transitions++;
            Check(force.enabled&&car.NumberOfAxles==6,"stock traction and axle count restored");
            Check(sim.poweredWheels.poweredWheels.SequenceEqual(original)&&ReferenceEquals(sim.wheelslipController.engineBrakingActivePort,braking),"exact stock axle references and engine-braking reader restored");
            Check(sim.poweredWheels.poweredWheels.Select(w=>(int)w.index).SequenceEqual(new[]{0,1,2,3}),"stock axle indexes restored");
            Near(force.wheelRadius,originalRadius,"stock force radius restored");
        }
        Reject(()=>adapter.Apply(.712f,Array.Empty<PoweredWheel>(),0,0),"zero total axles rejected");
        Reject(()=>adapter.Apply(.712f,Array.Empty<PoweredWheel>(),4,.5f),"impossible unpowered weight rejected");
        Reject(()=>adapter.Apply(float.NaN,Array.Empty<PoweredWheel>(),4,0),"NaN radius rejected");
        Reject(()=>adapter.Apply(.712f,new[]{original[0],original[0]},4,.5f),"duplicate axles rejected");
        Check(force.enabled&&car.NumberOfAxles==6,"validation rejects before mutation");
        var disabled=TrainCar.Create();disabled.SimController.drivingForce.enabled=false;var snapshot=new B99PhysicsState(disabled);
        snapshot.Apply(.712f,Array.Empty<PoweredWheel>(),4,0);snapshot.Restore();
        Check(!disabled.SimController.drivingForce.enabled,"pre-existing disabled native force remains disabled");
        var geometry=new GeometryFixture();geometry.Apply();
        Check(geometry.Parts.Length==30&&geometry.Parts.All(t=>!t.gameObject.activeSelf),"actual no-driver geometry hides both mechanisms, 24 brake parts and four powered axles");
    }
    private static void Explosions(bool alreadyCached)
    {
        var car=TrainCar.Create();var body=new GameObject();body.transform.parent=car.transform;
        var normal=new Material();var burnt=new Material();var painted=new Material();
        var existing=Renderer(body.transform,normal);var hiddenDriver=Renderer(body.transform,normal,false);
        var handler=car.gameObject.Add<ExplosionModelHandler>();
        var stock=new ExplosionModelHandler.MaterialSwapData{swapMaterial=burnt,affectedGameObjects=new[]{body}};
        var foreign=new ExplosionModelHandler.MaterialSwapData{swapMaterial=new Material(),affectedGameObjects=Array.Empty<GameObject>()};
        var originalArray=new[]{foreign,stock};handler.materialSwaps=originalArray;
        handler.gameObjectsToDisable=Array.Empty<GameObject>();handler.gameObjectSwaps=Array.Empty<ExplosionModelHandler.GameObjectSwapData>();handler.Awake();
        if(alreadyCached){handler.HandleExplosionModelChange();handler.RevertToUnexplodedModel();explosions++;}
        var cache=stock.affectedRenderers;var registration=new B99ExplosionMaterials(car,body.transform);
        Check(originalArray.Length==2&&handler.materialSwaps.Length==3,"per-car array copied; native configuration entries retained");
        Check(ReferenceEquals(stock.affectedRenderers,cache)&&stock.affectedGameObjects.Length==1,"registration leaves native cache and root data untouched");
        var axle=Renderer(car.transform,normal);var lod=Renderer(axle.transform,painted,false);
        var rod=Renderer(body.transform,normal); // native body discovery can overlap this added part
        registration.Refresh(new Renderer[]{existing,axle,lod,rod,axle});
        Check(registration.Count==5,"original body gear plus added renderers registered once, including inactive LOD");
        var other=TrainCar.Create();var otherRenderer=Renderer(other.transform,painted);
        for(int cycle=0;cycle<4;cycle++)
        {
            handler.HandleExplosionModelChange();explosions++;
            Check(new[]{existing,axle,lod,rod}.All(r=>ReferenceEquals(r.sharedMaterial,burnt)),"native explosion burns original, added axles, rods and inactive LODs");
            Check(!lod.gameObject.activeSelf,"explosion does not activate hidden LODs");
            Check(ReferenceEquals(hiddenDriver.sharedMaterial,burnt),"driver hidden by initial 4-0-4 is included in explosion material lifecycle");
            Check(ReferenceEquals(otherRenderer.sharedMaterial,painted),"another locomotive's material remains unchanged");
            bool refused=false;try{registration.Refresh(new[]{axle});}catch(InvalidOperationException){refused=true;}Check(refused,"burnt originals cannot be captured during an explosion");
            handler.RevertToUnexplodedModel();
            Check(ReferenceEquals(existing.sharedMaterial,normal)&&ReferenceEquals(axle.sharedMaterial,normal)&&ReferenceEquals(lod.sharedMaterial,painted)&&ReferenceEquals(rod.sharedMaterial,normal),"native repair restores exact original material references");
            if(cycle==0)
            {
                var rear=Renderer(car.transform,painted,false);registration.Refresh(new[]{rear});
                handler.HandleExplosionModelChange();explosions++;
                Check(ReferenceEquals(rear.sharedMaterial,burnt),"rear engine created after first explosion receives native burnt material");
                handler.RevertToUnexplodedModel();Check(ReferenceEquals(rear.sharedMaterial,painted),"late rear engine repairs to its own material");
                Check(registration.Count==6,"late registration extends one per-car native swap");
            }
            hiddenDriver.gameObject.SetActive(true);
            Check(ReferenceEquals(hiddenDriver.sharedMaterial,normal),"hidden driver repairs correctly before becoming visible");
            registration.Refresh(new[]{axle,lod,rod});Check(registration.Count==6,"repeated arrangement refresh does not duplicate native registrations");
        }
    }
    private static void Main(string[] args)
    {
        Physics();Explosions(false);Explosions(true);
        var report=new{passed=true,checks,transitions,explosions,validation="Actual B99PhysicsState, B99ExplosionMaterials and ShowNoDrivers; installed native ExplosionModelHandler and DrivingForce algorithms plus wheelslip reset/properties. Visibility changes only. Managed hierarchy/entity/physics-dispatch stand-ins; no Unity scene, real rigidbody forces, renderer output, audio or in-game acceptance."};
        File.WriteAllText(args[0],JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"PASS {checks} assertions, {transitions} transitions, {explosions} explosions");
    }
}
