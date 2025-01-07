using Unity.Mathematics;
using UnityEditor.Graphs;
using UnityEngine;

namespace Boids.Domain
{
    public static class MathExtensions
    {
        public static float GetPresumedLinearScale(this float4x4 matrix)
        {
            return math.length(matrix.c0.xyz);
        }

        public static float4 ToFloat4(this Color color)
        {
            return new float4(color.r, color.g, color.b, color.a);
        }
        
        public static Color ToColor(this float4 color)
        {
            return new Color(color.x, color.y, color.z, color.w);
        }
    }
}