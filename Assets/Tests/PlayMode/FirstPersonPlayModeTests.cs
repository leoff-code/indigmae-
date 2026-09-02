using System.Collections;
using System.Linq;
using System.Reflection;
using CrystalSprint;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CrystalSprintTests
{
    public sealed class FirstPersonPlayModeTests
    {
        private PlayerController player;
        private FirstPersonCamera look;
        private FirstPersonViewmodel arms;
        private Mouse mouse;
        private Keyboard keyboard;
        private GameObject temporaryWall;
        private InputSettings previousInputSettings;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("CrystalSprint");
            yield return null;
            player = Object.FindAnyObjectByType<PlayerController>();
            look = Object.FindAnyObjectByType<FirstPersonCamera>();
            arms = Object.FindAnyObjectByType<FirstPersonViewmodel>();
            player.Warp(new Vector3(0, 1.2f, -20));
            player.SetTestInput(Vector2.zero, false);
            look.SetViewAngles(0, 4);
            yield return new WaitForSeconds(.6f);
        }

        [TearDown]
        public void Cleanup()
        {
            Time.timeScale = 1;
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (previousInputSettings != null)
            {
                InputSettings temporarySettings = InputSystem.settings;
                InputSystem.settings = previousInputSettings;
                if (temporarySettings != previousInputSettings) Object.Destroy(temporarySettings);
                previousInputSettings = null;
            }
            if (temporaryWall != null) Object.Destroy(temporaryWall);
            mouse = null; keyboard = null; temporaryWall = null;
        }

        [UnityTest]
        public IEnumerator EyeCameraAndOverlayRetainCompleteWorldCharacter()
        {
            Assert.That(look.enabled && player.UseViewFacing, Is.True);
            Assert.That(Camera.main.GetComponent<ThirdPersonCamera>().enabled, Is.False);
            Invoke(look, "LateUpdate");
            Assert.That(Vector3.Distance(look.transform.position, look.EyePosition), Is.LessThan(.0001f));
            Assert.That(look.transform.position.y - player.transform.position.y, Is.EqualTo(.73f).Within(.001f));
            Assert.That(Object.FindObjectsByType<AudioListener>().Count(l => l.enabled), Is.EqualTo(1));
            int layer = LayerMask.NameToLayer("FirstPersonViewmodel");
            var mainData = Camera.main.GetUniversalAdditionalCameraData();
            var overlay = look.ViewmodelCamera;
            Assert.That(mainData.cameraStack, Does.Contain(overlay));
            Assert.That(overlay.GetUniversalAdditionalCameraData().renderType, Is.EqualTo(CameraRenderType.Overlay));
            Assert.That(overlay.GetUniversalAdditionalCameraData().clearDepth, Is.True);
            Assert.That(overlay.cullingMask, Is.EqualTo(1 << layer));
            Assert.That(Camera.main.cullingMask & (1 << layer), Is.Zero);
            Assert.That(arms.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(arms.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
            foreach (Renderer part in player.transform.Find("Visual").GetComponentsInChildren<Renderer>())
                Assert.That(part.enabled && part.gameObject.activeInHierarchy, Is.True, part.name);
            Assert.That(player.GetComponent<CharacterController>().enabled, Is.True);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator RealMouseBindingsLookImmediatelyAndEscapeClickRespectCursorLock()
        {
            // Batch mode has no focused Game view. Match Unity's InputTestFixture routing
            // without changing the project's input settings asset or replacing the real actions.
            previousInputSettings = InputSystem.settings;
            InputSystem.settings = Object.Instantiate(previousInputSettings);
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            #if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            #endif
            mouse = InputSystem.AddDevice<Mouse>(); keyboard = InputSystem.AddDevice<Keyboard>();
            yield return null;
            var cursor = Object.FindAnyObjectByType<CursorLockController>();
            cursor.LockCursor(); Invoke(cursor, "Update");
            look.SetViewAngles(0, 0);
            InputSystem.QueueDeltaStateEvent(mouse.delta, new Vector2(100, 50)); UpdatePlayerInput();
            Assert.That(mouse.delta.ReadValue().x, Is.EqualTo(100), "Queued mouse event was not routed to the player input update.");
            var cameraInput = (GameInput)typeof(FirstPersonCamera).GetField("input", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(look);
            var action = (InputAction)typeof(GameInput).GetField("look", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(cameraInput);
            Assert.That(action.enabled, Is.True, "Look action disabled.");
            Assert.That(cameraInput.Look.x, Is.EqualTo(100), "The camera's actual look action did not receive the mouse delta.");
            Invoke(look, "Update");
            Assert.That(look.Yaw, Is.EqualTo(10f).Within(.01f));
            Assert.That(look.Pitch, Is.EqualTo(-5f).Within(.01f));
            Assert.That(Quaternion.Angle(player.transform.rotation, Quaternion.Euler(0, 10, 0)), Is.LessThan(.001f));
            look.SetViewAngles(90, -500); Assert.That(look.Pitch, Is.EqualTo(-80));
            look.SetViewAngles(90, 500); Assert.That(look.Pitch, Is.EqualTo(75));

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); UpdatePlayerInput(); Invoke(cursor, "Update");
            Assert.That(cursor.IsLocked, Is.False);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.QueueDeltaStateEvent(mouse.delta, new Vector2(150, -100)); UpdatePlayerInput(); Invoke(look, "Update");
            Assert.That(look.Yaw, Is.EqualTo(90));
            Assert.That(look.Pitch, Is.EqualTo(75));
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left)); UpdatePlayerInput(); Invoke(cursor, "Update");
            Assert.That(cursor.IsLocked && cursor.JustLockedThisFrame, Is.True);
            Invoke(player.GetComponent<LumberjackEquipment>(), "Update");
            Assert.That(player.GetComponent<LumberjackEquipment>().AttackCount, Is.Zero, "Recapture click must not attack.");
            InputSystem.QueueStateEvent(mouse, new MouseState()); UpdatePlayerInput();
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left)); UpdatePlayerInput();
            Invoke(cursor, "Update"); Invoke(player.GetComponent<LumberjackEquipment>(), "Update");
            Assert.That(player.GetComponent<LumberjackEquipment>().AttackCount, Is.EqualTo(1), "A subsequent actual mouse click must attack.");
            InputSystem.QueueStateEvent(mouse, new MouseState());
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift, Key.Space)); UpdatePlayerInput();
            Assert.That(cameraInput.Move, Is.EqualTo(Vector2.up));
            Assert.That(cameraInput.SprintHeld && cameraInput.JumpPressed, Is.True);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); UpdatePlayerInput();
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator StrafeBackpedalSprintJumpAndCollisionKeepDirectCamera()
        {
            look.SetViewAngles(0, 0);
            Vector3 start = player.transform.position;
            player.SetTestInput(Vector2.right, false);
            yield return new WaitForSeconds(.35f);
            Assert.That(player.transform.position.x - start.x, Is.GreaterThan(.5f));
            Assert.That(Quaternion.Angle(player.transform.rotation, Quaternion.identity), Is.LessThan(.01f));
            player.SetTestInput(Vector2.down, false);
            float z = player.transform.position.z;
            yield return new WaitForSeconds(.4f);
            Assert.That(player.transform.position.z, Is.LessThan(z - .5f));
            Assert.That(Quaternion.Angle(player.transform.rotation, Quaternion.identity), Is.LessThan(.01f));
            player.SetTestInput(Vector2.up, false, true);
            yield return new WaitForSeconds(.85f);
            Assert.That(player.IsSprinting, Is.True); Assert.That(player.PlanarSpeed, Is.GreaterThan(9f));
            float groundHeight = player.transform.position.y;
            player.SetTestInput(Vector2.zero, true);
            yield return new WaitForSeconds(.15f);
            Assert.That(player.IsGrounded, Is.False);
            Assert.That(player.transform.position.y, Is.GreaterThan(groundHeight + .5f));
            Invoke(look, "LateUpdate");
            Assert.That(Vector3.Distance(look.transform.position, look.EyePosition), Is.LessThan(.0001f));
            yield return new WaitForSeconds(1.4f);
            Assert.That(player.IsGrounded, Is.True);
            player.Warp(new Vector3(0, 1.2f, -20));
            temporaryWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            temporaryWall.transform.position = new Vector3(0, 1, -18);
            temporaryWall.transform.localScale = new Vector3(4, 3, .2f);
            player.SetTestInput(Vector2.up, false, true);
            yield return new WaitForSeconds(1f);
            Assert.That(player.transform.position.z, Is.LessThan(-18.5f), "Character collision was lost.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator InventoryAndExistingChopDriveTheFirstPersonAxe()
        {
            var equipment = player.GetComponent<LumberjackEquipment>();
            var animation = player.GetComponent<LumberjackVisual>();
            Assert.That(Object.FindAnyObjectByType<InventoryHud>().SlotCount, Is.EqualTo(4));
            Assert.That(arms.AxeVisible, Is.True);
            equipment.SelectSlot(2); yield return null; Invoke(arms, "LateUpdate");
            Assert.That(arms.AxeVisible, Is.False); Assert.That(equipment.TriggerAttack(), Is.False);
            equipment.SelectSlot(0); yield return null; Invoke(arms, "LateUpdate");
            Assert.That(arms.AxeVisible, Is.True);
            Vector3 rest = arms.transform.InverseTransformPoint(arms.RightWrist.position);
            Quaternion idleAxe = arms.Axe.rotation;
            Assert.That(equipment.TriggerAttack(), Is.True); Assert.That(equipment.TriggerAttack(), Is.False);
            yield return new WaitForSeconds(.4f); Invoke(arms, "LateUpdate");
            Assert.That(animation.IsAttacking, Is.True);
            Assert.That(Vector3.Distance(arms.transform.InverseTransformPoint(arms.RightWrist.position), rest), Is.GreaterThan(.015f));
            yield return new WaitForSeconds(.34f); Invoke(arms, "LateUpdate");
            Assert.That(Quaternion.Angle(arms.Axe.rotation, idleAxe), Is.GreaterThan(20f));
            yield return new WaitForSeconds(.6f);
            Assert.That(animation.IsAttacking, Is.False); Assert.That(equipment.AttackCount, Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EveryChopPoseKeepsGripJointLengthsAndCameraClearance()
        {
            var camera = look.ViewmodelCamera;
            Transform contact = arms.Axe.Find("Carpentry Axe/Hand Contact");
            camera.aspect = 1.6f;
            for (int sample = 0; sample <= 100; sample++)
            {
                arms.EvaluatePose(sample / 100f);
                Assert.That(Vector3.Distance(contact.position, arms.RightWrist.position), Is.LessThan(.0001f));
                Assert.That(Vector3.Distance(arms.RightElbow.position, arms.RightWrist.position), Is.EqualTo(.38f).Within(.0001f));
                Assert.That(Vector3.Distance(arms.RightElbow.position, arms.RightElbow.parent.position), Is.EqualTo(.35f).Within(.0001f));
                Vector3 forearm = arms.RightWrist.position - arms.RightElbow.position;
                Assert.That(Vector3.Angle(forearm, arms.Axe.up), Is.InRange(47f, 90.1f), "Excessive wrist bend.");
                foreach (Transform hand in new[] { arms.RightWrist, arms.LeftWrist })
                {
                    Vector3 viewport = camera.WorldToViewportPoint(hand.position);
                    Assert.That(viewport.x, Is.InRange(.1f, .9f)); Assert.That(viewport.y, Is.InRange(.02f, .35f));
                }
                foreach (MeshFilter mesh in arms.Axe.GetComponentsInChildren<MeshFilter>())
                    foreach (Vector3 vertex in mesh.sharedMesh.vertices)
                        Assert.That(camera.transform.InverseTransformPoint(mesh.transform.TransformPoint(vertex)).z, Is.GreaterThan(camera.nearClipPlane + .1f));
            }
            foreach (Renderer renderer in arms.GetComponentsInChildren<Renderer>())
            foreach (Material material in renderer.sharedMaterials)
            {
                Assert.That(material != null && material.shader.isSupported, Is.True);
                Assert.That(material.shader.name, Is.Not.EqualTo("Hidden/InternalErrorShader"));
                #if UNITY_EDITOR
                Assert.That(UnityEditor.ShaderUtil.ShaderHasError(material.shader), Is.False);
                #endif
            }
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator OwnBodyOnlyCastsShadowsAndRestoresForOtherCameras()
        {
            var visibility = Camera.main.GetComponent<FirstPersonBodyVisibility>();
            Renderer head = player.transform.Find("Visual/Upper Body/Head Rig/Head").GetComponent<Renderer>();
            var original = head.shadowCastingMode;
            Invoke(visibility, "BeginCamera", default(ScriptableRenderContext), Camera.main);
            Assert.That(head.shadowCastingMode, Is.EqualTo(ShadowCastingMode.ShadowsOnly));
            Assert.That(head.enabled && head.gameObject.activeInHierarchy, Is.True);
            Assert.That(arms.GetComponentsInChildren<Renderer>().All(r => r.forceRenderingOff), Is.True);
            Invoke(visibility, "EndCamera", default(ScriptableRenderContext), Camera.main);
            Assert.That(head.shadowCastingMode, Is.EqualTo(original));
            Invoke(visibility, "BeginCamera", default(ScriptableRenderContext), look.ViewmodelCamera);
            Assert.That(head.shadowCastingMode, Is.EqualTo(original));
            Assert.That(arms.GetComponentsInChildren<Renderer>().All(r => !r.forceRenderingOff), Is.True);
            Invoke(visibility, "EndCamera", default(ScriptableRenderContext), look.ViewmodelCamera);
            temporaryWall = new GameObject("Other World Camera");
            Camera other = temporaryWall.AddComponent<Camera>(); other.enabled = false;
            Invoke(visibility, "BeginCamera", default(ScriptableRenderContext), other);
            Assert.That(head.shadowCastingMode, Is.EqualTo(original));
            Assert.That(arms.GetComponentsInChildren<Renderer>().All(r => r.forceRenderingOff), Is.True);
            Invoke(visibility, "EndCamera", default(ScriptableRenderContext), other);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CloseObstacleRetractsAxeWithoutHidingIt()
        {
            temporaryWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            temporaryWall.transform.position = look.transform.position + look.transform.forward * .7f;
            temporaryWall.transform.localScale = new Vector3(2, 3, .1f);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(.5f);
            Assert.That(arms.WallRetraction, Is.GreaterThan(.3f));
            Assert.That(arms.AxeVisible, Is.True);
            Assert.That(arms.transform.localPosition.z, Is.LessThan(-.03f));
            Vector3 hand = look.ViewmodelCamera.WorldToViewportPoint(arms.RightWrist.position);
            Assert.That(hand.z, Is.GreaterThan(.4f));
            LogAssert.NoUnexpectedReceived();
        }

        private static void Invoke(object target, string name, params object[] arguments) =>
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);

        private static void UpdatePlayerInput()
        {
            // InputSystem.Update() chooses Editor updates when the batch Game view lacks focus.
            // Explicitly advance the real Dynamic input buffer; editor updates do not fire actions.
            typeof(InputSystem).GetMethod("Update", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(InputUpdateType) }, null).Invoke(null, new object[] { InputUpdateType.Dynamic });
        }
    }
}
