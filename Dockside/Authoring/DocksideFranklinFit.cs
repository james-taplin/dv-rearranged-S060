using UnityEngine;

// Fit the reused valve shafts to the actual S060 engine unit. These stationary
// bearings/castings connect the original moving joints to the cylinder and axle;
// they follow the same bogie and do not introduce another animator.
public static class DocksideFranklinFit
{
    public static void Apply(Transform unit, string folder)
    {
        var iron = SteamBogieVisuals.Metal(folder, "CastIron", new Color(.095f, .105f, .11f), .55f, .28f);
        var steel = SteamBogieVisuals.Metal(folder, "MachinedSteel", new Color(.32f, .34f, .35f), .8f, .55f);
        foreach (int side in new[] { -1, 1 })
        {
            string suffix = side < 0 ? "L" : "R";
            var root = SteamBogieVisuals.Group(unit, "[Franklin fitted supports " + suffix + "]");
            var gear = unit.Find("Franklin fit " + suffix);
            var input = unit.InverseTransformPoint(gear.Find("transmission_box").position);
            // A measured wheel-driven input replaces the former solid axle sleeve.
            DocksideDriveDetail.Fit(unit, root, gear, suffix, side, folder, steel);
            SteamBogieVisuals.Part(root, "Gearbox frame mounting foot", PrimitiveType.Cube,
                new Vector3(input.x, DocksidePlan.DriverRadius + .26f, input.z), new Vector3(.16f, .08f, .24f), iron);
            var joint = unit.InverseTransformPoint(gear.Find("cylinder/u_joint_mapped").position);
            float bottom = DocksidePlan.DriverRadius + .25f, top = joint.y;
            var valve = SteamBogieVisuals.Part(root, "Rotary valve drive housing", PrimitiveType.Cylinder,
                new Vector3(joint.x, (bottom + top) / 2, joint.z), new Vector3(.14f, (top - bottom) / 2, .14f), iron);
            valve.localRotation = Quaternion.identity;
            SteamBogieVisuals.Part(root, "Valve drive bearing cap", PrimitiveType.Cylinder,
                new Vector3(joint.x, top - .045f, joint.z), new Vector3(.18f, .025f, .18f), steel);
            SteamBogieVisuals.Part(root, "Valve chest drive saddle", PrimitiveType.Cube,
                new Vector3(side * 1.13f, bottom, DocksidePlan.CylinderZ - .04f), new Vector3(.28f, .08f, .30f), iron);
        }
    }
}
