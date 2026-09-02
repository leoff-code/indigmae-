using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CrystalSprint;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CrystalSprintEditor
{
    public static class EnvironmentPackIntegration
    {
        public const string PackRoot = "Assets/InnerverseInteractive/Ultimate Nature – Starter";
        private static Collider[] terrain;
        private static readonly System.Random random = new(602091);
        private static int captureStage;
        private static float captureAfter;

        // Render in Play Mode so URP, LOD selection, reflection probes and water motion are active.
        public static void CapturePreviews()
        {
            EditorSceneManager.OpenScene(CrystalSprintProjectSetup.ScenePath);
            SessionState.SetBool("CrystalSprint.EnvironmentCapture", true);
            EditorApplication.update += CaptureUpdate;
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void ResumeCapture()
        {
            if (SessionState.GetBool("CrystalSprint.EnvironmentCapture", false)) EditorApplication.update += CaptureUpdate;
        }

        private static void CaptureUpdate()
        {
            if (!EditorApplication.isPlaying || Time.timeSinceLevelLoad < 2f) return;
            try
            {
                Camera camera = Camera.main;
                camera.GetComponent<ThirdPersonCamera>().enabled = false;
                if (captureStage == 0)
                {
                    CaptureView(camera, new Vector3(-23f, 21f, -30f), new Vector3(0f, 0.5f, 0f), "environment-overview");
                    Transform stump = Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None).First(item => item.Kind == EnvironmentAssetKind.Stump).transform;
                    CaptureView(camera, stump.position + new Vector3(3.2f, 2.1f, 3.6f), stump.position + Vector3.up * 0.45f, "environment-stump");
                    Transform mushroom = Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None).First(item => item.Kind == EnvironmentAssetKind.Mushroom).transform;
                    CaptureView(camera, mushroom.position + new Vector3(1.7f, 1f, 1.7f), mushroom.position + Vector3.up * 0.14f, "environment-floor");
                    Transform log = Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None).First(item => item.Kind == EnvironmentAssetKind.Log).transform;
                    CaptureView(camera, log.position + new Vector3(4f, 2.5f, 4f), log.position + Vector3.up * 0.3f, "environment-log");
                    Transform player = Object.FindAnyObjectByType<PlayerController>().transform;
                    CaptureView(camera, player.position + player.forward * 5f + Vector3.up * 2f, player.position + Vector3.up * 1.3f, "environment-player");
                    Object.FindAnyObjectByType<FishJumpSystem>().TriggerJumpNow();
                    captureAfter = Time.time + 0.55f;
                    captureStage = 1;
                }
                else if (Time.time > captureAfter)
                {
                    CaptureView(camera, new Vector3(11f, 6f, -13f), Vector3.zero, "environment-water");
                    foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                        foreach (Material material in renderer.sharedMaterials)
                            if (material == null || ShaderUtil.ShaderHasError(material.shader)) throw new InvalidOperationException("Render shader error on " + renderer.name);
                    SessionState.SetBool("CrystalSprint.EnvironmentCapture", false);
                    EditorApplication.update -= CaptureUpdate;
                    Debug.Log("Play Mode environment previews captured without shader errors.");
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                SessionState.SetBool("CrystalSprint.EnvironmentCapture", false);
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void CaptureView(Camera camera, Vector3 position, Vector3 target, string name)
        {
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
            camera.aspect = 16f / 9f;
            RenderTexture output = new(1600, 900, 24, RenderTextureFormat.ARGB32);
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = output });
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = output;
            Texture2D texture = new(1600, 900, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, 1600f, 900f), 0, 0);
            texture.Apply();
            Directory.CreateDirectory("Logs/EnvironmentPreviews");
            File.WriteAllBytes("Logs/EnvironmentPreviews/" + name + ".png", texture.EncodeToPNG());
            RenderTexture.active = previous;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(output);
        }

        [MenuItem("Tools/Crystal Sprint/Apply Imported Environment To Existing Scene")]
        public static void Apply()
        {
            EditorSceneManager.OpenScene(CrystalSprintProjectSetup.ScenePath);
            if (Object.FindAnyObjectByType<InstancedForestGrass>() != null)
            {
                DenseForestUpgrade.Validate();
                Debug.Log("The newer vegetation integration is active; retained Nature details require no reintegration.");
                return;
            }
            terrain = new Collider[]
            {
                GameObject.Find("Ground").GetComponent<MeshCollider>(),
                GameObject.Find("Continuous Mountain Terrain").GetComponent<MeshCollider>()
            };
            ConfigurePipeline();
            if (Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None).Length == 0)
            {
                ReplaceTrees();
                ReplaceStumps();
                ReplaceScenery();
                ReplaceWater();
                AddForestFloorDetails();
            }
            SeatCliffOutcrops();
            ConvertRetainedMaterials();
            Validate();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            if (!EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), CrystalSprintProjectSetup.ScenePath))
                throw new InvalidOperationException("Could not save the integrated scene.");
            AssetDatabase.SaveAssets();
            Debug.Log("Environment integration completed. Existing terrain, player and gameplay preserved.");
        }

        private static void ConfigurePipeline()
        {
            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PackRoot + "/Settings/UNS_URP.asset");
            if (pipeline == null) throw new InvalidOperationException("The pack's URP configuration is missing.");
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            for (int level = 0; level < QualitySettings.names.Length; level++)
            {
                QualitySettings.SetQualityLevel(level, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);
            PlayerSettings.colorSpace = ColorSpace.Linear;
            Camera camera = Camera.main;
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.requiresDepthOption = CameraOverrideOption.On;
            data.requiresColorOption = CameraOverrideOption.On;
            data.renderPostProcessing = false;
        }

        private static void ConvertRetainedMaterials()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" }))
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                bool standard = material.shader.name == "Standard";
                bool particles = material.shader.name == "Particles/Standard Unlit" || material.shader.name == "Legacy Shaders/Particles/Alpha Blended";
                if (!standard && !particles) continue;
                Texture texture = material.mainTexture;
                Vector2 scale = material.mainTextureScale;
                Vector2 offset = material.mainTextureOffset;
                Color color = material.color;
                float smoothness = material.HasProperty("_Glossiness") ? material.GetFloat("_Glossiness") : 0f;
                float metallic = material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0f;
                Color emission = material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;
                material.shader = Shader.Find(particles ? "Universal Render Pipeline/Particles/Unlit" : "Universal Render Pipeline/Lit");
                material.shaderKeywords = Array.Empty<string>();
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", scale);
                material.SetTextureOffset("_BaseMap", offset);
                material.SetColor("_BaseColor", color);
                material.SetColor("_Color", color);
                if (standard)
                {
                    material.SetFloat("_Smoothness", smoothness);
                    material.SetFloat("_Metallic", metallic);
                    material.SetColor("_EmissionColor", emission);
                    if (emission.maxColorComponent > 0.001f) material.EnableKeyword("_EMISSION");
                }
                if (particles)
                {
                    material.SetFloat("_Surface", 1f);
                    material.SetFloat("_Blend", 0f);
                    material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    material.SetFloat("_ZWrite", 0f);
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.renderQueue = (int)RenderQueue.Transparent;
                    material.SetShaderPassEnabled("ShadowCaster", false);
                    material.SetShaderPassEnabled("DepthOnly", false);
                }
                EditorUtility.SetDirty(material);
            }
        }

        private static GameObject LoadPrefab(string relative) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(PackRoot + "/Environment/" + relative + ".prefab")
            ?? throw new InvalidOperationException("Missing pack prefab: " + relative);

        private static GameObject Spawn(string relative, EnvironmentAssetKind kind, Transform parent, Vector3 point, Vector3 scale, float yaw, float slope = 1f, bool collision = true)
        {
            GameObject prefab = LoadPrefab(relative);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent, false);
            instance.transform.localScale = scale;
            RaycastHit ground = TerrainHit(point);
            Quaternion tilt = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(Vector3.up, ground.normal), slope);
            instance.transform.SetPositionAndRotation(new Vector3(point.x, 0f, point.z), tilt * Quaternion.Euler(0f, yaw, 0f));
            MeshFilter mesh = PrimaryMesh(instance);
            Vector3 lowest = mesh.transform.TransformPoint(mesh.sharedMesh.vertices[0]);
            foreach (Vector3 vertex in mesh.sharedMesh.vertices)
            {
                Vector3 world = mesh.transform.TransformPoint(vertex);
                if (world.y < lowest.y) lowest = world;
            }
            RaycastHit contact = TerrainHit(lowest);
            instance.transform.position += Vector3.up * (contact.point.y - lowest.y - 0.012f);
            instance.AddComponent<EnvironmentAssetInstance>().Configure(kind, AssetDatabase.GetAssetPath(prefab), contact.point);
            if (!collision)
            {
                foreach (Collider collider in instance.GetComponentsInChildren<Collider>()) collider.enabled = false;
            }
            else if (kind == EnvironmentAssetKind.Stump)
            {
                foreach (Collider collider in instance.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
                instance.AddComponent<MeshCollider>().sharedMesh = mesh.sharedMesh;
            }
            else if (kind == EnvironmentAssetKind.Log)
            {
                foreach (MeshCollider collider in instance.GetComponentsInChildren<MeshCollider>()) collider.enabled = false;
            }
            if (collision)
                instance.AddComponent<SurfaceMarker>().Configure(kind == EnvironmentAssetKind.Rock || kind == EnvironmentAssetKind.Cliff ? SurfaceType.Stone : SurfaceType.Wood);
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
            {
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);
            }
            VerifyPackInstance(instance);
            return instance;
        }

        private static void ReplaceTrees()
        {
            Transform group = GameObject.Find("Trees").transform;
            int index = 0;
            foreach (Transform old in group.Cast<Transform>().ToArray())
            {
                bool tall = random.NextDouble() < 0.55;
                float scale = tall ? Range(0.53f, 0.76f) : Range(0.91f, 1.25f);
                GameObject tree = Spawn("Trees/Fir/Prefabs/UNS_Spruce_0" + (tall ? "1" : "2"), EnvironmentAssetKind.Tree,
                    group, old.position, Vector3.one * scale, Range(0f, 360f), 0.15f);
                tree.name = $"Tree {++index:00} - " + (tall ? "Spruce Mature" : "Spruce Young");
                Object.DestroyImmediate(old.gameObject);
            }
        }

        private static void ReplaceStumps()
        {
            Transform obstacles = GameObject.Find("Obstacles").transform;
            foreach (Transform group in obstacles)
            {
                foreach (Transform old in group.Cast<Transform>().ToArray())
                {
                    Bounds bounds = VisualBounds(old.gameObject);
                    float width = Mathf.Clamp((bounds.size.x + bounds.size.z) * 0.5f, 1.35f, 2.15f);
                    float height = Mathf.Clamp(bounds.size.y, 0.62f, 1.08f);
                    GameObject stump = Spawn("Props/Logs/Prefabs/UNS_Stump", EnvironmentAssetKind.Stump, group,
                        old.position, new Vector3(width / 1.152f, height / 0.986f, width / 1.152f * Range(0.94f, 1.05f)), Range(0f, 360f), 0f);
                    stump.name = old.name + " - UNS";
                    Object.DestroyImmediate(old.gameObject);
                }
            }
        }

        private static void ReplaceScenery()
        {
            Transform details = GameObject.Find("Natural Details").transform;
            foreach (Transform old in details.Cast<Transform>().ToArray())
            {
                bool bush = old.name.StartsWith("Bush", StringComparison.Ordinal);
                string relative = bush ? "Vegetation/Bushes/Prefabs/UNS_Bush" : "Rocks/Forest/Prefabs/UNS_Standard_Rock_0" + random.Next(1, 6);
                GameObject prefab = LoadPrefab(relative);
                Bounds source = VisualBounds(prefab);
                float target = bush ? Range(0.85f, 1.4f) : Range(0.9f, 1.6f);
                float scale = target / Mathf.Max(source.size.x, source.size.z);
                GameObject instance = Spawn(relative, bush ? EnvironmentAssetKind.Bush : EnvironmentAssetKind.Rock, details,
                    old.position, Vector3.one * scale, Range(0f, 360f), 1f, !bush && target > 1.2f);
                instance.name = old.name + " - UNS";
                Object.DestroyImmediate(old.gameObject);
            }
            Transform shoreline = GameObject.Find("Natural Shoreline").transform;
            foreach (Transform old in shoreline.Cast<Transform>().ToArray())
            {
                string relative = "Rocks/River/Prefabs/UNS_Tiny_Rock_0" + random.Next(1, 6);
                Bounds source = VisualBounds(LoadPrefab(relative));
                float scale = Range(0.55f, 1.1f) / Mathf.Max(source.size.x, source.size.z);
                GameObject rock = Spawn(relative, EnvironmentAssetKind.Rock, shoreline, old.position, Vector3.one * scale, Range(0f, 360f), 1f, false);
                rock.name = old.name + " - UNS";
                Object.DestroyImmediate(old.gameObject);
            }
            Transform ledges = GameObject.Find("Rock Ledges and Outcrops").transform;
            foreach (Transform old in ledges.Cast<Transform>().ToArray())
            {
                string relative = "Rocks/Cliffs/Prefabs/UNS_Rock_Cliff_0" + random.Next(1, 6);
                Bounds source = VisualBounds(LoadPrefab(relative));
                float scale = Range(3.5f, 6f) / Mathf.Max(source.size.x, source.size.z);
                GameObject rock = Spawn(relative, EnvironmentAssetKind.Cliff, ledges, old.position, Vector3.one * scale, Range(0f, 360f), 0.45f);
                rock.name = old.name + " - UNS";
                Object.DestroyImmediate(old.gameObject);
            }
        }

        private static void ReplaceWater()
        {
            GameObject old = GameObject.Find("Animated Water Surface");
            float height = old.transform.position.y;
            string modelPath = PackRoot + "/Environment/Water/Models/UNS_Water_Detailed.fbx";
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
            GameObject prefab = LoadPrefab("Water/Prefabs/UNS_Water_Detailed");
            GameObject water = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            water.name = "Animated Water Surface";
            water.transform.SetParent(old.transform.parent, false);
            Bounds local = VisualBounds(water);
            // The pack's plane uses a corner pivot. Terrain occludes its edges to preserve the shoreline.
            Vector3 scale = new(0.42f, 0.02f, 0.42f);
            water.transform.localScale = scale;
            water.transform.position = new Vector3(-local.center.x * scale.x, height - local.center.y * scale.y, -local.center.z * scale.z);
            foreach (Collider collider in water.GetComponentsInChildren<Collider>()) collider.enabled = false;
            foreach (Renderer renderer in water.GetComponentsInChildren<Renderer>()) renderer.shadowCastingMode = ShadowCastingMode.Off;
            PondSurfaceMotion motion = water.AddComponent<PondSurfaceMotion>();
            motion.Configure(height, (MeshCollider)terrain[0]);
            water.AddComponent<EnvironmentAssetInstance>().Configure(EnvironmentAssetKind.Water, AssetDatabase.GetAssetPath(prefab), new Vector3(0f, height, 0f));
            Object.FindAnyObjectByType<FishJumpSystem>().ConfigureWater(motion);
            ReflectionProbe probe = GameObject.Find("Pond Reflection Probe").GetComponent<ReflectionProbe>();
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.size = new Vector3(26f, 12f, 26f);
            VerifyPackInstance(water);
            Object.DestroyImmediate(old);
        }

        private static void SeatCliffOutcrops()
        {
            // A single lowest-vertex contact perches an irregular rock on one corner on a slope.
            // Fit the complete model to the actual terrain: its lower quarter forms the embedded
            // base of the outcrop, while the remaining rock visibly emerges from the mountainside.
            foreach (EnvironmentAssetInstance item in Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None))
            {
                if (item.Kind != EnvironmentAssetKind.Cliff) continue;
                Transform root = item.transform;
                root.position = new Vector3(root.position.x, 0f, root.position.z);
                MeshFilter mesh = PrimaryMesh(item.gameObject);
                Vector3[] points = mesh.sharedMesh.vertices.Select(mesh.transform.TransformPoint).ToArray();
                float[] offsets = points.Select(point => TerrainHit(point).point.y - point.y).OrderBy(value => value).ToArray();
                float height = offsets[Mathf.FloorToInt((offsets.Length - 1) * 0.75f)] - 0.025f;
                root.position += Vector3.up * height;
                Vector3 contactVertex = points.OrderBy(point => Mathf.Abs(TerrainHit(point).point.y - point.y - height)).First();
                item.Configure(item.Kind, item.SourcePrefab, TerrainHit(contactVertex).point);
                PrefabUtility.RecordPrefabInstancePropertyModifications(root);
            }
        }

        private static void AddForestFloorDetails()
        {
            Transform parent = GameObject.Find("Environment").transform;
            Transform logs = new GameObject("Fallen Timber").transform;
            Transform mushrooms = new GameObject("Mushroom Clusters").transform;
            Transform branches = new GameObject("Forest Floor Branches").transform;
            logs.SetParent(parent, false);
            mushrooms.SetParent(parent, false);
            branches.SetParent(parent, false);
            for (int index = 0; index < 7; index++)
            {
                GameObject instance = Spawn("Props/Logs/Prefabs/UNS_Log", EnvironmentAssetKind.Log, logs,
                    DetailPosition(2.5f), Vector3.one * Range(0.9f, 1.45f), Range(0f, 360f));
                instance.name = $"Fallen Log {index + 1:00}";
            }
            for (int index = 0; index < 42; index++)
            {
                GameObject instance = Spawn("Vegetation/Mushrooms/Prefabs/UNS_Mushroom_Patch", EnvironmentAssetKind.Mushroom, mushrooms,
                    DetailPosition(0.75f), Vector3.one * Range(0.8f, 1.5f), Range(0f, 360f), 1f, false);
                instance.name = $"Mushroom Patch {index + 1:00}";
            }
            for (int index = 0; index < 32; index++)
            {
                GameObject instance = Spawn("Props/Branches/Prefabs/UNS_Branch", EnvironmentAssetKind.Branch, branches,
                    DetailPosition(0.8f), Vector3.one * Range(0.7f, 1.35f), Range(0f, 360f), 1f, false);
                instance.name = $"Ground Branch {index + 1:00}";
            }
        }

        private static Vector3 DetailPosition(float clearance)
        {
            EnvironmentAssetInstance[] items = Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None);
            EnvironmentAssetInstance[] anchors = items.Where(item => item.Kind == EnvironmentAssetKind.Tree || item.Kind == EnvironmentAssetKind.Stump || item.Kind == EnvironmentAssetKind.Rock).ToArray();
            for (int attempt = 0; attempt < 3000; attempt++)
            {
                Vector3 anchor = anchors[random.Next(anchors.Length)].transform.position;
                float angle = Range(0f, Mathf.PI * 2f);
                float distance = Range(clearance + 0.55f, clearance + 2f);
                Vector3 point = anchor + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
                if (new Vector2(point.x, point.z).magnitude > 38.5f || new Vector2(point.x, point.z).magnitude < 11f ||
                    (Mathf.Abs(point.x) < 4.7f && point.z < 30f)) continue;
                if (items.Any(item => item.Kind != EnvironmentAssetKind.Water && HorizontalDistance(item.transform.position, point) < clearance)) continue;
                return point;
            }
            throw new InvalidOperationException("Could not place a forest detail with sufficient clearance.");
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        private static float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

        private static RaycastHit TerrainHit(Vector3 point)
        {
            Ray ray = new(new Vector3(point.x, 100f, point.z), Vector3.down);
            RaycastHit result = default;
            float nearest = float.MaxValue;
            foreach (Collider collider in terrain)
            {
                if (collider.Raycast(ray, out RaycastHit hit, 150f) && hit.distance < nearest)
                {
                    result = hit;
                    nearest = hit.distance;
                }
            }
            if (nearest == float.MaxValue) throw new InvalidOperationException("No terrain below " + point);
            return result;
        }

        private static MeshFilter PrimaryMesh(GameObject instance)
        {
            LODGroup group = instance.GetComponent<LODGroup>();
            return group != null ? group.GetLODs()[0].renderers[0].GetComponent<MeshFilter>() : instance.GetComponentInChildren<MeshFilter>();
        }

        private static void VerifyPackInstance(GameObject instance)
        {
            foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>())
                if (filter.sharedMesh == null || !AssetDatabase.GetAssetPath(filter.sharedMesh).StartsWith(PackRoot, StringComparison.Ordinal))
                    throw new InvalidOperationException("Non-pack or missing mesh on " + instance.name);
            foreach (Material material in instance.GetComponentsInChildren<Renderer>().SelectMany(renderer => renderer.sharedMaterials))
            {
                if (material == null || !AssetDatabase.GetAssetPath(material).StartsWith(PackRoot, StringComparison.Ordinal) || material.shader.name != "Universal Render Pipeline/Lit")
                    throw new InvalidOperationException("Non-pack or incompatible material on " + instance.name);
                if (material.name != "UNS_Water" && material.GetTexture("_BaseMap") == null)
                    throw new InvalidOperationException("Missing pack texture on " + instance.name);
            }
        }

        [MenuItem("Tools/Crystal Sprint/Validate Imported Environment")]
        public static void Validate()
        {
            if (Object.FindAnyObjectByType<InstancedForestGrass>() != null) { DenseForestUpgrade.Validate(); return; }
            EnvironmentAssetInstance[] items = Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None);
            if (items.Length == 0) throw new InvalidOperationException("No imported environment instances found.");
            foreach (EnvironmentAssetInstance item in items) VerifyPackInstance(item.gameObject);
            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null || material.shader.name == "Hidden/InternalErrorShader" || ShaderUtil.ShaderHasError(material.shader))
                        throw new InvalidOperationException("Missing/broken scene material on " + renderer.name);
                    if (material.shader.name == "Standard" || material.shader.name == "CrystalSprint/InteractiveFoliage" || material.shader.name == "CrystalSprint/PondWater")
                        throw new InvalidOperationException("Legacy material remains visible on " + renderer.name);
                }
            }
            StringBuilder report = new("Imported environment validation\n");
            foreach (IGrouping<EnvironmentAssetKind, EnvironmentAssetInstance> group in items.GroupBy(item => item.Kind))
                report.AppendLine($"{group.Key}: {group.Count()} instances; {string.Join(", ", group.Select(item => Path.GetFileNameWithoutExtension(item.SourcePrefab)).Distinct())}");
            report.AppendLine("All placed meshes and materials in replaced categories originate from Ultimate Nature – Starter.");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/environment-integration-report.txt", report.ToString());
            Debug.Log(report.ToString());
        }

        [MenuItem("Tools/Crystal Sprint/Inspect Imported Environment Pack")]
        public static void Inspect()
        {
            StringBuilder report = new();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PackRoot + "/Environment" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                Bounds bounds = VisualBounds(instance);
                report.AppendLine($"{path}: min={bounds.min:F3}, size={bounds.size:F3}, LODs={instance.GetComponentsInChildren<LODGroup>().Length}, colliders={instance.GetComponentsInChildren<Collider>().Length}");
                foreach (MeshFilter mesh in instance.GetComponentsInChildren<MeshFilter>())
                    report.AppendLine($"  {mesh.name}: {mesh.sharedMesh.vertexCount} vertices; bounds={mesh.sharedMesh.bounds}");
                foreach (Material material in instance.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Distinct())
                    report.AppendLine($"  Material: {AssetDatabase.GetAssetPath(material)} | {material.shader.name} | supported={material.shader.isSupported}");
                Object.DestroyImmediate(instance);
            }
            EditorSceneManager.OpenScene(CrystalSprintProjectSetup.ScenePath);
            foreach (string root in new[] { "Trees", "Obstacles", "Natural Details", "Mountain Boundary", "Central Pond" })
            {
                GameObject group = GameObject.Find(root);
                report.AppendLine($"SCENE {root}: {group.transform.childCount} children");
                foreach (Transform child in group.transform)
                    report.AppendLine($"  {child.name} @ {child.position:F2}; bounds={VisualBounds(child.gameObject)}; source={PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject)}");
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/environment-pack-audit.txt", report.ToString());
            Debug.Log("Environment pack inspection written to Logs/environment-pack-audit.txt");
        }

        private static Bounds VisualBounds(GameObject instance)
        {
            MeshFilter[] meshes = instance.GetComponentsInChildren<MeshFilter>();
            if (meshes.Length == 0) return new Bounds(instance.transform.position, Vector3.zero);
            Bounds bounds = new(meshes[0].transform.TransformPoint(meshes[0].sharedMesh.bounds.center), Vector3.zero);
            foreach (MeshFilter filter in meshes)
            {
                Bounds local = filter.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = local.center + Vector3.Scale(local.extents,
                        new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
                    bounds.Encapsulate(filter.transform.TransformPoint(point));
                }
            }
            return bounds;
        }
    }
}
