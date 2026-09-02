using UnityEngine;

namespace CrystalSprint
{
    // The complete existing forest/pond terrain inside 55 m is preserved bit-for-bit.
    public sealed class IslandCoast : MonoBehaviour
    {
        public const float SeaLevel = -2.4f;
        public static float ShoreRadius(float angle) => 73f + 4f * Mathf.Sin(angle * 3f + .7f)
            + 2.6f * Mathf.Sin(angle * 5f - 1.1f) + 1.3f * Mathf.Sin(angle * 9f);
        public static float Progress(float x, float z)
        {
            float radius = new Vector2(x, z).magnitude;
            return Mathf.InverseLerp(55f, ShoreRadius(Mathf.Atan2(z, x)), radius);
        }
        public static float Height(float x, float z, float original)
        {
            float radius = new Vector2(x, z).magnitude;
            if (radius <= 55f) return original;
            float shore = ShoreRadius(Mathf.Atan2(z, x));
            float t = (radius - 55f) / (shore - 55f);
            return t <= 1f ? Mathf.Lerp(original, SeaLevel, Mathf.SmoothStep(0f, 1f, t))
                : SeaLevel - (radius - shore) * .19f;
        }
        public static float GrassCoverage(Vector3 p) => 1f - Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(.02f, .42f, Progress(p.x, p.z)));
    }
}
