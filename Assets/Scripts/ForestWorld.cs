using UnityEngine;

namespace CrystalSprint
{
    // Shared layout rules keep terrain, vegetation and the protected walking routes in agreement.
    public static class ForestWorld
    {
        public const float Size = 135f;
        public const float Radius = 58.69f;
        public const int TreeCount = 240;
        public const int BushCount = 140;
        public const string Kit = "Assets/Vegetation_Stylized_Pack_ByLuxArtStudios";

        public static float Height(float x, float z)
        {
            float broad = (Mathf.PerlinNoise(x * .032f + 8.4f, z * .032f + 3.1f) - .5f) * .72f;
            float detail = (Mathf.PerlinNoise(x * .085f + 21.7f, z * .085f + 14.2f) - .5f) * .16f;
            float rolling = Mathf.Sin(x * .075f) * Mathf.Cos(z * .063f) * .12f;
            float a = Mathf.Atan2(z, x);
            float shape = 1f + Mathf.Sin(a * 3f + .4f) * .075f + Mathf.Sin(a * 5f - 1.1f) * .045f + Mathf.Sin(a * 9f + .8f) * .022f;
            float depression = (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4.35f * shape, 9.65f * shape, new Vector2(x, z).magnitude))) * .78f;
            return broad + detail + rolling - depression;
        }

        public static float PathDistance(Vector2 p)
        {
            float northSouth = Mathf.Abs(p.x - 2.3f * Mathf.Sin(p.y * .065f));
            float eastWest = Mathf.Abs(p.y - 15f - 4f * Mathf.Sin(p.x * .065f));
            float pondLoop = Mathf.Abs(p.magnitude - 11.2f);
            return Mathf.Min(northSouth, eastWest, pondLoop);
        }
    }
}
