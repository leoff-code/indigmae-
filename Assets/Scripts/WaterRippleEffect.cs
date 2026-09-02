using UnityEngine;

namespace CrystalSprint
{
    public sealed class WaterRippleEffect : MonoBehaviour
    {
        [SerializeField] private float lifetime = 1.35f;
        [SerializeField] private float endScale = 3.2f;

        private Renderer cachedRenderer;
        private MaterialPropertyBlock properties;
        private Color initialColor = new(0.72f, 0.94f, 1f, 0.82f);
        private float age;
        private PondSurfaceMotion waterSurface;

        public void FollowSurface(PondSurfaceMotion surface) => waterSurface = surface;

        public void Configure(float duration, float scale)
        {
            lifetime = duration;
            endScale = scale;
        }

        private void Awake()
        {
            cachedRenderer = GetComponent<Renderer>();
            properties = new MaterialPropertyBlock();
            if (cachedRenderer != null && cachedRenderer.sharedMaterial != null)
            {
                Material material = cachedRenderer.sharedMaterial;
                if (material.HasProperty("_BaseColor")) initialColor = material.GetColor("_BaseColor");
                else if (material.HasProperty("_Color")) initialColor = material.GetColor("_Color");
            }
        }

        private void Update()
        {
            if (waterSurface != null)
            {
                Vector3 position = transform.position;
                position.y = waterSurface.SampleHeight(position) + 0.02f;
                transform.position = position;
            }
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / lifetime);
            float scale = Mathf.Lerp(0.18f, endScale, Mathf.SmoothStep(0f, 1f, t));
            transform.localScale = new Vector3(scale, 1f, scale);
            if (cachedRenderer != null)
            {
                cachedRenderer.GetPropertyBlock(properties);
                Color fading = initialColor;
                fading.a *= 1f - t;
                properties.SetColor("_Color", fading);
                properties.SetColor("_BaseColor", fading);
                cachedRenderer.SetPropertyBlock(properties);
            }

            if (age >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
