using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Boids.Domain.Obstacles
{
    public enum ObstacleType
    {
        None = 0,
        SphereRepel,
        //SphereAttract,
    }
    
    public struct ObstacleDisabledFlag : IComponentData
    {
    }

    [Serializable]
    public struct Obstacle
    {
        public ObstacleType variant;
        public float obstacleRadius;
        public float obstacleHardSurfaceRadiusFraction;

        /// <summary>
        /// Get the distance from the center of the obstacle.
        /// </summary>
        /// <param name="queryRelativeToCenter">the query point relative to the center of this obstacle</param>
        /// <returns>A value in [0..1) if inside the obstacle radius, or [1..) if outside</returns>
        /// <exception cref="NotImplementedException"></exception>
        public readonly float GetNormalizedDistance(in float2 queryRelativeToCenter)
        {
            if (variant is not ObstacleType.SphereRepel)
            {
                throw new NotImplementedException("Only SphereRepel is supported");
            }
            
            var distance = math.length(queryRelativeToCenter);
            var normalizedDistance = distance / obstacleRadius;
            return normalizedDistance;
        }
    }

    [Serializable]
    public struct ObstacleComponent : IComponentData
    {
        public ObstacleType variant;
        public float obstacleRadius;
        public float obstacleHardSurfaceRadius;

        public readonly Obstacle AdjustForScale(float linearScale)
        {
            return new Obstacle
            {
                variant = this.variant,
                obstacleRadius = this.obstacleRadius * linearScale,
                obstacleHardSurfaceRadiusFraction = this.obstacleHardSurfaceRadius / this.obstacleRadius,
            };
        }
    }
    
    public struct OriginalColor : IComponentData
    {
        public float4 Color;
    }
}