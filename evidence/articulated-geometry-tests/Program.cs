using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RearrangedS282;
using UnityEngine;

internal static class Program
{
    private static int checks,partitions;
    private static readonly List<string> scenarios=new List<string>();
    private static void Check(bool ok,string message) {checks++;if(!ok)throw new InvalidOperationException(message);}
    private static bool Near(float a,float b,float tolerance=2e-5f)=>Math.Abs(a-b)<=tolerance;
    private static void Throws<T>(Action action,string scenario) where T:Exception
    {bool caught=false;try{action();}catch(T){caught=true;}Check(caught,scenario);}
    private static Mesh Make(Vector3[] positions,params int[][] indices)
    {
        var mesh=new Mesh{name="native frame fixture",vertices=positions,subMeshCount=indices.Length};
        for(int i=0;i<indices.Length;i++)mesh.SetTriangles(indices[i].ToList(),i);
        return mesh;
    }
    private static float Area(Mesh mesh)
    {
        float result=0;
        for(int sub=0;sub<mesh.subMeshCount;sub++)
        {
            var t=mesh.GetTriangles(sub);
            for(int i=0;i<t.Length;i+=3)result+=Vector3.Cross(mesh.vertices[t[i+1]]-mesh.vertices[t[i]],mesh.vertices[t[i+2]]-mesh.vertices[t[i]]).magnitude*.5f;
        }
        return result;
    }
    private static void Partition(Mesh source,Vector3 point,Vector3 normal,string scenario)
    {
        var front=ArticulatedFrameClipper.Clip(source,point,normal,true);
        var rear=ArticulatedFrameClipper.Clip(source,point,normal,false);
        Check(Near(Area(front)+Area(rear),Area(source),1e-4f),scenario+" conserves area");
        foreach(var pair in new[]{(front,1f),(rear,-1f)})
        {
            Check(pair.Item1.subMeshCount==source.subMeshCount,scenario+" preserves submesh count");
            Check(pair.Item1.recalculatedBounds,scenario+" recalculates bounds");
            foreach(var v in pair.Item1.vertices)Check(Vector3.Dot(v-point,normal)*pair.Item2>=-2e-5f,scenario+" confines vertices to halfspace");
            for(int sub=0;sub<source.subMeshCount;sub++)
            {
                var original=source.GetTriangles(sub);var output=pair.Item1.GetTriangles(sub);
                if(original.Length<3)continue;
                var orientation=Vector3.Cross(source.vertices[original[1]]-source.vertices[original[0]],source.vertices[original[2]]-source.vertices[original[0]]);
                for(int i=0;i<output.Length;i+=3)
                {
                    var face=Vector3.Cross(pair.Item1.vertices[output[i+1]]-pair.Item1.vertices[output[i]],pair.Item1.vertices[output[i+2]]-pair.Item1.vertices[output[i]]);
                    Check(face.sqrMagnitude>1e-12f,scenario+" emits no degenerate face");
                    Check(Vector3.Dot(face,orientation)>0f,scenario+" preserves winding");
                }
            }
        }
        partitions++;
    }
    private static Vector4 Uv(Vector3 p,int channel)=>new Vector4(p.x+channel*2,p.z-channel,p.x+p.z,channel+.25f);
    private static void Main(string[] args)
    {
        var square=Make(new[]{new Vector3(-1,0,-1),new Vector3(1,0,-1),new Vector3(1,0,1),new Vector3(-1,0,1)},new[]{0,2,1},new[]{0,3,2});
        square.normals=Enumerable.Repeat(new Vector3(0,1,0),4).ToArray();
        square.tangents=Enumerable.Repeat(new Vector4(1,0,0,-1),4).ToArray();
        square.colors=square.vertices.Select(v=>new Color((v.x+1)*.5f,(v.z+1)*.5f,.25f,.75f)).ToArray();
        for(int channel=0;channel<8;channel++)square.SetUVs(channel,square.vertices.Select(v=>Uv(v,channel)).ToList());
        var originalPositions=(Vector3[])square.vertices.Clone();var originalTriangles=square.GetTriangles(0);
        Partition(square,Vector3.zero,new Vector3(0,0,1),"center plane");
        foreach(bool keepForward in new[]{true,false})
        {
            var half=ArticulatedFrameClipper.Clip(square,Vector3.zero,new Vector3(0,0,1),keepForward);
            Check(half.normals.Length==half.vertices.Length&&half.tangents.Length==half.vertices.Length&&half.colors.Length==half.vertices.Length,"attributes retain vertex counts");
            for(int i=0;i<half.vertices.Length;i++)
            {
                var p=half.vertices[i];
                Check(Near(half.normals[i].y,1)&&Near(half.normals[i].magnitude,1),"normal preserved/interpolated normalized");
                Check(Near(half.tangents[i].x,1)&&Near(half.tangents[i].w,-1),"tangent direction and handedness preserved");
                Check(Near(half.colors[i].r,(p.x+1)*.5f)&&Near(half.colors[i].g,(p.z+1)*.5f)&&Near(half.colors[i].b,.25f)&&Near(half.colors[i].a,.75f),"colors interpolate at plane");
                for(int channel=0;channel<8;channel++)
                {
                    Check(half.Uv(channel).Length==half.vertices.Length,"all eight UV channels retained");
                    var actual=half.Uv(channel)[i];var expected=Uv(p,channel);
                    Check(Near(actual.x,expected.x)&&Near(actual.y,expected.y)&&Near(actual.z,expected.z)&&Near(actual.w,expected.w),"all UV components interpolate");
                }
            }
        }
        Check(square.vertices.Select((v,i)=>Near((v-originalPositions[i]).sqrMagnitude,0)).All(v=>v)&&square.GetTriangles(0).SequenceEqual(originalTriangles),"source mesh geometry unchanged");
        Check(!square.destroyed&&!square.recalculatedBounds&&!square.recalculatedNormals&&square.indexFormat==UnityEngine.Rendering.IndexFormat.UInt16,"source ownership/flags unchanged");
        scenarios.Add("two material submeshes, eight UV channels, normals, tangents, colors, winding and immutable input");
        Partition(square,new Vector3(.3f,0,.3f),new Vector3(3,4,5),"oblique non-normalized plane");
        Partition(square,new Vector3(0,0,1),new Vector3(0,0,1),"plane through edge");
        Partition(square,new Vector3(0,0,2),new Vector3(0,0,1),"all geometry behind plane");
        var vertexCut=Make(new[]{new Vector3(-1,0,0),new Vector3(1,0,1),new Vector3(1,0,-1)},new[]{0,1,2});
        Partition(vertexCut,Vector3.zero,new Vector3(0,0,1),"plane through vertex");
        var coplanar=Make(new[]{Vector3.zero,new Vector3(1,0,0),new Vector3(0,1,0)},new[]{0,1,2});
        foreach(bool front in new[]{true,false})Check(Near(Area(ArticulatedFrameClipper.Clip(coplanar,Vector3.zero,new Vector3(0,0,1),front)),.5f),"coplanar triangles retained on each requested half");
        var degenerate=Make(new[]{Vector3.zero,Vector3.zero,Vector3.zero},new[]{0,1,2});
        Check(ArticulatedFrameClipper.Clip(degenerate,Vector3.zero,new Vector3(0,0,1),true).vertices.Length==0,"degenerate input skipped");
        scenarios.Add("center/oblique/outside cuts, exact plane vertex/edge, coplanar faces and degenerate input");
        var random=new Random(58173);
        for(int i=0;i<200;i++)
        {
            Vector3 V()=>new Vector3((float)random.NextDouble()*4-2,(float)random.NextDouble()*4-2,(float)random.NextDouble()*4-2);
            var triangle=Make(new[]{V(),V(),V()},new[]{0,1,2});
            Partition(triangle,V()*.3f,V(),"random partition "+i);
        }
        scenarios.Add("200 deterministic arbitrary 3D triangle/plane partitions with area/halfspace/winding checks");
        var noAttrs=ArticulatedFrameClipper.Clip(vertexCut,Vector3.zero,new Vector3(0,0,1),true);
        Check(noAttrs.recalculatedNormals&&noAttrs.normals.Length==noAttrs.vertices.Length,"missing normals request native recalculation");
        Check(noAttrs.tangents.Length==0&&noAttrs.colors.Length==0&&Enumerable.Range(0,8).All(c=>noAttrs.Uv(c).Length==0),"absent optional channels stay absent");
        var large=Make(new[]{new Vector3(0,0,1),new Vector3(1,0,1),new Vector3(0,1,1)},Enumerable.Range(0,23000).SelectMany(_=>new[]{0,1,2}).ToArray());
        var largeResult=ArticulatedFrameClipper.Clip(large,Vector3.zero,new Vector3(0,0,1),true);
        Check(largeResult.vertices.Length==69000&&largeResult.indexFormat==UnityEngine.Rendering.IndexFormat.UInt32,"large result selects 32-bit indices");
        Throws<InvalidOperationException>(()=>ArticulatedFrameClipper.Clip(null,Vector3.zero,new Vector3(0,0,1),true),"null mesh fails clearly");
        var invalid=Make(vertexCut.vertices,new[]{0,1,2});invalid.isReadable=false;
        Throws<InvalidOperationException>(()=>ArticulatedFrameClipper.Clip(invalid,Vector3.zero,new Vector3(0,0,1),true),"unreadable mesh fails clearly");invalid.isReadable=true;
        Throws<ArgumentException>(()=>ArticulatedFrameClipper.Clip(invalid,Vector3.zero,Vector3.zero,true),"zero plane normal fails clearly");
        invalid.normals=new[]{Vector3.zero};Throws<InvalidOperationException>(()=>ArticulatedFrameClipper.Clip(invalid,Vector3.zero,new Vector3(0,0,1),true),"malformed normals rejected");invalid.normals=Array.Empty<Vector3>();
        invalid.SetUVs(7,new List<Vector4>{Vector4.zero});Throws<InvalidOperationException>(()=>ArticulatedFrameClipper.Clip(invalid,Vector3.zero,new Vector3(0,0,1),true),"malformed UV channel rejected");invalid.SetUVs(7,new List<Vector4>());
        invalid.blendShapeCount=1;Throws<InvalidOperationException>(()=>ArticulatedFrameClipper.Clip(invalid,Vector3.zero,new Vector3(0,0,1),true),"unsupported blendshape rejected");invalid.blendShapeCount=0;
        invalid.boneWeights=new[]{new BoneWeight()};Throws<InvalidOperationException>(()=>ArticulatedFrameClipper.Clip(invalid,Vector3.zero,new Vector3(0,0,1),true),"unsupported skinning rejected");invalid.boneWeights=Array.Empty<BoneWeight>();
        invalid.SetTopology(0,MeshTopology.Lines);Throws<InvalidOperationException>(()=>ArticulatedFrameClipper.Clip(invalid,Vector3.zero,new Vector3(0,0,1),true),"non-triangle topology rejected");invalid.SetTopology(0,MeshTopology.Triangles);
        invalid.SetTriangles(new List<int>{0,1},0);Throws<InvalidOperationException>(()=>ArticulatedFrameClipper.Clip(invalid,Vector3.zero,new Vector3(0,0,1),true),"incomplete triangle rejected");
        invalid.SetTriangles(new List<int>{0,1,9},0);Throws<InvalidOperationException>(()=>ArticulatedFrameClipper.Clip(invalid,Vector3.zero,new Vector3(0,0,1),true),"out-of-range index rejected");
        scenarios.Add("missing optional channels, 69k vertex result, null/unreadable/invalid input rejection");
        var report=new{passed=true,checks,partitions,scenarios,validation="Extracted actual ArticulatedFrameClipper source with lightweight Unity vector/mesh stand-ins. No Unity runtime, live asset rendering, collision, or curve clearance tested."};
        File.WriteAllText(args[0],JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("PASS: "+checks+" assertions across "+partitions+" geometric partitions.");
    }
}
