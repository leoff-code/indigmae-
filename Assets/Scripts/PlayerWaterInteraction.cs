using UnityEngine;

namespace CrystalSprint
{
    [DefaultExecutionOrder(15)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerWaterInteraction : MonoBehaviour
    {
        [SerializeField] private PondSurfaceMotion surface;
        [SerializeField] private GameObject ripplePrefab, splashPrefab;
        private CharacterController body;
        private PlayerController player;
        private bool wasWet;
        private float nextStep;
        private Vector3 lastWetPosition;
        public int EntryCount { get; private set; }
        public int ExitCount { get; private set; }
        public int StepEffectCount { get; private set; }
        public bool IsInWater => surface != null && body != null &&
            body.bounds.min.y < surface.SampleHeight(transform.position) + .08f && surface.ContainsWater(transform.position);

        public void Configure(PondSurfaceMotion water, GameObject ripple, GameObject splash)
        { surface = water; ripplePrefab = ripple; splashPrefab = splash; }

        private void Awake() { body = GetComponent<CharacterController>(); player = GetComponent<PlayerController>(); }

        private void Update()
        {
            if (surface == null) return;
            bool wet = IsInWater;
            if (wet)
            {
                lastWetPosition = transform.position;
                lastWetPosition.y = surface.SampleHeight(lastWetPosition);
            }
            if (wet != wasWet)
            {
                Emit(lastWetPosition, true);
                if (wet) EntryCount++; else ExitCount++;
                nextStep = Time.time + .42f;
            }
            else if (wet && player.IsGrounded && player.PlanarSpeed > .8f && Time.time >= nextStep)
            {
                Emit(lastWetPosition, false);
                StepEffectCount++;
                nextStep = Time.time + (player.IsSprinting ? .30f : .46f);
            }
            wasWet = wet;
        }

        private void Emit(Vector3 point, bool crossing)
        {
            if (!surface.ContainsWater(point)) return;
            point.y = surface.SampleHeight(point);
            if (ripplePrefab != null)
            {
                GameObject ripple = Instantiate(ripplePrefab, point + Vector3.up * .02f, Quaternion.identity);
                ripple.name = crossing ? "Player Shore Ripple" : "Player Wading Ripple";
                WaterRippleEffect effect = ripple.GetComponent<WaterRippleEffect>();
                effect.FollowSurface(surface); effect.Configure(crossing ? 1.2f : .9f, crossing ? 2f : 1.15f);
            }
            if (splashPrefab != null)
            {
                GameObject splash = Instantiate(splashPrefab, point + Vector3.up * .025f, Quaternion.identity);
                splash.name = crossing ? "Player Shore Splash" : "Player Wading Droplets";
                var main = splash.GetComponent<ParticleSystem>().main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                splash.transform.localScale = Vector3.one * (crossing ? .52f : .22f);
            }
        }
    }
}
