using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DV.Wheels;
using LocoSim.Implementations.Wheels;
using UnityEngine;

namespace RearrangedS060
{
    internal static class S060ShayAssets
    {
        private static AssetBundle bundle;
        internal static GameObject Resolve()
        {
            if (bundle == null) bundle=AssetBundle.LoadFromFile(Path.Combine(Main.ModPath,"assets","Shay","shay_running_gear"));
            if (bundle == null) throw new InvalidOperationException("Shay running-gear bundle is unavailable.");
            return bundle.LoadAsset<GameObject>("assets/shayintegration/s060shay.prefab")
                ?? throw new InvalidOperationException("Shay running-gear prefab is unavailable.");
        }
        internal static TextAsset Motion => bundle.LoadAllAssets<TextAsset>().Single();
    }

    internal sealed class S060Shay : S060Conversion
    {
        internal override string Name => "Two-truck Shay (native wheels, 43:19 reduction)";
        internal GameObject Root { get; private set; }
        internal Transform[] Markers { get; private set; }
        private readonly List<(GameObject part,bool active)> hidden=new List<(GameObject,bool)>();
        private readonly List<(Renderer part,bool enabled)> renderers=new List<(Renderer,bool)>();
        private readonly List<(Behaviour part,bool enabled)> readers=new List<(Behaviour,bool)>();
        private readonly List<GameObject> added=new List<GameObject>();
        private S060NativeQuartering quartering;
        private S060ShayBody body;
        private S060ShayPlumbing plumbing;
        private S060ArticulatedSimulation simulation;
        private S060StockCocks stockCocks;
        private S060ShayEffects effects;
        private MeshRenderer materialSource;
        private Material lastMaterial;
        protected override void Apply()
        {
            var prefab=S060ShayAssets.Resolve(); simulation=S060ArticulatedSimulation.Ensure(Parts.Car);
            quartering=new S060NativeQuartering(Parts.MechanismLeft,Parts.MechanismRight); quartering.Retain();
            stockCocks=new S060StockCocks(Parts.Car); stockCocks.Mute();
            var powered=BuildModels(prefab);
            Physics.Apply(S060Parts.DriverRadius*.8f,powered,4,1);
            Parts.FrontBogie.axles=Parts.RearBogie.axles=null;
            simulation.Select(S060EngineMode.Shay);
            var motion=Root.AddComponent<S060ShayMotion>();motion.Bind(simulation.Bank,Root.transform,S060ShayAssets.Motion,Markers);
            effects=S060ShayEffects.Bind(Parts.Car,Root,name=>Markers.Single(t=>t.name==name));
            RefreshMaterials(Markers);
        }
        internal PoweredWheel[] BuildModels(GameObject prefab)
        {
            Root=UnityEngine.Object.Instantiate(prefab,Parts.Car.transform); added.Add(Root); Root.name="[S060 Shay]"; Root.SetActive(false);
            Root.transform.localPosition=Vector3.zero; Root.transform.localRotation=Quaternion.identity; Root.transform.localScale=Vector3.one;
            body=S060ShayBody.Apply(Parts.Body);
            foreach(var mechanism in new[]{Parts.MechanismLeft,Parts.MechanismRight}) Hide(mechanism.gameObject);
            for(int i=1;i<=3;i++) Hide(Parts.AxleFor(i).gameObject);
            foreach(var reader in Parts.Car.GetComponentsInChildren<PoweredWheelRotationViaAnimation>(true)) {readers.Add((reader,reader.enabled));reader.enabled=false;}
            foreach(var renderer in Parts.Body.GetComponentsInChildren<Renderer>(true).Where(r=>r.name.StartsWith("s060_cylinder_",StringComparison.Ordinal)||r.name.Contains("caliper")||r.name.Contains("brake_shoes")))
            {renderers.Add((renderer,renderer.enabled));renderer.enabled=false;}
            materialSource=Parts.Body.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name=="s060_body"&&r.sharedMaterial!=null);
            var all=Root.GetComponentsInChildren<Transform>(true); Markers=all;
            Transform Find(string name)=>all.Single(t=>t.name==name);
            foreach(bool rear in new[]{true,false})
            {
                var parent=(rear?Parts.RearBogie:Parts.FrontBogie).transform.Find("bogie_car");
                foreach(string name in new[]{rear?"TRUCK_REAR":"TRUCK_FRONT",rear?"TRUCK_REAR_PIVOT":"TRUCK_FRONT_PIVOT"})
                {var frame=Find(name); added.Add(frame.gameObject);frame.SetParent(parent,false);frame.localPosition=Vector3.zero;frame.localRotation=Quaternion.identity;}
            }
            // All lookup arrays were captured before native-bogie reparenting.
            var powered=new List<PoweredWheel>();
            string[] axleNames={"REAR_AXLE_1","REAR_AXLE_2","FRONT_AXLE_3","FRONT_AXLE_4"};
            for(int i=0;i<4;i++)
            {
                var axle=Find(axleNames[i]); int native=i==0?1:i==3?3:2;
                foreach(var filter in axle.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh==null))
                {
                    bool left=filter.name.EndsWith("_L",StringComparison.Ordinal);
                    var source=Parts.Wheels(native).Single(t=>t.parent==(left?Parts.MechanismLeft:Parts.MechanismRight));
                    filter.sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;
                    filter.GetComponent<MeshRenderer>().sharedMaterial=source.GetComponent<MeshRenderer>().sharedMaterial;
                    var nativeLod=source.Find(source.name+"_LOD1");
                    if(nativeLod==null || nativeLod.GetComponent<MeshFilter>()?.sharedMesh==null)
                        throw new InvalidOperationException("Native Shay distant wheel alias missing: "+source.name);
                    var distant=new GameObject("Native wheel distant");distant.transform.SetParent(filter.transform,false);
                    distant.AddComponent<MeshFilter>().sharedMesh=nativeLod.GetComponent<MeshFilter>().sharedMesh;
                    var distantRenderer=distant.AddComponent<MeshRenderer>();distantRenderer.sharedMaterial=filter.GetComponent<MeshRenderer>().sharedMaterial;
                    var lod=filter.gameObject.AddComponent<LODGroup>();
                    lod.SetLODs(new[]{new LOD(.10f,new Renderer[]{filter.GetComponent<MeshRenderer>()}),new LOD(.015f,new Renderer[]{distantRenderer})});lod.RecalculateBounds();
                }
                var parent=(i<2?Parts.RearBogie:Parts.FrontBogie).transform.Find("bogie_car");
                var live=UnityEngine.Object.Instantiate(Parts.AxleFor(1).gameObject,parent); added.Add(live);live.name="[axle] S060 Shay "+i;
                live.transform.position=axle.position;live.transform.localRotation=Quaternion.identity;
                foreach(var renderer in live.GetComponentsInChildren<Renderer>(true)) renderer.enabled=false;
                var pw=live.GetComponent<PoweredWheel>();pw.wheelTransform=axle.GetComponentsInChildren<MeshFilter>(true).Single(f=>f.name.EndsWith("_R",StringComparison.Ordinal)).transform;
                powered.Add(pw);live.SetActive(true);
            }
            Root.SetActive(true);
            Markers=all.Concat(all.SelectMany(t=>t.Cast<Transform>().Where(child=>child.name=="Native wheel distant"))).ToArray();
            foreach(var frame in all.Where(t=>t.name=="TRUCK_REAR"||t.name=="TRUCK_FRONT"||t.name=="TRUCK_REAR_PIVOT"||t.name=="TRUCK_FRONT_PIVOT"))
            { if(frame.parent!=Root.transform && !frame.IsChildOf(Root.transform)) frame.gameObject.SetActive(true); }
            plumbing=S060ShayPlumbing.Bind(Parts.Car.transform,Parts.Body,Root.transform,all,materialSource.sharedMaterial);
            return powered.ToArray();
        }
        private void RefreshMaterials(Transform[] parts)
        {
            lastMaterial=materialSource.sharedMaterial;
            foreach(var renderer in parts.SelectMany(t=>t.GetComponents<MeshRenderer>()))
                if(renderer.sharedMaterial!=null && (renderer.sharedMaterial.name.Contains("PAINT")||renderer.sharedMaterial.name.Contains("painted"))) renderer.sharedMaterial=lastMaterial;
            Root.AddComponent<S060ShayMaterials>().Bind(materialSource,parts);
        }
        private void Hide(GameObject part){hidden.Add((part,part.activeSelf));part.SetActive(false);}
        protected override void Undo()
        {
            // Retain failed owners/frames for the conversion transaction's retry.
            effects?.Dispose();effects=null;
            simulation?.Select(S060EngineMode.Simple);
            Physics.Restore(); RestoreModels();
        }
        internal void RestoreModels()
        {
            plumbing?.Dispose();plumbing=null;
            body?.Dispose();body=null;
            foreach(var h in hidden) if(h.part!=null) h.part.SetActive(h.active);hidden.Clear();
            foreach(var h in renderers) if(h.part!=null) h.part.enabled=h.enabled;renderers.Clear();
            foreach(var h in readers) if(h.part!=null) h.part.enabled=h.enabled;readers.Clear();
            quartering?.Dispose();quartering=null;stockCocks?.Dispose();stockCocks=null;
            // Detached frames have independent native parents and must be removed too.
            foreach(var item in added) if(item!=null){item.SetActive(false);if(Application.isPlaying)UnityEngine.Object.Destroy(item);else UnityEngine.Object.DestroyImmediate(item);}added.Clear();Root=null;Markers=null;
            Parts.FrontBogie.axles=Parts.RearBogie.axles=null;
        }
        protected override void ReleasePresentationMeshes(){plumbing?.Dispose();body?.Dispose();}
    }

    internal sealed class S060ShayMaterials:MonoBehaviour
    {
        private Renderer source;private Renderer[] targets;private Material current;
        internal void Bind(Renderer source,Transform[] parts)
        {this.source=source;current=source.sharedMaterial;targets=parts.SelectMany(t=>t.GetComponents<MeshRenderer>()).Where(r=>r.sharedMaterial==current).Cast<Renderer>().ToArray();}
        private void LateUpdate(){if(source==null||source.sharedMaterial==current)return;current=source.sharedMaterial;foreach(var r in targets)if(r!=null)r.sharedMaterial=current;}
    }
}
