using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RearrangedS060
{
    // Visual LP exhaust return only. The existing compound owner and single
    // chimney already carry LP exhaust; this introduces no ports or readers.
    internal sealed class S060MeyerExhaustPipe : IDisposable
    {
        // Keep the unit collector and body-side bend below the cab floor even
        // when suspension lifts the rear unit. The fixed return runs low along
        // the right frame and rises only beside the smokebox.
        internal const float Radius = .095f, HeaderY = 1.40f, SleeveLength = 1.18f, CourseX = 1.82f, CourseY = 1.32f;
        internal static readonly Vector3 RearBallPosition = new Vector3(1.10f, HeaderY, 1.70f);
        internal static readonly Vector3 BodyBallPosition = new Vector3(1.60f, 1.32f, -4.95f);
        // Exact B99 smokebox sheet intersection and outward face normal.
        internal static readonly Vector3 SmokeboxSurface = new Vector3(.5529617f,2.80f,3.50f);
        internal static readonly Vector3 SmokeboxNormal = new Vector3(.7730095f,.6343944f,0);
        internal static readonly Vector3 SmokeboxPosition = SmokeboxSurface + SmokeboxNormal*.08f;
        internal Transform BodyRoot { get; private set; }
        internal Transform RearRoot { get; private set; }
        internal Transform RearJoint { get; private set; }
        internal Transform BodyJoint { get; private set; }
        internal Transform SmokeboxJoint { get; private set; }
        internal Transform InnerSleeve { get; private set; }
        internal Transform OuterSleeve { get; private set; }
        internal Transform SlipCollar { get; private set; }
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<MeshRenderer> renderers = new List<MeshRenderer>();
        private readonly List<GameObject> objects = new List<GameObject>();
        private MeshRenderer materialSource;
        private Material material;
        private Bend connection;
        private bool disposed;

        internal static S060MeyerExhaustPipe Bind(Transform body, Transform rear, Transform nativeShell)
        {
            var owner = new S060MeyerExhaustPipe();
            try
            {
                owner.materialSource = nativeShell.GetComponentsInChildren<MeshRenderer>(true)
                    .Where(r => r.name == "s060_body" && r.sharedMaterial != null)
                    .OrderByDescending(r => r.enabled && r.gameObject.activeInHierarchy).First();
                owner.material = owner.materialSource.sharedMaterial;
                owner.BodyRoot = owner.Group(body, "[Meyer single rear steam trunk]");
                owner.RearRoot = owner.Group(rear, "[Meyer rear exhaust collector]");
                foreach (int side in new[] { -1, 1 })
                {
                    var chest = rear.Find("[Round cylinders " + (side < 0 ? "L" : "R") + "]/Valve chest");
                    if (chest == null) throw new InvalidOperationException("Native LP chest takeoff required.");
                    // The donor cylinder's local Y is its longitudinal axis.
                    // Locate its top in unit coordinates, not in mesh-local axes.
                    var points=chest.GetComponent<MeshFilter>().sharedMesh.vertices.Select(p=>rear.InverseTransformPoint(chest.TransformPoint(p))).ToArray();
                    var b=new Bounds(points[0],Vector3.zero);foreach(var p in points)b.Encapsulate(p);
                    var skin=new Vector3(b.center.x,b.max.y,Mathf.Clamp(1.70f,b.min.z+.04f,b.max.z-.04f));
                    var end = new Vector3(skin.x,HeaderY,1.70f);var direction=(end-skin).normalized;
                    var cap=skin+direction*.030f;
                    owner.Render(owner.RearRoot,"LP chest union "+side,SmoothPathMesh(new[]{skin-direction*.045f,cap},.095f));
                    owner.Marker(owner.RearRoot, "[LP exhaust takeoff " + side + "]", cap);
                    owner.Render(owner.RearRoot, "LP chest outlet " + side, SmoothPathMesh(new[] {
                        cap, end }, .075f, .24f));
                }
                owner.Render(owner.RearRoot, "LP collector", OpenEnds(SmoothPathMesh(new[] { new Vector3(-1.10f, HeaderY, 1.70f), RearBallPosition }, Radius),false,true));
                owner.RearJoint = owner.Marker(owner.RearRoot, "[LP unit union]", RearBallPosition);
                owner.BodyJoint = owner.Marker(owner.BodyRoot, "[LP body union]", BodyBallPosition);
                owner.Render(owner.BodyRoot, "Single underframe steam trunk", OpenEnds(SmoothPathMesh(new[] {
                    BodyBallPosition, BodyBallPosition+Vector3.back*.40f,
                    new Vector3(CourseX,CourseY,-5.35f),
                    // Pass below the front HP unit's moving admission bend.
                    new Vector3(CourseX,CourseY,-2.55f),new Vector3(CourseX,.90f,-2.05f),
                    new Vector3(CourseX,.90f,-.35f),new Vector3(CourseX,CourseY,.05f),
                    new Vector3(CourseX,CourseY,2.70f), new Vector3(CourseX,2.35f,3.35f),
                    SmokeboxSurface+SmokeboxNormal*.38f,SmokeboxPosition },Radius,.30f),true,false));
                owner.SmokeboxJoint = owner.Marker(owner.BodyRoot, "[LP smokebox socket]", SmokeboxPosition);
                owner.Render(owner.BodyRoot, "Smokebox inlet flange", SmoothPathMesh(new[] { SmokeboxSurface-SmokeboxNormal*.02f, SmokeboxPosition }, .125f));
                foreach (var mount in new[] { new Vector2(1.28f,.50f), new Vector2(1.28f,1.85f) })
                {
                    // Reuse the native running plate as the hanger seat. Keep
                    // the span above the two existing HP feeds, with the drop
                    // outside them; the return retains its 190 mm diameter.
                    float z=mount.y;
                    float plate = RunningPlateHeight(body, nativeShell, mount.x, z);
                    var seat = new Vector3(mount.x, plate - .02f, z);
                    owner.Marker(owner.BodyRoot, "[Frame hanger seat " + z + "]", seat);
                    owner.Render(owner.BodyRoot, "Frame hanger " + z, SmoothPathMesh(new[] {
                        seat, new Vector3(CourseX, seat.y, z), new Vector3(CourseX, CourseY+Radius+.009f, z) }, .018f, .08f));
                    owner.Render(owner.BodyRoot, "Frame pipe strap " + z, SmoothPathMesh(new[] {
                        new Vector3(CourseX,CourseY,z-.018f), new Vector3(CourseX,CourseY,z+.018f) }, Radius+.009f));
                }
                owner.connection = new Bend(Radius);
                owner.connection.Part = owner.Render(owner.BodyRoot, "Rear articulated bend", owner.connection.Mesh);
                owner.InnerSleeve = owner.connection.Part;
                owner.UpdateGeometry(); return owner;
            }
            catch { owner.Dispose(); throw; }
        }
        internal void UpdateGeometry()
        {
            if (disposed || BodyRoot == null) return;
            if (materialSource != null && materialSource.sharedMaterial != material)
            {
                material = materialSource.sharedMaterial;
                foreach (var renderer in renderers) if (renderer != null) renderer.sharedMaterial = material;
            }
            connection.Move(BodyRoot.InverseTransformPoint(RearJoint.position), BodyBallPosition,
                BodyRoot.InverseTransformDirection(RearRoot.TransformDirection(Vector3.right)), Vector3.back);
        }
        private static float RunningPlateHeight(Transform body, Transform shell, float x, float z)
        {
            float top = float.NegativeInfinity;
            foreach (var f in shell.GetComponentsInChildren<MeshFilter>(true).Where(f => (f.name == "s060_body" || f.name == "s060_tank_floor") && f.sharedMesh != null && f.sharedMesh.isReadable))
            {
                var v = f.sharedMesh.vertices.Select(p => body.InverseTransformPoint(f.transform.TransformPoint(p))).ToArray(); var t = f.sharedMesh.triangles;
                for (int i = 0; i < t.Length; i += 3)
                {
                    var a = v[t[i]]; var b = v[t[i + 1]]; var c = v[t[i + 2]];
                    float d = (b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z); if (Mathf.Abs(d) < 1e-9f) continue;
                    float u = ((b.z-c.z)*(x-c.x)+(c.x-b.x)*(z-c.z))/d;
                    float w = ((c.z-a.z)*(x-c.x)+(a.x-c.x)*(z-c.z))/d;
                    if (u < 0 || w < 0 || u+w > 1) continue;
                    float y = u*a.y+w*b.y+(1-u-w)*c.y; if (y > 1.45f && y < 1.70f) top = Mathf.Max(top,y);
                }
            }
            if (float.IsNegativeInfinity(top)) throw new InvalidOperationException("Measured native running plate required for pipe hangers.");
            return top;
        }

        private Transform Group(Transform parent, string name)
        { var go = new GameObject(name); objects.Add(go); go.transform.SetParent(parent, false); return go.transform; }
        private Transform Marker(Transform parent, string name, Vector3 position)
        { var part = Group(parent, name); part.localPosition = position; return part; }
        private Transform Ball(Transform parent, string name, Vector3 position, float radius)
        {
            var part = Render(parent, name, JointMesh(radius));
            part.localPosition = position; return part;
        }
        internal static Mesh JointMesh(float radius)
        {
            // Build the joint as lathe rings rather than importing a model or
            // giving a primitive the locomotive's complete paint atlas.
            const int rings = 9; var centres = new Vector3[rings]; var radii = new float[rings];
            for (int i = 0; i < rings; i++)
            { float angle = Mathf.PI * i / (rings - 1); centres[i] = new Vector3(0, 0, -radius * Mathf.Cos(angle)); radii[i] = Mathf.Max(.001f, radius * Mathf.Sin(angle)); }
            var mesh = Tube(centres, radii); mesh.RecalculateNormals(); return mesh;
        }
        private Transform Pipe(Transform parent, string name, float radius, Vector3[] points, bool rounded = true)
            => Render(parent, name, PathMesh(points, radius, rounded));
        internal static Mesh PathMesh(Vector3[] points, float radius, bool rounded = true)
        {
            var centres = rounded ? Rounded(points) : points;
            var radii = new float[centres.Length]; for (int i = 0; i < radii.Length; i++) radii[i] = radius;
            return Tube(centres, radii);
        }
        private Transform Render(Transform parent, string name, Mesh mesh)
        {
            meshes.Add(mesh); mesh.name = "Meyer LP return " + name;
            var part = Group(parent, name); part.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderers.Add(renderer);
            return part;
        }
        private static Vector3[] Rounded(Vector3[] points)
        {
            var result = new List<Vector3> { points[0] };
            for (int i = 1; i < points.Length - 1; i++)
            {
                float cut = Mathf.Min(.18f, Mathf.Min(Vector3.Distance(points[i - 1], points[i]), Vector3.Distance(points[i], points[i + 1])) * .3f);
                var a = points[i] + (points[i - 1] - points[i]).normalized * cut;
                var b = points[i] + (points[i + 1] - points[i]).normalized * cut;
                for (int j = 0; j <= 4; j++)
                { float t = j / 4f; result.Add((1 - t) * (1 - t) * a + 2 * (1 - t) * t * points[i] + t * t * b); }
            }
            result.Add(points[points.Length - 1]); return result.ToArray();
        }
        internal static Mesh Tube(Vector3[] centres, float[] radii, int sides = 16)
        {
            var vertices = new Vector3[centres.Length * sides + 2]; var normals = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length]; var triangles = new int[(centres.Length - 1) * sides * 6 + sides * 6];
            int index = 0;
            Vector3 previousTangent = Vector3.forward, previousRight = Vector3.right;
            for (int ring = 0; ring < centres.Length; ring++)
            {
                var tangent = (centres[Math.Min(ring + 1, centres.Length - 1)] - centres[Math.Max(0, ring - 1)]).normalized;
                var reference = Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > .95f ? Vector3.forward : Vector3.up;
                var right = ring == 0 ? Vector3.Cross(reference, tangent).normalized : (Quaternion.FromToRotation(previousTangent, tangent) * previousRight).normalized;
                var up = Vector3.Cross(tangent, right);
                previousTangent = tangent; previousRight = right;
                for (int side = 0; side < sides; side++)
                {
                    float angle = 2 * Mathf.PI * side / sides; int v = ring * sides + side;
                    var normal = right * Mathf.Cos(angle) + up * Mathf.Sin(angle);
                    vertices[v] = centres[ring] + normal * radii[ring]; normals[v] = normal;
                    uv[v] = new Vector2(.6868f, .8948f) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * .004f;
                    if (ring == 0) continue;
                    int a = (ring - 1) * sides + side, b = (ring - 1) * sides + (side + 1) % sides;
                    int c = v, d = ring * sides + (side + 1) % sides;
                    triangles[index++] = a; triangles[index++] = b; triangles[index++] = c;
                    triangles[index++] = b; triangles[index++] = d; triangles[index++] = c;
                }
            }
            int start = vertices.Length - 2, end = vertices.Length - 1;
            vertices[start] = centres[0]; vertices[end] = centres[centres.Length - 1];
            normals[start] = (centres[0] - centres[1]).normalized; normals[end] = (centres[centres.Length - 1] - centres[centres.Length - 2]).normalized;
            uv[start] = uv[end] = new Vector2(.6868f, .8948f);
            for (int side = 0; side < sides; side++)
            {
                triangles[index++] = start; triangles[index++] = (side + 1) % sides; triangles[index++] = side;
                triangles[index++] = end; triangles[index++] = (centres.Length - 1) * sides + side; triangles[index++] = (centres.Length - 1) * sides + (side + 1) % sides;
            }
            var mesh = new Mesh();
            try { mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uv; mesh.triangles = triangles; mesh.RecalculateBounds(); return mesh; }
            catch { Release(mesh); throw; }
        }
        internal static Mesh SmoothPathMesh(Vector3[] points, float radius, float bend = .35f)
        {
            var centres = new List<Vector3> { points[0] };
            for (int i = 1; i + 1 < points.Length; i++)
            {
                var u = (points[i]-points[i-1]).normalized; var v = (points[i+1]-points[i]).normalized;
                float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(u,v),-1,1));
                if (angle < .001f || angle > 3.13f) { centres.Add(points[i]); continue; }
                float cut = Mathf.Min(bend * Mathf.Tan(angle/2), .45f * Mathf.Min(Vector3.Distance(points[i],points[i-1]),Vector3.Distance(points[i+1],points[i])));
                float r = cut/Mathf.Tan(angle/2); var normal = Vector3.Cross(u,v).normalized;
                var start = points[i]-u*cut; var centre = start+Vector3.Cross(normal,u)*r; var radial = start-centre;
                int steps = Mathf.Max(4,Mathf.CeilToInt(angle/(Mathf.PI/24)));
                for (int j=0;j<=steps;j++) centres.Add(centre+Quaternion.AngleAxis(angle*Mathf.Rad2Deg*j/steps,normal)*radial);
            }
            centres.Add(points[points.Length-1]); return Tube(centres.ToArray(),Enumerable.Repeat(radius,centres.Count).ToArray(),24);
        }
        private static Mesh OpenEnds(Mesh mesh,bool start,bool end)
        {
            const int sides=24;var t=mesh.triangles;int first=t.Length-sides*6;
            mesh.triangles=t.Where((v,i)=>i<first||(((i-first)%6<3)?!start:!end)).ToArray();return mesh;
        }
        // Cached cubic bend. Only the short unit/body connections deform; the
        // long pipes remain rigid. Radius is never scaled with articulation.
        internal sealed class Bend
        {
            internal readonly Mesh Mesh;
            internal Transform Part;
            private const int Rings=25, Sides=24;
            private readonly Vector3[] points=new Vector3[Rings], vertices=new Vector3[Rings*Sides+2], normals=new Vector3[Rings*Sides+2];
            private Vector3 previousA,previousB,previousU,previousV; private bool initialized;
            internal Bend(float radius)
            {
                Radius=radius; for(int i=0;i<Rings;i++) points[i]=Vector3.forward*i/(Rings-1);
                Mesh=OpenEnds(Tube(points,Enumerable.Repeat(radius,Rings).ToArray(),Sides),true,true); Mesh.MarkDynamic();
            }
            internal float Radius { get; }
            internal void Move(Vector3 a,Vector3 b,Vector3 u,Vector3 v)
            {
                if(!S060PipePose.Finite(a)||!S060PipePose.Finite(b)||!S060PipePose.Direction(u)||!S060PipePose.Direction(v)) return;
                u.Normalize();v.Normalize(); float h=Mathf.Min(.38f,Vector3.Distance(a,b)*.45f);
                if(initialized && Vector3.Distance(a,previousA)+Vector3.Distance(b,previousB)+.38f*(Vector3.Distance(u,previousU)+Vector3.Distance(v,previousV)) < S060PipePose.MaxError/4) return;
                var c=a+u*h;var d=b-v*h;
                for(int i=0;i<Rings;i++){float t=i/(float)(Rings-1),s=1-t;points[i]=s*s*s*a+3*s*s*t*c+3*s*t*t*d+t*t*t*b;}
                Vector3 lastTangent=Vector3.forward,lastRight=Vector3.right;
                for(int ring=0;ring<Rings;ring++)
                {
                    var tangent=ring==0?u:ring==Rings-1?v:(points[ring+1]-points[ring-1]).normalized;
                    var reference=Mathf.Abs(Vector3.Dot(tangent,Vector3.up))>.95f?Vector3.forward:Vector3.up;
                    var right=ring==0?Vector3.Cross(reference,tangent).normalized:(Quaternion.FromToRotation(lastTangent,tangent)*lastRight).normalized;
                    var up=Vector3.Cross(tangent,right);lastTangent=tangent;lastRight=right;
                    for(int side=0;side<Sides;side++){float angle=2*Mathf.PI*side/Sides;int j=ring*Sides+side;normals[j]=right*Mathf.Cos(angle)+up*Mathf.Sin(angle);vertices[j]=points[ring]+normals[j]*Radius;}
                }
                vertices[vertices.Length-2]=a;vertices[vertices.Length-1]=b;
                normals[normals.Length-2]=-u;normals[normals.Length-1]=v;
                Mesh.vertices=vertices;Mesh.normals=normals;Mesh.RecalculateBounds();
                previousA=a;previousB=b;previousU=u;previousV=v;initialized=true;
            }
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            foreach (var part in objects) if (part != null) { part.SetActive(false); Release(part); }
            objects.Clear();
            foreach (var mesh in meshes) if (mesh != null) Release(mesh);
            meshes.Clear(); renderers.Clear(); materialSource = null; material = null;
        }
        private static void Release(Object value) { if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
