#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ShayEffectsPlayMode
{
    private const string Pending="S060.ShayEffects.Pending";
    [InitializeOnLoadMethod] private static void Subscribe(){EditorApplication.playModeStateChanged-=Changed;EditorApplication.playModeStateChanged+=Changed;}
    public static void RunBatch(){Application.runInBackground=true;EditorApplication.isPaused=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Subscribe();SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}
    private static void Changed(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("Shay effects lifecycle audit").AddComponent<ShayEffectsPlayModeRunner>();}}
}

// Actual Unity activation and unchanged B99 Doppler.Awake. The absent game's
// ECS queue and unbound fixture particle-controller teardown are skipped.
public sealed class ShayEffectsPlayModeRunner:MonoBehaviour
{
    private const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
    private readonly List<GameObject> owned=new List<GameObject>();
    private readonly List<string> errors=new List<string>();
    private object harmony,world,previousWorld;
    private Type worldType,converterType,dopplerType;
    private bool expectingOldFailure;
    private int checks,oldErrors;
    private string managed,hash;
    private IEnumerator Start()
    {
        Application.logMessageReceived+=Logged;
        var run=Run();
        while(true){object next;try{if(!run.MoveNext())break;next=run.Current;}catch(Exception ex){Finish(false,ex.ToString());yield break;}yield return next;}
        Finish(true,null);
    }
    private IEnumerator Run()
    {
        managed=Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED");AppDomain.CurrentDomain.AssemblyResolve+=Resolve;
        var runtime=Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME");
        using(var sha=SHA256.Create())using(var stream=File.OpenRead(runtime))hash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");
        var mod=Assembly.LoadFrom(runtime);var game=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));
        var nativeAudio=Assembly.LoadFrom(Path.Combine(managed,"DV.NAudio.dll"));dopplerType=nativeAudio.GetType("DV.DopplerEffects.Doppler",true);
        converterType=Assembly.LoadFrom(Path.Combine(managed,"DV.Utils.ECS.dll")).GetType("DV.Utils.DVConvertToEntity",true);
        var ha=Assembly.LoadFrom(Path.Combine(managed,"UnityModManager/0Harmony.dll"));harmony=Activator.CreateInstance(ha.GetType("HarmonyLib.Harmony",true),"s060.shay.native-awake.audit");
        Patch(ha,converterType.GetMethod("Initialize",All),nameof(SkipQueue));Patch(ha,converterType.GetMethod("OnDestroy",All),nameof(SkipQueue));
        var controllerType=game.GetType("DV.Simulation.Controllers.ParticlesPortReadersController",true);Patch(ha,controllerType.GetMethod("OnDestroy",All),nameof(SkipQueue));
        worldType=Assembly.LoadFrom(Path.Combine(managed,"Unity.Entities.dll")).GetType("Unity.Entities.World",true);
        Assembly.LoadFrom(Path.Combine(managed,"Unity.Entities.Hybrid.dll"));
        previousWorld=worldType.GetProperty("DefaultGameObjectInjectionWorld").GetValue(null,null);
        var constructor=worldType.GetConstructors().First(c=>c.GetParameters().Length>=1&&c.GetParameters()[0].ParameterType==typeof(string));
        world=constructor.Invoke(constructor.GetParameters().Select((p,i)=>i==0?(object)"Isolated Shay Doppler archetypes":p.IsOptional?p.DefaultValue:Activator.CreateInstance(p.ParameterType)).ToArray());
        worldType.GetProperty("DefaultGameObjectInjectionWorld").SetValue(null,world,null);dopplerType.GetMethod("StaticInit",All).Invoke(null,null);

        var source=New("Processed CCL effect template",false);var controller=source.AddComponent(controllerType);
        controllerType.GetField("selfInitialization").SetValue(controller,false);
        var pprType=controllerType.GetNestedType("ParticlePortReader");var updateType=pprType.GetNestedType("PortParticleUpdateDefinition");
        var readers=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(pprType));controllerType.GetField("particlePortReaders").SetValue(controller,readers);
        for(int i=0;i<2;i++)
        {
            var drip=New("CylCockWaterDripParticles(Clone)");drip.transform.SetParent(source.transform,false);drip.AddComponent<ParticleSystem>();
            var item=Activator.CreateInstance(pprType);pprType.GetField("particlesParent").SetValue(item,drip);
            var gates=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(updateType));
            foreach(var id in new[]{"steamEngine.WATER_IN_CYLINDERS_NORMALIZED","cylinderCock.EXT_IN"}){var gate=Activator.CreateInstance(updateType);updateType.GetField("portId").SetValue(gate,id);gates.Add(gate);}
            pprType.GetField("particleUpdaters").SetValue(item,gates);readers.Add(item);
        }
        var jetType=game.GetType("DV.Simulation.Controllers.CylinderCockParticlePortReader",true);var jets=source.AddComponent(jetType);var setupType=jetType.GetNestedType("CylinderSetup");var setups=Array.CreateInstance(setupType,2);
        for(int i=0;i<2;i++){var setup=Activator.CreateInstance(setupType);foreach(var end in new[]{"front","rear"}){var leaf=New(end+i);leaf.transform.SetParent(source.transform,false);leaf.AddComponent<ParticleSystem>();setupType.GetField(end+"ParticlesParent").SetValue(setup,leaf);setupType.GetField(end+"ActivityCurve").SetValue(setup,AnimationCurve.Linear(0,1,1,1));}setups.SetValue(setup,i);}jetType.GetField("cylinderSetups").SetValue(jets,setups);
        var audioType=game.GetType("DV.ModularAudioCar.CylinderCockLayeredPortReader",true);var audio=source.AddComponent(audioType);
        var layerType=Assembly.LoadFrom(Path.Combine(managed,"DV.LayeredAudio.dll")).GetType("LayeredAudio",true);var emitters=Array.CreateInstance(layerType,2);
        for(int i=0;i<2;i++){var emitter=New("Native cock emitter "+i,false);emitter.transform.SetParent(source.transform,false);emitter.AddComponent<AudioSource>();var layer=emitter.AddComponent(layerType);layerType.GetField("layers",All).SetValue(layer,Array.CreateInstance(layerType.GetNestedType("Layer"),0));emitter.AddComponent(dopplerType);emitter.SetActive(true);emitters.SetValue(layer,i);}audioType.GetField("cylCockAudio").SetValue(audio,emitters);
        Check(source.GetComponentsInChildren(converterType,true).Length==0,"Inactive template contains no Awake-generated converter");
        var old=Instantiate(source);owned.Add(old);
        // Seed Editor-unsupported nested native configuration; no sound mixer
        // or port playback is tested here. Doppler components remain untouched.
        foreach(var layer in old.GetComponentsInChildren(layerType,true))layerType.GetField("layers",All).SetValue(layer,Array.CreateInstance(layerType.GetNestedType("Layer"),0));
        controllerType.GetField("particlePortReaders").SetValue(old.GetComponent(controllerType),Activator.CreateInstance(typeof(List<>).MakeGenericType(pprType)));
        old.SetActive(true);yield return null;
        var oldAudio=old.GetComponent(audioType);var running=(Component)((Array)audioType.GetField("cylCockAudio").GetValue(oldAudio)).GetValue(0);
        Check(running.GetComponentsInChildren(converterType,true).Length==1,"First native Doppler activation adds its converter");
        expectingOldFailure=true;var bad=Instantiate(running,old.transform);yield return null;expectingOldFailure=false;
        Check(oldErrors>=2,"Old active-emitter re-clone reproduces duplicate converter and native Awake exception");
        old.SetActive(false);DestroyImmediate(old);errors.Clear();

        var parent=New("Shay car/model");var markers=new Dictionary<string,Transform>();
        for(int i=1;i<=3;i++)foreach(var end in new[]{"FRONT","REAR"}){string name="COCK_C"+i+"_"+end;var marker=New(name);marker.transform.SetParent(parent.transform,false);marker.transform.localPosition=new Vector3(1.36f,end=="FRONT"?2.43f:1.93f,(i-1)*.24f);markers.Add(name,marker.transform);}
        // Reflection-loaded native controller lists are not copied by this
        // Editor's external-MonoBehaviour serializer. Use the explicitly seeded
        // native controller, then execute shipping Fit and actual native Awake.
        // The old reproduction above still clones a running native emitter.
        var fit=mod.GetType("RearrangedS060.S060ShayEffects",true).GetMethod("Fit",All);
        var stage=source;stage.transform.SetParent(parent.transform,false);
        fit.Invoke(null,new object[]{stage,parent.transform,new Func<string,Transform>(n=>markers[n])});
        {
            Check(!stage.activeInHierarchy,"Shipping fitting retains the inactive assembly boundary");
            Check(stage.GetComponentsInChildren(converterType,true).Length==0,"All nested native copies precede the first Awake");
            var fitted=stage.GetComponentsInChildren(audioType,true).Single();var sounds=(Array)audioType.GetField("cylCockAudio").GetValue(fitted);
            foreach(var sound in sounds)layerType.GetField("layers",All).SetValue(sound,Array.CreateInstance(layerType.GetNestedType("Layer"),0));
            Check(sounds.Length==3,"Three native sound emitters are assembled");
            var fittedController=stage.GetComponentsInChildren(controllerType,true).Single();var fittedDrips=(IList)controllerType.GetField("particlePortReaders").GetValue(fittedController);
            Check(fittedDrips.Count==3,"Native-named CCL drip aliases produce three fitted readers");
            stage.SetActive(true);yield return null;
            foreach(Component sound in sounds)Check(sound.GetComponentsInChildren(converterType,true).Length==1,"Actual native Doppler.Awake creates exactly one converter per fitted emitter");
            for(int cycle=0;cycle<12;cycle++){stage.SetActive(false);stage.SetActive(true);yield return null;Check(stage.GetComponentsInChildren(converterType,true).Length==3,"Repeated activation never duplicates native conversion components");}
            Check(errors.Count==0,"Shipping assembly/activation emits no native errors: "+string.Join(";",errors));
            stage.SetActive(false);DestroyImmediate(stage);yield return null;
        }
    }
    private static bool SkipQueue()=>false;
    private void Patch(Assembly ha,MethodInfo method,string prefix){var hook=Activator.CreateInstance(ha.GetType("HarmonyLib.HarmonyMethod",true),typeof(ShayEffectsPlayModeRunner).GetMethod(prefix,All));var target=harmony.GetType().GetMethod("Patch");var args=new object[target.GetParameters().Length];args[0]=method;args[1]=hook;target.Invoke(harmony,args);}
    private GameObject New(string name,bool active=true){var go=new GameObject(name);go.SetActive(active);owned.Add(go);return go;}
    private void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks++;}
    private void Logged(string text,string stack,LogType type){if(expectingOldFailure){if(text.Contains("DVConvertToEntity")||stack.Contains("DV.DopplerEffects.Doppler.Awake")||stack.Contains("DV.DopplerEffects.Doppler:Awake"))oldErrors++;return;}if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(text);}
    private Assembly Resolve(object sender,ResolveEventArgs args){string name=new AssemblyName(args.Name).Name;var existing=AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a=>a.GetName().Name==name);if(existing!=null)return existing;foreach(var dir in new[]{managed,Path.Combine(managed,"UnityModManager"),Path.Combine(Environment.GetEnvironmentVariable("S060_AUDIT_MODS"),"LocoMeshSplitter")}){var file=Path.Combine(dir,name+".dll");if(File.Exists(file))return Assembly.LoadFrom(file);}return null;}
    private void Finish(bool passed,string error)
    {
        foreach(var go in owned.AsEnumerable().Reverse())if(go!=null){go.SetActive(false);DestroyImmediate(go);}
        if(worldType!=null)worldType.GetProperty("DefaultGameObjectInjectionWorld").SetValue(null,previousWorld,null);(world as IDisposable)?.Dispose();
        if(harmony!=null)harmony.GetType().GetMethod("UnpatchAll").Invoke(harmony,new object[]{"s060.shay.native-awake.audit"});
        Application.logMessageReceived-=Logged;AppDomain.CurrentDomain.AssemblyResolve-=Resolve;
        string output=Environment.GetEnvironmentVariable("S060_SHAY_EFFECTS_AUDIT_OUTPUT");Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output,"shay-effects-validation.json"),JsonUtility.ToJson(new Report{passed=passed,checks=checks,runtimeSha256=hash,actualPlayMode=Application.isPlaying,oldDopplerFailureReproduced=oldErrors>=2,productionInactiveAssembly=true,nativeDopplerAwake=true,gameWorldConversionQueueStubbed=true,nativeAudioConfigurationSeeded=true,audiblePlaybackValidated=false,gameScene=false,error=error},true));EditorApplication.Exit(passed?0:1);
    }
    [Serializable]private sealed class Report{public bool passed,actualPlayMode,oldDopplerFailureReproduced,productionInactiveAssembly,nativeDopplerAwake,gameWorldConversionQueueStubbed,nativeAudioConfigurationSeeded,audiblePlaybackValidated,gameScene;public int checks;public string runtimeSha256,error;}
}
#endif
