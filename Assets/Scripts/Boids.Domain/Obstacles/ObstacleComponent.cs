using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;

namespace Boids.Domain.Obstacles
{
    public enum ObstacleBehaviorVariant
    {
        None = 0,
        Repel,
        Attract,
    }

    public enum ObstacleShapeVariant
    {
        Sphere,
        Beam,
    }
    
    public struct ObstacleDisabledFlag : IComponentData
    {
    }

    [Serializable]
    public struct ObstacleBehavior
    {
        public ObstacleBehaviorVariant variant;
        public float obstacleEffectMultiplier;
        public float maxEffectMagnitude;
    }

    [Serializable]
    public struct ObstacleShape
    {
        public ObstacleShapeVariant shapeVariant;
        
        public float obstacleRadius;
        /// <inheritdoc cref="ObstacleShapeDataDefinition.obstacleSecondarySize"/>
        public float obstacleSecondarySize;
        /// <summary>
        /// unused for rotationally symmetric obstacles (spheres)
        /// </summary>
        public float obstacleRotation;

        /// <summary>
        /// gets the maximum distance from the center which could be affected by this obstacle
        /// </summary>
        /// <remarks>
        /// The actual obstacle may be smaller, but will not be larger.
        /// </remarks>
        public float MaximumExtent()
        {
            return obstacleRadius + obstacleSecondarySize;
        }
        
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
            switch (shapeVariant)
            {
                case ObstacleShapeVariant.Sphere:
                    var len = math.length(relativeToCenter);
                    return (len, relativeToCenter.NormalizeSafeWithLen(len));
                case ObstacleShapeVariant.Beam:
                    // 2d rotation
                    var rotatedRelative = math.mul(float2x2.Rotate(-obstacleRotation), relativeToCenter);
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
                    var worldSpaceNormal = math.mul(float2x2.Rotate(obstacleRotation), localSpaceNormal);
                    return (distance, worldSpaceNormal);
                default:
                    throw new NotImplementedException("Unknown obstacle shape");
            }
        }
    }

    [Serializable]
    public struct ObstacleShapeDataDefinition
    {
        public ObstacleShapeVariant shapeVariant;
        
        [Range(1f, 30f)]
        public float obstacleRadius;
        
        /// <summary>
        /// for sphere, unused
        /// for beam, the length of the beam
        /// </summary>
        public float obstacleSecondarySize;
        
        public readonly ObstacleShape GetWorldSpace(in LocalToWorld localToWorld)
        {
            var presumedLinearScale = localToWorld.Value.GetPresumedLinearScale();
            var rotation = math.Euler(localToWorld.Value.Rotation()).z;
            return AdjustForScale(presumedLinearScale, rotation);
        }
        private readonly ObstacleShape AdjustForScale(float linearScale, float rotation)
        {
            return new ObstacleShape
            {
                shapeVariant = this.shapeVariant,
                obstacleRadius = this.obstacleRadius * linearScale,
                obstacleSecondarySize = this.obstacleSecondarySize * linearScale,
                obstacleRotation = rotation,
            };
        }
    }

    [Serializable]
    public struct Obstacle
    {
        public ObstacleBehavior behavior;
        public ObstacleShape shape;

        public float obstacleHardSurfaceRadiusFraction;

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
            switch (behavior.variant)
            {
                case ObstacleBehaviorVariant.Repel:
                    upToSurfaceOfObstacle += awayFromObstacleNormal * boidSettings.obstacleAvoidanceConstantRepellent;
                    var hardSurface = normalizedDistanceFromMe < obstacleHardSurfaceRadiusFraction;
                    if (!hardSurface)
                    {
                        upToSurfaceOfObstacle = upToSurfaceOfObstacle.ClampMagnitude(behavior.maxEffectMagnitude);
                        return (upToSurfaceOfObstacle, false);
                    }
                    
                    // reflect away from the hard surface
                    var reflectedHeading = math.reflect(boidLinearVelocity, awayFromObstacleNormal);
                    var reflectedAwayFromObstacle = math.dot(reflectedHeading, awayFromObstacleNormal) > 0;
                    reflectedHeading = math.select(boidLinearVelocity, reflectedHeading , reflectedAwayFromObstacle);
                    var resultHeading = reflectedHeading + upToSurfaceOfObstacle * boidSettings.obstacleAvoidanceWeight;
                    return (resultHeading, true);
                case ObstacleBehaviorVariant.Attract:
                    float2 towardsObstacleSteering = -upToSurfaceOfObstacle * behavior.obstacleEffectMultiplier;
                    towardsObstacleSteering = towardsObstacleSteering.ClampMagnitude(behavior.maxEffectMagnitude);
                    return (towardsObstacleSteering, false);
                case ObstacleBehaviorVariant.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [Serializable]
    public struct ObstacleComponent : IComponentData
    {
        public ObstacleBehavior behavior;
        public ObstacleShapeDataDefinition shapeData;
        public float obstacleHardSurfaceRadiusFraction;

        public readonly Obstacle GetWorldSpace(in LocalToWorld localToWorld)
        {
            return new Obstacle
            {
                behavior = this.behavior,
                shape = this.shapeData.GetWorldSpace(localToWorld),
                obstacleHardSurfaceRadiusFraction = obstacleHardSurfaceRadiusFraction,
            };
        }
    }
    
    public struct OriginalColor : IComponentData
    {
        public float4 Color;
    }
}