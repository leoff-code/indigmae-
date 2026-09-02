using System;
using System.IO;
using System.Reflection;
using CrystalSprint;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CrystalSprintEditor
{
    public static class CabinWaterReview
    {
        private static int stage;
        private static float deadline;
        private static Camera review;
        private static Vector3 landing;
        private const string Folder = CabinWaterIntegration.ReportFolder;
        public static void Capture()
        {
            EditorSceneManager.OpenScene(CrystalSprintProjectSetup.ScenePath);
            SessionState.SetBool("CrystalSprint.CabinWaterReview", true);
            EditorApplication.update += Update; EditorApplication.EnterPlaymode();
        }
        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (SessionState.GetBool("CrystalSprint.CabinWaterReview", false)) EditorApplication.update += Update;
        }
        private static void Update()
        {
            if (!EditorApplication.isPlaying || Time.timeSinceLevelLoad < 2 || Time.realtimeSinceStartup < deadline) return;
            try
            {
                PlayerController player = Object.FindAnyObjectByType<PlayerController>();
                PondCabin cabin = Object.FindAnyObjectByType<PondCabin>();
                FirstPersonCamera look = Object.FindAnyObjectByType<FirstPersonCamera>();
                FishJumpSystem fish = Object.FindAnyObjectByType<FishJumpSystem>();
                if (stage == 0)
                {
                    review = new GameObject("Temporary Cabin Review Camera").AddComponent<Camera>();
                    review.enabled = false; review.cullingMask = Camera.main.cullingMask; review.fieldOfView = 56;
                    var data = review.GetUniversalAdditionalCameraData(); data.renderPostProcessing = true; data.requiresDepthOption = CameraOverrideOption.On;
                    player.Warp(cabin.Porch + Vector3.up * 1.1f); player.SetTestInput(Vector2.zero, false); look.SetViewAngles(180, 0);
                    deadline = Time.realtimeSinceStartup + .8f;
                }
                else if (stage == 1)
                {
                    View(new Vector3(0, 8, -18), new Vector3(10, 1.6f, 2), "01-cabin-and-pond");
                    View(new Vector3(11, 4, -13), new Vector3(18, 2.6f, 2), "02-cabin-entrance-scale");
                    View(new Vector3(28, 4.8f, 11), new Vector3(18, 2.7f, 4), "03-cabin-foundation-rear");
                    Renderer water = Object.FindAnyObjectByType<PondSurfaceMotion>().GetComponentInChildren<Renderer>();
                    Material final = water.sharedMaterial;
                    water.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/InnerverseInteractive/Ultimate Nature – Starter/Environment/Water/Materials/UNS_Water.mat");
                    View(new Vector3(2, 3.6f, -12), new Vector3(0, -.29f, 0), "04-water-before");
                    water.sharedMaterial = final;
                    View(review.transform.position, new Vector3(0, -.29f, 0), "05-water-new-shader");
                    player.Warp(cabin.Interior + Vector3.up * 1.1f); look.SetViewAngles(180, 3);
                    deadline = Time.realtimeSinceStartup + .7f;
                }
                else if (stage == 2)
                {
                    Save(Camera.main, "06-first-person-inside-looking-out");
                    look.SetViewAngles(0, 10); Save(Camera.main, "07-first-person-interior-floor");
                    player.Warp(cabin.Approach + Vector3.up * 1.1f); look.SetViewAngles(0, -4);
                    deadline = Time.realtimeSinceStartup + .6f;
                }
                else if (stage == 3)
                {
                    Save(Camera.main, "08-first-person-approach");
                    fish.TriggerJumpNow();
                    FishJumpActor actor = fish.ActiveFish.GetComponent<FishJumpActor>();
                    landing = (Vector3)typeof(FishJumpActor).GetField("end", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(actor);
                    Vector3 start = fish.ActiveFish.transform.position;
                    View(start + new Vector3(3, 2.2f, -3), start + Vector3.up * .35f, "09-fish-takeoff");
                    deadline = Time.realtimeSinceStartup + .6f;
                }
                else if (stage == 4)
                {
                    Vector3 fishPosition = fish.ActiveFish != null ? fish.ActiveFish.transform.position : landing;
                    View(fishPosition + new Vector3(3, 1.8f, -4), fishPosition, "10-fish-airborne");
                    deadline = Time.realtimeSinceStartup + .75f;
                }
                else if (stage == 5)
                {
                    View(landing + new Vector3(3, 2.5f, -3), landing, "11-fish-landing-ripple");
                    player.Warp(new Vector3(0, .4f, -3)); player.SetTestInput(Vector2.zero, false); look.SetViewAngles(0, 55);
                    deadline = Time.realtimeSinceStartup + .22f;
                }
                else if (stage == 6)
                {
                    Save(Camera.main, "12-player-water-entry");
                    player.SetTestInput(Vector2.up, false); look.SetViewAngles(90, 50);
                    deadline = Time.realtimeSinceStartup + .6f;
                }
                else
                {
                    Save(Camera.main, "13-player-wading"); DenseForestUpgrade.Validate(); Finish(0); return;
                }
                stage++;
            }
            catch (Exception e) { Debug.LogException(e); Finish(1); }
        }
        private static void View(Vector3 position, Vector3 target, string name)
        { review.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position)); Save(review, name); }
        private static void Save(Camera camera, string name)
        {
            Texture2D texture = FirstPersonReview.RenderStack(camera);
            Directory.CreateDirectory(Folder); File.WriteAllBytes(Folder + "/" + name + ".png", texture.EncodeToPNG()); Object.DestroyImmediate(texture);
        }
        private static void Finish(int code)
        {
            SessionState.EraseBool("CrystalSprint.CabinWaterReview"); EditorApplication.update -= Update;
            Debug.Log("Cabin/water review finished: " + code); EditorApplication.Exit(code);
        }
    }
}
