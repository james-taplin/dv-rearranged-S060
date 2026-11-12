using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RearrangedS060
{
    // Two substantial HP feeds. The paired external interstage pipes and S282
    // manifold castings are retired; the existing native steam circuit remains.
    internal sealed class S060MeyerInwardPipes : IDisposable
    {
        internal const float AdmissionX=1.56f, AdmissionY=1.44f, AdmissionCourseY=1.44f, AdmissionRadius=.085f;
        internal Transform BodyRoot { get; private set; }
        internal Transform FrontRoot { get; private set; }
        internal Transform RearRoot { get; private set; }
        private readonly List<GameObject> objects=new List<GameObject>();
        private readonly List<Mesh> meshes=new List<Mesh>();
        private readonly List<MeshRenderer> renderers=new List<MeshRenderer>();
        private readonly Dictionary<int,Transform> ends=new Dictionary<int,Transform>();
        private readonly Dictionary<int,Vector3> sockets=new Dictionary<int,Vector3>();
        private readonly Dictionary<int,S060MeyerExhaustPipe.Bend> bends=new Dictionary<int,S060MeyerExhaustPipe.Bend>();
        private MeshRenderer materialSource;private Material material;private bool disposed;
        internal Vector3 AdmissionSocket(int side)=>sockets[side];
        internal Transform FrontAdmission(int side)=>ends[side];
        internal static bool IsInward(Transform body,Transform front)=>front.Find("[Native S060 cylinders]")!=null && Vector3.Dot(body.forward,front.forward)<-.5f;
        internal static S060MeyerInwardPipes Bind(Transform body,Transform front,Transform rear,Transform shell,Vector3 leftBoiler,Vector3 rightBoiler,Vector3 leftStem,Vector3 rightStem)
        {
            var owner=new S060MeyerInwardPipes();
            try
            {
                owner.materialSource=shell.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="s060_body"&&r.sharedMaterial!=null).OrderByDescending(r=>r.enabled&&r.gameObject.activeInHierarchy).First();
                owner.material=owner.materialSource.sharedMaterial;
                owner.BodyRoot=owner.Group(body,"[Meyer HP steam feeds]");
                owner.FrontRoot=owner.Group(front,"[Meyer HP chest unions]");
                owner.RearRoot=owner.Group(rear,"[Meyer internal receiver datum]");
                foreach(int side in new[]{-1,1})
                {
                    var boiler=side<0?leftBoiler:rightBoiler;var end=side<0?leftStem:rightStem;
                    var socket=new Vector3(side*AdmissionX,AdmissionY,-.65f);owner.sockets.Add(side,socket);
                    owner.Render(owner.BodyRoot,"HP smokebox feed "+side,S060MeyerExhaustPipe.SmoothPathMesh(new[]{boiler,boiler+Vector3.down*.18f,
                        new Vector3(side*AdmissionX,1.92f,boiler.z),new Vector3(side*AdmissionX,AdmissionY,2.60f),socket},AdmissionRadius,.32f));
                    var marker=owner.Group(owner.FrontRoot,"[HP chest inlet "+side+"]");marker.localPosition=end;owner.ends.Add(side,marker);
                    owner.Render(owner.FrontRoot,"HP chest flange "+side,S060MeyerExhaustPipe.SmoothPathMesh(new[]{end-Vector3.right*(Mathf.Sign(end.x)*.055f),end},.112f));
                    var bend=new S060MeyerExhaustPipe.Bend(AdmissionRadius);bend.Part=owner.Render(owner.BodyRoot,"HP articulated bend "+side,bend.Mesh);owner.bends.Add(side,bend);
                }
                owner.UpdateGeometry();return owner;
            }
            catch{owner.Dispose();throw;}
        }
        internal void UpdateMaterial()
        {
            if(disposed||materialSource==null||materialSource.sharedMaterial==material)return;
            material=materialSource.sharedMaterial;foreach(var renderer in renderers)if(renderer!=null)renderer.sharedMaterial=material;
        }
        internal void UpdateGeometry()
        {
            if(disposed||BodyRoot==null)return;UpdateMaterial();
            for(int side=-1;side<=1;side+=2)
                bends[side].Move(sockets[side],BodyRoot.InverseTransformPoint(ends[side].position),Vector3.back,BodyRoot.InverseTransformDirection(FrontRoot.TransformDirection(Vector3.right*(-Mathf.Sign(ends[side].localPosition.x)))));
        }
        internal static Mesh TrimUpperCourse(Mesh source, float cutY, out Vector3 axis)
        {
            // Use the actual lower stem section, preserving complete lower
            // casting/transfer faces and each original material section.
            var vertices = source.vertices.ToList(); var uv = source.uv.ToList();
            bool hasUv = uv.Count == vertices.Count;
            var sections = new List<Vector3>(); var output = new List<int>[source.subMeshCount];
            for (int sub = 0; sub < output.Length; sub++)
            {
                output[sub] = new List<int>(); var old = source.GetTriangles(sub);
                for (int i = 0; i < old.Length; i += 3)
                {
                    var ids = new[] { old[i], old[i + 1], old[i + 2] };
                    if (ids.All(j => vertices[j].y <= cutY)) { output[sub].AddRange(ids); continue; }
                    var poly = new List<(Vector3 p, Vector2 uv)>();
                    for (int e = 0; e < 3; e++)
                    {
                        int ia = ids[e], ib = ids[(e + 1) % 3]; var a = vertices[ia]; var b = vertices[ib];
                        var auv = hasUv ? uv[ia] : Vector2.zero; var buv = hasUv ? uv[ib] : Vector2.zero;
                        if (a.y <= cutY) poly.Add((a, auv));
                        if ((a.y <= cutY) == (b.y <= cutY)) continue;
                        float t = (cutY - a.y) / (b.y - a.y); var p = Vector3.LerpUnclamped(a, b, t);
                        sections.Add(p); poly.Add((p, Vector2.LerpUnclamped(auv, buv, t)));
                    }
                    if (poly.Count < 3) continue;
                    int start = vertices.Count;
                    foreach (var item in poly) { vertices.Add(item.p); if (hasUv) uv.Add(item.uv); }
                    for (int j = 1; j + 1 < poly.Count; j++) output[sub].AddRange(new[] { start, start + j, start + j + 1 });
                }
            }
            var ring = sections.GroupBy(p => new Vector3(Mathf.Round(p.x * 100000), Mathf.Round(p.y * 100000), Mathf.Round(p.z * 100000))).Select(g => g.First()).ToArray();
            if (ring.Length < 6) throw new InvalidOperationException("Measured Meyer admission stem section is missing.");
            axis = new Vector3((ring.Min(p => p.x) + ring.Max(p => p.x)) / 2, cutY, (ring.Min(p => p.z) + ring.Max(p => p.z)) / 2);
            var centre = axis;
            ring = ring.OrderBy(p => Mathf.Atan2(p.z - centre.z, p.x - centre.x)).ToArray();
            for (int i = 0; i < ring.Length; i++)
            {
                var a = ring[i]; var b = ring[(i + 1) % ring.Length];
                if (Vector3.Cross(a - axis, b - axis).y < 0) { var swap = a; a = b; b = swap; }
                int start = vertices.Count; vertices.AddRange(new[] { axis, a, b }); output[0].AddRange(new[] { start, start + 1, start + 2 });
                if (hasUv) uv.AddRange(Enumerable.Repeat(new Vector2(.6868f, .8948f), 3));
            }
            var copy = Object.Instantiate(source);
            try
            {
                var used = output.SelectMany(t => t).Distinct().ToArray();
                var map = used.Select((old, current) => (old, current)).ToDictionary(v => v.old, v => v.current);
                copy.name = source.name + " (retained Meyer chest)"; copy.Clear(); copy.subMeshCount = output.Length;
                copy.SetVertices(used.Select(j => vertices[j]).ToList()); if (hasUv) copy.SetUVs(0, used.Select(j => uv[j]).ToList());
                for (int sub = 0; sub < output.Length; sub++) copy.SetTriangles(output[sub].Select(j => map[j]).ToArray(), sub);
                copy.RecalculateNormals(); if (hasUv) copy.RecalculateTangents();
                copy.RecalculateBounds();
                return copy;
            }
            catch { Release(copy); throw; }
        }
        private Transform Group(Transform parent,string name){var go=new GameObject(name);objects.Add(go);go.transform.SetParent(parent,false);return go.transform;}
        private Transform Render(Transform parent,string name,Mesh mesh){meshes.Add(mesh);mesh.name=name;var t=Group(parent,name);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=material;renderers.Add(r);return t;}
        public void Dispose(){if(disposed)return;disposed=true;foreach(var go in objects)if(go!=null){go.SetActive(false);Release(go);}foreach(var mesh in meshes)if(mesh!=null)Release(mesh);objects.Clear();meshes.Clear();renderers.Clear();bends.Clear();ends.Clear();sockets.Clear();}
        private static void Release(Object value){if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
    }
}
