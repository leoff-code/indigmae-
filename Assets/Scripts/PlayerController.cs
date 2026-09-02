using UnityEngine;

namespace CrystalSprint
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float walkSpeed = 6.8f;
        [SerializeField, Min(0f)] private float sprintSpeed = 10.4f;
        [SerializeField, Min(0f)] private float acceleration = 22f;
        [SerializeField, Min(0f)] private float deceleration = 30f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.38f;
        [SerializeField, Min(0f)] private float rotationSpeed = 14f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.65f;
        [SerializeField] private float gravity = -28f;
        [SerializeField, Min(0f)] private float groundedPull = 4f;

        private CharacterController characterController;
        private Transform cameraTransform;
        private GameInput input;
        private Vector3 planarVelocity;
        private float verticalVelocity;
        private bool grounded;
        private Vector3 groundNormal = Vector3.up;
        private bool useTestInput;
        private Vector2 testMove;
        private bool testJump;
        private bool testSprint;
        private int groundingSuppressionFrames;

        public bool IsGrounded => grounded;
        public bool IsSprinting { get; private set; }
        public float PlanarSpeed => new Vector2(planarVelocity.x, planarVelocity.z).magnitude;
        public Vector3 GroundNormal => groundNormal;
        // First-person look owns yaw; strafing/backpedalling must not turn the character away from the view.
        public bool UseViewFacing { get; set; }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            cameraTransform = Camera.main != null ? Camera.main.transform : null;
            input = new GameInput();
        }

        private void OnEnable() => input?.Enable();

        private void OnDisable() => input?.Disable();

        private void OnDestroy() => input?.Dispose();

        private void Update()
        {
            UpdateGrounding();
            Vector2 moveInput = useTestInput ? testMove : input.Move;
            bool jumpPressed = useTestInput ? ConsumeTestJump() : input.JumpPressed;
            bool sprintHeld = useTestInput ? testSprint : input.SprintHeld;
            Move(moveInput, jumpPressed, sprintHeld);
        }

        private void Move(Vector2 moveInput, bool jumpPressed, bool sprintHeld)
        {
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 inputDirection = Vector3.ClampMagnitude(forward * moveInput.y + right * moveInput.x, 1f);
            if (!UseViewFacing && inputDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(inputDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    1f - Mathf.Exp(-rotationSpeed * Time.deltaTime));
            }

            IsSprinting = sprintHeld && grounded && inputDirection.sqrMagnitude > 0.04f;
            float targetSpeed = IsSprinting ? sprintSpeed : walkSpeed;
            Vector3 targetVelocity = inputDirection * targetSpeed;
            float velocityChange = inputDirection.sqrMagnitude > 0.001f ? acceleration : deceleration;
            if (!grounded)
            {
                velocityChange *= airControl;
            }
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, velocityChange * Time.deltaTime);

            if (grounded && verticalVelocity < 0f)
            {
                verticalVelocity = -groundedPull;
            }

            if (jumpPressed && grounded)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                grounded = false;
                IsSprinting = false;
            }

            verticalVelocity += gravity * Time.deltaTime;
            Vector3 velocity = planarVelocity + Vector3.up * verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
        }

        private void UpdateGrounding()
        {
            if (groundingSuppressionFrames > 0)
            {
                groundingSuppressionFrames--;
                grounded = false;
                groundNormal = Vector3.up;
                return;
            }

            if (characterController == null || verticalVelocity > 0.1f)
            {
                grounded = false;
                groundNormal = Vector3.up;
                return;
            }

            float radius = characterController.radius * 0.86f;
            float distance = characterController.height * 0.5f - radius + 0.075f;
            Vector3 origin = transform.position + characterController.center + Vector3.up * 0.04f;
            bool groundFound = Physics.SphereCast(
                origin,
                radius,
                Vector3.down,
                out RaycastHit hit,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore);
            grounded = characterController.isGrounded || groundFound;
            groundNormal = groundFound ? hit.normal : Vector3.up;
        }

        private bool ConsumeTestJump()
        {
            bool value = testJump;
            testJump = false;
            return value;
        }

        public void SetTestInput(Vector2 move, bool jump, bool sprint = false)
        {
            useTestInput = true;
            testMove = move;
            testJump |= jump;
            testSprint = sprint;
        }

        public void ClearTestInput()
        {
            useTestInput = false;
            testMove = Vector2.zero;
            testJump = false;
            testSprint = false;
        }

        public void Warp(Vector3 position)
        {
            characterController.enabled = false;
            transform.position = position;
            characterController.enabled = true;
            verticalVelocity = 0f;
            planarVelocity = Vector3.zero;
            grounded = false;
            groundingSuppressionFrames = 2;
            Physics.SyncTransforms();
        }
    }
}
