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
        public float NormalizedDistanceFromCenter;

        public bool IsValid => NormalizedDistanceFromCenter < float.MaxValue;
        
        public static ObstacleCellData Empty => new ObstacleCellData
        {
            NormalizedDistanceFromCenter = float.MaxValue
        };
        
        public void Accumulate(in Obstacle obstacle, in float2 obstaclePos, in float2 cellCenter)
        {
            var obstacleRelative = obstaclePos - cellCenter;
            var sqDistance = math.lengthsq(obstacleRelative);
            if (sqDistance > obstacle.RadiusSq)
            {
                return;
            }
            
            var distance = math.sqrt(sqDistance);
            var normalizedDistanceFromCenter = distance / obstacle.obstacleRadius;
            if (normalizedDistanceFromCenter >= NormalizedDistanceFromCenter) return;
            
            Obstacle = obstacle;
            Position = obstaclePos;
            NormalizedDistanceFromCenter = normalizedDistanceFromCenter;
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