using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CrystalSprint;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace CrystalSprintEditor
{
    public static class IslandGameplaySetup
    {
        public const string AssetsRoot="Assets/IslandGameplay";
        public const string OceanPack="Assets/Houidisoft technology/One Click Add Water -Stylized Water Shader";
        public static void Apply()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CrystalSprint.unity");
            if(Object.FindAnyObjectByType<IslandCoast>()!=null)throw new InvalidOperationException("Island already installed; do not overwrite the scene.");
            foreach(var folder in new[]{AssetsRoot,AssetsRoot+"/Meshes",AssetsRoot+"/Materials",AssetsRoot+"/Prefabs",AssetsRoot+"/Textures"})Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            MakeIsland(); MakeHarvesting(); MakeMusic();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();Debug.Log("ISLAND_GAMEPLAY_INSTALLED");
        }
        private static T Save<T>(T asset,string path) where T:Object {AssetDatabase.CreateAsset(asset,path);return asset;}
        private static Material Lit(string name,Color color,float smooth=.15f)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);
            return Save(m,AssetsRoot+"/Materials/"+name+".mat");
        }
        private static GameObject MeshObject(string name,Mesh mesh,Material material,Transform parent,bool solid)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=material;
            if(solid){go.AddComponent<MeshCollider>().sharedMesh=mesh;go.AddComponent<SurfaceMarker>().Configure(SurfaceType.Stone);}
            return go;
        }
        private static void MakeIsland()
        {
            var island=new GameObject("Island Coast and Ocean");island.AddComponent<IslandCoast>();
            var ground=GameObject.Find("Ground");var old=ground.GetComponent<MeshFilter>().sharedMesh;
            var mesh=Object.Instantiate(old);mesh.name="Island Ground - Protected Central Forest";
            var points=mesh.vertices;for(int i=0;i<points.Length;i++)points[i].y=IslandCoast.Height(points[i].x,points[i].z,points[i].y);
            mesh.vertices=points;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();Save(mesh,AssetsRoot+"/Meshes/IslandGround.asset");
            ground.GetComponent<MeshFilter>().sharedMesh=mesh;ground.GetComponent<MeshCollider>().sharedMesh=mesh;
            var oldMaterial=ground.GetComponent<Renderer>().sharedMaterial;
            var shore=new Material(Shader.Find("CrystalSprint/Island Shore URP")){name="Yughues Meadow Sand Wet Shore"};
            shore.SetTexture("_BaseMap",oldMaterial.GetTexture("_BaseMap"));shore.SetTextureScale("_BaseMap",oldMaterial.GetTextureScale("_BaseMap"));shore.SetColor("_BaseColor",oldMaterial.GetColor("_BaseColor"));
            foreach(var suffix in new[]{"d","n","s"})
                shore.SetTexture(suffix=="d"?"_SandMap":suffix=="n"?"_SandNormal":"_SandSpec",AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/YughuesFreeSandMaterials/Textures/T_YFSM_01_{suffix}.tga"));
            Save(shore,AssetsRoot+"/Materials/IslandShore.mat");
            // Keep the original Lit shader for every triangle surrounding the pond/cabin/forest core.
            var core=new List<int>();var beach=new List<int>();var triangles=old.triangles;
            for(int i=0;i<triangles.Length;i+=3)
            {
                bool inside=true;for(int k=0;k<3;k++){var p=points[triangles[i+k]];inside&=new Vector2(p.x,p.z).magnitude<53f;}
                (inside?core:beach).AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});
            }
            mesh.subMeshCount=2;mesh.SetTriangles(core,0);mesh.SetTriangles(beach,1);EditorUtility.SetDirty(mesh);
            ground.GetComponent<Renderer>().sharedMaterials=new[]{oldMaterial,shore};
            // Continue the exact 1.5 m grid beyond the previous square, sharing edge coordinates/heights.
            var outer=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            const int side=161;const float half=120;
            for(int z=0;z<side;z++)for(int x=0;x<side;x++)
            {
                float px=x*1.5f-half,pz=z*1.5f-half;
                outer.Add(new Vector3(px,IslandCoast.Height(px,pz,ForestWorld.Height(px,pz)),pz));
                uv.Add(new Vector2((px+48)/96*14,(pz+48)/96*14));
                if(x==side-1||z==side-1)continue;
                if(px>=-67.5f&&px<67.5f&&pz>=-67.5f&&pz<67.5f)continue;
                int j=z*side+x;tris.AddRange(new[]{j,j+side,j+1,j+1,j+side,j+side+1});
            }
            var outerMesh=new Mesh{name="Connected Beach and Seabed",vertices=outer.ToArray(),uv=uv.ToArray(),triangles=tris.ToArray()};outerMesh.RecalculateNormals();outerMesh.RecalculateTangents();outerMesh.RecalculateBounds();
            Save(outerMesh,AssetsRoot+"/Meshes/BeachSeabed.asset");MeshObject("Connected Beach and Seabed",outerMesh,shore,island.transform,true);
            var grass=Object.FindAnyObjectByType<InstancedForestGrass>();
            var so=new SerializedObject(grass);so.FindProperty("groundMesh").objectReferenceValue=mesh;so.ApplyModifiedPropertiesWithoutUndo();grass.Rebuild();
            // Only obsolete boundary objects are removed. The source assets remain in the project/backup.
            foreach(var item in Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None).Where(i=>i.Kind==EnvironmentAssetKind.Cliff).ToArray())Object.DestroyImmediate(item.gameObject);
            foreach(string name in new[]{"Mountain Boundary","Meadow Rock Transition"}){var item=GameObject.Find(name);if(item!=null)Object.DestroyImmediate(item);}
            var left=GameObject.Find("Continuous Mountain Terrain");if(left!=null)Object.DestroyImmediate(left);
            var ocean=Object.Instantiate(AssetDatabase.LoadAssetAtPath<Material>(OceanPack+"/Resources/water.mat"));ocean.name="Island Ocean - Houidisoft";
            ocean.SetFloat("_Depth",5.5f);ocean.SetFloat("_Metallic",.05f);ocean.SetFloat("_Smouthness",.82f);ocean.SetFloat("_Normal_strenght",.23f);
            ocean.SetFloat("_Waves_height",.15f);ocean.SetFloat("_WaveSpeed",.065f);ocean.SetFloat("_Waves_Length",.20f);
            ocean.SetFloat("_water_movement_speed",.55f);ocean.SetFloat("_Texture_scale",85f);ocean.SetFloat("_Foam_depth",.65f);
            ocean.SetFloat("_foam_cutoff",5f);ocean.SetFloat("_foam_scale",.06f);ocean.SetFloat("_caustics_Strenght",0f);
            ocean.SetFloat("_Refraction_Power",.12f);
            ocean.SetColor("_deep_water_color",new Color(.045f,.20f,.29f,1));ocean.SetColor("_shallow_water_color",new Color(.17f,.48f,.46f,.23f));
            ocean.SetColor("_foam_color",new Color(.80f,.88f,.82f,1));Save(ocean,AssetsRoot+"/Materials/IslandOcean.mat");
            // Dense near-shore grid, exponentially widening cells out to a 2.4 km ocean horizon.
            var coords=new List<float>();for(int j=-100;j<=100;j++)coords.Add(j*1.5f);
            for(int j=1;j<=24;j++){float v=150+1050*Mathf.Pow(j/24f,1.65f);coords.Insert(0,-v);coords.Add(v);}
            int across=coords.Count;var sea=new List<Vector3>();var seaUV=new List<Vector2>();var seaTris=new List<int>();
            for(int z=0;z<across;z++)for(int x=0;x<across;x++)
            {
                sea.Add(new Vector3(coords[x],IslandCoast.SeaLevel,coords[z]));seaUV.Add(new Vector2(coords[x]/2400f,coords[z]/2400f));
                if(x==across-1||z==across-1)continue;
                // No ocean geometry under the pond. Its effects/material/depth remain independent.
                if(Mathf.Abs(coords[x])<52&&Mathf.Abs(coords[x+1])<52&&Mathf.Abs(coords[z])<52&&Mathf.Abs(coords[z+1])<52)continue;
                int j=z*across+x;seaTris.AddRange(new[]{j,j+across,j+1,j+1,j+across,j+across+1});
            }
            var seaMesh=new Mesh{name="Island Ocean Surface",indexFormat=IndexFormat.UInt32,vertices=sea.ToArray(),uv=seaUV.ToArray(),triangles=seaTris.ToArray()};seaMesh.RecalculateNormals();seaMesh.RecalculateTangents();seaMesh.RecalculateBounds();
            Save(seaMesh,AssetsRoot+"/Meshes/OceanSurface.asset");var seaObject=MeshObject("Surrounding Ocean",seaMesh,ocean,island.transform,false);seaObject.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            Camera.main.farClipPlane=1900;Camera.main.GetUniversalAdditionalCameraData().requiresDepthOption=CameraOverrideOption.On;
            // The package explicitly requests a prepass. This only changes when depth is produced, not pond settings.
            var pipeline=(UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            var pso=new SerializedObject(pipeline);var rendererData=pso.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue;
            var rso=new SerializedObject(rendererData);var depth=rso.FindProperty("m_CopyDepthMode");if(depth!=null){depth.enumValueIndex=2;rso.ApplyModifiedPropertiesWithoutUndo();}
        }
        private static GameObject Primitive(string name,PrimitiveType type,Transform parent,Vector3 p,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        private static void MakeHarvesting()
        {
            var bark=Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None).First(i=>i.Kind==EnvironmentAssetKind.Tree).GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).First(m=>m.name.Contains("Trunk"));
            var cut=Lit("Fresh Cut Wood",new Color(.69f,.44f,.22f));
            var chips=Lit("Wood Chips",new Color(.55f,.29f,.11f));
            var dust=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")){name="Bark Dust"};dust.SetColor("_BaseColor",new Color(.46f,.33f,.19f,.4f));
            dust.SetFloat("_Surface",1);dust.SetFloat("_Blend",0);dust.SetFloat("_SrcBlend",5);dust.SetFloat("_DstBlend",10);dust.SetFloat("_ZWrite",0);dust.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");dust.renderQueue=3000;Save(dust,AssetsRoot+"/Materials/BarkDust.mat");
            var chipGo=GameObject.CreatePrimitive(PrimitiveType.Cube);var chip=Object.Instantiate(chipGo.GetComponent<MeshFilter>().sharedMesh);Object.DestroyImmediate(chipGo);
            var vertices=chip.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]=Vector3.Scale(vertices[i],new Vector3(1,.22f,2.1f));chip.vertices=vertices;chip.RecalculateBounds();Save(chip,AssetsRoot+"/Meshes/WoodChip.asset");
            var bundle=new GameObject("Wood Bundle");
            for(int i=0;i<3;i++)
            {
                Vector3 p=new((i-1)*.19f,i==1?.13f:0,0);
                var log=Primitive("Split Log "+i,PrimitiveType.Cylinder,bundle.transform,p,new Vector3(.21f,.28f,.21f),bark);log.transform.localRotation=Quaternion.Euler(90,0,i*13);
                foreach(float end in new[]{-.281f,.281f}){var cap=Primitive("End Grain",PrimitiveType.Cylinder,bundle.transform,p+Vector3.forward*end,new Vector3(.19f,.006f,.19f),cut);cap.transform.localRotation=Quaternion.Euler(90,0,0);}
            }
            var rope=Lit("Bundle Cord",new Color(.27f,.21f,.12f));
            foreach(float z in new[]{-.14f,.14f})Primitive("Binding",PrimitiveType.Cube,bundle.transform,new Vector3(0,.04f,z),new Vector3(.6f,.05f,.035f),rope);
            bundle.AddComponent<BoxCollider>().size=new Vector3(.62f,.35f,.61f);bundle.AddComponent<WoodBundlePickup>();
            var prefab=PrefabUtility.SaveAsPrefabAsset(bundle,AssetsRoot+"/Prefabs/WoodBundle.prefab");
            CreateIcon(bundle);Object.DestroyImmediate(bundle);
            foreach(var item in Object.FindObjectsByType<EnvironmentAssetInstance>(FindObjectsSortMode.None).Where(i=>i.Kind==EnvironmentAssetKind.Tree))
            {
                foreach(var t in item.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
                item.gameObject.AddComponent<ChoppableTree>().Configure(cut,chips,dust,chip,prefab);
            }
            var player=Object.FindAnyObjectByType<PlayerController>();var arms=Object.FindAnyObjectByType<FirstPersonViewmodel>();
            var axeMesh=arms.Axe.GetComponentInChildren<MeshFilter>();var mesh=axeMesh.sharedMesh;
            var bladeVertices=mesh.vertices.Where(v=>v.y>Mathf.Lerp(mesh.bounds.min.y,mesh.bounds.max.y,.80f)).ToArray();
            float edge=bladeVertices.Min(v=>v.z);var edgeVertices=bladeVertices.Where(v=>v.z<edge+.012f).ToArray();
            Vector3 bladeLocal=edgeVertices.Aggregate(Vector3.zero,(a,b)=>a+b)/edgeVertices.Length;
            var blade=new GameObject("Blade Contact").transform;blade.SetParent(axeMesh.transform,false);blade.localPosition=bladeLocal;
            player.gameObject.AddComponent<AxeChopping>().Configure(blade);
            var icons=new List<RawImage>();var hud=Object.FindAnyObjectByType<InventoryHud>();
            for(int i=2;i<=4;i++)
            {
                var go=new GameObject("Wood Bundle Icon",typeof(RectTransform),typeof(RawImage));go.transform.SetParent(GameObject.Find("Inventory Slot "+i).transform,false);
                var image=go.GetComponent<RawImage>();image.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(AssetsRoot+"/Textures/WoodBundleIcon.asset");image.raycastTarget=false;image.enabled=false;
                image.rectTransform.sizeDelta=new Vector2(52,52);icons.Add(image);
            }
            var label=Label("Equipped Item",hud.transform,new Vector2(0,55),new Vector2(280,25),15);label.text="Axt";hud.ConfigureWood(icons.ToArray(),label);
        }
        private static void CreateIcon(GameObject bundle)
        {
            bundle.transform.position=new Vector3(0,-500,0);bundle.transform.rotation=Quaternion.Euler(15,30,-25);
            foreach(var t in bundle.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
            var camera=new GameObject("Wood Icon Camera").AddComponent<Camera>();camera.transform.position=bundle.transform.position+new Vector3(.4f,.7f,-1.5f);camera.transform.LookAt(bundle.transform.position);
            camera.orthographic=true;camera.orthographicSize=.46f;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.enabled=false;
            var rt=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(256,256,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,256,256),0,0);texture.Apply();texture.name="Wood Bundle Inventory Icon";Save(texture,AssetsRoot+"/Textures/WoodBundleIcon.asset");RenderTexture.active=previous;
            Object.DestroyImmediate(rt);Object.DestroyImmediate(camera.gameObject);
        }
        private static Text Label(string name,Transform parent,Vector2 pos,Vector2 size,int fontSize)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var t=go.GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=fontSize;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.rectTransform.anchoredPosition=pos;t.rectTransform.sizeDelta=size;return t;
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchoredPosition=pos;r.sizeDelta=size;go.GetComponent<Image>().color=color;return r;
        }
        private static void MakeMusic()
        {
            var root=new GameObject("Music and Pause");var source=root.AddComponent<AudioSource>();source.playOnAwake=false;
            string[] clips=AssetDatabase.FindAssets("t:AudioClip").Select(AssetDatabase.GUIDToAssetPath).ToArray();
            string path=clips.FirstOrDefault(p=>p.ToLowerInvariant().Contains("jungle"));if(path!=null)source.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            var canvas=GameObject.Find("HUD");
            var panel=Rect("Music Menu",canvas.transform,new Vector2(-190,-95),new Vector2(330,145),new Color(.055f,.095f,.085f,.97f));panel.anchorMin=panel.anchorMax=new Vector2(1,1);
            var label=Label("Volume",panel,new Vector2(0,40),new Vector2(285,32),21);
            var sliderRoot=new GameObject("Music Volume Slider",typeof(RectTransform),typeof(Slider));sliderRoot.transform.SetParent(panel,false);var sr=sliderRoot.GetComponent<RectTransform>();sr.sizeDelta=new Vector2(265,30);
            var background=Rect("Track",sr,Vector2.zero,new Vector2(265,8),new Color(.20f,.29f,.27f));
            var handle=Rect("Handle",sr,Vector2.zero,new Vector2(20,28),new Color(.87f,.73f,.43f));
            var slider=sliderRoot.GetComponent<Slider>();slider.minValue=0;slider.maxValue=1;slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();slider.direction=Slider.Direction.LeftToRight;
            Label("Menu Help",panel,new Vector2(0,-43),new Vector2(310,38),14).text="M: Weiterspielen  ·  Esc: Maus freigeben";
            root.AddComponent<MusicMenu>().Configure(source,panel.gameObject,slider,label);panel.gameObject.SetActive(false);
            var hint=Label("Music Help",canvas.transform,new Vector2(-110,-24),new Vector2(190,25),16);hint.rectTransform.anchorMin=hint.rectTransform.anchorMax=new Vector2(1,1);hint.text="M – Musik / Pause";
            Debug.Log("JUNGLE_MUSIC: "+(path??"MISSING - awaiting user audio path"));
        }
    }
}
