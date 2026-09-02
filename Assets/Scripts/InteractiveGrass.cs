using UnityEngine;

namespace CrystalSprint
{
    public sealed class InteractiveGrass : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float interactionRadius = 1.25f;
        [SerializeField, Min(0.1f)] private float foliageInteractionRadius = 1.45f;

        private static readonly int InteractorId = Shader.PropertyToID("_GrassInteractor");
        private static readonly int FoliageInteractorId = Shader.PropertyToID("_FoliageInteractor");
        private PlayerController player;
        private PlayerWaterInteraction water;

        public bool IsInteractingWithGrass { get; private set; }

        private void Awake() { player = GetComponent<PlayerController>(); water = GetComponent<PlayerWaterInteraction>(); }

        private void LateUpdate()
        {
            Vector3 position = transform.position;
            Shader.SetGlobalVector(FoliageInteractorId, new Vector4(position.x, position.y + 0.3f, position.z, foliageInteractionRadius));

            IsInteractingWithGrass = false;
            if (player != null && player.IsGrounded && (water == null || !water.IsInWater) &&
                Physics.Raycast(position + Vector3.up * 0.15f, Vector3.down, out RaycastHit hit, 1.4f, ~0, QueryTriggerInteraction.Ignore))
            {
                SurfaceMarker surface = hit.collider.GetComponentInParent<SurfaceMarker>();
                IsInteractingWithGrass = surface != null && surface.Type == SurfaceType.Grass && IslandCoast.Progress(position.x,position.z)<.25f;
                if (IsInteractingWithGrass)
                {
                    Shader.SetGlobalVector(InteractorId, new Vector4(position.x, hit.point.y, position.z, interactionRadius));
                    return;
                }
            }

            Shader.SetGlobalVector(InteractorId, new Vector4(0f, -10000f, 0f, 0f));
        }
    }
}
