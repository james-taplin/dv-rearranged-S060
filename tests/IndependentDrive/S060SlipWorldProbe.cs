// Force-free real-Unity/real-Entities experiment. It loads installed game code;
// the controlled environment below is not a running locomotive or game save.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DV;
using DV.ECS.Train;
using DV.Utils;
using Newtonsoft.Json.Linq;
using S060SlipFixture;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Object = UnityEngine.Object;

public static class S060SlipWorldProbe
{
    static readonly List<string> checks = new List<string>();
    static readonly List<Object> environment = new List<Object>();
    const BindingFlags Flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); checks.Add(name); }
    static Type GameType(string name) => typeof(TrainCar).Assembly.GetTypes().Single(t => t.Name == name);
    static Component Singleton(string name)
    {
        var type = GameType(name);
        var root = new GameObject("[Slip fixture] " + name); root.SetActive(false); environment.Add(root);
        var component = root.AddComponent(type);
        typeof(SingletonBehaviour<>).MakeGenericType(type).GetField("_instance", Flags).SetValue(null, component);
        return component;
    }
    static EntityQuery Query(Type type, string name, ComponentSystemBase system) =>
        (EntityQuery)type.GetMethod("<>GetEntityQuery_For" + name + "_From", Flags).Invoke(null, new object[] { system });

    static int HelperCount(EntityManager manager) =>
        manager.CreateEntityQuery(ComponentType.ReadOnly<WheelslipControllerSystem.WheelslipInputData>()).CalculateEntityCount() +
        manager.CreateEntityQuery(ComponentType.ReadOnly<AdhesionControllerSystem.AdhesionCalculationData>()).CalculateEntityCount();

    public static void Run(string output)
    {
        CandidateRuntime.Initialize();
        World world = null;
        try
        {
            if(World.DefaultGameObjectInjectionWorld!=null)
                throw new InvalidOperationException("This synthetic environment probe must run only in its isolated empty Unity fixture.");
            Unity.Burst.BurstCompiler.Options.EnableBurstCompilation = false;
            var app=Singleton("AppUtil");
            var pause=Singleton("PausePhysicsHandler");
            var spawner=Singleton("CarSpawner");
            // No pool exists in this empty fixture. A freshly constructed native
            // CarSpawner otherwise reports setup in progress and gates both jobs.
            spawner.GetType().GetField("poolInitialized",Flags).SetValue(spawner,true);
            var globals = ScriptableObject.CreateInstance<Globals>(); environment.Add(globals);
            var gameParams = ScriptableObject.CreateInstance<GameParams>(); environment.Add(gameParams);
            gameParams.AdhesionInfluencedByWeather = false;
            gameParams.WheelslipAllowed = true; gameParams.WheelSlideAllowed = true;
            typeof(Globals).GetField("gameParamsInstance", Flags).SetValue(globals, gameParams);
            typeof(Globals).GetField("_current", Flags).SetValue(null, globals);
            Check(TimeUtil.IsFlowing, "Fixture runs with flowing time in the real Unity engine");
            Check(!(bool)app.GetType().GetProperty("IsTimePausedSafer",Flags).GetValue(app),"Synthetic environment is not paused");
            Check(!(bool)spawner.GetType().GetProperty("PoolSetupInProgress",Flags).GetValue(spawner),"Synthetic environment has completed pool setup");

            world = new World("Independent-slip force-free native fixture");
            var manager = world.EntityManager;
            var begin = world.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
            var end = world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            var adhesion = world.GetOrCreateSystem<AdhesionControllerSystem>();
            var slip = world.GetOrCreateSystem<WheelslipControllerSystem>();
            var carrier = manager.CreateEntity(NativeSlipEntitySchema.AdhesionCarrier(manager));
            var a = manager.CreateEntity(NativeSlipEntitySchema.SlipGroup(manager));
            var b = manager.CreateEntity(NativeSlipEntitySchema.SlipGroup(manager));
            var shared = new AdhesionControllerSystem.StaticSharedAdhesionCalculationData {bogieCount=2,numOfAxles=4};
            manager.SetComponentData(carrier,shared);
            manager.SetComponentData(carrier,new LocalToWorld {Value=float4x4.identity});
            manager.SetComponentData(carrier,new AdhesionControllerSystem.StaticAdhesionCalculationData {wheelslipFrictionCoef=.24f,wheelSlideFrictionCoef=.24f});
            var calc = new AdhesionControllerSystem.AdhesionCalculationData {weightPerAxle=1000f,sandCoef=1f,isGrounded=true};
            manager.SetComponentData(carrier,calc);

            Check(Query(typeof(AdhesionControllerSystem),"Copy_TrainCar_Data",adhesion).CalculateEntityCount()==0,"Carrier excluded from native managed car input-copy query");
            Check(Query(typeof(WheelslipControllerSystem),"Copy_TrainCar_Data",slip).CalculateEntityCount()==0,"Groups excluded from native managed controller input-copy query");
            Check(Query(typeof(AdhesionControllerSystem),"Calculate_Adhesion",adhesion).CalculateEntityCount()==1,"Exactly one carrier matches native adhesion calculation");
            Check(Query(typeof(WheelslipControllerSystem),"Calculate_Wheelslip",slip).CalculateEntityCount()==2,"Exactly two groups match native wheelslip calculation");
            double elapsed=0;
            bool useDrop=false, preventSlip=false;
            float firstGripMultiplier=1;
            var dropCurve=AnimationCurve.Linear(0,1,.25f,.4f);
            Action<float,float,float> step = (forceA,forceB,slide) => {
                elapsed += .02; world.SetTime(new TimeData(elapsed,.02f));
                begin.Update(); adhesion.Update(); manager.CompleteAllJobs();
                var grip=manager.GetComponentData<AdhesionControllerSystem.AdhesionWheelslipOutputData>(carrier);
                foreach(var pair in new[]{(entity:a,force:forceA),(entity:b,force:forceB)}) {
                    var groupGrip=grip;
                    if(pair.entity==a) groupGrip.wheelslipForceLimitPerAxle*=firstGripMultiplier;
                    float drop=useDrop?dropCurve.Evaluate(manager.GetComponentData<WheelslipControllerSystem.WheelslipOutputData>(pair.entity).wheelslip):1;
                    NativeSlipEntitySchema.WriteGroupInputs(manager,pair.entity,
                        new WheelslipControllerSystem.WheelslipInputData {generatedForce=pair.force,numberOfPoweredAxles=2,maxWheelslipRpm=370,wheelslipToAdhesionDrop=drop,preventWheelslip=preventSlip},
                        shared,groupGrip,new AdhesionControllerSystem.WheelSlideData{wheelSlide=slide});
                }
                slip.Update(); manager.CompleteAllJobs(); end.Update();
            };
            step(1000,100,0);
            float initialGrip=manager.GetComponentData<AdhesionControllerSystem.AdhesionWheelslipOutputData>(carrier).wheelslipForceLimitPerAxle;
            Check(Math.Abs(initialGrip-240)<.001,"Native carrier calculates fresh grip at zero ground speed (actual "+initialGrip+")");
            for(int i=0;i<200;i++) step(1000,100,0);
            var outA=manager.GetComponentData<WheelslipControllerSystem.WheelslipOutputData>(a);
            var outB=manager.GetComponentData<WheelslipControllerSystem.WheelslipOutputData>(b);
            Check(outA.wheelslip>.99f && outB.wheelslip==0,"Real native systems slip one group while the other grips");
            Check(Math.Abs(outA.totalForceLimit+outB.totalForceLimit-960)<.001,"Native separate group limits conserve the assigned total grip");
            Check(outA.orientedMaxWheelslipRpm==370,"Native output uses supplied configured RPM maximum");
            for(int i=0;i<200;i++) step(1000,1000,0);
            Check(manager.GetComponentData<WheelslipControllerSystem.WheelslipOutputData>(b).wheelslip>.99f,"Both native group states can slip");
            for(int i=0;i<200;i++) step(100,100,0);
            Check(manager.GetComponentData<WheelslipControllerSystem.WheelslipOutputData>(a).wheelslip==0 && manager.GetComponentData<WheelslipControllerSystem.WheelslipOutputData>(b).wheelslip==0,"Both native states recover independently");
            calc.sandCoef=2; manager.SetComponentData(carrier,calc); step(100,100,0);
            Check(Math.Abs(manager.GetComponentData<WheelslipControllerSystem.WheelslipOutputData>(a).totalForceLimit-960)<.001,"Native sand coefficient modifies grip without another resource consumer");
            for(int i=0;i<200;i++) step(1000,1000,.5f);
            Check(manager.GetComponentData<WheelslipControllerSystem.WheelslipOutputData>(a).wheelslip==0,"Copied host wheel slide suppresses powered slip");
            calc.sandCoef=1; manager.SetComponentData(carrier,calc);
            firstGripMultiplier=.5f;
            for(int i=0;i<200;i++) step(400,400,0);
            Check(manager.GetComponentData<WheelslipControllerSystem.WheelslipOutputData>(a).wheelslip>.65f && manager.GetComponentData<WheelslipControllerSystem.WheelslipOutputData>(b).wheelslip==0,
                "Diagnostic-only different grip inputs yield separate native slip under equal torque demand");
            firstGripMultiplier=1; useDrop=true;
            for(int i=0;i<200;i++) step(-1000,100,0);
            outA=manager.GetComponentData<WheelslipControllerSystem.WheelslipOutputData>(a);
            Check(outA.wheelslip>.99f && outA.orientedMaxWheelslipRpm==-370,"Native group slip supports reverse demand independently");
            Check(Math.Abs(outA.totalForceLimit-192)<.01,"Per-group native adhesion-drop input reduces its force limit");
            for(int i=0;i<200;i++) step(20,100,0);
            Check(manager.GetComponentData<WheelslipControllerSystem.WheelslipOutputData>(a).wheelslip==0,"Native adhesion-drop case recovers after demand falls");
            gameParams.WheelslipAllowed=false;
            for(int i=0;i<200;i++) step(1000,1000,0);
            Check(manager.GetComponentData<WheelslipControllerSystem.WheelslipOutputData>(a).wheelslip==0 && manager.GetComponentData<WheelslipControllerSystem.WheelslipOutputData>(b).wheelslip==0,
                "Native global no-wheelslip setting respected by calculation helpers");
            gameParams.WheelslipAllowed=true; preventSlip=true;
            for(int i=0;i<200;i++) step(1000,1000,0);
            Check(manager.GetComponentData<WheelslipControllerSystem.WheelslipOutputData>(a).wheelslip==0,"Native prevent-wheelslip input respected");
            bool rejected=false;
            var retired=manager.CreateEntity(NativeSlipEntitySchema.SlipGroup(manager)); manager.DestroyEntity(retired);
            try { NativeSlipEntitySchema.WriteGroupInputs(manager,retired,new WheelslipControllerSystem.WheelslipInputData(),shared,new AdhesionControllerSystem.AdhesionWheelslipOutputData(),new AdhesionControllerSystem.WheelSlideData()); }
            catch(InvalidOperationException) { rejected=true; }
            Check(rejected,"Retired versioned entity cannot receive group inputs");
            begin.Update(); manager.CompleteAllJobs(); end.Update();
            manager.DestroyEntity(a); manager.DestroyEntity(b); manager.DestroyEntity(carrier);
            begin.Update(); end.Update();
            Check(!manager.Exists(a) && !manager.Exists(b) && !manager.Exists(carrier),"All three fixture entity identities retired after native buffers drain");
            Check(HelperCount(manager)==0,"No owned helper entities remain");
            // Use the actual conversion wrappers and production ordered systems.
            // Dynamic game assemblies did not receive the game's bootstrap event,
            // so deliver that same isolated-World notification explicitly.
            typeof(DVConvertToEntity).GetMethod("OnWorldInitialized",Flags).Invoke(null,new object[]{world});
            var conversion=world.GetOrCreateSystem<DVConvertToEntitySystem>();
            var simulation=world.GetOrCreateSystem<SimulationSystemGroup>();
            simulation.AddSystemToUpdateList(conversion); simulation.AddSystemToUpdateList(begin);
            simulation.AddSystemToUpdateList(adhesion); simulation.AddSystemToUpdateList(slip); simulation.AddSystemToUpdateList(end);
            var parent=new GameObject("[Slip wrapper fixture]"); environment.Add(parent);
            for(int activation=1;activation<=3;activation++)
            {
                var runtime=new NativeSlipRuntimeState(parent.transform,manager,2,activation);
                int samples=0;
                float runtimeDemand=1000;
                float runtimeGripMultiplier=activation==2?.5f:1f;
                runtime.PrepareInputs+=()=>{
                    runtime.SetCarrier(shared,new AdhesionControllerSystem.StaticAdhesionCalculationData{wheelslipFrictionCoef=.24f,wheelSlideFrictionCoef=.24f},calc,new LocalToWorld{Value=float4x4.identity});
                    for(int group=0;group<2;group++) runtime.SetGroup(group,
                        new WheelslipControllerSystem.WheelslipInputData{generatedForce=group==0?runtimeDemand:100,numberOfPoweredAxles=2,maxWheelslipRpm=370,wheelslipToAdhesionDrop=1},shared,new AdhesionControllerSystem.WheelSlideData(),group==0?runtimeGripMultiplier:1);
                };
                runtime.OutputsReady+=()=>samples++;
                elapsed+=.02; world.SetTime(new TimeData(elapsed,.02f)); simulation.Update();
                Check(runtime.Ready && runtime.HasCompletedSample && samples==1,"Wrapper conversion and ordered native first sample, activation "+activation);
                Check(runtime.BasePerAxleGrip==240 && runtime.GetOutput(0).wheelslip>0 && runtime.GetOutput(1).wheelslip==0,"Automatic native ordering preserves separate slip, activation "+activation);
                Check(Math.Abs(runtime.GetOutput(0).totalForceLimit-480*runtimeGripMultiplier)<.001f && runtime.GetOutput(1).totalForceLimit==480,"Diagnostic-only runtime grip allocation stays per-group, activation "+activation);
                // Finish on the first recovery-to-zero sample, which can leave a
                // native tag-removal command queued for the following Begin ECB.
                runtimeDemand=0;
                for(int tick=0;tick<200 && runtime.GetOutput(0).wheelslip>0;tick++) { elapsed+=.02; world.SetTime(new TimeData(elapsed,.02f)); simulation.Update(); }
                Check(runtime.GetOutput(0).wheelslip==0,"Ordered runtime recovers before retirement, activation "+activation);
                var helperRoot=runtime.Root;
                int finalSamples=samples;
                runtime.Dispose(); runtime.Dispose();
                Check(!runtime.HasCompletedSample,"Disposal synchronously invalidates force-readiness, activation "+activation);
                // Run synchronously inside this fixture invocation: simulate the
                // object's end-of-frame destruction now, exercising native
                // OnDisable/OnDestroy queueing before the next simulation ECB.
                Object.DestroyImmediate(helperRoot);
                elapsed+=.02; world.SetTime(new TimeData(elapsed,.02f)); simulation.Update();
                Check(samples==finalSamples && HelperCount(manager)==0,"Native wrapper retirement drains with no stale samples/helpers, activation "+activation);
            }
            Check(NativeSlipRuntimeState.StateCount==0,"No disposed runtime state registrations remain");
            var invalid=new NativeSlipRuntimeState(parent.transform,manager,2,4);
            var invalidRoot=invalid.Root;
            int faults=0;
            invalid.PrepareInputs+=()=>{throw new InvalidOperationException("Intentional fixture input fault");};
            invalid.Failed+=error=>faults++;
            elapsed+=.02; world.SetTime(new TimeData(elapsed,.02f)); simulation.Update();
            Check(faults==1 && !invalid.Ready && !invalid.HasCompletedSample && NativeSlipRuntimeState.StateCount==0,"Input fault retires activation before any force-ready sample");
            Object.DestroyImmediate(invalidRoot);
            elapsed+=.02; world.SetTime(new TimeData(elapsed,.02f)); simulation.Update();
            Check(HelperCount(manager)==0,"Faulted native wrappers retire without owned entity leaks");
            var timeType=typeof(World).Assembly.GetType("Unity.Entities.WorldTime",true);
            int frameworkTimeEntities=manager.CreateEntityQuery(ComponentType.ReadOnly(timeType)).CalculateEntityCount();
            Check(frameworkTimeEntities==1 && manager.UniversalQuery.CalculateEntityCount()==1,"Only the native WorldTime singleton remains before World disposal");
            S060OwnerProbe.Run(world, simulation, checks);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output,new JObject { ["passed"]=true,["unityVersion"]=Application.unityVersion,["checks"]=checks.Count,["labels"]=new JArray(checks),["runtimeSha256"]=CandidateRuntime.Hash,
                ["playerContext"]=!Application.isEditor,
                ["limits"]="Actual installed native systems in a separate Unity World, synthetic inactive singleton environment, Burst disabled. Manual calculation tests plus production ordered SimulationSystemGroup and native conversion-wrapper creation/retirement tested. Fixture forces end-of-frame helper GameObject destruction synchronously before next group update. No vehicle forces, actual weather, resource consumption, full game player-loop registration, animation, finite boiler or game save exercised."}.ToString());
            Debug.Log("NATIVE_SLIP_WORLD_PASS "+checks.Count);
        }
        catch(Exception error)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output,new JObject{["passed"]=false,["checks"]=checks.Count,["labels"]=new JArray(checks),["runtimeSha256"]=CandidateRuntime.Hash,["error"]=error.ToString()}.ToString());
            throw;
        }
        finally
        {
            if(world!=null && world.IsCreated) world.Dispose();
            foreach(var item in environment) if(item!=null) Object.DestroyImmediate(item);
        }
    }
}
