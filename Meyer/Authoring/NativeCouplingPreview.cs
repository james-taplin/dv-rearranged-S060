#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;
// Native housing placement replaces CCL donor preview hardware. Production
// native coupling controls, hose bones and transforms are not changed.
public static class NativeCouplingPreview
{
    [Serializable]private class Data{public Node[] nodes;public Entry[] meshes;}
    [Serializable]private class Node{public string name,mesh;public Vector3 position,scale;public Quaternion rotation;public Node[] children;}
    [Serializable]private class Entry{public string name;public Vector3[] vertices;public int[] triangles;}
    public struct Placement{public float frontEmbed,rearEmbed;}
    public static Placement Apply(GameObject root,List<Object> owned)
    {
        var data=JsonUtility.FromJson<Data>(File.ReadAllText(Environment.GetEnvironmentVariable("S060_COUPLING_FIXTURE")));
        var result=new Placement();
        foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if(f.GetComponent<Renderer>()==null)continue;
            bool hardware=f.GetComponentsInParent<Transform>(true).Any(t=>t.name=="BuffersAndChainRig"||t.name=="hoses"||t.name.IndexOf("Coupler",StringComparison.OrdinalIgnoreCase)>=0)||
                f.name.StartsWith("Native rear buffer head QA")||f.name.IndexOf("CableBase",StringComparison.OrdinalIgnoreCase)>=0;
            if(hardware)f.GetComponent<Renderer>().enabled=false;
        }
        var iron=new Material(Shader.Find("Standard")){color=new Color(.10f,.11f,.105f)};iron.SetFloat("_Metallic",.5f);owned.Add(iron);
        var red=new Material(iron){color=new Color(.55f,.025f,.018f)};owned.Add(red);
        var meshes=data.meshes.ToDictionary(m=>m.name);
        void Walk(Node node,Matrix4x4 parent,bool rearRig,bool rearBuffer)
        {
            var matrix=parent*Matrix4x4.TRS(node.position,node.rotation,node.scale);
            // Separate native rear buffer-head renderers are siblings of the
            // rig, not children of its anchors. Apply the same 3.70 m shift as
            // S060MeyerLights; otherwise ghost heads remain beneath the cab.
            if(node.name=="Buffer_RL"||node.name=="Buffer_RR")
            {matrix=Matrix4x4.Translate(new Vector3(0,0,-3.70f))*matrix;rearRig=true;}
            if(node.name=="BuffersAndChainRig"){rearRig=node.position.z<0;if(rearRig)matrix=Matrix4x4.Translate(new Vector3(0,0,-3.995f))*matrix;}
            if(rearRig&&node.name.StartsWith("buffer anchor")){matrix=Matrix4x4.Translate(new Vector3(0,0,.295f))*matrix;rearBuffer=true;}
            if(node.name=="HookPlate_R")matrix=Matrix4x4.Translate(new Vector3(0,0,-3.995f))*matrix;
            if(!string.IsNullOrEmpty(node.mesh)&&node.mesh!="Open"&&node.mesh!="Closed")
            {
                var e=meshes[node.mesh];var v=e.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
                var mesh=new Mesh{vertices=v,triangles=e.triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Add(mesh);
                var go=new GameObject("Exact native QA "+(rearRig?"rear ":"front ")+node.name);go.transform.SetParent(root.transform,false);owned.Add(go);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=node.mesh.StartsWith("CableBase")?red:iron;
                if(node.mesh=="CableBase_01")
                {if(rearRig)result.rearEmbed=v.Max(p=>p.z)+7.98f;else result.frontEmbed=4.0f-v.Min(p=>p.z);}
            }
            foreach(var child in node.children)Walk(child,matrix,rearRig,rearBuffer);
        }
        foreach(var node in data.nodes)Walk(node,Matrix4x4.identity,false,false);
        // Buffer heads and stems are separate native renderers. The donor-only
        // preview never supplied the real front stems; include them explicitly.
        var lamps=JsonUtility.FromJson<Data>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW")),"meyer-lights-native.json")));
        var stem=lamps.meshes.Single(e=>e.name=="s060_buffer_stems");
        var indices=new List<int>();for(int i=0;i<stem.triangles.Length;i+=3)if(Enumerable.Range(0,3).All(k=>stem.vertices[stem.triangles[i+k]].z>0))indices.AddRange(new[]{stem.triangles[i],stem.triangles[i+1],stem.triangles[i+2]});
        var stemMesh=new Mesh{vertices=stem.vertices,triangles=indices.ToArray()};stemMesh.RecalculateNormals();stemMesh.RecalculateBounds();owned.Add(stemMesh);
        var stemObject=new GameObject("Exact native QA front buffer stems");stemObject.transform.SetParent(root.transform,false);owned.Add(stemObject);stemObject.AddComponent<MeshFilter>().sharedMesh=stemMesh;stemObject.AddComponent<MeshRenderer>().sharedMaterial=iron;

        return result;
    }
}
#endif
