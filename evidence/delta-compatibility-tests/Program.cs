using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CarChanger.Common.Configs;
using CarChanger.Game.Components;
using DV.Wheels;
using HarmonyLib;
using LocoSim.Implementations.Wheels;
using Newtonsoft.Json.Linq;
using RearrangedS282;
using UnityEngine;
using UObject=UnityEngine.Object;

internal static partial class Program {
    private static readonly List<string> Checks=new();
    private static void Check(bool result,string name) {if(!result)throw new Exception(name);Checks.Add(name);}
    private static void Near(float a,float b,string name)=>Check(Math.Abs(a-b)<.0002f,name);
    private static void VectorNear(Vector3 a,Vector3 b,string name)=>Check(Math.Abs(a.x-b.x)<.0002f&&Math.Abs(a.y-b.y)<.0002f&&Math.Abs(a.z-b.z)<.0002f,name);
    private static bool QuaternionNear(Quaternion a,Quaternion b)=>Math.Abs(Math.Abs(a.x*b.x+a.y*b.y+a.z*b.z+a.w*b.w)-1)<.0001;
    private static object Hook(string name,params object[] args)=>typeof(DeltaTrailingTruckCompatibility).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
    private static GameObject Child(Transform parent,string name,Vector3? position=null) {
        var o=new GameObject(name);o.transform.SetParent(parent,false);if(position.HasValue)o.transform.localPosition=position.Value;return o;
    }
    private static Transform Path(Transform parent,string path) {
        foreach(var segment in path.Split('/'))parent=parent.Find(segment)??Child(parent,segment).transform;
        return parent;
    }
    private static MeshRenderer Model(Transform parent,string name,Mesh mesh) {
        var o=Child(parent,name);o.AddComponent<MeshFilter>().sharedMesh=mesh;
        var r=o.AddComponent<MeshRenderer>();r.sharedMaterials=new[]{new Material{name=name+" material"}};return r;
    }
    private sealed class Fixture {
        internal TrainCar Car;
        internal Transform BogieRoot,Trailer;
        internal MeshRenderer NativeWheel;
        internal GameObject[] Parts;
        internal bool[] PartFlags;
        internal GameObject Body;
        internal AppliedChange Owner;
        internal PoweredWheelsManager Manager;
        internal PoweredWheel[] Drivers;
        internal Bogie.AxleInfo[] Axles;
        internal WheelRotationViaCode Rotation;
        internal Mesh WheelMesh=new(){name="Delta wheel mesh"},WheelLodMesh=new(){name="Delta wheel LOD mesh"},FrameMesh=new(){name="Delta frame mesh"};
        internal DeltaTrailingTruckVisuals Visuals=>Car.GetComponent<DeltaTrailingTruckVisuals>();
        internal Fixture(string id,float radius=.355f) {
            var root=new GameObject(id);Car=root.AddComponent<TrainCar>();Car.ID=id;
            root.transform.localPosition=new Vector3(20,3,40);root.transform.localRotation=Quaternion.Euler(0,25,0);
            var bogie=Child(root.transform,"RearBogie");Car.RearBogie=bogie.AddComponent<Bogie>();Car.Bogies=new[]{Car.RearBogie};
            BogieRoot=Child(bogie.transform,"bogie_car").transform;
            Manager=root.AddComponent<PoweredWheelsManager>();
            Drivers=Enumerable.Range(0,6).Select(i=>{
                var axle=Child(BogieRoot,"[axle] driver "+i,new Vector3(0,.6096f,10-i*2));
                var driver=axle.AddComponent<PoweredWheel>();driver.index=i;driver.state=i%3;return driver;
            }).ToArray();Manager.poweredWheels=Drivers;
            Trailer=Child(BogieRoot,"[axle] trailer",new Vector3(0,radius,-3.1f)).transform;
            NativeWheel=Model(Trailer,"native trailer model",new Mesh{name="native mesh"});
            Axles=Drivers.Select(d=>new Bogie.AxleInfo(d.transform)).Append(new Bogie.AxleInfo(Trailer)).ToArray();Car.RearBogie.Axles=Axles;
            Rotation=Child(root.transform,"[wheel rotation]").AddComponent<WheelRotationViaCode>();Rotation.transformsToRotate=new[]{Trailer};Rotation.wheelRadius=radius;Rotation.wheelCircumference=radius*2*Mathf.PI;
            var split=Path(root.transform,"LocoS282A_Body/Static_LOD0/s282a_split_body(Clone)");
            Parts=new[]{"s282a_subframe_left","s282a_subframe_right","s282a_trailing_suspension_l","s282a_trailing_suspension_r"}.Select(n=>Child(split,n)).ToArray();
            Parts[1].SetActive(false);PartFlags=Parts.Select(p=>p.activeSelf).ToArray();
            var ownerGo=Child(root.transform,"Applied Delta");Owner=ownerGo.AddComponent<AppliedChange>();Owner.TrainCar=Car;Owner.Config=new LocoS282730AConfig{ModificationId="DeltaTrailingTruckAxles"};
            Body=Child(ownerGo.transform,"Delta body");Owner._body=Body;
            Child(Body.transform,"Axle_F").SetActive(false);
            var sourceBogie=Path(Body.transform,"Axle_R/bogie_car");
            // Duplicate names deliberately match the optional prefab. First two
            // axles carry powered markers and must never become visual sources.
            for(int i=0;i<2;i++){var powered=Child(sourceBogie,"[axle]");powered.AddComponent<CarChanger.Common.Components.PoweredAxle>();Model(powered.transform,"axleR_model",new Mesh{name="wrong powered source"});}
            var sourceAxle=Child(sourceBogie,"[axle]",new Vector3(0,.459f,-2.53f));
            var wheel=Model(sourceAxle.transform,"axleR_model",WheelMesh);
            wheel.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.TwoSided;wheel.receiveShadows=false;wheel.sortingOrder=7;
            var wheelLod=Model(wheel.transform,"LOD1",WheelLodMesh);
            var group=wheel.gameObject.AddComponent<LODGroup>();group.fadeMode=LODFadeMode.CrossFade;group.animateCrossFading=true;group.size=3.4f;group.localReferencePoint=new Vector3(0,.2f,0);
            group.SetLODs(new[]{new LOD(.5f,new Renderer[]{wheel}){fadeTransitionWidth=.1f},new LOD(.15f,new Renderer[]{wheelLod}){fadeTransitionWidth=.2f}});
            Model(sourceBogie,"Delta trailing truck",FrameMesh);
        }
        internal void Apply()=>Check(!(bool)Hook("ApplyDelta",Owner,Owner.Config),Car.ID+": incompatible native Delta callback suppressed");
        internal Transform[] Roots()=>Car.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith(DeltaTrailingTruckVisuals.VisualRootName,StringComparison.Ordinal)).ToArray();
        internal Transform WheelRoot()=>Roots().Single(t=>t.parent==Trailer);
        internal Transform FrameRoot()=>Roots().Single(t=>t.parent==Car.RearBogie.transform);
        internal void NativeIntact(string label) {
            Check(ReferenceEquals(Manager.poweredWheels,Drivers)&&Manager.poweredWheels.Length==6&&Drivers.Select((d,i)=>d.index==i&&d.state==i%3).All(x=>x),label+": six driver identities/states/indices preserved");
            Check(ReferenceEquals(Car.RearBogie.Axles,Axles)&&Axles.Select(a=>a.transform).SequenceEqual(Drivers.Select(d=>d.transform).Append(Trailer)),label+": real axle objects and registered axle array preserved");
            Check(Rotation.transformsToRotate.Length==1&&Rotation.transformsToRotate[0]==Trailer&&Math.Abs(Rotation.wheelCircumference-Rotation.wheelRadius*2*Mathf.PI)<.00001f,label+": native wheel rotation binding/radius preserved");
        }
        internal void Restored(string label) {
            Check(NativeWheel.enabled,label+": native trailer renderer restored");
            Check(Parts.Select(p=>p.activeSelf).SequenceEqual(PartFlags),label+": all four native body visibility flags restored including initially hidden part");
        }
    }
