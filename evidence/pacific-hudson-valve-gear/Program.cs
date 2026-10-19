using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RearrangedS282;
using UnityEngine;

internal static class Program
{
    static int checks,poseScenarios;
    static readonly List<string> scenarios=new();
    static void Check(bool value,string message){checks++;if(!value)throw new InvalidOperationException(message);}
    static bool Near(Vector3 a,Vector3 b,float epsilon=5e-5f)=>(a-b).magnitude<=epsilon;
    static Vector3 V(JsonElement v)=>new(v[0].GetSingle(),v[1].GetSingle(),v[2].GetSingle());
    static Quaternion Q(JsonElement v)=>new(v[0].GetSingle(),v[1].GetSingle(),v[2].GetSingle(),v[3].GetSingle());
    static void Throws(Action action,string message){bool threw=false;try{action();}catch(InvalidOperationException){threw=true;}Check(threw,message);}
    static Transform Path(Transform root,string path)
    {foreach(var segment in path.Split('/'))root=root.children.FirstOrDefault(c=>c.name==segment)??root.Add(segment);return root;}
    sealed class Saved
    {
        readonly Transform t;readonly Vector3 p,s;readonly Quaternion q;readonly bool active;readonly Mesh mesh;
        internal Saved(Transform t){this.t=t;p=t.localPosition;s=t.localScale;q=t.localRotation;active=t.gameObject.activeSelf;mesh=t.gameObject.filter?.sharedMesh;}
        internal void Restore(){t.localPosition=p;t.localScale=s;t.localRotation=q;t.gameObject.SetActive(active);if(t.gameObject.filter!=null)t.gameObject.filter.sharedMesh=mesh;}
    }
    static IEnumerable<Transform> All(Transform root){yield return root;foreach(var child in root.children)foreach(var t in All(child))yield return t;}
    sealed class Rig
    {
        internal readonly WheelRearranger w=new();
        internal readonly List<(Transform t,Matrix4x4 native)> mapped=new();
        internal readonly List<MeshFilter> pipes=new();
        internal readonly List<Saved> stock=new();
        internal Rig(JsonElement data)
        {
            w.transform=new GameObject("S282 fixture").transform;
            w.driveR=Path(w.transform,"LocoS282A_Body/MovingParts_LOD0/LocoS282A_Drivetrain R");
            w.driveL=Path(w.transform,"LocoS282A_Body/MovingParts_LOD0/LocoS282A_Drivetrain L");
            w.driveL.localScale=new(-1,1,1);
            foreach(var node in data.GetProperty("transforms").EnumerateArray())
            {
                string p=node.GetProperty("path").GetString();int start=p.IndexOf("/LocoS282A_Drivetrain R",StringComparison.Ordinal);
                string relative=p[(start+"/LocoS282A_Drivetrain R".Length)..].TrimStart('/');
                if(relative.Length==0)continue;
                foreach(var drive in new[]{w.driveL,w.driveR})
                {var t=Path(drive,relative);t.localPosition=V(node.GetProperty("position"));t.localRotation=Q(node.GetProperty("rotation"));t.localScale=V(node.GetProperty("scale"));}
            }
            w.lCrosshead=w.driveL.Find("s282_mech_push_rod");w.rCrosshead=w.driveR.Find("s282_mech_push_rod");
            for(int lod=0;lod<2;lod++)
            {
                var split=Path(w.transform,"LocoS282A_Body/Static_LOD"+lod+"/s282a_split_body"+(lod==0?"(Clone)":"_LOD1(Clone)"));
                split.localPosition=new(0,0,lod==0?4.88f:4.885f);
                foreach(string name in new[]{"s282a_cylinder_l","s282a_cylinder_r","s282a_cylinder_cocks","s282a_crosshead_guide_l","s282a_crosshead_guide_r","s282a_lifting_arm_support_l","s282a_lifting_arm_support_r"})
                {var t=split.Add(name);mapped.Add((t,t.localToWorldMatrix));}
                var reachSupport=split.Add("s282a_reach_rod_support");
                if(lod==0){w.lLiftingArmSupport=split.Find("s282a_lifting_arm_support_l");w.rLiftingArmSupport=split.Find("s282a_lifting_arm_support_r");w.reachRodSupport=reachSupport;}
                foreach(int side in new[]{-1,1})
                {
                    string nativeName="s282_locomotive_body"+(lod==1?"_LOD1":"")+(side<0?"_r":"_l");
                    var record=data.GetProperty("pipes").EnumerateArray().Single(p=>p.GetProperty("name").GetString()==nativeName);
                    var vertices=record.GetProperty("vertices").EnumerateArray().Select(V).ToArray();
                    pipes.Add(new MeshFilter(split.Add("s282a_dry_pipe_"+(side<0?"r":"l")),new Mesh{name=nativeName,vertices=vertices,uv=Enumerable.Repeat(.25f,vertices.Length).ToArray(),colors=new[]{.1f,.8f},tangents=new[]{1f,0f,0f,-1f},triangles=new[]{0,1,2,2,1,3}}));
                }
            }
            foreach(string path in new[]{"LocoS282A_Particles/CylCockSteam","LocoS282A_Particles/CylCockWaterDrip","LocoS282A_Particles/CylinderCrack","[colliders]/[walkable]/[old]/Exterior/cylinder","[colliders]/[walkable]/[old]/Exterior/cylinder (1)","[colliders]/[walkable]/[old]/Exterior/area_big_cylinder","[colliders]/[items]/[old]/Exterior/area_big_cylinder"})
            {var t=Path(w.transform,path);if(path.EndsWith("CylinderCrack"))t.localPosition=new(1.42f,1.04f,9.08f);mapped.Add((t,t.localToWorldMatrix));}
            foreach(var t in All(w.transform))stock.Add(new Saved(t));
            w.Prepare();
        }
        internal void Restore(){foreach(var p in stock)p.Restore();}
        internal void Legacy()
        {
            w.driveL.localScale=new(-1,1.2879f,1.2879f);w.driveR.localScale=new(1,1.2879f,1.2879f);
            w.driveL.localPosition=w.driveR.localPosition=new(0,0,-2.1f);
            foreach(var p in mapped.Where(p=>p.t.name.StartsWith("s282a_cylinder")||p.t.name.Contains("crosshead_guide")))p.t.localPosition=new(0,.21f,0);
            w.lLiftingArmSupport.gameObject.SetActive(false);w.rLiftingArmSupport.gameObject.SetActive(false);
            w.lCrosshead.localScale=w.rCrosshead.localScale=new(1,.78f,1);
        }
    }
    static void NativePose(Rig rig,JsonElement clip,JsonElement frame)
    {
        var dynamic=frame.GetProperty("values");
        foreach(var binding in clip.GetProperty("bindings").EnumerateArray())
        {
            string path=binding.GetProperty("path").GetString();var indices=binding.GetProperty("indices").EnumerateArray().Select(x=>x.GetInt32()).ToArray();
            var constants=binding.GetProperty("first").EnumerateArray().Select(x=>x.GetSingle()).ToArray();
            var values=indices.Select((i,k)=>dynamic.TryGetProperty(i.ToString(),out var value)?value.GetSingle():constants[k]).ToArray();
            foreach(var drive in new[]{rig.w.driveL,rig.w.driveR})
            {
                var target=drive.Find(path);Check(target!=null,"Native animation path remains available: "+path);
                switch(binding.GetProperty("attribute").GetInt32())
                {case 1:target.localPosition=new(values[0],values[1],values[2]);break;case 2:target.localRotation=new(values[0],values[1],values[2],values[3]);break;case 3:target.localScale=new(values[0],values[1],values[2]);break;}
            }
        }
    }
    static void CheckMapped(Rig rig,Matrix4x4 world)
    {
        var affine=Matrix4x4.TRS(new(0,0,-2.1f),Quaternion.identity,new(1,1.2879f,1.2879f));
        foreach(var part in rig.mapped)
        {
            // Particle outlets follow the mapped pivot only; emitters are never stretched.
            bool outlet=part.t.name=="CylCockSteam"||part.t.name=="CylCockWaterDrip"||part.t.name=="CylinderCrack";
            var pivot=(world*affine*part.native).MultiplyPoint3x4(Vector3.zero);
            foreach(var point in new[]{Vector3.zero,new Vector3(1.1f,.7f,3.8f),new Vector3(-1.1f,1.7f,4.6f)})
                if(outlet) Check(Near(part.t.localToWorldMatrix.MultiplyPoint3x4(point),pivot+world.MultiplyVector(part.native.MultiplyVector(point)),.0003f),"Particle outlet moves with the mapped pivot at native orientation and size");
                else Check(Near(part.t.localToWorldMatrix.MultiplyPoint3x4(point),(world*affine*part.native).MultiplyPoint3x4(point),.0003f),"Native support and cylinder shell use the identical animated-frame mapping");
        }
    }
    static void Main(string[] args)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(args[0]));var data=doc.RootElement;
        var strings=data.GetProperty("controller").GetProperty("strings").EnumerateArray().Select(x=>x[1].GetString()).ToArray();
        Check(strings.Contains("Cutoff")&&strings.Contains("SpeedMultiplier"),"Native controller has original Cutoff and SpeedMultiplier parameters");
        Check(data.GetProperty("clips").GetArrayLength()==3,"All three native cutoff clips represented");
        var rig=new Rig(data);int nativeCount=All(rig.w.transform).Count();var originals=rig.pipes.Select(f=>f.sharedMesh).ToArray();var originalVertices=originals.Select(m=>m.vertices.ToArray()).ToArray();
        foreach(var clip in data.GetProperty("clips").EnumerateArray())
        foreach(var frame in clip.GetProperty("frames").EnumerateArray())
        foreach(int layout in new[]{4,5})
        {
            rig.Restore();NativePose(rig,clip,frame);rig.Legacy();
            // The locomotive has moved and rotated since setup. A cached world
            // pose would fail these checks even though it worked at spawn.
            rig.w.transform.localPosition=new(127.25f,3.5f,-44.7f);
            rig.w.transform.localRotation=Quaternion.AngleAxis(73,Vector3.up);
            var world=rig.w.transform.localToWorldMatrix;
            var animated=All(rig.w.driveR).Select(t=>(t,position:t.localPosition,rotation:t.localRotation)).ToArray();
            rig.w.Apply();CheckMapped(rig,world);
            foreach(var t in animated)
            {Check(Near(t.t.localPosition,t.position),"Native animated position is untouched");Check(Near(new(t.t.localRotation.x,t.t.localRotation.y,t.t.localRotation.z),new(t.rotation.x,t.rotation.y,t.rotation.z))&&Math.Abs(t.t.localRotation.w-t.rotation.w)<1e-5,"Native animated rotation and phase are untouched");}
            Check(Near(rig.w.lCrosshead.localScale,Vector3.one)&&Near(rig.w.rCrosshead.localScale,Vector3.one),"Authored crosshead constant replaces legacy 0.78 compensation");
            Check(rig.w.lLiftingArmSupport.gameObject.activeSelf&&rig.w.rLiftingArmSupport.gameObject.activeSelf,"Both native lifting-arm bearings visible");
            Check(!rig.w.reachRodSupport.gameObject.activeSelf&&!rig.w.driveR.Find("LubricatorParts").gameObject.activeSelf&&!rig.w.driveR.Find("s282_mech_cutoff_rod").gameObject.activeSelf,"Cab reach rod and mechanical oiler stay excluded");
            Check(!rig.w.transform.Find("LocoS282A_Body/Static_LOD1/s282a_split_body_LOD1(Clone)/s282a_reach_rod_support").gameObject.activeSelf,"Unused reach-rod support is also hidden in body LOD1");
            var before=rig.mapped.Select(p=>p.t.localToWorldMatrix.MultiplyPoint3x4(new(1,2,3))).ToArray();
            rig.w.Apply();for(int i=0;i<before.Length;i++)Check(Near(before[i],rig.mapped[i].t.localToWorldMatrix.MultiplyPoint3x4(new(1,2,3))),"Repeated apply is absolute and does not accumulate offsets");
            Check(All(rig.w.transform).Count()==nativeCount,"No hierarchy or ownership changes");
            poseScenarios++;
        }
        scenarios.Add("96 native High/Med/Low sampled poses across Pacific/Hudson, with moving/rotated car, unchanged native animated phase and absolute support mapping");
        for(int i=0;i<rig.pipes.Count;i++)
        {
            var copy=rig.pipes[i].sharedMesh;var original=originals[i];
            Check(!ReferenceEquals(copy,original),"Each pipe has a per-car copy");
            Check(original.vertices.SequenceEqual(originalVertices[i]),"Native pipe asset remains immutable");
            Check(copy.uv.SequenceEqual(original.uv)&&copy.colors.SequenceEqual(original.colors)&&copy.triangles.SequenceEqual(original.triangles)&&copy.subMeshCount==original.subMeshCount,"Pipe topology, UVs, colors and submeshes preserved");
            Check(copy.boundsRecalculated&&copy.normalsRecalculated&&copy.tangentsRecalculated,"Corrected pipe recalculates geometry-dependent data");
            float low=original.vertices.Min(v=>v.y),high=original.vertices.Max(v=>v.y);
            var map=Matrix4x4.TRS(new(0,0,-2.1f),Quaternion.identity,new(1,1.2879f,1.2879f));
            // Actual copied mesh local space includes the LMS 4.88/4.885 body origin.
            var pipe=rig.pipes[i].transform;var nativeOffset=i<2?4.88f:4.885f;
            for(int k=0;k<original.vertices.Length;k++)
            {
                var v=original.vertices[k];var output=copy.vertices[k];
                if(Math.Abs(v.y-high)<1e-5)Check(Near(v,output),"Native highest-Y boiler-end vertices remain fixed (not a whole socket-ring assertion)");
                if(Math.Abs(v.y-low)<1e-5)Check(Near(output,map.MultiplyPoint3x4(v+new Vector3(0,0,nativeOffset))-new Vector3(0,0,nativeOffset)),"Native lower pipe endpoint follows cylinder exactly");
                Check(output.y>=low*1.2879f-1e-4&&output.y<=high+1e-4,"Native pipe remains inside ordered cylinder/boiler height interval");
            }
        }
        scenarios.Add("Four actual native dry-pipe vertex samples, fixed highest-Y/mapped lowest-Y endpoint vertices, immutable sources and preserved mesh attributes; full socket seating requires visual inspection");
        var copies=rig.pipes.Select(f=>f.sharedMesh).ToArray();rig.Restore();
        for(int i=0;i<rig.pipes.Count;i++)Check(ReferenceEquals(rig.pipes[i].sharedMesh,originals[i]),"Stock/pool snapshot restores native pipe sharedMesh");
        foreach(var p in rig.mapped)Check(Near(p.t.localToWorldMatrix.MultiplyPoint3x4(new(1,2,3)),p.native.MultiplyPoint3x4(new(1,2,3))),"Stock restores original support/outlet/collider pose");
        rig.Legacy();rig.w.Apply();for(int i=0;i<copies.Length;i++)Check(ReferenceEquals(rig.pipes[i].sharedMesh,copies[i]),"Reused loco reuses only its own cached corrected mesh");
        rig.w.Dispose();for(int i=0;i<copies.Length;i++){Check(copies[i].destroyed,"Dispose destroys owned corrected mesh");Check(!originals[i].destroyed&&ReferenceEquals(rig.pipes[i].sharedMesh,originals[i]),"Dispose preserves and restores original mesh");}rig.w.Dispose();
        scenarios.Add("Actual partial setup/apply, repeated switch, stock/pool mesh/pose restore, reuse and idempotent disposal");
        var missing=new Rig(data);missing.w.lLiftingArmSupport.parent.children.Remove(missing.w.lLiftingArmSupport);missing.w.Prepare();Throws(missing.w.Apply,"Missing optional support defers failure until polished arrangement requested");
        var unreadable=new Rig(data);unreadable.pipes[0].sharedMesh.isReadable=false;Throws(()=>{unreadable.Legacy();unreadable.w.Apply();},"Unreadable optional pipe rejects polished arrangement");unreadable.Restore();unreadable.w.Dispose();
        Throws(()=>PacificHudsonAffine.BendPipe(Array.Empty<Vector3>(),Matrix4x4.identity,Matrix4x4.identity),"Empty pipe rejected");
        Throws(()=>PacificHudsonAffine.BendPipe(new[]{Vector3.zero,Vector3.right,Vector3.forward},Matrix4x4.identity,Matrix4x4.identity),"Pipe without height separation rejected");
        Throws(()=>PacificHudsonAffine.BendPipe(new[]{Vector3.zero,new Vector3(0,float.NaN,0),Vector3.up},Matrix4x4.identity,Matrix4x4.identity),"Nonfinite mesh rejected");
        foreach(float sign in new[]{-1f,1f})foreach(float angle in new[]{0f,90f,180f,273f})
        {
            var scale=new Vector3(sign,1.2879f,1.2879f);var matrix=Matrix4x4.TRS(new(1,2,3),Quaternion.AngleAxis(angle,Vector3.right),scale);
            PacificHudsonAffine.Decompose(matrix,scale,out var p,out var q,out var s);var roundtrip=Matrix4x4.TRS(p,q,s);
            foreach(var point in new[]{Vector3.zero,Vector3.right,Vector3.up,Vector3.forward,new Vector3(-1,2,4)})Check(Near(matrix.MultiplyPoint3x4(point),roundtrip.MultiplyPoint3x4(point)),"Signed native frame round trip preserves handedness and X-axis orientation");
        }
        Throws(()=>PacificHudsonAffine.Decompose(Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new(0,1,1)),Vector3.one,out _,out _,out _),"Singular support rejected");
        var shear=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new(1,2,3))*Matrix4x4.TRS(Vector3.zero,Quaternion.AngleAxis(45,Vector3.up),Vector3.one);
        Throws(()=>PacificHudsonAffine.Decompose(shear,Vector3.one,out _,out _,out _),"Incompatible native shear rejected");
        Check(!PacificHudsonAffine.TryDecompose(shear,Vector3.one,out _,out _,out _),"Sheared mapping reported for move-only fallback");
        Check(PacificHudsonAffine.TryDecompose(Matrix4x4.TRS(new(1,2,3),Quaternion.AngleAxis(30,Vector3.right),new(1,1.2879f,1.2879f)),Vector3.one,out _,out _,out _),"X-axis rotated support still maps fully");
        bool singularThrows=false;try{PacificHudsonAffine.TryDecompose(Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new(0,1,1)),Vector3.one,out _,out _,out _);}catch(InvalidOperationException){singularThrows=true;}
        Check(singularThrows,"Singular support still rejected by TryDecompose");
        scenarios.Add("Missing/unreadable optional assets, invalid geometry, signed support decomposition and shear rejection");
        File.WriteAllText(args[1],JsonSerializer.Serialize(new{status="PASS",checks,nativePoseScenarios=poseScenarios,nativeCutoffClips=3,nativeDryPipeVertexSamples=data.GetProperty("pipes").EnumerateArray().Sum(p=>p.GetProperty("vertices").GetArrayLength()),scenarios,limits="Managed stand-ins and actual native clip/mesh samples. Unity rendering, material appearance, visual clearance, live cutoff/reverse, oil-cup interaction, save/reload and game acceptance are not exercised."},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"PASS {checks} checks; {poseScenarios} native cutoff poses; 4 native pipe samples");
    }
}
