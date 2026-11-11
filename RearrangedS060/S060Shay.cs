using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DV.Wheels;
using LocoSim.Implementations.Wheels;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace RearrangedS060
{
    internal static class S060ShayAssets
    {
        private static AssetBundle bundle;
        internal static GameObject Resolve()
        {
            if (bundle == null) bundle=AssetBundle.LoadFromFile(Path.Combine(Main.ModPath,"assets","Shay","shay_running_gear"));
            if (bundle == null) throw new InvalidOperationException("Shay running-gear bundle is unavailable.");
            ValidateMotion(Motion);
            return bundle.LoadAsset<GameObject>("assets/shayintegration/s060shay.prefab")
                ?? throw new InvalidOperationException("Shay running-gear prefab is unavailable.");
        }
        internal static TextAsset Motion => bundle.LoadAllAssets<TextAsset>().Single();
        internal static void ValidateMotion(TextAsset data)
        {
            var specification = JObject.Parse(data.text);
            bool Finite(string key) => (float?)specification[key] is float value && !float.IsNaN(value) && !float.IsInfinity(value);
            bool Matches(string key, float expected) => Finite(key) && Math.Abs((float)specification[key] - expected) <= .00001f;
            if ((int?)specification["ringTeeth"] != S060SteamCalibration.Shay.RingTeeth ||
                (int?)specification["pinionTeeth"] != S060SteamCalibration.Shay.PinionTeeth ||
                (int?)specification["engineFitRevision"] != 2 ||
                !Matches("rollingRadius", S060Selection.ShayRollingRadius) ||
                !Matches("engineBore", S060SteamCalibration.Shay.Bore) ||
                !Matches("engineStroke", S060SteamCalibration.Shay.Stroke) ||
                !Matches("truckShaftY", S060SteamCalibration.Shay.TruckShaftY) ||
                !Matches("truckShaftX", S060SteamCalibration.Shay.TruckShaftX) ||
                !Finite("cylinderPitch") || (float)specification["cylinderPitch"] <= 0 || !Finite("valveAxisX"))
                throw new InvalidOperationException("Update the included Shay assets for the 0.80 m wheels, larger engine and 86:20 reduction before selecting Shay.");
        }
    }

    internal sealed class S060Shay : S060Conversion
    {
        internal override string Name => "Two-truck Shay (native wheels, 4.3:1 reduction)";
        internal Transform Body => Parts.Body;
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
            var powered=BuildModels(prefab,S060ShayWheelset.FromStock());
            Physics.Apply(S060ShayWheelset.RollingRadius,powered,4,1);
            Parts.FrontBogie.axles=Parts.RearBogie.axles=null;
            simulation.Select(S060EngineMode.Shay);
            var motion=Root.AddComponent<S060ShayMotion>();motion.Bind(simulation.Bank,Root.transform,S060ShayAssets.Motion,Markers);
            effects=S060ShayEffects.Bind(Parts.Car,Root,name=>Markers.Single(t=>t.name==name));
            RefreshMaterials(Markers);
        }
        internal PoweredWheel[] BuildModels(GameObject prefab,S060ShayWheelset pony)
        {
            if(pony?.Near==null||pony.Material==null) throw new InvalidOperationException("Stock S282 pony wheelset unavailable for the Shay.");
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
                var axle=Find(axleNames[i]);
                // Live native wheelset; resize only its radial plane, retaining gauge.
                var filter=axle.GetComponentsInChildren<MeshFilter>(true).Single(f=>f.sharedMesh==null&&f.name.EndsWith("_NATIVE_SET",StringComparison.Ordinal));
                filter.transform.localRotation=pony.Rotation;filter.transform.localScale=pony.Scale;
                filter.sharedMesh=pony.Near;filter.GetComponent<MeshRenderer>().sharedMaterial=pony.Material;
                if(pony.Far!=null)
                {
                    var distant=new GameObject("Native wheel distant");distant.transform.SetParent(filter.transform,false);
                    distant.AddComponent<MeshFilter>().sharedMesh=pony.Far;
                    var distantRenderer=distant.AddComponent<MeshRenderer>();distantRenderer.sharedMaterial=pony.Material;
                    var lod=filter.gameObject.AddComponent<LODGroup>();
                    lod.SetLODs(new[]{new LOD(.10f,new Renderer[]{filter.GetComponent<MeshRenderer>()}),new LOD(.015f,new Renderer[]{distantRenderer})});lod.RecalculateBounds();
                }
                var parent=(i<2?Parts.RearBogie:Parts.FrontBogie).transform.Find("bogie_car");
                var live=UnityEngine.Object.Instantiate(Parts.AxleFor(1).gameObject,parent); added.Add(live);live.name="[axle] S060 Shay "+i;
                live.transform.position=axle.position;live.transform.localRotation=Quaternion.identity;
                foreach(var renderer in live.GetComponentsInChildren<Renderer>(true)) renderer.enabled=false;
                var pw=live.GetComponent<PoweredWheel>();pw.wheelTransform=filter.transform;
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
            // The Shay meshes have their own UVs, not the S060 body atlas, so painted
            // and machined slots get working finishes rather than the livery sheet.
            foreach(var renderer in parts.SelectMany(t=>t.GetComponents<MeshRenderer>()))
            {
                var material=renderer.sharedMaterial; if(material==null) continue;
                if(material.name.Contains("PAINT")||material.name.Contains("painted")) renderer.sharedMaterial=S060ShayFinish.Gunmetal(material);
                else if(material.name=="gear_steel"||material.name=="shaft_steel"||material.name=="S060_MACHINED") renderer.sharedMaterial=S060ShayFinish.Steel(material);
            }
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

    // Stock S282 pony wheelset, aliased live; radial sizing preserves lateral gauge.
    internal sealed class S060ShayWheelset
    {
        // Fit contract R2: tread radius at the S060's own contact line (x .7575).
        internal const float RollingRadius=S060Selection.ShayRollingRadius;
        internal const float NativeRollingRadius=.3552532f;
        internal Mesh Near, Far; internal Material Material;
        internal Quaternion Rotation=Quaternion.identity; internal Vector3 Scale=Vector3.one;
        internal static S060ShayWheelset FromStock()
        {
            var donor=S060PonyAssets.Resolve();
            var filters=donor.Axle.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null&&f.sharedMesh.name.StartsWith("s282_wheels_front",StringComparison.OrdinalIgnoreCase)).ToArray();
            var near=filters.FirstOrDefault(f=>f.sharedMesh.name.IndexOf("LOD",StringComparison.OrdinalIgnoreCase)<0)??filters.FirstOrDefault();
            var far=filters.FirstOrDefault(f=>f!=near&&f.sharedMesh.name.IndexOf("LOD1",StringComparison.OrdinalIgnoreCase)>=0);
            if(near==null) throw new InvalidOperationException("Stock S282 pony wheelset mesh unavailable.");
            // Keep the donor's mesh orientation/scale relative to its own axle.
            var rel=donor.Axle.worldToLocalMatrix*near.transform.localToWorldMatrix;
            return new S060ShayWheelset{Near=near.sharedMesh,Far=far?.sharedMesh,Material=near.GetComponent<MeshRenderer>()?.sharedMaterial,
                Rotation=rel.rotation,Scale=Vector3.Scale(rel.lossyScale,new Vector3(1,RollingRadius/NativeRollingRadius,RollingRadius/NativeRollingRadius))};
        }
    }

    // Shared run-time finishes built from the bundle's own Standard materials. Statics outlive
    // a save reload, but the bundle's Standard shader may not: a copy whose shader has gone is
    // rebuilt from the live template, never reused (it rendered magenta after reload, 0.22.2).
    internal static class S060ShayFinish
    {
        private static Material gunmetal, steel;
        private static Texture2D grime;
        internal static Material Gunmetal(Material template)=>Live(gunmetal)?gunmetal:gunmetal=Make(template,"S060 Shay dirty gunmetal",new Color(.21f,.215f,.22f),.55f,.28f);
        internal static Material Steel(Material template)=>Live(steel)?steel:steel=Make(template,"S060 Shay oily steel",new Color(.34f,.34f,.33f),.75f,.42f);
        private static bool Live(Material m)=>m!=null&&m.shader!=null&&m.shader.isSupported;
        private static Material Make(Material template,string name,Color color,float metallic,float gloss)
        {
            var m=new Material(template){name=name,color=color,mainTexture=Grime(),mainTextureScale=new Vector2(2,2)};
            m.SetTexture("_MetallicGlossMap",null);m.DisableKeyword("_METALLICGLOSSMAP");
            m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",gloss);
            return m;
        }
        // Tileable soot/oil mottling used as an albedo multiplier; generated, so nothing is shipped.
        internal static Texture2D Grime()
        {
            if(grime!=null) return grime;
            const int size=128; var pixels=new Color32[size*size];
            float Lattice(int x,int y,int period){x=((x%period)+period)%period;y=((y%period)+period)%period;uint h=unchecked((uint)(x*374761393+y*668265263+period*69069));h=unchecked((h^(h>>13))*1274126177u);return (h^(h>>16))/(float)uint.MaxValue;}
            float Noise(float u,float v,int period)
            {
                float x=u*period,y=v*period;int ix=Mathf.FloorToInt(x),iy=Mathf.FloorToInt(y);float fx=x-ix,fy=y-iy;fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy);
                return Mathf.Lerp(Mathf.Lerp(Lattice(ix,iy,period),Lattice(ix+1,iy,period),fx),Mathf.Lerp(Lattice(ix,iy+1,period),Lattice(ix+1,iy+1,period),fx),fy);
            }
            for(int y=0;y<size;y++)
                for(int x=0;x<size;x++)
                {
                    float u=x/(float)size,v=y/(float)size;
                    float n=.5f*Noise(u,v,4)+.3f*Noise(u,v,12)+.2f*Noise(u,v,40);
                    float shade=Mathf.Lerp(.72f,1.08f,n),rust=Mathf.Clamp01((Noise(u,v,7)-.62f)*4);
                    pixels[y*size+x]=new Color(Mathf.Clamp01(shade*(1+.10f*rust)),Mathf.Clamp01(shade*(1+.02f*rust)),Mathf.Clamp01(shade*(1-.08f*rust)),1);
                }
            grime=new Texture2D(size,size,TextureFormat.RGBA32,true){name="S060 Shay grime",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
            grime.SetPixels32(pixels);grime.Apply(true,true);return grime;
        }
    }

    internal sealed class S060ShayMaterials:MonoBehaviour
    {
        private Renderer source;private Renderer[] targets;private Material current;
        internal void Bind(Renderer source,Transform[] parts)
        {this.source=source;current=source.sharedMaterial;targets=parts.SelectMany(t=>t.GetComponents<MeshRenderer>()).Where(r=>r.sharedMaterial==current).Cast<Renderer>().ToArray();}
        private void LateUpdate(){if(source==null||source.sharedMaterial==current)return;current=source.sharedMaterial;foreach(var r in targets)if(r!=null)r.sharedMaterial=current;}
    }
}

