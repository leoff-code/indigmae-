using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CrystalSprintEditor
{
    public static class ForestKitProbe
    {
        public const string Kit = "Assets/Vegetation_Stylized_Pack_ByLuxArtStudios";
        public static void Inspect()
        {
            StringBuilder report = new();
            string[] paths = AssetDatabase.FindAssets("t:Prefab", new[] { Kit + "/Prefabs" }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToArray();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera camera = new GameObject("Probe Camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.15f, 0.19f, 0.22f);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.gray;
            Light light = new GameObject("Probe Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(30f, -25f, 0f);
            light.intensity = 1.4f;
            Directory.CreateDirectory("Logs/ForestKitProbe");
            foreach (string path in paths)
            {
                GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
                report.AppendLine($"{path}: bounds {bounds}; LODs {item.GetComponent<LODGroup>()?.GetLODs().Length ?? 0}");
                foreach (MeshFilter filter in item.GetComponentsInChildren<MeshFilter>())
                    report.AppendLine($"  Mesh {filter.name}: {filter.sharedMesh.vertexCount} verts, {filter.sharedMesh.triangles.Length / 3} tris, {filter.sharedMesh.subMeshCount} submeshes, matrix {filter.transform.localEulerAngles}");
                foreach (Material material in renderers.SelectMany(renderer => renderer.sharedMaterials).Distinct())
                {
                    report.AppendLine($"  Material {material.name}: {material.shader.name}, error={ShaderUtil.ShaderHasError(material.shader)}, color={material.color}");
                    foreach (string prop in material.GetTexturePropertyNames())
                        if (material.GetTexture(prop) != null) report.AppendLine($"    {prop}={AssetDatabase.GetAssetPath(material.GetTexture(prop))}");
                }
                // Preview original geometry/textures with a temporary URP material if supplied shader is Built-in.
                foreach (Renderer renderer in renderers)
                {
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(original =>
                    {
                        if (original.shader.name.StartsWith("Universal") || original.shader.name.StartsWith("Shader Graphs")) return original;
                        Material copy = new(Shader.Find("Universal Render Pipeline/Lit"));
                        copy.SetTexture("_BaseMap", original.mainTexture);
                        copy.SetColor("_BaseColor", original.color);
                        copy.SetFloat("_Smoothness", 0.1f);
                        copy.SetFloat("_Cull", 0f);
                        copy.SetFloat("_AlphaClip", 1f);
                        copy.EnableKeyword("_ALPHATEST_ON");
                        return copy;
                    }).ToArray();
                }
                item.GetComponent<LODGroup>()?.ForceLOD(0);
                camera.orthographicSize = Mathf.Max(bounds.size.y, bounds.size.x) * 0.62f;
                Vector3 from = bounds.center + new Vector3(0.4f, 0.15f, -1f).normalized * bounds.size.magnitude * 2f;
                camera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(bounds.center - from));
                Texture2D frame = Render(camera, 400, 400);
                File.WriteAllBytes("Logs/ForestKitProbe/" + Path.GetFileNameWithoutExtension(path) + ".png", frame.EncodeToPNG());
                Object.DestroyImmediate(frame);
                Object.DestroyImmediate(item);
            }
            File.WriteAllText("Logs/ForestKitProbe/audit.txt", report.ToString());
            EditorSceneManager.OpenScene(CrystalSprintProjectSetup.ScenePath);
            Debug.Log("Forest kit audit and previews completed.");
        }

        public static Texture2D Render(Camera camera, int width, int height)
        {
            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            camera.aspect = (float)width / height;
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture old = RenderTexture.active;
            RenderTexture.active = target;
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = old;
            Object.DestroyImmediate(target);
            return texture;
        }
    }
}
