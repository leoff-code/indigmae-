using System.IO;
using System.Linq;
using System.Text;
using CrystalSprint;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CrystalSprintEditor
{
    public static class CabinCollisionRepair
    {
        [MenuItem("Tools/Crystal Sprint/Repair Cabin Solid Colliders")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Leave Play Mode first.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new System.InvalidOperationException("Save the scene first.");
            GameObject root = PrefabUtility.LoadPrefabContents(CabinWaterIntegration.CabinPrefab);
            try { BuildSolidShell(root); PrefabUtility.SaveAsPrefabAsset(root, CabinWaterIntegration.CabinPrefab); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("Cabin: one-sided wall/frame mesh colliders disabled; fitted solid walls, glass and gable volumes installed. Door opening unchanged.");
        }

        public static void BuildSolidShell(GameObject root)
        {
            if (root.transform.Find("Solid Building Colliders") != null) return;
            foreach (string name in new[] { "CabinWalls", "DoorFrame" })
            {
                MeshCollider old = root.transform.Find(name).GetComponent<MeshCollider>();
                old.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(old);
            }
            Transform shell = new GameObject("Solid Building Colliders").transform;
            shell.SetParent(root.transform, false);
            void Box(string name, Vector3 min, Vector3 max)
            {
                GameObject part = new(name); part.transform.SetParent(shell, false);
                BoxCollider collider = part.AddComponent<BoxCollider>();
                collider.center = (min + max) * .5f; collider.size = max - min;
            }
            // Solid wall sections fit around the original three glazed windows and door.
            const float wallTop = 3.56f, windowBottom = .998f, windowTop = 2.556f;
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * 2.76f; string label = side < 0 ? "Left" : "Right";
                Box(label + " Rear Wall", new Vector3(x - .09f, 0, -3.779f), new Vector3(x + .09f, wallTop, -1.18f));
                Box(label + " Front Wall", new Vector3(x - .09f, 0, 1.14f), new Vector3(x + .09f, wallTop, 3.779f));
                Box(label + " Window Sill Wall", new Vector3(x - .09f, 0, -1.18f), new Vector3(x + .09f, windowBottom, 1.14f));
                Box(label + " Window Header Wall", new Vector3(x - .09f, windowTop, -1.18f), new Vector3(x + .09f, wallTop, 1.14f));
            }
            Box("Rear Wall", new Vector3(-2.85f, 0, -3.869f), new Vector3(2.85f, wallTop, -3.689f));
            void Front(string name, float x0, float x1, float y0, float y1) =>
                Box(name, new Vector3(x0, y0, 3.68f), new Vector3(x1, y1, 3.86f));
            Front("Front Left Pier", -2.85f, -1.927f, 0, wallTop);
            Front("Front Window Sill Wall", -1.927f, -.179f, 0, windowBottom);
            Front("Front Window Header Wall", -1.927f, -.179f, windowTop, wallTop);
            // Small fitting clearances accommodate the thick leaf's swept outside corners.
            Front("Door Left Jamb Wall", -.179f, .374f, 0, wallTop);
            Front("Door Right Jamb Wall", 1.55f, 2.85f, 0, wallTop);
            Front("Door Lintel", .374f, 1.55f, 2.36f, wallTop);
            Transform window = root.transform.Find("SmallWindow");
            BoxCollider glass = window.GetComponent<BoxCollider>();
            if (glass == null) glass = window.gameObject.AddComponent<BoxCollider>();
            Bounds geometry = window.GetComponent<MeshFilter>().sharedMesh.bounds;
            glass.center = geometry.center; glass.size = geometry.size; glass.isTrigger = false;
            // The two side windows already have correctly fitted, two-sided BoxColliders.
            Mesh gable = CreateGable();
            foreach (int side in new[] { -1, 1 })
            {
                GameObject part = new(side < 0 ? "Rear Solid Gable" : "Front Solid Gable");
                part.transform.SetParent(shell, false); part.transform.localPosition = new Vector3(0, 0, side * 3.779f);
                MeshCollider volume = part.AddComponent<MeshCollider>(); volume.sharedMesh = gable; volume.convex = true;
            }
        }

        private static Mesh CreateGable()
        {
            const string path = "Assets/Meshes/PondCabin/SolidGableCollision.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (mesh != null) return mesh;
            mesh = new Mesh { name = "SolidGableCollision" };
            mesh.vertices = new[] { new Vector3(-2.76f, 3.55f, -.09f), new Vector3(2.76f, 3.55f, -.09f), new Vector3(0, 5.07f, -.09f),
                new Vector3(-2.76f, 3.55f, .09f), new Vector3(2.76f, 3.55f, .09f), new Vector3(0, 5.07f, .09f) };
            mesh.triangles = new[] { 0, 2, 1, 3, 4, 5, 0, 1, 4, 0, 4, 3, 1, 2, 5, 1, 5, 4, 2, 0, 3, 2, 3, 5 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh;
        }

        public static void Inspect()
        {
            EditorSceneManager.OpenScene(CrystalSprintProjectSetup.ScenePath);
            PondCabin cabin = Object.FindAnyObjectByType<PondCabin>();
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            Physics.SyncTransforms();
            StringBuilder report = new();
            report.AppendLine($"Player: {player.GetComponent<CharacterController>()}; Rigidbody: {player.GetComponent<Rigidbody>() != null}; layer={player.gameObject.layer}; queriesHitBackfaces={Physics.queriesHitBackfaces}");
            foreach (Collider collider in cabin.GetComponentsInChildren<Collider>(true))
                report.AppendLine($"{collider.name}: {collider.GetType().Name}, enabled={collider.enabled}, trigger={collider.isTrigger}, layer={collider.gameObject.layer}, ignored={Physics.GetIgnoreLayerCollision(player.gameObject.layer, collider.gameObject.layer)}, bounds={collider.bounds}");
            MeshCollider walls = cabin.transform.Find("CabinWalls").GetComponent<MeshCollider>();
            foreach (Vector3 local in new[] { new Vector3(0, 1.5f, -3.779f), new Vector3(2.765f, 1.5f, -2.5f), new Vector3(-2.765f, 1.5f, -2.5f) })
            {
                Vector3 outward = local.z < -3 ? Vector3.back : Vector3.right * Mathf.Sign(local.x);
                Vector3 p = cabin.transform.TransformPoint(local), n = cabin.transform.TransformDirection(outward);
                foreach (int side in new[] { 1, -1 })
                {
                    bool hit = walls.Raycast(new Ray(p + n * side, -n * side), out RaycastHit data, 2);
                    report.AppendLine($"Wall ray {local}, {(side == 1 ? "outside" : "inside")}: hit={hit}, normal={data.normal}");
                }
            }
            Directory.CreateDirectory("Logs/CabinInteractions"); File.WriteAllText("Logs/CabinInteractions/collider-before.txt", report.ToString()); Debug.Log(report);
        }
    }
}
