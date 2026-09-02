using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CrystalSprint;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CrystalSprintEditor
{
    public static class DenseForestUpgrade
    {
        public const string Materials = "Assets/Materials/ForestKit";
        public const string Prefabs = "Assets/Prefabs/ForestKit";
        private static System.Random random;
        private static MeshCollider ground;
        private static readonly Dictionary<Material, Material> converted = new();
        private static float Random(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

        [MenuItem("Tools/Crystal Sprint/Apply Dense Vegetation Forest Upgrade")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before modifying the scene.");
            EditorSceneManager.OpenScene(CrystalSprintProjectSetup.ScenePath);
            foreach (string folder in new[] { Materials, Prefabs, "Assets/Meshes/ForestKit", "Assets/Settings" }) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            random = new System.Random(92026);
            converted.Clear();
            ExpandTerrain();
            EnsureMountainBarrier();
            SeatMountainDetails();
            ground = GameObject.Find("Ground").GetComponent<MeshCollider>();
            ReplaceVegetation();
            ExtendDetails();
            InstallGrass();
            UpgradePlayerRig();
            ConfigureLighting();
            AddCredit();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Validate();
            Debug.Log("Dense forest upgrade saved: 135 x 135 terrain, 240 kit trees, 140 kit bushes, instanced kit grass and articulated single axe chop.");
        }

        private static void ExpandTerrain()
        {
            MeshFilter filter = GameObject.Find("Ground").GetComponent<MeshFilter>();
            if (filter.sharedMesh.bounds.size.x > 130f) return;
            Vector3[] old = filter.sharedMesh.vertices;
            const int side = 91;
            Vector3[] vertices = new Vector3[side * side];
            Vector2[] uv = new Vector2[vertices.Length];
            List<int> triangles = new();
            for (int z = 0; z < side; z++)
            for (int x = 0; x < side; x++)
            {
                float px = x * 1.5f - 67.5f, pz = z * 1.5f - 67.5f;
                float y = x >= 13 && x <= 77 && z >= 13 && z <= 77 ? old[(z - 13) * 65 + x - 13].y : ForestWorld.Height(px, pz);
                vertices[z * side + x] = new Vector3(px, y, pz);
                uv[z * side + x] = new Vector2((px + 48f) / 96f * 14f, (pz + 48f) / 96f * 14f);
                if (x == side - 1 || z == side - 1) continue;
                int i = z * side + x;
                triangles.AddRange(new[] { i, i + side, i + 1, i + 1, i + side, i + side + 1 });
            }
            Mesh mesh = new() { name = "Expanded Rolling Meadow", vertices = vertices, uv = uv, triangles = triangles.ToArray() };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
            SaveAsset(mesh, "Assets/Meshes/ForestKit/ExpandedRollingMeadow.asset");
            filter.sharedMesh = mesh;
            filter.GetComponent<MeshCollider>().sharedMesh = mesh;
            foreach (string name in new[] { "Continuous Mountain Terrain", "Meadow Rock Transition" })
            {
                MeshFilter border = GameObject.Find(name).GetComponent<MeshFilter>();
                Mesh expanded = Object.Instantiate(border.sharedMesh);
                Vector3[] points = expanded.vertices;
                for (int i = 0; i < points.Length; i++)
                {
                    float radius = new Vector2(points[i].x, points[i].z).magnitude;
                    float oldBase = ForestWorld.Height(points[i].x, points[i].z);
                    points[i].x *= Mathf.Sqrt(2f); points[i].z *= Mathf.Sqrt(2f);
                    if (name == "Meadow Rock Transition" || radius < 42f)
                        points[i].y += ForestWorld.Height(points[i].x, points[i].z) - oldBase;
                }
                expanded.vertices = points; expanded.RecalculateNormals(); expanded.RecalculateBounds(); expanded.RecalculateTangents();
                expanded.name = "Expanded " + name;
                SaveAsset(expanded, "Assets/Meshes/ForestKit/" + name.Replace(" ", "") + ".asset");
                border.sharedMesh = expanded;
                if (border.TryGetComponent(out MeshCollider collider)) collider.sharedMesh = expanded;
            }
            Physics.SyncTransforms();
            MeshCollider mountain = GameObject.Find("Continuous Mountain Terrain").GetComponent<MeshCollider>();
            foreach (EnvironmentAssetInstance cliff in Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None).Where(item => item.Kind == EnvironmentAssetKind.Cliff))
            {
                Vector3 p = cliff.transform.position;
                p.x *= Mathf.Sqrt(2f); p.z *= Mathf.Sqrt(2f); p.y = 0f;
                cliff.transform.position = p;
                MeshFilter primary = cliff.GetComponentInChildren<MeshFilter>();
                List<float> offsets = new();
                foreach (Vector3 vertex in primary.sharedMesh.vertices)
                {
                    Vector3 world = primary.transform.TransformPoint(vertex);
                    if (world.y > primary.GetComponent<Renderer>().bounds.min.y + primary.GetComponent<Renderer>().bounds.size.y * .3f) continue;
                    if (mountain.Raycast(new Ray(new Vector3(world.x, 60f, world.z), Vector3.down), out RaycastHit hit, 100f)) offsets.Add(hit.point.y - world.y);
                }
                offsets.Sort();
                if (offsets.Count > 0) cliff.transform.position += Vector3.up * (offsets[(int)((offsets.Count - 1) * .75f)] - .1f);
                Vector3 foot = cliff.GetComponentsInChildren<Renderer>()[0].bounds.min;
                cliff.Configure(cliff.Kind, cliff.SourcePrefab, new Vector3(p.x, foot.y, p.z));
            }
        }

        private static void ReplaceVegetation()
        {
            EnvironmentAssetInstance[] old = Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None)
                .Where(item => item.Kind == EnvironmentAssetKind.Tree || item.Kind == EnvironmentAssetKind.Bush).ToArray();
            // Source prefabs and materials are retained; only the superseded scene instances are replaced.
            foreach (EnvironmentAssetInstance item in old) Object.DestroyImmediate(item.gameObject);
            Transform treeParent = GameObject.Find("Trees").transform;
            GameObject bushObject = GameObject.Find("Kit Bushes") ?? new GameObject("Kit Bushes");
            bushObject.transform.SetParent(treeParent.parent, true);
            GameObject[] trees = Enumerable.Range(0, 10).Select(i => BuildVariant("Trees/S_Tree_" + (char)('A' + i), true)).ToArray();
            GameObject[] bushes = Enumerable.Range(0, 4).Select(i => BuildVariant("Bushes/S_Bush_" + (char)('A' + i), false)).ToArray();
            List<Vector2> placed = new();
            EnvironmentAssetInstance[] obstacles = Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None)
                .Where(item => item.Kind == EnvironmentAssetKind.Stump || item.Kind == EnvironmentAssetKind.Log || item.Kind == EnvironmentAssetKind.Rock).ToArray();
            for (int attempts = 0; placed.Count < ForestWorld.TreeCount && attempts < 30000; attempts++)
            {
                Vector2 p = Point(13f, ForestWorld.Radius - 3f);
                if (ForestWorld.PathDistance(p) < 4.5f || (p - new Vector2(0f, -20f)).magnitude < 7f || placed.Any(other => (other - p).sqrMagnitude < 3.5f * 3.5f)) continue;
                if (obstacles.Any(o => (new Vector2(o.transform.position.x, o.transform.position.z) - p).magnitude < 2.6f)) continue;
                int variant = placed.Count % trees.Length;
                GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(trees[variant]);
                tree.transform.SetParent(treeParent, false);
                float height = BoundsOf(tree).size.y;
                tree.transform.localScale = Vector3.one * (Random(8f, 11.5f) / height);
                tree.transform.rotation = Quaternion.Euler(0f, Random(0f, 360f), 0f);
                Seat(tree, p, true);
                tree.name = $"Forest Tree {(char)('A' + variant)} {placed.Count + 1:000}";
                placed.Add(p);
            }
            if (placed.Count != ForestWorld.TreeCount) throw new InvalidOperationException("Could not place all trees safely.");
            List<Vector2> bushPoints = new();
            for (int attempts = 0; bushPoints.Count < ForestWorld.BushCount && attempts < 20000; attempts++)
            {
                Vector2 p = attempts % 3 == 0 ? Point(12f, ForestWorld.Radius - 2f) : placed[random.Next(placed.Count)] + UnityDirection() * Random(1.4f, 3f);
                if (p.magnitude > ForestWorld.Radius - 1.8f || p.magnitude < 10f || ForestWorld.PathDistance(p) < 2.2f || (p - new Vector2(0f, -20f)).magnitude < 5f) continue;
                if (bushPoints.Any(other => (other - p).magnitude < 1.7f) || placed.Any(other => (other - p).magnitude < 1.25f)) continue;
                if (obstacles.Any(o => (new Vector2(o.transform.position.x, o.transform.position.z) - p).magnitude < 2.1f)) continue;
                int variant = bushPoints.Count % bushes.Length;
                GameObject bush = (GameObject)PrefabUtility.InstantiatePrefab(bushes[variant]);
                bush.transform.SetParent(bushObject.transform, false);
                bush.transform.localScale = Vector3.one * Random(.55f, .92f);
                bush.transform.rotation = Quaternion.Euler(0f, Random(0f, 360f), 0f);
                Seat(bush, p, false);
                bush.name = $"Forest Bush {(char)('A' + variant)} {bushPoints.Count + 1:000}";
                bushPoints.Add(p);
            }
        }

        private static void EnsureMountainBarrier()
        {
            // Expanding horizontal distances must not turn the perimeter into a walkable exit ramp.
            MeshFilter filter = GameObject.Find("Continuous Mountain Terrain").GetComponent<MeshFilter>();
            Mesh mesh = filter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = vertices[i];
                if (new Vector2(p.x, p.z).magnitude < 75f) continue;
                float angle = Mathf.Atan2(p.z, p.x);
                float broad = Mathf.Sin(angle * 3f + .7f) * 1.7f + Mathf.Sin(angle * 7f - .4f) * .9f;
                float detail = Mathf.Sin(angle * 17f + 1.3f) * .45f;
                p.y = (13.5f + broad * 1.25f + detail * 1.8f) * 1.65f;
                vertices[i] = p;
            }
            mesh.vertices = vertices; mesh.RecalculateBounds(); mesh.RecalculateNormals(); mesh.RecalculateTangents();
            filter.GetComponent<MeshCollider>().sharedMesh = null;
            filter.GetComponent<MeshCollider>().sharedMesh = mesh;
            EditorUtility.SetDirty(mesh);
        }

        [MenuItem("Tools/Crystal Sprint/Reseat Expanded Mountain Details")]
        public static void RepairMountainContacts()
        {
            EditorSceneManager.OpenScene(CrystalSprintProjectSetup.ScenePath);
            SeatMountainDetails();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static void SeatMountainDetails()
        {
            MeshCollider mountain = GameObject.Find("Continuous Mountain Terrain").GetComponent<MeshCollider>();
            Physics.SyncTransforms();
            foreach (EnvironmentAssetInstance item in Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None).Where(i => i.Kind == EnvironmentAssetKind.Cliff))
            {
                Transform root = item.transform;
                root.position = new Vector3(root.position.x, 0f, root.position.z);
                MeshFilter filter = item.GetComponentInChildren<MeshFilter>();
                Vector3[] points = filter.sharedMesh.vertices.Select(filter.transform.TransformPoint).ToArray();
                float lower = points.Min(p => p.y), upper = points.Max(p => p.y);
                List<float> offsets = new();
                foreach (Vector3 point in points)
                {
                    if (point.y > Mathf.Lerp(lower, upper, .3f)) continue;
                    if (mountain.Raycast(new Ray(new Vector3(point.x, 60f, point.z), Vector3.down), out RaycastHit sample, 100f))
                        offsets.Add(sample.point.y - point.y);
                }
                if (offsets.Count == 0) throw new InvalidOperationException("No mountain support for " + item.name);
                offsets.Sort();
                root.position += Vector3.up * (offsets[(int)((offsets.Count - 1) * .75f)] - .12f);
                // The provenance contact is on the real slope under the formation, not an obsolete pre-expansion height.
                if (mountain.Raycast(new Ray(new Vector3(root.position.x, 60f, root.position.z), Vector3.down), out RaycastHit contact, 100f))
                {
                    // Account for differently simplified feet in every imported LOD, not just LOD0.
                    float highestLodFoot = item.GetComponentsInChildren<MeshFilter>()
                        .Max(lod => lod.sharedMesh.vertices.Min(vertex => lod.transform.TransformPoint(vertex).y));
                    root.position -= Vector3.up * Mathf.Max(0f, highestLodFoot - contact.point.y + .08f);
                    item.Configure(item.Kind, item.SourcePrefab, contact.point);
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(root);
                EditorUtility.SetDirty(item);
            }
        }

        private static Vector2 Point(float min, float max) => UnityDirection() * Mathf.Sqrt(Random(min * min, max * max));
        private static Vector2 UnityDirection() { float a = Random(0f, Mathf.PI * 2f); return new Vector2(Mathf.Cos(a), Mathf.Sin(a)); }

        private static GameObject BuildVariant(string relative, bool tree)
        {
            string source = ForestWorld.Kit + "/Prefabs/" + relative + ".prefab";
            GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source));
            foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(ConvertMaterial).ToArray();
                renderer.receiveShadows = true;
            }
            foreach (Collider collider in item.GetComponentsInChildren<Collider>()) collider.enabled = false;
            if (tree)
            {
                Bounds bounds = BoundsOf(item);
                CapsuleCollider trunk = item.AddComponent<CapsuleCollider>();
                trunk.radius = Mathf.Clamp(bounds.size.y * .039f, .23f, .55f);
                trunk.height = bounds.size.y * .56f;
                float lowestTrunk = Lowest(item, true).y;
                trunk.center = new Vector3(0f, lowestTrunk + trunk.height * .5f, 0f);
            }
            LODGroup group = item.GetComponent<LODGroup>();
            LOD[] lods = group.GetLODs();
            for (int i = 0; i < lods.Length; i++) lods[i].screenRelativeTransitionHeight = i == 0 ? .20f : i == 1 ? .09f : .018f;
            group.SetLODs(lods); group.RecalculateBounds();
            EnvironmentAssetInstance marker = item.GetComponent<EnvironmentAssetInstance>() ?? item.AddComponent<EnvironmentAssetInstance>();
            marker.Configure(tree ? EnvironmentAssetKind.Tree : EnvironmentAssetKind.Bush, source, Vector3.zero);
            string path = Prefabs + "/Forest_" + Path.GetFileNameWithoutExtension(source).Substring(2) + ".prefab";
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(item, path);
            Object.DestroyImmediate(item);
            return asset;
        }

        private static Material ConvertMaterial(Material original)
        {
            if (converted.TryGetValue(original, out Material cached)) return cached;
            Material material = new(Shader.Find("Universal Render Pipeline/Lit")) { name = "Forest_" + original.name, enableInstancing = true };
            bool trunk = original.name == "M_Trunk";
            Texture texture = original.HasProperty("_MainTex") ? original.GetTexture("_MainTex") : original.GetTexturePropertyNames().Select(original.GetTexture).FirstOrDefault(t => t != null);
            material.SetTexture("_BaseMap", texture);
            Color color = trunk || original.name == "M_Plant_Atlas" ? Color.white : new Color(.49f, .65f, .27f);
            if (original.name.Contains("02")) color = new Color(.43f, .60f, .25f);
            if (original.name.Contains("03")) color = new Color(.60f, .68f, .29f);
            if (original.name.Contains("04")) color = new Color(.34f, .53f, .27f);
            if (original.name.Contains("05")) color = new Color(.67f, .69f, .32f);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", trunk ? .12f : .18f);
            if (trunk)
            {
                material.SetTexture("_BumpMap", original.GetTexture("_BumpMap"));
                material.SetFloat("_BumpScale", .65f); material.EnableKeyword("_NORMALMAP");
            }
            else
            {
                material.SetFloat("_Cull", 0f); material.SetFloat("_AlphaClip", 1f); material.SetFloat("_Cutoff", .4f);
                material.EnableKeyword("_ALPHATEST_ON"); material.SetOverrideTag("RenderType", "TransparentCutout"); material.renderQueue = 2450;
            }
            string path = Materials + "/" + material.name + ".mat";
            SaveAsset(material, path);
            material = AssetDatabase.LoadAssetAtPath<Material>(path);
            converted.Add(original, material);
            return material;
        }

        private static Bounds BoundsOf(GameObject item)
        {
            Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static Vector3 Lowest(GameObject item, bool trunkOnly)
        {
            MeshFilter filter = item.GetComponentInChildren<MeshFilter>();
            Vector3[] vertices = filter.sharedMesh.vertices;
            int[] indices = trunkOnly ? filter.sharedMesh.GetIndices(0) : filter.sharedMesh.triangles;
            Vector3 lowest = new(0f, float.PositiveInfinity, 0f);
            foreach (int index in indices)
            {
                Vector3 world = filter.transform.TransformPoint(vertices[index]);
                if (world.y < lowest.y) lowest = world;
            }
            return lowest;
        }

        private static void Seat(GameObject item, Vector2 p, bool trunkOnly)
        {
            item.transform.position = new Vector3(p.x, 0f, p.y);
            Vector3 foot = Lowest(item, trunkOnly);
            if (!ground.Raycast(new Ray(new Vector3(foot.x, 50f, foot.z), Vector3.down), out RaycastHit hit, 100f)) throw new InvalidOperationException("Missing ground at " + p);
            item.transform.position += Vector3.up * (hit.point.y - foot.y - .025f);
            EnvironmentAssetInstance marker = item.GetComponent<EnvironmentAssetInstance>();
            marker.Configure(marker.Kind, marker.SourcePrefab, hit.point);
        }

        private static void ExtendDetails()
        {
            if (GameObject.Find("Outer Forest Details") != null) return;
            Transform parent = new GameObject("Outer Forest Details").transform;
            parent.SetParent(GameObject.Find("Trees").transform.parent, true);
            EnvironmentAssetInstance[] originals = Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None);
            foreach ((EnvironmentAssetKind kind, int count) in new[] { (EnvironmentAssetKind.Log, 6), (EnvironmentAssetKind.Mushroom, 60), (EnvironmentAssetKind.Branch, 40), (EnvironmentAssetKind.Rock, 20) })
            {
                EnvironmentAssetInstance[] sources = originals.Where(item => item.Kind == kind).ToArray();
                for (int i = 0; i < count; i++)
                {
                    Vector2 p = Point(39f, ForestWorld.Radius - 3f);
                    if (ForestWorld.PathDistance(p) < 2f || originals.Where(o => o.Kind == EnvironmentAssetKind.Tree).Any(o => (new Vector2(o.transform.position.x, o.transform.position.z) - p).magnitude < 1.6f)) { i--; continue; }
                    EnvironmentAssetInstance source = sources[i % sources.Length];
                    GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source.SourcePrefab));
                    item.transform.SetParent(parent, false);
                    item.transform.localScale = source.transform.lossyScale * Random(.8f, 1.15f);
                    item.transform.rotation = Quaternion.Euler(0f, Random(0f, 360f), 0f);
                    item.AddComponent<EnvironmentAssetInstance>().Configure(kind, source.SourcePrefab, Vector3.zero);
                    if (kind == EnvironmentAssetKind.Mushroom || kind == EnvironmentAssetKind.Branch)
                        foreach (Collider collider in item.GetComponentsInChildren<Collider>()) collider.enabled = false;
                    Seat(item, p, false);
                }
            }
        }

        private static void InstallGrass()
        {
            string[] names = { "S_Grass_01A", "S_Grass_02A", "S_Grass_C" };
            Mesh[] meshes = new Mesh[names.Length]; Material[] surfaces = new Material[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                GameObject original = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ForestWorld.Kit + "/Prefabs/Bushes/" + names[i] + ".prefab"));
                MeshFilter filter = original.GetComponentInChildren<MeshFilter>();
                Mesh mesh = Object.Instantiate(filter.sharedMesh);
                Matrix4x4 matrix = original.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                Vector3[] points = mesh.vertices;
                float lowest = points.Min(p => matrix.MultiplyPoint3x4(p).y);
                for (int vertex = 0; vertex < points.Length; vertex++) points[vertex] = matrix.MultiplyPoint3x4(points[vertex]) - Vector3.up * lowest;
                mesh.vertices = points; mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
                mesh.name = names[i] + " Root Space";
                string meshPath = "Assets/Meshes/ForestKit/" + names[i] + ".asset";
                SaveAsset(mesh, meshPath); meshes[i] = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                Material source = filter.GetComponent<Renderer>().sharedMaterial;
                Texture atlas = source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : source.GetTexturePropertyNames().Select(source.GetTexture).FirstOrDefault(t => t != null);
                Material material = new(Shader.Find("CrystalSprint/Kit Interactive Grass")) { name = "Interactive_" + names[i], enableInstancing = true };
                material.SetTexture("_BaseMap", atlas);
                material.SetColor("_BaseColor", new Color(.30f, .43f, .16f));
                material.SetColor("_TipColor", new Color(.64f, .73f, .36f));
                material.SetFloat("_MeshHeight", meshes[i].bounds.size.y);
                string materialPath = Materials + "/" + material.name + ".mat";
                SaveAsset(material, materialPath);
                surfaces[i] = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                Object.DestroyImmediate(original);
            }
            GameObject field = GameObject.Find("Interactive Grass Field");
            if (field.TryGetComponent(out MeshRenderer oldRenderer)) Object.DestroyImmediate(oldRenderer);
            if (field.TryGetComponent(out MeshFilter oldMesh)) Object.DestroyImmediate(oldMesh);
            List<Vector4> clearances = new();
            Physics.SyncTransforms();
            foreach (EnvironmentAssetInstance item in Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None))
            {
                if (item.Kind == EnvironmentAssetKind.Bush || item.Kind == EnvironmentAssetKind.Branch || item.Kind == EnvironmentAssetKind.Mushroom || item.Kind == EnvironmentAssetKind.Water || item.Kind == EnvironmentAssetKind.Cliff) continue;
                Collider collider = item.GetComponentsInChildren<Collider>().FirstOrDefault(c => c.enabled);
                if (collider == null) continue;
                Bounds b = collider.bounds;
                float radius = item.Kind == EnvironmentAssetKind.Tree ? Mathf.Max(b.extents.x, b.extents.z) * .85f : Mathf.Min(2f, Mathf.Max(b.extents.x, b.extents.z)) * .8f;
                clearances.Add(new Vector4(b.center.x, b.center.z, radius, 0f));
            }
            InstancedForestGrass grass = field.GetComponent<InstancedForestGrass>() ?? field.AddComponent<InstancedForestGrass>();
            grass.Configure(meshes, surfaces, ground.sharedMesh, clearances.ToArray());
            Debug.Log($"Generated {grass.InstanceCount:N0} instanced kit grass clumps; no individual grass GameObjects.");
        }

        public static void UpgradePlayerRig()
        {
            const string path = "Assets/Prefabs/LumberjackPlayer.prefab";
            GameObject player = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform visual = player.transform.Find("Visual");
                if (visual.Find("Upper Body") != null)
                {
                    foreach (Transform joint in visual.GetComponentsInChildren<Transform>().Where(t => t.name == "Right Elbow" || t.name == "Left Elbow")) AddElbowCover(joint);
                    player.GetComponent<LumberjackVisual>().ApplyReferencePose();
                    PrefabUtility.SaveAsPrefabAsset(player, path);
                    return;
                }
                Transform body = new GameObject("Upper Body").transform;
                body.SetParent(visual, false);
                string[] names = { "Torso", "Head Rig", "Left Suspender", "Right Suspender", "Left Button", "Right Button", "Free Arm Pivot", "Right Arm Pivot" };
                foreach (string name in names) visual.Find(name)?.SetParent(body, true);
                Transform leftArm = body.Find("Free Arm Pivot"), rightArm = body.Find("Right Arm Pivot");
                Transform[] left = Articulate(leftArm, false), right = Articulate(rightArm, true);
                Transform grip = rightArm.GetComponentsInChildren<Transform>().First(t => t.name == "Axe Grip");
                grip.SetParent(right[1], false); grip.localPosition = Vector3.zero; grip.localRotation = Quaternion.identity;
                Transform axe = grip.Find("Held Axe"); axe.localPosition = Vector3.zero; axe.localRotation = Quaternion.identity;
                LumberjackVisual animation = player.GetComponent<LumberjackVisual>();
                animation.Configure(visual, visual.Find("Left Leg"), visual.Find("Right Leg"), leftArm, rightArm, grip);
                animation.ConfigureArticulation(body, left[0], right[0], left[1], right[1]);
                PrefabUtility.SaveAsPrefabAsset(player, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
        }

        private static Transform[] Articulate(Transform shoulder, bool right)
        {
            shoulder.localPosition = new Vector3(right ? .43f : -.43f, .34f, 0f);
            shoulder.localRotation = Quaternion.identity;
            Transform sleeve = shoulder.Find(right ? "Right Arm" : "Free Arm");
            sleeve.localPosition = new Vector3(0f, -.175f, 0f); sleeve.localScale = new Vector3(.245f, .20f, .245f);
            Transform elbow = new GameObject(right ? "Right Elbow" : "Left Elbow").transform;
            elbow.SetParent(shoulder, false); elbow.localPosition = new Vector3(0f, -.35f, 0f);
            GameObject forearm = Object.Instantiate(sleeve.gameObject, elbow);
            forearm.name = right ? "Right Forearm" : "Left Forearm";
            forearm.transform.localPosition = new Vector3(0f, -.19f, 0f); forearm.transform.localScale = new Vector3(.21f, .205f, .21f);
            Transform wrist = new GameObject(right ? "Right Wrist" : "Left Wrist").transform;
            wrist.SetParent(elbow, false); wrist.localPosition = new Vector3(0f, -.38f, 0f);
            Transform hand = shoulder.Find(right ? "Right Hand" : "Free Hand");
            hand.SetParent(wrist, false); hand.localPosition = Vector3.zero; hand.localRotation = Quaternion.identity;
            AddElbowCover(elbow);
            return new[] { elbow, wrist };
        }

        private static void AddElbowCover(Transform elbow)
        {
            if (elbow.Find("Sleeve Joint") != null) return;
            GameObject joint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            joint.name = "Sleeve Joint"; joint.transform.SetParent(elbow, false); joint.transform.localScale = Vector3.one * .21f;
            Object.DestroyImmediate(joint.GetComponent<Collider>());
            joint.GetComponent<Renderer>().sharedMaterial = elbow.GetComponentInChildren<Renderer>().sharedMaterial;
        }

        private static void ConfigureLighting()
        {
            string pipelinePath = "Assets/Settings/ForestURP.asset";
            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(GraphicsSettings.currentRenderPipeline), pipelinePath);
                pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            }
            pipeline.shadowDistance = 90f;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = pipeline; }
            QualitySettings.SetQualityLevel(current, false);
            Light sun = GameObject.Find("Directional Light").GetComponent<Light>();
            sun.transform.rotation = Quaternion.Euler(27f, -52f, 0f);
            sun.color = new Color(1f, .87f, .67f); sun.intensity = 1.85f; sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .95f; sun.shadowBias = .025f; sun.shadowNormalBias = .22f;
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light != sun && light.type == LightType.Directional) light.enabled = false;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.ambientSkyColor = new Color(.58f, .65f, .70f);
            RenderSettings.ambientEquatorColor = new Color(.42f, .46f, .36f);
            RenderSettings.ambientGroundColor = new Color(.28f, .31f, .23f);
            // A dim sky-side fill preserves readable faces in deep canopy shade.
            GameObject fillObject = GameObject.Find("Forest Sky Bounce") ?? new GameObject("Forest Sky Bounce");
            Light fill = fillObject.GetComponent<Light>();
            if (fill == null) fill = fillObject.AddComponent<Light>();
            fill.enabled = true; fill.type = LightType.Directional; fill.intensity = .22f; fill.color = new Color(.72f, .83f, 1f);
            fill.shadows = LightShadows.None; fill.transform.rotation = Quaternion.Euler(55f, 130f, 0f);
            RenderSettings.reflectionIntensity = .6f;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.39f, .47f, .49f); RenderSettings.fogDensity = .007f;
            const string skyPath = "Assets/Materials/ForestKit/ForestExtendedDaySky.mat";
            Material source = AssetDatabase.LoadAssetAtPath<Material>("Assets/BOXOPHOBIC/Skybox Cubemap Extended/Demo/Materials/Skybox Cubemap Extended Day.mat");
            if (source == null) throw new InvalidOperationException("Imported Extended Day skybox is missing.");
            Material sky = new(source) { name = "Forest Extended Day Sky" };
            sky.SetFloat("_Exposure", .85f); sky.SetColor("_TintColor", new Color(.47f, .49f, .50f));
            sky.SetFloat("_EnableRotation", 1f); sky.EnableKeyword("_ENABLEROTATION_ON"); sky.SetFloat("_Rotation", 125f); sky.SetFloat("_RotationSpeed", 0f);
            sky.SetFloat("_EnableFog", 1f); sky.EnableKeyword("_ENABLEFOG_ON"); sky.SetFloat("_FogIntensity", .65f); sky.SetFloat("_FogHeight", .22f); sky.SetFloat("_FogSmoothness", .5f);
            SaveAsset(sky, skyPath); RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            // A/B rendering showed banded demo gradients and no matching sun in the imported cubemap.
            // Keep the tested shader available, but retain the smoother procedural sun/sky for this scene.
            Material atmosphere = new(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ForestSkybox.mat")) { name = "Forest Atmosphere Sky" };
            atmosphere.SetFloat("_Exposure", 1.05f); atmosphere.SetFloat("_AtmosphereThickness", 1.05f); atmosphere.SetFloat("_SunSize", .032f);
            SaveAsset(atmosphere, Materials + "/ForestAtmosphereSky.mat");
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(Materials + "/ForestAtmosphereSky.mat");
            Renderer floor = GameObject.Find("Ground").GetComponent<Renderer>();
            Material meadow = new(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Ground.mat")) { name = "Forest Meadow Floor" };
            meadow.SetColor("_BaseColor", new Color(.72f, .85f, .58f));
            meadow.SetFloat("_Smoothness", .09f);
            SaveAsset(meadow, Materials + "/ForestMeadowFloor.mat");
            floor.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Materials + "/ForestMeadowFloor.mat");
            GameObject volumeObject = GameObject.Find("Forest Color Grading") ?? new GameObject("Forest Color Grading");
            Volume volume = volumeObject.GetComponent<Volume>() ?? volumeObject.AddComponent<Volume>(); volume.isGlobal = true;
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Daytime Forest Grading";
            Tonemapping tone = profile.Add<Tonemapping>(true); tone.mode.value = TonemappingMode.ACES;
            ColorAdjustments colors = profile.Add<ColorAdjustments>(true); colors.contrast.value = 12f; colors.saturation.value = -6f; colors.postExposure.value = .15f;
            string profilePath = "Assets/Settings/ForestGrading.asset";
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath) == null)
            {
                AssetDatabase.CreateAsset(profile, profilePath);
                foreach (VolumeComponent component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            }
            else { Object.DestroyImmediate(profile); profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath); }
            if (profile.TryGet(out ColorAdjustments adjustments))
            {
                adjustments.contrast.value = 7f; adjustments.postExposure.value = .25f;
                EditorUtility.SetDirty(adjustments);
            }
            volume.sharedProfile = profile;
            Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            Camera.main.farClipPlane = 230f;
            EditorUtility.SetDirty(pipeline);
        }

        private static void AddCredit()
        {
            if (GameObject.Find("Vegetation Credit") != null) return;
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            Text template = canvas.GetComponentInChildren<Text>();
            GameObject label = new("Vegetation Credit", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(canvas.transform, false);
            RectTransform rect = label.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f); rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-18f, -14f); rect.sizeDelta = new Vector2(330f, 26f);
            Text text = label.GetComponent<Text>(); text.font = template.font; text.fontSize = 16; text.alignment = TextAnchor.MiddleRight;
            text.color = new Color(.9f, .93f, .83f, .92f); text.text = "Vegetation: LUX ART STUDIOS"; text.raycastTarget = false;
            Outline outline = label.AddComponent<Outline>(); outline.effectColor = new Color(0f, 0f, 0f, .65f); outline.effectDistance = new Vector2(1f, -1f);
        }

        private static void SaveAsset(Object created, string path)
        {
            Object existing = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (existing == null) AssetDatabase.CreateAsset(created, path);
            else { EditorUtility.CopySerialized(created, existing); EditorUtility.SetDirty(existing); Object.DestroyImmediate(created); }
        }

        [MenuItem("Tools/Crystal Sprint/Validate Dense Forest")]
        public static void Validate()
        {
            EnvironmentAssetInstance[] items = Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None);
            foreach (EnvironmentAssetKind kind in new[] { EnvironmentAssetKind.Tree, EnvironmentAssetKind.Bush })
            {
                EnvironmentAssetInstance[] group = items.Where(item => item.Kind == kind).ToArray();
                if (group.Length != (kind == EnvironmentAssetKind.Tree ? ForestWorld.TreeCount : ForestWorld.BushCount)) throw new InvalidOperationException("Unexpected " + kind + " count.");
                foreach (EnvironmentAssetInstance item in group)
                    if (!item.SourcePrefab.StartsWith(ForestWorld.Kit) || new Vector2(item.transform.position.x, item.transform.position.z).magnitude >= ForestWorld.Radius)
                        throw new InvalidOperationException("Wrong source or out-of-bounds vegetation: " + item.name);
            }
            foreach (Material material in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).SelectMany(r => r.sharedMaterials).Distinct())
                if (material == null || material.shader == null || ShaderUtil.ShaderHasError(material.shader) || material.shader.name == "Custom/S_ShaderTree" || material.shader.name == "Standard")
                    throw new InvalidOperationException("Incompatible material: " + material?.name);
            InstancedForestGrass grass = Object.FindAnyObjectByType<InstancedForestGrass>();
            if (grass == null || grass.InstanceCount < 20000) throw new InvalidOperationException("Grass density is below target.");
            if (ShaderUtil.ShaderHasError(RenderSettings.skybox.shader)) throw new InvalidOperationException("Skybox shader failed.");
            Debug.Log($"Forest validation passed: {grass.InstanceCount} grass clumps, {ForestWorld.TreeCount} trees, {ForestWorld.BushCount} bushes.");
        }
    }
}
