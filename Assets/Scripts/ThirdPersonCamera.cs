using UnityEngine;

namespace CrystalSprint
{
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(0.1f)] private float distance = 9.5f;
        [SerializeField, Min(0f)] private float sensitivity = 0.12f;
        [SerializeField, Min(0f)] private float positionSmoothness = 16f;
        [SerializeField] private Vector2 pitchLimits = new(-15f, 65f);
        [SerializeField] private LayerMask obstructionMask = ~0;

        private GameInput input;
        private float yaw;
        private float pitch = 22f;

        public Transform Target { get => target; set => target = value; }

        private void Awake()
        {
            input = new GameInput();
            yaw = transform.eulerAngles.y;
        }

        private void OnEnable() => input?.Enable();

        private void OnDisable() => input?.Disable();

        private void OnDestroy() => input?.Dispose();

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector2 look = Cursor.lockState == CursorLockMode.Locked ? input.Look : Vector2.zero;
            yaw += look.x * sensitivity;
            pitch = Mathf.Clamp(pitch - look.y * sensitivity, pitchLimits.x, pitchLimits.y);

            Vector3 focus = target.position + Vector3.up * 1.25f;
            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 direction = orbit * Vector3.back;
            float adjustedDistance = distance;

            if (Physics.SphereCast(focus, 0.22f, direction, out RaycastHit hit, distance,
                    obstructionMask, QueryTriggerInteraction.Ignore))
            {
                adjustedDistance = Mathf.Max(0.35f, hit.distance - 0.15f);
            }

            Vector3 desiredPosition = focus + direction * adjustedDistance;
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                1f - Mathf.Exp(-positionSmoothness * Time.deltaTime));
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }
    }
}
