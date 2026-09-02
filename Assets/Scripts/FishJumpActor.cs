using UnityEngine;

namespace CrystalSprint
{
    public sealed class FishJumpActor : MonoBehaviour
    {
        private Vector3 start;
        private Vector3 end;
        private float duration;
        private float height;
        private float elapsed;
        private GameObject ripplePrefab;
        private GameObject splashPrefab;
        private PondSurfaceMotion waterSurface;

        public void Begin(Vector3 jumpStart, Vector3 jumpEnd, float jumpDuration, float jumpHeight, GameObject ripple, GameObject splash, PondSurfaceMotion surface = null)
        {
            start = jumpStart;
            end = jumpEnd;
            duration = jumpDuration;
            height = jumpHeight;
            ripplePrefab = ripple;
            splashPrefab = splash;
            waterSurface = surface;
            transform.position = start;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (waterSurface != null) end.y = waterSurface.SampleHeight(end);
            Vector3 horizontal = Vector3.Lerp(start, end, t);
            float arc = 4f * height * t * (1f - t);
            transform.position = horizontal + Vector3.up * arc;

            Vector3 velocity = (end - start) / duration + Vector3.up * (4f * height * (1f - 2f * t) / duration);
            if (velocity.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            }

            if (t >= 1f)
            {
                SpawnWaterEffects(end);
                Destroy(gameObject);
            }
        }

        private void SpawnWaterEffects(Vector3 position)
        {
            if (ripplePrefab != null)
            {
                GameObject ripple = Instantiate(ripplePrefab, position + Vector3.up * 0.02f, Quaternion.identity);
                ripple.GetComponent<WaterRippleEffect>()?.FollowSurface(waterSurface);
            }

            if (splashPrefab != null)
            {
                Instantiate(splashPrefab, position + Vector3.up * 0.025f, Quaternion.identity);
            }
        }
    }
}
