using UnityEngine;

// Keep the existing asset names/GUIDs while sharing the shaped-rod recipe.
public static class DocksideRodGeometry
{
    public static Mesh Create(string folder, string name, float length)
    {
        bool main = name == "Main";
        var mesh = DocksideDetailMesh.Link("DocksideRod" + name, length, .067f, .042f, main ? .050f : .067f, main ? .022f : .042f, .024f, .024f);
        var points = mesh.vertices; var normals = mesh.normals;
        for (int i = 0; i < points.Length; i++)
        {
            points[i].z = (points[i].z - length / 2) / length;
            normals[i] = new Vector3(normals[i].x, normals[i].y, normals[i].z * length).normalized;
        }
        mesh.vertices = points; mesh.normals = normals; mesh.RecalculateTangents(); mesh.RecalculateBounds();
        return DocksideDriveDetail.Save(folder, mesh);
    }
}
