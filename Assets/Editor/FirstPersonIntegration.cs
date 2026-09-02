using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
    public static class FirstPersonIntegration
    {
        public const string PrefabPath = "Assets/Prefabs/FirstPerson/LumberjackArms.prefab";
        public const string ViewmodelLayer = "FirstPersonViewmodel";

        [MenuItem("Tools/Crystal Sprint/Install First Person")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play Mode first.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the open scene first.");
            var scene = EditorSceneManager.OpenScene(CrystalSprintProjectSetup.ScenePath);
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            Camera main = Camera.main;
            if (player == null || main == null) throw new InvalidOperationException("Existing player/camera missing.");
            string before = UnchangedWorldFingerprint(player, main);
            int layer = EnsureLayer();
            GameObject prefab = CreateArms(layer);
            ThirdPersonCamera orbit = main.GetComponent<ThirdPersonCamera>();
            if (orbit != null) orbit.enabled = false;
            FirstPersonCamera look = main.GetComponent<FirstPersonCamera>() ?? main.gameObject.AddComponent<FirstPersonCamera>();
            Transform existingOverlay = main.transform.Find("First Person Arms Camera");
            Camera overlay = existingOverlay != null ? existingOverlay.GetComponent<Camera>() : new GameObject("First Person Arms Camera").AddComponent<Camera>();
            overlay.transform.SetParent(main.transform, false);
            overlay.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            overlay.fieldOfView = 70f;
            overlay.nearClipPlane = .035f;
            overlay.farClipPlane = 4f;
            overlay.cullingMask = 1 << layer;
            overlay.allowHDR = main.allowHDR;
            overlay.allowMSAA = main.allowMSAA;
            var overlayData = overlay.GetUniversalAdditionalCameraData();
            overlayData.renderType = CameraRenderType.Overlay;
            var overlaySettings = new SerializedObject(overlayData);
            overlaySettings.FindProperty("m_ClearDepth").boolValue = true;
            overlaySettings.ApplyModifiedPropertiesWithoutUndo();
            overlayData.renderShadows = false;
            var mainData = main.GetUniversalAdditionalCameraData();
            mainData.renderType = CameraRenderType.Base;
            // Grade the complete world + arms once, at the end of the stack.
            overlayData.renderPostProcessing |= mainData.renderPostProcessing;
            overlayData.volumeLayerMask = mainData.volumeLayerMask;
            mainData.renderPostProcessing = false;
            if (!mainData.cameraStack.Contains(overlay)) mainData.cameraStack.Add(overlay);
            main.cullingMask &= ~(1 << layer);
            main.fieldOfView = 75f;
            main.nearClipPlane = .05f;
            look.Configure(player, overlay);
            look.SetViewAngles(player.transform.eulerAngles.y, 4f);

            Transform oldArms = overlay.transform.Find("Lumberjack First Person Arms");
            GameObject arms = oldArms != null ? oldArms.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            arms.name = "Lumberjack First Person Arms";
            arms.transform.SetParent(overlay.transform, false);
            arms.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            arms.transform.localScale = Vector3.one;
            FirstPersonViewmodel viewmodel = arms.GetComponent<FirstPersonViewmodel>();
            viewmodel.Bind(look, player);
            PrefabUtility.RecordPrefabInstancePropertyModifications(viewmodel);
            var visibility = main.GetComponent<FirstPersonBodyVisibility>() ?? main.gameObject.AddComponent<FirstPersonBodyVisibility>();
            visibility.Configure(look, main, overlay, player.transform.Find("Visual"), arms.transform);
            EditorUtility.SetDirty(look); EditorUtility.SetDirty(visibility);
            string after = UnchangedWorldFingerprint(player, main);
            if (before != after) throw new InvalidOperationException("An unrelated scene object changed during installation.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/FirstPersonReview");
            File.WriteAllText("Logs/FirstPersonReview/installation.txt", $"Environment/UI/lighting unchanged: {before == after}\nBefore: {before}\nAfter: {after}\nViewmodel layer: {layer}\nWorld character and axe prefab unchanged.\n");
            Debug.Log("First person installed. Complete world figure preserved; all other scene objects unchanged: " + before);
        }

        private static int EnsureLayer()
        {
            int existing = LayerMask.NameToLayer(ViewmodelLayer);
            if (existing >= 0) return existing;
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tags.FindProperty("layers");
            for (int i = 8; i < 32; i++)
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                {
                    layers.GetArrayElementAtIndex(i).stringValue = ViewmodelLayer;
                    tags.ApplyModifiedPropertiesWithoutUndo();
                    return i;
                }
            throw new InvalidOperationException("No free viewmodel layer.");
        }

        private static GameObject CreateArms(int layer)
        {
            Directory.CreateDirectory("Assets/Prefabs/FirstPerson");
            Directory.CreateDirectory("Assets/Materials/FirstPerson");
            AssetDatabase.Refresh();
            Material shirt = ViewMaterial("Assets/Materials/Lumberjack_Shirt.mat", "Sleeves");
            Material skin = ViewMaterial("Assets/Materials/Lumberjack_Skin.mat", "Hands");
            GameObject root = new("Lumberjack First Person Arms");
            try
            {
                var left = CreateArm(root.transform, false, shirt, skin);
                var right = CreateArm(root.transform, true, shirt, skin);
                GameObject axe = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LumberjackAxe.prefab"));
                axe.name = "First Person Carpentry Axe";
                axe.transform.SetParent(right.wrist, false);
                axe.transform.localPosition = Vector3.zero;
                axe.transform.localRotation = Quaternion.identity;
                axe.transform.localScale = Vector3.one * .62f;
                foreach (Renderer renderer in axe.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m => ViewMaterial(AssetDatabase.GetAssetPath(m), "Axe_" + m.name)).ToArray();
                foreach (Transform part in root.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = layer;
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                }
                root.AddComponent<FirstPersonViewmodel>().ConfigureRig(left.shoulder, left.elbow, left.wrist, right.shoulder, right.elbow, right.wrist, axe);
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static (Transform shoulder, Transform elbow, Transform wrist) CreateArm(Transform root, bool right, Material shirt, Material skin)
        {
            string side = right ? "Right" : "Left";
            Transform shoulder = Joint(side + " Shoulder", root, new Vector3(right ? .38f : -.38f, -.35f, -.03f));
            Part(side + " Upper Sleeve", shoulder, PrimitiveType.Capsule, new(0, -.175f, 0), new(.215f, .19f, .215f), shirt);
            Transform elbow = Joint(side + " Elbow", shoulder, Vector3.down * .35f);
            Part(side + " Sleeve Joint", elbow, PrimitiveType.Sphere, Vector3.zero, Vector3.one * .21f, shirt);
            Part(side + " Forearm Sleeve", elbow, PrimitiveType.Capsule, new(0, -.135f, 0), new(.175f, .16f, .175f), shirt);
            Part(side + " Skin Wrist", elbow, PrimitiveType.Capsule, new(0, -.325f, 0), new(.12f, .075f, .12f), skin);
            Transform wrist = Joint(side + " Wrist", elbow, Vector3.down * .38f);
            // Grip socket is the centre of a rounded closed hand. Fingers overlap the palm,
            // wrapping the original axe handle, whose contact point is exactly this socket.
            Part(side + " Palm", wrist, PrimitiveType.Sphere, new(0, -.012f, -.012f), new(.13f, .17f, .15f), skin);
            for (int i = 0; i < 4; i++)
                Part(side + " Finger " + (i + 1), wrist, PrimitiveType.Sphere, new(.041f, -.058f + i * .035f, .025f), new(.06f, .043f, .09f), skin);
            Transform thumb = Part(side + " Thumb", wrist, PrimitiveType.Capsule, new(-.043f, .033f, .032f), new(.058f, .057f, .058f), skin);
            thumb.localRotation = Quaternion.Euler(25f, 0f, -25f);
            return (shoulder, elbow, wrist);
        }

        private static Transform Joint(string name, Transform parent, Vector3 position)
        {
            Transform result = new GameObject(name).transform;
            result.SetParent(parent, false); result.localPosition = position;
            return result;
        }

        private static Transform Part(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part.transform;
        }

        private static Material ViewMaterial(string sourcePath, string name)
        {
            string path = "Assets/Materials/FirstPerson/" + name + ".mat";
            Material source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
            if (source == null) throw new InvalidOperationException("Missing viewmodel source material: " + sourcePath);
            Material result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result == null) { result = new Material(source); AssetDatabase.CreateAsset(result, path); }
            else result.CopyPropertiesFromMaterial(source);
            result.name = name;
            // Prevent the separately-rendered arms receiving the world character's self-shadow.
            if (result.HasProperty("_ReceiveShadows")) result.SetFloat("_ReceiveShadows", 0f);
            result.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            EditorUtility.SetDirty(result);
            return result;
        }

        private static string UnchangedWorldFingerprint(PlayerController player, Camera camera)
        {
            StringBuilder data = new();
            foreach (GameObject root in player.gameObject.scene.GetRootGameObjects())
            {
                if (root == player.gameObject || root == camera.gameObject) continue;
                foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                {
                    data.Append(EditorJsonUtility.ToJson(item.gameObject));
                    foreach (Component component in item.GetComponents<Component>())
                        if (component != null) data.Append(EditorJsonUtility.ToJson(component));
                }
            }
            using SHA256 hash = SHA256.Create();
            return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(data.ToString())));
        }
    }
}
