using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RearrangedS060
{
    // The unchanged Shay reducer and a Dockside fit using actual pipe sections.
    // Both retain hollow walls, annular rolled lips and the Shay finish.
    internal static class S060SteamSleeve
    {
        internal static Mesh Create(Vector3 socket, float nativeRadius, float outletOuterRadius = .068f)
        {
            float r = nativeRadius + .004f;
            var y = new[] { .010f, -.008f, -.030f, -.072f, -.095f, -.110f };
            var outer = new[] { r, r, r - .004f, outletOuterRadius + .006f, outletOuterRadius, outletOuterRadius };
            return Hollow(y.Select(v => socket + Vector3.up * v).ToArray(), outer, outer.Select(v => v - .008f).ToArray(), 32);
        }
        internal static void CutTriangle(Vector3 a, Vector3 b, Vector3 c, float y, List<Vector3> section)
        {
            int hits = 0; Vector3 first = Vector3.zero, second = Vector3.zero;
            CutEdge(a,b,y,ref hits,ref first,ref second); CutEdge(b,c,y,ref hits,ref first,ref second); CutEdge(c,a,y,ref hits,ref first,ref second);
            if (hits == 2 && (first-second).sqrMagnitude > 1e-14f) { section.Add(first); section.Add(second); }
        }
        private static void CutEdge(Vector3 a,Vector3 b,float y,ref int hits,ref Vector3 first,ref Vector3 second)
        {
            if ((a.y >= y) == (b.y >= y)) return;
            var point = Vector3.LerpUnclamped(a,b,(y-a.y)/(b.y-a.y)); if(hits++ == 0)first=point;else second=point;
        }
        internal static Vector3[] Section(Vector3[] vertices, int[] triangles, float y)
        {
            var section = new List<Vector3>();
            for (int t = 0; t < triangles.Length; t += 3) CutTriangle(vertices[triangles[t]], vertices[triangles[t + 1]], vertices[triangles[t + 2]], y, section);
            return section.ToArray();
        }
        // The main connected arc is the pipe wall. Native mounting tabs are
        // separate arcs; include those in the outside lip, never in its bore.
        internal static Vector3[] PipeProfile(Vector3[] segments)
        {
            var points = new List<Vector3>(); var edges = new List<(int a, int b)>();
            int Index(Vector3 p)
            {
                int i = points.FindIndex(q => (q - p).sqrMagnitude < 4e-10f);
                if (i >= 0) return i; points.Add(p); return points.Count - 1;
            }
            for (int i = 0; i < segments.Length; i += 2) edges.Add((Index(segments[i]), Index(segments[i + 1])));
            var seen = new HashSet<int>(); var largest = new List<int>();
            for (int i = 0; i < points.Count; i++)
            {
                if (seen.Contains(i)) continue;
                var pending = new Stack<int>(); var component = new List<int>(); pending.Push(i);
                while (pending.Count != 0)
                {
                    int j = pending.Pop(); if (!seen.Add(j)) continue; component.Add(j);
                    foreach (var edge in edges) { if (edge.a == j) pending.Push(edge.b); if (edge.b == j) pending.Push(edge.a); }
                }
                if (component.Count > largest.Count) largest = component;
            }
            if (largest.Count < 6) throw new InvalidOperationException("No connected pipe-wall section for sleeve fit.");
            var profile = largest.Select(i => points[i]).ToArray(); var centre = Centre(profile);
            return profile.OrderBy(p => Mathf.Atan2(p.z - centre.z, p.x - centre.x)).ToArray();
        }
        private static Vector3 Centre(Vector3[] points) => new Vector3((points.Min(p => p.x) + points.Max(p => p.x)) * .5f, points[0].y, (points.Min(p => p.z) + points.Max(p => p.z)) * .5f);
        private static Vector3[] Hull(Vector3[] points)
        {
            var sorted = points.OrderBy(p => p.x).ThenBy(p => p.z).ToArray(); var hull = new List<Vector3>();
            float Cross(Vector3 a, Vector3 b, Vector3 c) => (b.x-a.x)*(c.z-a.z)-(b.z-a.z)*(c.x-a.x);
            foreach (var p in sorted) { while (hull.Count >= 2 && Cross(hull[hull.Count-2], hull[hull.Count-1], p) <= 0) hull.RemoveAt(hull.Count-1); hull.Add(p); }
            int lower = hull.Count;
            for (int i = sorted.Length - 2; i >= 0; i--) { var p = sorted[i]; while (hull.Count > lower && Cross(hull[hull.Count-2], hull[hull.Count-1], p) <= 0) hull.RemoveAt(hull.Count-1); hull.Add(p); }
            hull.RemoveAt(hull.Count-1); return hull.ToArray();
        }
        private static Vector3 OnProfile(Vector3[] polygon, Vector3 centre, float angle)
        {
            var d = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)); float radius = float.PositiveInfinity;
            for (int i = 0; i < polygon.Length; i++)
            {
                var a = new Vector2(polygon[i].x-centre.x, polygon[i].z-centre.z);
                var b = new Vector2(polygon[(i+1)%polygon.Length].x-centre.x, polygon[(i+1)%polygon.Length].z-centre.z); var edge = b-a;
                float cross = d.x*edge.y-d.y*edge.x; if (Mathf.Abs(cross) < 1e-10f) continue;
                float r = (a.x*edge.y-a.y*edge.x)/cross, t = (a.x*d.y-a.y*d.x)/cross;
                if (r > 0 && t >= -.00001f && t <= 1.00001f) radius = Mathf.Min(radius,r);
            }
            if (float.IsInfinity(radius)) throw new InvalidOperationException("Sleeve section is not star-shaped about its measured centre.");
            return centre + new Vector3(d.x,0,d.y)*radius;
        }
        internal static void Fit(Mesh mesh, Vector3[] nativeTop, Vector3[] nativeCut, Vector3[] manifoldHigh, Vector3[] manifoldLow)
        {
            var top = PipeProfile(nativeTop); var cut = PipeProfile(nativeCut);
            var high = PipeProfile(manifoldHigh); var low = PipeProfile(manifoldLow);
            var topHull = Hull(nativeTop); var cutHull = Hull(nativeCut);
            var tc = Centre(top); var cc = Centre(cut); var hc = Centre(high); var lc = Centre(low);
            // Retain every native corner angle, with extra regular samples for
            // the curved reducer. The mouth follows the actual faceted pipe,
            // including its inclination, with .7 mm insertion overlap.
            var candidates = top.Concat(cut).Concat(topHull).Concat(cutHull).Select(p => Mathf.Atan2(p.z-tc.z,p.x-tc.x))
                .Concat(Enumerable.Range(0,48).Select(i => i*2*Mathf.PI/48)).Select(a => Mathf.Repeat(a,2*Mathf.PI)).OrderBy(a => a);
            var angleList = new List<float>();
            foreach(float a in candidates) if(angleList.Count == 0 || a-angleList[angleList.Count-1] > .00001f) angleList.Add(a);
            if(angleList[0]+2*Mathf.PI-angleList[angleList.Count-1] < .00001f) angleList.RemoveAt(angleList.Count-1);
            var angles = angleList.ToArray();
            int sides = angles.Length, rings = 7, stride = sides + 1, layer = rings*stride;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (int wall = 0; wall < 2; wall++) for (int ring = 0; ring < rings; ring++) for (int side = 0; side <= sides; side++)
            {
                float angle = angles[side % sides]; var radial = new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                var nt = OnProfile(wall == 0 ? topHull : top, tc, angle);
                var nc = OnProfile(wall == 0 ? cutHull : cut, cc, angle);
                var mh = OnProfile(high,hc,angle); var ml = OnProfile(low,lc,angle);
                var tilt = (tc-cc)/(tc.y-cc.y);
                Vector3 point;
                switch (ring)
                {
                    case 0: point = nt; break;
                    case 1: point = nc + tilt*.002f; break;
                    case 2: point = nc - tilt*.008f; break;
                    case 3: point = Vector3.Lerp(nc,mh,.30f); point.y=cc.y-.030f; break;
                    case 4: point = Vector3.Lerp(nc,mh,.78f); point.y=cc.y-.072f; break;
                    case 5: point = mh; break;
                    default: point = ml; break;
                }
                point += radial*(wall == 0 ? (ring < 2 ? .010f : .008f) : -.0007f);
                vertices.Add(point); uv.Add(new Vector2(angle*.22f,(tc.y-point.y)*2));
                if (ring == 0 || side == sides) continue;
                int c=wall*layer+ring*stride+side,a=c-stride,b=a+1,d=c+1;
                // Outward walls; separate annular lips retain crisp end faces.
                if(wall==0)triangles.AddRange(new[]{a,b,c,b,d,c});else triangles.AddRange(new[]{a,c,b,b,c,d});
            }
            foreach(int ring in new[]{0,rings-1})
            {
                int start=vertices.Count;
                for(int side=0;side<=sides;side++)
                {
                    foreach(int wall in new[]{0,1}) { var p=vertices[wall*layer+ring*stride+side];vertices.Add(p);uv.Add(new Vector2(p.x,p.z)*2); }
                    if(side==sides)continue;
                    int a=start+side*2,b=a+1,c=a+2,d=a+3;
                    if(ring==0)triangles.AddRange(new[]{a,b,c,b,d,c});else triangles.AddRange(new[]{a,c,b,b,c,d});
                }
            }
            mesh.Clear();mesh.vertices=vertices.ToArray();mesh.uv=uv.ToArray();mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();
            var normals=mesh.normals;
            for(int wall=0;wall<2;wall++)for(int ring=0;ring<rings;ring++) { int a=wall*layer+ring*stride,b=a+sides;normals[a]=normals[b]=(normals[a]+normals[b]).normalized; }
            mesh.normals=normals;mesh.RecalculateTangents();mesh.RecalculateBounds();
        }
        private static Mesh Hollow(Vector3[] centres,float[] outer,float[] inner,int sides=24)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            var rights=new Vector3[centres.Length];var ups=new Vector3[centres.Length];
            Vector3 previousTangent=Vector3.forward,previousRight=Vector3.right;float distance=0;
            int stride=sides+1,layer=centres.Length*stride;
            for(int ring=0;ring<centres.Length;ring++)
            {
                var tangent=(centres[Math.Min(ring+1,centres.Length-1)]-centres[Math.Max(0,ring-1)]).normalized;
                var reference=Mathf.Abs(Vector3.Dot(tangent,Vector3.up))>.95f?Vector3.forward:Vector3.up;
                rights[ring]=ring==0?Vector3.Cross(reference,tangent).normalized:(Quaternion.FromToRotation(previousTangent,tangent)*previousRight).normalized;
                ups[ring]=Vector3.Cross(tangent,rights[ring]);previousTangent=tangent;previousRight=rights[ring];
            }
            for(int wall=0;wall<2;wall++)
            {
                distance=0;
                for(int ring=0;ring<centres.Length;ring++)
                {
                    if(ring>0)distance+=Vector3.Distance(centres[ring-1],centres[ring]);
                    float radius=wall==0?outer[ring]:inner[ring];
                    for(int side=0;side<=sides;side++)
                    {
                        float a=2*Mathf.PI*side/sides;
                        vertices.Add(centres[ring]+(rights[ring]*Mathf.Cos(a)+ups[ring]*Mathf.Sin(a))*radius);
                        uv.Add(new Vector2(a*radius*2,distance*2));
                        if(ring==0||side==sides)continue;
                        int c=wall*layer+ring*stride+side,a0=c-stride,b=a0+1,d=c+1;
                        if(wall==0)triangles.AddRange(new[]{a0,b,c,b,d,c});else triangles.AddRange(new[]{a0,c,b,b,c,d});
                    }
                }
            }
            // Separate annular lip vertices retain a crisp end face without
            // interrupting the smooth inner/outer walls or their metric UVs.
            foreach(int ring in new[]{0,centres.Length-1})
            {
                int start=vertices.Count;
                for(int side=0;side<=sides;side++)
                {
                    float a=2*Mathf.PI*side/sides;var radial=rights[ring]*Mathf.Cos(a)+ups[ring]*Mathf.Sin(a);
                    foreach(float r in new[]{outer[ring],inner[ring]}){vertices.Add(centres[ring]+radial*r);uv.Add(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r*2);}
                    if(side==sides)continue;
                    int o=start+side*2,i=o+1,n=o+2,j=o+3;
                    if(ring==0)triangles.AddRange(new[]{o,i,n,i,j,n});else triangles.AddRange(new[]{o,n,i,i,n,j});
                }
            }
            var mesh=new Mesh{vertices=vertices.ToArray(),uv=uv.ToArray(),triangles=triangles.ToArray()};mesh.RecalculateNormals();
            var normals=mesh.normals;
            for(int wall=0;wall<2;wall++)for(int ring=0;ring<centres.Length;ring++)
            {int a=wall*layer+ring*stride,b=a+sides;normals[a]=normals[b]=(normals[a]+normals[b]).normalized;}
            mesh.normals=normals;mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }
    }
}
