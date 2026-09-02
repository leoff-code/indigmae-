using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CrystalSprint;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CrystalSprintEditor
{
    public static class DenseForestReview
    {
        private const string Folder = "Logs/DenseForestReview";
        private static int stage;
        private static float deadline;
        private static readonly List<float> times = new();
        private static readonly FrameTiming[] timing = new FrameTiming[1];
        private static readonly List<double> gpuTimes = new();
        private static RenderTexture benchmarkTarget;
        private static Texture2D fence;
        public static void Capture()
        {
            EditorSceneManager.OpenScene(CrystalSprintProjectSetup.ScenePath);
            SessionState.SetBool("CrystalSprint.DenseForestReview", true);
            EditorApplication.update += Update;
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (SessionState.GetBool("CrystalSprint.DenseForestReview", false)) EditorApplication.update += Update;
        }

        private static void Update()
        {
            if (!EditorApplication.isPlaying || Time.timeSinceLevelLoad < 3f || Time.realtimeSinceStartup < deadline) return;
            try
            {
                Camera camera = Camera.main;
                PlayerController player = Object.FindAnyObjectByType<PlayerController>();
                camera.GetComponent<ThirdPersonCamera>().enabled = false;
                if (camera.TryGetComponent(out FirstPersonCamera firstPerson)) firstPerson.enabled = false;
                if (stage == 0)
                {
                    QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
                    player.Warp(new Vector3(0f, 1.2f, -20f)); player.SetTestInput(Vector2.zero, false);
                    player.transform.rotation = Quaternion.identity;
                    deadline = Time.realtimeSinceStartup + 1f;
                }
                else if (stage == 1)
                {
                    CaptureView(camera, new Vector3(-57f, 76f, -76f), Vector3.zero, "01-map-overview");
                    CaptureView(camera, new Vector3(0f, 2.8f, -28f), new Vector3(5f, 3.5f, -1f), "02-forest-ground");
                    CaptureView(camera, new Vector3(16f, 2.5f, 35f), new Vector3(28f, 3f, 44f), "03-new-outer-forest");
                    CaptureView(camera, new Vector3(10f, 5f, -12f), new Vector3(0f, .2f, 0f), "04-pond");
                    Material finalSky = RenderSettings.skybox;
                    RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ForestKit/ForestExtendedDaySky.mat");
                    CaptureView(camera, new Vector3(0f, 4f, 0f), new Vector3(35f, 28f, -30f), "05-extended-sky");
                    RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ForestSkybox.mat");
                    CaptureView(camera, camera.transform.position, camera.transform.position + camera.transform.forward * 30f, "06-previous-sky");
                    RenderSettings.skybox = finalSky;
                    deadline = Time.realtimeSinceStartup + .5f;
                }
                else if (stage == 2)
                {
                    Vector3 p = player.transform.position;
                    CaptureView(camera, p + new Vector3(2.9f, 1.2f, 4.5f), p + Vector3.up * .15f, "07-axe-front");
                    CaptureView(camera, p + new Vector3(4.8f, .5f, .1f), p + Vector3.up * .15f, "08-axe-side");
                    CaptureView(camera, p + new Vector3(2.8f, 1.1f, -4.5f), p + Vector3.up * .15f, "09-axe-rear");
                    VerifyRenderedGrassInteraction(camera, player);
                    LumberjackVisual visual = player.GetComponent<LumberjackVisual>();
                    Time.timeScale = 0f;
                    visual.PlayAttack();
                    float[] phases = { .15f, .30f, .40f, .49f, .64f, .82f, 1f };
                    foreach (float phase in phases)
                    {
                        typeof(LumberjackVisual).GetField("attackTime", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(visual, phase * LumberjackVisual.AttackDuration);
                        typeof(LumberjackVisual).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(visual, null);
                        CaptureView(camera, p + new Vector3(3.2f, 1.1f, 4.2f), p + Vector3.up * .2f, "chop-" + phase.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                    }
                    Time.timeScale = 1f;
                    CaptureView(camera, new Vector3(20f, 2.2f, 37f), new Vector3(0f, 2.5f, 10f), "10-performance-view");
                    benchmarkTarget = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                    fence = new Texture2D(1, 1, TextureFormat.RGB24, false);
                    deadline = Time.realtimeSinceStartup + 4f;
                }
                else
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    RenderPipeline.SubmitRenderRequest(camera, new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = benchmarkTarget });
                    RenderTexture old = RenderTexture.active;
                    RenderTexture.active = benchmarkTarget;
                    fence.ReadPixels(new Rect(800f, 500f, 1f, 1f), 0, 0); fence.Apply();
                    RenderTexture.active = old;
                    watch.Stop();
                    times.Add((float)watch.Elapsed.TotalMilliseconds);
                    FrameTimingManager.CaptureFrameTimings();
                    if (FrameTimingManager.GetLatestTimings(1, timing) > 0 && timing[0].gpuFrameTime > 0) gpuTimes.Add(timing[0].gpuFrameTime);
                    if (times.Count < 240) return;
                    times.Sort(); gpuTimes.Sort();
                    InstancedForestGrass grass = Object.FindAnyObjectByType<InstancedForestGrass>();
                    Directory.CreateDirectory(Folder);
                    File.WriteAllText(Folder + "/performance.txt", $"GPU: {SystemInfo.graphicsDeviceName}\nResolution: 1600 x 1000, MSAA 4\nGrass total: {grass.InstanceCount}\nGrass visible: {grass.LastDrawnInstances}\nGrass draw calls: {grass.LastDrawCalls}\n240-frame synchronized render median: {times[120]:F2} ms\nP95: {times[228]:F2} ms\nIncludes CPU submission and blocking GPU readback; not standalone-game FPS.\nGPU timing: {(gpuTimes.Count > 0 ? gpuTimes[gpuTimes.Count / 2] : 0):F2} ms (zero means unavailable)\n");
                    DenseForestUpgrade.Validate();
                    Finish(0);
                    return;
                }
                stage++;
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(1); }
        }

        private static void CaptureView(Camera camera, Vector3 position, Vector3 target, string name)
        {
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
            camera.fieldOfView = 53f;
            Texture2D frame = ForestKitProbe.Render(camera, 1600, 1000);
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(Folder + "/" + name + ".png", frame.EncodeToPNG());
            Object.DestroyImmediate(frame);
        }

        private static void VerifyRenderedGrassInteraction(Camera camera, PlayerController player)
        {
            float previousScale = Time.timeScale;
            Vector4 interactor = Shader.GetGlobalVector("_GrassInteractor");
            Renderer[] actor = player.GetComponentsInChildren<Renderer>();
            bool[] enabled = actor.Select(r => r.enabled).ToArray();
            Time.timeScale = 0f;
            for (int i = 0; i < actor.Length; i++) actor[i].enabled = false;
            Vector3 target = new(interactor.x, interactor.y, interactor.z);
            camera.transform.SetPositionAndRotation(target + new Vector3(1.5f, 2.8f, 2.5f), Quaternion.LookRotation(new Vector3(-1.5f, -2.8f, -2.5f)));
            Texture2D bent = ForestKitProbe.Render(camera, 800, 600);
            Shader.SetGlobalVector("_GrassInteractor", Vector4.zero);
            Texture2D resting = ForestKitProbe.Render(camera, 800, 600);
            Color32[] a = bent.GetPixels32(), b = resting.GetPixels32();
            int changed = 0;
            for (int i = 0; i < a.Length; i++)
                if (Math.Abs(a[i].r - b[i].r) + Math.Abs(a[i].g - b[i].g) + Math.Abs(a[i].b - b[i].b) > 25) changed++;
            File.WriteAllBytes(Folder + "/grass-bending-active.png", bent.EncodeToPNG());
            File.WriteAllBytes(Folder + "/grass-bending-released.png", resting.EncodeToPNG());
            File.WriteAllText(Folder + "/grass-gpu-check.txt", $"Grounded interaction radius: {interactor.w:F2}\nChanged pixels with interaction disabled: {changed}\n");
            Object.DestroyImmediate(bent); Object.DestroyImmediate(resting);
            Shader.SetGlobalVector("_GrassInteractor", interactor);
            for (int i = 0; i < actor.Length; i++) actor[i].enabled = enabled[i];
            Time.timeScale = previousScale;
            if (interactor.w <= 0f || changed < 1000) throw new InvalidOperationException("GPU grass bending is not visible.");
        }

        private static void Finish(int code)
        {
            Time.timeScale = 1f;
            SessionState.SetBool("CrystalSprint.DenseForestReview", false);
            EditorApplication.update -= Update;
            EditorApplication.Exit(code);
        }
    }
}
