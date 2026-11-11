using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using CCL.Types;
using CCL.Types.Components;
using CCL.Types.Proxies.Wheels;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class DocksideDraftBuilder
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    public static void CreateDraft()
    {
        string output = Environment.GetEnvironmentVariable("DOCKSIDE_OUTPUT");
        if (string.IsNullOrEmpty(output)) throw new InvalidOperationException("DOCKSIDE_OUTPUT is required.");
        Directory.CreateDirectory(output);
        try
        {
            string folder = "Assets/_CCL_CARS/" + DocksidePlan.CarName;
            if (!AssetDatabase.IsValidFolder(folder))
            {
                var wizard = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CCL.Creator.Wizards.CarWizard")).First(t => t != null);
                var car = Activator.CreateInstance(wizard.GetNestedType("CarSettings", Flags));
                var packSettings = Activator.CreateInstance(wizard.GetNestedType("PackSettings", Flags));
                Set(car, "Kind", 2); Set(car, "ID", DocksidePlan.CarId); Set(car, "Name", DocksidePlan.CarName);
                Set(car, "BaseCarType", BaseTrainCarType.LocoS060); Set(car, "Role", 0); Set(packSettings, "Author", "james-taplin");
                wizard.GetMethod("CreateNewCar", Flags).Invoke(null, new[] { car, packSettings }); AssetDatabase.Refresh();
            }
            var type = AssetDatabase.LoadAssetAtPath<CustomCarType>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:CustomCarType", new[] { folder }).Single()));
            var variant = AssetDatabase.LoadAssetAtPath<CustomCarVariant>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:CustomCarVariant", new[] { folder }).Single()));
            type.wheelRadius = DocksidePlan.DriverRadius; type.useDefaultWheelRotation = false; type.IsSteamLocomotive = true;
            variant.FrontBogie = variant.RearBogie = BogieType.Custom;
            string prefab = AssetDatabase.GetAssetPath(variant.prefab);
            var root = PrefabUtility.LoadPrefabContents(prefab);
            try
            {
                var materialPath = folder + "/DocksidePlaceholder.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("Standard")) { color = new Color(.12f, .17f, .14f) };
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                var model = root.transform.Find("Model"); Clear(model);
                Alias(model, "S060 body (native alias)", MeyerPlan.BodyMesh, material, Vector3.one);
                var front = Bogie(root.transform.Find("BogieF"), DocksidePlan.BogieCentre, material);
                var rear = Bogie(root.transform.Find("BogieR"), -DocksidePlan.BogieCentre, material);
                Franklin(front, folder, material); Franklin(rear, folder, material);
                foreach (var unit in new[] { front, rear })
                {
                    // Keep the original animated shaft hierarchy, replace only
                    // the source's cuboid cylinder housing with round castings.
                    foreach (string side in new[] { "L", "R" })
                        SteamBogieVisuals.HideFranklinHousing(unit.Find("Franklin fit " + side + "/cylinder"));
                    DocksideFranklinFit.Apply(unit, folder);
                    SteamBogieVisuals.Frame(unit, DocksidePlan.AxlePitch, DocksidePlan.DriverRadius, folder);
                    SteamBogieVisuals.Cylinders(unit, .22f, DocksidePlan.DriverRadius, DocksidePlan.CylinderZ, folder, DocksidePlan.Bore);
                }
                DocksideSteamAuthoring.Apply(root, front, rear);
                foreach (var effects in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "[Meyer engine effects]"))
                    foreach (Transform part in effects.GetComponentsInChildren<Transform>(true))
                    {
                        float x = Mathf.Sign(part.localPosition.x) * DocksidePlan.PistonAxisX;
                        if (part.name.StartsWith("Cock front ")) part.localPosition = new Vector3(x, .30f, DocksidePlan.CylinderZ + .20f);
                        if (part.name.StartsWith("Cock rear ")) part.localPosition = new Vector3(x, .30f, DocksidePlan.CylinderZ - .20f);
                        if (part.name.StartsWith("Water drip ") || part.name.StartsWith("Cock audio ")) part.localPosition = new Vector3(x, .29f, DocksidePlan.CylinderZ);
                    }
                var manager = root.GetComponent<PoweredWheelsManagerProxy>() ?? root.AddComponent<PoweredWheelsManagerProxy>();
                manager.GetWheelsFromDefaultBogies = false; manager.poweredWheels = root.GetComponentsInChildren<PoweredWheelProxy>(true);
                DocksideMotionAuthoring.Apply(root, front, rear, folder, material);
                foreach (var unit in new[] { front, rear }) SteamBogieVisuals.PositionEffects(unit, DocksidePlan.DriverRadius, DocksidePlan.CylinderZ, .22f);
                foreach (var unit in new[] { front, rear }) SteamEffectsAuthoring.Apply(unit);
                SteamPipeAuthoring.Apply(front, rear, folder, DocksidePlan.DriverRadius, DocksidePlan.CylinderZ, .22f, .22f);
                MeyerMechanicalAuthoring.Apply(root);
                foreach (var collider in root.transform.Find("[colliders]/[bogies]").GetComponentsInChildren<CapsuleCollider>())
                {
                    collider.center = new Vector3(0, DocksidePlan.DriverRadius, Mathf.Sign(collider.center.z) * DocksidePlan.BogieCentre);
                    collider.radius = DocksidePlan.DriverRadius; collider.height = DocksidePlan.AxlePitch + 2 * DocksidePlan.DriverRadius;
                }
                DocksideDetailAudit.Validate(root, output);
                DocksideSpacingAudit.Validate(root, output, false);
                DocksideBoundaryMeasure.Measure(root, output, true);
                PrefabUtility.SaveAsPrefabAsset(root, prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            EditorUtility.SetDirty(type); EditorUtility.SetDirty(variant); AssetDatabase.SaveAssets();
            foreach (var engine in variant.prefab.GetComponentsInChildren<CCL.Types.Proxies.Simulation.Steam.ReciprocatingSteamEngineDefinitionProxy>(true))
                File.WriteAllText(Path.Combine(output, engine.ID + "-calibration.json"), EditorJsonUtility.ToJson(engine, true));
            var pack = AssetDatabase.LoadAssetAtPath<CustomCarPack>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:CustomCarPack", new[] { folder }).Single()));
            pack.Version = "0.8.1"; pack.ExporterVersion = ExporterConstants.ExporterVersion.ToString();
            type.ForceValidation(); EditorUtility.SetDirty(type); EditorUtility.SetDirty(pack); AssetDatabase.SaveAssets();
            string destination = Path.Combine(output, DocksidePlan.CarId); Directory.CreateDirectory(destination);
            var helper = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CCL.Creator.Utility.AssetBundleHelper")).First(t => t != null);
            helper.GetMethod("CreateBundle", Flags).Invoke(null, new object[] { destination, new List<(Object, string)> { (pack, null) } });
            DocksideBundleAudit.Validate(Path.Combine(destination, "ccl_bundle"), Path.Combine(output, "bundle-validation.json"));
            File.WriteAllText(Path.Combine(destination, "Info.json"), "{\"Id\":\"" + DocksidePlan.CarId + "\",\"DisplayName\":\"Franklin Dockside 0-4-4-0T prototype\",\"Version\":\"0.8.1\",\"Author\":\"james-taplin\",\"ManagerVersion\":\"0.27.3\",\"Requirements\":[\"DVCustomCarLoader\",\"RearrangedS060-0.16.0\"]}\n");
            File.WriteAllText(Path.Combine(output, "authoring-result.json"), "{\"bundleBuilt\":true,\"carId\":\"" + DocksidePlan.CarId + "\",\"arrangement\":\"0-4-4-0T articulated duplex\",\"simpleExpansion\":true,\"driverDiameter\":1.0,\"axlePitch\":1.2,\"bogieCentre\":1.75,\"wheelWidthScaled\":false,\"rearExtension\":false,\"rodAnimationImplemented\":true,\"installed\":false,\"runtimeValidated\":false}\n");
            EditorApplication.Exit(0);
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(output, "authoring-error.txt"), ex.ToString()); Debug.LogException(ex); EditorApplication.Exit(1); }
    }

    private static Transform Bogie(Transform anchor, float z, Material material)
    {
        anchor.localPosition = new Vector3(0, 0, z);
        var frame = anchor.Find("bogie_car"); frame.localPosition = Vector3.zero;
        var axles = frame.Cast<Transform>().Where(t => t.name.StartsWith("[axle]", StringComparison.Ordinal)).ToArray();
        if (axles.Length != 2) throw new InvalidOperationException("Two axles required per unit.");
        foreach (var child in frame.Cast<Transform>().Where(t => !axles.Contains(t) && t.name != "ContactPoints").ToArray()) Object.DestroyImmediate(child.gameObject);
        for (int i = 0; i < 2; i++)
        {
            // CCL's template gives both axles the same name. Animation paths
            // must distinguish them while preserving the native [axle] prefix.
            axles[i].name = i == 0 ? "[axle] rear" : "[axle] front";
            Clear(axles[i]); axles[i].localPosition = new Vector3(0, DocksidePlan.DriverRadius, (i == 0 ? -1 : 1) * DocksidePlan.AxlePitch / 2);
            axles[i].localRotation = Quaternion.identity;
            float radial = DocksidePlan.DriverRadius / DocksidePlan.NativeDriverRadius;
            Alias(axles[i], "Small driver R (native alias)", (i == 0 ? "s060_Wheels_01" : MeyerPlan.DriverMesh), material, new Vector3(1, radial, radial)).localRotation = Quaternion.Euler(-90, 0, 0);
            Alias(axles[i], "Small driver L (native alias)", (i == 0 ? "s060_Wheels_01" : MeyerPlan.DriverMesh), material, new Vector3(-1, radial, radial));
            var powered = axles[i].GetComponent<PoweredWheelProxy>() ?? axles[i].gameObject.AddComponent<PoweredWheelProxy>();
            powered.wheelTransform = axles[i].Find("Small driver R (native alias)"); powered.localRotationAxis = Vector3.right;
        }
        foreach (Transform contact in frame.Find("ContactPoints"))
        { var p = contact.localPosition; p.z = contact.name.StartsWith("F", StringComparison.Ordinal) ? DocksidePlan.AxlePitch / 2 : -DocksidePlan.AxlePitch / 2; contact.localPosition = p; }
        return frame;
    }
    private static Transform Alias(Transform parent, string name, string meshName, Material placeholder, Vector3 scale)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localScale = scale;
        var filter = go.AddComponent<MeshFilter>(); var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = placeholder;
        var mesh = go.AddComponent<MeshGrabberFilter>(); mesh.Filter = filter; mesh.ReplacementName = meshName;
        MaterialAlias(renderer); return go.transform;
    }
    private static void MaterialAlias(MeshRenderer renderer)
    {
        var alias = renderer.gameObject.AddComponent<MaterialGrabberRenderer>(); alias.RenderersToAffect = new Renderer[] { renderer };
        alias.Replacements = new[] { new MaterialGrabberRenderer.IndexToName { RendererIndex = 0, ReplacementName = MeyerPlan.BodyMaterial } };
    }
    private static void Franklin(Transform bogie, string folder, Material material)
    {
        string path = Environment.GetEnvironmentVariable("DOCKSIDE_FRANKLIN_BUNDLE");
        var bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null) throw new InvalidOperationException("Existing Franklin source bundle unavailable.");
        try
        {
            var source = bundle.LoadAsset<GameObject>("franklin_type_b");
            if (source == null || source.transform.Find("cylinder") == null) throw new InvalidOperationException("Franklin source cylinder missing.");
            for (int side = -1; side <= 1; side += 2)
            {
                var holder = new GameObject(side < 0 ? "Franklin fit L" : "Franklin fit R").transform;
                holder.SetParent(bogie, false); holder.localScale = new Vector3(side * .70f, .70f, DocksidePlan.FranklinZScale);
                // Fit only the author-owned Franklin gear radially in X/Y.
                // Native wheel width/gauge stay untouched. The cylinder and gearbox datums align with the piston
                // axis and rear driver; the original shaft/joint curves survive.
                holder.localPosition = new Vector3(side * (DocksidePlan.PistonAxisX - source.transform.Find("cylinder").localPosition.x * .70f),
                    .50f - source.transform.Find("cylinder").localPosition.y * .70f,
                    DocksidePlan.CylinderZ - source.transform.Find("cylinder").localPosition.z * DocksidePlan.FranklinZScale);
                int index = 0; CopyGeometry(source.transform, holder, folder, material, ref index);
            }
        }
        finally { bundle.Unload(true); }
    }
    private static void CopyGeometry(Transform source, Transform target, string folder, Material material, ref int index)
    {
        foreach (Transform child in source)
        {
            var copy = new GameObject(child.name).transform; copy.SetParent(target, false);
            copy.localPosition = child.localPosition; copy.localRotation = child.localRotation; copy.localScale = child.localScale;
            var filter = child.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                // Only the fork's own custom Franklin meshes are copied. No
                // animations, scripts, native textures or materials are exported.
                if (filter.sharedMesh.name != "default") throw new InvalidOperationException("Unexpected Franklin source mesh: " + filter.sharedMesh.name);
                string meshPath = folder + "/FranklinCustomMesh" + index++ + ".asset";
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (mesh == null) { mesh = Object.Instantiate(filter.sharedMesh); mesh.name = "Dockside custom Franklin " + (index - 1); AssetDatabase.CreateAsset(mesh, meshPath); }
                copy.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = copy.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; MaterialAlias(renderer);
            }
            CopyGeometry(child, copy, folder, material, ref index);
        }
    }
    private static void Clear(Transform parent) { if (parent == null) throw new InvalidOperationException("Template transform missing."); foreach (var child in parent.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject); }
    private static void Set(object owner, string name, object value) { var field = owner.GetType().GetField(name, Flags); field.SetValue(owner, field.FieldType.IsEnum && value is int ? Enum.ToObject(field.FieldType, value) : value); }
}

