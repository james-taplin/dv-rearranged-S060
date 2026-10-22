using UnityEngine;

// Keep the existing asset names/GUIDs while sharing the shaped-rod recipe.
public static class DocksideRodGeometry
{
    public static Mesh Create(string folder, string name, float length) => SteamRodGeometry.Create(folder, name, length, "DocksideRod", .067f, .042f);
}
