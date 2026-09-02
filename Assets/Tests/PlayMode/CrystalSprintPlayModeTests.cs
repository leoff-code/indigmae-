using System.Collections;
using System.Reflection;
using System.Linq;
using CrystalSprint;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CrystalSprintTests
{
    public sealed class CrystalSprintPlayModeTests
    {
        private const string SceneName = "CrystalSprint";

        [UnitySetUp]
        public IEnumerator LoadGame()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneContainsCompletePlayableGame()
        {
            Assert.That(GameManager.Instance, Is.Not.Null);
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(player, Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<ThirdPersonCamera>(), Is.Not.Null);
            GameObject boundary = GameObject.Find("Mountain Boundary");
            Assert.That(boundary, Is.Not.Null);
            Assert.That(boundary.GetComponentInChildren<MeshCollider>(), Is.Not.Null);
            GameObject trees = GameObject.Find("Trees");
            Assert.That(trees, Is.Not.Null);
            Assert.That(trees.transform.childCount, Is.EqualTo(ForestWorld.TreeCount));
            Assert.That(trees.transform.GetChild(0).GetComponent<EnvironmentAssetInstance>().Kind, Is.EqualTo(EnvironmentAssetKind.Tree));
            Assert.That(trees.GetComponentsInChildren<LODGroup>().Length, Is.EqualTo(ForestWorld.TreeCount));
            Assert.That(GameObject.Find("Natural Details").transform.childCount, Is.EqualTo(26));
            GameObject ground = GameObject.Find("Ground");
            Assert.That(ground.GetComponent<MeshCollider>().sharedMesh.bounds.size.x, Is.EqualTo(ForestWorld.Size).Within(0.1f));
            Assert.That(ground.GetComponent<SurfaceMarker>().Type, Is.EqualTo(SurfaceType.Grass));
            GameObject grass = GameObject.Find("Interactive Grass Field");
            Assert.That(grass, Is.Not.Null);
            Assert.That(grass.GetComponent<InstancedForestGrass>().InstanceCount, Is.GreaterThan(24000));
            Assert.That(GameObject.Find("Meadow Rock Transition").GetComponent<Renderer>().sharedMaterial.shader.name, Is.EqualTo("CrystalSprint/TerrainBlend"));
            GameObject pond = GameObject.Find("Central Pond");
            Assert.That(pond, Is.Not.Null);
            Transform water = pond.transform.Find("Animated Water Surface");
            Assert.That(water.GetComponentInChildren<Renderer>().sharedMaterial.name, Is.EqualTo("Pond_SimpleWater"));
            Assert.That(water.GetComponentInChildren<Renderer>().sharedMaterial.shader.name, Is.EqualTo("Custom/SimpleWaterURP"));
            Assert.That(water.GetComponent<PondSurfaceMotion>(), Is.Not.Null);
            ReflectionProbe pondProbe = pond.GetComponentInChildren<ReflectionProbe>();
            Assert.That(pondProbe, Is.Not.Null);
            Assert.That(pondProbe.mode, Is.EqualTo(UnityEngine.Rendering.ReflectionProbeMode.Realtime));
            Assert.That(player.transform.Find("Visual/Upper Body/Head Rig/Left Pupil"), Is.Not.Null);
            Assert.That(player.transform.Find("Visual/Upper Body/Head Rig/Hat Crown"), Is.Not.Null);
            Assert.That(player.transform.Find("Visual/Upper Body/Head Rig/Beard Center"), Is.Not.Null);
            Assert.That(player.GetComponent<GroundMovementParticles>(), Is.Not.Null);
            Assert.That(player.GetComponent<InteractiveGrass>(), Is.Not.Null);
            Assert.That(player.transform.Find("Visual/Shoulder Log"), Is.Null);
            Assert.That(GameObject.Find("Collectibles"), Is.Null);
            Assert.That(player.GetComponent<LumberjackEquipment>(), Is.Not.Null);
            Assert.That(player.transform.Find("Visual/Upper Body/Right Arm Pivot/Right Elbow/Right Wrist/Axe Grip/Held Axe"), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<FishJumpSystem>().FishVariantCount, Is.EqualTo(3));
            Assert.That(Object.FindAnyObjectByType<InventoryHud>().SlotCount, Is.EqualTo(4));

            Renderer head = player.transform.Find("Visual/Upper Body/Head Rig/Head").GetComponent<Renderer>();
            Renderer eye = player.transform.Find("Visual/Upper Body/Head Rig/Left Eye White").GetComponent<Renderer>();
            Renderer pupil = player.transform.Find("Visual/Upper Body/Head Rig/Left Pupil").GetComponent<Renderer>();
            Renderer beard = player.transform.Find("Visual/Upper Body/Head Rig/Beard Center").GetComponent<Renderer>();
            Renderer hat = player.transform.Find("Visual/Upper Body/Head Rig/Hat Brim").GetComponent<Renderer>();
            Renderer torso = player.transform.Find("Visual/Upper Body/Torso").GetComponent<Renderer>();
            Renderer leftSuspender = player.transform.Find("Visual/Upper Body/Left Suspender").GetComponent<Renderer>();
            Renderer rightSuspender = player.transform.Find("Visual/Upper Body/Right Suspender").GetComponent<Renderer>();
            Renderer belt = player.transform.Find("Visual/Belt").GetComponent<Renderer>();
            Renderer buckle = player.transform.Find("Visual/Buckle").GetComponent<Renderer>();
            Assert.That(head.bounds.Intersects(eye.bounds), Is.True, "Eye is detached from the head.");
            Assert.That(eye.bounds.Intersects(pupil.bounds), Is.True, "Pupil is detached from the eye.");
            Assert.That(head.bounds.Intersects(beard.bounds), Is.True, "Beard is detached from the head.");
            Assert.That(head.bounds.Intersects(hat.bounds), Is.True, "Hat is detached from the head.");
            Assert.That(torso.bounds.Intersects(leftSuspender.bounds), Is.True, "Left suspender is detached from the torso.");
            Assert.That(torso.bounds.Intersects(rightSuspender.bounds), Is.True, "Right suspender is detached from the torso.");
            Assert.That(belt.bounds.Intersects(buckle.bounds), Is.True, "Belt buckle is detached from the belt.");
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ImportedEnvironmentHasPackMaterialsLodsAndGroundContact()
        {
            EnvironmentAssetInstance[] items = Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None);
            Assert.That(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name, Is.EqualTo("ForestURP"));
            Assert.That(items.Count(item => item.Kind == EnvironmentAssetKind.Tree), Is.EqualTo(ForestWorld.TreeCount));
            Assert.That(items.Where(item => item.Kind == EnvironmentAssetKind.Tree).Select(item => item.SourcePrefab).Distinct().Count(), Is.EqualTo(10));
            Assert.That(items.Count(item => item.Kind == EnvironmentAssetKind.Stump), Is.EqualTo(13));
            Assert.That(items.Count(item => item.Kind == EnvironmentAssetKind.Log), Is.GreaterThanOrEqualTo(7));
            Assert.That(items.Count(item => item.Kind == EnvironmentAssetKind.Mushroom), Is.GreaterThanOrEqualTo(42));
            Assert.That(items.Count(item => item.Kind == EnvironmentAssetKind.Branch), Is.GreaterThanOrEqualTo(32));
            foreach (EnvironmentAssetInstance item in items)
            {
                bool kit = item.Kind == EnvironmentAssetKind.Tree || item.Kind == EnvironmentAssetKind.Bush;
                Assert.That(item.SourcePrefab, Does.StartWith(kit ? ForestWorld.Kit : "Assets/InnerverseInteractive/Ultimate Nature – Starter/"));
                Assert.That(item.GetComponent<LODGroup>(), Is.Not.Null, item.name + " lost its supplied LOD group.");
                Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
                foreach (Material material in renderers.SelectMany(renderer => renderer.sharedMaterials))
                {
                    Assert.That(material, Is.Not.Null, item.name);
                    Assert.That(material.shader.name, Is.EqualTo(item.Kind == EnvironmentAssetKind.Water ? "Custom/SimpleWaterURP" : "Universal Render Pipeline/Lit"), item.name);
                    Assert.That(material.shader.isSupported, Is.True, item.name);
                    if (item.Kind != EnvironmentAssetKind.Water) Assert.That(material.GetTexture("_BaseMap"), Is.Not.Null, item.name);
                    #if UNITY_EDITOR
                    Assert.That(UnityEditor.AssetDatabase.GetAssetPath(material), Does.StartWith(item.Kind == EnvironmentAssetKind.Water ? "Assets/Materials/PondCabin/" : kit ? "Assets/Materials/ForestKit/" : "Assets/InnerverseInteractive/"));
                    #endif
                }
                if (item.Kind == EnvironmentAssetKind.Water) continue;
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
                Assert.That(bounds.min.y, Is.LessThanOrEqualTo(item.GroundContact.y + 0.06f), item.name + " is floating above its terrain contact.");
                Assert.That(bounds.max.y, Is.GreaterThan(item.GroundContact.y), item.name + " is buried under the terrain.");
                if (item.Kind == EnvironmentAssetKind.Tree)
                    Assert.That(new Vector2(item.transform.position.x, item.transform.position.z).magnitude, Is.LessThan(ForestWorld.Radius));
                if (item.Kind == EnvironmentAssetKind.Mushroom || item.Kind == EnvironmentAssetKind.Branch)
                    Assert.That(item.GetComponentsInChildren<Collider>().All(collider => !collider.enabled), Is.True, "Small details should not obstruct walking.");
            }
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ImportedWaterMovesAndRipplesFollowItsSurface()
        {
            PondSurfaceMotion water = Object.FindAnyObjectByType<PondSurfaceMotion>();
            Mesh mesh = water.GetComponentInChildren<MeshFilter>().sharedMesh;
            Vector3 before = mesh.vertices[0];
            yield return new WaitForSeconds(0.45f);
            Assert.That(Vector3.Distance(before, mesh.vertices[0]), Is.GreaterThan(0.001f));
            FishJumpSystem fish = Object.FindAnyObjectByType<FishJumpSystem>();
            Assert.That(fish.TriggerJumpNow(), Is.True);
            yield return null;
            WaterRippleEffect ripple = Object.FindAnyObjectByType<WaterRippleEffect>();
            Assert.That(ripple, Is.Not.Null);
            Assert.That(water.ContainsWater(ripple.transform.position), Is.True);
            Assert.That(ripple.transform.position.y, Is.EqualTo(water.SampleHeight(ripple.transform.position) + 0.02f).Within(0.02f));
            yield return new WaitForSeconds(1.5f);
            Assert.That(fish.ActiveFish == null, Is.True);
            Assert.That(Object.FindAnyObjectByType<WaterRippleEffect>(), Is.Not.Null, "Landing ripple was not created.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator InventoryAxeAndSingleArticulatedChopWork()
        {
            LumberjackEquipment equipment = Object.FindAnyObjectByType<LumberjackEquipment>();
            InventoryHud inventory = Object.FindAnyObjectByType<InventoryHud>();
            Assert.That(equipment, Is.Not.Null);
            Assert.That(inventory, Is.Not.Null);
            Assert.That(equipment.SelectedSlot, Is.EqualTo(0));
            Assert.That(equipment.AxeEquipped, Is.True);
            Assert.That(GameObject.Find("Inventory Slot 1").GetComponentInChildren<RawImage>().texture, Is.Not.Null);
            for (int slot = 2; slot <= 4; slot++)
            {
                Assert.That(GameObject.Find($"Inventory Slot {slot}").GetComponentInChildren<RawImage>(), Is.Null, $"Slot {slot} should be empty.");
            }

            equipment.SelectSlot(2);
            yield return null;
            Assert.That(equipment.SelectedSlot, Is.EqualTo(2));
            Assert.That(equipment.AxeEquipped, Is.False);
            Assert.That(equipment.TriggerAttack(), Is.False, "An empty inventory slot could attack.");
            equipment.SelectSlot(0);
            yield return null;
            Assert.That(equipment.AxeEquipped, Is.True);

            LumberjackVisual visual = equipment.GetComponent<LumberjackVisual>();
            Assert.That(LumberjackVisual.AttackVariantCount, Is.EqualTo(1));
            Transform arm = equipment.transform.Find("Visual/Upper Body/Right Arm Pivot");
            Transform body = equipment.transform.Find("Visual/Upper Body");
            Quaternion rest = body.localRotation;
            Assert.That(equipment.TriggerAttack(), Is.True);
            Assert.That(equipment.TriggerAttack(), Is.False, "Attack overlap should be rejected.");
            yield return new WaitForSeconds(.32f);
            Quaternion windup = arm.localRotation;
            Assert.That(Quaternion.Angle(rest, body.localRotation), Is.GreaterThan(8f), "The torso does not participate.");
            yield return new WaitForSeconds(.25f);
            Assert.That(Quaternion.Angle(windup, arm.localRotation), Is.GreaterThan(25f), "No distinct downswing.");
            yield return new WaitForSeconds(.65f);
            Assert.That(visual.IsAttacking, Is.False);
            Assert.That(Quaternion.Angle(rest, body.localRotation), Is.LessThan(1f), "Recovery did not finish.");
            Assert.That(equipment.AttackCount, Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator FishJumpUsesThreeDistinctModelsAndWaterEffects()
        {
            FishJumpSystem fishSystem = Object.FindAnyObjectByType<FishJumpSystem>();
            Assert.That(fishSystem, Is.Not.Null);
            Assert.That(fishSystem.JumpInterval, Is.EqualTo(10f).Within(0.01f));
            Assert.That(fishSystem.FishVariantCount, Is.EqualTo(3));
            Assert.That(fishSystem.FishPrefabs[0].name, Is.EqualTo("Fish_Trout"));
            Assert.That(fishSystem.FishPrefabs[1].name, Is.EqualTo("Fish_Perch"));
            Assert.That(fishSystem.FishPrefabs[2].name, Is.EqualTo("Fish_Pike"));
            Vector3 trout = fishSystem.FishPrefabs[0].transform.Find("Body").localScale;
            Vector3 perch = fishSystem.FishPrefabs[1].transform.Find("Body").localScale;
            Vector3 pike = fishSystem.FishPrefabs[2].transform.Find("Body").localScale;
            Assert.That(Vector3.Distance(trout, perch), Is.GreaterThan(0.15f));
            Assert.That(Vector3.Distance(perch, pike), Is.GreaterThan(0.25f));
            Assert.That(Vector3.Distance(trout, pike), Is.GreaterThan(0.2f));

            Assert.That(fishSystem.TriggerJumpNow(), Is.True);
            yield return null;
            Assert.That(fishSystem.JumpCount, Is.EqualTo(1));
            Assert.That(fishSystem.ActiveFish, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<WaterRippleEffect>(FindObjectsSortMode.None).Length, Is.GreaterThanOrEqualTo(1));
            Assert.That(Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length, Is.GreaterThanOrEqualTo(2), "No water splash was spawned in addition to the movement particles.");
            yield return new WaitForSeconds(1.65f);
            Assert.That(fishSystem.ActiveFish == null, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator FishJumpTimerFiresReliablyAfterTenSeconds()
        {
            FishJumpSystem fishSystem = Object.FindAnyObjectByType<FishJumpSystem>();
            Assert.That(fishSystem.JumpCount, Is.EqualTo(0));
            yield return new WaitForSeconds(10.12f);
            Assert.That(fishSystem.JumpCount, Is.EqualTo(1), "The scheduled fish jump did not fire at the ten-second interval.");
            Assert.That(fishSystem.ActiveFish, Is.Not.Null, "The automatically spawned fish is not following its jump arc.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator GrassInteractionRequiresGroundContact()
        {
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            InteractiveGrass grassInteraction = player.GetComponent<InteractiveGrass>();
            yield return null;
            Assert.That(player.IsGrounded, Is.True);
            Assert.That(grassInteraction.IsInteractingWithGrass, Is.True);

            player.Warp(player.transform.position + Vector3.up * 3f);
            player.SetTestInput(Vector2.zero, false);
            yield return null;
            yield return null;
            Assert.That(player.IsGrounded, Is.False);
            Assert.That(grassInteraction.IsInteractingWithGrass, Is.False, "Airborne player still bends the grass.");
            Assert.That(Shader.GetGlobalVector("_GrassInteractor").w, Is.EqualTo(0f).Within(0.001f));
            Assert.That(Shader.GetGlobalVector("_FoliageInteractor").w, Is.GreaterThan(1f));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CursorLocksEscReleasesAndClickLocksAgain()
        {
            CursorLockController cursorLock = Object.FindAnyObjectByType<CursorLockController>();
            Assert.That(cursorLock, Is.Not.Null);
            cursorLock.LockCursor();
            Assert.That(cursorLock.IsLocked, Is.True);
            if (!Application.isBatchMode)
            {
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.Locked));
                Assert.That(Cursor.visible, Is.False);
            }

            MethodInfo processInput = typeof(CursorLockController).GetMethod("ProcessInput", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(processInput, Is.Not.Null);
            processInput.Invoke(cursorLock, new object[] { true, false });
            Assert.That(cursorLock.IsLocked, Is.False);
            if (!Application.isBatchMode)
            {
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
                Assert.That(Cursor.visible, Is.True);
            }

            processInput.Invoke(cursorLock, new object[] { false, true });
            Assert.That(cursorLock.IsLocked, Is.True);
            if (!Application.isBatchMode)
            {
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.Locked));
                Assert.That(Cursor.visible, Is.False);
            }

            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator StumpCollidersGiveAStableLandingAndUseWoodEffects()
        {
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            Transform obstacleGroup = GameObject.Find("Obstacles").transform.GetChild(0);
            Assert.That(obstacleGroup.GetComponent<BoxCollider>(), Is.Null, "The obsolete invisible group collider still exists.");
            Assert.That(GameObject.Find("Continuous Mountain Terrain").GetComponent<SurfaceMarker>().Type, Is.EqualTo(SurfaceType.Stone));
            EnvironmentAssetInstance[] stumps = Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None)
                .Where(item => item.Kind == EnvironmentAssetKind.Stump).ToArray();
            Assert.That(stumps.Length, Is.EqualTo(13));
            foreach (EnvironmentAssetInstance stump in stumps)
            {
                Collider[] active = stump.GetComponentsInChildren<Collider>().Where(collider => collider.enabled).ToArray();
                Assert.That(active.Length, Is.EqualTo(1), stump.name + " has duplicate collision geometry.");
                Assert.That(active[0], Is.TypeOf<MeshCollider>(), stump.name);
                Collider stumpCollider = active[0];
                Assert.That(stump.GetComponent<SurfaceMarker>().Type, Is.EqualTo(SurfaceType.Wood));
                Vector3 landing = stumpCollider.bounds.center;
                landing.y = stumpCollider.bounds.max.y + 2.2f;
                Assert.That(stumpCollider.Raycast(new Ray(landing, Vector3.down), out RaycastHit top, 5f), Is.True, stump.name);
                Assert.That(top.normal.y, Is.GreaterThan(0.9f), stump.name + " has no level landing surface.");
                Assert.That(top.point.y - stump.GroundContact.y, Is.LessThan(1.5f), stump.name + " exceeds the player's jump height.");
                player.Warp(landing);
                player.SetTestInput(Vector2.zero, false);
                yield return null;
                float timeout = Time.time + 3f;
                while (!player.IsGrounded && Time.time < timeout) yield return null;
                Assert.That(player.IsGrounded, Is.True, "Player did not land on " + stump.name);
                Assert.That(player.transform.position.y, Is.GreaterThan(top.point.y - 0.12f), "Player fell through " + stump.name);
                float landedY = player.transform.position.y;
                yield return new WaitForSeconds(0.25f);
                Assert.That(Mathf.Abs(player.transform.position.y - landedY), Is.LessThan(0.06f), "Player slides or bounces on " + stump.name);
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator MovementJumpAndRestartWork()
        {
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(player, Is.Not.Null);

            Vector3 start = player.transform.position;
            player.SetTestInput(Vector2.up, false);
            yield return new WaitForSeconds(0.45f);
            Assert.That(Vector3.Distance(start, player.transform.position), Is.GreaterThan(0.3f), "WASD-style movement did not move the player.");
            float walkingSpeed = player.PlanarSpeed;

            player.SetTestInput(Vector2.up, false, true);
            yield return new WaitForSeconds(0.5f);
            LumberjackVisual visual = player.GetComponent<LumberjackVisual>();
            Assert.That(player.IsSprinting, Is.True, "Shift-style sprint input did not activate sprinting.");
            Assert.That(player.PlanarSpeed, Is.GreaterThan(walkingSpeed + 1f), "Sprint is not faster than walking.");
            Assert.That(visual.SprintBlend, Is.GreaterThan(0.55f), "Visible sprint animation did not blend in.");

            player.SetTestInput(Vector2.zero, true);
            float yBeforeJump = player.transform.position.y;
            yield return new WaitForSeconds(0.2f);
            Assert.That(player.transform.position.y, Is.GreaterThan(yBeforeJump + 0.1f), "Jump did not lift the player.");

            GameManager previousManager = GameManager.Instance;
            previousManager.RestartGame();
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == SceneName &&
                                              GameManager.Instance != null &&
                                              GameManager.Instance != previousManager);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }
    }
}
