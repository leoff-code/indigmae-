using UnityEngine;

namespace CrystalSprint
{
    [DefaultExecutionOrder(80)]
    public sealed class FirstPersonViewmodel : MonoBehaviour
    {
        [SerializeField] private FirstPersonCamera view;
        [SerializeField] private PlayerController player;
        [SerializeField] private LumberjackEquipment equipment;
        [SerializeField] private LumberjackVisual worldAnimation;
        [SerializeField] private Transform leftShoulder, leftElbow, leftWrist;
        [SerializeField] private Transform rightShoulder, rightElbow, rightWrist;
        [SerializeField] private GameObject axe;
        private readonly RaycastHit[] obstructionHits = new RaycastHit[12];
        private float gait, movement, sprint, wallBlend;
        private Vector2 sway;
        private AxeChopping chopping;
        public Transform RightWrist => rightWrist;
        public Transform LeftWrist => leftWrist;
        public Transform RightElbow => rightElbow;
        public Transform Axe => axe.transform;
        public float WallRetraction => wallBlend;
        public bool AxeVisible => axe != null && axe.activeSelf;

        public void ConfigureRig(Transform ls, Transform le, Transform lw, Transform rs, Transform re, Transform rw, GameObject heldAxe)
        {
            leftShoulder = ls; leftElbow = le; leftWrist = lw;
            rightShoulder = rs; rightElbow = re; rightWrist = rw; axe = heldAxe;
            EvaluatePose(0f);
        }

        public void Bind(FirstPersonCamera camera, PlayerController owner)
        {
            view = camera; player = owner;
            equipment = owner.GetComponent<LumberjackEquipment>();
            worldAnimation = owner.GetComponent<LumberjackVisual>();
            chopping = owner.GetComponent<AxeChopping>();
        }

        private void LateUpdate()
        {
            if (view == null || !view.isActiveAndEnabled || player == null) return;
            bool equipped = equipment != null && equipment.AxeEquipped;
            if (axe.activeSelf != equipped) axe.SetActive(equipped);
            movement = Mathf.MoveTowards(movement, player.IsGrounded ? Mathf.Clamp01(player.PlanarSpeed / 6.8f) : 0f, Time.deltaTime * 6f);
            sprint = Mathf.MoveTowards(sprint, player.IsSprinting ? 1f : 0f, Time.deltaTime * 5f);
            gait += Time.deltaTime * Mathf.Lerp(8.5f, 12f, sprint) * movement;
            sway = Vector2.Lerp(sway, Vector2.ClampMagnitude(-view.LookDelta, 1.5f), 1f - Mathf.Exp(-Time.deltaTime * 18f));
            float nearest = 1.1f;
            int hits = Physics.SphereCastNonAlloc(view.transform.position, .12f, view.transform.forward, obstructionHits, nearest, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits; i++)
                if (!obstructionHits[i].transform.IsChildOf(player.transform)) nearest = Mathf.Min(nearest, obstructionHits[i].distance);
            wallBlend = Mathf.Lerp(wallBlend, 1f - Mathf.InverseLerp(.35f, 1.05f, nearest), 1f - Mathf.Exp(-Time.deltaTime * 14f));
            transform.localPosition = new Vector3(Mathf.Sin(gait) * .009f * movement, Mathf.Abs(Mathf.Cos(gait)) * .009f * movement - wallBlend * .025f, -wallBlend * .10f);
            transform.localRotation = Quaternion.Euler(sway.y * .65f, sway.x * .7f, -Mathf.Sin(gait) * .8f * movement);
            if (chopping == null) chopping = player.GetComponent<AxeChopping>();
            float pose = worldAnimation != null && worldAnimation.IsAttacking ? worldAnimation.AttackProgress : 0f;
            EvaluatePose(chopping != null ? chopping.PoseProgress(pose) : pose);
        }

        public void EvaluatePose(float attackProgress)
        {
            if (rightShoulder == null) return;
            Vector3 hold = Vector3.Lerp(new Vector3(.29f, -.30f, .62f), new Vector3(.31f, -.34f, .57f), sprint);
            Vector3 right = hold;
            Vector3 left = new(-.28f, -.31f, .57f);
            float wristFlex = 12f;
            if (attackProgress > 0f)
            {
                right = Arc(attackProgress, hold, new Vector3(.33f, -.28f, .57f), new Vector3(.17f, -.32f, .65f), new Vector3(.14f, -.35f, .59f), hold);
                left += Arc(attackProgress, Vector3.zero, new Vector3(-.015f, .02f, .035f), new Vector3(-.03f, -.015f, 0f), new Vector3(-.02f, -.02f, -.02f), Vector3.zero);
                wristFlex = Arc(attackProgress, Vector3.right * 12f, Vector3.right * 4f, Vector3.right * 32f, Vector3.right * 42f, Vector3.right * 12f).x;
            }
            SolveArm(rightShoulder, rightElbow, rightWrist, right, 1f, wristFlex);
            SolveArm(leftShoulder, leftElbow, leftWrist, left, -1f, 0f);
        }

        private static Vector3 Arc(float t, Vector3 idle, Vector3 windup, Vector3 contact, Vector3 follow, Vector3 recovery)
        {
            if (t < .30f) return Vector3.Lerp(idle, windup, Mathf.SmoothStep(0f, 1f, t / .30f));
            if (t < .49f) return Vector3.Lerp(windup, contact, Mathf.SmoothStep(0f, 1f, (t - .30f) / .19f));
            if (t < .64f) return Vector3.Lerp(contact, follow, Mathf.SmoothStep(0f, 1f, (t - .49f) / .15f));
            return Vector3.Lerp(follow, recovery, Mathf.SmoothStep(0f, 1f, (t - .64f) / .36f));
        }

        private void SolveArm(Transform shoulder, Transform elbow, Transform wrist, Vector3 localTarget, float side, float wristFlex)
        {
            const float upper = .35f, lower = .38f;
            Vector3 start = shoulder.position;
            Vector3 delta = transform.TransformPoint(localTarget) - start;
            float distance = Mathf.Clamp(delta.magnitude, .2f, upper + lower - .02f);
            Vector3 direction = delta.normalized;
            Vector3 target = start + direction * distance;
            Vector3 pole = transform.TransformDirection(new Vector3(side, .28f, -.06f));
            Vector3 bend = Vector3.ProjectOnPlane(pole, direction).normalized;
            float along = (upper * upper - lower * lower + distance * distance) / (2f * distance);
            Vector3 joint = start + direction * along + bend * Mathf.Sqrt(Mathf.Max(0f, upper * upper - along * along));
            shoulder.rotation = Quaternion.FromToRotation(Vector3.down, (joint - start).normalized);
            elbow.position = joint;
            elbow.rotation = Quaternion.FromToRotation(Vector3.down, (target - joint).normalized);
            wrist.position = target;
            Vector3 forearm = (target - joint).normalized;
            Vector3 bladeNormal = Vector3.ProjectOnPlane(transform.right, forearm).normalized;
            Vector3 shaft = Quaternion.AngleAxis(wristFlex, bladeNormal) * Vector3.Cross(forearm, bladeNormal).normalized;
            wrist.rotation = Quaternion.LookRotation(bladeNormal, shaft);
        }
    }
}
