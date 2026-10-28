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
using Object = UnityEngine.Object;

// Native meshes/transforms + shipping geometry, rods and cylinder fittings.
// Native ECS initialization/traction is outside this isolated geometry fixture.
public static class ConventionalNativeAudit
{
    [Serializable] private class Entry { public string name; public Vector3[] vertices; public Vector2[] uv; public int[] triangles; }
    [Serializable] private class Node { public string name, mesh; public Vector3 position, scale; public Quaternion rotation; public Node[] children; }
    [Serializable] private class Data { public Node[] roots; public Entry[] meshes; }
    [Serializable] private class Report { public bool passed, actualNativeMeshes, candidateGeometry, fiveDrivenAxles, fullSizeCylinders, tenOilSites, cleanup, crankSweep, tyreClearance;
        public bool gameRuntimeValidated = false, shayNativeWheelReuse; public int checks; public string runtimeSha256, shayBundleSha256; }
    private static readonly BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static int checks;
    private static readonly List<Object> owned = new List<Object>();
    private static Assembly runtime;
    private static Material material;
    private static string output;
    public static void RunBatch()
    {
        output = Environment.GetEnvironmentVariable("S060_CONVENTIONAL_OUTPUT"); Directory.CreateDirectory(output);
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try
        {
            string dll = Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME"); runtime = Assembly.LoadFrom(dll);
            var data = JsonUtility.FromJson<Data>(File.ReadAllText(Environment.GetEnvironmentVariable("S060_CONVENTIONAL_NATIVE")));
            var bodyData = JsonUtility.FromJson<Data>(File.ReadAllText(Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW"))).meshes.Single(m => m.name == "s060_body");
            var texture = new Texture2D(2, 2); texture.LoadImage(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW")), "native-preview-atlas.png"))); owned.Add(texture);
            material = new Material(Shader.Find("Standard")); material.mainTexture = texture; owned.Add(material);
            foreach (bool ten in new[] { false, true }) Validate(data, bodyData, ten);
            if(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("S060_SHAY_AUDIT_BUNDLE"))) ValidateShay(data,bodyData);
            if(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("S060_MEYER_AUDIT_BUNDLE"))) ValidateMeyerBody(data,bodyData);
            File.WriteAllText(Path.Combine(output, "conventional-validation.json"), JsonUtility.ToJson(new Report { passed = true, checks = checks,
                actualNativeMeshes = true, candidateGeometry = true, fiveDrivenAxles = true, fullSizeCylinders = true, tenOilSites = true, cleanup = true, crankSweep = true, tyreClearance = true, runtimeSha256 = Hash(dll),
                shayNativeWheelReuse = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("S060_SHAY_AUDIT_BUNDLE")),
                shayBundleSha256 = Hash(Environment.GetEnvironmentVariable("S060_SHAY_AUDIT_BUNDLE")) }, true));
            EditorApplication.Exit(0);
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(output,"error.txt"), error.ToString()); Debug.LogException(error); EditorApplication.Exit(1); }
        finally { foreach (var obj in owned) if (obj != null) Object.DestroyImmediate(obj); }
    }
    private static object New(string name) => Activator.CreateInstance(runtime.GetType("RearrangedS060." + name, true), true);
    private static void Set(object owner, string name, object value)
    {
        for(var type=owner.GetType();type!=null;type=type.BaseType)
        {
            var property=type.GetProperty(name,All|BindingFlags.DeclaredOnly);
            if(property?.GetSetMethod(true)!=null) {property.GetSetMethod(true).Invoke(owner,new[]{value});return;}
        }
        throw new InvalidOperationException("Fixture property setter missing: "+name);
    }
    private static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name, All).Invoke(owner,args);
    private static Mesh Mesh(Entry entry, Func<Vector3,bool> keep = null)
    {
        var mesh = new Mesh { name = entry.name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        var triangles=keep == null ? entry.triangles : entry.triangles.Where((v,i) => {int first=i-i%3; return Enumerable.Range(first,3).All(j=>keep(entry.vertices[entry.triangles[j]]));}).ToArray();
        var used=triangles.Distinct().ToArray(); var remap=used.Select((v,i)=>new {v,i}).ToDictionary(p=>p.v,p=>p.i);
        mesh.vertices=used.Select(i=>entry.vertices[i]).ToArray(); mesh.uv=used.Select(i=>entry.uv[i]).ToArray(); mesh.triangles=triangles.Select(i=>remap[i]).ToArray();
        mesh.RecalculateBounds(); mesh.RecalculateNormals(); owned.Add(mesh); return mesh;
    }
    private static Transform Draw(Transform parent, string name, Mesh mesh)
    { var go=new GameObject(name); go.transform.SetParent(parent,false); go.AddComponent<MeshFilter>().sharedMesh=mesh; go.AddComponent<MeshRenderer>().sharedMaterial=material; return go.transform; }
    private static Transform Tree(Transform parent, Node node, Data data)
    {
        var go=new GameObject(node.name); go.transform.SetParent(parent,false); go.transform.localPosition=node.position; go.transform.localRotation=node.rotation; go.transform.localScale=node.scale;
        if (!string.IsNullOrEmpty(node.mesh)) {go.AddComponent<MeshFilter>().sharedMesh=Mesh(data.meshes.Single(e=>e.name==node.mesh)); go.AddComponent<MeshRenderer>().sharedMaterial=material;}
        foreach(var child in node.children) Tree(go.transform,child,data);
        // Lowest-detail native children are exported for reference; only the
        // highest-detail mesh is drawn in this transient preview.
        if (node.name.Contains("LOD")) go.SetActive(false);
        return go.transform;
    }
    private static void Validate(Data data, Entry bodyData, bool ten)
    {
        var root=new GameObject("Native S060 conventional fixture"); root.SetActive(false);
        var game=Assembly.LoadFrom(Path.Combine(Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED"),"Assembly-CSharp.dll"));
        var car=root.AddComponent(game.GetType("TrainCar",true));
        var body=new GameObject("LocoS060_Body").transform; body.SetParent(root.transform,false);
        foreach(var node in data.roots) Tree(body,node,data);
        bool Cylinder(Vector3 v,int side) => SteamNativePreview.NativeCylinder(v,side);
        bool Brake(Vector3 v) => Mathf.Abs(v.x)>=.41f&&Mathf.Abs(v.x)<=1f&&v.y<=.8215f&&((v.z>=2.16f&&v.z<=2.59f)||(v.z>=.6f&&v.z<=1.31f)||(v.z>=-.91f&&v.z<=-.57f));
        // Match the existing native preview/splitter masks: original brakes are
        // hidden by S060DriverScale, not retained inside the body QA mesh.
        Draw(body,"s060_body",Mesh(bodyData,v=>!Cylinder(v,-1)&&!Cylinder(v,1)&&!Brake(v)));
        var cylinders=new List<Transform>();
        foreach(int side in new[]{-1,1}) cylinders.Add(Draw(body,side<0?"s060_cylinder_left":"s060_cylinder_right",Mesh(bodyData,v=>Cylinder(v,side)&&!Brake(v))));
        var beforeCylinder=cylinders.Select(c=>c.GetComponent<MeshFilter>().sharedMesh.bounds).ToArray();
        var parts=New("S060Parts"); Set(parts,"Car",car); Set(parts,"Body",body);
        var left=body.Find("DrivingMechanism_L"); var right=body.Find("DrivingMechanism_R"); Set(parts,"MechanismLeft",left); Set(parts,"MechanismRight",right);
        var front=new GameObject("Axle_F"); front.transform.SetParent(root.transform,false);
        var rear=new GameObject("Axle_R"); rear.transform.SetParent(root.transform,false);
        var bogieType=game.GetType("Bogie",true); Set(parts,"FrontBogie",front.AddComponent(bogieType)); Set(parts,"RearBogie",rear.AddComponent(bogieType));
        var poweredType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LocoSim.Implementations.Wheels.PoweredWheel")).First(t=>t!=null);
        var wheelMap=(IDictionary)parts.GetType().GetField("wheels",All).GetValue(parts);
        var axleMap=(IDictionary)parts.GetType().GetField("axleForWheel",All).GetValue(parts);
        var poweredMap=(IDictionary)parts.GetType().GetField("poweredForWheel",All).GetValue(parts);
        for(int index=1;index<=3;index++)
        {
            wheelMap.Add(index,new List<Transform>{left.Find("s060_Wheels_0"+index),right.Find("s060_Wheels_0"+index)});
            var axle=new GameObject("[axle]"); axle.transform.SetParent(index==1?rear.transform:front.transform,false);
            var powered=axle.AddComponent(poweredType); poweredType.GetField("wheelTransform",All).SetValue(powered,right.Find("s060_Wheels_0"+index));
            axleMap.Add(index,axle.transform); poweredMap.Add(index,powered);
        }
        var scale=Activator.CreateInstance(runtime.GetType("RearrangedS060.S060DriverScale",true),All,null,new[]{parts},null);
        float factor=ten?1f/1.424256f:.8f; Call(scale,"Apply",factor);
        var owner=New(ten?"S060TenCoupled":"S060EightCoupled"); Set(owner,"Parts",parts); Set(owner,"Scale",scale);
        Check(!Application.isPlaying,"This native geometry fixture runs only in EditMode, without the game lifecycle.");
        root.SetActive(true);
        IDisposable rods=null;
        try
        {
            var powered=(Array)Call(owner,"BuildGeometry"); int count=ten?5:4;
            Check(powered.Length==count,"Candidate must return every live driven axle.");
            rods=(IDisposable)runtime.GetType("RearrangedS060.S060CouplingRods",true).GetMethod("Apply",All).Invoke(null,new[]{parts});
            float shift=(float)owner.GetType().GetProperty("CylinderShift",All).GetValue(owner,null); Call(scale,"PositionFittings",shift);
            var wheels=right.Cast<Transform>().Where(t=>t.name.StartsWith("s060_Wheels_0")&&!t.name.Contains("LOD")).OrderBy(t=>t.position.z).ToArray();
            Check(wheels.Length==count,"Each side must have the required wheel count.");
            for(int i=1;i<wheels.Length;i++)
            {
                float a=wheels[i-1].GetComponent<MeshFilter>().sharedMesh.bounds.max.z*factor;
                float b=-wheels[i].GetComponent<MeshFilter>().sharedMesh.bounds.min.z*factor;
                Check(wheels[i].position.z-wheels[i-1].position.z-a-b>.015f,"Actual adjacent flange envelopes need running clearance.");
            }
            Check(Mathf.Abs(wheels[0].position.z-(ten?-2f:-1.3978f))<.00001f,"Actual rearmost wheel must use the fitted station.");
            Check(wheels[0].position.z-.7582f*factor>-2.7f,"Rear flange must stay forward of cab entry.");
            Check(wheels.Last().position.z+.7582f*factor<3.47f,"Front flange must clear the coupler band.");
            for(int i=0;i<cylinders.Count;i++)
            {
                Check(cylinders[i].localScale==Vector3.one&&cylinders[i].GetComponent<MeshFilter>().sharedMesh.bounds==beforeCylinder[i],"Native cylinder castings must never be rescaled.");
                Check(cylinders[i].position.z+beforeCylinder[i].max.z<4.0921f,"Moved full-size cylinders must remain behind the native nose: "+ten+" end="+(cylinders[i].position.z+beforeCylinder[i].max.z));
            }
            Check(left.localScale.x==-1&&right.localScale.x==1,"Native gauge and mirror remain unchanged.");
            var cups=root.AddComponent(runtime.GetType("RearrangedS060.S060OilCups",true)); cups.GetType().GetField("car",All).SetValue(cups,car);
            var sites=(IList)Call(cups,"Sites"); Check(sites.Count==2*count,"Every driven axle must expose two oil-cup sites.");
            Render(body.gameObject,ten?"0100":"080");
            if(ten) Sweep(body,left,right,root.transform,cylinders);
        }
        finally
        {
            rods?.Dispose(); Call(owner,"RestoreGeometry"); Call(owner,"RestoreGeometry"); Call(scale,"Restore");
            Check(right.Cast<Transform>().Count(t=>t.name.StartsWith("s060_Wheels_0")&&!t.name.Contains("LOD"))==3,"Cleanup must restore the three native wheels.");
            Check(left.localScale==new Vector3(-1,1,1)&&right.localScale==Vector3.one,"Cleanup must restore native mechanism scales.");
            Check(cylinders.All(c=>c.localPosition==Vector3.zero),"Cleanup restores native cylinder/outlet placement.");
            Object.DestroyImmediate(root);
        }
    }
    private static void Sweep(Transform body,Transform left,Transform right,Transform space,List<Transform> cylinders)
    {
        var surfaces=cylinders.Select(c=>c.GetComponent<MeshFilter>()).ToList();
        var bodyFilter=body.Find("s060_body").GetComponent<MeshFilter>();var source=bodyFilter.sharedMesh;
        // Unity copies mesh arrays on each getter. Snapshot once before the
        // face filter instead of copying the whole body for every vertex.
        var bodyVertices=source.vertices;var bodyTriangles=source.triangles;
        var low=new Mesh {vertices=bodyVertices,triangles=bodyTriangles.Where((v,i)=>{int first=i-i%3;return Enumerable.Range(first,3).Any(j=>bodyVertices[bodyTriangles[j]].y<1.15f);}).ToArray()};owned.Add(low);
        var lower=new GameObject("Native lower body surface");lower.transform.SetParent(body,false);lower.AddComponent<MeshFilter>().sharedMesh=low;
        surfaces.Add(lower.GetComponent<MeshFilter>());
        foreach(var side in new[]{left,right})
        {
            var wheels=side.Cast<Transform>().Where(t=>t.name.StartsWith("s060_Wheels_0")&&!t.name.Contains("LOD")).OrderBy(t=>t.localPosition.z).ToArray();
            var originals=wheels.ToDictionary(w=>w,w=>w.localRotation);var drive=side.Find("s060_Wheels_01");var rod=side.Find("s060_mech_wheels_connect");
            var rest=rod.localPosition;const float crank=.335356f;var initial=drive.TransformPoint(new Vector3(.98730177f,-crank,0));
            var tyres=new Dictionary<Transform,MeshFilter>();
            var targets=new Dictionary<Transform,SteamMeshContact.Shell[]>();
            foreach(var wheel in wheels)
            {
                var mesh=wheel.GetComponent<MeshFilter>().sharedMesh;var wheelVertices=mesh.vertices;var wheelTriangles=mesh.triangles;
                var tireMesh=new Mesh {vertices=wheelVertices,triangles=wheelTriangles.Where((v,i)=>{int first=i-i%3;return Enumerable.Range(first,3).All(j=>wheelVertices[wheelTriangles[j]].x>.70f);}).ToArray()};owned.Add(tireMesh);
                var go=new GameObject("Actual outer wheel/flange");go.transform.SetParent(wheel,false);var filter=go.AddComponent<MeshFilter>();filter.sharedMesh=tireMesh;tyres.Add(wheel,filter);
                // Conservative full-revolution tyre envelope. Triangle AABBs
                // outside it cannot contact this wheel at any sampled phase.
                var centre=space.InverseTransformPoint(wheel.position)+new Vector3(side==left?-.95f:.95f,0,0);
                var region=new Bounds(centre,new Vector3(.75f,1.2f,1.2f));
                targets.Add(wheel,surfaces.Select(f=>new SteamMeshContact.Shell(f,space,region)).ToArray());
            }
            foreach(float phase in new[]{0f,90f,180f,270f,315f,225f,135f,45f})
            {
                foreach(var wheel in wheels.Where(w=>!w.name.Contains("rearranged"))) wheel.localRotation=originals[wheel]*Quaternion.Euler(phase,0,0);
                foreach(var wheel in wheels.Where(w=>w.name.Contains("rearranged")))
                {var follow=wheel.GetComponent(runtime.GetType("RearrangedS060.S060FollowPose",true));Call(follow,"LateUpdate");}
                rod.localPosition=rest+side.InverseTransformVector(drive.TransformPoint(new Vector3(.98730177f,-crank,0))-initial);
                foreach(var wheel in wheels)
                {
                    Check(Quaternion.Angle(wheel.localRotation,drive.localRotation)<.001f,"Every added wheel follows the native crank through forward/reverse poses.");
                    var shell=new SteamMeshContact.Shell(tyres[wheel],space);
                    foreach(var target in targets[wheel]) Check(!shell.Intersects(target,out var contact),"Five-axle wheel/flange touches native body/cylinder at phase "+phase+": "+contact);
                }
                var filter=rod.GetComponent<MeshFilter>();var vertices=filter.sharedMesh.vertices;
                for(int i=0;i<wheels.Length;i++)
                {
                    var boss=Vector3.zero;for(int v=i*512;v<(i+1)*512;v++)boss+=vertices[v]/512f;var centre=filter.transform.TransformPoint(boss);
                    var pin=wheels[i].TransformPoint(new Vector3(.98730177f,-crank,0));
                    Check(Mathf.Abs(centre.y-pin.y)<.00003f&&Mathf.Abs(centre.z-pin.z)<.00003f,"Every fitted coupling boss stays on its driven pin through crank motion.");
                }
            }
            foreach(var wheel in wheels)wheel.localRotation=originals[wheel];rod.localPosition=rest;
        }
    }
    private static void ValidateShay(Data data,Entry bodyData)
    {
        var root=new GameObject("Native S060 Shay fixture"); root.SetActive(false);
        var game=Assembly.LoadFrom(Path.Combine(Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED"),"Assembly-CSharp.dll"));
        var car=root.AddComponent(game.GetType("TrainCar",true));
        var body=new GameObject("LocoS060_Body").transform; body.SetParent(root.transform,false);
        foreach(var node in data.roots) Tree(body,node,data);
        bool Cylinder(Vector3 v,int side) => SteamNativePreview.NativeCylinder(v,side);
        bool Brake(Vector3 v) => Mathf.Abs(v.x)>=.41f&&Mathf.Abs(v.x)<=1f&&v.y<=.8215f&&((v.z>=2.16f&&v.z<=2.59f)||(v.z>=.6f&&v.z<=1.31f)||(v.z>=-.91f&&v.z<=-.57f));
        // Match the existing native preview/splitter masks: original brakes are
        // hidden by S060DriverScale, not retained inside the body QA mesh.
        Draw(body,"s060_body",Mesh(bodyData,v=>!Cylinder(v,-1)&&!Cylinder(v,1)&&!Brake(v)));
        var cylinders=new List<Transform>();
        foreach(int side in new[]{-1,1}) cylinders.Add(Draw(body,side<0?"s060_cylinder_left":"s060_cylinder_right",Mesh(bodyData,v=>Cylinder(v,side)&&!Brake(v))));
        var beforeCylinder=cylinders.Select(c=>c.GetComponent<MeshFilter>().sharedMesh.bounds).ToArray();
        var parts=New("S060Parts"); Set(parts,"Car",car); Set(parts,"Body",body);
        var left=body.Find("DrivingMechanism_L"); var right=body.Find("DrivingMechanism_R"); Set(parts,"MechanismLeft",left); Set(parts,"MechanismRight",right);
        var front=new GameObject("Axle_F"); front.transform.SetParent(root.transform,false);front.transform.localPosition=new Vector3(0,0,.925f);
        var frontFrame=new GameObject("bogie_car");frontFrame.transform.SetParent(front.transform,false);
        var rear=new GameObject("Axle_R"); rear.transform.SetParent(root.transform,false);rear.transform.localPosition=new Vector3(0,0,-1.4f);
        var rearFrame=new GameObject("bogie_car");rearFrame.transform.SetParent(rear.transform,false);
        var bogieType=game.GetType("Bogie",true); Set(parts,"FrontBogie",front.AddComponent(bogieType)); Set(parts,"RearBogie",rear.AddComponent(bogieType));
        var poweredType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LocoSim.Implementations.Wheels.PoweredWheel")).First(t=>t!=null);
        var wheelMap=(IDictionary)parts.GetType().GetField("wheels",All).GetValue(parts);
        var axleMap=(IDictionary)parts.GetType().GetField("axleForWheel",All).GetValue(parts);
        var poweredMap=(IDictionary)parts.GetType().GetField("poweredForWheel",All).GetValue(parts);
        for(int index=1;index<=3;index++)
        {
            wheelMap.Add(index,new List<Transform>{left.Find("s060_Wheels_0"+index),right.Find("s060_Wheels_0"+index)});
            var axle=new GameObject("[axle]"); axle.transform.SetParent(index==1?rearFrame.transform:frontFrame.transform,false);
            var powered=axle.AddComponent(poweredType); poweredType.GetField("wheelTransform",All).SetValue(powered,right.Find("s060_Wheels_0"+index));
            axleMap.Add(index,axle.transform); poweredMap.Add(index,powered);
        }

        var bundlePath=Environment.GetEnvironmentVariable("S060_SHAY_AUDIT_BUNDLE");
        var bundle=AssetBundle.LoadFromFile(bundlePath);
        var prefab=bundle.LoadAsset<GameObject>("assets/shayintegration/s060shay.prefab");
        Check(prefab!=null,"Shipping Shay prefab loads by its verified asset path");
        var owner=New("S060Shay");Set(owner,"Parts",parts);root.SetActive(true);
        var bodyFilter=body.GetComponentsInChildren<MeshFilter>(true).Single(f=>f.name=="s060_body");var originalBody=bodyFilter.sharedMesh;
        // R2 wheels: the stock S282 pony wheelset, from a private QA fixture in place of the live prefab.
        var ponyData=JsonUtility.FromJson<Data>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("S060_CONVENTIONAL_NATIVE")),"s282-pony-native.json"))).meshes.Single(m=>m.name=="s282_wheels_front");
        var ponyType=runtime.GetType("RearrangedS060.S060ShayWheelset");var pony=Activator.CreateInstance(ponyType,true);
        ponyType.GetField("Near",All).SetValue(pony,Mesh(ponyData));ponyType.GetField("Material",All).SetValue(pony,material);
        var returned=(Array)Call(owner,"BuildModels",prefab,pony);
        Check(returned.Length==4,"Shay geometry returns four real native PoweredWheel objects");
        var markers=(Transform[])owner.GetType().GetProperty("Markers",All).GetValue(owner,null);
        var model=(GameObject)owner.GetType().GetProperty("Root",All).GetValue(owner,null);
        var aliases=markers.SelectMany(t=>t.GetComponents<MeshFilter>()).Where(f=>f.name.Contains("_NATIVE_")).ToArray();
        Check(aliases.Length==4&&aliases.All(f=>f.sharedMesh!=null&&f.sharedMesh.name=="s282_wheels_front"),"All four axles alias one complete stock S282 pony wheelset");
        foreach(var f in aliases)Check(f.transform.localScale==Vector3.one,"Pony wheelsets keep stock size and gauge (unscaled)");
        foreach(var f in aliases)Check(Mathf.Abs(f.transform.position.y-root.transform.TransformPoint(0,.3552532f,0).y)<.002f,"Pony wheelset axle sits at the R2 rolling radius");
        Check(bodyFilter.sharedMesh!=originalBody,"Measured tank and lower-frame relief is a per-car mesh copy");
        ValidateShayRelief(body,bodyFilter.transform,originalBody,bodyFilter.sharedMesh);
        Call(owner,"RefreshMaterials",(object)markers);
        var finishes=markers.Where(t=>!t.name.Contains("_NATIVE_")&&t.name!="Native wheel distant").SelectMany(t=>t.GetComponents<MeshRenderer>()).Select(r=>r.sharedMaterial).Where(m=>m!=null).ToArray();
        Check(!finishes.Any(m=>m==bodyFilter.GetComponent<MeshRenderer>().sharedMaterial),"No Shay part wears the S060 body atlas material");
        Check(!finishes.Any(m=>m.name.Contains("PAINT")||m.name.Contains("painted")||m.name=="gear_steel"||m.name=="shaft_steel"||m.name=="S060_MACHINED"),"Shay painted and machined slots use the run-time gunmetal and steel finishes");
        Check(finishes.Any(m=>m.name=="S060 Shay dirty gunmetal")&&finishes.Any(m=>m.name=="S060 Shay oily steel"),"Both Shay finishes are applied");
        var bank=New("S060EngineBank");var slot=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(runtime.GetType("RearrangedS060.S060EngineSlot"));
        var engineType=runtime.GetType("RearrangedS060.S060ShayEngine");var engine=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(engineType);
        var nativeEngine=engineType.BaseType;var engines=Array.CreateInstance(nativeEngine,4);engines.SetValue(engine,3);
        slot.GetType().GetField("Engines",All).SetValue(slot,engines);bank.GetType().GetField("Rear",All).SetValue(bank,slot);
        var motion=model.AddComponent(runtime.GetType("RearrangedS060.S060ShayMotion"));Call(motion,"Bind",bank,model.transform,bundle.LoadAllAssets<TextAsset>().Single(),markers);
        var transmission=(Component)motion.GetType().GetField("transmission",All).GetValue(motion);
        foreach(float yaw in new[]{-12f,0,12f})
        foreach(double turn in new[]{0.0,.125,.25,.75,1.0,42.875})
        {
            rear.transform.localRotation=Quaternion.Euler(1,yaw,-1);front.transform.localRotation=Quaternion.Euler(-1,-yaw,1);
            Set(engine,"GearTurns",turn);Call(motion,"LateUpdate");
            float engagement=(float)transmission.GetType().GetField("rearEngagement").GetValue(transmission);
            float frontEngagement=(float)transmission.GetType().GetField("frontEngagement").GetValue(transmission);
            Check(engagement>=.08f&&frontEngagement>=.08f,"Shipping articulated links retain the frozen minimum engagement");
            Check(markers.Where(t=>t.name.StartsWith("OIL_CUP_")).Count()==8,"Two supported axlebox cup markers per driven axle survive articulation");
            Check(markers.All(t=>!float.IsNaN(t.localRotation.x)),"Shipping combined engine/transmission produces finite poses");
        }
        rear.transform.localRotation=front.transform.localRotation=Quaternion.identity;
        // The game's 0.20.2 trace showed an empty parts list: prove every gear role is bound and actually turns.
        var bound=((Array)transmission.GetType().GetField("parts").GetValue(transmission)).Cast<object>().ToArray();
        Check(bound.Length==43,"All 43 Shay transmission parts are bound from the bundle's motion data");
        Transform Role(string role)=>(Transform)bound.Select(p=>new{role=(string)p.GetType().GetField("role").GetValue(p),t=(Transform)p.GetType().GetField("transform").GetValue(p)}).First(p=>p.role==role).t;
        var turning=new[]{"outlet","line","pinion","wheel","cross"}.Select(Role).ToArray();
        Set(engine,"GearTurns",0.0);Call(motion,"LateUpdate");var at0=turning.Select(t=>t.localRotation).ToArray();
        Set(engine,"GearTurns",.3);Call(motion,"LateUpdate");
        for(int i=0;i<turning.Length;i++)Check(Quaternion.Angle(at0[i],turning[i].localRotation)>1,"Shay "+turning[i].name+" turns with the gear clock");
        Set(engine,"GearTurns",0.0);Call(motion,"LateUpdate");
        Render(root,"shay");
        var detached=markers.Where(t=>t.name=="TRUCK_REAR"||t.name=="TRUCK_FRONT"||t.name=="TRUCK_REAR_PIVOT"||t.name=="TRUCK_FRONT_PIVOT").ToArray();
        Call(owner,"RestoreModels");Check(bodyFilter.sharedMesh==originalBody,"Leaving Shay restores the exact native body mesh");
        Check(detached.All(t=>t==null||!t.gameObject.activeSelf),"Leaving Shay retires every detached truck and transmission frame");
        Object.DestroyImmediate(root);bundle.Unload(true);
    }

    // Whole-piece relief: protected cab steps survive untouched, the outside reverser
    // goes whole, and the trimmed frame is closed by belly, sills and the engine pocket.
    private static void ValidateShayRelief(Transform body,Transform filter,Mesh original,Mesh relief)
    {
        Vector3[] Centroids(Mesh mesh)
        {
            var v=mesh.vertices.Select(p=>body.InverseTransformPoint(filter.TransformPoint(p))).ToArray();var t=mesh.triangles;
            return Enumerable.Range(0,t.Length/3).Select(i=>(v[t[3*i]]+v[t[3*i+1]]+v[t[3*i+2]])/3).ToArray();
        }
        bool In(Vector3 p,Vector3 min,Vector3 max)=>p.x>min.x&&p.y>min.y&&p.z>min.z&&p.x<max.x&&p.y<max.y&&p.z<max.z;
        var before=Centroids(original);var after=Centroids(relief);
        foreach(float s in new[]{1f,-1f})
        {
            Vector3 min=new Vector3(s>0?1.10f:-1.36f,.40f,-3.45f),max=new Vector3(s>0?1.36f:-1.10f,1.32f,-2.78f);
            int native=before.Count(p=>In(p,min,max));
            Check(native>0&&after.Count(p=>In(p,min,max))==native,"Shay relief keeps every cab step/ladder triangle whole");
        }
        // Engine-side reverser parts only; the deck, tank strip and frame plate share the wider box.
        Vector3 rMin=new Vector3(.90f,1.44f,-.51f),rMax=new Vector3(1.28f,1.555f,-.07f);
        Check(before.Any(p=>In(p,rMin,rMax))&&!after.Any(p=>In(p,rMin,rMax)),"Shay relief removes the outside reverser whole, never half");
        Check(after.Count(p=>Mathf.Abs(p.y-1.085f)<.001f&&Mathf.Abs(p.x)<.8f)>=2,"Shay R2 relief closes the trimmed frame with a belly plate at 1.085 m");
        // R2: the native frame plates stay down to the belly, so they close the sides themselves.
        Vector3 fMin=new Vector3(.40f,1.10f,-1.0f),fMax=new Vector3(.62f,1.60f,2.5f);
        Check(after.Count(p=>In(p,fMin,fMax))>=before.Count(p=>In(p,fMin,fMax))*.9f,"Shay R2 relief keeps the native frame band above the trucks");
        Check(after.Count(p=>Mathf.Abs(p.x-.835f)<.001f&&p.y>1.62f&&p.y<2.36f)>=2,"Shay relief lines the engine pocket in the right tank");
        Check(!after.Any(p=>In(p,new Vector3(-.75f,.05f,-2.5f),new Vector3(.75f,1.05f,2.8f))),"No native body triangle remains inside the R2 truck bay");
    }

    // Meyer R2: the runtime's own body-mounted rear bodywork on the native S060 body,
    // with the real donor engine units, rendered and checked against the rear unit sweep.
    private static void ValidateMeyerBody(Data data,Entry bodyData)
    {
        var root=new GameObject("Native S060 Meyer R2 fixture");
        var body=new GameObject("LocoS060_Body").transform;body.SetParent(root.transform,false);
        Draw(body,"s060_body",Mesh(bodyData,v=>!SteamNativePreview.NativeCylinder(v,-1)&&!SteamNativePreview.NativeCylinder(v,1)));
        var stub=new GameObject("rear frame stub").transform;stub.SetParent(root.transform,false);
        foreach(var name in new[]{"[Attached rear platform]","[Meyer rear pannier L]","[Meyer rear pannier R]","[Period rear chassis]"})new GameObject(name).transform.SetParent(stub,false);
        var type=runtime.GetType("RearrangedS060.S060MeyerBodywork",true);
        var owner=type.GetMethod("Build",All,null,new[]{typeof(Transform),typeof(Transform),typeof(Transform)},null).Invoke(null,new object[]{root.transform,body,stub});
        var parts=((Transform)type.GetProperty("Root",All).GetValue(owner,null)).GetComponentsInChildren<MeshFilter>(true);
        Check(parts.Length==4&&parts.All(f=>f.sharedMesh!=null&&f.sharedMesh.vertexCount>0),"Meyer R2 builds paintwork, ironwork, buffer beam and hopper coal");
        Check(stub.Cast<Transform>().All(t=>!t.gameObject.activeSelf),"Meyer R2 retires the old bogie-borne deck, panniers and bunker box");
        var verts=parts.SelectMany(f=>f.sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(f.transform.TransformPoint(v)))).ToArray();
        // Rear engine unit sweep (yaw +-6, pitch/roll +-1, +-20 mm): z -7.77..-4.13, top 1.61 m.
        Check(verts.Where(v=>v.z>-7.80f&&v.z<-4.10f).All(v=>v.y>=1.615f),"Meyer R2 bodywork stays above the rear unit's swept envelope");
        Check(verts.Max(v=>v.y)<3.87f&&verts.Min(v=>v.z)>-8.28f&&verts.Max(v=>Mathf.Abs(v.x))<1.42f,"Meyer R2 bodywork stays inside the cab roof, coupler and body width");
        // Real donor engine units for the picture; native wheels for its empty aliases.
        var bundle=AssetBundle.LoadFromFile(Environment.GetEnvironmentVariable("S060_MEYER_AUDIT_BUNDLE"));
        try
        {
            var pack=bundle.LoadAllAssets<CCL.Types.CustomCarPack>().FirstOrDefault();
            var donor=pack?.Cars.SelectMany(c=>c.liveries).Select(l=>l.prefab).FirstOrDefault(g=>g!=null&&g.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="[Meyer rear pannier L]"));
            Check(donor!=null,"Meyer donor prefab with two engine units loads");
            var copy=Object.Instantiate(donor,root.transform);copy.transform.localPosition=Vector3.zero;
            // The donor's standalone body alias is not used by the runtime (only its two engine frames are).
            foreach(var t in copy.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="S060 body (native alias)").ToArray())t.gameObject.SetActive(false);
            foreach(var t in copy.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="[Attached rear platform]"||t.name.StartsWith("[Meyer rear pannier")||t.name=="[Period rear chassis]"||t.name.StartsWith("[S060 transfer pipe")).ToArray())t.gameObject.SetActive(false);
            var wheel=Mesh(data.meshes.Single(m=>m.name=="s060_Wheels_02"));
            foreach(var f in copy.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh==null&&f.name.Contains("driver"))){f.sharedMesh=wheel;var r=f.GetComponent<MeshRenderer>();if(r!=null)r.sharedMaterial=material;}
            foreach(var c in copy.GetComponentsInChildren<Component>(true).Where(c=>c is Collider||c is MonoBehaviour).ToArray())Object.DestroyImmediate(c);
            RenderWide(root,"meyer-r2-side",new Vector3(12,2.0f,-2.2f),new Vector3(0,2.0f,-2.2f),4.3f);
            RenderWide(root,"meyer-r2-rear",new Vector3(9,4.5f,-12),new Vector3(0,1.9f,-4.5f),3.6f);
        }
        finally{bundle.Unload(true);type.GetMethod("Dispose").Invoke(owner,null);Object.DestroyImmediate(root);}
    }
    private static void RenderWide(GameObject body,string name,Vector3 from,Vector3 at,float size)
    {
        var scene=EditorSceneManager.NewPreviewScene(); var clone=Object.Instantiate(body); clone.SetActive(true); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clone,scene);
        var cameraGo=new GameObject("QA camera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo,scene); var camera=cameraGo.AddComponent<Camera>(); camera.scene=scene; camera.orthographic=true; camera.orthographicSize=size;
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.84f,.88f,.92f); camera.transform.position=from; camera.transform.LookAt(at);
        var lightGo=new GameObject("QA light"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo,scene); var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(45,-30,0);
        var target=new RenderTexture(1800,900,24); camera.targetTexture=target; camera.Render(); var previous=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(1800,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1800,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+"-native-preview.png"),image.EncodeToPNG());
        RenderTexture.active=previous;Object.DestroyImmediate(image);Object.DestroyImmediate(target);EditorSceneManager.ClosePreviewScene(scene);
    }

    private static void Render(GameObject body,string name)
    {
        var scene=EditorSceneManager.NewPreviewScene(); var clone=Object.Instantiate(body); clone.SetActive(true); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clone,scene);
        var cameraGo=new GameObject("QA camera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo,scene); var camera=cameraGo.AddComponent<Camera>(); camera.scene=scene; camera.orthographic=true; camera.orthographicSize=2.8f;
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.84f,.88f,.92f); camera.transform.position=new Vector3(10,2.2f,0); camera.transform.LookAt(new Vector3(0,1.7f,0));
        var lightGo=new GameObject("QA light"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo,scene); var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(45,-30,0);
        var target=new RenderTexture(1600,900,24); camera.targetTexture=target; camera.Render(); var previous=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+"-native-preview.png"),image.EncodeToPNG());
        RenderTexture.active=previous;Object.DestroyImmediate(image);Object.DestroyImmediate(target);EditorSceneManager.ClosePreviewScene(scene);
    }
    private static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks++;}
    private static string Hash(string path){using(var sha=SHA256.Create())using(var s=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(s)).Replace("-","");}
    private static Assembly Resolve(object sender,ResolveEventArgs args)
    {
        string name=new AssemblyName(args.Name).Name;var found=AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a=>a.GetName().Name==name);if(found!=null)return found;
        string managed=Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED");foreach(var dir in new[]{managed,Path.Combine(managed,"UnityModManager")}){string p=Path.Combine(dir,name+".dll");if(File.Exists(p))return Assembly.LoadFrom(p);}return null;
    }
}
