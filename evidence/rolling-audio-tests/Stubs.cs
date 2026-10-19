using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
    public class Component
    {
        internal readonly List<Component> children=new List<Component>();
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T:Component
        {
            IEnumerable<Component> Walk(Component component)
            {
                if(component==null)yield break;
                yield return component;
                foreach(var child in component.children)
                    foreach(var result in Walk(child))yield return result;
            }
            return Walk(this).OfType<T>().ToArray();
        }
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) {this.x=x;this.y=y;this.z=z;}
        public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);
    }
    public sealed class Transform:Component {public Vector3 position;}
    public sealed class Rigidbody {public Vector3 velocity=new Vector3(1,0,0);}
    public static class Mathf
    {
        public static int FloorToInt(float value)=>(int)Math.Floor(value);
        public static float Round(float value)=>(float)Math.Round(value);
    }
}
public sealed class TrainStress {public float slowBuildUpStress;}
public sealed class RailTrack
{
    public float jointsSpan=20;
    public float JointsSpan=>jointsSpan;
    public bool isJunctionTrack;
    public PointSet GetKinkedPointSet()=>new PointSet();
}
public sealed class PointSet {public double span=100000;}
public sealed class Traveller {public double Span=100;}
public sealed class Bogie
{
    public sealed class AxleInfo
    {
        public float distanceFromBogiePivot;
        public UnityEngine.Transform transform=new UnityEngine.Transform();
    }
    private AxleInfo[] axles;
    internal int axleReads;
    public AxleInfo[] Axles {get {axleReads++;return axles;}set {axles=value;}}
    public UnityEngine.Rigidbody rb=new UnityEngine.Rigidbody();
    public RailTrack track=new RailTrack();
    public Traveller traveller=new Traveller();
    public float TrackDirectionSign=1;
}
public sealed class TrainCar:UnityEngine.Component
{
    public UnityEngine.Transform interior=new UnityEngine.Transform();
    public Bogie[] Bogies={new Bogie(),new Bogie()};
    public TrainStress stress=new TrainStress();
    internal void SetAxles(int total)
    {
        int front=total/2,rear=total-front;
        for(int i=0;i<2;i++)Bogies[i].Axles=Enumerable.Range(0,i==0?front:rear).Select(j=>new Bogie.AxleInfo{distanceFromBogiePivot=j*.7f}).ToArray();
    }
}
namespace DV.ModularAudioCar
{
    public class CarAudioModule:UnityEngine.Component
    {
        public virtual bool ExternalUpdate=>true;
        public virtual void UpdateModule(float deltaTime) { }
    }
    public sealed class LayeredAudio {public void Set(float value) { }}
    public sealed class AudioManager {public float rollingSpeedMult=1;}
    public sealed class BogieAudioController
    {
        public LayeredAudio currentRollingAudio=new LayeredAudio(),currentSquealingAudio=new LayeredAudio();
    }
    public partial class CarRollingAudioModule:CarAudioModule
    {
        public TrainCar car;
        private int[][] lastJointStep;
        private BogieAudioController[] bogieAudioControllers={new BogieAudioController(),new BogieAudioController()};
        private AudioManager am=new AudioManager();
        internal int[][] Cache=>lastJointStep;
        internal void InitializeForFixture(TrainCar trainCar) {car=trainCar;ResetJointSoundValues();}
        private void PlayJointAtBogie(float speed,UnityEngine.Vector3 position,bool isJunctionJoint) { }
    }
    public partial class CarModularAudio {public List<CarAudioModule> audioModules=new List<CarAudioModule>();}
    public sealed class BrakeUpdateProbe:CarAudioModule
    {
        internal int Updates;
        public override void UpdateModule(float deltaTime) {Updates++;}
    }
}
namespace RearrangedS282
{
    internal partial class WheelRearranger:UnityEngine.Component
    {
        private TrainCar loco;
        internal WheelRearranger(TrainCar car) {loco=car;}
        internal void RefreshForFixture()=>RefreshConventionalRollingAudio();
    }
}
