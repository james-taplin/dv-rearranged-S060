using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public static class MeyerMechanicalAuthoring
{
    // Replace the SDK's 16.88 m generic platform envelopes. Body bounds and
    // coupling height/centres come from the measured S060 probe; bogie capsule
    // lengths are candidates for this new two-axle geometry, pending rail tests.
    public static void Apply(GameObject root, bool extended = false)
    {
        var colliders = root.transform.Find("[colliders]");
        if (colliders == null) throw new InvalidOperationException("CCL collider root missing.");
        foreach (Transform child in colliders.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
        var collision = Group(colliders, "[collision]");
        Box(collision, "body", new Vector3(0, 2.12f, -.1f), new Vector3(2.6f, 3.4f, 8.2f));
        if (extended)
        {
            Box(collision, "rear deck", new Vector3(0, 1.55f, MeyerPlan.RearCentre - .25f), new Vector3(2.35f, .10f, 3.4f));
            Box(collision, "rear buffer beam", new Vector3(0, 1.05f, MeyerPlan.RearCentre - 1.8f), new Vector3(2.35f, .25f, .16f));
            foreach (int side in new[] { -1, 1 }) Box(collision, "rear pannier " + side, new Vector3(side * (float)MeyerPlan.TankCentreX, (float)MeyerPlan.TankCentreY, MeyerPlan.RearCentre), new Vector3(.30f, .55f, 1.30f));
        }
        var walkable = Group(colliders, "[walkable]");
        Box(walkable, "prototype deck", new Vector3(0, 1.02f, -.1f), new Vector3(2.6f, .10f, 8.2f));
        var items = Group(colliders, "[items]");
        Box(items, "prototype deck", new Vector3(0, 1.02f, -.1f), new Vector3(2.6f, .10f, 8.2f));
        var camera = Group(colliders, "[camera dampening]");
        var cameraBox = Box(camera, "cab", new Vector3(0, 2.66f, -2.384f), new Vector3(2.5f, 2.25f, 2.2f)); cameraBox.isTrigger = true;
        var bogies = Group(colliders, "[bogies]");
        foreach (float direction in new[] { 1f, -1f })
        {
            var holder = Group(bogies, direction > 0 ? "front" : "rear");
            var capsule = holder.gameObject.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0, .71f, extended ? (direction > 0 ? MeyerPlan.FrontCentre : MeyerPlan.RearCentre) : direction * (float)MeyerPlan.BogieCentre);
            capsule.direction = 2; capsule.radius = .71f; capsule.height = (float)MeyerPlan.AxlePitch + 1.42f;
        }
        SetCoupler(root.transform.Find("[coupler_rig_front]"), 3.97f);
        SetCoupler(root.transform.Find("[coupler_rig_rear]"), extended ? MeyerPlan.RearCouplerZ : -3.97f);
    }
    private static void SetCoupler(Transform rig, float z)
    {
        if (rig == null) throw new InvalidOperationException("CCL coupling rig missing.");
        rig.localPosition = new Vector3(0, 1.05f, z);
    }
    private static Transform Group(Transform parent, string name)
    { var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform; }
    private static BoxCollider Box(Transform parent, string name, Vector3 centre, Vector3 size)
    { var go = Group(parent, name); var box = go.gameObject.AddComponent<BoxCollider>(); box.center = centre; box.size = size; return box; }
}
