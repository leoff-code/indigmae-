using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrystalSprint
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private GameInput input;
        private bool isRestarting;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            input = new GameInput();
        }

        private void OnEnable() => input?.Enable();

        private void OnDisable() => input?.Disable();

        private void OnDestroy()
        {
            input?.Dispose();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (input.RestartPressed)
            {
                RestartGame();
            }
        }

        public void RestartGame()
        {
            if (!isRestarting)
            {
                StartCoroutine(RestartAsync());
            }
        }

        private IEnumerator RestartAsync()
        {
            isRestarting = true;
            AsyncOperation load = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
            if (load != null)
            {
                yield return load;
            }
        }
    }
}
