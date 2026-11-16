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
        public bool gameRuntimeValidated = false, shayNativeWheelReuse, previewOnly; public int checks; public string runtimeSha256, shayBundleSha256; }
    private static readonly BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static int checks;
    private static readonly List<Object> owned = new List<Object>();
    private static Assembly runtime;
    private static Material material;
    private static string output;
    public static Action<object,GameObject> OilCupAudit;
    public static void RunRodBatch() { Environment.SetEnvironmentVariable("S060_CONVENTIONAL_RODS_ONLY","1"); RunBatch(); }
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
            if(Environment.GetEnvironmentVariable("S060_ONLY_SHAY")!="1")foreach (bool ten in new[] { false, true }) Validate(data, bodyData, ten);
            bool rodsOnly=Environment.GetEnvironmentVariable("S060_CONVENTIONAL_RODS_ONLY")=="1";
            if(!rodsOnly&&!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("S060_SHAY_AUDIT_BUNDLE"))) ValidateShay(data,bodyData);
            if(!rodsOnly&&!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("S060_MEYER_AUDIT_BUNDLE"))) if(Environment.GetEnvironmentVariable("S060_ONLY_SHAY")!="1")ValidateMeyerBody(data,bodyData);
            if(!rodsOnly&&Environment.GetEnvironmentVariable("S060_ONLY_SHAY")!="1")ValidateFixedValveGear(bodyData);
            if(rodsOnly) SteamNormalRodAudit.Validate(output);
            bool preview=Environment.GetEnvironmentVariable("S060_SHAY_PREVIEW_ONLY")=="1",shayOnly=Environment.GetEnvironmentVariable("S060_ONLY_SHAY")=="1";
            File.WriteAllText(Path.Combine(output, "conventional-validation.json"), JsonUtility.ToJson(new Report { passed = true, checks = checks,
                actualNativeMeshes = true, candidateGeometry = true, fiveDrivenAxles = !shayOnly, fullSizeCylinders = !preview, tenOilSites = !shayOnly, cleanup = true, crankSweep = !preview, tyreClearance = !shayOnly, previewOnly=preview, runtimeSha256 = Hash(dll),
                shayNativeWheelReuse = !rodsOnly&&!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("S060_SHAY_AUDIT_BUNDLE")),
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
        var nativeDoor=body.GetComponentsInChildren<MeshFilter>(true).Single(f=>f.name=="s060_smokebox_door");var doorMesh=nativeDoor.sharedMesh;
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
            Check(nativeDoor.sharedMesh==doorMesh&&nativeDoor.gameObject.activeInHierarchy&&nativeDoor.GetComponent<MeshRenderer>().enabled,"Native smokebox door remains present on the fitted conventional layout");
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
                    var pin=wheels[i].TransformPoint(new Vector3(.98730177f,-crank,0));
                    var local=filter.transform.InverseTransformPoint(pin);
                    var cap=vertices.Where(v=>new Vector2(v.y-local.y,v.z-local.z).sqrMagnitude<.000001f).ToArray();
                    Check(cap.Length>=2,"Each native bearing eye retains both original cap centres");
                    var centre=filter.transform.TransformPoint(cap.Aggregate(Vector3.zero,(sum,v)=>sum+v)/cap.Length);
                    Check(Mathf.Abs(centre.y-pin.y)<.00015f&&Mathf.Abs(centre.z-pin.z)<.00015f,"Native coupling-eye centres follow their pins within the retained source-mesh precision.");
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
        var nativeDoor=body.GetComponentsInChildren<MeshFilter>(true).Single(f=>f.name=="s060_smokebox_door");var doorMesh=nativeDoor.sharedMesh;
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
        // Native wheel geometry, original UVs and its OWN S282 atlas. The S060
        // body material previously used here made the private preview misleading.
        var ponyData=JsonUtility.FromJson<Data>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("S060_CONVENTIONAL_NATIVE")),"s282-pony-native.json"))).meshes.Single(m=>m.name=="s282_wheels_front");
        var ponyType=runtime.GetType("RearrangedS060.S060ShayWheelset");var pony=Activator.CreateInstance(ponyType,true);
        var wheelMaterial=new Material(Shader.Find("Standard")){name="LocoS282A_Body (native QA)"};owned.Add(wheelMaterial);
        foreach(var map in new[]{"albedo","metallic"})
        {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,true,map=="metallic");
            texture.LoadImage(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("S060_CONVENTIONAL_NATIVE")),"shay-native-wheel-"+map+".png")));owned.Add(texture);
            wheelMaterial.SetTexture(map=="albedo"?"_MainTex":"_MetallicGlossMap",texture);
        }
        wheelMaterial.EnableKeyword("_METALLICGLOSSMAP");
        ponyType.GetField("Near",All).SetValue(pony,Mesh(ponyData));ponyType.GetField("Material",All).SetValue(pony,wheelMaterial);ponyType.GetField("Scale",All).SetValue(pony,new Vector3(1,.4f/.3552532f,.4f/.3552532f));
        var returned=(Array)Call(owner,"BuildModels",prefab,pony);
        Check(nativeDoor.sharedMesh==doorMesh&&nativeDoor.gameObject.activeInHierarchy&&nativeDoor.GetComponent<MeshRenderer>().enabled,"Shay conversion preserves the separate native smokebox door");
        Check(returned.Length==4,"Shay geometry returns four real native PoweredWheel objects");
        OilCupAudit?.Invoke(owner,root);
        var markers=(Transform[])owner.GetType().GetProperty("Markers",All).GetValue(owner,null);
        var model=(GameObject)owner.GetType().GetProperty("Root",All).GetValue(owner,null);
        var aliases=markers.SelectMany(t=>t.GetComponents<MeshFilter>()).Where(f=>f.name.Contains("_NATIVE_")).ToArray();
        Check(aliases.Length==4&&aliases.All(f=>f.sharedMesh!=null&&f.sharedMesh.name=="s282_wheels_front"),"All four axles alias one complete stock S282 pony wheelset");
        foreach(var f in aliases)Check(Math.Abs(f.transform.localScale.x-1)<1e-6&&Math.Abs(f.transform.localScale.y-.4f/.3552532f)<1e-5&&f.transform.localScale.y==f.transform.localScale.z,"Pony wheelsets scale radially to 0.80 m and retain native X/gauge");
        foreach(var f in aliases)
        {
            Check(f.GetComponent<MeshRenderer>().sharedMaterial==wheelMaterial,"Native wheels retain their own S282 material");
            Check(f.sharedMesh.uv.SequenceEqual(ponyData.uv),"Native wheel UVs remain byte-for-byte unmodified");
        }
        foreach(var f in aliases)Check(Mathf.Abs(f.transform.position.y-root.transform.TransformPoint(0,.4f,0).y)<.002f,"Pony wheelset axle sits at the R2 rolling radius");
        Check(bodyFilter.sharedMesh!=originalBody,"Measured tank and lower-frame relief is a per-car mesh copy");
        ValidateShayRelief(body,bodyFilter.transform,originalBody,bodyFilter.sharedMesh);
        Call(owner,"RefreshMaterials",(object)markers);
        Render(root,"shay-authoring-fit");
        RenderWide(root,"shay-sleeve-detail",new Vector3(4,2.2f,6),new Vector3(.85f,2.1f,3.15f),.6f);
        Check(model.GetComponentsInChildren<MeshRenderer>(true).Where(r=>!r.name.Contains("_NATIVE_")&&r.name!="Native wheel distant").All(r=>r.sharedMaterial!=material),"Every custom mesh uses its own finish rather than the complete native body atlas");
        var reducers=model.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.StartsWith("Shay hollow tapered sleeve")).ToArray();
        Check(reducers.Length==2,"Both native steam sleeves have smooth hollow tapered reducers");
        foreach(var reducer in reducers)
        {
            var mesh=reducer.sharedMesh;
            Check(mesh.vertexCount==528&&mesh.uv.Length==mesh.vertexCount,"Reducer has separate inner/outer walls and mapped annular lips");
            Check(mesh.vertices.All(p=>new Vector2(p.x-mesh.bounds.center.x,p.z-mesh.bounds.center.z).magnitude>.049f),"Reducer steam passage is open without central cap vertices");
        }
        var finishes=markers.Where(t=>!t.name.Contains("_NATIVE_")&&t.name!="Native wheel distant").SelectMany(t=>t.GetComponents<MeshRenderer>()).Select(r=>r.sharedMaterial).Where(m=>m!=null).ToArray();
        Check(!finishes.Any(m=>m==bodyFilter.GetComponent<MeshRenderer>().sharedMaterial),"No Shay part wears the S060 body atlas material");
        Check(!finishes.Any(m=>m.name.Contains("PAINT")||m.name.Contains("painted")||m.name=="gear_steel"||m.name=="shaft_steel"||m.name=="S060_MACHINED"),"Shay painted and machined slots use the run-time gunmetal and steel finishes");
        Check(finishes.Any(m=>m.name=="S060 Shay dirty gunmetal")&&finishes.Any(m=>m.name=="S060 Shay oily steel"),"Both Shay finishes are applied");
        // Reproduce the reported full-atlas repeat after a paint/material
        // replacement. The old fixture only examined untouched bundle slots.
        var finishTargets=markers.SelectMany(t=>t.GetComponents<MeshRenderer>()).Concat(model.GetComponentsInChildren<MeshRenderer>(true)).Distinct()
            .Where(r=>r.sharedMaterial!=null&&r.sharedMaterial.name=="S060 Shay dirty gunmetal").Take(2).ToArray();
        var finishHomes=finishTargets.Select(r=>r.sharedMaterial).ToArray();
        var nativeWheelHomes=markers.Where(t=>t.name.EndsWith("_NATIVE_SET",StringComparison.Ordinal)).SelectMany(t=>t.GetComponents<MeshRenderer>()).ToDictionary(r=>r,r=>r.sharedMaterial);
        foreach(var r in finishTargets)r.sharedMaterial=material;
        Call(model.GetComponent(runtime.GetType("RearrangedS060.S060ShayMaterials",true)),"LateUpdate");
        bool finishRestored=finishTargets.Select((r,i)=>r.sharedMaterial==finishHomes[i]).All(v=>v);
        File.WriteAllText(Path.Combine(output,"shay-finish-paint-validation.json"),"{\"passed\":"+(finishRestored?"true":"false")+",\"fullAtlasReplacementReproduced\":true,\"gameScene\":false}");
        Check(finishRestored,"Shay custom finishes recover after a full-body atlas material replaces them");
        Check(nativeWheelHomes.All(p=>p.Key.sharedMaterial==p.Value),"Finish recovery preserves native wheel UVs and wheel materials");
        if(Environment.GetEnvironmentVariable("S060_SHAY_PREVIEW_ONLY")=="1")
        {
            if(Environment.GetEnvironmentVariable("S060_PIPE_SWEEP_PREVIEW")=="1")
            {
                ValidateShayPipes(root.transform,model.transform,bodyFilter,markers);
                if(model.transform.Find("Shay compressor exhaust branch")!=null) ValidateShayCompressorBranch(root.transform,model.transform);
            }
            RenderWide(root,"shay-pipe-profile",new Vector3(9,2.85f,2.4f),new Vector3(1.0f,2.65f,2.4f),1.05f);
            RenderWide(root,"shay-pipe-front-quarter",new Vector3(4.5f,1.6f,6.3f),new Vector3(1.05f,2.65f,2.75f),.95f);
            RenderWide(root,"shay-pipe-top-quarter",new Vector3(4,6,4.3f),new Vector3(1.0f,2.65f,2.5f),1.10f);
            File.WriteAllText(Path.Combine(output,"shay-fitted-body.json"),JsonUtility.ToJson(new Entry{name="Fitted native Shay body",vertices=bodyFilter.sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(bodyFilter.transform.TransformPoint(v))).ToArray(),triangles=bodyFilter.sharedMesh.triangles}));
            RenderWide(root,"shay-offside",new Vector3(-9,1.7f,.3f),new Vector3(0,1.7f,.3f),2.7f);
            RenderWide(root,"shay-front-low",new Vector3(-5,.7f,8),new Vector3(0,1.0f,2.8f),1.6f);
            RenderWide(root,"shay-bogie-detail",new Vector3(7,.68f,-1.56f),new Vector3(0,.68f,-1.56f),1.05f);
            RenderWide(root,"shay-compressor-detail",new Vector3(5,1.25f,6),new Vector3(1.05f,1.17f,3.15f),.8f);
            Call(owner,"RestoreModels");Check(bodyFilter.sharedMesh==originalBody,"Preview restores exact native body mesh");
            Object.DestroyImmediate(root);bundle.Unload(true);return;
        }
        ValidateShayPipes(root.transform,model.transform,bodyFilter,markers);
        var exhaustReturn=model.GetComponentsInChildren<MeshFilter>(true).Single(f=>f.name=="Shay single-chimney exhaust return");
        ValidateShayCompressorBranch(root.transform,model.transform);
        // Native TrainCarPlate prefab: 10 x 320 x 1300 mm at the measured
        // S060 anchors (+/-1.30, 2.065, 1.871). Include it in this fixture:
        // it is generated separately and was absent from the body-only check.
        var fittings=model.GetComponentsInChildren<MeshFilter>(true).Single(f=>f.name=="Shay pipe flanges, studs and supports");
        foreach(int side in new[]{-1,1})
        {
            var board=GameObject.CreatePrimitive(PrimitiveType.Cube);board.transform.SetParent(root.transform,false);
            board.transform.localPosition=new Vector3(side*1.30f,2.065f,1.871f);board.transform.localScale=new Vector3(.01f,.32f,1.30f);
            try {Check(!new SteamMeshContact.Shell(fittings,root.transform).Intersects(new SteamMeshContact.Shell(board.GetComponent<MeshFilter>(),root.transform)),"Shay collared stays must clear the real information-board dimensions on both sides");}
            finally {Object.DestroyImmediate(board);}
        }
        RenderWide(root,"shay-upper-pipe-detail",new Vector3(5,2.5f,2.2f),new Vector3(1.2f,2.5f,2.2f),.85f);
        var bank=New("S060EngineBank");var slot=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(runtime.GetType("RearrangedS060.S060EngineSlot"));
        var engineType=runtime.GetType("RearrangedS060.S060ShayEngine");var engine=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(engineType);
        var nativeEngine=engineType.BaseType;var engines=Array.CreateInstance(nativeEngine,4);engines.SetValue(engine,3);
        slot.GetType().GetField("Engines",All).SetValue(slot,engines);bank.GetType().GetField("Rear",All).SetValue(bank,slot);
        var engineHomes=model.transform.Find("Engine").GetComponentsInChildren<MeshFilter>(true).ToDictionary(f=>f,f=>f.sharedMesh);
        Check(model.transform.Find("Trucks/BODY_ROOT/SHAY_FRAME_DETAILS")!=null,"R3 includes rigid frame bearers and mounted reservoir");
        var motion=model.AddComponent(runtime.GetType("RearrangedS060.S060ShayMotion"));Call(motion,"Bind",bank,model.transform,bundle.LoadAllAssets<TextAsset>().Single(),markers);
        var transmission=(Component)motion.GetType().GetField("transmission",All).GetValue(motion);
        foreach(float yaw in new[]{-12f,0,12f})
        foreach(double turn in new[]{0.0,.125,.25,.375,.5,.625,.75,.875,1.0,42.875})
        {
            rear.transform.localRotation=Quaternion.Euler(1,yaw,-1);front.transform.localRotation=Quaternion.Euler(-1,-yaw,1);
            Set(engine,"GearTurns",turn);Call(motion,"LateUpdate");
            float engagement=(float)transmission.GetType().GetField("rearEngagement").GetValue(transmission);
            float frontEngagement=(float)transmission.GetType().GetField("frontEngagement").GetValue(transmission);
            Check(engagement>=.08f&&frontEngagement>=.08f,"Shipping articulated links retain the frozen minimum engagement");
            Check(markers.Where(t=>t.name.StartsWith("OIL_CUP_")).Count()==8,"Two supported axlebox cup markers per driven axle survive articulation");
            Check(markers.All(t=>!float.IsNaN(t.localRotation.x)),"Shipping combined engine/transmission produces finite poses");
            if(yaw==0){ValidateShayPowerPose(model.transform.Find("Engine"),bodyFilter,turn);Debug.Log("Shay native power pose "+turn+" passed");}
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
        RenderWide(root,"shay-offside",new Vector3(-9,1.0f,.3f),new Vector3(0,1.0f,.3f),2.6f);
        RenderWide(root,"shay-front-low",new Vector3(-5,.7f,8),new Vector3(0,1.0f,2.8f),1.6f);
        RenderWide(root,"shay-smokebox-door",new Vector3(-2.5f,2.8f,7),new Vector3(0,2.55f,3.75f),1.0f);
        RenderWide(root,"shay-sleeve-detail",new Vector3(4,2.2f,6),new Vector3(.85f,2.1f,3.15f),.6f);
        var detached=markers.Where(t=>t.name=="TRUCK_REAR"||t.name=="TRUCK_FRONT"||t.name=="TRUCK_REAR_PIVOT"||t.name=="TRUCK_FRONT_PIVOT").ToArray();
        Object.DestroyImmediate(motion);
        Check(engineHomes.All(p=>p.Key.sharedMesh==p.Value),"The fully authored R3 engine never replaces its shared engine meshes");
        Call(owner,"RestoreModels");Check(bodyFilter.sharedMesh==originalBody,"Leaving Shay restores the exact native body mesh");
        Check(nativeDoor.sharedMesh==doorMesh&&nativeDoor.GetComponent<MeshRenderer>().enabled,"Leaving Shay retains the exact original smokebox door mesh");
        File.WriteAllText(Path.Combine(output,"native-smokebox-door-validation.json"),"{\"passed\":true,\"separateNativeMesh\":true,\"visibleDuringShay\":true,\"exactMeshPreserved\":true,\"fixtureOnly\":true}");
        Check(detached.All(t=>t==null||!t.gameObject.activeSelf),"Leaving Shay retires every detached truck and transmission frame");
        Object.DestroyImmediate(root);bundle.Unload(true);
    }

    private static void ValidateShayCompressorBranch(Transform space,Transform model)
    {
        var main=model.Find("Shay single-chimney exhaust return").GetComponent<MeshFilter>();
        var branch=model.Find("Shay compressor exhaust branch")?.GetComponent<MeshFilter>() ?? main;
        var pumpJoin=new Bounds(new Vector3(1.31f,2.85f,3.45f),Vector3.one*.16f);
        Check(model.Find("Trucks/BODY_ROOT/SHAY_FRAME_DETAILS").GetComponentsInChildren<MeshFilter>(true)
            .Where(f=>f.name.EndsWith("LOD0")).Any(f=>new SteamMeshContact.Shell(f,space,pumpJoin)
                .Intersects(new SteamMeshContact.Shell(branch,space,pumpJoin))),"Shay exhaust remains connected to the original compressor takeoff");
        if(branch!=main)Check(new SteamMeshContact.Shell(branch,space).Intersects(new SteamMeshContact.Shell(main,space)),"Swept compressor branch joins the main exhaust pipe");
    }
    private static void ValidateShayPipes(Transform space,Transform model,MeshFilter body,Transform[] markers)
    {
        // Exact triangle clipping keeps the rest of each long pipe in the test;
        // only the physical flange/port interfaces are permitted to intersect.
        var ports=markers.Where(t=>new[]{"STEAM_IN","EXHAUST_OUT","SMOKEBOX_RETURN"}.Contains(t.name))
            .Select(t=>new Bounds(space.InverseTransformPoint(t.position),Vector3.one*.25f))
            // Small auxiliary compressor services tee into the existing full-size
            // admission and exhaust courses at these physical junctions only.
            .Concat(new[]{new Bounds(new Vector3(.7052183f,1.95f,3.1679118f),Vector3.one*.16f),new Bounds(new Vector3(1.31f,2.85f,3.45f),Vector3.one*.18f)}).ToArray();
        List<Vector3> Clip(List<Vector3> polygon,int axis,float edge,bool less)
        {
            var result=new List<Vector3>();
            for(int i=0;i<polygon.Count;i++)
            {var a=polygon[i];var b=polygon[(i+1)%polygon.Count];bool ia=less?a[axis]<=edge:a[axis]>=edge,ib=less?b[axis]<=edge:b[axis]>=edge;
             if(ia)result.Add(a);if(ia!=ib)result.Add(Vector3.LerpUnclamped(a,b,(edge-a[axis])/(b[axis]-a[axis])));}
            return result;
        }
        var targets=new[]{body}.Concat(model.Find("Engine/LOD0/FIXED").GetComponentsInChildren<MeshFilter>(true))
            .Concat(model.Find("Trucks/BODY_ROOT/SHAY_FRAME_DETAILS").GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.EndsWith("LOD0"))).ToArray();
        foreach(var f in model.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.StartsWith("Shay ")&&(f.name.EndsWith("feed")||f.name.EndsWith("riser")||f.name.EndsWith("return")||f.name=="Shay compressor exhaust branch")))
        {
            Debug.Log("Checking main Shay pipe: "+f.name);
            var v=f.sharedMesh.vertices.Select(p=>space.InverseTransformPoint(f.transform.TransformPoint(p))).ToArray();var t=f.sharedMesh.triangles;
            var polygons=Enumerable.Range(0,t.Length/3).Select(i=>new[]{v[t[3*i]],v[t[3*i+1]],v[t[3*i+2]]}.ToList()).ToList();
            foreach(var port in ports)
            {
                var next=new List<List<Vector3>>();
                foreach(var polygon in polygons)
                {
                    var remainder=polygon;
                    foreach(var plane in new[]{(a:0,e:port.min.x,l:false),(a:0,e:port.max.x,l:true),(a:1,e:port.min.y,l:false),(a:1,e:port.max.y,l:true),(a:2,e:port.min.z,l:false),(a:2,e:port.max.z,l:true)})
                    {var outside=Clip(remainder,plane.a,plane.e,!plane.l);if(outside.Count>=3)next.Add(outside);remainder=Clip(remainder,plane.a,plane.e,plane.l);if(remainder.Count==0)break;}
                }
                polygons=next;
            }
            var points=new List<Vector3>();foreach(var poly in polygons)for(int i=1;i+1<poly.Count;i++)points.AddRange(new[]{poly[0],poly[i],poly[i+1]});
            var mesh=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,vertices=points.ToArray(),triangles=Enumerable.Range(0,points.Count).ToArray()};
            var go=new GameObject(f.name+" outside actual port interfaces");go.transform.SetParent(space,false);var proxy=go.AddComponent<MeshFilter>();proxy.sharedMesh=mesh;
            try
            {
                var shell=new SteamMeshContact.Shell(proxy,space);
                foreach(var target in targets)Check(!shell.Intersects(new SteamMeshContact.Shell(target,space,shell.Bounds),out var hit),"Main Shay pipe clears native body, fixed engine and frame details: "+f.name+" / "+target.name+" / "+hit);
                Debug.Log("Shay pipe clearance passed: "+f.name);
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(mesh);}
        }
    }

    private static void ValidateShayPowerPose(Transform engine,MeshFilter body,double turn)
    {
        Bounds Used(MeshFilter f,Transform space)
        {
            var points=f.sharedMesh.vertices;var indices=f.sharedMesh.triangles.Distinct().ToArray();
            var b=new Bounds(space.InverseTransformPoint(f.transform.TransformPoint(points[indices[0]])),Vector3.zero);
            foreach(var i in indices)b.Encapsulate(space.InverseTransformPoint(f.transform.TransformPoint(points[i])));b.Expand(.00001f);return b;
        }
        var bore=.252f;
        var actualStroke=Math.Sqrt(1.05*1.05-.065*.065)-Math.Sqrt(.75*.75-.065*.065);
        var calibration=runtime.GetType("RearrangedS060.S060SteamCalibration+Shay");
        Check(Math.Abs((float)calibration.GetField("Stroke",All).GetRawConstantValue()-actualStroke)<.000001,"Shay native stroke matches the actual offset-slider geometry");
        Check((float)calibration.GetField("Bore",All).GetRawConstantValue()==bore,"Shay bore fixture uses the authored cylinder power revision");
        foreach(var lod in engine.Cast<Transform>().Where(t=>t.name.StartsWith("LOD",StringComparison.Ordinal)))
        {
            var fixedParts=lod.Find("FIXED").GetComponentsInChildren<MeshFilter>(true);
            foreach(int i in Enumerable.Range(1,3))
            {
                var pin=lod.Find("CRANK/THROW_"+i+"/Crank_pin_"+i).GetComponent<MeshFilter>();
                var rod=lod.Find("ROD_"+i);var head=lod.Find("CROSSHEAD_"+i);
                var eye=rod.Find("Rod_big_eye_"+i).GetComponent<MeshFilter>();
                var wrist=head.Find("Wrist_pin_"+i).GetComponent<MeshFilter>();
                Check(Vector3.Distance(pin.transform.TransformPoint(pin.sharedMesh.bounds.center),eye.transform.TransformPoint(eye.sharedMesh.bounds.center))<.00001,"Actual longer crank pin meets actual big-end eye throughout the sweep");
                var small=rod.Find("Rod_small_eye_"+i).GetComponent<MeshFilter>();
                Check(Vector3.Distance(small.transform.TransformPoint(small.sharedMesh.bounds.center),wrist.transform.TransformPoint(wrist.sharedMesh.bounds.center))<.00001,"Actual fixed-length rod eye meets the wrist pin throughout the sweep");
                var piston=head.Find("Piston_"+i).GetComponent<MeshFilter>();var pistonBounds=Used(piston,lod);
                Check(pistonBounds.min.y>1.23f+.005f&&pistonBounds.max.y<1.59f-.005f,"Long-stroke piston stays between the shortened internal head spigots");
                var casting=lod.Find("FIXED/Cylinder_casting_"+i).GetComponent<MeshFilter>();
                Check(Math.Abs(casting.sharedMesh.bounds.size.x-.306f)<.00001,"R3 cylinder jackets have 27 mm nominal walls around the 252 mm bore");
                if(turn==0)
                {
                    var chest=lod.Find("FIXED/Valve_chest_"+i).GetComponent<MeshFilter>();
                    var branch=lod.Find("FIXED/Steam_branch_"+i).GetComponent<MeshFilter>();
                    var header=lod.Find("FIXED/Steam_header").GetComponent<MeshFilter>();
                    var area=Used(chest,engine);area.Encapsulate(Used(branch,engine));
                    Check(new SteamMeshContact.Shell(chest,engine,area).Intersects(new SteamMeshContact.Shell(casting,engine,area)),"Moved valve chest still joins the unchanged outer cylinder casting");
                    Check(new SteamMeshContact.Shell(branch,engine,area).Intersects(new SteamMeshContact.Shell(chest,engine,area)),"Adapted steam branch still joins its moved valve chest");
                    Check(new SteamMeshContact.Shell(branch,engine,area).Intersects(new SteamMeshContact.Shell(header,engine,area)),"Adapted branch keeps the original fixed boiler-header connection");
                    var exhaustBranch=lod.Find("FIXED/Exhaust_branch_"+i).GetComponent<MeshFilter>();
                    var exhaustHeader=lod.Find("FIXED/Exhaust_header").GetComponent<MeshFilter>();area.Encapsulate(Used(exhaustBranch,engine));
                    Check(new SteamMeshContact.Shell(exhaustBranch,engine,area).Intersects(new SteamMeshContact.Shell(casting,engine,area)),"Adapted exhaust branch stays connected to the outer cylinder wall");
                    Check(new SteamMeshContact.Shell(exhaustBranch,engine,area).Intersects(new SteamMeshContact.Shell(exhaustHeader,engine,area)),"Adapted exhaust branch keeps its original fixed header connection");
                    Check(!new SteamMeshContact.Shell(chest,engine,area).Intersects(new SteamMeshContact.Shell(body,engine,area)),"Moved valve chest clears the existing native-body pocket");
                }
                var guide=lod.Find("FIXED/Guide_("+(i-1)+", -1)").GetComponent<MeshFilter>();var guideBounds=Used(guide,lod);
                var yoke=lod.Find("FIXED/Guide_lower_yoke_"+i).GetComponent<MeshFilter>();
                foreach(var rail in fixedParts.Where(f=>f.name.StartsWith("Guide_("+(i-1)+",",StringComparison.Ordinal)))
                {
                    var area=Used(rail,engine);
                    Check(new SteamMeshContact.Shell(rail,engine,area).Intersects(new SteamMeshContact.Shell(yoke,engine,area)),"Split guide-bearing rails remain physically joined to both lower yoke strips");
                }
                foreach(var cheek in head.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("Crosshead_cheek_",StringComparison.Ordinal)))
                {var b=Used(cheek,lod);Check(b.min.y>guideBounds.min.y&&b.max.y<guideBounds.max.y,"Both crosshead cheeks remain inside the supported existing guide length");}
                var moving=rod.GetComponentsInChildren<MeshFilter>().Concat(head.GetComponentsInChildren<MeshFilter>()).Concat(lod.Find("CRANK/THROW_"+i).GetComponentsInChildren<MeshFilter>());
                foreach(var f in moving)
                {
                    var area=Used(f,engine);var movingShell=new SteamMeshContact.Shell(f,engine,area);
                    Check(!movingShell.Intersects(new SteamMeshContact.Shell(body,engine,area),out var bodyHit),"Long-stroke Shay motion clears the fitted native body: "+f.name+" / "+bodyHit);
                    foreach(var stationary in fixedParts)
                    {
                        // The original front sliding-bearing face remains an
                        // intended guide/cheek engagement, not a moving-rod gap.
                        if(f.name.StartsWith("Crosshead_cheek_",StringComparison.Ordinal)&&stationary.name.StartsWith("Guide_(",StringComparison.Ordinal))continue;
                        var b=Used(stationary,engine);if(!b.Intersects(area))continue;
                        Check(!movingShell.Intersects(new SteamMeshContact.Shell(stationary,engine,area),out var hit),"Long-stroke Shay motion clears fixed engine geometry: "+f.name+" / "+stationary.name+" / "+hit);
                    }
                }
            }
        }
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
        // The redundant leaf springs are removed whole. Structural plates
        // still reach the measured belly; new original girders are separate.
        Check(after.Any(p=>p.y>=1.08f&&p.y<1.12f&&Mathf.Abs(p.x)>.40f&&Mathf.Abs(p.x)<.62f),"R3 retains native structural plate above the truck bay");
        Check(after.Count(p=>Mathf.Abs(p.x-.75f)<.001f&&p.y>1.62f&&p.y<2.36f)>=2,"Shay relief lines the engine pocket in the right tank");
        foreach(float side in new[]{-1f,1f})Check(after.Count(p=>Mathf.Abs(p.x-side*.86f)<.001f&&p.y>.969f&&p.y<1.085f)>=4,"Shay frame sill fills the band above the trucks on both sides");
        foreach(float side in new[]{-1f,1f})Check(after.Any(p=>Mathf.Abs(p.x-side*.86f)<.001f&&p.z>3.5f&&p.y>=.97f),"Shay frame sills run on to the front buffer beam");
        Check(after.Count(p=>Mathf.Abs(p.y-1.07f)<.001f&&p.x<-.87f&&p.x>-1.12f)>=4,"Shay off-side oil cup shelves stand on the sill");
        Check(!after.Any(p=>In(p,new Vector3(-.75f,.05f,-2.5f),new Vector3(.75f,1.05f,2.8f))&&!(Mathf.Abs(p.z)<=.201f&&p.y>=.659f)),"No body triangle remains inside the R2 truck bay, except the cross stretcher above the 0.59 m mid-sweep");
    }

    // Meyer R2: the runtime's own body-mounted rear bodywork on the native S060 body,
    // with the real donor engine units, rendered and checked against the rear unit sweep.
    private static void ValidateMeyerBody(Data data,Entry bodyData)
    {
        var root=new GameObject("Native S060 Meyer R2 fixture");
        var body=new GameObject("LocoS060_Body").transform;body.SetParent(root.transform,false);
        Draw(body,"s060_body",Mesh(bodyData,v=>!SteamNativePreview.NativeCylinder(v,-1)&&!SteamNativePreview.NativeCylinder(v,1)));
        var stub=new GameObject("rear frame stub").transform;stub.SetParent(root.transform,false);
        stub.localPosition=new Vector3(0,0,-6.165f);
        foreach(var name in new[]{"[Attached rear platform]","[Meyer rear pannier L]","[Meyer rear pannier R]","[Period rear chassis]"})new GameObject(name).transform.SetParent(stub,false);
        var type=runtime.GetType("RearrangedS060.S060MeyerBodywork",true);
        var owner=type.GetMethod("Build",All,null,new[]{typeof(Transform),typeof(Transform),typeof(Transform)},null).Invoke(null,new object[]{root.transform,body,stub});
        var parts=((Transform)type.GetProperty("Root",All).GetValue(owner,null)).GetComponentsInChildren<MeshFilter>(true);
        var upperNames=new[]{"Meyer R2 paintwork","Meyer R2 ironwork","Meyer R2 buffer beam","Meyer R2 hopper tarpaulin"};
        var upperParts=parts.Where(f=>upperNames.Contains(f.name)).ToArray();
        Check(parts.Length==8&&upperParts.Length==4&&parts.All(f=>f.sharedMesh!=null&&f.sharedMesh.vertexCount>0),"Meyer builds four upper meshes, fixed bearers, short drawgear, loco fork and heavy link");
        Check(stub.GetComponentsInChildren<MeshFilter>(true).Count(f=>f.name=="Meyer bogie drawgear pin")==1,"Rear pin and fork belong to the moving unit");
        var oldNames=new[]{"[Attached rear platform]","[Meyer rear pannier L]","[Meyer rear pannier R]","[Period rear chassis]"};
        Check(stub.Cast<Transform>().Where(t=>oldNames.Contains(t.name)).Count()==4&&stub.Cast<Transform>().Where(t=>oldNames.Contains(t.name)).All(t=>!t.gameObject.activeSelf),"Meyer retires the original bogie-borne deck, panniers and bunker box");
        Check(stub.GetComponentsInChildren<MeshFilter>(true).Count(f=>f.name=="Meyer rear plate frame and spring saddles"&&f.gameObject.activeSelf)==1,"New rear underframe belongs to the moving unit");
        // Preserve this original upper-body envelope check. The two new lower
        // frames are covered by the measured 27-pose underframe/contact suite.
        var verts=upperParts.SelectMany(f=>f.sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(f.transform.TransformPoint(v)))).ToArray();
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
            // The runtime's own LP exhaust return, bound to the donor rear frame, for the picture.
            var rearFrame=copy.GetComponentsInChildren<Transform>(true).First(t=>t.name=="bogie_car"&&t.parent!=null&&t.parent.name=="BogieR");
            var pipeType=runtime.GetType("RearrangedS060.S060MeyerExhaustPipe",true);
            var pipe=pipeType.GetMethod("Bind",All).Invoke(null,new object[]{body,rearFrame,body});
            RenderWide(root,"meyer-r2-side",new Vector3(12,2.0f,-2.2f),new Vector3(0,2.0f,-2.2f),4.3f);
            RenderWide(root,"meyer-r2-rear",new Vector3(9,4.5f,-12),new Vector3(0,1.9f,-4.5f),3.6f);
        }
        finally{bundle.Unload(true);type.GetMethod("Dispose").Invoke(owner,null);Object.DestroyImmediate(root);}
    }
    private static void RenderWide(GameObject body,string name,Vector3 from,Vector3 at,float size)
    {
        var scene=EditorSceneManager.NewPreviewScene(); var clone=Object.Instantiate(body); clone.SetActive(true); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clone,scene);
        // Unity does not clone per-renderer property blocks. Preserve native
        // brake emission in the preview instead of silently displaying cold shoes.
        var originals=body.GetComponentsInChildren<Renderer>(true);var copies=clone.GetComponentsInChildren<Renderer>(true);
        if(originals.Length!=copies.Length)throw new InvalidOperationException("Preview renderer hierarchy changed.");
        for(int i=0;i<originals.Length;i++){var block=new MaterialPropertyBlock();originals[i].GetPropertyBlock(block);copies[i].SetPropertyBlock(block);}
        var cameraGo=new GameObject("QA camera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo,scene); var camera=cameraGo.AddComponent<Camera>(); camera.scene=scene; camera.orthographic=true; camera.orthographicSize=size;
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.84f,.88f,.92f); camera.transform.position=from; camera.transform.LookAt(at);
        var lightGo=new GameObject("QA light"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo,scene); var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(45,from.x < at.x ? 150 : -30,0);
        var target=new RenderTexture(1800,900,24); camera.targetTexture=target; camera.Render(); var previous=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(1800,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1800,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+"-native-preview.png"),image.EncodeToPNG());
        RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(target);EditorSceneManager.ClosePreviewScene(scene);
    }

    private static void ValidateFixedValveGear(Entry bodyData)
    {
        var root=new GameObject("Native fixed valve gear retirement fixture");
        var body=Draw(root.transform,"s060_body",Mesh(bodyData));
        var filter=body.GetComponent<MeshFilter>();var original=filter.sharedMesh;
        var uv=original.uv;var normals=original.normals;
        var type=runtime.GetType("RearrangedS060.S060FixedValveGear",true);
        IDisposable owner=null;
        try
        {
            owner=(IDisposable)type.GetMethod("Apply",All).Invoke(null,new object[]{root.transform});
            var copy=filter.sharedMesh;
            Check(copy!=original&&original.triangles.Length-copy.triangles.Length>600,"Retire both measured curved castings and their detached mountings");
            Check(copy.uv.SequenceEqual(uv)&&copy.normals.SequenceEqual(normals),"Retiring gear preserves UVs and normals on retained native geometry");
            var kept=new HashSet<int>(copy.triangles);var v=original.vertices;
            var removed=original.triangles.Distinct().Where(i=>!kept.Contains(i)).Select(i=>v[i]).ToArray();
            Check(removed.Any(p=>p.x>.83f)&&removed.Any(p=>p.x<-.83f),"Retire the fixed gear on both sides");
            Check(removed.All(p=>Mathf.Abs(p.x)>=.83f&&Mathf.Abs(p.x)<=1.20f&&p.y>=.87f&&p.y<=1.64f&&p.z>=.72f&&p.z<=1.34f),"No wheels, moving rods, cylinders or frame faces removed outside the reported gear region");
            RenderWide(root,"retired-fixed-gear",new Vector3(-5,1.30f,1.02f),new Vector3(0,1.30f,1.02f),.62f);
            owner.Dispose();owner=null;
            Check(filter.sharedMesh==original,"Restoring stock restores the exact original fixed gear mesh");
            owner=(IDisposable)type.GetMethod("Apply",All).Invoke(null,new object[]{root.transform});owner.Dispose();owner=null;
            Check(filter.sharedMesh==original,"Repeated fixed gear retirement/restoration is reversible");
        }
        finally{owner?.Dispose();Object.DestroyImmediate(root);}
    }

    private static void Render(GameObject body,string name)
    {
        var scene=EditorSceneManager.NewPreviewScene(); var clone=Object.Instantiate(body); clone.SetActive(true); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clone,scene);
        var cameraGo=new GameObject("QA camera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo,scene); var camera=cameraGo.AddComponent<Camera>(); camera.scene=scene; camera.orthographic=true; camera.orthographicSize=2.8f;
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.84f,.88f,.92f); camera.transform.position=new Vector3(10,2.2f,0); camera.transform.LookAt(new Vector3(0,1.7f,0));
        var lightGo=new GameObject("QA light"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo,scene); var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(45,-30,0);
        var target=new RenderTexture(1600,900,24); camera.targetTexture=target; camera.Render(); var previous=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+"-native-preview.png"),image.EncodeToPNG());
        RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(target);EditorSceneManager.ClosePreviewScene(scene);
    }
    private static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks++;}
    private static string Hash(string path){using(var sha=SHA256.Create())using(var s=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(s)).Replace("-","");}
    private static Assembly Resolve(object sender,ResolveEventArgs args)
    {
        string name=new AssemblyName(args.Name).Name;var found=AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a=>a.GetName().Name==name);if(found!=null)return found;
        string managed=Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED");foreach(var dir in new[]{managed,Path.Combine(managed,"UnityModManager")}){string p=Path.Combine(dir,name+".dll");if(File.Exists(p))return Assembly.LoadFrom(p);}return null;
    }
}
