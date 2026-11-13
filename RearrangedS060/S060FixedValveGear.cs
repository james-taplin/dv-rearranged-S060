using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

namespace RearrangedS060
{
    // The curved expansion-link casting and its small mountings are static
    // islands in s060_body, outside the moving/scaled DrivingMechanism objects.
    // Retire only those measured islands when the stock valve gear is displaced.
    internal sealed class S060FixedValveGear:IDisposable
    {
        private readonly List<(MeshFilter filter,Mesh original,Mesh copy)> owned=new List<(MeshFilter,Mesh,Mesh)>();
        internal static bool IsDisplacedLink(Bounds b)
        {
            float lo=Mathf.Min(Mathf.Abs(b.min.x),Mathf.Abs(b.max.x)),hi=Mathf.Max(Mathf.Abs(b.min.x),Mathf.Abs(b.max.x));
            return b.min.x*b.max.x>0&&lo>=.83f&&hi<=1.20f&&b.min.y>=.87f&&b.max.y<=1.64f&&b.min.z>=.72f&&b.max.z<=1.34f;
        }
        internal static S060FixedValveGear Apply(Transform body)
        {
            var owner=new S060FixedValveGear();
            try
            {
                foreach(var filter in body.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name=="s060_body"&&f.sharedMesh!=null&&f.sharedMesh.isReadable))
                {
                    var original=filter.sharedMesh;
                    var points=original.vertices.Select(p=>body.InverseTransformPoint(filter.transform.TransformPoint(p))).ToArray();
                    var triangles=Enumerable.Range(0,original.subMeshCount).Select(original.GetTriangles).ToArray();
                    var island=S060MeyerClearance.Islands(points,triangles,out var boxes);
                    var remove=boxes.Select(IsDisplacedLink).ToArray();
                    if(!remove.Any(v=>v))continue;
                    var copy=Object.Instantiate(original);copy.name=original.name+" (retired fixed valve gear)";
                    owner.owned.Add((filter,original,copy));
                    for(int sub=0;sub<triangles.Length;sub++)
                    {
                        var t=triangles[sub];var kept=new List<int>();
                        for(int i=0;i<t.Length;i+=3)if(!remove[island[t[i]]])kept.AddRange(new[]{t[i],t[i+1],t[i+2]});
                        copy.SetTriangles(kept,sub);
                    }
                    copy.RecalculateBounds();filter.sharedMesh=copy;
                }
                return owner;
            }
            catch{owner.Dispose();throw;}
        }
        public void Dispose()
        {
            foreach(var entry in owned)if(entry.filter!=null&&entry.filter.sharedMesh==entry.copy)entry.filter.sharedMesh=entry.original;
            ReleaseResources();
        }
        internal void ReleaseResources()
        {
            foreach(var entry in owned)if(entry.copy!=null){if(Application.isPlaying)Object.Destroy(entry.copy);else Object.DestroyImmediate(entry.copy);}
            owned.Clear();
        }
    }
}
