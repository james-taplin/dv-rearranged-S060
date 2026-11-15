// Only Unity/game/UMM boundaries are replaced. MultiplayerSync and Settings are actual sources.
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field,AllowMultiple=true)]public sealed class HeaderAttribute:Attribute{public HeaderAttribute(string value){}}
    [AttributeUsage(AttributeTargets.Field)]public sealed class SpaceAttribute:Attribute{public SpaceAttribute(int value){}}
    public class Component {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>()where T:class=>gameObject.GetComponent<T>();}
    public class MonoBehaviour:Component { }
    public sealed class GameObject
    {
        public readonly Transform transform=new();
        readonly List<Component> components=new();
        public T AddComponent<T>()where T:Component,new(){var value=new T{gameObject=this};components.Add(value);return value;}
        public T GetComponent<T>()where T:class=>components.OfType<T>().FirstOrDefault();
    }
    public sealed class Transform{public bool MeshReady;public Transform Find(string path)=>MeshReady?this:null;}
    public static class Time {public static float unscaledTime;}
    public static class Mathf {public static float Abs(float value)=>Math.Abs(value); public static float Clamp(float v,float a,float b)=>Math.Clamp(v,a,b);}
    public static class GUILayout {public static readonly List<string> Labels=new();public static void Label(string value)=>Labels.Add(value);}
}
namespace UnityModManagerNet
{
    public enum DrawType{Auto,Toggle,ToggleGroup,Slider}
    [AttributeUsage(AttributeTargets.Field)]public sealed class DrawAttribute:Attribute{public int Height,Precision;public double Min,Max;public DrawAttribute(string value,DrawType type=DrawType.Auto){}}
    public interface IDrawable{}
    public static class UnityModManager
    {
        public class ModEntry{public bool Active;}
        public static ModEntry CurrentMod;
        public static ModEntry FindMod(string id)=>CurrentMod;
        public class ModSettings{public virtual void Save(ModEntry entry){}public static void Save<T>(T value,ModEntry entry){}}
    }
}
namespace DV.ThingTypes{public enum TrainCarType{LocoSteamHeavy,Other}}
namespace DV.Utils{}
namespace LocoMeshSplitter.MeshLoaders{public sealed class S060MeshLoader:UnityEngine.Component{}}
public sealed class TrainCar:UnityEngine.Component
{
    public string ID,CarGUID;
    public DV.ThingTypes.TrainCarType carType=DV.ThingTypes.TrainCarType.LocoSteamHeavy;
    public object logicCar=new();public Adhesion adhesionController=new();
    public sealed class Adhesion{public bool IsWheelSliding;}
    public Entity entity=new();
    public Controller SimController=new();
    public bool BogiesReady=true,Moving,isExploded,derailed;
    public float GetForwardSpeed()=>Moving?1f:0f;
    public bool NormalLivery=true,uniqueCar,Restored;
    public bool AreBogiesFullyInitialized()=>BogiesReady;
    public sealed class Entity{public bool IsConverted=true;}
    public sealed class Controller{public Flow simFlow=new();public PoweredWheels poweredWheels=new();public Drive drivingForce=new();public Slip wheelslipController=new();public object tractionPortsFeeder=new();}
    public sealed class Drive{public float generatedForce;}
    public sealed class Slip{public bool IsWheelslipping;}
    public sealed class InputPort{public float Value;}
    public sealed class Flow{public bool PortReady=true;public InputPort Throttle=new();public bool TryGetPort(string name,out InputPort port){port=PortReady?Throttle:null;return PortReady;}}
    public sealed class PoweredWheels{public object poweredWheels=new();}
}
public sealed class ExplosionModelHandler:UnityEngine.Component { public bool usingExplodedModel; }
public sealed class CarSpawner
{
    private static CarSpawner current=new();
    public static int InstanceReads;
    public static CarSpawner Instance{get{InstanceReads++;return current;}set{current=value;}}
    public readonly List<TrainCar> AllCars=new();
    public event Action<TrainCar> CarAboutToBeDeleted;
    // Native deletion removes AllCars and emits this event before either pool
    // return or PrepareForDestroy/Object.Destroy, including world unload.
    public void Delete(TrainCar car){AllCars.Remove(car);CarAboutToBeDeleted?.Invoke(car);}
}
namespace RearrangedS060
{
    internal static class S060Parts { internal static bool IsS060(TrainCar car)=>car!=null&&car.NormalLivery; }
    internal static class Main
    {
        internal static string ModPath=System.IO.Path.GetTempPath(),RunningVersion="test-core";
        internal static readonly ModLogger Logger=new();
        internal sealed class ModLogger { internal readonly List<string> Rows=new(); internal void LogException(string value,Exception error)=>Rows.Add(value+error); internal void Warning(string value)=>Rows.Add(value); internal void Error(string value)=>Rows.Add(value); }
    }
    internal sealed class S060Conversion
    {
        internal S060Selection Selection;
        internal bool CleanupPending;
        internal static readonly Dictionary<TrainCar,S060Conversion> Active=new();
        internal static S060Conversion Current(TrainCar car)=>Active.TryGetValue(car,out var c)?c:null;
    }
    internal sealed class S060Lifecycle:UnityEngine.MonoBehaviour
    {
        internal bool HasPending,Refuse;
        internal int Attempts;
        internal bool SawRemote,Restoring;
        internal static bool ApplyLocal(TrainCar car,S060Selection selection,out string reason,bool restoring=false)
        {
            var owner=car.GetComponent<S060Lifecycle>()??car.gameObject.AddComponent<S060Lifecycle>();
            owner.Attempts++;owner.SawRemote=S060MultiplayerSync.ApplyingRemote;owner.Restoring=restoring;
            if(!S060MultiplayerSync.BeforeSwitch(car)||owner.Refuse||car.Moving&&!owner.SawRemote)
            {reason="Stopped-car fixture refusal";return false;}
            S060Conversion.Active[car]=new S060Conversion{Selection=selection};owner.HasPending=false;
            S060MultiplayerSync.OnApplied(car,selection);reason="Applied";return true;
        }
    }
}
