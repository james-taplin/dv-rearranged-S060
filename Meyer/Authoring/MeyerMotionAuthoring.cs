using System;
using System.Linq;
using CCL.Types.Proxies.Ports;
using CCL.Types.Proxies.Wheels;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using RearrangedS060;
using Object = UnityEngine.Object;

public static class MeyerMotionAuthoring
{
    public static Vector3 Pin(float t, int side, bool front)
    {
        float a = t * 2 * Mathf.PI;
        return new Vector3(side * S060OilCupGeometry.CouplingRodPlaneX, (float)MeyerPlan.DriverRadius - MeyerPlan.CrankRadius * Mathf.Sin(a),
            (front ? 1 : -1) * (float)MeyerPlan.AxlePitch / 2 + MeyerPlan.CrankRadius * Mathf.Cos(a));
    }
    public static Vector3 MainPin(float t, int side) { var p = Pin(t, side, false); p.x = side * S060OilCupGeometry.MainRodPlaneX; return p; }
    public static Vector3 PistonCrosshead(float t, int side) { var p = Crosshead(t, side); p.x = side * S060OilCupGeometry.PistonAxisX; return p; }
    public static Vector3 Crosshead(float t, int side)
    {
        var p = MainPin(t, side); float dy = p.y - (float)MeyerPlan.DriverRadius;
        return new Vector3(p.x, (float)MeyerPlan.DriverRadius, p.z + Mathf.Sqrt(MeyerPlan.MainRodLength * MeyerPlan.MainRodLength - dy * dy));
    }
    public static void Apply(GameObject root, Transform front, Transform rear, string folder, Material metal)
    {
        var old = root.GetComponent<PoweredWheelRotationViaCodeProxy>(); if (old != null) Object.DestroyImmediate(old);
        string clipPath = folder + "/MeyerDrive.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, clipPath); }
        clip.ClearCurves(); clip.frameRate = 60; clip.wrapMode = WrapMode.Loop;
        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true; AnimationUtility.SetAnimationClipSettings(clip, settings);
        foreach (int side in new[] { -1, 1 })
        {
            string suffix = side < 0 ? "L" : "R"; float quarter = side < 0 ? .25f : 0;
            foreach (var unit in new[] { rear, front }) CreateRods(unit, suffix, side, metal, folder);
            foreach (string axle in new[] { "[axle] rear", "[axle] front" })
                Curve(clip, axle + "/S060 driver " + suffix + " (native alias)", "localEulerAnglesRaw.x", t => 360 * t + (side > 0 ? -90 : 0));
            string path = "[Meyer rods " + suffix + "]/";
            Position(clip, path + "Coupling rod", t => (Pin(t + quarter, side, false) + Pin(t + quarter, side, true)) / 2);
            Position(clip, path + "Main rod", t => (MainPin(t + quarter, side) + Crosshead(t + quarter, side)) / 2);
            Curve(clip, path + "Main rod", "localEulerAnglesRaw.x", t => -Mathf.Asin(MeyerPlan.CrankRadius * Mathf.Sin((t + quarter) * 2 * Mathf.PI) / MeyerPlan.MainRodLength) * Mathf.Rad2Deg);
            Position(clip, path + "Crosshead", t => Crosshead(t + quarter, side));
            var gland = new Vector3(side * 1.075f, (float)MeyerPlan.DriverRadius, MeyerPlan.CylinderZ + .05f);
            Position(clip, path + "Piston rod", t => (PistonCrosshead(t + quarter, side) + gland) / 2);
            Curve(clip, path + "Piston rod", "m_LocalScale.z", t => gland.z - Crosshead(t + quarter, side).z);
            foreach (string axis in new[] { "x", "y" }) Curve(clip, path + "Piston rod", "m_LocalScale." + axis, t => .035f);
            foreach (bool first in new[] { false, true })
                Position(clip, path + (first ? "Front crank eye" : "Rear crank eye"), t => Pin(t + quarter, side, first) + new Vector3(side * .03f, 0, 0));
        }
        clip.EnsureQuaternionContinuity(); EditorUtility.SetDirty(clip);
        string controllerPath = folder + "/MeyerDrive.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(controllerPath, clip);
        var state = controller.layers[0].stateMachine.states.Single().state;
        state.motion = clip; state.speed = 0; state.writeDefaultValues = false; EditorUtility.SetDirty(state); EditorUtility.SetDirty(controller);
        foreach (var unit in new[] { rear, front })
        {
            var animator = unit.GetComponent<Animator>();
            if (animator == null) animator = unit.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.keepAnimatorControllerStateOnDisable = true;
            var reader = unit.GetComponent<AnimatorPortReaderProxy>();
            if (reader == null) reader = unit.gameObject.AddComponent<AnimatorPortReaderProxy>();
            reader.updateType = AnimatorPortReaderProxy.UpdateType.SET_NORMALIZED_TIME;
            reader.portId = (unit == front ? MeyerSteamAuthoring.Hp : MeyerSteamAuthoring.Lp) + ".CRANK_ROTATION";
            reader.valueMultiplier = 1; reader.valueOffset = 0;
        }
    }
    private static void CreateRods(Transform unit, string suffix, int side, Material metal, string folder)
    {
        var root = SteamBogieVisuals.Group(unit, "[Meyer rods " + suffix + "]");
        foreach (var rod in new[] { new { Name = "Coupling rod", Asset = "MeyerCoupling", Length = (float)MeyerPlan.AxlePitch }, new { Name = "Main rod", Asset = "MeyerMain", Length = MeyerPlan.MainRodLength } })
        {
            var part = new GameObject(rod.Name); part.transform.SetParent(root, false);
            part.transform.localScale = new Vector3(1, 1, rod.Length);
            part.AddComponent<MeshFilter>().sharedMesh = SteamRodGeometry.Create(folder, rod.Asset, rod.Length);
            part.AddComponent<MeshRenderer>().sharedMaterial = metal;
        }
        SteamBogieVisuals.Part(root, "Crosshead", PrimitiveType.Cube, Vector3.zero, new Vector3(.13f, .09f, .13f), metal);
        SteamBogieVisuals.Part(root, "Piston rod", PrimitiveType.Cube, Vector3.zero, new Vector3(.035f, .035f, 1), metal);
        foreach (string name in new[] { "Rear crank eye", "Front crank eye" })
            SteamBogieVisuals.Part(root, name, PrimitiveType.Cylinder, Vector3.zero, new Vector3(.15f, .0075f, .15f), metal).localRotation = Quaternion.Euler(0, 0, 90);
        foreach (int sign in new[] { -1, 1 })
            SteamBogieVisuals.Part(root, "Crosshead guide " + sign, PrimitiveType.Cube, new Vector3(side * S060OilCupGeometry.MainRodPlaneX, (float)MeyerPlan.DriverRadius + sign * .075f, .31f), new Vector3(.08f, .025f, .90f), metal);
        foreach (float z in new[] { -.14f, .76f })
        {
            foreach (int edge in new[] { -1, 1 })
                SteamBogieVisuals.Part(root, "Guide end cheek", PrimitiveType.Cube, new Vector3(side * S060OilCupGeometry.MainRodPlaneX + edge * .09f, (float)MeyerPlan.DriverRadius, z), new Vector3(.025f, .20f, .025f), metal);
            SteamBogieVisuals.Part(root, "Guide lower tie", PrimitiveType.Cube, new Vector3(side * S060OilCupGeometry.MainRodPlaneX, (float)MeyerPlan.DriverRadius - .1125f, z), new Vector3(.205f, .025f, .025f), metal);
        }
    }
    private static void Position(AnimationClip clip, string path, Func<float, Vector3> value)
    { for (int axis = 0; axis < 3; axis++) { int i = axis; Curve(clip, path, "m_LocalPosition." + "xyz"[axis], t => value(t)[i]); } }
    private static void Curve(AnimationClip clip, string path, string property, Func<float, float> value)
    {
        var keys = Enumerable.Range(0, 257).Select(i => new Keyframe(i / 256f, value(i / 256f))).ToArray();
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < keys.Length; i++) { AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear); AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear); }
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
    }
}
