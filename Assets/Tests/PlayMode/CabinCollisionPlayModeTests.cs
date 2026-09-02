using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CrystalSprint;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CrystalSprintTests
{
    public sealed class CabinCollisionPlayModeTests
    {
        private PondCabin cabin;
        private PlayerController player;
        private CharacterController body;
        private FirstPersonCamera look;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("CrystalSprint"); yield return null;
            cabin = Object.FindAnyObjectByType<PondCabin>(); player = Object.FindAnyObjectByType<PlayerController>();
            body = player.GetComponent<CharacterController>(); look = Object.FindAnyObjectByType<FirstPersonCamera>();
            player.SetTestInput(Vector2.zero, false);
            HingedDoorInteractable door = cabin.GetComponentInChildren<HingedDoorInteractable>();
            if (door != null && !door.IsOpen) { door.Interact(null); yield return new WaitForSeconds(1f); }
        }

        private void Probe(Vector3 localPoint, Vector3 localOutward, bool outside, List<string> failures)
        {
            Vector3 point = cabin.transform.TransformPoint(localPoint);
            Vector3 side = cabin.transform.TransformDirection(localOutward) * (outside ? 1 : -1);
            player.Warp(point + side * 1.1f);
            // Real, unchanged CharacterController, tested at both walking and jumping heights.
            // Substeps omit gravity so the elevated window test cannot pass by hitting its sill.
            for (int step = 0; step < 40; step++) body.Move(-side * .09f);
            float clearance = Vector3.Dot(player.transform.position - point, side);
            if (clearance < body.radius * .5f) failures.Add($"{localPoint:F2} {(outside ? "outside->inside" : "inside->outside")}: clearance={clearance:F3}");
        }

        private void Walls(bool outside)
        {
            var failures = new List<string>();
            foreach (float height in new[] { 1.06f, 2.1f })
            {
                foreach (float z in new[] { -3f, -2f, -1f, 0f, 1f, 2f, 3f })
                { Probe(new Vector3(-2.76f, height, z), Vector3.left, outside, failures); Probe(new Vector3(2.76f, height, z), Vector3.right, outside, failures); }
                foreach (float x in new[] { -2.2f, -1.1f, 0f, 1.1f, 2.2f }) Probe(new Vector3(x, height, -3.77f), Vector3.back, outside, failures);
                foreach (float x in new[] { -2.2f, -1.05f, -.1f, 2.2f }) Probe(new Vector3(x, height, 3.77f), Vector3.forward, outside, failures);
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private void Windows(bool outside)
        {
            var failures = new List<string>();
            foreach (float offset in new[] { -.45f, 0f, .45f })
            {
                Probe(new Vector3(-2.716f, 1.85f, -.02f + offset), Vector3.left, outside, failures);
                Probe(new Vector3(2.671f, 1.85f, -.02f + offset), Vector3.right, outside, failures);
                Probe(new Vector3(-1.053f + offset, 1.85f, 3.696f), Vector3.forward, outside, failures);
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest] public IEnumerator WallsBlockOutsideToInside() { Walls(true); yield return null; }
        [UnityTest] public IEnumerator WallsBlockInsideToOutside() { Walls(false); yield return null; }
        [UnityTest] public IEnumerator WindowsBlockOutsideToInside() { Windows(true); yield return null; }
        [UnityTest] public IEnumerator WindowsBlockInsideToOutside() { Windows(false); yield return null; }

        [UnityTest]
        public IEnumerator DoorwayAllowsActualWalkingInside()
        {
            player.Warp(cabin.Approach + Vector3.up * 1.1f); look.SetViewAngles(0, 0);
            yield return new WaitForSeconds(.6f); player.SetTestInput(Vector2.up, false);
            yield return new WaitForSeconds(1.6f); player.SetTestInput(Vector2.zero, false); yield return new WaitForSeconds(.25f);
            Assert.That(cabin.transform.InverseTransformPoint(player.transform.position).z, Is.InRange(-2.5f, 3.1f));
            Assert.That(player.IsGrounded, Is.True); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DoorwayAllowsActualWalkingOutside()
        {
            player.Warp(cabin.Interior + Vector3.up * 1.1f); look.SetViewAngles(180, 0);
            yield return new WaitForSeconds(.6f); player.SetTestInput(Vector2.up, false);
            yield return new WaitForSeconds(1.6f); player.SetTestInput(Vector2.zero, false); yield return new WaitForSeconds(.25f);
            Assert.That(cabin.transform.InverseTransformPoint(player.transform.position).z, Is.GreaterThan(7f));
            Assert.That(player.IsGrounded, Is.True); LogAssert.NoUnexpectedReceived();
        }
    }
}
