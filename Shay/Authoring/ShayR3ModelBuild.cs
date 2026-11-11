using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

// Imports the same editable original recipes used by the R2 authoring tools.
// One Editor session builds the integrated asset; native meshes stay live aliases.
public static class ShayR3ModelBuild
{
    [Serializable] public class EngineSpec { public float[] origin; public float cylinderPitch, valveAxisX, bore, stroke; }
    [Serializable] public class GearSpec { public int[] teeth; public float coneApexX; }
    [Serializable] public class ShaftSpec { public float memberLength,jointEndAllowance; public float[][] engineEnds,truckEndsLocal; }
    [Serializable] public class Contract { public EngineSpec engine; public GearSpec bevelGears; public float rollingRadius; public ShayBundleBuild.Motion motion; }
    [Serializable] public class EngineGroup { public string name,parent; public float[] position; public float rotationZ; }
    [Serializable] public class EnginePart { public string name,group; public int slot; public float[] vertices,normals,uv; public int[] triangles; }
    [Serializable] public class EngineLod { public EngineGroup[] groups; public EnginePart[] parts; }
    [Serializable] public class EngineMarker { public string name,parent; public float[] position,rotationQuaternion; }
    [Serializable] public class EngineMaterial { public string name; public float[] previewRGBA; }
    [Serializable] public class EngineRecipe { public EngineLod[] lods; public EngineMarker[] markers; public EngineMaterial[] materials; }
    [Serializable] public class TruckNode { public string name,parent; public float[] position; }
    [Serializable] public class Geometry { public Vector3[] vertices; public int[] triangles; }
    [Serializable] public class TruckPart { public string name,parent,material,category; public Geometry[] lods; }
    [Serializable] public class TruckMarker { public string name,parent; public float[] position; }
    [Serializable] public class NativeAlias { public string path; public float[] scale; }
    [Serializable] public class TruckRecipe { public TruckNode[] nodes; public TruckPart[] parts; public TruckMarker[] markers; public NativeAlias[] nativeAliases; }
    [Serializable] public class TransmissionMesh { public string name; public Vector3[] vertices,normals; public Vector2[] uv; public int[] triangles; }
    [Serializable] public class TransmissionMarker { public string name,parent; public Vector3 position; public Quaternion rotation; }
    [Serializable] public class TransmissionPart { public string name,parent,mesh,slot,role; public int index; public Vector3 position,euler,initialPosition; public Quaternion initialRotation; }
    [Serializable] public class TransmissionRecipe { public TransmissionMesh[] meshes; public TransmissionMarker[] markers; public TransmissionPart[] parts; }
    private const string Assets = "Assets/ShayR3Original";
    private static int serial;
    private static Vector3 V(float[] a) => new Vector3(a[0],a[1],a[2]);
    private static Vector3[] Vectors(float[] a) => Enumerable.Range(0,a.Length/3).Select(i=>new Vector3(a[3*i],a[3*i+1],a[3*i+2])).ToArray();
    private static T Read<T>(string p) => JsonUtility.FromJson<T>(File.ReadAllText(p));
    private static Transform Node(string name,Transform parent,Vector3 position=default(Vector3))
    { var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t; }
    private static T Save<T>(T value,string name) where T:Object
    {
        string path=Assets+"/"+name;var old=AssetDatabase.LoadAssetAtPath<T>(path);
        if(old!=null){EditorUtility.CopySerialized(value,old);Object.DestroyImmediate(value);EditorUtility.SetDirty(old);return old;}
        AssetDatabase.CreateAsset(value,path);return value;
    }
    private static Material Material(string name,Color color)
    {var m=new Material(Shader.Find("Standard")){name=name,color=color};m.SetFloat("_Metallic",.55f);m.SetFloat("_Glossiness",.30f);return Save(m,name+".mat");}
    private static Mesh Mesh(string name,Vector3[] v,int[] t,Vector3[] n=null,Vector2[] uv=null,bool flat=false)
    {
        if(flat){v=t.Select(i=>v[i]).ToArray();t=Enumerable.Range(0,v.Length).ToArray();}
        var m=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,vertices=v,triangles=t};
        if(n!=null&&n.Length==v.Length)m.normals=n;else m.RecalculateNormals();
        m.uv=uv!=null&&uv.Length==v.Length?uv:v.Select(p=>new Vector2(p.z,p.y)).ToArray();
        m.RecalculateBounds();m.RecalculateTangents();return m;
    }
    private static Renderer Draw(Transform parent,string name,Mesh mesh,Material material)
    {
        var t=Node(name,parent);t.gameObject.AddComponent<MeshFilter>().sharedMesh=Save(mesh,"mesh_"+(serial++)+".asset");
        var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=material;return r;
    }
    private static void Lod(Transform root,Renderer[][] renderers,float near=.25f,float far=.035f)
    {
        var g=root.gameObject.AddComponent<LODGroup>();g.SetLODs(renderers.Select((r,i)=>new LOD(i==0?near:i==renderers.Length-1?far:.12f,r)).ToArray());g.RecalculateBounds();
    }
    public static GameObject Create(string source,out ShayBundleBuild.Motion motion)
    {
        serial=0;Directory.CreateDirectory(Assets);AssetDatabase.Refresh();
        var c=Read<Contract>(Path.Combine(source,"shared/fit-contract.json"));
        var root=new GameObject("S060 Shay R3 — 0.80 m / 86:20");
        var e=Read<EngineRecipe>(Path.Combine(source,"engine/unity/Assets/EngineStudy/engine-model.json"));
        var engine=Node("Engine",root.transform,V(c.engine.origin));
        var em=e.materials.Select(m=>Material(m.name,new Color(m.previewRGBA[0],m.previewRGBA[1],m.previewRGBA[2]))).ToArray();
        var er=new List<Renderer[]>();
        for(int l=0;l<e.lods.Length;l++)
        {
            var level=Node("LOD"+l,engine);var nodes=new Dictionary<string,Transform>{{"ROOT",level}};
            foreach(var g in e.lods[l].groups){var t=Node(g.name,nodes[g.parent],V(g.position));t.localRotation=Quaternion.AngleAxis(g.rotationZ,Vector3.forward);nodes.Add(g.name,t);}
            var renderers=new List<Renderer>();
            foreach(var p in e.lods[l].parts)
            {
                var uv=Enumerable.Range(0,p.uv.Length/2).Select(i=>new Vector2(p.uv[i*2],p.uv[i*2+1])).ToArray();
                renderers.Add(Draw(nodes[p.group],p.name,Mesh(p.name,Vectors(p.vertices),p.triangles,Vectors(p.normals),uv),em[p.slot]));
            }
            if(l==0)foreach(var m in e.markers)
            {var t=Node(m.name,m.parent=="ROOT"?engine:nodes[m.parent],V(m.position));var q=m.rotationQuaternion;t.localRotation=new Quaternion(q[0],q[1],q[2],q[3]);}
            er.Add(renderers.ToArray());
        }
        Lod(engine,er.ToArray());

        var tr=Read<TruckRecipe>(Path.Combine(source,"trucks/source/unity-recipe.json"));var trucks=Node("Trucks",root.transform);
        var tn=new Dictionary<string,Transform>{{"ROOT",trucks}};
        foreach(var n in tr.nodes)tn.Add(n.name,Node(n.name,tn[n.parent],V(n.position)));
        var tm=new Dictionary<string,Material>();
        foreach(var name in tr.parts.Select(p=>p.material).Distinct())tm[name]=Material(name,new Color(.23f,.24f,.24f));
        string Owner(string name)
        {while(!new[]{"TRUCK_REAR","TRUCK_FRONT","REAR_BODY_ATTACHMENT","FRONT_BODY_ATTACHMENT","SHAY_FRAME_DETAILS"}.Contains(name))name=tr.nodes.Single(n=>n.name==name).parent;return name;}
        var buckets=new Dictionary<string,List<CombineInstance>>();
        foreach(var p in tr.parts)
        {
            string owner=Owner(p.parent);
            for(int l=0;l<2;l++)
            {
                var m=p.lods[l];var mesh=Mesh(p.name,m.vertices,m.triangles,flat:p.category!="pressure_vessel");
                string key=owner+"|"+l+"|"+p.material;if(!buckets.ContainsKey(key))buckets.Add(key,new List<CombineInstance>());
                buckets[key].Add(new CombineInstance{mesh=mesh,transform=tn[owner].worldToLocalMatrix*tn[p.parent].localToWorldMatrix});
            }
        }
        foreach(string owner in buckets.Keys.Select(k=>k.Split('|')[0]).Distinct())
        {
            var lods=new List<Renderer>[] {new List<Renderer>(),new List<Renderer>()};
            foreach(var pair in buckets.Where(p=>p.Key.StartsWith(owner+"|",StringComparison.Ordinal)))
            {var k=pair.Key.Split('|');int l=int.Parse(k[1]);var mesh=new Mesh{name=owner+" "+k[2]+" LOD"+l,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray());lods[l].Add(Draw(tn[owner],mesh.name,mesh,tm[k[2]]));foreach(var part in pair.Value)Object.DestroyImmediate(part.mesh);}
            Lod(tn[owner],lods.Select(l=>l.ToArray()).ToArray(),.10f,.015f);
        }
        foreach(var m in tr.markers)Node(m.name,tn[m.parent],V(m.position));
        foreach(var a in tr.nativeAliases){var t=tn[a.path];t.localScale=V(a.scale);t.gameObject.AddComponent<MeshFilter>();t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=tm["frame_painted"];}

        var tx=Read<TransmissionRecipe>(Path.Combine(source,"transmission/unity/Assets/Transmission/recipe.json"));var transmission=Node("Transmission",root.transform);
        var txNodes=new Dictionary<string,Transform>{{"ROOT",transmission}};
        foreach(var m in tx.markers){var t=Node(m.name,txNodes[m.parent],m.position);t.localRotation=m.rotation;txNodes.Add(m.name,t);}
        var txMeshes=tx.meshes.ToDictionary(m=>m.name,m=>Mesh(m.name,m.vertices,m.triangles,m.normals,m.uv));
        var txMaterials=tx.parts.Select(p=>p.slot).Distinct().ToDictionary(n=>n,n=>Material(n,new Color(.38f,.38f,.36f)));
        var bindings=new List<ShayBundleBuild.Binding>();var txLods=new List<Renderer>[] {new List<Renderer>(),new List<Renderer>(),new List<Renderer>()};
        foreach(var p in tx.parts)
        {
            var t=Node(p.name+"__AXIS",txNodes[p.parent],p.position);t.localRotation=Quaternion.Euler(p.euler);
            bindings.Add(new ShayBundleBuild.Binding{path=AnimationUtility.CalculateTransformPath(t,transmission),role=p.role,index=p.index,restPosition=t.localPosition,restRotation=t.localRotation});
            if(new[]{"member","outer_yoke","inner_yoke","cross"}.Contains(p.role)){t.localPosition=p.initialPosition;t.localRotation=p.initialRotation;}
            for(int l=0;l<3;l++)txLods[l].Add(Draw(t,p.name+"_LOD"+l,Object.Instantiate(txMeshes[p.mesh+"_LOD"+l]),txMaterials[p.slot]));
        }
        foreach(var m in txMeshes.Values)Object.DestroyImmediate(m);
        Lod(transmission,txLods.Select(l=>l.ToArray()).ToArray(),.3f,.02f);
        motion=c.motion??throw new InvalidOperationException("Frozen R3 motion datums missing.");motion.parts=bindings.ToArray();
        AssetDatabase.SaveAssets();return root;
    }
}
