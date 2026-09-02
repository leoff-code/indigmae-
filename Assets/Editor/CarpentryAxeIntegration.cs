using System;
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
    // Editor-only, scoped replacement: preserve the existing equipment prefab and icon GUIDs.
    public static class CarpentryAxeIntegration
    {
        public const string SourcePath = "Assets/CoolWorks_Studio/Carpentry_Tools/Prefabs/Axe_Straight.prefab";
        private const string AxePath = "Assets/Prefabs/LumberjackAxe.prefab";
        private const string IconPath = "Assets/Textures/AxeInventoryIcon.asset";
        private const string PreviewFolder = "Logs/CarpentryAxePreviews";

        public static void Inspect()
        {
            foreach (string name in new[] { "Axe_Curved", "Axe_Straight" })
            {
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath.Replace("Axe_Straight", name));
                Mesh mesh = source.GetComponent<MeshFilter>().sharedMesh;
                Debug.Log($"{name}: {mesh.vertexCount} vertices, bounds {mesh.bounds}, material {source.GetComponent<Renderer>().sharedMaterial.shader.name}, hand anchor {HandAnchor(mesh):F4}");
            }
        }

        [MenuItem("Tools/Crystal Sprint/Replace Held Axe With Carpentry Axe")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play Mode before replacing the axe.");
            for (int index = 0; index < UnityEngine.SceneManagement.SceneManager.sceneCount; index++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty)
                    throw new InvalidOperationException("Save the open scene before replacing the axe.");
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            if (source == null) throw new InvalidOperationException("The imported Carpentry axe is missing.");
            Material material = source.GetComponent<Renderer>().sharedMaterial;
            if (material == null || material.shader.name != "Universal Render Pipeline/Lit" || !material.shader.isSupported || ShaderUtil.ShaderHasError(material.shader))
                throw new InvalidOperationException("The axe material must be a working URP/Lit material.");
            foreach (string texture in new[] { "_BaseMap", "_BumpMap", "_SpecGlossMap", "_OcclusionMap" })
                if (material.GetTexture(texture) == null) throw new InvalidOperationException("Missing axe texture: " + texture);

            GameObject root = PrefabUtility.LoadPrefabContents(AxePath);
            try
            {
                Transform[] oldParts = root.transform.Cast<Transform>().ToArray();
                GameObject axe = (GameObject)PrefabUtility.InstantiatePrefab(source, root.scene);
                axe.name = "Carpentry Axe";
                axe.transform.SetParent(root.transform, false);
                // The source pivot is at the head; the existing weapon pivot is in the hand.
                // Keep the old head direction (-X), overall size and animated hand socket.
                Vector3 anchor = HandAnchor(axe.GetComponent<MeshFilter>().sharedMesh);
                axe.transform.localScale = Vector3.one * 2.5f;
                axe.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                axe.transform.localPosition = -(axe.transform.localRotation * (anchor * 2.5f));
                Transform handContact = new GameObject("Hand Contact").transform;
                handContact.SetParent(axe.transform, false);
                handContact.localPosition = anchor;
                // The old held axe has no physics body/hit collider. Do not import loose-prop physics.
                foreach (Collider collider in axe.GetComponents<Collider>()) Object.DestroyImmediate(collider);
                foreach (Rigidbody body in axe.GetComponents<Rigidbody>()) Object.DestroyImmediate(body);
                if (Vector3.Distance(handContact.position, root.transform.position) > 0.001f)
                    throw new InvalidOperationException("Axe handle does not meet the existing grip pivot.");
                foreach (Transform oldPart in oldParts) Object.DestroyImmediate(oldPart.gameObject);
                PrefabUtility.SaveAsPrefabAsset(root, AxePath);
                Debug.Log($"Carpentry axe fitted: source anchor={anchor:F4}, local position={axe.transform.localPosition:F4}, rotation=(0,-90,0), scale=2.5. Existing Held Axe scale/rotation unchanged.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            CreateInventoryIcon();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(CrystalSprintProjectSetup.ScenePath);
            Transform held = Object.FindAnyObjectByType<PlayerController>().transform.Find("Visual/Upper Body/Right Arm Pivot/Right Elbow/Right Wrist/Axe Grip/Held Axe");
            if (held == null || held.Find("Carpentry Axe/Hand Contact") == null) throw new InvalidOperationException("The scene has not inherited the new axe.");
            if (GameObject.Find("Inventory Slot 1").GetComponentInChildren<RawImage>().texture != AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath))
                throw new InvalidOperationException("Slot 1 lost its icon reference.");
            Debug.Log("Carpentry axe and rendered icon updated. Scene, player prefab, inventory and attack scripts were not saved or modified.");
        }

        private static Vector3 HandAnchor(Mesh mesh)
        {
            // Place the hand just above the end of the wooden handle, using its cross-section.
            float y = mesh.bounds.min.y + 0.055f;
            Vector3[] vertices = mesh.vertices;
            float nearestRing = vertices.Min(vertex => Mathf.Abs(vertex.y - y));
            Vector3[] ring = vertices.Where(vertex => Mathf.Abs(vertex.y - y) <= nearestRing + 0.012f).ToArray();
            return new Vector3((ring.Min(vertex => vertex.x) + ring.Max(vertex => vertex.x)) * 0.5f, y,
                (ring.Min(vertex => vertex.z) + ring.Max(vertex => vertex.z)) * 0.5f);
        }

        private static void CreateInventoryIcon()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject axe = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AxePath));
            axe.transform.rotation = Quaternion.Euler(0f, -10f, -28f);
            Bounds bounds = axe.GetComponentInChildren<Renderer>().bounds;
            Camera camera = new GameObject("Icon Camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.size.y, bounds.size.x) * 0.59f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.allowHDR = false;
            camera.transform.SetPositionAndRotation(bounds.center + Vector3.forward * 4f, Quaternion.Euler(0f, 180f, 0f));
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.7f, 0.75f, 0.8f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.45f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.23f, 0.2f);
            Light key = new GameObject("Icon Key").AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 2.2f;
            key.transform.rotation = Quaternion.Euler(35f, 210f, 0f);
            Light fill = new GameObject("Icon Fill").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.65f;
            fill.transform.rotation = Quaternion.Euler(-15f, 110f, 0f);
            Texture2D rendered = Render(camera, 256, 256);
            rendered.name = "AxeInventoryIcon";
            rendered.wrapMode = TextureWrapMode.Clamp;
            rendered.filterMode = FilterMode.Bilinear;
            Color32[] pixels = rendered.GetPixels32();
            if (pixels.Count(pixel => pixel.a > 32) < 1000 || pixels[0].a > 0)
                throw new InvalidOperationException("The rendered axe icon is empty or lacks transparency.");
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            EditorUtility.CopySerialized(rendered, existing);
            EditorUtility.SetDirty(existing);
            Directory.CreateDirectory(PreviewFolder);
            File.WriteAllBytes(PreviewFolder + "/axe-icon.png", rendered.EncodeToPNG());
            Object.DestroyImmediate(rendered);
        }

        private static Texture2D Render(Camera camera, int width, int height)
        {
            RenderTexture output = new(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            RenderTexture previous = RenderTexture.active;
            camera.aspect = (float)width / height;
            try
            {
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = output });
                RenderTexture.active = output;
                Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                return texture;
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(output);
            }
        }

        public static void CapturePreviews() => DenseForestReview.Capture();
    }
}
