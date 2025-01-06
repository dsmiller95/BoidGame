using System;
using Unity.Mathematics;

namespace Boids.Domain.Obstacles
{
    [Serializable]
    public struct ObstacleFunction
    {
        public static readonly ObstacleFunction Default = new ObstacleFunction
        {
            spiralSpacing = 10f,
            angleDensityMultiplier = 16f / math.PI2,
        };
        
        public float spiralSpacing;
        public float angleDensityMultiplier;

        public readonly float GetMaximumDensity()
        {
            return 2 * angleDensityMultiplier / (spiralSpacing * spiralSpacing);
        }
        
        public readonly float2 GetObstacleFromField(float2 myPos)
        {
            float dist = math.length(myPos);
            float distBucketIndex = math.round(dist / spiralSpacing);
            if(dist < 0.0001f || distBucketIndex < 0.0001f)
            {
                return new float2(0, 0);
            }
            
            float angle = math.atan2(myPos.y, myPos.x);
            float bucketedDist = distBucketIndex * spiralSpacing;
            float angleSpacing = 1f/(distBucketIndex * angleDensityMultiplier);
    
            float bucketedAngle = math.round(angle / angleSpacing) * angleSpacing;
    
    
            var result = new float2(math.cos(bucketedAngle), math.sin(bucketedAngle)) * bucketedDist;
            return result;
        }
    }
}