using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace RearrangedS060
{
    internal sealed class S060ShayPlumbing:IDisposable
    {
        private S060BodyPipes branches;
        private readonly List<Mesh> meshes=new List<Mesh>();
        internal static S060ShayPlumbing Bind(Transform car,Transform shell,Transform root,Transform[] markers,Material material)
        {
            var owner=new S060ShayPlumbing();
            try
            {
                owner.branches=S060BodyPipes.Apply(shell,Vector3.zero,true);
                Vector3 Marker(string name)=>car.InverseTransformPoint(markers.Single(t=>t.name==name).position);
                var inlet=Marker("STEAM_IN");var exhaust=Marker("EXHAUST_OUT");var smokebox=Marker("SMOKEBOX_RETURN");
                var socket=car.InverseTransformPoint(shell.TransformPoint(owner.branches.BoilerSocket(1)));
                owner.Pipe(root,"Shay boiler admission",new[]{socket,new Vector3(1.48f,2.72f,3.0f),new Vector3(1.48f,2.72f,-.28f),new Vector3(inlet.x,2.72f,inlet.z),inlet},.013f,material);
                owner.Pipe(root,"Shay single-chimney exhaust",new[]{exhaust,new Vector3(1.48f,2.62f,exhaust.z),new Vector3(1.48f,2.62f,smokebox.z),smokebox},.019f,material);
                return owner;
            }
            catch{owner.Dispose();throw;}
        }
        private void Pipe(Transform root,string name,Vector3[] points,float radius,Material material)
        {
            var mesh=S060MeyerExhaustPipe.Tube(points,Enumerable.Repeat(radius,points.Length).ToArray());meshes.Add(mesh);mesh.name=name;
            var part=new GameObject(name);part.transform.SetParent(root,false);part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        public void Dispose()
        {
            branches?.Dispose();branches=null;
            foreach(var mesh in meshes)if(mesh!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(mesh);else UnityEngine.Object.DestroyImmediate(mesh);}meshes.Clear();
        }
    }
}
