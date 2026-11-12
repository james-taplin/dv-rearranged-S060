#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object=UnityEngine.Object;

// Native methods and geometry only; this is not a game-session acceptance test.
public sealed class MeyerRunningGearAudit
{
    private const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
    [Serializable]private class Data{public Entry[] meshes;}
    [Serializable]private class Entry{public string name;public Vector3[] vertices,normals;public Vector2[] uv;public int[] triangles;}
    private Component heat;private Renderer[] originalHeat;private IDisposable owner;
    private Transform root,front,rear;
    private MeshFilter[] detail;private Renderer[] oldLeaves;private bool[] leafEnabled;
    private float newMinimumRadius=float.PositiveInfinity;
    private int comparisons,poses;
    private readonly List<string> contacts=new List<string>();
    private readonly HashSet<string> bodyPoses=new HashSet<string>();
    public static MeyerRunningGearAudit Prepare(GameObject go,List<Object> native)
    {
        var audit=new MeyerRunningGearAudit{root=go.transform};
        var data=JsonUtility.FromJson<Data>(File.ReadAllText(Environment.GetEnvironmentVariable("S060_DETAIL_NATIVE")));
        var sourceMaterial=go.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name=="s060_body").sharedMaterial;
        MeshFilter Draw(Entry entry,string name,Func<Vector3,bool> select,Material material)
        {
            var selected=Enumerable.Range(0,entry.triangles.Length/3).Where(i=>Enumerable.Range(0,3).All(k=>select(entry.vertices[entry.triangles[3*i+k]]))).SelectMany(i=>new[]{entry.triangles[3*i],entry.triangles[3*i+1],entry.triangles[3*i+2]}).ToArray();
            var mesh=new Mesh{name=name,vertices=entry.vertices,uv=entry.uv,triangles=selected};mesh.RecalculateNormals();mesh.RecalculateBounds();native.Add(mesh);
            var part=new GameObject(name);part.transform.SetParent(go.transform,false);var f=part.AddComponent<MeshFilter>();f.sharedMesh=mesh;var r=part.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.enabled=false;return f;
        }
        var brakeMaterial=new Material(Shader.Find("Standard"));brakeMaterial.EnableKeyword("_EMISSION");brakeMaterial.SetColor("_EmissionColor",Color.black);native.Add(brakeMaterial);
        string atlas=Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("S060_DETAIL_NATIVE")),"native-brake-atlas.png");
        if(File.Exists(atlas)){var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(atlas));brakeMaterial.mainTexture=texture;native.Add(texture);}
        var shoe=Draw(data.meshes.Single(e=>e.name=="s060_brake_shoes"),"s060_brake_shoes",p=>true,brakeMaterial);
        var game=Assembly.LoadFrom(Path.Combine(Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED"),"Assembly-CSharp.dll"));var type=game.GetType("DV.Simulation.Brake.BrakesOverheatingController",true);
        audit.heat=go.AddComponent(type);audit.originalHeat=new Renderer[]{shoe.GetComponent<MeshRenderer>()};type.GetField("brakeRenderers").SetValue(audit.heat,audit.originalHeat);
        var gradientField=type.GetField("overheatColor");var gradient=ScriptableObject.CreateInstance(gradientField.FieldType);native.Add(gradient);gradientField.FieldType.GetField("colorGradient").SetValue(gradient,new Gradient());gradientField.SetValue(audit.heat,gradient);
        return audit;
    }
    public IDisposable Bind(Assembly runtime,Transform front,Transform rear)
    {
        this.front=front;this.rear=rear;
        oldLeaves=new[]{front,rear}.SelectMany(f=>f.Find("[Powered bogie frame]").GetComponentsInChildren<Renderer>(true).Where(r=>r.name.StartsWith("Leaf spring "))).ToArray();leafEnabled=oldLeaves.Select(r=>r.enabled).ToArray();
        owner=(IDisposable)runtime.GetType("RearrangedS060.S060MeyerRunningGear",true).GetMethod("Bind",All).Invoke(null,new object[]{root,root,front,rear});
        detail=new[]{front,rear}.SelectMany(f=>f.Find("[Meyer native brakes and visible springs]").GetComponentsInChildren<MeshFilter>()).ToArray();
        if(detail.Count(f=>f.name.StartsWith("Meyer native brake shoes "))!=4||detail.Count(f=>f.name.StartsWith("Meyer native leaf spring "))!=8||oldLeaves.Any(r=>r.enabled))throw new InvalidOperationException("Eight native shoes, eight visible spring packs and retired hidden cube packs required.");
        var registered=(Renderer[])heat.GetType().GetField("brakeRenderers").GetValue(heat);
        if(registered.Length!=originalHeat.Length+4||detail.Where(f=>f.name.StartsWith("Meyer native brake shoes ")).Any(f=>!registered.Contains(f.GetComponent<Renderer>())))throw new InvalidOperationException("All four paired shoe renderers must use the original native heat controller.");
        var block=new MaterialPropertyBlock();var glow=new Color(4,.30f,.02f);block.SetColor("_EmissionColor",glow);
        var set=heat.GetType().GetMethod("SetOverheatPropertyBlockToAllRenderers",All);set.Invoke(heat,new object[]{block});
        foreach(var r in registered){var actual=new MaterialPropertyBlock();r.GetPropertyBlock(actual);if(((Vector4)(actual.GetColor("_EmissionColor")-glow)).sqrMagnitude>1e-8f)throw new InvalidOperationException("Native brake glow property application failed.");}
        set.Invoke(heat,new object[]{null});foreach(var r in registered){var actual=new MaterialPropertyBlock();r.GetPropertyBlock(actual);if(!actual.isEmpty)throw new InvalidOperationException("Native brake glow property clearing failed.");}
        var bodySource=(MeshRenderer)owner.GetType().GetField("bodyMaterialSource",All).GetValue(owner);var shoeSource=(MeshRenderer)owner.GetType().GetField("shoeMaterialSource",All).GetValue(owner);
        var bodyMaterial=bodySource.sharedMaterial;var shoeMaterial=shoeSource.sharedMaterial;var bodyChanged=new Material(bodyMaterial);var shoeChanged=new Material(shoeMaterial);
        try{bodySource.sharedMaterial=bodyChanged;shoeSource.sharedMaterial=shoeChanged;owner.GetType().GetMethod("LateUpdate",All).Invoke(owner,null);foreach(var f in detail)if(f.GetComponent<MeshRenderer>().sharedMaterial!=(f.name.StartsWith("Meyer native brake shoes ")?shoeChanged:bodyChanged))throw new InvalidOperationException("Restored native detail must follow both live material sources.");}
        finally{bodySource.sharedMaterial=bodyMaterial;shoeSource.sharedMaterial=shoeMaterial;owner.GetType().GetMethod("LateUpdate",All).Invoke(owner,null);Object.DestroyImmediate(bodyChanged);Object.DestroyImmediate(shoeChanged);}
        return owner;
    }
    public int Pose(MeshFilter bend)
    {
        poses++;newMinimumRadius=Mathf.Min(newMinimumRadius,Curvature(bend.sharedMesh));if(newMinimumRadius<.1425f)throw new InvalidOperationException("Return junction bend too tight: "+newMinimumRadius);
        // The static suspension hangers meet their own frame on purpose. Check
        // the restored shoes, hangers and leaf packs against moving wheels/rods,
        // plus the body/framework, with no wheel or body attachment exemption.
        foreach(var frame in new[]{front,rear})
        {
            var parts=detail.Where(f=>f.transform.IsChildOf(frame)).ToArray();
            var moving=frame.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<Renderer>()?.enabled==true&&(f.name.Contains("driver ")||f.name.StartsWith("Native S060 full-size cylinder QA ")||f.GetComponentsInParent<Transform>().Any(t=>t.name.StartsWith("[Meyer rods ")||t.name.StartsWith("[Round cylinders ")))).ToArray();
            // These members all share one rigid unit transform. Their relative
            // geometry is invariant across the articulation-only pose sweep.
            if(poses==1)foreach(var f in parts)foreach(var target in moving)Clear(f,target);
            var body=root.GetComponentsInChildren<MeshFilter>().Where(f=>(f.name=="s060_body"||f.name.StartsWith("Meyer R2 ")||f.name=="Meyer fixed tank-frame bearers")&&f.GetComponent<Renderer>()?.enabled==true).ToArray();
            string key=frame.GetInstanceID()+"|"+frame.position.ToString("F9")+"|"+frame.rotation.ToString("F9");
            if(bodyPoses.Add(key))foreach(var f in parts)foreach(var target in body)Clear(f,target);
        }
        return 2;
    }
    private void Clear(MeshFilter a,MeshFilter b){comparisons++;if(!a.GetComponent<Renderer>().bounds.Intersects(b.GetComponent<Renderer>().bounds))return;var shell=new SteamMeshContact.Shell(a,root);if(shell.Intersects(new SteamMeshContact.Shell(b,root,shell.Bounds),out var hit)&&contacts.Count<80)contacts.Add("pose "+poses+" "+a.transform.parent.parent.parent.name+": "+hit);}
    private static float Curvature(Mesh mesh)
    {
        var v=mesh.vertices;var p=new Vector3[25];for(int i=0;i<25;i++)for(int j=0;j<24;j++)p[i]+=v[i*24+j]/24;float minimum=float.PositiveInfinity;
        for(int i=1;i<24;i++){var a=p[i]-p[i-1];var b=p[i+1]-p[i];float cross=Vector3.Cross(a,b).magnitude;if(cross>1e-9f)minimum=Mathf.Min(minimum,a.magnitude*b.magnitude*(a+b).magnitude/(2*cross));}return minimum;
    }
    public void Finish(string output)
    {File.WriteAllLines(Path.Combine(output,"running-gear-contacts.txt"),contacts);if(contacts.Count!=0)throw new InvalidOperationException("Restored running-gear contacts; see running-gear-contacts.txt.");File.WriteAllText(Path.Combine(output,"running-gear-validation.json"),JsonUtility.ToJson(new Report{passed=true,poses=poses,nativeTriangleComparisons=comparisons,nativeShoes=8,nativeSpringPacks=8,originalNativeHeatController=true,nativeGlowSetAndClear=true,newMinimumBendRadius=newMinimumRadius,gameScene=false},true));}
    public void CheckRestored()
    {if(!ReferenceEquals(heat.GetType().GetField("brakeRenderers").GetValue(heat),originalHeat)||oldLeaves.Where((r,i)=>r.enabled!=leafEnabled[i]).Any()||detail.Any(f=>f!=null))throw new InvalidOperationException("Native heat array, old spring visibility and added detail cleanup must restore exactly.");}
    [Serializable]private sealed class Report{public bool passed,originalNativeHeatController,nativeGlowSetAndClear,gameScene;public int poses,nativeTriangleComparisons,nativeShoes,nativeSpringPacks;public float newMinimumBendRadius;}
    public void HeatPreview(bool enabled)
    {MaterialPropertyBlock block=null;if(enabled){block=new MaterialPropertyBlock();block.SetColor("_EmissionColor",new Color(4,.30f,.02f));}heat.GetType().GetMethod("SetOverheatPropertyBlockToAllRenderers",All).Invoke(heat,new object[]{block});}
}
#endif
