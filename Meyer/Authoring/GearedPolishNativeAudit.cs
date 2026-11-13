using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using CCL.Types;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Focused geometry fixture. Never starts DV or constructs a TrainCar.
public static class GearedPolishNativeAudit
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static int checks;
    private static void Check(bool value,string message) { checks++;if(!value)throw new InvalidOperationException(message); }
    private static string Hash(string path) {using(var sha=SHA256.Create())using(var s=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(s)).Replace("-","");}
    public static void RunBatch()
    {
        string output = Environment.GetEnvironmentVariable("S060_GEARED_POLISH_OUTPUT");
        Directory.CreateDirectory(output);
        var owned = new List<Object>(); AssetBundle bundle = null; GameObject clone = null;
        try
        {
            bundle = AssetBundle.LoadFromFile(Environment.GetEnvironmentVariable("S060_DOCKSIDE_AUDIT_BUNDLE"));
            clone = Object.Instantiate(bundle.LoadAllAssets<CustomCarPack>().Single().Cars[0].liveries[0].prefab);
            SteamNativePreview.Apply(clone, owned);
            var assembly=Assembly.LoadFrom(Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME"));
            var owners=new List<IDisposable>();
            var originals=clone.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null&&f.sharedMesh.name.StartsWith("s060_Wheels_01")).ToDictionary(f=>f,f=>f.sharedMesh);
            var units=clone.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="bogie_car").ToArray();
            foreach(var unit in units)
                owners.Add((IDisposable)assembly.GetType("RearrangedS060.S060MeyerGearMounts",true).GetMethod("BindWheelCranks",All).Invoke(null,new object[]{unit}));
            owners.Add((IDisposable)assembly.GetType("RearrangedS060.S060FixedValveGear",true).GetMethod("Apply",All).Invoke(null,new object[]{clone.transform}));
            Check(originals.Count==4,"Both native01 wheels on both units required.");
            foreach(var pair in originals)
            {
                var copy=pair.Key.sharedMesh;Check(copy!=pair.Value&&copy.triangles.Length<pair.Value.triangles.Length,"Original return cranks were not removed.");
                Check(copy.vertices.SequenceEqual(pair.Value.vertices)&&copy.uv.SequenceEqual(pair.Value.uv)&&copy.normals.SequenceEqual(pair.Value.normals),"Wheel profile, journals and UVs must remain exact.");
            }
            foreach(var unit in units)
            {
                var clip=unit.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single();
                for(int phase=0;phase<=32;phase++)
                {
                    clip.SampleAnimation(unit.gameObject,phase/32f);
                    foreach(string side in new[]{"L","R"})
                    {
                        var casing=unit.Find("Franklin fit "+side+"/transmission_box/default").GetComponent<MeshFilter>();
                        var mesh=casing.sharedMesh;Check(mesh.isReadable,"Reopened gearbox must have actual readable vertices.");
                        var b=new Bounds(mesh.vertices[0],Vector3.zero);foreach(var p in mesh.vertices)b.Encapsulate(p);
                        Check(b.size.sqrMagnitude>.01f&&(b.size-mesh.bounds.size).sqrMagnitude<1e-10f,"Cached bounds conceal missing gearbox geometry.");
                        var box=new SteamMeshContact.Shell(casing,unit);
                        foreach(var name in new[]{"Wheel crank","Hub sleeve"})
                            Check(!box.Intersects(new SteamMeshContact.Shell(unit.Find("Franklin return crank "+side+"/"+name).GetComponent<MeshFilter>(),unit)),"Restored gearbox clips authored crank: "+side+" "+phase);
                        foreach(var name in new[]{"Main rod","Coupling rod"})
                            Check(!box.Intersects(new SteamMeshContact.Shell(unit.Find("[Dockside rods "+side+"]/"+name).GetComponent<MeshFilter>(),unit)),"Restored gearbox clips authored rods: "+side+" "+phase);
                    }
                }
                clip.SampleAnimation(unit.gameObject,0);
            }
            typeof(ConventionalNativeAudit).GetField("output", All).SetValue(null, output);
            var render = typeof(ConventionalNativeAudit).GetMethod("RenderWide", All);
            render.Invoke(null, new object[] {clone,"dockside-right",new Vector3(5,1.1f,1.05f),new Vector3(0,.7f,1.05f),.75f});
            render.Invoke(null, new object[] {clone,"dockside-left",new Vector3(-5,1.1f,1.05f),new Vector3(0,.7f,1.05f),.75f});
            var boxes = clone.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.transform.parent!=null&&f.transform.parent.name=="transmission_box").ToArray();
            File.WriteAllText(Path.Combine(output,"gearboxes.txt"),string.Join("\n",boxes.Select(f=>f.name+" "+f.sharedMesh.name+" "+f.sharedMesh.vertexCount+" "+f.GetComponent<MeshRenderer>().enabled)));
            foreach(var owner in owners)owner.Dispose();
            Check(originals.All(p=>p.Key.sharedMesh==p.Value),"Stock wheel mesh restoration must be exact.");
            File.WriteAllText(Path.Combine(output,"geared-polish-validation.json"),"{\"passed\":true,\"checks\":"+checks+",\"runtimeSha256\":\""+Hash(Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME"))+"\",\"bundleSha256\":\""+Hash(Environment.GetEnvironmentVariable("S060_DOCKSIDE_AUDIT_BUNDLE"))+"\",\"phasesPerUnit\":33,\"gearboxes\":4,\"stockRestoration\":true,\"gameplayValidated\":false}");
            EditorApplication.Exit(0);
        }
        catch(Exception ex) {File.WriteAllText(Path.Combine(output,"error.txt"),ex.ToString());Debug.LogException(ex);EditorApplication.Exit(1);}
        finally {if(clone!=null)Object.DestroyImmediate(clone);foreach(var o in owned)if(o!=null)Object.DestroyImmediate(o);if(bundle!=null)bundle.Unload(true);}
    }
}
