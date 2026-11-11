using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;
namespace RearrangedS060
{
    // Both retained native sleeves feed one admission riser; exhaust stays separate.
    internal sealed class S060ShayPlumbing:IDisposable
    {
        private S060BodyPipes branches;
        private readonly List<Mesh> meshes=new List<Mesh>(), fittings=new List<Mesh>();
        private readonly List<GameObject> objects=new List<GameObject>();
        private Transform root;
        private Material iron,steel;
        internal static S060ShayPlumbing Bind(Transform car,Transform shell,Transform root,Transform[] markers,Material material)
        {
            var owner=new S060ShayPlumbing{root=root};
            try
            {
                owner.branches=S060BodyPipes.Apply(shell,Vector3.zero,true,false);
                var template=root.Find("Engine").GetComponentsInChildren<MeshRenderer>(true).First(r=>r.sharedMaterial!=null).sharedMaterial;
                owner.iron=S060ShayFinish.Gunmetal(template);owner.steel=S060ShayFinish.Steel(template);
                Vector3 Marker(string name)=>car.InverseTransformPoint(markers.Single(t=>t.name==name).position);
                var inlet=Marker("STEAM_IN");var exhaust=Marker("EXHAUST_OUT");var smokebox=Marker("SMOKEBOX_RETURN");
                Vector3 Socket(int side)=>car.InverseTransformPoint(shell.TransformPoint(owner.branches.BoilerSocket(side)));
                var junction=new Vector3(1.48f,.88f,.15f);
                owner.Pipe("Shay right sleeve feed",.060f,new[]{Socket(1)-Vector3.up*.10f,new Vector3(.7052183f,1.74f,3.1679118f),new Vector3(1.48f,1.74f,3.1679118f),new Vector3(1.48f,1.35f,2.85f),new Vector3(1.48f,1.35f,.72f),new Vector3(1.48f,.88f,.60f),junction});
                owner.Pipe("Shay offside under-boiler feed",.060f,new[]{Socket(-1)-Vector3.up*.10f,new Vector3(-.7052183f,1.74f,3.1679118f),new Vector3(-1.48f,1.74f,3.1679118f),new Vector3(-1.48f,1.35f,2.85f),new Vector3(-1.48f,1.35f,.72f),new Vector3(-1.48f,.88f,.80f),new Vector3(.90f,.88f,.80f),new Vector3(1.48f,.88f,.80f),junction});
                owner.Pipe("Shay main admission riser",.070f,new[]{junction,new Vector3(1.48f,.88f,-.12f),new Vector3(1.48f,1.20f,-.48f),new Vector3(1.48f,inlet.y,-.48f),inlet});
                var normal=new Vector3(.88192f,.47140f,0);
                owner.Pipe("Shay single-chimney exhaust return",.070f,new[]{exhaust,new Vector3(1.64f,exhaust.y,exhaust.z),new Vector3(1.64f,2.72f,.96f),new Vector3(1.53f,2.72f,2.98f),new Vector3(1.53f,2.86f,3.45f),smokebox+normal*.24f,smokebox-normal*.015f});
                owner.Flange(inlet,Vector3.right,.095f,.026f);
                owner.Flange(exhaust,Vector3.right,.105f,.026f);
                owner.Flange(smokebox+normal*.004f,normal,.115f,.042f);
                foreach(int side in new[]{-1,1})
                {
                    owner.Sleeve(Socket(side),owner.branches.BoilerSocketRadius(side));
                    foreach(float z in new[]{1.12f,2.54f})
                    {
                        owner.Collared(new Vector3(side*1.48f,1.35f,z),Vector3.forward,.074f,.035f);
                        owner.AddFitting(S060MeyerExhaustPipe.PathMesh(new[]{new Vector3(side*1.30f,1.62f,z),new Vector3(side*1.48f,1.43f,z)},.014f,false));
                    }
                }
                owner.Collared(junction+Vector3.forward*.03f,Vector3.forward,.080f,.09f);
                owner.Flange(new Vector3(1.48f,1.83f,-.48f),Vector3.up,.103f,.026f);
                foreach(float z in new[]{1.48f,2.46f})
                {
                    float x=Mathf.Lerp(1.64f,1.53f,(z-.96f)/(2.98f-.96f));
                    owner.Collared(new Vector3(x,2.72f,z),Vector3.forward,.083f,.034f);
                    owner.AddFitting(S060MeyerExhaustPipe.PathMesh(new[]{new Vector3(1.30f,2.10f,z),new Vector3(x,2.64f,z)},.018f,false));
                }
                owner.CombineFittings();return owner;
            }
            catch{owner.Dispose();throw;}
        }
        private void Pipe(string name,float radius,Vector3[] points)
        {
            var centres=Rounded(points);var radii=Enumerable.Repeat(radius,centres.Length).ToArray();
            if(name.EndsWith("feed",StringComparison.Ordinal))
                for(int i=0;i<centres.Length;i++)radii[i]=Mathf.Lerp(.070f,radius,Mathf.Clamp01(Vector3.Distance(centres[i],points[points.Length-1])/.15f));
            Render(name,Hollow(centres,radii,radii.Select(r=>r-.008f).ToArray()),iron);
        }
        internal static Vector3[] Rounded(Vector3[] points)
        {
            var result=new List<Vector3>{points[0]};
            for(int i=1;i<points.Length-1;i++)
            {
                float cut=Mathf.Min(.28f,Mathf.Min(Vector3.Distance(points[i-1],points[i]),Vector3.Distance(points[i],points[i+1]))*.42f);
                var a=points[i]+(points[i-1]-points[i]).normalized*cut;var b=points[i]+(points[i+1]-points[i]).normalized*cut;
                for(int j=0;j<=8;j++){float t=j/8f;result.Add((1-t)*(1-t)*a+2*(1-t)*t*points[i]+t*t*b);}
            }
            result.Add(points[points.Length-1]);return result.ToArray();
        }
        private static Mesh Hollow(Vector3[] centres,float[] outer,float[] inner,int sides=24)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            var rights=new Vector3[centres.Length];var ups=new Vector3[centres.Length];
            Vector3 previousTangent=Vector3.forward,previousRight=Vector3.right;float distance=0;
            int stride=sides+1,layer=centres.Length*stride;
            for(int ring=0;ring<centres.Length;ring++)
            {
                var tangent=(centres[Math.Min(ring+1,centres.Length-1)]-centres[Math.Max(0,ring-1)]).normalized;
                var reference=Mathf.Abs(Vector3.Dot(tangent,Vector3.up))>.95f?Vector3.forward:Vector3.up;
                rights[ring]=ring==0?Vector3.Cross(reference,tangent).normalized:(Quaternion.FromToRotation(previousTangent,tangent)*previousRight).normalized;
                ups[ring]=Vector3.Cross(tangent,rights[ring]);previousTangent=tangent;previousRight=rights[ring];
            }
            for(int wall=0;wall<2;wall++)
            {
                distance=0;
                for(int ring=0;ring<centres.Length;ring++)
                {
                    if(ring>0)distance+=Vector3.Distance(centres[ring-1],centres[ring]);
                    float radius=wall==0?outer[ring]:inner[ring];
                    for(int side=0;side<=sides;side++)
                    {
                        float a=2*Mathf.PI*side/sides;
                        vertices.Add(centres[ring]+(rights[ring]*Mathf.Cos(a)+ups[ring]*Mathf.Sin(a))*radius);
                        uv.Add(new Vector2(a*radius*2,distance*2));
                        if(ring==0||side==sides)continue;
                        int c=wall*layer+ring*stride+side,a0=c-stride,b=a0+1,d=c+1;
                        if(wall==0)triangles.AddRange(new[]{a0,b,c,b,d,c});else triangles.AddRange(new[]{a0,c,b,b,c,d});
                    }
                }
            }
            // Separate annular lip vertices retain a crisp end face without
            // interrupting the smooth inner/outer walls or their metric UVs.
            foreach(int ring in new[]{0,centres.Length-1})
            {
                int start=vertices.Count;
                for(int side=0;side<=sides;side++)
                {
                    float a=2*Mathf.PI*side/sides;var radial=rights[ring]*Mathf.Cos(a)+ups[ring]*Mathf.Sin(a);
                    foreach(float r in new[]{outer[ring],inner[ring]}){vertices.Add(centres[ring]+radial*r);uv.Add(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r*2);}
                    if(side==sides)continue;
                    int o=start+side*2,i=o+1,n=o+2,j=o+3;
                    if(ring==0)triangles.AddRange(new[]{o,i,n,i,j,n});else triangles.AddRange(new[]{o,n,i,i,n,j});
                }
            }
            var mesh=new Mesh{vertices=vertices.ToArray(),uv=uv.ToArray(),triangles=triangles.ToArray()};mesh.RecalculateNormals();
            var normals=mesh.normals;
            for(int wall=0;wall<2;wall++)for(int ring=0;ring<centres.Length;ring++)
            {int a=wall*layer+ring*stride,b=a+sides;normals[a]=normals[b]=(normals[a]+normals[b]).normalized;}
            mesh.normals=normals;mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }
        private void Collared(Vector3 centre,Vector3 axis,float radius,float width)
            =>AddFitting(Hollow(new[]{centre-axis*width/2,centre+axis*width/2},new[]{radius,radius},new[]{radius<.078f?.059f:.069f,radius<.078f?.059f:.069f}));
        private void Sleeve(Vector3 socket,float nativeRadius)
        {
            // An open reducer over the actual cut sleeve, with a rolled lip and a
            // continuous inner wall. No cap closes the steam passage.
            float r=nativeRadius+.004f;
            var y=new[]{.010f,-.008f,-.030f,-.072f,-.095f,-.110f};
            var outer=new[]{r,r,r-.004f,.074f,.068f,.068f};
            var inner=outer.Select(v=>v-.008f).ToArray();
            Render("Shay hollow tapered sleeve "+(socket.x<0?"offside":"engine side"),Hollow(y.Select(v=>socket+Vector3.up*v).ToArray(),outer,inner,32),iron);
        }
        private void Flange(Vector3 centre,Vector3 axis,float radius,float width)
        {
            Collared(centre,axis,radius,width);
            Vector3 u=Vector3.Cross(axis,Mathf.Abs(axis.y)>.9f?Vector3.forward:Vector3.up).normalized,v=Vector3.Cross(axis,u);
            for(int i=0;i<8;i++)
            {float a=i*Mathf.PI/4;var p=centre+(u*Mathf.Cos(a)+v*Mathf.Sin(a))*(radius-.016f);AddFitting(S060MeyerExhaustPipe.Tube(new[]{p-axis*(width/2+.008f),p+axis*(width/2+.008f)},new[]{.008f,.008f}));}
        }
        private void AddFitting(Mesh mesh){meshes.Add(mesh);fittings.Add(mesh);}
        private void CombineFittings()
        {
            var mesh=new Mesh{name="Shay pipe flanges, studs and supports",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
            mesh.CombineMeshes(fittings.Select(m=>new CombineInstance{mesh=m,transform=Matrix4x4.identity}).ToArray());
            foreach(var m in fittings){meshes.Remove(m);Release(m);}fittings.Clear();Render(mesh.name,mesh,steel);
        }
        private void Render(string name,Mesh mesh,Material material)
        {
            meshes.Add(mesh);mesh.name=name;var part=new GameObject(name);objects.Add(part);part.transform.SetParent(root,false);
            part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        public void Dispose()
        {
            branches?.Dispose();branches=null;
            foreach(var part in objects)if(part!=null){part.SetActive(false);Release(part);}objects.Clear();
            foreach(var mesh in meshes)if(mesh!=null)Release(mesh);meshes.Clear();fittings.Clear();
        }
        private static void Release(Object o){if(Application.isPlaying)Object.Destroy(o);else Object.DestroyImmediate(o);}
    }
}
