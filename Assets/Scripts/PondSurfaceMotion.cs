using UnityEngine;

namespace CrystalSprint
{
    public sealed class PondSurfaceMotion : MonoBehaviour
    {
        [SerializeField] private float surfaceHeight;
        [SerializeField] private MeshCollider pondGround;
        [SerializeField] private float amplitude = 0.022f;
        private MeshFilter[] filters;
        private Mesh[] originals;
        private Mesh[] animated;
        private Vector3[][] restVertices;
        private Vector3[][] vertices;
        private float nextUpdate;

        public float SurfaceHeight => surfaceHeight;
        public float Amplitude => amplitude;

        public void Configure(float height, MeshCollider ground)
        {
            surfaceHeight = height;
            pondGround = ground;
        }

        public float SampleHeight(Vector3 position) => surfaceHeight + Wave(position.x, position.z);

        public bool ContainsWater(Vector3 position)
        {
            // The new sea also has low terrain. Keep the existing pond interaction strictly
            // on its original water mesh; no sea contact may spawn pond-height effects.
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Bounds area = renderer.bounds;
                if (position.x < area.min.x || position.x > area.max.x || position.z < area.min.z || position.z > area.max.z) return false;
            }
            return pondGround != null && pondGround.Raycast(new Ray(new Vector3(position.x, 10f, position.z), Vector3.down), out RaycastHit hit, 20f)
                && hit.point.y < surfaceHeight - 0.045f;
        }

        private float Wave(float x, float z) => amplitude *
            (Mathf.Sin(x * 1.25f + z * 0.65f + Time.time * 1.1f) * 0.6f +
             Mathf.Sin(z * 1.6f - x * 0.45f - Time.time * 0.83f) * 0.4f);

        private void Awake()
        {
            filters = GetComponentsInChildren<MeshFilter>();
            originals = new Mesh[filters.Length];
            animated = new Mesh[filters.Length];
            restVertices = new Vector3[filters.Length][];
            vertices = new Vector3[filters.Length][];
            for (int index = 0; index < filters.Length; index++)
            {
                originals[index] = filters[index].sharedMesh;
                animated[index] = Instantiate(originals[index]);
                animated[index].name = originals[index].name + " (animated instance)";
                animated[index].MarkDynamic();
                restVertices[index] = animated[index].vertices;
                vertices[index] = new Vector3[restVertices[index].Length];
                filters[index].sharedMesh = animated[index];
            }
        }

        private void LateUpdate()
        {
            if (Time.time < nextUpdate) return;
            nextUpdate = Time.time + 1f / 30f;
            for (int meshIndex = 0; meshIndex < filters.Length; meshIndex++)
            {
                Transform meshTransform = filters[meshIndex].transform;
                for (int index = 0; index < vertices[meshIndex].Length; index++)
                {
                    Vector3 world = meshTransform.TransformPoint(restVertices[meshIndex][index]);
                    world.y += Wave(world.x, world.z);
                    vertices[meshIndex][index] = meshTransform.InverseTransformPoint(world);
                }
                animated[meshIndex].vertices = vertices[meshIndex];
                animated[meshIndex].RecalculateNormals();
                animated[meshIndex].RecalculateBounds();
            }
        }

        private void OnDestroy()
        {
            if (animated == null) return;
            for (int index = 0; index < animated.Length; index++)
            {
                if (filters[index] != null) filters[index].sharedMesh = originals[index];
                if (animated[index] != null) Destroy(animated[index]);
            }
        }
    }
}
