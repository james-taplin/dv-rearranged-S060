using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace RearrangedS060
{
    // Own readable per-car mesh copies; never ship or mutate native geometry.
    // The native body is split into its connected pieces. Small pieces in the relief
    // are removed whole, so nothing (ladder, reverser, spring) is left half cut. Only
    // large structural plates are trimmed, and the trimmed edges are closed by frame
    // sills, a belly plate and a lined engine pocket that sample the body's own paint.
    internal sealed class S060ShayBody : IDisposable
    {
        private readonly List<(MeshFilter filter,Mesh original,Mesh copy)> owned=new List<(MeshFilter,Mesh,Mesh)>();
        private struct Vertex { internal Vector3 p; internal Vector2 uv; internal Vertex(Vector3 p,Vector2 uv){this.p=p;this.uv=uv;} }
        internal enum Piece { Keep, Remove, Trim }
        // Fit contract R2 reliefs (body metres, +X right, +Z smokebox). The pony-wheel trucks
        // sweep to 0.85-1.01 m, so native structure stays down to 1.085 m (R1: 1.285 m).
        internal static readonly Bounds Bay=FromTo(new Vector3(-1.5f,-.1f,-2.68f),new Vector3(1.5f,1.085f,2.94f));
        internal static readonly Bounds Notch=FromTo(new Vector3(.75f,1.0f,-.65f),new Vector3(1.55f,2.50f,.74f));
        private static readonly Bounds[] Protected={FromTo(new Vector3(.72f,1.285f,-2.7f),new Vector3(1.65f,3.37f,-1.3f)),FromTo(new Vector3(.72f,.2f,-3.65f),new Vector3(1.65f,1.285f,-2.82f))};
        // Outside reverser reach rod and the conventional cylinder blocks: always whole.
        private static readonly Bounds Reverser=FromTo(new Vector3(.25f,1.40f,-.52f),new Vector3(1.30f,1.66f,-.05f));
        private static readonly Bounds Cylinder=FromTo(new Vector3(.70f,.30f,2.55f),new Vector3(1.40f,1.60f,3.75f));
        // Trimmed plates also lose the saddle foot left in front of the bay once the cylinders go.
        internal static readonly Bounds Apron=FromTo(new Vector3(-.85f,-.1f,2.94f),new Vector3(.85f,1.085f,3.55f));
        private const float TrimReach=.25f, TrimArea=.3f;
        private const float SillX=.86f, SillBottom=.97f, DeckUnderside=1.62f;
        // Off-side oil cups stand on shelves on the sill's outer face, over each truck pivot.
        internal const float SillCupX=-1.04f, SillCupShelf=1.07f;
        internal static readonly float[] SillCupZ={-1.40f,.925f};
        internal static Bounds FromTo(Vector3 min,Vector3 max){var b=new Bounds();b.SetMinMax(min,max);return b;}

        internal static S060ShayBody Apply(Transform body)
        {
            var owner=new S060ShayBody();
            try
            {
                foreach(var f in body.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name=="s060_body"&&f.sharedMesh!=null&&f.sharedMesh.isReadable))
                {
                    var source=f.sharedMesh;var sourceVertices=source.vertices;var sourceUv=source.uv;
                    var uv=sourceUv.Length==sourceVertices.Length?sourceUv:new Vector2[sourceVertices.Length];
                    var local=sourceVertices.Select(p=>body.InverseTransformPoint(f.transform.TransformPoint(p))).ToArray();
                    var triangles=Enumerable.Range(0,source.subMeshCount).Select(s=>source.GetTriangles(s)).ToArray();
                    var island=Islands(local,triangles,out int count);
                    var pieces=Classify(local,triangles,island,count);
                    var vertices=new List<Vector3>();var coords=new List<Vector2>();var submeshes=new List<List<int>>();
                    Vector3 ToFilter(Vector3 p)=>f.transform.InverseTransformPoint(body.TransformPoint(p));
                    for(int sub=0;sub<triangles.Length;sub++)
                    {
                        var indices=triangles[sub];var output=new List<int>();
                        for(int i=0;i<indices.Length;i+=3)
                        {
                            var piece=pieces[island[indices[i]]];
                            if(piece==Piece.Remove) continue;
                            var parts=new List<List<Vertex>> { Enumerable.Range(0,3).Select(j=>new Vertex(local[indices[i+j]],uv[indices[i+j]])).ToList() };
                            if(piece==Piece.Trim) foreach(var box in new[]{Bay,Apron,Notch}) parts=parts.SelectMany(p=>Subtract(p,box)).ToList();
                            foreach(var poly in parts)
                            {
                                int start=vertices.Count;foreach(var v in poly){vertices.Add(ToFilter(v.p));coords.Add(v.uv);}
                                for(int j=1;j<poly.Count-1;j++){output.Add(start);output.Add(start+j);output.Add(start+j+1);}
                            }
                        }
                        submeshes.Add(output);
                    }
                    var deck=PaintSample(local,uv,triangles,c=>c.n.y<-.9f&&c.p.y>1.55f&&c.p.y<1.70f&&Mathf.Abs(c.p.x)<1.3f);
                    var tank=PaintSample(local,uv,triangles,c=>Mathf.Abs(c.n.x)>.9f&&c.p.x>1.1f&&c.p.y>1.8f&&c.p.y<2.5f);
                    void Quad(int sub,Vector2 paint,Vector3 normal,params Vector3[] corners)
                    {
                        // Front faces wind clockwise as seen along -normal, in the filter's own space.
                        var q=corners.Select(ToFilter).ToArray();var facing=f.transform.InverseTransformDirection(body.TransformDirection(normal));
                        if(Vector3.Dot(Vector3.Cross(q[1]-q[0],q[2]-q[0]),facing)<0) q=new[]{q[0],q[3],q[2],q[1]};
                        int start=vertices.Count;vertices.AddRange(q);coords.AddRange(Enumerable.Repeat(paint,4));
                        submeshes[sub].AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
                    }
                    // Belly plate under the trimmed native frame/firebox (the frame plates themselves
                    // now close the sides down to 1.085 m, so R1's side sills are gone).
                    float zf=Apron.max.z,zr=Bay.min.z,yb=Bay.max.y;
                    Quad(deck.sub,deck.uv,Vector3.down,new Vector3(-SillX,yb,zr),new Vector3(SillX,yb,zr),new Vector3(SillX,yb,zf),new Vector3(-SillX,yb,zf));
                    Quad(deck.sub,deck.uv,Vector3.forward,new Vector3(-SillX,yb,zf),new Vector3(SillX,yb,zf),new Vector3(SillX,yb+.12f,zf),new Vector3(-SillX,yb+.12f,zf));
                    // Deep frame sills fill the band between the belly and the swinging trucks (truck sweep
                    // tops out at 0.85-0.87 m at |x| 0.62-0.86): channel-section, riveted, broken on the
                    // right where the engine passes through the frame.
                    void Solid(Vector3 lo,Vector3 hi)
                    {
                        Vector3 P(int x,int y,int z)=>new Vector3(x==0?lo.x:hi.x,y==0?lo.y:hi.y,z==0?lo.z:hi.z);
                        Quad(deck.sub,deck.uv,Vector3.left,P(0,0,0),P(0,1,0),P(0,1,1),P(0,0,1));Quad(deck.sub,deck.uv,Vector3.right,P(1,0,0),P(1,0,1),P(1,1,1),P(1,1,0));
                        Quad(deck.sub,deck.uv,Vector3.down,P(0,0,0),P(0,0,1),P(1,0,1),P(1,0,0));Quad(deck.sub,deck.uv,Vector3.up,P(0,1,0),P(1,1,0),P(1,1,1),P(0,1,1));
                        Quad(deck.sub,deck.uv,Vector3.back,P(0,0,0),P(1,0,0),P(1,1,0),P(0,1,0));Quad(deck.sub,deck.uv,Vector3.forward,P(0,0,1),P(0,1,1),P(1,1,1),P(1,0,1));
                    }
                    // The sills run on to the front buffer beam (the Apron cut left it hanging), and are
                    // closed into box girders, so close up there is frame behind them, not a skin.
                    foreach(float s in new[]{-1f,1f})
                        foreach(var (sa,sb) in s>0?new[]{(zr,Notch.min.z-.04f),(Notch.max.z+.04f,zf)}:new[]{(zr,zf)})
                        {
                            float outer=s*SillX,web=s*(SillX-.02f),flange=s*(SillX-.08f),inner=s*(SillX-.06f);
                            Solid(new Vector3(Mathf.Min(outer,web),SillBottom,sa),new Vector3(Mathf.Max(outer,web),yb,sb));            // web
                            Solid(new Vector3(Mathf.Min(outer,flange),SillBottom,sa),new Vector3(Mathf.Max(outer,flange),SillBottom+.025f,sb)); // bottom flange
                            Solid(new Vector3(Mathf.Min(flange,inner),SillBottom,sa),new Vector3(Mathf.Max(flange,inner),yb,sb));       // inner web
                            for(float z=sa+.08f;z<sb-.04f;z+=.15f)
                                foreach(float y in new[]{SillBottom+.05f,yb-.04f})
                                    Solid(new Vector3(outer+(s>0?0:-.008f),y-.01f,z-.01f),new Vector3(outer+(s>0?.008f:0),y+.01f,z+.01f));
                        }
                    // Front headstock tying both sills to the buffer beam; the front truck sweeps no
                    // higher than 0.45 m beyond z 3.0.
                    Solid(new Vector3(-SillX,SillBottom,zf-.12f),new Vector3(SillX,yb,zf));
                    // Deep cross stretcher between the trucks, where neither truck rises above 0.6 m.
                    Solid(new Vector3(-(SillX-.02f),.66f,-.20f),new Vector3(SillX-.08f,yb,.20f));
                    // Off-side oil cup shelves with a web below, on the sill's outer face.
                    foreach(float z in SillCupZ)
                    {
                        Solid(new Vector3(SillCupX-.07f,SillCupShelf-.025f,z-.07f),new Vector3(-SillX,SillCupShelf,z+.07f));
                        Solid(new Vector3(SillCupX-.04f,SillBottom+.02f,z-.008f),new Vector3(-SillX,SillCupShelf-.025f,z+.008f));
                    }
                    // Engine pocket in the right tank, open to the side, painted like the tank.
                    float x0=Notch.min.x,x1=1.31f,y0=DeckUnderside,y1=Notch.max.y,z0=Notch.min.z,z1=Notch.max.z;
                    Quad(tank.sub,tank.uv,Vector3.right,new Vector3(x0,y0,z0),new Vector3(x0,y1,z0),new Vector3(x0,y1,z1),new Vector3(x0,y0,z1));
                    Quad(tank.sub,tank.uv,Vector3.forward,new Vector3(x0,y0,z0),new Vector3(x1,y0,z0),new Vector3(x1,y1,z0),new Vector3(x0,y1,z0));
                    Quad(tank.sub,tank.uv,Vector3.back,new Vector3(x0,y0,z1),new Vector3(x1,y0,z1),new Vector3(x1,y1,z1),new Vector3(x0,y1,z1));
                    Quad(tank.sub,tank.uv,Vector3.down,new Vector3(x0,y1,z0),new Vector3(x1,y1,z0),new Vector3(x1,y1,z1),new Vector3(x0,y1,z1));
                    var copy=new Mesh {name=source.name+" (Shay relief)", indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
                    owner.owned.Add((f,source,copy));copy.SetVertices(vertices);copy.SetUVs(0,coords);copy.subMeshCount=submeshes.Count;
                    for(int sub=0;sub<submeshes.Count;sub++)copy.SetTriangles(submeshes[sub],sub);
                    copy.RecalculateNormals();copy.RecalculateBounds();copy.RecalculateTangents();f.sharedMesh=copy;
                }
                if(owner.owned.Count==0)throw new InvalidOperationException("Readable split S060 body unavailable for the measured Shay relief.");
                return owner;
            }
            catch{owner.Dispose();throw;}
        }

        // Connected pieces, welding UV-seam duplicates by position.
        internal static int[] Islands(Vector3[] local,int[][] triangles,out int count)
        {
            var weld=new Dictionary<Vector3Int,int>();var root=new int[local.Length];
            for(int i=0;i<local.Length;i++)
            {
                var key=Vector3Int.RoundToInt(local[i]*10000);
                if(!weld.TryGetValue(key,out root[i])){root[i]=i;weld[key]=i;}
            }
            var parent=Enumerable.Range(0,local.Length).ToArray();
            int Find(int a){while(parent[a]!=a){parent[a]=parent[parent[a]];a=parent[a];}return a;}
            foreach(var indices in triangles)
                for(int i=0;i<indices.Length;i+=3)
                {
                    int a=Find(root[indices[i]]);
                    parent[Find(root[indices[i+1]])]=a;parent[Find(root[indices[i+2]])]=a;
                }
            var ids=new Dictionary<int,int>();var island=new int[local.Length];
            for(int i=0;i<local.Length;i++){int r=Find(root[i]);if(!ids.TryGetValue(r,out int id))ids[r]=id=ids.Count;island[i]=id;}
            count=ids.Count;return island;
        }

        internal static Piece[] Classify(Vector3[] local,int[][] triangles,int[] island,int count)
        {
            var bounds=new Bounds?[count];var area=new float[count];
            foreach(var indices in triangles)
                for(int i=0;i<indices.Length;i+=3)
                {
                    int id=island[indices[i]];Vector3 a=local[indices[i]],b=local[indices[i+1]],c=local[indices[i+2]];
                    var box=bounds[id]??new Bounds(a,Vector3.zero);box.Encapsulate(a);box.Encapsulate(b);box.Encapsulate(c);bounds[id]=box;
                    area[id]+=Vector3.Cross(b-a,c-a).magnitude/2;
                }
            var pieces=new Piece[count];
            for(int id=0;id<count;id++) if(bounds[id].HasValue) pieces[id]=Classify(bounds[id].Value,area[id]);
            return pieces;
        }

        internal static Piece Classify(Bounds piece,float area)
        {
            // Whole obsolete conventional springs, their loose adjustment parts,
            // and the outside expansion links have no axle beneath them in Shay.
            var springs=FromTo(new Vector3(.20f,1.11f,-.65f),new Vector3(.74f,1.54f,2.66f));
            var link=FromTo(new Vector3(.83f,1.085f,.72f),new Vector3(1.20f,1.635f,1.34f));
            var guides=FromTo(new Vector3(1.03f,.86f,1.10f),new Vector3(1.075f,1.11f,2.74f));
            if(new[]{springs,link,guides}.Any(b=>Inside(piece,b)||Inside(piece,Mirror(b))))return Piece.Remove;
            bool prot=Protected.Any(p=>Overlaps(piece,p)||Overlaps(piece,Mirror(p)));
            if(Inside(piece,Reverser)||Inside(piece,Cylinder)||Inside(piece,Mirror(Cylinder))) return Piece.Remove;
            var result=Piece.Keep;
            foreach(var relief in new[]{Bay,Notch})
            {
                if(!Overlaps(piece,relief)) continue;
                if(prot && relief==Bay && !Inside(piece,relief)) return Piece.Keep;
                if(Inside(piece,relief)||Reach(piece,relief)<=TrimReach||area<TrimArea) return Piece.Remove;
                result=Piece.Trim;
            }
            return result;
        }
        private static bool Overlaps(Bounds a,Bounds b){for(int i=0;i<3;i++)if(a.min[i]>=b.max[i]||a.max[i]<=b.min[i])return false;return true;}
        private static bool Inside(Bounds a,Bounds b){for(int i=0;i<3;i++)if(a.min[i]<b.min[i]-1e-4f||a.max[i]>b.max[i]+1e-4f)return false;return true;}
        private static float Reach(Bounds a,Bounds b){float r=0;for(int i=0;i<3;i++)r=Mathf.Max(r,Mathf.Max(b.min[i]-a.min[i],a.max[i]-b.max[i]));return r;}
        private static Bounds Mirror(Bounds b)=>FromTo(new Vector3(-b.max.x,b.min.y,b.min.z),new Vector3(-b.min.x,b.max.y,b.max.z));

        // UV at the centre of the largest matching face, so closures carry the live livery's own paint.
        private static (int sub,Vector2 uv) PaintSample(Vector3[] local,Vector2[] uv,int[][] triangles,Func<(Vector3 p,Vector3 n),bool> match)
        {
            float best=-1;(int,Vector2) found=(0,Vector2.zero);
            for(int sub=0;sub<triangles.Length;sub++)
                for(int i=0;i<triangles[sub].Length;i+=3)
                {
                    int a=triangles[sub][i],b=triangles[sub][i+1],c=triangles[sub][i+2];
                    var cross=Vector3.Cross(local[b]-local[a],local[c]-local[a]);float size=cross.magnitude;
                    if(size<=best||size==0||!match(((local[a]+local[b]+local[c])/3,cross/size))) continue;
                    best=size;found=(sub,(uv[a]+uv[b]+uv[c])/3);
                }
            if(best<0) throw new InvalidOperationException("Native S060 paint sample for the Shay closures is missing.");
            return found;
        }

        private static IEnumerable<List<Vertex>> Subtract(List<Vertex> polygon,Bounds box)
        {
            var pending=polygon;
            for(int side=0;side<6&&pending.Count>=3;side++)
            {
                int axis=side/2;bool lower=side%2==0;float edge=lower?box.min[axis]:box.max[axis];
                var inside=new List<Vertex>();var outside=new List<Vertex>();
                float Distance(Vertex v)=>(lower?1:-1)*(v.p[axis]-edge);
                for(int i=0;i<pending.Count;i++)
                {
                    var a=pending[i];var b=pending[(i+1)%pending.Count];float da=Distance(a),db=Distance(b);(da>=0?inside:outside).Add(a);
                    if((da>=0)!=(db>=0)){float t=da/(da-db);var cut=new Vertex(Vector3.LerpUnclamped(a.p,b.p,t),Vector2.LerpUnclamped(a.uv,b.uv,t));inside.Add(cut);outside.Add(cut);}
                }
                if(outside.Count>=3)yield return outside;pending=inside;
            }
        }
        public void Dispose(){foreach(var m in owned){if(m.filter!=null&&m.filter.sharedMesh==m.copy)m.filter.sharedMesh=m.original;if(m.copy!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(m.copy);else UnityEngine.Object.DestroyImmediate(m.copy);}}owned.Clear();}
    }
}
