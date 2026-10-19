// Small hierarchy and native-simulation API substitutes for executing actual
// graph ownership/lifecycle code. Native engine math is tested separately.
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace UnityEngine {
 public class Object { public bool destroyed; public static void Destroy(Object o){if(o!=null)o.destroyed=true;} }
 public class Component:Object {public GameObject gameObject; public Transform transform=>gameObject.transform;public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();}
 public class MonoBehaviour:Component {}
 public sealed class Transform:Component {public Transform parent;public readonly List<Transform> children=new();public void SetParent(Transform p,bool b){parent?.children.Remove(this);parent=p;p?.children.Add(this);}public Transform Find(string name)=>children.FirstOrDefault(c=>c.gameObject.name==name);}
 public sealed class GameObject:Object { public string name;public bool activeSelf=true;public Transform transform;private readonly List<Component> components=new();public GameObject(string n=""){name=n;transform=new Transform{gameObject=this};components.Add(transform);}public T AddComponent<T>()where T:Component,new(){var x=new T{gameObject=this};components.Add(x);return x;}public T GetComponent<T>()where T:Component=>components.OfType<T>().FirstOrDefault();public void SetActive(bool b){activeSelf=b;} }
 public static class Mathf {public static float Sqrt(float x)=>(float)Math.Sqrt(x);}
}
namespace HarmonyLib { public class Harmony { public void Patch(object original,HarmonyMethod prefix=null,HarmonyMethod postfix=null){} } public class HarmonyMethod {public HarmonyMethod(Type t,string n){}}public static class AccessTools{public static object Method(Type t,string n)=>null;} }
namespace DV.ThingTypes {public enum TrainCarType {LocoSteamHeavy,Other}}
namespace DV.Damage {public sealed class DamageController:UnityEngine.MonoBehaviour {public string[] mechanicalPTDamagerPortIds,mechanicalPTHealthStateExternalInPortIds;} }
public sealed class TrainCar:UnityEngine.MonoBehaviour { public DV.ThingTypes.TrainCarType carType;public DV.Simulation.Cars.SimController SimController;}
namespace DV.Simulation.Cars {public sealed class SimController:UnityEngine.MonoBehaviour {public LocoSim.Definitions.SimConnectionDefinition connectionsDefinition;public LocoSim.Implementations.SimulationFlow simFlow;public void Initialize(TrainCar c,DV.Damage.DamageController d){}} }
namespace LocoSim.Definitions {
 public enum PortType {OUT,IN,EXTERNAL_IN,READONLY_OUT} public enum PortValueType{GENERIC,TORQUE,MASS_RATE}
 public sealed class PortReferenceDefinition {public string ID;public PortReferenceDefinition(PortValueType t,string id){ID=id;}}
 public sealed class PortDefinition {public string ID;public PortDefinition(PortType t,PortValueType v,string id){ID=id;}}
 public abstract class SimComponentDefinition:UnityEngine.MonoBehaviour {public string ID;public abstract LocoSim.Implementations.SimComponent InstantiateImplementation();}
 public class ReciprocatingSteamEngineDefinition:SimComponentDefinition {public int numCylinders=2;public float cylinderBore=.61f,pistonStroke=.711f,minCutoff=.05f,maxCutoff=.9f,throttleMaxFlow=6,steamChestVolume=1000,passiveDamagePerRev=.01f,noOilDamagePerRev=1,cylinderCrackDamage=100,waterDamagePercentagePerChuff=.025f;public override LocoSim.Implementations.SimComponent InstantiateImplementation()=>new LocoSim.Implementations.ReciprocatingSteamEngine(this);}
 public sealed class MultiplePortSumDefinition:SimComponentDefinition {public PortReferenceDefinition[] inputs;public PortDefinition output;public override LocoSim.Implementations.SimComponent InstantiateImplementation()=>new LocoSim.Implementations.Sum(this);}
 public sealed class EmptyDefinition:SimComponentDefinition{public override LocoSim.Implementations.SimComponent InstantiateImplementation()=>new LocoSim.Implementations.Empty(ID);}
 public sealed class SimConnectionDefinition:UnityEngine.MonoBehaviour{public SimComponentDefinition[] executionOrder;public Connection[] connections;public PortReferenceConnection[] portReferenceConnections;}
 public sealed class Connection{public string fullPortIdOut,fullPortIdIn;public Connection(string o,string i){fullPortIdOut=o;fullPortIdIn=i;}}
 public sealed class PortReferenceConnection{public string portReferenceId,portId;public PortReferenceConnection(string r,string p){portReferenceId=r;portId=p;}}
}
namespace LocoSim.Implementations {
 using LocoSim.Definitions;
 public class Port{public string id;private float value,previous;public float Value{get=>value;set{previous=this.value;this.value=value;if(value!=previous)ValueUpdatedInternally?.Invoke(value);}}public float Diff=>value-previous;public event Action<float> ValueUpdatedInternally;public Port(string s){id=s;}}
 public abstract class SimComponent{public string id;public List<Port> ports=new();protected SimComponent(string s){id=s;}protected Port P(string s){var p=new Port(id+"."+s);ports.Add(p);return p;}public abstract void Tick(float delta);}
 public sealed class Empty:SimComponent{public Empty(string id):base(id){}public override void Tick(float delta){}}
 public sealed class Sum:SimComponent{public Sum(MultiplePortSumDefinition d):base(d.ID){P("OUT");}public override void Tick(float delta){}}
 public class ReciprocatingSteamEngine:SimComponent{
 public float crankRotation,prevCrankRotation,torqueVelocity,waterInCylindersDamageMeter;public int nativeTicks;
 public Port crankRotationReadOut,torqueOut,intakeFlowReadOut,intakeFlowNormalizedReadOut,exhaustFlowReadOut,exhaustPressureReadOut,exhaustTemperatureReadOut,chuffFrequencyReadOut,cylindersInletValveOpen,cylinderCockFlowNormalizedReadOut,cylinderCrackFlowNormalizedReadOut,generatedMechanicalDamageReadOut,ashesInPipesReadOut,healthStateExtIn,isCylinderCrackedReadOut;
 public ReciprocatingSteamEngine(ReciprocatingSteamEngineDefinition d):base(d.ID){crankRotationReadOut=P("CRANK_ROTATION");torqueOut=P("TORQUE_OUT");intakeFlowReadOut=P("INTAKE_FLOW");intakeFlowNormalizedReadOut=P("INTAKE_FLOW_NORMALIZED");exhaustFlowReadOut=P("EXHAUST_FLOW");exhaustPressureReadOut=P("EXHAUST_PRESSURE");exhaustTemperatureReadOut=P("EXHAUST_TEMPERATURE");chuffFrequencyReadOut=P("CHUFF_FREQUENCY");cylindersInletValveOpen=P("CYLINDERS_INLET_VALVE_OPEN");cylinderCockFlowNormalizedReadOut=P("CYLINDER_COCK_FLOW_NORMALIZED");cylinderCrackFlowNormalizedReadOut=P("CYLINDER_CRACK_FLOW_NORMALIZED");generatedMechanicalDamageReadOut=P("GENERATED_MECHANICAL_DAMAGE");ashesInPipesReadOut=P("ASHES_IN_PIPES");healthStateExtIn=P("HEALTH_STATE_EXT_IN");isCylinderCrackedReadOut=P("IS_CYLINDER_CRACKED");}
 public override void Tick(float delta){nativeTicks++;}
 }
 public sealed class SimulationFlow {public List<SimComponent> OrderedSimComps;private Dictionary<string,Port> ports;public SimulationFlow(SimConnectionDefinition graph){OrderedSimComps=graph.executionOrder.Select(d=>d.InstantiateImplementation()).ToList();ports=OrderedSimComps.SelectMany(c=>c.ports).ToDictionary(p=>p.id);}public bool TryGetPort(string s,out Port p)=>ports.TryGetValue(s,out p);public void SetSaveStateData(JObject j){} }
}
namespace RearrangedS282 {internal static class Main{internal static Logger Logger=new();}internal sealed class Logger{internal List<string> warnings=new();internal void Warning(string s){warnings.Add(s);}}}
