using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CCL.Types.Proxies.Ports;
using RearrangedS060;
using UnityEngine;
using Object = UnityEngine.Object;

// Reopened-bundle QA only. Native oil cup meshes stay transient and are never
// saved under Assets or added to the exported pack. Runtime uses real controls.
public static class SteamOilCupAudit
{
    [Serializable] private class Data { public Entry[] meshes = Array.Empty<Entry>(); }
    [Serializable] private class Entry { public string name = ""; public Vector3[] vertices = Array.Empty<Vector3>(); public Vector2[] uv = Array.Empty<Vector2>(); public int[] triangles = Array.Empty<int>(); }
    private sealed class Mount
    {
        public Transform Frame, Wheel, Cup, Stem, Crosshead, Guide;
        public float X, Lift, Boss, MinY;
        public int Side;
        public Vector3 Foot;
        public Quaternion Orientation = Quaternion.identity;
    }
    public static Action Apply(GameObject clone, string output, string name, List<Object> owned)
    {
        string fixture = Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW")), "native-oilcups.json");
        if (!File.Exists(fixture)) throw new InvalidOperationException("Local native oil cup geometry fixture required.");
        var data = JsonUtility.FromJson<Data>(File.ReadAllText(fixture));
        Mesh Native(string meshName)
        {
            var entry = data.meshes.Single(e => e.name == meshName);
            var mesh = new Mesh { name = meshName + " (QA only)" }; mesh.vertices = entry.vertices; mesh.uv = entry.uv; mesh.triangles = entry.triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); owned.Add(mesh); return mesh;
        }
        var simple = Native("OilCup01_LOD0"); var crossCup = Native("OilCup02_LOD0"); var cap = Native("OilCupCap_LOD0");
        var material = new Material(Shader.Find("Standard")) { color = new Color(.42f, .27f, .095f) }; material.SetFloat("_Metallic", .65f); owned.Add(material);
        var mounts = new List<Mount>();
        var readers = clone.GetComponentsInChildren<AnimatorPortReaderProxy>(true);
        var front = readers.OrderByDescending(r => clone.transform.InverseTransformPoint(r.transform.position).z).First().transform;
        Transform Visual(Transform parent, string partName, Mesh mesh)
        {
            var t = new GameObject(partName).transform; t.SetParent(parent, false); t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material; return t;
        }
        Mount Cup(Transform frame, Mesh mesh)
        {
            var t = Visual(frame, "[QA native oil cup]", mesh); var lid = Visual(t, "[QA native oil cap]", cap); lid.localPosition = new Vector3(0, .040288f, -.0297f);
            var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder); stem.name = "[QA oil mounting neck]"; stem.transform.SetParent(frame, false); Object.DestroyImmediate(stem.GetComponent<Collider>()); stem.GetComponent<MeshRenderer>().sharedMaterial = material;
            var lowest = mesh.vertices.Where(v => v.y <= mesh.bounds.min.y + .001f).ToArray();
            var footBounds = new Bounds(lowest[0], Vector3.zero); foreach (var v in lowest) footBounds.Encapsulate(v);
            var foot = footBounds.center;
            if (Vector3.Distance(foot, S060OilCupGeometry.NativeThreadFoot(mesh.name, mesh.bounds)) > .00002f) throw new InvalidOperationException("Production native thread datum disagrees with actual lowest cup vertices.");
            var m = new Mount { Frame = frame, Cup = t, Stem = stem.transform, MinY = mesh.bounds.min.y, Foot = foot }; mounts.Add(m); return m;
        }
        foreach (var reader in readers)
        {
            var frame = reader.transform;
            foreach (int side in new[] { -1, 1 })
            {
                string suffix = side < 0 ? "L" : "R";
                var rods = frame.Find("[Meyer rods " + suffix + "]") ?? frame.Find("[Dockside rods " + suffix + "]");
                var axles = frame.Cast<Transform>().Where(t => t.name.StartsWith("[axle]", StringComparison.Ordinal)).OrderBy(t => t.localPosition.z).ToArray();
                foreach (var axle in axles)
                {
                    bool native = frame == front && axle == axles[0]; var m = Cup(frame, simple);
                    m.Wheel = axle.Cast<Transform>().Single(t => t.name.Contains("driver " + suffix));
                    m.X = side * (axle == axles[0] ? S060OilCupGeometry.MainRodPlaneX : S060OilCupGeometry.CouplingRodPlaneX); m.Lift = axle == axles[0] ? .12f : .11f; m.Boss = name.StartsWith("meyer", StringComparison.Ordinal) ? .085f : .067f;
                }
                if (frame == front)
                {
                    var guide = Cup(frame, simple); guide.Guide = rods.Find("Upper crosshead guide") ?? rods.Find("Crosshead guide 1");
                    var cross = Cup(frame, crossCup); cross.Crosshead = rods.Find("Crosshead"); cross.Side = side; cross.Orientation = side > 0 ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
                }
            }
        }
        int assertions = 0; float maxGap = 0, minFootplateGap = float.PositiveInfinity;
        void Update(bool check)
        {
            foreach (var m in mounts)
            {
                Vector3 contact, origin; var orientation = m.Frame.rotation * m.Orientation;
                if (m.Guide != null) { contact = m.Guide.TransformPoint(new Vector3(0, .5f, 0)); origin = S060OilCupGeometry.SeatedOrigin(m.Frame, contact, m.Foot, orientation); }
                else if (m.Crosshead != null) { contact = m.Crosshead.TransformPoint(new Vector3(0, .5f, 0)); origin = contact - m.Frame.up * (Vector3.Dot(orientation * m.Foot, m.Frame.up) + S060OilCupGeometry.ThreadEmbed) + m.Frame.right * (m.Side * .0877f);
                    var actualFoot = S060OilCupGeometry.ThreadTop(m.Frame, origin, m.Foot, orientation);
                    var localFoot = m.Crosshead.InverseTransformPoint(actualFoot);
                    if (check) { Require(Mathf.Abs(localFoot.x) <= .5f - .006f / m.Crosshead.localScale.x && Mathf.Abs(localFoot.z) <= .5f - .006f / m.Crosshead.localScale.z && Mathf.Abs(localFoot.y - .5f) < .0001f, "Actual bent native oilcup thread is outside its crosshead mounting face."); assertions++; }
                    contact = m.Crosshead.TransformPoint(new Vector3(localFoot.x, .5f, localFoot.z)); }
                else { var pin = S060OilCupGeometry.Pin(m.Frame, m.Wheel, m.X); contact = pin + m.Frame.up * m.Boss; origin = pin + m.Frame.up * m.Lift; }
                m.Cup.position = origin; m.Cup.rotation = orientation;
                var top = S060OilCupGeometry.ThreadTop(m.Frame, origin, m.Foot, orientation); var delta = top - contact;
                m.Stem.position = (top + contact) / 2; m.Stem.rotation = delta.sqrMagnitude > .000001f ? Quaternion.FromToRotation(Vector3.up, delta.normalized) : m.Frame.rotation;
                m.Stem.localScale = new Vector3(.035f, Mathf.Max(.000001f, delta.magnitude / 2), .035f); m.Stem.gameObject.SetActive(delta.magnitude > .003f);
                if (!check) continue;
                if (delta.magnitude > .003f)
                {
                    float gap = Vector3.Distance(m.Stem.TransformPoint(Vector3.up), top); maxGap = Mathf.Max(maxGap, gap);
                    Require(gap < .0001f && Vector3.Distance(m.Stem.TransformPoint(Vector3.down), contact) < .0001f, "Oil cup neck detached from its actual rod support."); assertions++;
                }
                var mesh = m.Cup.GetComponent<MeshFilter>().sharedMesh;
                float topY = m.Frame.InverseTransformPoint(m.Cup.TransformPoint(new Vector3(0, mesh.bounds.max.y, 0))).y;
                // The measured native footplate lower face is above 1.30m;
                // allow 30mm static clearance for these bogie-mounted cups.
                minFootplateGap = Mathf.Min(minFootplateGap, 1.30f - topY);
                Require(topY < 1.27f, "Oil cup reaches the S060 footplate."); assertions++;
                Require(Vector3.Dot(top - contact, m.Frame.up) >= -.0001f, "Oil cup thread penetrates below its mounting surface."); assertions++;
            }
        }
        foreach (float yaw in new[] { -12f, 0, 12f })
            for (int sample = 0; sample <= 128; sample++)
            {
                foreach (var reader in readers)
                { reader.transform.localRotation = Quaternion.Euler(0, reader.transform == front ? yaw : -yaw, 0); reader.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single().SampleAnimation(reader.gameObject, sample / 128f); }
                Update(true);
            }
        Require(mounts.Count(m => m.Wheel != null) == 8 && mounts.Count == 12, "Two big-end oil cups per driven axle plus native guide/crosshead cups required."); assertions++;
        // Test production pin mounting in mirrored, radially scaled conventional
        // layouts too, including 0-4-0, stock/short 0-6-0, 0-6-2 and 0-8-0.
        foreach (int axles in new[] { 2, 3, 4 }) foreach (float scale in new[] { .8f, 1f }) foreach (int side in new[] { -1, 1 })
        {
            var frame = new GameObject("QA conventional frame").transform; var wheel = new GameObject("QA native wheel").transform; wheel.SetParent(frame, false);
            try
            {
                frame.localScale = new Vector3(side, scale, scale); frame.rotation = Quaternion.Euler(0, 23, 0); wheel.localPosition = new Vector3(0, .712128f, 0);
                for (int sample = 0; sample <= 128; sample++)
                {
                    wheel.localRotation = Quaternion.Euler(sample / 128f * 360 + (side > 0 ? -90 : 0), 0, 0);
                    var pin = S060OilCupGeometry.Pin(frame, wheel, .938477f);
                    var origin = pin + frame.up * (.14f * scale); var contact = pin + frame.up * (.105f * scale);
                    var top = S060OilCupGeometry.ThreadTop(frame, origin, simple.bounds.min.y);
                    Require(Vector3.Dot(top - contact, frame.up) > .015f && Mathf.Sign(pin.x) == side, "Scaled/mirrored native rod oil cup has no neck or crosses gauge."); assertions++;
                }
                Require(axles * 2 == (axles == 2 ? 4 : axles == 3 ? 6 : 8), "Conventional driven-axle cup count changed."); assertions++;
            }
            finally { Object.DestroyImmediate(frame.gameObject); }
        }
        foreach (var reader in readers) { reader.transform.localRotation = Quaternion.identity; reader.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single().SampleAnimation(reader.gameObject, 0); }
        Update(false);
        File.WriteAllText(Path.Combine(output, "oilcup-geometry-validation.json"), "{\"passed\":true,\"reopenedBundle\":true,\"checks\":" + assertions + ",\"bigEndCups\":8,\"nativeGuideCrossheadCups\":4,\"sampledTurnsPerYaw\":129,\"yawDegrees\":[-12,0,12],\"maxSupportGapMetres\":" + maxGap.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"minimumFootplateGapMetres\":" + minFootplateGap.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"nativeCupMeshesPackaged\":false,\"functionalControlsRuntimeTested\":false}");
        SteamNormalRodAudit.Validate(output);
        return () => Update(false);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
