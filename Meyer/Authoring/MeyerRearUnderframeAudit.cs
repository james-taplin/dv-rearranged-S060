#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

// Bounded additions to the proven pipe fixture: actual new frame triangles,
// rendered native wheels/rods and floor clearance in the same 27 poses.
public sealed class MeyerRearUnderframeAudit
{
    private readonly Transform root,rear;
    private readonly MeshFilter fixedFrame,unitFrame;
    private readonly MeshFilter[] floors;
    private readonly MeshFilter link,housing,frontFork,rearFork;
    private float maximumDrawgearTravel,maximumPinError;
    private float minimumFloorGap=float.PositiveInfinity;
    private int comparisons,poses;
    public bool Removed=>fixedFrame==null&&unitFrame==null&&link==null&&housing==null&&frontFork==null&&rearFork==null;
    public MeyerRearUnderframeAudit(Transform root,Transform rear)
    {
        this.root=root;this.rear=rear;
        fixedFrame=root.GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="Meyer fixed tank-frame bearers");
        unitFrame=rear.GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="Meyer rear plate frame and spring saddles");
        if(!fixedFrame.transform.IsChildOf(root.Find("[Meyer R2 bodywork]"))||!unitFrame.transform.IsChildOf(rear))throw new InvalidOperationException("Separate body and swivelling frame ownership required.");
        floors=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh!=null&&(f.name=="s060_body"||f.name=="Meyer R2 ironwork")).ToArray();
        MeshFilter Part(string n)=>root.GetComponentsInChildren<MeshFilter>().Single(f=>f.name==n);
        link=Part("Meyer heavy articulated drawbar");housing=Part("Meyer short drawgear housing");
        frontFork=Part("Meyer loco drawgear pin");rearFork=Part("Meyer bogie drawgear pin");
    }
    public int ValidatePose(MeshFilter[] pipes)
    {
        poses++;int checks=0;
        bool Shown(MeshFilter f)=>f.sharedMesh!=null&&f.GetComponent<Renderer>()!=null&&f.GetComponent<Renderer>().enabled&&f.GetComponentsInParent<Transform>(true).All(t=>t.gameObject.activeSelf);
        var moving=rear.GetComponentsInChildren<MeshFilter>().Where(Shown).ToArray();
        // Verify both real eyes stay concentric with their pins; the link is
        // rigid, and only the front shank may slide in its body-mounted box.
        float pinError=Mathf.Max(Vector3.Distance(link.transform.TransformPoint(Vector3.zero),frontFork.transform.position),Vector3.Distance(link.transform.TransformPoint(new Vector3(0,0,.675f)),rearFork.transform.position));
        maximumPinError=Mathf.Max(maximumPinError,pinError);
        maximumDrawgearTravel=Mathf.Max(maximumDrawgearTravel,Mathf.Abs(root.InverseTransformPoint(frontFork.transform.position).z+4.17f));
        if(pinError>.0001f||maximumDrawgearTravel>.06f)throw new InvalidOperationException("Drawbar pin seating or short drawgear stroke exceeded.");checks+=2;
        foreach(var part in new[]{link,housing,frontFork,rearFork})
        {
            foreach(var f in moving.Where(f=>f.name.Contains("driver ")||f.GetComponentsInParent<Transform>().Any(t=>t.name.StartsWith("[Meyer rods ")))){Clear(part,f);checks++;}
            foreach(var pipe in pipes){Clear(part,pipe);checks++;}
        }
        foreach(var f in floors){Clear(link,f);checks++;}
        // New unit members intentionally join the old static frame and spring
        // mounts. Only wheels and animated rods are forbidden contacts there.
        foreach(var f in moving.Where(f=>f.name.Contains("driver ")||f.GetComponentsInParent<Transform>().Any(t=>t.name.StartsWith("[Meyer rods "))))
        {Clear(unitFrame,f);Clear(fixedFrame,f);checks+=2;}
        foreach(var f in floors.Concat(root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Meyer R2 paintwork"||f.name=="Meyer R2 buffer beam")))
        {Clear(unitFrame,f);checks++;}
        // Clip only the actual central bearing's mating volume. Rail, wheel,
        // floor and spring clearances receive no attachment exemption.
        var pivot=root.InverseTransformPoint(rear.position);
        var bearing=new Bounds(new Vector3(pivot.x,1.53f,pivot.z),new Vector3(.62f,.24f,.62f));
        var cut=Outside(fixedFrame,bearing);
        try{Clear(unitFrame,cut);checks++;}finally{Object.DestroyImmediate(cut.sharedMesh);Object.DestroyImmediate(cut.gameObject);}
        foreach(var pipe in pipes.Where(f=>f.name=="LP collector"||f.name.StartsWith("LP chest outlet ")||f.name=="Rear articulated bend"))
            foreach(var floor in floors)minimumFloorGap=Mathf.Min(minimumFloorGap,FloorGap(pipe,floor));
        return checks;
    }
    public bool RepairContract(Transform lpBody, string output)
    {
        var native = floors.Single(f=>f.name=="s060_body");
        var plate = floors.Single(f=>f.name=="Meyer R2 ironwork");
        bool Joins(MeshFilter added, Vector3 centre, Vector3 size)
        {
            var region = new Bounds(centre,size);
            return new SteamMeshContact.Shell(added,root,region).Intersects(new SteamMeshContact.Shell(native,root,region));
        }
        bool left = Joins(fixedFrame,new Vector3(-.43f,1.60f,-4.10f),new Vector3(.22f,.09f,.42f));
        bool right = Joins(fixedFrame,new Vector3(.43f,1.60f,-4.10f),new Vector3(.22f,.09f,.42f));
        bool plateLeft = Joins(plate,new Vector3(-1.10f,1.64f,-4.10f),new Vector3(.25f,.10f,.50f));
        bool plateRight = Joins(plate,new Vector3(1.10f,1.64f,-4.10f),new Vector3(.25f,.10f,.50f));
        var trunk = lpBody.GetComponentsInChildren<MeshFilter>().Single(f=>f.name.StartsWith("Single "));
        // Clip the actual faces: long straight spans need not have an interior
        // vertex, and a vertex-only filter would falsely report an empty run.
        var tv=Vertices(trunk);var tt=trunk.sharedMesh.triangles;var samples=new List<Vector3>();
        for(int i=0;i<tt.Length;i+=3)
        {
            var poly=new List<Vector3>{tv[tt[i]],tv[tt[i+1]],tv[tt[i+2]]};
            poly=Clip(poly,p=>p.z+4.20f,false);poly=Clip(poly,p=>p.z-2.20f,true);samples.AddRange(poly);
        }
        var course = samples.ToArray();
        float top = course.Length==0 ? float.PositiveInfinity : course.Max(p=>p.y);
        bool low = course.Length!=0 && top < 1.55f && course.Min(p=>p.z)<-3.9f && course.Max(p=>p.z)>2f;
        // The bottom face isolates the plate/lips from upright hopper edging.
        bool openPlate=!plateLeft&&!plateRight&&Vertices(plate).Where(p=>p.y>1.619f&&p.y<1.621f&&Mathf.Abs(p.x)>1f).Max(p=>p.z)<-4.379f;
        bool locoMount=Joins(housing,new Vector3(0,1.08f,-3.95f),new Vector3(.60f,.32f,.36f));
        bool unitMount=new SteamMeshContact.Shell(rearFork,root).Intersects(new SteamMeshContact.Shell(unitFrame,root));
        bool owned=rearFork.transform.IsChildOf(rear)&&!frontFork.transform.IsChildOf(rear)&&!link.transform.IsChildOf(rear);
        bool upperBridgeRemoved=!left&&!right&&Vertices(fixedFrame).Max(p=>p.z)<-4.379f;
        bool passed=upperBridgeRemoved&&openPlate&&locoMount&&unitMount&&owned&&low;
        File.WriteAllText(Path.Combine(output,"meyer-repair-contract.json"),JsonUtility.ToJson(new RepairReport{
            passed=passed,upperBridgeRemoved=upperBridgeRemoved,plateGapOpen=openPlate,locoDrawgearMounted=locoMount,bogieForkMounted=unitMount,separateDrawgearOwnership=owned,
            returnBelowRunningPlate=low,maximumLongCourseY=top,gameplayValidated=false},true));
        return passed;
    }
    private void Clear(MeshFilter a,MeshFilter b)
    {
        comparisons++;
        if(new SteamMeshContact.Shell(a,root).Intersects(new SteamMeshContact.Shell(b,root),out var hit))throw new InvalidOperationException("Rear underframe: "+hit);
    }
    public void Finish(string output)
    {
        if(float.IsInfinity(minimumFloorGap)||minimumFloorGap<.045f)throw new InvalidOperationException("Measured under-cab floor margin must exceed 45 mm; actual "+minimumFloorGap);
        File.WriteAllText(Path.Combine(output,"rear-underframe-validation.json"),JsonUtility.ToJson(new Report{passed=true,poses=poses,triangleComparisons=comparisons,minimumFloorGapMetres=minimumFloorGap,maximumDrawgearTravel=maximumDrawgearTravel,maximumPinError=maximumPinError,collectorLoweredMetres=.04f,bodyUnionLoweredMetres=.12f,bodyAndUnitOwnership=true,gameScene=false},true));
    }
    private Vector3[] Vertices(MeshFilter f)=>f.sharedMesh.vertices.Select(p=>root.InverseTransformPoint(f.transform.TransformPoint(p))).ToArray();
    private MeshFilter Outside(MeshFilter f,Bounds box)
    {
        var v=Vertices(f);var t=f.sharedMesh.triangles;var result=new List<Vector3>();
        for(int i=0;i<t.Length;i+=3)
        {
            var remainder=new List<Vector3>{v[t[i]],v[t[i+1]],v[t[i+2]]};
            for(int axis=0;axis<3&&remainder.Count>=3;axis++)
            {
                var lo=Clip(remainder,p=>p[axis]-box.min[axis],true);Add(lo,result);
                remainder=Clip(remainder,p=>p[axis]-box.min[axis],false);
                var hi=Clip(remainder,p=>p[axis]-box.max[axis],false);Add(hi,result);
                remainder=Clip(remainder,p=>p[axis]-box.max[axis],true);
            }
        }
        var mesh=new Mesh{vertices=result.ToArray(),triangles=Enumerable.Range(0,result.Count).ToArray()};
        var go=new GameObject("Fixed underframe outside bearing");go.transform.SetParent(root,false);var filter=go.AddComponent<MeshFilter>();filter.sharedMesh=mesh;return filter;
    }
    private static void Add(List<Vector3> polygon,List<Vector3> result)
    {for(int j=1;j+1<polygon.Count;j++)if(Vector3.Cross(polygon[j]-polygon[0],polygon[j+1]-polygon[0]).sqrMagnitude>1e-14f)result.AddRange(new[]{polygon[0],polygon[j],polygon[j+1]});}
    private static List<Vector3> Clip(List<Vector3> polygon,Func<Vector3,float> distance,bool below)
    {
        var result=new List<Vector3>();for(int i=0;i<polygon.Count;i++){var a=polygon[i];var b=polygon[(i+1)%polygon.Count];float da=distance(a),db=distance(b);if(below?da<=0:da>=0)result.Add(a);if((da<0)!=(db<0))result.Add(Vector3.LerpUnclamped(a,b,da/(da-db)));}return result;
    }
    private float FloorGap(MeshFilter pipe,MeshFilter floor)
    {
        float gap=float.PositiveInfinity;var v=Vertices(floor);var t=floor.sharedMesh.triangles;var pv=Vertices(pipe);var pt=pipe.sharedMesh.triangles;
        for(int i=0;i<t.Length;i+=3)
        {
            var a=v[t[i]];var b=v[t[i+1]];var c=v[t[i+2]];var n=Vector3.Cross(b-a,c-a).normalized;
            if(Mathf.Abs(n.y)<.95f||a.y<1.45f||a.y>1.70f||b.y<1.45f||b.y>1.70f||c.y<1.45f||c.y>1.70f||Mathf.Max(a.z,Mathf.Max(b.z,c.z))< -5f||Mathf.Min(a.z,Mathf.Min(b.z,c.z))> -3.5f)continue;
            float Cross(Vector3 u,Vector3 w)=>u.x*w.z-u.z*w.x;
            float sign=Mathf.Sign(Cross(b-a,c-a));
            for(int j=0;j<pt.Length;j+=3)
            {
                var poly=new List<Vector3>{pv[pt[j]],pv[pt[j+1]],pv[pt[j+2]]};
                foreach(var edge in new[]{(a,b),(b,c),(c,a)})poly=Clip(poly,p=>sign*Cross(edge.Item2-edge.Item1,p-edge.Item1),false);
                foreach(var p in poly)gap=Mathf.Min(gap,a.y-(n.x*(p.x-a.x)+n.z*(p.z-a.z))/n.y-p.y);
            }
        }
        return gap;
    }
    [Serializable]private sealed class Report{public bool passed,bodyAndUnitOwnership,gameScene;public int poses,triangleComparisons;public float minimumFloorGapMetres,maximumDrawgearTravel,maximumPinError,collectorLoweredMetres,bodyUnionLoweredMetres;}
    [Serializable]private sealed class RepairReport{public bool passed,upperBridgeRemoved,plateGapOpen,locoDrawgearMounted,bogieForkMounted,separateDrawgearOwnership,returnBelowRunningPlate,gameplayValidated;public float maximumLongCourseY;}
}
#endif
