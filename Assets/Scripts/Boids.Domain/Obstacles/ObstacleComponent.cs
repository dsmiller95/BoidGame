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
    public struct ObstacleShapeData
    {
        public ObstacleShape shape;
        
        public float obstacleRadius;
        /// <inheritdoc cref="ObstacleShapeDataDefinition.obstacleSecondarySize"/>
        public float obstacleSecondarySize;
        /// <summary>
        /// unused for rotationally symmetric obstacles (spheres)
        /// </summary>
        public float obstacleRotation;
    }

    [Serializable]
    public struct ObstacleShapeDataDefinition
    {
        public ObstacleShape shape;
        
        public float obstacleRadius;
        /// <summary>
        /// for sphere, unused
        /// for beam, the length of the beam
        /// </summary>
        public float obstacleSecondarySize;
        
        public readonly ObstacleShapeData GetWorldSpace(in LocalToWorld localToWorld)
        {
            var presumedLinearScale = localToWorld.Value.GetPresumedLinearScale();
            var rotation = math.Euler(localToWorld.Value.Rotation()).z;
            return AdjustForScale(presumedLinearScale, rotation);
        }
        private readonly ObstacleShapeData AdjustForScale(float linearScale, float rotation)
        {
            return new ObstacleShapeData
            {
                shape = this.shape,
                obstacleRadius = this.obstacleRadius * linearScale,
                obstacleSecondarySize = this.obstacleSecondarySize * linearScale,
                obstacleRotation = rotation,
            };
        }
    }

    [Serializable]
    public struct Obstacle
    {
        public ObstacleVariantData variantData;
        public ObstacleShapeData shapeData;
        public readonly ObstacleShape shape => shapeData.shape;
        public readonly float obstacleRadius => shapeData.obstacleRadius;
        /// <summary>
        /// for sphere, unused
        /// for beam, the length of the beam
        /// </summary>
        public readonly float obstacleSecondarySize => shapeData.obstacleSecondarySize;
        /// <summary>
        /// unused for rotationally symmetric obstacles (spheres)
        /// </summary>
        public readonly float obstacleRotation => shapeData.obstacleRotation;
        
        public float obstacleHardSurfaceRadiusFraction;

        /// <summary>
        /// Get the distance from the center of the obstacle.
        /// </summary>
        /// <param name="queryRelativeToCenter">the query point relative to the center of this obstacle</param>
        /// <returns>A value in [0..1) if inside the obstacle radius, or [1..) if outside</returns>
        /// <exception cref="NotImplementedException"></exception>
        public readonly float GetNormalizedDistance(in float2 queryRelativeToCenter)
        {
            var (dist, _) = GetDistanceAndNormal(queryRelativeToCenter);
            return dist / obstacleRadius;
        }
        public readonly (float, float2) GetNormalizedDistanceAndNormal(in float2 queryRelativeToCenter)
        {
            var (dist, normal) = GetDistanceAndNormal(queryRelativeToCenter);
            return (dist / obstacleRadius, normal);
        }

        private readonly (float, float2) GetDistanceAndNormal(in float2 relativeToCenter)
        {
            switch (shape)
            {
                case ObstacleShape.Sphere:
                    var len = math.length(relativeToCenter);
                    return (len, relativeToCenter.NormalizeSafeWithLen(len));
                case ObstacleShape.Beam:
                    // 2d rotation
                    var rotatedRelative = math.mul(float2x2.Rotate(-this.obstacleRotation), relativeToCenter);
                    var distanceAlongBeam = math.abs(rotatedRelative.x);
                    var distanceAboveBeam = math.abs(rotatedRelative.y);
                    var distanceFromEnd = distanceAlongBeam - obstacleSecondarySize; 
                    
                    float2 localSpaceNormal;
                    float distance;
                    if (distanceFromEnd <= 0)
                    {
                        localSpaceNormal = new float2(0, math.sign(rotatedRelative.y));
                        distance = distanceAboveBeam;
                        //return (distanceAboveBeam, worldSpaceNormal);
                    }
                    else
                    {
                        var localFromEnd = new float2(distanceFromEnd, distanceAboveBeam);
                        localFromEnd *= math.sign(relativeToCenter);
                        localSpaceNormal = math.normalizesafe(localFromEnd);
                        distance = math.sqrt(distanceFromEnd * distanceFromEnd + distanceAboveBeam * distanceAboveBeam);
                    }
                    var worldSpaceNormal = math.mul(float2x2.Rotate(this.obstacleRotation), localSpaceNormal);
                    return (distance, worldSpaceNormal);
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
            in float2 normalFromObstacle,
            in float normalizedDistanceFromMe,
            in Boid boidSettings,
            in float2 boidLinearVelocity)
        {
            var awayFromObstacleNormal = normalFromObstacle;//math.normalizesafe(fromObstacle);

            float distanceToSurface = 1 - normalizedDistanceFromMe;

            float2 upToSurfaceOfObstacle = awayFromObstacleNormal * distanceToSurface;
            switch (this.variantData.variant)
            {
                case ObstacleType.Repel:
                    upToSurfaceOfObstacle += awayFromObstacleNormal * boidSettings.obstacleAvoidanceConstantRepellent;
                    var hardSurface = normalizedDistanceFromMe < obstacleHardSurfaceRadiusFraction;
                    if (!hardSurface)
                    {
                        upToSurfaceOfObstacle = upToSurfaceOfObstacle.ClampMagnitude(variantData.maxEffectMagnitude);
                        return (upToSurfaceOfObstacle, false);
                    }
                    
                    // reflect away from the hard surface
                    var reflectedHeading = math.reflect(boidLinearVelocity, awayFromObstacleNormal);
                    var reflectedAwayFromObstacle = math.dot(reflectedHeading, awayFromObstacleNormal) > 0;
                    reflectedHeading = math.select(boidLinearVelocity, reflectedHeading , reflectedAwayFromObstacle);
                    var resultHeading = reflectedHeading + upToSurfaceOfObstacle * boidSettings.obstacleAvoidanceWeight;
                    return (resultHeading, true);
                case ObstacleType.Attract:
                    float2 towardsObstacleSteering = -upToSurfaceOfObstacle * variantData.obstacleEffectMultiplier;
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
        public ObstacleShapeDataDefinition shapeData;
        public readonly ObstacleShape shape => shapeData.shape;
        public readonly float obstacleRadius => shapeData.obstacleRadius;
        public readonly float obstacleSecondarySize => shapeData.obstacleSecondarySize;
        public float obstacleHardSurfaceRadius;

        public readonly Obstacle GetWorldSpace(in LocalToWorld localToWorld)
        {
            return new Obstacle
            {
                variantData = this.variantData,
                shapeData = this.shapeData.GetWorldSpace(localToWorld),
                obstacleHardSurfaceRadiusFraction = this.obstacleHardSurfaceRadius / this.obstacleRadius,
            };
        }
    }
    
    public struct OriginalColor : IComponentData
    {
        public float4 Color;
    }
}