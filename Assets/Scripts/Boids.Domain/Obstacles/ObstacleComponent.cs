﻿using System;
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
        Attract,
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
    public struct ObstacleVariantData
    {
        public ObstacleType variant;
        public float obstacleEffectMultiplier;
        public float maxEffectMagnitude;
    }

    [Serializable]
    public struct Obstacle
    {
        public ObstacleVariantData variantData;
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

        public readonly (float2 resultHeading, bool forceHeading) GetHeading(
            in float2 relativeToObstacle,
            in float normalizedDistanceFromMe,
            in Boid boidSettings,
            in float2 boidLinearVelocity)
        {
            var fromObstacle = relativeToObstacle;
            var toObstacle = -fromObstacle;
            var awayFromObstacleNormal = math.normalizesafe(fromObstacle);

            float2 toSurfaceOfObstacle = toObstacle + awayFromObstacleNormal * obstacleRadius;
            switch (this.variantData.variant)
            {
                case ObstacleType.Repel:
                    toSurfaceOfObstacle += awayFromObstacleNormal * boidSettings.obstacleAvoidanceConstantRepellent;
                    var hardSurface = normalizedDistanceFromMe < obstacleHardSurfaceRadiusFraction;
                    if (!hardSurface)
                    {
                        toSurfaceOfObstacle = toSurfaceOfObstacle.ClampMagnitude(variantData.maxEffectMagnitude);
                        return (toSurfaceOfObstacle, false);
                    }
                    
                    // reflect away from the hard surface
                    var reflectedHeading = math.reflect(boidLinearVelocity, fromObstacle);
                    var reflectedAwayFromObstacle = math.dot(reflectedHeading, fromObstacle) > 0;
                    reflectedHeading = math.select(boidLinearVelocity, reflectedHeading , reflectedAwayFromObstacle);
                    var resultHeading = reflectedHeading + toSurfaceOfObstacle * boidSettings.obstacleAvoidanceWeight;
                    return (resultHeading, true);
                case ObstacleType.Attract:
                    float2 towardsObstacleSteering = -toSurfaceOfObstacle * variantData.obstacleEffectMultiplier;
                    towardsObstacleSteering = towardsObstacleSteering.ClampMagnitude(variantData.maxEffectMagnitude);
                    return (towardsObstacleSteering, false);
                case ObstacleType.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [Serializable]
    public struct ObstacleComponent : IComponentData
    {
        public ObstacleVariantData variantData;
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
                variantData = this.variantData,
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