namespace RearrangedS060
{
    // R2 oil-cup seats (James, 2026-10-28): three on the gear-side line-shaft bearing arms,
    // two on shelves on the off-side frame sill (the sill hid them on the truck frames),
    // one on top of the middle crank bearing. Native circuit index -> seat. Every seat is
    // upright and owned by the Shay root/frames, so leaving the Shay removes it.
    internal static class S060ShaySeats
    {
        internal static Dictionary<int,Transform> Build(S060Shay shay)
        {
            Transform Find(string name)=>shay.Markers.Single(t=>t!=null&&t.name==name);
            Transform Seat(Transform parent,string name,Vector3 local)
            {
                var seat=new GameObject("[S060 oil seat] "+name).transform;seat.SetParent(parent,false);
                seat.localPosition=local;seat.localRotation=Quaternion.identity;return seat;
            }
            const float flangeTop=.8502963f, apex=S060SteamCalibration.Shay.TruckShaftX;
            Transform OnSill(string name,float z)
            {
                var seat=Seat(shay.Root.transform,name,Vector3.zero);
                seat.position=shay.Body.TransformPoint(new Vector3(S060ShayBody.SillCupX,S060ShayBody.SillCupShelf,z));seat.rotation=shay.Body.rotation;return seat;
            }
            Vector3 OnBearingArm(string bearing)
            {
                var b=Find(bearing+"__AXIS").localPosition;return new Vector3(apex-.10f,flangeTop,b.z);
            }
            return new Dictionary<int,Transform>
            {
                [3]=Seat(Find("TRUCK_REAR_PIVOT"),"gear rear outer bearing",OnBearingArm("bearing_1")),
                [4]=Seat(Find("TRUCK_FRONT_PIVOT"),"gear front inner bearing",OnBearingArm("bearing_3")),
                [5]=Seat(Find("TRUCK_FRONT_PIVOT"),"gear front outer bearing",OnBearingArm("bearing_4")),
                [0]=OnSill("off-side rear sill",S060ShayBody.SillCupZ[0]),
                [1]=OnSill("off-side front sill",S060ShayBody.SillCupZ[1]),
                [2]=Seat(Find("Engine"),"crank bearing",new Vector3(0,.09f,.14f)),
            };
        }
    }
}
