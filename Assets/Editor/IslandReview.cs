using System;
using System.IO;
using System.Linq;
using System.Text;
using CrystalSprint;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace CrystalSprintEditor
{
    public static class IslandReview
    {
        private static int stage;
        private static float deadline;
        private static Camera camera;
        private static ChoppableTree tree;
        private static readonly StringBuilder report=new();
        public static void Capture()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CrystalSprint.unity");SessionState.SetBool("IslandReview",true);
            EditorApplication.update+=Update;EditorApplication.EnterPlaymode();
        }
        [InitializeOnLoadMethod] private static void Resume(){if(SessionState.GetBool("IslandReview",false))EditorApplication.update+=Update;}
        private static void View(Vector3 p,Vector3 target,string filename)
        {
            camera.transform.SetPositionAndRotation(p,Quaternion.LookRotation(target-p));
            var image=FirstPersonReview.RenderStack(camera);Directory.CreateDirectory("Logs/Island");File.WriteAllBytes("Logs/Island/"+filename+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
        }
        private static void Update()
        {
            if(!EditorApplication.isPlaying||Time.timeSinceLevelLoad<2||Time.realtimeSinceStartup<deadline)return;
            try
            {
                var player=Object.FindAnyObjectByType<PlayerController>();var view=Object.FindAnyObjectByType<FirstPersonCamera>();var arms=Object.FindAnyObjectByType<FirstPersonViewmodel>();var axe=player.GetComponent<AxeChopping>();
                if(stage==0)
                {
                    camera=new GameObject("Island Review Camera").AddComponent<Camera>();camera.enabled=false;camera.cullingMask=Camera.main.cullingMask;camera.farClipPlane=1900;camera.fieldOfView=60;
                    camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
                    camera.GetUniversalAdditionalCameraData().requiresDepthOption=CameraOverrideOption.On;
                    camera.GetUniversalAdditionalCameraData().requiresColorOption=CameraOverrideOption.On;
                    View(new Vector3(110,105,-130),new Vector3(0,0,0),"01-island-overview");
                    View(new Vector3(59,2,-21),new Vector3(90,-2,-20),"02-beach");
                    View(new Vector3(18,5,-12),new Vector3(2,0,1),"03-pond-preserved");
                    tree=Object.FindObjectsByType<ChoppableTree>(FindObjectsSortMode.None).OrderBy(t=>(t.transform.position-new Vector3(0,0,-25)).sqrMagnitude).First();
                    var col=tree.GetComponents<CapsuleCollider>().First(c=>c.enabled);var contact=tree.GetComponent<EnvironmentAssetInstance>().GroundContact;
                    player.Warp(new Vector3(col.bounds.center.x,contact.y+1.1f,col.bounds.center.z-col.bounds.extents.x-.65f));player.SetTestInput(Vector2.zero,false);view.SetViewAngles(0,7);
                    deadline=Time.realtimeSinceStartup+.7f;
                }
                else if(stage==1)
                {
                    var col=tree.GetComponents<CapsuleCollider>().First(c=>c.enabled);
                    foreach(float phase in new[]{0f,.3f,.4f,.49f,.56f,.64f})
                    {
                        arms.EvaluatePose(phase);report.AppendLine($"phase={phase} blade={axe.Blade.position} local={view.transform.InverseTransformPoint(axe.Blade.position)} closest={col.ClosestPoint(axe.Blade.position)} gap={Vector3.Distance(axe.Blade.position,col.ClosestPoint(axe.Blade.position))}");
                    }
                    arms.EvaluatePose(0);var image=FirstPersonReview.RenderStack(Camera.main);File.WriteAllBytes("Logs/Island/04-tree-ready.png",image.EncodeToPNG());Object.DestroyImmediate(image);
                    player.GetComponent<LumberjackEquipment>().TriggerAttack();deadline=Time.realtimeSinceStartup+1.4f;
                }
                else if(stage==2)
                {
                    report.AppendLine($"Actual axe hits {axe.ValidHits}, tree hits {tree.StandingHits}, contactProgress {axe.ContactProgress}");
                    while(tree.StandingHits<3)tree.ReceiveAxeContact(player.GetComponent<LumberjackEquipment>(),100+tree.StandingHits,tree.transform.position+Vector3.up*2,Vector3.back);
                    player.Warp(new Vector3(0,1.1f,-20));
                    deadline=Time.realtimeSinceStartup+1.2f;
                }
                else if(stage==3)
                {
                    var cut=tree.Stump.position;View(cut+new Vector3(6,4,-8),cut+Vector3.up*2,"05-falling");deadline=Time.realtimeSinceStartup+4;
                }
                else if(stage==4)
                {
                    var cut=tree.Stump.position;var target=tree.FallenObject.GetComponent<Renderer>().bounds.center;
                    View(cut+new Vector3(8,4,-8),target,"06-fallen");View(cut+new Vector3(1.4f,1.1f,-1.4f),cut,"07-matching-stump");
                    report.AppendLine($"Fallen angle={tree.FallAngle}, state={tree.State}");
                    Vector3 side=Vector3.Cross(Vector3.up,tree.FallenObject.up).normalized;
                    Vector3 middle=cut+tree.FallenObject.up*1.6f;
                    tree.FallenObject.GetComponent<MeshCollider>().Raycast(new Ray(middle+Vector3.up*8,Vector3.down),out var top,15);
                    var ground=GameObject.Find("Ground").GetComponent<MeshCollider>();ground.Raycast(new Ray(top.point+side*.65f+Vector3.up*10,Vector3.down),out var foot,20);
                    player.Warp(foot.point+Vector3.up*1.1f);view.SetViewAngles(Mathf.Atan2(-side.x,-side.z)*Mathf.Rad2Deg,65);deadline=Time.realtimeSinceStartup+.4f;
                }
                else if(stage==5){player.GetComponent<LumberjackEquipment>().TriggerAttack();deadline=Time.realtimeSinceStartup+.51f;}
                else if(stage==6)
                {
                    var image=FirstPersonReview.RenderStack(Camera.main);File.WriteAllBytes("Logs/Island/08-low-axe-impact.png",image.EncodeToPNG());Object.DestroyImmediate(image);
                    deadline=Time.realtimeSinceStartup+1;
                }
                else if(stage==7)
                {
                    player.GetComponent<LumberjackEquipment>().TryAddWoodBundle();
                    var canvas=GameObject.Find("HUD").GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=Camera.main;canvas.planeDistance=.4f;
                    MusicMenu.Instance.SetOpen(true);Canvas.ForceUpdateCanvases();
                    var image=FirstPersonReview.RenderStack(Camera.main);File.WriteAllBytes("Logs/Island/09-music-inventory-ui.png",image.EncodeToPNG());Object.DestroyImmediate(image);
                    File.WriteAllText("Logs/Island/runtime-review.txt",report.ToString());
                    foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))foreach(var m in renderer.sharedMaterials)
                        if(m==null||m.shader==null||ShaderUtil.ShaderHasError(m.shader))throw new Exception("Shader failure: "+renderer.name);
                    SessionState.SetBool("IslandReview",false);EditorApplication.update-=Update;Debug.Log("ISLAND_REVIEW_OK");EditorApplication.Exit(0);
                }
                stage++;
            }
            catch(Exception e){SessionState.SetBool("IslandReview",false);Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
