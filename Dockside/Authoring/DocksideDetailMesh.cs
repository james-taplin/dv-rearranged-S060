using System;
using System.Collections.Generic;
using UnityEngine;

// Adapted from the S282 FranklinRodMesh (MIT); dimensions and axes are S060-specific.
// Continuous bearing/web skin, smooth bore walls and deliberate hard edges.
// No S282 runtime, native game mesh or texture is embedded.
public static class DocksideDetailMesh
{
    public static Mesh Link(string name, float length, float nearRadius, float nearHole, float farRadius, float farHole, float nearDepth, float farDepth)
    { return BuildLink(name, new Vector3(0, 0, length), nearRadius, nearHole, farRadius, farHole, nearDepth, farDepth); }
    private static Mesh BuildLink(string name, Vector3 far, float nearRadius, float nearHole, float farRadius, float farHole, float nearDepth, float farDepth)
    {
        const int segments = 48;
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var uv = new List<Vector2>();
        var ringNormals = new Dictionary<int,Vector3>();
        void Quad(int a, int b, int c, int d)
        {
            triangles.Add(a); triangles.Add(b); triangles.Add(c);
            triangles.Add(a); triangles.Add(c); triangles.Add(d);
        }
        void Ring(Vector3 centre, float outer, float inner, float x0, float x1,int openingStart)
        {
            // Separate flat faces and cylindrical walls; duplicate the UV seam
            // but explicitly give both sides the same analytic normal.
            for (int surface=0; surface<4; surface++)
            {
                int start=vertices.Count;
                for (int i=0; i<=segments; i++)
                {
                    float angle=2*Mathf.PI*i/segments;
                    var radial=new Vector3(0,Mathf.Cos(angle),Mathf.Sin(angle));
                    Vector3 p0,p1,normal;
                    if(surface==0){p0=new Vector3(x0,centre.y,centre.z)+radial*outer;p1=new Vector3(x0,centre.y,centre.z)+radial*inner;normal=Vector3.left;}
                    else if(surface==1){p0=new Vector3(x1,centre.y,centre.z)+radial*outer;p1=new Vector3(x1,centre.y,centre.z)+radial*inner;normal=Vector3.right;}
                    else if(surface==2){p0=new Vector3(x0,centre.y,centre.z)+radial*outer;p1=new Vector3(x1,centre.y,centre.z)+radial*outer;normal=radial;}
                    else {p0=new Vector3(x0,centre.y,centre.z)+radial*inner;p1=new Vector3(x1,centre.y,centre.z)+radial*inner;normal=-radial;}
                    int index=vertices.Count;vertices.Add(p0);vertices.Add(p1);
                    uv.Add(new Vector2((float)i/segments,0));uv.Add(new Vector2((float)i/segments,1));
                    ringNormals[index]=normal;ringNormals[index+1]=normal;
                }
                for(int i=0;i<segments;i++)
                {
                    if(surface==2&&i>=openingStart&&i<openingStart+4)continue;
                    int a=start+2*i,b=a+2;
                    if(surface==0||surface==3)Quad(a,a+1,b+1,b);
                    else Quad(a,b,b+1,a+1);
                }
            }
        }
        Ring(Vector3.zero,nearRadius,nearHole,-nearDepth,nearDepth,10);
        Ring(far,farRadius,farHole,-farDepth,farDepth,34);
        var dir=far.normalized;var side=new Vector3(0,-dir.z,dir.y);float h=Mathf.Min(nearRadius,farRadius)*.46f;
        float length=far.magnitude;
        if(length<.15f||Vector3.Distance(dir,Vector3.forward)>.0001f)
            throw new ArgumentException("Dockside rod needs a positive local bearing span",nameof(far));
        var lo=new[]{-nearDepth,-nearDepth,-farDepth,-farDepth};var hi=new[]{nearDepth,nearDepth,farDepth,farDepth};
        // Five points join exactly to the four-segment opening in each ring.
        // Intermediate cross-sections are straight and preserve the original
        // clearances; a closed skin replaces the old intersecting box pieces.
        Vector3 Point(int section,int across)
        {
            float fraction=across/4f;
            if(section==0){float a=(fraction-.5f)*30f*Mathf.Deg2Rad;return dir*(nearRadius*Mathf.Cos(a))+side*(nearRadius*Mathf.Sin(a));}
            if(section==3){float a=(fraction-.5f)*30f*Mathf.Deg2Rad;return far-dir*(farRadius*Mathf.Cos(a))+side*(farRadius*Mathf.Sin(a));}
            float start=nearRadius*Mathf.Cos(15*Mathf.Deg2Rad), end=length-farRadius*Mathf.Cos(15*Mathf.Deg2Rad);
            return dir*Mathf.Lerp(start,end,section==1?1f/3:2f/3)+side*((fraction*2-1)*h);
        }
        // The two broad faces are continuous along the web, with intentionally
        // separate vertices at the hard edges of the narrow side faces.
        for(int face=0;face<2;face++)
        {
            int start=vertices.Count;
            for(int section=0;section<4;section++)for(int j=0;j<5;j++)
            {
                var p=Point(section,j);vertices.Add(new Vector3(face==0?lo[section]:hi[section],p.y,p.z));uv.Add(new Vector2(section/3f,j/4f));
            }
            for(int section=0;section<3;section++)for(int j=0;j<4;j++)
            {
                int a=start+section*5+j,b=a+5;
                if(face==0)Quad(a,a+1,b+1,b);else Quad(a,b,b+1,a+1);
            }
        }
        for(int sideFace=0;sideFace<2;sideFace++)
        {
            int start=vertices.Count;int j=sideFace==0?0:4;
            for(int section=0;section<4;section++){var p=Point(section,j);vertices.Add(new Vector3(lo[section],p.y,p.z));vertices.Add(new Vector3(hi[section],p.y,p.z));uv.Add(new Vector2(section/3f,0));uv.Add(new Vector2(section/3f,1));}
            for(int section=0;section<3;section++){int a=start+2*section,b=a+2;if(sideFace==0)Quad(a,b,b+1,a+1);else Quad(a,a+1,b+1,b);}
        }
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);
        mesh.RecalculateNormals();var normals=mesh.normals;foreach(var item in ringNormals)normals[item.Key]=item.Value;mesh.normals=normals;
        ApplyMetalUv(mesh);mesh.RecalculateBounds();
        return mesh;
    }

    public static void ApplyMetalUv(Mesh mesh)
    {
        var uv=mesh.uv;
        for(int i=0;i<uv.Length;i++) uv[i]=new Vector2(.8739f+uv[i].x*.001f,.7333f+uv[i].y*.001f);
        mesh.uv=uv; mesh.RecalculateTangents();
    }
    public static Mesh Tube(string name,float outer,float inner,float x0,float x1)
    {
        const int count=48; var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
        for(int face=0;face<4;face++)
        {
            int start=v.Count;
            for(int i=0;i<=count;i++)
            {
                float a=2*Mathf.PI*i/count;var r=new Vector3(0,Mathf.Cos(a),Mathf.Sin(a));Vector3 p,q,normal;
                if(face==0){p=Vector3.right*x0+r*outer;q=Vector3.right*x0+r*inner;normal=Vector3.left;}
                else if(face==1){p=Vector3.right*x1+r*outer;q=Vector3.right*x1+r*inner;normal=Vector3.right;}
                else if(face==2){p=Vector3.right*x0+r*outer;q=Vector3.right*x1+r*outer;normal=r;}
                else{p=Vector3.right*x0+r*inner;q=Vector3.right*x1+r*inner;normal=-r;}
                v.Add(p);v.Add(q);n.Add(normal);n.Add(normal);uv.Add(new Vector2((float)i/count,0));uv.Add(new Vector2((float)i/count,1));
            }
            for(int i=0;i<count;i++){int a=start+2*i,b=a+2;if(face==0||face==3)t.AddRange(new[]{a,a+1,b+1,a,b+1,b});else t.AddRange(new[]{a,b,b+1,a,b+1,a+1});}
        }
        var m=new Mesh{name=name};m.SetVertices(v);m.SetNormals(n);m.SetUVs(0,uv);m.SetTriangles(t,0);ApplyMetalUv(m);m.RecalculateBounds();return m;
    }
    public static Mesh Piston(string name)
    {
        // Unity's smooth cylinder supplies the same physical .035 m diameter,
        // with its axis baked along Z instead of the former rectangular prism.
        var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);var m=UnityEngine.Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh);UnityEngine.Object.DestroyImmediate(go);
        var rotation=Quaternion.Euler(90,0,0);var v=m.vertices;var n=m.normals;
        for(int i=0;i<v.Length;i++){v[i]=rotation*new Vector3(v[i].x,v[i].y*.5f,v[i].z);n[i]=rotation*n[i];}
        m.vertices=v;m.normals=n;m.name=name;ApplyMetalUv(m);m.RecalculateBounds();return m;
    }
    public static Mesh Slider(string name)
    {
        // Two cheeks leave a 56 mm slot around the 48 mm main-rod small end.
        // The end tie joins the piston socket beyond the bearing's swept edge.
        var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();
        void Box(Vector3 lo,Vector3 hi)
        {
            Vector3 P(int k)=>new Vector3((k&1)==0?lo.x:hi.x,(k&2)==0?lo.y:hi.y,(k&4)==0?lo.z:hi.z);
            foreach(var q in new[]{new[]{0,4,6,2},new[]{1,3,7,5},new[]{0,1,5,4},new[]{2,6,7,3},new[]{0,2,3,1},new[]{4,5,7,6}})
            {int a=v.Count;foreach(int k in q){var p=P(k);v.Add(new Vector3(p.x/.13f,p.y/.10f,p.z/.15f));}uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});t.AddRange(new[]{a,a+1,a+2,a,a+2,a+3});}
        }
        Box(new Vector3(-.065f,-.05f,-.075f),new Vector3(-.028f,.05f,.075f));
        Box(new Vector3(.028f,-.05f,-.075f),new Vector3(.065f,.05f,.075f));
        Box(new Vector3(-.028f,-.05f,.058f),new Vector3(.028f,.05f,.075f));
        var m=new Mesh{name=name};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();ApplyMetalUv(m);m.RecalculateBounds();return m;
    }
}
