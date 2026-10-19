// Minimal native/Unity stand-ins for managed execution of the actual Delta bridge.
// They cover hierarchy, transforms and visibility, not native rendering/physics.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using N = System.Numerics;
#pragma warning disable CS0649 // Native-field inventory is populated by fixture reflection.

namespace UnityEngine {
    public class Object {
        public string name;
        internal bool destroyed;
        public static implicit operator bool(Object o) => !ReferenceEquals(o,null) && !o.destroyed;
        public static bool operator ==(Object a,Object b) {
            bool an=ReferenceEquals(a,null)||a.destroyed,bn=ReferenceEquals(b,null)||b.destroyed;
            return an||bn ? an==bn : ReferenceEquals(a,b);
        }
        public static bool operator !=(Object a,Object b)=>!(a==b);
        public override bool Equals(object obj)=>ReferenceEquals(this,obj);
        public override int GetHashCode()=>System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
        public static void Destroy(Object o) {
            if (o == null || o.destroyed) return;
            if (o is GameObject go) {
                foreach (var c in go.components.ToArray()) Destroy(c);
                go.transform.parent?.Children.Remove(go.transform);
                go.SetActive(false);
            } else if (o is Component c) {
                c.GetType().GetMethod("OnDestroy", BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)?.Invoke(c, null);
                c.gameObject.components.Remove(c);
            }
            o.destroyed = true;
        }
        public static void DestroyImmediate(Object o) => Destroy(o);
    }
    public class Component : Object {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
        public Component GetComponent(Type t) => gameObject.GetComponent(t);
        public T[] GetComponents<T>() where T : Component => gameObject.GetComponents<T>();
        public Component[] GetComponents(Type t) => gameObject.components.Where(t.IsInstanceOfType).ToArray();
        public T[] GetComponentsInChildren<T>(bool includeInactive=false) where T : Component => gameObject.GetComponentsInChildren<T>(includeInactive);
        public T GetComponentInChildren<T>(bool includeInactive=false) where T : Component => GetComponentsInChildren<T>(includeInactive).FirstOrDefault();
        public T GetComponentInParent<T>() where T : Component {
            for (var t=transform; t!=null; t=t.parent) { var found=t.GetComponent<T>(); if(found) return found; }
            return null;
        }
    }
    public class Behaviour : Component { public bool enabled=true; }
    public class MonoBehaviour : Behaviour { }
    public sealed class GameObject : Object {
        internal readonly List<Component> components = new();
        public readonly Transform transform;
        public int layer;
        public bool activeSelf=true;
        public bool activeInHierarchy => activeSelf && (transform.parent==null || transform.parent.gameObject.activeInHierarchy);
        public GameObject(string objectName="GameObject") { name=objectName; transform=new Transform {gameObject=this}; components.Add(transform); }
        public void SetActive(bool value) => activeSelf=value;
        public T AddComponent<T>() where T:Component,new() { var c=new T {gameObject=this}; components.Add(c); return c; }
        public Component AddComponent(Type type) {var c=(Component)Activator.CreateInstance(type); c.gameObject=this;components.Add(c);return c;}
        public T GetComponent<T>() where T:Component => components.OfType<T>().FirstOrDefault(c=>!c.destroyed);
        public Component GetComponent(Type type) => components.FirstOrDefault(c=>!c.destroyed&&type.IsInstanceOfType(c));
        public T[] GetComponents<T>() where T:Component => components.OfType<T>().Where(c=>!c.destroyed).ToArray();
        public T[] GetComponentsInChildren<T>(bool includeInactive=false) where T:Component {
            var found=new List<T>();
            void Visit(Transform t) {
                if(t.gameObject.destroyed) return;
                if(includeInactive||t.gameObject.activeInHierarchy) found.AddRange(t.gameObject.GetComponents<T>());
                foreach(var child in t.Children.ToArray()) Visit(child);
            }
            Visit(transform);return found.ToArray();
        }
    }
    public struct Vector3 {
        public float x,y,z;
        public Vector3(float a,float b,float c) {x=a;y=b;z=c;}
        public static Vector3 one=>new(1,1,1);
        public static Vector3 zero=>new(0,0,0);
        public static Vector3 up=>new(0,1,0);
        public static Vector3 forward=>new(0,0,1);
        internal N.Vector3 Native=>new(x,y,z);
        internal static Vector3 From(N.Vector3 v)=>new(v.X,v.Y,v.Z);
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a,float b)=>new(a.x*b,a.y*b,a.z*b);
        public static Vector3 operator /(Vector3 a,float b)=>a*(1/b);
        public static Vector3 Scale(Vector3 a,Vector3 b)=>new(a.x*b.x,a.y*b.y,a.z*b.z);
        public override string ToString()=>$"({x},{y},{z})";
    }
    public struct Quaternion {
        public float x,y,z,w;
        public Quaternion(float a,float b,float c,float d) {x=a;y=b;z=c;w=d;}
        internal N.Quaternion Native=>new(x,y,z,w);
        internal static Quaternion From(N.Quaternion q)=>new(q.X,q.Y,q.Z,q.W);
        public static Quaternion identity=>new(0,0,0,1);
        public static Quaternion Euler(float x,float y,float z)=>From(N.Quaternion.CreateFromYawPitchRoll(y*Mathf.PI/180,x*Mathf.PI/180,z*Mathf.PI/180));
        public static Quaternion Euler(Vector3 v)=>Euler(v.x,v.y,v.z);
        public static Quaternion Inverse(Quaternion q)=>From(N.Quaternion.Inverse(q.Native));
        public static Quaternion operator *(Quaternion a,Quaternion b)=>From(a.Native*b.Native);
        public static Vector3 operator *(Quaternion a,Vector3 b)=>Vector3.From(N.Vector3.Transform(b.Native,a.Native));
    }
    public sealed class Transform : Component, IEnumerable<Transform> {
        internal readonly List<Transform> Children=new();
        private Transform parentValue;
        public new string name {get=>gameObject.name;set=>gameObject.name=value;}
        public Vector3 localPosition,localScale=Vector3.one;
        public Quaternion localRotation=Quaternion.identity;
        public Transform parent {get=>parentValue;set=>SetParent(value,true);}
        public int childCount=>Children.Count;
        public Vector3 position {get=>parent==null?localPosition:parent.TransformPoint(localPosition);set=>localPosition=parent==null?value:parent.InverseTransformPoint(value);}
        public Quaternion rotation {get=>parent==null?localRotation:parent.rotation*localRotation;set=>localRotation=parent==null?value:Quaternion.Inverse(parent.rotation)*value;}
        public Vector3 lossyScale=>parent==null?localScale:Vector3.Scale(parent.lossyScale,localScale);
        public Vector3 forward=>rotation*Vector3.forward;
        public Transform GetChild(int n)=>Children[n];
        public void SetParent(Transform p,bool worldPositionStays=true) {
            var pos=position;var rot=rotation;var size=lossyScale;
            parentValue?.Children.Remove(this);parentValue=p;p?.Children.Add(this);
            if(worldPositionStays) {position=pos;rotation=rot;localScale=p==null?size:Divide(size,p.lossyScale);}
        }
        public void SetPositionAndRotation(Vector3 p,Quaternion q) {position=p;rotation=q;}
        private static Vector3 Divide(Vector3 a,Vector3 b)=>new(a.x/b.x,a.y/b.y,a.z/b.z);
        public Vector3 TransformPoint(Vector3 v)=>position+rotation*Vector3.Scale(v,lossyScale);
        public Vector3 InverseTransformPoint(Vector3 v)=>Divide(Quaternion.Inverse(rotation)*(v-position),lossyScale);
        public Transform Find(string path) {
            var parts=path.Split('/');var t=this;
            foreach(var part in parts) {t=t.Children.FirstOrDefault(c=>c.name==part);if(t==null)return null;}
            return t;
        }
        public bool IsChildOf(Transform t) {for(var p=this;p!=null;p=p.parent)if(ReferenceEquals(p,t))return true;return false;}
        public IEnumerator<Transform> GetEnumerator()=>Children.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    public static class Mathf {
        public const float PI=(float)Math.PI;
        public static float Clamp(float v,float min,float max)=>Math.Max(min,Math.Min(max,v));
        public static float Abs(float v)=>Math.Abs(v);
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Min(float a,float b)=>Math.Min(a,b);
        public static bool Approximately(float a,float b)=>Math.Abs(a-b)<Math.Max(.000001f,Math.Max(Math.Abs(a),Math.Abs(b))*.000001f);
    }
    public sealed class Mesh : Object { }
    public sealed class Material : Object { }
    public sealed class MeshFilter : Component {public Mesh sharedMesh;}
    public class Renderer : Component {
        public bool enabled=true,forceRenderingOff,receiveShadows=true;
        public Material[] sharedMaterials=Array.Empty<Material>();
        public Rendering.ShadowCastingMode shadowCastingMode;
        public Rendering.LightProbeUsage lightProbeUsage;
        public Rendering.ReflectionProbeUsage reflectionProbeUsage;
        public Transform probeAnchor;
        public int sortingLayerID,sortingOrder;
    }
    public sealed class MeshRenderer : Renderer { }
    public enum LODFadeMode {None,CrossFade,SpeedTree}
    public struct LOD {
        public float screenRelativeTransitionHeight,fadeTransitionWidth;
        public Renderer[] renderers;
        public LOD(float height,Renderer[] value) {screenRelativeTransitionHeight=height;renderers=value;fadeTransitionWidth=0;}
    }
    public sealed class LODGroup : Behaviour {
        public LODFadeMode fadeMode;
        public bool animateCrossFading;
        public Vector3 localReferencePoint;
        public float size;
        private LOD[] levels=Array.Empty<LOD>();
        public LOD[] GetLODs()=>levels;
        public void SetLODs(LOD[] value)=>levels=value;
        public void RecalculateBounds() { }
    }
}
namespace UnityEngine.Rendering {
    public enum ShadowCastingMode {Off,On,TwoSided,ShadowsOnly}
    public enum LightProbeUsage {Off,BlendProbes,UseProxyVolume,CustomProvided}
    public enum ReflectionProbeUsage {Off,BlendProbes,BlendProbesAndSkybox,Simple}
}
namespace DV.Wheels {
    public sealed class PoweredWheel : UnityEngine.MonoBehaviour {
        public UnityEngine.Transform wheelTransform;
        public UnityEngine.Vector3 localRotationAxis=UnityEngine.Vector3.forward;
        public int index,state;
    }
}
namespace LocoSim.Implementations.Wheels { }
namespace UnityEngine {
    public sealed class Collider : Component { }
    public sealed class Rigidbody : Component { }
    public sealed class Animator : Behaviour { }
}
namespace RearrangedS282 {
    internal enum WheelArrangementType { s260=34,s240=35,s222=36 }
    internal sealed class Settings { public float giant222DriverDiameterMetres=2.8f; }
    internal static class Main { public static readonly Settings settings=new(); public static readonly TestLogger Logger=new(); }
    internal sealed class TestLogger { public readonly List<string> Messages=new(); public void Log(string v)=>Messages.Add(v); }
    internal sealed class B99PhysicsState { public float DriverRadius=.712f; }
    internal partial class WheelRearranger : UnityEngine.MonoBehaviour {
        internal UnityEngine.Transform driveL,driveR;
        internal UnityEngine.Transform firstLeftDriveWheel,secondLeftDriveWheel,thirdLeftDriveWheel,fourthLeftDriveWheel;
        internal UnityEngine.Transform firstRightDriveWheel,secondRightDriveWheel,thirdRightDriveWheel,fourthRightDriveWheel;
        internal UnityEngine.Transform driveAxle1,driveAxle2,driveAxle3,driveAxle4,driveAxle5,driveAxle6;
        internal UnityEngine.Transform leftFirstBrakeCaliper,leftSecondBrakeCaliper,leftThirdBrakeCaliper,leftFourthBrakeCaliper,leftFifthBrakeCaliper,leftSixthBrakeCaliper;
        internal UnityEngine.Transform rightFirstBrakeCaliper,rightSecondBrakeCaliper,rightThirdBrakeCaliper,rightFourthBrakeCaliper,rightFifthBrakeCaliper,rightSixthBrakeCaliper;
        internal UnityEngine.Transform spark1L,spark1R,spark2L,spark2R,spark3L,spark3R,spark4L,spark4R,spark5L,spark5R,spark6L,spark6R;
        internal UnityEngine.Mesh flangedDriver;
        internal B99PhysicsState stage2Physics=new();
        internal float AppliedRadius;
        private void setWheelRadius(float radius)=>AppliedRadius=radius;
        internal void ApplyPrototype()=>ApplyGiantSingleDriverPrototype();
        internal void Align(int id)=>AlignAdditionalPoweredAxles(id);
        internal void Suspend()=>SuspendAdditionalGeometry();
        internal void Dispose()=>DisposeAdditionalGeometry();
        internal UnityEngine.Transform[] OwnedWheels=>(UnityEngine.Transform[])giantSingleWheels.Clone();
    }
}

