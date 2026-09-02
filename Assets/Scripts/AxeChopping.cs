using UnityEngine;

namespace CrystalSprint
{
    [DefaultExecutionOrder(120)]
    public sealed class AxeChopping : MonoBehaviour
    {
        [SerializeField] private Transform blade;
        private LumberjackEquipment equipment;
        private LumberjackVisual animation;
        private FirstPersonViewmodel arms;
        private readonly RaycastHit[] hits=new RaycastHit[24];
        private readonly Collider[] overlaps=new Collider[24];
        private Vector3 previous;
        private int swing=-1;
        public int ValidHits { get; private set; }
        public float ContactProgress { get; private set; } = -1;
        public Transform Blade => blade;
        public Vector3 LastSweepPoint { get; private set; }
        public Collider LastObstacle { get; private set; }
        public float DownwardLean => animation != null && animation.IsAttacking
            ? Mathf.Sin(animation.AttackProgress*Mathf.PI)*1.02f*Mathf.InverseLerp(35,65,Camera.main.GetComponent<FirstPersonCamera>().Pitch) : 0f;
        public void Configure(Transform edge){blade=edge;}
        private void Awake()
        {equipment=GetComponent<LumberjackEquipment>();animation=GetComponent<LumberjackVisual>();arms=FindAnyObjectByType<FirstPersonViewmodel>();}
        private void LateUpdate()
        {
            if(blade==null || MusicMenu.IsOpen || Time.timeScale<=0)return;
            if(swing!=equipment.AttackCount){swing=equipment.AttackCount;ContactProgress=-1;LastObstacle=null;}
            Vector3 current=blade.position;
            float t=animation.AttackProgress;
            if(equipment.AxeEquipped && animation.IsAttacking && t>=.37f && t<=.56f && ContactProgress<0)
            {
                LastSweepPoint=current;
                Vector3 delta=current-previous;
                Collider nearest=null;Vector3 point=default,normal=default;float distance=float.MaxValue;
                int count=delta.magnitude>.0001f?Physics.SphereCastNonAlloc(previous,.065f,delta.normalized,hits,delta.magnitude,~0,QueryTriggerInteraction.Ignore):0;
                for(int i=0;i<count;i++)
                {
                    var h=hits[i];if(h.transform.IsChildOf(transform)||h.distance>=distance || IsMovementProxy(h.collider))continue;
                    nearest=h.collider;point=h.point;normal=h.normal;distance=h.distance;
                }
                if(nearest==null)
                {
                    count=Physics.OverlapSphereNonAlloc(current,.065f,overlaps,~0,QueryTriggerInteraction.Ignore);
                    for(int i=0;i<count;i++)
                    {
                        var c=overlaps[i];if(c.transform.IsChildOf(transform) || IsMovementProxy(c))continue;
                        // A triangle/sphere overlap is already a valid narrow-phase contact.
                        // ClosestPoint is not supported by all non-convex MeshCollider backends.
                        Vector3 p=c is MeshCollider mesh && !mesh.convex?current:c.ClosestPoint(current);float d=(p-current).sqrMagnitude;if(d>=distance)continue;
                        nearest=c;point=p;normal=(current-p).sqrMagnitude>.00001f?(current-p).normalized:-Camera.main.transform.forward;distance=d;
                    }
                }
                if(nearest!=null)
                {
                    LastObstacle=nearest;
                    ContactProgress=t;
                    var tree=nearest.GetComponentInParent<ChoppableTree>();
                    if(tree!=null && tree.AcceptsCollider(nearest) && tree.ReceiveAxeContact(equipment,swing,point,normal))ValidHits++;
                }
            }
            previous=current;
        }
        public float PoseProgress(float t)
        {
            if(ContactProgress<0 || t<ContactProgress)return t;
            // Stop at contact and recover on the same arc, instead of driving through the trunk.
            return Mathf.Lerp(ContactProgress,0,Mathf.SmoothStep(0,1,Mathf.InverseLerp(ContactProgress+.10f,1,t)));
        }
        private static bool IsMovementProxy(Collider c) => c.GetComponentInParent<ChoppableTree>() != null && c is CapsuleCollider;
    }
}
