using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace RearrangedS060
{
    // Own readable per-car mesh copies; never ship or mutate native geometry.
    internal sealed class S060ShayBody : IDisposable
    {
        private readonly List<(MeshFilter filter,Mesh original,Mesh copy)> owned=new List<(MeshFilter,Mesh,Mesh)>();
        private struct Vertex { internal Vector3 p; internal Vector2 uv; internal Vertex(Vector3 p,Vector2 uv){this.p=p;this.uv=uv;} }
        internal static S060ShayBody Apply(Transform body)
        {
            var owner=new S060ShayBody();
            var boxes=new[]{new Bounds(new Vector3(0,.5925f,.14f),new Vector3(3,1.385f,5.92f)),new Bounds(new Vector3(1.22f,1.79f,.11f),new Vector3(.37f,1.58f,1.10f))};
            try
            {
                foreach(var f in body.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name=="s060_body"&&f.sharedMesh!=null&&f.sharedMesh.isReadable))
                {
                    var source=f.sharedMesh;var sourceVertices=source.vertices;var uv=source.uv;
                    var local=sourceVertices.Select(p=>body.InverseTransformPoint(f.transform.TransformPoint(p))).ToArray();
                    var vertices=new List<Vector3>();var coords=new List<Vector2>();var submeshes=new List<int[]>();
                    for(int sub=0;sub<source.subMeshCount;sub++)
                    {
                        var indices=source.GetTriangles(sub);var output=new List<int>();
                        for(int i=0;i<indices.Length;i+=3)
                        {
                            var pieces=new List<List<Vertex>> { Enumerable.Range(0,3).Select(j=>new Vertex(local[indices[i+j]],uv.Length==sourceVertices.Length?uv[indices[i+j]]:Vector2.zero)).ToList() };
                            foreach(var box in boxes) pieces=pieces.SelectMany(p=>Subtract(p,box)).ToList();
                            foreach(var poly in pieces)
                            {
                                int start=vertices.Count;foreach(var v in poly){vertices.Add(f.transform.InverseTransformPoint(body.TransformPoint(v.p)));coords.Add(v.uv);}
                                for(int j=1;j<poly.Count-1;j++){output.Add(start);output.Add(start+j);output.Add(start+j+1);}
                            }
                        }
                        submeshes.Add(output.ToArray());
                    }
                    var copy=new Mesh {name=source.name+" (Shay R1 relief)", indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
                    owner.owned.Add((f,source,copy));copy.SetVertices(vertices);copy.SetUVs(0,coords);copy.subMeshCount=submeshes.Count;
                    for(int sub=0;sub<submeshes.Count;sub++)copy.SetTriangles(submeshes[sub],sub);
                    copy.RecalculateNormals();copy.RecalculateBounds();copy.RecalculateTangents();f.sharedMesh=copy;
                }
                if(owner.owned.Count==0)throw new InvalidOperationException("Readable split S060 body unavailable for the measured Shay relief.");
                return owner;
            }
            catch{owner.Dispose();throw;}
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
