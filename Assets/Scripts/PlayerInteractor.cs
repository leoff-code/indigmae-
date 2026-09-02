using UnityEngine;
using UnityEngine.UI;

namespace CrystalSprint
{
    [DefaultExecutionOrder(100)]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera view;
        [SerializeField] private Text prompt;
        [SerializeField, Min(.1f)] private float reach = 2.75f;
        private readonly RaycastHit[] hits = new RaycastHit[64];
        private GameInput input;
        private CursorLockController cursorLock;
        private int lastUseFrame = -1;
        public IInteractable Target { get; private set; }
        public float Reach => reach;

        public void Configure(Camera camera, Text label) { view = camera; prompt = label; }
        private void Awake() { input = new GameInput(); cursorLock = FindAnyObjectByType<CursorLockController>(); }
        private void OnEnable() => input?.Enable();
        private void OnDisable() { input?.Disable(); Target = null; if (prompt != null) prompt.enabled = false; }
        private void OnDestroy() => input?.Dispose();

        private void LateUpdate()
        {
            RefreshTarget();
            if (input.InteractPressed) TryInteract();
            if (prompt != null) prompt.enabled = Target != null;
        }

        public void RefreshTarget()
        {
            Target = null;
            if (view == null || (cursorLock != null && !cursorLock.IsLocked)) return;
            int count = Physics.RaycastNonAlloc(view.transform.position, view.transform.forward, hits, reach, view.cullingMask, QueryTriggerInteraction.Collide);
            // Fail closed if a crowded ray exceeds the fixed buffer; never use through a wall.
            if (count == hits.Length) return;
            float nearest = float.PositiveInfinity;
            IInteractable candidate = null;
            for (int i = 0; i < count; i++)
            {
                Collider collider = hits[i].collider;
                if (collider.transform.IsChildOf(transform)) continue;
                IInteractable usable = collider.GetComponentInParent<IInteractable>();
                if (collider.isTrigger && usable == null) continue;
                if (hits[i].distance >= nearest) continue;
                nearest = hits[i].distance; candidate = usable;
            }
            // The nearest solid occludes everything behind it, even when it isn't usable.
            if (candidate != null && candidate.CanInteract) Target = candidate;
        }

        public bool TryInteract()
        {
            RefreshTarget();
            if (Target == null || lastUseFrame == Time.frameCount) return false;
            lastUseFrame = Time.frameCount;
            Target.Interact(this);
            return true;
        }
    }
}
