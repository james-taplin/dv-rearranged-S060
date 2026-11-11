using System;
using System.IO;
using System.Linq;
using UnityEngine;

// Offline pose of the existing runtime's offset frames about native supports.
public static class SteamBogiePoseAudit
{
    [Serializable] private sealed class Support { public string name = ""; public Vector3 position = Vector3.zero; }
    [Serializable] private sealed class Data { public Support[] supports = Array.Empty<Support>(); }
    private static string loadedPath;
    private static Data data;
    public static void Pose(Transform car, Transform unit, Vector3 home, bool front, float yaw)
    {
        string path = Environment.GetEnvironmentVariable("S060_DOCKSIDE_SUPPORTS");
        if (path != loadedPath || data == null)
        {
            data = JsonUtility.FromJson<Data>(File.ReadAllText(path)); loadedPath = path;
            if (data.supports.Length != 2) throw new InvalidOperationException("Two measured native S060 supports required.");
        }
        var pivot = data.supports.Single(s => s.name == (front ? "Axle_F" : "Axle_R")).position;
        var rotation = Quaternion.Euler(0, yaw, 0);
        unit.position = car.TransformPoint(pivot + rotation * (home - pivot));
        unit.rotation = car.rotation * rotation;
    }
}
