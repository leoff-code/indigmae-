using UnityEngine;

namespace CrystalSprint
{
    [DefaultExecutionOrder(-600)]
    [RequireComponent(typeof(Camera))]
    public sealed class FirstPersonCamera : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private Camera viewmodelCamera;
        [SerializeField] private Vector3 eyeOffset = new(0f, .73f, .06f);
        [SerializeField, Range(.02f, .3f)] private float mouseSensitivity = .10f;
        [SerializeField] private Vector2 pitchLimits = new(-80f, 75f);
        private GameInput input;
        private CursorLockController cursorLock;
        private float yaw;
        private float pitch = 4f;
        public float Yaw => yaw;
        public float Pitch => pitch;
        public Vector2 LookDelta { get; private set; }
        public Vector3 EyePosition => player == null ? transform.position : player.transform.TransformPoint(eyeOffset);
        public PlayerController Player => player;
        public Camera ViewmodelCamera => viewmodelCamera;

        public void Configure(PlayerController owner, Camera overlay)
        {
            player = owner;
            viewmodelCamera = overlay;
        }

        private void Awake()
        {
            input = new GameInput();
            cursorLock = FindAnyObjectByType<CursorLockController>();
            if (player != null) yaw = player.transform.eulerAngles.y;
            ApplyRotation();
        }

        private void OnEnable()
        {
            input?.Enable();
            if (player != null) player.UseViewFacing = true;
            if (viewmodelCamera != null) viewmodelCamera.enabled = true;
        }

        private void OnDisable()
        {
            input?.Disable();
            if (player != null) player.UseViewFacing = false;
            if (viewmodelCamera != null) viewmodelCamera.enabled = false;
        }

        private void OnDestroy() => input?.Dispose();

        private void Update()
        {
            LookDelta = Vector2.zero;
            if (cursorLock != null ? cursorLock.IsLocked && !cursorLock.JustLockedThisFrame : Cursor.lockState == CursorLockMode.Locked)
            {
                // Mouse delta already represents this frame; only stick input is time-scaled.
                LookDelta = input.Look * (input.LookUsesGamepad ? 10f * Time.deltaTime : mouseSensitivity);
                yaw = Mathf.Repeat(yaw + LookDelta.x, 360f);
                pitch = Mathf.Clamp(pitch - LookDelta.y, pitchLimits.x, pitchLimits.y);
            }
            ApplyRotation();
        }

        private void LateUpdate()
        {
            if (player != null) transform.position = EyePosition;
        }

        public void SetViewAngles(float heading, float elevation)
        {
            yaw = Mathf.Repeat(heading, 360f);
            pitch = Mathf.Clamp(elevation, pitchLimits.x, pitchLimits.y);
            ApplyRotation();
            if (player != null) transform.position = EyePosition;
        }

        private void ApplyRotation()
        {
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            if (player != null) player.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
