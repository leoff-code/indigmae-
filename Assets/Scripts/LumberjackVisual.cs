using UnityEngine;

namespace CrystalSprint
{
    // Two-bone arms keep the hand attached while shoulder, elbow and wrist articulate.
    [DefaultExecutionOrder(40)]
    public sealed class LumberjackVisual : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform axeGrip;
        [SerializeField] private Transform upperBody;
        [SerializeField] private Transform leftElbow;
        [SerializeField] private Transform rightElbow;
        [SerializeField] private Transform leftWrist;
        [SerializeField] private Transform rightWrist;
        [SerializeField] private float strideAngle = 24f;
        [SerializeField] private float strideSpeed = 9f;
        public const float AttackDuration = 1.15f;
        public const int AttackVariantCount = 1;
        private CharacterController controller;
        private PlayerController player;
        private LumberjackEquipment equipment;
        private Quaternion leftLegRest, rightLegRest, rootRotation;
        private Vector3 rootPosition, bodyPosition;
        private float phase, movementBlend, sprintBlend, attackTime;
        private bool attacking;
        private Vector3 attackStart;
        private const float UpperLength = .35f, LowerLength = .38f;
        public float SprintBlend => sprintBlend;
        public bool IsAttacking => attacking;
        public int LastAttackType { get; private set; } = -1;
        public float AttackProgress => attacking ? Mathf.Clamp01(attackTime / AttackDuration) : 0f;
        public Transform RightElbow => rightElbow;
        public Transform RightWrist => rightWrist;

        // The original project generator can still configure its unarticulated source model.
        public void Configure(Transform root, Transform left, Transform right, Transform leftArmTransform, Transform rightArmTransform, Transform axeGripTransform)
        {
            visualRoot = root; leftLeg = left; rightLeg = right;
            leftArm = leftArmTransform; rightArm = rightArmTransform; axeGrip = axeGripTransform;
        }

        public void ConfigureArticulation(Transform body, Transform le, Transform re, Transform lw, Transform rw)
        {
            upperBody = body; leftElbow = le; rightElbow = re; leftWrist = lw; rightWrist = rw;
            ApplyReferencePose();
        }

        public void ApplyReferencePose()
        {
            if (upperBody == null) return;
            SolveArm(rightArm, rightElbow, rightWrist, new Vector3(.67f, -.23f, .35f), 1f, true);
            SolveArm(leftArm, leftElbow, leftWrist, new Vector3(-.57f, -.30f, .14f), -1f, false);
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            player = GetComponent<PlayerController>();
            equipment = GetComponent<LumberjackEquipment>();
            if (visualRoot == null) return;
            rootPosition = visualRoot.localPosition;
            rootRotation = visualRoot.localRotation;
            leftLegRest = leftLeg.localRotation; rightLegRest = rightLeg.localRotation;
            if (upperBody != null) bodyPosition = upperBody.localPosition;
        }

        private Vector3 HoldTarget()
        {
            float gait = Mathf.Sin(phase) * movementBlend;
            return Vector3.Lerp(new Vector3(.67f, -.23f, .35f), new Vector3(.66f, -.04f, .43f), sprintBlend)
                   + new Vector3(0f, Mathf.Abs(gait) * .018f, gait * .035f);
        }

