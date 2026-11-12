#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Copy alongside a uniquely named QA asmdef outside Assets/Editor.
public static class CollisionContactPlayMode
{
    private const string Pending = "S060.NativeContactAudit.Pending";
    [InitializeOnLoadMethod] private static void Subscribe()
    { EditorApplication.playModeStateChanged -= Changed; EditorApplication.playModeStateChanged += Changed; }
    public static void RunBatch()
    { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); Subscribe(); SessionState.SetBool(Pending, true); EditorApplication.EnterPlaymode(); }
    private static void Changed(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false); new GameObject("Native CID contact audit").AddComponent<CollisionContactPlayModeRunner>();
    }
}

public sealed class CollisionContactPlayModeRunner : MonoBehaviour
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private int checks, sourceEnters, targetEnters, targetStays, targetExits, selfContacts;
    private bool targetPause;
    private object pause, harmony;
    private static object pauseFixture;
    private IDisposable relay;
    private string runtimeSha256, phase;
    private bool nativeEnterStayExit, pauseFlagPreserved, unownedContactsIgnored, relayTeardown, sameFrameRecovery, directParentDestructionReleasesBindings;
    private readonly List<string> history = new List<string>();
    private readonly List<GameObject> owned = new List<GameObject>();
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
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        string managed = Managed;
        var game = Assembly.LoadFrom(Path.Combine(managed, "Assembly-CSharp.dll"));
        var cid = game.GetType("CollisionInfoDispenser", true);
        string runtime = Environment.GetEnvironmentVariable("S060_CONTACT_AUDIT_RUNTIME");
        if (string.IsNullOrEmpty(runtime)) runtime = Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME");
        using (var sha = SHA256.Create()) using (var stream = File.OpenRead(runtime)) runtimeSha256 = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        var production = Assembly.LoadFrom(runtime);
        var bind = production.GetType("RearrangedS060.S060CollisionRelay", true).GetMethod("Bind", BindingFlags.Static | BindingFlags.NonPublic);
        Physics.autoSimulation = true; Time.timeScale = 1;
        var pauseObject = New("Offline pause flag");
        pause = pauseObject.AddComponent(game.GetType("DV.PausePhysicsHandler", true)); ((Behaviour)pause).enabled = false; pauseFixture = pause;
        var ha = Assembly.LoadFrom(Path.Combine(managed, "UnityModManager/0Harmony.dll"));
        var ht = ha.GetType("HarmonyLib.Harmony", true); harmony = Activator.CreateInstance(ht, "s060.contact.pause.fixture");
        var hook = Activator.CreateInstance(ha.GetType("HarmonyLib.HarmonyMethod", true), typeof(CollisionContactPlayModeRunner).GetMethod(nameof(SkipFixtureListeners), BindingFlags.Static | BindingFlags.NonPublic));
        ht.GetMethod("Patch").Invoke(harmony, new object[] { pause.GetType().GetMethod("SetupListeners", Fields), hook, null, null, null });
        string[] cases = { "baseline", "relay", "paused", "unowned", "disposed", "same-frame" };
        for (int index = 0; index < cases.Length; index++)
        {
            phase = cases[index]; int firstObject = owned.Count;
            var origin = new Vector3(100 + index * 30, 100, 100);
            pause.GetType().GetProperty("IgnorePhysicsEvents").SetValue(pause, phase == "paused", null);
            var car = New("car body " + phase); car.transform.position = origin; var carBody = car.AddComponent<Rigidbody>(); carBody.isKinematic = true; car.AddComponent<BoxCollider>();
            var target = car.AddComponent(cid); Listen(cid, target, "CollisionEnterInfo", nameof(TargetEnter)); Listen(cid, target, "CollisionStayInfo", nameof(TargetStay)); Listen(cid, target, "CollisionExitInfo", nameof(TargetExit));
            var bogie = New("rear bogie " + phase); bogie.transform.position = origin; var body = bogie.AddComponent<Rigidbody>(); body.useGravity = false; body.constraints = RigidbodyConstraints.FreezeRotation;
            var joint = bogie.AddComponent<ConfigurableJoint>(); joint.connectedBody = carBody; joint.enableCollision = false; joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Free;
            var leaf = New("owned shell " + phase); leaf.transform.SetParent(bogie.transform, false); var shell = leaf.AddComponent<BoxCollider>();
            if (phase != "baseline") relay = (IDisposable)bind.Invoke(null, new object[] { bogie, target, new Collider[] { shell } });
            if (phase == "disposed" || phase == "same-frame")
            {
                relay.Dispose(); relay.Dispose(); relay = null;
                if (phase == "same-frame") relay = (IDisposable)bind.Invoke(null, new object[] { bogie, target, new Collider[] { shell } });
            }
            var source = bogie.GetComponents(cid).OfType<Behaviour>().FirstOrDefault(c => c.enabled) ?? bogie.AddComponent(cid);
            Listen(cid, source, "CollisionEnterInfo", nameof(SourceEnter));
            var wall = New("external wall " + phase); wall.transform.position = origin + new Vector3(2, 0, phase == "unowned" ? 3 : 0); wall.AddComponent<BoxCollider>();
            if (phase == "unowned") { var other = New("unowned shell"); other.transform.SetParent(bogie.transform, false); other.transform.localPosition = new Vector3(0, 0, 3); other.AddComponent<BoxCollider>(); }
            Physics.SyncTransforms();
            int observed = sourceEnters, enters = targetEnters, stays = targetStays, exits = targetExits;
            body.velocity = Vector3.right * 2;
            yield return Await(() => sourceEnters > observed);
            Check(sourceEnters == observed + 1, phase + ": fresh body/wall must deliver one actual native source Enter");
            bool forwarded = phase == "relay" || phase == "paused" || phase == "same-frame";
            Check(targetEnters == enters + (forwarded ? 1 : 0), phase + ": compiled relay must forward exactly its owned fresh Enter");
            if (forwarded) Check(targetPause == (phase == "paused"), phase + ": native pause flag is retained");
            if (phase == "relay")
            {
                for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
                Check(targetStays > stays, "compiled relay forwards actual native Stay");
                wall.transform.position += Vector3.right * 10; Physics.SyncTransforms();
                yield return Await(() => targetExits > exits);
                Check(targetExits == exits + 1, "compiled relay forwards actual native Exit once"); nativeEnterStayExit = true;
            }
            if (phase == "paused") pauseFlagPreserved = true;
            if (phase == "unowned") unownedContactsIgnored = true;
            if (phase == "disposed") relayTeardown = true;
            if (phase == "same-frame") sameFrameRecovery = true;
            relay?.Dispose(); relay = null;
            for (int i = owned.Count - 1; i >= firstObject; i--) if (owned[i] != null) { owned[i].SetActive(false); DestroyImmediate(owned[i]); }
            yield return null;
        }
        Check(selfContacts == 0, "connected native bodies never generate self-contact events");
        phase = "parent-destruction";
        var nativeRoot = New("owner native collision root"); nativeRoot.transform.position = new Vector3(1000, 100, 100);
        nativeRoot.AddComponent<Rigidbody>().isKinematic = true; var materialSource = nativeRoot.AddComponent<BoxCollider>();
        var frame = New("owner rear frame"); frame.transform.position = nativeRoot.transform.position;
        var ownerType = production.GetType("RearrangedS060.S060MeyerCollision", true);
        var owner = (IDisposable)ownerType.GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { nativeRoot.transform, frame.transform, materialSource });
        var counter = new DisposeCounter(); ((IList)ownerType.GetField("contactBindings", Fields).GetValue(owner)).Add(counter);
        Check(((Component)owner).gameObject.activeInHierarchy, "production collision owner is active before parent deletion");
        Destroy(frame);
        yield return null;
        yield return null;
        Check((UnityEngine.Object)owner == null && counter.Count == 1, "direct parent deletion releases production owner's contact bindings exactly once");
        owner.Dispose(); owner.Dispose();
        Check(counter.Count == 1, "post-destruction production Dispose remains idempotent");
        directParentDestructionReleasesBindings = true;
    }
    private IEnumerator Await(Func<bool> condition)
    { float end = Time.realtimeSinceStartup + 3; while (!condition() && Time.realtimeSinceStartup < end) yield return new WaitForFixedUpdate(); }
    private GameObject New(string name) { var go = new GameObject(name); owned.Add(go); return go; }
    private void Listen(Type type, object receiver, string name, string method)
    { var e = type.GetEvent(name); e.AddEventHandler(receiver, Delegate.CreateDelegate(e.EventHandlerType, this, GetType().GetMethod(method, Fields))); }
    private void SourceEnter(Collision c, bool paused) { sourceEnters++; if (c.collider.name.StartsWith("car body", StringComparison.Ordinal)) selfContacts++; history.Add(phase + ":" + c.collider.name + ":target=" + targetEnters + ":this=" + (c.contactCount > 0 ? c.GetContact(0).thisCollider.name : "none")); }
    private void TargetEnter(Collision c, bool paused) { targetEnters++; targetPause = paused; }
    private void TargetStay(Collision c, bool paused) { targetStays++; }
    private void TargetExit(Collision c, bool paused) { targetExits++; }
    private void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); checks++; }
    // This disabled fixture never registered native AppUtil listeners.
    private static bool SkipFixtureListeners(object __instance) => !ReferenceEquals(__instance, pauseFixture);
    private void Finish(bool passed, string error)
    {
        Physics.autoSimulation = false; relay?.Dispose(); relay = null;
        foreach (var go in owned.AsEnumerable().Reverse()) if (go != null) DestroyImmediate(go);
        pauseFixture = null; if (harmony != null) harmony.GetType().GetMethod("UnpatchAll").Invoke(harmony, new object[] { "s060.contact.pause.fixture" });
        string output = Environment.GetEnvironmentVariable("S060_CONTACT_AUDIT_OUTPUT"); Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "native-contact-delivery.json"), JsonUtility.ToJson(new Report { passed = passed, checks = checks, actualPlayMode = Application.isPlaying, actualNativeCID = true,
            productionRelayUsed = runtimeSha256 != null, runtimeSha256 = runtimeSha256, nativeEnterStayExit = nativeEnterStayExit, pauseFlagPreserved = pauseFlagPreserved,
            unownedContactsIgnored = unownedContactsIgnored, relayTeardown = relayTeardown, sameFrameRecovery = sameFrameRecovery, directParentDestructionReleasesBindings = directParentDestructionReleasesBindings,
            sourceEnters = sourceEnters, targetEnters = targetEnters, targetStays = targetStays, targetExits = targetExits, selfContacts = selfContacts, history = history.ToArray(), error = error }, true));
        if (error != null) Debug.LogError(error); EditorApplication.Exit(passed ? 0 : 1);
    }
    private static string Managed => Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED") ?? @"B:\SteamLibrary\steamapps\common\Derail Valley\DerailValley_Data\Managed";
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        string name = new AssemblyName(args.Name).Name; var found = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name); if (found != null) return found;
        foreach (var folder in new[] { Managed, Path.Combine(Managed, "UnityModManager") }) { string path = Path.Combine(folder, name + ".dll"); if (File.Exists(path)) return Assembly.LoadFrom(path); } return null;
    }
    private sealed class DisposeCounter : IDisposable { internal int Count; public void Dispose() { Count++; } }
    [Serializable] private sealed class Report
    {
        public bool passed, actualPlayMode, actualNativeCID, productionRelayUsed, nativeEnterStayExit, pauseFlagPreserved, unownedContactsIgnored, relayTeardown, sameFrameRecovery, directParentDestructionReleasesBindings;
        public int checks, sourceEnters, targetEnters, targetStays, targetExits, selfContacts; public string error, runtimeSha256; public string[] history;
    }
}
#endif
