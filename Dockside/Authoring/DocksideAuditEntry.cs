using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using CCL.Types;
using System.Linq;

// Reopen an existing pack without reauthoring or rebuilding it.
public static class DocksideAuditEntry
{
    public static void RunPreview()
    {
        string output = Environment.GetEnvironmentVariable("DOCKSIDE_OUTPUT"); Directory.CreateDirectory(output);
        var bundle = AssetBundle.LoadFromFile(Environment.GetEnvironmentVariable("DOCKSIDE_AUDIT_BUNDLE"));
        try { SteamBogiePreview.Render(bundle.LoadAllAssets<CustomCarPack>().Single().Cars[0].liveries[0].prefab, output, "dockside", DocksidePlan.DriverRadius, "Small driver "); EditorApplication.Exit(0); }
        catch (Exception ex) { File.WriteAllText(Path.Combine(output, "preview-error.txt"), ex.ToString()); Debug.LogException(ex); EditorApplication.Exit(1); }
        finally { bundle.Unload(true); }
    }
    public static void RunPipeFit()
    {
        string output = Environment.GetEnvironmentVariable("DOCKSIDE_OUTPUT"); Directory.CreateDirectory(output);
        var bundle = AssetBundle.LoadFromFile(Environment.GetEnvironmentVariable("DOCKSIDE_AUDIT_BUNDLE"));
        try { SteamPipeAudit.Validate(bundle.LoadAllAssets<CustomCarPack>().Single().Cars[0].liveries[0].prefab, output); EditorApplication.Exit(0); }
        catch (Exception ex) { File.WriteAllText(Path.Combine(output, "pipe-fit-error.txt"), ex.ToString()); Debug.LogException(ex); EditorApplication.Exit(1); }
        finally { bundle.Unload(true); }
    }
    public static void RunDetailNegative()
    {
        string output = Environment.GetEnvironmentVariable("DOCKSIDE_OUTPUT");
        Directory.CreateDirectory(output);
        var bundle = AssetBundle.LoadFromFile(Environment.GetEnvironmentVariable("DOCKSIDE_AUDIT_BUNDLE"));
        try
        {
            try { DocksideDetailAudit.Validate(bundle.LoadAllAssets<CustomCarPack>().Single().Cars[0].liveries[0].prefab, output); }
            catch (InvalidOperationException ex)
            {
                if (!ex.Message.StartsWith("Detailed Franklin geometry missing", StringComparison.Ordinal)) throw;
                File.WriteAllText(Path.Combine(output, "negative-control.json"), "{\"passed\":true,\"expectedFailure\":true,\"oldBundleRejected\":true,\"reason\":\"Detailed Franklin geometry missing\",\"productionRebuilt\":false}");
                EditorApplication.Exit(0); return;
            }
            throw new InvalidOperationException("Old pack incorrectly passed the detailed-mechanism gate.");
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(output, "negative-error.txt"), ex.ToString()); Debug.LogException(ex); EditorApplication.Exit(1); }
        finally { bundle.Unload(true); }
    }
    public static void Run()
    {
        string output = Environment.GetEnvironmentVariable("DOCKSIDE_OUTPUT");
        try
        {
            Directory.CreateDirectory(output);
            DocksideBundleAudit.Validate(Environment.GetEnvironmentVariable("DOCKSIDE_AUDIT_BUNDLE"), Path.Combine(output, "bundle-validation.json"));
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(output, "audit-error.txt"), ex.ToString());
            Debug.LogException(ex); EditorApplication.Exit(1);
        }
    }
}
