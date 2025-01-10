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
        void Execute(
            [EntityIndexInQuery] int entityIndexInQuery, in LocalToWorld localToWorld,
            in ObstacleComponent obstacle, in SdfShapeComponent shape)
        {
            ObstaclePositions[entityIndexInQuery] = localToWorld.Position.xy;
            Obstacles[entityIndexInQuery] = shape.GetWorldSpace(localToWorld, obstacle);
        }
    }
}