// Managed stand-ins preserve Unity's column-vector matrix multiplication order.
// They exercise geometry and ownership, not Unity rendering or Animator evaluation.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using N = System.Numerics;

namespace UnityEngine
{
    public class Object
    {
        public bool destroyed;
        public string name;
        public static void Destroy(Object value) { if(value!=null)value.destroyed=true; }
        public static Mesh Instantiate(Mesh source) => source.Clone();
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) {this.x=x;this.y=y;this.z=z;}
        public static Vector3 zero => new(); public static Vector3 one=>new(1,1,1);
        public static Vector3 right=>new(1,0,0); public static Vector3 up=>new(0,1,0); public static Vector3 forward=>new(0,0,1);
        public float magnitude=>(float)Math.Sqrt(Dot(this,this));
        public Vector3 normalized=>this/magnitude;
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a,float b)=>new(a.x*b,a.y*b,a.z*b);
        public static Vector3 operator /(Vector3 a,float b)=>a*(1/b);
        public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
        public static Vector3 Cross(Vector3 a,Vector3 b)=>new(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
        public N.Vector3 Native=>new(x,y,z);
        public static Vector3 From(N.Vector3 n)=>new(n.X,n.Y,n.Z);
    }
    public struct Quaternion
    {
        public float x,y,z,w;
        public Quaternion(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}
        public static Quaternion identity=>new(0,0,0,1);
        public N.Quaternion Native=>new(x,y,z,w);
        public static Quaternion From(N.Quaternion n)=>new(n.X,n.Y,n.Z,n.W);
        public static Quaternion AngleAxis(float degrees,Vector3 axis)=>From(N.Quaternion.CreateFromAxisAngle(axis.Native,degrees*(float)Math.PI/180));
        public static Quaternion LookRotation(Vector3 forward,Vector3 upwards)
        {
            var z=forward.normalized;var x=Vector3.Cross(upwards,z).normalized;var y=Vector3.Cross(z,x);
            return From(N.Quaternion.CreateFromRotationMatrix(new N.Matrix4x4(x.x,x.y,x.z,0,y.x,y.y,y.z,0,z.x,z.y,z.z,0,0,0,0,1)));
        }
    }
    public struct Matrix4x4
    {
        // Transpose of Unity's matrix, because System.Numerics uses row vectors.
        internal N.Matrix4x4 n;
        public static Matrix4x4 identity=>new(){n=N.Matrix4x4.Identity};
        public static Matrix4x4 TRS(Vector3 p,Quaternion q,Vector3 s)=>new(){n=N.Matrix4x4.CreateScale(s.Native)*N.Matrix4x4.CreateFromQuaternion(q.Native)*N.Matrix4x4.CreateTranslation(p.Native)};
        public static Matrix4x4 operator *(Matrix4x4 a,Matrix4x4 b)=>new(){n=b.n*a.n};
        public Vector3 MultiplyPoint3x4(Vector3 value)=>Vector3.From(N.Vector3.Transform(value.Native,n));
        public Vector3 MultiplyVector(Vector3 value)=>Vector3.From(N.Vector3.TransformNormal(value.Native,n));
        public Matrix4x4 inverse {get {if(!N.Matrix4x4.Invert(n,out var i))throw new InvalidOperationException("Singular fixture matrix");return new(){n=i};}}
    }
    public static class Mathf
    {
        public static float Sign(float v)=>v>=0?1:-1;
        public static float Abs(float v)=>Math.Abs(v); public static float Min(float a,float b)=>Math.Min(a,b); public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Clamp01(float v)=>Math.Clamp(v,0,1);
    }
    public sealed class GameObject : Object
    {
        public readonly Transform transform;
        public bool activeSelf=true;
        internal MeshFilter filter;
        public GameObject(string name){this.name=name;transform=new Transform(this);}
        public void SetActive(bool value){activeSelf=value;}
        public T GetComponent<T>() where T:class=>filter as T;
    }
    public sealed class Transform : Object,IEnumerable<Transform>
    {
        public readonly GameObject gameObject;
        public Transform parent;
        public readonly List<Transform> children=new();
        public Vector3 localPosition=Vector3.zero,localScale=Vector3.one;
        public Quaternion localRotation=Quaternion.identity;
        public new string name {get=>gameObject.name;set=>gameObject.name=value;}
        public Transform(GameObject go){gameObject=go;}
        public Transform Add(string name){var t=new GameObject(name).transform;t.parent=this;children.Add(t);return t;}
        public T GetComponent<T>() where T:class=>gameObject.GetComponent<T>();
        public Transform Find(string path)
        {Transform value=this;foreach(var part in path.Split('/')){value=value.children.FirstOrDefault(c=>c.name==part);if(value==null)return null;}return value;}
        public Matrix4x4 localToWorldMatrix=>(parent==null?Matrix4x4.identity:parent.localToWorldMatrix)*Matrix4x4.TRS(localPosition,localRotation,localScale);
        public Matrix4x4 worldToLocalMatrix=>localToWorldMatrix.inverse;
        public IEnumerator<Transform> GetEnumerator()=>children.GetEnumerator(); IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    public sealed class Mesh : Object
    {
        public bool isReadable=true;
        public Vector3[] vertices=Array.Empty<Vector3>();
        public float[] uv=Array.Empty<float>(),colors=Array.Empty<float>(),tangents=Array.Empty<float>();
        public int[] triangles=Array.Empty<int>();
        public int subMeshCount=2;
        public int vertexCount=>vertices.Length;
        public bool boundsRecalculated,normalsRecalculated,tangentsRecalculated;
        public void RecalculateBounds(){boundsRecalculated=true;} public void RecalculateNormals(){normalsRecalculated=true;} public void RecalculateTangents(){tangentsRecalculated=true;}
        public Mesh Clone()=>new(){name=name,vertices=vertices.ToArray(),uv=uv.ToArray(),colors=colors.ToArray(),tangents=tangents.ToArray(),triangles=triangles.ToArray(),subMeshCount=subMeshCount};
    }
    public sealed class MeshFilter
    {
        public readonly Transform transform;
        public Mesh sharedMesh;
        public MeshFilter(Transform transform,Mesh mesh){this.transform=transform;sharedMesh=mesh;transform.gameObject.filter=this;}
    }
}
namespace RearrangedS282
{
    using UnityEngine;
    internal partial class WheelRearranger
    {
        internal Transform transform,driveL,driveR,lCrosshead,rCrosshead,lLiftingArmSupport,rLiftingArmSupport,reachRodSupport;
        internal int gearCalls;
        private void ShowValveGear(){gearCalls++;reachRodSupport.gameObject.SetActive(true);}
        private void HideReachRod(){foreach(var drive in new[]{driveL,driveR})drive.Find("s282_mech_cutoff_rod").gameObject.SetActive(false);}
        internal void Prepare()=>SetupPacificHudsonValveGear();
        internal void Apply()=>ApplyPacificHudsonValveGear();
        internal void Dispose()=>DisposePacificHudsonValveGear();
    }
    internal static class Main
    {
        internal static readonly LoggerStub Logger=new LoggerStub();
        internal sealed class LoggerStub{internal readonly System.Collections.Generic.List<string> Lines=new System.Collections.Generic.List<string>();internal void Log(string line)=>Lines.Add(line);}
    }
}
