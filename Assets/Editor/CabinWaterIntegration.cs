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
    public static class CabinWaterIntegration
    {
        public const string CabinSource = "Assets/Cozy Mountain Cabin/Demo/Sample.unity";
        public const string CabinPrefab = "Assets/Prefabs/PondCabin/PondsideCabin.prefab";
        public const string WaterSource = "Assets/Houidisoft technology/Simple water/Resources/water material sample.mat";
        public const string WaterMaterial = "Assets/Materials/PondCabin/Pond_SimpleWater.mat";
        public const string ReportFolder = "Logs/CabinWaterReview";

        [MenuItem("Tools/Crystal Sprint/Integrate Pond Cabin And Water")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play Mode first.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the open scene first.");
            Directory.CreateDirectory("Assets/Prefabs/PondCabin"); Directory.CreateDirectory("Assets/Materials/PondCabin");
            Directory.CreateDirectory("Assets/Meshes/PondCabin"); Directory.CreateDirectory(ReportFolder);
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(CrystalSprintProjectSetup.ScenePath);
            if (GameObject.Find("Pondside Cabin") != null) throw new InvalidOperationException("Cabin already installed; preserve its manual edits.");
            CreateCabinPrefab();
            var scene = EditorSceneManager.OpenScene(CrystalSprintProjectSetup.ScenePath);
            GameObject existing = GameObject.Find("Pondside Cabin");
            if (existing != null) throw new InvalidOperationException("Cabin already installed. Preserve manual scene placement; edit its prefab instead of rerunning integration.");
            MeshCollider terrain = GameObject.Find("Ground").GetComponent<MeshCollider>();
            GameObject cabin = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CabinPrefab), scene);
            cabin.name = "Pondside Cabin";
            cabin.transform.SetPositionAndRotation(new Vector3(18, GroundHeight(terrain, new Vector2(18, -1)) + .015f, 4), Quaternion.Euler(0, 180, 0));
            cabin.transform.localScale = Vector3.one * 1.35f;
            List<Bounds> clearings = new()
            {
                new Bounds(new Vector3(18, 0, 4), new Vector3(8.2f, 10, 11.2f)),
                new Bounds(new Vector3(18, 0, -3.4f), new Vector3(5.8f, 10, 5f)),
                new Bounds(new Vector3(16.7f, 0, -6.8f), new Vector3(3.1f, 10, 4.2f))
            };
            for (int i = 0; i < 7; i++) clearings.Add(new Bounds(new Vector3(16.7f - i * 1.05f, 0, -8f - Mathf.Sin(i * .48f) * .7f), new Vector3(2.2f, 10, 2f)));
            int moved = ClearSite(terrain, clearings);
            Object.FindAnyObjectByType<InstancedForestGrass>().SetLocalClearings(clearings.ToArray());
            ConfigureWater();
            FitPondBedToTerrain();
            PrefabUtility.RecordPrefabInstancePropertyModifications(cabin.transform);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            PondCabin site = cabin.GetComponent<PondCabin>();
            File.WriteAllText(ReportFolder + "/integration.txt", $"Source assembly: {CabinSource} / CozyCabin\nPrefab: {CabinPrefab}\nScale: 1.35 uniform\nRoot: {cabin.transform.position:F3}; yaw 180\nDoor: {site.DoorWidth:F2} m wide, {site.DoorHeight:F2} m high\nEntrance: {site.Entrance:F3}\nRepositioned existing vegetation/props: {moved}\nTerrain mesh unchanged. Interior floor + low foundation added. Door held open at 105 degrees.\nWater: original Custom/SimpleWaterURP shader and normal map, dedicated material. CPU waves/fish timing/effects preserved.\n");
            Debug.Log("Cabin and water integration saved. " + moved + " nearby props relocated; none deleted.");
        }

        private static void CreateCabinPrefab()
        {
            EditorSceneManager.OpenScene(CabinSource);
            GameObject root = GameObject.Find("CozyCabin");
            root.name = "Pondside Cabin";
            var materialMap = new Dictionary<Material, Material>();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                renderer.lightmapIndex = -1; renderer.realtimeLightmapIndex = -1;
                renderer.sharedMaterials = renderer.sharedMaterials.Select(source =>
                {
                    if (!materialMap.TryGetValue(source, out Material converted))
                    {
                        converted = SaveMaterial(ConvertMaterial(source), "Cabin_" + source.name);
                        materialMap.Add(source, converted);
                    }
                    return converted;
                }).ToArray();
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            foreach (Transform part in root.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(part.gameObject, StaticEditorFlags.BatchingStatic);
            Transform door = root.transform.Find("Door");
            door.localRotation = Quaternion.Euler(0, 105, 0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(door);
            Transform curtain = root.transform.Find("Curtain (3)");
            curtain.localPosition = new Vector3(2.17f, curtain.localPosition.y, curtain.localPosition.z);
            curtain.localScale = new Vector3(1, 1, .22f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(curtain);
            // The sample assembly already overrides the coarse walls/frame boxes with meshes.
            foreach (string name in new[] { "CabinWalls", "DoorFrame" })
                if (root.transform.Find(name).GetComponent<MeshCollider>() == null) throw new InvalidOperationException(name + " must retain its doorway-shaped collider.");
            root.AddComponent<PondCabin>(); root.AddComponent<SurfaceMarker>().Configure(SurfaceType.Wood);
            Material wood = materialMap.Values.First(m => m.name.Contains("vertical_woods"));
            GameObject floor = new("Interior Plank Floor"); floor.transform.SetParent(root.transform, false);
            BoxCollider floorCollider = floor.AddComponent<BoxCollider>(); floorCollider.center = new Vector3(0, .204f, 0); floorCollider.size = new Vector3(5.45f, .1f, 7.5f);
            Mesh plank = CreatePlankMesh();
            for (int index = 0; index < 11; index++)
            {
                GameObject board = new("Floorboard " + (index + 1)); board.transform.SetParent(floor.transform, false);
                board.transform.localPosition = new Vector3(-2.475f + index * .495f, .21f, 0);
                board.transform.localScale = new Vector3(.489f, .088f, 7.45f);
                board.AddComponent<MeshFilter>().sharedMesh = plank; board.AddComponent<MeshRenderer>().sharedMaterial = wood;
            }
            GameObject foundation = GameObject.CreatePrimitive(PrimitiveType.Cube); foundation.name = "Low Stone Foundation";
            foundation.transform.SetParent(root.transform, false); foundation.transform.localPosition = new Vector3(0, -.055f, 0);
            foundation.transform.localScale = new Vector3(5.50f, .48f, 7.54f);
            foundation.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MountainRock.mat");
            foundation.AddComponent<SurfaceMarker>().Configure(SurfaceType.Stone);
            foreach (Light light in root.GetComponentsInChildren<Light>())
            {
                light.intensity = .55f; light.range = 4; light.shadows = LightShadows.None;
                light.lightmapBakeType = LightmapBakeType.Realtime;
                PrefabUtility.RecordPrefabInstancePropertyModifications(light);
            }
            Light fill = new GameObject("Soft Interior Light").AddComponent<Light>(); fill.transform.SetParent(root.transform, false);
            fill.transform.localPosition = new Vector3(0, 2.6f, .4f); fill.type = LightType.Point; fill.range = 7.5f; fill.intensity = .65f;
            fill.color = new Color(1, .83f, .63f); fill.shadows = LightShadows.None;
            CabinCollisionRepair.BuildSolidShell(root);
            PrefabUtility.SaveAsPrefabAsset(root, CabinPrefab);
            // None of the sample scene, imported prefabs, textures or materials are saved.
        }

        private static Mesh CreatePlankMesh()
        {
            string path = "Assets/Meshes/PondCabin/InteriorPlank.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (existing != null) return existing;
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh mesh = Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh); Object.DestroyImmediate(cube);
            mesh.name = "InteriorPlank";
            // Reuse the clean timber region in the supplied wood texture atlas (including normals).
            Vector2[] uv = mesh.uv;
            for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(Mathf.Lerp(.825f, .975f, uv[i].x), Mathf.Lerp(.575f, .965f, uv[i].y));
            mesh.uv = uv; AssetDatabase.CreateAsset(mesh, path); return mesh;
        }

        private static Material SaveMaterial(Material temporary, string name)
        {
            string path = "Assets/Materials/PondCabin/" + name + ".mat";
            Material saved = AssetDatabase.LoadAssetAtPath<Material>(path);
            temporary.name = name;
            if (saved == null) { AssetDatabase.CreateAsset(temporary, path); return temporary; }
            EditorUtility.CopySerialized(temporary, saved); Object.DestroyImmediate(temporary); EditorUtility.SetDirty(saved); return saved;
        }

        private static int ClearSite(MeshCollider terrain, List<Bounds> clearings)
        {
            EnvironmentAssetInstance[] all = Object.FindObjectsByType<EnvironmentAssetInstance>();
            Bounds building = new(new Vector3(18, 0, 4), new Vector3(13, 20, 20));
            bool InArea(Vector3 p, Bounds b) => b.Contains(new Vector3(p.x, b.center.y, p.z));
            var affected = all.Where(item => item.Kind != EnvironmentAssetKind.Water && item.Kind != EnvironmentAssetKind.Cliff &&
                ((item.Kind == EnvironmentAssetKind.Tree && InArea(item.GroundContact, building)) || clearings.Any(c => InArea(item.GroundContact, c)))).OrderBy(item => item.name).ToArray();
            System.Random random = new(2092026);
            foreach (EnvironmentAssetInstance item in affected)
            {
                bool tree = item.Kind == EnvironmentAssetKind.Tree;
                bool large = tree || item.Kind == EnvironmentAssetKind.Stump || item.Kind == EnvironmentAssetKind.Rock;
                Vector2 point = default; bool placed = false;
                for (int trial = 0; trial < 10000; trial++)
                {
                    point = new Vector2(26f + (float)random.NextDouble() * 23f, -30f + (float)random.NextDouble() * 42f);
                    if (point.magnitude > ForestWorld.Radius - 4f) continue;
                    if (ForestWorld.PathDistance(point) < (tree ? 4.6f : 2f)) continue;
                    if (all.Any(other => other != item && other.Kind != EnvironmentAssetKind.Water &&
                        Vector2.Distance(point, new Vector2(other.GroundContact.x, other.GroundContact.z)) <
                        (other.Kind == EnvironmentAssetKind.Tree ? (tree ? 3.5f : 2.1f) :
                         other.Kind == EnvironmentAssetKind.Stump || other.Kind == EnvironmentAssetKind.Rock || other.Kind == EnvironmentAssetKind.Log ? (large ? 2.6f : 1.8f) :
                         other.Kind == EnvironmentAssetKind.Bush ? 1.4f : .55f))) continue;
                    placed = true; break;
                }
                if (!placed) throw new InvalidOperationException("No safe nearby placement for " + item.name);
                Vector3 contact = new(point.x, GroundHeight(terrain, point), point.y);
                item.transform.position += contact - item.GroundContact;
                item.Configure(item.Kind, item.SourcePrefab, contact);
                PrefabUtility.RecordPrefabInstancePropertyModifications(item.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(item);
                if (large) clearings.Add(new Bounds(contact, new Vector3(tree ? 1.8f : 2.5f, 10, tree ? 1.8f : 2.5f)));
            }
            return affected.Length;
        }

        private static float GroundHeight(MeshCollider ground, Vector2 point)
        {
            if (!ground.Raycast(new Ray(new Vector3(point.x, 30, point.y), Vector3.down), out RaycastHit hit, 60)) throw new InvalidOperationException("No terrain below site.");
            return hit.point.y;
        }

        public static void RefineAndInspect()
        {
            // These copies belong to this integration; the imported package is untouched.
            foreach (string name in new[] { "Cabin_walls_mat", "Cabin_curtain_mat" })
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PondCabin/" + name + ".mat");
                material.SetFloat("_Cull", (float)CullMode.Off);
                material.doubleSidedGI = true;
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(CrystalSprintProjectSetup.ScenePath);
            PondSurfaceMotion water = Object.FindAnyObjectByType<PondSurfaceMotion>();
            StringBuilder report = new();
            foreach (MeshFilter filter in water.GetComponentsInChildren<MeshFilter>())
            {
                Mesh mesh = filter.sharedMesh;
                Vector3[] vertices = mesh.vertices.Select(filter.transform.TransformPoint).ToArray();
                int[] triangles = mesh.triangles;
                int upwards = 0, downwards = 0;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    float y = Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]], vertices[triangles[i + 2]] - vertices[triangles[i]]).y;
                    if (y > 0) upwards++; else downwards++;
                }
                report.AppendLine($"{filter.name}: {AssetDatabase.GetAssetPath(mesh)}, vertices={vertices.Length}, up={upwards}, down={downwards}, minY={vertices.Min(v => v.y)}, maxY={vertices.Max(v => v.y)}");
            }
            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>())
                if (renderer.bounds.Contains(new Vector3(0, water.SurfaceHeight, 0)) || renderer.name.Contains("Water") || renderer.name.Contains("Pond"))
                    report.AppendLine($"Renderer {renderer.name}: bounds={renderer.bounds}, materials={string.Join(",", renderer.sharedMaterials.Select(m => m.name))}");
            File.WriteAllText(ReportFolder + "/water-geometry.txt", report.ToString());
            FitPondBedToTerrain();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log(report);
        }

        private static void FitPondBedToTerrain()
        {
            const string path = "Assets/Meshes/PondCabin/TerrainConformingPondBed.asset";
            MeshFilter bed = GameObject.Find("Pond Bed").GetComponent<MeshFilter>();
            Mesh saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (saved == null)
            {
                // The old radial disc and the square terrain grid intersected between vertices.
                // Reuse the terrain's actual triangles for this visual overlay, without touching
                // the terrain mesh/collider, shoreline outline, water height or fish sampler.
                MeshFilter ground = GameObject.Find("Ground").GetComponent<MeshFilter>();
                Vector3[] source = ground.sharedMesh.vertices.Select(ground.transform.TransformPoint).ToArray();
                Vector3[] outline = bed.sharedMesh.vertices.Skip(bed.sharedMesh.vertexCount - 64).Select(bed.transform.TransformPoint).ToArray();
                float DistanceToEdge(Vector3 point)
                {
                    float angle = Mathf.Repeat(Mathf.Atan2(point.z, point.x), Mathf.PI * 2) * 64 / (Mathf.PI * 2);
                    int segment = Mathf.FloorToInt(angle);
                    Vector3 a = outline[segment % 64], b = outline[(segment + 1) % 64];
                    float radius = Mathf.Lerp(new Vector2(a.x, a.z).magnitude, new Vector2(b.x, b.z).magnitude, angle - segment);
                    return new Vector2(point.x, point.z).magnitude - radius;
                }
                var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>();
                int[] sourceIndices = ground.sharedMesh.triangles;
                for (int i = 0; i < sourceIndices.Length; i += 3)
                {
                    Vector3[] triangle = { source[sourceIndices[i]], source[sourceIndices[i + 1]], source[sourceIndices[i + 2]] };
                    if (triangle.All(v => DistanceToEdge(v) > 0)) continue;
                    var clipped = new List<Vector3>();
                    Vector3 previous = triangle[2]; float previousDistance = DistanceToEdge(previous);
                    foreach (Vector3 current in triangle)
                    {
                        float distance = DistanceToEdge(current);
                        if ((distance <= 0) != (previousDistance <= 0))
                            clipped.Add(Vector3.Lerp(previous, current, previousDistance / (previousDistance - distance)));
                        if (distance <= 0) clipped.Add(current);
                        previous = current; previousDistance = distance;
                    }
                    int start = vertices.Count;
                    foreach (Vector3 point in clipped)
                    {
                        vertices.Add(bed.transform.InverseTransformPoint(point + Vector3.up * .012f));
                        uv.Add(new Vector2(point.x / 19.1f + .5f, point.z / 19.1f + .5f));
                    }
                    for (int p = 1; p < clipped.Count - 1; p++)
                    { indices.Add(start); indices.Add(start + p); indices.Add(start + p + 1); }
                }
                saved = new Mesh { name = "TerrainConformingPondBed" };
                saved.SetVertices(vertices); saved.SetUVs(0, uv); saved.SetTriangles(indices, 0); saved.RecalculateNormals(); saved.RecalculateBounds();
                AssetDatabase.CreateAsset(saved, path);
            }
            bed.sharedMesh = saved;
            Material material = new(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PondBed.mat"));
            material.SetColor("_BaseColor", new Color(.62f, .56f, .42f));
            bed.GetComponent<Renderer>().sharedMaterial = SaveMaterial(material, "Pond_ShoreBed");
            // A cosmetic overlay must not cast shadows back onto the ground 12 mm beneath it.
            bed.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void ConfigureWater()
        {
            Material material = new(AssetDatabase.LoadAssetAtPath<Material>(WaterSource));
            material.shaderKeywords = Array.Empty<string>();
            // Keep the CPU waves as the single height source for fish, ripples and player contact.
            material.SetFloat("_WaveStrength", 0); material.SetFloat("_NormalTiling", 6.5f);
            material.SetFloat("_NormalStrength", .18f); material.SetFloat("_NormalSpeed", .055f);
            material.SetFloat("_WaterDepth", .8f); material.SetFloat("_FresnelPower", 3.4f); material.SetFloat("_ReflectionStrength", .48f);
            material.SetFloat("_FoamDistance", .14f); material.SetFloat("_FoamTiling", 8f); material.SetFloat("_FoamSpeed", .025f);
            material.SetColor("_ShallowColor", new Color(.22f, .49f, .42f, .38f)); material.SetColor("_DeepColor", new Color(.035f, .23f, .24f, .85f));
            material.SetColor("_FoamColor", new Color(.67f, .81f, .75f, 1));
            material.SetTexture("_FoamNoiseTex", CreateFoamNoise());
            material = SaveMaterial(material, "Pond_SimpleWater");
            PondSurfaceMotion water = Object.FindAnyObjectByType<PondSurfaceMotion>();
            foreach (Renderer renderer in water.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material; PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            var cameraData = Camera.main.GetUniversalAdditionalCameraData();
            cameraData.requiresDepthOption = CameraOverrideOption.On;
            var fish = new SerializedObject(Object.FindAnyObjectByType<FishJumpSystem>());
            GameObject ripple = (GameObject)fish.FindProperty("ripplePrefab").objectReferenceValue;
            GameObject splash = (GameObject)fish.FindProperty("splashPrefab").objectReferenceValue;
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            var interaction = player.GetComponent<PlayerWaterInteraction>() ?? player.gameObject.AddComponent<PlayerWaterInteraction>();
            interaction.Configure(water, ripple, splash);
            EditorUtility.SetDirty(interaction);
        }

        private static Texture2D CreateFoamNoise()
        {
            string path = "Assets/Materials/PondCabin/SoftFoamNoise.asset";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path); if (existing != null) return existing;
            Texture2D texture = new(128, 128, TextureFormat.RGB24, true, true) { name = "SoftFoamNoise", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
            Color[] pixels = new Color[128 * 128];
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                float u = x / 127f, v = y / 127f;
                float a = Mathf.Lerp(Mathf.PerlinNoise(u * 5 + 10, v * 5 + 10), Mathf.PerlinNoise((u - 1) * 5 + 10, v * 5 + 10), u);
                float b = Mathf.Lerp(Mathf.PerlinNoise(u * 5 + 10, (v - 1) * 5 + 10), Mathf.PerlinNoise((u - 1) * 5 + 10, (v - 1) * 5 + 10), u);
                float value = Mathf.SmoothStep(0f, .8f, Mathf.InverseLerp(.32f, .65f, Mathf.Lerp(a, b, v)));
                pixels[y * 128 + x] = new Color(value, value, value, 1);
            }
            texture.SetPixels(pixels); texture.Apply(); AssetDatabase.CreateAsset(texture, path); return texture;
        }

        public static void Inspect()
        {
            Directory.CreateDirectory(ReportFolder);
            EditorSceneManager.OpenScene(CabinSource);
            GameObject root = GameObject.Find("CozyCabin");
            if (root == null) throw new InvalidOperationException("Assembled cabin missing from supplied sample scene.");
            StringBuilder report = new();
            foreach (Transform part in root.GetComponentsInChildren<Transform>(true))
            {
                report.AppendLine($"{PathOf(part, root.transform)} pos={part.localPosition:F3} rot={part.localEulerAngles:F1} scale={part.localScale:F3}");
                foreach (Renderer renderer in part.GetComponents<Renderer>())
                    report.AppendLine($"  bounds={renderer.bounds} materials={string.Join(",", renderer.sharedMaterials.Select(m => m == null ? "MISSING" : m.name + ":" + m.shader.name))}");
                foreach (Collider collider in part.GetComponents<Collider>())
                    report.AppendLine($"  {collider.GetType().Name} enabled={collider.enabled} bounds={collider.bounds}");
            }
            File.WriteAllText(ReportFolder + "/source-inspection.txt", report.ToString());
            GameObject model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Cozy Mountain Cabin/Models/cozy_cabin.fbx"));
            foreach (Renderer mesh in model.GetComponentsInChildren<Renderer>()) report.AppendLine($"FBX {mesh.name}: {mesh.bounds}");
            Object.DestroyImmediate(model);
            Physics.SyncTransforms();
            var wall = root.transform.Find("CabinWalls").GetComponent<MeshCollider>();
            foreach (float y in new[] { .3f, .5f, 1f, 2f, 2.25f, 2.4f })
            {
                string open = "";
                for (float x = .1f; x < 1.9f; x += .1f)
                    if (!wall.Raycast(new Ray(new Vector3(x, y, 5), Vector3.back), out _, 2)) open += x.ToString("F1") + ",";
                report.AppendLine($"Door wall opening at y={y}: x={open}");
            }
            report.AppendLine("Interior floor in wall mesh: " + (wall.Raycast(new Ray(new Vector3(0, 3, 0), Vector3.down), out RaycastHit floorHit, 6) ? floorHit.point.ToString("F3") : "none"));
            File.WriteAllText(ReportFolder + "/geometry-inspection.txt", report.ToString());
            Debug.Log(report.ToString());
            // Preview only: replace Standard materials in memory, never modify imported files.
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterials = renderer.sharedMaterials.Select(ConvertMaterial).ToArray();
            foreach (Light light in Object.FindObjectsByType<Light>()) light.enabled = false;
            Light sun = new GameObject("Inspection Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.8f; sun.transform.rotation = Quaternion.Euler(35, -30, 0);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = Color.gray;
            Camera camera = Camera.main; camera.transform.localScale = Vector3.one;
            camera.fieldOfView = 52;
            camera.transform.SetPositionAndRotation(new Vector3(-8, 5, 13), Quaternion.LookRotation(new Vector3(8, -3, -13)));
            Texture2D picture = FirstPersonReview.RenderStack(camera);
            File.WriteAllBytes(ReportFolder + "/source-cabin.png", picture.EncodeToPNG());
            Object.DestroyImmediate(picture);
            EditorSceneManager.OpenScene(CrystalSprintProjectSetup.ScenePath);
            MeshCollider ground = GameObject.Find("Ground").GetComponent<MeshCollider>();
            report.Clear();
            for (int z = -12; z <= 15; z += 3)
            for (int x = 10; x <= 25; x += 3)
                if (ground.Raycast(new Ray(new Vector3(x, 20, z), Vector3.down), out RaycastHit hit, 40)) report.AppendLine($"ground ({x},{z}): {hit.point.y:F3}");
            foreach (EnvironmentAssetInstance item in Object.FindObjectsByType<EnvironmentAssetInstance>())
                if (item.transform.position.x > 10 && item.transform.position.x < 27 && Mathf.Abs(item.transform.position.z) < 14)
                    report.AppendLine($"nearby {item.Kind} {item.name} pos={item.transform.position:F2}");
            File.WriteAllText(ReportFolder + "/site-inspection.txt", report.ToString());
        }

        private static string PathOf(Transform part, Transform root) => part == root ? part.name : PathOf(part.parent, root) + "/" + part.name;

        private static Material ConvertMaterial(Material source)
        {
            Material target = new(Shader.Find("Universal Render Pipeline/Lit")) { name = source.name + "_URP" };
            // Source walls and cloth are single-sided surfaces, but the cabin is enterable.
            if (source.name == "walls_mat" || source.name == "curtain_mat")
            { target.SetFloat("_Cull", (float)CullMode.Off); target.doubleSidedGI = true; }
            target.SetColor("_BaseColor", source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white);
            foreach (var pair in new[] { ("_MainTex", "_BaseMap"), ("_BumpMap", "_BumpMap"), ("_MetallicGlossMap", "_MetallicGlossMap"), ("_EmissionMap", "_EmissionMap") })
                if (source.HasProperty(pair.Item1) && source.GetTexture(pair.Item1) != null) target.SetTexture(pair.Item2, source.GetTexture(pair.Item1));
            target.SetFloat("_Smoothness", .32f);
            target.SetFloat("_Metallic", source.HasProperty("_Metallic") ? source.GetFloat("_Metallic") : 0);
            if (target.GetTexture("_BumpMap") != null) { target.EnableKeyword("_NORMALMAP"); target.SetFloat("_BumpScale", .8f); }
            if (target.GetTexture("_MetallicGlossMap") != null) target.EnableKeyword("_METALLICSPECGLOSSMAP");
            if (source.HasProperty("_EmissionColor")) target.SetColor("_EmissionColor", source.GetColor("_EmissionColor"));
            if (target.GetTexture("_EmissionMap") != null) target.EnableKeyword("_EMISSION");
            if (source.name.Contains("glass"))
            {
                target.SetFloat("_Surface", 1); target.SetFloat("_Blend", 0); target.SetFloat("_ZWrite", 0);
                target.SetFloat("_SrcBlend", (int)BlendMode.SrcAlpha); target.SetFloat("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                target.SetOverrideTag("RenderType", "Transparent"); target.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                Color color = target.GetColor("_BaseColor"); color.a = .22f; target.SetColor("_BaseColor", color); target.renderQueue = 3000;
            }
            return target;
        }
    }
}
