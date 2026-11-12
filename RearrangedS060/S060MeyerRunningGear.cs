using System;
using System.Collections.Generic;
using System.Linq;
using DV.Simulation.Brake;
using UnityEngine;
using Object=UnityEngine.Object;

namespace RearrangedS060
{
    // Native shoes and native heat controller, fitted to the two visual units.
    // No brake simulation, ports or fictitious shoe actuator are added.
    internal sealed class S060MeyerRunningGear : MonoBehaviour,IDisposable
    {
        private readonly List<GameObject> groups=new List<GameObject>();
        private readonly List<Mesh> meshes=new List<Mesh>();
        private readonly List<(Renderer renderer,bool enabled)> hidden=new List<(Renderer,bool)>();
        private readonly List<MeshRenderer> painted=new List<MeshRenderer>();
        private readonly List<MeshRenderer> shoeRenderers=new List<MeshRenderer>();
        private MeshRenderer shoeMaterialSource;
        private Material shoeMaterial;
        private BrakesOverheatingController heat;
        private Renderer[] originalHeat;
        private MeshRenderer bodyMaterialSource;
        private Material bodyMaterial;
        private bool disposed,destroying;
        internal static S060MeyerRunningGear Bind(Transform car,Transform body,Transform front,Transform rear)
        {
            var owner=car.gameObject.AddComponent<S060MeyerRunningGear>();
            try
            {
                owner.heat=car.GetComponent<BrakesOverheatingController>()??throw new InvalidOperationException("Native S060 brake heat controller required.");
                owner.originalHeat=owner.heat.brakeRenderers;
                if(owner.originalHeat==null||owner.originalHeat.Length==0)throw new InvalidOperationException("Native brake heat renderer sources required.");
                var shoes=body.GetComponentsInChildren<MeshFilter>(true).First(f=>f.name.IndexOf("brake_shoes",StringComparison.OrdinalIgnoreCase)>=0&&f.sharedMesh!=null);
                owner.shoeMaterialSource=shoes.GetComponent<MeshRenderer>();owner.shoeMaterial=owner.shoeMaterialSource.sharedMaterial;
                var shell=body.GetComponentsInChildren<MeshFilter>(true).First(f=>f.name=="s060_body"&&f.sharedMesh!=null);
                owner.bodyMaterialSource=shell.GetComponent<MeshRenderer>();owner.bodyMaterial=owner.bodyMaterialSource.sharedMaterial;
                var shoeRenderers=new List<Renderer>();
                foreach(var frame in new[]{front,rear})
                {
                    var go=new GameObject("[Meyer native brakes and visible springs]");owner.groups.Add(go);go.transform.SetParent(frame,false);
                    foreach(int side in new[]{-1,1})owner.Draw(go.transform,Carrier(side),new[]{owner.bodyMaterial},true);
                    foreach(int axle in new[]{-1,1})
                    {
                        float z=axle*(float)MeyerPlan.AxlePitch/2;
                        Vector3 Place(Vector3 p)=>new Vector3(p.x,p.y,z-(p.z-S060Parts.Driver3Z));
                        var shoe=Section(shoes,body,p=>p.z>2.1f,Place,n=>new Vector3(n.x,n.y,-n.z),"Meyer native brake shoes "+axle,true);
                        var shoeRenderer=owner.Draw(go.transform,shoe,owner.shoeMaterialSource.sharedMaterials,false);shoeRenderers.Add(shoeRenderer);owner.shoeRenderers.Add(shoeRenderer);
                        foreach(int side in new[]{-1,1})
                        {
                            // Original shoe shape, mirrored to the trailing side;
                            // pick an actual backing vertex for the hanger pin.
                            var target=new Vector3(side*.76f,.25f,z-.70f);
                            var pin=shoe.vertices.Where(p=>p.x*side>0).OrderBy(p=>(p-target).sqrMagnitude).First();
                            owner.Draw(go.transform,S060MeyerExhaustPipe.PathMesh(new[]{new Vector3(side*1.20f,1.24f,Mathf.Clamp(pin.z,-1.35f,1.35f)),new Vector3(side*1.20f,pin.y,pin.z),pin},.022f),new[]{owner.bodyMaterial},true).name="Meyer brake hanger "+side+" "+axle;
                            // Retain the stock laminated shape and atlas, fitted
                            // into the visible space above the outer driver face.
                            bool Spring(Vector3 p)=>p.x*side>.37f&&p.x*side<.61f&&p.y>1.12f&&p.y<1.45f&&p.z>.98f&&p.z<2.42f;
                            Vector3 SpringPosition(Vector3 p)=>new Vector3(side*1.26f+(p.x-side*.49f)*.5f,1.32f+(p.y-1.30f)*.3f,z+(p.z-1.7006f)*.80f);
                            Vector3 SpringNormal(Vector3 n)=>new Vector3(n.x/.5f,n.y/.3f,n.z/.80f).normalized;
                            owner.Draw(go.transform,Section(shell,body,Spring,SpringPosition,SpringNormal,"Meyer native leaf spring "+side+" "+axle),new[]{owner.bodyMaterial},true);
                            foreach(int end in new[]{-1,1})
                            {
                                float at=z+end*.56f;
                                owner.Draw(go.transform,S060MeyerExhaustPipe.PathMesh(new[]{new Vector3(side*1.20f,1.24f,at),new Vector3(side*1.26f,1.31f,at)},.024f),new[]{owner.bodyMaterial},true).name="Meyer spring hanger "+side+" "+axle+" "+end;
                            }
                        }
                    }
                    foreach(var renderer in frame.Find("[Powered bogie frame]").GetComponentsInChildren<Renderer>(true).Where(r=>r.name.StartsWith("Leaf spring ",StringComparison.Ordinal)))
                    {owner.hidden.Add((renderer,renderer.enabled));renderer.enabled=false;}
                }
                owner.heat.brakeRenderers=owner.originalHeat.Concat(shoeRenderers).Distinct().ToArray();
                return owner;
            }
            catch{owner.Dispose();throw;}
        }
        private MeshRenderer Draw(Transform parent,Mesh mesh,Material[] materials,bool paint)
        {
            meshes.Add(mesh);var go=new GameObject(mesh.name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterials=materials;if(paint)painted.Add(renderer);return renderer;
        }
        private static Mesh Carrier(int side)
        {
            var lo=new Vector3(side*1.20f-.05f,1.215f,-1.40f);var hi=new Vector3(side*1.20f+.05f,1.275f,1.40f);
            var c=new[]{new Vector3(lo.x,lo.y,lo.z),new Vector3(hi.x,lo.y,lo.z),new Vector3(hi.x,hi.y,lo.z),new Vector3(lo.x,hi.y,lo.z),new Vector3(lo.x,lo.y,hi.z),new Vector3(hi.x,lo.y,hi.z),new Vector3(hi.x,hi.y,hi.z),new Vector3(lo.x,hi.y,hi.z)};
            var v=new List<Vector3>();var t=new List<int>();foreach(var face in new[]{new[]{0,3,2,1},new[]{4,5,6,7},new[]{0,4,7,3},new[]{1,2,6,5},new[]{0,1,5,4},new[]{3,7,6,2}}){int i=v.Count;v.AddRange(face.Select(k=>c[k]));t.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});}
            var mesh=new Mesh{name="Meyer spring carrier "+side,vertices=v.ToArray(),triangles=t.ToArray(),uv=Enumerable.Repeat(new Vector2(.6868f,.8948f),v.Count).ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static Mesh Section(MeshFilter source,Transform body,Func<Vector3,bool> select,Func<Vector3,Vector3> position,Func<Vector3,Vector3> normal,string name,bool reverse=false)
        {
            var mesh=source.sharedMesh;if(!mesh.isReadable)throw new InvalidOperationException("Native running-gear source must be readable: "+mesh.name);
            var matrix=body.worldToLocalMatrix*source.transform.localToWorldMatrix;var v=mesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();var n=mesh.normals;var uv=mesh.uv;
            var triangles=mesh.triangles;var chosen=Enumerable.Range(0,triangles.Length/3).Where(i=>Enumerable.Range(0,3).All(k=>select(v[triangles[3*i+k]]))).SelectMany(i=>new[]{triangles[3*i],triangles[3*i+1],triangles[3*i+2]}).ToArray();
            if(chosen.Length==0)throw new InvalidOperationException("Measured native section missing: "+name);
            var ids=chosen.Distinct().ToArray();var map=ids.Select((id,index)=>new{id,index}).ToDictionary(p=>p.id,p=>p.index);var normals=matrix.inverse.transpose;
            var fittedTriangles=chosen.Select(i=>map[i]).ToArray();if((matrix.determinant<0)^reverse)for(int i=0;i<fittedTriangles.Length;i+=3){int swap=fittedTriangles[i];fittedTriangles[i]=fittedTriangles[i+2];fittedTriangles[i+2]=swap;}
            var result=new Mesh{name=name,vertices=ids.Select(i=>position(v[i])).ToArray(),uv=ids.Select(i=>uv[i]).ToArray(),triangles=fittedTriangles};
            result.normals=ids.Select(i=>normal(normals.MultiplyVector(n[i]).normalized)).ToArray();result.RecalculateBounds();result.RecalculateTangents();return result;
        }
        private void LateUpdate()
        {
            if(disposed)return;
            if(bodyMaterialSource!=null&&bodyMaterialSource.sharedMaterial!=bodyMaterial){bodyMaterial=bodyMaterialSource.sharedMaterial;foreach(var renderer in painted)if(renderer!=null)renderer.sharedMaterial=bodyMaterial;}
            if(shoeMaterialSource!=null&&shoeMaterialSource.sharedMaterial!=shoeMaterial){shoeMaterial=shoeMaterialSource.sharedMaterial;foreach(var renderer in shoeRenderers)if(renderer!=null)renderer.sharedMaterials=shoeMaterialSource.sharedMaterials;}
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;enabled=false;
            if(heat!=null&&originalHeat!=null)heat.brakeRenderers=originalHeat;
            foreach(var home in hidden)if(home.renderer!=null)home.renderer.enabled=home.enabled;hidden.Clear();
            foreach(var go in groups)if(go!=null){go.SetActive(false);Release(go);}groups.Clear();
            foreach(var mesh in meshes)Release(mesh);meshes.Clear();painted.Clear();shoeRenderers.Clear();if(!destroying)Release(this);
        }
        private void OnDestroy(){destroying=true;Dispose();}
        private static void Release(Object value){if(value==null)return;if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
    }
}
