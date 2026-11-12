#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds only a private native-method fixture. It is never installed or copied
// into a release. Game ECS NativeContainer layouts require a Mono player, rather
// than imported player assemblies running in Editor safety-checking mode.
public static class IndependentSlipNativeAudit
{
    public static void BuildPlayer()
    {
        try
        {
            string root = Directory.GetParent(Application.dataPath).Parent.FullName;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrap = Type.GetType("IndependentSlipPlayerBootstrap, S060.IndependentSlipBootstrap", true);
            new GameObject("Offline independent-slip fixture").AddComponent(bootstrap);
            const string path = "Assets/IndependentSlipFixture.unity";
            if (!EditorSceneManager.SaveScene(scene, path)) throw new InvalidOperationException("Cannot save private fixture scene.");
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Standalone, ManagedStrippingLevel.Disabled);
            PlayerSettings.stripEngineCode = false;
            string executable = Path.Combine(root, "player", "IndependentSlipFixture.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(executable));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { path }, locationPathName = executable,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Private fixture build failed: " + report.summary.result);
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
#endif
