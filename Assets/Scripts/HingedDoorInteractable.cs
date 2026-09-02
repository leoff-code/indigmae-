using UnityEngine;

namespace CrystalSprint
{
    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    public sealed class HingedDoorInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField, Range(80f, 100f)] private float openAngle = 95f;
        [SerializeField, Min(.1f)] private float duration = .8f;
        [SerializeField] private Quaternion closedRotation = Quaternion.identity;
        [SerializeField] private bool startsOpen;
        private Rigidbody hingeBody;
        private BoxCollider leaf;
        private CharacterController playerBody;
        private readonly Collider[] nearby = new Collider[32];
        private float amount;
        private bool targetOpen;
        public float OpenAmount => amount;
        public float OpenAngle => openAngle;
        public bool IsOpen => amount >= .999f;
        public bool IsMoving => !Mathf.Approximately(amount, targetOpen ? 1 : 0);
        public bool IsObstructed { get; private set; }
        public int InteractionCount { get; private set; }
        public bool CanInteract => isActiveAndEnabled && (!IsMoving || IsObstructed);

        public void Configure(bool initiallyOpen)
        {
            startsOpen = initiallyOpen; closedRotation = Quaternion.identity;
            transform.localRotation = closedRotation * Quaternion.Euler(0, initiallyOpen ? openAngle : 0, 0);
        }
        private void Awake()
        {
            hingeBody = GetComponent<Rigidbody>(); leaf = GetComponent<BoxCollider>();
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) playerBody = player.GetComponent<CharacterController>();
            amount = startsOpen ? 1 : 0; targetOpen = startsOpen;
            transform.localRotation = Rotation(amount);
        }
        public void Interact(PlayerInteractor user)
        {
            if (!CanInteract) return;
            targetOpen = !targetOpen; IsObstructed = false; InteractionCount++;
        }
        private Quaternion Rotation(float value) => closedRotation * Quaternion.Euler(0, openAngle * Mathf.SmoothStep(0, 1, value), 0);
        private void FixedUpdate()
        {
            if (!IsMoving) { IsObstructed = false; return; }
            float next = Mathf.MoveTowards(amount, targetOpen ? 1 : 0, Time.fixedDeltaTime / duration);
            // Sample the small swept angular step. A closing/opening leaf pauses at the player
            // instead of pushing the CharacterController through a wall or trapping it.
            for (int sample = 1; sample <= 3; sample++)
            {
                Quaternion pose = transform.parent.rotation * Rotation(Mathf.Lerp(amount, next, sample / 3f));
                if (WouldTouchPlayer(pose))
                { IsObstructed = true; return; }
            }
            IsObstructed = false; amount = next;
            hingeBody.MoveRotation(transform.parent.rotation * Rotation(amount));
        }

        private bool WouldTouchPlayer(Quaternion pose)
        {
            if (playerBody == null || !playerBody.enabled) return false;
            Vector3 scale = transform.lossyScale;
            Vector3 center = transform.position + pose * Vector3.Scale(leaf.center, scale);
            // Waiting for actual penetration is too late: the CharacterController and
            // speculative contacts begin depenetrating within its skin/contact margin.
            float clearance = playerBody.skinWidth + .08f;
            Vector3 halfSize = Vector3.Scale(leaf.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))) * .5f + Vector3.one * clearance;
            int count = Physics.OverlapBoxNonAlloc(center, halfSize, nearby, pose, 1 << playerBody.gameObject.layer, QueryTriggerInteraction.Ignore);
            if (count == nearby.Length) return true;
            for (int i = 0; i < count; i++) if (nearby[i] == playerBody) return true;
            return false;
        }
    }
}
