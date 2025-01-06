using Boids.Domain.Obstacles;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Boids.Domain.BoidJobs
{
    internal struct ObstacleData
    {
        public Obstacle Obstacle;
        public float2 Position;
            
        public static ObstacleData From(in Obstacle obstacle, in LocalTransform presumedWorld)
        {
            return new ObstacleData()
            {
                Position = presumedWorld.Position.xy,
                Obstacle = obstacle,
            };
        }
    }
    [BurstCompile]
    internal partial struct PopulateObstacleHashMap : IJobEntity
    {
        public SpatialHashDefinition SpatialHashDefinition;
            
        public NativeParallelMultiHashMap<int2, ObstacleData>.ParallelWriter SpatialMapWriter;
            
        private void Execute(in Obstacle obstacle, in LocalTransform presumedWorld)
        {
            var boidData = ObstacleData.From(obstacle, presumedWorld);
            var cell = SpatialHashDefinition.GetCell(presumedWorld.Position.xy);
            SpatialMapWriter.Add(cell, boidData);
        }
    }
}