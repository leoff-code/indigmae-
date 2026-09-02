using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CrystalSprint;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CrystalSprintEditor
{
    public static class CabinInteractionSetup
    {
        [MenuItem("Tools/Crystal Sprint/Install Cabin Interactions")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play Mode first.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the open scene first.");
            GameObject root = PrefabUtility.LoadPrefabContents(CabinWaterIntegration.CabinPrefab);
            try
            {
                if (root.transform.Find("Solid Building Colliders") == null) throw new InvalidOperationException("Repair and test cabin collision first.");
                Transform door = root.transform.Find("Door");
                if (door.GetComponent<HingedDoorInteractable>() != null) throw new InvalidOperationException("Interactions already installed; preserve manual prefab changes.");
                SeatDoorHinge(door);
                Rigidbody rigidbody = door.gameObject.AddComponent<Rigidbody>();
                rigidbody.isKinematic = true; rigidbody.useGravity = false;
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                door.gameObject.AddComponent<HingedDoorInteractable>().Configure(false);
                foreach (Transform part in door.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(part.gameObject, 0);
                PrefabUtility.RecordPrefabInstancePropertyModifications(door);

                Mesh source = root.transform.Find("Curtain").GetComponent<MeshFilter>().sharedMesh;
                Mesh left = SplitCurtain(source, false), right = SplitCurtain(source, true);
                foreach (string name in new[] { "Curtain", "Curtain (1)", "Curtain (2)", "Curtain (3)" })
                {
                    Transform curtain = root.transform.Find(name);
                    bool doorway = name == "Curtain (3)";
                    if (doorway)
                    {
                        curtain.localPosition = new Vector3(.966f, 1.389f, 3.399f);
                        curtain.localScale = new Vector3(1, 1, .43f);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(curtain);
                    }
                    MeshRenderer old = curtain.GetComponent<MeshRenderer>(); old.enabled = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(old);
                    Transform Panel(string panelName, Mesh mesh)
                    {
                        GameObject panel = new(panelName); panel.transform.SetParent(curtain, false);
                        panel.AddComponent<MeshFilter>().sharedMesh = mesh;
                        panel.AddComponent<MeshRenderer>().sharedMaterials = old.sharedMaterials;
                        // Only visible cloth is usable: gathered door curtains do not cover the
                        // door's interaction ray with an invisible full-width trigger.
                        BoxCollider target = panel.AddComponent<BoxCollider>(); target.isTrigger = true;
                        target.center = mesh.bounds.center; target.size = new Vector3(Mathf.Max(.16f, mesh.bounds.size.x), mesh.bounds.size.y, mesh.bounds.size.z);
                        return panel.transform;
                    }
                    Transform a = Panel("Left Fabric Panel", left), b = Panel("Right Fabric Panel", right);
                    curtain.gameObject.AddComponent<CurtainInteractable>().Configure(a, b, source.bounds.size.z, doorway);
                    foreach (Transform part in curtain.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(part.gameObject, 0);
                }
                PrefabUtility.SaveAsPrefabAsset(root, CabinWaterIntegration.CabinPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            var scene = EditorSceneManager.OpenScene(CrystalSprintProjectSetup.ScenePath);
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            Text controls = Object.FindObjectsByType<Text>().First(t => t.name == "Controls");
            if (!controls.text.Contains("Benutzen")) controls.text = controls.text.Replace("\n", "   |   E  Benutzen\n");
            GameObject label = new("Use Prompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(controls.transform.parent, false);
            Text prompt = label.GetComponent<Text>(); prompt.font = controls.font; prompt.fontSize = 19;
            prompt.alignment = TextAnchor.MiddleCenter; prompt.text = "E \u2013 Benutzen"; prompt.raycastTarget = false; prompt.color = Color.white;
            RectTransform rect = label.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(220, 35); rect.anchoredPosition = new Vector2(0, -55);
            var shadow = label.AddComponent<Shadow>(); shadow.effectDistance = new Vector2(1, -1); shadow.effectColor = new Color(0, 0, 0, .9f);
            prompt.enabled = false;
            player.gameObject.AddComponent<PlayerInteractor>().Configure(Camera.main, prompt);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("Generic Use action installed; 95-degree hinged door and four paired, smoothly gathered curtains. Scene systems preserved.");
        }

        public static void RepairDoorHinge()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(CabinWaterIntegration.CabinPrefab);
            try
            {
                SeatDoorHinge(root.transform.Find("Door"));
                Transform shell = root.transform.Find("Solid Building Colliders");
                BoxCollider left = shell.Find("Door Left Jamb Wall").GetComponent<BoxCollider>();
                left.center = new Vector3((-.179f + .374f) * .5f, 1.78f, 3.77f); left.size = new Vector3(.374f + .179f, 3.56f, .18f);
                BoxCollider right = shell.Find("Door Right Jamb Wall").GetComponent<BoxCollider>();
                right.center = new Vector3(2.2f, 1.78f, 3.77f); right.size = new Vector3(1.3f, 3.56f, .18f);
                BoxCollider lintel = shell.Find("Door Lintel").GetComponent<BoxCollider>();
                lintel.center = new Vector3((.374f + 1.55f) * .5f, 2.96f, 3.77f); lintel.size = new Vector3(1.55f - .374f, 1.2f, .18f);
                PrefabUtility.SaveAsPrefabAsset(root, CabinWaterIntegration.CabinPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.OpenScene(CrystalSprintProjectSetup.ScenePath);
            Text controls = Object.FindObjectsByType<Text>().First(t => t.name == "Controls");
            if (controls.text.EndsWith("   |   E  Benutzen"))
            {
                controls.text = controls.text.Substring(0, controls.text.Length - "   |   E  Benutzen".Length).Replace("\n", "   |   E  Benutzen\n");
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            PondCabin cabin = Object.FindAnyObjectByType<PondCabin>();
            HingedDoorInteractable door = cabin.GetComponentInChildren<HingedDoorInteractable>();
            BoxCollider leaf = door.GetComponent<BoxCollider>();
            var report = new System.Text.StringBuilder();
            report.AppendLine($"Door pivot {door.transform.localPosition:F6}; mesh {door.GetComponent<MeshFilter>().sharedMesh.bounds}; box center {leaf.center:F6}, size {leaf.size:F6}");
            foreach (Collider solid in cabin.GetComponentsInChildren<Collider>())
            {
                if (!solid.enabled || solid.isTrigger || solid.transform.IsChildOf(door.transform)) continue;
                float maximum = 0; int at = 0;
                for (int angle = 0; angle <= 95; angle++)
                    if (Physics.ComputePenetration(leaf, door.transform.position, cabin.transform.rotation * Quaternion.Euler(0, angle, 0),
                        solid, solid.transform.position, solid.transform.rotation, out _, out float depth) && depth > maximum) { maximum = depth; at = angle; }
                if (maximum > 0) report.AppendLine($"{solid.name}: max penetration {maximum:F6} at {at} degrees");
            }
            File.WriteAllText("Logs/CabinInteractions/door-fit.txt", report.ToString());
        }

        private static void SeatDoorHinge(Transform door)
        {
            const string path = "Assets/Meshes/PondCabin/DoorAtHinge.asset";
            MeshFilter filter = door.GetComponent<MeshFilter>();
            if (AssetDatabase.GetAssetPath(filter.sharedMesh) == path) return;
            // The source pivot is on the back of the thick leaf, not its outward-opening
            // hinge edge. Move the axis to that edge while retaining the exact closed pose.
            Vector3 offset = new(0, 0, .122f);
            Mesh mesh = Object.Instantiate(filter.sharedMesh); mesh.name = "DoorAtHinge";
            mesh.vertices = mesh.vertices.Select(v => v - offset).ToArray(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path); filter.sharedMesh = mesh;
            door.localPosition += offset;
            BoxCollider collider = door.GetComponent<BoxCollider>(); collider.center -= offset;
            foreach (Transform child in door) { child.localPosition -= offset; PrefabUtility.RecordPrefabInstancePropertyModifications(child); }
            PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
            PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
            PrefabUtility.RecordPrefabInstancePropertyModifications(door);
        }

        private struct Vertex
        {
            public Vector3 position, normal;
            public Vector2 uv;
            public static Vertex Lerp(Vertex a, Vertex b, float t) => new()
            { position = Vector3.Lerp(a.position, b.position, t), normal = Vector3.Lerp(a.normal, b.normal, t).normalized, uv = Vector2.Lerp(a.uv, b.uv, t) };
        }

        private static Mesh SplitCurtain(Mesh source, bool right)
        {
            string path = "Assets/Meshes/PondCabin/Curtain" + (right ? "Right" : "Left") + ".asset";
            Mesh saved = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (saved != null) return saved;
            Vector3[] positions = source.vertices, normals = source.normals; Vector2[] uv = source.uv; int[] triangles = source.triangles;
            float middle = source.bounds.center.z, pivot = middle + (right ? 1 : -1) * source.bounds.size.z * .25f;
            var output = new List<Vertex>(); var indices = new List<int>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var polygon = new List<Vertex>();
                for (int j = 0; j < 3; j++) { int index = triangles[i + j]; polygon.Add(new Vertex { position = positions[index], normal = normals[index], uv = uv[index] }); }
                var clipped = new List<Vertex>(); Vertex previous = polygon[2];
                float Distance(Vertex v) => (v.position.z - middle) * (right ? 1 : -1);
                foreach (Vertex current in polygon)
                {
                    float a = Distance(previous), b = Distance(current);
                    if ((a >= 0) != (b >= 0)) clipped.Add(Vertex.Lerp(previous, current, a / (a - b)));
                    if (b >= 0) clipped.Add(current); previous = current;
                }
                int first = output.Count; output.AddRange(clipped);
                for (int p = 1; p < clipped.Count - 1; p++) { indices.Add(first); indices.Add(first + p); indices.Add(first + p + 1); }
            }
            saved = new Mesh { name = right ? "CurtainRight" : "CurtainLeft" };
            saved.SetVertices(output.Select(v => v.position - new Vector3(0, 0, pivot)).ToArray());
            saved.SetNormals(output.Select(v => v.normal).ToArray()); saved.SetUVs(0, output.Select(v => v.uv).ToArray());
            saved.SetTriangles(indices, 0); saved.RecalculateBounds(); saved.RecalculateTangents(); AssetDatabase.CreateAsset(saved, path); return saved;
        }
    }
}
