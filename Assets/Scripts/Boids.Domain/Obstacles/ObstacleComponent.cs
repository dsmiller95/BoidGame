using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Boids.Domain.Obstacles
{
    public enum ObstacleType
    {
        None = 0,
        Repel,
    }

    public enum ObstacleShape
    {
        Sphere,
        Beam,
    }
    
    public struct ObstacleDisabledFlag : IComponentData
    {
    }

    [Serializable]
    public struct Obstacle
    {
        public ObstacleType variant;
        public ObstacleShape shape;
        public float obstacleRadius;
        /// <summary>
        /// for sphere, unused
        /// for beam, the length of the beam
        /// </summary>
        public float obstacleSecondarySize;
        /// <summary>
        /// unused for rotationally symmetric obstacles (spheres)
        /// </summary>
        public float obstacleRotation;
        
        public float obstacleHardSurfaceRadiusFraction;

        /// <summary>
        /// Get the distance from the center of the obstacle.
        /// </summary>
        /// <param name="queryRelativeToCenter">the query point relative to the center of this obstacle</param>
        /// <returns>A value in [0..1) if inside the obstacle radius, or [1..) if outside</returns>
        /// <exception cref="NotImplementedException"></exception>
        public readonly float GetNormalizedDistance(in float2 queryRelativeToCenter)
        {
            return GetDistance(queryRelativeToCenter) / obstacleRadius;
        }

        private readonly float GetDistance(in float2 relativeToCenter)
        {
            switch (shape)
            {
                case ObstacleShape.Sphere:
                    return math.length(relativeToCenter);
                case ObstacleShape.Beam:
                    // 2d rotation
                    var rotatedRelative = math.mul(float2x2.Rotate(-this.obstacleRotation), relativeToCenter);
                    var distanceAlongBeam = math.abs(rotatedRelative.x);
                    var distanceAboveBeam = math.abs(rotatedRelative.y);
                    var distanceFromEnd = distanceAlongBeam - obstacleSecondarySize; 
                    
                    if (distanceFromEnd <= 0)
                    {
                        return distanceAboveBeam;
                    }
                    return math.sqrt(distanceFromEnd * distanceFromEnd + distanceAboveBeam * distanceAboveBeam);
                default:
                    throw new NotImplementedException("Unknown obstacle shape");
            }
        }
        
        public readonly bool IsInsideHardSurface(in float normalizedDistance)
        {
            return normalizedDistance < obstacleHardSurfaceRadiusFraction;
        }
    }

    [Serializable]
    public struct ObstacleComponent : IComponentData
    {
        public ObstacleType variant;
        public ObstacleShape shape;
        public float obstacleRadius;
        public float obstacleSecondarySize;
        public float obstacleHardSurfaceRadius;

        public readonly Obstacle GetWorldSpace(in LocalToWorld localToWorld)
        {
            var presumedLinearScale = localToWorld.Value.GetPresumedLinearScale();
            var obstacleRotation = math.Euler(localToWorld.Value.Rotation()).z;
            return AdjustForScale(presumedLinearScale, obstacleRotation);
        }
        
        public readonly Obstacle AdjustForScale(float linearScale, float rotation)
        {
            return new Obstacle
            {
                variant = this.variant,
                shape = this.shape,
                obstacleRadius = this.obstacleRadius * linearScale,
                obstacleSecondarySize = this.obstacleSecondarySize * linearScale,
                obstacleRotation = rotation,
                obstacleHardSurfaceRadiusFraction = this.obstacleHardSurfaceRadius / this.obstacleRadius,
            };
        }
    }
    
    public struct OriginalColor : IComponentData
    {
        public float4 Color;
    }
}