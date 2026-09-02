using System;
using System.IO;
using CrystalSprint;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CrystalSprintEditor
{
    public static class FirstPersonReview
    {
        private const string Folder = "Logs/FirstPersonReview";
        private static int stage;
        private static float deadline;
        public static void Capture()
        {
            EditorSceneManager.OpenScene(CrystalSprintProjectSetup.ScenePath);
            SessionState.SetBool("CrystalSprint.FirstPersonReview", true);
            EditorApplication.update += Update;
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (SessionState.GetBool("CrystalSprint.FirstPersonReview", false)) EditorApplication.update += Update;
        }

        private static void Update()
        {
            if (!EditorApplication.isPlaying || Time.timeSinceLevelLoad < 2f || Time.realtimeSinceStartup < deadline) return;
            try
            {
                PlayerController player = Object.FindAnyObjectByType<PlayerController>();
                FirstPersonCamera look = Object.FindAnyObjectByType<FirstPersonCamera>();
                FirstPersonViewmodel arms = Object.FindAnyObjectByType<FirstPersonViewmodel>();
                Camera main = Camera.main;
                if (stage == 0)
                {
                    player.Warp(new Vector3(0, 1.2f, -20)); player.SetTestInput(Vector2.zero, false);
                    look.SetViewAngles(20f, 4f);
                    deadline = Time.realtimeSinceStartup + .8f;
                }
                else if (stage == 1)
                {
                    Time.timeScale = 0f;
                    Save(main, "01-idle");
                    foreach (float phase in new[] { .30f, .49f, .64f, .85f })
                    {
                        arms.EvaluatePose(phase);
                        Save(main, "chop-" + phase.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                    }
                    arms.EvaluatePose(0);
                    look.SetViewAngles(20, 75); Save(main, "02-look-down");
                    look.SetViewAngles(20, -80); Save(main, "03-look-up");
                    look.SetViewAngles(20, 4);
                    Camera outside = new GameObject("Temporary Body Review Camera").AddComponent<Camera>();
                    outside.cullingMask = main.cullingMask;
                    outside.fieldOfView = 48;
                    Vector3 target = player.transform.position;
                    outside.transform.SetPositionAndRotation(target + new Vector3(3.2f, 1f, 4.2f), Quaternion.LookRotation(new Vector3(-3.2f, -.9f, -4.2f)));
                    Save(outside, "04-world-character-preserved");
                    Object.Destroy(outside.gameObject);
                    Time.timeScale = 1;
                    player.SetTestInput(Vector2.up, false);
                    deadline = Time.realtimeSinceStartup + .55f;
                }
                else if (stage == 2)
                {
                    Save(main, "05-walking");
                    player.SetTestInput(Vector2.up, false, true);
                    deadline = Time.realtimeSinceStartup + .5f;
                }
                else if (stage == 3)
                {
                    Save(main, "06-sprinting");
                    player.SetTestInput(Vector2.zero, true);
                    deadline = Time.realtimeSinceStartup + .18f;
                }
                else if (stage == 4)
                {
                    Save(main, "07-jumping");
                    player.SetTestInput(Vector2.zero, false);
                    deadline = Time.realtimeSinceStartup + 1.2f;
                }
                else if (stage == 5)
                {
                    // Temporary play-mode obstacle: deliberately close to the eye/axe.
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = "Temporary Viewmodel Clipping Test";
                    wall.transform.SetPositionAndRotation(main.transform.position + main.transform.forward * .7f, Quaternion.Euler(0, look.Yaw, 0));
                    wall.transform.localScale = new Vector3(2, 3, .1f);
                    deadline = Time.realtimeSinceStartup + .5f;
                }
                else if (stage == 6)
                {
                    Save(main, "08-close-obstacle");
                    Finish(0); return;
                }
                stage++;
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(1); }
        }

        public static Texture2D RenderStack(Camera camera, int width = 1600, int height = 1000)
        {
            RenderTexture output = new(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            RenderTexture previous = RenderTexture.active;
            float previousAspect = camera.aspect;
            camera.aspect = (float)width / height;
            try
            {
                // StandardRequest includes the URP overlay stack; SingleCameraRequest does not.
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = output });
                RenderTexture.active = output;
                Texture2D result = new(width, height, TextureFormat.RGB24, false);
                result.ReadPixels(new Rect(0, 0, width, height), 0, 0); result.Apply();
                return result;
            }
            finally { camera.aspect = previousAspect; RenderTexture.active = previous; Object.DestroyImmediate(output); }
        }

        private static void Save(Camera camera, string name)
        {
            Texture2D texture = RenderStack(camera);
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(Folder + "/" + name + ".png", texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        private static void Finish(int code)
        {
            Time.timeScale = 1;
            SessionState.EraseBool("CrystalSprint.FirstPersonReview");
            EditorApplication.update -= Update;
            Debug.Log("First-person rendered review exit: " + code);
            EditorApplication.Exit(code);
        }
    }
}
