using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

// A separate asmdef avoids a name collision with the installed Assembly-CSharp.
public sealed class IndependentSlipPlayerBootstrap : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return null;
        string root = Directory.GetParent(Application.dataPath).Parent.FullName;
        string managed = File.ReadAllText(Path.Combine(root, "managed-path.txt")).Trim();
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
            string name = new AssemblyName(args.Name).Name + ".dll";
            foreach (string path in new[] { Path.Combine(managed, name), Path.Combine(managed, "UnityModManager", name) })
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            return null;
        };
        try
        {
            Assembly.LoadFrom(Path.Combine(managed, "UnityEngine.dll"));
            Assembly.LoadFrom(Path.Combine(managed, "Unity.Entities.Hybrid.dll"));
            Assembly.LoadFrom(Path.Combine(managed, "Assembly-CSharp.dll"));
            Assembly.LoadFrom(Path.Combine(managed, "Unity.Transforms.dll"));
            // Match UMM's actual load boundary: native registered components only.
            Assembly.LoadFrom(Path.Combine(managed, "Unity.Entities.dll")).GetType("Unity.Entities.TypeManager", true)
                .GetMethod("Initialize").Invoke(null, null);
            var probe = Assembly.LoadFrom(Path.Combine(root, "probe-bin", "NativeProbe.dll"));
            probe.GetType("S060SlipWorldProbe").GetMethod("Run").Invoke(null, new object[] { Path.Combine(root, "result.json") });
            Application.Quit(0);
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(root, "bootstrap-error.txt"), error.ToString());
            Debug.LogException(error); Application.Quit(1);
        }
    }
}
