﻿using System;
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
    
    public struct ObstacleMayDisableFlag : IComponentData
    {
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
    public struct ObstacleRender : IComponentData
    {
        public float4 color;
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
            in float2 normalFromObstacle,
            in float normalizedDistanceFromMe,
            in Boid boidSettings,
            in float2 boidLinearVelocity)
        {
            var awayFromObstacleNormal = normalFromObstacle;

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
    public struct SdfShapeComponent : IComponentData
    {
        public ShapeDataDefinition shapeData;
        
        public readonly float ReceivesDrag(in LocalToWorld localToWorld, float2 relativeToCenter)
        {
            var shape = shapeData.GetWorldSpace(localToWorld);
            var normalizedDistance = shape.GetNormalizedDistance(relativeToCenter);
            return normalizedDistance;
        }
        
        public readonly Obstacle GetWorldSpace(in LocalToWorld localToWorld, in ObstacleComponent obstacleBehavor)
        {
            return new Obstacle
            {
                behavior = obstacleBehavor.behavior,
                shape = this.shapeData.GetWorldSpace(localToWorld),
                obstacleHardSurfaceRadiusFraction = obstacleBehavor.obstacleHardSurfaceRadiusFraction,
            };
        }
    }
    
    [Serializable]
    public struct ObstacleComponent : IComponentData
    {
        public ObstacleBehavior behavior;
        public float obstacleHardSurfaceRadiusFraction;
    }
    
    public struct OriginalColor : IComponentData
    {
        public float4 Color;
    }
}