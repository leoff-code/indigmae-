using System;
using System.Collections.Generic;
using System.IO;
using CrystalSprint;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrystalSprintEditor
{
    public static class CrystalSprintProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/CrystalSprint.unity";
        public const string BuildPath = "Builds/macOS/CrystalSprint.app";

        [MenuItem("Tools/Crystal Sprint/Generate Game")]
        public static void GenerateGame()
        {
            if (File.Exists(ScenePath) && File.ReadAllText(ScenePath).Contains("CrystalSprint.EnvironmentAssetInstance"))
            {
                Debug.Log("The scene uses the imported environment pack. Keeping the existing scene for builds; generation was skipped.");
                return;
            }
            ConfigureProject();
            EnsureFolders();
            AssetDatabase.DeleteAsset("Assets/Scenes/SetupTest.unity");
            AssetDatabase.DeleteAsset("Assets/Materials/Crystal.mat");
            AssetDatabase.DeleteAsset("Assets/Prefabs/CrystalCollectible.prefab");
            AssetDatabase.DeleteAsset("Assets/Materials/Obstacle.mat");
            AssetDatabase.DeleteAsset("Assets/Prefabs/Obstacle.prefab");
            AssetDatabase.DeleteAsset("Assets/Materials/Player.mat");
            AssetDatabase.DeleteAsset("Assets/Materials/GoldCoin.mat");
            AssetDatabase.DeleteAsset("Assets/Prefabs/CoinCollectible.prefab");
            AssetDatabase.DeleteAsset("Assets/Materials/Wall.mat");
            AssetDatabase.DeleteAsset("Assets/Prefabs/TreeStump_Small.prefab");
            AssetDatabase.DeleteAsset("Assets/Prefabs/TreeStump_Medium.prefab");
            AssetDatabase.DeleteAsset("Assets/Prefabs/TreeStump_Tall.prefab");
            AssetDatabase.DeleteAsset("Assets/Prefabs/GrassTuft.prefab");
            AssetDatabase.DeleteAsset("Assets/Materials/Grass.mat");

            Texture2D groundTexture = CreateNatureTexture("GroundTexture", new Color(0.12f, 0.32f, 0.055f), new Color(0.3f, 0.54f, 0.12f), 8f, false);
            Texture2D rockTexture = CreateNatureTexture("RockTexture", new Color(0.19f, 0.21f, 0.2f), new Color(0.52f, 0.53f, 0.48f), 5f, true);
            Texture2D barkTexture = CreateNatureTexture("BarkTexture", new Color(0.16f, 0.055f, 0.018f), new Color(0.46f, 0.2f, 0.055f), 12f, true);
            Texture2D cutTexture = CreateWoodCutTexture();
            Texture2D foliageTexture = CreateNatureTexture("FoliageTexture", new Color(0.045f, 0.2f, 0.035f), new Color(0.25f, 0.58f, 0.1f), 10f, false);

            Material ground = CreateMaterial("Ground", new Color(0.2f, 0.48f, 0.11f), 0f, 0.16f, false, groundTexture, new Vector2(14f, 14f));
            Material grass = CreateInteractiveGrassMaterial();
            Material movementParticles = CreateParticleMaterial();
            Material rock = CreateMaterial("MountainRock", new Color(0.44f, 0.46f, 0.42f), 0.02f, 0.2f, false, rockTexture, new Vector2(9f, 3f));
            Material rockLight = CreateMaterial("MountainRockLight", new Color(0.58f, 0.58f, 0.51f), 0.02f, 0.17f, false, rockTexture, new Vector2(7f, 3f));
            Material terrainBlend = CreateTerrainBlendMaterial(groundTexture, rockTexture);
            Material pondBed = CreateMaterial("PondBed", new Color(0.28f, 0.22f, 0.12f), 0f, 0.12f, false, rockTexture, new Vector2(3f, 3f));
            Material pondWater = CreatePondWaterMaterial();
            Material woodBark = CreateMaterial("WoodBark", new Color(0.42f, 0.19f, 0.055f), 0f, 0.26f, false, barkTexture, new Vector2(3f, 2f));
            Material woodCut = CreateMaterial("WoodCut", new Color(0.78f, 0.52f, 0.25f), 0f, 0.34f, false, cutTexture, Vector2.one);
            Material skin = CreateMaterial("Lumberjack_Skin", new Color(0.92f, 0.61f, 0.42f), 0f, 0.52f);
            Material beard = CreateMaterial("Lumberjack_Beard", new Color(0.24f, 0.075f, 0.025f), 0f, 0.28f);
            Material shirt = CreateMaterial("Lumberjack_Shirt", new Color(0.68f, 0.055f, 0.035f), 0f, 0.32f);
            Material denim = CreateMaterial("Lumberjack_Denim", new Color(0.055f, 0.16f, 0.3f), 0f, 0.3f);
            Material leather = CreateMaterial("Lumberjack_Leather", new Color(0.11f, 0.045f, 0.02f), 0f, 0.35f);
            Material charcoal = CreateMaterial("Lumberjack_Charcoal", new Color(0.018f, 0.018f, 0.015f), 0f, 0.5f);
            Material eyeWhite = CreateMaterial("Lumberjack_EyeWhite", new Color(0.94f, 0.9f, 0.78f), 0f, 0.55f);
            Material axeSteel = CreateMaterial("Axe_Steel", new Color(0.42f, 0.5f, 0.54f), 0.82f, 0.68f);
            Material axeEdge = CreateMaterial("Axe_Edge", new Color(0.77f, 0.84f, 0.86f), 0.92f, 0.82f);
            Material fishSilver = CreateMaterial("Fish_Silver", new Color(0.34f, 0.48f, 0.42f), 0.18f, 0.62f);
            Material fishGold = CreateMaterial("Fish_Gold", new Color(0.58f, 0.38f, 0.075f), 0.12f, 0.58f);
            Material fishOlive = CreateMaterial("Fish_Olive", new Color(0.21f, 0.31f, 0.12f), 0.08f, 0.54f);
            Material fishFin = CreateMaterial("Fish_Fins", new Color(0.52f, 0.15f, 0.065f), 0f, 0.45f);
            Material rippleMaterial = CreateWaterRippleMaterial();
            Material splashMaterial = CreateWaterSplashMaterial();
            Material foliage = CreateInteractiveFoliageMaterial("Tree_Foliage", foliageTexture, new Color(0.055f, 0.25f, 0.03f), new Color(0.18f, 0.48f, 0.07f));
            Material foliageLight = CreateInteractiveFoliageMaterial("Tree_FoliageLight", foliageTexture, new Color(0.1f, 0.34f, 0.045f), new Color(0.3f, 0.62f, 0.12f));
            Material skybox = CreateSkyboxMaterial();

            GameObject[] treeStumpPrefabs =
            {
                CreateTreeStumpPrefab("TreeStump_Short", woodBark, woodCut, 1.05f, 0.62f, 0),
                CreateTreeStumpPrefab("TreeStump_Round", woodBark, woodCut, 1.18f, 0.78f, 1),
                CreateTreeStumpPrefab("TreeStump_Wide", woodBark, woodCut, 1.32f, 0.72f, 0),
                CreateTreeStumpPrefab("TreeStump_Knotted", woodBark, woodCut, 1.1f, 0.88f, 2),
                CreateTreeStumpPrefab("TreeStump_Branch", woodBark, woodCut, 1.16f, 0.82f, 3)
            };
            GameObject bushPrefab = CreateBushPrefab(foliage, foliageLight);
            GameObject smallRockPrefab = CreateSmallRockPrefab(rock, rockLight);
            GameObject axePrefab = CreateAxePrefab(woodBark, axeSteel, axeEdge);
            Texture2D axeIcon = CreateAxeIcon();
            GameObject lumberjackPrefab = CreateLumberjackPrefab(skin, beard, shirt, denim, leather, charcoal, eyeWhite, movementParticles, axePrefab);
            GameObject[] fishPrefabs =
            {
                CreateFishPrefab("Fish_Trout", fishSilver, fishFin, eyeWhite, charcoal, 0),
                CreateFishPrefab("Fish_Perch", fishGold, fishFin, eyeWhite, charcoal, 1),
                CreateFishPrefab("Fish_Pike", fishOlive, fishFin, eyeWhite, charcoal, 2)
            };
            GameObject ripplePrefab = CreateRipplePrefab(rippleMaterial);
            GameObject splashPrefab = CreateSplashPrefab(splashMaterial);
            Mesh coniferLayer = CreateConiferLayerMeshAsset();
            GameObject[] treePrefabs =
            {
                CreateTreePrefab("Tree_Small", woodBark, foliage, foliageLight, coniferLayer, 0.82f, 0),
                CreateTreePrefab("Tree_Medium", woodBark, foliage, foliageLight, coniferLayer, 1f, 1),
                CreateTreePrefab("Tree_Large", woodBark, foliage, foliageLight, coniferLayer, 1.18f, 2)
            };
            CreateScene(ground, grass, rock, rockLight, terrainBlend, pondBed, pondWater, skybox, treeStumpPrefabs, bushPrefab, smallRockPrefab, lumberjackPrefab, treePrefabs, fishPrefabs, ripplePrefab, splashPrefab, axeIcon);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Crystal Sprint project and scene generated successfully.");
        }

        [MenuItem("Tools/Crystal Sprint/Build macOS Universal")]
        public static void BuildMacOS()
        {
            GenerateGame();
            Directory.CreateDirectory(Path.GetDirectoryName(BuildPath) ?? "Builds/macOS");

            BuildPlayerOptions options = new()
            {
                scenes = new[] { ScenePath },
                locationPathName = BuildPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.CleanBuildCache
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"Crystal Sprint build result: {summary.result}; errors: {summary.totalErrors}; " +
                      $"warnings: {summary.totalWarnings}; size: {summary.totalSize} bytes; " +
                      $"duration: {summary.totalTime}.");

            if (summary.result != BuildResult.Succeeded || summary.totalErrors != 0)
            {
                throw new BuildFailedException($"macOS build failed: {summary.result}.");
            }
        }

        private static void ConfigureProject()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode3D;
            PlayerSettings.companyName = "Niklas Liebensteiner";
            PlayerSettings.productName = "Crystal Sprint";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.niklasliebensteiner.crystalsprint");
            PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone, 2);
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = true;
            QualitySettings.vSyncCount = 1;

            SerializedObject settings = new(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            SerializedProperty inputHandler = settings.FindProperty("activeInputHandler");
            if (inputHandler != null)
            {
                inputHandler.intValue = 1;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            const string vscodePath = "/Applications/Visual Studio Code.app";
            EditorPrefs.SetString("kScriptsDefaultApp", vscodePath);
            Unity.CodeEditor.CodeEditor.SetExternalScriptEditor(vscodePath);
            Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Materials");
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory("Assets/Textures");
            Directory.CreateDirectory("Assets/Meshes");
            Directory.CreateDirectory("Assets/Shaders");
            Directory.CreateDirectory("Assets/Scripts");
            Directory.CreateDirectory("Assets/Tests/PlayMode");
        }

        private static Material CreateMaterial(string name, Color color, float metallic, float smoothness, bool emission = false, Texture texture = null, Vector2? textureScale = null)
        {
            string path = $"Assets/Materials/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            material.mainTexture = texture;
            material.mainTextureScale = textureScale ?? Vector2.one;
            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.65f);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateInteractiveGrassMaterial()
        {
            const string path = "Assets/Materials/InteractiveGrass.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("CrystalSprint/InteractiveGrass");
            if (shader == null)
            {
                throw new InvalidOperationException("Interactive grass shader could not be loaded.");
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", new Color(0.055f, 0.25f, 0.025f, 1f));
            material.SetColor("_TipColor", new Color(0.31f, 0.67f, 0.095f, 1f));
            material.SetFloat("_WindStrength", 0.075f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateInteractiveFoliageMaterial(string name, Texture texture, Color baseColor, Color tipColor)
        {
            string path = $"Assets/Materials/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("CrystalSprint/InteractiveFoliage");
            if (shader == null)
            {
                throw new InvalidOperationException("Interactive foliage shader could not be loaded.");
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.SetTexture("_MainTex", texture);
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_TipColor", tipColor);
            material.SetFloat("_BendStrength", 0.44f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateTerrainBlendMaterial(Texture grassTexture, Texture rockTexture)
        {
            const string path = "Assets/Materials/MeadowRockTransition.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("CrystalSprint/TerrainBlend");
            if (shader == null)
            {
                throw new InvalidOperationException("Terrain blend shader could not be loaded.");
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.SetTexture("_GrassTex", grassTexture);
            material.SetTexture("_RockTex", rockTexture);
            material.SetColor("_GrassColor", new Color(0.2f, 0.48f, 0.11f));
            material.SetColor("_RockColor", new Color(0.48f, 0.49f, 0.45f));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreatePondWaterMaterial()
        {
            const string path = "Assets/Materials/PondWater.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("CrystalSprint/PondWater");
            if (shader == null)
            {
                throw new InvalidOperationException("Pond water shader could not be loaded.");
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor("_ShallowColor", new Color(0.08f, 0.56f, 0.47f, 0.68f));
            material.SetColor("_DeepColor", new Color(0.025f, 0.18f, 0.29f, 0.84f));
            material.SetFloat("_WaveStrength", 0.042f);
            material.SetFloat("_WaveSpeed", 1.1f);
            material.SetFloat("_ReflectionStrength", 0.78f);
            material.SetFloat("_RippleScale", 2.65f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateWaterRippleMaterial()
        {
            const string path = "Assets/Materials/WaterRipple.mat";
            Shader shader = Shader.Find("CrystalSprint/WaterRipple");
            if (shader == null)
            {
                throw new InvalidOperationException("Water ripple shader could not be loaded.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor("_Color", new Color(0.72f, 0.94f, 1f, 0.82f));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateWaterSplashMaterial()
        {
            const string path = "Assets/Materials/WaterSplash.mat";
            Shader shader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (shader != null)
            {
                material.shader = shader;
            }

            material.color = new Color(0.58f, 0.9f, 1f, 0.78f);
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", material.color);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateParticleMaterial()
        {
            const string path = "Assets/Materials/MovementDust.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (shader != null)
            {
                material.shader = shader;
            }

            material.color = Color.white;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D CreateNatureTexture(string name, Color dark, Color light, float frequency, bool addCracks)
        {
            string path = $"Assets/Textures/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            const int size = 128;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, true)
            {
                name = name,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = x / (float)size;
                    float ny = y / (float)size;
                    float broad = Mathf.PerlinNoise(nx * frequency + 2.7f, ny * frequency + 7.1f);
                    float detail = Mathf.PerlinNoise(nx * frequency * 3.2f + 11.3f, ny * frequency * 3.2f + 4.9f);
                    float value = Mathf.Clamp01(broad * 0.72f + detail * 0.28f);
                    Color color = Color.Lerp(dark, light, value);
                    if (addCracks && Mathf.Abs(detail - 0.48f) < 0.035f)
                    {
                        color *= 0.58f;
                    }

                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true, false);
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        private static Texture2D CreateWoodCutTexture()
        {
            const string path = "Assets/Textures/WoodCutTexture.asset";
            AssetDatabase.DeleteAsset(path);
            const int size = 128;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, true)
            {
                name = "WoodCutTexture",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[size * size];
            Vector2 center = new(0.5f, 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 uv = new(x / (float)(size - 1), y / (float)(size - 1));
                    float distance = Vector2.Distance(uv, center);
                    float rings = Mathf.Sin(distance * 82f + Mathf.PerlinNoise(uv.x * 6f, uv.y * 6f) * 3f) * 0.5f + 0.5f;
                    pixels[y * size + x] = Color.Lerp(new Color(0.48f, 0.24f, 0.07f), new Color(0.9f, 0.66f, 0.32f), 0.35f + rings * 0.45f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true, false);
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        private static Material CreateSkyboxMaterial()
        {
            const string path = "Assets/Materials/ForestSkybox.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_SkyTint", new Color(0.38f, 0.62f, 0.83f));
            material.SetColor("_GroundColor", new Color(0.24f, 0.32f, 0.2f));
            material.SetFloat("_AtmosphereThickness", 0.84f);
            material.SetFloat("_SunSize", 0.038f);
            material.SetFloat("_Exposure", 1.24f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateTreeStumpPrefab(string name, Material barkMaterial, Material cutMaterial, float width, float height, int branchStyle)
        {
            GameObject source = new(name);
            BoxCollider collider = source.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, height * 0.5f, 0f);
            collider.size = new Vector3(width * 0.82f, height, width * 0.82f);
            SurfaceMarker surface = source.AddComponent<SurfaceMarker>();
            surface.Configure(SurfaceType.Wood);
            CreateStumpPart("Bark", source.transform, PrimitiveType.Cylinder, new Vector3(width * 0.5f, height * 0.5f, width * 0.5f), new Vector3(0f, height * 0.5f, 0f), Quaternion.identity, barkMaterial);
            CreateStumpPart("Cut Surface", source.transform, PrimitiveType.Cylinder, new Vector3(width * 0.43f, 0.025f, width * 0.43f), new Vector3(0f, height + 0.015f, 0f), Quaternion.Euler(0f, branchStyle * 17f, 0f), cutMaterial);
            CreateStumpPart("Root A", source.transform, PrimitiveType.Sphere, new Vector3(width * 0.32f, 0.12f, width * 0.22f), new Vector3(width * 0.26f, 0.1f, 0f), Quaternion.identity, barkMaterial);
            CreateStumpPart("Root B", source.transform, PrimitiveType.Sphere, new Vector3(width * 0.25f, 0.1f, width * 0.32f), new Vector3(-width * 0.18f, 0.08f, width * 0.2f), Quaternion.identity, barkMaterial);
            CreateStumpPart("Bark Bulge", source.transform, PrimitiveType.Sphere, new Vector3(width * 0.2f, height * 0.22f, width * 0.16f), new Vector3(-width * 0.42f, height * 0.34f, 0f), Quaternion.identity, barkMaterial);

            if (branchStyle > 0)
            {
                float direction = branchStyle == 2 ? -1f : 1f;
                float branchY = height * (branchStyle == 3 ? 0.48f : 0.58f);
                CreateStumpPart("Side Branch", source.transform, PrimitiveType.Cylinder, new Vector3(width * 0.15f, 0.22f, width * 0.15f), new Vector3(direction * width * 0.43f, branchY, 0f), Quaternion.Euler(0f, 0f, -direction * 54f), barkMaterial);
                CreateStumpPart("Branch Cut", source.transform, PrimitiveType.Cylinder, new Vector3(width * 0.105f, 0.018f, width * 0.105f), new Vector3(direction * width * 0.6f, branchY + 0.18f, 0f), Quaternion.Euler(0f, 0f, -direction * 54f), cutMaterial);
            }

            if (branchStyle == 3)
            {
                CreateStumpPart("Rear Twig", source.transform, PrimitiveType.Cylinder, new Vector3(width * 0.11f, 0.16f, width * 0.11f), new Vector3(0f, height * 0.4f, -width * 0.44f), Quaternion.Euler(48f, 0f, 0f), barkMaterial);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, $"Assets/Prefabs/{name}.prefab");
            UnityEngine.Object.DestroyImmediate(source);
            return prefab;
        }

        private static void CreateStumpPart(string name, Transform parent, PrimitiveType primitive, Vector3 scale, Vector3 position, Quaternion rotation, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.SetLocalPositionAndRotation(position, rotation);
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
        }

        private static GameObject CreateAxePrefab(Material handleMaterial, Material steelMaterial, Material edgeMaterial)
        {
            GameObject source = new("LumberjackAxe");
            CreateRoundedPart("Curved Wooden Handle", source.transform, PrimitiveType.Capsule, new Vector3(0.065f, 0.54f, 0.065f), new Vector3(0f, 0.5f, 0f), Quaternion.Euler(0f, 0f, -4f), handleMaterial);
            CreateRoundedPart("Handle Grip", source.transform, PrimitiveType.Capsule, new Vector3(0.082f, 0.18f, 0.082f), new Vector3(0.04f, 0.08f, 0f), Quaternion.Euler(0f, 0f, -8f), handleMaterial);
            Mesh axeHeadMesh = CreateAxeHeadMeshAsset();
            CreateMeshPart("Forged Axe Head", source.transform, axeHeadMesh, Vector3.one, new Vector3(-0.1f, 1.05f, 0f), Quaternion.identity, steelMaterial);
            CreateRoundedPart("Polished Cutting Edge", source.transform, PrimitiveType.Capsule, new Vector3(0.035f, 0.24f, 0.11f), new Vector3(-0.42f, 1.05f, 0f), Quaternion.Euler(0f, 0f, 9f), edgeMaterial);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, "Assets/Prefabs/LumberjackAxe.prefab");
            UnityEngine.Object.DestroyImmediate(source);
            return prefab;
        }

        private static Mesh CreateAxeHeadMeshAsset()
        {
            const string path = "Assets/Meshes/AxeHead.asset";
            AssetDatabase.DeleteAsset(path);
            Vector2[] profile =
            {
                new(-0.39f, -0.25f), new(-0.08f, -0.18f), new(0.22f, -0.1f),
                new(0.24f, 0.12f), new(-0.05f, 0.18f), new(-0.35f, 0.28f)
            };
            Vector3[] vertices = new Vector3[profile.Length * 2];
            for (int side = 0; side < 2; side++)
            {
                float z = side == 0 ? -0.105f : 0.105f;
                for (int index = 0; index < profile.Length; index++)
                {
                    vertices[side * profile.Length + index] = new Vector3(profile[index].x, profile[index].y, z);
                }
            }

            List<int> triangles = new();
            for (int index = 1; index < profile.Length - 1; index++)
            {
                triangles.Add(0); triangles.Add(index + 1); triangles.Add(index);
                triangles.Add(profile.Length); triangles.Add(profile.Length + index); triangles.Add(profile.Length + index + 1);
            }
            for (int index = 0; index < profile.Length; index++)
            {
                int next = (index + 1) % profile.Length;
                triangles.Add(index); triangles.Add(next); triangles.Add(profile.Length + index);
                triangles.Add(next); triangles.Add(profile.Length + next); triangles.Add(profile.Length + index);
            }

            Mesh mesh = new() { name = "AxeHead" };
            mesh.vertices = vertices;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Texture2D CreateAxeIcon()
        {
            const int size = 128;
            const string path = "Assets/Textures/AxeInventoryIcon.asset";
            AssetDatabase.DeleteAsset(path);
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false) { name = "AxeInventoryIcon", filterMode = FilterMode.Bilinear };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new(x - 64f, y - 64f);
                    float handleDistance = Mathf.Abs(Vector2.Dot(p - new Vector2(4f, -3f), new Vector2(0.82f, -0.57f)));
                    float handleAlong = Vector2.Dot(p - new Vector2(4f, -3f), new Vector2(0.57f, 0.82f));
                    bool handle = handleDistance < 5.5f && handleAlong > -47f && handleAlong < 42f;
                    Vector2 headCenter = new(29f, 27f);
                    bool head = x > 67 && x < 113 && y > 75 && y < 105 && Mathf.Abs((y - headCenter.y - 64f) * 0.45f) < (113 - x);
                    pixels[y * size + x] = handle
                        ? new Color(0.5f, 0.25f, 0.08f, 1f)
                        : head ? new Color(0.76f, 0.84f, 0.87f, 1f) : Color.clear;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        private static GameObject CreateFishPrefab(string name, Material bodyMaterial, Material finMaterial, Material eyeWhiteMaterial, Material pupilMaterial, int variant)
        {
            GameObject source = new(name);
            Vector3 bodyScale = variant switch
            {
                0 => new Vector3(0.38f, 0.3f, 0.82f),
                1 => new Vector3(0.43f, 0.46f, 0.68f),
                _ => new Vector3(0.29f, 0.25f, 1.02f)
            };
            CreateRoundedPart("Body", source.transform, PrimitiveType.Sphere, bodyScale, Vector3.zero, Quaternion.identity, bodyMaterial);
            float nose = bodyScale.z * 0.8f;
            CreateRoundedPart(variant == 2 ? "Pointed Snout" : "Muzzle", source.transform, PrimitiveType.Sphere,
                variant == 2 ? new Vector3(0.19f, 0.17f, 0.36f) : new Vector3(0.25f, 0.2f, 0.28f),
                new Vector3(0f, 0f, nose), Quaternion.identity, bodyMaterial);
            Mesh finMesh = GetFishFinMeshAsset();
            float tailScale = variant == 2 ? 0.48f : variant == 1 ? 0.38f : 0.43f;
            CreateMeshPart("Forked Tail", source.transform, finMesh, new Vector3(tailScale, tailScale, tailScale), new Vector3(0f, 0f, -bodyScale.z * 0.92f), Quaternion.Euler(0f, 180f, 0f), finMaterial);
            CreateMeshPart("Dorsal Fin", source.transform, finMesh, new Vector3(variant == 1 ? 0.34f : 0.23f, variant == 1 ? 0.5f : 0.28f, 0.25f), new Vector3(0f, bodyScale.y * 0.78f, -0.08f), Quaternion.Euler(90f, 0f, 0f), finMaterial);
            CreateMeshPart("Left Fin", source.transform, finMesh, new Vector3(0.21f, 0.18f, 0.2f), new Vector3(-bodyScale.x * 0.8f, -0.03f, 0.12f), Quaternion.Euler(0f, -74f, 18f), finMaterial);
            CreateMeshPart("Right Fin", source.transform, finMesh, new Vector3(0.21f, 0.18f, 0.2f), new Vector3(bodyScale.x * 0.8f, -0.03f, 0.12f), Quaternion.Euler(0f, 74f, -18f), finMaterial);
            Vector3 leftEyePosition = new(-bodyScale.x * 0.79f, bodyScale.y * 0.38f, nose * 0.78f);
            Vector3 rightEyePosition = new(bodyScale.x * 0.79f, bodyScale.y * 0.38f, nose * 0.78f);
            CreateRoundedPart("Left Eye White", source.transform, PrimitiveType.Sphere, new Vector3(0.075f, 0.075f, 0.06f), leftEyePosition, Quaternion.identity, eyeWhiteMaterial);
            CreateRoundedPart("Right Eye White", source.transform, PrimitiveType.Sphere, new Vector3(0.075f, 0.075f, 0.06f), rightEyePosition, Quaternion.identity, eyeWhiteMaterial);
            CreateRoundedPart("Left Pupil", source.transform, PrimitiveType.Sphere, new Vector3(0.035f, 0.04f, 0.035f), leftEyePosition + new Vector3(-0.055f, 0f, 0.02f), Quaternion.identity, pupilMaterial);
            CreateRoundedPart("Right Pupil", source.transform, PrimitiveType.Sphere, new Vector3(0.035f, 0.04f, 0.035f), rightEyePosition + new Vector3(0.055f, 0f, 0.02f), Quaternion.identity, pupilMaterial);
            if (variant == 0)
            {
                for (int index = 0; index < 3; index++)
                {
                    float z = -0.28f + index * 0.28f;
                    CreateRoundedPart($"Trout Spot {index + 1}", source.transform, PrimitiveType.Sphere, new Vector3(0.025f, 0.055f, 0.075f), new Vector3(-bodyScale.x * 0.98f, 0.06f - index * 0.025f, z), Quaternion.identity, pupilMaterial);
                }
            }
            else if (variant == 1)
            {
                for (int index = 0; index < 3; index++)
                {
                    float z = -0.3f + index * 0.29f;
                    CreateRoundedPart($"Perch Stripe {index + 1}", source.transform, PrimitiveType.Capsule, new Vector3(0.026f, 0.22f, 0.055f), new Vector3(-bodyScale.x * 0.98f, 0.02f, z), Quaternion.Euler(0f, 0f, 8f - index * 7f), pupilMaterial);
                }
            }
            else
            {
                CreateMeshPart("Rear Dorsal Fin", source.transform, finMesh, new Vector3(0.19f, 0.25f, 0.22f), new Vector3(0f, bodyScale.y * 0.76f, -0.48f), Quaternion.Euler(90f, 0f, 0f), finMaterial);
            }
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, $"Assets/Prefabs/{name}.prefab");
            UnityEngine.Object.DestroyImmediate(source);
            return prefab;
        }

        private static Mesh GetFishFinMeshAsset()
        {
            const string path = "Assets/Meshes/FishFin.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool create = mesh == null;
            mesh ??= new Mesh { name = "FishFin" };
            mesh.Clear();
            mesh.vertices = new[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(-1f, 0.78f, 0f),
                new Vector3(-0.62f, 0f, 0f), new Vector3(-1f, -0.78f, 0f),
                new Vector3(0f, 0f, 0.025f), new Vector3(-1f, 0.78f, 0.025f),
                new Vector3(-0.62f, 0f, 0.025f), new Vector3(-1f, -0.78f, 0.025f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 4, 6, 5, 4, 7, 6 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (create)
            {
                AssetDatabase.CreateAsset(mesh, path);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
            }
            return mesh;
        }

        private static GameObject CreateRipplePrefab(Material material)
        {
            GameObject source = new("WaterRipple");
            MeshFilter filter = source.AddComponent<MeshFilter>();
            filter.sharedMesh = CreateRingMeshAsset();
            source.AddComponent<MeshRenderer>().sharedMaterial = material;
            source.AddComponent<WaterRippleEffect>().Configure(1.35f, 3.2f);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, "Assets/Prefabs/WaterRipple.prefab");
            UnityEngine.Object.DestroyImmediate(source);
            return prefab;
        }

        private static Mesh CreateRingMeshAsset()
        {
            const string path = "Assets/Meshes/WaterRippleRing.asset";
            AssetDatabase.DeleteAsset(path);
            const int segments = 48;
            Vector3[] vertices = new Vector3[segments * 2];
            int[] triangles = new int[segments * 6];
            for (int index = 0; index < segments; index++)
            {
                float angle = index / (float)segments * Mathf.PI * 2f;
                vertices[index * 2] = new Vector3(Mathf.Cos(angle) * 0.42f, 0f, Mathf.Sin(angle) * 0.42f);
                vertices[index * 2 + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, 0f, Mathf.Sin(angle) * 0.5f);
                int next = (index + 1) % segments;
                int t = index * 6;
                triangles[t] = index * 2; triangles[t + 1] = next * 2 + 1; triangles[t + 2] = index * 2 + 1;
                triangles[t + 3] = index * 2; triangles[t + 4] = next * 2; triangles[t + 5] = next * 2 + 1;
            }
            Mesh mesh = new() { name = "WaterRippleRing", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static GameObject CreateSplashPrefab(Material material)
        {
            GameObject source = new("WaterSplash");
            ParticleSystem particles = source.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = true;
            main.duration = 0.45f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.38f, 0.72f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.3f, 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.65f, 0.92f, 1f, 0.88f), new Color(0.31f, 0.72f, 0.9f, 0.66f));
            main.gravityModifier = 1.05f;
            main.stopAction = ParticleSystemStopAction.Destroy;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18, 28) });
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 34f;
            shape.radius = 0.18f;
            ParticleSystemRenderer renderer = source.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, "Assets/Prefabs/WaterSplash.prefab");
            UnityEngine.Object.DestroyImmediate(source);
            return prefab;
        }

        private static GameObject CreateLumberjackPrefab(
            Material skin,
            Material beard,
            Material shirt,
            Material denim,
            Material leather,
            Material charcoal,
            Material eyeWhite,
            Material movementParticleMaterial,
            GameObject axePrefab)
        {
            GameObject source = new("LumberjackPlayer");
            CharacterController controller = source.AddComponent<CharacterController>();
            controller.height = 2.1f;
            controller.radius = 0.52f;
            controller.center = Vector3.zero;
            controller.stepOffset = 0.35f;
            controller.slopeLimit = 50f;
            controller.skinWidth = 0.06f;
            controller.minMoveDistance = 0f;
            source.AddComponent<PlayerController>();

            GameObject visualRoot = new("Visual");
            visualRoot.transform.SetParent(source.transform, false);

            CreateRoundedPart("Torso", visualRoot.transform, PrimitiveType.Capsule, new Vector3(0.72f, 0.5f, 0.52f), new Vector3(0f, -0.02f, 0f), Quaternion.identity, shirt);
            CreateRoundedPart("Belt", visualRoot.transform, PrimitiveType.Cylinder, new Vector3(0.38f, 0.045f, 0.29f), new Vector3(0f, -0.34f, 0f), Quaternion.identity, leather);
            CreateRoundedPart("Buckle", visualRoot.transform, PrimitiveType.Sphere, new Vector3(0.09f, 0.07f, 0.045f), new Vector3(0f, -0.34f, 0.15f), Quaternion.identity, charcoal);
            CreateRoundedPart("Left Suspender", visualRoot.transform, PrimitiveType.Capsule, new Vector3(0.055f, 0.34f, 0.035f), new Vector3(-0.21f, 0.015f, 0.2f), Quaternion.identity, denim);
            CreateRoundedPart("Right Suspender", visualRoot.transform, PrimitiveType.Capsule, new Vector3(0.055f, 0.34f, 0.035f), new Vector3(0.21f, 0.015f, 0.2f), Quaternion.identity, denim);
            CreateRoundedPart("Left Button", visualRoot.transform, PrimitiveType.Sphere, new Vector3(0.045f, 0.045f, 0.025f), new Vector3(-0.21f, 0.25f, 0.215f), Quaternion.identity, charcoal);
            CreateRoundedPart("Right Button", visualRoot.transform, PrimitiveType.Sphere, new Vector3(0.045f, 0.045f, 0.025f), new Vector3(0.21f, 0.25f, 0.215f), Quaternion.identity, charcoal);

            GameObject leftLeg = CreateRoundedPart("Left Leg", visualRoot.transform, PrimitiveType.Capsule, new Vector3(0.22f, 0.35f, 0.23f), new Vector3(-0.2f, -0.67f, 0f), Quaternion.identity, denim);
            GameObject rightLeg = CreateRoundedPart("Right Leg", visualRoot.transform, PrimitiveType.Capsule, new Vector3(0.22f, 0.35f, 0.23f), new Vector3(0.2f, -0.67f, 0f), Quaternion.identity, denim);
            CreateRoundedPart("Left Boot", leftLeg.transform, PrimitiveType.Sphere, new Vector3(1.18f, 0.48f, 1.58f), new Vector3(0f, -0.78f, 0.28f), Quaternion.identity, leather);
            CreateRoundedPart("Right Boot", rightLeg.transform, PrimitiveType.Sphere, new Vector3(1.18f, 0.48f, 1.58f), new Vector3(0f, -0.78f, 0.28f), Quaternion.identity, leather);

            GameObject leftArm = new("Free Arm Pivot");
            leftArm.transform.SetParent(visualRoot.transform, false);
            leftArm.transform.SetLocalPositionAndRotation(new Vector3(-0.52f, 0.34f, 0f), Quaternion.Euler(0f, 0f, -10f));
            CreateRoundedPart("Free Arm", leftArm.transform, PrimitiveType.Capsule, new Vector3(0.19f, 0.34f, 0.19f), new Vector3(0f, -0.34f, 0f), Quaternion.identity, shirt);
            CreateRoundedPart("Free Hand", leftArm.transform, PrimitiveType.Sphere, new Vector3(0.17f, 0.17f, 0.17f), new Vector3(0f, -0.72f, 0f), Quaternion.identity, skin);

            GameObject rightArm = new("Right Arm Pivot");
            rightArm.transform.SetParent(visualRoot.transform, false);
            rightArm.transform.SetLocalPositionAndRotation(new Vector3(0.52f, 0.34f, 0f), Quaternion.Euler(0f, 0f, 10f));
            CreateRoundedPart("Right Arm", rightArm.transform, PrimitiveType.Capsule, new Vector3(0.19f, 0.34f, 0.19f), new Vector3(0f, -0.34f, 0f), Quaternion.identity, shirt);
            CreateRoundedPart("Right Hand", rightArm.transform, PrimitiveType.Sphere, new Vector3(0.17f, 0.17f, 0.17f), new Vector3(0f, -0.72f, 0f), Quaternion.identity, skin);
            GameObject axeGrip = new("Axe Grip");
            axeGrip.transform.SetParent(rightArm.transform, false);
            axeGrip.transform.SetLocalPositionAndRotation(new Vector3(0f, -0.72f, 0.035f), Quaternion.Euler(12f, 0f, -8f));
            GameObject heldAxe = (GameObject)PrefabUtility.InstantiatePrefab(axePrefab);
            heldAxe.name = "Held Axe";
            heldAxe.transform.SetParent(axeGrip.transform, false);
            heldAxe.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 0f, -16f));
            heldAxe.transform.localScale = Vector3.one * 0.76f;

            GameObject headRig = new("Head Rig");
            headRig.transform.SetParent(visualRoot.transform, false);
            headRig.transform.localPosition = new Vector3(0f, 0.63f, 0.04f);
            CreateRoundedPart("Head", headRig.transform, PrimitiveType.Sphere, new Vector3(0.68f, 0.7f, 0.64f), Vector3.zero, Quaternion.identity, skin);
            CreateRoundedPart("Left Ear", headRig.transform, PrimitiveType.Sphere, new Vector3(0.15f, 0.18f, 0.12f), new Vector3(-0.33f, 0f, 0f), Quaternion.identity, skin);
            CreateRoundedPart("Right Ear", headRig.transform, PrimitiveType.Sphere, new Vector3(0.15f, 0.18f, 0.12f), new Vector3(0.33f, 0f, 0f), Quaternion.identity, skin);
            CreateRoundedPart("Beard Center", headRig.transform, PrimitiveType.Sphere, new Vector3(0.42f, 0.34f, 0.3f), new Vector3(0f, -0.17f, 0.2f), Quaternion.identity, beard);
            CreateRoundedPart("Beard Left", headRig.transform, PrimitiveType.Sphere, new Vector3(0.28f, 0.3f, 0.26f), new Vector3(-0.2f, -0.18f, 0.17f), Quaternion.Euler(0f, 0f, -10f), beard);
            CreateRoundedPart("Beard Right", headRig.transform, PrimitiveType.Sphere, new Vector3(0.28f, 0.3f, 0.26f), new Vector3(0.2f, -0.18f, 0.17f), Quaternion.Euler(0f, 0f, 10f), beard);
            CreateRoundedPart("Beard Chin", headRig.transform, PrimitiveType.Sphere, new Vector3(0.3f, 0.28f, 0.25f), new Vector3(0f, -0.35f, 0.13f), Quaternion.identity, beard);
            CreateRoundedPart("Nose", headRig.transform, PrimitiveType.Sphere, new Vector3(0.14f, 0.16f, 0.15f), new Vector3(0f, 0.01f, 0.28f), Quaternion.identity, skin);
            CreateRoundedPart("Left Eye White", headRig.transform, PrimitiveType.Sphere, new Vector3(0.13f, 0.1f, 0.035f), new Vector3(-0.12f, 0.1f, 0.275f), Quaternion.identity, eyeWhite);
            CreateRoundedPart("Right Eye White", headRig.transform, PrimitiveType.Sphere, new Vector3(0.13f, 0.1f, 0.035f), new Vector3(0.12f, 0.1f, 0.275f), Quaternion.identity, eyeWhite);
            CreateRoundedPart("Left Pupil", headRig.transform, PrimitiveType.Sphere, new Vector3(0.05f, 0.06f, 0.018f), new Vector3(-0.12f, 0.1f, 0.296f), Quaternion.identity, charcoal);
            CreateRoundedPart("Right Pupil", headRig.transform, PrimitiveType.Sphere, new Vector3(0.05f, 0.06f, 0.018f), new Vector3(0.12f, 0.1f, 0.296f), Quaternion.identity, charcoal);
            CreateRoundedPart("Left Eyebrow", headRig.transform, PrimitiveType.Sphere, new Vector3(0.16f, 0.04f, 0.025f), new Vector3(-0.12f, 0.19f, 0.27f), Quaternion.Euler(0f, 0f, -8f), beard);
            CreateRoundedPart("Right Eyebrow", headRig.transform, PrimitiveType.Sphere, new Vector3(0.16f, 0.04f, 0.025f), new Vector3(0.12f, 0.19f, 0.27f), Quaternion.Euler(0f, 0f, 8f), beard);
            CreateRoundedPart("Left Moustache", headRig.transform, PrimitiveType.Sphere, new Vector3(0.19f, 0.08f, 0.08f), new Vector3(-0.08f, -0.06f, 0.29f), Quaternion.Euler(0f, 0f, -12f), beard);
            CreateRoundedPart("Right Moustache", headRig.transform, PrimitiveType.Sphere, new Vector3(0.19f, 0.08f, 0.08f), new Vector3(0.08f, -0.06f, 0.29f), Quaternion.Euler(0f, 0f, 12f), beard);
            CreateRoundedPart("Hat Brim", headRig.transform, PrimitiveType.Cylinder, new Vector3(0.75f, 0.045f, 0.7f), new Vector3(0f, 0.3f, 0f), Quaternion.identity, shirt);
            CreateRoundedPart("Hat Crown", headRig.transform, PrimitiveType.Capsule, new Vector3(0.5f, 0.18f, 0.46f), new Vector3(0f, 0.43f, -0.015f), Quaternion.identity, shirt);
            CreateRoundedPart("Hat Band", headRig.transform, PrimitiveType.Cylinder, new Vector3(0.52f, 0.035f, 0.48f), new Vector3(0f, 0.31f, -0.01f), Quaternion.identity, leather);

            LumberjackVisual animation = source.AddComponent<LumberjackVisual>();
            animation.Configure(visualRoot.transform, leftLeg.transform, rightLeg.transform, leftArm.transform, rightArm.transform, axeGrip.transform);
            LumberjackEquipment equipment = source.AddComponent<LumberjackEquipment>();
            equipment.Configure(animation, heldAxe);
            CreateFootstepParticles(source, movementParticleMaterial);
            source.AddComponent<InteractiveGrass>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, "Assets/Prefabs/LumberjackPlayer.prefab");
            UnityEngine.Object.DestroyImmediate(source);
            return prefab;
        }

        private static void CreateFootstepParticles(GameObject player, Material material)
        {
            GameObject particleObject = new("Surface Movement Particles");
            particleObject.transform.SetParent(player.transform, false);
            particleObject.transform.localPosition = new Vector3(0f, -0.98f, -0.28f);
            ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.4f;
            main.startSpeed = 0.5f;
            main.startSize = 0.08f;
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.12f;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = false;
            ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;

            GroundMovementParticles movementParticles = player.AddComponent<GroundMovementParticles>();
            movementParticles.Configure(particles);
        }

        private static Mesh CreateConiferLayerMeshAsset()
        {
            const string path = "Assets/Meshes/ConiferFoliageLayer.asset";
            AssetDatabase.DeleteAsset(path);
            const int sides = 16;
            Vector3[] vertices = new Vector3[sides + 2];
            Vector2[] uvs = new Vector2[vertices.Length];
            for (int index = 0; index < sides; index++)
            {
                float angle = index / (float)sides * Mathf.PI * 2f;
                float irregularity = 1f + Mathf.Sin(index * 2.37f) * 0.045f;
                vertices[index] = new Vector3(Mathf.Cos(angle) * irregularity, 0f, Mathf.Sin(angle) * irregularity);
                uvs[index] = new Vector2(index / (float)sides, 0f);
            }

            int tip = sides;
            int bottom = sides + 1;
            vertices[tip] = new Vector3(0f, 1f, 0f);
            vertices[bottom] = Vector3.zero;
            uvs[tip] = new Vector2(0.5f, 1f);
            uvs[bottom] = new Vector2(0.5f, 0.5f);
            int[] triangles = new int[sides * 6];
            for (int index = 0; index < sides; index++)
            {
                int next = (index + 1) % sides;
                int triangle = index * 6;
                triangles[triangle] = index;
                triangles[triangle + 1] = tip;
                triangles[triangle + 2] = next;
                triangles[triangle + 3] = next;
                triangles[triangle + 4] = bottom;
                triangles[triangle + 5] = index;
            }

            Mesh mesh = new() { name = "ConiferFoliageLayer" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static GameObject CreateTreePrefab(string name, Material bark, Material foliage, Material foliageLight, Mesh coniferLayer, float size, int variant)
        {
            GameObject source = new(name);
            CapsuleCollider trunkCollider = source.AddComponent<CapsuleCollider>();
            trunkCollider.radius = 0.48f * size;
            trunkCollider.height = 3.1f * size;
            trunkCollider.center = new Vector3(0f, 1.55f * size, 0f);
            CreateRoundedPart("Trunk", source.transform, PrimitiveType.Cylinder, new Vector3(0.46f * size, 1.55f * size, 0.46f * size), new Vector3(0f, 1.55f * size, 0f), Quaternion.identity, bark);
            CreateRoundedPart("Root A", source.transform, PrimitiveType.Sphere, new Vector3(0.55f * size, 0.18f * size, 0.32f * size), new Vector3(0.32f * size, 0.13f * size, 0f), Quaternion.identity, bark);
            CreateRoundedPart("Root B", source.transform, PrimitiveType.Sphere, new Vector3(0.36f * size, 0.15f * size, 0.5f * size), new Vector3(-0.22f * size, 0.11f * size, 0.25f * size), Quaternion.identity, bark);
            float lean = (variant - 1) * 0.07f * size;
            CreateMeshPart("Foliage Layer Lower", source.transform, coniferLayer, new Vector3(1.9f * size, 2.15f * size, 1.82f * size), new Vector3(0f, 1.35f * size, 0f), Quaternion.Euler(0f, variant * 19f, 0f), foliage);
            CreateMeshPart("Foliage Layer Middle", source.transform, coniferLayer, new Vector3(1.55f * size, 2.0f * size, 1.48f * size), new Vector3(-lean, 2.2f * size, lean), Quaternion.Euler(0f, 31f + variant * 23f, 0f), foliageLight);
            CreateMeshPart("Foliage Layer Upper", source.transform, coniferLayer, new Vector3(1.2f * size, 1.78f * size, 1.15f * size), new Vector3(lean, 3.02f * size, 0f), Quaternion.Euler(0f, 58f + variant * 17f, 0f), foliage);
            CreateMeshPart("Foliage Crown", source.transform, coniferLayer, new Vector3(0.82f * size, 1.55f * size, 0.78f * size), new Vector3(0f, 3.76f * size, -lean), Quaternion.Euler(0f, 12f + variant * 29f, 0f), foliageLight);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, $"Assets/Prefabs/{name}.prefab");
            UnityEngine.Object.DestroyImmediate(source);
            return prefab;
        }

        private static GameObject CreateMeshPart(string name, Transform parent, Mesh mesh, Vector3 scale, Vector3 position, Quaternion rotation, Material material)
        {
            GameObject part = new(name);
            part.transform.SetParent(parent, false);
            part.transform.SetLocalPositionAndRotation(position, rotation);
            part.transform.localScale = scale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterial = material;
            return part;
        }

        private static GameObject CreateRoundedPart(string name, Transform parent, PrimitiveType primitive, Vector3 scale, Vector3 position, Quaternion rotation, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.SetLocalPositionAndRotation(position, rotation);
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            return part;
        }

        private static GameObject CreateBushPrefab(Material foliage, Material foliageLight)
        {
            GameObject source = new("ForestBush");
            CreateRoundedPart("Bush Center", source.transform, PrimitiveType.Sphere, new Vector3(0.72f, 0.48f, 0.62f), new Vector3(0f, 0.24f, 0f), Quaternion.identity, foliage);
            CreateRoundedPart("Bush Left", source.transform, PrimitiveType.Sphere, new Vector3(0.5f, 0.4f, 0.48f), new Vector3(-0.42f, 0.2f, 0.06f), Quaternion.identity, foliageLight);
            CreateRoundedPart("Bush Right", source.transform, PrimitiveType.Sphere, new Vector3(0.52f, 0.42f, 0.5f), new Vector3(0.42f, 0.21f, -0.04f), Quaternion.identity, foliage);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, "Assets/Prefabs/ForestBush.prefab");
            UnityEngine.Object.DestroyImmediate(source);
            return prefab;
        }

        private static GameObject CreateSmallRockPrefab(Material rock, Material rockLight)
        {
            GameObject source = new("SmallRockCluster");
            CreateRoundedPart("Rock Main", source.transform, PrimitiveType.Sphere, new Vector3(0.6f, 0.32f, 0.5f), new Vector3(0f, 0.16f, 0f), Quaternion.Euler(0f, 17f, 0f), rock);
            CreateRoundedPart("Rock Accent", source.transform, PrimitiveType.Sphere, new Vector3(0.32f, 0.22f, 0.28f), new Vector3(0.4f, 0.11f, 0.08f), Quaternion.Euler(0f, -23f, 0f), rockLight);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, "Assets/Prefabs/SmallRockCluster.prefab");
            UnityEngine.Object.DestroyImmediate(source);
            return prefab;
        }

        private static void CreateScene(
            Material groundMaterial,
            Material grassMaterial,
            Material rockMaterial,
            Material rockLightMaterial,
            Material terrainBlendMaterial,
            Material pondBedMaterial,
            Material pondWaterMaterial,
            Material skyboxMaterial,
            GameObject[] treeStumpPrefabs,
            GameObject bushPrefab,
            GameObject smallRockPrefab,
            GameObject lumberjackPrefab,
            GameObject[] treePrefabs,
            GameObject[] fishPrefabs,
            GameObject ripplePrefab,
            GameObject splashPrefab,
            Texture2D axeIcon)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject environment = new("Environment");
            CreateGroundTerrain(groundMaterial, environment.transform);
            CreateGrassField(grassMaterial, environment.transform);
            CreateMountainBoundary(rockMaterial, rockLightMaterial, environment.transform);
            CreateTerrainTransition(terrainBlendMaterial, environment.transform);
            CreatePond(pondBedMaterial, pondWaterMaterial, rockMaterial, rockLightMaterial, environment.transform);

            GameObject obstacles = new("Obstacles");
            CreateStumpObstacle(treeStumpPrefabs, "West Stump Group", new Vector3(-17f, 0.7f, -11f), new Vector3(7f, 1.4f, 4.8f), obstacles.transform, 0);
            CreateStumpObstacle(treeStumpPrefabs, "Central Tall Stump", new Vector3(16f, 1.15f, -6f), new Vector3(4.5f, 2.3f, 4.5f), obstacles.transform, 2);
            CreateStumpObstacle(treeStumpPrefabs, "North Stump Barrier", new Vector3(1f, 0.85f, 17f), new Vector3(9f, 1.7f, 3.4f), obstacles.transform, 1);
            CreateStumpObstacle(treeStumpPrefabs, "West Landmark", new Vector3(-28f, 1.35f, 20f), new Vector3(4.2f, 2.7f, 4.2f), obstacles.transform, 3);
            CreateStumpObstacle(treeStumpPrefabs, "East Stump Group", new Vector3(27f, 0.8f, -22f), new Vector3(7.5f, 1.6f, 4.5f), obstacles.transform, 4);

            CreateNaturalDetails(bushPrefab, smallRockPrefab);
            CreateTrees(treePrefabs);

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(lumberjackPrefab);
            player.name = "Player";
            player.transform.position = new Vector3(0f, SampleGroundHeight(0f, -36f) + 1.05f, -36f);

            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 6f, -48f), Quaternion.Euler(20f, 0f, 0f));
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 240f;
            cameraObject.AddComponent<AudioListener>();
            ThirdPersonCamera thirdPersonCamera = cameraObject.AddComponent<ThirdPersonCamera>();
            thirdPersonCamera.Target = player.transform;

            GameObject lightObject = new("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.92f, 0.8f);
            light.intensity = 1.18f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.72f;
            light.shadowBias = 0.035f;
            light.shadowNormalBias = 0.28f;
            lightObject.transform.rotation = Quaternion.Euler(52f, -36f, 0f);

            GameObject fillLightObject = new("Sky Fill Light");
            Light fillLight = fillLightObject.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.48f, 0.67f, 0.9f);
            fillLight.intensity = 0.22f;
            fillLight.shadows = LightShadows.None;
            fillLightObject.transform.rotation = Quaternion.Euler(32f, 142f, 0f);
            RenderSettings.skybox = skyboxMaterial;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientIntensity = 1.08f;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.68f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.32f, 0.43f, 0.33f);
            RenderSettings.ambientGroundColor = new Color(0.14f, 0.21f, 0.12f);
            RenderSettings.reflectionIntensity = 1.15f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.59f, 0.72f, 0.75f);
            RenderSettings.fogDensity = 0.0072f;

            GameObject systems = new("Game Systems");
            systems.AddComponent<GameManager>();
            systems.AddComponent<CursorLockController>();
            systems.AddComponent<BuildSmokeTest>();
            FishJumpSystem fishSystem = systems.AddComponent<FishJumpSystem>();
            fishSystem.Configure(fishPrefabs, ripplePrefab, splashPrefab, SampleBaseGroundHeight(0f, 0f) - 0.25f, 7.35f);
            LumberjackEquipment equipment = player.GetComponent<LumberjackEquipment>();
            CreateGameHud(equipment, axeIcon);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Could not save scene at {ScenePath}.");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static float SampleGroundHeight(float x, float z)
        {
            float height = SampleBaseGroundHeight(x, z);
            float pondDistance = new Vector2(x, z).magnitude;
            float pondAngle = Mathf.Atan2(z, x);
            float pondShape = PondShapeMultiplier(pondAngle);
            float pondDepression = (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4.35f * pondShape, 9.65f * pondShape, pondDistance))) * 0.78f;
            return height - pondDepression;
        }

        private static float PondShapeMultiplier(float angle) =>
            1f + Mathf.Sin(angle * 3f + 0.4f) * 0.075f + Mathf.Sin(angle * 5f - 1.1f) * 0.045f + Mathf.Sin(angle * 9f + 0.8f) * 0.022f;

        private static float SampleBaseGroundHeight(float x, float z)
        {
            float broad = (Mathf.PerlinNoise(x * 0.032f + 8.4f, z * 0.032f + 3.1f) - 0.5f) * 0.72f;
            float detail = (Mathf.PerlinNoise(x * 0.085f + 21.7f, z * 0.085f + 14.2f) - 0.5f) * 0.16f;
            float rolling = Mathf.Sin(x * 0.075f) * Mathf.Cos(z * 0.063f) * 0.12f;
            return broad + detail + rolling;
        }

        private static void CreateGroundTerrain(Material material, Transform parent)
        {
            const int resolution = 65;
            const float size = 96f;
            Vector3[] vertices = new Vector3[resolution * resolution];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
            for (int zIndex = 0; zIndex < resolution; zIndex++)
            {
                for (int xIndex = 0; xIndex < resolution; xIndex++)
                {
                    float x = -size * 0.5f + xIndex / (float)(resolution - 1) * size;
                    float z = -size * 0.5f + zIndex / (float)(resolution - 1) * size;
                    int index = zIndex * resolution + xIndex;
                    vertices[index] = new Vector3(x, SampleGroundHeight(x, z), z);
                    uvs[index] = new Vector2(xIndex / (float)(resolution - 1) * 14f, zIndex / (float)(resolution - 1) * 14f);
                }
            }

            int triangleIndex = 0;
            for (int zIndex = 0; zIndex < resolution - 1; zIndex++)
            {
                for (int xIndex = 0; xIndex < resolution - 1; xIndex++)
                {
                    int a = zIndex * resolution + xIndex;
                    int b = a + 1;
                    int c = a + resolution;
                    int d = c + 1;
                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = d;
                }
            }

            Mesh mesh = new() { name = "RollingMeadowTerrain" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            const string path = "Assets/Meshes/RollingMeadowTerrain.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);

            GameObject ground = new("Ground");
            ground.transform.SetParent(parent, false);
            ground.AddComponent<MeshFilter>().sharedMesh = mesh;
            ground.AddComponent<MeshRenderer>().sharedMaterial = material;
            ground.AddComponent<MeshCollider>().sharedMesh = mesh;
            SurfaceMarker surface = ground.AddComponent<SurfaceMarker>();
            surface.Configure(SurfaceType.Grass);
            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);
        }

        private static void CreateGrassField(Material material, Transform parent)
        {
            const int bladeCount = 40000;
            Vector3[] vertices = new Vector3[bladeCount * 3];
            Color[] colors = new Color[vertices.Length];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[vertices.Length];
            System.Random random = new(91837);
            for (int blade = 0; blade < bladeCount; blade++)
            {
                float angle;
                float radius;
                float x;
                float z;
                do
                {
                    angle = (float)random.NextDouble() * Mathf.PI * 2f;
                    radius = Mathf.Sqrt((float)random.NextDouble()) * 40.5f;
                    x = Mathf.Cos(angle) * radius;
                    z = Mathf.Sin(angle) * radius;
                }
                while (new Vector2(x, z).magnitude < 9.9f);
                float rotation = (float)random.NextDouble() * Mathf.PI * 2f;
                float width = 0.045f + (float)random.NextDouble() * 0.065f;
                float height = 0.24f + (float)random.NextDouble() * 0.34f;
                Vector3 right = new(Mathf.Cos(rotation) * width, 0f, Mathf.Sin(rotation) * width);
                Vector3 basePosition = new(x, SampleGroundHeight(x, z) + 0.012f, z);
                int index = blade * 3;
                vertices[index] = basePosition - right;
                vertices[index + 1] = basePosition + right;
                vertices[index + 2] = basePosition + new Vector3(Mathf.Sin(rotation) * height * 0.13f, height, -Mathf.Cos(rotation) * height * 0.13f);
                colors[index] = Color.black;
                colors[index + 1] = Color.black;
                colors[index + 2] = Color.white;
                uvs[index] = Vector2.zero;
                uvs[index + 1] = Vector2.right;
                uvs[index + 2] = new Vector2(0.5f, 1f);
                triangles[index] = index;
                triangles[index + 1] = index + 2;
                triangles[index + 2] = index + 1;
            }

            Mesh mesh = new() { name = "InteractiveGrassField", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Bounds bounds = mesh.bounds;
            bounds.Expand(new Vector3(2f, 2f, 2f));
            mesh.bounds = bounds;
            const string path = "Assets/Meshes/InteractiveGrassField.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);

            GameObject grass = new("Interactive Grass Field");
            grass.transform.SetParent(parent, false);
            grass.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = grass.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            GameObjectUtility.SetStaticEditorFlags(grass, StaticEditorFlags.BatchingStatic);
        }

        private static void CreateTerrainTransition(Material material, Transform parent)
        {
            const int segments = 128;
            float[] radii = { 38.4f, 39.7f, 40.7f, 41.5f, 42.8f };
            Vector3[] vertices = new Vector3[segments * radii.Length];
            Vector2[] uvs = new Vector2[vertices.Length];
            Color[] colors = new Color[vertices.Length];
            int[] triangles = new int[segments * (radii.Length - 1) * 6];

            for (int segment = 0; segment < segments; segment++)
            {
                float angle = segment / (float)segments * Mathf.PI * 2f;
                for (int ring = 0; ring < radii.Length; ring++)
                {
                    float radius = radii[ring];
                    float x = Mathf.Cos(angle) * radius;
                    float z = Mathf.Sin(angle) * radius;
                    float height = radius <= 41.5f
                        ? SampleGroundHeight(x, z) + 0.025f
                        : SampleMountainHeight(radius, angle) + 0.025f;
                    int index = segment * radii.Length + ring;
                    vertices[index] = new Vector3(x, height, z);
                    uvs[index] = new Vector2(x * 0.08f, z * 0.08f);
                    float breakup = Mathf.Sin(angle * 19f + ring * 1.7f) * 0.08f;
                    float blend = Mathf.Clamp01(ring / (float)(radii.Length - 1) + breakup);
                    colors[index] = new Color(blend, blend, blend, 1f);
                }
            }

            int triangleIndex = 0;
            for (int segment = 0; segment < segments; segment++)
            {
                int next = (segment + 1) % segments;
                for (int ring = 0; ring < radii.Length - 1; ring++)
                {
                    int a = segment * radii.Length + ring;
                    int b = next * radii.Length + ring;
                    int c = segment * radii.Length + ring + 1;
                    int d = next * radii.Length + ring + 1;
                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = d;
                    triangles[triangleIndex++] = c;
                }
            }

            Mesh mesh = new() { name = "MeadowRockTransition" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            const string path = "Assets/Meshes/MeadowRockTransition.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);

            GameObject transition = new("Meadow Rock Transition");
            transition.transform.SetParent(parent, false);
            transition.AddComponent<MeshFilter>().sharedMesh = mesh;
            transition.AddComponent<MeshRenderer>().sharedMaterial = material;
            GameObjectUtility.SetStaticEditorFlags(transition, StaticEditorFlags.BatchingStatic);
        }

        private static void CreatePond(Material bedMaterial, Material waterMaterial, Material rockMaterial, Material rockLightMaterial, Transform parent)
        {
            GameObject pond = new("Central Pond");
            pond.transform.SetParent(parent, false);

            Mesh bedMesh = CreatePondDiscMesh("PondBedMesh", 9.55f, 8, true);
            GameObject bed = new("Pond Bed");
            bed.transform.SetParent(pond.transform, false);
            bed.AddComponent<MeshFilter>().sharedMesh = bedMesh;
            bed.AddComponent<MeshRenderer>().sharedMaterial = bedMaterial;
            GameObjectUtility.SetStaticEditorFlags(bed, StaticEditorFlags.BatchingStatic);

            Mesh waterMesh = CreatePondDiscMesh("PondWaterMesh", 8.1f, 8, false);
            GameObject water = new("Animated Water Surface");
            water.transform.SetParent(pond.transform, false);
            water.transform.localPosition = new Vector3(0f, SampleBaseGroundHeight(0f, 0f) - 0.25f, 0f);
            water.AddComponent<MeshFilter>().sharedMesh = waterMesh;
            MeshRenderer waterRenderer = water.AddComponent<MeshRenderer>();
            waterRenderer.sharedMaterial = waterMaterial;
            waterRenderer.shadowCastingMode = ShadowCastingMode.Off;

            GameObject reflectionObject = new("Pond Reflection Probe");
            reflectionObject.transform.SetParent(pond.transform, false);
            reflectionObject.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            ReflectionProbe reflectionProbe = reflectionObject.AddComponent<ReflectionProbe>();
            reflectionProbe.mode = ReflectionProbeMode.Realtime;
            reflectionProbe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            reflectionProbe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
            reflectionProbe.size = new Vector3(20f, 9f, 20f);
            reflectionProbe.intensity = 1.18f;
            reflectionProbe.resolution = 128;

            Mesh shorelineRock = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/AngularMountainRock.asset");
            GameObject shoreline = new("Natural Shoreline");
            shoreline.transform.SetParent(pond.transform, false);
            System.Random random = new(77231);
            const int rockCount = 30;
            for (int index = 0; index < rockCount; index++)
            {
                float angle = (index / (float)rockCount + ((float)random.NextDouble() - 0.5f) * 0.035f) * Mathf.PI * 2f;
                float radius = (8.55f + (float)random.NextDouble() * 1.08f) * PondShapeMultiplier(angle);
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                GameObject rock = new($"Shore Rock {index + 1:00}");
                rock.transform.SetParent(shoreline.transform, false);
                rock.transform.SetPositionAndRotation(
                    new Vector3(x, SampleGroundHeight(x, z) - 0.025f, z),
                    Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f));
                float scale = 0.28f + (float)random.NextDouble() * 0.42f;
                rock.transform.localScale = new Vector3(scale * 1.45f, scale * 0.62f, scale);
                rock.AddComponent<MeshFilter>().sharedMesh = shorelineRock;
                rock.AddComponent<MeshRenderer>().sharedMaterial = index % 4 == 0 ? rockLightMaterial : rockMaterial;
                GameObjectUtility.SetStaticEditorFlags(rock, StaticEditorFlags.BatchingStatic);
            }
        }

        private static Mesh CreatePondDiscMesh(string name, float radius, int rings, bool conformToGround)
        {
            const int segments = 64;
            Vector3[] vertices = new Vector3[1 + segments * rings];
            Vector2[] uvs = new Vector2[vertices.Length];
            vertices[0] = conformToGround ? new Vector3(0f, SampleGroundHeight(0f, 0f) + 0.018f, 0f) : Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);
            for (int ring = 1; ring <= rings; ring++)
            {
                for (int segment = 0; segment < segments; segment++)
                {
                    float angle = segment / (float)segments * Mathf.PI * 2f;
                    float ringRadius = radius * PondShapeMultiplier(angle) * ring / rings;
                    float x = Mathf.Cos(angle) * ringRadius;
                    float z = Mathf.Sin(angle) * ringRadius;
                    int index = 1 + (ring - 1) * segments + segment;
                    float y = conformToGround ? SampleGroundHeight(x, z) + 0.018f : 0f;
                    vertices[index] = new Vector3(x, y, z);
                    uvs[index] = new Vector2(x / (radius * 2f) + 0.5f, z / (radius * 2f) + 0.5f);
                }
            }

            int[] triangles = new int[segments * 3 + (rings - 1) * segments * 6];
            int triangleIndex = 0;
            for (int segment = 0; segment < segments; segment++)
            {
                int next = (segment + 1) % segments;
                triangles[triangleIndex++] = 0;
                triangles[triangleIndex++] = 1 + next;
                triangles[triangleIndex++] = 1 + segment;
            }
            for (int ring = 1; ring < rings; ring++)
            {
                int innerStart = 1 + (ring - 1) * segments;
                int outerStart = 1 + ring * segments;
                for (int segment = 0; segment < segments; segment++)
                {
                    int next = (segment + 1) % segments;
                    int a = innerStart + segment;
                    int b = innerStart + next;
                    int c = outerStart + segment;
                    int d = outerStart + next;
                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = d;
                    triangles[triangleIndex++] = c;
                }
            }

            Mesh mesh = new() { name = name };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            string path = $"Assets/Meshes/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void CreateGameHud(LumberjackEquipment equipment, Texture2D axeIcon)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject canvasObject = new("HUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            Text controls = CreateText("Controls", canvasObject.transform, font, 16, TextAnchor.UpperLeft);
            controls.text = "WASD  Bewegen   |   Shift  Sprinten   |   Leertaste  Springen\nMaus  Kamera   |   Linksklick  Axt   |   1–4  Inventar   |   Esc  Maus frei   |   R  Neustart";
            RectTransform controlsRect = controls.rectTransform;
            controlsRect.anchorMin = new Vector2(0f, 1f);
            controlsRect.anchorMax = new Vector2(0f, 1f);
            controlsRect.pivot = new Vector2(0f, 1f);
            controlsRect.anchoredPosition = new Vector2(22f, -18f);
            controlsRect.sizeDelta = new Vector2(820f, 52f);
            controls.color = new Color(1f, 1f, 1f, 0.82f);

            GameObject inventory = new("Inventory");
            inventory.transform.SetParent(canvasObject.transform, false);
            RectTransform inventoryRect = inventory.AddComponent<RectTransform>();
            inventoryRect.anchorMin = new Vector2(1f, 0f);
            inventoryRect.anchorMax = new Vector2(1f, 0f);
            inventoryRect.pivot = new Vector2(1f, 0f);
            inventoryRect.anchoredPosition = new Vector2(-28f, 30f);
            inventoryRect.sizeDelta = new Vector2(382f, 96f);

            Image[] frames = new Image[4];
            for (int index = 0; index < frames.Length; index++)
            {
                GameObject slot = new($"Inventory Slot {index + 1}");
                slot.transform.SetParent(inventory.transform, false);
                RectTransform slotRect = slot.AddComponent<RectTransform>();
                slotRect.anchorMin = new Vector2(0f, 0f);
                slotRect.anchorMax = new Vector2(0f, 0f);
                slotRect.pivot = new Vector2(0f, 0f);
                slotRect.anchoredPosition = new Vector2(index * 94f, 0f);
                slotRect.sizeDelta = new Vector2(86f, 86f);
                frames[index] = slot.AddComponent<Image>();
                frames[index].color = new Color(0.09f, 0.12f, 0.15f, 0.88f);
                Outline outline = slot.AddComponent<Outline>();
                outline.effectColor = new Color(0.72f, 0.82f, 0.86f, 0.72f);
                outline.effectDistance = new Vector2(2f, -2f);

                Text number = CreateText($"Slot Number {index + 1}", slot.transform, font, 16, TextAnchor.UpperLeft);
                number.text = (index + 1).ToString();
                number.color = new Color(1f, 1f, 1f, 0.88f);
                number.rectTransform.anchorMin = Vector2.zero;
                number.rectTransform.anchorMax = Vector2.one;
                number.rectTransform.offsetMin = new Vector2(7f, 4f);
                number.rectTransform.offsetMax = new Vector2(-4f, -4f);
            }

            GameObject iconObject = new("Axe Icon");
            iconObject.transform.SetParent(frames[0].transform, false);
            RawImage icon = iconObject.AddComponent<RawImage>();
            icon.texture = axeIcon;
            icon.color = Color.white;
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0.12f, 0.12f);
            iconRect.anchorMax = new Vector2(0.88f, 0.88f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            InventoryHud inventoryHud = inventory.AddComponent<InventoryHud>();
            inventoryHud.Configure(equipment, frames);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static Text CreateText(string name, Transform parent, Font font, int size, TextAnchor alignment)
        {
            GameObject textObject = new(name);
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent);
            box.transform.SetPositionAndRotation(position, Quaternion.identity);
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            GameObjectUtility.SetStaticEditorFlags(box, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);
            return box;
        }

        private static void CreateMountainBoundary(Material rock, Material rockLight, Transform parent)
        {
            GameObject boundary = new("Mountain Boundary");
            boundary.transform.SetParent(parent);
            const string meshPath = "Assets/Meshes/MountainRing.asset";
            AssetDatabase.DeleteAsset(meshPath);
            Mesh mesh = CreateMountainRingMesh();
            AssetDatabase.CreateAsset(mesh, meshPath);

            GameObject terrain = new("Continuous Mountain Terrain");
            terrain.transform.SetParent(boundary.transform, false);
            terrain.AddComponent<MeshFilter>().sharedMesh = mesh;
            terrain.AddComponent<MeshRenderer>().sharedMaterial = rock;
            terrain.AddComponent<MeshCollider>().sharedMesh = mesh;
            SurfaceMarker surface = terrain.AddComponent<SurfaceMarker>();
            surface.Configure(SurfaceType.Stone);
            GameObjectUtility.SetStaticEditorFlags(terrain, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);
            CreateMountainRockDetails(boundary.transform, rock, rockLight);
        }

        private static void CreateMountainRockDetails(Transform parent, Material rock, Material rockLight)
        {
            const string path = "Assets/Meshes/AngularMountainRock.asset";
            AssetDatabase.DeleteAsset(path);
            const int sides = 8;
            Vector3[] vertices = new Vector3[sides * 2 + 2];
            Vector2[] uvs = new Vector2[vertices.Length];
            for (int index = 0; index < sides; index++)
            {
                float angle = index / (float)sides * Mathf.PI * 2f;
                float lowerRadius = 0.82f + Mathf.Sin(index * 2.9f) * 0.17f;
                float upperRadius = 0.5f + Mathf.Cos(index * 1.7f) * 0.12f;
                vertices[index] = new Vector3(Mathf.Cos(angle) * lowerRadius, 0f, Mathf.Sin(angle) * lowerRadius);
                vertices[index + sides] = new Vector3(Mathf.Cos(angle) * upperRadius + 0.08f, 0.72f + Mathf.Sin(index * 1.3f) * 0.08f, Mathf.Sin(angle) * upperRadius - 0.05f);
                uvs[index] = new Vector2(index / (float)sides, 0f);
                uvs[index + sides] = new Vector2(index / (float)sides, 0.6f);
            }

            int top = sides * 2;
            int bottom = top + 1;
            vertices[top] = new Vector3(-0.08f, 1.18f, 0.06f);
            vertices[bottom] = Vector3.zero;
            uvs[top] = new Vector2(0.5f, 1f);
            uvs[bottom] = new Vector2(0.5f, 0.5f);
            int[] triangles = new int[sides * 12];
            for (int index = 0; index < sides; index++)
            {
                int next = (index + 1) % sides;
                int triangle = index * 12;
                triangles[triangle] = index;
                triangles[triangle + 1] = index + sides;
                triangles[triangle + 2] = next;
                triangles[triangle + 3] = next;
                triangles[triangle + 4] = index + sides;
                triangles[triangle + 5] = next + sides;
                triangles[triangle + 6] = index + sides;
                triangles[triangle + 7] = top;
                triangles[triangle + 8] = next + sides;
                triangles[triangle + 9] = next;
                triangles[triangle + 10] = bottom;
                triangles[triangle + 11] = index;
            }

            Mesh rockMesh = new() { name = "AngularMountainRock" };
            rockMesh.vertices = vertices;
            rockMesh.uv = uvs;
            rockMesh.triangles = triangles;
            rockMesh.RecalculateNormals();
            rockMesh.RecalculateBounds();
            AssetDatabase.CreateAsset(rockMesh, path);

            GameObject details = new("Rock Ledges and Outcrops");
            details.transform.SetParent(parent, false);
            System.Random random = new(44017);
            const int count = 38;
            for (int index = 0; index < count; index++)
            {
                float angle = (index / (float)count + ((float)random.NextDouble() - 0.5f) * 0.018f) * Mathf.PI * 2f;
                float radius = 42.3f + (float)random.NextDouble() * 8.4f;
                float height = SampleMountainHeight(radius, angle) - 0.06f;
                GameObject formation = new($"Rock Formation {index + 1:00}");
                formation.transform.SetParent(details.transform, false);
                formation.transform.SetPositionAndRotation(
                    new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius),
                    Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 90f + (float)random.NextDouble() * 45f, 0f));
                formation.transform.localScale = new Vector3(
                    1.1f + (float)random.NextDouble() * 2.1f,
                    0.55f + (float)random.NextDouble() * 1.25f,
                    0.75f + (float)random.NextDouble() * 1.4f);
                formation.AddComponent<MeshFilter>().sharedMesh = rockMesh;
                formation.AddComponent<MeshRenderer>().sharedMaterial = index % 4 == 0 ? rockLight : rock;
                GameObjectUtility.SetStaticEditorFlags(formation, StaticEditorFlags.BatchingStatic);
            }
        }

        private static float SampleMountainHeight(float radius, float angle)
        {
            float broad = Mathf.Sin(angle * 3f + 0.7f) * 1.7f + Mathf.Sin(angle * 7f - 0.4f) * 0.9f;
            float detail = Mathf.Sin(angle * 17f + 1.3f) * 0.45f;
            float radiusNoise = broad * 0.45f + detail;
            float innerRadius = 41.5f;
            float middleRadius = 46f + radiusNoise;
            float upperRadius = 51.5f + radiusNoise;
            float outerRadius = 57f + radiusNoise;
            float innerHeight = SampleGroundHeight(Mathf.Cos(angle) * innerRadius, Mathf.Sin(angle) * innerRadius);
            float middleHeight = 2.2f + Mathf.Abs(detail) * 1.8f;
            float upperHeight = 7.2f + broad * 0.8f + Mathf.Abs(detail) * 2f;
            float outerHeight = 13.5f + broad * 1.25f + detail * 1.8f;

            if (radius <= middleRadius)
            {
                return Mathf.Lerp(innerHeight, middleHeight, Mathf.InverseLerp(innerRadius, middleRadius, radius));
            }

            if (radius <= upperRadius)
            {
                return Mathf.Lerp(middleHeight, upperHeight, Mathf.InverseLerp(middleRadius, upperRadius, radius));
            }

            return Mathf.Lerp(upperHeight, outerHeight, Mathf.InverseLerp(upperRadius, outerRadius, radius));
        }

        private static Mesh CreateMountainRingMesh()
        {
            const int segments = 128;
            const int layers = 4;
            float[] radii = { 41.5f, 46f, 51.5f, 57f };
            Vector3[] vertices = new Vector3[segments * layers];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[segments * (layers - 1) * 6];

            for (int index = 0; index < segments; index++)
            {
                float t = index / (float)segments;
                float angle = t * Mathf.PI * 2f;
                float broad = Mathf.Sin(angle * 3f + 0.7f) * 1.7f + Mathf.Sin(angle * 7f - 0.4f) * 0.9f;
                float detail = Mathf.Sin(angle * 17f + 1.3f) * 0.45f;

                for (int layer = 0; layer < layers; layer++)
                {
                    float radiusNoise = layer == 0 ? 0f : broad * 0.45f + detail;
                    float radius = radii[layer] + radiusNoise;
                    float height = layer switch
                    {
                        0 => SampleGroundHeight(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius),
                        1 => 2.2f + Mathf.Abs(detail) * 1.8f,
                        2 => 7.2f + broad * 0.8f + Mathf.Abs(detail) * 2f,
                        _ => 13.5f + broad * 1.25f + detail * 1.8f
                    };
                    int vertexIndex = index * layers + layer;
                    vertices[vertexIndex] = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
                    uvs[vertexIndex] = new Vector2(t * 8f, layer / (float)(layers - 1) * 3f);
                }
            }

            int triangleIndex = 0;
            for (int index = 0; index < segments; index++)
            {
                int next = (index + 1) % segments;
                for (int layer = 0; layer < layers - 1; layer++)
                {
                    int a = index * layers + layer;
                    int b = next * layers + layer;
                    int c = index * layers + layer + 1;
                    int d = next * layers + layer + 1;
                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = d;
                    triangles[triangleIndex++] = c;
                }
            }

            Mesh mesh = new() { name = "MountainRing" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void CreateStumpObstacle(GameObject[] prefabs, string name, Vector3 position, Vector3 size, Transform parent, int variantOffset)
        {
            GameObject obstacle = new(name);
            obstacle.transform.SetParent(parent);
            obstacle.transform.position = new Vector3(position.x, SampleGroundHeight(position.x, position.z), position.z);

            int count = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(size.x, size.z) / 2.7f), 1, 3);
            bool spreadAlongX = size.x >= size.z;
            float longAxis = spreadAlongX ? size.x : size.z;
            float shortAxis = spreadAlongX ? size.z : size.x;
            float width = Mathf.Min(1.8f, shortAxis * 0.82f);

            for (int index = 0; index < count; index++)
            {
                float along = ((index + 1f) / (count + 1f) - 0.5f) * longAxis;
                float across = ((index % 2 == 0) ? -0.12f : 0.12f) * shortAxis;
                GameObject stump = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[(variantOffset + index) % prefabs.Length]);
                stump.name = $"Stump {index + 1:00}";
                stump.transform.SetParent(obstacle.transform, false);
                float stumpX = spreadAlongX ? along : across;
                float stumpZ = spreadAlongX ? across : along;
                float localGroundOffset = SampleGroundHeight(position.x + stumpX, position.z + stumpZ) - obstacle.transform.position.y;
                stump.transform.localPosition = spreadAlongX
                    ? new Vector3(along, localGroundOffset, across)
                    : new Vector3(across, localGroundOffset, along);
                stump.transform.localRotation = Quaternion.Euler(0f, (variantOffset * 53f) + index * 41f, 0f);
                float widthScale = Mathf.Clamp(width * (0.83f + 0.06f * ((index + variantOffset) % 3)), 1.08f, 1.5f);
                float heightScale = 0.96f + 0.1f * ((index + variantOffset) % 3);
                stump.transform.localScale = new Vector3(widthScale, heightScale, widthScale);
                GameObjectUtility.SetStaticEditorFlags(stump, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);
            }
        }

        private static void CreateNaturalDetails(GameObject bushPrefab, GameObject rockPrefab)
        {
            GameObject details = new("Natural Details");
            System.Random random = new(59327);
            const int total = 48;
            for (int index = 0; index < total; index++)
            {
                bool isBush = index < 22;
                GameObject prefab = isBush ? bushPrefab : rockPrefab;
                Vector3 position = FindNaturalPosition(random, 3.2f, null);
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                string typeName = isBush ? "Bush" : "Rock";
                instance.name = $"{typeName} {index + 1:000}";
                instance.transform.SetParent(details.transform);
                instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f));
                float scale = 0.75f + (float)random.NextDouble() * 0.55f;
                instance.transform.localScale = Vector3.one * scale;
                GameObjectUtility.SetStaticEditorFlags(instance, StaticEditorFlags.BatchingStatic);
            }
        }

        private static void CreateTrees(GameObject[] prefabs)
        {
            GameObject trees = new("Trees");
            System.Random random = new(14729);
            List<Vector3> positions = new();
            const int treeCount = 46;
            while (positions.Count < treeCount)
            {
                positions.Add(FindNaturalPosition(random, 5.5f, positions));
            }

            for (int index = 0; index < positions.Count; index++)
            {
                GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[random.Next(prefabs.Length)]);
                tree.name = $"Tree {index + 1:00}";
                tree.transform.SetParent(trees.transform);
                Vector3 position = positions[index];
                position.y = SampleGroundHeight(position.x, position.z);
                tree.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f));
                float scale = 0.76f + (float)random.NextDouble() * 0.5f;
                tree.transform.localScale = Vector3.one * scale;
                GameObjectUtility.SetStaticEditorFlags(tree, StaticEditorFlags.BatchingStatic);
            }
        }

        private static Vector3 FindNaturalPosition(System.Random random, float minimumSpacing, List<Vector3> existing)
        {
            for (int attempt = 0; attempt < 4000; attempt++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float radius = Mathf.Sqrt((float)random.NextDouble()) * 38.5f;
                Vector3 candidate = new(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if ((Mathf.Abs(candidate.x) < 5.5f && candidate.z < 30f) ||
                    new Vector2(candidate.x, candidate.z).magnitude < 11f)
                {
                    continue;
                }

                if (Vector3.Distance(candidate, new Vector3(0f, 0f, -36f)) < 7f ||
                    IsNearObstacle(candidate, new Vector3(-17f, 0f, -11f), 7f) ||
                    IsNearObstacle(candidate, new Vector3(16f, 0f, -6f), 6f) ||
                    IsNearObstacle(candidate, new Vector3(1f, 0f, 17f), 7f) ||
                    IsNearObstacle(candidate, new Vector3(-28f, 0f, 20f), 6f) ||
                    IsNearObstacle(candidate, new Vector3(27f, 0f, -22f), 7f))
                {
                    continue;
                }

                bool overlaps = false;
                if (existing != null)
                {
                    foreach (Vector3 position in existing)
                    {
                        if (Vector3.Distance(candidate, position) < minimumSpacing)
                        {
                            overlaps = true;
                            break;
                        }
                    }
                }

                if (!overlaps)
                {
                    candidate.y = SampleGroundHeight(candidate.x, candidate.z);
                    return candidate;
                }
            }

            throw new InvalidOperationException("Could not find a natural scenery position.");
        }

        private static bool IsNearObstacle(Vector3 position, Vector3 obstacle, float radius) =>
            Vector2.Distance(new Vector2(position.x, position.z), new Vector2(obstacle.x, obstacle.z)) < radius;
    }
}
