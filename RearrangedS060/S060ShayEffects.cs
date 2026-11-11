using System;
using System.Linq;
using System.Reflection;
using DV.Simulation.Controllers;
using DV.Simulation.Ports;
using DV.ModularAudioCar;
using UnityEngine;
namespace RearrangedS060
{
    internal sealed class S060ShayEffects:IDisposable
    {
        private GameObject root;
        private S060ArticulatedPresentation presentation;
        internal static S060ShayEffects Bind(TrainCar car,GameObject model,Func<string,Transform> find)
        {
            var owner=new S060ShayEffects();
            try
            {
                // Reuse only the processed native effects subtree. Cloning a
                // complete donor truck would leave unused running-gear scripts.
                var source=S060Articulated.Preflight(false)[0].GetComponentsInChildren<ParticlesPortReadersController>(true).Single();
                owner.root=Build(source,model.transform,car.transform,find);
                owner.root.SetActive(true);
                owner.presentation=new S060ArticulatedPresentation(car,owner.root,S060EngineMapping.MainId,false,3);owner.presentation.Bind();return owner;
            }
            catch{owner.Dispose();throw;}
        }
        // Native Doppler.Awake adds a DisallowMultipleComponent ECS converter.
        // Complete every nested clone before the first activation, so no copy
        // inherits a converter added by an earlier clone's Awake.
        internal static GameObject Build(ParticlesPortReadersController source,Transform parent,Transform car,Func<string,Transform> find)
        {
            var staging=new GameObject("[Shay native cylinder effects]");staging.SetActive(false);staging.transform.SetParent(parent,false);
            try
            {
                var effects=UnityEngine.Object.Instantiate(source.gameObject,staging.transform);
                effects.name="[Meyer engine effects]"; // shared native teardown guard
                Fit(effects,car,find);effects.SetActive(true);
                return staging;
            }
            catch
            {
                if(Application.isPlaying)UnityEngine.Object.Destroy(staging);else UnityEngine.Object.DestroyImmediate(staging);
                throw;
            }
        }
        internal static void Fit(GameObject root,Transform car,Func<string,Transform> find)
        {
                if(root.activeInHierarchy)throw new InvalidOperationException("Shay effects must be assembled before activation.");
                var controller=root.GetComponentsInChildren<ParticlesPortReadersController>(true).Single();
                // CCL substitutes each authored holder with its native alias.
                // The port contract survives; the holder's display name does not.
                var oldDrips=controller.particlePortReaders.Where(p=>p.particlesParent!=null &&
                    p.particleUpdaters.Any(u=>u.portId.EndsWith(".WATER_IN_CYLINDERS_NORMALIZED",StringComparison.Ordinal)) &&
                    p.particleUpdaters.Any(u=>u.portId=="cylinderCock.EXT_IN")).ToArray();
                if(oldDrips.Length!=2)throw new InvalidOperationException("Native Shay drip templates missing.");
                var jets=root.GetComponentsInChildren<CylinderCockParticlePortReader>(true).Single();
                var audio=root.GetComponentsInChildren<CylinderCockLayeredPortReader>(true).Single();
                var original=jets.cylinderSetups[0];var setups=new CylinderCockParticlePortReader.CylinderSetup[3];
                var sources=new LayeredAudio[3];
                for(int i=0;i<3;i++)
                {
                    var front=UnityEngine.Object.Instantiate(original.frontParticlesParent,jets.transform);var rear=UnityEngine.Object.Instantiate(original.rearParticlesParent,jets.transform);
                    FitJet(front,find("COCK_C"+(i+1)+"_FRONT"));FitJet(rear,find("COCK_C"+(i+1)+"_REAR"));
                    setups[i]=new CylinderCockParticlePortReader.CylinderSetup {frontParticlesParent=front,rearParticlesParent=rear,frontActivityCurve=Shift(original.frontActivityCurve,i/3f),rearActivityCurve=Shift(original.rearActivityCurve,i/3f)};
                    sources[i]=UnityEngine.Object.Instantiate(audio.cylCockAudio[0],audio.transform);sources[i].transform.position=(front.transform.position+rear.transform.position)/2;
                }
                foreach(var s in jets.cylinderSetups){s.frontParticlesParent.SetActive(false);s.rearParticlesParent.SetActive(false);}
                foreach(var source in audio.cylCockAudio)source.gameObject.SetActive(false);
                jets.cylinderSetups=setups;audio.cylCockAudio=sources;
                var clone=typeof(object).GetMethod("MemberwiseClone",BindingFlags.Instance|BindingFlags.NonPublic);
                var drips=new ParticlesPortReadersController.ParticlePortReader[3];
                for(int i=0;i<3;i++)
                {
                    var drip=(ParticlesPortReadersController.ParticlePortReader)clone.Invoke(oldDrips[0],null);
                    drip.particlesParent=UnityEngine.Object.Instantiate(oldDrips[0].particlesParent,controller.transform);drip.particlesParent.name="Water drip "+i;
                    drip.particlesParent.transform.position=find("COCK_C"+(i+1)+"_REAR").position;drip.particleSystemsData=null;
                    var outlet=find("COCK_C"+(i+1)+"_REAR");
                    drip.particlesParent.transform.rotation=outlet.rotation;
                    foreach(var ps in drip.particlesParent.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        ps.transform.SetPositionAndRotation(outlet.position,outlet.rotation);
                        // The native horizontal-cylinder drip box is 0.86 m long.
                        // Each upright cylinder has one compact drain mouth.
                        var shape=ps.shape;shape.position=Vector3.zero;shape.rotation=Vector3.zero;shape.scale=Vector3.one*.014f;
                    }
                    drip.particleUpdaters=oldDrips[0].particleUpdaters.Select(p=>(ParticlesPortReadersController.ParticlePortReader.PortParticleUpdateDefinition)clone.Invoke(p,null)).ToList();
                    drips[i]=drip;
                }
                foreach(var drip in oldDrips)drip.particlesParent.SetActive(false);
                controller.particlePortReaders=controller.particlePortReaders.Except(oldDrips).Concat(drips).ToList();
        }
        private static void FitJet(GameObject holder,Transform outlet)
        {
            holder.transform.SetPositionAndRotation(outlet.position,outlet.rotation);
            foreach(var ps in holder.GetComponentsInChildren<ParticleSystem>(true))
            {
                // Native SteamExhaust children already carry +/-90-degree yaw.
                // Align the actual particle systems, not only their parent.
                ps.transform.SetPositionAndRotation(outlet.position,outlet.rotation);
                var main=ps.main;main.startLifetimeMultiplier*=.25f;main.startSpeedMultiplier*=.25f;
                main.startSizeMultiplier*=.18f;
                // Preserve the native buoyant shape at the shortened jet scale.
                main.gravityModifierMultiplier*=.0625f;
                var shape=ps.shape;shape.position=Vector3.zero;shape.rotation=Vector3.zero;shape.scale=Vector3.one;shape.radius=.007f;
            }
        }
        private static AnimationCurve Shift(AnimationCurve source,float offset)
        {
            var keys=Enumerable.Range(-1,3).SelectMany(turn=>source.keys.Select(k=>{k.time+=turn-offset;return k;})).OrderBy(k=>k.time).ToArray();
            return new AnimationCurve(keys){preWrapMode=WrapMode.Loop,postWrapMode=WrapMode.Loop};
        }
        public void Dispose(){presentation?.Dispose();presentation=null;if(root!=null){root.SetActive(false);if(Application.isPlaying)UnityEngine.Object.Destroy(root);else UnityEngine.Object.DestroyImmediate(root);}root=null;}
    }
}
