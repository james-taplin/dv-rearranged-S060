#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class QuarteringPlayMode
{
    private const string Pending = "S060.QuarteringAudit.Pending";
    [InitializeOnLoadMethod] private static void Subscribe()
    { EditorApplication.playModeStateChanged -= Changed; EditorApplication.playModeStateChanged += Changed; }
    public static void RunBatch()
    {
        Application.runInBackground = true; EditorApplication.isPaused = false;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        const string folder = "Assets/QA/QuarteringFixture";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/QA", "QuarteringFixture");
        var clip = new AnimationClip { name = "Quartering fixture" };
        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("wheel", typeof(Transform), "localEulerAnglesRaw.x"), AnimationCurve.Linear(0, 0, 1, 360));
        const string clipPath = folder + "/Drive.anim", controllerPath = folder + "/Drive.controller";
        // Generated fixture assets only, inside the ignored authoring project.
        AssetDatabase.DeleteAsset(clipPath); AssetDatabase.DeleteAsset(controllerPath);
        AssetDatabase.CreateAsset(clip, clipPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(controllerPath, clip);
        controller.AddParameter("speed", AnimatorControllerParameterType.Float);
        var state = controller.layers[0].stateMachine.states.Single().state;
        state.speedParameter = "speed"; state.speedParameterActive = true;
        EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
        Subscribe(); SessionState.SetBool(Pending, true); EditorApplication.EnterPlaymode();
    }
    private static void Changed(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false); new GameObject("S060 native quartering audit").AddComponent<QuarteringPlayModeRunner>();
    }
}