#if !ACTUAL_HARMONY
    public static int Main(string[] args) {
        try {
            var absent=new Harmony();AccessTools.OptionalAvailable=false;DeltaTrailingTruckCompatibility.Register(absent);
            Check(absent.Calls.Count==0,"No optional mod present: no Harmony hooks registered");
            var harmony=new Harmony();AccessTools.OptionalAvailable=true;DeltaTrailingTruckCompatibility.Register(harmony);
            Check(harmony.Calls.Count==5,"Supported optional API receives five narrow integration hooks");
            Check(harmony.Calls.Any(c=>c.Original.DeclaringType==typeof(S282ModelTweaks.PatchAppliedChange)&&c.Original.Name=="ApplyS282730A"&&c.Prefix.Name=="ApplyDelta"),"Delta hook targets the static Model Tweaks callback");
            var a=new Fixture("L-001");var b=new Fixture("L-002");
            var scoped=new object[]{a.Owner,a.Owner.Config,null};Hook("BeginApply",scoped);
            Check(!(bool)Hook("AllowNativeHiding",a.Body)&&(bool)Hook("AllowNativeHiding",b.Body),"Scoped hiding suppression affects only the current Delta body");
            var exception=new Exception("fixture");Check(ReferenceEquals(Hook("EndApply",scoped[2],exception),exception),"Apply finalizer preserves the original exception");
            Check((bool)Hook("AllowNativeHiding",a.Body),"Apply finalizer restores native hiding outside the Delta scope");
            var otherConfig=new LocoS282730AConfig{ModificationId="OtherAxles"};
            Check((bool)Hook("ApplyDelta",a.Owner,otherConfig),"Unrelated axle modifications retain their native callback");

            a.Apply();
            Check(a.Roots().Length==2,"Single active unpowered trailer creates one wheel presentation and one frame presentation");
            Check(a.Car.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh==a.WheelMesh&&!f.transform.IsChildOf(a.Body.transform)).Count()==1,"Duplicate powered source axles are excluded; unpowered Delta source selected");
            Check(!a.NativeWheel.enabled&&a.Parts.All(p=>!p.activeSelf),"Delta hides only native trailer renderers and the four body presentation parts");
            a.NativeIntact("Apply");
            VectorNear(a.WheelRoot().lossyScale,new Vector3(1,.355f/.459f,.355f/.459f),"Delta wheel radius scales to native trailer without changing width");
            var frame=a.FrameRoot();var frameRotation=frame.rotation;var framePosition=frame.position;
            a.Trailer.localRotation=Quaternion.Euler(80,0,0);
            Check(QuaternionNear(frame.rotation,frameRotation)&&!QuaternionNear(a.WheelRoot().rotation,frame.rotation),"Rotating native axle rotates the wheel presentation while truck frame stays fixed");
            VectorNear(frame.position,framePosition,"Wheel spin does not move the truck frame");
            a.Trailer.localRotation=Quaternion.identity;
            var copiedGroup=a.WheelRoot().GetComponentInChildren<LODGroup>(true);var copiedLods=copiedGroup.GetLODs();
            Check(copiedLods.Length==2&&copiedLods.SelectMany(l=>l.renderers).All(r=>r.transform.IsChildOf(a.WheelRoot())),"LOD groups point at cloned renderers rather than source-body renderers");
            Check(copiedGroup.fadeMode==LODFadeMode.CrossFade&&copiedGroup.animateCrossFading&&copiedLods[1].fadeTransitionWidth==.2f,"LOD fade and transition settings preserved");
            var copiedRenderer=a.WheelRoot().GetComponentsInChildren<MeshRenderer>(true).First();
            Check(copiedRenderer.shadowCastingMode==UnityEngine.Rendering.ShadowCastingMode.TwoSided&&!copiedRenderer.receiveShadows&&copiedRenderer.sortingOrder==7,"Renderer presentation settings preserved");
            Check(a.Roots().SelectMany(t=>t.GetComponentsInChildren<Component>(true)).All(c=>c is Transform||c is MeshFilter||c is MeshRenderer||c is LODGroup||c.GetType().Name=="DeltaPresentationMarker"),"Presentation clones contain no powered-wheel or optional-mod runtime components");
            b.Apply();var bRoots=b.Roots();
            a.Visuals.Clear(b.Owner);Check(a.Roots().Length==2,"Cleanup for a different owner cannot remove the active Delta selection");

            for(int cycle=0;cycle<8;cycle++) {
                var suspendedRoots=a.Roots();
                DeltaTrailingTruckCompatibility.Suspend(a.Car);a.Restored("Suspend cycle "+cycle);
                Check(suspendedRoots.All(r=>!r.gameObject.activeSelf&&r.parent==null)&&a.Roots().Length==0,"Suspended presentation inactive and detached before geometry snapshot, cycle "+cycle);
                // An arrangement chooses a new native trailer centre/radius. The
                // next refresh must use these current values, not stale Delta data.
                a.Trailer.localPosition=new Vector3(0,.53f,-3.1f-cycle*.05f);
                a.Rotation.wheelRadius=.53f;a.Rotation.wheelCircumference=.53f*2*Mathf.PI;
                DeltaTrailingTruckCompatibility.Refresh(a.Car);
                Check(a.Roots().Length==2,"Refresh replaces old presentation without accumulating roots, cycle "+cycle);
                VectorNear(a.WheelRoot().lossyScale,new Vector3(1,.53f/.459f,.53f/.459f),"Refresh follows current arrangement trailer radius, cycle "+cycle);
                a.NativeIntact("Refresh cycle "+cycle);
            }
            Check(b.Roots().SequenceEqual(bRoots)&&!b.NativeWheel.enabled,"Switching another car leaves the control car's presentation unchanged");

            DeltaTrailingTruckCompatibility.Suspend(a.Car);a.Trailer.gameObject.SetActive(false);DeltaTrailingTruckCompatibility.Refresh(a.Car);
            Check(a.Roots().Length==0&&!a.Body.activeSelf,"Zero-trailer arrangement suppresses Delta presentation");a.Restored("Zero trailer");a.NativeIntact("Zero trailer");
            a.Trailer.gameObject.SetActive(true);
            var second=Child(a.BogieRoot,"[axle] second trailer",new Vector3(0,.53f,-4.2f));
            a.Car.RearBogie.Axles=a.Axles.Append(new Bogie.AxleInfo(second.transform)).ToArray();DeltaTrailingTruckCompatibility.Refresh(a.Car);
            Check(a.Roots().Length==0&&!a.Body.activeSelf,"Two-trailer arrangement suppresses single-axle Delta presentation");a.Restored("Two trailers");
            a.Car.RearBogie.Axles=a.Axles;UObject.Destroy(second);DeltaTrailingTruckCompatibility.Refresh(a.Car);
            Check(a.Roots().Length==2,"Returning to one trailer restores the retained Delta selection");

            a.Visuals.Clear(a.Owner);
            Check(a.Roots().Length==0&&a.Body.activeSelf&&!a.Body.transform.Find("Axle_F").gameObject.activeSelf&&a.Body.transform.Find("Axle_R").gameObject.activeSelf,"Direct clear restores source-body/root visibility and removes presentation");
            a.Apply();
            Check(!(bool)Hook("AllowNativeReset",a.Owner),"Delta removal skips incompatible native Model Tweaks reset");
            Hook("RemoveDelta",a.Owner);a.Restored("Removal after arrangements");a.NativeIntact("Removal after arrangements");
            Check(a.Roots().Length==0&&!a.Body.activeSelf&&!a.Body.transform.Find("Axle_F").gameObject.activeSelf&&a.Body.transform.Find("Axle_R").gameObject.activeSelf,"Native removal hides its soon-destroyed body and restores child-root flags");
            Hook("RemoveDelta",a.Owner);Check(a.Roots().Length==0,"Repeated Delta removal is idempotent");
            a.Owner.Config=otherConfig;Check((bool)Hook("AllowNativeReset",a.Owner),"Unrelated changes retain their native reset");
            Check(b.Roots().SequenceEqual(bRoots)&&!b.NativeWheel.enabled,"Removing first car leaves control car untouched");
            b.Car.isExploded=true;DeltaTrailingTruckCompatibility.Refresh(b.Car);
            Check(b.Roots().Length==0&&!b.Body.activeSelf,"Exploded car suppresses Delta presentation");b.Restored("Explosion");

            var bad=new Fixture("L-003");bad.Rotation.wheelRadius=0;bad.Rotation.wheelCircumference=0;bad.Apply();
            Check(bad.Roots().Length==0&&!bad.Body.activeSelf,"Invalid rolling radius fails closed with no Delta presentation");bad.Restored("Invalid radius");bad.NativeIntact("Invalid radius");
            var life=new Fixture("L-004");life.Apply();
            var oldOwner=life.Owner;
            var replacement=Child(life.Car.transform,"Replacement owner").AddComponent<AppliedChange>();replacement.TrainCar=life.Car;replacement.Config=oldOwner.Config;replacement._body=life.Body;
            life.Visuals.Bind(replacement,life.Body);UObject.Destroy(oldOwner);life.Visuals.Clear(oldOwner);
            Check(life.Roots().Length==2,"Destroyed old owner cleanup cannot remove a newer owner's presentation");
            UObject.Destroy(replacement);
            typeof(DeltaTrailingTruckVisuals).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(life.Visuals,null);
            Check(life.Roots().Length==0,"Destroyed active owner triggers presentation cleanup");life.Restored("Owner destruction");life.NativeIntact("Owner destruction");
            var refs=typeof(DeltaTrailingTruckCompatibility).Assembly.GetReferencedAssemblies().Select(n=>n.Name).ToArray();
            Check(!refs.Any(n=>n.StartsWith("CarChanger",StringComparison.Ordinal)||n=="S282ModelTweaks"),"Managed harness needs no optional Car Changer/Model Tweaks assembly reference");

            var report=new JObject{["passed"]=true,["checks"]=new JArray(Checks),["limitations"]="Actual Delta adapter and visual source executed with narrow Unity/Harmony/native stand-ins. No Unity renderer, real Harmony dispatch, native axle dynamics, deferred Destroy timing, save/repaint lifecycle or in-game visual validation exercised."};
            Directory.CreateDirectory(args[0]);File.WriteAllText(System.IO.Path.Combine(args[0],"validation.json"),report.ToString());
            Console.WriteLine(Checks.Count+" Delta compatibility checks passed against actual production sources.");return 0;
        }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
#endif
}
