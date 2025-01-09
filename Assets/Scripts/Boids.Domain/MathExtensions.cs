using Unity.Mathematics;
using UnityEngine;

namespace Boids.Domain
{
    public static class MathExtensions
    {
        public static readonly float Epsilon = 0.00001f;
        
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
        
        public static float2 ClampMagnitude(this float2 vector, float min, float max)
        {
            var sqrMagnitude = math.lengthsq(vector); 
            if (sqrMagnitude < min * min)
            {
                return math.normalizesafe(vector) * min;
            }
            if(sqrMagnitude > max * max)
            {
                return math.normalizesafe(vector) * max;
            }

            return vector;
        }
        
        public static float2 ClampMagnitude(this float2 vector, float max)
        {
            var sqrMagnitude = math.lengthsq(vector); 
            if(sqrMagnitude > max * max)
            {
                return math.normalizesafe(vector) * max;
            }
            return vector;
        }

        public static float2 NormalizeSafeWithLen(this float2 vector, float len, float2 defaultValue = new float2())
        {
            if(len < Epsilon)
            {
                return defaultValue;
            }
            return vector / len;
        }
    }
}