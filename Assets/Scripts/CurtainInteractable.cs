using UnityEngine;

namespace CrystalSprint
{
    public sealed class CurtainInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform leftPanel, rightPanel;
        [SerializeField] private float fabricWidth;
        [SerializeField, Min(.1f)] private float duration = .85f;
        [SerializeField] private bool startsOpen;
        private float amount;
        private bool targetOpen;
        public Transform LeftPanel => leftPanel;
        public Transform RightPanel => rightPanel;
        public float OpenAmount => amount;
        public bool IsOpen => amount >= .999f;
        public bool IsMoving => !Mathf.Approximately(amount, targetOpen ? 1 : 0);
        public bool CanInteract => isActiveAndEnabled && !IsMoving;
        public int InteractionCount { get; private set; }

        public void Configure(Transform left, Transform right, float width, bool initiallyOpen)
        {
            leftPanel = left; rightPanel = right; fabricWidth = width; startsOpen = initiallyOpen;
            amount = initiallyOpen ? 1 : 0; targetOpen = initiallyOpen; ApplyPose();
        }
        private void Awake() { amount = startsOpen ? 1 : 0; targetOpen = startsOpen; ApplyPose(); }
        public void Interact(PlayerInteractor user)
        {
            if (!CanInteract) return;
            targetOpen = !targetOpen; InteractionCount++;
        }
        private void Update()
        {
            if (!IsMoving) return;
            amount = Mathf.MoveTowards(amount, targetOpen ? 1 : 0, Time.deltaTime / duration); ApplyPose();
        }
        private void ApplyPose()
        {
            if (leftPanel == null || rightPanel == null) return;
            float gather = Mathf.Lerp(1, .22f, Mathf.SmoothStep(0, 1, amount));
            float center = fabricWidth * .5f - fabricWidth * .25f * gather;
            leftPanel.localPosition = new Vector3(0, 0, -center);
            rightPanel.localPosition = new Vector3(0, 0, center);
            // Gather only across the pleats: cloth height and fold depth stay unchanged.
            leftPanel.localScale = rightPanel.localScale = new Vector3(1, 1, gather);
        }
    }
}
