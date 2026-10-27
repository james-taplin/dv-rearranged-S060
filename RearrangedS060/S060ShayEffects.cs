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
                owner.root=UnityEngine.Object.Instantiate(source.gameObject,model.transform);
                owner.root.name="[Meyer engine effects]"; // shared native teardown guard
                Fit(owner.root,car.transform,find);
                owner.presentation=new S060ArticulatedPresentation(car,owner.root,S060EngineMapping.MainId,false,3);owner.presentation.Bind();return owner;
            }
            catch{owner.Dispose();throw;}
        }
        internal static void Fit(GameObject root,Transform car,Func<string,Transform> find)
        {
                var jets=root.GetComponentsInChildren<CylinderCockParticlePortReader>(true).Single();
                var audio=root.GetComponentsInChildren<CylinderCockLayeredPortReader>(true).Single();
                var original=jets.cylinderSetups[0];var setups=new CylinderCockParticlePortReader.CylinderSetup[3];
                var sources=new LayeredAudio[3];
                for(int i=0;i<3;i++)
                {
                    var front=UnityEngine.Object.Instantiate(original.frontParticlesParent,jets.transform);var rear=UnityEngine.Object.Instantiate(original.rearParticlesParent,jets.transform);
                    front.transform.position=find("COCK_C"+(i+1)+"_FRONT").position;rear.transform.position=find("COCK_C"+(i+1)+"_REAR").position;
                    front.transform.rotation=rear.transform.rotation=Quaternion.LookRotation(-car.up,car.forward);
                    foreach(var ps in front.GetComponentsInChildren<ParticleSystem>(true).Concat(rear.GetComponentsInChildren<ParticleSystem>(true))){var main=ps.main;main.startLifetimeMultiplier*=.25f;main.startSpeedMultiplier*=.25f;}
                    setups[i]=new CylinderCockParticlePortReader.CylinderSetup {frontParticlesParent=front,rearParticlesParent=rear,frontActivityCurve=Shift(original.frontActivityCurve,i/3f),rearActivityCurve=Shift(original.rearActivityCurve,i/3f)};
                    sources[i]=UnityEngine.Object.Instantiate(audio.cylCockAudio[0],audio.transform);sources[i].transform.position=(front.transform.position+rear.transform.position)/2;
                }
                foreach(var s in jets.cylinderSetups){s.frontParticlesParent.SetActive(false);s.rearParticlesParent.SetActive(false);}
                foreach(var source in audio.cylCockAudio)source.gameObject.SetActive(false);
                jets.cylinderSetups=setups;audio.cylCockAudio=sources;root.SetActive(true);
                var controller=root.GetComponentsInChildren<ParticlesPortReadersController>(true).Single();
                var oldDrips=controller.particlePortReaders.Where(p=>p.particlesParent.name.StartsWith("Water drip ",StringComparison.Ordinal)).ToArray();
                if(oldDrips.Length!=2)throw new InvalidOperationException("Native Shay drip templates missing.");
                var clone=typeof(object).GetMethod("MemberwiseClone",BindingFlags.Instance|BindingFlags.NonPublic);
                var drips=new ParticlesPortReadersController.ParticlePortReader[3];
                for(int i=0;i<3;i++)
                {
                    var drip=(ParticlesPortReadersController.ParticlePortReader)clone.Invoke(oldDrips[0],null);
                    drip.particlesParent=UnityEngine.Object.Instantiate(oldDrips[0].particlesParent,controller.transform);drip.particlesParent.name="Water drip "+i;
                    drip.particlesParent.transform.position=find("COCK_C"+(i+1)+"_REAR").position;drip.particleSystemsData=null;
                    drip.particlesParent.transform.rotation=Quaternion.LookRotation(-car.up,car.forward);
                    drip.particleUpdaters=oldDrips[0].particleUpdaters.Select(p=>(ParticlesPortReadersController.ParticlePortReader.PortParticleUpdateDefinition)clone.Invoke(p,null)).ToList();
                    drips[i]=drip;
                }
                foreach(var drip in oldDrips)drip.particlesParent.SetActive(false);
                controller.particlePortReaders=controller.particlePortReaders.Except(oldDrips).Concat(drips).ToList();
        }
        private static AnimationCurve Shift(AnimationCurve source,float offset)
        {
            var keys=Enumerable.Range(-1,3).SelectMany(turn=>source.keys.Select(k=>{k.time+=turn-offset;return k;})).OrderBy(k=>k.time).ToArray();
            return new AnimationCurve(keys){preWrapMode=WrapMode.Loop,postWrapMode=WrapMode.Loop};
        }
        public void Dispose(){presentation?.Dispose();presentation=null;if(root!=null){root.SetActive(false);if(Application.isPlaying)UnityEngine.Object.Destroy(root);else UnityEngine.Object.DestroyImmediate(root);}root=null;}
    }
}
