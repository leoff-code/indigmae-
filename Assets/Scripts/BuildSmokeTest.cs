using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrystalSprint
{
    public sealed class BuildSmokeTest : MonoBehaviour
    {
        private static int restartCount;

        private void Start()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-crystalSprintSmokeTest") >= 0)
            {
                StartCoroutine(Run());
            }
        }

        private IEnumerator Run()
        {
            yield return null;

            if (restartCount > 0)
            {
                bool restartPassed = GameManager.Instance != null;
                Finish(restartPassed, "restart reset");
                yield break;
            }

            PlayerController player = FindAnyObjectByType<PlayerController>();
            ThirdPersonCamera gameCamera = FindAnyObjectByType<ThirdPersonCamera>();
            GameObject boundary = GameObject.Find("Mountain Boundary");
            GameObject trees = GameObject.Find("Trees");
            if (player == null || gameCamera == null || GameManager.Instance == null || boundary == null || trees == null)
            {
                Finish(false, "required scene objects");
                yield break;
            }

            Vector3 start = player.transform.position;
            player.SetTestInput(Vector2.up, false);
            yield return new WaitForSeconds(0.35f);
            player.SetTestInput(Vector2.zero, true);
            float beforeJump = player.transform.position.y;
            yield return new WaitForSeconds(0.2f);
            bool moved = Vector3.Distance(start, player.transform.position) > 0.3f;
            bool jumped = player.transform.position.y > beforeJump + 0.1f;

            if (!moved || !jumped)
            {
                Finish(false, $"movement={moved}, jump={jumped}");
                yield break;
            }

            Debug.Log("CRYSTAL_SPRINT_SMOKE: movement, camera, jump, terrain and trees passed.");
            restartCount++;
            GameManager.Instance.RestartGame();
        }

        private static void Finish(bool passed, string detail)
        {
            if (passed)
            {
                Debug.Log($"CRYSTAL_SPRINT_SMOKE: PASS ({detail}).");
                Application.Quit(0);
            }
            else
            {
                Debug.LogError($"CRYSTAL_SPRINT_SMOKE: FAIL ({detail}).");
                Application.Quit(2);
            }
        }
    }
}
