using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CrystalSprint
{
    public enum TreeHarvestState { Standing, Falling, Fallen, Processing, Harvested }
    public sealed class ChoppableTree : MonoBehaviour
    {
        [SerializeField] private Material cutWood, chipMaterial, dustMaterial;
        [SerializeField] private Mesh chipMesh;
        [SerializeField] private GameObject bundlePrefab;
        [SerializeField] private Mesh readableCutSource;
        public void ConfigureCutSource(Mesh mesh) => readableCutSource=mesh;
        private readonly List<Mesh> ownedMeshes=new();
        private Transform fallen, stump;
        private CapsuleCollider trunk;
        private MeshCollider terrain;
        private Vector3 contact, fallDirection;
        private Quaternion standingRotation;
        private float reaction;
        private int lastSwing=-1;
        private LumberjackEquipment lastOwner;
        public TreeHarvestState State { get; private set; }
        public int StandingHits { get; private set; }
        public int FallenHits { get; private set; }
        public Transform FallenObject => fallen;
        public Transform Stump => stump;
        public Vector3 LastHitPoint { get; private set; }
        public float FallAngle { get; private set; }
        public bool CanBeHit => State==TreeHarvestState.Standing || State==TreeHarvestState.Fallen;
        public bool AcceptsCollider(Collider target) => target.GetComponent<TreeHitSurface>() != null &&
            (State == TreeHarvestState.Standing || State == TreeHarvestState.Fallen && fallen != null && target.transform == fallen);
        public void Configure(Material cut,Material chips,Material dust,Mesh geometry,GameObject bundle)
        {cutWood=cut;chipMaterial=chips;dustMaterial=dust;chipMesh=geometry;bundlePrefab=bundle;}
        private void Awake()
        {
            trunk=GetComponents<CapsuleCollider>().First(c=>c.enabled);
            contact=GetComponent<EnvironmentAssetInstance>().GroundContact;
            terrain=GameObject.Find("Ground").GetComponent<MeshCollider>();
            standingRotation=transform.rotation;
        }
        private void Update()
        {
            if(State!=TreeHarvestState.Standing || reaction<=0) return;
            reaction=Mathf.Max(0,reaction-Time.deltaTime);
            float sway=Mathf.Sin((.32f-reaction)*28f)*reaction*2f;
            transform.rotation=Quaternion.AngleAxis(sway,Vector3.Cross(Vector3.up,fallDirection))*standingRotation;
        }
        // The caller must provide a real blade sweep contact, not a crosshair/range guess.
        public bool ReceiveAxeContact(LumberjackEquipment owner,int swing,Vector3 point,Vector3 normal)
        {
            if(!CanBeHit || owner==null || !owner.AxeEquipped || (lastOwner==owner && lastSwing==swing)) return false;
            lastOwner=owner;lastSwing=swing;LastHitPoint=point;
            ChoppingEffects.Burst(point,normal,chipMaterial,dustMaterial,chipMesh);
            if(State==TreeHarvestState.Standing)
            {
                StandingHits++;reaction=.32f;
                fallDirection=Vector3.ProjectOnPlane(transform.position-owner.transform.position,Vector3.up).normalized;
                if(StandingHits==3) { transform.rotation=standingRotation; State=TreeHarvestState.Falling; StartCoroutine(Fall()); }
            }
            else if(++FallenHits==3) { State=TreeHarvestState.Processing;StartCoroutine(Process(owner)); }
            else StartCoroutine(LogReaction(normal));
            return true;
        }
        private float Ground(Vector3 p)
        {
            if(terrain.Raycast(new Ray(new Vector3(p.x,20,p.z),Vector3.down),out var hit,50))return hit.point.y;
            return IslandCoast.Height(p.x,p.z,ForestWorld.Height(p.x,p.z));
        }
        private MeshRenderer Part(string label,Mesh mesh,Material[] materials,Transform parent,Vector3 p)
        {
            var go=new GameObject(label);go.transform.SetParent(parent,true);go.transform.position=p;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;ownedMeshes.Add(mesh);
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterials=materials;return renderer;
        }
        private IEnumerator Fall()
        {
            yield return new WaitForSeconds(.22f);
            Vector3 cut=contact+Vector3.up*.36f;
            var lod=GetComponent<LODGroup>();
            var source=lod.GetLODs()[0].renderers.Select(r=>r.GetComponent<MeshFilter>()).First(f=>f!=null);
            var materials=source.GetComponent<Renderer>().sharedMaterials.Concat(new[]{cutWood}).ToArray();
            Mesh top=TreeMeshCut.Cut(source,cut,true,readableCutSource), bottom=TreeMeshCut.Cut(source,cut,false,readableCutSource);
            // Objects remain under their original tree for clean scene reload/ownership.
            stump=Part("Matching Cut Stump",bottom,materials,transform,cut).transform;
            fallen=Part("Felled Original Tree",top,materials,transform,cut).transform;
            lod.enabled=false;
            foreach(var r in lod.GetLODs().SelectMany(l=>l.renderers).Distinct()){r.enabled=false;r.forceRenderingOff=true;}
            trunk.enabled=false;
            foreach(var c in GetComponents<Collider>())c.enabled=false;
            foreach(var surface in GetComponentsInChildren<TreeHitSurface>())surface.GetComponent<Collider>().enabled=false;
            float radius=trunk.radius*Mathf.Max(transform.lossyScale.x,transform.lossyScale.z);
            var stumpCollider=stump.gameObject.AddComponent<CapsuleCollider>();stumpCollider.radius=radius;stumpCollider.height=.36f;
            stumpCollider.center=new Vector3(0,-.18f,0);stump.gameObject.AddComponent<SurfaceMarker>().Configure(SurfaceType.Wood);
            // Original tree colliders are approximate; the fallen trunk gets a finite solid capsule.
            var log=fallen.gameObject.AddComponent<CapsuleCollider>();log.radius=radius;
            log.height=Mathf.Max(1f,trunk.height*transform.lossyScale.y-.36f);log.center=Vector3.up*(log.height*.5f);
            fallen.gameObject.AddComponent<SurfaceMarker>().Configure(SurfaceType.Wood);
            var barkMesh=Object.Instantiate(top);barkMesh.name="Felled Bark Collision";
            var barkTriangles=new List<int>();for(int s=0;s<materials.Length;s++)if(materials[s].name.Contains("Trunk") || s==materials.Length-1)barkTriangles.AddRange(top.GetTriangles(s));
            barkMesh.subMeshCount=1;barkMesh.SetTriangles(barkTriangles,0);ownedMeshes.Add(barkMesh);
            fallen.gameObject.AddComponent<MeshCollider>().sharedMesh=barkMesh;fallen.gameObject.AddComponent<TreeHitSurface>();
            fallDirection=ChooseDirection(fallDirection,log.height);
            Vector3 axis=Vector3.Cross(Vector3.up,fallDirection);
            float endAngle=88f;
            var restVertices=top.vertices;
            var deformed=new Vector3[restVertices.Length];
            // Leafy crowns flex and settle instead of holding a felled trunk upright at 50 degrees.
            // Cache the final terrain support: no thousands of raycasts per animation frame.
            Quaternion endRotation=Quaternion.AngleAxis(endAngle,axis);
            var support=new float[restVertices.Length];
            for(int i=0;i<support.Length;i++)support[i]=Ground(cut+endRotation*restVertices[i])+.025f;
            void SeatCrown()
            {
                for(int i=0;i<restVertices.Length;i++)
                {
                    Vector3 w=fallen.TransformPoint(restVertices[i]);
                    if(w.y<support[i])w.y=support[i];
                    deformed[i]=fallen.InverseTransformPoint(w);
                }
                top.vertices=deformed;top.RecalculateBounds();
            }
            float angle=0, speed=2f;
            while(angle<endAngle)
            {
                speed+=Time.deltaTime*(10f+48f*Mathf.Sin(angle*Mathf.Deg2Rad));
                angle=Mathf.Min(endAngle,angle+speed*Time.deltaTime); FallAngle=angle;
                fallen.rotation=Quaternion.AngleAxis(angle,axis);if(angle>38)SeatCrown();Physics.SyncTransforms();yield return null;
            }
            Vector3 impact=cut+fallen.up*log.height;impact.y=Ground(impact)+.08f;
            ChoppingEffects.Burst(impact,Vector3.up,chipMaterial,dustMaterial,chipMesh,true);
            for(float t=0;t<.45f;t+=Time.deltaTime)
            {
                float bounce=Mathf.Sin(t/.45f*Mathf.PI)*1.1f;
                fallen.rotation=Quaternion.AngleAxis(endAngle-bounce,axis);SeatCrown();yield return null;
            }
            fallen.rotation=Quaternion.AngleAxis(endAngle,axis);SeatCrown();top.RecalculateNormals();top.RecalculateTangents();
            barkMesh.vertices=top.vertices;barkMesh.RecalculateBounds();var barkCollider=fallen.GetComponent<MeshCollider>();barkCollider.sharedMesh=null;barkCollider.sharedMesh=barkMesh;
            log.enabled=false; // Once settled, use the actual bark surface, not an oversized invisible capsule.
            var grass=FindAnyObjectByType<InstancedForestGrass>();
            if(grass!=null)
            {
                var clearings=new List<Bounds>{new Bounds(contact,new Vector3(radius*3.6f,3,radius*3.6f))};
                for(float d=.6f;d<log.height;d+=.8f)clearings.Add(new Bounds(cut+fallen.up*d,new Vector3(radius*2.8f,4,radius*2.8f)));
                grass.AddLocalClearings(clearings.ToArray());
            }
            State=TreeHarvestState.Fallen;
        }
        private Vector3 ChooseDirection(Vector3 preferred,float length)
        {
            Vector3 best=preferred;float bestScore=float.MaxValue;
            foreach(float turn in new[]{0f,35f,-35f,70f,-70f,110f,-110f,180f})
            {
                Vector3 d=Quaternion.Euler(0,turn,0)*preferred;float score=Mathf.Abs(turn)*.01f;
                foreach(var hit in Physics.SphereCastAll(contact+Vector3.up*1.5f,.65f,d,length,~0,QueryTriggerInteraction.Ignore))
                {
                    if(hit.transform.IsChildOf(transform)||hit.collider==terrain)continue;
                    score+=hit.collider.GetComponentInParent<PondCabin>()!=null?100:8;
                }
                if(score<bestScore){bestScore=score;best=d;}
            }
            return best;
        }
        private IEnumerator LogReaction(Vector3 normal)
        {
            Quaternion rest=fallen.rotation;
            for(float t=0;t<.20f;t+=Time.deltaTime)
            {fallen.rotation=Quaternion.AngleAxis(Mathf.Sin(t/.20f*Mathf.PI)*.35f,normal)*rest;yield return null;}
            if(fallen!=null)fallen.rotation=rest;
        }
        private IEnumerator Process(LumberjackEquipment owner)
        {
            foreach(var collider in fallen.GetComponents<Collider>())collider.enabled=false;
            Vector3 position=fallen.TransformPoint(fallen.GetComponent<CapsuleCollider>().center);
            Vector3[] offsets={fallen.up*.45f,-fallen.up*.45f,Vector3.zero};
            var pieces=new List<Transform>();
            foreach(var offset in offsets)
            {
                var piece=Instantiate(bundlePrefab,position+offset,Quaternion.Euler(0,Random.Range(0,360),0));
                piece.name="Processing wood pieces";piece.GetComponent<WoodBundlePickup>().enabled=false;
                foreach(var c in piece.GetComponents<Collider>())c.enabled=false;
                pieces.Add(piece.transform);
            }
            Vector3 original=fallen.localScale;
            bool burst=false;
            for(float t=0;t<.85f;t+=Time.deltaTime)
            {
                float a=Mathf.Clamp01(t/.85f);
                // Short compression into visibly separate pieces, not an immediate disappearing tree.
                fallen.localScale=original*Mathf.Lerp(1,.02f,Mathf.SmoothStep(0,1,a));
                foreach(var piece in pieces)piece.position+=Vector3.up*(Mathf.Cos(a*Mathf.PI)*.6f*Time.deltaTime);
                if(!burst && a>.2f){burst=true;ChoppingEffects.Burst(position,Vector3.up,chipMaterial,dustMaterial,chipMesh,true);}
                yield return null;
            }
            foreach(var piece in pieces)Destroy(piece.gameObject);
            Destroy(fallen.gameObject);
            if(owner==null || !owner.TryAddWoodBundle())
            {
                position.y=Ground(position)+.18f; Instantiate(bundlePrefab,position,Quaternion.identity);
            }
            State=TreeHarvestState.Harvested;
        }
        private void OnDestroy(){foreach(var mesh in ownedMeshes)if(mesh!=null)Destroy(mesh);}
    }
}