public sealed class QuarteringPlayModeRunner : MonoBehaviour
{
    private GameObject root;
    private Animator left, right;
    private int checks;
    private string hash;
    private Type ownerType;
    private object owner;
    private bool oldResetReproduced, repeatedTransitions, forwardReverse, flagsRestored, teardown;
    private IEnumerator Start()
    {
        var run = Run();
        while (true)
        {
            object next;
            try { if (!run.MoveNext()) break; next = run.Current; }
            catch (Exception ex) { Finish(false, ex.ToString()); yield break; }
            yield return next;
        }
        Finish(true, null);
    }
    private IEnumerator Run()
    {
        Time.timeScale = 1; Application.runInBackground = true;
        var runtime = Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME");
        using (var sha = SHA256.Create()) using (var stream = File.OpenRead(runtime)) hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        ownerType = Assembly.LoadFrom(runtime).GetType("RearrangedS060.S060NativeQuartering", true);
        BuildPair(false, false); Seek(.25f, 0);
        yield return null;
        Check(GapError() < .1f, "Baseline native-style pair starts quartered");
        left.gameObject.SetActive(false); right.gameObject.SetActive(false);
        left.gameObject.SetActive(true); right.gameObject.SetActive(true);
        yield return null; yield return null;
        Check(GapError() > 89f, "Old disable/reactivate path actually loses quartering in Unity 2019");
        oldResetReproduced = true; DestroyImmediate(root);

        for (int flags = 0; flags < 4; flags++)
        {
            bool keepLeft = (flags & 1) != 0, keepRight = (flags & 2) != 0;
            BuildPair(keepLeft, keepRight);
            Seek(.16f, .91f); yield return null;
            for (int cycle = 0; cycle < 4; cycle++)
            {
                float leftTime = Phase(left), rightTime = Phase(right);
                var leftPose = left.transform.Find("wheel").localRotation;
                var rightPose = right.transform.Find("wheel").localRotation;
                owner = Activator.CreateInstance(ownerType, BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new object[] { left.transform, right.transform }, null);
                ownerType.GetMethod("Retain", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, null);
                Check(left.keepAnimatorControllerStateOnDisable && right.keepAnimatorControllerStateOnDisable, "Both original states retained before hiding");
                left.gameObject.SetActive(false); right.gameObject.SetActive(false);
                yield return null; yield return null;
                left.gameObject.SetActive(true); right.gameObject.SetActive(true);
                // Production Undo restores the hierarchy/readers then the flags
                // in the same frame, before the next Animator sample.
                ((IDisposable)owner).Dispose(); ((IDisposable)owner).Dispose(); owner = null;
                yield return null; yield return null;
                Check(CycleError(Phase(left), leftTime) < .1f && CycleError(Phase(right), rightTime) < .1f, "Repeated return preserves the exact paused native cycle");
                Check(GapError() < .1f, "Repeated articulated hide/return preserves quartering");
                Check(Quaternion.Angle(leftPose, left.transform.Find("wheel").localRotation) < .1f && Quaternion.Angle(rightPose, right.transform.Find("wheel").localRotation) < .1f, "Actual sampled wheel poses survive return");
                Check(left.keepAnimatorControllerStateOnDisable == keepLeft && right.keepAnimatorControllerStateOnDisable == keepRight, "Original per-side retention flags restored exactly");
                foreach (float speed in new[] { 1f, -1f })
                {
                    left.SetFloat("speed", speed); right.SetFloat("speed", speed);
                    for (int frame = 0; frame < 4; frame++) yield return null;
                    left.SetFloat("speed", 0); right.SetFloat("speed", 0);
                    Check(GapError() < .1f, "Native forward/reverse playback stays quartered after return");
                }
            }
            DestroyImmediate(root);
        }
        repeatedTransitions = forwardReverse = flagsRestored = true;
        BuildPair(false, true);
        owner = Activator.CreateInstance(ownerType, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { left.transform, right.transform }, null);
        ownerType.GetMethod("Retain", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, null);
        DestroyImmediate(left.gameObject); ((IDisposable)owner).Dispose(); ((IDisposable)owner).Dispose(); owner = null;
        Check(right.keepAnimatorControllerStateOnDisable, "Cleanup tolerates a destroyed native side and restores the surviving side's original setting");
        teardown = true;
    }
    private void BuildPair(bool keepLeft, bool keepRight)
    {
        root = new GameObject("Original S060 mechanism fixture");
        Animator Side(string name, bool keep)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform, false);
            new GameObject("wheel").transform.SetParent(go.transform, false);
            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/QA/QuarteringFixture/Drive.controller");
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.keepAnimatorControllerStateOnDisable = keep; return animator;
        }
        left = Side("DrivingMechanism_L", keepLeft); right = Side("DrivingMechanism_R", keepRight);
    }
    private void Seek(float l, float r)
    {
        // Seed the fixture once, as the native wheel controller's Awake does.
        // Production never calls Play, Update or Rebind.
        foreach (var a in new[] { left, right }) { a.SetFloat("speed", 0); a.Play("Base Layer.Drive", 0, a == left ? l : r); a.Update(0); }
    }
    private static float Phase(Animator a) => Mathf.Repeat(a.GetCurrentAnimatorStateInfo(0).normalizedTime, 1);
    private static float CycleError(float a, float b) => Mathf.Abs(Mathf.DeltaAngle(a * 360, b * 360));
    private float GapError() => CycleError(Mathf.Repeat(Phase(left) - Phase(right), 1), .25f);
    private void Check(bool passed, string message) { if (!passed) throw new InvalidOperationException(message); checks++; }
    private void Finish(bool passed, string error)
    {
        (owner as IDisposable)?.Dispose(); if (root != null) DestroyImmediate(root);
        string output = Environment.GetEnvironmentVariable("S060_QUARTERING_AUDIT_OUTPUT"); Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "quartering-validation.json"), JsonUtility.ToJson(new Report { passed = passed, checks = checks, runtimeSha256 = hash,
            actualPlayMode = Application.isPlaying, productionRetention = hash != null, oldResetReproduced = oldResetReproduced, repeatedTransitions = repeatedTransitions,
            forwardReverse = forwardReverse, originalFlagsRestored = flagsRestored, teardown = teardown, gameScene = false, error = error }, true));
        if (error != null) Debug.LogError(error); EditorApplication.Exit(passed ? 0 : 1);
    }
    [Serializable] private sealed class Report
    {
        public bool passed, actualPlayMode, productionRetention, oldResetReproduced, repeatedTransitions, forwardReverse, originalFlagsRestored, teardown, gameScene;
        public int checks; public string runtimeSha256, error;
    }
}
#endif
