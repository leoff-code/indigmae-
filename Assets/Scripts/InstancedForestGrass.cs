using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CrystalSprint
{
    [ExecuteAlways]
    public sealed class InstancedForestGrass : MonoBehaviour
    {
        [SerializeField] private Mesh[] meshes;
        [SerializeField] private Material[] materials;
        [SerializeField] private Mesh groundMesh;
        [SerializeField] private Vector4[] clearances;
        [SerializeField] private Bounds[] localClearings;
        [SerializeField, Min(.3f)] private float spacing = .64f;
        [SerializeField, Min(10f)] private float drawDistance = 65f;
        private readonly List<Cell> cells = new();
        private readonly Plane[] planes = new Plane[6];
        private Vector3[] terrainVertices;
        private int terrainSide;
        public int InstanceCount { get; private set; }
        public int LastDrawnInstances { get; private set; }
        public int LastDrawCalls { get; private set; }
        public Mesh[] SourceMeshes => meshes;

        public void SetLocalClearings(Bounds[] regions) { localClearings = regions; Rebuild(); }

        public bool IsInLocalClearing(Vector3 position)
        {
            if (localClearings == null) return false;
            foreach (Bounds area in localClearings)
                if (area.Contains(new Vector3(position.x, area.center.y, position.z))) return true;
            return false;
        }

        private sealed class Cell
        {
            public Bounds bounds;
            public Matrix4x4[][] variants;
        }

        public void Configure(Mesh[] geometry, Material[] surfaces, Mesh terrain, Vector4[] exclusions)
        {
            meshes = geometry;
            materials = surfaces;
            groundMesh = terrain;
            clearances = exclusions;
            Rebuild();
        }

        private void OnEnable()
        {
            Rebuild();
            RenderPipelineManager.beginCameraRendering += RenderGrass;
        }

        private void OnDisable() => RenderPipelineManager.beginCameraRendering -= RenderGrass;

        public float SampleGround(float x, float z)
        {
            if (terrainVertices == null || terrainVertices.Length == 0) return ForestWorld.Height(x, z);
            float fx = Mathf.Clamp((x + ForestWorld.Size * .5f) / 1.5f, 0f, terrainSide - 1.001f);
            float fz = Mathf.Clamp((z + ForestWorld.Size * .5f) / 1.5f, 0f, terrainSide - 1.001f);
            int ix = (int)fx, iz = (int)fz;
            float u = fx - ix, v = fz - iz;
            float a = terrainVertices[iz * terrainSide + ix].y;
            float b = terrainVertices[iz * terrainSide + ix + 1].y;
            float c = terrainVertices[(iz + 1) * terrainSide + ix].y;
            float d = terrainVertices[(iz + 1) * terrainSide + ix + 1].y;
            // Same diagonal as the terrain triangles, not a bilinear approximation above their surface.
            return u + v <= 1f ? a + (b - a) * u + (c - a) * v : d + (c - d) * (1f - u) + (b - d) * (1f - v);
        }

        public void Rebuild()
        {
            cells.Clear();
            InstanceCount = 0;
            if (meshes == null || materials == null || meshes.Length == 0 || groundMesh == null) return;
            terrainVertices = groundMesh.vertices;
            terrainSide = Mathf.RoundToInt(Mathf.Sqrt(terrainVertices.Length));
            System.Random random = new(9022026);
            const float cellSize = 8f;
            int across = Mathf.CeilToInt(ForestWorld.Radius * 2f / cellSize);
            for (int cz = 0; cz < across; cz++)
            for (int cx = 0; cx < across; cx++)
            {
                float x0 = -ForestWorld.Radius + cx * cellSize;
                float z0 = -ForestWorld.Radius + cz * cellSize;
                List<Matrix4x4>[] lists = new List<Matrix4x4>[meshes.Length];
                for (int variant = 0; variant < lists.Length; variant++) lists[variant] = new();
                for (float z = z0; z < z0 + cellSize; z += spacing)
                for (float x = x0; x < x0 + cellSize; x += spacing)
                {
                    float px = x + ((float)random.NextDouble() - .5f) * spacing;
                    float pz = z + ((float)random.NextDouble() - .5f) * spacing;
                    Vector2 p = new(px, pz);
                    if (p.magnitude > ForestWorld.Radius - .6f) continue;
                    float y = SampleGround(px, pz);
                    if (p.magnitude < 10.2f && y < -.22f) continue;
                    bool blocked = false;
                    if (clearances != null)
                        foreach (Vector4 obstacle in clearances)
                            if ((p - new Vector2(obstacle.x, obstacle.y)).sqrMagnitude < obstacle.z * obstacle.z) { blocked = true; break; }
                    if (blocked) continue;
                    float path = ForestWorld.PathDistance(p);
                    if (path < .62f && random.NextDouble() < .94) continue;
                    int variant = random.NextDouble() < .08 ? 2 : random.Next(2);
                    variant = Mathf.Min(variant, meshes.Length - 1);
                    float scale = .72f + (float)random.NextDouble() * .43f;
                    float height = (.62f + (float)random.NextDouble() * .34f) * Mathf.Lerp(.33f, 1f, Mathf.InverseLerp(.6f, 1.7f, path));
                    Vector3 position = new(px, y - .015f, pz);
                    lists[variant].Add(Matrix4x4.TRS(position, Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f), new Vector3(scale, height, scale)));
                    InstanceCount++;
                }
                Cell cell = new() { bounds = new Bounds(new Vector3(x0 + 4f, .5f, z0 + 4f), new Vector3(10f, 4f, 10f)), variants = new Matrix4x4[meshes.Length][] };
                for (int variant = 0; variant < meshes.Length; variant++)
                {
                    Matrix4x4[] array = lists[variant].ToArray();
                    for (int index = array.Length - 1; index > 0; index--)
                    {
                        int swap = random.Next(index + 1);
                        (array[index], array[swap]) = (array[swap], array[index]);
                    }
                    cell.variants[variant] = array;
                }
                cells.Add(cell);
            }
            // Filter only after the seeded generation/shuffle: every grass instance outside
            // the new cabin clearings retains exactly its original transform and variation.
            if (localClearings != null && localClearings.Length > 0)
                foreach (Cell cell in cells)
                    for (int variant = 0; variant < cell.variants.Length; variant++)
                    {
                        var kept = new List<Matrix4x4>();
                        foreach (Matrix4x4 matrix in cell.variants[variant])
                            if (!IsInLocalClearing(matrix.GetColumn(3))) kept.Add(matrix);
                        InstanceCount -= cell.variants[variant].Length - kept.Count;
                        cell.variants[variant] = kept.ToArray();
                    }
        }

        private void RenderGrass(ScriptableRenderContext context, Camera camera)
        {
            // The first-person overlay renders arms only; avoid submitting world grass again.
            if ((camera.cullingMask & (1 << gameObject.layer)) == 0) return;
            if (camera.cameraType == CameraType.Preview || !SystemInfo.supportsInstancing) return;
            if (cells.Count == 0 || meshes == null) return;
            GeometryUtility.CalculateFrustumPlanes(camera, planes);
            LastDrawnInstances = LastDrawCalls = 0;
            foreach (Cell cell in cells)
            {
                float distance = Vector3.Distance(camera.transform.position, cell.bounds.center);
                if (distance > drawDistance + 6f || !GeometryUtility.TestPlanesAABB(planes, cell.bounds)) continue;
                for (int variant = 0; variant < meshes.Length; variant++)
                {
                    Matrix4x4[] instances = cell.variants[variant];
                    int count = instances.Length;
                    // Random ordering within each cell makes thinning look organic, not like deleted rows.
                    count = Mathf.CeilToInt(count * Mathf.Lerp(1f, .4f, Mathf.InverseLerp(30f, drawDistance, distance)));
                    if (count == 0) continue;
                    RenderParams parameters = new(materials[variant])
                    {
                        camera = camera, worldBounds = cell.bounds,
                        shadowCastingMode = ShadowCastingMode.Off, receiveShadows = true,
                        lightProbeUsage = LightProbeUsage.Off, layer = gameObject.layer
                    };
                    Graphics.RenderMeshInstanced(parameters, meshes[variant], 0, instances, count);
                    LastDrawnInstances += count;
                    LastDrawCalls++;
                }
            }
        }
    }
}
