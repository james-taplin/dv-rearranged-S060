using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
    public class Object { public bool destroyed; public static void Destroy(Object value) { if (value != null) value.destroyed = true; } }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public static Vector3 zero => new Vector3();
        public float sqrMagnitude => x*x+y*y+z*z;
        public float magnitude => (float)Math.Sqrt(sqrMagnitude);
        public Vector3 normalized => magnitude > 1e-10f ? this/magnitude : zero;
        public static Vector3 operator +(Vector3 a,Vector3 b) => new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b) => new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a,float b) => new Vector3(a.x*b,a.y*b,a.z*b);
        public static Vector3 operator /(Vector3 a,float b) => a*(1f/b);
        public static float Dot(Vector3 a,Vector3 b) => a.x*b.x+a.y*b.y+a.z*b.z;
        public static Vector3 Cross(Vector3 a,Vector3 b) => new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
        public static Vector3 LerpUnclamped(Vector3 a,Vector3 b,float t) => a+(b-a)*t;
    }
    public struct Vector4
    {
        public float x,y,z,w;
        public Vector4(float x,float y,float z,float w) { this.x=x;this.y=y;this.z=z;this.w=w; }
        public static Vector4 zero => new Vector4();
        public static Vector4 LerpUnclamped(Vector4 a,Vector4 b,float t) => new Vector4(a.x+(b.x-a.x)*t,a.y+(b.y-a.y)*t,a.z+(b.z-a.z)*t,a.w+(b.w-a.w)*t);
    }
    public struct Color
    {
        public float r,g,b,a;
        public Color(float r,float g,float b,float a=1f) {this.r=r;this.g=g;this.b=b;this.a=a;}
        public static Color white => new Color(1,1,1);
        public static Color LerpUnclamped(Color a,Color b,float t) => new Color(a.r+(b.r-a.r)*t,a.g+(b.g-a.g)*t,a.b+(b.b-a.b)*t,a.a+(b.a-a.a)*t);
    }
    public struct BoneWeight { }
    public enum MeshTopology { Triangles, Lines }
    public sealed class Mesh : Object
    {
        public string name;
        public bool isReadable=true;
        public Vector3[] vertices=Array.Empty<Vector3>(),normals=Array.Empty<Vector3>();
        public Vector4[] tangents=Array.Empty<Vector4>();
        public Color[] colors=Array.Empty<Color>();
        public BoneWeight[] boneWeights=Array.Empty<BoneWeight>();
        public int blendShapeCount;
        public Rendering.IndexFormat indexFormat=Rendering.IndexFormat.UInt16;
        public bool recalculatedBounds,recalculatedNormals;
        private readonly Dictionary<int,Vector4[]> uv=new Dictionary<int,Vector4[]>();
        private readonly Dictionary<int,int[]> triangles=new Dictionary<int,int[]>();
        private readonly Dictionary<int,MeshTopology> topology=new Dictionary<int,MeshTopology>();
        public int subMeshCount=1;
        public void GetUVs(int channel,List<Vector4> result) { result.Clear();if(uv.TryGetValue(channel,out var data))result.AddRange(data); }
        public Vector4[] Uv(int channel) => uv.TryGetValue(channel,out var data)?data:Array.Empty<Vector4>();
        public void SetUVs(int channel,List<Vector4> data) {uv[channel]=data.ToArray();}
        public MeshTopology GetTopology(int submesh) => topology.TryGetValue(submesh,out var value)?value:MeshTopology.Triangles;
        public void SetTopology(int submesh,MeshTopology value) { topology[submesh]=value; }
        public int[] GetTriangles(int submesh) => triangles.TryGetValue(submesh,out var data)?data.ToArray():Array.Empty<int>();
        public void SetTriangles(List<int> data,int submesh) { triangles[submesh]=data.ToArray(); }
        public void SetVertices(List<Vector3> data) { vertices=data.ToArray(); }
        public void SetNormals(List<Vector3> data) { normals=data.ToArray(); }
        public void SetTangents(List<Vector4> data) { tangents=data.ToArray(); }
        public void SetColors(List<Color> data) { colors=data.ToArray(); }
        public void RecalculateBounds() { recalculatedBounds=true; }
        public void RecalculateNormals() { recalculatedNormals=true;normals=Enumerable.Repeat(new Vector3(0,1,0),vertices.Length).ToArray(); }
    }
}
namespace UnityEngine.Rendering { public enum IndexFormat {UInt16,UInt32} }
