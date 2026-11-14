using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Bounded local fit check, reusing the existing exact-export geometry fixture.
public static class FourCoupledAirEquipmentAudit
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    static Type fixture = typeof(ConventionalNativeAudit);
    static object F(string name, params object[] args) => fixture.GetMethod(name, All).Invoke(null, args);
    static int checks;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
    static void Set(object o, string n, object v) => o.GetType().GetProperty(n, All).GetSetMethod(true).Invoke(o, new[]{v});
    static void Field(string n, object v) => fixture.GetField(n, All).SetValue(null, v);
    static object Read(string path) => JsonUtility.FromJson(File.ReadAllText(path), fixture.GetNestedType("Data", All));
    static object[] Items(object data, string field) => ((IEnumerable)data.GetType().GetField(field).GetValue(data)).Cast<object>().ToArray();
    static string Name(object o) => (string)o.GetType().GetField("name").GetValue(o);
    public static void RunBatch()
    {
        string output = Environment.GetEnvironmentVariable("S060_CONVENTIONAL_OUTPUT");
        Directory.CreateDirectory(output); Field("output",output);
        AppDomain.CurrentDomain.AssemblyResolve += (s,e) => (Assembly)F("Resolve",s,e);
        try
        {
            string dll=Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME");
            var runtime=Assembly.LoadFrom(dll); Field("runtime",runtime);
            var data=Read(Environment.GetEnvironmentVariable("S060_CONVENTIONAL_NATIVE"));
            var bodyData=Items(Read(Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW")),"meshes").Single(e=>Name(e)=="s060_body");
            var texture=new Texture2D(2,2); texture.LoadImage(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW")),"native-preview-atlas.png")));
            var material=new Material(Shader.Find("Standard"));material.mainTexture=texture;Field("material",material);
            foreach(float scale in new[]{1f,.55f,1.15f})
            {
                var root=new GameObject("0-4-0 native fit preview");var body=root.transform;
                foreach(var node in Items(data,"roots")) F("Tree",body,node,data);
                // resources.assets confirms these three GameObjects are inactive.
                // The legacy JSON fixture did not export m_IsActive.
                foreach(var node in body.GetComponentsInChildren<Transform>(true).Where(t=>new[]{"s060_mech_reach_rod_UNUSED","s060_mech_05_UNUSED_LUBRICATOR","s060_mech_06_UNUSED_LUBRICATOR"}.Contains(t.name)))node.gameObject.SetActive(false);
                // Same brake mask as the proven conventional fixture, limited to
                // the removed middle axle rather than the two retained brakes.
                Func<Vector3,bool> keep = v=>!(Mathf.Abs(v.x)>=.41f&&Mathf.Abs(v.x)<=1f&&v.y<=.8215f&&v.z>=.6f&&v.z<=1.31f);
                F("Draw",body,"s060_body",F("Mesh",bodyData,keep));
                var bodyFilter=body.GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="s060_body");var bodyHome=bodyFilter.sharedMesh;
                var parts=Activator.CreateInstance(runtime.GetType("RearrangedS060.S060Parts"),true);Set(parts,"Body",body);
                var left=body.Find("DrivingMechanism_L");var right=body.Find("DrivingMechanism_R");Set(parts,"MechanismLeft",left);Set(parts,"MechanismRight",right);
                foreach(var side in new[]{left,right})
                {
                    side.Find("s060_Wheels_02").gameObject.SetActive(false);
                    side.localScale=new Vector3(side.localScale.x,scale,scale);
                }
                var rods=(IDisposable)runtime.GetType("RearrangedS060.S060CouplingRods").GetMethod("Apply",All).Invoke(null,new[]{parts});
                var equipmentType=runtime.GetType("RearrangedS060.S060FourCoupledAirEquipment");
                var owner=(IDisposable)equipmentType.GetMethod("Bind",All).Invoke(null,new object[]{body,scale});
                var layout=FitBody(runtime,parts,body,scale);
                Check(bodyFilter.sharedMesh!=bodyHome&&bodyFilter.sharedMesh.triangles.Length<bodyHome.triangles.Length,"Requested fixed curved gear castings must be removed.");
                Check(body.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="s060_mech_03"||r.name=="s060_mech_03_LOD1").All(r=>r.enabled),"Working animated reverser and its LOD must stay visible.");
                var equipment=(Transform)equipmentType.GetProperty("Root",All).GetValue(owner,null);
                var filter=equipment.GetComponent<MeshFilter>();var mesh=filter.sharedMesh;
                Check(equipment.parent==body,"Equipment must be rigid body mounted.");
                Check(equipment.GetComponentsInChildren<Renderer>().Length==1,"Equipment must use one draw.");
                var shell=new SteamMeshContact.Shell(filter,body);
                if(Environment.GetEnvironmentVariable("S060_040_PRESENTATION_ONLY")!="1")foreach(var side in new[]{left,right})
                {
                    var wheels=new[]{side.Find("s060_Wheels_01"),side.Find("s060_Wheels_03")};
                    var home=wheels.Select(w=>w.localRotation).ToArray();var rod=side.Find("s060_mech_wheels_connect");var rodHome=rod.localPosition;
                    var initial=wheels[0].TransformPoint(new Vector3(.98730177f,-.335356f,0));
                    for(int phase=0;phase<360;phase+=15)
                    {
                        for(int i=0;i<wheels.Length;i++)wheels[i].localRotation=home[i]*Quaternion.Euler(phase,0,0);
                        rod.localPosition=rodHome+side.InverseTransformVector(wheels[0].TransformPoint(new Vector3(.98730177f,-.335356f,0))-initial);
                        foreach(var target in side.GetComponentsInChildren<MeshFilter>().Where(f=>!f.name.Contains("LOD")))
                        {
                            string contact;
                            Check(!shell.Intersects(new SteamMeshContact.Shell(target,body),out contact),"Scale "+scale+" phase "+phase+" intersects "+target.name+": "+contact);
                        }
                    }
                    for(int i=0;i<wheels.Length;i++)wheels[i].localRotation=home[i];rod.localPosition=rodHome;
                }
                if(scale==1f)
                {
                    F("RenderWide",root,"040-right",new Vector3(7,1.0f,.15f),new Vector3(0,1.0f,.15f),1.05f);
                    F("RenderWide",root,"040-left",new Vector3(-7,1.0f,.15f),new Vector3(0,1.0f,.15f),1.05f);
                    F("RenderWide",root,"040-detail",new Vector3(4,1.6f,2.1f),new Vector3(.65f,.85f,.15f),.68f);
                }
                var renderer=equipment.GetComponent<MeshRenderer>();var alternate=new Material(material);
                var source=body.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="s060_body");source.sharedMaterial=alternate;
                equipmentType.GetMethod("LateUpdate",All).Invoke(owner,null);Check(renderer.sharedMaterial==alternate,"Finish must follow the native material.");
                owner.Dispose();owner.Dispose();Check(equipment==null&&mesh==null,"Leaving the layout must release root and mesh idempotently.");
                layout.GetType().GetMethod("Restore",All).Invoke(layout,null);
                Check(bodyFilter.sharedMesh==bodyHome,"Leaving 040 must restore the exact native body mesh.");
                Check(body.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="s060_mech_03"||r.name=="s060_mech_03_LOD1").All(r=>r.enabled),"Leaving 040 must restore the link renderers.");
                owner=(IDisposable)equipmentType.GetMethod("Bind",All).Invoke(null,new object[]{body,scale});
                layout=FitBody(runtime,parts,body,scale);
                equipment=(Transform)equipmentType.GetProperty("Root",All).GetValue(owner,null);mesh=equipment.GetComponent<MeshFilter>().sharedMesh;
                // Late-loaded production MonoBehaviour messages are not driven
                // by this EditMode fixture. Exercise the callback explicitly.
                equipmentType.GetMethod("OnDestroy",All).Invoke(owner,null);
                layout.GetType().GetMethod("Restore",All).Invoke(layout,null);
                Check(bodyFilter.sharedMesh==bodyHome,"Destruction callback must restore the exact native body mesh.");
                rods.Dispose();Object.DestroyImmediate(root);Check(mesh==null,"Destruction callback must release its generated mesh.");
                Object.DestroyImmediate(alternate);
            }
            bool presentationOnly=Environment.GetEnvironmentVariable("S060_040_PRESENTATION_ONLY")=="1";
            File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"checks\":"+checks+",\"runtimeSha256\":\""+F("Hash",dll)+"\",\"nativeWheelAndRodFit\":"+(!presentationOnly).ToString().ToLowerInvariant()+",\"presentationAndRestoration\":true,\"workingReverserVisible\":true,\"gameplayTested\":false}");
            EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Path.Combine(output,"error.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }

    // Match Convert/ResetRunningGear's actual body-mesh owner order: air parts,
    // translated cylinder pipes, fixed gear; then air disposal and Scale.Restore.
    // The old standalone air test omitted the pipe copy made at non-stock sizes.
    static object FitBody(Assembly runtime,object parts,Transform body,float scale)
    {
        var layout=Activator.CreateInstance(runtime.GetType("RearrangedS060.S060DriverScale"),All,null,new[]{parts},null);
        if(!Mathf.Approximately(scale,1f))
        {
            var delta=(Vector3)runtime.GetType("RearrangedS060.S060PipeGeometry").GetMethod("CylinderOffset",All).Invoke(null,new object[]{scale,0f});
            var pipes=runtime.GetType("RearrangedS060.S060BodyPipes").GetMethod("Apply",All).Invoke(null,new object[]{body,delta,false,true});
            var fittings=Activator.CreateInstance(runtime.GetType("RearrangedS060.S060CylinderFittings"),true);
            fittings.GetType().GetField("pipes",All).SetValue(fittings,pipes);
            layout.GetType().GetField("fittings",All).SetValue(layout,fittings);
        }
        layout.GetType().GetMethod("RetireFixedValveGear",All).Invoke(layout,null);
        return layout;
    }
}
