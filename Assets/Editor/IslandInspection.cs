using System.IO;
using System.Linq;
using System.Text;
using CrystalSprint;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CrystalSprintEditor
{
    public static class IslandInspection
    {
        public static void Inspect()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CrystalSprint.unity");
            var report = new StringBuilder();
            foreach (var item in Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None)
                .Where(i => i.Kind == EnvironmentAssetKind.Tree).GroupBy(i => i.SourcePrefab).Select(g => g.First()))
            {
                report.AppendLine($"TREE {item.name} {item.SourcePrefab} pos={item.transform.position} scale={item.transform.localScale}");
                foreach(var c in item.GetComponentsInChildren<Collider>()) report.AppendLine($"COL {c.name} {c.GetType().Name} {c.enabled} {c.bounds}");
                foreach(var f in item.GetComponentsInChildren<MeshFilter>()) report.AppendLine($"MESH {f.name} {f.sharedMesh.name} {f.sharedMesh.vertexCount} {f.sharedMesh.bounds} sub={f.sharedMesh.subMeshCount} local={f.transform.localPosition} rot={f.transform.localEulerAngles} mats={string.Join(",",f.GetComponent<Renderer>().sharedMaterials.Select(m=>m.name))}");
            }
            foreach(var r in new[]{GameObject.Find("Ground").GetComponent<Renderer>(), Object.FindAnyObjectByType<FirstPersonViewmodel>().Axe.GetComponentInChildren<Renderer>()})
                report.AppendLine($"RENDERER {r.name} {r.bounds} MATERIAL {r.sharedMaterial.name} {r.sharedMaterial.shader.name} path={AssetDatabase.GetAssetPath(r.sharedMaterial)}");
            var ocean = AssetDatabase.LoadAssetAtPath<Material>("Assets/Houidisoft technology/One Click Add Water -Stylized Water Shader/Resources/water.mat");
            for(int i=0;i<ocean.shader.GetPropertyCount();i++)
            {
                var n=ocean.shader.GetPropertyName(i); var t=ocean.shader.GetPropertyType(i);
                report.AppendLine($"OCEAN {n} {ocean.shader.GetPropertyDescription(i)} {t} = {(t==ShaderPropertyType.Color?ocean.GetColor(n).ToString():t==ShaderPropertyType.Vector?ocean.GetVector(n).ToString():t==ShaderPropertyType.Texture?ocean.GetTexture(n)?.name:ocean.GetFloat(n).ToString())}");
            }
            foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) report.AppendLine($"ROOT {root.name} {root.transform.position}");
            Directory.CreateDirectory("Logs/Island"); File.WriteAllText("Logs/Island/inspection.txt",report.ToString());
            Debug.Log("ISLAND_INSPECTION_OK");
        }
    }
}
