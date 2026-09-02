using System.Linq;
using CrystalSprint;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace CrystalSprintEditor
{
    public static class IslandFinish
    {
        public static void ApplyAndReview(){Apply();IslandReview.Capture();}
        public static void Apply()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CrystalSprint.unity");
            var player=Object.FindAnyObjectByType<PlayerController>();if(player.GetComponent<IslandWaterSafety>()==null)player.gameObject.AddComponent<IslandWaterSafety>();
            var hud=Object.FindAnyObjectByType<InventoryHud>();
            var notice=GameObject.Find("Inventory Notice");
            if(notice==null)
            {
                notice=new GameObject("Inventory Notice",typeof(RectTransform),typeof(Text));notice.transform.SetParent(GameObject.Find("HUD").transform,false);
                var label=notice.GetComponent<Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=19;label.alignment=TextAnchor.MiddleCenter;label.color=new Color(1,.87f,.6f);label.raycastTarget=false;
                label.rectTransform.anchorMin=label.rectTransform.anchorMax=new Vector2(.5f,0);label.rectTransform.anchoredPosition=new Vector2(0,110);label.rectTransform.sizeDelta=new Vector2(650,35);hud.ConfigureNotice(label);
            }
            if(GameObject.Find("Coastal Driftwood and Pebbles")==null)
            {
                var parent=new GameObject("Coastal Driftwood and Pebbles").transform;parent.SetParent(Object.FindAnyObjectByType<IslandCoast>().transform,false);
                var sources=Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None).Where(i=>i.Kind==EnvironmentAssetKind.Branch||i.Kind==EnvironmentAssetKind.Rock).ToArray();
                var random=new System.Random(92026);
                var terrain=GameObject.Find("Ground").GetComponent<MeshCollider>();var beach=GameObject.Find("Connected Beach and Seabed").GetComponent<MeshCollider>();
                for(int i=0;i<28;i++)
                {
                    float angle=(float)random.NextDouble()*Mathf.PI*2;float radius=58+(float)random.NextDouble()*7;
                    Vector3 p=new(Mathf.Cos(angle)*radius,20,Mathf.Sin(angle)*radius);Ray ray=new(p,Vector3.down);
                    if(!terrain.Raycast(ray,out var hit,40)&&!beach.Raycast(ray,out hit,40))continue;
                    var original=sources[i%sources.Length];var item=Object.Instantiate(original.gameObject,parent);item.name="Coastal "+original.Kind+" "+(i+1);
                    item.transform.localScale*=original.Kind==EnvironmentAssetKind.Rock?.25f:.75f;
                    item.transform.position=hit.point;item.transform.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);
                    float foot=item.GetComponentsInChildren<Renderer>().Min(r=>r.bounds.min.y);item.transform.position+=Vector3.up*(hit.point.y-foot-.035f);
                    foreach(var collider in item.GetComponentsInChildren<Collider>())collider.enabled=false;
                    item.GetComponent<EnvironmentAssetInstance>().Configure(original.Kind,original.SourcePrefab,hit.point);
                }
            }
            foreach(var detail in GameObject.Find("Coastal Driftwood and Pebbles").GetComponentsInChildren<EnvironmentAssetInstance>())
            {
                var renderers=detail.GetComponentsInChildren<Renderer>();float min=renderers.Min(r=>r.bounds.min.y),max=renderers.Max(r=>r.bounds.max.y);
                float embed=Mathf.Min(.012f,(max-min)*.15f);
                detail.transform.position+=Vector3.up*(detail.GroundContact.y-min-embed);
            }
            var ocean=AssetDatabase.LoadAssetAtPath<Material>(IslandGameplaySetup.AssetsRoot+"/Materials/IslandOcean.mat");ocean.SetFloat("_Foam_depth",1.7f);ocean.SetFloat("_foam_cutoff",5);EditorUtility.SetDirty(ocean);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();Debug.Log("ISLAND_FINISH_OK");
        }
    }
}
