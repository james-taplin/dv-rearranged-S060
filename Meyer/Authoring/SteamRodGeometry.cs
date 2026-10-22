using RearrangedS060;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SteamRodGeometry
{
    public static Mesh Create(string folder, string name, float length, string prefix = "SteamRod", float bossRadius = .085f, float hole = .0595f)
    {
        string path = folder + "/" + prefix + name + ".asset";
        var source = S060RodMesh.Normalized(prefix + name, length, bossRadius, hole); var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null) { mesh = source; AssetDatabase.CreateAsset(mesh, path); }
        else { EditorUtility.CopySerialized(source, mesh); Object.DestroyImmediate(source); }
        EditorUtility.SetDirty(mesh); return mesh;
    }
}
