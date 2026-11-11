using System;
using System.IO;
using System.Linq;
using CCL.Types.Proxies.Ports;
using CCL.Types.Proxies.Wheels;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

// All motion is authored into one native Animator per engine unit. The native
// crank port supplies absolute time; no independent visual RPM integrator.
public static class DocksideMotionAuthoring
{
    private const int Samples = 256;
    public static void Apply(GameObject root, Transform front, Transform rear, string folder, Material material)
    {
        var code = root.GetComponent<PoweredWheelRotationViaCodeProxy>();
        if (code != null) Object.DestroyImmediate(code);
        string clipPath = folder + "/DocksideDrive.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, clipPath); }
        clip.ClearCurves(); clip.frameRate = 60; clip.wrapMode = WrapMode.Loop;
        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        // Built bundles omit editor curve bindings. Import the existing authored
        // source clip locally and copy only its custom Franklin joint channels.
        var source = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DocksideSource/FranklinSource.anim");
        if (source == null) throw new InvalidOperationException("Original authored Franklin gear clip missing.");
        try
        {
            foreach (int side in new[] { 1, -1 })
            {
                float quarter = side > 0 ? 0 : .25f;
                string suffix = side > 0 ? "R" : "L";
                string gearPath = "Franklin fit " + suffix;
                // Copy only the custom Franklin shaft/joint curves, preserving
                // authored joint angles and the original 2:1 transmission motion.
                int paths = 0;
                foreach (var binding in AnimationUtility.GetCurveBindings(source).Where(b => b.path.StartsWith("franklin_type_b(Clone)/", StringComparison.Ordinal)))
                {
                    string path = binding.path.Substring("franklin_type_b(Clone)/".Length);
                    if (path == "cylinder/u_joint") path = "cylinder/u_joint_mapped";
                    if (front.Find(gearPath + "/" + path) == null) throw new InvalidOperationException("Franklin animated joint missing: " + path);
                    var curve = AnimationUtility.GetEditorCurve(source, binding);
                    var target = EditorCurveBinding.FloatCurve(gearPath + "/" + path, binding.type, binding.propertyName);
                    // Keep cyclic angles continuous across the quarter-turn wrap.
                    float cycleDelta = curve.Evaluate(source.length) - curve.Evaluate(0);
                    Curve(clip, target, t => curve.Evaluate(Mathf.Repeat(t + quarter, 1) * source.length) + Mathf.Floor(t + quarter) * cycleDelta);
                    paths++;
                }
                if (paths < 5) throw new InvalidOperationException("Franklin source motion curves were not readable.");
                for (int axle = 0; axle < 2; axle++)
                {
                    var axles = front.Cast<Transform>().Where(t => t.name.StartsWith("[axle]", StringComparison.Ordinal)).ToArray();
                    string path = axles[axle].name + "/Small driver " + suffix + " (native alias)";
                    Curve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.x"), t => t * 360 + (side > 0 ? -90 : 0));
                    Curve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.y"), t => 0);
                    Curve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.z"), t => 0);
                }
                foreach (var unit in new[] { front, rear }) CreateRods(unit, suffix, side, material, folder);
                string rods = "[Dockside rods " + suffix + "]";
                Curve(clip, EditorCurveBinding.FloatCurve("Franklin return crank " + suffix, typeof(Transform), "localEulerAnglesRaw.x"), t => (t + quarter) * 360);
                Position(clip, rods + "/Coupling rod", t => Coupling(t + quarter, side));
                Position(clip, rods + "/Main rod", t => (MainPin(t + quarter, side) + Crosshead(t + quarter, side)) / 2);
                Curve(clip, EditorCurveBinding.FloatCurve(rods + "/Main rod", typeof(Transform), "localEulerAnglesRaw.x"), t => MainAngle(t + quarter));
                Curve(clip, EditorCurveBinding.FloatCurve(rods + "/Main rod", typeof(Transform), "localEulerAnglesRaw.y"), t => 0);
                Curve(clip, EditorCurveBinding.FloatCurve(rods + "/Main rod", typeof(Transform), "localEulerAnglesRaw.z"), t => 0);
                Position(clip, rods + "/Crosshead", t => Crosshead(t + quarter, side));
                Position(clip, rods + "/Crosshead bearing pin", t => Crosshead(t + quarter, side));
                Position(clip, rods + "/Piston rod", t => (PistonCrosshead(t + quarter, side) + new Vector3(side * DocksidePlan.PistonAxisX, DocksidePlan.DriverRadius, DocksidePlan.CylinderZ + .05f)) / 2);
                Curve(clip, EditorCurveBinding.FloatCurve(rods + "/Piston rod", typeof(Transform), "m_LocalScale.z"), t => DocksidePlan.CylinderZ + .05f - Crosshead(t + quarter, side).z);
                foreach (string axis in new[] { "x", "y" })
                    Curve(clip, EditorCurveBinding.FloatCurve(rods + "/Piston rod", typeof(Transform), "m_LocalScale." + axis), t => .035f);
                Position(clip, rods + "/Rear crank eye", t => Pin(t + quarter, side, false) + new Vector3(side * .03f, 0, 0));
                Position(clip, rods + "/Front crank eye", t => Pin(t + quarter, side, true) + new Vector3(side * .03f, 0, 0));
            }
        }
        finally { }
        clip.EnsureQuaternionContinuity(); EditorUtility.SetDirty(clip);
        string controllerPath = folder + "/DocksideDrive.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(controllerPath, clip);
        var state = controller.layers[0].stateMachine.states.Single().state;
        state.motion = clip; state.speed = 0; state.writeDefaultValues = false;
        EditorUtility.SetDirty(state); EditorUtility.SetDirty(controller);
        foreach (var unit in new[] { rear, front })
        {
            var animator = unit.GetComponent<Animator>();
            if (animator == null) animator = unit.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.keepAnimatorControllerStateOnDisable = true;
            var reader = unit.GetComponent<AnimatorPortReaderProxy>();
            if (reader == null) reader = unit.gameObject.AddComponent<AnimatorPortReaderProxy>();
            reader.updateType = AnimatorPortReaderProxy.UpdateType.SET_NORMALIZED_TIME;
            reader.portId = (unit == rear ? MeyerSteamAuthoring.Hp : MeyerSteamAuthoring.Lp) + ".CRANK_ROTATION";
            reader.valueMultiplier = 1; reader.valueOffset = 0;
        }
        AssetDatabase.SaveAssets();
    }
    public static Vector3 Pin(float turns, int side, bool front)
    {
        float a = turns * 2 * Mathf.PI;
        return new Vector3(side * DocksidePlan.RodPlaneX, DocksidePlan.DriverRadius - DocksidePlan.CrankRadius * Mathf.Sin(a),
            (front ? 1 : -1) * DocksidePlan.AxlePitch / 2 + DocksidePlan.CrankRadius * Mathf.Cos(a));
    }
    public static Vector3 Crosshead(float turns, int side)
    {
        var p = MainPin(turns, side); float dy = p.y - DocksidePlan.DriverRadius;
        return new Vector3(p.x, DocksidePlan.DriverRadius, p.z + Mathf.Sqrt(DocksidePlan.MainRodLength * DocksidePlan.MainRodLength - dy * dy));
    }
    public static Vector3 PistonCrosshead(float turns, int side) { var p = Crosshead(turns, side); p.x = side * DocksidePlan.PistonAxisX; return p; }
    public static Vector3 MainPin(float turns, int side) { var p = Pin(turns, side, false); p.x = side * DocksidePlan.MainRodPlaneX; return p; }
    private static Vector3 Coupling(float turns, int side) => (Pin(turns, side, false) + Pin(turns, side, true)) / 2;
    private static float MainAngle(float turns) => -Mathf.Asin(DocksidePlan.CrankRadius * Mathf.Sin(turns * 2 * Mathf.PI) / DocksidePlan.MainRodLength) * Mathf.Rad2Deg;
    private static void CreateRods(Transform unit, string suffix, int side, Material material, string folder)
    {
        string name = "[Dockside rods " + suffix + "]";
        var old = unit.Find(name); if (old != null) Object.DestroyImmediate(old.gameObject);
        var parent = new GameObject(name).transform; parent.SetParent(unit, false);
        foreach (var rod in new[] { new { Name = "Coupling rod", Asset = "Coupling", Length = DocksidePlan.AxlePitch }, new { Name = "Main rod", Asset = "Main", Length = DocksidePlan.MainRodLength } })
        {
            var part = new GameObject(rod.Name); part.transform.SetParent(parent, false);
            part.transform.localScale = new Vector3(1, 1, rod.Length);
            part.AddComponent<MeshFilter>().sharedMesh = DocksideRodGeometry.Create(folder, rod.Asset, rod.Length);
            var renderer = part.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; SteamBogieVisuals.LiveMaterial(renderer);
        }
        var crosshead = DocksideDriveDetail.Part(parent, "Crosshead", DocksideDriveDetail.Save(folder, DocksideDetailMesh.Slider("DocksideDetailCrosshead")), SteamBogieVisuals.Metal(folder, "CastIron", new Color(.095f, .105f, .11f), .55f, .28f), false);
        crosshead.localScale = new Vector3(.13f, .10f, .15f);
        DocksideDriveDetail.Part(parent, "Crosshead bearing pin", DocksideDriveDetail.Save(folder, DocksideDetailMesh.Tube("DocksideDetailCrossheadPin", .020f, .005f, -.071f, .071f)), material);
        var piston = DocksideDriveDetail.Part(parent, "Piston rod", DocksideDriveDetail.Save(folder, DocksideDetailMesh.Piston("DocksideDetailPiston")), material);
        piston.localScale = new Vector3(.035f, .035f, 1);
        foreach (string end in new[] { "Rear", "Front" })
        {
            DocksideDriveDetail.Part(parent, end + " crank eye", DocksideDriveDetail.Save(folder, DocksideDetailMesh.Tube("DocksideDetailCrankWasher", .060f, .042f, -.0075f, .0075f)), material);
        }
        foreach (int sign in new[] { -1, 1 }) foreach (int cheek in new[] { -1, 1 })
        {
            string guide = (cheek < 0 ? "Inner " : "") + (sign < 0 ? "Lower crosshead guide" : "Upper crosshead guide");
            var rail = Primitive(parent, guide, PrimitiveType.Cube, new Vector3(.037f, .025f, .78f), material);
            rail.localPosition = new Vector3(side * (DocksidePlan.MainRodPlaneX + cheek * .0465f), DocksidePlan.DriverRadius + sign * .0675f, .80f);
        }
        // Crosshead guides now have fixed end brackets tied to the frame,
        // rather than appearing as two unsupported rectangular strips.
        foreach (float z in new[] { .41f, 1.19f })
        {
            foreach (int edge in new[] { -1, 1 })
            {
                var bracket = Primitive(parent, "Crosshead guide end cheek", PrimitiveType.Cube, new Vector3(.025f, .18f, .035f), material);
                bracket.localPosition = new Vector3(side * DocksidePlan.MainRodPlaneX + edge * .09f, DocksidePlan.DriverRadius, z);
            }
            var tie = Primitive(parent, "Crosshead guide bottom tie", PrimitiveType.Cube, new Vector3(.205f, .025f, .035f), material);
            tie.localPosition = new Vector3(side * DocksidePlan.MainRodPlaneX, DocksidePlan.DriverRadius - .10f, z);
        }
        var frame = Primitive(parent, "Frame support", PrimitiveType.Cube, new Vector3(.10f, .08f, 1.45f), material);
        frame.localPosition = new Vector3(side * .58f, .74f, 0);
    }
    private static Transform Primitive(Transform parent, string name, PrimitiveType kind, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(kind); go.name = name; go.transform.SetParent(parent, false); go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material; Object.DestroyImmediate(go.GetComponent<Collider>()); return go.transform;
    }
    private static void Position(AnimationClip clip, string path, Func<float, Vector3> sample)
    { foreach (int axis in new[] { 0, 1, 2 }) { int i = axis; Curve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition." + "xyz"[axis]), t => sample(t)[i]); } }
    private static void Curve(AnimationClip clip, EditorCurveBinding binding, Func<float, float> sample)
    {
        var keys = Enumerable.Range(0, Samples + 1).Select(i => new Keyframe((float)i / Samples, sample((float)i / Samples))).ToArray();
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < keys.Length; i++) { AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear); AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear); }
        AnimationUtility.SetEditorCurve(clip, binding, curve);
    }
}
