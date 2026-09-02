using UnityEngine;

namespace CrystalSprint
{
    public sealed class FishJumpSystem : MonoBehaviour
    {
        [SerializeField] private GameObject[] fishPrefabs;
        [SerializeField] private GameObject ripplePrefab;
        [SerializeField] private GameObject splashPrefab;
        [SerializeField, Min(1f)] private float jumpInterval = 10f;
        [SerializeField, Min(1f)] private float pondRadius = 7.1f;
        [SerializeField] private float waterHeight;
        [SerializeField] private PondSurfaceMotion waterSurface;

        private float nextJump;

        public int FishVariantCount => fishPrefabs?.Length ?? 0;
        public float JumpInterval => jumpInterval;
        public int JumpCount { get; private set; }
        public GameObject ActiveFish { get; private set; }
        public GameObject[] FishPrefabs => fishPrefabs;

        public void ConfigureWater(PondSurfaceMotion surface) => waterSurface = surface;

        public void Configure(GameObject[] fish, GameObject ripple, GameObject splash, float surfaceHeight, float radius)
        {
            fishPrefabs = fish;
            ripplePrefab = ripple;
            splashPrefab = splash;
            waterHeight = surfaceHeight;
            pondRadius = radius;
        }

        private void Start() => nextJump = Time.time + jumpInterval;

        private void Update()
        {
            if (Time.time >= nextJump)
            {
                TriggerJumpNow();
                nextJump = Time.time + jumpInterval;
            }
        }

        public bool TriggerJumpNow()
        {
            if (fishPrefabs == null || fishPrefabs.Length == 0 || ActiveFish != null)
            {
                return false;
            }

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float travelAngle = angle + Random.Range(1.15f, 2.0f);
            float startRadius = Random.Range(1.4f, pondRadius * 0.72f);
            float travelDistance = Random.Range(3.2f, 5.3f);
            Vector3 start = new(Mathf.Cos(angle) * startRadius, waterHeight, Mathf.Sin(angle) * startRadius);
            Vector3 direction = new(Mathf.Cos(travelAngle), 0f, Mathf.Sin(travelAngle));
            Vector3 end = start + direction * travelDistance;
            Vector2 endPlanar = new(end.x, end.z);
            if (endPlanar.magnitude > pondRadius)
            {
                endPlanar = endPlanar.normalized * (pondRadius - 0.25f);
                end = new Vector3(endPlanar.x, waterHeight, endPlanar.y);
            }

            if (waterSurface != null)
            {
                // Keep both splashes inside the actual water/terrain intersection.
                for (int attempt = 0; attempt < 12 && !waterSurface.ContainsWater(start); attempt++) start *= 0.8f;
                for (int attempt = 0; attempt < 12 && !waterSurface.ContainsWater(end); attempt++) end *= 0.8f;
                start.y = waterSurface.SampleHeight(start);
                end.y = waterSurface.SampleHeight(end);
            }

            int variant = Random.Range(0, fishPrefabs.Length);
            ActiveFish = Instantiate(fishPrefabs[variant], start, Quaternion.LookRotation(direction));
            ActiveFish.name = $"Jumping Fish ({fishPrefabs[variant].name})";
            FishJumpActor actor = ActiveFish.AddComponent<FishJumpActor>();
            actor.Begin(start, end, Random.Range(1.15f, 1.42f), Random.Range(1.65f, 2.25f), ripplePrefab, splashPrefab, waterSurface);
            SpawnWaterEffects(start);
            JumpCount++;
            return true;
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
