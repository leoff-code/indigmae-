using System.Collections;
using System.Linq;
using CrystalSprint;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CrystalSprintTests
{
    public sealed class CabinWaterPlayModeTests
    {
        private PlayerController player;
        private FirstPersonCamera look;
        private PondCabin cabin;
        private PondSurfaceMotion water;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("CrystalSprint"); yield return null;
            player = Object.FindAnyObjectByType<PlayerController>(); look = Object.FindAnyObjectByType<FirstPersonCamera>();
            cabin = Object.FindAnyObjectByType<PondCabin>(); water = Object.FindAnyObjectByType<PondSurfaceMotion>();
            player.SetTestInput(Vector2.zero, false);
            HingedDoorInteractable door = cabin.GetComponentInChildren<HingedDoorInteractable>();
            if (door != null && !door.IsOpen) { door.Interact(null); yield return new WaitForSeconds(1f); }
        }

        [UnityTest]
        public IEnumerator CabinFitsTheCharacterAndHasADryGentleApproach()
        {
            Assert.That(cabin, Is.Not.Null);
            CharacterController body = player.GetComponent<CharacterController>();
            Assert.That(cabin.DoorWidth, Is.GreaterThan(body.radius * 2 + .35f));
            Assert.That(cabin.DoorHeight, Is.GreaterThan(body.height + .5f));
            Assert.That(cabin.transform.localScale.x, Is.EqualTo(1.35f).Within(.001f));
            Assert.That(new Vector2(cabin.Entrance.x, cabin.Entrance.z).magnitude, Is.GreaterThan(15f));
            var terrain = GameObject.Find("Ground").GetComponent<MeshCollider>();
            for (float step = 0; step <= 8; step += .5f)
            {
                Vector3 point = cabin.Approach + cabin.ExitDirection * step;
                Assert.That(water.ContainsWater(point), Is.False, "Exiting cabin heads into water.");
                Assert.That(terrain.Raycast(new Ray(point + Vector3.up * 10, Vector3.down), out RaycastHit hit, 20), Is.True);
                Assert.That(Vector3.Angle(hit.normal, Vector3.up), Is.LessThan(10));
            }
            Assert.That(cabin.transform.Find("Solid Building Colliders"), Is.Not.Null);
            Assert.That(cabin.GetComponentsInChildren<MeshCollider>().Any(c => c.enabled && !c.convex), Is.False, "One-sided wall/frame meshes must stay disabled.");
            Assert.That(cabin.transform.Find("Door").localEulerAngles.y, Is.EqualTo(95f).Within(.1f));
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ActualFirstPersonControllerWalksThroughDoorBothWays()
        {
            player.Warp(cabin.Approach + Vector3.up * 1.1f);
            look.SetViewAngles(0, 0); yield return new WaitForSeconds(.65f);
            player.SetTestInput(Vector2.up, false);
            yield return new WaitForSeconds(1.6f);
            player.SetTestInput(Vector2.zero, false); yield return new WaitForSeconds(.25f);
            Vector3 inside = cabin.transform.InverseTransformPoint(player.transform.position);
            Assert.That(inside.z, Is.InRange(-2.5f, 3.1f), "Door, porch or floor blocked entry: " + inside);
            Assert.That(player.IsGrounded, Is.True);
            float floor = cabin.Interior.y;
            Assert.That(player.transform.position.y - player.GetComponent<CharacterController>().height * .5f, Is.EqualTo(floor).Within(.13f));
            Assert.That(player.GetComponent<PlayerWaterInteraction>().IsInWater, Is.False);
            look.SetViewAngles(180, 0); player.SetTestInput(Vector2.up, false);
            yield return new WaitForSeconds(1.6f);
            player.SetTestInput(Vector2.zero, false); yield return new WaitForSeconds(.25f);
            Assert.That(cabin.transform.InverseTransformPoint(player.transform.position).z, Is.GreaterThan(7f), "Door blocked exit.");
            Assert.That(player.IsGrounded, Is.True);
            Assert.That(player.GetComponent<PlayerWaterInteraction>().IsInWater, Is.False);
            Assert.That(Object.FindAnyObjectByType<FirstPersonViewmodel>().AxeVisible, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator InteriorFloorSupportsPlayerAndWallsBlockMovement()
        {
            player.Warp(cabin.transform.TransformPoint(new Vector3(0, .254f, 0)) + Vector3.up * 1.15f);
            look.SetViewAngles(90, 0); yield return new WaitForSeconds(.7f);
            Assert.That(player.IsGrounded, Is.True);
            player.SetTestInput(Vector2.up, false, true); yield return new WaitForSeconds(1.2f);
            player.SetTestInput(Vector2.zero, false);
            Assert.That(Mathf.Abs(cabin.transform.InverseTransformPoint(player.transform.position).x), Is.LessThan(2.55f), "Player passed through cabin wall.");
            Assert.That(player.transform.position.y, Is.GreaterThan(cabin.Interior.y + .95f));
            Assert.That(player.GetComponent<InteractiveGrass>().IsInteractingWithGrass, Is.False, "Grass bends through wooden floor.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CabinAndWaterUseCompleteCompatibleMaterials()
        {
            foreach (Renderer renderer in cabin.GetComponentsInChildren<Renderer>())
            foreach (Material material in renderer.sharedMaterials)
            {
                Assert.That(material, Is.Not.Null); Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
                Assert.That(material.shader.isSupported, Is.True);
                if (material.name == "Cabin_walls_mat" || material.name == "Cabin_curtain_mat")
                    Assert.That(material.GetFloat("_Cull"), Is.Zero, "Interior faces must not disappear from inside the cabin.");
                #if UNITY_EDITOR
                Assert.That(UnityEditor.ShaderUtil.ShaderHasError(material.shader), Is.False);
                #endif
            }
            Material surface = water.GetComponentInChildren<Renderer>().sharedMaterial;
            Assert.That(surface.shader.name, Is.EqualTo("Custom/SimpleWaterURP"));
            Assert.That(surface.GetTexture("_NormalMap"), Is.Not.Null); Assert.That(surface.GetTexture("_FoamNoiseTex"), Is.Not.Null);
            Assert.That(surface.GetFloat("_WaveStrength"), Is.Zero, "GPU waves must not desynchronise the existing surface sampler.");
            Assert.That(surface.GetFloat("_NormalSpeed"), Is.GreaterThan(0));
            Assert.That(Camera.main.GetUniversalAdditionalCameraData().requiresDepthOption, Is.EqualTo(CameraOverrideOption.On));
            #if UNITY_EDITOR
            Assert.That(UnityEditor.ShaderUtil.ShaderHasError(surface.shader), Is.False);
            #endif
            Assert.That(Object.FindAnyObjectByType<InstancedForestGrass>().IsInLocalClearing(cabin.Interior), Is.True);
            Assert.That(Object.FindAnyObjectByType<InstancedForestGrass>().IsInLocalClearing(cabin.Approach), Is.True);
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PondBedOverlayFollowsActualTerrainWithoutChangingItsCollider()
        {
            MeshFilter bed = GameObject.Find("Pond Bed").GetComponent<MeshFilter>();
            Assert.That(bed.sharedMesh.name, Is.EqualTo("TerrainConformingPondBed"));
            Assert.That(bed.GetComponent<Collider>(), Is.Null, "Visual bed must not change walking or fish sampling.");
            MeshCollider ground = GameObject.Find("Ground").GetComponent<MeshCollider>();
            Vector3[] points = bed.sharedMesh.vertices; int[] triangles = bed.sharedMesh.triangles;
            for (int i = 0; i < triangles.Length; i += 9)
            {
                Vector3 p = bed.transform.TransformPoint((points[triangles[i]] + points[triangles[i + 1]] + points[triangles[i + 2]]) / 3f);
                Assert.That(ground.Raycast(new Ray(p + Vector3.up * 3, Vector3.down), out RaycastHit hit, 6), Is.True);
                Assert.That(p.y - hit.point.y, Is.EqualTo(.012f).Within(.003f), "Intersecting bed/terrain produces green triangles through the water.");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerCreatesShoreAndWadingEffectsWithoutChangingMovement()
        {
            var interaction = player.GetComponent<PlayerWaterInteraction>();
            player.Warp(new Vector3(0, 1.1f, -10)); yield return new WaitForSeconds(.6f);
            Assert.That(interaction.IsInWater, Is.False);
            look.SetViewAngles(0, 0); player.SetTestInput(Vector2.up, false);
            yield return new WaitForSeconds(1.45f);
            Assert.That(interaction.EntryCount, Is.EqualTo(1));
            Assert.That(interaction.IsInWater, Is.True);
            Assert.That(interaction.StepEffectCount, Is.GreaterThan(0));
            Assert.That(player.GetComponent<InteractiveGrass>().IsInteractingWithGrass, Is.False);
            Assert.That(Object.FindObjectsByType<WaterRippleEffect>().Any(r => r.name.StartsWith("Player")), Is.True);
            player.SetTestInput(Vector2.zero, false);
            player.Warp(new Vector3(0, 1.1f, -10)); yield return new WaitForSeconds(.2f);
            Assert.That(interaction.ExitCount, Is.EqualTo(1));
            Assert.That(interaction.IsInWater, Is.False);
            foreach (WaterRippleEffect ripple in Object.FindObjectsByType<WaterRippleEffect>())
                Assert.That(water.ContainsWater(ripple.transform.position), Is.True, "Exit effect appeared on dry land.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator FishStillProducesTakeoffAndLandingEffectsOnNewWater()
        {
            FishJumpSystem fish = Object.FindAnyObjectByType<FishJumpSystem>();
            Assert.That(fish.JumpInterval, Is.EqualTo(10f)); Assert.That(fish.FishVariantCount, Is.EqualTo(3));
            Assert.That(fish.TriggerJumpNow(), Is.True); yield return null;
            WaterRippleEffect first = Object.FindAnyObjectByType<WaterRippleEffect>();
            Assert.That(first, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<ParticleSystem>().Any(p => p.name.StartsWith("WaterSplash")), Is.True);
            yield return new WaitForSeconds(1.48f);
            Assert.That(fish.ActiveFish == null, Is.True);
            Assert.That(Object.FindObjectsByType<WaterRippleEffect>().Any(r => r != first), Is.True, "No landing ripple.");
            Assert.That(Object.FindObjectsByType<ParticleSystem>().Any(p => p.name.StartsWith("WaterSplash")), Is.True, "No landing splash.");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
