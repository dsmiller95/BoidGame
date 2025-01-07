using Boids.Domain.Obstacles;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Boids.Domain.BoidJobs
{
    [BurstCompile]
    partial struct CopyPerObstacleData : IJobEntity
    {
        public NativeArray<float2> ObstaclePositions;
        public NativeArray<Obstacle> Obstacles;
        void Execute([EntityIndexInQuery] int entityIndexInQuery, in LocalToWorld localToWorld, in Obstacle obstacle)
        {
            ObstaclePositions[entityIndexInQuery] = localToWorld.Position.xy;
            var presumedLinearScale = localToWorld.Value.GetPresumedLinearScale();
            Obstacles[entityIndexInQuery] = obstacle.AdjustForScale(presumedLinearScale);
        }
    }
}