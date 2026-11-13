#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using CCL.Types;
using RearrangedS060;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
public static class MeyerSmoothSteamAudit
{
    private const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
    private static int checks,poses;private static string output;
    private static readonly List<Bounds> ports=new List<Bounds>();
    public static void RunBatch()
    {
        output=Environment.GetEnvironmentVariable("S060_SMOOTH_STEAM_OUTPUT");Directory.CreateDirectory(output);
        ResolveEventHandler resolver=(s,a)=>{string n=new AssemblyName(a.Name).Name;var loaded=AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(v=>v.GetName().Name==n);if(loaded!=null)return loaded;string p=Path.Combine(Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED"),n+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
        AppDomain.CurrentDomain.AssemblyResolve+=resolver;
        var native=new List<Object>();var owners=new List<IDisposable>();GameObject root=null;AssetBundle bundle=null;
        try
        {
            checks+=SteamMeshContact.ValidateAlgorithm();
            var runtime=Assembly.LoadFrom(Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME"));
            bundle=AssetBundle.LoadFromFile(Environment.GetEnvironmentVariable("S060_MEYER_AUDIT_BUNDLE"));
            var donor=bundle.LoadAllAssets<CustomCarPack>().Single().Cars.SelectMany(c=>c.liveries).Select(l=>l.prefab).First(g=>g!=null&&g.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="bogie_car")==2);
            root=Object.Instantiate(donor);root.SetActive(false);Check(SteamNativePreview.Apply(root,native),"Native geometry fixture required.");
            foreach(var script in root.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);
            var detailAudit=MeyerRunningGearAudit.Prepare(root,native);
            IDisposable frontMotion=null,frontMounts=null,rearMounts=null,equipment=null,bodywork=null,clearance=null,runningGear=null;
            var frames = root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "bogie_car").OrderBy(t => root.transform.InverseTransformPoint(t.position).z).ToArray();
            var rear = frames[0]; var front = frames[1];
            if (true)
            {
                var layout = runtime.GetType("RearrangedS060.S060MeyerLayout", true);
                front.localRotation = (Quaternion)layout.GetProperty("FrontRotation", All).GetValue(null, null);
                rear.localPosition += (Vector3)layout.GetProperty("RearVisualOffset", All).GetValue(null, null);
            }
            root.SetActive(true);
            var beforeAssembly = root.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).ToDictionary(f => f, f => f.sharedMesh);
            var stockBody = beforeAssembly.First(p => p.Key.name == "s060_body");
            if (true)
            {
                frontMotion = (IDisposable)runtime.GetType("RearrangedS060.S060MeyerFrontMotion", true).GetMethod("Bind", All, null, new[] { typeof(Transform) }, null).Invoke(null, new object[] { front });
                SteamNativePreview.RodSources(root.transform, native);
                var mountBind = runtime.GetType("RearrangedS060.S060MeyerGearMounts", true).GetMethod("Bind", All);
                frontMounts = (IDisposable)mountBind.Invoke(null, new object[] { front, root.transform });
                rearMounts = (IDisposable)mountBind.Invoke(null, new object[] { rear, root.transform });
                runningGear=detailAudit.Bind(runtime,front,rear);
                foreach (var frame in new[] { front, rear })
                    foreach (var wheel in frame.Find("[axle] rear").GetComponentsInChildren<MeshFilter>(true))
                        checks += MeyerInwardPhaseAudit.CheckMainJournal(beforeAssembly[wheel], wheel.sharedMesh);
                // Existing native preview imports the old complete castings;
                // apply the same compiled per-car placement datum as Bind.
                float shift = (float)runtime.GetType("RearrangedS060.S060MeyerLayout").GetField("FrontCylinderShift", All).GetRawConstantValue();
                foreach (var f in front.GetComponentsInChildren<MeshFilter>(true).Where(f => f.name.StartsWith("Native S060 full-size cylinder QA ", StringComparison.Ordinal))) f.transform.localPosition += Vector3.forward * shift;
                foreach (string side in new[] { "L", "R" })
                    foreach (var renderer in front.Find("[Round cylinders " + side + "]").GetComponentsInChildren<Renderer>(true))
                        renderer.enabled = renderer.name == "Cylinder saddle";
                equipment = (IDisposable)runtime.GetType("RearrangedS060.S060MeyerFrontEquipment", true).GetMethod("Bind", All, null, new[] { typeof(Transform), typeof(Transform) }, null).Invoke(null, new object[] { root.transform, root.transform });
                bodywork = (IDisposable)runtime.GetType("RearrangedS060.S060MeyerBodywork", true).GetMethod("Build", All, null, new[] { typeof(Transform), typeof(Transform), typeof(Transform) }, null).Invoke(null, new object[] { root.transform, root.transform, rear });
            }
            // Removed proxy readers do not evaluate the donor Animator. Pose
            // both real clips explicitly before any gear contact or preview.
            // Native p=.125 maps to inward-front q=-p+.75=.625 (phase audit).
            foreach (var pose in new[] { (frame: front, phase: .625f), (frame: rear, phase: .125f) })
            {
                var animator = pose.frame.GetComponent<Animator>();
                var clip = animator.runtimeAnimatorController.animationClips.Distinct().Single();
                clip.SampleAnimation(pose.frame.gameObject, pose.phase * clip.length); animator.enabled = false;
            }
            if (true)
            {
                frontMotion.GetType().GetMethod("ApplyPose", All).Invoke(frontMotion, null);
                foreach (var frame in new[] { front, rear }) checks += MeyerInwardPhaseAudit.CheckGearMounts(frame, true);
            }
            var beforeClearance = root.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).ToDictionary(f => f, f => f.sharedMesh);
            if (true) clearance = (IDisposable)runtime.GetType("RearrangedS060.S060MeyerClearance", true).GetMethod("Apply", All, null, new[] { typeof(Transform) }, null).Invoke(null, new object[] { root.transform });
            var source = root.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).ToDictionary(f => f, f => f.sharedMesh);
            owners.AddRange(new[]{frontMotion,frontMounts,rearMounts,runningGear,equipment,bodywork,clearance});
            var pipeType=runtime.GetType("RearrangedS060.S060SteamPipes",true);
            var rendererHomes=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.GetComponentsInParent<Transform>(true).Any(t=>t.name.StartsWith("[S060 manifold ")||t.name.StartsWith("[S060 transfer pipe "))).ToDictionary(r=>r,r=>r.enabled);
            var pipeOwner=(IDisposable)pipeType.GetMethod("Bind",All).Invoke(null,new object[]{root.transform,front,rear,root.transform});owners.Add(pipeOwner);
            var update=pipeType.GetMethod("UpdateGeometry",All);
            var hp=pipeType.GetProperty("InwardPipes",All).GetValue(pipeOwner,null);var lp=pipeType.GetProperty("ExhaustReturn",All).GetValue(pipeOwner,null);
            // Fitting only: iterate the existing pipe authoring source without
            // rebuilding production. Final validation leaves this unset and
            // exercises the exact candidate DLL throughout.
            S060MeyerExhaustPipe fittingReturn=null;
            if(Environment.GetEnvironmentVariable("S060_REPAIR_FIT_SOURCE")=="1")
            {
                ((IDisposable)lp).Dispose();
                fittingReturn=S060MeyerExhaustPipe.Bind(root.transform,rear,root.transform);
                lp=fittingReturn;owners.Add(fittingReturn);
            }
            Transform Group(object o,string n)=>(Transform)o.GetType().GetProperty(n,All).GetValue(o,null);
            var hpBody=Group(hp,"BodyRoot");var hpFront=Group(hp,"FrontRoot");var lpBody=Group(lp,"BodyRoot");var lpRear=Group(lp,"RearRoot");
            var pipes=new[]{hpBody,hpFront,lpBody,lpRear}.SelectMany(t=>t.GetComponentsInChildren<MeshFilter>()).ToArray();
            Check(pipes.Count(f=>f.name.StartsWith("Single "))==1,"Exactly one visible long rear trunk.");
            Check(pipes.Count(f=>f.name.StartsWith("HP smokebox feed "))==2,"Two HP feeds.");
            foreach(var frame in new[]{front,rear})foreach(var group in frame.Cast<Transform>().Where(t=>t.name.StartsWith("[S060 manifold ")||t.name.StartsWith("[S060 transfer pipe ")))
                Check(group.GetComponentsInChildren<Renderer>(true).All(r=>!r.enabled),"All old Meyer side pipes and manifolds retired.");
            var originalCoupling=NativeCouplingPreview.Apply(root,native);
            Check(originalCoupling.frontEmbed>=-.001f&&originalCoupling.frontEmbed<.015f,"Native front valve housing seated on beam.");
            Check(originalCoupling.rearEmbed>=-.001f&&originalCoupling.rearEmbed<.015f,"Native rear valve housing seated on R2 beam.");
            // Explicit attachment volumes cover genuine ports only. They do not
            // exempt the cab, tanks, floor, wheels or long pipe courses.
            foreach(var f in hpBody.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("HP smokebox feed ")))
            {var p=Cap(f,false,root.transform);var b=new Bounds(p+Vector3.down*.13f,new Vector3(.32f,.34f,.32f));ports.Add(b);}
            foreach(var t in hpFront.Cast<Transform>().Where(t=>t.name.StartsWith("[HP chest inlet ")))
                ports.Add(new Bounds(root.transform.InverseTransformPoint(t.position),new Vector3(.16f,.25f,.25f)));
            foreach(var t in lpRear.Cast<Transform>().Where(t=>t.name.StartsWith("[LP exhaust takeoff ")))
                ports.Add(new Bounds(root.transform.InverseTransformPoint(t.position),new Vector3(.22f,.22f,.22f)));
            var inletMount=BoundsOf(lpBody.Find("Smokebox inlet flange").GetComponent<MeshFilter>(),root.transform);inletMount.Expand(.012f);ports.Add(inletMount);
            foreach(var seat in lpBody.Cast<Transform>().Where(t=>t.name.StartsWith("[Frame hanger seat ")))
                ports.Add(new Bounds(root.transform.InverseTransformPoint(seat.position),new Vector3(.10f,.06f,.06f)));
            var collisionPipes=pipes.Where(f=>!f.name.StartsWith("Roof saddle ")&&!f.name.StartsWith("Roof strap ")).ToArray();
            bool Shown(MeshFilter f)=>f.GetComponent<Renderer>()!=null&&f.GetComponent<Renderer>().enabled&&f.GetComponentsInParent<Transform>(true).Where(t=>t!=root.transform).All(t=>t.gameObject.activeSelf);
            var targets=root.GetComponentsInChildren<MeshFilter>(true).Where(f=>!pipes.Contains(f)&&f.sharedMesh!=null&&Shown(f)).ToArray();
            var underframe=new MeyerRearUnderframeAudit(root.transform,rear);
            bool repairs=underframe.RepairContract(lpBody,output);
            if(Environment.GetEnvironmentVariable("S060_REPAIR_BASELINE")!="1")Check(repairs,"Continuous native frame/plate joints and low exhaust return; see meyer-repair-contract.json.");
            if(fittingReturn==null&&Environment.GetEnvironmentVariable("S060_REPAIR_BASELINE")!="1")
            {
                // All collision, walking and item layers use this same factory.
                var collision=(Transform)bodywork.GetType().GetMethod("Group",All).Invoke(bodywork,new object[]{root.transform,"Repair plate collider QA",0,null});
                var plateBox=collision.Find("Rear running plate").GetComponent<BoxCollider>();
                Check(Mathf.Abs(plateBox.center.z+plateBox.size.z*.5f+3.92f)<.0001f,"Running plate collision reaches the repaired native frame joint.");
                Object.DestroyImmediate(collision.gameObject);
            }
            Check(Mathf.Abs((float)lp.GetType().GetField("HeaderY",All).GetRawConstantValue()-1.40f)<.00001f,"Collector lowered 40 mm; clears native chest covers.");
            Check(Mathf.Abs(((Vector3)lp.GetType().GetField("BodyBallPosition",All).GetValue(null)).y-1.32f)<.00001f,"Under-cab flexible return dropped 120 mm at body union.");
            var entries=CreateEntries(root.transform,native);
            var frontHome=front.parent.localPosition;var rearHome=rear.parent.localPosition;
            File.WriteAllLines(Path.Combine(output,"rear-native-bounds.txt"), new[]{"rear frame car position: "+root.transform.InverseTransformPoint(rear.position).ToString("F5")}.Concat(rear.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null).Select(f=>f.name+"; shown="+Shown(f)+"; rear="+BoundsOf(f,rear)+"; car="+BoundsOf(f,root.transform))));
            Render(root,"rear-chassis-right",new Vector3(8,2.6f,-6.1f),new Vector3(0,1.35f,-6.1f),1.5f);
            Render(root,"rear-chassis-left",new Vector3(-8,2.6f,-6.1f),new Vector3(0,1.35f,-6.1f),1.5f);
            Render(root,"smooth-steam-left",new Vector3(-13,4.3f,-2.2f),new Vector3(0,2.2f,-1.9f),4.5f);
            Render(root,"smooth-steam-right",new Vector3(13,4.3f,-2.2f),new Vector3(0,2.2f,-1.9f),4.5f);
            Render(root,"smooth-steam-roof",new Vector3(-8,7,-6),new Vector3(-.5f,2.8f,-2.0f),3.3f);
            Render(root,"native-front-valve",new Vector3(-3,1.9f,6),new Vector3(-.35f,1.0f,4.05f),.7f);
            Render(root,"native-rear-valve",new Vector3(3,1.7f,-10),new Vector3(.35f,1.0f,-8.05f),.7f);
            detailAudit.HeatPreview(true);
            Render(root,"rear-native-brakes-heated",new Vector3(8,2.0f,-6.1f),new Vector3(0,1.0f,-6.1f),1.45f);
            Render(root,"front-native-brakes-heated",new Vector3(-8,2.0f,.9f),new Vector3(0,1.0f,.9f),1.45f);
            detailAudit.HeatPreview(false);
            var contacts=new List<string>();
            foreach(float fy in new[]{-6f,0,6f})foreach(float ry in new[]{-6f,0,6f})foreach(int heave in new[]{-1,0,1})
            {
                front.parent.localPosition=frontHome+Vector3.up*(-heave*.02f);rear.parent.localPosition=rearHome+Vector3.up*(heave*.02f);
                front.parent.localRotation=Quaternion.Euler(heave,fy,-heave);rear.parent.localRotation=Quaternion.Euler(-heave,ry,heave);update.Invoke(pipeOwner,null);fittingReturn?.UpdateGeometry();poses++;
                checks+=underframe.ValidatePose(pipes);
                checks+=detailAudit.Pose(lpBody.Find("Rear articulated bend").GetComponent<MeshFilter>());
                foreach(var low in new[]{lpBody,lpRear}.SelectMany(t=>t.GetComponentsInChildren<MeshFilter>()))
                    foreach(var high in new[]{hpBody,hpFront}.SelectMany(t=>t.GetComponentsInChildren<MeshFilter>()))
                    {
                        var region=BoundsOf(low,root.transform);
                        if(!region.Intersects(BoundsOf(high,root.transform)))continue;
                        Check(!new SteamMeshContact.Shell(low,root.transform,region).Intersects(new SteamMeshContact.Shell(high,root.transform,region),out var pipeHit),
                            "Separate HP/LP pipe clearance: "+pipeHit);
                    }
                // Moving ports follow the castings, not their straight-track positions.
                for(int j=2;j<4;j++){int side=j==2?-1:1;var t=(Transform)hp.GetType().GetMethod("FrontAdmission",All).Invoke(hp,new object[]{side});var box=ports[j];box.center=root.transform.InverseTransformPoint(t.position);ports[j]=box;}
                int rearPort=4;foreach(var t in lpRear.Cast<Transform>().Where(t=>t.name.StartsWith("[LP exhaust takeoff "))){var box=ports[rearPort];box.center=root.transform.InverseTransformPoint(t.position);ports[rearPort++]=box;}
                foreach(var pipe in collisionPipes)
                {
                    Check(ClearEntry(pipe,root.transform,entries,out var entryHit),"Cab-entry clearance: "+entryHit);
                    var region=BoundsOf(pipe,root.transform);var shell=new SteamMeshContact.Shell(pipe,root.transform,region);
                    foreach(var target in targets)
                    {
                        if(poses>1&&!pipe.name.Contains("articulated bend")&&!pipe.transform.IsChildOf(front)&&!pipe.transform.IsChildOf(rear)&&!target.transform.IsChildOf(front)&&!target.transform.IsChildOf(rear))continue;
                        var targetBounds=BoundsOf(target,root.transform);if(!targetBounds.Intersects(region))continue;
                        var carved=OutsidePorts(target,root.transform,region);if(carved==null)continue;
                        try
                        {
                            if(shell.Intersects(new SteamMeshContact.Shell(carved,root.transform,region),out var hit)&&contacts.Count<30)
                            {
                                contacts.Add("yaw "+fy+"/"+ry+",heave "+heave+"; "+hit);
                                if(fittingReturn!=null)
                                {
                                    File.WriteAllLines(Path.Combine(output,"contacts.txt"),contacts);
                                    throw new InvalidOperationException("Source pipe fitting contact; see contacts.txt.");
                                }
                            }
                        }
                        finally{Object.DestroyImmediate(carved.sharedMesh);Object.DestroyImmediate(carved.gameObject);}
                    }
                    Check(pipe.sharedMesh.vertices.All(p=>S060PipePose.Finite(p)),"Finite pipe mesh.");
                }
                foreach(int side in new[]{-1,1})
                {
                    var end=(Transform)hp.GetType().GetMethod("FrontAdmission",All).Invoke(hp,new object[]{side});
                    var start=(Vector3)hp.GetType().GetMethod("AdmissionSocket",All).Invoke(hp,new object[]{side});
                    var bend=hpBody.Find("HP articulated bend "+side).GetComponent<MeshFilter>();
                    Check(Vector3.Distance(Cap(bend,false,root.transform),start)<.0003f && Vector3.Distance(Cap(bend,true,root.transform),root.transform.InverseTransformPoint(end.position))<.0003f,"Both HP bend endpoints remain connected.");
                }
                var rearBend=lpBody.Find("Rear articulated bend").GetComponent<MeshFilter>();
                Check(Vector3.Distance(Cap(rearBend,false,root.transform),root.transform.InverseTransformPoint(Group(lp,"RearJoint").position))<.0003f && Vector3.Distance(Cap(rearBend,true,root.transform),root.transform.InverseTransformPoint(Group(lp,"BodyJoint").position))<.0003f,"Rear bend endpoints remain connected.");
                foreach(var f in collisionPipes.Where(f=>f.name.Contains("articulated bend")))
                {
                    var v=f.sharedMesh.vertices;float radius=f.name.StartsWith("HP")?.085f:.095f;float error=0;
                    for(int ring=0;ring<25;ring++){var centre=Vector3.zero;for(int s=0;s<24;s++)centre+=v[ring*24+s]/24;for(int s=0;s<24;s++)error=Mathf.Max(error,Mathf.Abs(Vector3.Distance(v[ring*24+s],centre)-radius));}
                    Check(error<.00001f,"Articulation preserves actual circular pipe radius.");
                }
            }
            underframe.Finish(output);
            detailAudit.Finish(output);
            File.WriteAllLines(Path.Combine(output,"contacts.txt"),contacts);Check(contacts.Count==0,"Pipe/body contact; see contacts.txt.");
            front.parent.localPosition=frontHome;rear.parent.localPosition=rearHome;
            front.parent.localRotation=rear.parent.localRotation=Quaternion.identity;update.Invoke(pipeOwner,null);fittingReturn?.UpdateGeometry();
            rear.parent.localRotation=Quaternion.Euler(-1,6,1);rear.parent.localPosition=rearHome+Vector3.up*.02f;update.Invoke(pipeOwner,null);fittingReturn?.UpdateGeometry();
            Render(root,"rear-chassis-articulated",new Vector3(7,3.6f,-3.7f),new Vector3(0,1.45f,-6.0f),1.7f);
            Render(root,"under-cab-return",new Vector3(5,1.7f,-3.8f),new Vector3(.55f,1.45f,-4.45f),1.3f);
            var live=(MeshRenderer)hp.GetType().GetField("materialSource",All).GetValue(hp);var originalMaterial=live.sharedMaterial;var changedMaterial=new Material(originalMaterial);native.Add(changedMaterial);
            live.sharedMaterial=changedMaterial;update.Invoke(pipeOwner,null);fittingReturn?.UpdateGeometry();Check(pipes.All(f=>f.GetComponent<MeshRenderer>().sharedMaterial==changedMaterial),"Every pipe follows the current native livery material.");
            live.sharedMaterial=originalMaterial;update.Invoke(pipeOwner,null);fittingReturn?.UpdateGeometry();
            pipeOwner.Dispose();owners.Remove(pipeOwner);foreach(var h in rendererHomes)Check(h.Key.enabled==h.Value,"Original renderer visibility restored.");foreach(var h in source)Check(h.Key.sharedMesh==h.Value,"Exact original mesh identity restored.");
            for(int i=owners.Count-1;i>=0;i--)owners[i]?.Dispose();owners.Clear();detailAudit.CheckRestored();Check(underframe.Removed,"Added fixed and swivelling underframes removed on restoration.");Object.DestroyImmediate(root);root=null;
            foreach(var o in native)if(o!=null)Object.DestroyImmediate(o);native.Clear();bundle.Unload(true);bundle=null;
            typeof(InwardPipeRuntimeAudit).GetMethod("Validate",All).Invoke(null,new object[]{runtime,Environment.GetEnvironmentVariable("S060_DOCKSIDE_AUDIT_BUNDLE"),false});
            File.WriteAllText(Path.Combine(output,"smooth-steam-validation.json"),JsonUtility.ToJson(new Report{passed=true,checks=checks,poses=poses,runtimeSha256=Hash(Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME")),singleRearTrunk=true,sourceRoutePreview=fittingReturn!=null,inwardMeyerConnected=true,docksideLegacyUnchanged=true,cabEntryClear=true,liveMaterialsAndCleanup=true,noPipeBodyContacts=true,hpDiameter=.170f,frontValveEmbed=originalCoupling.frontEmbed,rearValveEmbed=originalCoupling.rearEmbed},true));
            EditorApplication.Exit(0);
        }
        catch(Exception ex){File.WriteAllText(Path.Combine(output,"error.txt"),ex.ToString());Debug.LogException(ex);EditorApplication.Exit(1);}
        finally{for(int i=owners.Count-1;i>=0;i--)owners[i]?.Dispose();if(root!=null)Object.DestroyImmediate(root);foreach(var o in native)if(o!=null)Object.DestroyImmediate(o);if(bundle!=null)bundle.Unload(true);AppDomain.CurrentDomain.AssemblyResolve-=resolver;}
    }
    private static MeshFilter[] CreateEntries(Transform body, List<Object> owned)
    {
        var result = new List<MeshFilter>();
        foreach (int side in new[] { -1, 1 })
        {
            var b = new Bounds(new Vector3(side * 1.63f, 2.55f, -2.7f), new Vector3(.5f, 1.8f, 3.2f));
            var a = b.min; var z = b.max;
            var mesh = new Mesh { name = "Unchanged cab entry QA volume", vertices = new[] {
                new Vector3(a.x,a.y,a.z), new Vector3(z.x,a.y,a.z), new Vector3(z.x,z.y,a.z), new Vector3(a.x,z.y,a.z),
                new Vector3(a.x,a.y,z.z), new Vector3(z.x,a.y,z.z), new Vector3(z.x,z.y,z.z), new Vector3(a.x,z.y,z.z) },
                triangles = new[] { 0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,3,7,6,3,6,2,0,4,7,0,7,3,1,2,6,1,6,5 } };
            owned.Add(mesh); var go = new GameObject("Unchanged cab entry QA " + side); owned.Add(go); go.transform.SetParent(body, false);
            var filter = go.AddComponent<MeshFilter>(); filter.sharedMesh = mesh; result.Add(filter);
        }
        return result.ToArray();
    }
    private static bool ClearEntry(MeshFilter f, Transform body, MeshFilter[] entries, out string hit)
    {
        hit = null;
        var v = f.sharedMesh.vertices.Select(p => body.InverseTransformPoint(f.transform.TransformPoint(p))).ToArray();
        var shell = new SteamMeshContact.Shell(f, body);
        foreach (var entry in entries)
        {
            var target = new SteamMeshContact.Shell(entry, body);
            // An entirely contained small pipe may not cross a box face.
            foreach (int j in f.sharedMesh.triangles)
                if (target.Bounds.Contains(v[j])) { hit = entry.name + " contains actual pipe vertex " + v[j].ToString("F7"); return false; }
            if (shell.Intersects(target, out hit)) return false;
        }
        return true;
    }
    private static Vector3 Cap(MeshFilter f,bool end,Transform root){var v=f.sharedMesh.vertices;return root.InverseTransformPoint(f.transform.TransformPoint(v[v.Length-(end?1:2)]));}
    private static Bounds BoundsOf(MeshFilter f,Transform root){var v=f.sharedMesh.vertices;var used=f.sharedMesh.triangles.Distinct().ToArray();var b=new Bounds(root.InverseTransformPoint(f.transform.TransformPoint(v[used[0]])),Vector3.zero);foreach(int i in used)b.Encapsulate(root.InverseTransformPoint(f.transform.TransformPoint(v[i])));b.Expand(.00001f);return b;}
    private static List<Vector3> Clip(List<Vector3> polygon,int axis,float edge,bool below)
    {var p=new List<Vector3>();for(int i=0;i<polygon.Count;i++){var a=polygon[i];var b=polygon[(i+1)%polygon.Count];float da=a[axis]-edge,db=b[axis]-edge;if(below?da<=0:da>=0)p.Add(a);if((da<0)!=(db<0))p.Add(Vector3.LerpUnclamped(a,b,da/(da-db)));}return p;}
    private static MeshFilter OutsidePorts(MeshFilter f,Transform root,Bounds region)
    {
        var v=f.sharedMesh.vertices.Select(p=>root.InverseTransformPoint(f.transform.TransformPoint(p))).ToArray();var t=f.sharedMesh.triangles;var result=new List<Vector3>();
        for(int i=0;i<t.Length;i+=3)
        {
            var tri=new List<Vector3>{v[t[i]],v[t[i+1]],v[t[i+2]]};var b=new Bounds(tri[0],Vector3.zero);b.Encapsulate(tri[1]);b.Encapsulate(tri[2]);if(!b.Intersects(region))continue;
            var polygons=new List<List<Vector3>>{tri};
            foreach(var port in ports.Where(p=>p.Intersects(b)))
            {
                var next=new List<List<Vector3>>();foreach(var poly in polygons){var remainder=poly;for(int axis=0;axis<3&&remainder.Count>=3;axis++){var lo=Clip(remainder,axis,port.min[axis],true);if(lo.Count>=3)next.Add(lo);remainder=Clip(remainder,axis,port.min[axis],false);var hi=Clip(remainder,axis,port.max[axis],false);if(hi.Count>=3)next.Add(hi);remainder=Clip(remainder,axis,port.max[axis],true);}}polygons=next;
            }
            foreach(var poly in polygons)for(int j=1;j+1<poly.Count;j++)if(Vector3.Cross(poly[j]-poly[0],poly[j+1]-poly[0]).sqrMagnitude>1e-14f)result.AddRange(new[]{poly[0],poly[j],poly[j+1]});
        }
        if(result.Count==0)return null;var mesh=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,vertices=result.ToArray(),triangles=Enumerable.Range(0,result.Count).ToArray()};var go=new GameObject(f.name+" contact surface");go.transform.SetParent(root,false);var filter=go.AddComponent<MeshFilter>();filter.sharedMesh=mesh;return filter;
    }
    private static void Render(GameObject root,string name,Vector3 eye,Vector3 target,float size)
    {var type=typeof(ConventionalNativeAudit);type.GetField("output",All).SetValue(null,output);var fill=new GameObject("QA camera fill");fill.transform.SetParent(root.transform,false);fill.transform.rotation=Quaternion.LookRotation(target-eye);var light=fill.AddComponent<Light>();light.type=LightType.Directional;light.intensity=.65f;try{type.GetMethod("RenderWide",All,null,new[]{typeof(GameObject),typeof(string),typeof(Vector3),typeof(Vector3),typeof(float)},null).Invoke(null,new object[]{root,name,eye,target,size});}finally{Object.DestroyImmediate(fill);}}
    private static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);checks++;}
    private static string Hash(string path){using(var sha=SHA256.Create())using(var s=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(s)).Replace("-","");}
    [Serializable]private sealed class Report{public bool passed,singleRearTrunk,sourceRoutePreview,inwardMeyerConnected,docksideLegacyUnchanged,cabEntryClear,liveMaterialsAndCleanup,noPipeBodyContacts;public int checks,poses;public float hpDiameter,frontValveEmbed,rearValveEmbed;public string runtimeSha256;public bool gameScene=false;}
}
#endif
