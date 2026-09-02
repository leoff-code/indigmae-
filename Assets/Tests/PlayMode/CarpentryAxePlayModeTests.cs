using System.Collections;
using System.Linq;
using CrystalSprint;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CrystalSprintTests
{
    public sealed class CarpentryAxePlayModeTests
    {
        private PlayerController player;
        private Transform held;
        private Transform model;
        private Transform hand;
        private Transform contact;

        [UnitySetUp]
        public IEnumerator LoadGame()
        {
            yield return SceneManager.LoadSceneAsync("CrystalSprint", LoadSceneMode.Single);
            yield return null;
            player = Object.FindAnyObjectByType<PlayerController>();
            hand = player.transform.Find("Visual/Upper Body/Right Arm Pivot/Right Elbow/Right Wrist/Right Hand");
            held = player.transform.Find("Visual/Upper Body/Right Arm Pivot/Right Elbow/Right Wrist/Axe Grip/Held Axe");
            model = held.Find("Carpentry Axe");
            Assert.That(model, Is.Not.Null, "The old axe is still equipped.");
            contact = model.Find("Hand Contact");
        }

        [UnityTest]
        public IEnumerator ImportedAxeAndRenderedIconKeepFourSlotInventory()
        {
            Assert.That(held.childCount, Is.EqualTo(1), "Duplicate or old axe parts remain equipped.");
            Assert.That(held.GetComponentsInChildren<Renderer>().Length, Is.EqualTo(1));
            Assert.That(held.GetComponentsInChildren<Collider>().Length, Is.Zero, "Loose-prop collisions interfere with the player.");
            Assert.That(held.GetComponentsInChildren<Rigidbody>().Length, Is.Zero, "Loose-prop physics detaches the axe.");
            Mesh mesh = model.GetComponent<MeshFilter>().sharedMesh;
            Material material = model.GetComponent<Renderer>().sharedMaterial;
            Assert.That(mesh.vertexCount, Is.GreaterThan(100));
            Assert.That(material.name, Is.EqualTo("Axes"));
            Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
            Assert.That(material.shader.isSupported, Is.True);
            foreach (string property in new[] { "_BaseMap", "_BumpMap", "_SpecGlossMap", "_OcclusionMap" })
                Assert.That(material.GetTexture(property), Is.Not.Null, property);
            #if UNITY_EDITOR
            Assert.That(UnityEditor.AssetDatabase.GetAssetPath(mesh), Does.EndWith("Carpentry_Tools/Meshes/Axe_Straight.fbx"));
            Assert.That(UnityEditor.AssetDatabase.GetAssetPath(material), Does.EndWith("Carpentry_Tools/Materials/Axes.mat"));
            Assert.That(UnityEditor.ShaderUtil.ShaderHasError(material.shader), Is.False);
            LumberjackVisual prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LumberjackPlayer.prefab").GetComponent<LumberjackVisual>();
            Transform prefabAxe = prefab.RightWrist.Find("Axe Grip/Held Axe");
            Assert.That(Vector3.Angle(prefabAxe.up, prefab.RightWrist.position - prefab.RightElbow.position), Is.EqualTo(90f).Within(.2f), "Edit-mode reference pose has the wrong grip.");
            #endif
            Texture2D icon = GameObject.Find("Inventory Slot 1").GetComponentInChildren<RawImage>().texture as Texture2D;
            Assert.That(icon, Is.Not.Null);
            Assert.That(icon.width, Is.EqualTo(256));
            Color32[] pixels = icon.GetPixels32();
            Assert.That(pixels.Count(pixel => pixel.a > 32), Is.GreaterThan(1000), "Icon is empty.");
            Assert.That(pixels[0].a, Is.Zero, "Icon background should be transparent.");
            Assert.That(pixels.Count(pixel => pixel.a > 128 && pixel.r > 220 && pixel.b > 220 && pixel.g < 30), Is.Zero, "Icon contains magenta error pixels.");
            Assert.That(Object.FindAnyObjectByType<InventoryHud>().SlotCount, Is.EqualTo(4));
            LumberjackEquipment equipment = player.GetComponent<LumberjackEquipment>();
            Assert.That(equipment.AxeEquipped, Is.True);
            for (int slot = 1; slot < 4; slot++)
            {
                equipment.SelectSlot(slot);
                yield return null;
                Assert.That(held.gameObject.activeSelf, Is.False);
                Assert.That(equipment.TriggerAttack(), Is.False);
            }
            equipment.SelectSlot(0);
            yield return null;
            Assert.That(held.gameObject.activeSelf, Is.True);
            AssertGrip();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator HandleStaysPerpendicularDuringMovementSprintAndRepeatedChops()
        {
            AssertGrip();
            // Use the open central approach so movement checks do not depend on obstacle placement.
            player.Warp(new Vector3(0f, 0.3f, -20f));
            yield return new WaitForSeconds(0.3f);
            player.SetTestInput(Vector2.up, false);
            for (int frame = 0; frame < 8; frame++) { yield return new WaitForSeconds(0.1f); AssertGrip(); }
            Assert.That(player.PlanarSpeed, Is.GreaterThan(1f));
            player.SetTestInput(Vector2.up, false, true);
            for (int frame = 0; frame < 8; frame++) { yield return new WaitForSeconds(0.1f); AssertGrip(); }
            Assert.That(player.GetComponent<LumberjackVisual>().SprintBlend, Is.GreaterThan(0.6f));
            player.SetTestInput(Vector2.zero, false);
            yield return new WaitForSeconds(0.4f);
            LumberjackEquipment equipment = player.GetComponent<LumberjackEquipment>();
            LumberjackVisual visual = player.GetComponent<LumberjackVisual>();
            for (int attack = 0; attack < 3; attack++)
            {
                Assert.That(equipment.TriggerAttack(), Is.True);
                Assert.That(visual.LastAttackType, Is.Zero);
                for (int frame = 0; frame < 12; frame++) { yield return new WaitForSeconds(0.1f); AssertGrip(); }
                Assert.That(visual.IsAttacking, Is.False);
            }
            Assert.That(equipment.AttackCount, Is.EqualTo(3));
            LogAssert.NoUnexpectedReceived();
        }

        private void AssertGrip()
        {
            Assert.That(contact, Is.Not.Null);
            LumberjackVisual visual = player.GetComponent<LumberjackVisual>();
            Vector3 forearm = visual.RightWrist.position - visual.RightElbow.position;
            Assert.That(Vector3.Angle(held.up, forearm), Is.EqualTo(90f).Within(.2f), "Axe shaft is not perpendicular to the forearm.");
            Assert.That(Vector3.Distance(hand.position, contact.position), Is.LessThan(.002f), "Grip does not coincide with the hand.");
            Transform torso = player.transform.Find("Visual/Upper Body/Torso");
            foreach (Vector3 vertex in model.GetComponent<MeshFilter>().sharedMesh.vertices)
            {
                Vector3 local = torso.InverseTransformPoint(model.TransformPoint(vertex));
                Assert.That(local.x * local.x + local.z * local.z, Is.GreaterThan(.24f), "Axe mesh penetrates the torso.");
            }
            Assert.That(hand.InverseTransformPoint(contact.position).magnitude, Is.LessThan(0.48f), "Handle contact is outside the spherical hand.");
            Assert.That(held.InverseTransformPoint(contact.position).magnitude, Is.LessThan(0.001f), "The imported handle slipped away from its animated grip pivot.");
            Mesh mesh = model.GetComponent<MeshFilter>().sharedMesh;
            float nearestWood = mesh.vertices.Min(vertex => Vector3.Distance(model.TransformPoint(vertex), contact.position));
            Assert.That(nearestWood, Is.LessThan(0.065f), "Contact marker is not actually on the wooden handle.");
        }
    }
}
