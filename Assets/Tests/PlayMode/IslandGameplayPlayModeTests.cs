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
    public sealed class IslandGameplayPlayModeTests
    {
        private PlayerController player;private FirstPersonCamera view;private LumberjackEquipment equipment;
        private Keyboard keyboard;private Mouse mouse;private InputSettings settings;private float volume;
        [UnitySetUp] public IEnumerator Load()
        {
            Time.timeScale=1;yield return SceneManager.LoadSceneAsync("CrystalSprint");yield return null;
            player=Object.FindAnyObjectByType<PlayerController>();view=Object.FindAnyObjectByType<FirstPersonCamera>();equipment=player.GetComponent<LumberjackEquipment>();player.SetTestInput(Vector2.zero,false);
            volume=PlayerPrefs.GetFloat(MusicMenu.VolumePreference,.35f);
        }
        [TearDown] public void Cleanup()
        {
            if(MusicMenu.Instance!=null)MusicMenu.Instance.SetOpen(false);Time.timeScale=1;
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);
            if(settings!=null){var temp=InputSystem.settings;InputSystem.settings=settings;Object.Destroy(temp);}
            PlayerPrefs.SetFloat(MusicMenu.VolumePreference,volume);PlayerPrefs.Save();
        }
        [UnityTest] public IEnumerator CoastPreservesCentralMeshPondAndModels()
        {
            Assert.That(Object.FindObjectsByType<ChoppableTree>(FindObjectsSortMode.None).Length,Is.EqualTo(240));
            Assert.That(GameObject.Find("Continuous Mountain Terrain"),Is.Null);
            var ground=GameObject.Find("Ground").GetComponent<MeshFilter>().sharedMesh;
            #if UNITY_EDITOR
            var original=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/ForestKit/ExpandedRollingMeadow.asset");
            for(int i=0;i<ground.vertexCount;i++)
                if(new Vector2(original.vertices[i].x,original.vertices[i].z).magnitude<=55f)Assert.That(ground.vertices[i],Is.EqualTo(original.vertices[i]),"Changed protected forest terrain");
            #endif
            var sea=GameObject.Find("Surrounding Ocean");Assert.That(sea.GetComponent<Renderer>().bounds.size.x,Is.GreaterThan(2000));
            var pond=Object.FindAnyObjectByType<PondSurfaceMotion>();Assert.That(pond.SurfaceHeight,Is.EqualTo(-.29f).Within(.01f));Assert.That(pond.Amplitude,Is.EqualTo(.022f));
            Assert.That(pond.ContainsWater(new Vector3(70,-2,0)),Is.False,"Sea must not trigger pond effects");
            var coastal=GameObject.Find("Connected Beach and Seabed").GetComponent<MeshCollider>();
            for(int angle=0;angle<360;angle+=5)
            {
                float a=angle*Mathf.Deg2Rad;float r=IslandCoast.ShoreRadius(a);Vector3 p=new(Mathf.Cos(a)*r,10,Mathf.Sin(a)*r);
                Ray ray=new(p,Vector3.down);bool hit=GameObject.Find("Ground").GetComponent<MeshCollider>().Raycast(ray,out var h,30)||coastal.Raycast(ray,out h,30);
                Assert.That(hit,Is.True,"Missing connected beach");Assert.That(h.point.y,Is.EqualTo(IslandCoast.SeaLevel).Within(.15f));
            }
            foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))foreach(var material in renderer.sharedMaterials)
            {
                Assert.That(material!=null&&material.shader.isSupported,Is.True,renderer.name);
                #if UNITY_EDITOR
                Assert.That(UnityEditor.ShaderUtil.ShaderHasError(material.shader),Is.False,material.name);
                #endif
            }
            yield return null;LogAssert.NoUnexpectedReceived();
        }
        private IEnumerator AtStandingTree(ChoppableTree tree)
        {
            Vector3 ground=tree.GetComponent<EnvironmentAssetInstance>().GroundContact;
            var surface=tree.GetComponentInChildren<TreeHitSurface>().GetComponent<Collider>();
            Ray ray=new(new Vector3(ground.x,ground.y+1.92f,ground.z-8),Vector3.forward);
            Assert.That(surface.Raycast(ray,out var hit,16),Is.True,"Cannot find actual bark at chop height");
            player.Warp(new Vector3(hit.point.x-.03f,ground.y+1.1f,hit.point.z-.86f));view.SetViewAngles(0,7);
            yield return new WaitForSeconds(.45f);
        }
        private IEnumerator Swing()
        {Assert.That(equipment.TriggerAttack(),Is.True);yield return new WaitForSeconds(1.22f);}
        [UnityTest] public IEnumerator ActualBladeContactsCountOnceAtImpactAndMissesDoNotCount()
        {
            var tree=Object.FindObjectsByType<ChoppableTree>(FindObjectsSortMode.None).OrderBy(t=>(t.transform.position-new Vector3(0,0,-25)).sqrMagnitude).First();
            player.Warp(new Vector3(0,1.1f,-20));view.SetViewAngles(0,-65);yield return Swing();
            Assert.That(tree.StandingHits,Is.Zero);
            yield return AtStandingTree(tree);
            Assert.That(equipment.TriggerAttack(),Is.True);yield return new WaitForSeconds(.3f);Assert.That(tree.StandingHits,Is.Zero,"Damage during windup");
            yield return new WaitForSeconds(.95f);Assert.That(tree.StandingHits,Is.EqualTo(1),"Actual blade did not contact bark");
            Assert.That(tree.State,Is.EqualTo(TreeHarvestState.Standing));
            yield return Swing();Assert.That(tree.StandingHits,Is.EqualTo(2));yield return Swing();Assert.That(tree.StandingHits,Is.EqualTo(3));
            Assert.That(tree.State,Is.EqualTo(TreeHarvestState.Falling));float earlier=tree.FallAngle;yield return new WaitForSeconds(.35f);Assert.That(tree.FallAngle,Is.GreaterThan(earlier));
            float timeout=Time.time+8;while(tree.State==TreeHarvestState.Falling && Time.time<timeout)yield return null;
            Assert.That(tree.State,Is.EqualTo(TreeHarvestState.Fallen));Assert.That(tree.FallAngle,Is.GreaterThan(80));Assert.That(tree.Stump,Is.Not.Null);
            Assert.That(tree.Stump.GetComponent<Renderer>().bounds.max.y,Is.EqualTo(tree.GetComponent<EnvironmentAssetInstance>().GroundContact.y+.36f).Within(.03f));
            Assert.That(tree.AcceptsCollider(tree.Stump.GetComponent<Collider>()),Is.False,"Stump must not consume the fallen log's hits");
            // Approach the actual fallen bark from the side, then chop down with the same input/animation.
            Vector3 along=tree.FallenObject.up;Vector3 side=Vector3.Cross(Vector3.up,along).normalized;
            var bark=tree.FallenObject.GetComponent<MeshCollider>();
            Vector3 middle=tree.FallenObject.position+along*1.6f;
            Assert.That(bark.Raycast(new Ray(middle+Vector3.up*8,Vector3.down),out var top,15),Is.True);
            Vector3 stand=top.point+side*.65f;
            var terrain=GameObject.Find("Ground").GetComponent<MeshCollider>();Assert.That(terrain.Raycast(new Ray(stand+Vector3.up*10,Vector3.down),out var foot,20),Is.True);
            player.Warp(foot.point+Vector3.up*1.1f);
            float yaw=Mathf.Atan2(-side.x,-side.z)*Mathf.Rad2Deg;view.SetViewAngles(yaw,65);yield return new WaitForSeconds(.4f);
            for(int i=1;i<=3;i++)
            {
                yield return Swing();
                Assert.That(tree.FallenHits,Is.EqualTo(i),"Actual downward axe missed fallen trunk");
            }
            yield return new WaitForSeconds(1);Assert.That(tree.State,Is.EqualTo(TreeHarvestState.Harvested));Assert.That(equipment.HasWood(1),Is.True);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator AllTenModelsSplitAndFallenWoodAwardsExistingInventory()
        {
            var trees=Object.FindObjectsByType<ChoppableTree>(FindObjectsSortMode.None).GroupBy(t=>t.GetComponent<EnvironmentAssetInstance>().SourcePrefab).Select(g=>g.First()).ToArray();
            int id=100;
            foreach(var tree in trees)
            {
                var p=tree.GetComponent<EnvironmentAssetInstance>().GroundContact+Vector3.up;
                for(int i=0;i<3;i++)Assert.That(tree.ReceiveAxeContact(equipment,id++,p,Vector3.back),Is.True);
                Assert.That(tree.ReceiveAxeContact(equipment,id-1,p,Vector3.back),Is.False);
            }
            yield return new WaitForSeconds(8);
            foreach(var tree in trees)
            {
                Assert.That(tree.State,Is.EqualTo(TreeHarvestState.Fallen),tree.name);Assert.That(tree.Stump,Is.Not.Null);Assert.That(tree.FallenObject,Is.Not.Null);
                Assert.That(tree.GetComponent<LODGroup>().GetLODs().SelectMany(l=>l.renderers).All(r=>!r.enabled && r.forceRenderingOff),Is.True,"Standing model remained visible after falling");
                var p=tree.FallenObject.position;
                for(int i=0;i<3;i++)Assert.That(tree.ReceiveAxeContact(equipment,id++,p,Vector3.up),Is.True);
                Assert.That(tree.State,Is.EqualTo(TreeHarvestState.Processing));Assert.That(tree.FallenObject,Is.Not.Null,"Tree vanished before processing animation");
                yield return new WaitForSeconds(1.0f);Assert.That(tree.State,Is.EqualTo(TreeHarvestState.Harvested));Assert.That(tree.FallenHits,Is.EqualTo(3));
            }
            Assert.That(equipment.HasWood(1)&&equipment.HasWood(2)&&equipment.HasWood(3),Is.True);
            Assert.That(Object.FindAnyObjectByType<InventoryHud>().SlotCount,Is.EqualTo(4));Assert.That(equipment.ItemName(0),Is.EqualTo("Axt"));
            var pickups=Object.FindObjectsByType<WoodBundlePickup>(FindObjectsSortMode.None);Assert.That(pickups.Length,Is.EqualTo(7),"Full inventory must preserve all excess wood in the world");
            pickups[0].Interact(player.GetComponent<PlayerInteractor>());Assert.That(pickups[0]!=null,Is.True);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator MAndSliderPauseInputRestoreCursorAndSaveVolume()
        {
            settings=InputSystem.settings;InputSystem.settings=Object.Instantiate(settings);InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            #if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            #endif
            keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();yield return null;
            var menu=MusicMenu.Instance;var cursor=Object.FindAnyObjectByType<CursorLockController>();cursor.LockCursor();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.M));InputUpdate();Invoke(menu,"Update");
            Assert.That(MusicMenu.IsOpen,Is.True);Assert.That(Time.timeScale,Is.Zero);Assert.That(cursor.IsLocked,Is.False);
            Vector3 start=player.transform.position;player.SetTestInput(Vector2.up,true,true);yield return new WaitForSecondsRealtime(.2f);Assert.That(player.transform.position,Is.EqualTo(start));
            Assert.That(equipment.TriggerAttack(),Is.False);Assert.That(player.GetComponent<PlayerInteractor>().TryInteract(),Is.False);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));InputUpdate();Invoke(cursor,"Update");Assert.That(cursor.IsLocked,Is.False,"Slider click recaptured cursor");
            Canvas.ForceUpdateCanvases();
            var sliderRect=(RectTransform)menu.VolumeSlider.transform;
            Vector3 point=sliderRect.TransformPoint(new Vector3(sliderRect.rect.xMin+sliderRect.rect.width*.23f,0,0));
            var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,point),button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
            menu.VolumeSlider.OnPointerDown(pointer);Assert.That(menu.Volume,Is.EqualTo(.23f).Within(.06f),"UI pointer did not operate the slider");menu.VolumeSlider.OnPointerUp(pointer);
            menu.VolumeSlider.value=.73f;Assert.That(menu.Volume,Is.EqualTo(.73f).Within(.001f));Assert.That(PlayerPrefs.GetFloat(MusicMenu.VolumePreference),Is.EqualTo(.73f).Within(.001f));
            menu.VolumeSlider.value=0;Assert.That(menu.Source.volume,Is.Zero);menu.VolumeSlider.value=1;Assert.That(menu.Volume,Is.EqualTo(1));Assert.That(menu.Source.loop,Is.True);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.M));InputUpdate();Invoke(menu,"Update");Assert.That(MusicMenu.IsOpen,Is.False);Assert.That(Time.timeScale,Is.EqualTo(1));Assert.That(cursor.IsLocked,Is.True);
            player.SetTestInput(Vector2.zero,false);InputSystem.QueueStateEvent(mouse,new MouseState());InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputUpdate();
            menu.SetOpen(true);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));InputUpdate();Invoke(menu,"Update");Invoke(cursor,"Update");Assert.That(MusicMenu.IsOpen,Is.False);Assert.That(cursor.IsLocked,Is.False);Assert.That(Time.timeScale,Is.EqualTo(1));
            yield return null;LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator DeepOceanReturnsToShoreWithoutChangingPondInteraction()
        {
            var ground=GameObject.Find("Ground").GetComponent<MeshCollider>();
            Assert.That(ground.Raycast(new Ray(new Vector3(60,10,0),Vector3.down),out var hit,30),Is.True);
            player.Warp(hit.point+Vector3.up*1.1f);yield return new WaitForSeconds(.6f);
            Vector3 safe=player.transform.position;
            player.Warp(new Vector3(85,-4,0));yield return null;yield return null;
            Assert.That(Vector3.Distance(player.transform.position,safe),Is.LessThan(.3f));
            Assert.That(player.GetComponent<PlayerWaterInteraction>().IsInWater,Is.False,"Ocean mistakenly used pond logic");
            Assert.That(equipment.Notice,Does.Contain("Ufer"));LogAssert.NoUnexpectedReceived();
        }
        private static void Invoke(object target,string name)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
        private static void InputUpdate()=>typeof(InputSystem).GetMethod("Update",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(InputUpdateType)},null).Invoke(null,new object[]{InputUpdateType.Dynamic});
    }
}
