using System;
using System.IO;
using CrystalSprint;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CrystalSprintEditor
{
    public static class CabinInteractionReview
    {
        private static int stage;
        private static float deadline;
        private static CurtainInteractable curtain;
        public static void Capture()
        {
            EditorSceneManager.OpenScene(CrystalSprintProjectSetup.ScenePath);
            SessionState.SetBool("CrystalSprint.CabinInteractionReview", true);
            EditorApplication.update += Update; EditorApplication.EnterPlaymode();
        }
        [InitializeOnLoadMethod]
        private static void Resume()
        { if (SessionState.GetBool("CrystalSprint.CabinInteractionReview", false)) EditorApplication.update += Update; }
        private static void Update()
        {
            if (!EditorApplication.isPlaying || Time.timeSinceLevelLoad < 2 || Time.realtimeSinceStartup < deadline) return;
            try
            {
                var player = Object.FindAnyObjectByType<PlayerController>(); var cabin = Object.FindAnyObjectByType<PondCabin>();
                var look = Object.FindAnyObjectByType<FirstPersonCamera>(); var door = cabin.GetComponentInChildren<HingedDoorInteractable>();
                void Aim(Vector3 point)
                {
                    Vector3 direction = (point - look.EyePosition).normalized;
                    look.SetViewAngles(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, -Mathf.Asin(direction.y) * Mathf.Rad2Deg);
                }
                if (stage == 0)
                {
                    Object.FindAnyObjectByType<CursorLockController>().LockCursor(); player.SetTestInput(Vector2.zero, false);
                    player.Warp(cabin.transform.TransformPoint(new Vector3(.966f, .254f, 5.4f)) + Vector3.up * 1.1f);
                    Aim(door.GetComponent<Renderer>().bounds.center); deadline = Time.realtimeSinceStartup + .6f;
                }
                else if (stage == 1) { Save("01-door-closed"); door.Interact(null); deadline = Time.realtimeSinceStartup + .35f; }
                else if (stage == 2) { Save("02-door-opening"); deadline = Time.realtimeSinceStartup + .65f; }
                else if (stage == 3)
                {
                    Save("03-door-open-95-degrees");
                    curtain = cabin.transform.Find("Curtain").GetComponent<CurtainInteractable>();
                    player.Warp(cabin.transform.TransformPoint(new Vector3(-.95f, .254f, 0)) + Vector3.up * 1.1f);
                    Aim(curtain.transform.position); deadline = Time.realtimeSinceStartup + .6f;
                }
                else if (stage == 4) { Save("04-curtains-closed"); curtain.Interact(null); deadline = Time.realtimeSinceStartup + .4f; }
                else if (stage == 5) { Save("05-curtains-opening"); deadline = Time.realtimeSinceStartup + .65f; }
                else if (stage == 6) { Save("06-curtains-open"); curtain.Interact(null); deadline = Time.realtimeSinceStartup + 1f; }
                else { Save("07-curtains-closed-again"); Finish(0); return; }
                stage++;
            }
            catch (Exception e) { Debug.LogException(e); Finish(1); }
        }
        private static void Save(string name)
        {
            Texture2D image = FirstPersonReview.RenderStack(Camera.main);
            Directory.CreateDirectory("Logs/CabinInteractions"); File.WriteAllBytes("Logs/CabinInteractions/" + name + ".png", image.EncodeToPNG()); Object.DestroyImmediate(image);
        }
        private static void Finish(int code)
        {
            SessionState.EraseBool("CrystalSprint.CabinInteractionReview"); EditorApplication.update -= Update;
            Debug.Log("Cabin interaction rendered review exit: " + code); EditorApplication.Exit(code);
        }
    }
}
