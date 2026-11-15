using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

// Production Apply/Undo presentation boundary with exact native brake geometry.
// The absent axle/physics owner deliberately stops execution before simulation.
public static class FourCoupledBrakeNativeAudit
{
    const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
    static readonly Type Fixture=typeof(ConventionalNativeAudit);
    static int checks;
    static object F(string name,params object[] args)=>Fixture.GetMethod(name,All).Invoke(null,args);
    static void Field(string name,object value)=>Fixture.GetField(name,All).SetValue(null,value);
    static void Set(object owner,string name,object value)
    {
        for(var type=owner.GetType();type!=null;type=type.BaseType)
        {
            var property=type.GetProperty(name,All|BindingFlags.DeclaredOnly);
            if(property?.GetSetMethod(true)!=null){property.GetSetMethod(true).Invoke(owner,new[]{value});return;}
        }
        throw new InvalidOperationException("Fixture property missing: "+name);
    }
    static void Check(bool ok,string why){if(!ok)throw new InvalidOperationException(why);checks++;}
    public static void RunBatch()
    {
        string output=Environment.GetEnvironmentVariable("S060_040_BRAKE_OUTPUT"),dll=Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME");
        Directory.CreateDirectory(output);Field("output",output);
        AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>(Assembly)F("Resolve",s,e);
        GameObject root=null;
        try
        {
            var runtime=Assembly.LoadFrom(dll);Field("runtime",runtime);
            var dataType=Fixture.GetNestedType("Data",All);
            var data=JsonUtility.FromJson(File.ReadAllText(Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW")),dataType);
            var shoeData=JsonUtility.FromJson(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW")),"meyer-running-gear-native.json")),dataType);
            object Entry(object d,string name)=>((IEnumerable)dataType.GetField("meshes").GetValue(d)).Cast<object>().Single(x=>(string)x.GetType().GetField("name").GetValue(x)==name);
            var material=new Material(Shader.Find("Standard"));Field("material",material);
            root=new GameObject("Inactive 040 native brake boundary");root.SetActive(false);
            var body=root.transform;
            var shoes=(Transform)F("Draw",body,"s060_brake_shoes",F("Mesh",Entry(shoeData,"s060_brake_shoes"),null));
            var shoeFilter=shoes.GetComponent<MeshFilter>();var original=shoeFilter.sharedMesh;
            var calipers=new List<Renderer>();
            foreach(string lod in new[]{"LOD0","LOD1"})
            {
                var group=new GameObject(lod).transform;group.SetParent(body,false);
                foreach(string side in new[]{"l","r"})foreach(int index in new[]{1,2,3})
                {
                    float lo=index==1?2.16f:index==2?.6f:-.91f,hi=index==1?2.59f:index==2?1.31f:-.57f;
                    Func<Vector3,bool> keep=v=>v.x*(side=="l"?1:-1)>=.41f&&v.x*(side=="l"?1:-1)<=1f&&v.y<=.8215f&&v.z>=lo&&v.z<=hi;
                    calipers.Add(((Transform)F("Draw",group,"s060_caliper_"+side+"_"+index,F("Mesh",Entry(data,"s060_body"),keep))).GetComponent<Renderer>());
                }
            }
            var wheels=new List<Transform>();foreach(string side in new[]{"L","R"}){var w=new GameObject("middle driver "+side).transform;w.SetParent(body,false);wheels.Add(w);}
            var parts=Activator.CreateInstance(runtime.GetType("RearrangedS060.S060Parts"),true);Set(parts,"Body",body);
            ((IDictionary)parts.GetType().GetField("wheels",All).GetValue(parts)).Add(2,wheels);
            var owner=Activator.CreateInstance(runtime.GetType("RearrangedS060.S060FourCoupled"),true);Set(owner,"Parts",parts);
            bool baseline=Environment.GetEnvironmentVariable("S060_040_BRAKE_BASELINE")=="1";
            for(int repeat=0;repeat<2;repeat++)
            {
                try{owner.GetType().GetMethod("Apply",All).Invoke(owner,null);throw new Exception("Fixture must stop before physics.");}
                catch(TargetInvocationException e){Check(e.InnerException is InvalidOperationException&&e.InnerException.Message.StartsWith("No [axle] maps"),"Expected presentation-only axle boundary");}
                Check(calipers.Where(r=>r.name.EndsWith("_2")).All(r=>!r.enabled),"Both middle calipers are hidden at both LODs");
                Check(calipers.Where(r=>!r.name.EndsWith("_2")).All(r=>r.enabled),"Outer calipers remain visible");
                var fitted=shoeFilter.sharedMesh;var positions=original.vertices;
                bool Middle(int i)=>positions[i].z>=.6f&&positions[i].z<=1.31f;
                int[] expected=original.triangles.Where((v,i)=>!Enumerable.Range(i-i%3,3).All(j=>Middle(original.triangles[j]))).ToArray();
                Check(baseline?fitted.triangles.Any(Middle):!fitted.triangles.Any(Middle),baseline?"Baseline reproduces floating middle shoes":"No middle shoe triangles remain");
                if(!baseline)
                {
                    Check(fitted!=original&&fitted.triangles.SequenceEqual(expected),"Only the exact two middle shoes are removed");
                    Check(fitted.vertices.SequenceEqual(original.vertices)&&fitted.normals.SequenceEqual(original.normals)&&fitted.uv.SequenceEqual(original.uv),"Retained native positions, normals and UVs are exact");
                    Check(shoes.GetComponent<Renderer>().enabled&&shoes.GetComponent<Renderer>().sharedMaterial==material,"Native shoe renderer and material binding are retained");
                }
                try{owner.GetType().GetMethod("Undo",All).Invoke(owner,null);throw new Exception("Fixture has no native physics owner.");}
                catch(TargetInvocationException e){Check(e.InnerException is NullReferenceException,"Expected absent-physics fixture boundary");}
                Check(shoeFilter.sharedMesh==original&&original!=null,"Leaving 040 restores the exact native six-shoe mesh");
                Check(calipers.All(r=>r.enabled)&&wheels.All(w=>w.gameObject.activeSelf),"Leaving 040 restores all native calipers and middle wheels");
                if(!baseline)Check(fitted==null,"Owned brake mesh is released after restoration");
            }
            File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"checks\":"+checks+",\"baselineReproduced\":"+baseline.ToString().ToLowerInvariant()+",\"runtimeSha256\":\""+F("Hash",dll)+"\",\"nativeGeometry\":true,\"productionApplyUndo\":true,\"simulationBoundaryStubbed\":true,\"gameplayValidated\":false}");
            EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Path.Combine(output,"error.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
        finally{if(root!=null)Object.DestroyImmediate(root);}
    }
}
