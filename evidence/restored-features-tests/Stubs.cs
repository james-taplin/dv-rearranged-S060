using System;
using System.Collections.Generic;
using System.Linq;
using DV.ECS.Train;
using DV.Simulation.Cars;
using DV.Wheels;
using LocoSim.Implementations;

namespace UnityEngine
{
    public class Object
    {
        public static void Destroy(Object o) { }
        public static GameObject Instantiate(GameObject source, Transform parent, bool worldPositionStays)
        { var go=new GameObject();go.transform.parent=parent;return go; }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform=>gameObject.transform;
        public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>();
        public T[] GetComponentsInChildren<T>(bool includeInactive=false) where T:Component=>gameObject.GetComponentsInChildren<T>(includeInactive);
        public T GetComponentInChildren<T>(bool includeInactive=false) where T:Component=>GetComponentsInChildren<T>(includeInactive).FirstOrDefault();
    }
    public class MonoBehaviour:Component {public bool enabled=true;}
    public class GameObject:Object
    {
        public readonly Transform transform;
        internal readonly List<Component> components=new List<Component>();
        public bool activeSelf=true;
        public bool activeInHierarchy=>activeSelf && (transform.parent==null || transform.parent.gameObject.activeInHierarchy);
        public GameObject(){transform=new Transform{gameObject=this};components.Add(transform);}
        public T Add<T>() where T:Component,new(){var c=new T{gameObject=this};components.Add(c);return c;}
        public T GetComponent<T>() where T:Component=>components.OfType<T>().FirstOrDefault();
        public T[] GetComponentsInChildren<T>(bool includeInactive=false) where T:Component
        {
            IEnumerable<T> Walk(Transform t){
                if(!includeInactive&&!t.gameObject.activeInHierarchy)yield break;
                foreach(var c in t.gameObject.components.OfType<T>())yield return c;
                foreach(var child in t.children)foreach(var c in Walk(child))yield return c;
            }
            return Walk(transform).ToArray();
        }
        public void SetActive(bool value){activeSelf=value;}
        public string GetPath()=>"fixture";
    }
    public class Transform:Component
    {
        internal readonly List<Transform> children=new List<Transform>();
        private Transform ancestor;
        public Transform parent{get=>ancestor;set{ancestor?.children.Remove(this);ancestor=value;ancestor?.children.Add(this);}}
        public Vector3 position,forward=new Vector3(0,0,1);
        public Quaternion rotation;
        public bool IsChildOf(Transform root){for(var t=this;t!=null;t=t.parent)if(ReferenceEquals(t,root))return true;return false;}
        public Transform Find(string path)=>gameObject.GetComponent<FixtureLinks>()?.paths[path];
    }
    public sealed class FixtureLinks:Component {public Dictionary<string,Transform> paths=new Dictionary<string,Transform>();}
    public class Renderer:Component{public Material sharedMaterial;}
    public sealed class MeshRenderer:Renderer{}
    public sealed class Material:Object{}
    public struct Quaternion{}
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 operator *(Vector3 v,float f)=>new Vector3(v.x*f,v.y*f,v.z*f);
    }
    public sealed class Rigidbody{public float force;public void AddForce(Vector3 v){force+=v.z;}}
    public static class Mathf
    {
        public static float Abs(float f)=>Math.Abs(f);
        public static float Sign(float f)=>Math.Sign(f);
        public static float Clamp(float f,float min,float max)=>Math.Clamp(f,min,max);
        public static int RoundToInt(float f)=>(int)Math.Round(f);
    }
    public static class Debug{public static void LogError(object text,object context=null)=>throw new Exception(text.ToString());}
    public class SerializeField:Attribute{}
}
namespace DV.Utils{public class SingletonBehaviour<T> where T:new(){public static T Instance=new T();}}
public sealed class PausePhysicsHandler{public bool PhysicsHandlingInProcess;}
namespace LocoSim.Attributes{public sealed class PortIdAttribute:Attribute{public PortIdAttribute(LocoSim.Definitions.PortValueType t,bool b){}}}
namespace LocoSim.Definitions
{
    public enum PortValueType{TORQUE}
    public class ConfigurablePortDefinition:UnityEngine.Component{public float value=4;}
}
namespace LocoSim.Implementations
{
    public class Port{public float Value;}
    public class SimulationFlow
    {
        internal readonly Dictionary<string,Port> ports=new Dictionary<string,Port>();
        public bool TryGetPort(string id,out Port port)=>ports.TryGetValue(id,out port);
    }
}
namespace LocoSim.Implementations.Wheels
{
    public class PoweredWheel:UnityEngine.Component{public byte index;}
}
namespace DV.Wheels
{
    public class PoweredWheelRotationViaAnimation:UnityEngine.Component{public float wheelRadius=.91f,wheelCircumference=5.72f;}
    public partial class WheelslipController:UnityEngine.MonoBehaviour
    {
        internal TrainCar car;
        public Port engineBrakingActivePort,numberOfPoweredAxlesPort;
        public string numberOfPoweredAxlesPortId="powered";
        public DrivingForce DrivingForce;
        public float wheelslip,OrientedMaxWheelslipRpm,TotalForceLimit=30000;
    }
}
namespace DV.Simulation.Cars
{
    public class PoweredWheelsManager{public LocoSim.Implementations.Wheels.PoweredWheel[] poweredWheels;}
    public class TractionFeeder{public float speedMsToWheelRpmConst=10.5f,wheelRpmToSpeedKmhConst=.342857f;}
    public class SimController
    {
        public SimulationFlow simFlow=new SimulationFlow();
        public DrivingForce drivingForce;
        public WheelslipController wheelslipController;
        public PoweredWheelsManager poweredWheels=new PoweredWheelsManager();
        public TractionFeeder tractionPortsFeeder=new TractionFeeder();
    }
}
namespace DV.ECS.Train
{
    public static class AdhesionControllerSystem
    {
        public struct StaticSharedAdhesionCalculationData{public byte numOfAxles,bogieCount;}
        public struct StaticAdhesionCalculationData{public float wheelslipFrictionCoef,wheelSlideFrictionCoef;}
        public struct AdhesionCalculationData{public float weightPerAxle,engineBrakingForcePerAxle;}
    }
    public static class WheelslipControllerSystem
    {
        public struct WheelslipInputData{public ushort numberOfPoweredAxles;public float generatedForce;public bool isEngineBraking;}
        public struct WheelslipOutputData{public float wheelslip,wheelslipSmoothRefVel,orientedMaxWheelslipRpm,totalForceLimit;}
    }
}
public class Some<T>{private T v;public Some(T v){this.v=v;}public bool IsSome(out T value){value=v;return v!=null;}}
public class EntityFixture
{
    public bool IsConverted=true;
    private Dictionary<Type,object> data=new Dictionary<Type,object>();
    public void SetComponentData<T>(T value)=>data[typeof(T)]=value;
    public T GetComponentData<T>()=>data.TryGetValue(typeof(T),out var value)?(T)value:default;
    public Some<T> TryGetComponentData<T>()=>new Some<T>(GetComponentData<T>());
}
public sealed class AdhesionController
{
    public Some<WheelslipController> wheelslipController;
    public float wheelSlide;
    public void ResetState(){wheelslipController.IsSome(out var w);w.ResetState();}
}
public sealed class MassFixture
{
    public TrainCar car;
    public float WeightPerAxle=>137000f/car.numberOfAxles;
    public float TotalBogiesMass=18000;
}
public sealed class Bogie
{
    public bool enabled=true;
    public UnityEngine.Rigidbody rb=new UnityEngine.Rigidbody();
    public UnityEngine.Transform transform=new UnityEngine.GameObject().transform;
    public float brakingForce,maxBrakingForcePerKg;
}
public sealed class NativeTypeFixture{public float wheelRadius=.91f;}
public sealed class NativeLiveryFixture{public NativeTypeFixture parentType=new NativeTypeFixture();}
public sealed class BrakeFixture{public float brakingFactor;}
public sealed class GroundFixture{public bool IsGrounded=true;}
public sealed class TrainCar:UnityEngine.MonoBehaviour
{
    public EntityFixture entity=new EntityFixture();
    public SimController SimController=new SimController();
    public int numberOfAxles=6;
    public int NumberOfAxles=>numberOfAxles;
    public AdhesionController adhesionController=new AdhesionController();
    public MassFixture massController;
    public Bogie[] Bogies={new Bogie(),new Bogie()};
    public Bogie RearBogie=>Bogies[1];
    public UnityEngine.Rigidbody rb=new UnityEngine.Rigidbody();
    public NativeLiveryFixture carLivery=new NativeLiveryFixture();
    public BrakeFixture brakeSystem=new BrakeFixture();
    public GroundFixture groundFriction=new GroundFixture();
    public bool derailed,isEligibleForSleep;
    public void ForceOptimizationState(bool sleep){}
    public static TrainCar Resolve(UnityEngine.GameObject go)=>go.GetComponent<TrainCar>();
    public static TrainCar Create()
    {
        var go=new UnityEngine.GameObject();var car=go.Add<TrainCar>();var sim=car.SimController;
        var def=new UnityEngine.GameObject().Add<LocoSim.Definitions.ConfigurablePortDefinition>();
        go.Add<UnityEngine.FixtureLinks>().paths.Add("[sim]/poweredAxles",def.transform);
        var wheelGo=new UnityEngine.GameObject();wheelGo.transform.parent=car.transform;wheelGo.Add<PoweredWheelRotationViaAnimation>();
        sim.simFlow.ports.Add("powered",new Port{Value=4});sim.simFlow.ports.Add("torque",new Port{Value=91000});
        sim.drivingForce=go.Add<DrivingForce>();sim.drivingForce.torqueGeneratedPortId="torque";sim.drivingForce.Init(car,sim.simFlow);
        sim.wheelslipController=go.Add<WheelslipController>();var slip=sim.wheelslipController;
        slip.car=car;slip.DrivingForce=sim.drivingForce;slip.numberOfPoweredAxlesPort=sim.simFlow.ports["powered"];slip.engineBrakingActivePort=new Port();
        sim.poweredWheels.poweredWheels=Enumerable.Range(0,4).Select(i=>new LocoSim.Implementations.Wheels.PoweredWheel{index=(byte)i}).ToArray();
        car.adhesionController.wheelslipController=new Some<WheelslipController>(slip);car.massController=new MassFixture{car=car};
        car.entity.SetComponentData(new AdhesionControllerSystem.StaticSharedAdhesionCalculationData{numOfAxles=6,bogieCount=2});
        car.entity.SetComponentData(new AdhesionControllerSystem.StaticAdhesionCalculationData{wheelslipFrictionCoef=.25f,wheelSlideFrictionCoef=.18f});
        return car;
    }
}
