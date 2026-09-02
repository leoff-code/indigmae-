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
    public sealed class DenseForestPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("CrystalSprint");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExpandedTerrainPreservesPondAndHasContinuousSteepBoundary()
        {
            MeshCollider ground = GameObject.Find("Ground").GetComponent<MeshCollider>();
            float areaRatio = ground.bounds.size.x * ground.bounds.size.z / (96f * 96f);
            Assert.That(areaRatio, Is.InRange(1.95f, 2.05f));
            #if UNITY_EDITOR
            Mesh old = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/RollingMeadowTerrain.asset");
            Vector3[] previous = old.vertices, expanded = ground.sharedMesh.vertices;
            for (int z = 0; z < 65; z++)
            for (int x = 0; x < 65; x++)
                Assert.That(Vector3.Distance(previous[z * 65 + x], expanded[(z + 13) * 91 + x + 13]), Is.LessThan(.001f), "Original terrain/pond changed.");
            #endif
            MeshCollider mountain = GameObject.Find("Continuous Mountain Terrain").GetComponent<MeshCollider>();
            for (int angle = 0; angle < 360; angle += 15)
            {
                Vector3 outward = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                foreach (float radius in new[] { 59f, 65f, 73f, 77f })
                {
                    Vector3 point = outward * radius + Vector3.up * 50f;
                    Assert.That(mountain.Raycast(new Ray(point, Vector3.down), out RaycastHit hit, 80f), Is.True, "Gap in boundary at " + angle);
                    if (radius == 77f) Assert.That(Vector3.Angle(hit.normal, Vector3.up), Is.GreaterThan(52f), "Outer mountain is an escape ramp.");
                }
            }
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            player.Warp(new Vector3(0f, 1.5f, 51f)); player.SetTestInput(Vector2.zero, false);
            yield return new WaitForSeconds(1f);
            Assert.That(player.IsGrounded, Is.True, "Expanded area is not walkable.");
            Assert.That(player.transform.position.y, Is.GreaterThan(-1f));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator KitVegetationIsDenseVariedGroundedAndRoutesStayClear()
        {
            EnvironmentAssetInstance[] items = Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None);
            EnvironmentAssetInstance[] trees = items.Where(i => i.Kind == EnvironmentAssetKind.Tree).ToArray();
            EnvironmentAssetInstance[] bushes = items.Where(i => i.Kind == EnvironmentAssetKind.Bush).ToArray();
            Assert.That(trees.Length, Is.EqualTo(240)); Assert.That(bushes.Length, Is.EqualTo(140));
            Assert.That(trees.Select(t => t.SourcePrefab).Distinct().Count(), Is.EqualTo(10));
            Assert.That(bushes.Select(t => t.SourcePrefab).Distinct().Count(), Is.EqualTo(4));
            foreach (EnvironmentAssetInstance tree in trees)
            {
                Assert.That(tree.SourcePrefab, Does.StartWith(ForestWorld.Kit));
                Vector2 p = new(tree.transform.position.x, tree.transform.position.z);
                Assert.That(ForestWorld.PathDistance(p), Is.GreaterThanOrEqualTo(4.5f));
                Assert.That(tree.GetComponentsInChildren<Collider>().Count(c => c.enabled), Is.EqualTo(1));
                Assert.That(tree.GetComponent<LODGroup>().GetLODs().Length, Is.EqualTo(3));
            }
            foreach (EnvironmentAssetInstance bush in bushes)
                Assert.That(bush.GetComponentsInChildren<Collider>().All(c => !c.enabled), Is.True, "Bush blocks walking.");
            InstancedForestGrass grass = Object.FindAnyObjectByType<InstancedForestGrass>();
            Assert.That(grass.InstanceCount, Is.GreaterThan(24000));
            Assert.That(grass.transform.childCount, Is.Zero, "Grass uses individual GameObjects.");
            Assert.That(grass.SourceMeshes.Select(m => m.name).Distinct().Count(), Is.EqualTo(3));
            Assert.That(grass.SourceMeshes.All(m => m.vertexCount > 200), Is.True, "Kit mesh was replaced by placeholder blades.");
            #if UNITY_EDITOR
            foreach (Mesh mesh in grass.SourceMeshes)
                Assert.That(UnityEditor.AssetDatabase.GetAssetPath(mesh), Does.StartWith("Assets/Meshes/ForestKit/S_Grass"));
            #endif
            Assert.That(GameObject.Find("Vegetation Credit").GetComponent<Text>().text, Does.Contain("LUX ART STUDIOS"));
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator GrassSamplingMatchesColliderAndDoesNotReactToAnAirbornePlayer()
        {
            InstancedForestGrass grass = Object.FindAnyObjectByType<InstancedForestGrass>();
            MeshCollider ground = GameObject.Find("Ground").GetComponent<MeshCollider>();
            for (float z = -54.7f; z < 55f; z += 6.3f)
            for (float x = -54.2f; x < 55f; x += 7.7f)
            {
                Assert.That(ground.Raycast(new Ray(new Vector3(x, 20f, z), Vector3.down), out RaycastHit hit, 40f), Is.True);
                Assert.That(grass.SampleGround(x, z), Is.EqualTo(hit.point.y).Within(.003f), "Floating grass root.");
            }
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            player.Warp(new Vector3(0f, 1.1f, -20f)); player.SetTestInput(Vector2.zero, false);
            yield return new WaitForSeconds(.6f);
            Assert.That(Shader.GetGlobalVector("_GrassInteractor").w, Is.GreaterThan(1f));
            player.SetTestInput(Vector2.zero, true);
            yield return new WaitForSeconds(.15f);
            Assert.That(player.IsGrounded, Is.False);
            Assert.That(Shader.GetGlobalVector("_GrassInteractor").w, Is.Zero);
            yield return new WaitForSeconds(1.5f);
            Assert.That(player.IsGrounded, Is.True);
            Assert.That(Shader.GetGlobalVector("_GrassInteractor").w, Is.GreaterThan(1f));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
