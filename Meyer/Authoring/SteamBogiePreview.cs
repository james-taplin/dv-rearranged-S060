using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using RearrangedS060;
using CCL.Types.Proxies.Ports;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

public static class SteamBogiePreview
{
    public static void Render(GameObject model, string output, string name, float wheelRadius, string wheelPrefix)
    {
        var clone = Object.Instantiate(model);
        // Audits also hold a live instance at the origin. Put this render far
        // from it as well as in a preview scene, so only this instance is seen.
        var offset = new Vector3(10000, 0, 0); clone.transform.position += offset;
        var wheel = new Material(Shader.Find("Standard")) { color = new Color(.25f, .27f, .28f) };
        var ground = new Material(Shader.Find("Standard")) { color = new Color(.48f, .49f, .48f) };
        Material guide = null;
        var owned = new List<Object>(); S060SteamPipes pipes = null;
        var cameraObject = new GameObject("Bogie preview camera"); var lightObject = new GameObject("Bogie preview light");
        var scene = EditorSceneManager.NewPreviewScene();
        SceneManager.MoveGameObjectToScene(clone, scene);
        SceneManager.MoveGameObjectToScene(cameraObject, scene);
        SceneManager.MoveGameObjectToScene(lightObject, scene);
        var previousAmbient = RenderSettings.ambientLight;
        try
        {
            bool nativePreview = SteamNativePreview.Apply(clone, owned);
            var updateOilCups = SteamOilCupAudit.Apply(clone, output, name, owned);
            pipes = S060SteamPipes.Bind(clone.transform, clone.transform.Find("BogieF/bogie_car"), clone.transform.Find("BogieR/bogie_car"), clone.transform);
            int hiddenPlates = clone.GetComponentsInChildren<Renderer>(true).Count(r => r.name == "DummyPlate" && !r.enabled);
            File.WriteAllText(Path.Combine(output, "preview-source.json"), "{\"nativeLocalPreview\":" + (nativePreview ? "true" : "false") + ",\"hiddenSdkPlatePlaceholders\":" + hiddenPlates + ",\"nativeIdentityPlatesChanged\":false,\"nativeAssetsPackaged\":false,\"liveGameExecuted\":false}");
            foreach (var mesh in clone.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith(wheelPrefix, StringComparison.Ordinal) && t.GetComponent<MeshFilter>().sharedMesh == null).ToArray())
            {
                var disc = SteamBogieVisuals.Part(mesh, "Preview wheel disc (native alias unresolved)", PrimitiveType.Cylinder,
                    new Vector3(.82f, 0, 0), new Vector3(wheelRadius * 2 / mesh.localScale.y, .045f, wheelRadius * 2 / mesh.localScale.z), wheel);
                disc.localRotation = Quaternion.Euler(0, 0, 90);
            }
            bool extended = name == "meyer";
            SteamBogieVisuals.Part(clone.transform, "Preview ground", PrimitiveType.Cube, new Vector3(0, -.1f, extended ? -1.8f : 0), new Vector3(5, .10f, extended ? 17 : 11), ground);
            foreach (float x in new[] { -.7175f, .7175f }) SteamBogieVisuals.Part(clone.transform, "Preview rail", PrimitiveType.Cube, new Vector3(x, -.025f, extended ? -1.8f : 0), new Vector3(.07f, .05f, extended ? 16 : 10), wheel);
            if (extended && !nativePreview)
            {
                guide = new Material(Shader.Find("Sprites/Default")) { color = new Color(.3f, .5f, .7f, .65f) };
                // Measured body envelope guide only; native body alias remains
                // unresolved. No game mesh/texture is extracted or packaged.
                foreach (float y in new[] { .42f, 3.82f })
                    Line(clone.transform, guide, new[] { new Vector3(-1.3f,y,-4.2f), new Vector3(1.3f,y,-4.2f), new Vector3(1.3f,y,4f), new Vector3(-1.3f,y,4f), new Vector3(-1.3f,y,-4.2f) });
                foreach (float x in new[] { -1.3f, 1.3f }) foreach (float z in new[] { -4.2f, 4f })
                    Line(clone.transform, guide, new[] { new Vector3(x,.42f,z), new Vector3(x,3.82f,z) });
                // Keep this tiny preview-only material alive until scene close.
            }
            foreach (var t in clone.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
            File.WriteAllLines(Path.Combine(output, name + "-preview-renderers.txt"), clone.GetComponentsInChildren<Renderer>(true).Select(r =>
                r.name + " | " + UnityEditor.AnimationUtility.CalculateTransformPath(r.transform, clone.transform) + " | " + r.GetType().Name + " | " + r.enabled + " | " +
                MeshName(r) + " | " + r.bounds.size));
            var focus = new Vector3(0, nativePreview ? 1.75f : 1.0f, extended ? -2f : 0);
            var camera = cameraObject.AddComponent<Camera>(); camera.scene = scene; camera.cullingMask = 1 << 31; camera.orthographic = true; camera.orthographicSize = extended ? 3.9f : 2.6f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.87f, .90f, .93f);
            var fillObject = new GameObject("Bogie preview fill"); SceneManager.MoveGameObjectToScene(fillObject, scene);
            var fill = fillObject.AddComponent<Light>(); fill.type = LightType.Directional; fill.intensity = .7f; fill.transform.rotation = Quaternion.Euler(20,145,0);
            var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.3f; light.transform.rotation = Quaternion.Euler(45, -25, 0);
            RenderSettings.ambientLight = new Color(.57f, .57f, .57f);
            for (int frame = 0; frame < (name == "dockside" ? 7 : extended ? 5 : 3); frame++)
            {
                foreach (var reader in clone.GetComponentsInChildren<AnimatorPortReaderProxy>(true))
                {
                    bool front = reader.portId.StartsWith(MeyerSteamAuthoring.Lp);
                    float yaw = frame == 2 ? 12 : frame == 3 ? 15 : frame == 4 ? -15 : 0;
                    reader.transform.localRotation = Quaternion.Euler(0, front ? yaw : -yaw, 0);
                    reader.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single().SampleAnimation(reader.gameObject, frame * .125f + (front ? .125f : 0));
                }
                updateOilCups();
                pipes.UpdateGeometry();
                camera.transform.position = offset + focus + (frame >= 3 ? new Vector3(-8, 2.1f, 4) : frame == 0 ? new Vector3(8, 1.25f, .1f) : new Vector3(7, 2.4f, 5));
                if (!extended && name == "dockside" && frame >= 3)
                {
                    var close = new Vector3(0, 1.15f, DocksidePlan.BogieCentre + .35f);
                    camera.orthographicSize = 1.25f;
                    camera.transform.position = offset + close + new Vector3(frame == 3 ? 6 : -6, .65f, .75f);
                    camera.transform.LookAt(offset + close);
                    light.transform.rotation = Quaternion.Euler(35, frame == 3 ? -40 : 40, 0);
                }
                else camera.transform.LookAt(offset + focus);
                if (!extended && name == "dockside" && frame >= 5)
                {
                    var neck = new Vector3(frame == 5 ? .705f : -.705f, 2.07f, 3.17f); camera.orthographicSize = .30f;
                    camera.transform.position = offset + neck + new Vector3(frame == 5 ? 4 : -4, .85f, 4);
                    camera.transform.LookAt(offset + neck);
                    light.transform.rotation = Quaternion.Euler(35, frame == 5 ? -40 : 40, 0);
                }
                Capture(camera, Path.Combine(output, name + "-running-gear-" + frame + ".png"), 1600, 900);
            }
            if (name == "dockside")
            {
                var close = new Vector3(0, 1.15f, DocksidePlan.BogieCentre + .35f); camera.orthographicSize = 1.25f;
                camera.transform.position = offset + close + new Vector3(6, .65f, .75f); camera.transform.LookAt(offset + close);
                light.transform.rotation = Quaternion.Euler(35, -40, 0);
                for (int frame = 0; frame < 16; frame++)
                {
                    foreach (var reader in clone.GetComponentsInChildren<AnimatorPortReaderProxy>(true))
                    {
                        reader.transform.localRotation = Quaternion.identity;
                        bool front = reader.portId.StartsWith(MeyerSteamAuthoring.Lp);
                        reader.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single().SampleAnimation(reader.gameObject, frame / 16f + (front ? .125f : 0));
                    }
                    updateOilCups(); pipes.UpdateGeometry();
                    Capture(camera, Path.Combine(output, "dockside-cycle-" + frame.ToString("D2") + ".png"), 1000, 600);
                }
            }
        }
        finally { if (pipes != null) pipes.Dispose(); RenderSettings.ambientLight = previousAmbient; EditorSceneManager.ClosePreviewScene(scene); Object.DestroyImmediate(wheel); Object.DestroyImmediate(ground); if (guide != null) Object.DestroyImmediate(guide); foreach (var item in owned) if (item != null) Object.DestroyImmediate(item); }
    }
    private static void Capture(Camera camera, string path, int width, int height)
    {
        var target = new RenderTexture(width, height, 24); camera.targetTexture = target; camera.Render();
        var old = RenderTexture.active; RenderTexture.active = target;
        var pixels = new Texture2D(width, height, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
        File.WriteAllBytes(path, pixels.EncodeToPNG());
        RenderTexture.active = old; camera.targetTexture = null; Object.DestroyImmediate(pixels); Object.DestroyImmediate(target);
    }
    private static string MeshName(Renderer renderer)
    { var filter = renderer.GetComponent<MeshFilter>(); return filter != null && filter.sharedMesh != null ? filter.sharedMesh.name : "no mesh"; }
    private static void Line(Transform parent, Material material, Vector3[] points)
    {
        var go = new GameObject("Preview native body envelope guide"); go.transform.SetParent(parent, false);
        var line = go.AddComponent<LineRenderer>(); line.useWorldSpace = false; line.positionCount = points.Length;
        line.SetPositions(points); line.startWidth = line.endWidth = .015f; line.sharedMaterial = material;
    }
}
