using System;
using System.IO;
using System.Linq;
using CCL.Types.Proxies.Audio;
using CCL.Types.Proxies.VFX;
using UnityEngine;

// Fit native effects to the actual drain-valve caps. Verde's separate stage
// readers are the reference pattern; its art, scale and crossed crank IDs are
// not copied. No new particle systems or simulation components are introduced.
public static class SteamEffectsAuthoring
{
    [Serializable] private class Data { public Entry[] meshes = Array.Empty<Entry>(); }
    [Serializable] private class Entry { public string name = ""; public Vector3[] vertices = Array.Empty<Vector3>(); }
    private static Vector3[] native;
    public static Vector3 NativeOutlet(int side, bool front)
    {
        if (native == null) native = JsonUtility.FromJson<Data>(File.ReadAllText(Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW"))).meshes.Single(m => m.name == "s060_body").vertices;
        float station = RearrangedS060.S060PipeGeometry.NativeCylinderCentreZ + (front ? .35f : -.35f);
        var candidate = native.Where(p => p.x * side > .95f && p.x * side < 1.15f && p.y > 0 && p.y < .45f && Mathf.Abs(p.z - station) < .08f).ToArray();
        if (candidate.Length == 0) throw new InvalidOperationException("Native S060 drain-valve mesh cap missing.");
        float y = candidate.Min(p => p.y);
        var cap = candidate.Where(p => Mathf.Abs(p.y - y) < .00001f).ToArray();
        return new Vector3((cap.Min(p => p.x) + cap.Max(p => p.x)) / 2, y, (cap.Min(p => p.z) + cap.Max(p => p.z)) / 2);
    }
    public static Vector3 Outlet(Transform frame, int side, bool front, bool nativeFront)
    {
        if (nativeFront) return frame.TransformPoint(NativeOutlet(side, front) + new Vector3(0, 0, MeyerPlan.CylinderZ - RearrangedS060.S060PipeGeometry.NativeCylinderCentreZ));
        var cock = frame.Find("[Round cylinders " + (side < 0 ? "L" : "R") + "]/Cylinder cock " + (front ? "1" : "-1"));
        if (cock == null) throw new InvalidOperationException("Actual cylinder drain valve missing.");
        var filter = cock.GetComponent<MeshFilter>();
        var bounds = filter.sharedMesh.bounds;
        return filter.transform.TransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
    }
    public static void Apply(Transform frame, bool nativeFront = false)
    {
        var reader = frame.GetComponentsInChildren<CylinderCockParticlePortReaderProxy>(true).Single();
        var audio = frame.GetComponentsInChildren<CylinderCockLayeredPortReaderProxy>(true).Single();
        var controller = frame.GetComponentsInChildren<ParticlesPortReadersControllerProxy>(true).Single();
        for (int cylinder = 0; cylinder < 2; cylinder++)
        {
            // Derive side from the physical audio holder, preserving the native
            // cylinder index/quartering rather than assuming left/right order.
            int side = frame.InverseTransformPoint(audio.cylCockAudio[cylinder].transform.position).x < 0 ? -1 : 1;
            var front = Outlet(frame, side, true, nativeFront); var rear = Outlet(frame, side, false, nativeFront);
            var setup = reader.cylinderSetups[cylinder];
            setup.frontParticlesParent.transform.position = front;
            setup.rearParticlesParent.transform.position = rear;
            setup.frontParticlesParent.transform.rotation = setup.rearParticlesParent.transform.rotation = Quaternion.LookRotation(-frame.up, frame.forward);
            // A single native drip holder per cylinder stays on the rear drain
            // valve; the shared hiss source sits between its two actual ends.
            audio.cylCockAudio[cylinder].transform.position = (front + rear) / 2;
            controller.particlePortReaders.Single(r => r.particlesParent.name == "Water drip " + cylinder).particlesParent.transform.position = rear;
        }
        if (frame.Find("[S060 fitted effects 0.7]") == null)
        { var marker = new GameObject("[S060 fitted effects 0.7]"); marker.transform.SetParent(frame, false); }
    }
}
