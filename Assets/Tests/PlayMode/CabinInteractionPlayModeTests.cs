using System.Collections;
using System.Linq;
using System.Reflection;
using CrystalSprint;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CrystalSprintTests
{
    public sealed class TestUsableObject : MonoBehaviour, IInteractable
    {
        public bool CanInteract => isActiveAndEnabled;
        public int Uses { get; private set; }
        public void Interact(PlayerInteractor user) => Uses++;
    }

    public sealed class CabinInteractionPlayModeTests
    {
        private PondCabin cabin;
        private PlayerController player;
        private FirstPersonCamera look;
        private PlayerInteractor user;
        private HingedDoorInteractable door;
        private Keyboard keyboard;
        private InputSettings previousSettings;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("CrystalSprint"); yield return null;
            cabin = Object.FindAnyObjectByType<PondCabin>(); player = Object.FindAnyObjectByType<PlayerController>();
            look = Object.FindAnyObjectByType<FirstPersonCamera>(); user = player.GetComponent<PlayerInteractor>();
            door = cabin.GetComponentInChildren<HingedDoorInteractable>(); player.SetTestInput(Vector2.zero, false);
            previousSettings = InputSystem.settings; InputSystem.settings = Object.Instantiate(previousSettings);
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            #if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            #endif
            keyboard = InputSystem.AddDevice<Keyboard>();
            Object.FindAnyObjectByType<CursorLockController>().LockCursor();
            yield return null;
        }

        [TearDown]
        public void Cleanup()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (previousSettings != null)
            { var temporary = InputSystem.settings; InputSystem.settings = previousSettings; Object.Destroy(temporary); }
        }

        private static void UpdateInput() => typeof(InputSystem).GetMethod("Update", BindingFlags.Static | BindingFlags.NonPublic,
            null, new[] { typeof(InputUpdateType) }, null).Invoke(null, new object[] { InputUpdateType.Dynamic });
        private void PressE()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); UpdateInput();
            typeof(PlayerInteractor).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(user, null);
        }
        private void ReleaseE() { InputSystem.QueueStateEvent(keyboard, new KeyboardState()); UpdateInput(); }
        private void Aim(Vector3 target)
        {
            Vector3 direction = (target - look.EyePosition).normalized;
            look.SetViewAngles(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, -Mathf.Asin(direction.y) * Mathf.Rad2Deg);
            Physics.SyncTransforms(); user.RefreshTarget();
        }
        // Stand within Use range, but outside the leaf's swing. The obstructed case below
        // deliberately stands in that swing and verifies that no pushing occurs.
        private void DoorApproach() => player.Warp(cabin.transform.TransformPoint(new Vector3(.966f, .254f, 5.6f)) + Vector3.up * 1.1f);

        [UnityTest]
        public IEnumerator EOpensAndClosesDoorOncePerPressAtTheOriginalHinge()
        {
            DoorApproach(); yield return new WaitForSeconds(.6f);
            Vector3 pivot = door.transform.localPosition;
            Aim(door.GetComponent<Renderer>().bounds.center); Assert.That(user.Target, Is.SameAs(door));
            PressE(); Assert.That(door.InteractionCount, Is.EqualTo(1));
            yield return new WaitForSeconds(.3f);
            Assert.That(door.OpenAmount, Is.InRange(.1f, .9f), "Door teleported rather than animated.");
            yield return new WaitForSeconds(.65f);
            Assert.That(door.IsOpen, Is.True); Assert.That(door.InteractionCount, Is.EqualTo(1), "Holding E must not repeat.");
            Assert.That(door.transform.localPosition, Is.EqualTo(pivot));
            Assert.That(door.transform.localEulerAngles.y, Is.EqualTo(95).Within(.1f));
            ReleaseE(); yield return null;
            Aim(door.GetComponent<Renderer>().bounds.center); Assert.That(user.Target, Is.SameAs(door));
            PressE(); ReleaseE(); yield return new WaitForSeconds(1);
            Assert.That(door.OpenAmount, Is.Zero); Assert.That(door.InteractionCount, Is.EqualTo(2));
            Assert.That(player.GetComponent<LumberjackEquipment>().AttackCount, Is.Zero);
            Assert.That(cabin.GetComponentsInChildren<CurtainInteractable>().Sum(c => c.InteractionCount), Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ClosedDoorBlocksCharacterControllerFromBothSides()
        {
            CharacterController body = player.GetComponent<CharacterController>();
            Vector3 point = cabin.transform.TransformPoint(new Vector3(.966f, 1.1f, 3.69f));
            foreach (int side in new[] { -1, 1 })
            {
                Vector3 direction = cabin.ExitDirection * side;
                player.Warp(point + direction * 1.1f);
                for (int i = 0; i < 40; i++) body.Move(-direction * .09f);
                Assert.That(Vector3.Dot(player.transform.position - point, direction), Is.GreaterThan(.20f));
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator DoorSweepAvoidsSolidWallsAndPausesForPlayer()
        {
            BoxCollider leaf = door.GetComponent<BoxCollider>();
            foreach (int angle in Enumerable.Range(0, 96))
            foreach (Collider solid in cabin.GetComponentsInChildren<Collider>())
            {
                if (!solid.enabled || solid.isTrigger || solid.transform.IsChildOf(door.transform)) continue;
                bool overlap = Physics.ComputePenetration(leaf, door.transform.position, cabin.transform.rotation * Quaternion.Euler(0, angle, 0),
                    solid, solid.transform.position, solid.transform.rotation, out _, out float depth);
                Assert.That(!overlap || depth < .002f, Is.True, $"Door clips {solid.name} by {depth} at {angle} degrees.");
            }
            player.Warp(cabin.transform.TransformPoint(new Vector3(1.15f, .254f, 4.2f)) + Vector3.up * 1.1f);
            yield return new WaitForSeconds(.6f); Vector3 start = player.transform.position;
            var body = player.GetComponent<CharacterController>();
            bool predicted = Physics.ComputePenetration(leaf, door.transform.position, cabin.transform.rotation * Quaternion.Euler(0, 45, 0),
                body, body.transform.position, body.transform.rotation, out _, out float predictedDepth);
            door.Interact(user); yield return new WaitForSeconds(1.1f);
            Assert.That(door.IsObstructed, Is.True, $"Guard: predicted45={predicted}/{predictedDepth}, start={start:F3}, now={player.transform.position:F3}, open={door.OpenAmount}, uses={door.InteractionCount}"); Assert.That(door.OpenAmount, Is.LessThan(1));
            Assert.That(Vector2.Distance(new Vector2(start.x, start.z), new Vector2(player.transform.position.x, player.transform.position.z)), Is.LessThan(.1f));
            player.Warp(cabin.Approach + Vector3.up * 1.1f); yield return new WaitForSeconds(1);
            Assert.That(door.IsOpen, Is.True); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EveryCurtainPairUsesEAndGathersBothSidesWithoutChangingHeight()
        {
            CurtainInteractable[] curtains = cabin.GetComponentsInChildren<CurtainInteractable>();
            Assert.That(curtains.Length, Is.EqualTo(4));
            foreach (CurtainInteractable curtain in curtains)
            {
                Vector3 inward = cabin.transform.position - curtain.transform.position; inward.y = 0; inward.Normalize();
                Vector3 position = curtain.transform.position + inward * 1.6f; position.y = cabin.Interior.y + 1.1f;
                player.Warp(position); yield return new WaitForSeconds(.5f);
                bool beganOpen = curtain.IsOpen;
                float left = curtain.LeftPanel.localPosition.z, right = curtain.RightPanel.localPosition.z;
                float height = curtain.LeftPanel.GetComponent<Renderer>().bounds.size.y;
                Aim(curtain.LeftPanel.GetComponent<Renderer>().bounds.center);
                Assert.That(user.Target, Is.SameAs(curtain), curtain.name + " cannot be selected from inside.");
                int otherUses = curtains.Where(c => c != curtain).Sum(c => c.InteractionCount);
                PressE(); ReleaseE(); yield return new WaitForSeconds(.35f);
                Assert.That(curtain.OpenAmount, Is.InRange(.1f, .9f));
                Assert.That(curtain.LeftPanel.GetComponent<Renderer>().bounds.size.y, Is.EqualTo(height).Within(.001f));
                Assert.That(curtain.LeftPanel.localPosition.z, beganOpen ? Is.GreaterThan(left) : Is.LessThan(left));
                Assert.That(curtain.RightPanel.localPosition.z, beganOpen ? Is.LessThan(right) : Is.GreaterThan(right));
                yield return new WaitForSeconds(.65f);
                Assert.That(curtain.IsOpen, Is.EqualTo(!beganOpen));
                Aim(curtain.LeftPanel.GetComponent<Renderer>().bounds.center); Assert.That(user.Target, Is.SameAs(curtain));
                PressE(); ReleaseE(); yield return new WaitForSeconds(1);
                Assert.That(curtain.IsOpen, Is.EqualTo(beganOpen));
                Assert.That(curtain.InteractionCount, Is.EqualTo(2));
                Assert.That(curtains.Where(c => c != curtain).Sum(c => c.InteractionCount), Is.EqualTo(otherUses));
            }
            Assert.That(door.InteractionCount, Is.Zero); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator GenericUseSelectsOnlyOneObjectAndHonorsOcclusionRangeAndCursor()
        {
            player.Warp(new Vector3(0, 1.2f, -20)); look.SetViewAngles(0, 0); yield return new WaitForSeconds(.6f);
            Vector3 eye = look.EyePosition;
            GameObject Make(string name, float distance)
            { GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = name; cube.transform.position = eye + Vector3.forward * distance; cube.transform.localScale = Vector3.one * .4f; return cube; }
            GameObject first = Make("Generic usable front", 1.5f), second = Make("Generic usable rear", 2.3f);
            TestUsableObject a = first.AddComponent<TestUsableObject>(), b = second.AddComponent<TestUsableObject>();
            Aim(first.transform.position); Assert.That(user.Target, Is.SameAs(a));
            PressE(); Assert.That(a.Uses, Is.EqualTo(1)); Assert.That(b.Uses, Is.Zero); ReleaseE(); yield return null;
            GameObject obstruction = Make("Nonusable solid occluder", .85f); Physics.SyncTransforms();
            user.RefreshTarget(); Assert.That(user.Target, Is.Null); PressE(); ReleaseE(); Assert.That(a.Uses, Is.EqualTo(1));
            Object.Destroy(obstruction); yield return null;
            var cursor = Object.FindAnyObjectByType<CursorLockController>(); cursor.ReleaseCursor();
            PressE(); ReleaseE(); Assert.That(a.Uses, Is.EqualTo(1)); yield return null; cursor.LockCursor();
            first.transform.position += Vector3.forward * 5; second.transform.position += Vector3.forward * 5;
            Physics.SyncTransforms(); user.RefreshTarget(); Assert.That(user.Target, Is.Null);
            PressE(); ReleaseE(); Assert.That(a.Uses, Is.EqualTo(1)); Assert.That(b.Uses, Is.Zero);
            Object.Destroy(first); Object.Destroy(second); yield return null; LogAssert.NoUnexpectedReceived();
        }
    }
}
