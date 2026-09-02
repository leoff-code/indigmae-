using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CrystalSprint;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CrystalSprintEditor
{
    public static class IslandRefinement
    {
        public static void Apply()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CrystalSprint.unity");
            string root=IslandGameplaySetup.AssetsRoot;
            var ocean=AssetDatabase.LoadAssetAtPath<Material>(root+"/Materials/IslandOcean.mat");
            ocean.SetFloat("_caustics_Alpha",0);ocean.SetFloat("_caustics_Strenght",1);
            ocean.SetFloat("_Waves_height",.025f);ocean.SetFloat("_Waves_Length",1.5f);ocean.SetFloat("_Peak_Sharpness",.001f);
            ocean.SetFloat("_Normal_strenght",.14f);ocean.SetFloat("_Smouthness",.73f);
            ocean.SetFloat("_Foam_depth",2.2f);ocean.SetFloat("_foam_cutoff",.6f);ocean.SetFloat("_foam_scale",.018f);
            ocean.SetColor("_deep_water_color",new Color(.015f,.08f,.12f,1));
            ocean.SetColor("_shallow_water_color",new Color(.07f,.25f,.22f,.22f));
            ocean.SetColor("_foam_color",new Color(.58f,.72f,.66f,.5f));EditorUtility.SetDirty(ocean);
            var barkMeshes=new Dictionary<Mesh,Mesh>();
            foreach(var tree in Object.FindObjectsByType<ChoppableTree>(FindObjectsSortMode.None))
            {
                var renderer=tree.GetComponent<LODGroup>().GetLODs()[0].renderers[0];var source=renderer.GetComponent<MeshFilter>().sharedMesh;
                string readablePath=root+"/Meshes/"+source.name+"_ReadableCutSource.asset";
                var readable=AssetDatabase.LoadAssetAtPath<Mesh>(readablePath);
                if(readable==null)
                {
                    readable=new Mesh{name=source.name+" Shared Readable Cut Source",vertices=source.vertices,normals=source.normals,tangents=source.tangents,uv=source.uv,colors=source.colors,subMeshCount=source.subMeshCount};
                    for(int s=0;s<source.subMeshCount;s++)readable.SetTriangles(source.GetTriangles(s),s);
                    readable.RecalculateBounds();AssetDatabase.CreateAsset(readable,readablePath);
                }
                tree.ConfigureCutSource(readable);EditorUtility.SetDirty(tree);
                if(tree.GetComponentInChildren<TreeHitSurface>()!=null)continue;
                if(!barkMeshes.TryGetValue(source,out var collision))
                {
                    collision=Object.Instantiate(source);collision.name=source.name+" Bark Collision";
                    var tris=new List<int>();for(int s=0;s<renderer.sharedMaterials.Length;s++)if(renderer.sharedMaterials[s].name.Contains("Trunk"))tris.AddRange(source.GetTriangles(s));
                    if(tris.Count==0)throw new Exception("No bark found: "+tree.name);
                    collision.subMeshCount=1;collision.SetTriangles(tris,0);
                    AssetDatabase.CreateAsset(collision,root+"/Meshes/"+source.name+"_BarkCollision.asset");barkMeshes.Add(source,collision);
                }
                var child=new GameObject("Actual Bark Contact");child.transform.SetParent(renderer.transform,false);
                child.AddComponent<MeshCollider>().sharedMesh=collision;child.AddComponent<TreeHitSurface>();
            }
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();Debug.Log("ISLAND_REFINED");
        }
    }
}
