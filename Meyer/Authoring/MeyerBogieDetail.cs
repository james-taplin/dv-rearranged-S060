using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Period-style detailing for the fictional attached power unit. Inspired by
// museum Kitson-Meyer frames; this is not a replica of a historical class.
public static class MeyerBogieDetail
{
    private static readonly Rect Green = new Rect(.817f, .332f, .042f, .052f);
    private static readonly Rect Iron = new Rect(.680f, .887f, .012f, .012f);
    private static readonly Rect Red = new Rect(.315f, .450f, .018f, .045f);
    public static void Apply(Transform rear, string folder)
    {
        var material = SteamBogieVisuals.Metal(folder, "PeriodFramePreview", new Color(.12f, .14f, .13f), .5f, .3f);
        var details = SteamBogieVisuals.Group(rear, "[Period rear chassis]");
        Transform Part(string name, PrimitiveType kind, Vector3 at, Vector3 size, Rect uv)
        { var part = SteamBogieVisuals.Part(details, name, kind, at, size, material); Patch(part.GetComponent<MeshFilter>(), uv); return part; }
        void Rivet(string name, Vector3 at, int side)
        { var r = Part(name, PrimitiveType.Cylinder, at, new Vector3(.032f, .008f, .032f), Iron); r.localRotation = Quaternion.Euler(0, 0, side * 90); }
        // A compact sloped bunker makes the upper works read as a complete
        // attached tank unit. Its shell lands on the deck and all caps, hinges
        // and supports intersect their parent structure. It is visual only.
        var bunker = new GameObject("Sloped rear bunker"); bunker.transform.SetParent(details, false);
        bunker.AddComponent<MeshFilter>().sharedMesh = BunkerShell();
        bunker.AddComponent<MeshRenderer>().sharedMaterial = material;
        foreach (float x in new[] { -.61f, .61f })
        {
            Part("Bunker rolled side rim", PrimitiveType.Cube, new Vector3(x, 2.32f, -.295f), new Vector3(.075f, .045f, 1.58f), Green);
            foreach (float z in new[] { -.84f, .20f })
                Part("Bunker side stiffener", PrimitiveType.Cube, new Vector3(x, 1.96f, z), new Vector3(.075f, .73f, .055f), Green);
        }
        foreach (float z in new[] { -1.06f, .47f })
            Part("Bunker rolled end rim", PrimitiveType.Cube, new Vector3(0, 2.32f, z), new Vector3(1.23f, .045f, .075f), Green);
        Part("Bunker coal bed", PrimitiveType.Cube, new Vector3(0, 2.13f, -.29f), new Vector3(1.06f, .14f, 1.36f), Iron);
        for (int row = 0; row < 4; row++) for (int column = 0; column < 3; column++)
            Part("Coal mound", PrimitiveType.Sphere, new Vector3(-.36f + column * .35f, 2.19f + .02f * ((row + column) % 2), -.82f + row * .34f),
                new Vector3(.35f, .13f, .36f), Iron);
        var access = Part("Bunker rear cleanout door", PrimitiveType.Cube, new Vector3(0, 1.92f, -1.195f), new Vector3(.48f, .31f, .035f), Green);
        access.localRotation = Quaternion.Euler(17, 0, 0);
        foreach (float x in new[] { -.17f, .17f })
            Part("Bunker cleanout hinge", PrimitiveType.Cube, new Vector3(x, 1.81f, -1.235f), new Vector3(.065f, .12f, .055f), Iron).localRotation = access.localRotation;
        foreach (int side in new[] { -1, 1 })
        {
            float x = side * .72f;
            Part("Riveted upper frame flange", PrimitiveType.Cube, new Vector3(x, 1.22f, -.13f), new Vector3(.18f, .045f, 2.55f), Iron);
            Part("Lower frame flange", PrimitiveType.Cube, new Vector3(x, 1.01f, -.13f), new Vector3(.18f, .045f, 2.55f), Iron);
            foreach (float z in new[] { -.8f, .8f })
            {
                Part("Axlebox casting", PrimitiveType.Cube, new Vector3(side * .77f, .73f, z), new Vector3(.18f, .24f, .30f), Iron);
                Part("Axlebox oil cap", PrimitiveType.Cylinder, new Vector3(side * .77f, .875f, z), new Vector3(.075f, .022f, .075f), Iron);
                Part("Horn keeper plate", PrimitiveType.Cube, new Vector3(side * .77f, .56f, z), new Vector3(.19f, .045f, .39f), Iron);
                Part("Spring buckle", PrimitiveType.Cube, new Vector3(side * .91f, 1.18f, z), new Vector3(.12f, .12f, .075f), Iron);
                foreach (int end in new[] { -1, 1 })
                {
                    Part("Spring hanger", PrimitiveType.Cylinder, new Vector3(side * .89f, 1.10f, z + end * .30f), new Vector3(.035f, .13f, .035f), Iron);
                    Rivet("Horn stay bolt", new Vector3(side * .875f, .58f, z + end * .13f), side);
                }
                // Shoes sit behind the wheel face, with a visible hanger and
                // pull rod. No unsupported brake simulation component is added.
                Part("Brake hanger", PrimitiveType.Cube, new Vector3(side * .78f, .64f, z - .61f), new Vector3(.045f, .48f, .045f), Iron);
                Part("Brake block", PrimitiveType.Cube, new Vector3(side * .80f, .51f, z - .65f), new Vector3(.16f, .22f, .07f), Iron);
            }
            Part("Brake pull rod", PrimitiveType.Cylinder, new Vector3(side * .75f, .40f, -.15f), new Vector3(.032f, .90f, .032f), Iron).localRotation = Quaternion.Euler(90, 0, 0);
            for (int n = 0; n < 11; n++) Rivet("Frame web rivet", new Vector3(side * .825f, 1.12f, -1.30f + n * .24f), side);
            foreach (float z in new[] { -.41f, .41f })
            {
                Part("Pannier mounting strap", PrimitiveType.Cube, new Vector3(side * (float)MeyerPlan.TankCentreX, 2.160f, z), new Vector3(.34f, .025f, .065f), Green);
                if (z < 0) foreach (float y in new[] { 1.65f, 2.10f })
                    for (int n = 0; n < 7; n++) Rivet("Pannier seam rivet", new Vector3(side * 1.211f, y, -.48f + n * .16f), side);
            }
            foreach (float y in new[] { .43f, .69f, .95f, 1.21f, 1.47f })
                Part("Rear access step", PrimitiveType.Cube, new Vector3(side * .45f, y, -1.78f), new Vector3(.37f, .035f, .28f), Iron);
            Part("Rear step stringer", PrimitiveType.Cube, new Vector3(side * .45f, 1.01f, -1.65f), new Vector3(.06f, 1.25f, .06f), Iron);
            foreach (float z in new[] { -.92f, -1.55f })
                Part("Deck handrail stanchion", PrimitiveType.Cylinder, new Vector3(side * 1.08f, 1.86f, z), new Vector3(.035f, .29f, .035f), Iron);
            Part("Deck handrail", PrimitiveType.Cylinder, new Vector3(side * 1.08f, 2.14f, -1.235f), new Vector3(.035f, .34f, .035f), Iron).localRotation = Quaternion.Euler(90, 0, 0);
        }
        Part("Brake cross shaft", PrimitiveType.Cylinder, new Vector3(0, .40f, -.35f), new Vector3(.045f, .81f, .045f), Iron).localRotation = Quaternion.Euler(0, 0, 90);
        // Bake only the new static details: rivets do not become hundreds of
        // individual runtime draw calls. Motion datums and rods stay separate.
        var filters = details.GetComponentsInChildren<MeshFilter>();
        var combined = new Mesh { name = "MeyerPeriodDetails", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        combined.CombineMeshes(filters.Select(f => new CombineInstance { mesh = f.sharedMesh, transform = details.worldToLocalMatrix * f.transform.localToWorldMatrix }).ToArray(), true, true);
        string path = folder + "/MeyerPeriodDetails.asset"; var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (saved == null) { AssetDatabase.CreateAsset(combined, path); saved = combined; }
        else { EditorUtility.CopySerialized(combined, saved); Object.DestroyImmediate(combined); }
        foreach (var f in filters) { var mesh = f.sharedMesh; Object.DestroyImmediate(f.gameObject); Object.DestroyImmediate(mesh); }
        details.gameObject.AddComponent<MeshFilter>().sharedMesh = saved;
        var renderer = details.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; SteamBogieVisuals.LiveMaterial(renderer);
    }
    private static Mesh BunkerShell()
    {
        var vertices = new System.Collections.Generic.List<Vector3>();
        var uv = new System.Collections.Generic.List<Vector2>();
        var triangles = new System.Collections.Generic.List<int>();
        Vector3[] Ring(float x, float y, float rear, float front) => new[] {
            new Vector3(-x, y, rear), new Vector3(x, y, rear), new Vector3(x, y, front), new Vector3(-x, y, front) };
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int n = vertices.Count; vertices.AddRange(new[] { a, b, c, d });
            uv.AddRange(new[] { new Vector2(Green.x, Green.y), new Vector2(Green.x, Green.yMax), new Vector2(Green.xMax, Green.yMax), new Vector2(Green.xMax, Green.y) });
            triangles.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
        }
        var bottom = Ring(.64f, 1.60f, -1.28f, .75f);
        var top = Ring(.61f, 2.32f, -1.06f, .47f);
        var innerTop = Ring(.55f, 2.32f, -1.00f, .41f);
        var innerBottom = Ring(.56f, 1.70f, -1.15f, .65f);
        for (int i = 0; i < 4; i++)
        {
            int next = (i + 1) % 4;
            Quad(bottom[i], top[i], top[next], bottom[next]);
            Quad(top[i], innerTop[i], innerTop[next], top[next]);
            Quad(innerBottom[i], innerBottom[next], innerTop[next], innerTop[i]);
        }
        Quad(bottom[0], bottom[1], bottom[2], bottom[3]);
        Quad(innerBottom[0], innerBottom[3], innerBottom[2], innerBottom[1]);
        var mesh = new Mesh { name = "Meyer sloped bunker shell" };
        mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }
    public static void AliasAll(Transform unit, string folder)
    {
        int index = 0;
        foreach (var renderer in unit.GetComponentsInChildren<MeshRenderer>(true))
        {
            SteamBogieVisuals.LiveMaterial(renderer);
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || (filter.sharedMesh.name != "Cube" && filter.sharedMesh.name != "Cylinder")) continue;
            bool green = renderer.transform.GetComponentsInParent<Transform>().Any(t => t.name.IndexOf("pannier", StringComparison.OrdinalIgnoreCase) >= 0);
            Patch(filter, renderer.name == "Rear buffer beam" ? Red : green ? Green : Iron);
            string path = folder + "/LiveSurface" + (unit.parent.name == "BogieR" ? "Rear" : "Front") + index++ + ".asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (saved == null) { AssetDatabase.CreateAsset(filter.sharedMesh, path); }
            else { var copy = filter.sharedMesh; EditorUtility.CopySerialized(copy, saved); Object.DestroyImmediate(copy); filter.sharedMesh = saved; }
        }
    }
    private static void Patch(MeshFilter filter, Rect rect)
    {
        var mesh = Object.Instantiate(filter.sharedMesh); mesh.name = "Meyer live surface";
        var uv = mesh.uv;
        for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(rect.x + uv[i].x * rect.width, rect.y + uv[i].y * rect.height);
        mesh.uv = uv; filter.sharedMesh = mesh;
    }
}
