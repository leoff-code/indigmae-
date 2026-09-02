using UnityEngine;

namespace CrystalSprint
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class GroundMovementParticles : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particles;
        [SerializeField, Min(0.02f)] private float emissionInterval = 0.09f;
        [SerializeField, Min(0f)] private float minimumSpeed = 0.7f;

        private CharacterController controller;
        private float emissionTimer;
        private PlayerWaterInteraction water;

        public SurfaceType CurrentSurface { get; private set; } = SurfaceType.Grass;

        public void Configure(ParticleSystem particleSystem) => particles = particleSystem;

        private void Awake() { controller = GetComponent<CharacterController>(); water = GetComponent<PlayerWaterInteraction>(); }

        private void Update()
        {
            if (particles == null || controller == null || !controller.isGrounded || (water != null && water.IsInWater))
            {
                return;
            }

            Vector2 planarVelocity = new(controller.velocity.x, controller.velocity.z);
            if (planarVelocity.magnitude < minimumSpeed)
            {
                emissionTimer = 0f;
                return;
            }

            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.35f, ~0, QueryTriggerInteraction.Ignore))
            {
                SurfaceMarker marker = hit.collider.GetComponentInParent<SurfaceMarker>();
                CurrentSurface = marker != null ? marker.Type : SurfaceType.Grass;
            }

            emissionTimer -= Time.deltaTime;
            if (emissionTimer > 0f)
            {
                return;
            }

            emissionTimer = emissionInterval;
            ParticleSystem.EmitParams emit = new()
            {
                startColor = SurfaceColor(CurrentSurface),
                startSize = CurrentSurface == SurfaceType.Stone ? 0.105f : 0.075f,
                startLifetime = CurrentSurface == SurfaceType.Stone ? 0.48f : 0.38f,
                velocity = Vector3.up * 0.32f - transform.forward * 0.42f + Random.insideUnitSphere * 0.16f
            };
            particles.Emit(emit, 2);
        }

        private static Color SurfaceColor(SurfaceType type) => type switch
        {
            SurfaceType.Wood => new Color(0.72f, 0.43f, 0.16f, 0.86f),
            SurfaceType.Stone => new Color(0.86f, 0.88f, 0.84f, 0.72f),
            _ => new Color(0.38f, 0.28f, 0.09f, 0.78f)
        };
    }
}
