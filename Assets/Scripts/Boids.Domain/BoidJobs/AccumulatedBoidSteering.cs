﻿using System;
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

        private Obstacle _nearestObstacle;
        private float2 _nearestObstacleRelative;
        private float _nearestObstacleDistance;
        private float _nearestObstacleDistanceNormalizedFromCenter;
        private bool HasObstacle => // we have a variant, and we are inside the obstacle's radius
            _nearestObstacle.variant != ObstacleType.None &&
            _nearestObstacleDistance < _nearestObstacle.obstacleRadius;

        private float2 _awayFromBounds;
        
        // private float2 _nearestTargetRelative;
        // private float _nearestTargetDistance;

        public static AccumulatedBoidSteering Empty => new()
        {
            _nearestObstacleDistance = float.MaxValue,
            _nearestObstacleDistanceNormalizedFromCenter = float.MaxValue,
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

        public void AccumulateObstacleCell(in ObstacleCellData obstacleCellData, in float2 position)
        {
            if (!obstacleCellData.IsValid) return;
            if (obstacleCellData.Obstacle.variant is not ObstacleType.SphereRepel)
            {
                throw new NotImplementedException("Different obstacles not implemented");
            }
            
            var relativeObstaclePosition = obstacleCellData.Position - position;
            var distance = math.length(relativeObstaclePosition);
            var normalizedDistanceFromCenter = distance / obstacleCellData.Obstacle.obstacleRadius;
            if (distance > 0.00001f && normalizedDistanceFromCenter < this._nearestObstacleDistanceNormalizedFromCenter)
            {
                this._nearestObstacleRelative = relativeObstaclePosition;
                this._nearestObstacleDistance = distance;
                this._nearestObstacle = obstacleCellData.Obstacle;
                this._nearestObstacleDistanceNormalizedFromCenter = normalizedDistanceFromCenter;
            }
        }
        
        public void AccumulateBounds(in BoidBoundingBox bounds, in float2 position)
        {
            var min = bounds.min;
            var max = bounds.max;
            var halfSize = (max - min) / 2;
            var center = (max + min) / 2;
            var relativePosition = position - center;
            var relativePositionClamped = math.clamp(relativePosition, -halfSize, halfSize);
            _awayFromBounds = relativePositionClamped - relativePosition;
        }
        
        public (float2 heading, bool hardSurface) GetTargetForward(in Boid boidSettings, in float2 linearVelocity, in float2 position)
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

            var fromObstacle = -_nearestObstacleRelative;
            var avoidObstacleSteering = float2.zero;
            bool hitHardObstacle = false;
            if (HasObstacle)
            {
                var toObstacle = -fromObstacle;
                var awayFromObstacleNormal = math.normalizesafe(fromObstacle);
                avoidObstacleSteering = toObstacle + awayFromObstacleNormal * _nearestObstacle.obstacleRadius;
                avoidObstacleSteering += awayFromObstacleNormal * boidSettings.obstacleAvoidanceConstantRepellent;
                hitHardObstacle = _nearestObstacleDistance < _nearestObstacle.obstacleHardSurfaceRadius;
                if (hitHardObstacle)
                {
                    // reflect away from the hard surface
                    var reflectedHeading = math.reflect(linearVelocity, fromObstacle);
                    var reflectedAwayFromObstacle = math.dot(reflectedHeading, fromObstacle) > 0;
                    reflectedHeading = math.select(linearVelocity, reflectedHeading , reflectedAwayFromObstacle);
                    var resultHeading = reflectedHeading + avoidObstacleSteering * boidSettings.obstacleAvoidanceWeight;
                    return (resultHeading, true);
                }
            }

            var flockingHeading = _separation * boidSettings.separationWeight +
                                  _alignment * boidSettings.alignmentWeight +
                                  _cohesion * boidSettings.cohesionWeight;
            
            
            var targetForward = 
                math.select(new float2(), flockingHeading, !hitHardObstacle) +
                avoidObstacleSteering * boidSettings.obstacleAvoidanceWeight +
                _awayFromBounds * boidSettings.boundsAvoidanceWeight;

            return (targetForward, hitHardObstacle);
        }
    }
}