        private void LateUpdate()
        {
            if (visualRoot == null || upperBody == null) return;
            float speed = controller == null ? 0f : new Vector2(controller.velocity.x, controller.velocity.z).magnitude;
            movementBlend = Mathf.MoveTowards(movementBlend, Mathf.Clamp01(speed / 1.2f), Time.deltaTime * 7f);
            sprintBlend = Mathf.MoveTowards(sprintBlend, player != null && player.IsSprinting ? 1f : 0f, Time.deltaTime * 5f);
            phase += Time.deltaTime * Mathf.Lerp(strideSpeed, 14.5f, sprintBlend) * movementBlend;
            float gait = Mathf.Sin(phase);
            float swing = gait * Mathf.Lerp(strideAngle, 38f, sprintBlend) * movementBlend;
            Vector3 rightTarget = equipment != null && equipment.AxeEquipped ? HoldTarget() : new Vector3(.56f, -.34f, .13f + gait * .17f * movementBlend);
            Vector3 leftTarget = new(-.57f, -.30f + sprintBlend * .14f, .14f - gait * .20f * movementBlend);
            Vector3 rotation = new(4f * sprintBlend, -gait * 2f * movementBlend, 0f);
            Vector3 weight = Vector3.zero;
            if (attacking)
            {
                attackTime += Time.deltaTime;
                float t = Mathf.Clamp01(attackTime / AttackDuration);
                rightTarget = Arc(t, attackStart, new Vector3(.67f, .62f, .21f), new Vector3(.70f, -.12f, .57f), new Vector3(.68f, -.30f, .40f), HoldTarget());
                rotation += Arc(t, Vector3.zero, new Vector3(-7f, -17f, -4f), new Vector3(12f, 13f, 5f), new Vector3(9f, 17f, 3f), Vector3.zero);
                weight = Arc(t, Vector3.zero, new Vector3(-.035f, .018f, -.035f), new Vector3(.035f, -.028f, .05f), new Vector3(.025f, -.018f, .04f), Vector3.zero);
                leftTarget += Arc(t, Vector3.zero, new Vector3(-.035f, .18f, .12f), new Vector3(-.05f, .12f, -.03f), new Vector3(-.02f, .06f, -.07f), Vector3.zero);
                if (t >= 1f) attacking = false;
            }
            leftLeg.localRotation = leftLegRest * Quaternion.Euler(swing - weight.z * 40f, 0f, 0f);
            rightLeg.localRotation = rightLegRest * Quaternion.Euler(-swing + weight.z * 25f, 0f, 0f);
            visualRoot.localPosition = rootPosition + Vector3.up * (Mathf.Abs(gait) * .02f * movementBlend);
            visualRoot.localRotation = rootRotation;
            upperBody.localPosition = bodyPosition + weight;
            upperBody.localRotation = Quaternion.Euler(rotation);
            SolveArm(rightArm, rightElbow, rightWrist, rightTarget, 1f, true);
            SolveArm(leftArm, leftElbow, leftWrist, leftTarget, -1f, false);
        }

        private static Vector3 Arc(float t, Vector3 idle, Vector3 windup, Vector3 contact, Vector3 follow, Vector3 recovery)
        {
            if (t < .30f) return Vector3.Lerp(idle, windup, Mathf.SmoothStep(0f, 1f, t / .30f));
            if (t < .49f) return Vector3.Lerp(windup, contact, Mathf.SmoothStep(0f, 1f, (t - .30f) / .19f));
            if (t < .64f) return Vector3.Lerp(contact, follow, Mathf.SmoothStep(0f, 1f, (t - .49f) / .15f));
            return Vector3.Lerp(follow, recovery, Mathf.SmoothStep(0f, 1f, (t - .64f) / .36f));
        }

        private void SolveArm(Transform shoulder, Transform elbow, Transform wrist, Vector3 localTarget, float side, bool holdsAxe)
        {
            Vector3 origin = shoulder.position;
            Vector3 target = upperBody.TransformPoint(localTarget);
            Vector3 delta = target - origin;
            float distance = Mathf.Clamp(delta.magnitude, .15f, UpperLength + LowerLength - .025f);
            Vector3 direction = delta.normalized;
            target = origin + direction * distance;
            Vector3 pole = upperBody.TransformDirection(new Vector3(side * .8f, -.4f, -.45f));
            Vector3 bend = (pole - Vector3.Dot(pole, direction) * direction).normalized;
            float along = (UpperLength * UpperLength - LowerLength * LowerLength + distance * distance) / (2f * distance);
            Vector3 joint = origin + direction * along + bend * Mathf.Sqrt(Mathf.Max(0f, UpperLength * UpperLength - along * along));
            shoulder.rotation = Quaternion.FromToRotation(Vector3.down, (joint - origin).normalized);
            elbow.position = joint;
            elbow.rotation = Quaternion.FromToRotation(Vector3.down, (target - joint).normalized);
            wrist.position = target;
            Vector3 forearm = (target - joint).normalized;
            Vector3 bladeNormal = upperBody.right;
            bladeNormal = (bladeNormal - Vector3.Dot(bladeNormal, forearm) * forearm).normalized;
            Vector3 shaft = Vector3.Cross(forearm, bladeNormal).normalized;
            // The imported axe wrapper has its shaft along local Y and grip at the origin.
            wrist.rotation = holdsAxe ? Quaternion.LookRotation(bladeNormal, shaft) : elbow.rotation;
        }

        public bool PlayAttack()
        {
            if (attacking) return false;
            attacking = true;
            LastAttackType = 0;
            attackTime = 0f;
            attackStart = HoldTarget();
            return true;
        }
    }
}
