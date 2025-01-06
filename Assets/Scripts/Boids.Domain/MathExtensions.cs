using Unity.Mathematics;

namespace Boids.Domain
{
    public static class MathExtensions
    {
        public static float GetPresumedLinearScale(this float4x4 matrix)
        {
            return math.length(matrix.c0.xyz);
        }
    }
}