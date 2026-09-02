using UnityEngine;

namespace CrystalSprint
{
    public enum SurfaceType
    {
        Grass,
        Wood,
        Stone
    }

    public sealed class SurfaceMarker : MonoBehaviour
    {
        [SerializeField] private SurfaceType surfaceType;

        public SurfaceType Type => surfaceType;

        public void Configure(SurfaceType type) => surfaceType = type;
    }
}
