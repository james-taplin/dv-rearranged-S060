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

// Authoring-only recipe. Builds a CCL prototype; never installs it.
public static class MeyerDraftBuilder
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

    [MenuItem("Rearranged S060/Meyer/Create mechanical draft")]
    public static void CreateDraft()
    {
        string output = Environment.GetEnvironmentVariable("MEYER_OUTPUT");
        if (string.IsNullOrEmpty(output)) output = Path.GetFullPath("MeyerEvidence");
        Directory.CreateDirectory(output);
        try
        {
            string folder = "Assets/_CCL_CARS/" + MeyerPlan.CarName;
            if (AssetDatabase.IsValidFolder(folder))
            {
                if (Environment.GetEnvironmentVariable("MEYER_UPDATE_DRAFT") != "1")
                    throw new InvalidOperationException("Meyer draft exists; use the explicit update switch to edit it.");
            }
            else
            {
                var wizard = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CCL.Creator.Wizards.CarWizard")).First(t => t != null);
                var car = Activator.CreateInstance(wizard.GetNestedType("CarSettings", Flags));
                var pack = Activator.CreateInstance(wizard.GetNestedType("PackSettings", Flags));
                Set(car, "Kind", 2);  // DVTrainCarKind.Loco, verified against CCL 3.1.9
                Set(car, "ID", MeyerPlan.CarId);
                Set(car, "Name", MeyerPlan.CarName);
                Set(car, "BaseCarType", BaseTrainCarType.LocoS060);
                Set(car, "Role", 0);
                Set(pack, "Author", "james-taplin");
                wizard.GetMethod("CreateNewCar", Flags).Invoke(null, new[] { car, pack });
                AssetDatabase.Refresh();
            }

            var types = AssetDatabase.FindAssets("t:CustomCarType", new[] { folder });
            var variants = AssetDatabase.FindAssets("t:CustomCarVariant", new[] { folder });
            if (types.Length != 1 || variants.Length != 1) throw new InvalidOperationException("Expected one canonical type and one variant from CCL.");
            var type = AssetDatabase.LoadAssetAtPath<CustomCarType>(AssetDatabase.GUIDToAssetPath(types[0]));
            var variant = AssetDatabase.LoadAssetAtPath<CustomCarVariant>(AssetDatabase.GUIDToAssetPath(variants[0]));
            type.wheelRadius = (float)MeyerPlan.DriverRadius;
            type.useDefaultWheelRotation = false;
            type.IsSteamLocomotive = true;
            variant.FrontBogie = BogieType.Custom;
            variant.RearBogie = BogieType.Custom;

            string prefabPath = AssetDatabase.GetAssetPath(variant.prefab);
            if (string.IsNullOrEmpty(prefabPath)) prefabPath = folder + "/" + MeyerPlan.CarId + "_template.prefab";
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var model = root.transform.Find("Model");
                if (model == null) throw new InvalidOperationException("CCL model root missing.");
                foreach (Transform child in model.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
                string materialPath = folder + "/MeyerPlaceholder.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("Standard"));
                    material.name = "Meyer editor placeholder";
                    material.color = new Color(.16f, .24f, .15f);
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                Alias(model, "S060 body (native alias)", MeyerPlan.BodyMesh, material, false);

                // The actual CCL 3.1.9 template has its visual/custom bogie
                // anchors directly under the car. [colliders]/[bogies] is a
                // distinct collision hierarchy, not the visual bogie root.
                var front = ConfigureBogie(root.transform.Find("BogieF"), MeyerPlan.FrontCentre, material);
                var rear = ConfigureBogie(root.transform.Find("BogieR"), MeyerPlan.RearCentre, material);
                AddPannier(rear, -1f, material);
                AddPannier(rear, +1f, material);
                MeyerSteamAuthoring.Apply(root, front, rear);
                ConfigurePoweredWheels(root);
                foreach (var unit in new[] { rear, front })
                {
                    float cylinderRadius = unit == front ? .255f : .337f;
                    SteamBogieVisuals.Frame(unit, (float)MeyerPlan.AxlePitch, (float)MeyerPlan.DriverRadius, folder);
                    SteamBogieVisuals.Cylinders(unit, cylinderRadius, (float)MeyerPlan.DriverRadius, MeyerPlan.CylinderZ, folder, unit == front ? .594f : .42f);
                    SteamBogieVisuals.PositionEffects(unit, (float)MeyerPlan.DriverRadius, MeyerPlan.CylinderZ, cylinderRadius);
                }
                MeyerMotionAuthoring.Apply(root, front, rear, folder, SteamBogieVisuals.Metal(folder, "RodSteel", new Color(.32f, .34f, .35f), .8f, .5f));
                MeyerMechanicalAuthoring.Apply(root, true);
                RearPlatform(rear, folder);
                MeyerBogieDetail.Apply(rear, folder);
                SteamBogieVisuals.Group(front, "[Native S060 cylinders]");
                SteamEffectsAuthoring.Apply(front, nativeFront: true);
                SteamEffectsAuthoring.Apply(rear);
                SteamPipeAuthoring.Apply(front, rear, folder, (float)MeyerPlan.DriverRadius, MeyerPlan.CylinderZ, .255f, .337f, nativeFront: true);
                MeyerBogieDetail.AliasAll(front, folder); MeyerBogieDetail.AliasAll(rear, folder);
                // The radio copies the bogie-owned buffers. Suppress the SDK's
                // additional rear buffer visuals on the standalone prefab.
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    bool rigBuffer = renderer.transform.IsChildOf(root.transform.Find("[coupler_rig_rear]")) && renderer.name.StartsWith("Buffer_", StringComparison.Ordinal);
                    bool stem = renderer.transform.parent != null && renderer.transform.parent.name == "[BufferStems]" && root.transform.InverseTransformPoint(renderer.transform.position).z < 0;
                    if (rigBuffer || stem) renderer.enabled = false;
                }

                // This preview rotation demonstrates separate bogie ownership;
                // return the authored prefab to straight-ahead after checking.
                foreach (float yaw in new[] { -15f, 0f, 15f })
                {
                    front.localRotation = Quaternion.Euler(0f, yaw, 0f);
                    rear.localRotation = Quaternion.Euler(0f, -yaw, 0f);
                    if (rear.Find("[Meyer rear pannier L]").parent != rear || rear.Find("[Meyer rear pannier R]").parent != rear)
                        throw new InvalidOperationException("Panniers must travel with the rear bogie.");
                }
                front.localRotation = rear.localRotation = Quaternion.identity;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            EditorUtility.SetDirty(type);
            EditorUtility.SetDirty(variant);
            AssetDatabase.SaveAssets();
            foreach (var engine in variant.prefab.GetComponentsInChildren<CCL.Types.Proxies.Simulation.Steam.ReciprocatingSteamEngineDefinitionProxy>(true))
                File.WriteAllText(Path.Combine(output, engine.ID + "-calibration.json"), EditorJsonUtility.ToJson(engine, true));
            bool exported = Environment.GetEnvironmentVariable("MEYER_EXPORT") == "1";
            if (exported) ExportPrototype(folder, output, type);
            File.WriteAllText(Path.Combine(output, "authoring-result.json"),
                "{\"draftCreated\":true,\"carId\":\"" + MeyerPlan.CarId +
                "\",\"wheelArrangement\":\"0-4+4-0T\",\"plannedPoweredBogies\":2,\"axlesPerBogie\":2," +
                "\"rearPanniers\":2,\"addedResourceCapacityLitres\":0," +
                "\"waterCircuitImplemented\":false,\"compoundAdapterRequired\":true,\"rodAnimationImplemented\":true," +
                "\"cylinderCocksRequiredOnBothEngines\":true,\"cylinderCockBindingsAuthored\":true," +
                "\"bundleBuilt\":" + (exported ? "true" : "false") + ",\"installed\":false,\"runtimeValidated\":false}\n");
            Debug.Log("Meyer prototype authored: " + prefabPath + ". In-game validation and final fit remain pending.");
            if (Environment.GetEnvironmentVariable("MEYER_EXIT") == "1") EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(output, "authoring-error.txt"), ex.ToString());
            Debug.LogException(ex);
            if (Environment.GetEnvironmentVariable("MEYER_EXIT") == "1") EditorApplication.Exit(1);
            else throw;
        }
    }

    private static void ExportPrototype(string folder, string output, CustomCarType type)
    {
        var packs = AssetDatabase.FindAssets("t:CustomCarPack", new[] { folder });
        if (packs.Length != 1) throw new InvalidOperationException("Expected one CCL pack.");
        var pack = AssetDatabase.LoadAssetAtPath<CustomCarPack>(AssetDatabase.GUIDToAssetPath(packs[0]));
        pack.Version = "0.7.0"; pack.ExporterVersion = ExporterConstants.ExporterVersion.ToString();
        type.ForceValidation(); EditorUtility.SetDirty(type); EditorUtility.SetDirty(pack); AssetDatabase.SaveAssets();
        string destination = Path.Combine(output, "RS060_Meyer_0440"); Directory.CreateDirectory(destination);
        var helper = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CCL.Creator.Utility.AssetBundleHelper")).First(t => t != null);
        helper.GetMethod("CreateBundle", Flags).Invoke(null, new object[] { destination, new List<(Object, string)> { (pack, null) } });
        string bundle = Path.Combine(destination, "ccl_bundle");
        if (!File.Exists(bundle) || new FileInfo(bundle).Length == 0) throw new InvalidOperationException("CCL bundle build did not produce a bundle.");
        MeyerBundleAudit.Validate(bundle, Path.Combine(output, "bundle-validation.json"));
        MeyerLightsAudit.Validate(output);
        File.WriteAllText(Path.Combine(destination, "Info.json"), "{\"Id\":\"RS060_Meyer_0440\",\"DisplayName\":\"S060 Meyer with attached pannier steam bogie\",\"Version\":\"0.7.0\",\"Author\":\"james-taplin\",\"ManagerVersion\":\"0.27.3\",\"Requirements\":[\"DVCustomCarLoader\",\"RearrangedS060-0.16.0\"]}\n");
    }

    private static Transform ConfigureBogie(Transform bogie, float z, Material material)
    {
        if (bogie == null) throw new InvalidOperationException("CCL front/rear custom bogie anchor missing.");
        bogie.localPosition = new Vector3(0f, 0f, z);
        var frame = bogie.Find("bogie_car");
        if (frame == null) throw new InvalidOperationException("CCL bogie_car missing.");
        frame.localPosition = Vector3.zero;
        var axles = frame.Cast<Transform>().Where(t => t.name.StartsWith("[axle]", StringComparison.Ordinal)).ToArray();
        if (axles.Length != 2) throw new InvalidOperationException("Expected a two-axle CCL custom bogie.");
        foreach (Transform child in frame.Cast<Transform>().Where(t => !axles.Contains(t) && t.name != "ContactPoints").ToArray()) Object.DestroyImmediate(child.gameObject);
        for (int i = 0; i < axles.Length; i++)
        {
            axles[i].name = i == 0 ? "[axle] rear" : "[axle] front";
            foreach (Transform child in axles[i].Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            axles[i].localPosition = new Vector3(0f, (float)MeyerPlan.DriverRadius, (i == 0 ? -1f : 1f) * (float)MeyerPlan.AxlePitch / 2f);
            axles[i].localRotation = Quaternion.identity;
            // The main-rod axle uses the genuine longer native crankpin. The
            // other axle retains the short coupling-only pin, at native width.
            string wheelMesh = i == 0 ? "s060_Wheels_01" : MeyerPlan.DriverMesh;
            Alias(axles[i], "S060 driver R (native alias)", wheelMesh, material, false).localRotation = Quaternion.Euler(-90f, 0f, 0f);
            Alias(axles[i], "S060 driver L (native alias)", wheelMesh, material, true);
            var powered = axles[i].GetComponent<PoweredWheelProxy>() ?? axles[i].gameObject.AddComponent<PoweredWheelProxy>();
            powered.wheelTransform = axles[i].Find("S060 driver R (native alias)"); powered.localRotationAxis = Vector3.right;
        }
        foreach (Transform contact in frame.Find("ContactPoints"))
        {
            var position = contact.localPosition;
            position.z = contact.name.StartsWith("F", StringComparison.Ordinal) ? (float)MeyerPlan.AxlePitch / 2 : -(float)MeyerPlan.AxlePitch / 2;
            contact.localPosition = position;
        }
        return frame;
    }

    private static void ConfigurePoweredWheels(GameObject root)
    {
        var wheels = root.GetComponentsInChildren<PoweredWheelProxy>(true);
        if (wheels.Length != 4) throw new InvalidOperationException("Meyer requires four actual powered axles.");
        var manager = root.GetComponent<PoweredWheelsManagerProxy>() ?? root.AddComponent<PoweredWheelsManagerProxy>();
        manager.GetWheelsFromDefaultBogies = false; manager.poweredWheels = wheels;
        var rotation = root.GetComponent<PoweredWheelRotationViaCodeProxy>() ?? root.AddComponent<PoweredWheelRotationViaCodeProxy>();
        rotation.wheelRadius = (float)MeyerPlan.DriverRadius;
        rotation.additionalTransformsToRotate = new PoweredWheelRotationViaCodeProxy.TransformRotationConfig[0]; rotation.OnValidate();
    }

    private static Transform Alias(Transform parent, string name, string meshName, Material placeholder, bool mirror)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localScale = new Vector3(mirror ? -1f : 1f, 1f, 1f);
        var filter = obj.AddComponent<MeshFilter>();
        var renderer = obj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = placeholder;
        var mesh = obj.AddComponent<MeshGrabberFilter>();
        mesh.Filter = filter;
        mesh.ReplacementName = meshName;
        var material = obj.AddComponent<MaterialGrabberRenderer>();
        material.RenderersToAffect = new Renderer[] { renderer };
        material.Replacements = new[] { new MaterialGrabberRenderer.IndexToName { RendererIndex = 0, ReplacementName = MeyerPlan.BodyMaterial } };
        return obj.transform;
    }

    private static void AddPannier(Transform rear, float side, Material material)
    {
        var tank = SteamBogieVisuals.Group(rear, side < 0 ? "[Meyer rear pannier L]" : "[Meyer rear pannier R]");
        tank.localPosition = new Vector3(side * (float)MeyerPlan.TankCentreX, (float)MeyerPlan.TankCentreY, 0f);
        SteamBogieVisuals.Part(tank, "Tank shell", PrimitiveType.Cube, Vector3.zero, new Vector3(.30f, .55f, 1f), material);
        foreach (float end in new[] { -.5f, .5f }) SteamBogieVisuals.Part(tank, "Rounded tank end", PrimitiveType.Cylinder,
            new Vector3(0, 0, end), new Vector3(.30f, .275f, .30f), material);
        SteamBogieVisuals.Part(tank, "Pannier filler cap", PrimitiveType.Cylinder, new Vector3(0, .30f, -.25f), new Vector3(.14f, .025f, .14f), material);
    }

    private static void RearPlatform(Transform rear, string folder)
    {
        var metal = SteamBogieVisuals.Metal(folder, "BogieFrame", new Color(.075f, .085f, .09f), .45f, .24f);
        var platform = SteamBogieVisuals.Group(rear, "[Attached rear platform]");
        SteamBogieVisuals.Part(platform, "Pannier deck", PrimitiveType.Cube, new Vector3(0, 1.55f, MeyerPlan.DeckCentreZ), new Vector3(2.35f, .10f, MeyerPlan.DeckLength), metal);
        SteamBogieVisuals.Part(platform, "Rear buffer beam", PrimitiveType.Cube, new Vector3(0, 1.05f, MeyerPlan.RearBeamCentreZ), new Vector3(2.35f, .25f, MeyerPlan.RearBeamDepth), metal);
        foreach (int side in new[] { -1, 1 })
        {
            // Native rear buffer heads already follow the relocated coupling
            // rig. Do not add a second static pair on top of them.
            SteamBogieVisuals.Part(platform, "Buffer beam frame extension " + side, PrimitiveType.Cube,
                new Vector3(side * .65f, 1.13f, -1.58f), new Vector3(.16f, .13f, .58f), metal);
            SteamBogieVisuals.Part(platform, "Buffer beam deck bracket " + side, PrimitiveType.Cube,
                new Vector3(side * .65f, 1.35f, -1.55f), new Vector3(.12f, .40f, .12f), metal);
            SteamBogieVisuals.Part(platform, "Rear lamp pedestal " + side, PrimitiveType.Cube,
                new Vector3(side * 1.016f, 1.25f, -1.72f), new Vector3(.13f, .25f, .18f), metal);
            foreach (int end in new[] { -1, 1 })
                SteamBogieVisuals.Part(platform, "Deck pedestal " + side + " " + end, PrimitiveType.Cube, new Vector3(side * .60f, 1.34f, end * .60f), new Vector3(.12f, .44f, .12f), metal);
        }
        SteamBogieVisuals.Part(platform, "Articulation drawbar", PrimitiveType.Cube, new Vector3(0, .98f, 1.45f), new Vector3(.20f, .10f, 1.25f), metal);
    }

    private static void Set(object owner, string name, object value)
    {
        var field = owner.GetType().GetField(name, Flags);
        field.SetValue(owner, field.FieldType.IsEnum && value is int ? Enum.ToObject(field.FieldType, value) : value);
    }
}

