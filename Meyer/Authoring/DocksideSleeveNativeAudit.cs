#if UNITY_EDITOR
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

// Actual candidate DLL, native S060 neck triangles and the final CCL frame.
public static class DocksideSleeveNativeAudit
{
    private const BindingFlags Flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static int checks;
    private static void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
    // Independent triangle cuts and point-to-segment distances, rather than
    // the production profile sampler or a minimum clearance/envelope test.
    private static Vector3[] Section(Vector3[] vertices,int[] triangles,float y)
    {
        var result=new List<Vector3>();
        for(int t=0;t<triangles.Length;t+=3)
        {
            var cut=new List<Vector3>();
            for(int e=0;e<3;e++)
            {
                var a=vertices[triangles[t+e]];var b=vertices[triangles[t+(e+1)%3]];
                if((a.y>=y)!=(b.y>=y))cut.Add(Vector3.LerpUnclamped(a,b,(y-a.y)/(b.y-a.y)));
            }
            if(cut.Count==2)result.AddRange(cut);
        }
        return result.ToArray();
    }
    private static float Distance(Vector3 p,Vector3[] segments)
    {
        float best=float.PositiveInfinity;
        for(int i=0;i+1<segments.Length;i+=2)
        {
            var a=segments[i];var b=segments[i+1];var d=b-a;
            var nearest=a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude);best=Mathf.Min(best,Vector3.Distance(p,nearest));
        }
        return best;
    }
    public static void RunBatch()
    {
        string output = Environment.GetEnvironmentVariable("S060_DOCKSIDE_SLEEVE_OUTPUT"), runtime = Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME");
        Directory.CreateDirectory(output);
        string managed = Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED");
        ResolveEventHandler resolver = (sender, args) =>
        {
            string name = new AssemblyName(args.Name).Name;
            var loaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name); if (loaded != null) return loaded;
            foreach (string directory in new[] { managed, Path.Combine(managed, "UnityModManager"), Path.Combine(Environment.GetEnvironmentVariable("S060_AUDIT_MODS"), "LocoMeshSplitter") })
            { string path = Path.Combine(directory, name + ".dll"); if (File.Exists(path)) return Assembly.LoadFrom(path); }
            return null;
        };
        AppDomain.CurrentDomain.AssemblyResolve += resolver;
        var owned = new List<Object>(); GameObject car = null; AssetBundle bundle = null; IDisposable owner = null;
        try
        {
            var assembly = Assembly.LoadFrom(runtime); var type = assembly.GetType("RearrangedS060.S060SteamPipes", true);
            bundle = AssetBundle.LoadFromFile(Environment.GetEnvironmentVariable("S060_DOCKSIDE_AUDIT_BUNDLE"));
            car = Object.Instantiate(bundle.LoadAllAssets<CustomCarPack>().Single().Cars[0].liveries[0].prefab);
            var front = car.transform.Find("BogieF/bogie_car"); var rear = car.transform.Find("BogieR/bogie_car");
            var frontHome = car.transform.InverseTransformPoint(front.position); var rearHome = car.transform.InverseTransformPoint(rear.position);
            var native = SteamNativePreview.CreatePipeFixture(car.transform, owned); var originalBody = native.GetComponent<MeshFilter>().sharedMesh;
            var bind = type.GetMethod("Bind", Flags); var update = type.GetMethod("UpdateGeometry", Flags);
            owner = (IDisposable)bind.Invoke(null, new object[] { car.transform, front, rear, native });
            var sleeves = car.GetComponentsInChildren<MeshFilter>(true).Where(f => f.name.StartsWith("[Dockside smokebox sleeve ", StringComparison.Ordinal)).ToArray();
            Check(sleeves.Length == 2, "Both fitted smokebox sleeves required.");
            var sleeveMeshes = sleeves.Select(f => f.sharedMesh).ToArray();
            foreach (var sleeve in sleeves)
            {
                Check(sleeve.transform.parent == car.transform, "Sleeve follows rigid native body, not a swivelling bogie.");
                Check(sleeve.GetComponent<MeshRenderer>().sharedMaterial.name == "S060 Shay dirty gunmetal", "Same proven Shay gunmetal finish required.");
                var mesh = sleeve.sharedMesh; var v = mesh.vertices;
                Check(v.Length % 18 == 0, "Fitted hollow seven-course cuff topology required; oversized circular cup rejected.");
                Check(mesh.normals.Length == v.Length && mesh.tangents.Length == v.Length && mesh.uv.Length == v.Length, "Complete smooth-wall attributes required.");
                int stride = v.Length/18, sides = stride-1; var indices = mesh.triangles;
                int start = indices.Length-sides*12;
                for(int t=start;t<indices.Length;t+=3)
                {
                    var cross=Vector3.Cross(v[indices[t+1]]-v[indices[t]],v[indices[t+2]]-v[indices[t]]);
                    Check(t<start+sides*6 ? cross.y>0 : cross.y<0,"Both annular lips must have outward winding.");
                }
            }
            // Existing Shay output must remain byte-equivalent at default dimensions.
            var sleeveType = assembly.GetType("RearrangedS060.S060SteamSleeve", true);
            var oldHollow = assembly.GetType("RearrangedS060.S060ShayPlumbing", true).GetMethod("Hollow", Flags);
            var socket = new Vector3(.7052183f, 2.1f, 3.1679118f); const float nativeRadius = .215314761f;
            var y = new[] { .010f, -.008f, -.030f, -.072f, -.095f, -.110f }; float radius = nativeRadius + .004f;
            var outer = new[] { radius, radius, radius - .004f, .074f, .068f, .068f };
            var legacy = (Mesh)oldHollow.Invoke(null, new object[] { y.Select(d => socket + Vector3.up * d).ToArray(), outer, outer.Select(r => r - .008f).ToArray(), 32 }); owned.Add(legacy);
            var shared = (Mesh)sleeveType.GetMethod("Create", Flags).Invoke(null, new object[] { socket, nativeRadius, .068f }); owned.Add(shared);
            Check(legacy.vertices.SequenceEqual(shared.vertices) && legacy.normals.SequenceEqual(shared.normals) && legacy.uv.SequenceEqual(shared.uv) && legacy.triangles.SequenceEqual(shared.triangles) && legacy.tangents.SequenceEqual(shared.tangents), "Shared default recipe changes the existing Shay sleeve geometry.");
            float maxNativeFit = 0, maxManifoldFit = 0;
            foreach (float yaw in new[] { -15f, -12f, 0, 12f, 15f }) foreach (float bodyYaw in new[] { 0, 73f })
            {
                car.transform.SetPositionAndRotation(new Vector3(120, 20, -73), Quaternion.Euler(0, bodyYaw, 0));
                SteamBogiePoseAudit.Pose(car.transform, front, frontHome, true, yaw); SteamBogiePoseAudit.Pose(car.transform, rear, rearHome, false, -yaw); update.Invoke(owner, null);
                foreach (var sleeve in sleeves)
                {
                    int side=sleeve.name.Contains(" L]") ? -1 : 1;
                    var manifold=front.Find("[S060 manifold "+(side<0?"L":"R")+"]");
                    var matrix=car.transform.worldToLocalMatrix*manifold.localToWorldMatrix;
                    var fitted=manifold.GetComponent<MeshFilter>().sharedMesh;
                    var lower=Section(fitted.vertices.Select(matrix.MultiplyPoint3x4).ToArray(),fitted.triangles,1.990f);
                    var rawNative=Section(originalBody.vertices,originalBody.triangles,2.112f);
                    bool NativePoint(Vector3 p)=>Math.Sign(p.x)==side && Math.Abs(p.x)>=.52f && Math.Abs(p.x)<=1.5f && p.z>=2.98f && p.z<=3.35f;
                    var nativeCut=Enumerable.Range(0,rawNative.Length/2).Where(i=>NativePoint(rawNative[i*2]) && NativePoint(rawNative[i*2+1])).SelectMany(i=>new[]{rawNative[i*2],rawNative[i*2+1]}).ToArray();
                    var vertices=sleeve.sharedMesh.vertices; int stride=vertices.Length/18;
                    int tested=0;
                    for(int i=0;i<stride-1;i++)
                    {
                        var upper=vertices[7*stride+i]; var bottom=vertices[13*stride+i];
                        // The native pipe omits its invisible back inside the
                        // boiler; measure the entire exposed half independently.
                        if(side*upper.x>.71f)
                        {
                            float gap=Distance(upper,nativeCut);maxNativeFit=Mathf.Max(maxNativeFit,gap);
                            Check(gap<.001f,"Upper cuff must touch the actual angled native pipe within 1 mm, not surround an envelope: "+gap);tested++;
                        }
                        float lowerGap=Distance(bottom,lower);maxManifoldFit=Mathf.Max(maxManifoldFit,lowerGap);
                        Check(lowerGap<.001f,"Lower cuff must follow actual manifold section within 1 mm throughout support motion: "+lowerGap);
                    }
                    Check(tested>10,"Native exposed pipe profile was not adequately sampled.");
                }
            }
            owner.Dispose(); owner = null;
            Check(sleeveMeshes.All(m => m == null), "Sleeve meshes survive disposal.");
            Check(car.GetComponentsInChildren<Transform>(true).All(t => !t.name.StartsWith("[Dockside smokebox sleeve ", StringComparison.Ordinal)), "Sleeve objects survive disposal.");
            Check(native.GetComponent<MeshFilter>().sharedMesh == originalBody, "Native neck mesh is not restored.");
            owner = (IDisposable)bind.Invoke(null, new object[] { car.transform, front, rear, native });
            Check(car.GetComponentsInChildren<MeshFilter>(true).Count(f => f.name.StartsWith("[Dockside smokebox sleeve ", StringComparison.Ordinal)) == 2, "Repeated binding duplicates sleeves.");
            owner.Dispose(); owner = null;
            string hash; using (var sha = SHA256.Create()) using (var stream = File.OpenRead(runtime)) hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            File.WriteAllText(Path.Combine(output, "dockside-sleeve-validation.json"), "{\"passed\":true,\"checks\":" + checks + ",\"runtimeSha256\":\"" + hash + "\",\"actualCandidateDll\":true,\"sameShayDefaultGeometry\":true,\"sleeves\":2,\"nativePivotYawDegrees\":[-15,-12,0,12,15],\"maximumNativeProfileGapMetres\":" + maxNativeFit.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"maximumManifoldProfileGapMetres\":" + maxManifoldFit.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"cleanupAndRebind\":true,\"gameScene\":false}");
            EditorApplication.Exit(0);
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(output, "sleeve-error.txt"), ex.ToString()); Debug.LogException(ex); EditorApplication.Exit(1); }
        finally { owner?.Dispose(); if (car != null) Object.DestroyImmediate(car); foreach (var o in owned) if (o != null) Object.DestroyImmediate(o); if (bundle != null) bundle.Unload(true); AppDomain.CurrentDomain.AssemblyResolve -= resolver; }
    }
}
#endif
