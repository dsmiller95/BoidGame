using System;
using Boids.Domain.Obstacles;
using Unity.Mathematics;
using UnityEngine;

namespace Boids.Domain.BoidJobs
{
    internal struct AccumulatedBoidSteering
    {
        private float2 _separation;
        private int _separationCount;
            
        private float2 _alignment;
        private int _alignmentCount;
            
        private float2 _cohesion;
        private int _cohesionCount;

        private float2 _nearestObstacleRelative;
        private float _nearestObstacleDistance;
        
        // private float2 _nearestTargetRelative;
        // private float _nearestTargetDistance;

        public static AccumulatedBoidSteering Empty => new()
        {
            _nearestObstacleDistance = float.MaxValue
        };
            
        public void Accumulate(in Boid boidSettings, in OtherBoidData otherBoid, in float2 toNeighbor, in float distance)
        {
            if (distance < boidSettings.separationRadius)
            {
                _separationCount++;
                    
                var fromNeighbor = -toNeighbor;
                var fromNeighborNormalized = fromNeighbor / distance;
                var separationAdjustment = 1f - (distance / boidSettings.separationRadius);
                //separationAdjustment = separationAdjustment * separationAdjustment;
                //separationAdjustment = Mathf.Clamp01(separationAdjustment);
                _separation += fromNeighborNormalized * (0.5f + separationAdjustment * 0.5f);
            }

            if (distance < boidSettings.alignmentRadius)
            {
                _alignmentCount++;
                _alignment += otherBoid.Velocity;
            }

            if (distance < boidSettings.cohesionRadius)
            {
                _cohesionCount++;
                _cohesion += otherBoid.Position;
            }
        }
            
        public void AccumulateObstacle(in float2 relativeObstaclePosition)
        {
            var distance = math.length(relativeObstaclePosition);
            if (distance < _nearestObstacleDistance && distance > 0.00001f)
            {
                _nearestObstacleRelative = relativeObstaclePosition;
                _nearestObstacleDistance = distance;
            }
        }
        public void AccumulateObstacle(in ObstacleData obstacleData, in float2 position)
        {
            if (obstacleData.Obstacle.variant is not ObstacleType.SphereRepel)
            {
                throw new NotImplementedException("Different obstacles not implemented");
            }
            
            var relativeObstaclePosition = obstacleData.Position - position;
            this.AccumulateObstacle(in relativeObstaclePosition);
        }

        public float2 GetTargetForward(in Boid boidSettings, in float2 linearVelocity, in float2 position, in float deltaTime)
        {
            if(_separationCount > 0)
            {
            }
            else _separation = Vector2.zero;
            
            if(_alignmentCount > 0)
            {
                _alignment = (_alignment / _alignmentCount) - linearVelocity;
            }
            else _alignment = Vector2.zero;

            if (_cohesionCount > 0)
            {
                _cohesion /= _cohesionCount;
                _cohesion -= position;
            }
            else _cohesion = Vector2.zero;

            var fromObstacle = -_nearestObstacleRelative;;
            var obstacleDistance = _nearestObstacleDistance;
            var avoidObstacleSteering = float2.zero;
            if (obstacleDistance < boidSettings.obstacleAvoidanceRadius)
            {
                var toObstacle = -fromObstacle;
                avoidObstacleSteering = toObstacle + math.normalizesafe(fromObstacle) * boidSettings.obstacleAvoidanceRadius;
            }
                
            var targetForward = _separation * boidSettings.separationWeight +
                                _alignment * boidSettings.alignmentWeight +
                                _cohesion * boidSettings.cohesionWeight +
                                avoidObstacleSteering * boidSettings.obstacleAvoidanceWeight;

            return targetForward;
        }
    }
}