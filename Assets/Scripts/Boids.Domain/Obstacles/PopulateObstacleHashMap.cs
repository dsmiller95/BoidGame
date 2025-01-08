using Boids.Domain.Obstacles;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Boids.Domain.BoidJobs
{
    /// <summary>
    /// Data for a cell in the hashmap containing the closest obstacle to the center of that cell
    /// </summary>
    internal struct ObstacleCellData
    {
        public Obstacle Obstacle;
        public float2 Position;
        private float _normalizedDistanceFromCenter;

        public bool IsValid => _normalizedDistanceFromCenter < float.MaxValue;
        
        public static ObstacleCellData Empty => new ObstacleCellData
        {
            _normalizedDistanceFromCenter = float.MaxValue
        };
        
        public void Accumulate(in Obstacle obstacle, in float2 obstaclePos, in float2 cellCenter)
        {
            var cellRelativeToObstacle = cellCenter - obstaclePos;
            var normalizedDistanceFromCenter = obstacle.shape.GetNormalizedDistance(cellRelativeToObstacle);
            if (normalizedDistanceFromCenter >= _normalizedDistanceFromCenter) return;
            
            Obstacle = obstacle;
            Position = obstaclePos;
            _normalizedDistanceFromCenter = normalizedDistanceFromCenter;
        }
    }

    
    [BurstCompile]
    internal struct PopulateObstacleHashMap : IJobParallelFor
    {
        public SpatialHashDefinition SpatialHashDefinition;
        
        public NativeArray<int2> Buckets;
        
        [ReadOnly] public NativeArray<Obstacle> AllObstacles;
        [ReadOnly] public NativeArray<float2> AllObstaclePositions;
        public NativeParallelHashMap<int2, ObstacleCellData>.ParallelWriter ObstacleDataWriter;
        
        public void Execute(int index)
        {
            var obstacleData = ObstacleCellData.Empty;
            var bucket = Buckets[index];
            var cellCenter = SpatialHashDefinition.GetCenterOfCell(bucket);
            
            for (int i = 0; i < AllObstacles.Length; i++)
            {
                obstacleData.Accumulate(AllObstacles[i], AllObstaclePositions[i], cellCenter);
            }
            
            ObstacleDataWriter.TryAdd(bucket, obstacleData);
        }
    }
}