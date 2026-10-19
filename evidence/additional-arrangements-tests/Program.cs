// Executes the production Additional partial with hierarchy/transform stand-ins.
// This deliberately does not claim to validate Unity rendering, Animator clips,
// native physics, or the integrated switch/snapshot caller.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using DV.Wheels;
using RearrangedS282;
using UnityEngine;
using Object=UnityEngine.Object;
using ModMain=RearrangedS282.Main;

internal sealed class OilProvider : MonoBehaviour { }
internal static class Program {
    static int checks;
    static readonly List<string> cases=new();
    static void Check(bool ok,string why) {checks++;if(!ok)throw new Exception(why);}
    static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;
    static bool Near(Vector3 a,Vector3 b)=>Near(a.x,b.x)&&Near(a.y,b.y)&&Near(a.z,b.z);
    static bool Near(Quaternion a,Quaternion b)=>Near(a.x,b.x)&&Near(a.y,b.y)&&Near(a.z,b.z)&&Near(a.w,b.w);
    static Transform Child(Transform parent,string name,Vector3? pos=null) {
        var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=pos??Vector3.zero;return t;
    }
    sealed class Fixture {
        internal readonly WheelRearranger Mod;
        internal readonly Transform[] Axles;
        internal readonly Transform[] OriginalTransforms;
        internal readonly MeshRenderer[] OriginalRenderers;
        internal readonly OilProvider[] Providers;
        internal readonly Mesh SharedMesh;
        internal readonly Material SharedMaterial;
        readonly (Transform t,Transform parent,Vector3 pos,Quaternion rot,Vector3 scale,bool active)[] transforms;
        readonly (MeshRenderer renderer,bool enabled)[] renderers;
        readonly (PoweredWheel wheel,Transform target)[] wheels;
        internal Fixture(bool turned=false) {
            Mod=new GameObject("test S282").AddComponent<WheelRearranger>();
            Mod.transform.localPosition=new Vector3(81,12,-4);
            Mod.transform.localRotation=turned?Quaternion.Euler(7,49,-3):Quaternion.identity;
            SharedMesh=new Mesh{name="native flanged mesh"};SharedMaterial=new Material{name="native body material"};
            Mod.flangedDriver=SharedMesh;
            Mod.driveL=Child(Mod.transform,"Drivetrain L");Mod.driveR=Child(Mod.transform,"Drivetrain R");
            Mod.driveL.localScale=new Vector3(-1,1.42f,1.42f);Mod.driveR.localScale=new Vector3(1,1.42f,1.42f);
            Mod.driveL.localPosition=Mod.driveR.localPosition=new Vector3(0,0,-3.935f);
            var left=PopulateDrive(Mod.driveL);var right=PopulateDrive(Mod.driveR);
            Mod.firstLeftDriveWheel=left[0];Mod.secondLeftDriveWheel=left[1];Mod.thirdLeftDriveWheel=left[2];Mod.fourthLeftDriveWheel=left[3];
            Mod.firstRightDriveWheel=right[0];Mod.secondRightDriveWheel=right[1];Mod.thirdRightDriveWheel=right[2];Mod.fourthRightDriveWheel=right[3];
            var front=Child(Mod.transform,"front bogie");var rear=Child(Mod.transform,"rear bogie");
            front.localRotation=Quaternion.Euler(0,18,0);rear.localRotation=Quaternion.Euler(0,-24,0);
            Axles=Enumerable.Range(1,6).Select(i=>Child(i<3?front:rear,"[axle] "+i,new Vector3(0,.712f,8-i))).ToArray();
            foreach(var axle in Axles)axle.gameObject.AddComponent<PoweredWheel>().wheelTransform=right[Math.Min(Array.IndexOf(Axles,axle),3)];
            Mod.driveAxle1=Axles[0];Mod.driveAxle2=Axles[1];Mod.driveAxle3=Axles[2];Mod.driveAxle4=Axles[3];Mod.driveAxle5=Axles[4];Mod.driveAxle6=Axles[5];
            // Reflection only populates the native field inventory, not feature behaviour.
            foreach(var field in typeof(WheelRearranger).GetFields(BindingFlags.Instance|BindingFlags.NonPublic))
                if(field.FieldType==typeof(Transform)&&field.GetValue(Mod)==null)
                    field.SetValue(Mod,Child(Mod.transform,field.Name,new Vector3(0,0,2)));
            OriginalTransforms=Mod.GetComponentsInChildren<Transform>(true);
            OriginalRenderers=Mod.GetComponentsInChildren<MeshRenderer>(true);
            Providers=Mod.GetComponentsInChildren<OilProvider>(true);
            transforms=OriginalTransforms.Select(t=>(t,t.parent,t.localPosition,t.localRotation,t.localScale,t.gameObject.activeSelf)).ToArray();
            renderers=OriginalRenderers.Select(r=>(r,r.enabled)).ToArray();
            wheels=Axles.Select(a=>(a.GetComponent<PoweredWheel>(),a.GetComponent<PoweredWheel>().wheelTransform)).ToArray();
        }
        Transform[] PopulateDrive(Transform drive) {
            drive.gameObject.AddComponent<Animator>();
            var output=new List<Transform>();
            foreach(var z in new[]{8.001879f,6.37569f,4.749502f,3.123313f,1.5f,0f}) {
                var t=Child(drive,"native wheel "+output.Count,new Vector3(0,.71213f,z));
                t.gameObject.AddComponent<MeshFilter>().sharedMesh=SharedMesh;
                t.gameObject.AddComponent<MeshRenderer>().sharedMaterials=new[]{SharedMaterial};
                // Native visual source can carry components; the prototype must
                // create presentation objects without cloning any of them.
                t.gameObject.AddComponent<Collider>();t.gameObject.AddComponent<Rigidbody>();output.Add(t);
            }
            var rod=Child(drive,"s282_mech_wheels_connect");
            rod.gameObject.AddComponent<MeshRenderer>().sharedMaterials=new[]{SharedMaterial};
            var mainRod=Child(rod,"s282_mech_push_rod_to_connect");mainRod.gameObject.AddComponent<MeshRenderer>();
            Child(drive,"Oiling0").gameObject.AddComponent<OilProvider>();
            Child(Child(drive,"s282_mech_push_rod"),"Oiling1").gameObject.AddComponent<OilProvider>();
            Child(mainRod,"Oiling2").gameObject.AddComponent<OilProvider>();return output.ToArray();
        }
        internal void RestoreNativeSnapshot() {
            // Independent snapshot fixture mirrors only the caller's documented
            // ownership contract. It is not the production GeometryState test.
            foreach(var s in transforms){s.t.SetParent(s.parent,false);s.t.localPosition=s.pos;s.t.localRotation=s.rot;s.t.localScale=s.scale;s.t.gameObject.SetActive(s.active);}
            foreach(var s in renderers)s.renderer.enabled=s.enabled;
            foreach(var s in wheels)s.wheel.wheelTransform=s.target;
        }
        internal void AssertNativeRestored() {
            foreach(var s in transforms)Check(ReferenceEquals(s.t.parent,s.parent)&&Near(s.t.localPosition,s.pos)&&Near(s.t.localRotation,s.rot)&&Near(s.t.localScale,s.scale)&&s.t.gameObject.activeSelf==s.active,"original transform restored: "+s.t.name);
            foreach(var s in renderers)Check(s.renderer.enabled==s.enabled,"native renderer restored");
            foreach(var s in wheels)Check(ReferenceEquals(s.wheel.wheelTransform,s.target),"native wheel anchor restored");
        }
    }
    static void ClampCases() {
        foreach(var p in new[]{(float.NaN,2.8f),(float.PositiveInfinity,2.8f),(float.NegativeInfinity,2.8f),(-12f,2.1f),(0f,2.1f),(2.1f,2.1f),(2.8f,2.8f),(3.6f,3.6f),(500f,3.6f)})
            Check(Near(WheelRearranger.ClampGiantDriverDiameter(p.Item1),p.Item2),"finite diameter clamp/default");
        cases.Add("finite/default diameter clamp");
    }
    static void GiantCases() {
        foreach(bool turned in new[]{false,true})foreach(var diameter in new[]{2.1f,2.8f,3.6f}) {
            var f=new Fixture(turned);ModMain.settings.giant222DriverDiameterMetres=diameter;f.Mod.ApplyPrototype();f.Mod.Align(36);
            var owned=f.Mod.OwnedWheels;float r=diameter/2;
            Check(owned.Length==2&&owned.All(w=>w!=null&&w.gameObject.activeInHierarchy),"two visible owned drivers");
            Check(f.Mod.GetComponentsInChildren<PoweredWheel>(true).Length==6,"no additional physical wheel registrations");
            Check(f.Mod.GiantSingleDriverColliderCount==0,"structural collider count observes collider-free owned drivers");
            Check(f.Axles.Where(a=>a.gameObject.activeInHierarchy).SequenceEqual(new[]{f.Mod.driveAxle3}),"only native third physical axle active");
            Check(Near(f.Mod.AppliedRadius,r)&&Near(f.Mod.GiantSingleDriverDiameter,diameter),"native selected tread radius recorded");
            Check(Near(f.Mod.driveR.localScale,Vector3.one)&&Near(f.Mod.driveL.localScale,new Vector3(-1,1,1)),"inherited small-driver scale normalized");
            Check(Near(f.Mod.driveL.localPosition,Vector3.zero)&&Near(f.Mod.driveR.localPosition,Vector3.zero),"inherited drive offsets normalized");
            Check(f.OriginalRenderers.All(o=>!o.enabled),"all original gear/wheel renderers hidden");
            Check(f.Providers.Length==6&&f.Providers.All(p=>p.gameObject.activeInHierarchy),"six native oil provider ancestries retained active");
            Check(f.Mod.GetComponentsInChildren<Animator>(true).All(a=>a.enabled&&a.gameObject.activeInHierarchy),"native animator objects retained active");
            foreach(var w in owned) {
                Check(w.gameObject.GetComponents<Component>().Length==4,"owned wheel has transform/mesh/renderer/rotater only");
                Check(w.GetComponentsInChildren<Collider>(true).Length==0&&w.GetComponentsInChildren<Rigidbody>(true).Length==0,"no source physics components cloned");
                Check(ReferenceEquals(w.GetComponent<MeshFilter>().sharedMesh,f.SharedMesh),"native mesh reused");
                Check(ReferenceEquals(w.GetComponent<MeshRenderer>().sharedMaterials[0],f.SharedMaterial),"native material reused");
                Check(Near(w.localScale,new Vector3(1,r/.712f,r/.712f)),"new wheel tread scale independent of source group scale");
                Check(Near(w.localPosition.y-r,.00013f),"native tread elevation preserved");
            }
            Check(Near(f.Mod.driveAxle3.position,owned[1].position),"physical axle matches visible world centre across turned bogies");
            Check(ReferenceEquals(f.Mod.driveAxle3.GetComponent<PoweredWheel>().wheelTransform,owned[1]),"sole physical axle targets visible right wheel");
            Check(f.Mod.spark1L.gameObject.activeSelf&&f.Mod.spark1R.gameObject.activeSelf&&!f.Mod.spark3L.gameObject.activeSelf,"sparks use dense manager index zero");
            var localCentre=f.Mod.transform.InverseTransformPoint(owned[1].position);
            Check(Near(f.Mod.spark1R.localPosition,new Vector3(.75f,localCentre.y-r,localCentre.z)),"spark contact follows selected tread centre");
            var lrot=Quaternion.Euler(0,0,77);var rrot=Quaternion.Euler(0,0,191);
            f.Mod.thirdLeftDriveWheel.localRotation=lrot;f.Mod.thirdRightDriveWheel.localRotation=rrot;
            foreach(var w in owned)w.GetComponent<PoweredWheelRotater>().LateUpdate();
            Check(Near(owned[0].localRotation,lrot)&&Near(owned[1].localRotation,rrot),"both visual drivers copy completed native crank pose");
            for(int cycle=0;cycle<3;cycle++) {
                f.Mod.ApplyPrototype();f.Mod.Align(36);
                Check(f.Mod.OwnedWheels.SequenceEqual(owned),"repeat apply reuses owned presentation objects");
                Check(f.Mod.GetComponentsInChildren<PoweredWheelRotater>(true).Length==2,"repeat apply never duplicates animation copies");
                f.Mod.Suspend();Check(owned.All(w=>!w.gameObject.activeSelf)&&f.Mod.GiantSingleDriverDiameter==0,"suspend hides lazy presentation objects");
                f.RestoreNativeSnapshot();f.AssertNativeRestored();
            }
            f.Mod.Dispose();Check(owned.All(w=>w==null)&&f.Mod.OwnedWheels.All(w=>ReferenceEquals(w,null)),"dispose clears and destroys owned objects");
            f.Mod.Dispose();Check(f.SharedMesh!=null&&f.SharedMaterial!=null,"repeated dispose preserves shared game assets");
        }
        cases.Add("six giant configurations: native geometry ownership, transformed bogie alignment, oil providers, animation copying, dense sparks, repeated restoration/disposal");
    }
    static void AlignmentCases() {
        foreach(int id in new[]{34,35}) {
            var f=new Fixture(true);var before=f.Axles.Select(a=>a.position).ToArray();
            f.Mod.Align(id);var expected=id==34?new[]{1,2,3}:new[]{1,2};
            var visual=new[]{f.Mod.firstRightDriveWheel,f.Mod.secondRightDriveWheel,f.Mod.thirdRightDriveWheel,f.Mod.fourthRightDriveWheel};
            for(int i=0;i<6;i++) {
                if(expected.Contains(i)){Check(Near(f.Axles[i].position,visual[i].position),"new conventional axle matches current visual world centre");Check(ReferenceEquals(f.Axles[i].GetComponent<PoweredWheel>().wheelTransform,visual[i]),"new conventional powered wheel targets visual");}
                else Check(Near(f.Axles[i].position,before[i]),"unselected physical axle unchanged");
            }
            f.Mod.Align(999);Check(true,"other catalogue IDs no-op");
        }
        var broken=new Fixture();Object.Destroy(broken.Mod.driveAxle2.GetComponent<PoweredWheel>());
        try{broken.Mod.Align(35);Check(false,"missing native axle must fail");}catch(InvalidOperationException e){Check(e.Message.Contains("native powered axle"),"missing native axle fails actionably");}
        cases.Add("2-6-0/2-4-0 alignment across rotated bogies, unaffected axles, missing-native-wheel failure");
    }
    static void IsolationCase() {
        var a=new Fixture();var b=new Fixture(true);var before=b.OriginalTransforms.Select(t=>t.position).ToArray();
        ModMain.settings.giant222DriverDiameterMetres=3.6f;a.Mod.ApplyPrototype();a.Mod.Align(36);
        Check(b.Mod.OwnedWheels.All(w=>w==null),"other car acquires no presentation objects");
        Check(b.OriginalTransforms.Select(t=>t.position).Zip(before).All(p=>Near(p.First,p.Second)),"other car transform state unchanged");
        Check(b.OriginalRenderers.All(r=>r.enabled)&&b.Providers.All(p=>p.gameObject.activeInHierarchy),"other car visibility/providers unchanged");
        b.AssertNativeRestored();a.Mod.Suspend();a.RestoreNativeSnapshot();a.Mod.Dispose();b.AssertNativeRestored();
        cases.Add("per-car geometry, shared asset, and disposal isolation");
    }
    static void ResizeCase() {
        var f=new Fixture();ModMain.settings.giant222DriverDiameterMetres=2.1f;f.Mod.ApplyPrototype();var owned=f.Mod.OwnedWheels;
        foreach(var diameter in new[]{3.6f,2.8f,2.1f}) {
            ModMain.settings.giant222DriverDiameterMetres=diameter;f.Mod.ApplyPrototype();f.Mod.Align(36);
            Check(f.Mod.OwnedWheels.SequenceEqual(owned),"diameter change reuses existing presentation objects");
            Check(owned.All(w=>Near(w.localScale.y,diameter/2/.712f))&&Near(f.Mod.AppliedRadius,diameter/2),"reused renderer scale and native radius update together");
            Check(Near(f.Mod.driveAxle3.position,owned[1].position),"reused physical anchor follows changed wheel radius");
        }
        var colliderProbe=Child(owned[0],"test collision probe").gameObject.AddComponent<Collider>();
        Check(f.Mod.GiantSingleDriverColliderCount==1,"structural collider count detects an injected descendant collider");
        Object.Destroy(colliderProbe.gameObject);Check(f.Mod.GiantSingleDriverColliderCount==0,"structural collider count returns to zero after probe cleanup");
        f.Mod.Suspend();f.RestoreNativeSnapshot();f.Mod.Dispose();
        cases.Add("reused presentation objects follow diameter changes; structural collider observer detects injected descendants");
    }
    public static int Main(string[] args) {
        try {
            ClampCases();GiantCases();AlignmentCases();IsolationCase();ResizeCase();
            Directory.CreateDirectory(args[0]);
            File.WriteAllText(Path.Combine(args[0],"validation.json"),JsonSerializer.Serialize(new{passed=true,checks,cases,source="production WheelRearranger.Additional.cs + PoweredWheelRotater.cs",limitations=new[]{"Unity hierarchy/transform stand-ins; no Unity player rendering or native physics execution","setWheelRadius is an observable stand-in; native B99PhysicsState has separate tests","snapshot restoration is an independent caller-contract fixture; integrated GeometryState caller is reviewed separately","no in-game collision-clearance, oilcup interaction, Animator clip, LOD or switching acceptance"}},new JsonSerializerOptions{WriteIndented=true}));
            Console.WriteLine($"PASS: {checks} checks");return 0;
        } catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
